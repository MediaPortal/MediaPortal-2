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

    private readonly ImportJobInformation _importJobInformation;
    private readonly ImporterWorkerNewGen _parentImporterWorker;
    private readonly TaskCompletionSource<object> _tcs;
    private readonly CancellationTokenSource _cts;
    private readonly int _importJobNumber;
    private int _numberOfLastPendingImportResource;
    private readonly ConcurrentDictionary<ResourcePath, PendingImportResourceNewGen> _pendingImportResources;

    private DirectoryUnfoldBlock _directoryUnfoldBlock;

    #endregion

    #region Constructor

    public ImportJobController(ImportJobNewGen importJob, int importJobNumber, ImporterWorkerNewGen parentImporterWorker)
    {
      _importJobInformation = importJob.ImportJobInformation;
      _importJobNumber = importJobNumber;
      _parentImporterWorker = parentImporterWorker;
      _numberOfLastPendingImportResource = 0;
      _pendingImportResources = new ConcurrentDictionary<ResourcePath, PendingImportResourceNewGen>();
      _tcs = new TaskCompletionSource<object>();
      _cts = new CancellationTokenSource();

      SetupDataflowBlocks(importJob.PendingImportResources);

      // Todo: This continuation shall happen after the last DataflowBlock has finished
      _directoryUnfoldBlock.Completion.ContinueWith(OnFinished);
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

    #endregion

    #region Public methods

    public void Activate(IMediaBrowsing mediaBrowsingCallback, IImportResultHandler importResultHandler)
    {
      // ToDo: Make sure we activate all blocks (if necessary with mediaBrowsingCallback and importResultHandler)
      _directoryUnfoldBlock.Activate();
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker / {0}: Activated", this);
    }

    public void Suspend()
    {
      // ToDo: Make sure we suspend all blocks
      _directoryUnfoldBlock.Suspend();
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker / {0}: Suspended", this);
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
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker / {0}: Could not register {1}", this, pendingImportResource);
    }

    public void UnregisterPendingImportResource(PendingImportResourceNewGen pendingImportResource)
    {
      PendingImportResourceNewGen removedPendingImportResource;
      if(!_pendingImportResources.TryRemove(pendingImportResource.PendingResourcePath, out removedPendingImportResource))
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker / {0}: Could not unregister {1}", this, pendingImportResource);
    }

    #endregion

    #region Private methods

    private void OnFinished(Task previousTask)
    {
      if (_pendingImportResources.Count > 0)
      {
        // The ImportJob has finished, but we have PendingImportJobResources left that have not been disposed.
        // This should only happen when the ImportJob finishes in cancelled oder faulted state. When the ImportJob
        // ran to completion, the DataflowBlocks should have disposed all the PendingImportResources.
        if(!previousTask.IsCanceled && !previousTask.IsFaulted)
          ServiceRegistration.Get<ILogger>().Warn("ImporterWorker / {0}: The ImportJob ran to completion but there are {1} undisposed PendingImportResources left. Disposing them now...", this, _pendingImportResources.Count);
        
        var pendingImportReouces = new List<PendingImportResourceNewGen>(_pendingImportResources.Values);
        foreach (var pendingImportResource in pendingImportReouces)
          pendingImportResource.Dispose();
      }

      if (previousTask.IsFaulted)
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker / {0}: Error while processing", previousTask.Exception, this);
      else if (previousTask.IsCanceled)
        ServiceRegistration.Get<ILogger>().Info("ImporterWorker / {0}: Canceled", this);
      else
        ServiceRegistration.Get<ILogger>().Info("ImporterWorker / {0}: Completed", this);

      if (!_parentImporterWorker.TryUnregisterImportJobController(_importJobInformation))
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker / {0}: Could not remove myself from the ImporterWorker's dictionaly of running ImportJobs", this);

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
      // Create the blocks
      _directoryUnfoldBlock = new DirectoryUnfoldBlock(_cts.Token, this);

      // Link the blocks
      _directoryUnfoldBlock.LinkTo(DataflowBlock.NullTarget<PendingImportResourceNewGen>());

      // Fill the blocks
      if (pendingImportResources == null)
      {
        // This ImportJob was freshly created and not persisted to disk before
        // Just post the BasePath as new PendingImportResource
        IResourceAccessor ra;
        _importJobInformation.BasePath.TryCreateLocalResourceAccessor(out ra);
        var fsra = ra as IFileSystemResourceAccessor;
        var rootImportResource = new PendingImportResourceNewGen(null, fsra, PendingImportResourceNewGen.DataflowNetworkPosition.None, this);
        _directoryUnfoldBlock.Post(rootImportResource);
      }
      else
      {
        // This ImportJob was persisted to disk before
        foreach (var pendingImportResource in pendingImportResources)
        {
          pendingImportResource.InitializeAfterDeserialization(this);
          
          // ToDo: Adapt to further DataflowBlocks
          switch (pendingImportResource.LastFinishedBlock)
          {
            case PendingImportResourceNewGen.DataflowNetworkPosition.None:
              _directoryUnfoldBlock.Post(pendingImportResource);
              break;
            case PendingImportResourceNewGen.DataflowNetworkPosition.DirectoryUnfoldBlock:
              pendingImportResource.Dispose();
              break;
          }
        }
      }
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return String.Format("ImportJob #{0}", _importJobNumber);
    }

    #endregion
  }
}
