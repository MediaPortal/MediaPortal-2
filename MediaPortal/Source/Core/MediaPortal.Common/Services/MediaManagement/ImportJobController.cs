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
  public class ImportJobController : IDisposable
  {
    #region Variables

    private readonly int _importJobNumber;
    private readonly ImportJobInformation _importJobInformation;
    private readonly ImporterWorkerNewGen _parentImporterWorker;
    private readonly TaskCompletionSource<object> _importJobControllerCompletion;
    private readonly TaskCompletionSource<object> _firstBlockHasFinished;
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
      _importJobControllerCompletion = new TaskCompletionSource<object>();
      _firstBlockHasFinished = new TaskCompletionSource<object>();
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
      get { return _importJobControllerCompletion.Task; }      
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

    /// <summary>
    /// Activates the ImportJob
    /// </summary>
    /// <param name="mediaBrowsingCallback"></param>
    /// <param name="importResultHandler"></param>
    public void Activate(IMediaBrowsing mediaBrowsingCallback, IImportResultHandler importResultHandler)
    {
      ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportStarted, _importJobInformation.BasePath);
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
    }

    /// <summary>
    /// Suspends the ImportJob
    /// </summary>
    /// <remarks>
    /// Calling this method does not mean that no processing is going on anymore. it only means
    /// that no PendingImportResource objects are passed from the InputBlocks to the InnerBlocks
    /// of the DataflowBlocks anymore.
    /// </remarks>
    public void Suspend()
    {
      foreach (var block in _dataflowBlocks)
        block.Suspend();
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}: Suspended", this);
    }

    /// <summary>
    /// Cancels the ImportJob
    /// </summary>
    /// <remarks>
    /// Be careful, when an ImportJob is canceled via this method, it does not dispose itself.
    /// The code that calls this method must also call <see cref="Dispose"/>
    /// </remarks>
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
      // We need to make sure that only the PendingImportResource object that is registered can unregister itself, hence
      // the ReferenceEquals condition. If we don't do that and have two different PendingImportResource objects pointing
      // to the same ResourcePath (which are therefore "equal"), the registration fails as described above, but the 
      // deregistration succeeds. As a result, the ImportJob will not be saved to disk completely on shutdown.
      PendingImportResourceNewGen registeredPendingImportResourceNewGen;
      if (_pendingImportResources.TryGetValue(pendingImportResource.PendingResourcePath, out registeredPendingImportResourceNewGen))
        if (ReferenceEquals(pendingImportResource, registeredPendingImportResourceNewGen))
        {
          PendingImportResourceNewGen removedPendingImportResource;
          if (!_pendingImportResources.TryRemove(pendingImportResource.PendingResourcePath, out removedPendingImportResource))
            ServiceRegistration.Get<ILogger>().Warn("ImporterWorker.{0}: Could not unregister {1}", this, pendingImportResource);          
        }      
      
      // If this ImportJobController is not completed, notify the ImporterWorker
      // every 25 disposed (i.e. completed) PendingImportResources
      if (_notifyProgress && Interlocked.Increment(ref _numberOfDisposedPendingImportResources) % 25 == 0)
        _parentImporterWorker.NotifyProgress(false);
    }

    internal void FirstBlockHasFinished()
    {
      _firstBlockHasFinished.TrySetResult(new object());
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
      }

      if (previousTask.IsFaulted)
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker.{0}: Error while processing", previousTask.Exception, this);
      else if (previousTask.IsCanceled)
        ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}: Canceled", this);
      else
        ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}: Completed", this);

      // When this ImportJob was cancelled it needs to be disposed manually.
      if (!previousTask.IsCanceled)
        Dispose();

      ImporterWorkerMessaging.SendImportMessage(previousTask.IsCanceled ? ImporterWorkerMessaging.MessageType.ImportScheduleCanceled : ImporterWorkerMessaging.MessageType.ImportCompleted, _importJobInformation.BasePath);
      _parentImporterWorker.NotifyProgress(true);

      // If this ImportJob faulted or was cancelled we can't do anything but log it (which we do above).
      // Therefore the Completion Task of this ImportJobController always returns 'RunToCompletion' to
      // avoid exceptions being thrown when this Task is awaited.
      _importJobControllerCompletion.SetResult(null);
    }

    /// <summary>
    /// Instantiates all the necessary DataflowBlocks for the given ImportJob
    /// </summary>
    /// <remarks>
    /// We first have to distinguish between two cases here:
    ///  - BasePath points to a resource for which we can only create an IResourceAccessor - not an IFilesystemResourceAccessor
    ///    For this case we only import that single resource and don't have to take care of directories and subdirectories
    ///    ToDo: This still needs to be implemented
    /// - BasePath points to a resource for which we can create an IFilesystemResourceAccessor
    ///   Here we first check whether the resource on the given BasePath exists. If not, we do nothing.
    ///   If it does exist, we distinguish two cases:
    ///   - The ImportJob was restored from disk, in which case we push the existing PendingImportResources to the respective DataflowBlocks.
    ///   - The ImportJob was freshly created, in which case we push the BasePath to the first DataFlowBlock.
    ///     In this case there are three subcases:
    ///     - BasePath points to a single resource
    ///     - BasePath points to a directory which is not a single resource and the ImportJob does not include subdirectories
    ///     - BasePath points to a directory which is not a single resource and the ImportJob does include subdirectories
    ///     These subcases, however, are taken care of by the DataflowBlocks - not by the ImportJobController
    /// </remarks>
    private void SetupDataflowBlocks(IEnumerable<PendingImportResourceNewGen> pendingImportResources)
    {
      // If we cannot access the BasePath at all, we just log and return
      IResourceAccessor ra = null;
      try
      {
        if (!_importJobInformation.BasePath.TryCreateLocalResourceAccessor(out ra))
        {
          ServiceRegistration.Get<ILogger>().Warn("ImporterWorker.{0}: Unable to access resource path '{1}'.", this, _importJobInformation.BasePath);
          return;
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker.{0}: Error while creating ResourceAccessor for resource path '{1}'.", e, this, _importJobInformation.BasePath);
        if (ra != null)
          ra.Dispose();
        return;
      }

      try
      {
        // As of now we have a ResourceAccessor that needs to be disposed
        using (ra)
        {
          // If we have a ResourceAccessor which is not an IFileSystemResourceAccessor, just import that single resource
          if (!(ra is IFileSystemResourceAccessor))
          {
            // ToDo: Implement import of Non-IFilesSystemResourceAccessors
            return;
          }

          // Now we are sure it is an IFileSystemResourceAccessor
          var fsra = ra as IFileSystemResourceAccessor;

          // If the BasePath does not exist, we do nothing. This is necessary to avoid whole shares being removed from
          // the MediaLibrary when a RefreshImport is scheduled while e.g. network resources are unavailable.
          // If fsra is a NetworkNeighborhoddResourceAccessor and its IsServerPath() method returns true, fsra.Exists()
          // will always return true. If therefore the BasePath of this Import points to a whole server and this server
          // is not available during a RefreshImport, the whole share will be deleted from the MediaLibrary.
          // ToDo: Rework this in NetworkNeighborhoodResourceAccessor
          if (!fsra.Exists)
          {
            ServiceRegistration.Get<ILogger>().Warn("ImporterWorker.{0}: Resource '{1}' does not exists or is not available.", this, _importJobInformation.BasePath);
            return;
          }
        
          // Now we are sure that we need a DataflowBlock network

          // Create the blocks
          _dataflowBlocks.Add(new DirectoryUnfoldBlock(_cts.Token, _importJobInformation, this));
          _dataflowBlocks.Add(new DirectorySaveBlock(_cts.Token, _importJobInformation, this));
          _dataflowBlocks.Add(new FileUnfoldBlock(_cts.Token, _importJobInformation, this));
          _dataflowBlocks.Add(new ChangeUnfoldBlock(_cts.Token, _importJobInformation, this));
          _dataflowBlocks.Add(new MediaItemLoadBlock(_cts.Token, _importJobInformation, this));
          _dataflowBlocks.Add(new MetadataExtractorBlock(_cts.Token, _importJobInformation, this));
          _dataflowBlocks.Add(new MediaItemSaveBlock(_cts.Token, _importJobInformation, this));

          // Link the blocks
          for (int i = 0; i < _dataflowBlocks.Count - 1; i++)
            _dataflowBlocks[i].LinkTo(_dataflowBlocks[i + 1], new DataflowLinkOptions { PropagateCompletion = true });
          _dataflowBlocks[_dataflowBlocks.Count - 1].LinkTo(DataflowBlock.NullTarget<PendingImportResourceNewGen>());

          // Fill the blocks
          var completeFirstBlockAfterTheseTasks = new HashSet<Task>();
          bool firstBlockNeedsCompletion = true;
          if (pendingImportResources == null)
          {
            // This ImportJob was freshly created and not persisted to disk before
            // Just post the BasePath as new PendingImportResource
            var rootImportResource = new PendingImportResourceNewGen(null, fsra.Clone() as IFileSystemResourceAccessor, DirectoryUnfoldBlock.BLOCK_NAME, this);
            _dataflowBlocks[0].Post(rootImportResource);
            firstBlockNeedsCompletion = false;
          }
          else
          {
            // This ImportJob was persisted to disk before
            int numberOfRestoredPendingResources = 0;
            foreach (var pendingImportResource in pendingImportResources)
            {
              pendingImportResource.InitializeAfterDeserialization(this);
              ImporterWorkerDataflowBlockBase block = _dataflowBlocks.Find(b => b.ToString() == pendingImportResource.CurrentBlock);
              if (block != null)
              {
                completeFirstBlockAfterTheseTasks.Add(block.SendAsync(pendingImportResource, _cts.Token));
                numberOfRestoredPendingResources++;
                if (block == _dataflowBlocks[0])
                  firstBlockNeedsCompletion = false;
              }
              else
              {
                ServiceRegistration.Get<ILogger>().Error("ImporterWorker.{0}: Could not add {1} after deserialization. DataflowBlock with name {2} does not exist.", this, pendingImportResource, pendingImportResource.CurrentBlock);
                pendingImportResource.Dispose();
              }
            }
            ServiceRegistration.Get<ILogger>().Debug("ImporterWorker.{0}: {1} PendingImportResources restored.", this, numberOfRestoredPendingResources);
          }
          completeFirstBlockAfterTheseTasks.Add(_firstBlockHasFinished.Task);
          if (firstBlockNeedsCompletion)
            FirstBlockHasFinished();

          // The first DataflowBlock in the network (DirectoryUnfoldBlock) must be set to completed when
          // (a) The DirectoryUnfoldBlock has signaled that it is finished (by calling FirstBlockHasFinished()) and
          // (b) in case of an ImportJob that has been restored from disk, all restored PendingImportResources
          //     have been put into the Dataflow network
          Task.WhenAll(completeFirstBlockAfterTheseTasks).ContinueWith(previousTask => _dataflowBlocks[0].Complete());
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker.{0}: Error while setting up DataflowBlocks for resource path '{1}.", e, this, _importJobInformation.BasePath);
      }
    }

    #endregion

    #region Interface implementations

    public void Dispose()
    {
      // dispose all undisposed PendingImportResource objects
      var pendingImportReouces = new List<PendingImportResourceNewGen>(_pendingImportResources.Values);
      foreach (var pendingImportResource in pendingImportReouces)
        pendingImportResource.Dispose();

      // Remove this ImportJobController from the ImporterWorker's dictionary of existing ImportJobs
      if (!_parentImporterWorker.TryUnregisterImportJobController(_importJobInformation))
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker.{0}: Could not remove myself from the ImporterWorker's dictionary of running ImportJobs", this);

      ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}: Disposed", this);
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
