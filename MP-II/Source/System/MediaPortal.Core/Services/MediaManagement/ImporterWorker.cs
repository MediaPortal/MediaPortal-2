#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
  internal class ImporterAbortException : ApplicationException
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
    protected Queue<ImportJob> _importJobs = new Queue<ImportJob>();
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

    private void OnMessageReceived(AsynchronousMessageQueue queue, QueueMessage message)
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
        foreach (ImportJob importJob in settings.PendingImportJobs)
          _importJobs.Enqueue(importJob);
        Monitor.PulseAll(_syncObj);
      }
    }

    public void CheckImporterSuspended()
    {
      if (IsSuspended)
        throw new ImporterAbortException();
    }

    protected void AddImportJob(ImportJob job)
    {
      lock (_syncObj)
      {
        _importJobs.Enqueue(job);
        Monitor.PulseAll(_syncObj);
      }
    }

    protected ImportJob DequeueImportJob()
    {
      lock (_syncObj)
        return _importJobs.Count == 0 ? null : _importJobs.Dequeue();
    }

    protected ImportJob PeekImportJob()
    {
      lock (_syncObj)
        return _importJobs.Count == 0 ? null : _importJobs.Peek();
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
          if (Process(job))
            DequeueImportJob();
          else
            IsSuspended = true;
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

    protected void StartImporterLoop()
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
    /// <param name="jobType">Determines if the given file will be completely imported or just refreshed against the
    /// media library.</param>
    /// <param name="fileAccessor">Resource accessor for the file to import.</param>
    /// <param name="metadataExtractors">Metadata extractors to apply on the resource.</param>
    /// <param name="mediaItemAspectTypes">Types of the media item aspects which are expected to be filled.</param>
    /// <param name="mediaBrowsing">Callback interface to the media library for the refresh import type.</param>
    /// <param name="resultHandler">Callback to notify the import result.</param>
    /// <param name="mediaAccessor">Convenience reference to the media accessor.</param>
    protected void ImportFile(ImportJobType jobType, IResourceAccessor fileAccessor,
        ICollection<IMetadataExtractor> metadataExtractors, ICollection<MediaItemAspectMetadata> mediaItemAspectTypes,
        IMediaBrowsing mediaBrowsing, IImportResultHandler resultHandler, IMediaAccessor mediaAccessor)
    {
      try
      {
        if (jobType == ImportJobType.Refresh)
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
        ServiceScope.Get<ILogger>().Warn("ImporterWorker: Problem while importing resource '{0}'", e, fileAccessor.LocalResourcePath);
        throw;
      }
    }

    /// <summary>
    /// Imports or refreshes the directory with the specified <paramref name="directoryAccessor"/>.
    /// </summary>
    /// <param name="jobType">Determines if the given directory will be completely imported or just refreshed against the
    /// media library.</param>
    /// <param name="directoryAccessor">Resource accessor for the directory to import.</param>
    /// <param name="metadataExtractors">Metadata extractors to apply on the resources.</param>
    /// <param name="mediaItemAspectTypes">Types of the media item aspects which are expected to be filled.</param>
    /// <param name="mediaBrowsing">Callback interface to the media library for the refresh import type.</param>
    /// <param name="resultHandler">Callback to notify the import result.</param>
    /// <param name="mediaAccessor">Convenience reference to the media accessor.</param>
    protected void ImportDirectory(ImportJobType jobType, IFileSystemResourceAccessor directoryAccessor,
        ICollection<IMetadataExtractor> metadataExtractors, ICollection<MediaItemAspectMetadata> mediaItemAspectTypes,
        IMediaBrowsing mediaBrowsing, IImportResultHandler resultHandler, IMediaAccessor mediaAccessor)
    {
      try
      {
        CheckImporterSuspended();
        ImporterWorkerMessaging.SendImportStatusMessage(directoryAccessor.LocalResourcePath);
        IDictionary<string, MediaItem> path2Item = new Dictionary<string, MediaItem>();
        if (jobType == ImportJobType.Refresh)
        {
          foreach (MediaItem mediaItem in mediaBrowsing.Browse(directoryAccessor.LocalResourcePath,
              IMPORTER_PROVIDER_MIA_ID_ENUMERATION, EMPTY_MIA_ID_ENUMERATION))
          {
            MediaItemAspect providerResourceAspect;
            if (mediaItem.Aspects.TryGetValue(ProviderResourceAspect.ASPECT_ID, out providerResourceAspect))
              path2Item[providerResourceAspect.GetAttribute<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH)] = mediaItem;
          }
        }
        CheckImporterSuspended();
        foreach (IFileSystemResourceAccessor fileAccessor in FileSystemResourceNavigator.GetFiles(directoryAccessor))
        { // Add & update files
          try
          {
            MediaItemAspect importerAspect;
            MediaItem mediaItem;
            if (jobType == ImportJobType.Refresh &&
                path2Item.TryGetValue(fileAccessor.LocalResourcePath.Serialize(), out mediaItem) &&
                mediaItem.Aspects.TryGetValue(ImporterAspect.ASPECT_ID, out importerAspect) &&
                importerAspect.GetAttribute<DateTime>(ImporterAspect.ATTR_LAST_IMPORT_DATE) > fileAccessor.LastChanged)
              // We can skip this file; it was imported after the last change time of the item
              continue;
            ImportResource(fileAccessor, metadataExtractors, mediaItemAspectTypes, resultHandler, mediaAccessor);
          }
          catch (Exception e)
          {
            ServiceScope.Get<ILogger>().Warn("ImporterWorker: Problem while importing resource '{0}'", e, fileAccessor.LocalResourcePath);
          }
        }
        if (jobType == ImportJobType.Refresh)
        { // Remove non-present files
          foreach (string pathStr in path2Item.Keys)
          {
            ResourcePath path = ResourcePath.Deserialize(pathStr);
            if (!directoryAccessor.Exists(path.LastPathSegment.Path))
              resultHandler.DeleteMediaItem(path);
          }
        }
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Warn("ImporterWorker: Problem while importing directory '{0}'", e, directoryAccessor.LocalResourcePath);
        throw;
      }
    }

    /// <summary>
    /// Executes the given <paramref name="importJob"/>.
    /// </summary>
    /// <param name="importJob">Import job to be executed.</param>
    /// <returns><c>true</c>, if the import job could be successfully executed, else <c>false</c>. In case the return value
    /// is <c>false</c>, the import job wasn't processed completely and should be re-scheduled.</returns>
    protected bool Process(ImportJob importJob)
    {
      // Preparation
      IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
      IImportResultHandler resultHandler = _importResultHandler;
      IMediaBrowsing mediaBrowsing = _mediaBrowsingCallback;
      if (mediaBrowsing == null || resultHandler == null)
        return false;
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

        // Import
        IResourceAccessor accessor = importJob.BasePath.CreateLocalMediaItemAccessor();
        if (!(accessor is IFileSystemResourceAccessor))
          ImportFile(importJob.JobType, accessor, metadataExtractors, mediaItemAspectTypes,
              mediaBrowsing, resultHandler, mediaAccessor);
        else
        {
          importJob.PendingResources.Add((IFileSystemResourceAccessor) accessor);
          while (importJob.PendingResources.Count > 0)
          {
            CheckImporterSuspended();
            IFileSystemResourceAccessor fsra = importJob.PendingResources.FirstOrDefault();
            if (fsra.IsFile)
              ImportFile(importJob.JobType, accessor, metadataExtractors, mediaItemAspectTypes,
                  mediaBrowsing, resultHandler, mediaAccessor);
            else if (fsra.IsDirectory)
            {
              ImportDirectory(importJob.JobType, fsra, metadataExtractors, mediaItemAspectTypes,
                  mediaBrowsing, resultHandler, mediaAccessor);
              CheckImporterSuspended();
              if (importJob.IncludeSubDirectories)
                // Enqueue subdirectories to work queue
                foreach (IFileSystemResourceAccessor childDirectory in FileSystemResourceNavigator.GetChildDirectories(fsra))
                  importJob.PendingResources.Add(childDirectory);
            }
            else
              ServiceScope.Get<ILogger>().Warn("ImporterWorker: Cannot import resource '{0}': It's neither a file nor a directory", fsra.LocalResourcePath.Serialize());
            importJob.PendingResources.Remove(fsra);
          }
        }
        return true;
      }
      catch (ImporterAbortException)
      {
        ServiceScope.Get<ILogger>().Info("ImporterWorker: Aborting import job '{0}'", importJob);
        throw;
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
          else
            Monitor.PulseAll(_syncObj);
        }
      }
    }

    public void Startup()
    {
      _messageQueue.Start();
      LoadPendingImportJobs();
      StartImporterLoop();
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
        StartImporterLoop();
      }
    }

    public void ScheduleImport(ResourcePath path, ICollection<string> mediaCategories, bool includeSubDirectories)
    {
      ICollection<Guid> metadataExtractorIds = GetMetadataExtractorIdsForMediaCategories(mediaCategories);
      ImportJob job = new ImportJob(ImportJobType.Import, path, metadataExtractorIds, includeSubDirectories);
      AddImportJob(job);
    }

    public void ScheduleRefresh(ResourcePath path, ICollection<string> mediaCategories, bool includeSubDirectories)
    {
      ICollection<Guid> metadataExtractorIds = GetMetadataExtractorIdsForMediaCategories(mediaCategories);
      ImportJob job = new ImportJob(ImportJobType.Refresh, path, metadataExtractorIds, includeSubDirectories);
      AddImportJob(job);
    }

    #endregion
  }
}
