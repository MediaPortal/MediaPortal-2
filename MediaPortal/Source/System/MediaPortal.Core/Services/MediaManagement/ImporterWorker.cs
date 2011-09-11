#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Runtime;
using MediaPortal.Core.Settings;
using MediaPortal.Core.SystemResolver;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Core.Services.MediaManagement
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

  // TODO: Schedule regular reimports for all local shares
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
    protected static IEnumerable<Guid> EMPTY_MIA_ID_ENUMERATION = new Guid[] {};

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

    public ImporterWorker()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
          {
            SystemMessaging.CHANNEL
          });
      _messageQueue.MessageReceived += OnMessageReceived;
      // Message queue will be started in method Start()
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
        SystemMessaging.MessageType messageType =
            (SystemMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case SystemMessaging.MessageType.SystemStateChanged:
            SystemState newState = (SystemState) message.MessageData[SystemMessaging.NEW_STATE];
            if (newState == SystemState.ShuttingDown)
              IsSuspended = true;
            break;
        }
      }
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
        ImporterWorkerSettings settings = settingsManager.Load<ImporterWorkerSettings>();
        settings.PendingImportJobs = new List<ImportJob>(_importJobs);
        settingsManager.Save(settings);
        _importJobs.Clear();
      }
    }

    protected void LoadPendingImportJobs()
    {
      lock (_syncObj)
      {
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
        ImporterWorkerSettings settings = settingsManager.Load<ImporterWorkerSettings>();
        _importJobs.Clear();
        CollectionUtils.AddAll(_importJobs, settings.PendingImportJobs);
        _importJobsReadyAvailableEvent.Set();
      }
    }

    protected void CheckSuspended()
    {
      if (IsSuspended)
        throw new ImportSuspendedException();
    }

    protected void CheckImportStillRunning(ImportJobState state)
    {
      CheckSuspended();
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
          WaitHandle.WaitAny(new WaitHandle[] {_importJobsReadyAvailableEvent, _suspendedEvent});
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
        _workerThread = new Thread(ImporterLoop) {Name = "Importer", Priority = ThreadPriority.BelowNormal};
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
    /// <param name="mediaItemAspectTypes">Media item aspect types which are expected to be filled. All of those
    /// media item aspects will be present in the result, but not all of their values might be set if no metadata extractor
    /// filled them.</param>
    /// <param name="resultHandler">Callback to notify the import results.</param>
    /// <param name="mediaAccessor">Convenience reference to the media accessor.</param>
    /// <returns><c>true</c>, if metadata could be extracted from the given <paramref name="mediaItemAccessor"/>, else
    /// <c>false</c>.</returns>
    protected bool ImportResource(IResourceAccessor mediaItemAccessor, Guid parentDirectoryId,
        ICollection<IMetadataExtractor> metadataExtractors, ICollection<MediaItemAspectMetadata> mediaItemAspectTypes,
        IImportResultHandler resultHandler, IMediaAccessor mediaAccessor)
    {
      const bool forceQuickMode = false; // Allow extractions with probably longer runtime.
      ResourcePath path = mediaItemAccessor.LocalResourcePath;
      ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportStatus, path);
      IDictionary<Guid, MediaItemAspect> aspects = mediaAccessor.ExtractMetadata(mediaItemAccessor, metadataExtractors, forceQuickMode);
      if (aspects == null)
        // No metadata could be extracted
        return false;
      // Fill empty entries for media item aspects which aren't returned - this will cleanup those aspects in media library
      foreach (MediaItemAspectMetadata mediaItemAspectType in mediaItemAspectTypes)
      {
        if (!aspects.ContainsKey(mediaItemAspectType.AspectId))
          aspects[mediaItemAspectType.AspectId] = new MediaItemAspect(mediaItemAspectType);
      }
      resultHandler.UpdateMediaItem(parentDirectoryId, path, aspects.Values);
      resultHandler.DeleteUnderPath(path);
      return true;
    }

    /// <summary>
    /// Imports or refreshes a single file without a parent directory with the specified <paramref name="fileAccessor"/>.
    /// </summary>
    /// <param name="importJob">The import job being processed.</param>
    /// <param name="fileAccessor">Resource accessor for the file to import.</param>
    /// <param name="metadataExtractors">Metadata extractors to apply on the resource.</param>
    /// <param name="mediaItemAspectTypes">Types of the media item aspects which are expected to be filled.</param>
    /// <param name="mediaBrowsing">Callback interface to the media library for the refresh import type.</param>
    /// <param name="resultHandler">Callback to notify the import result.</param>
    /// <param name="mediaAccessor">Convenience reference to the media accessor.</param>
    protected void ImportSingleFile(ImportJob importJob, IResourceAccessor fileAccessor,
        ICollection<IMetadataExtractor> metadataExtractors, ICollection<MediaItemAspectMetadata> mediaItemAspectTypes,
        IMediaBrowsing mediaBrowsing, IImportResultHandler resultHandler, IMediaAccessor mediaAccessor)
    {
      ResourcePath currentFilePath = fileAccessor.LocalResourcePath;
      try
      {
        if (importJob.JobType == ImportJobType.Refresh)
        {
          MediaItem mediaItem = mediaBrowsing.LoadItem(currentFilePath,
              IMPORTER_MIA_ID_ENUMERATION, EMPTY_MIA_ID_ENUMERATION);
          MediaItemAspect importerAspect;
          if (mediaItem != null && mediaItem.Aspects.TryGetValue(ImporterAspect.ASPECT_ID, out importerAspect) &&
              (DateTime) importerAspect[ImporterAspect.ATTR_LAST_IMPORT_DATE] > fileAccessor.LastChanged)
            return;
        }
        ImportResource(fileAccessor, Guid.Empty, metadataExtractors, mediaItemAspectTypes, resultHandler, mediaAccessor);
      }
      catch (Exception e)
      {
        CheckSuspended(); // Throw ImportAbortException if suspended - will skip warning and tagging job as erroneous
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker: Problem while importing resource '{0}'", e, currentFilePath);
        importJob.State = ImportJobState.Erroneous;
      }
    }

    protected Guid GetOrAddDirectory(IFileSystemResourceAccessor directoryAccessor, Guid parentDirectoryId,
        IMediaBrowsing mediaBrowsing, IImportResultHandler resultHandler)
    {
      ResourcePath directoryPath = directoryAccessor.LocalResourcePath;
      MediaItem directoryItem = mediaBrowsing.LoadItem(directoryPath, EMPTY_MIA_ID_ENUMERATION, DIRECTORY_MIA_ID_ENUMERATION);
      if (directoryItem != null)
      {
        MediaItemAspect da;
        if (!directoryItem.Aspects.TryGetValue(DirectoryAspect.ASPECT_ID, out da))
        { // This is the case if the path was formerly imported as a non-directory media item; we cannot reuse it
          resultHandler.DeleteMediaItem(directoryPath);
          directoryItem = null;
        }
      }
      if (directoryItem == null)
      { // Add directory item to ML
        MediaItemAspect mia = new MediaItemAspect(MediaAspect.Metadata);
        mia.SetAttribute(MediaAspect.ATTR_TITLE, directoryAccessor.ResourceName);
        mia.SetAttribute(MediaAspect.ATTR_MIME_TYPE, null);
        mia.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, DateTime.MinValue);
        mia.SetAttribute(MediaAspect.ATTR_RATING, 0);
        mia.SetAttribute(MediaAspect.ATTR_COMMENT, null);
        mia.SetAttribute(MediaAspect.ATTR_LASTPLAYED, DateTime.MinValue);
        MediaItemAspect da = new MediaItemAspect(DirectoryAspect.Metadata);
        IList<MediaItemAspect> aspects = new List<MediaItemAspect>(new[]
          {
              mia,
              da,
          });
        return resultHandler.UpdateMediaItem(parentDirectoryId, directoryPath, aspects);
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
    /// <param name="mediaItemAspectTypes">Types of the media item aspects which are expected to be filled.</param>
    /// <param name="mediaBrowsing">Callback interface to the media library for the refresh import type.</param>
    /// <param name="resultHandler">Callback to notify the import result.</param>
    /// <param name="mediaAccessor">Convenience reference to the media accessor.</param>
    /// <returns>Id of the directory's media item or <c>null</c>, if the given <paramref name="directoryAccessor"/>
    /// was imported as a media item or if an error occured. If <c>null</c> is returned, the directory import should be
    /// considered to be finished.</returns>
    protected Guid? ImportDirectory(ImportJob importJob, Guid parentDirectoryId, IFileSystemResourceAccessor directoryAccessor,
        ICollection<IMetadataExtractor> metadataExtractors, ICollection<MediaItemAspectMetadata> mediaItemAspectTypes,
        IMediaBrowsing mediaBrowsing, IImportResultHandler resultHandler, IMediaAccessor mediaAccessor)
    {
      ResourcePath currentDirectoryPath = directoryAccessor.LocalResourcePath;
      try
      {
        ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportStatus, currentDirectoryPath);
        if (ImportResource(directoryAccessor, parentDirectoryId, metadataExtractors, mediaItemAspectTypes,
            resultHandler, mediaAccessor))
          // The directory could be imported as a media item
          return null;
        Guid directoryId = GetOrAddDirectory(directoryAccessor, parentDirectoryId, mediaBrowsing, resultHandler);
        IDictionary<string, MediaItem> path2Item = new Dictionary<string, MediaItem>();
        if (importJob.JobType == ImportJobType.Refresh)
        {
          foreach (MediaItem mediaItem in mediaBrowsing.Browse(directoryId,
              IMPORTER_PROVIDER_MIA_ID_ENUMERATION, EMPTY_MIA_ID_ENUMERATION))
          {
            MediaItemAspect providerResourceAspect;
            if (mediaItem.Aspects.TryGetValue(ProviderResourceAspect.ASPECT_ID, out providerResourceAspect))
              path2Item[providerResourceAspect.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH)] = mediaItem;
          }
        }
        CheckImportStillRunning(importJob.State);
        // If the directory itself was identified as a normal media item, don't import its children.
        // Necessary for DVD directories, for example.
        { 
          ICollection<IFileSystemResourceAccessor> files = FileSystemResourceNavigator.GetFiles(directoryAccessor);
          if (files != null)
            foreach (IFileSystemResourceAccessor fileAccessor in files)
            { // Add & update files
              ResourcePath currentFilePath = fileAccessor.LocalResourcePath;
              string serializedFilePath = currentFilePath.Serialize();
              try
              {
                MediaItemAspect importerAspect;
                MediaItem mediaItem;
                if (importJob.JobType == ImportJobType.Refresh &&
                    path2Item.TryGetValue(serializedFilePath, out mediaItem) &&
                    mediaItem.Aspects.TryGetValue(ImporterAspect.ASPECT_ID, out importerAspect) &&
                    importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_LAST_IMPORT_DATE) > fileAccessor.LastChanged)
                { // We can skip this file; it was imported after the last change time of the item
                  path2Item.Remove(serializedFilePath);
                  continue;
                }
                if (ImportResource(fileAccessor, directoryId, metadataExtractors, mediaItemAspectTypes,
                    resultHandler, mediaAccessor))
                  path2Item.Remove(serializedFilePath);
              }
              catch (Exception e)
              {
                CheckSuspended(); // Throw ImportAbortException if suspended - will skip warning and tagging job as erroneous
                ServiceRegistration.Get<ILogger>().Warn("ImporterWorker: Problem while importing resource '{0}'", e, serializedFilePath);
                importJob.State = ImportJobState.Erroneous;
              }
              CheckImportStillRunning(importJob.State);
            }
        }
        if (importJob.JobType == ImportJobType.Refresh)
        { // Remove remaining (= non-present) files
          foreach (string pathStr in path2Item.Keys)
          {
            ResourcePath path = ResourcePath.Deserialize(pathStr);
            try
            {
              IResourceAccessor ra = path.CreateLocalResourceAccessor();
              if (!ra.IsFile)
                // Don't touch directories because they will be imported in a different call of ImportDirectory
                continue;
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
      catch (Exception e)
      {
        CheckSuspended(); // Throw ImportAbortException if suspended - will skip warning and tagging job as erroneous
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
          ICollection<MediaItemAspectMetadata> mediaItemAspectTypes = new HashSet<MediaItemAspectMetadata>();
          ICollection<IMetadataExtractor> metadataExtractors = new List<IMetadataExtractor>();
          foreach (Guid metadataExtractorId in importJob.MetadataExtractorIds)
          {
            IMetadataExtractor extractor;
            if (!mediaAccessor.LocalMetadataExtractors.TryGetValue(metadataExtractorId, out extractor))
              continue;
            metadataExtractors.Add(extractor);
            CollectionUtils.AddAll(mediaItemAspectTypes, extractor.Metadata.ExtractedAspectTypes.Values);
          }

          // Prepare import
          if (state == ImportJobState.Scheduled)
          {
            ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Starting import job '{0}'", importJob);
            ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportStarted, importJob.BasePath);
            IResourceAccessor accessor = importJob.BasePath.CreateLocalResourceAccessor();
            IFileSystemResourceAccessor fsra = accessor as IFileSystemResourceAccessor;
            if (fsra != null)
            { // Prepare complex import process
              importJob.PendingResources.Add(new PendingImportResource(Guid.Empty, fsra));
              importJob.State = ImportJobState.Started;
            }
            else
            { // Simple single-item-import
              ImportSingleFile(importJob, accessor, metadataExtractors, mediaItemAspectTypes,
                  mediaBrowsing, resultHandler, mediaAccessor);
              lock (importJob.SyncObj)
                if (importJob.State == ImportJobState.Started)
                  importJob.State = ImportJobState.Finished;
              return;
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
                ImportResource(fsra, pendingImportResource.ParentDirectory, metadataExtractors, mediaItemAspectTypes, resultHandler, mediaAccessor);
              else if (fsra.IsDirectory)
              {
                CheckImportStillRunning(importJob.State);
                Guid? currentDirectoryId = ImportDirectory(importJob, pendingImportResource.ParentDirectory, fsra, metadataExtractors, mediaItemAspectTypes,
                    mediaBrowsing, resultHandler, mediaAccessor);
                CheckImportStillRunning(importJob.State);
                if (currentDirectoryId.HasValue && importJob.IncludeSubDirectories)
                  // Add subdirectories in front of work queue
                  lock (importJob.SyncObj)
                    foreach (IFileSystemResourceAccessor childDirectory in FileSystemResourceNavigator.GetChildDirectories(fsra))
                      importJob.PendingResources.Insert(0, new PendingImportResource(currentDirectoryId.Value, childDirectory));
              }
              else
                ServiceRegistration.Get<ILogger>().Warn("ImporterWorker: Cannot import resource '{0}': It's neither a file nor a directory", fsra.LocalResourcePath.Serialize());
            }
            lock (importJob.SyncObj)
              importJob.PendingResources.Remove(pendingImportResource);
          }
          lock (importJob.SyncObj)
            if (importJob.State == ImportJobState.Started)
              importJob.State = ImportJobState.Finished;
          ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Finished import job '{0}'", importJob);
            ImporterWorkerMessaging.SendImportMessage(ImporterWorkerMessaging.MessageType.ImportCompleted, importJob.BasePath);
          return;
        }
        catch (Exception e)
        {
          CheckSuspended(); // Throw ImportAbortException if suspended - will skip warning and tagging job as erroneous
          ServiceRegistration.Get<ILogger>().Warn("ImporterWorker: Problem processing '{0}'", e, importJob);
          importJob.State = ImportJobState.Erroneous;
        }
      }
      catch (ImportSuspendedException)
      {
        ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Suspending import job '{0}' ({1} items pending - will be continued next time)", importJob, importJob.PendingResources.Count);
        return;
      }
      catch (ImportAbortException)
      {
        ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Aborting import job '{0}' ({1} items pending)", importJob, importJob.PendingResources.Count);
        return;
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

    public void Startup()
    {
      IsSuspended = true;
      _messageQueue.Start();
      LoadPendingImportJobs();
      Initialize();
      // Don't start importer loop here - will be started when Activate is called
    }

    public void Shutdown()
    {
      ShutdownImporterLoop();
      _messageQueue.Shutdown();
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
          _importJobs.Remove(job);
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
    }

    #endregion
  }
}
