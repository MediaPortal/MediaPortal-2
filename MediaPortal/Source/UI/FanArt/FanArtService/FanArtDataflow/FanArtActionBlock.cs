#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Extensions.UserServices.FanArtService.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MediaPortal.Extensions.UserServices.FanArtService.FanArtDataflow
{
  /// <summary>
  /// Handles fanart collection and deletion for individual media items
  /// and additionally persists and restores any pending actions to disk.
  /// </summary>
  public class FanArtActionBlock
  {
    private readonly CancellationTokenSource _cts;
    private readonly TaskCompletionSource<object> _tcs;

    //When fanart collection is restored from disk during startup the aspects of all items
    //restored to this block need to be reloaded from the database.
    //To avoid a big spike in db connections this semaphore limits the number of
    //concurrent connections when reloading the aspects.
    protected readonly SemaphoreSlim _loadItemThrottle = new SemaphoreSlim(2, 2);

    //Persists pending actions to disk
    protected readonly ITargetBlock<object> _persistBlock;
    //Does the actual collection/deletion
    protected readonly IPropagatorBlock<FanArtManagerAction, FanArtManagerAction> _innerBlock;
    //Marks actions as completed
    protected readonly ITargetBlock<FanArtManagerAction> _outputBlock;
    //Stores currently requested actions, used to persist any pending actions on shutdown
    protected ConcurrentDictionary<Guid, FanArtManagerAction> _pendingFanArtDownloads;

    #region Constructor

    public FanArtActionBlock()
    {
      _cts = new CancellationTokenSource();
      _tcs = new TaskCompletionSource<object>();
      _pendingFanArtDownloads = new ConcurrentDictionary<Guid, FanArtManagerAction>();

      //This block is separate from the main block network. It has a bounded capacity
      //of 2 to ensure that at most we have 1 action being processed and 1 pending.
      //Any additional persist requests will be covered by the already pending action.
      _persistBlock = new ActionBlock<object>(_ => PersistPendingActions(),
        new ExecutionDataflowBlockOptions { BoundedCapacity = 2, MaxDegreeOfParallelism = 1 });

      //Give the processing block an unlimited bounded capacity to ensure
      //that no actions are ever dropped. Any unprocessed actions will be
      //persisted and restored on server startup
      _innerBlock = new TransformBlock<FanArtManagerAction, FanArtManagerAction>(
        new Func<FanArtManagerAction, Task<FanArtManagerAction>>(InnerBlockMethod),
        new ExecutionDataflowBlockOptions { CancellationToken = _cts.Token, MaxDegreeOfParallelism = Environment.ProcessorCount });

      //Action block to mark actions as completed
      _outputBlock = new ActionBlock<FanArtManagerAction>(
        a => OutputBlockMethod(a),
        new ExecutionDataflowBlockOptions { CancellationToken = _cts.Token });

      //Link the blocks and handle fault/completion propagation
      _innerBlock.LinkTo(_outputBlock, new DataflowLinkOptions { PropagateCompletion = true });
      _innerBlock.Completion.ContinueWith(OnAnyBlockFaulted, TaskContinuationOptions.OnlyOnFaulted);
      _outputBlock.Completion.ContinueWith(OnAnyBlockFaulted, TaskContinuationOptions.OnlyOnFaulted);
      Task.WhenAll(_innerBlock.Completion, _outputBlock.Completion).ContinueWith(OnAllBlocksFinished);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Processes the <paramref name="action"/>.
    /// </summary>
    /// <param name="action">The action to process.</param>
    /// <returns>Task that completes when the action has been processed.</returns>
    private async Task<FanArtManagerAction> InnerBlockMethod(FanArtManagerAction action)
    {
      if (action.Type == ActionType.Collect)
      {
        //This action might have been restored in which case we need to reload the aspects
        if (await TryRestoreAspects(action))
          await CollectFanArt(action.MediaItemId, action.Aspects);
      }
      else if (action.Type == ActionType.Delete)
        await DeleteFanArt(action.MediaItemId);
      return action;
    }

    /// <summary>
    /// Removes the <paramref name="action"/> from the list of pending actions
    /// so it is not restored on next server startup.
    /// </summary>
    /// <param name="action"></param>
    private void OutputBlockMethod(FanArtManagerAction action)
    {
      FanArtManagerAction removedAction;
      if (_pendingFanArtDownloads.TryRemove(action.ActionId, out removedAction))
      {
        removedAction.Aspects?.Clear();
        //Remove the completed action from the persisted list of pending actions
        _persistBlock.Post(null);
      }
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
      //Complete the persist block now all processing
      //blocks have finished to ensure that all pending
      //actions are persisted
      _persistBlock.Complete();
      await _persistBlock.Completion;

      if (finishedTask.IsFaulted)
      {
        ServiceRegistration.Get<ILogger>().Error("FanArtActionBlock: Error processing fanart actions", finishedTask.Exception);
        // ReSharper disable once AssignNullToNotNullAttribute
        _tcs.SetException(finishedTask.Exception);
      }
      else
      {
        //Log the cancellation but set the TaskCompletionSource to RanToCompletion to
        //avoid throwing exceptions if the Completion Task is awaited
        if (finishedTask.IsCanceled)
          ServiceRegistration.Get<ILogger>().Debug("FanArtActionBlock: Fanart actions cancelled");
        _tcs.SetResult(null);
      }
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Checks whether the <paramref name="action"/> has any aspects and if not
    /// tries to restore them from the media library.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns>True if the aspects were present or successfully restored.</returns>
    protected async Task<bool> TryRestoreAspects(FanArtManagerAction action)
    {
      //Already has aspects, nothing to do
      if (action.Aspects != null)
        return true;
      
      //No aspects, this resource was restored from disk, try and restore the aspects from the media library.
      //Throttle the number of concurrent queries To avoid a spike during startup.
      await _loadItemThrottle.WaitAsync(_cts.Token);
      try
      {
        IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
        MediaItemQuery query = new MediaItemQuery(null,
          mediaLibrary.GetManagedMediaItemAspectMetadata().Keys,
          new MediaItemIdFilter(action.MediaItemId));
        var items = mediaLibrary.Search(query, false, null, true);
        if (items != null && items.Count > 0)
        {
          action.Aspects = items[0].Aspects;
          return true;
        }
        ServiceRegistration.Get<ILogger>().Warn("FanArtActionBlock: Unable to restore FanArtAction, media item with id {0} was not found in the media library", action.MediaItemId);
        return false;
      }
      finally
      {
        _loadItemThrottle.Release();
      }
    }

    /// <summary>
    /// Persists all currently pending actions to disk.
    /// </summary>
    protected void PersistPendingActions()
    {
      ISettingsManager sm = ServiceRegistration.Get<ISettingsManager>();
      FanArtActionSettings settings = sm.Load<FanArtActionSettings>();
      settings.PendingFanArtActions = PendingActions.ToArray();
      sm.Save(settings);
    }

    protected void RestorePendingActions()
    {
      ISettingsManager sm = ServiceRegistration.Get<ISettingsManager>();
      FanArtManagerAction[] pendingActions = sm.Load<FanArtActionSettings>().PendingFanArtActions;
      if (pendingActions == null || pendingActions.Length == 0)
        return;
      foreach (FanArtManagerAction pendingAction in pendingActions)
        Post(pendingAction);
      ServiceRegistration.Get<ILogger>().Debug("FanArtActionBlock: Restored {0} pending actions", pendingActions.Length);
    }

    /// <summary>
    /// Collects all fanart for the given media item using the registered <see cref="IMediaFanArtHandler"/>s
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="aspects">The aspects of the media item.</param>
    protected async Task CollectFanArt(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      try
      {
        IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
        IEnumerable<IMediaFanArtHandler> handlers = mediaAccessor.LocalFanArtHandlers.Values.Where(h => h.FanArtAspects.Any(a => aspects.ContainsKey(a)));
        foreach (IMediaFanArtHandler handler in handlers)
          await handler.CollectFanArtAsync(mediaItemId, aspects);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("FanArtActionBlock: Error collecting fanart for media item {0}.", ex, mediaItemId);
      }
    }

    /// <summary>
    /// Deletes all fanart for the given media item.
    /// </summary>
    /// <param name="mediaItemId"></param>
    protected Task DeleteFanArt(Guid mediaItemId)
    {
      try
      {
        //ToDo: Async deletion?
        IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
        foreach (IMediaFanArtHandler handler in mediaAccessor.LocalFanArtHandlers.Values)
          handler.DeleteFanArt(mediaItemId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("FanArtActionBlock: Error deleting fanart for media item {0}.", ex, mediaItemId);
      }
      return Task.CompletedTask;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Schedules a new <see cref="FanArtManagerAction"/> for processing.
    /// </summary>
    /// <param name="action">The action to process.</param>
    /// <returns>True if the action was scheduled.</returns>
    public bool Post(FanArtManagerAction action)
    {
      _pendingFanArtDownloads.TryAdd(action.ActionId, action);
      //Persist the newly pending action
      _persistBlock.Post(null);
      //Post the action for processing
      return _innerBlock.Post(action);
    }

    public void Restore()
    {
      RestorePendingActions();
    }

    /// <summary>
    /// Completes all currently scheduled <see cref="FanArtManagerAction"/>s
    /// and prevents any new actions from being scheduled.
    /// </summary>
    public void Complete()
    {
      _innerBlock.Complete();
    }

    /// <summary>
    /// Immediately cancels processing of all currently scheduled <see cref="FanArtManagerAction"/>s.
    /// Any pending actions will not be completed.
    /// </summary>
    public void Cancel()
    {
      _cts.Cancel();
    }

    /// <summary>
    /// Task that completes when all <see cref="FanArtManagerAction"/>s
    /// have been processed or cancelled.
    /// </summary>
    public Task Completion
    {
      get { return _tcs.Task; }
    }

    /// <summary>
    /// Collection of all <see cref="FanArtManagerAction"/>s that
    /// have been scheduled but not yet processed.
    /// </summary>
    public IList<FanArtManagerAction> PendingActions
    {
      get { return new List<FanArtManagerAction>(_pendingFanArtDownloads.Values); }
    }

    #endregion
  }
}
