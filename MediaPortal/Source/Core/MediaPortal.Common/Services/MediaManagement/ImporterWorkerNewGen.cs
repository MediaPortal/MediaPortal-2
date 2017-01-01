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
using System.Threading.Tasks.Dataflow;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.TaskScheduler;
using MediaPortal.Utilities;
using Task = System.Threading.Tasks.Task;

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
  /// (except for Shutdown()) immediately return to the caller. For every such call, an 
  /// <see cref="ImporterWorkerAction"/> is instantiated and posted to an ActionBlock. From there,
  /// the <see cref="ImporterWorkerAction"/>s are picked up and processed sequentially but asynchronously.
  /// A call to Shutdown() blocks until the shutdown is completed.
  /// For every scheduled ImportJob, an <see cref="ImportJobController"/> is instantiated. The
  /// <see cref="ImportJobController"/> is added to a ConcurrentDictionary where it remains until the respective
  /// ImportJob is finished.
  /// Situations affecting a particular ImportJob (including setting up the necessary TPL Dataflow network for an
  /// ImportJob, canceling, suspending or activating a particular ImportJob) are handled by the respective
  /// <see cref="ImportJobController"/>.
  /// This class handles every situation that may affect all ImportJobs, such as suspension or activation of all
  /// ImportJobs or shutdown. It coordinates these actions between all the ImportJobs and then calls the respective
  /// methods of the relevant <see cref="ImportJobController"/>s.
  /// </remarks>
  public class ImporterWorkerNewGen : IImporterWorker, IDisposable
  {
    #region Enums

    /// <summary>
    /// Represents the status of the <see cref="ImporterWorkerNewGen"/>
    /// </summary>
    /// <remarks>
    /// <see cref="Shutdown"/>: The <see cref="ImporterWorkerNewGen "/> was either just instantiated and Startup()
    /// has not yet been called or the <see cref="ImporterWorkerNewGen"/>'s Shutdown() method has been called.
    /// In this state the <see cref="ImporterWorkerNewGen"/>'s status is saved to disk. It does not listen to or
    /// send messages, nor does it process any ImportJobs or accept any new ImportJobs to be scheduled.
    /// <see cref="Suspended"/>: Either the <see cref="ImporterWorkerNewGen"/> had the state <see cref="Shutdown"/>
    /// and the Startup() method has been called or it had the state <see cref="Activated"/> and the Suspend()
    /// method has been called. In this state, the <see cref="ImporterWorkerNewGen"/> listens to and sends messages,
    /// accepts new ImportJobs to be scheduled or cancelations of existing ImportJobs. However, all pending ImportJobs
    /// are not processed, but suspended. In the MP2-Server this state is valid only for a short time when the MP2-Server
    /// service starts or when the service terminates. In the MP2-Client this status is additionally valid when the
    /// client has lost its connection to the MP2-Server.
    /// <see cref="Activated"/>: The <see cref="ImporterWorkerNewGen"/>'s Activate() method has been called. In this
    /// state, all the pending ImportJobs are being processed.
    /// State changes are only valid in the following order:
    /// Shutdown -> Suspended, Suspended -> Shutdown, Suspended -> Activated and Activated -> Suspended.
    /// </remarks>
    public enum Status
    {
      Shutdown,
      Suspended,
      Activated
    }

    #endregion

    #region Variables

    /// <summary>
    /// Processes the requested <see cref="ImporterWorkerAction"/>s
    /// </summary>
    /// <remarks>
    /// This DataflowBlock must not have a MaxDegreeOfParallelism other than 1 which is the default value.
    /// It is required behaviour for this block to process one <see cref="ImporterWorkerAction"/> after the other.
    /// </remarks>
    private readonly ActionBlock<ImporterWorkerAction> _actionBlock;

    /// <summary>
    /// Holds one <see cref="ImportJobController"/> for every active ImportJob
    /// </summary>
    /// <remarks>
    /// This dictionaly is in general only accessed through <see cref="ImporterWorkerAction"/>s and thus sequentially.
    /// However, when an ImportJob finishes, the ImportJob is removed from this dictionaly asynchronously, which is
    /// why we need to use a ConcurrentDictionary.
    /// </remarks>
    private readonly ConcurrentDictionary<ImportJobInformation, ImportJobController> _importJobControllers;

    /// <summary>
    /// Holds the unique number that was assigned to the last <see cref="ImportJobController"/>
    /// </summary>
    private int _numberOfLastImportJob;

    /// <summary>
    /// Abstraction object for write access to the MediaLibrary
    /// </summary>
    /// <remarks>
    /// This field is only accessed through <see cref="ImporterWorkerAction"/>s and thus sequentially
    /// </remarks>
    private IImportResultHandler _importResultHandler;
    
    /// <summary>
    /// Abstraction object for read access to the MediaLibrary
    /// </summary>
    /// <remarks>
    /// This field is only accessed through <see cref="ImporterWorkerAction"/>s and thus sequentially
    /// </remarks>
    private IMediaBrowsing _mediaBrowsing;

    /// <summary>
    /// <see cref="Status"/> of the <see cref="ImporterWorkerNewGen"/>
    /// </summary>
    /// <remarks>
    /// This field may be accessed asynchronously through the <see cref="IsSuspended"/> property, which
    /// is why we make it volatile.
    /// </remarks>
    private volatile Status _status;

    /// <summary>
    /// A value that is incremented by one for every notification this <see cref="ImporterWorkerNewGen"/>
    /// received from one of its <see cref="ImportJobController"/>s
    /// </summary>
    private long _numberOfProgressNotifications;

    /// <summary>
    /// Message queue to listen to system messages
    /// </summary>
    private readonly AsynchronousMessageQueue _messageQueue;

    /// <summary>
    /// Notifies about changes to the <see cref="ImporterWorkerSettings"/>
    /// </summary>
    private readonly SettingsChangeWatcher<ImporterWorkerSettings> _settings;

    /// <summary>
    /// GUID of the regular refresh import task
    /// </summary>
    private Guid _importerTaskId;

    #endregion

    #region Constructor

    public ImporterWorkerNewGen()
    {
      _actionBlock = new ActionBlock<ImporterWorkerAction>(action => ProcessActionRequest(action));
      _importJobControllers = new ConcurrentDictionary<ImportJobInformation, ImportJobController>();
      _numberOfLastImportJob = 0;
      _numberOfProgressNotifications = 0;
      _status = Status.Shutdown;
      _settings = new SettingsChangeWatcher<ImporterWorkerSettings>();
      _settings.SettingsChanged += OnSettingsChanged;
      _messageQueue = new AsynchronousMessageQueue(this, new[]
        {
          SystemMessaging.CHANNEL,
          TaskSchedulerMessaging.CHANNEL
        });
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    #endregion

    #region Private methods

    #region ImporterWorkerAction handling

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
          case ImporterWorkerAction.ActionType.ScheduleImport:
            DoScheduleImport(action.JobInformation.GetValueOrDefault());
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
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker: Error while processing Action of type {0}", ex, action.Type);
        action.Fault(ex);
      }
    }

    #endregion

    #region ImporterWorkerAction implementations

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
      _messageQueue.Start();
      LoadPendingImportJobs();
      ScheduleRegularRefreshImports();
      _status = Status.Suspended;
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Started");
    }

    private void DoActivate(IMediaBrowsing mediaBrowsingCallback, IImportResultHandler importResultHandler)
    {
      if (_status != Status.Suspended)
      {
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker: Activation was requested although status was not 'Suspended' but '{0}'", _status);
        return;
      }

      _mediaBrowsing = mediaBrowsingCallback;
      _importResultHandler = importResultHandler;

      foreach (var kvp in _importJobControllers)
      {
        // To avoid peaks on system startup when multiple ImportJobs were saved to disk and are now
        // reactivated, we start one ImportJob every 500ms.
        // Currently we also need this because the MediaAccessor is not threadsafe on startup
        // see here: http://forum.team-mediaportal.com/threads/mediaaccessor-not-thread-safe.125132/
        // ToDo: Make MediaAccessor threadsafe on startup
        Task.Delay(500).Wait();
        kvp.Value.Activate(_mediaBrowsing, _importResultHandler);
      }

      _status = Status.Activated;
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Activated ({0} ImportJobs pending)", _importJobControllers.Count);
    }

    private void DoScheduleImport(ImportJobInformation importJobInformation)
    {
      if (_status == Status.Shutdown)
      {
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker: Scheduling of an ImportJob was requested although status was neither 'Activated' nor 'Suspended' but 'Shutdown'");
        return;
      }

      // For now we always set this to active to make it look like the old ImporterWorker
      // ToDo: Remove this and all usages of ImportJobInformation.State
      importJobInformation.State = ImportJobState.Active;
      
      // if the ImportJob to be scheduled is the same as or contains an
      // already running ImportJob, cancel the already running ImportJob
      // and schedule this one
      var jobsToBeCancelled = new HashSet<ImportJobController>();
      foreach (var kvp in _importJobControllers)
        if (importJobInformation >= kvp.Key)
        {
          ServiceRegistration.Get<ILogger>().Info("ImporterWorker: {0} is contained in or the same as the ImportJob which is currently being scheduled. Canceling {1}", kvp.Value, kvp.Value);
          kvp.Value.Cancel();
          jobsToBeCancelled.Add(kvp.Value);
        }
      // We need to wait here until the canceled ImportJobs are removed from _importJobControllers
      // otherwise we run into trouble when the ImportJobs equal each other because then they
      // have the same key in _importJobControllers.
      Task.WhenAll(jobsToBeCancelled.Select(controller => controller.Completion)).Wait();
      foreach (var controller in jobsToBeCancelled)
        controller.Dispose();

      var importJobController = new ImportJobController(new ImportJobNewGen(importJobInformation, null), Interlocked.Increment(ref _numberOfLastImportJob), this);
      _importJobControllers[importJobInformation] = importJobController;

      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Scheduled {0} ({1}) (Path ='{2}', ImportJobType='{3}', IncludeSubdirectories='{4}')", importJobController, _status, importJobInformation.BasePath, importJobInformation.JobType, importJobInformation.IncludeSubDirectories);
      ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportScheduled, importJobInformation.BasePath, importJobInformation.JobType);

      if (_status == Status.Activated)
        importJobController.Activate(_mediaBrowsing, _importResultHandler);
    }

    private void DoCancelImport(ImportJobInformation? importJobInformation)
    {
      if (_status == Status.Shutdown)
      {
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker: Cancelation of an ImportJob was requested although status was neither 'Activated' nor 'Suspended' but 'Shutdown'");
        return;
      }

      if (importJobInformation == null)
      {
        // Cancel all ImportJobs
        foreach (var kvp in _importJobControllers)
          kvp.Value.Cancel();
        Task.WhenAll(_importJobControllers.Values.Select(i => i.Completion)).Wait();
        foreach (var kvp in _importJobControllers)
          kvp.Value.Dispose();
      }
      else
      {
        // Cancel only the ImportJobs for the specified path and all its child paths
        var jobsToBeCanceled = new HashSet<ImportJobController>();
        foreach (var kvp in _importJobControllers)
          if (importJobInformation.Value.BasePath.IsSameOrParentOf(kvp.Key.BasePath))
          {
            kvp.Value.Cancel();
            jobsToBeCanceled.Add(kvp.Value);
          }
        Task.WhenAll(jobsToBeCanceled.Select(controller => controller.Completion)).Wait();
        foreach (var controller in jobsToBeCanceled)
          controller.Dispose();
      }
    }

    private void DoSuspend()
    {
      // This method can be called when the Status is Active or already Suspended. The reason for the latter is
      // that when a DataflowBlock contained in an ImportJobController accesses the MediaLibrary and realizes that
      // the MediaLibrary is not accessible because the connection to the MP2-Server was lost, it will request a
      // call to this method. Since we run multiple DataflowBlocks in parallel, several DataflowBlocks may realize
      // the disconnect and each of them requests a call to this method. If the state was already Suspended, we do
      // noting and just return.
      if (_status == Status.Shutdown)
      {
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker: Suspension was requested although status was 'Shutdown'");
        return;
      }
      if (_status == Status.Suspended)
        return;

      foreach (var kvp in _importJobControllers)
        kvp.Value.Suspend();

      _status = Status.Suspended;
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Suspended ({0} ImportJobs pending)", _importJobControllers.Count);
    }

    private void DoShutdown()
    {
      if (_status == Status.Shutdown)
      {
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker: Shutdown was requested although status was already 'Shutdown'");
        return;
      }
      if (_status == Status.Activated)
        DoSuspend();

      _messageQueue.Shutdown();

      // Cancel all ImportJobs but do not dispose them, yet to be able to save them to disk
      foreach (var kvp in _importJobControllers)
        kvp.Value.Cancel();
      Task.WhenAll(_importJobControllers.Values.Select(i => i.Completion)).Wait();
      
      int numberOfPendingImportJobs = _importJobControllers.Count();
      PersistPendingImportJobs();
      if (numberOfPendingImportJobs > 0)
        ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Persisted {0} pending ImportJobs to disk.", numberOfPendingImportJobs);

      foreach (var kvp in _importJobControllers)
        kvp.Value.Dispose();
      _settings.Dispose();

      _actionBlock.Complete();
      _status = Status.Shutdown;
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Shutdown");
    }

    #endregion

    #region Event handler

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        var messageType = (SystemMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SystemMessaging.MessageType.SystemStateChanged:
            var newState = (SystemState)message.MessageData[SystemMessaging.NEW_STATE];
            if (newState == SystemState.ShuttingDown)
              Suspend();
            break;
        }
      }
      if (message.ChannelName == TaskSchedulerMessaging.CHANNEL)
      {
        var messageType = (TaskSchedulerMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case TaskSchedulerMessaging.MessageType.DUE:
            var dueTask = (Common.TaskScheduler.Task)message.MessageData[TaskSchedulerMessaging.TASK];
            if (dueTask.ID == _importerTaskId)
              // Forward a new message which will be handled by MediaLibrary (it knows the shares and configuration), then it will
              // schedule the local shares.
              ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.RefreshLocalShares);
            break;
        }
      }
    }

    private void OnSettingsChanged(object sender, EventArgs e)
    {
      // Do not react on next SettingsChanged event
      // ScheduleRegularRefreshImports() may change the ImporterWorkerSettings, which would
      // trigger the OnSettingsChaned event resulting in an infinite loop.
      _settings.SettingsChanged = null;
      ScheduleRegularRefreshImports();
      _settings.SettingsChanged += OnSettingsChanged;
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

    private void PersistPendingImportJobs()
    {
      if (_status != Status.Suspended)
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker: ImportJobs can only be persisted if the ImporterWorker is in status suspended.");
      else
      {
        var settings = new PendingResourcesSettings { PendingImportJobs = _importJobControllers.Values.Select(i => i.ImportJob).ToList() };
        ServiceRegistration.Get<ISettingsManager>().Save(settings);
      }
    }

    private void LoadPendingImportJobs()
    {
      if (_status != Status.Shutdown)
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker: ImportJobs can only be loaded if the ImporterWorker is in status shutdown.");
      else
      {
        var settings = ServiceRegistration.Get<ISettingsManager>().Load<PendingResourcesSettings>();
        if (settings.PendingImportJobs.Count > 0)
        {
          int numberOfSuccessfullyRestoredImportJobs = 0;
          foreach (var importJob in settings.PendingImportJobs)
          {
            try
            {
              var importJobController = new ImportJobController(importJob, Interlocked.Increment(ref _numberOfLastImportJob), this);
              numberOfSuccessfullyRestoredImportJobs++;
              _importJobControllers[importJob.ImportJobInformation] = importJobController;

            }
            catch (Exception ex)
            {
              ServiceRegistration.Get<ILogger>().Error("ImporterWorker: Error while restoring ImportJob from disk", ex);
            }
          }
          ServiceRegistration.Get<ILogger>().Info("ImporterWorker: {0} persisted ImportJobs restored from disk", numberOfSuccessfullyRestoredImportJobs);
        }
      }
    }

    private void SendProgressNotificationMessage()
    {
      ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportProgress, _importJobControllers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Progress));
    }

    private void LogProgress()
    {
      var progress = _importJobControllers.Select(kvp => kvp.Value.Progress).ToList();
      var created = progress.Sum(i => i.Item1);
      var completed = progress.Sum(i => i.Item2);
      if (created != 0)
        ServiceRegistration.Get<ILogger>().Info("ImporterWorker: {0:P0} completed ({1} ImportJob(s), in total {2} of {3} so far identified resources processed)", (double)completed / created, progress.Count, completed, created);
    }

    private void ScheduleRegularRefreshImports()
    {
      var scheduler = ServiceRegistration.Get<ITaskScheduler>();
      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      _importerTaskId = _settings.Settings.ImporterScheduleId;

      // Allow removal of existing import tasks
      if (!_settings.Settings.EnableAutoRefresh)
      {
        if (_importerTaskId != Guid.Empty)
        {
          scheduler.RemoveTask(_importerTaskId);
          _importerTaskId = _settings.Settings.ImporterScheduleId = Guid.Empty;
          settingsManager.Save(_settings.Settings);
        }
        return;
      }

      var schedule = new Schedule
      {
        Hour = (int)_settings.Settings.ImporterStartTime,
        Minute = (int)((_settings.Settings.ImporterStartTime - (int)_settings.Settings.ImporterStartTime) * 60),
        Day = -1,
        Type = ScheduleType.TimeBased
      };

      var importTask = new Common.TaskScheduler.Task("ImporterWorker", schedule, Occurrence.Repeat, DateTime.MaxValue, true, true);
      if (_importerTaskId == Guid.Empty)
      {
        _importerTaskId = scheduler.AddTask(importTask);
        _settings.Settings.ImporterScheduleId = _importerTaskId;
        settingsManager.Save(_settings.Settings);
      }
      else
        scheduler.UpdateTask(_importerTaskId, importTask);
    }

    #endregion

    #endregion

    #region Internal helper methods

    /// <summary>
    /// Requests an <see cref="ImporterWorkerAction"/>
    /// </summary>
    /// <param name="action"><see cref="ImporterWorkerAction"/> to be requested</param>
    /// <returns><see cref="System.Threading.Tasks.Task"/> that completes when the <see cref="ImporterWorkerAction"/> is completed</returns>
    internal Task RequestAction(ImporterWorkerAction action)
    {
      _actionBlock.Post(action);
      return action.Completion;
    }

    /// <summary>
    /// Tries to remove an ImportJobController from <see cref="_importJobControllers"/>
    /// </summary>
    /// <param name="importJobInformation">
    /// <see cref="ImportJobInformation"/> describing the <see cref="ImportJobController"/> to be removed
    /// </param>
    /// <returns>true if removal was successful, otherwise false</returns>
    internal bool TryUnregisterImportJobController(ImportJobInformation importJobInformation)
    {
      ImportJobController removedController;
      return _importJobControllers.TryRemove(importJobInformation, out removedController);
    }

    /// <summary>
    /// This method is called by <see cref="ImportJobController"/>s when they think it's time to notify about progress
    /// </summary>
    /// <remarks>
    /// <param name="forceNotification"></param> is true when the ImportJobController was just created or has finished.
    /// In this case we always send an ImportProgress message, but don't log because start and completion of an
    /// <see cref="ImportJobController"/> are already logged.
    /// In all other cases <param name="forceNotification"></param> is false. We only react on every fourth notification
    /// of this kind and then log the progress and send a respective ImportProgress messsage.
    /// </remarks>
    /// <param name="forceNotification">If true, an ImportProgress message is guaranteed to be sent</param>
    internal void NotifyProgress(bool forceNotification)
    {
      if (forceNotification)
      {
        SendProgressNotificationMessage();
        return;
      }
      if (Interlocked.Increment(ref _numberOfProgressNotifications) % 4 == 0)
      {
        LogProgress();
        SendProgressNotificationMessage();
      }
    }

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
        return _importJobControllers.Keys;
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
      RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.ScheduleImport, new ImportJobInformation(ImportJobType.Import, path, GetMetadataExtractorIdsForMediaCategories(categories), includeSubDirectories)));
    }

    public void ScheduleRefresh(ResourcePath path, IEnumerable<string> mediaCategories, bool includeSubDirectories)
    {
      if (path == null)
        throw new ArgumentNullException("path");
      if (mediaCategories == null)
        throw new ArgumentNullException("mediaCategories");

      var categories = mediaCategories as string[] ?? mediaCategories.ToArray();
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Refresh for path '{0}', MediaCategories: '{1}', {2}including subdirectories requested...", path, String.Join(",", categories), includeSubDirectories ? "" : "not ");
      RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.ScheduleImport, new ImportJobInformation(ImportJobType.Refresh, path, GetMetadataExtractorIdsForMediaCategories(categories), includeSubDirectories)));
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      if (_status != Status.Shutdown)
        DoShutdown();
    }

    #endregion

    #endregion
  }
}
