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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks;

namespace MediaPortal.Common.Services.MediaManagement
{
  /// <summary>
  /// An <see cref="ImportJobController"/> is responsible for handling a single ImportJob
  /// </summary>
  /// <remarks>
  /// It holds the TPL Datflow network for the ImportJob and is responsible for cancelling
  /// and suspending the particular ImportJob. If it determines that the <see cref="ImporterWorkerNewGen"/>
  /// shoud be suspended (e.g. because on the MP2 Client side the MP2 Server is disconnected) it notifies the
  /// <see cref="ImporterWorkerNewGen"/> (_parentImporterWorker) which will then make sure that
  /// all <see cref="ImportJobController"/>s are suspended.
  /// </remarks>
  public class ImportJobController
  {
    #region Variables

    private readonly int _importJobNumber;
    private readonly ImportJobInformation _importJobInformation;
    private readonly ImporterWorkerNewGen _parentImporterWorker;
    private readonly TaskCompletionSource<object> _tcs;
    private readonly CancellationTokenSource _cts;
    private readonly List<ImporterWorkerDataflowBlockBase> _dataflowBlocks;
    private readonly ConcurrentDictionary<ResourcePath, PendingImportResourceNewGen> _pendingImportResources;
    private int _numberOfLastPendingImportResource;
    private int _numberOfDisposedPendingImportResources;
    private bool _notifyProgress;

    #endregion

    #region Constructor

    public ImportJobController(ImportJobNewGen importJob, int importJobNumber, ImporterWorkerNewGen parentImporterWorker)
    {
      _importJobInformation = importJob.ImportJobInformation;
      _importJobNumber = importJobNumber;
      _parentImporterWorker = parentImporterWorker;
      _numberOfLastPendingImportResource = 0;
      _numberOfDisposedPendingImportResources = 0;
      _notifyProgress = true;
      _pendingImportResources = new ConcurrentDictionary<ResourcePath, PendingImportResourceNewGen>();
      _tcs = new TaskCompletionSource<object>();
      _cts = new CancellationTokenSource();
      _parentImporterWorker.NotifyProgress(true);

      _dataflowBlocks = new List<ImporterWorkerDataflowBlockBase>();
      SetupDataflowBlocks(importJob.PendingImportResources);
      _dataflowBlocks.ForEach(block => block.Completion.ContinueWith(OnAnyBlockFaulted, TaskContinuationOptions.OnlyOnFaulted));
      Task.WhenAll(_dataflowBlocks.Select(block => block.Completion)).ContinueWith(OnFinished);
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Returns a <see cref="Task"/> that represents the status of the ImportJob
    /// </summary>
    public Task Completion
    {
      get { return _tcs.Task; }      
    }

    /// <summary>
    /// Returns the Information that describes then current ImportJob and its state
    /// </summary>
    /// <remarks>
    /// The <see cref="ImportJobNewGen"/> returned by this property is only valid when this property is
    /// accessed while the <see cref="ImporterWorkerNewGen"/> is suspended. Otherwise this property returns null.
    /// </remarks>
    public ImportJobNewGen ImportJob
    {
      get
      {
        if (_parentImporterWorker.IsSuspended)
          return new ImportJobNewGen(_importJobInformation, _pendingImportResources.Values.OrderBy(pir => pir.PendingImportResourceNumber).ToList());
        return null;
      }
    }

    /// <summary>
    /// Returns the progress of this <see cref="ImportJobController"/>
    /// </summary>
    /// <remarks>
    /// Return type is a Tuple of two integers, the first of which is the number of <see cref="PendingImportResource"/>s
    /// created during this import, the second of which is the number of <see cref="PendingImportResource"/>s
    /// disposed (i.e. completed) during this import.
    /// </remarks>
    public Tuple<int, int> Progress
    {
      get
      {
        int created = _numberOfLastPendingImportResource;
        int completed = created - _pendingImportResources.Count;
        return new Tuple<int, int>(created, completed);
      }
    }

    public ImporterWorkerNewGen ParentImporterWorker
    {
      get { return _parentImporterWorker; }
    }

    #endregion

    #region Public methods

    public void Activate(IMediaBrowsing mediaBrowsingCallback, IImportResultHandler importResultHandler)
    {
      // To avoid peaks on system startup we start one Block every 100ms.
      // Currently we also need this because the MediaAccessor is not threadsafe on startup
      // see here: http://forum.team-mediaportal.com/threads/mediaaccessor-not-thread-safe.125132/
      // ToDo: Make MediaAccessor threadsafe on startup
      foreach (var block in _dataflowBlocks)
      {
        block.Activate(mediaBrowsingCallback, importResultHandler);
        Task.Delay(100).Wait();
      }
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}: Activated", this);
      ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportStarted, _importJobInformation.BasePath);
    }

    public void Suspend()
    {
      foreach (var block in _dataflowBlocks)
        block.Suspend();
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}: Suspended", this);
    }

    public void Cancel()
    {
      _cts.Cancel();
    }

    public int GetNumberOfNextPendingImportResource()
    {
      return Interlocked.Increment(ref _numberOfLastPendingImportResource);
    }
    
    public void RegisterPendingImportResource(PendingImportResourceNewGen pendingImportResource)
    {
      if (!_pendingImportResources.TryAdd(pendingImportResource.PendingResourcePath, pendingImportResource))
      {
        // Due to the BoundedCapacity of the of FileUnfoldBlock.OutputBlock and MetadataExtractorBlock.InputBlock
        // it may be the case that a directory resource has already been processed by the FileUnfoldBlock but is
        // not immediately passed to the MetadataExtractorBlock.InputBlock, where its CurrentBlock property is set.
        // If in this situation we suspend the ImportJob to disk and later on resume from disk, the directory resource
        // is processed again by the FileUnfoldBlock although the contained file resources have already been unfolded.
        // As a result, the PendingImportResource object for this file resource is created twice, which, at the creation
        // of the second object, returns false when calling TryAdd above. We can safely set the IsValid property of the
        // second one to false to filter it out (i.e. it will be disposed in the respective OutputBlock).
        pendingImportResource.IsValid = false;
      }
    }

    public void UnregisterPendingImportResource(PendingImportResourceNewGen pendingImportResource)
    {
      PendingImportResourceNewGen removedPendingImportResource;
      if(!_pendingImportResources.TryRemove(pendingImportResource.PendingResourcePath, out removedPendingImportResource))
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker.{0}: Could not unregister {1}", this, pendingImportResource);
      
      // If this ImportJobController is not completed, notify the ImporterWorker
      // every 25 disposed (i.e. completed) PendingImportResources
      if (_notifyProgress && Interlocked.Increment(ref _numberOfDisposedPendingImportResources) % 25 == 0)
        _parentImporterWorker.NotifyProgress(false);
    }

    #endregion

    #region Private methods

    private void OnAnyBlockFaulted(Task faultedTask)
    {
      // When any of the DataflowBlocks faults, the faulted state is propagated to the following
      // DataflowBlocks, but not to the preceding DataflowBlocks. In this case we therefore cancel all DataflowBlocks
      // to ensure that all DataflowBlocks are completed and release their resources.
      _cts.Cancel();
    }

    private void OnFinished(Task previousTask)
    {
      // Do not notify about progress anymore for every disposed PendingImportResource
      _notifyProgress = false;
      
      if (_pendingImportResources.Count > 0)
      {
        // The ImportJob has finished, but we have PendingImportJobResources left that have not been disposed.
        // This should only happen when the ImportJob finishes in cancelled oder faulted state. When the ImportJob
        // ran to completion, the DataflowBlocks should have disposed all the PendingImportResources.
        if(!previousTask.IsCanceled && !previousTask.IsFaulted)
          ServiceRegistration.Get<ILogger>().Warn("ImporterWorker.{0}: The ImportJob ran to completion but there are {1} undisposed PendingImportResources left. Disposing them now...", this, _pendingImportResources.Count);
        
        var pendingImportReouces = new List<PendingImportResourceNewGen>(_pendingImportResources.Values);
        foreach (var pendingImportResource in pendingImportReouces)
          pendingImportResource.Dispose();
      }

      if (previousTask.IsFaulted)
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker.{0}: Error while processing", previousTask.Exception, this);
      else if (previousTask.IsCanceled)
        ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}: Canceled", this);
      else
        ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}: Completed", this);

      if (!_parentImporterWorker.TryUnregisterImportJobController(_importJobInformation))
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker.{0}: Could not remove myself from the ImporterWorker's dictionaly of running ImportJobs", this);

      ImporterWorkerMessaging.SendImportMessage(previousTask.IsCanceled ? ImporterWorkerMessaging.MessageType.ImportScheduleCanceled : ImporterWorkerMessaging.MessageType.ImportCompleted, _importJobInformation.BasePath);
      _parentImporterWorker.NotifyProgress(true);

      // If this ImportJob faulted or was cancelled we can't do anything but log it (which we do above).
      // Therefore the Completion Task of this ImportJobController always returns 'RunToCompletion' to
      // avoid exceptions being thrown when this Task is awaited.
      _tcs.SetResult(null);
    }

    /// <summary>
    /// Instantiates all the necessary DataflowBlocks for the given ImportJob
    /// </summary>
    /// <remarks>
    /// ToDo: We need to handle three cases here:
    /// - BasePath points to a single resource
    /// - BasePath points to a directory which is not a single resource and the ImportJob does not include subdirectories
    /// - BasePath points to a directory which is not a single resource and the ImportJob does include subdirectories
    /// </remarks>
    private void SetupDataflowBlocks(IEnumerable<PendingImportResourceNewGen> pendingImportResources)
    {
      // ToDo: Add additional blocks here
      // Create the blocks
      _dataflowBlocks.Add(new DirectoryUnfoldBlock(_cts.Token, _importJobInformation, this));
      _dataflowBlocks.Add(new DirectorySaveBlock(_cts.Token, _importJobInformation, this));
      _dataflowBlocks.Add(new FileUnfoldBlock(_cts.Token, _importJobInformation, this));
      _dataflowBlocks.Add(new MetadataExtractorBlock(_cts.Token, _importJobInformation, this, false));
      _dataflowBlocks.Add(new MediaItemSaveBlock(_cts.Token, _importJobInformation, this));

      // Link the blocks
      for (int i = 0; i < _dataflowBlocks.Count -1; i++)
        _dataflowBlocks[i].LinkTo(_dataflowBlocks[i + 1], new DataflowLinkOptions { PropagateCompletion = true });
      _dataflowBlocks[_dataflowBlocks.Count - 1].LinkTo(DataflowBlock.NullTarget<PendingImportResourceNewGen>());

      // Fill the blocks
      if (pendingImportResources == null)
      {
        // This ImportJob was freshly created and not persisted to disk before
        // Just post the BasePath as new PendingImportResource
        IResourceAccessor ra;
        _importJobInformation.BasePath.TryCreateLocalResourceAccessor(out ra);
        var fsra = ra as IFileSystemResourceAccessor;
        var rootImportResource = new PendingImportResourceNewGen(null, fsra, DirectoryUnfoldBlock.BLOCK_NAME, this);
        _dataflowBlocks[0].Post(rootImportResource);
      }
      else
      {
        // This ImportJob was persisted to disk before
        foreach (var pendingImportResource in pendingImportResources)
        {
          pendingImportResource.InitializeAfterDeserialization(this);
          ImporterWorkerDataflowBlockBase block = _dataflowBlocks.Find(b => b.ToString() == pendingImportResource.CurrentBlock);
          if (block != null)
            block.SendAsync(pendingImportResource, _cts.Token);
          else
          {
            ServiceRegistration.Get<ILogger>().Error("ImporterWorker.{0}: Could not add {1} after deserialization. DataflowBlock with name {2} does not exist.", this, pendingImportResource, pendingImportResource.CurrentBlock);
            pendingImportResource.Dispose();
          }
        }
      }
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return String.Format("ImportJob(#{0})", _importJobNumber);
    }

    #endregion
  }
}
