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
using System.Threading.Tasks;
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
  /// This class makes heavy use of multitasking. Adding ImportJobs returns nearly immediately to the caller.
  /// These ImportJobs are added to a BlockingCollection from where they are picked up by <see cref="ProcessNewImportJobRequests"/>
  /// running in another task. This task sets up an <see cref="ImportJobController"/> for every ImportJob. the
  /// <see cref="ImportJobController"/> is added to a ConcurrentDictionary where it remains until the respective
  /// ImportJob is finished. The ImportJob itself (including setting up the necessary TPL Dataflow network) is
  /// handled by the respective <see cref="ImportJobController"/>.
  /// This class handles every situation that may affect all ImportJobs, such as suspension or shutdown. Situations
  /// that only affect single ImportJobs are handled by the respective <see cref="ImportJobController"/>s.
  /// ToDo: Handle suspension
  /// ToDo: Handle state saving on shutdown
  /// ToDo: Handle cancellation of ImportJobs
  /// ToDo: Handle messaging
  /// </remarks>
  public class ImporterWorkerNewGen : IImporterWorker, IDisposable
  {
    #region Variables

    /// <summary>
    /// Holds an <see cref="ImportJobInformation"/> for every ImportJob waiting for its
    /// <see cref="ImportJobController"/> to be set up
    /// </summary>
    private readonly BlockingCollection<ImportJobInformation> _newImportJobRequests;
    
    /// <summary>
    /// Executes <see cref="ProcessNewImportJobRequests"/>. Completes when <see cref="_newImportJobRequests"/>'
    /// CompleteAdding method is called and <see cref="ImportJobController"/>s for all remaining entries of
    /// <see cref="_newImportJobRequests"/> have been set up
    /// </summary>
    private Task _newImportJobRequestProcessorTask;
    
    /// <summary>
    /// Holds one <see cref="ImportJobController"/> for every active ImportJob
    /// </summary>
    private readonly ConcurrentDictionary<ImportJobInformation, ImportJobController> _importJobs;
    
    private IImportResultHandler _importResultHandler;
    private IMediaBrowsing _mediaBrowsing;

    #endregion

    #region Constructor

    public ImporterWorkerNewGen()
    {
      _newImportJobRequests = new BlockingCollection<ImportJobInformation>();
      _importJobs = new ConcurrentDictionary<ImportJobInformation, ImportJobController>();
    }

    #endregion

    #region Public properties

    public IMediaBrowsing MediaBrowsing
    {
      get { return _mediaBrowsing; }
    }

    public IImportResultHandler ImportResultHandler
    {
      get { return _importResultHandler; }
    }

    #endregion

    #region Private methods

    #region Processing Loops

    /// <summary>
    /// Runs in a separate task and creates an <see cref="ImportJobController"/> for every
    /// <see cref="ImportJobInformation"/> in <see cref="_newImportJobRequests"/>
    /// </summary>
    private void ProcessNewImportJobRequests()
    {
      // GetConsumingEnumerable returns an Enumerable which blocks until (a) the next item is
      // available or (b) the CompleteAdding method is called
      foreach (var newImportJobInformation in _newImportJobRequests.GetConsumingEnumerable())
      {
        // Todo: Check for overlaps with existing ImportJobs

        var importJobInformation = new ImportJobInformation(newImportJobInformation);
        var importJobController = new ImportJobController(importJobInformation, this);
        importJobController.Completion.ContinueWith(prevTask => OnImportJobCompleted(importJobInformation));
        _importJobs[importJobInformation] = importJobController;
        ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Started {0}", importJobController);
      }
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: NewImportJobRequestProcessor finished...");
    }

    #endregion

    #region Event handler

    private void OnImportJobCompleted(ImportJobInformation importJobInformation)
    {
      ImportJobController importJobController;
      _importJobs.TryRemove(importJobInformation, out importJobController);
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Finished {0}", importJobController);
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
        return false;
      }
      internal set
      {
        if (value)
        {
          
        }
      }
    }

    public ICollection<ImportJobInformation> ImportJobs
    {
      get
      {
        return new List<ImportJobInformation>();
      }
    }

    public void Startup()
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Starting up...");
    }

    public void Shutdown()
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Shutting down...");
      
      // Do not allow adding more ImportJobs.
      // At the same time ends the ProcessNewImportJobRequests loop.
      _newImportJobRequests.CompleteAdding();
      _newImportJobRequestProcessorTask.Wait();
    }

    public void Activate(IMediaBrowsing mediaBrowsingCallback, IImportResultHandler importResultHandler)
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Activating...");

      _mediaBrowsing = mediaBrowsingCallback;
      _importResultHandler = importResultHandler;
      
      // Start the ProcessNewImportJobRequests loop and reserve a separate Thread for it in
      // the ThreadPool (therefore we use TaskCreationOptions.LongRunning)
      _newImportJobRequestProcessorTask = Task.Factory.StartNew(ProcessNewImportJobRequests, TaskCreationOptions.LongRunning);
    }

    public void Suspend()
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Suspending...");
    }

    public void CancelPendingJobs()
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Cancelling pending jobs...");
    }

    public void CancelJobsForPath(ResourcePath path)
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Cancelling jobs for path '{0}'...", path);
    }

    public void ScheduleImport(ResourcePath path, IEnumerable<string> mediaCategories, bool includeSubDirectories)
    {
      var categories = mediaCategories as string[] ?? mediaCategories.ToArray();
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Scheduling import for path '{0}', MediaCategories: '{1}', {2}including subdirectories...", path, String.Join(",", categories), includeSubDirectories ? "" : "not ");
      _newImportJobRequests.Add(new ImportJobInformation(ImportJobType.Import, path, GetMetadataExtractorIdsForMediaCategories(categories), includeSubDirectories));
    }

    public void ScheduleRefresh(ResourcePath path, IEnumerable<string> mediaCategories, bool includeSubDirectories)
    {
      var categories = mediaCategories as string[] ?? mediaCategories.ToArray();
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Scheduling refresh for path '{0}', MediaCategories: '{1}', {2}including subdirectories...", path, String.Join(",", categories), includeSubDirectories ? "" : "not ");
      _newImportJobRequests.Add(new ImportJobInformation(ImportJobType.Refresh, path, GetMetadataExtractorIdsForMediaCategories(categories), includeSubDirectories));
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
