#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks
{
  /// <summary>
  /// Takes directory MediaItems and saves them to the MediaLibrary
  /// </summary>
  /// <remarks>
  /// </remarks>
  class DirectorySaveBlock : IPropagatorBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>
  {
    #region Consts

    private static readonly IEnumerable<Guid> DIRECTORY_MIA_ID_ENUMERATION = new[]
      {
        DirectoryAspect.ASPECT_ID
      };
    private static readonly IEnumerable<Guid> EMPTY_MIA_ID_ENUMERATION = new Guid[] { };

    #endregion

    #region Variables

    private readonly BufferBlock<PendingImportResourceNewGen> _inputBufferBlock;
    private readonly BufferBlock<PendingImportResourceNewGen> _suspensionBufferBlock;
    private readonly TransformManyBlock<PendingImportResourceNewGen, PendingImportResourceNewGen> _innerBlock;
    private readonly ConcurrentBag<IDisposable> _suspensionLinks;
    private readonly ImportJobController _parentImportJobController;
    private readonly TaskCompletionSource<object> _tcs;
    private readonly int _maxDegreeOfParallelism;
    private readonly Stopwatch _stopWatch;
    private IMediaBrowsing _mediaBrowsingCallback;
    private IImportResultHandler _importResultHandler;
    private readonly bool _refresh;
    private readonly ConcurrentDictionary<ResourcePath, Guid> _parentDirectoryIds;
    private int _directoriesProcessed;

    #endregion

    #region Constructor

    /// <summary>
    /// Initiates the DirectoryUnfoldBlock
    /// </summary>
    /// <param name="ct">CancellationToken used to cancel this block</param>
    /// <param name="refresh">true if this is a refresh import, otherwise false</param>
    /// <param name="parentImportJobController">ImportJobController to which this DirectoryUnfoldBlock belongs</param>
    public DirectorySaveBlock(CancellationToken ct, bool refresh, ImportJobController parentImportJobController)
    {
      _parentImportJobController = parentImportJobController;
      _refresh = refresh;
      _maxDegreeOfParallelism = 1;
      _parentDirectoryIds = new ConcurrentDictionary<ResourcePath, Guid>();
      
      _tcs = new TaskCompletionSource<object>();
      _inputBufferBlock = new BufferBlock<PendingImportResourceNewGen>(new DataflowBlockOptions { CancellationToken = ct });
      _suspensionBufferBlock = new BufferBlock<PendingImportResourceNewGen>(new DataflowBlockOptions { CancellationToken = ct });
      _innerBlock = new TransformManyBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>(p => ProcessDirectory(p), new ExecutionDataflowBlockOptions { BoundedCapacity = _maxDegreeOfParallelism, MaxDegreeOfParallelism = _maxDegreeOfParallelism, CancellationToken = ct });
      _innerBlock.Completion.ContinueWith(OnFinished);
      _suspensionLinks = new ConcurrentBag<IDisposable>();

      _stopWatch = new Stopwatch();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Main method that is called for every <see cref="PendingImportResourceNewGen"/> in this block
    /// </summary>
    /// <param name="importResource">Directory resource to be saved to the MediaLibrary</param>
    /// <returns></returns>
    private IEnumerable<PendingImportResourceNewGen> ProcessDirectory(PendingImportResourceNewGen importResource)
    {
      try
      {
        Interlocked.Increment(ref _directoriesProcessed);

        // Directories that are single resources (such as DVD directories) are not saved in this block
        // We just pass them to the next block.
        if (!importResource.IsSingleResource)
        {
          // We only save to the MediaLibrary if
          // (a) this is a first time import (i.e. not a refresh import), or
          // (b) this is a refresh import and the respective directory MediaItem is not yet in the MediaLibrary
          if (!_refresh || IsRefreshNeeded(importResource.ResourceAccessor))
          {
            Guid? parentDirectoryId = GetParentDirectoryId(importResource.ParentDirectory);
            if (parentDirectoryId == null)
            {
              // If we cannot determine the parent directory ID we have an error case and
              // cannot save this directory MediaItem
              importResource.Dispose();
              return new HashSet<PendingImportResourceNewGen>();
            }
            Guid directoryId = AddDirectory(importResource.ResourceAccessor, parentDirectoryId.Value);
            _parentDirectoryIds[importResource.PendingResourcePath] = directoryId;
          }
        }

        importResource.LastFinishedBlock = PendingImportResourceNewGen.DataflowNetworkPosition.DirectorySaveBlock;

        // ToDo: Remove this and do it later
        importResource.Dispose();

        return new HashSet<PendingImportResourceNewGen> { importResource };
      }
      catch (DisconnectedException)
      {
        // The MediaLibrary has been disconnected.
        // Post the PendingImportResource currently being processed back to the _suspensionBufferBlock,
        // Request suspension of the ImporterWorker and wait until we are in suspended state.
        _suspensionBufferBlock.Post(importResource);
        ServiceRegistration.Get<ILogger>().Info("ImporterWorker / {0} / DirectorySaveBlock: MediaLibrary disconnected. Requesting suspension...", _parentImportJobController);
        _parentImportJobController.ParentImporterWorker.RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.Suspend)).Wait();
        return new HashSet<PendingImportResourceNewGen>();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker / {0} / DirectorySaveBlock: Error while processing {1}", ex, _parentImportJobController, importResource);
        importResource.Dispose();
        return new HashSet<PendingImportResourceNewGen>();
      }
    }

    private bool IsRefreshNeeded(IFileSystemResourceAccessor directoryAccessor)
    {
      var directoryPath = directoryAccessor.CanonicalLocalResourcePath;
      var directoryItem = _mediaBrowsingCallback.LoadLocalItem(directoryPath, EMPTY_MIA_ID_ENUMERATION, DIRECTORY_MIA_ID_ENUMERATION);
      if (directoryItem != null)
      {
        MediaItemAspect directoryAspect;
        if (!directoryItem.Aspects.TryGetValue(DirectoryAspect.ASPECT_ID, out directoryAspect))
        {
          // This is the case if the parentResourcePath was formerly imported as a single resource.
          // We cannot reuse it and it is necessary to delete this old MediaItem.
          _importResultHandler.DeleteMediaItem(directoryPath);
          directoryItem = null;
        }
        else
          // This directory is already correctly stored in the MediaLibrary. No need to store it again,
          // we just cache its ID for potential subdirectories to be stored during this refresh.
          _parentDirectoryIds[directoryPath] = directoryItem.MediaItemId;
      }
      return (directoryItem == null);
    }
    
    private Guid? GetParentDirectoryId(ResourcePath parentResourcePath)
    {
      // Parent directory of a share's BasePath is null and must be saved
      // with Guid.Empty as parent directory ID
      if (parentResourcePath == null)
        return Guid.Empty;

      // We save directories in the order we unfolded them, i.e. the parent directory has
      // been saved before we try to save the child directory. When saving the parent directory
      // we store its ID in _parentDirectoryIds to cache them. So usually we should find the
      // parent directory ID in this cache.
      Guid result;
      if (_parentDirectoryIds.TryGetValue(parentResourcePath, out result))
        return result;

      // If the above wasn't successful, we have to load the parent directory MediaItem from
      // the MediaLibrary to get its ID. This should only be necessary if the ImportJob was
      // persisted to disk before and resumed after a restart of the application. In this
      // case we don't have the parent directory IDs cached in _parentDirectoryIds.
      var parentDirectoryMediaItem = _mediaBrowsingCallback.LoadLocalItem(parentResourcePath, DIRECTORY_MIA_ID_ENUMERATION, EMPTY_MIA_ID_ENUMERATION);
      if (parentDirectoryMediaItem == null)
      {
        // If the parent directory ID could not be found in the MediaLibrary, this is an error
        // case: The order of the directories to be saved was wrong.
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker / {0} / DirectorySaveBlock: Could not find GUID of parent directory ({1}). Directories were posted to this block in the wrong order.", _parentImportJobController, parentResourcePath);
        return null;
      }
      // If we had to load the parent directory ID from the MediaLibrary, we store it in our
      // cache so that we don't have to load it again for the next subdirectory of that
      // parent directory.
      _parentDirectoryIds[parentResourcePath] = parentDirectoryMediaItem.MediaItemId;
      return parentDirectoryMediaItem.MediaItemId;
    }

    private Guid AddDirectory(IFileSystemResourceAccessor directoryAccessor, Guid parentDirectoryId)
    {
      var directoryPath = directoryAccessor.CanonicalLocalResourcePath;
      var mediaAspect = new MediaItemAspect(MediaAspect.Metadata);
      mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, directoryAccessor.ResourceName);
      mediaAspect.SetAttribute(MediaAspect.ATTR_MIME_TYPE, null);
      mediaAspect.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, DateTime.MinValue);
      mediaAspect.SetAttribute(MediaAspect.ATTR_RATING, 0);
      mediaAspect.SetAttribute(MediaAspect.ATTR_COMMENT, null);
      mediaAspect.SetAttribute(MediaAspect.ATTR_LASTPLAYED, DateTime.MinValue);
      var directoryAspect = new MediaItemAspect(DirectoryAspect.Metadata);
      IList<MediaItemAspect> aspects = new List<MediaItemAspect>(new[]
        {
            mediaAspect,
            directoryAspect
        });
      return _importResultHandler.UpdateMediaItem(parentDirectoryId, directoryPath, aspects);
    }

    /// <summary>
    /// Runs when the _innerBlock is finished
    /// </summary>
    /// <param name="previousTask">_innerBlock.Completion</param>
    private void OnFinished(Task previousTask)
    {
      _stopWatch.Stop();
      _parentDirectoryIds.Clear();

      if (previousTask.IsFaulted)
      {
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker / {0} / DirectorySaveBlock: Error after saving {1} directories; time elapsed: {2}; MaxDegreeOfParallelism = {3}", _parentImportJobController, _directoriesProcessed, _stopWatch.Elapsed, _maxDegreeOfParallelism);
        // ReSharper disable once AssignNullToNotNullAttribute
        _tcs.SetException(previousTask.Exception);
      }
      else if (previousTask.IsCanceled)
      {
        ServiceRegistration.Get<ILogger>().Debug("ImporterWorker / {0} / DirectorySaveBlock: Canceled after saving {1} directories; time elapsed: {2}; MaxDegreeOfParallelism = {3}", _parentImportJobController, _directoriesProcessed, _stopWatch.Elapsed, _maxDegreeOfParallelism);
        _tcs.SetCanceled();
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Debug("ImporterWorker / {0} / DirectorySaveBlock: Saved {1} directories; time elapsed: {2}; MaxDegreeOfParallelism = {3}", _parentImportJobController, _directoriesProcessed, _stopWatch.Elapsed, _maxDegreeOfParallelism);
        _tcs.SetResult(null);
      }
    }

    #endregion

    #region Public methods

    public void Activate(IMediaBrowsing mediaBrowsingCallback, IImportResultHandler importResultHandler)
    {
      _mediaBrowsingCallback = mediaBrowsingCallback;
      _importResultHandler = importResultHandler;
      _suspensionLinks.Add(_inputBufferBlock.LinkTo(_suspensionBufferBlock, new DataflowLinkOptions { PropagateCompletion = true }));
      _suspensionLinks.Add(_suspensionBufferBlock.LinkTo(_innerBlock, new DataflowLinkOptions { PropagateCompletion = true }));
      _stopWatch.Start();
    }

    public void Suspend()
    {
      IDisposable link;
      while (_suspensionLinks.TryTake(out link))
        link.Dispose();
      _stopWatch.Stop();

      // We need to reorder the items in the _suspensionBufferBlock by their PendingImportResourceNumber
      IList<PendingImportResourceNewGen> items;
      if (_suspensionBufferBlock.TryReceiveAll(out items))
      {
        var sortedItems = items.OrderBy(item => item.PendingImportResourceNumber);
        foreach (var item in sortedItems)
          _suspensionBufferBlock.Post(item);
      }
    }

    #endregion

    #region Interface implementations

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, PendingImportResourceNewGen messageValue, ISourceBlock<PendingImportResourceNewGen> source, bool consumeToAccept)
    {
      return (_inputBufferBlock as ITargetBlock<PendingImportResourceNewGen>).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    }

    public void Complete()
    {
      _inputBufferBlock.Complete();
    }

    void IDataflowBlock.Fault(Exception exception)
    {
      (_inputBufferBlock as IDataflowBlock).Fault(exception);
    }

    public Task Completion
    {
      get { return _tcs.Task; }
    }

    public IDisposable LinkTo(ITargetBlock<PendingImportResourceNewGen> target, DataflowLinkOptions linkOptions)
    {
      return _innerBlock.LinkTo(target, linkOptions);
    }

    PendingImportResourceNewGen ISourceBlock<PendingImportResourceNewGen>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<PendingImportResourceNewGen> target, out bool messageConsumed)
    {
      return (_innerBlock as ISourceBlock<PendingImportResourceNewGen>).ConsumeMessage(messageHeader, target, out messageConsumed);
    }

    bool ISourceBlock<PendingImportResourceNewGen>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<PendingImportResourceNewGen> target)
    {
      return (_innerBlock as ISourceBlock<PendingImportResourceNewGen>).ReserveMessage(messageHeader, target);
    }

    void ISourceBlock<PendingImportResourceNewGen>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<PendingImportResourceNewGen> target)
    {
      (_innerBlock as ISourceBlock<PendingImportResourceNewGen>).ReleaseReservation(messageHeader, target);
    }

    #endregion
  }
}
