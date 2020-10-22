#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Settings;
using MediaPortal.Extensions.TranscodingService.Settings;

namespace MediaPortal.Extensions.TranscodingService.AnalysisDataflow
{
  /// <summary>
  /// Handles analysis and deletion for individual media items
  /// and additionally persists and restores any pending actions to disk.
  /// </summary>
  public class AnalysisActionBlock
  {
    // The maximum number of messages to process at a time.
    protected const int BATCH_SIZE = 20;

    // The maximum time in milliseconds to wait between processing batches.
    protected const int BATCH_TIMEOUT = 2000;

    private readonly CancellationTokenSource _cts;
    private readonly TaskCompletionSource<object> _tcs;

    // Media item ids to analyzed can be added in bulk, to avoid a big
    // spike in db connections this semaphore limits the number of concurrent db queries.
    protected readonly SemaphoreSlim _loadItemThrottle = new SemaphoreSlim(1, 1);

    // Persists pending actions to disk
    protected readonly ITargetBlock<object> _persistBlock;

    protected IPropagatorBlock<AnalysisManagerAction, AnalysisManagerAction[]> _inputBlock;
    // Does the actual collection/deletion
    protected readonly IPropagatorBlock<AnalysisManagerAction[], AnalysisManagerAction[]> _innerBlock;
    // Marks actions as completed
    protected readonly ITargetBlock<AnalysisManagerAction[]> _outputBlock;
    // Stores currently requested actions, used to persist any pending actions on shutdown
    protected ConcurrentDictionary<Guid, AnalysisManagerAction> _pendingMediaAnalysis;

    public bool ImportersRunning { get; set; }

    #region Constructor

    public AnalysisActionBlock()
    {
      _cts = new CancellationTokenSource();
      _tcs = new TaskCompletionSource<object>();
      _pendingMediaAnalysis = new ConcurrentDictionary<Guid, AnalysisManagerAction>();

      // This block is separate from the main block network. It has a bounded capacity
      // of 2 to ensure that at most we have 1 action being processed and 1 pending.
      // Any additional persist requests will be covered by the already pending action.
      _persistBlock = new ActionBlock<object>(_ => PersistPendingActions(),
        new ExecutionDataflowBlockOptions { BoundedCapacity = 2, MaxDegreeOfParallelism = 1 });

      // Input block that batches up multiple actions 
      _inputBlock = new TimeoutBatchBlock<AnalysisManagerAction>(BATCH_SIZE, BATCH_TIMEOUT, new GroupingDataflowBlockOptions { CancellationToken = _cts.Token });

      // Give the processing block an unlimited bounded capacity to ensure
      // that no actions are ever dropped. Any unprocessed actions will be
      // persisted and restored on server startup
      _innerBlock = new TransformBlock<AnalysisManagerAction[], AnalysisManagerAction[]>(
        new Func<AnalysisManagerAction[], Task<AnalysisManagerAction[]>>(InnerBlockMethod),
        new ExecutionDataflowBlockOptions { CancellationToken = _cts.Token, MaxDegreeOfParallelism = 2 });

      // Action block to mark actions as completed
      _outputBlock = new ActionBlock<AnalysisManagerAction[]>(
        a => OutputBlockMethod(a),
        new ExecutionDataflowBlockOptions { CancellationToken = _cts.Token });

      ISettingsManager sm = ServiceRegistration.Get<ISettingsManager>();
      var settings = sm.Load<TranscodingServiceSettings>();

      // Link the blocks and handle fault/completion propagation
      _inputBlock.LinkTo(_innerBlock, new DataflowLinkOptions { PropagateCompletion = true });
      _innerBlock.LinkTo(_outputBlock, new DataflowLinkOptions { PropagateCompletion = true });
      _inputBlock.Completion.ContinueWith(OnAnyBlockFaulted, TaskContinuationOptions.OnlyOnFaulted);
      _innerBlock.Completion.ContinueWith(OnAnyBlockFaulted, TaskContinuationOptions.OnlyOnFaulted);
      _outputBlock.Completion.ContinueWith(OnAnyBlockFaulted, TaskContinuationOptions.OnlyOnFaulted);
      Task.WhenAll(_inputBlock.Completion, _innerBlock.Completion, _outputBlock.Completion).ContinueWith(OnAllBlocksFinished);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Analyzes or deletes media items for each <see cref="AnalysisManagerAction"/> in a batch.
    /// </summary>
    /// <param name="actions">The actions to process.</param>
    /// <returns>Task that completes when the actions have been processed.</returns>
    private async Task<AnalysisManagerAction[]> InnerBlockMethod(AnalysisManagerAction[] actions)
    {
      //Persist any newly pending actions
      _persistBlock.Post(null);
      await LoadAspects(actions.Where(a => a.Type == ActionType.Analyze).ToArray());
      List<Task> actionTasks = new List<Task>();
      foreach (var action in actions)
      {
        if (action.Type == ActionType.Analyze)
          actionTasks.Add(AnalyzeMediaItem(action.MediaItemId, action.Aspects));
        else if (action.Type == ActionType.Delete)
          actionTasks.Add(DeleteAnalysis(action.MediaItemId));
      }
      await Task.WhenAll(actionTasks);
      return actions;
    }

    /// <summary>
    /// Removes the <paramref name="actions"/> from the list of pending actions
    /// so they are not restored on next server startup.
    /// </summary>
    /// <param name="actions"></param>
    private void OutputBlockMethod(AnalysisManagerAction[] actions)
    {
      AnalysisManagerAction removedAction;
      foreach (var action in actions)
        if (_pendingMediaAnalysis.TryRemove(action.ActionId, out removedAction))
          action.Aspects?.Clear();

      //Remove the completed actions from the persisted list of pending actions
      _persistBlock.Post(null);
    }

    /// <summary>
    /// Runs when any of <see cref="_innerBlock"/> or <see cref="_outputBlock"/> faults
    /// </summary>
    /// <param name="faultedTask">Completion property of the faulted DataflowBlock</param>
    private void OnAnyBlockFaulted(Task faultedTask)
    {
      // When one of the two DataflowBlocks faults, the faulted state is propagated to the following
      // DataflowBlocks, but not to the preceding DataflowBlocks. In this case we therefore fault all DataflowBlocks
      // that are not yet in a faulted state to ensure that all DataflowBlocks are completed and release their resources.
      if (!_inputBlock.Completion.IsFaulted)
        _inputBlock.Fault(faultedTask.Exception);
      if (!_innerBlock.Completion.IsFaulted)
        _innerBlock.Fault(faultedTask.Exception);
      if (!_outputBlock.Completion.IsFaulted)
        _outputBlock.Fault(faultedTask.Exception);
    }

    /// <summary>
    /// Runs when all DataflowBlocks are finished
    /// </summary>
    /// <param name="finishedTask">Task representing the state of all DataflowBlocks</param>
    private async Task OnAllBlocksFinished(Task finishedTask)
    {
      // Complete the persist block now all processing blocks have
      // finished to ensure that all pending actions are persisted.
      _persistBlock.Complete();
      await _persistBlock.Completion;

      if (finishedTask.IsFaulted)
      {
        ServiceRegistration.Get<ILogger>().Error("AnalysisActionBlock: Error processing analysis actions", finishedTask.Exception);
        // ReSharper disable once AssignNullToNotNullAttribute
        _tcs.SetException(finishedTask.Exception);
      }
      else
      {
        //Log the cancellation but set the TaskCompletionSource to RanToCompletion to
        //avoid throwing exceptions if the Completion Task is awaited
        if (finishedTask.IsCanceled)
          ServiceRegistration.Get<ILogger>().Debug("AnalysisActionBlock: Analysis actions canceled");
        _tcs.SetResult(null);
      }
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Checks whether the <paramref name="actions"/> has any aspects and if not
    /// tries to load them from the media library.
    /// </summary>
    /// <param name="actions">The action to check.</param>
    /// <returns>True if the aspects were present or successfully loaded.</returns>
    protected async Task LoadAspects(AnalysisManagerAction[] actions)
    {
      var mediaItemIds = actions.Where(a => a.Type == ActionType.Analyze && (a.Aspects == null || a.Aspects.Count == 0))
        .Select(a => a.MediaItemId).Distinct();

      var queryFilter = new MediaItemIdFilter(mediaItemIds);
      if (queryFilter.MediaItemIds.Count == 0)
        return;

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();

      MediaItemQuery query = new MediaItemQuery(null,
        mediaLibrary.GetManagedMediaItemAspectMetadata().Keys,
        queryFilter);

      // Throttle the number of concurrent queries To avoid spikes when loading aspects.
      await _loadItemThrottle.WaitAsync(_cts.Token);
      try
      {
        var items = mediaLibrary.Search(query, false, null, true);
        if (items != null && items.Count > 0)
        {
          foreach (var item in items)
            foreach (var action in actions)
              if (action.MediaItemId == item.MediaItemId)
              {
                action.Aspects = item.Aspects;
                break;
              }
        }
        else
          ServiceRegistration.Get<ILogger>().Warn("AnalysisActionBlock: Unable to load aspects, no matching media items were found in the media library");
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("AnalysisActionBlock: Error loading aspects from the media library", ex);
      }
      finally
      {
        _loadItemThrottle.Release();
      }
    }

    protected bool AddPendingAction(Guid actionId, ActionType actionType, Guid mediaItemId)
    {
      return _pendingMediaAnalysis.TryAdd(actionId, new AnalysisManagerAction(actionType, mediaItemId) { ActionId = actionId });
    }

    /// <summary>
    /// Persists all currently pending actions to disk.
    /// </summary>
    protected void PersistPendingActions()
    {
      ISettingsManager sm = ServiceRegistration.Get<ISettingsManager>();
      AnalysisActionSettings settings = sm.Load<AnalysisActionSettings>();
      settings.PendingAnalysisActions = PendingActions;
      ServiceRegistration.Get<ILogger>().Debug("AnalysisActionBlock: Persisting {0} analysis actions.", settings.PendingAnalysisActions.Length);
      sm.Save(settings);
    }

    /// <summary>
    /// Restores pending actions from disk and posts them for processing.
    /// </summary>
    protected void RestorePendingActions()
    {
      ISettingsManager sm = ServiceRegistration.Get<ISettingsManager>();
      AnalysisManagerAction[] pendingActions = sm.Load<AnalysisActionSettings>().PendingAnalysisActions;
      if (pendingActions == null || pendingActions.Length == 0)
        return;
      foreach (AnalysisManagerAction pendingAction in pendingActions)
        Post(pendingAction);
      ServiceRegistration.Get<ILogger>().Debug("AnalysisActionBlock: Restored {0} pending actions", pendingActions.Length);
    }

    /// <summary>
    /// Analyzes the given media item using the registered <see cref="IMediaAnalyzer"/>s
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="aspects">The aspects of the media item.</param>
    protected async Task AnalyzeMediaItem(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      try
      {
        if ((aspects?.Count ?? 0) == 0)
          return;

        IMediaAnalyzer analyzer = ServiceRegistration.Get<IMediaAnalyzer>();
        await analyzer.DeleteAnalysisAsync(mediaItemId); //Delete the current analysis so it can be refreshed
        MediaItem mi = new MediaItem(mediaItemId, aspects);
        await analyzer.ParseMediaItemAsync(mi);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("AnalysisActionBlock: Error analyzing media item {0}.", ex, mediaItemId);
      }
    }

    /// <summary>
    /// Deletes analysis for the given media item.
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    protected async Task DeleteAnalysis(Guid mediaItemId)
    {
      try
      {
        IMediaAnalyzer analyzer = ServiceRegistration.Get<IMediaAnalyzer>();
        await analyzer.DeleteAnalysisAsync(mediaItemId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("AnalysisActionBlock: Error deleting analysis for media item {0}.", ex, mediaItemId);
      }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Schedules a new <see cref="AnalysisManagerAction"/> for processing.
    /// </summary>
    /// <param name="action">The action to process.</param>
    /// <returns>True if the action was scheduled.</returns>
    public bool Post(AnalysisManagerAction action)
    {
      if (AddPendingAction(action.ActionId, action.Type, action.MediaItemId))
      {
        // Post the action for processing
        return _inputBlock.Post(action);
      }
      //We return success because the action is already scheduled
      return true;
    }

    public void Restore()
    {
      RestorePendingActions();
    }

    /// <summary>
    /// Completes all currently scheduled <see cref="AnalysisManagerAction"/>s
    /// and prevents any new actions from being scheduled.
    /// </summary>
    public void Complete()
    {
      _inputBlock.Complete();
    }

    /// <summary>
    /// Immediately cancels processing of all currently scheduled <see cref="AnalysisManagerAction"/>s.
    /// Any pending actions will not be completed.
    /// </summary>
    public void Cancel()
    {
      _cts.Cancel();
    }

    /// <summary>
    /// Task that completes when all <see cref="AnalysisManagerAction"/>s
    /// have been processed or canceled.
    /// </summary>
    public Task Completion
    {
      get { return _tcs.Task; }
    }

    /// <summary>
    /// Collection of all <see cref="AnalysisManagerAction"/>s that
    /// have been scheduled but not yet processed.
    /// </summary>
    public AnalysisManagerAction[] PendingActions
    {
      get { return _pendingMediaAnalysis.Values.ToArray(); }
    }

    #endregion
  }
}
