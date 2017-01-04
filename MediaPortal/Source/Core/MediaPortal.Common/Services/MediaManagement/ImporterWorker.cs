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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.TaskScheduler;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Common.Services.MediaManagement
{
  /// <summary>
  /// The import was suspended and will be continued at another time.
  /// </summary>
  internal class ImportSuspendedException : ApplicationException
  {
  }

  /// <summary>
  /// The import was aborted.
  /// </summary>
  internal class ImportAbortException : ApplicationException
  {
  }

  public class ImporterWorker : IImporterWorker, IDisposable
  {
    #region Consts

    protected static IEnumerable<Guid> IMPORTER_MIA_ID_ENUMERATION = new Guid[]
        {
          ImporterAspect.ASPECT_ID,
        };
    protected static IEnumerable<Guid> IMPORTER_PROVIDER_MIA_ID_ENUMERATION = new Guid[]
        {
          ImporterAspect.ASPECT_ID,
          ProviderResourceAspect.ASPECT_ID,
        };
    protected static IEnumerable<Guid> DIRECTORY_MIA_ID_ENUMERATION = new Guid[]
        {
          DirectoryAspect.ASPECT_ID,
        };
    protected static IEnumerable<Guid> EMPTY_MIA_ID_ENUMERATION = new Guid[] { };

    #endregion

    protected AsynchronousMessageQueue _messageQueue;
    protected object _syncObj = new object();
    protected string _localSystemId = null;
    protected Thread _workerThread = null;
    protected IMediaBrowsing _mediaBrowsingCallback = null;
    protected IImportResultHandler _importResultHandler = null;
    protected List<ImportJob> _importJobs = new List<ImportJob>(); // We need more flexibility than the default Queue implementation provides, so we just use a List
    protected ManualResetEvent _suspendedEvent = new ManualResetEvent(true);
    protected AutoResetEvent _importJobsReadyAvailableEvent = new AutoResetEvent(false);
    protected Guid _importerTaskId;
    protected SettingsChangeWatcher<ImporterWorkerSettings> _settings = new SettingsChangeWatcher<ImporterWorkerSettings>();
    protected bool _ignoreChange = false;

    public ImporterWorker()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
          {
            SystemMessaging.CHANNEL,
            TaskSchedulerMessaging.CHANNEL
          });
      _messageQueue.MessageReceived += OnMessageReceived;
      // Message queue will be started in method Start()
      _settings.SettingsChanged += OnSettingsChanged;
    }

    public void Dispose()
    {
      ShutdownImporterLoop();
      _messageQueue.Shutdown();
      _suspendedEvent.Set();
      _suspendedEvent.Close();
      _importJobsReadyAvailableEvent.Close();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case SystemMessaging.MessageType.SystemStateChanged:
            SystemState newState = (SystemState) message.MessageData[SystemMessaging.NEW_STATE];
            if (newState == SystemState.ShuttingDown)
              IsSuspended = true;
            break;
        }
      }
      if (message.ChannelName == TaskSchedulerMessaging.CHANNEL)
      {
        TaskSchedulerMessaging.MessageType messageType = (TaskSchedulerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case TaskSchedulerMessaging.MessageType.DUE:
            Task dueTask = (Task) message.MessageData[TaskSchedulerMessaging.TASK];
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
      if (!_ignoreChange)
        ScheduleImports();

      _ignoreChange = false;
    }

    protected bool IsImportJobAvailable
    {
      get
      {
        lock (_syncObj)
          return _importJobs.Count > 0;
      }
    }

    protected void Initialize()
    {
      _localSystemId = ServiceRegistration.Get<ISystemResolver>().LocalSystemId;
    }

    protected ICollection<Guid> GetMetadataExtractorIdsForMediaCategories(IEnumerable<string> mediaCategories)
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      ICollection<Guid> result = new HashSet<Guid>();
      foreach (string mediaCategory in mediaCategories)
        CollectionUtils.AddAll(result, mediaAccessor.GetMetadataExtractorsForCategory(mediaCategory));
      return result;
    }

    protected void PersistPendingImportJobs()
    {
      lock (_syncObj)
      {
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
        _settings.Settings.PendingImportJobs = new List<ImportJob>(_importJobs);
        settingsManager.Save(_settings.Settings);
        foreach (ImportJob job in _importJobs)
          job.Dispose();
        _importJobs.Clear();
      }
    }

    protected void LoadPendingImportJobs()
    {
      lock (_syncObj)
      {
        _importJobs.Clear();
        CollectionUtils.AddAll(_importJobs, _settings.Settings.PendingImportJobs);
        _importJobsReadyAvailableEvent.Set();
      }
    }

    protected void ScheduleImports()
    {
      lock (_syncObj)
      {
        ITaskScheduler scheduler = ServiceRegistration.Get<ITaskScheduler>();
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
        _importerTaskId = _settings.Settings.ImporterScheduleId;

        // Allow removal of existing import tasks
        if (!_settings.Settings.EnableAutoRefresh) 
        {
          if (_importerTaskId != Guid.Empty)
          {
            scheduler.RemoveTask(_importerTaskId);
            _importerTaskId = _settings.Settings.ImporterScheduleId = Guid.Empty;
            _ignoreChange = true; // Do not react on next setting's change message!
            settingsManager.Save(_settings.Settings);
          }
          return;
        }

        Schedule schedule = new Schedule
          {
            Hour = (int) _settings.Settings.ImporterStartTime,
            Minute = (int) ((_settings.Settings.ImporterStartTime - (int) _settings.Settings.ImporterStartTime) * 60),
            Day = -1,
            Type = ScheduleType.TimeBased
          };
        
        Task importTask = new Task("ImporterWorker", schedule, Occurrence.Repeat, DateTime.MaxValue, true, true);
        if (_importerTaskId == Guid.Empty)
        {
          _importerTaskId = scheduler.AddTask(importTask);
          _settings.Settings.ImporterScheduleId = _importerTaskId;
          _ignoreChange = true; // Do not react on next setting's change message!
          settingsManager.Save(_settings.Settings);
        }
        else
          scheduler.UpdateTask(_importerTaskId, importTask);
      }
    }

    protected void CheckSuspended(Exception e)
    {
      if (e is ImportSuspendedException)
      {
        IsSuspended = true;
        throw e;
      }
      if (e is DisconnectedException)
      {
        IsSuspended = true;
        throw new ImportSuspendedException();
      }
      if (IsSuspended)
        throw new ImportSuspendedException();
    }

    protected void CheckImportStillRunning(ImportJobState state)
    {
      CheckSuspended(null);
      if (state == ImportJobState.Cancelled || state == ImportJobState.Erroneous)
        throw new ImportAbortException();
    }

    protected void EnqueueImportJob(ImportJob job)
    {
      lock (_syncObj)
      {
        job.State = ImportJobState.Scheduled;
        _importJobs.Insert(0, job);
        _importJobsReadyAvailableEvent.Set();
      }
    }

    protected ImportJob DequeueImportJob()
    {
      lock (_syncObj)
      {
        ImportJob job = PeekImportJob();
        if (job != null)
          _importJobs.RemoveAt(_importJobs.Count - 1);
        return job;
      }
    }

    protected ImportJob PeekImportJob()
    {
      lock (_syncObj)
        return _importJobs.Count == 0 ? null : _importJobs[_importJobs.Count - 1];
    }

    protected void RemoveImportJob(ImportJob job)
    {
      lock (_syncObj)
        _importJobs.Remove(job);
      job.Dispose();
    }

    protected void ImporterLoop()
    {
      while (!IsSuspended)
      {
        // We handle three cases here:
        // 1) Job available - process job and continue
        // 2) No job available - sleep until a job is available or we are suspended
        // 3) Job cannot be processed - exit loop and suspend
        ImportJob job;
        while ((job = PeekImportJob()) != null)
        {
          Process(job);
          ImportJobState state = job.State;
          if (state == ImportJobState.Finished || state == ImportJobState.Erroneous || state == ImportJobState.Cancelled)
            RemoveImportJob(job); // Maybe the queue was changed (cleared & filled again), so a simple call to Dequeue() could dequeue the wrong job
          if (IsSuspended)
            break;
        }
        if (!IsImportJobAvailable)
          WaitHandle.WaitAny(new WaitHandle[] { _importJobsReadyAvailableEvent, _suspendedEvent });
      }
    }

    protected void StartImporterLoop_NoLock()
    {
      Thread workerThread;
      lock (_syncObj)
        workerThread = _workerThread;
      if (workerThread != null)
        if (IsSuspended)
          // Still running - will end soon
          workerThread.Join(); // Must be done outside the lock, else we'd deadlock with the worker thread
        else
          // Already running - nothing to do
          return;
      lock (_syncObj)
      {
        IsSuspended = false;
        _workerThread = new Thread(ImporterLoop) { Name = "Importer", Priority = ThreadPriority.Lowest };
        _workerThread.Start();
      }
    }

    protected void ShutdownImporterLoop()
    {
      IsSuspended = true;
      Thread workerThread;
      lock (_syncObj)
        workerThread = _workerThread;
      if (workerThread != null)
        workerThread.Join(); // Must be done outside the lock, else we'd deadlock with the worker thread
      lock (_syncObj)
        _workerThread = null;
    }

    /// <summary>
    /// Imports the resource with the given <paramref name="mediaItemAccessor"/>.
    /// </summary>
    /// <remarks>
    /// This method will be called for file resources as well as for directory resources because some metadata extractors
    /// extract their metadata from directories.
    /// </remarks>
    /// <param name="mediaItemAccessor">File or directory resource to be imported.</param>
    /// <param name="parentDirectoryId">Media item id of the parent directory, if present, else <see cref="Guid.Empty"/>.</param>
    /// <param name="metadataExtractors">Collection of metadata extractors to apply to the given resoure.</param>
    /// <param name="resultHandler">Callback to notify the import results.</param>
    /// <param name="mediaAccessor">Convenience reference to the media accessor.</param>
    /// <returns><c>true</c>, if metadata could be extracted from the given <paramref name="mediaItemAccessor"/>, else
    /// <c>false</c>.</returns>
    protected bool ImportResource(ImportJob importJob, IResourceAccessor mediaItemAccessor, Guid parentDirectoryId, ICollection<IMetadataExtractor> metadataExtractors, 
      IImportResultHandler resultHandler, IMediaAccessor mediaAccessor)
    {
      const bool importOnly = false; // Allow extractions with probably longer runtime.
      ResourcePath path = mediaItemAccessor.CanonicalLocalResourcePath;
      ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportStatus, path);
      IDictionary<Guid, IList<MediaItemAspect>> aspects = mediaAccessor.ExtractMetadata(mediaItemAccessor, metadataExtractors, importOnly);
      if (aspects == null)
        // No metadata could be extracted
        return false;
      using (CancellationTokenSource cancelToken = new CancellationTokenSource())
      {
        try
        {
          resultHandler.UpdateMediaItem(parentDirectoryId, path, MediaItemAspect.GetAspects(aspects), importJob.JobType == ImportJobType.Refresh, importJob.BasePath, cancelToken.Token);
          resultHandler.DeleteUnderPath(path);
        }
        catch
        {
          cancelToken.Cancel();
          throw;
        }
      }
      return true;
    }

    /// <summary>
    /// Imports or refreshes a single file without a parent directory with the specified <paramref name="resourceAccessor"/>.
    /// </summary>
    /// <param name="importJob">The import job being processed.</param>
    /// <param name="resourceAccessor">Resource accessor for the file to import.</param>
    /// <param name="metadataExtractors">Metadata extractors to apply on the resource.</param>
    /// <param name="mediaBrowsing">Callback interface to the media library for the refresh import type.</param>
    /// <param name="resultHandler">Callback to notify the import result.</param>
    /// <param name="mediaAccessor">Convenience reference to the media accessor.</param>
    protected void ImportSingleFile(ImportJob importJob, IResourceAccessor resourceAccessor,
        ICollection<IMetadataExtractor> metadataExtractors, IMediaBrowsing mediaBrowsing,
        IImportResultHandler resultHandler, IMediaAccessor mediaAccessor)
    {
      ResourcePath currentFilePath = resourceAccessor.CanonicalLocalResourcePath;
      try
      {
        ImportResource(importJob, resourceAccessor, Guid.Empty, metadataExtractors, resultHandler, mediaAccessor);
      }
      catch (Exception e)
      {
        CheckSuspended(e); // Throw ImportAbortException if suspended - will skip warning and tagging job as erroneous
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker: Problem while importing resource '{0}'", e, currentFilePath);
        importJob.State = ImportJobState.Erroneous;
      }
    }

    protected Guid GetOrAddDirectory(ImportJob importJob, IFileSystemResourceAccessor directoryAccessor, Guid parentDirectoryId,
        IMediaBrowsing mediaBrowsing, IImportResultHandler resultHandler)
    {
      ResourcePath directoryPath = directoryAccessor.CanonicalLocalResourcePath;
      MediaItem directoryItem = mediaBrowsing.LoadLocalItem(directoryPath, EMPTY_MIA_ID_ENUMERATION, DIRECTORY_MIA_ID_ENUMERATION);
      if (directoryItem != null)
      {
        SingleMediaItemAspect da;
        if (!MediaItemAspect.TryGetAspect(directoryItem.Aspects, DirectoryAspect.Metadata, out da))
        { // This is the case if the path was formerly imported as a non-directory media item; we cannot reuse it
          resultHandler.DeleteMediaItem(directoryPath);
          directoryItem = null;
        }
      }
      if (directoryItem == null)
      { // Add directory item to ML
        MediaItemAspect mia = new SingleMediaItemAspect(MediaAspect.Metadata);
        mia.SetAttribute(MediaAspect.ATTR_TITLE, directoryAccessor.ResourceName);
        mia.SetAttribute(MediaAspect.ATTR_SORT_TITLE, directoryAccessor.ResourceName);
        mia.SetAttribute(MediaAspect.ATTR_ISVIRTUAL, false);
        mia.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, DateTime.MinValue);
        mia.SetAttribute(MediaAspect.ATTR_RATING, 0);
        mia.SetAttribute(MediaAspect.ATTR_COMMENT, null);
        mia.SetAttribute(MediaAspect.ATTR_LASTPLAYED, DateTime.MinValue);
        MediaItemAspect da = new SingleMediaItemAspect(DirectoryAspect.Metadata);
        IList<MediaItemAspect> aspects = new List<MediaItemAspect>(new[]
          {
              mia,
              da,
          });
        return resultHandler.UpdateMediaItem(parentDirectoryId, directoryPath, aspects, importJob.JobType == ImportJobType.Refresh, importJob.BasePath, CancellationToken.None);
      }
      return directoryItem.MediaItemId;
    }

    /// <summary>
    /// Imports or refreshes the directory with the specified <paramref name="directoryAccessor"/>. Sub directories will not
    /// be processed in this method.
    /// </summary>
    /// <param name="importJob">The import job being processed.</param>
    /// <param name="parentDirectoryId">Media item id of the parent directory, if present, else <see cref="Guid.Empty"/>.</param>
    /// <param name="directoryAccessor">Resource accessor for the directory to import.</param>
    /// <param name="metadataExtractors">Metadata extractors to apply on the resources.</param>
    /// <param name="mediaBrowsing">Callback interface to the media library for the refresh import type.</param>
    /// <param name="resultHandler">Callback to notify the import result.</param>
    /// <param name="mediaAccessor">Convenience reference to the media accessor.</param>
    /// <returns>Id of the directory's media item or <c>null</c>, if the given <paramref name="directoryAccessor"/>
    /// was imported as a media item or if an error occured. If <c>null</c> is returned, the directory import should be
    /// considered to be finished.</returns>
    protected Guid? ImportDirectory(ImportJob importJob, Guid parentDirectoryId, IFileSystemResourceAccessor directoryAccessor,
        ICollection<IMetadataExtractor> metadataExtractors, IMediaBrowsing mediaBrowsing,
        IImportResultHandler resultHandler, IMediaAccessor mediaAccessor)
    {
      ResourcePath currentDirectoryPath = directoryAccessor.CanonicalLocalResourcePath;
      try
      {
        ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportStatus, currentDirectoryPath);
        if (ImportResource(importJob, directoryAccessor, parentDirectoryId, metadataExtractors, resultHandler, mediaAccessor))
          // The directory could be imported as a media item.
          // If the directory itself was identified as a normal media item, don't import its children.
          // Necessary for DVD directories, for example.
          return null;
        Guid directoryId = GetOrAddDirectory(importJob, directoryAccessor, parentDirectoryId, mediaBrowsing, resultHandler);
        IDictionary<string, MediaItem> path2Item = new Dictionary<string, MediaItem>();
        if (importJob.JobType == ImportJobType.Refresh)
        {
          foreach (MediaItem mediaItem in mediaBrowsing.Browse(directoryId,
              IMPORTER_PROVIDER_MIA_ID_ENUMERATION, EMPTY_MIA_ID_ENUMERATION, null, false))
          {
            IList<MultipleMediaItemAspect> providerResourceAspects;
            if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, ProviderResourceAspect.Metadata, out providerResourceAspects))
              path2Item[providerResourceAspects[0].GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH)] = mediaItem;
          }
        }
        CheckImportStillRunning(importJob.State);
        ICollection<IFileSystemResourceAccessor> files = FileSystemResourceNavigator.GetFiles(directoryAccessor, false);
        if (files != null)
          foreach (IFileSystemResourceAccessor fileAccessor in files)
            using (fileAccessor)
            { // Add & update files
              ResourcePath currentFilePath = fileAccessor.CanonicalLocalResourcePath;
              string serializedFilePath = currentFilePath.Serialize();
              try
              {
                SingleMediaItemAspect importerAspect;
                MediaItem mediaItem;
                if (importJob.JobType == ImportJobType.Refresh &&
                    path2Item.TryGetValue(serializedFilePath, out mediaItem) &&
                    MediaItemAspect.TryGetAspect(mediaItem.Aspects, ImporterAspect.Metadata, out importerAspect) &&
                    importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_LAST_IMPORT_DATE) > fileAccessor.LastChanged)
                { // We can skip this file; it was imported after the last change time of the item
                  path2Item.Remove(serializedFilePath);
                  continue;
                }
                if (ImportResource(importJob, fileAccessor, directoryId, metadataExtractors, resultHandler, mediaAccessor))
                  path2Item.Remove(serializedFilePath);
              }
              catch (Exception e)
              {
                CheckSuspended(e); // Throw ImportAbortException if suspended - will skip warning and tagging job as erroneous
                ServiceRegistration.Get<ILogger>().Warn("ImporterWorker: Problem while importing resource '{0}'", e, serializedFilePath);
                importJob.State = ImportJobState.Erroneous;
              }
              CheckImportStillRunning(importJob.State);
            }
        if (importJob.JobType == ImportJobType.Refresh)
        { // Remove remaining (= non-present) files
          foreach (string pathStr in path2Item.Keys)
          {
            ResourcePath path = ResourcePath.Deserialize(pathStr);
            try
            {
              IResourceAccessor ra;
              if (!path.TryCreateLocalResourceAccessor(out ra))
                throw new IllegalCallException("Unable to access resource path '{0}'", importJob.BasePath);
              using (ra)
              {
                IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
                if (fsra == null || !fsra.IsFile)
                  // Don't touch directories because they will be imported in a different call of ImportDirectory
                  continue;
              }
            }
            catch (IllegalCallException)
            {
              // This happens if the resource doesn't exist any more - we also catch missing directories here
            }
            // Delete all remaining items
            resultHandler.DeleteMediaItem(path);
            CheckImportStillRunning(importJob.State);
          }
        }
        return directoryId;
      }
      catch (ImportSuspendedException)
      {
        throw;
      }
      catch (ImportAbortException)
      {
        throw;
      }
      catch (UnauthorizedAccessException e)
      {
        // If the access to the file or folder was denied, simply continue with the others
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker: Problem accessing resource '{0}', continueing with one", e, currentDirectoryPath);
      }
      catch (Exception e)
      {
        CheckSuspended(e); // Throw ImportAbortException if suspended - will skip warning and tagging job as erroneous
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker: Problem while importing directory '{0}'", e, currentDirectoryPath);
        importJob.State = ImportJobState.Erroneous;
      }
      return null;
    }

    /// <summary>
    /// Executes the given <paramref name="importJob"/>.
    /// </summary>
    /// <remarks>
    /// This method automatically terminates if it encounters that this importer worker was suspended or that the
    /// given <paramref name="importJob"/> was cancelled.
    /// </remarks>
    /// <param name="importJob">Import job to be executed. The state variables of this parameter will be updated by
    /// this method.</param>
    protected void Process(ImportJob importJob)
    {
      ImportJobState state = importJob.State;
      if (state == ImportJobState.Finished || state == ImportJobState.Cancelled || state == ImportJobState.Erroneous)
        return;

      // Preparation
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      IImportResultHandler resultHandler;
      IMediaBrowsing mediaBrowsing;
      lock (_syncObj)
      {
        resultHandler = _importResultHandler;
        mediaBrowsing = _mediaBrowsingCallback;
      }
      if (mediaBrowsing == null || resultHandler == null)
        // Can be the case if this importer worker was asynchronously suspended
        return;
      try
      {
        try
        {
          ICollection<IMetadataExtractor> metadataExtractors = new List<IMetadataExtractor>();
          foreach (Guid metadataExtractorId in importJob.MetadataExtractorIds)
          {
            IMetadataExtractor extractor;
            if (!mediaAccessor.LocalMetadataExtractors.TryGetValue(metadataExtractorId, out extractor))
              continue;
            metadataExtractors.Add(extractor);
          }

          // Prepare import
          if (state == ImportJobState.Scheduled)
          {
            ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Starting import job '{0}'", importJob);
            ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportStarted, importJob.BasePath);
            IResourceAccessor ra;
            if (!importJob.BasePath.TryCreateLocalResourceAccessor(out ra))
              throw new ArgumentException(string.Format("Unable to access resource path '{0}'", importJob.BasePath));
            using (ra)
            {
              IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
              if (fsra != null)
              { // Prepare complex import process
                importJob.PendingResources.Add(new PendingImportResource(Guid.Empty, (IFileSystemResourceAccessor) fsra.Clone()));
                importJob.State = ImportJobState.Active;
              }
              else
              { // Simple resource import
                ImportSingleFile(importJob, ra, metadataExtractors, mediaBrowsing, resultHandler, mediaAccessor);
                lock (importJob.SyncObj)
                  if (importJob.State == ImportJobState.Active)
                    importJob.State = ImportJobState.Finished;
                return;
              }
            }
          }
          else
            ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Resuming import job '{0}' ({1} items pending)", importJob, importJob.PendingResources.Count);

          // Actual import process
          while (importJob.HasPendingResources)
          {
            Thread.Sleep(0);
            CheckImportStillRunning(importJob.State);
            PendingImportResource pendingImportResource;
            lock (importJob.SyncObj)
              pendingImportResource = importJob.PendingResources.FirstOrDefault();
            if (pendingImportResource.IsValid)
            {
              IFileSystemResourceAccessor fsra = pendingImportResource.ResourceAccessor;
              int numPending = importJob.PendingResources.Count;
              string moreResources = numPending > 1 ? string.Format(" ({0} more resources pending)", numPending) : string.Empty;
              ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Importing '{0}'{1}", fsra.ResourcePathName, moreResources);
              if (fsra.IsFile && fsra.Exists)
                ImportResource(importJob, fsra, pendingImportResource.ParentDirectory, metadataExtractors, resultHandler, mediaAccessor);
              else if (!fsra.IsFile)
              {
                CheckImportStillRunning(importJob.State);
                Guid? currentDirectoryId = ImportDirectory(importJob, pendingImportResource.ParentDirectory, fsra, metadataExtractors,
                    mediaBrowsing, resultHandler, mediaAccessor);
                CheckImportStillRunning(importJob.State);
                if (currentDirectoryId.HasValue && importJob.IncludeSubDirectories)
                  // Add subdirectories in front of work queue
                  lock (importJob.SyncObj)
                  {
                    ICollection<IFileSystemResourceAccessor> directories = FileSystemResourceNavigator.GetChildDirectories(fsra, false);
                    if (directories != null)
                      foreach (IFileSystemResourceAccessor childDirectory in directories)
                        importJob.PendingResources.Insert(0, new PendingImportResource(currentDirectoryId.Value, childDirectory));
                  }
              }
              else
                ServiceRegistration.Get<ILogger>().Warn("ImporterWorker: Cannot import resource '{0}': It's neither a file nor a directory", fsra.CanonicalLocalResourcePath.Serialize());
            }
            lock (importJob.SyncObj)
              importJob.PendingResources.Remove(pendingImportResource);
            pendingImportResource.Dispose();
          }
          lock (importJob.SyncObj)
            if (importJob.State == ImportJobState.Active)
              importJob.State = ImportJobState.Finished;
          ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Finished import job '{0}'", importJob);
          ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportCompleted, importJob.BasePath);
        }
        catch (Exception e)
        {
          CheckSuspended(e); // Throw ImportAbortException if suspended - will skip warning and tagging job as erroneous
          ServiceRegistration.Get<ILogger>().Warn("ImporterWorker: Problem processing '{0}'", e, importJob);
          ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportCompleted, importJob.BasePath);
          importJob.State = ImportJobState.Erroneous;
        }
      }
      catch (ImportSuspendedException)
      {
        ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Suspending import job '{0}' ({1} items pending - will be continued next time)", importJob, importJob.PendingResources.Count);
      }
      catch (ImportAbortException)
      {
        ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Aborting import job '{0}' ({1} items pending)", importJob, importJob.PendingResources.Count);
      }
    }
    #region IImporterWorker implementation

    public bool IsSuspended
    {
      get { return _suspendedEvent.WaitOne(0); }
      internal set
      {
        lock (_syncObj)
        {
          if (value)
          {
            _suspendedEvent.Set();
            _mediaBrowsingCallback = null;
            _importResultHandler = null;
          }
          else
            _suspendedEvent.Reset();
        }
      }
    }

    public ICollection<ImportJobInformation> ImportJobs
    {
      get
      {
        lock (_syncObj)
          return _importJobs.Select(job => new ImportJobInformation(job.JobInformation)).ToList();
      }
    }

    public void Startup()
    {
      IsSuspended = true;
      _messageQueue.Start();
      LoadPendingImportJobs();
      ScheduleImports();
      Initialize();
      // Don't start importer loop here - will be started when Activate is called
    }

    public void Shutdown()
    {
      ShutdownImporterLoop();
      _messageQueue.Shutdown();
      _settings.Dispose();
      PersistPendingImportJobs();
    }

    public void Activate(IMediaBrowsing mediaBrowsingCallback, IImportResultHandler importResultHandler)
    {
      lock (_syncObj)
      {
        _mediaBrowsingCallback = mediaBrowsingCallback;
        _importResultHandler = importResultHandler;
      }
      StartImporterLoop_NoLock();
    }

    public void Suspend()
    {
      IsSuspended = true;
    }

    public void CancelPendingJobs()
    {
      lock (_syncObj)
      {
        foreach (ImportJob importJob in _importJobs)
          importJob.Cancel();
        _importJobs.Clear();
      }
    }

    public void CancelJobsForPath(ResourcePath path)
    {
      ICollection<ImportJob> importJobs;
      lock (_syncObj)
        importJobs = new List<ImportJob>(_importJobs);
      ICollection<ImportJob> cancelImportJobs = new List<ImportJob>();
      foreach (ImportJob job in importJobs)
      {
        if (path.IsSameOrParentOf(job.BasePath))
        {
          job.Cancel();
          cancelImportJobs.Add(job);
        }
      }
      lock (_syncObj)
        foreach (ImportJob job in cancelImportJobs)
          if (_importJobs.Remove(job))
            ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportScheduleCanceled, path);
    }

    public void ScheduleImport(ResourcePath path, IEnumerable<string> mediaCategories, bool includeSubDirectories)
    {
      ICollection<ImportJob> importJobs;
      lock (_syncObj)
        importJobs = new List<ImportJob>(_importJobs);
      if (importJobs.Any(checkJob => checkJob.JobType == ImportJobType.Import && checkJob.BasePath.IsSameOrParentOf(path)))
        // Path is already being scheduled as Import job
        // => the new job is already included in an already existing job
        return;
      CancelJobsForPath(path);
      ICollection<Guid> metadataExtractorIds = GetMetadataExtractorIdsForMediaCategories(mediaCategories);
      ImportJob job = new ImportJob(ImportJobType.Import, path, metadataExtractorIds, includeSubDirectories);
      EnqueueImportJob(job);
      ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportScheduled, path, ImportJobType.Import);
    }

    public void ScheduleRefresh(ResourcePath path, IEnumerable<string> mediaCategories, bool includeSubDirectories)
    {
      ICollection<ImportJob> importJobs;
      lock (_syncObj)
        importJobs = new List<ImportJob>(_importJobs);
      ICollection<ImportJob> removeImportJobs = new List<ImportJob>();
      foreach (ImportJob checkJob in importJobs)
      {
        if (checkJob.BasePath.IsSameOrParentOf(path))
        {
          // The new job is included in an already existing job - re-schedule existing job
          path = checkJob.BasePath;
          checkJob.Cancel();
          removeImportJobs.Add(checkJob);
        }
        if (path.IsParentOf(checkJob.BasePath))
        { // The new job will include the old job
          checkJob.Cancel();
          removeImportJobs.Add(checkJob);
        }
      }
      lock (_syncObj)
        foreach (ImportJob removeJob in removeImportJobs)
          _importJobs.Remove(removeJob);
      ICollection<Guid> metadataExtractorIds = GetMetadataExtractorIdsForMediaCategories(mediaCategories);
      ImportJob job = new ImportJob(ImportJobType.Refresh, path, metadataExtractorIds, includeSubDirectories);
      EnqueueImportJob(job);
      ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportScheduled, path, ImportJobType.Refresh);
    }

    #endregion
  }
}
