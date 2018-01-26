#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MediaPortal.Extensions.UserServices.FanArtService.FanArtDataflow
{
  public class FanArtActionBlock
  {
    private readonly CancellationTokenSource _cts;
    private readonly TaskCompletionSource<object> _tcs;
    
    protected readonly IPropagatorBlock<FanArtManagerAction, FanArtManagerAction> InnerBlock;
    protected readonly ITargetBlock<FanArtManagerAction> OutputBlock;

    protected ConcurrentDictionary<Guid, FanArtManagerAction> _pendingFanArtDownloads;

    #region Constructor

    public FanArtActionBlock()
    {
      _cts = new CancellationTokenSource();
      _tcs = new TaskCompletionSource<object>();
      _pendingFanArtDownloads = new ConcurrentDictionary<Guid, FanArtManagerAction>();

      InnerBlock = new TransformBlock<FanArtManagerAction, FanArtManagerAction>(
        new Func<FanArtManagerAction, Task<FanArtManagerAction>>(InnerBlockMethod),
        new ExecutionDataflowBlockOptions
        {
          CancellationToken = _cts.Token,
          MaxDegreeOfParallelism = Environment.ProcessorCount * 5
        });

      OutputBlock = new ActionBlock<FanArtManagerAction>(
        a => OutputBlockMethod(a),
        new ExecutionDataflowBlockOptions
        {
          CancellationToken = _cts.Token
        });

      InnerBlock.LinkTo(OutputBlock, new DataflowLinkOptions { PropagateCompletion = true });
      InnerBlock.Completion.ContinueWith(OnAnyBlockFaulted, TaskContinuationOptions.OnlyOnFaulted);
      OutputBlock.Completion.ContinueWith(OnAnyBlockFaulted, TaskContinuationOptions.OnlyOnFaulted);
      Task.WhenAll(InnerBlock.Completion, OutputBlock.Completion).ContinueWith(OnAllBlocksFinished);
    }

    #endregion

    #region Private Methods

    private async Task<FanArtManagerAction> InnerBlockMethod(FanArtManagerAction action)
    {
      if (action.Type == ActionType.Collect)
        await CollectFanArt(action.MediaItemId, action.Aspects);
      else if (action.Type == ActionType.Delete)
        await DeleteFanArt(action.MediaItemId);
      return action;
    }

    private void OutputBlockMethod(FanArtManagerAction action)
    {
      FanArtManagerAction removedAction;
      _pendingFanArtDownloads.TryRemove(action.ActionId, out removedAction);
    }
    
    /// <summary>
    /// Runs when any of <see cref="InnerBlock"/> or <see cref="OutputBlock"/> faults
    /// </summary>
    /// <param name="faultedTask">Completion property of the faulted DataflowBlock</param>
    private void OnAnyBlockFaulted(Task faultedTask)
    {
      // When one of the two DataflowBlocks faults, the faulted state is propagated to the following
      // DataflowBlocks, but not to the preceding DataflowBlocks. In this case we therefore fault all DataflowBlocks
      // that are not yet in a faulted state to ensure that all DataflowBlocks are completed and release their resources.
      if (!InnerBlock.Completion.IsFaulted)
        InnerBlock.Fault(faultedTask.Exception);
      if (!OutputBlock.Completion.IsFaulted)
        OutputBlock.Fault(faultedTask.Exception);
    }

    /// <summary>
    /// Runs when all DataflowBlocks are finished
    /// </summary>
    /// <param name="finishedTask">Task representing the state of all DataflowBlocks</param>
    private void OnAllBlocksFinished(Task finishedTask)
    {
      if (finishedTask.IsFaulted)
      {
        ServiceRegistration.Get<ILogger>().Error("FanArtDownloadBlock: Error processing fanart actions", finishedTask.Exception);
        // ReSharper disable once AssignNullToNotNullAttribute
        _tcs.SetException(finishedTask.Exception);
      }
      else
      {
        //Log the cancellation but set the TaskCompletionSource to RanToCompletion to avoid throwing exceptions if
        //the Completion Task is awaited
        if (finishedTask.IsCanceled)
          ServiceRegistration.Get<ILogger>().Debug("FanArtDownloadBlock: Fanart actions cancelled");
        _tcs.SetResult(null);
      }
    }

    #endregion

    #region Protected Methods

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
        await Task.WhenAll(handlers.Select(h => h.CollectFanArtAsync(mediaItemId, aspects)));
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("FanArtManagement: Error collecting fanart for media item {0}.", ex, mediaItemId);
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
        ServiceRegistration.Get<ILogger>().Error("FanArtManagement: Error deleting fanart for media item {0}.", ex, mediaItemId);
      }
      return Task.CompletedTask;
    }

    #endregion

    #region Public Methods

    public bool Post(FanArtManagerAction action)
    {
      _pendingFanArtDownloads.TryAdd(action.ActionId, action);
      return InnerBlock.Post(action);
    }

    public void Complete()
    {
      InnerBlock.Complete();
    }

    public void Cancel()
    {
      _cts.Cancel();
    }

    public Task Completion
    {
      get { return _tcs.Task; }
    }

    public IList<FanArtManagerAction> PendingActions
    {
      get { return new List<FanArtManagerAction>(_pendingFanArtDownloads.Values); }
    }

    #endregion
  }
}
