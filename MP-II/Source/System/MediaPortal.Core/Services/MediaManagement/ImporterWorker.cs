#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using MediaPortal.Core.ImporterWorker;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Runtime;
using MediaPortal.Utilities;

namespace MediaPortal.Core.Services.MediaManagement
{
  public class ImporterWorker : IImporterWorker
  {
    protected static IEnumerable<Guid> IMPORTER_ASPECT_ID_ENUMERATION = new Guid[]
        {
          ImporterAspect.ASPECT_ID
        };
    protected static IEnumerable<Guid> IMPORTER_PROVIDER_ASPECT_ID_ENUMERATION = new Guid[]
        {
          ProviderResourceAspect.ASPECT_ID,
          ImporterAspect.ASPECT_ID
        };
    protected static IEnumerable<Guid> EMPTY_MIA_ID_ENUMERATION = new Guid[] {};

    protected AsynchronousMessageQueue _messageQueue;
    protected object _syncObj = new object();
    protected Queue<ImportJob> _importJobs = new Queue<ImportJob>();
    protected bool _isActive = true;

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
              IsActive = false;
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

    protected void AddImportJob(ImportJob job)
    {
      lock (_syncObj)
      {
        _importJobs.Enqueue(job);
        Monitor.PulseAll(_syncObj);
      }
    }

    protected ImportJob? DequeueImportJob()
    {
      lock (_syncObj)
        return _importJobs.Count == 0 ? new ImportJob?() : _importJobs.Dequeue();
    }

    protected void ImporterLoop()
    {
      while (true)
      {
        ImportJob? job = DequeueImportJob();
        if (job.HasValue)
          Process(job.Value);
        lock (_syncObj)
        {
          if (!IsActive)
            // We have to check this in the synchronized block, else we could miss the PulseAll event
            break;
          // We need to check this in a synchronized block. If we wouldn't prevent other threads from
          // enqueuing data in this moment, we could miss the PulseAll event
          else if (!IsImportJobAvailable)
            Monitor.Wait(_syncObj);
        }
      }
    }

    /// <summary>
    /// Imports the resource with the given <paramref name="mediaItemAccessor"/>.
    /// </summary>
    /// <param name="mediaItemAccessor">File resource to be imported.</param>
    /// <param name="metadataExtractors">Collection of metadata extractors to apply to the given resoure.</param>
    /// <param name="mediaItemAspectTypes">Media item aspect types which are expected to be filled. All of those
    /// media item aspects will be present in the result, but not all of their values might be set if no metadata extractor
    /// filled them.</param>
    /// <param name="resultCallback">Callback to notify the import results.</param>
    /// <param name="mediaAccessor">Convenience reference to the media accessor.</param>
    protected void ImportResource(IResourceAccessor mediaItemAccessor, ICollection<IMetadataExtractor> metadataExtractors,
        ICollection<MediaItemAspectMetadata> mediaItemAspectTypes, IImportResultCallback resultCallback,
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
      resultCallback.UpdateMediaItem(path, aspects.Values);
    }

    /// <summary>
    /// Imports or refreshes the directory with the specified <paramref name="directoryAccessor"/>.
    /// </summary>
    /// <param name="jobType">Determines if the given directory will be completely imported or just refreshed against the
    /// media library.</param>
    /// <param name="directoryAccessor">Resource accessor for the directory to import.</param>
    /// <param name="metadataExtractors">Metadata extractors to apply on the resources.</param>
    /// <param name="mediaItemAspectTypes">Types of the media item aspects which are expected to be filled.</param>
    /// <param name="mediaLibraryCallback">Callback interface to the media library for the refresh import type.</param>
    /// <param name="resultCallback">Callback to notify the import results.</param>
    /// <param name="includeSubDirectories">If set to <c>true</c>, also subdirectories will be imported. Else, only the
    /// given directory will be imported.</param>
    /// <param name="mediaAccessor">Convenience reference to the media accessor.</param>
    protected void ImportDirectory(ImportJobType jobType, IFileSystemResourceAccessor directoryAccessor,
        ICollection<IMetadataExtractor> metadataExtractors, ICollection<MediaItemAspectMetadata> mediaItemAspectTypes,
        IMediaLibraryCallback mediaLibraryCallback, IImportResultCallback resultCallback, bool includeSubDirectories,
        IMediaAccessor mediaAccessor)
    {
      try
      {
        IDictionary<string, MediaItem> path2Item = new Dictionary<string, MediaItem>();
        if (jobType == ImportJobType.Refresh)
        {
          foreach (MediaItem mediaItem in mediaLibraryCallback.Browse(directoryAccessor.LocalResourcePath,
              IMPORTER_PROVIDER_ASPECT_ID_ENUMERATION, EMPTY_MIA_ID_ENUMERATION))
          {
            MediaItemAspect providerResourceAspect;
            if (mediaItem.Aspects.TryGetValue(ProviderResourceAspect.ASPECT_ID, out providerResourceAspect))
              path2Item[providerResourceAspect.GetAttribute<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH)] = mediaItem;
          }
        }
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
            ImportResource(fileAccessor, metadataExtractors, mediaItemAspectTypes, resultCallback, mediaAccessor);
          }
          catch (Exception e)
          {
            ServiceScope.Get<ILogger>().Warn("ImporterWorker: Problem while importing resource '{0}'", e, fileAccessor.LocalResourcePath);
            resultCallback.ImportError(fileAccessor);
          }
        }
        if (jobType == ImportJobType.Refresh)
        { // Remove non-present files
          foreach (string pathStr in path2Item.Keys)
          {
            ResourcePath path = ResourcePath.Deserialize(pathStr);
            if (!directoryAccessor.Exists(path.LastPathSegment.Path))
              resultCallback.DeleteMediaItem(path);
          }
        }
        if (includeSubDirectories)
          // Handle subdirectories
          foreach (IFileSystemResourceAccessor childDirectory in FileSystemResourceNavigator.GetChildDirectories(directoryAccessor))
            ImportDirectory(jobType, childDirectory, metadataExtractors, mediaItemAspectTypes,
                mediaLibraryCallback, resultCallback, true, mediaAccessor);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Warn("ImporterWorker: Problem while importing directory '{0}'", e, directoryAccessor.LocalResourcePath);
        resultCallback.ImportError(directoryAccessor);
      }
    }

    /// <summary>
    /// Imports or refreshes the file with the specified <paramref name="fileAccessor"/>.
    /// </summary>
    /// <param name="jobType">Determines if the given file will be completely imported or just refreshed against the
    /// media library.</param>
    /// <param name="fileAccessor">Resource accessor for the file to import.</param>
    /// <param name="metadataExtractors">Metadata extractors to apply on the resource.</param>
    /// <param name="mediaItemAspectTypes">Types of the media item aspects which are expected to be filled.</param>
    /// <param name="mediaLibraryCallback">Callback interface to the media library for the refresh import type.</param>
    /// <param name="resultCallback">Callback to notify the import result.</param>
    /// <param name="mediaAccessor">Convenience reference to the media accessor.</param>
    protected void ImportFile(ImportJobType jobType, IResourceAccessor fileAccessor,
        ICollection<IMetadataExtractor> metadataExtractors, ICollection<MediaItemAspectMetadata> mediaItemAspectTypes,
        IMediaLibraryCallback mediaLibraryCallback, IImportResultCallback resultCallback, IMediaAccessor mediaAccessor)
    {
      try
      {
        if (jobType == ImportJobType.Refresh)
        {
          MediaItem mediaItem = mediaLibraryCallback.Browse(fileAccessor.LocalResourcePath,
              IMPORTER_ASPECT_ID_ENUMERATION, EMPTY_MIA_ID_ENUMERATION).FirstOrDefault();
          MediaItemAspect importerAspect;
          if (mediaItem.Aspects.TryGetValue(ImporterAspect.ASPECT_ID, out importerAspect))
            if ((DateTime) importerAspect[ImporterAspect.ATTR_LAST_IMPORT_DATE] > fileAccessor.LastChanged)
              return;
        }
        ImportResource(fileAccessor, metadataExtractors, mediaItemAspectTypes, resultCallback, mediaAccessor);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Warn("ImporterWorker: Problem while importing resource '{0}'", e, fileAccessor.LocalResourcePath);
        resultCallback.ImportError(fileAccessor);
      }
    }

    /// <summary>
    /// Executes the given <paramref name="importJob"/>.
    /// </summary>
    /// <param name="importJob">Import job to be executed.</param>
    protected void Process(ImportJob importJob)
    {
      IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
      ICollection<Guid> metadataExtractorIds = new HashSet<Guid>();
      foreach (string mediaCategory in importJob.MediaCategories)
        CollectionUtils.AddAll(metadataExtractorIds, mediaAccessor.GetMetadataExtractorsForCategory(mediaCategory));
      ICollection<MediaItemAspectMetadata> mediaItemAspectTypes = new HashSet<MediaItemAspectMetadata>();
      ICollection<IMetadataExtractor> metadataExtractors = new List<IMetadataExtractor>();
      foreach (Guid metadataExtractorId in metadataExtractorIds)
      {
        IMetadataExtractor extractor;
        if (!mediaAccessor.LocalMetadataExtractors.TryGetValue(metadataExtractorId, out extractor))
          continue;
        metadataExtractors.Add(extractor);
        CollectionUtils.AddAll(mediaItemAspectTypes, extractor.Metadata.ExtractedAspectTypes.Values);
      }
      IResourceAccessor accessor = importJob.Path.CreateLocalMediaItemAccessor();
      if (accessor.IsFile || !(accessor is IFileSystemResourceAccessor))
        ImportFile(importJob.JobType, accessor, metadataExtractors, mediaItemAspectTypes,
            importJob.MediaLibraryCallback, importJob.ResultCallback, mediaAccessor);
      else
      {
        IFileSystemResourceAccessor fsra = (IFileSystemResourceAccessor) accessor;
        if (fsra.IsDirectory)
          ImportDirectory(importJob.JobType, fsra, metadataExtractors, mediaItemAspectTypes,
              importJob.MediaLibraryCallback, importJob.ResultCallback, importJob.IncludeSubdirectories, mediaAccessor);
        else
          ServiceScope.Get<ILogger>().Warn("ImporterWorker: Cannot import resource '{0}': It's neither a file nor a directory", fsra.LocalResourcePath.Serialize());
      }
    }

    #region IImporterWorker implementation

    public bool IsActive
    {
      get
      {
        lock (_syncObj)
          return _isActive;
      }
      set
      {
        lock (_syncObj)
          _isActive = value;
      }
    }

    public void Startup()
    {
      _messageQueue.Start();
      IsActive = true;
    }

    public void Shutdown()
    {
      IsActive = false;
      _messageQueue.Shutdown();
    }

    public void ScheduleImport(ResourcePath path, ICollection<string> mediaCategories, bool includeSubDirectories,
        IImportResultCallback resultCallback)
    {
      ImportJob job = new ImportJob
          {
            JobType = ImportJobType.Import,
            Path = path,
            IncludeSubdirectories = includeSubDirectories,
            MediaLibraryCallback = null,
            ResultCallback = resultCallback
          };
      AddImportJob(job);
    }

    public void ScheduleRefresh(ResourcePath path, ICollection<string> mediaCategories, bool includeSubDirectories,
        IMediaLibraryCallback mediaLibraryCallback, IImportResultCallback resultCallback)
    {
      ImportJob job = new ImportJob
          {
            JobType = ImportJobType.Refresh,
            Path = path,
            IncludeSubdirectories = includeSubDirectories,
            MediaLibraryCallback = mediaLibraryCallback,
            ResultCallback = resultCallback
          };
      AddImportJob(job);
    }

    #endregion
  }
}
