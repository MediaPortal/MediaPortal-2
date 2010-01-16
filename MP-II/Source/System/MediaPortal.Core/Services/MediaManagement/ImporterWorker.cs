#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Runtime;
using MediaPortal.Core.Settings;
using MediaPortal.Utilities;

namespace MediaPortal.Core.Services.MediaManagement
{
  internal class ImportAbortException : ApplicationException
  {
  }

  public class ImporterWorker : IImporterWorker
  {
    protected static IEnumerable<Guid> IMPORTER_MIA_ID_ENUMERATION = new Guid[]
        {
          ImporterAspect.ASPECT_ID
        };
    protected static IEnumerable<Guid> IMPORTER_PROVIDER_MIA_ID_ENUMERATION = new Guid[]
        {
          ProviderResourceAspect.ASPECT_ID,
          ImporterAspect.ASPECT_ID
        };
    protected static IEnumerable<Guid> EMPTY_MIA_ID_ENUMERATION = new Guid[] {};

    protected AsynchronousMessageQueue _messageQueue;
    protected object _syncObj = new object();
    protected Thread _workerThread = null;
    protected IMediaBrowsing _mediaBrowsingCallback = null;
    protected IImportResultHandler _importResultHandler = null;
    protected List<ImportJob> _importJobs = new List<ImportJob>(); // We need more flexibility than the default Queue implementation provides, so we just use a List
    protected bool _isSuspended = true;

    public ImporterWorker()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
          {
            SystemMessaging.CHANNEL
          });
      _messageQueue.MessageReceived += OnMessageReceived;
      // Message queue will be started in method Start()
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
            SystemState newState = (SystemState) message.MessageData[SystemMessaging.PARAM];
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

    protected ICollection<Guid> GetMetadataExtractorIdsForMediaCategories(ICollection<string> mediaCategories)
    {
      IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
      ICollection<Guid> result = new HashSet<Guid>();
      foreach (string mediaCategory in mediaCategories)
        CollectionUtils.AddAll(result, mediaAccessor.GetMetadataExtractorsForCategory(mediaCategory));
      return result;
    }

    protected void PersistPendingImportJobs()
    {
      lock (_syncObj)
      {
        ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
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
        ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
        ImporterWorkerSettings settings = settingsManager.Load<ImporterWorkerSettings>();
        _importJobs.Clear();
        CollectionUtils.AddAll(_importJobs, settings.PendingImportJobs);
        Monitor.PulseAll(_syncObj);
      }
    }

    protected void CheckSuspended()
    {
      if (IsSuspended)
        throw new ImportAbortException();
    }

    protected void CheckImportStillRunning(ImportJobState state)
    {
      if (IsSuspended || state == ImportJobState.Cancelled || state == ImportJobState.Erroneous)
        throw new ImportAbortException();
    }

    protected void EnqueueImportJob(ImportJob job)
    {
      lock (_syncObj)
      {
        job.State = ImportJobState.Scheduled;
        _importJobs.Insert(0, job);
        Monitor.PulseAll(_syncObj);
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
        // 2) No job available - sleep until a job is available
        // 3) Job cannot be processed - exit loop and suspend
        ImportJob job = PeekImportJob();
        if (job != null)
        {
          Process(job);
          ImportJobState state = job.State;
          if (state == ImportJobState.Finished || state == ImportJobState.Erroneous || state == ImportJobState.Cancelled)
            RemoveImportJob(job); // Maybe the queue was changed (cleared & filled again), so a simple call to Dequeue() could dequeue the wrong job
        }
        lock (_syncObj)
        {
          if (IsSuspended)
            // We have to check this in the synchronized block, else we could miss the PulseAll event
            break;
          // We need to check this in a synchronized block. If we wouldn't prevent other threads from
          // enqueuing data in this moment, we could miss the PulseAll event
          else if (!IsImportJobAvailable)
            Monitor.Wait(_syncObj);
        }
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
        _workerThread = new Thread(ImporterLoop);
        _workerThread.Start();
        IsSuspended = false;
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
    /// <param name="mediaItemAccessor">File resource to be imported.</param>
    /// <param name="metadataExtractors">Collection of metadata extractors to apply to the given resoure.</param>
    /// <param name="mediaItemAspectTypes">Media item aspect types which are expected to be filled. All of those
    /// media item aspects will be present in the result, but not all of their values might be set if no metadata extractor
    /// filled them.</param>
    /// <param name="resultHandler">Callback to notify the import results.</param>
    /// <param name="mediaAccessor">Convenience reference to the media accessor.</param>
    protected void ImportResource(IResourceAccessor mediaItemAccessor, ICollection<IMetadataExtractor> metadataExtractors,
        ICollection<MediaItemAspectMetadata> mediaItemAspectTypes, IImportResultHandler resultHandler,
        IMediaAccessor mediaAccessor)
    {
      ResourcePath path = mediaItemAccessor.LocalResourcePath;
      ImporterWorkerMessaging.SendImportStatusMessage(path);
      IDictionary<Guid, MediaItemAspect> aspects = mediaAccessor.ExtractMetadata(mediaItemAccessor, metadataExtractors);
      if (aspects == null)
        // No metadata could be extracted
        return;
      // Fill empty entries for media item aspects which aren't returned - this will cleanup those aspects in media library
      foreach (MediaItemAspectMetadata mediaItemAspectType in mediaItemAspectTypes)
      {
        if (!aspects.ContainsKey(mediaItemAspectType.AspectId))
          aspects[mediaItemAspectType.AspectId] = new MediaItemAspect(mediaItemAspectType);
      }
      resultHandler.UpdateMediaItem(path, aspects.Values);
    }

    /// <summary>
    /// Imports or refreshes the file with the specified <paramref name="fileAccessor"/>.
    /// </summary>
    /// <param name="importJob">The import job being processed.</param>
    /// <param name="fileAccessor">Resource accessor for the file to import.</param>
    /// <param name="metadataExtractors">Metadata extractors to apply on the resource.</param>
    /// <param name="mediaItemAspectTypes">Types of the media item aspects which are expected to be filled.</param>
    /// <param name="mediaBrowsing">Callback interface to the media library for the refresh import type.</param>
    /// <param name="resultHandler">Callback to notify the import result.</param>
    /// <param name="mediaAccessor">Convenience reference to the media accessor.</param>
    protected void ImportFile(ImportJob importJob, IResourceAccessor fileAccessor,
        ICollection<IMetadataExtractor> metadataExtractors, ICollection<MediaItemAspectMetadata> mediaItemAspectTypes,
        IMediaBrowsing mediaBrowsing, IImportResultHandler resultHandler, IMediaAccessor mediaAccessor)
    {
      try
      {
        if (importJob.JobType == ImportJobType.Refresh)
        {
          MediaItem mediaItem = mediaBrowsing.Browse(fileAccessor.LocalResourcePath,
              IMPORTER_MIA_ID_ENUMERATION, EMPTY_MIA_ID_ENUMERATION).FirstOrDefault();
          MediaItemAspect importerAspect;
          if (mediaItem != null && mediaItem.Aspects.TryGetValue(ImporterAspect.ASPECT_ID, out importerAspect) &&
              (DateTime) importerAspect[ImporterAspect.ATTR_LAST_IMPORT_DATE] > fileAccessor.LastChanged)
            return;
        }
        ImportResource(fileAccessor, metadataExtractors, mediaItemAspectTypes, resultHandler, mediaAccessor);
      }
      catch (Exception e)
      {
        CheckSuspended(); // Throw ImportAbortException if suspended - will skip warning and tagging job as erroneous
        ServiceScope.Get<ILogger>().Warn("ImporterWorker: Problem while importing resource '{0}'", e, fileAccessor.LocalResourcePath);
        importJob.State = ImportJobState.Erroneous;
      }
    }

    /// <summary>
    /// Imports or refreshes the directory with the specified <paramref name="directoryAccessor"/>. Sub directories will not
    /// be processed in this method.
    /// </summary>
    /// <param name="importJob">The import job being processed.</param>
    /// <param name="directoryAccessor">Resource accessor for the directory to import.</param>
    /// <param name="metadataExtractors">Metadata extractors to apply on the resources.</param>
    /// <param name="mediaItemAspectTypes">Types of the media item aspects which are expected to be filled.</param>
    /// <param name="mediaBrowsing">Callback interface to the media library for the refresh import type.</param>
    /// <param name="resultHandler">Callback to notify the import result.</param>
    /// <param name="mediaAccessor">Convenience reference to the media accessor.</param>
    protected void ImportDirectory(ImportJob importJob, IFileSystemResourceAccessor directoryAccessor,
        ICollection<IMetadataExtractor> metadataExtractors, ICollection<MediaItemAspectMetadata> mediaItemAspectTypes,
        IMediaBrowsing mediaBrowsing, IImportResultHandler resultHandler, IMediaAccessor mediaAccessor)
    {
      try
      {
        ImporterWorkerMessaging.SendImportStatusMessage(directoryAccessor.LocalResourcePath);
        IDictionary<string, MediaItem> path2Item = new Dictionary<string, MediaItem>();
        if (importJob.JobType == ImportJobType.Refresh)
        {
          foreach (MediaItem mediaItem in mediaBrowsing.Browse(directoryAccessor.LocalResourcePath,
              IMPORTER_PROVIDER_MIA_ID_ENUMERATION, EMPTY_MIA_ID_ENUMERATION))
          {
            MediaItemAspect providerResourceAspect;
            if (mediaItem.Aspects.TryGetValue(ProviderResourceAspect.ASPECT_ID, out providerResourceAspect))
              path2Item[providerResourceAspect.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH)] = mediaItem;
          }
        }
        CheckImportStillRunning(importJob.State);
        foreach (IFileSystemResourceAccessor fileAccessor in FileSystemResourceNavigator.GetFiles(directoryAccessor))
        { // Add & update files
          try
          {
            MediaItemAspect importerAspect;
            MediaItem mediaItem;
            if (importJob.JobType == ImportJobType.Refresh &&
                path2Item.TryGetValue(fileAccessor.LocalResourcePath.Serialize(), out mediaItem) &&
                mediaItem.Aspects.TryGetValue(ImporterAspect.ASPECT_ID, out importerAspect) &&
                importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_LAST_IMPORT_DATE) > fileAccessor.LastChanged)
              // We can skip this file; it was imported after the last change time of the item
              continue;
            ImportResource(fileAccessor, metadataExtractors, mediaItemAspectTypes, resultHandler, mediaAccessor);
          }
          catch (Exception e)
          {
            CheckSuspended(); // Throw ImportAbortException if suspended - will skip warning and tagging job as erroneous
            ServiceScope.Get<ILogger>().Warn("ImporterWorker: Problem while importing resource '{0}'", e, fileAccessor.LocalResourcePath);
            importJob.State = ImportJobState.Erroneous;
          }
          CheckImportStillRunning(importJob.State);
        }
        if (importJob.JobType == ImportJobType.Refresh)
        { // Remove non-present files
          foreach (string pathStr in path2Item.Keys)
          {
            ResourcePath path = ResourcePath.Deserialize(pathStr);
            if (!directoryAccessor.Exists(path.LastPathSegment.Path))
              resultHandler.DeleteMediaItem(path);
            CheckImportStillRunning(importJob.State);
          }
        }
      }
      catch (ImportAbortException)
      {
        throw;
      }
      catch (Exception e)
      {
        CheckSuspended(); // Throw ImportAbortException if suspended - will skip warning and tagging job as erroneous
        ServiceScope.Get<ILogger>().Warn("ImporterWorker: Problem while importing directory '{0}'", e, directoryAccessor.LocalResourcePath);
        importJob.State = ImportJobState.Erroneous;
      }
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
      IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
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

        ServiceScope.Get<ILogger>().Info("ImporterWorker: Processing import job '{0}'", importJob.BasePath);
        // Prepare import
        if (state == ImportJobState.Scheduled)
        {
          IResourceAccessor accessor = importJob.BasePath.CreateLocalMediaItemAccessor();
          if (accessor is IFileSystemResourceAccessor)
          { // Prepare complex import process
            importJob.PendingResources.Add((IFileSystemResourceAccessor) accessor);
            importJob.State = ImportJobState.Started;
          }
          else
          { // Simple single-item-import
            ImportFile(importJob, accessor, metadataExtractors, mediaItemAspectTypes,
                mediaBrowsing, resultHandler, mediaAccessor);
            lock (importJob.SyncObj)
              if (importJob.State == ImportJobState.Started)
                importJob.State = ImportJobState.Finished;
            return;
          }
        }

        // Actual import process
        while (importJob.HasPendingResources)
        {
          CheckImportStillRunning(importJob.State);
          IFileSystemResourceAccessor fsra;
          lock (importJob.SyncObj)
            fsra = importJob.PendingResources.FirstOrDefault();
          ServiceScope.Get<ILogger>().Info("ImporterWorker: Processing resource '{0}'", fsra.ResourcePathName);
          if (fsra.IsFile)
            ImportFile(importJob, fsra, metadataExtractors, mediaItemAspectTypes, mediaBrowsing, resultHandler, mediaAccessor);
          else if (fsra.IsDirectory)
          {
            CheckImportStillRunning(importJob.State);
            ImportDirectory(importJob, fsra, metadataExtractors, mediaItemAspectTypes,
                mediaBrowsing, resultHandler, mediaAccessor);
            CheckImportStillRunning(importJob.State);
            if (importJob.IncludeSubDirectories)
              // Enqueue subdirectories to work queue
              lock (importJob.SyncObj)
                foreach (IFileSystemResourceAccessor childDirectory in FileSystemResourceNavigator.GetChildDirectories(fsra))
                  importJob.PendingResources.Add(childDirectory);
          }
          else
            ServiceScope.Get<ILogger>().Warn("ImporterWorker: Cannot import resource '{0}': It's neither a file nor a directory", fsra.LocalResourcePath.Serialize());
          lock (importJob.SyncObj)
            importJob.PendingResources.Remove(fsra);
        }
        lock (importJob.SyncObj)
          if (importJob.State == ImportJobState.Started)
            importJob.State = ImportJobState.Finished;
        return;
      }
      catch (ImportAbortException)
      {
        ServiceScope.Get<ILogger>().Info("ImporterWorker: Aborting import job '{0}' ({1} items pending - will be continued next time)", importJob, importJob.PendingResources.Count);
        return;
      }
    }

    #region IImporterWorker implementation

    public bool IsSuspended
    {
      get
      {
        lock (_syncObj)
          return _isSuspended;
      }
      internal set
      {
        lock (_syncObj)
        {
          _isSuspended = value;
          if (_isSuspended)
          {
            _mediaBrowsingCallback = null;
            _importResultHandler = null;
          }
          Monitor.PulseAll(_syncObj);
        }
      }
    }

    public void Startup()
    {
      IsSuspended = true;
      _messageQueue.Start();
      LoadPendingImportJobs();
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

    public void ScheduleImport(ResourcePath path, ICollection<string> mediaCategories, bool includeSubDirectories)
    {
      ICollection<ImportJob> importJobs;
      lock (_syncObj)
        importJobs = new List<ImportJob>(_importJobs);
      foreach (ImportJob checkJob in importJobs)
        if (checkJob.JobType == ImportJobType.Import && checkJob.BasePath.IsSameOrParentOf(path))
          // Path is already being scheduled as Import job
          // => the new job is already included in an already existing job
          return;
      CancelJobsForPath(path);
      ICollection<Guid> metadataExtractorIds = GetMetadataExtractorIdsForMediaCategories(mediaCategories);
      ImportJob job = new ImportJob(ImportJobType.Import, path, metadataExtractorIds, includeSubDirectories);
      EnqueueImportJob(job);
    }

    public void ScheduleRefresh(ResourcePath path, ICollection<string> mediaCategories, bool includeSubDirectories)
    {
      ICollection<ImportJob> importJobs;
      lock (_syncObj)
        importJobs = new List<ImportJob>(_importJobs);
      ICollection<ImportJob> removeImportJobs = new List<ImportJob>();
      foreach (ImportJob checkJob in importJobs)
      {
        if (checkJob.BasePath.IsSameOrParentOf(path))
          // The new job is already included in an already existing job
          return;
        if (checkJob.JobType == ImportJobType.Refresh && path.IsParentOf(checkJob.BasePath))
        { // The new job will include the old job
          checkJob.Cancel();
          removeImportJobs.Add(checkJob);
        }
      }
      lock (_syncObj)
        foreach (ImportJob cancelJob in removeImportJobs)
          _importJobs.Remove(cancelJob);
      ICollection<Guid> metadataExtractorIds = GetMetadataExtractorIdsForMediaCategories(mediaCategories);
      ImportJob job = new ImportJob(ImportJobType.Refresh, path, metadataExtractorIds, includeSubDirectories);
      EnqueueImportJob(job);
    }

    #endregion
  }
}
