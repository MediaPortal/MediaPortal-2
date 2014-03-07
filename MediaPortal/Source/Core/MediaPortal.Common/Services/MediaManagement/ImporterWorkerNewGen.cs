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
using MediaPortal.Utilities;

namespace MediaPortal.Common.Services.MediaManagement
{
  /// <summary>
  /// Implementation of <see cref="IImporterWorker"/> using TPL Dataflow
  /// </summary>
  /// <remarks>
  /// This class takes care of importing the metadata of the Shares' content into the MediaLibrary.
  /// It is instantiated in the MP2 Server as well as in every MP2 Client. Therefore the access to
  /// the MediaLibrary is abstracted through <see cref="IImportResultHandler"/> and <see cref="IMediaBrowsing"/>
  /// While on the MP2 Server side the respective classes provide direct access to the MediaLibrary, on the
  /// MP2 Clients' side access to the MediaLibrary is routed via UPnP.
  /// This class makes heavy use of multitasking. Calls to any of the <see cref="IImporterWorker"/>-methods
  /// immediately return to the caller. For every such call, an <see cref="ImporterWorkerAction"/> is instantiated
  /// and posted to an ActionBlock. From there, the <see cref="ImporterWorkerAction"/>s are picked up and
  /// processed sequentially but asynchronously.
  /// For every scheduled ImportJob, an <see cref="ImportJobController"/> is instantiated. The
  /// <see cref="ImportJobController"/> is added to a ConcurrentDictionary where it remains until the respective
  /// ImportJob is finished. The ImportJob itself (including setting up the necessary TPL Dataflow network) is
  /// handled by the respective <see cref="ImportJobController"/>.
  /// This class handles every situation that may affect all ImportJobs, such as suspension or shutdown. Situations
  /// that only affect single ImportJobs are handled by the respective <see cref="ImportJobController"/>s.
  /// ToDo: Handle suspension
  /// ToDo: Handle state saving on shutdown
  /// ToDo: Handle messaging
  /// </remarks>
  public class ImporterWorkerNewGen : IImporterWorker, IDisposable
  {
    #region Enums

    public enum Status
    {
      Shutdown,
      Suspended,
      Activated
    }

    #endregion

    #region Variables

    /// <summary>
    /// Processes the <see cref="ImporterWorkerAction"/>s that are requested
    /// </summary>
    /// <remarks>
    /// This DataflowBlock must not have a MaxDegreeOfParallelism other than 1 which is the default value.
    /// It is required behaviour for this block to process one <see cref="ImporterWorkerAction"/> after the other.
    /// </remarks>
    private readonly ActionBlock<ImporterWorkerAction> _actionBlock;

    /// <summary>
    /// Holds one <see cref="ImportJobController"/> for every active ImportJob
    /// </summary>
    private readonly ConcurrentDictionary<ImportJobInformation, ImportJobController> _importJobs;

    /// <summary>
    /// Holds a unique number to be assigned to the next <see cref="ImportJobController"/>
    /// </summary>
    private int _numberOfNextImportJob;

    private IImportResultHandler _importResultHandler;
    private IMediaBrowsing _mediaBrowsing;

    private volatile Status _status;

    #endregion

    #region Constructor

    public ImporterWorkerNewGen()
    {
      _actionBlock = new ActionBlock<ImporterWorkerAction>(action => ProcessActionRequest(action));
      _importJobs = new ConcurrentDictionary<ImportJobInformation, ImportJobController>();
      _numberOfNextImportJob = 1;
      _status = Status.Shutdown;
    }

    #endregion

    #region Private methods

    #region ImporterWorkerAction handling

    /// <summary>
    /// Requests an <see cref="ImporterWorkerAction"/>
    /// </summary>
    /// <param name="action"><see cref="ImporterWorkerAction"/> to be requested</param>
    /// <returns><see cref="Task"/> that completes when the <see cref="ImporterWorkerAction"/> has completed</returns>
    private Task RequestAction(ImporterWorkerAction action)
    {
      _actionBlock.Post(action);
      return action.Completion;
    }

    /// <summary>
    /// Processes <see cref="ImporterWorkerAction"/>s posted to the <see cref="_actionBlock"/>
    /// </summary>
    /// <param name="action"><see cref="ImporterWorkerAction"/> to be processed</param>
    private void ProcessActionRequest(ImporterWorkerAction action)
    {
      try
      {
        switch (action.Type)
        {
          case ImporterWorkerAction.ActionType.Startup:
            DoStartup();
            break;
          case ImporterWorkerAction.ActionType.Activate:
            DoActivate(action.MediaBrowsingCallback, action.ImportResultHandler);
            break;
          case ImporterWorkerAction.ActionType.StartImport:
            DoStartImport(action.JobInformation.GetValueOrDefault());
            break;
          case ImporterWorkerAction.ActionType.CancelImport:
            DoCancelImport(action.JobInformation);
            break;
          case ImporterWorkerAction.ActionType.Suspend:
            DoSuspend();
            break;
          case ImporterWorkerAction.ActionType.Shutdown:
            DoShutdown();
            break;
        }
        action.Complete();
      }
      catch (Exception ex)
      {
        action.Fault(ex);
      }
    }

    #endregion

    #region ImportWorkerAction implementations

    // The methods in this region mirror the methods from the IImporterWorker interface.
    // They are only called from the _actionBlock which ensures that only one of these
    // methods runs at the same time.
    
    private void DoStartup()
    {
      if (_status != Status.Shutdown)
      {
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker: Startup was requested although status was not 'Shutdown' but '{0}'", _status);
        return;
      }
      // ToDo: Start messaging, load persisted Jobs, schedule regular imports
      _status = Status.Suspended;
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Started");
    }

    private void DoActivate(IMediaBrowsing mediaBrowsingCallback, IImportResultHandler importResultHandler)
    {
      if (_status != Status.Suspended)
      {
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker: Activation was requested although status was not 'Started' but '{0}'", _status);
        return;
      }
      Interlocked.Exchange(ref _mediaBrowsing, mediaBrowsingCallback);
      Interlocked.Exchange(ref _importResultHandler, importResultHandler);
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Activated");
    }

    private void DoStartImport(ImportJobInformation importJobInformation)
    {
      // ToDo. Check for Status
      // Todo: Check for overlaps with existing ImportJobs

      var importJobController = new ImportJobController(importJobInformation, _numberOfNextImportJob, this);
      importJobController.Completion.ContinueWith(previousTask => OnImportJobFinished(previousTask, importJobInformation));
      _importJobs[importJobInformation] = importJobController;
      Interlocked.Increment(ref _numberOfNextImportJob);
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Started {0} (Path ='{1}', ImportJobType='{2}', IncludeSubdirectories='{3}')", importJobController, importJobInformation.BasePath, importJobInformation.JobType, importJobInformation.IncludeSubDirectories);
    }

    private void DoCancelImport(ImportJobInformation? importJobInformation)
    {
      if (importJobInformation == null)
      {
        // Cancel all ImportJobs
        foreach (var importJobController in _importJobs.Values)
          importJobController.Cancel();
      }
      else
      {
        // Cancel only the ImportJobs for the specified path
        // (all other fields of importJobInformation are meaningless in case of a cancelation request)
        foreach (var importJobToCancel in _importJobs.Keys)
          if (importJobToCancel.BasePath == importJobInformation.Value.BasePath)
          {
            // ImportJobControllers can be removed asynchronously from _importJobs
            // when they finish. Therefore we double check for null here. If the
            // ImportJobController is no longer there, the ImportJob has finished
            // in the meantime. No need to cancel it anymore.
            ImportJobController importJobController;
            _importJobs.TryGetValue(importJobToCancel, out importJobController);
            if (importJobController != null)
              importJobController.Cancel();
          }
      }
    }

    private void DoSuspend()
    {
      // ToDo
    }

    private void DoShutdown()
    {
      // ToDo: Check for Status
      _actionBlock.Complete();
      if (_actionBlock.InputCount == 0)
        ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Shutdown");
      // ToDo: If there are Actions left after shutdown, cancel the Block
    }

    #endregion

    #region Event handler

    /// <summary>
    /// Continuation that runs after an ImportJob has finished
    /// </summary>
    private void OnImportJobFinished(Task previousTask, ImportJobInformation importJobInformation)
    {
      ImportJobController importJobController;
      if (_importJobs.TryRemove(importJobInformation, out importJobController))
      {
        if (previousTask.IsFaulted)
          ServiceRegistration.Get<ILogger>().Error("ImporterWorker: Error while processing {0}", previousTask.Exception, importJobController);
        else if (previousTask.IsCanceled)
          ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Canceled {0}", importJobController);
        else
          ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Finished {0}", importJobController);
      }
      else
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker: Could not remove ImportJobController for path '{0}'", importJobInformation.BasePath);
    }

    #endregion

    #region Basic helper methods

    private IEnumerable<Guid> GetMetadataExtractorIdsForMediaCategories(IEnumerable<string> mediaCategories)
    {
      var mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      ICollection<Guid> result = new HashSet<Guid>();
      foreach (string mediaCategory in mediaCategories)
        CollectionUtils.AddAll(result, mediaAccessor.GetMetadataExtractorsForCategory(mediaCategory));
      return result;
    }

    #endregion

    #endregion

    #region Interface implementations

    #region IImporterWorker implementation

    public bool IsSuspended
    {
      get
      {
        return _status == Status.Suspended;
      }
    }

    public ICollection<ImportJobInformation> ImportJobs
    {
      get
      {
        // ToDo
        return new List<ImportJobInformation>();
      }
    }

    public void Startup()
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Startup requested...");
      RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.Startup));
    }

    public void Shutdown()
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Shutdown requested...");
      RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.Shutdown)).Wait();
    }

    public void Activate(IMediaBrowsing mediaBrowsingCallback, IImportResultHandler importResultHandler)
    {
      if (mediaBrowsingCallback == null)
        throw new ArgumentNullException("mediaBrowsingCallback");
      if (importResultHandler == null)
        throw new ArgumentNullException("importResultHandler");

      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Activation requested...");
      RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.Activate, mediaBrowsingCallback, importResultHandler));
    }

    public void Suspend()
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Suspension requested...");
      RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.Suspend));
    }

    public void CancelPendingJobs()
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Cancelation of all pending jobs requested...");
      RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.CancelImport));
    }

    public void CancelJobsForPath(ResourcePath path)
    {
      if (path == null)
        throw new ArgumentNullException("path");

      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Cancelation of jobs for path '{0}' requested...", path);
      RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.CancelImport, new ImportJobInformation { BasePath = path }));
    }

    public void ScheduleImport(ResourcePath path, IEnumerable<string> mediaCategories, bool includeSubDirectories)
    {
      if (path == null)
        throw new ArgumentNullException("path");
      if (mediaCategories == null)
        throw new ArgumentNullException("mediaCategories");

      var categories = mediaCategories as string[] ?? mediaCategories.ToArray();
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Import for path '{0}', MediaCategories: '{1}', {2}including subdirectories requested...", path, String.Join(",", categories), includeSubDirectories ? "" : "not ");
      RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.StartImport, new ImportJobInformation(ImportJobType.Import, path, GetMetadataExtractorIdsForMediaCategories(categories), includeSubDirectories)));
    }

    public void ScheduleRefresh(ResourcePath path, IEnumerable<string> mediaCategories, bool includeSubDirectories)
    {
      if (path == null)
        throw new ArgumentNullException("path");
      if (mediaCategories == null)
        throw new ArgumentNullException("mediaCategories");

      var categories = mediaCategories as string[] ?? mediaCategories.ToArray();
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Refresh for path '{0}', MediaCategories: '{1}', {2}including subdirectories requested...", path, String.Join(",", categories), includeSubDirectories ? "" : "not ");
      RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.StartImport, new ImportJobInformation(ImportJobType.Refresh, path, GetMetadataExtractorIdsForMediaCategories(categories), includeSubDirectories)));
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
    }

    #endregion

    #endregion
  }
}
