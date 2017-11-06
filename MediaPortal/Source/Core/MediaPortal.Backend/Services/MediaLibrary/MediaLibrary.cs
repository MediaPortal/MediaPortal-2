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
using System.Data;
using System.Linq;
using System.Threading;
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.Exceptions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.Services.ResourceAccess.VirtualResourceProvider;
using MediaPortal.Utilities;
using MediaPortal.Utilities.DB;
using MediaPortal.Utilities.Exceptions;
using RelocationMode = MediaPortal.Backend.MediaLibrary.RelocationMode;
using MediaPortal.Backend.Services.UserProfileDataManagement;
using System.IO;
using MediaPortal.Common.UserProfileDataManagement;
using System.Diagnostics;
using MediaPortal.Common.Services.MediaManagement;
using System.Threading.Tasks;

namespace MediaPortal.Backend.Services.MediaLibrary
{
  // TODO: Preparation of some SQL statements? We could use a lazy initialized DBCommand cache which prepares DBCommands
  // on the fly and holds up to N prepared commands.
  public class MediaLibrary : IMediaLibrary, IDisposable
  {
    #region Inner classes

    protected class MediaBrowsingCallback : IMediaBrowsing
    {
      protected MediaLibrary _parent;

      public MediaBrowsingCallback(MediaLibrary parent)
      {
        _parent = parent;
      }

      public MediaItem LoadLocalItem(ResourcePath path,
          IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs, Guid? userProfileId = null)
      {
        try
        {
          return _parent.LoadItem(_parent.LocalSystemId, path, necessaryRequestedMIATypeIDs, optionalRequestedMIATypeIDs, userProfileId);
        }
        catch (Exception)
        {
          throw new DisconnectedException();
        }
      }

      public MediaItem LoadLocalItem(Guid mediaItemId,
          IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs, Guid? userProfileId = null)
      {
        try
        {
          return _parent.LoadItem(_parent.LocalSystemId, mediaItemId, necessaryRequestedMIATypeIDs, optionalRequestedMIATypeIDs, userProfileId);
        }
        catch (Exception)
        {
          throw new DisconnectedException();
        }
      }

      public IList<MediaItem> Browse(Guid parentDirectoryId,
          IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs, Guid? userProfileId,
          bool includeVirtual, uint? offset = null, uint? limit = null)
      {
        try
        {
          return _parent.Browse(parentDirectoryId, necessaryRequestedMIATypeIDs, optionalRequestedMIATypeIDs, userProfileId, includeVirtual, offset, limit);
        }
        catch (Exception)
        {
          throw new DisconnectedException();
        }
      }

      public IDictionary<Guid, DateTime> GetManagedMediaItemAspectCreationDates()
      {
        try
        {
          return _parent.GetManagedMediaItemAspectCreationDates();
        }
        catch (Exception)
        {
          throw new DisconnectedException();
        }
      }

      public ICollection<Guid> GetAllManagedMediaItemAspectTypes()
      {
        try
        {
          return _parent.GetManagedMediaItemAspectMetadata().Keys;
        }
        catch (Exception)
        {
          throw new DisconnectedException();
        }
      }

      public void MarkUpdatableMediaItems()
      {
        try
        {
          _parent.MarkUpdatableMediaItems();
        }
        catch (Exception)
        {
          throw new DisconnectedException();
        }
      }
    }

    protected class ImportResultHandler : IImportResultHandler
    {
      protected MediaLibrary _parent;

      public ImportResultHandler(MediaLibrary parent)
      {
        _parent = parent;
      }

      public Guid UpdateMediaItem(Guid parentDirectoryId, ResourcePath path, IEnumerable<MediaItemAspect> updatedAspects, bool isRefresh, ResourcePath basePath, CancellationToken cancelToken)
      {
        try
        {
          lock (_parent.GetResourcePathLock(basePath))
          {
            return _parent.AddOrUpdateMediaItem(parentDirectoryId, _parent.LocalSystemId, path, null, null, updatedAspects, true, isRefresh, cancelToken);
          }
        }
        catch (Exception)
        {
          throw new DisconnectedException();
        }
      }

      public Guid UpdateMediaItem(Guid parentDirectoryId, ResourcePath path, Guid mediaItemId, IEnumerable<MediaItemAspect> updatedAspects, bool isRefresh, ResourcePath basePath, CancellationToken cancelToken)
      {
        try
        {
          lock (_parent.GetResourcePathLock(basePath))
          {
            return _parent.AddOrUpdateMediaItem(parentDirectoryId, _parent.LocalSystemId, path, mediaItemId, null, updatedAspects, true, isRefresh, cancelToken);
          }
        }
        catch (Exception)
        {
          throw new DisconnectedException();
        }
      }

      public void DeleteMediaItem(ResourcePath path)
      {
        try
        {
          _parent.DeleteMediaItemOrPath(_parent.LocalSystemId, path, true);
        }
        catch (Exception)
        {
          throw new DisconnectedException();
        }
      }

      public void DeleteUnderPath(ResourcePath path)
      {
        try
        {
          _parent.DeleteMediaItemOrPath(_parent.LocalSystemId, path, false);
        }
        catch (Exception)
        {
          throw new DisconnectedException();
        }
      }
    }

    protected class ShareWatcher : IDisposable
    {
      protected MediaLibrary _parent;
      protected Share _share;
      protected IResourceChangeNotifier _fileChangeNotifier;
      protected DateTime? _lastChange;
      protected int _importDelay;
      protected Timer _checkTimer;

      public ShareWatcher(Share share, MediaLibrary parent, bool scheduleImport, int checkIntervalMs = 5000, int importDelaySecs = 300)
      {
        _share = share;
        _parent = parent;
        _importDelay = importDelaySecs;
        _lastChange = null;

        IResourceAccessor resAccess = null;
        if (!share.BaseResourcePath.TryCreateLocalResourceAccessor(out resAccess))
          return;

        ILocalFsResourceAccessor fileAccess = resAccess as ILocalFsResourceAccessor;
        if (fileAccess != null)
        {
          if (!fileAccess.Exists || fileAccess.IsFile)
            return;
        }
        else
        {
          return;
        }

        _fileChangeNotifier = resAccess as IResourceChangeNotifier;
        if (_fileChangeNotifier == null)
          return;

        if (!string.IsNullOrEmpty(resAccess.ResourcePathName))
        {
          if (scheduleImport)
            _lastChange = DateTime.Now;
          _checkTimer = new Timer(CheckShareChange, null, checkIntervalMs, checkIntervalMs);

          List<MediaSourceChangeType> changeTypes = new List<MediaSourceChangeType>();
          changeTypes.Add(MediaSourceChangeType.Created);
          changeTypes.Add(MediaSourceChangeType.Deleted);
          changeTypes.Add(MediaSourceChangeType.DirectoryDeleted);
          changeTypes.Add(MediaSourceChangeType.Renamed);
          _fileChangeNotifier.RegisterChangeTracker(ShareWatcherPathChanged, null, changeTypes);

          Logger.Info("MediaLibrary: Registered share watcher for path {0}", share.BaseResourcePath);
        }
      }

      private void ShareWatcherPathChanged(IResourceAccessor resourceAccessor, IResourceAccessor oldResourceAccessor, MediaSourceChangeType changeType)
      {
        try
        {
          ILocalFsResourceAccessor fileAccess = resourceAccessor as ILocalFsResourceAccessor;
          if (fileAccess == null)
            return;

          //Check if resource is part of share
          if (!_share.BaseResourcePath.IsParentOf(fileAccess.CanonicalLocalResourcePath))
            return;

          _lastChange = DateTime.Now;
        }
        catch (Exception e)
        {
          Logger.Error("MediaLibrary: Error logging change for share {0}", e, _share.Name);
        }
      }

      private void CheckShareChange(object stateInfo)
      {
        try
        {
          if (_lastChange.HasValue)
          {
            if ((DateTime.Now - _lastChange.Value).TotalSeconds > _importDelay)
            {
              _lastChange = null;

              IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();
              importerWorker.ScheduleImport(_share.BaseResourcePath, _share.MediaCategories, true);

              Logger.Info("MediaLibrary: Share watcher triggered import for path {0}", _share.BaseResourcePath);
            }
          }
        }
        catch (Exception e)
        {
          Logger.Error("MediaLibrary: Error starting import for share {0}", e, _share.Name);
        }
      }

      public void Dispose()
      {
        if (_fileChangeNotifier != null)
          _fileChangeNotifier.UnregisterChangeTracker(ShareWatcherPathChanged);

        if (_checkTimer != null)
          _checkTimer.Dispose();
      }
    }

    #endregion

    #region Consts

    protected const string KEY_CURRENTLY_IMPORTING_SHARE_IDS = "CurrentlyImportingShareIds";
    protected const char ESCAPE_CHAR = '\\';

    /// <summary>
    /// SQLite has a default variable limit of 100, this value is deliberately set a bit lower to allow a bit of headroom.
    /// Currently only used when requesting multiple MediaItems by id as the variable count can be easily determined for those queries.
    /// ToDo check the limits of other SQL providers.
    /// </summary>
    protected const int MAX_VARIABLES_LIMIT = 80;

    #endregion

    #region Protected fields

    protected IDictionary<string, SystemName> _systemsOnline = new Dictionary<string, SystemName>(); // System ids mapped to system names

    protected MIA_Management _miaManagement = null;
    protected RelationshipManagement _relationshipManagement = null;
    protected object _syncObj = new object();
    protected string _localSystemId;
    protected IMediaBrowsing _mediaBrowsingCallback;
    protected IImportResultHandler _importResultHandler;
    protected AsynchronousMessageQueue _messageQueue;
    protected bool _shutdown = false;
    protected readonly Dictionary<Guid, ShareWatcher> _shareWatchers = new Dictionary<Guid, ShareWatcher>();
    // Should be accessed only by GetResourcePathLock
    private readonly Dictionary<ResourcePath, object> _shareDeleteSync = new Dictionary<ResourcePath, object>();
    protected object _shareImportSync = new object();
    protected Dictionary<Guid, ShareImportState> _shareImportStates = new Dictionary<Guid, ShareImportState>();
    protected object _shareImportCacheSync = new object();
    protected ICollection<Share> _importingSharesCache;

    #endregion

    #region Private fields

    // Prepared SQL strings for better performance
    private static string SELECT_MEDIAITEM_ID_FROM_PATH_SQL = null;
    private static string INSERT_USER_PLAYCOUNT_SQL = null;
    private static string SELECT_MEDIAITEM_RELATIONSHIPS_FROM_ID_SQL = null;
    private static string DELETE_MEDIAITEM_RELATIONSHIPS_FROM_ID_SQL = null;
    private static string SELECT_ORPHAN_COUNT_SQL = null;
    private static string UPDATE_USER_PLAY_DATA_FROM_ID_SQL = null;
    private static string INSERT_USER_PLAY_DATA_FOR_ID_SQL = null;
    private static string UPDATE_MEDIAITEMS_DIRTY_ATTRIBUTE_FROM_ID_SQL = null;
    private static string SELECT_PARENT_USER_PLAY_DATA_FROM_ID_SQL = null;
    private static string SELECT_PLAY_DATA_FROM_PARENT_ID_SQL = null;
    private static string SELECT_USER_DATA_FROM_PARENT_ID_SQL = null;
    private static string SELECT_MEDIAITEM_USER_DATA_FROM_IDS_SQL = null;

    #endregion

    #region Ctor & dtor

    public MediaLibrary()
    {
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      _localSystemId = systemResolver.LocalSystemId;

      _mediaBrowsingCallback = new MediaBrowsingCallback(this);
      _importResultHandler = new ImportResultHandler(this);
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            ImporterWorkerMessaging.CHANNEL,
            ContentDirectoryMessaging.CHANNEL
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    public void Dispose()
    {
      _messageQueue.Shutdown();
    }

    #endregion

    #region Import Progress

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ContentDirectoryMessaging.CHANNEL)
      {
        ContentDirectoryMessaging.MessageType messageType = (ContentDirectoryMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ContentDirectoryMessaging.MessageType.RegisteredSharesChanged:
            UpdateShareWatchers();
            break;
        }
      }

      if (message.ChannelName == ImporterWorkerMessaging.CHANNEL)
      {
        ImporterWorkerMessaging.MessageType messageType = (ImporterWorkerMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ImporterWorkerMessaging.MessageType.ImportStarted:
          case ImporterWorkerMessaging.MessageType.ImportCompleted:
            {
              ResourcePath path = (ResourcePath)message.MessageData[ImporterWorkerMessaging.RESOURCE_PATH];
              Share share = null;
              lock (_shareImportCacheSync)
              {
                if (_importingSharesCache == null || messageType == ImporterWorkerMessaging.MessageType.ImportStarted)
                  _importingSharesCache = GetShares(null).Values;
                share = _importingSharesCache.BestContainingPath(path);
              }
              if (share == null)
                break;
              if (messageType == ImporterWorkerMessaging.MessageType.ImportStarted)
              {
                ContentDirectoryMessaging.SendShareImportMessage(ContentDirectoryMessaging.MessageType.ShareImportStarted, share.ShareId);
                lock (_shareImportSync)
                {
                  if (!_shareImportStates.ContainsKey(share.ShareId))
                    _shareImportStates.Add(share.ShareId, new ShareImportState { ShareId = share.ShareId, IsImporting = true, Progress = -1 });
                }
                UpdateServerState();
              }
              else
              {
                ContentDirectoryMessaging.SendShareImportMessage(ContentDirectoryMessaging.MessageType.ShareImportCompleted, share.ShareId);
                lock (_shareImportSync)
                {
                  if (_shareImportStates.ContainsKey(share.ShareId))
                    _shareImportStates.Remove(share.ShareId);
                }
                //Delay state update to ensure it's last
                Task.Run(async () =>
                {
                  await Task.Delay(1000);
                  UpdateServerState();
                });
              }
            }
            break;
          case ImporterWorkerMessaging.MessageType.ImportProgress:
            {
              var progress = (Dictionary<ImportJobInformation, Tuple<int, int>>)message.MessageData[ImporterWorkerMessaging.IMPORT_PROGRESS];
              if (progress != null)
              {
                bool anyProgressAvailable = false;
                foreach (ImportJobInformation importJobInfo in progress.Keys)
                {
                  Share share = null;
                  lock (_shareImportCacheSync)
                  {
                    if (_importingSharesCache == null)
                      _importingSharesCache = GetShares(null).Values;
                    share = _importingSharesCache.BestContainingPath(importJobInfo.BasePath);
                  }
                  if (share == null)
                    continue;
                  lock (_shareImportSync)
                  {
                    if (_shareImportStates.ContainsKey(share.ShareId))
                    {
                      _shareImportStates[share.ShareId].IsImporting = true;
                      var progressPercent = progress[importJobInfo].Item2 / (double)progress[importJobInfo].Item1 * 100.0;
                      int progressInt = progressPercent > 100 || progressPercent < 0 ? 0 : (int)progressPercent;
                      _shareImportStates[share.ShareId].Progress = progressInt;
                      anyProgressAvailable = true;
                    }
                  };
                }
                if (anyProgressAvailable)
                  UpdateServerState();
              }
            }
            break;
          case ImporterWorkerMessaging.MessageType.RefreshLocalShares:
            GetShares(null).Values.ToList().ForEach(TryScheduleLocalShareRefresh);
            break;
        }
      }
    }

    protected void UpdateServerState()
    {
      try
      {
        double? progress = null;
        double count = 0;
        bool importing = false;
        List<ShareImportState> shareStates = new List<ShareImportState>();
        lock (_shareImportSync)
          shareStates.AddRange(_shareImportStates.Values);
        foreach (ShareImportState shareSate in shareStates)
        {
          importing |= shareSate.IsImporting;
          if (shareSate.Progress >= 0)
          {
            if (!progress.HasValue)
              progress = 0;
            progress += shareSate.Progress;
            count++;
          }
        }

        var state = new ShareImportServerState
        {
          IsImporting = importing,
          Progress = (progress.HasValue && importing) ? Convert.ToInt32(progress / count) : -1,
          Shares = shareStates.ToArray()
        };
        ServiceRegistration.Get<IServerStateService>().UpdateState(ShareImportServerState.STATE_ID, state);
      }
      catch (Exception ex)
      {
        Logger.Warn("MediaLibrary: Error sending import progress", ex);
      }
    }

    #endregion

    public string LocalSystemId
    {
      get { return _localSystemId; }
    }

    #region Protected methods

    protected object GetResourcePathLock(ResourcePath path)
    {
      lock (_syncObj)
      {
        if (!_shareDeleteSync.ContainsKey(path))
          _shareDeleteSync.Add(path, new object());
        return _shareDeleteSync[path];
      }
    }

    protected MediaItemQuery BuildLoadItemQuery(string systemId, ResourcePath path)
    {
      return new MediaItemQuery(new List<Guid>(), new List<Guid>(),
          new BooleanCombinationFilter(BooleanOperator.And, new IFilter[]
            {
              new RelationalFilter(ProviderResourceAspect.ATTR_SYSTEM_ID, RelationalOperator.EQ, systemId),
              new RelationalFilter(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RelationalOperator.EQ, path.Serialize())
            }));
    }

    protected MediaItemQuery BuildLoadItemQuery(string systemId, Guid mediaItemId)
    {
      return new MediaItemQuery(new List<Guid>(), new List<Guid>(),
          new BooleanCombinationFilter(BooleanOperator.And, new IFilter[]
            {
              new RelationalFilter(ProviderResourceAspect.ATTR_SYSTEM_ID, RelationalOperator.EQ, systemId),
              new MediaItemIdFilter(mediaItemId)
            }));
    }

    protected MediaItemQuery BuildBrowseQuery(Guid directoryItemId)
    {
      return new MediaItemQuery(new List<Guid>(), new List<Guid>(),
          new RelationalFilter(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID, RelationalOperator.EQ, directoryItemId));
    }

    protected Share GetShare(ITransaction transaction, Guid shareId)
    {
      int systemIdIndex;
      int pathIndex;
      int shareNameIndex;
      int useWatcherIndex;

      ISQLDatabase database = transaction.Database;
      Share share;
      using (IDbCommand command = MediaLibrary_SubSchema.SelectShareByIdCommand(transaction, shareId, out systemIdIndex, out pathIndex, out shareNameIndex, out useWatcherIndex))
      using (IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
      {
        if (!reader.Read())
          return null;
        share = new Share(shareId, database.ReadDBValue<string>(reader, systemIdIndex), ResourcePath.Deserialize(
            database.ReadDBValue<string>(reader, pathIndex)),
            database.ReadDBValue<string>(reader, shareNameIndex),
            database.ReadDBValue<int>(reader, useWatcherIndex) == 1, null);
      }
      // Init share categories later to avoid opening new result sets inside reader loop (issue with MySQL)
      ICollection<string> mediaCategories = GetShareMediaCategories(transaction, shareId);
      CollectionUtils.AddAll(share.MediaCategories, mediaCategories);
      return share;
    }

    protected Guid? GetMediaItemId(ITransaction transaction, string systemId, ResourcePath resourcePath)
    {
      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = transaction.CreateCommand())
      {
        database.AddParameter(command, "SYSTEM_ID", systemId, typeof(string));
        database.AddParameter(command, "PATH", resourcePath.Serialize(), typeof(string));

        command.CommandText = SELECT_MEDIAITEM_ID_FROM_PATH_SQL;
        using (IDataReader reader = command.ExecuteReader())
        {
          if (!reader.Read())
            return null;
          return database.ReadDBValue<Guid>(reader, 0);
        }
      }
    }

    protected Guid AddMediaItem(ISQLDatabase database, ITransaction transaction, Guid? newMediaItemId)
    {
      Guid mediaItemId = newMediaItemId.HasValue ? newMediaItemId.Value : NewMediaItemId();
      //Logger.Debug("Creating media item {0}", mediaItemId);
      using (IDbCommand command = MediaLibrary_SubSchema.InsertMediaItemCommand(transaction, mediaItemId))
        command.ExecuteNonQuery();

      return mediaItemId;
    }

    protected virtual Guid NewMediaItemId()
    {
      return Guid.NewGuid();
    }

    protected int RelocateMediaItems(ITransaction transaction,
        string systemId, ResourcePath originalBasePath, ResourcePath newBasePath)
    {
      string originalBasePathStr = StringUtils.CheckSuffix(originalBasePath.Serialize(), "/");
      string newBasePathStr = StringUtils.CheckSuffix(newBasePath.Serialize(), "/");
      if (originalBasePathStr == newBasePathStr)
        return 0;

      string providerAspectTable = _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata);
      string systemIdAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SYSTEM_ID);
      string pathAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);

      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = transaction.CreateCommand())
      {
        command.CommandText = "UPDATE " + providerAspectTable + " SET " +
            pathAttribute + " = " + database.CreateStringConcatenationExpression("@PATH",
                database.CreateSubstringExpression(pathAttribute, (originalBasePathStr.Length + 1).ToString())) +
            " WHERE " + systemIdAttribute + " = @SYSTEM_ID AND " +
            database.CreateSubstringExpression(pathAttribute, "1", originalBasePathStr.Length.ToString()) + " = @ORIGBASEPATH";

        database.AddParameter(command, "PATH", newBasePathStr, typeof(string));
        database.AddParameter(command, "SYSTEM_ID", systemId, typeof(string));
        database.AddParameter(command, "ORIGBASEPATH", originalBasePathStr, typeof(string));

        return command.ExecuteNonQuery();
      }
    }

    protected IFilter CreateAddtionalFilter(bool filterOnlyOnline, bool includeVirtual)
    {
      IFilter additionalFilter = null;
      if (filterOnlyOnline)
        additionalFilter = AddOnlyOnlineFilter(additionalFilter);
      if (!includeVirtual)
        additionalFilter = AddExcludeVirtualFilter(additionalFilter);
      return additionalFilter;
    }

    protected IFilter AddOnlyOnlineFilter(IFilter innerFilter)
    {
      IFilter onlineFilter = new BooleanCombinationFilter(BooleanOperator.Or, _systemsOnline.Select(
          systemEntry => new RelationalFilter(ProviderResourceAspect.ATTR_SYSTEM_ID, RelationalOperator.EQ, systemEntry.Key)));
      return innerFilter == null ? onlineFilter : BooleanCombinationFilter.CombineFilters(BooleanOperator.And, innerFilter, onlineFilter);
    }

    protected IFilter AddExcludeVirtualFilter(IFilter innerFilter)
    {
      IFilter excludeVirtualFilter = new RelationalFilter(MediaAspect.ATTR_ISVIRTUAL, RelationalOperator.EQ, false);
      return innerFilter == null ? excludeVirtualFilter : BooleanCombinationFilter.CombineFilters(BooleanOperator.And, innerFilter, excludeVirtualFilter);
    }

    /// <summary>
    /// Determines whether the <paramref name="maybeRequestedMIATypeId"/> was included in either <paramref name="necessaryRequestedMIATypeIDs"/> or
    /// <paramref name="optionalRequestedMIATypeIDs"/>.
    /// </summary>
    /// <param name="maybeRequestedMIATypeId">The mia type id to check.</param>
    /// <param name="necessaryRequestedMIATypeIDs">Enumeration of necessary mia type ids.</param>
    /// <param name="optionalRequestedMIATypeIDs">Enumeration of optional mia type ids.</param>
    /// <returns>True if <paramref name="maybeRequestedMIATypeId"/> was included in either <paramref name="necessaryRequestedMIATypeIDs"/> or
    /// <paramref name="optionalRequestedMIATypeIDs"/>.</returns>
    protected bool IsMiaTypeRequested(Guid maybeRequestedMIATypeId, IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs)
    {
      return (necessaryRequestedMIATypeIDs != null && necessaryRequestedMIATypeIDs.Contains(maybeRequestedMIATypeId)) ||
        (optionalRequestedMIATypeIDs != null && optionalRequestedMIATypeIDs.Contains(maybeRequestedMIATypeId)); 
    }

    protected ICollection<string> GetShareMediaCategories(ITransaction transaction, Guid shareId)
    {
      int mediaCategoryIndex;
      ICollection<string> result = new List<string>();
      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = MediaLibrary_SubSchema.SelectShareCategoriesCommand(transaction, shareId, out mediaCategoryIndex))
      using (IDataReader reader = command.ExecuteReader())
      {
        while (reader.Read())
          result.Add(database.ReadDBValue<string>(reader, mediaCategoryIndex));
      }
      return result;
    }

    protected void AddMediaCategoryToShare(ITransaction transaction, Guid shareId, string mediaCategory)
    {
      using (IDbCommand command = MediaLibrary_SubSchema.InsertShareCategoryCommand(transaction, shareId, mediaCategory))
        command.ExecuteNonQuery();
    }

    protected void RemoveMediaCategoryFromShare(ITransaction transaction, Guid shareId, string mediaCategory)
    {
      using (IDbCommand command = MediaLibrary_SubSchema.DeleteShareCategoryCommand(transaction, shareId, mediaCategory))
        command.ExecuteNonQuery();
    }

    protected void TryScheduleLocalShareRefresh(Share share)
    {
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();

      if (share.SystemId == _localSystemId)
        importerWorker.ScheduleRefresh(share.BaseResourcePath, share.MediaCategories, true);
    }

    protected void TryScheduleLocalShareImport(Share share)
    {
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();

      if (share.SystemId == _localSystemId)
        importerWorker.ScheduleImport(share.BaseResourcePath, share.MediaCategories, true);
    }

    protected void TryCancelLocalImportJobs(Share share)
    {
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();

      if (share.SystemId == _localSystemId)
        importerWorker.CancelJobsForPath(share.BaseResourcePath);
    }

    #endregion

    #region IMediaLibrary implementation

    #region Startup & Shutdown

    public void Startup()
    {
      _shutdown = false;
      DatabaseSubSchemaManager updater = new DatabaseSubSchemaManager(MediaLibrary_SubSchema.SUBSCHEMA_NAME);
      updater.AddDirectory(MediaLibrary_SubSchema.SubSchemaScriptDirectory);
      int curVersionMajor;
      int curVersionMinor;
      if (!updater.UpdateSubSchema(out curVersionMajor, out curVersionMinor) ||
          curVersionMajor != MediaLibrary_SubSchema.EXPECTED_SCHEMA_VERSION_MAJOR ||
          curVersionMinor != MediaLibrary_SubSchema.EXPECTED_SCHEMA_VERSION_MINOR)
        throw new IllegalCallException(string.Format(
            "Unable to update the MediaLibrary's subschema version to expected version {0}.{1}",
            MediaLibrary_SubSchema.EXPECTED_SCHEMA_VERSION_MAJOR, MediaLibrary_SubSchema.EXPECTED_SCHEMA_VERSION_MINOR));

      _miaManagement = new MIA_Management();
      _relationshipManagement = new RelationshipManagement(_miaManagement, _localSystemId);
      //PrepareDatabaseQueries(); //Initial prepare

      NotifySystemOnline(_localSystemId, SystemName.GetLocalSystemName());
    }

    private void PrepareDatabaseQueries()
    {
      //Prepare SQLs
      SELECT_MEDIAITEM_RELATIONSHIPS_FROM_ID_SQL = "SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
        " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
        " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID" +
        " UNION" +
        " SELECT " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) +
        " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
        " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID";
      UPDATE_MEDIAITEMS_DIRTY_ATTRIBUTE_FROM_ID_SQL = "UPDATE " + _miaManagement.GetMIATableName(ImporterAspect.Metadata) +
        " SET " + _miaManagement.GetMIAAttributeColumnName(ImporterAspect.ATTR_DIRTY) + " = 1" +
        " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN ({0})";
      SELECT_PARENT_USER_PLAY_DATA_FROM_ID_SQL = "SELECT DISTINCT " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) +
        " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
        " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_PLAYABLE) + " = 1" +
        " AND " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID";
      SELECT_PLAY_DATA_FROM_PARENT_ID_SQL = "SELECT M." + _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL) +
        ", U." + UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME +
        ", M." + _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_PLAYCOUNT) +
        " FROM " + _miaManagement.GetMIATableName(MediaAspect.Metadata) + " M" +
        " LEFT OUTER JOIN " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME + " U" +
        " ON U." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = M." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
        " AND U." + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + " = @USER_PROFILE_ID" +
        " AND U." + UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + " = @USER_DATA_KEY" +
        " WHERE M." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN (" +
        " SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
        " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) + " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID" +
        " AND " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_PLAYABLE) + " = 1" +
        ")";
      SELECT_USER_DATA_FROM_PARENT_ID_SQL = "SELECT R." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
        ", U." + UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME +
        " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) + " R" +
        " JOIN " + _miaManagement.GetMIATableName(MediaAspect.Metadata) + " M" +
        " ON M." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = R." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
        " LEFT OUTER JOIN " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME + " U" +
        " ON U." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = R." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
        " AND U." + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + " = @USER_PROFILE_ID" +
        " AND U." + UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + " = @USER_DATA_KEY" +
        " WHERE R." + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID" +
        " AND R." + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_PLAYABLE) + " = 1" +
        " AND M." + _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL) + " = 0";
      SELECT_MEDIAITEM_ID_FROM_PATH_SQL = "SELECT " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata) +
        " WHERE " + _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SYSTEM_ID) + " = @SYSTEM_ID AND " +
        _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH) + " = @PATH";
      INSERT_USER_PLAYCOUNT_SQL = "INSERT INTO " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME +
        "(" + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + ", " +
        MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + ", " + UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + ", " +
        UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME + ") " +
        "SELECT " + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + ", @MEDIA_ITEM_ID, @DATA_KEY, @MEDIA_ITEM_DATA FROM " +
        UserProfileDataManagement_SubSchema.USER_TABLE_NAME;
      DELETE_MEDIAITEM_RELATIONSHIPS_FROM_ID_SQL = "DELETE FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
        " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID";
      SELECT_ORPHAN_COUNT_SQL = "SELECT COUNT(*) FROM " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME +
        " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN (" +
        " SELECT T0." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
        " FROM " + _miaManagement.GetMIATableName(MediaAspect.Metadata) + " T0" +
        " JOIN " + _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata) + " T1 ON " +
        " T1." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = " +
        " T0." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
        " WHERE T0." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID" +
        " AND T1." + _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_TYPE) + " = " + ProviderResourceAspect.TYPE_VIRTUAL +
        " AND NOT EXISTS (" +
        "SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
        " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
        " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID" +
        " OR " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID" +
        "))";
      UPDATE_USER_PLAY_DATA_FROM_ID_SQL = "UPDATE " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME +
        " SET " + UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME + " = @USER_DATA_VALUE" +
        " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID" +
        " AND " + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + " = @USER_PROFILE_ID" +
        " AND " + UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + " = @USER_DATA_KEY";
      INSERT_USER_PLAY_DATA_FOR_ID_SQL = "INSERT INTO " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME +
        " (" + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + ", " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + ", " +
        UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + ", " + UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME + ")" +
        " VALUES (@USER_PROFILE_ID, @ITEM_ID, @USER_DATA_KEY, @USER_DATA_VALUE)";
      SELECT_MEDIAITEM_USER_DATA_FROM_IDS_SQL = "SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + "," + UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + "," +
        UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME +
        " FROM " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME +
        " WHERE " + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + "=@PROFILE_ID AND " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN({0})";
    }

    public void ActivateImporterWorker()
    {
      PrepareDatabaseQueries(); //Second prepare
      InitShareWatchers();

      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();
      importerWorker.Activate(_mediaBrowsingCallback, _importResultHandler);
    }

    public void Shutdown()
    {
      _shutdown = true;
      NotifySystemOffline(_localSystemId);
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();
      importerWorker.Suspend();
      DeInitShareWatchers();
    }

    protected bool ShuttingDown
    {
      get
      {
        if (_shutdown)
          return true;
        if (ServiceRegistration.IsShuttingDown)
          return true;

        return false;
      }
    }

    #endregion

    #region Media query

    public MediaItemQuery BuildSimpleTextSearchQuery(string searchText, IEnumerable<Guid> necessaryMIATypes,
        IEnumerable<Guid> optionalMIATypes, IFilter filter, bool includeCLOBs, bool caseSensitive)
    {
      IFilter resultFilter;
      if (string.IsNullOrEmpty(searchText))
        resultFilter = new FalseFilter();
      else
      {
        IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
        ICollection<IFilter> textFilters = new List<IFilter>();
        ICollection<MediaItemAspectMetadata> types = new HashSet<MediaItemAspectMetadata>();
        if (necessaryMIATypes != null)
          foreach (Guid miaType in necessaryMIATypes)
            types.Add(miatr.LocallyKnownMediaItemAspectTypes[miaType]);
        if (optionalMIATypes != null)
          foreach (Guid miaType in optionalMIATypes)
            types.Add(miatr.LocallyKnownMediaItemAspectTypes[miaType]);
        if (types.Count == 0)
          types = miatr.LocallyKnownMediaItemAspectTypes.Values;
        foreach (MediaItemAspectMetadata miaType in types)
          foreach (MediaItemAspectMetadata.AttributeSpecification attrType in miaType.AttributeSpecifications.Values)
            if (attrType.AttributeType == typeof(string) &&
                attrType.MaxNumChars >= searchText.Length &&
                (includeCLOBs || !_miaManagement.IsCLOBAttribute(attrType)))
              textFilters.Add(new LikeFilter(attrType, "%" + SqlUtils.LikeEscape(searchText, ESCAPE_CHAR) + "%", ESCAPE_CHAR, caseSensitive));
        resultFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
            new BooleanCombinationFilter(BooleanOperator.Or, textFilters), filter);
      }
      return new MediaItemQuery(necessaryMIATypes, optionalMIATypes, resultFilter);
    }

    public void RefreshMediaItemMetadata(string systemId, Guid mediaItemId, bool clearMetadata)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();

      try
      {
        List<Guid> necessaryAspects = new List<Guid>();
        necessaryAspects.Add(ProviderResourceAspect.ASPECT_ID);

        List<Guid> optionalAspects = new List<Guid>(GetManagedMediaItemAspectMetadata().Keys);
        optionalAspects.Remove(ProviderResourceAspect.ASPECT_ID);
        optionalAspects.Remove(ExternalIdentifierAspect.ASPECT_ID);
        optionalAspects.Remove(MediaAspect.ASPECT_ID);
        optionalAspects.Remove(ImporterAspect.ASPECT_ID);

        //Find media item
        var loadItemQuery = BuildLoadItemQuery(systemId, mediaItemId);
        loadItemQuery.SetNecessaryRequestedMIATypeIDs(necessaryAspects);
        loadItemQuery.SetOptionalRequestedMIATypeIDs(optionalAspects);
        CompiledMediaItemQuery cmiq = CompiledMediaItemQuery.Compile(_miaManagement, loadItemQuery);
        var items = cmiq.QueryList(database, transaction);

        if (items != null && items.Count == 1)
        {
          if (clearMetadata)
          {
            //Remove relationships
            using (IDbCommand command = transaction.CreateCommand())
            {
              database.AddParameter(command, "ITEM_ID", mediaItemId, typeof(Guid));

              //Find relations
              List<Guid> relations = new List<Guid>();
              command.CommandText = SELECT_MEDIAITEM_RELATIONSHIPS_FROM_ID_SQL;
              using (IDataReader reader = command.ExecuteReader())
              {
                while (reader.Read())
                {
                  Guid relationId = database.ReadDBValue<Guid>(reader, 0);
                  if (!relations.Contains(relationId))
                    relations.Add(relationId);
                }
              }
              Logger.Debug("MediaLibrary: Delete media item {0} relations {1}", mediaItemId, relations.Count);

              //Delete relations
              command.CommandText = DELETE_MEDIAITEM_RELATIONSHIPS_FROM_ID_SQL;
              command.ExecuteNonQuery();

              //Delete orphaned relations
              foreach (Guid relationId in relations)
                DeleteOrphan(database, transaction, relationId);

              _miaManagement.CleanupAllOrphanedAttributeValues(transaction);
            }

            //Remove MIAs
            foreach (Guid aspect in items[0].Aspects.Keys)
            {
              _miaManagement.RemoveMIA(transaction, mediaItemId, aspect);
            }
          }

          //Set media item as changed
          MediaItemAspect importerAspect = _miaManagement.GetMediaItemAspect(transaction, mediaItemId, ImporterAspect.ASPECT_ID);
          importerAspect.SetAttribute(ImporterAspect.ATTR_DIRTY, false);
          importerAspect.SetAttribute(ImporterAspect.ATTR_LAST_IMPORT_DATE, importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_DATEADDED).AddDays(-1));
          _miaManagement.AddOrUpdateMIA(transaction, mediaItemId, importerAspect, false);

          //Find share
          var shares = GetShares(systemId);
          var resources = items[0].PrimaryResources;
          if (resources.Count > 0)
          {
            foreach (var share in shares.Values)
            {
              foreach (var resource in resources)
              {
                string accessorPath = (string)resource.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
                ResourcePath resourcePath = ResourcePath.Deserialize(accessorPath);

                if (share.BaseResourcePath.IsParentOf(resourcePath))
                {
                  TryScheduleLocalShareRefresh(share);
                  return;
                }
              }
            }
          }
        }
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error refreshing media item {0}", e, mediaItemId);
        throw;
      }
    }

    public MediaItem LoadItem(string systemId, ResourcePath path,
        IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs, Guid? userProfileId = null)
    {
      return LoadItem(BuildLoadItemQuery(systemId, path), necessaryRequestedMIATypeIDs, optionalRequestedMIATypeIDs, userProfileId);
    }

    public MediaItem LoadItem(string systemId, Guid mediaItemId,
        IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs, Guid? userProfileId = null)
    {
      return LoadItem(BuildLoadItemQuery(systemId, mediaItemId), necessaryRequestedMIATypeIDs, optionalRequestedMIATypeIDs, userProfileId);
    }

    public MediaItem LoadItem(MediaItemQuery loadItemQuery, IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs, Guid? userProfileId = null)
    {
      lock (_syncObj)
      {
        // The following lines are a temporary workaround for the fact that our MainQueryBuilder doesn't like
        // queries without necessary requested MIAs. We therefore add the ProviderResourceAspect as necessary
        // requested MIA, which doesn't hurt, because in LoadItem we have the ProviderResourceAspect in the
        // WHERE clause anyway to check for PATH and SYSTEM_ID. We only do this of course, if the ProvierResourceAspect
        // wasn't included in necessaryRequestedMIATypeIDs anyway, and if we did it, we remove the ProvierResourceAspect
        // from the result before returning it. This improves the query performance for SQLite by a factor of up to 400.
        // For details see: http://forum.team-mediaportal.com/threads/speed-improvements-for-the-medialibrary-with-very-large-databases.127220/page-17#post-1097294
        // ToDo: Rework the MainQueryBuilder to make this happen automatically.
        var removeProviderResourceAspect = false;
        var necessaryRequestedMIATypeIDsWithProvierResourceAspect = (necessaryRequestedMIATypeIDs == null) ? new List<Guid>() : necessaryRequestedMIATypeIDs.ToList();
        if (!necessaryRequestedMIATypeIDsWithProvierResourceAspect.Contains(ProviderResourceAspect.ASPECT_ID))
        {
          //MP2-706: Don't remove the provider resource aspect if optionalRequestedMIATypeIDs contains the aspect
          removeProviderResourceAspect = !IsMiaTypeRequested(ProviderResourceAspect.ASPECT_ID, necessaryRequestedMIATypeIDs, optionalRequestedMIATypeIDs);
          necessaryRequestedMIATypeIDsWithProvierResourceAspect.Add(ProviderResourceAspect.ASPECT_ID);
        }

        loadItemQuery.SetNecessaryRequestedMIATypeIDs(necessaryRequestedMIATypeIDsWithProvierResourceAspect);
        loadItemQuery.SetOptionalRequestedMIATypeIDs(optionalRequestedMIATypeIDs);
        CompiledMediaItemQuery cmiq = CompiledMediaItemQuery.Compile(_miaManagement, loadItemQuery, userProfileId);
        var result = cmiq.QueryMediaItem();

        // This is the second part of the rework as decribed above (remove ProviderResourceAspect if it wasn't requested)
        if (removeProviderResourceAspect && result != null)
          result.Aspects.Remove(ProviderResourceAspect.ASPECT_ID);

        LoadUserDataForMediaItem(userProfileId, result);

        return result;
      }
    }

    public IList<MediaItem> Browse(Guid parentDirectoryId,
        IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs,
        Guid? userProfileId, bool includeVirtual, uint? offset = null, uint? limit = null)
    {
      lock (_syncObj)
      {
        MediaItemQuery browseQuery = BuildBrowseQuery(parentDirectoryId);
        browseQuery.SetNecessaryRequestedMIATypeIDs(necessaryRequestedMIATypeIDs);
        browseQuery.SetOptionalRequestedMIATypeIDs(optionalRequestedMIATypeIDs);
        browseQuery.Limit = limit;
        browseQuery.Offset = offset;
        return Search(browseQuery, false, userProfileId, includeVirtual);
      }
    }

    public void MarkUpdatableMediaItems()
    {
      try
      {
        lock (_syncObj)
        {
          Stopwatch swImport = new Stopwatch();
          swImport.Start();
          ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
          IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
          List<Guid> requiredAspects = new List<Guid>(new Guid[] { MediaAspect.ASPECT_ID }); ;

          foreach (IRelationshipExtractor extractor in mediaAccessor.LocalRelationshipExtractors.Values)
          {
            var changeFilters = extractor.GetLastChangedItemsFilters();
            if (changeFilters == null || changeFilters.Count == 0)
              continue;

            Logger.Info("{0} marking all updateable media items ({1} ms)", extractor.GetType().Name, swImport.ElapsedMilliseconds);

            using (ITransaction transaction = database.BeginTransaction())
            {
              using (IDbCommand command = transaction.CreateCommand())
              {
                int itemCount = 0;
                foreach (var changeFilter in changeFilters)
                {
                  MediaItemQuery changeQuery = new MediaItemQuery(requiredAspects, changeFilter.Key);
                  if (changeFilter.Value > 0)
                    changeQuery.Limit = changeFilter.Value;
                  IList<MediaItem> foundItems = Search(database, transaction, changeQuery, false, null, false);
                  if (foundItems != null)
                  {
                    int currentItem = 0;
                    List<Guid> miUpdateList = new List<Guid>();
                    while (currentItem < foundItems.Count)
                    {
                      int remaining = foundItems.Count - currentItem;
                      int endItem = currentItem + (remaining > MAX_VARIABLES_LIMIT ? MAX_VARIABLES_LIMIT : remaining);
                      command.Parameters.Clear();
                      for (int index = currentItem; index < endItem; index++)
                        database.AddParameter(command, "MI" + index, foundItems[index].MediaItemId, typeof(Guid));
                      command.CommandText = string.Format(UPDATE_MEDIAITEMS_DIRTY_ATTRIBUTE_FROM_ID_SQL,
                        string.Join(",", foundItems.Where((id, index) => index >= currentItem && index < endItem).Select((id, index) => "@MI" + index)));
                      command.ExecuteNonQuery();
                      itemCount += (endItem - currentItem);
                      currentItem = endItem;
                    }
                  }
                }
                transaction.Commit();
                extractor.ResetLastChangedItems(); //Reset changes so they are not found again in next request

                if (itemCount > 0)
                  Logger.Info("{0} found {1} updatable media items ({2} ms)", extractor.GetType().Name, itemCount, swImport.ElapsedMilliseconds);
              }
            }
          }
        }
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error marking updated media items", e);
        throw;
      }
    }

    public IList<MediaItem> Search(MediaItemQuery query, bool filterOnlyOnline, Guid? userProfileId, bool includeVirtual)
    {
      return Search(null, null, query, filterOnlyOnline, userProfileId, includeVirtual);
    }

    public IList<MediaItem> Search(ISQLDatabase database, ITransaction transaction, MediaItemQuery query, bool filterOnlyOnline, Guid? userProfileId, bool includeVirtual)
    {
      IList<MediaItem> items = new List<MediaItem>();

      // We add the provider resource aspect to the necessary aspect types be able to filter online systems
      MediaItemQuery executeQuery = query;
      IFilter additionalFilter = null;
      if (filterOnlyOnline || !includeVirtual)
      {
        executeQuery = new MediaItemQuery(query); // Use constructor by other query to make sure all properties are copied (including sorting and limits)
        if (filterOnlyOnline)
          executeQuery.NecessaryRequestedMIATypeIDs.Add(ProviderResourceAspect.ASPECT_ID);
        if (!includeVirtual)
          executeQuery.NecessaryRequestedMIATypeIDs.Add(MediaAspect.ASPECT_ID);

        additionalFilter = CreateAddtionalFilter(filterOnlyOnline, includeVirtual);
        executeQuery.Filter = executeQuery.Filter != null ?
          BooleanCombinationFilter.CombineFilters(BooleanOperator.And, executeQuery.Filter, additionalFilter) : additionalFilter;
        executeQuery.SubqueryFilter = executeQuery.SubqueryFilter != null ?
          BooleanCombinationFilter.CombineFilters(BooleanOperator.And, executeQuery.SubqueryFilter, additionalFilter) : additionalFilter;
      }

      if (database == null)
        database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction searchTransaction = transaction;
      if (transaction == null)
        searchTransaction = database.BeginTransaction();

      try
      {
        CompiledMediaItemQuery cmiq = CompiledMediaItemQuery.Compile(_miaManagement, executeQuery, userProfileId);
        items = cmiq.QueryList(database, searchTransaction);
        //Logger.Debug("Found media items {0}", string.Join(",", items.Select(x => x.MediaItemId)));
        //TODO: Remove movies/series found through optional aspects that are not allowed according to user rating filter
        LoadUserDataForMediaItems(database, searchTransaction, userProfileId, items);
      }
      finally
      {
        if (transaction == null)
          searchTransaction.Dispose();
      }
      if (filterOnlyOnline && !IsMiaTypeRequested(ProviderResourceAspect.ASPECT_ID, query.NecessaryRequestedMIATypeIDs, query.OptionalRequestedMIATypeIDs))
      {
        // The provider resource aspect was not requested and thus has to be removed from the result items
        foreach (MediaItem item in items)
          item.Aspects.Remove(ProviderResourceAspect.ASPECT_ID);
      }
      if (!includeVirtual && !IsMiaTypeRequested(MediaAspect.ASPECT_ID, query.NecessaryRequestedMIATypeIDs, query.OptionalRequestedMIATypeIDs))
      { // The media aspect was not requested and thus has to be removed from the result items
        foreach (MediaItem item in items)
          item.Aspects.Remove(MediaAspect.ASPECT_ID);
      }

      return items;
    }

    public HomogenousMap GetValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType, IFilter selectAttributeFilter,
        ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypeIDs, IFilter filter, bool filterOnlyOnline, bool includeVirtual)
    {
      SelectProjectionFunction selectProjectionFunctionImpl;
      Type projectionValueType;
      switch (projectionFunction)
      {
        case ProjectionFunction.DateToYear:
          selectProjectionFunctionImpl = (string expr) =>
            {
              ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
              return database.CreateDateToYearProjectionExpression(expr);
            };
          projectionValueType = typeof(int);
          break;
        default:
          selectProjectionFunctionImpl = null;
          projectionValueType = null;
          break;
      }
      IAttributeFilter saf = selectAttributeFilter as IAttributeFilter;
      if (saf == null && selectAttributeFilter != null)
        filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, new IFilter[] { filter, selectAttributeFilter });

      IFilter additionalFilter = CreateAddtionalFilter(filterOnlyOnline, includeVirtual);
      if (additionalFilter != null)
        filter = filter != null ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, additionalFilter) : additionalFilter;

      CompiledGroupedAttributeValueQuery cdavq = CompiledGroupedAttributeValueQuery.Compile(_miaManagement,
          filterOnlyOnline ? necessaryMIATypeIDs.Union(new Guid[] { ProviderResourceAspect.ASPECT_ID }) : necessaryMIATypeIDs,
          attributeType, saf, selectProjectionFunctionImpl, projectionValueType,
          filter, additionalFilter);
      return cdavq.Execute().Item1;
    }

    public Tuple<HomogenousMap, HomogenousMap> GetKeyValueGroups(MediaItemAspectMetadata.AttributeSpecification keyAttributeType, MediaItemAspectMetadata.AttributeSpecification valueAttributeType,
      IFilter selectAttributeFilter, ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypeIDs, IFilter filter, bool filterOnlyOnline, bool includeVirtual)
    {
      SelectProjectionFunction selectProjectionFunctionImpl;
      Type projectionValueType;
      switch (projectionFunction)
      {
        case ProjectionFunction.DateToYear:
          selectProjectionFunctionImpl = (string expr) =>
          {
            ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
            return database.CreateDateToYearProjectionExpression(expr);
          };
          projectionValueType = typeof(int);
          break;
        default:
          selectProjectionFunctionImpl = null;
          projectionValueType = null;
          break;
      }
      IAttributeFilter saf = selectAttributeFilter as IAttributeFilter;
      if (saf == null && selectAttributeFilter != null)
        filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, new IFilter[] { filter, selectAttributeFilter });

      IFilter additionalFilter = CreateAddtionalFilter(filterOnlyOnline, includeVirtual);
      if (additionalFilter != null)
        filter = filter != null ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, additionalFilter) : additionalFilter;

      CompiledGroupedAttributeValueQuery cdavq = CompiledGroupedAttributeValueQuery.Compile(_miaManagement,
          filterOnlyOnline ? necessaryMIATypeIDs.Union(new Guid[] { ProviderResourceAspect.ASPECT_ID }) : necessaryMIATypeIDs,
          keyAttributeType, valueAttributeType, saf, selectProjectionFunctionImpl, projectionValueType,
          filter, additionalFilter);
      return cdavq.Execute();
    }

    public IList<MLQueryResultGroup> GroupValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IFilter selectAttributeFilter, ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypeIDs,
        IFilter filter, bool filterOnlyOnline, GroupingFunction groupingFunction, bool includeVirtual)
    {
      IDictionary<object, MLQueryResultGroup> groups = new Dictionary<object, MLQueryResultGroup>();
      IGroupingFunctionImpl groupingFunctionImpl;
      switch (groupingFunction)
      {
        case GroupingFunction.FirstCharacter:
          groupingFunctionImpl = new FirstCharGroupingFunction(attributeType);
          break;
        default:
          groupingFunctionImpl = new FirstCharGroupingFunction(attributeType);
          break;
      }
      foreach (KeyValuePair<object, object> resultItem in GetValueGroups(attributeType, selectAttributeFilter,
          projectionFunction, necessaryMIATypeIDs, filter, filterOnlyOnline, includeVirtual))
      {
        object valueGroupKey = resultItem.Key;
        int resultGroupItemCount = (int)resultItem.Value;
        object groupKey;
        IFilter additionalFilter;
        groupingFunctionImpl.GetGroup(valueGroupKey, out groupKey, out additionalFilter);
        MLQueryResultGroup rg;
        if (groups.TryGetValue(groupKey, out rg))
          rg.NumItemsInGroup += resultGroupItemCount;
        else
          groups[groupKey] = new MLQueryResultGroup(groupKey, resultGroupItemCount, additionalFilter);
      }
      List<MLQueryResultGroup> result = new List<MLQueryResultGroup>(groups.Values);
      result.Sort((a, b) => groupingFunctionImpl.Compare(a.GroupKey, b.GroupKey));
      return result;
    }

    public int CountMediaItems(IEnumerable<Guid> necessaryMIATypeIDs, IFilter filter, bool filterOnlyOnline, bool includeVirtual)
    {
      IFilter additionalFilter = CreateAddtionalFilter(filterOnlyOnline, includeVirtual);
      if (additionalFilter != null)
        filter = filter != null ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, additionalFilter) : additionalFilter;

      CompiledCountItemsQuery cciq = CompiledCountItemsQuery.Compile(_miaManagement,
          necessaryMIATypeIDs, filter, additionalFilter);
      return cciq.Execute();
    }

    private void LoadUserDataForMediaItem(Guid? userProfileId, MediaItem mediaItem)
    {
      LoadUserDataForMediaItems(null, null, userProfileId, new[] { mediaItem });
    }

    private void LoadUserDataForMediaItems(ISQLDatabase database, ITransaction transaction, Guid? userProfileId, IList<MediaItem> mediaItems)
    {
      if (!userProfileId.HasValue)
        return;

      if (database == null)
        database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction loadTransaction = transaction;
      if (transaction == null)
        loadTransaction = database.BeginTransaction();

      try
      {
        if (mediaItems != null)
        {
          int currentItem = 0;
          using (IDbCommand command = loadTransaction.CreateCommand())
          {
            while (currentItem < mediaItems.Count)
            {
              int remaining = mediaItems.Count - currentItem;
              int endItem = currentItem + (remaining > MAX_VARIABLES_LIMIT ? MAX_VARIABLES_LIMIT : remaining);
              command.Parameters.Clear();
              database.AddParameter(command, "PROFILE_ID", userProfileId.Value, typeof(Guid));
              for (int index = currentItem; index < endItem; index++)
                database.AddParameter(command, "MI" + index, mediaItems[index].MediaItemId, typeof(Guid));
              command.CommandText = string.Format(SELECT_MEDIAITEM_USER_DATA_FROM_IDS_SQL,
                string.Join(",", mediaItems.Where((id, index) => index >= currentItem && index < endItem).Select((id, index) => "@MI" + (index + currentItem))));
              using (IDataReader reader = command.ExecuteReader())
              {
                while (reader.Read())
                {
                  MediaItem item = mediaItems.FirstOrDefault(mi => mi.MediaItemId == database.ReadDBValue<Guid>(reader, 0));
                  if (item != null)
                    item.UserData.Add(database.ReadDBValue<string>(reader, 1), database.ReadDBValue<string>(reader, 2));
                }
              }
              currentItem = endItem;
            }
          }
        }
      }
      finally
      {
        if (transaction == null)
          loadTransaction.Dispose();
      }
    }

    #endregion

    #region Playlist management

    public ICollection<PlaylistInformationData> GetPlaylists()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int playlistIdIndex;
        int nameIndex;
        int playlistTypeIndex;
        int playlistItemsCountIndex;
        using (IDbCommand command = MediaLibrary_SubSchema.SelectPlaylistsCommand(transaction,
            out playlistIdIndex, out nameIndex, out playlistTypeIndex, out playlistItemsCountIndex))
        {
          ICollection<PlaylistInformationData> result = new List<PlaylistInformationData>();
          using (IDataReader reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              Guid playlistId = database.ReadDBValue<Guid>(reader, playlistIdIndex);
              string name = database.ReadDBValue<string>(reader, nameIndex);
              string playlistType = database.ReadDBValue<string>(reader, playlistTypeIndex);
              int playlistItemsCount = database.ReadDBValue<int>(reader, playlistItemsCountIndex);
              result.Add(new PlaylistInformationData(playlistId, name, playlistType, playlistItemsCount));
            }
          }
          return result;
        }
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public void SavePlaylist(PlaylistRawData playlistData)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      Guid playlistId = playlistData.PlaylistId;
      try
      {
        // Playlist contents are automatically deleted (ON DELETE CASCADE)
        using (IDbCommand command = MediaLibrary_SubSchema.DeletePlaylistCommand(transaction, playlistId))
          command.ExecuteNonQuery();

        using (IDbCommand command = MediaLibrary_SubSchema.InsertPlaylistCommand(transaction, playlistId, playlistData.Name, playlistData.PlaylistType))
          command.ExecuteNonQuery();

        // Add media items
        int ct = 0;
        foreach (Guid mediaItemId in playlistData.MediaItemIds)
          using (IDbCommand command = MediaLibrary_SubSchema.AddMediaItemToPlaylistCommand(transaction, playlistId, ct++, mediaItemId))
            command.ExecuteNonQuery();
        transaction.Commit();
        ContentDirectoryMessaging.SendPlaylistsChangedMessage();
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error adding playlist '{0}' (id '{1}')", e, playlistData.Name, playlistId);
        transaction.Rollback();
        throw;
      }
    }

    public bool DeletePlaylist(Guid playlistId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool plExists = false;
        int playlistNameIndex;
        int playlistTypeIndex;
        using (IDbCommand command = MediaLibrary_SubSchema.SelectPlaylistIdentificationDataCommand(transaction, playlistId,
            out playlistNameIndex, out playlistTypeIndex))
        using (IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
          if (reader.Read())
            plExists = true;
        if (!plExists)
        {
          transaction.Dispose();
          return false;
        }
        // Playlist contents are automatically deleted (ON DELETE CASCADE)
        using (IDbCommand command = MediaLibrary_SubSchema.DeletePlaylistCommand(transaction, playlistId))
          command.ExecuteNonQuery();
        transaction.Commit();
        ContentDirectoryMessaging.SendPlaylistsChangedMessage();
        return true;
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error deleting playlist '{0}'", e, playlistId);
        transaction.Rollback();
        throw;
      }
    }

    public PlaylistRawData ExportPlaylist(Guid playlistId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int nameIndex;
        int playlistTypeIndex;
        string name;
        string playlistType;
        using (IDbCommand command = MediaLibrary_SubSchema.SelectPlaylistIdentificationDataCommand(transaction, playlistId, out nameIndex, out playlistTypeIndex))
        using (IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
          if (reader.Read())
          {
            name = database.ReadDBValue<string>(reader, nameIndex);
            playlistType = database.ReadDBValue<string>(reader, playlistTypeIndex);
          }
          else
            return null;

        IList<Guid> mediaItemIds = new List<Guid>();
        int mediaItemIdIndex;
        using (IDbCommand command = MediaLibrary_SubSchema.SelectPlaylistContentsCommand(transaction, playlistId, out mediaItemIdIndex))
        using (IDataReader reader = command.ExecuteReader())
          while (reader.Read())
            mediaItemIds.Add(database.ReadDBValue<Guid>(reader, mediaItemIdIndex));
        return new PlaylistRawData(playlistId, name, playlistType, mediaItemIds);
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public IList<MediaItem> LoadCustomPlaylist(IList<Guid> mediaItemIds,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes, uint? offset = null, uint? limit = null)
    {
      IFilter filter = new MediaItemIdFilter(mediaItemIds);
      MediaItemQuery query = new MediaItemQuery(necessaryMIATypes, optionalMIATypes, filter);
      query.Limit = limit;
      query.Offset = offset;
      // Sort media items
      IDictionary<Guid, MediaItem> searchResult = new Dictionary<Guid, MediaItem>();
      foreach (MediaItem item in Search(query, false, null, false))
        searchResult[item.MediaItemId] = item;
      IList<MediaItem> result = new List<MediaItem>(searchResult.Count);
      foreach (Guid mediaItemId in mediaItemIds)
      {
        MediaItem item;
        if (searchResult.TryGetValue(mediaItemId, out item))
          result.Add(item);
      }
      return result;
    }

    #endregion

    #region Media import

    public Guid AddOrUpdateMediaItem(Guid parentDirectoryId, string systemId, ResourcePath path, IEnumerable<MediaItemAspect> mediaItemAspects, bool isRefresh)
    {
      return AddOrUpdateMediaItem(parentDirectoryId, systemId, path, null, null, mediaItemAspects, true, isRefresh, CancellationToken.None);
    }

    public Guid AddOrUpdateMediaItem(Guid parentDirectoryId, string systemId, ResourcePath path, Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects, bool isRefresh)
    {
      return AddOrUpdateMediaItem(parentDirectoryId, systemId, path, mediaItemId, null, mediaItemAspects, true, isRefresh, CancellationToken.None);
    }

    private string GetMediaItemTitle(IEnumerable<MediaItemAspect> mediaItemAspects, string defaultTitle)
    {
      foreach (MediaItemAspect mia in mediaItemAspects)
      {
        if (mia.Metadata.AspectId == MediaAspect.ASPECT_ID)
        {
          return mia.GetAttributeValue<string>(MediaAspect.ATTR_TITLE);
        }
      }
      return defaultTitle;
    }

    private bool HasStubResource(IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      foreach (MediaItemAspect mia in mediaItemAspects)
      {
        if (mia.Metadata.AspectId == ProviderResourceAspect.ASPECT_ID)
        {
          int resourceType = mia.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE);
          if (resourceType == ProviderResourceAspect.TYPE_STUB)
            return true;
        }
      }
      return false;
    }

    private void TransferTransientAspects(IEnumerable<MediaItemAspect> sourceMediaItemAspects, MediaItem destinationMediaItem)
    {
      foreach (MediaItemAspect mia in sourceMediaItemAspects)
      {
        if (mia.Metadata.IsTransientAspect)
        {
          if (!destinationMediaItem.Aspects.ContainsKey(mia.Metadata.AspectId))
            destinationMediaItem.Aspects.Add(mia.Metadata.AspectId, new List<MediaItemAspect>());
          destinationMediaItem.Aspects[mia.Metadata.AspectId].Add(mia);
        }
      }
    }

    private IDictionary<Guid, IList<MediaItemAspect>> ConvertAspects(IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      IDictionary<Guid, IList<MediaItemAspect>> extractedAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      foreach (MediaItemAspect aspect in mediaItemAspects)
      {
        if (!extractedAspects.ContainsKey(aspect.Metadata.AspectId))
          extractedAspects.Add(aspect.Metadata.AspectId, new List<MediaItemAspect>());

        extractedAspects[aspect.Metadata.AspectId].Add(aspect);
      }
      return extractedAspects;
    }

    private Guid AddOrUpdateMediaItem(Guid parentDirectoryId, string systemId, ResourcePath path, Guid? existingMediaItemId, Guid? newMediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects, bool reconcile, bool isRefresh, CancellationToken cancelToken)
    {
      Stopwatch swImport = new Stopwatch();
      swImport.Start();

      //Logger.Debug("Adding to {0} on {1} in {2}:\n{3}", parentDirectoryId, systemId, path, MediaItemAspect.GetInfo(mediaItemAspects, _miaManagement.ManagedMediaItemAspectTypes));

      // TODO: Avoid multiple write operations to the same media item
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        if (isRefresh)
        {
          Guid? refreshMediaItemId = existingMediaItemId.HasValue ? existingMediaItemId : GetMediaItemId(transaction, systemId, path);
          IDictionary<Guid, IList<MediaItemAspect>> extractedAspects = ConvertAspects(mediaItemAspects);
          bool dirty = false;
          if (extractedAspects.ContainsKey(ImporterAspect.ASPECT_ID))
            dirty = extractedAspects[ImporterAspect.ASPECT_ID][0].GetAttributeValue<bool>(ImporterAspect.ATTR_DIRTY);
          if (!dirty && refreshMediaItemId.HasValue)
          {
            transaction.Commit();

            if (extractedAspects.ContainsKey(DirectoryAspect.ASPECT_ID))
              return refreshMediaItemId.Value;

            if (reconcile)
              Reconcile(refreshMediaItemId.Value, extractedAspects, isRefresh, cancelToken);

            //Set media item as refreshed
            using (transaction = database.BeginTransaction())
            {
              MediaItemAspect importerAspect = _miaManagement.GetMediaItemAspect(transaction, refreshMediaItemId.Value, ImporterAspect.ASPECT_ID);
              importerAspect.SetAttribute(ImporterAspect.ATTR_DIRTY, false);
              importerAspect.SetAttribute(ImporterAspect.ATTR_LAST_IMPORT_DATE, DateTime.Now);
              _miaManagement.AddOrUpdateMIA(transaction, refreshMediaItemId.Value, importerAspect, false);
              transaction.Commit();
            }

            Logger.Info("Refreshed media item {0} ({1}) ({2} ms)", refreshMediaItemId.Value, Path.GetFileName(path.FileName), swImport.ElapsedMilliseconds);
            MediaLibraryMessaging.SendMediaItemAddedOrUpdatedMessage(new MediaItem(refreshMediaItemId.Value, extractedAspects));
            return refreshMediaItemId.Value;
          }
        }

        bool merged;
        string name = GetMediaItemTitle(mediaItemAspects, path.FileName);
        Guid? mediaItemId = AddOrUpdateMediaItem(database, transaction, parentDirectoryId, systemId, path, existingMediaItemId, newMediaItemId, mediaItemAspects, out merged);
        if (!mediaItemId.HasValue || mediaItemId.Value == Guid.Empty)
        {
          transaction.Rollback();
          return Guid.Empty;
        }
        transaction.Commit();

        if (!merged)
        {
          MediaItem item = Search(new MediaItemQuery(null, GetManagedMediaItemAspectMetadata().Keys, new MediaItemIdFilter(mediaItemId.Value)), false, null, true).FirstOrDefault();
          if (item != null)
          {
            //Transfer any transient aspects
            TransferTransientAspects(mediaItemAspects, item);

            bool cancel = cancelToken.IsCancellationRequested;
            try
            {
              if (reconcile && !cancel)
                Reconcile(item.MediaItemId, item.Aspects, isRefresh, cancelToken);
            }
            catch (Exception e)
            {
              Logger.Error("MediaLibrary: Error reconciling media item(s) in path '{0}'", e, (path != null ? path.Serialize() : null));
              cancel = true;
            }
            if (cancelToken.IsCancellationRequested)
              cancel = true;

            if (cancel)
            {
              //Delete media item so it can be reimported later
              transaction = database.BeginTransaction();
              _relationshipManagement.DeleteMediaItemAndRelationships(transaction, mediaItemId.Value);
              transaction.Commit();
              MediaLibraryMessaging.SendMediaItemsDeletedMessage();
              Logger.Info("Deleted media item {0} with name {1} ({2}) so it can be reimported ({3} ms)", mediaItemId.Value, name, Path.GetFileName(path.FileName), swImport.ElapsedMilliseconds);
              return Guid.Empty;
            }
            
            MediaLibraryMessaging.SendMediaItemAddedOrUpdatedMessage(item);
          }

          Logger.Info("Media item {0} with name {1} ({2}) imported ({3} ms)", mediaItemId.Value, name, Path.GetFileName(path.FileName), swImport.ElapsedMilliseconds);
        }
        return mediaItemId.Value;
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error adding or updating media item(s) in path '{0}'", e, (path != null ? path.Serialize() : null));
        transaction.Rollback();
        return Guid.Empty;
      }
    }

    private Guid AddOrUpdateMediaItem(ISQLDatabase database, ITransaction transaction, Guid parentDirectoryId, string systemId, ResourcePath path, Guid? existingMediaItemId, Guid? newMediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects, out bool merged)
    {
      Stopwatch swImport = new Stopwatch();
      swImport.Start();
      merged = false;
      string name = GetMediaItemTitle(mediaItemAspects, path.FileName);
      Guid? mediaItemId = null;
      if (existingMediaItemId.HasValue || !HasStubResource(mediaItemAspects))
        mediaItemId = existingMediaItemId.HasValue ? existingMediaItemId : GetMediaItemId(transaction, systemId, path);
      mediaItemAspects = RemoveInverseRelationships(mediaItemAspects);
      Logger.Debug("Adding media item {0} with name {1} ({2})", mediaItemId.HasValue ? mediaItemId : newMediaItemId, name, Path.GetFileName(path.FileName));

      DateTime now = DateTime.Now;
      MediaItemAspect pra;
      MediaItemAspect importerAspect;
      bool wasCreated = !mediaItemId.HasValue;
      if (wasCreated)
      {
        pra = new MultipleMediaItemAspect(ProviderResourceAspect.Metadata);
        pra.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
        if (path.BasePathSegment.ProviderId == VirtualResourceProvider.VIRTUAL_RESOURCE_PROVIDER_ID)
          pra.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_VIRTUAL);
        else
          pra.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
        pra.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, systemId);
        pra.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, path.Serialize());
        pra.SetAttribute(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID, parentDirectoryId);

        importerAspect = new SingleMediaItemAspect(ImporterAspect.Metadata);
        importerAspect.SetAttribute(ImporterAspect.ATTR_DATEADDED, now);
      }
      else
      {
        importerAspect = _miaManagement.GetMediaItemAspect(transaction, mediaItemId.Value, ImporterAspect.ASPECT_ID);
        pra = _miaManagement.GetMediaItemAspect(transaction, mediaItemId.Value, ProviderResourceAspect.ASPECT_ID);
      }

      Guid? mergedMediaItem = null;
      if (path.BasePathSegment.ProviderId != VirtualResourceProvider.VIRTUAL_RESOURCE_PROVIDER_ID)
        mergedMediaItem = MergeWithExisting(database, transaction, mediaItemId, mediaItemAspects, pra);

      if (mergedMediaItem != null)
      {
        merged = true;
        if (mergedMediaItem == Guid.Empty)
        {
          Logger.Info("Media item {0} with name {1} ({2}) cannot be saved. Needs to be merged ({3} ms)", mediaItemId.HasValue ? mediaItemId : newMediaItemId, name, Path.GetFileName(path.FileName), swImport.ElapsedMilliseconds);

          return Guid.Empty;
        }

        if (mediaItemId.HasValue && mergedMediaItem.Value != mediaItemId.Value)
        {
          _relationshipManagement.DeleteMediaItemAndRelationships(transaction, mediaItemId.Value);
          MediaLibraryMessaging.SendMediaItemsDeletedMessage();
        }

        Logger.Info("Media item {0} with name {1} ({2}) was merged into {3} ({4} ms)", mediaItemId.HasValue ? mediaItemId : newMediaItemId, name, Path.GetFileName(path.FileName), mergedMediaItem.Value, swImport.ElapsedMilliseconds);
        return mergedMediaItem.Value;
      }

      if (wasCreated)
      {
        mediaItemId = AddMediaItem(database, transaction, newMediaItemId);
        _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, pra, true);
      }
      importerAspect.SetAttribute(ImporterAspect.ATTR_DIRTY, false);

      if (!wasCreated || path.BasePathSegment.ProviderId == VirtualResourceProvider.VIRTUAL_RESOURCE_PROVIDER_ID)
        importerAspect.SetAttribute(ImporterAspect.ATTR_LAST_IMPORT_DATE, now);
      else //Set last import date so it will be included in a refresh
        importerAspect.SetAttribute(ImporterAspect.ATTR_LAST_IMPORT_DATE, now.AddDays(-1));

      _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, importerAspect, wasCreated);

      // Update
      foreach (MediaItemAspect mia in mediaItemAspects)
      {
        if (!_miaManagement.ManagedMediaItemAspectTypes.ContainsKey(mia.Metadata.AspectId))
          // Simply skip unknown MIA types. All types should have been added before import.
          continue;
        if (mia.Metadata.AspectId == MediaAspect.ASPECT_ID)
        {
          // Check some attributes
          bool? isVirtual = mia.GetAttributeValue<bool?>(MediaAspect.ATTR_ISVIRTUAL);
          if (!isVirtual.HasValue)
            mia.SetAttribute(MediaAspect.ATTR_ISVIRTUAL, false);
          bool? isStub = mia.GetAttributeValue<bool?>(MediaAspect.ATTR_ISSTUB);
          if (!isStub.HasValue)
            mia.SetAttribute(MediaAspect.ATTR_ISSTUB, false);
        }
        else if (mia.Metadata.AspectId == ProviderResourceAspect.ASPECT_ID)
        {
          // Only allow certain attributes to be overridden
          mia.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_SYSTEM_ID));
          string resourcePath = mia.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
          if (string.IsNullOrEmpty(resourcePath))
            mia.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH));
          object resourceType = mia.GetAttributeValue<object>(ProviderResourceAspect.ATTR_TYPE);
          if (resourceType == null)
            mia.SetAttribute(ProviderResourceAspect.ATTR_TYPE, pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE));
          mia.SetAttribute(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID, pra.GetAttributeValue<Guid>(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID));

          _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, mia);
        }
        else if (mia.Metadata.AspectId == ImporterAspect.ASPECT_ID)
        { // Those aspects are managed by the MediaLibrary
          // When merging media items this aspect could by present so just ignore it
          // Logger.Warn("MediaLibrary.AddOrUpdateMediaItem: Client tried to update ImporterAspect");
        }
      }

      int? playCount = null;
      foreach (MediaItemAspect mia in mediaItemAspects)
      {
        if (!_miaManagement.ManagedMediaItemAspectTypes.ContainsKey(mia.Metadata.AspectId))
          // Simply skip unknown MIA types. All types should have been added before import.
          continue;
        if (mia.Metadata.AspectId == ProviderResourceAspect.ASPECT_ID)
          //Already stored
          continue;
        if (mia.Metadata.AspectId == ImporterAspect.ASPECT_ID)
          //Already stored
          continue;
        if (mia.Metadata.IsTransientAspect)
          continue;
        if (mia.Deleted)
          _miaManagement.RemoveMIA(transaction, mediaItemId.Value, mia.Metadata.AspectId);
        else if (wasCreated)
          _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, mia, true);
        else
          _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, mia);

        if (mia.Metadata.AspectId == MediaAspect.ASPECT_ID)
        {
          playCount = mia.GetAttributeValue<int?>(MediaAspect.ATTR_PLAYCOUNT);
        }
      }

      //Check if user watch count need to be updated
      if (wasCreated && playCount.HasValue && playCount.Value > 0)
      {
        //Update user watch data
        using (IDbCommand command = transaction.CreateCommand())
        {
          command.CommandText = INSERT_USER_PLAYCOUNT_SQL;
          database.AddParameter(command, "MEDIA_ITEM_ID", mediaItemId.Value, typeof(Guid));
          IDbDataParameter dataKey = database.AddParameter(command, "DATA_KEY", UserDataKeysKnown.KEY_PLAY_PERCENTAGE, typeof(string));
          IDbDataParameter dataValue = database.AddParameter(command, "MEDIA_ITEM_DATA", "100", typeof(string));
          command.ExecuteNonQuery();
          dataKey.Value = UserDataKeysKnown.KEY_PLAY_COUNT;
          dataValue.Value = playCount.Value.ToString();
          command.ExecuteNonQuery();
        }

        //Update parent watch data
        _relationshipManagement.UpdateParentPlayState(transaction, mediaItemId.Value);
      }

      Logger.Info("Media item {0} with name {1} ({2}) added/updated ({3} ms)", mediaItemId.Value, name, Path.GetFileName(path.FileName), swImport.ElapsedMilliseconds);
      return mediaItemId.Value;
    }

    private ICollection<MediaItem> GetMediaItems(ISQLDatabase database, ITransaction transaction, ICollection<Guid> mediaItemIds, IEnumerable<Guid> necessaryRequestedMIATypeIds, IEnumerable<Guid> optionalRequestedMIATypeIds, bool filterOnlyOnline, Guid? userProfileId, bool includeVirtual, bool applyUserRestrictions)
    {
      if (mediaItemIds.Count < MAX_VARIABLES_LIMIT)
        return Search(database, transaction, new MediaItemQuery(necessaryRequestedMIATypeIds, optionalRequestedMIATypeIds, new MediaItemIdFilter(mediaItemIds)), filterOnlyOnline, userProfileId, includeVirtual);

      //If mediaItemIds count is greater than MAX_VARIABLES_LIMIT 'page' the requests to avoid exceeding sqlite's max variable limit when creating the IN(id,id,...) statement
      IDictionary<Guid, MediaItem> results = new Dictionary<Guid, MediaItem>();
      int currentItem = 0;
      while (currentItem < mediaItemIds.Count)
      {
        int remaining = mediaItemIds.Count - currentItem;
        int endItem = currentItem + (remaining > MAX_VARIABLES_LIMIT ? MAX_VARIABLES_LIMIT : remaining);
        var query = new MediaItemQuery(necessaryRequestedMIATypeIds, optionalRequestedMIATypeIds,
          new MediaItemIdFilter(mediaItemIds.Where((id, index) => index >= currentItem && index < endItem)));
        foreach (var mediaItem in Search(database, transaction, query, filterOnlyOnline, userProfileId, includeVirtual))
          results[mediaItem.MediaItemId] = mediaItem;
        currentItem = endItem;
      }
      return results.Values;
    }

    protected virtual void Reconcile(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> mediaItemAspects, bool isRefresh, CancellationToken cancelToken)
    {
      UpdateRelationships(mediaItemId, mediaItemAspects, isRefresh, cancelToken);
      Logger.Debug("Reconciled media item {0}", mediaItemId);
    }

    public void UpdateMediaItem(Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects, bool isRefresh)
    {
      UpdateMediaItem(mediaItemId, mediaItemAspects, false, isRefresh, CancellationToken.None);
    }

    private void UpdateMediaItem(Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects, bool reconcile, bool isRefresh, CancellationToken cancelToken)
    {
      // TODO: Avoid multiple write operations to the same media item
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      using (ITransaction transaction = database.BeginTransaction())
      {
        try
        {
          UpdateMediaItem(database, transaction, mediaItemId, mediaItemAspects);
          transaction.Commit();
        }
        catch (Exception e)
        {
          Logger.Error("MediaLibrary: Error updating media item with id '{0}'", e, mediaItemId);
          transaction.Rollback();
          throw;
        }
      }
      if (reconcile)
      {
        MediaItem item = Search(new MediaItemQuery(null, GetManagedMediaItemAspectMetadata().Keys, new MediaItemIdFilter(mediaItemId)), false, null, true).FirstOrDefault();
        Reconcile(mediaItemId, item.Aspects, isRefresh, cancelToken);
      }
    }

    private void UpdateMediaItem(ISQLDatabase database, ITransaction transaction, Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      mediaItemAspects = RemoveInverseRelationships(mediaItemAspects);
      foreach (MediaItemAspect mia in mediaItemAspects)
      {
        if (mia.Metadata.IsTransientAspect)
          continue;
        if (!_miaManagement.ManagedMediaItemAspectTypes.ContainsKey(mia.Metadata.AspectId))
          // Simply skip unknown MIA types. All types should have been added before update.
          continue;
        if (mia.Metadata.AspectId == ImporterAspect.ASPECT_ID)
        { // Those aspects are managed by the MediaLibrary
          //Logger.Warn("MediaLibrary.AddOrUpdateMediaItem: Client tried to update ImporterAspect");
          continue;
        }
        // Let MIA management decide if it's and add or update
        _miaManagement.AddOrUpdateMIA(transaction, mediaItemId, mia);
      }
    }

    protected virtual Guid? MergeWithExisting(ISQLDatabase database, ITransaction transaction, Guid? extractedMediaItemId, IEnumerable<MediaItemAspect> extractedAspectList, MediaItemAspect extractedProviderResourceAspects)
    {
      IDictionary<Guid, IList<MediaItemAspect>> extractedAspects = ConvertAspects(extractedAspectList);
      IList<MultipleMediaItemAspect> providerResourceAspects = null;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ProviderResourceAspect.Metadata, out providerResourceAspects))
      {
        //Don't merge virtual resource
        int resType = (int)providerResourceAspects[0].GetAttributeValue(ProviderResourceAspect.ATTR_TYPE);
        if (resType == ProviderResourceAspect.TYPE_VIRTUAL)
          return null;
      }

      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      foreach (IMediaMergeHandler mergeHandler in mediaAccessor.LocalMergeHandlers.Values)
      {
        // Extracted aspects must contain all of mergeHandler.MergeableAspects
        if (mergeHandler.MergeableAspects.All(g => extractedAspects.Keys.Contains(g)))
        {
          Guid existingMediaItemId = Guid.Empty;
          IDictionary<Guid, IList<MediaItemAspect>> existingAspects = null;
          bool found = MatchExistingItem(database, transaction, mergeHandler, extractedAspects, out existingMediaItemId, out existingAspects);
          if (found && existingMediaItemId != extractedMediaItemId)
          {
            Logger.Debug("Found mergeable media item {0}", existingMediaItemId);
            if (providerResourceAspects != null)
            {
              foreach (MultipleMediaItemAspect aspect in providerResourceAspects)
              {
                aspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, extractedProviderResourceAspects.GetAttributeValue(ProviderResourceAspect.ATTR_SYSTEM_ID));
                string resourcePath = aspect.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
                if (string.IsNullOrEmpty(resourcePath))
                  aspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, extractedProviderResourceAspects.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH));
                object resourceType = aspect.GetAttributeValue<object>(ProviderResourceAspect.ATTR_TYPE);
                if (resourceType == null)
                  aspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, extractedProviderResourceAspects.GetAttributeValue(ProviderResourceAspect.ATTR_TYPE));
                aspect.SetAttribute(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID, extractedProviderResourceAspects.GetAttributeValue(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID));
              }
            }

            if (mergeHandler.TryMerge(extractedAspects, existingAspects))
            {
              //Logger.Debug("Merging extracted item with [{2}] into existing item {0} with [{1}]", existingMediaItemId, 
              //  string.Join(",", existingAspects.Keys.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)),
              //  string.Join(",", extractedAspects.Keys.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));

              UpdateMergedMediaItem(database, transaction, existingMediaItemId, existingAspects.Values.SelectMany(x => x));
              _relationshipManagement.UpdateParents(transaction, existingMediaItemId);
              return existingMediaItemId;
            }
          }
          if (mergeHandler.RequiresMerge(extractedAspects))
          {
            return Guid.Empty;
          }
        }
      }
      return null;
    }

    private bool MatchExistingItem(ISQLDatabase database, ITransaction transaction, IMediaMergeHandler mergeHandler, IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, out Guid existingMediaItemId, out IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      IFilter filter = mergeHandler.GetSearchFilter(extractedAspects);
      if (filter != null)
      {
        IList<Guid> allAspectIds = GetManagedMediaItemAspectMetadata().Keys.Except(mergeHandler.MergeableAspects).ToList();
        if (allAspectIds.Contains(RelationshipAspect.ASPECT_ID))
        {
          //Because relationships are loaded for both parties in the relationship (one the inverse of the other) saving the aspects will cause a duplication of the relationship.
          //So don't load it to avoid duplication. Merging will still work because the existing relationship is already persisted.
          allAspectIds.Remove(RelationshipAspect.ASPECT_ID);
        }

        //For items that require merging load all aspects during the search. For other items opttmise on the assumption that a match won't be found 
        //by requesting only the MergeHandlers match aspects, the rest of the aspects are loaded if a match is found.
        bool loadAllAspects = mergeHandler.RequiresMerge(extractedAspects);
        IEnumerable<Guid> optionalAspectIds = loadAllAspects ? allAspectIds : mergeHandler.MatchAspects.Where(a => a != RelationshipAspect.ASPECT_ID);
        IList<MediaItem> existingItems = Search(database, transaction, new MediaItemQuery(mergeHandler.MergeableAspects, optionalAspectIds, filter), false, null, true);
        foreach (MediaItem existingItem in existingItems)
        {
          //Logger.Debug("Checking existing item {0} with [{1}]", existingItem.MediaItemId, string.Join(",", existingItem.Aspects.Keys.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));
          if (mergeHandler.TryMatch(extractedAspects, existingItem.Aspects))
          {
            MediaItem matchedItem;
            if (loadAllAspects)
              matchedItem = existingItem;
            else
              //ensure all aspects are loaded
              matchedItem = Search(database, transaction, new MediaItemQuery(mergeHandler.MergeableAspects, allAspectIds,
                  new MediaItemIdFilter(existingItem.MediaItemId)), false, null, true).FirstOrDefault();

            if (matchedItem != null)
            {
              existingMediaItemId = matchedItem.MediaItemId;
              existingAspects = matchedItem.Aspects;
              return true;
            }
          }
        }
      }
      existingMediaItemId = Guid.Empty;
      existingAspects = null;
      return false;
    }

    private IEnumerable<MediaItemAspect> RemoveInverseRelationships(IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      return mediaItemAspects.Where(mia =>
        mia.Metadata.AspectId != RelationshipAspect.ASPECT_ID ||
        _miaManagement.RelationshipExists(mia.GetAttributeValue<Guid>(RelationshipAspect.ATTR_ROLE), mia.GetAttributeValue<Guid>(RelationshipAspect.ATTR_LINKED_ROLE)));
    }

    private void UpdateMergedMediaItem(ISQLDatabase database, ITransaction transaction, Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      try
      {
        mediaItemAspects = RemoveInverseRelationships(mediaItemAspects);
        foreach (MediaItemAspect mia in mediaItemAspects)
        {
          if (!_miaManagement.ManagedMediaItemAspectTypes.ContainsKey(mia.Metadata.AspectId))
            // Simply skip unknown MIA types. All types should have been added before update.
            continue;
          // For multiple MIAs let MIA management decide if it's and add or update
          if (mia is MultipleMediaItemAspect)
            _miaManagement.AddOrUpdateMIA(transaction, mediaItemId, mia);
          else
            _miaManagement.AddOrUpdateMIA(transaction, mediaItemId, mia, false);
        }
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error updating merged media item with id '{0}'", e, mediaItemId);
        throw;
      }
    }

    protected virtual void UpdateRelationships(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> mediaItemAspects, bool isRefresh, CancellationToken cancelToken)
    {
      if (cancelToken.IsCancellationRequested || ShuttingDown)
        return;

      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      //Logger.Debug("Updating relationships for {0}", mediaItemId);

      // TODO: What happens to MIAs that the reconciler automatically adds which have been removed manually by the user?
      foreach (IRelationshipExtractor extractor in mediaAccessor.LocalRelationshipExtractors.Values)
      {
        foreach (IRelationshipRoleExtractor roleExtractor in extractor.RoleExtractors)
        {
          if (cancelToken.IsCancellationRequested || ShuttingDown)
            return;

          UpdateRelationship(roleExtractor, mediaItemId, mediaItemAspects, isRefresh, cancelToken);
        }
      }

      if (cancelToken.IsCancellationRequested || ShuttingDown)
        return;

      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      using (ITransaction transaction = database.BeginTransaction())
      {
        UpdateMediaItem(database, transaction, mediaItemId, mediaItemAspects.Values.SelectMany(x => x));
        //Update parents
        _relationshipManagement.UpdateParents(transaction, mediaItemId);
        transaction.Commit();
      }
    }

    private void UpdateRelationship(IRelationshipRoleExtractor roleExtractor, Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, bool isRefresh, CancellationToken cancelToken)
    {
      IList<Guid> roleAspectIds = new List<Guid>(roleExtractor.RoleAspects);
      roleAspectIds.Add(MediaAspect.ASPECT_ID);

      IList<Guid> linkedRoleAspectIds = new List<Guid>(roleExtractor.LinkedRoleAspects);
      linkedRoleAspectIds.Add(MediaAspect.ASPECT_ID);

      // Any usable item must contain all of roleExtractor.RoleAspects
      if (roleAspectIds.Except(aspects.Keys).Any())
        return;

      IList<RelationshipItem> extractedItems;
      if (!roleExtractor.TryExtractRelationships(aspects, !isRefresh, out extractedItems))
      {
        Logger.Debug("Extractor {0} extracted {1} media items from media item {2}", roleExtractor.GetType().Name, 0, mediaItemId);
        return;
      }
      Logger.Debug("Extractor {0} extracted {1} media items from media item {2}", roleExtractor.GetType().Name, extractedItems == null ? 0 : extractedItems.Count, mediaItemId);

      HashSet<Guid> updatedItems = new HashSet<Guid>();
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      using (ITransaction transaction = database.BeginTransaction())
      {
        //Try and find any extracted items already in the database or add them.
        foreach (var extractedItem in extractedItems)
        {
          if (cancelToken.IsCancellationRequested || ShuttingDown)
            return;

          IDictionary<Guid, IList<MediaItemAspect>> extractedItemAspects = extractedItem.Aspects;
          Guid extractedItemId = extractedItem.MediaItemId;
          if (!extractedItem.HasChanged && extractedItemId != Guid.Empty)
          {
            //item found in cache, just add the relationship, cached items don't need an update
            AddRelationship(roleExtractor, extractedItem.MediaItemId, aspects, extractedItemAspects);
            continue;
          }

          bool needsUpdate;
          Guid? matchedMediaItemId = MatchExternalItem(database, transaction, roleExtractor, extractedItemAspects, extractedItemId, linkedRoleAspectIds, out needsUpdate);
          if (matchedMediaItemId.HasValue)
          {
            //existing item found, add the relationship and mark it for updating if necessary
            AddRelationship(roleExtractor, matchedMediaItemId.Value, aspects, extractedItemAspects);
            roleExtractor.CacheExtractedItem(matchedMediaItemId.Value, extractedItemAspects);
            //false here means the extracted item is virtual but existing item isn't so don't update
            if (needsUpdate)
            {
              //update and reconcile as it might have changed
              UpdateMediaItem(database, transaction, matchedMediaItemId.Value, extractedItemAspects.Values.SelectMany(x => x));
              updatedItems.Add(matchedMediaItemId.Value);
            }
          }
          else
          {
            //new item, add it
            Guid newMediaItemId = NewMediaItemId();
            Logger.Debug("Adding new media item for extracted item {0}", newMediaItemId);
            bool merged;
            IEnumerable<MediaItemAspect> extractedAspects = extractedItemAspects.Values.SelectMany(x => x);
            newMediaItemId = AddOrUpdateMediaItem(database, transaction, Guid.Empty, _localSystemId, VirtualResourceProvider.ToResourcePath(newMediaItemId), null, newMediaItemId, extractedAspects, out merged);
            if (newMediaItemId != Guid.Empty)
            {
              AddRelationship(roleExtractor, newMediaItemId, aspects, extractedItemAspects);
              roleExtractor.CacheExtractedItem(newMediaItemId, extractedItemAspects);
              //merged items don't need reconciling
              if (!merged)
                updatedItems.Add(newMediaItemId);
            }
          }
        }
        transaction.Commit();
      }

      if (updatedItems.Count == 0)
        return;

      //reload all updated items and reconcile as they might have changed
      ICollection<MediaItem> items;
      using (ITransaction transaction = database.BeginTransaction())
        items = GetMediaItems(database, transaction, updatedItems, null, GetManagedMediaItemAspectMetadata().Keys, false, null, true, false);

      foreach (MediaItem item in items)
      {
        TransferTransientAspects(aspects.Values.SelectMany(x => x), item);
        Reconcile(item.MediaItemId, item.Aspects, isRefresh, cancelToken);
        MediaLibraryMessaging.SendMediaItemAddedOrUpdatedMessage(item);
      }
    }

    private Guid? MatchExternalItem(ISQLDatabase database, ITransaction transaction, IRelationshipRoleExtractor roleExtractor, IDictionary<Guid, IList<MediaItemAspect>> extractedItem, Guid extractedItemId, IList<Guid> linkedRoleAspectIds, out bool needsUpdate)
    {
      needsUpdate = false;
      IFilter filter = extractedItemId != Guid.Empty ? new MediaItemIdFilter(extractedItemId) : roleExtractor.GetSearchFilter(extractedItem);
      if (filter == null)
        return null;

      // Any potential linked item must contain all of LinkedRoleAspects
      HashSet<Guid> optionalAspectIds = new HashSet<Guid>(roleExtractor.MatchAspects);
      //make sure MediaAspect is included for checking if it's a virtual item
      optionalAspectIds.Add(MediaAspect.ASPECT_ID);
      if (optionalAspectIds.Contains(RelationshipAspect.ASPECT_ID))
      {
        //Because relationships are loaded for both parties in the relationship (one the inverse of the other) saving the aspects will cause a duplication of the relationship.
        //So don't load it to avoid duplication. Merging will still work because the existing relationship is already persisted.
        optionalAspectIds.Remove(RelationshipAspect.ASPECT_ID);
      }

      //Logger.Debug("Searching for external items matching {0} / {1} / {2} with [{3}]", source, type, id, string.Join(",", linkedRoleAspectIds.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));
      IList<MediaItem> externalItems = Search(database, transaction, new MediaItemQuery(linkedRoleAspectIds, optionalAspectIds.Except(linkedRoleAspectIds), filter), false, null, true);
      foreach (MediaItem externalItem in externalItems)
      {
        //Logger.Debug("Checking external item {0} with [{1}]", externalItem.MediaItemId, string.Join(",", externalItem.Aspects.Keys.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));
        if (roleExtractor.TryMatch(extractedItem, externalItem.Aspects))
        {
          Guid matchedMediaItemId = externalItem.MediaItemId;
          bool? isExistingStub = externalItem.Aspects[MediaAspect.ASPECT_ID][0].GetAttributeValue<bool?>(MediaAspect.ATTR_ISSTUB);
          bool? isExtractedStub = extractedItem[MediaAspect.ASPECT_ID][0].GetAttributeValue<bool?>(MediaAspect.ATTR_ISSTUB);
          if (isExistingStub == true || isExtractedStub == true)
          {
            extractedItem[MediaAspect.ASPECT_ID][0].SetAttribute(MediaAspect.ATTR_ISSTUB, true); //Update stub flag
          }
          bool? isExistingVirtual = externalItem.Aspects[MediaAspect.ASPECT_ID][0].GetAttributeValue<bool?>(MediaAspect.ATTR_ISVIRTUAL);
          if (isExistingVirtual == false)
          {
            bool? isExtractedVirtual = extractedItem[MediaAspect.ASPECT_ID][0].GetAttributeValue<bool?>(MediaAspect.ATTR_ISVIRTUAL);
            if (isExtractedVirtual == true)
              return matchedMediaItemId; //Do not overwrite the existing real media item with a virtual one
            extractedItem[MediaAspect.ASPECT_ID][0].SetAttribute(MediaAspect.ATTR_ISVIRTUAL, false); //Update virtual flag so it's not reset by the update
          }
          needsUpdate = true;
          return matchedMediaItemId;
        }
      }
      return null;
    }

    private bool AddRelationship(IRelationshipRoleExtractor roleExtractor, Guid itemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects)
    {
      if (!roleExtractor.BuildRelationship)
        return false;

      bool playable = false;
      var hierarchy = _miaManagement.GetRelationshipType(roleExtractor.Role, roleExtractor.LinkedRole);
      if (hierarchy != null)
        playable = hierarchy.UpdatePlayPercentage;
      int index;
      if (!roleExtractor.TryGetRelationshipIndex(aspects, linkedAspects, out index))
        index = 0;
      //Logger.Debug("Adding a {0} / {1} relationship linked to {2} at {3}", roleExtractor.LinkedRole, roleExtractor.Role, itemId, index);
      MediaItemAspect.AddOrUpdateRelationship(aspects, roleExtractor.Role, roleExtractor.LinkedRole, itemId, playable, index);
      return true;
    }

    private bool DeleteOrphan(ISQLDatabase database, ITransaction transaction, Guid mediaItemId)
    {
      try
      {
        using (IDbCommand command = transaction.CreateCommand())
        {
          database.AddParameter(command, "ITEM_ID", mediaItemId, typeof(Guid));

          command.CommandText = SELECT_ORPHAN_COUNT_SQL;
          if (Convert.ToInt32(command.ExecuteScalar()) > 0)
          {
            Logger.Debug("MediaLibrary: Deleted orphaned media item {0}", mediaItemId);
            _relationshipManagement.DeleteMediaItemAndRelationships(transaction, mediaItemId);
            MediaLibraryMessaging.SendMediaItemsDeletedMessage();
            return true;
          }
        }
        return false;
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error deleting orphaned media item {0}", e, mediaItemId);
        throw;
      }
    }

    private bool UpdateParentPlayUserData(Guid userProfileId, Guid mediaItemId, bool updatePlayDate)
    {
      try
      {
        List<Guid> parents = new List<Guid>();
        ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
        using (ITransaction transaction = database.BeginTransaction())
        {
          using (IDbCommand command = transaction.CreateCommand())
          {
            //Find parents
            command.CommandText = SELECT_PARENT_USER_PLAY_DATA_FROM_ID_SQL;
            database.AddParameter(command, "ITEM_ID", mediaItemId, typeof(Guid));
            using (IDataReader reader = command.ExecuteReader())
            {
              while (reader.Read())
              {
                if (!parents.Contains(database.ReadDBValue<Guid>(reader, 0)))
                  parents.Add(database.ReadDBValue<Guid>(reader, 0));
              }
            }

            command.Parameters.Clear();
            var itemParam = database.AddParameter(command, "ITEM_ID", Guid.Empty, typeof(Guid));
            var valueParam = database.AddParameter(command, "DATA_VALUE", 0, typeof(int));
            var keyParam = database.AddParameter(command, "USER_DATA_KEY", UserDataKeysKnown.KEY_PLAY_PERCENTAGE, typeof(string));
            var userParam = database.AddParameter(command, "USER_PROFILE_ID", Guid.Empty, typeof(Guid));
            foreach (var parentId in parents)
            {
              itemParam.Value = parentId;
              userParam.Value = userProfileId;
              keyParam.Value = UserDataKeysKnown.KEY_PLAY_COUNT;
              command.CommandText = SELECT_PLAY_DATA_FROM_PARENT_ID_SQL;
              float nonVirtualChildCount = 0;
              float watchedCount = 0;
              using (IDataReader reader = command.ExecuteReader())
              {
                while (reader.Read())
                {
                  bool? childVirtual = database.ReadDBValue<bool?>(reader, 0);
                  if (childVirtual == false)
                  {
                    nonVirtualChildCount++;

                    //Only non-virtual items can be counted as watched
                    int playCount = 0;
                    if (int.TryParse(database.ReadDBValue<string>(reader, 1), out playCount))
                    {
                      if (playCount > 0)
                        watchedCount++;
                    }
                    else //Prefer user play count but use overall play count if not available
                    {
                      int? totalPlayCount = database.ReadDBValue<int?>(reader, 2);
                      if (totalPlayCount.HasValue && totalPlayCount.Value > 0)
                        watchedCount++;
                    }
                  }
                }
              }

              //Update parent
              int watchPercentage = nonVirtualChildCount <= 0 ? 100 : Convert.ToInt32((watchedCount * 100F) / nonVirtualChildCount);
              if (watchPercentage >= 100)
                watchPercentage = 100;

              keyParam.Value = UserDataKeysKnown.KEY_PLAY_PERCENTAGE;
              valueParam.Value = watchPercentage.ToString();
              if (command.ExecuteNonQuery() == 0)
              {
                command.CommandText = INSERT_USER_PLAY_DATA_FOR_ID_SQL;
                command.ExecuteNonQuery();
                command.CommandText = UPDATE_USER_PLAY_DATA_FROM_ID_SQL;
              }
              Logger.Debug("MediaLibrary: Set parent media item {0} watch percentage = {1}", parentId, valueParam.Value);

              keyParam.Value = UserDataKeysKnown.KEY_PLAY_COUNT;
              valueParam.Value = watchPercentage >= 100 ? "1" : "0";
              if (command.ExecuteNonQuery() == 0)
              {
                command.CommandText = INSERT_USER_PLAY_DATA_FOR_ID_SQL;
                command.ExecuteNonQuery();
                command.CommandText = UPDATE_USER_PLAY_DATA_FROM_ID_SQL;
              }
              Logger.Debug("MediaLibrary: Set parent media item {0} watch count = {1}", parentId, valueParam.Value);

              if (updatePlayDate)
              {
                keyParam.Value = UserDataKeysKnown.KEY_PLAY_DATE;
                valueParam.Value = watchPercentage >= 100 ? DateTime.Now.ToString("s") : null;
                if (command.ExecuteNonQuery() == 0)
                {
                  command.CommandText = INSERT_USER_PLAY_DATA_FOR_ID_SQL;
                  command.ExecuteNonQuery();
                  command.CommandText = UPDATE_USER_PLAY_DATA_FROM_ID_SQL;
                }
                Logger.Debug("MediaLibrary: Set parent media item {0} watch date = {1}", parentId, valueParam.Value);
              }
            }
          }
          transaction.Commit();
        }

        return parents.Count > 0;
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error updating parent user data for media item {0}", e, mediaItemId);
        throw;
      }
    }

    private bool UpdateChildPlayUserData(Guid userProfileId, Guid mediaItemId, bool watched, bool updateWatchedDate)
    {
      try
      {
        ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
        Dictionary<Guid, int> childPlayCounts = new Dictionary<Guid, int>();
        using (ITransaction transaction = database.BeginTransaction())
        {
          using (IDbCommand command = transaction.CreateCommand())
          {
            database.AddParameter(command, "ITEM_ID", mediaItemId, typeof(Guid));
            database.AddParameter(command, "USER_PROFILE_ID", userProfileId, typeof(Guid));
            database.AddParameter(command, "USER_DATA_KEY", UserDataKeysKnown.KEY_PLAY_COUNT, typeof(string));

            command.CommandText = SELECT_USER_DATA_FROM_PARENT_ID_SQL;
            using (IDataReader reader = command.ExecuteReader())
            {
              while (reader.Read())
              {
                Guid? childId = database.ReadDBValue<Guid?>(reader, 0);
                int playCount = 0;
                if (watched)
                {
                  string plays = database.ReadDBValue<string>(reader, 1);
                  int.TryParse(plays, out playCount);
                  if (playCount == 0)
                  {
                    playCount++;
                  }
                }
                childPlayCounts.Add(childId.Value, playCount);
              }
            }

            //Update parent
            command.Parameters.Clear();
            var itemParam = database.AddParameter(command, "ITEM_ID", Guid.Empty, typeof(Guid));
            var userParam = database.AddParameter(command, "USER_PROFILE_ID", userProfileId, typeof(Guid));
            var keyParam = database.AddParameter(command, "USER_DATA_KEY", "", typeof(string));
            var valueParam = database.AddParameter(command, "USER_DATA_VALUE", "", typeof(string));
            command.CommandText = UPDATE_USER_PLAY_DATA_FROM_ID_SQL;
            foreach (var key in childPlayCounts)
            {
              itemParam.Value = key.Key;

              keyParam.Value = UserDataKeysKnown.KEY_PLAY_PERCENTAGE;
              valueParam.Value = key.Value > 0 ? "100" : "0";
              if (command.ExecuteNonQuery() == 0)
              {
                command.CommandText = INSERT_USER_PLAY_DATA_FOR_ID_SQL;
                command.ExecuteNonQuery();
                command.CommandText = UPDATE_USER_PLAY_DATA_FROM_ID_SQL;
              }
              Logger.Debug("MediaLibrary: Set parent media item {0} watch percentage = {1}", key.Key, valueParam.Value);

              keyParam.Value = UserDataKeysKnown.KEY_PLAY_COUNT;
              valueParam.Value = key.Value.ToString();
              if (command.ExecuteNonQuery() == 0)
              {
                command.CommandText = INSERT_USER_PLAY_DATA_FOR_ID_SQL;
                command.ExecuteNonQuery();
                command.CommandText = UPDATE_USER_PLAY_DATA_FROM_ID_SQL;
              }
              Logger.Debug("MediaLibrary: Set parent media item {0} watch count = {1}", key.Key, valueParam.Value);

              if(updateWatchedDate)
              {
                keyParam.Value = UserDataKeysKnown.KEY_PLAY_COUNT;
                valueParam.Value = key.Value > 0 ? DateTime.Now.ToString("s") : null;
                if (command.ExecuteNonQuery() == 0)
                {
                  command.CommandText = INSERT_USER_PLAY_DATA_FOR_ID_SQL;
                  command.ExecuteNonQuery();
                  command.CommandText = UPDATE_USER_PLAY_DATA_FROM_ID_SQL;
                }
                Logger.Debug("MediaLibrary: Set parent media item {0} watch date = {1}", key.Key, valueParam.Value);
              }
            }
          }
          transaction.Commit();
        }

        if (childPlayCounts.Count > 0)
        {
          UpdateParentPlayUserData(userProfileId, childPlayCounts.First().Key, updateWatchedDate);
          return true;
        }
        return false;
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error updating media item {0} childs user data", e, mediaItemId);
        throw;
      }
    }

    public void DeleteMediaItemOrPath(string systemId, ResourcePath path, bool inclusive)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      using (ITransaction transaction = database.BeginTransaction())
      {
        try
        {
          _relationshipManagement.DeletePathAndRelationships(transaction, systemId, path, inclusive);
          transaction.Commit();
          MediaLibraryMessaging.SendMediaItemsDeletedMessage();
        }
        catch (Exception e)
        {
          Logger.Error("MediaLibrary: Error deleting media item(s) of system '{0}' in path '{1}'",
              e, systemId, path.Serialize());
          transaction.Rollback();
          throw;
        }
      }
    }

    public void ClientStartedShareImport(Guid shareId)
    {
      Share share = GetShare(shareId);
      if (share == null)
      {
        Logger.Warn("MediaLibrary.ClientStartedShareImport: Unknown share id {0}", shareId);
        return;
      }
      IClientManager clientManager = ServiceRegistration.Get<IClientManager>();
      lock (clientManager.SyncObj)
      {
        ClientConnection client = clientManager.ConnectedClients.FirstOrDefault(c => c.Descriptor.MPFrontendServerUUID == share.SystemId);
        if (client == null)
          return;
        object value;
        if (client.Properties.TryGetValue(KEY_CURRENTLY_IMPORTING_SHARE_IDS, out value))
          ((ICollection<Guid>)value).Add(shareId);
        else
          client.Properties[KEY_CURRENTLY_IMPORTING_SHARE_IDS] = new List<Guid> { shareId };
      }
      ContentDirectoryMessaging.SendShareImportMessage(ContentDirectoryMessaging.MessageType.ShareImportStarted, shareId);
      lock (_shareImportSync)
      {
        if (!_shareImportStates.ContainsKey(share.ShareId))
          _shareImportStates.Add(share.ShareId, new ShareImportState { ShareId = share.ShareId, IsImporting = true, Progress = -1 });
      }
      UpdateServerState();
    }

    public void ClientCompletedShareImport(Guid shareId)
    {
      Share share = GetShare(shareId);
      if (share == null)
      {
        Logger.Warn("MediaLibrary.ClientCompletedShareImport: Unknown share id {0}", shareId);
        return;
      }
      IClientManager clientManager = ServiceRegistration.Get<IClientManager>();
      lock (clientManager.SyncObj)
      {
        ClientConnection client = clientManager.ConnectedClients.FirstOrDefault(c => c.Descriptor.MPFrontendServerUUID == share.SystemId);
        if (client == null)
          return;
        object value;
        if (client.Properties.TryGetValue(KEY_CURRENTLY_IMPORTING_SHARE_IDS, out value))
          ((ICollection<Guid>)value).Remove(shareId);
      }
      ContentDirectoryMessaging.SendShareImportMessage(ContentDirectoryMessaging.MessageType.ShareImportCompleted, shareId);
      lock (_shareImportSync)
      {
        if (_shareImportStates.ContainsKey(share.ShareId))
          _shareImportStates.Remove(share.ShareId);
      }
      UpdateServerState();
    }

    public ICollection<Guid> GetCurrentlyImportingShareIds()
    {
      ICollection<Guid> result = new List<Guid>();
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();
      // Shares of media library
      _importingSharesCache = GetShares(null).Values;
      CollectionUtils.AddAll(result, importerWorker.ImportJobs.Where(importJobInfo => importJobInfo.State == ImportJobState.Active).
          Select(importJobInfo => _importingSharesCache.BestContainingPath(importJobInfo.BasePath)).Where(share => share != null).Select(share => share.ShareId));
      // Client shares
      IClientManager clientManager = ServiceRegistration.Get<IClientManager>();
      lock (clientManager.SyncObj)
        CollectionUtils.AddAll(result, clientManager.ConnectedClients.Select(client =>
          {
            object value;
            return client.Properties.TryGetValue(KEY_CURRENTLY_IMPORTING_SHARE_IDS, out value) ? (ICollection<Guid>)value : null;
          }).Where(clientShares => clientShares != null).SelectMany(clientShares => clientShares).ToList());

      return result;
    }

    #endregion

    #region Playback

    public void NotifyPlayback(Guid mediaItemId, bool watched)
    {
      MediaItem item = Search(new MediaItemQuery(new Guid[] { MediaAspect.ASPECT_ID }, null, new MediaItemIdFilter(mediaItemId)), false, null, true).FirstOrDefault();
      if (item == null)
        return;
      SingleMediaItemAspect mediaAspect;
      MediaItemAspect.TryGetAspect(item.Aspects, MediaAspect.Metadata, out mediaAspect);
      mediaAspect.SetAttribute(MediaAspect.ATTR_LASTPLAYED, DateTime.Now);
      if (watched)
      {
        int playCount = (int)(mediaAspect.GetAttributeValue(MediaAspect.ATTR_PLAYCOUNT) ?? 0);
        mediaAspect.SetAttribute(MediaAspect.ATTR_PLAYCOUNT, playCount + 1);
      }
      UpdateMediaItem(mediaItemId, new MediaItemAspect[] { mediaAspect }, true);
    }

    private bool SetMediaItemUserData(ITransaction transaction, Guid userProfileId, Guid mediaItemId, string dataKey, string dataValue)
    {
      int count = 0;
      using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserMediaItemDataCommand(transaction, userProfileId, mediaItemId, dataKey))
      {
        count += command.ExecuteNonQuery();
      }
      if (dataValue != null)
      {
        using (IDbCommand command = UserProfileDataManagement_SubSchema.CreateUserMediaItemDataCommand(transaction, userProfileId, mediaItemId, dataKey, dataValue))
        {
          count += command.ExecuteNonQuery();
        }
      }
      return count > 0;
    }

    public void NotifyUserPlayback(Guid userProfileId, Guid mediaItemId, int percentage, bool updatePlayDate)
    {
      NotifyPlayback(mediaItemId, percentage >= 100);

      bool updateParents = false;
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      using (ITransaction transaction = database.BeginTransaction())
      {
        int dataIdx;
        int count = 0;
        if (percentage >= 100)
        {
          using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserMediaItemDataCommand(transaction, userProfileId, mediaItemId, UserDataKeysKnown.KEY_PLAY_COUNT, out dataIdx))
          {
            using (IDataReader reader = command.ExecuteReader())
            {
              if (reader.Read())
              {
                count = Convert.ToInt32(database.ReadDBValue<string>(reader, dataIdx));
              }
            }
          }
          count++;
          updateParents = true;

          //Update play count
          SetMediaItemUserData(transaction, userProfileId, mediaItemId, UserDataKeysKnown.KEY_PLAY_COUNT, count.ToString());
          //Update last played
          SetMediaItemUserData(transaction, userProfileId, mediaItemId, UserDataKeysKnown.KEY_PLAY_DATE, DateTime.Now.ToString("s"));
          //Update play percentage
          SetMediaItemUserData(transaction, userProfileId, mediaItemId, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, "100");
        }
        else if (percentage >= 0)
        {
          if (updatePlayDate)
          {
            //Update last played
            SetMediaItemUserData(transaction, userProfileId, mediaItemId, UserDataKeysKnown.KEY_PLAY_DATE, DateTime.Now.ToString("s"));
          }
          //Update play percentage
          SetMediaItemUserData(transaction, userProfileId, mediaItemId, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, percentage.ToString());
        }
        else
        {
          updateParents = true;

          //Reset play count
          SetMediaItemUserData(transaction, userProfileId, mediaItemId, UserDataKeysKnown.KEY_PLAY_COUNT, "0");
          //Delete last played
          SetMediaItemUserData(transaction, userProfileId, mediaItemId, UserDataKeysKnown.KEY_PLAY_DATE, null);
          //Delete percentage
          SetMediaItemUserData(transaction, userProfileId, mediaItemId, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, null);
        }
        transaction.Commit();
      }

      if (updateParents)
      {
        if (!UpdateChildPlayUserData(userProfileId, mediaItemId, percentage >= 100, updatePlayDate))
          UpdateParentPlayUserData(userProfileId, mediaItemId, updatePlayDate);
      }
    }

    #endregion

    #region User data management

    public void UserDataUpdated(Guid userProfileId, Guid mediaItemId, string userDataKey, string userData)
    {
      if (userDataKey == UserDataKeysKnown.KEY_PLAY_COUNT)
      {
        if (!UpdateChildPlayUserData(userProfileId, mediaItemId, Convert.ToInt32(userData) > 0, false))
          UpdateParentPlayUserData(userProfileId, mediaItemId, false);
      }
    }

    #endregion

    #region Media item aspect schema management

    public bool MediaItemAspectStorageExists(Guid aspectId)
    {
      return _miaManagement.MediaItemAspectStorageExists(aspectId);
    }

    public MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid aspectId)
    {
      return _miaManagement.GetMediaItemAspectMetadata(aspectId);
    }

    public void AddMediaItemAspectStorage(MediaItemAspectMetadata miam)
    {
      if (_miaManagement.AddMediaItemAspectStorage(miam))
      {
        Logger.Info("MediaLibrary: Media item aspect storage for MIA type '{0}' (name '{1}') was added", miam.AspectId, miam.Name);
        ContentDirectoryMessaging.SendMIATypesChangedMessage();
      }
    }

    public void RemoveMediaItemAspectStorage(Guid aspectId)
    {
      if (_miaManagement.RemoveMediaItemAspectStorage(aspectId))
      {
        Logger.Info("MediaLibrary: Media item aspect storage for MIA type '{0}' was removed", aspectId);
        ContentDirectoryMessaging.SendMIATypesChangedMessage();
      }
    }

    public IDictionary<Guid, MediaItemAspectMetadata> GetManagedMediaItemAspectMetadata()
    {
      return _miaManagement.ManagedMediaItemAspectTypes;
    }

    public IDictionary<Guid, DateTime> GetManagedMediaItemAspectCreationDates()
    {
      return _miaManagement.ManagedMediaItemAspectCreationDates;
    }

    public MediaItemAspectMetadata GetManagedMediaItemAspectMetadata(Guid aspectId)
    {
      return _miaManagement.GetMediaItemAspectMetadata(aspectId);
    }

    #endregion

    #region Relationship type schema management

    public void AddRelationship(RelationshipType relationshipType, bool isChildPrimaryResource)
    {
      _miaManagement.AddRelationship(relationshipType, isChildPrimaryResource);
    }
    
    public ICollection<RelationshipType> GetManagedRelationshipTypes()
    {
      return _miaManagement.LocallyKnownRelationshipTypes;
    }

    public ICollection<RelationshipType> GetManagedHierarchicalRelationshipTypes()
    {
      return _miaManagement.LocallyKnownHierarchicalRelationshipTypes;
    }

    #endregion

    #region Shares management

    private void UpdateShareWatchers()
    {
      lock (_syncObj)
      {
        Logger.Info("MediaLibrary: Share configuration changed, updating watchers");
        DeInitShareWatchers();
        InitShareWatchers();
      }
    }

    private void InitShareWatchers()
    {
      lock (_syncObj)
      {
        IDictionary<Guid, Share> shares = GetShares(_localSystemId);
        foreach (Share share in shares.Values)
        {
          if (!share.UseShareWatcher)
          {
            Logger.Info("MediaLibrary: Share watcher not enabled for path {0}", share.BaseResourcePath);
            continue;
          }

          // This should never happen, as the share ID is unique. But error reports show duplicate keys when adding a watcher?
          if (_shareWatchers.ContainsKey(share.ShareId))
            continue;

          try
          {
            ShareWatcher watcher = null;
            try
            {
              watcher = new ShareWatcher(share, this, false);
            }
            catch (Exception e)
            {
              Logger.Warn("MediaLibrary: Share watcher cannot be used for path {0}", e, share.BaseResourcePath);
              continue;
            }
            _shareWatchers.Add(share.ShareId, watcher);
          }
          catch (Exception e)
          {
            Logger.Error("MediaLibrary: Error initializing share watcher for {0}", e, share.BaseResourcePath);
          }
        }
      }
    }

    private void DeInitShareWatchers()
    {
      lock (_syncObj)
      {
        try
        {
          foreach (Guid shareId in _shareWatchers.Keys)
          {
            _shareWatchers[shareId].Dispose();
          }
          _shareWatchers.Clear();
        }
        catch (Exception e)
        {
          Logger.Error("MediaLibrary: Error when removing share watchers", e);
          throw;
        }
      }
    }

    public void RegisterShare(Share share)
    {
      Logger.Info("MediaLibrary: Registering share '{0}' at system {1}: Setting name '{2}', base resource path '{3}' and media categories '{4}'",
          share.ShareId, share.SystemId, share.Name, share.BaseResourcePath, StringUtils.Join(", ", share.MediaCategories));
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int shareIdIndex;
        using (IDbCommand command = MediaLibrary_SubSchema.SelectShareIdCommand(
            transaction, share.SystemId, share.BaseResourcePath, out shareIdIndex))
        using (IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
        {
          if (reader.Read())
            throw new ShareExistsException("A share with the given system '{0}' and path '{1}' already exists",
                share.SystemId, share.BaseResourcePath);
        }
        using (IDbCommand command = MediaLibrary_SubSchema.InsertShareCommand(transaction, share.ShareId, share.SystemId,
            share.BaseResourcePath, share.Name, share.UseShareWatcher))
          command.ExecuteNonQuery();

        foreach (string mediaCategory in share.MediaCategories)
          AddMediaCategoryToShare(transaction, share.ShareId, mediaCategory);

        transaction.Commit();

        ContentDirectoryMessaging.SendRegisteredSharesChangedMessage();

        TryScheduleLocalShareImport(share);
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error registering share '{0}'", e, share.ShareId);
        transaction.Rollback();
        throw;
      }
    }

    public Guid CreateShare(string systemId, ResourcePath baseResourcePath, string shareName, bool useShareWatcher,
        IEnumerable<string> mediaCategories)
    {
      Guid shareId = Guid.NewGuid();
      Logger.Info("MediaLibrary: Creating new share '{0}'", shareId);
      Share share = new Share(shareId, systemId, baseResourcePath, shareName, useShareWatcher, mediaCategories);
      RegisterShare(share);
      return shareId;
    }

    public void RemoveShare(Guid shareId)
    {
      Stopwatch swDelete = new Stopwatch();
      swDelete.Start();
      Logger.Info("MediaLibrary: Removing share '{0}'", shareId);
      Share share = GetShare(shareId);
      TryCancelLocalImportJobs(share);

      lock (GetResourcePathLock(share.BaseResourcePath))
      {
        ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
        ITransaction transaction = database.BeginTransaction();
        try
        {
          using (IDbCommand command = MediaLibrary_SubSchema.DeleteSharesCommand(transaction, new Guid[] { shareId }))
            command.ExecuteNonQuery();

          _relationshipManagement.DeletePathAndRelationships(transaction, share.SystemId, share.BaseResourcePath, true);

          transaction.Commit();

          MediaLibraryMessaging.SendMediaItemsDeletedMessage();
          ContentDirectoryMessaging.SendRegisteredSharesChangedMessage();
        }
        catch (Exception e)
        {
          Logger.Error("MediaLibrary: Error removing share '{0}'", e, shareId);
          transaction.Rollback();
          throw;
        }
      }
      lock (_syncObj)
        _shareDeleteSync.Remove(share.BaseResourcePath);
      Logger.Info("MediaLibrary: Share '{0}' removed ({1} ms)", shareId, swDelete.ElapsedMilliseconds);
    }

    public void RemoveSharesOfSystem(string systemId)
    {
      Stopwatch swDelete = new Stopwatch();
      swDelete.Start();
      Logger.Info("MediaLibrary: Removing all shares of system '{0}'", systemId);
      IDictionary<Guid, Share> shares = GetShares(systemId);
      foreach (Share share in shares.Values)
        TryCancelLocalImportJobs(share);

      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = MediaLibrary_SubSchema.DeleteSharesOfSystemCommand(transaction, systemId))
          command.ExecuteNonQuery();

        _relationshipManagement.DeletePathAndRelationships(transaction, systemId, null, true);

        transaction.Commit();

        MediaLibraryMessaging.SendMediaItemsDeletedMessage();
        ContentDirectoryMessaging.SendRegisteredSharesChangedMessage();
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error removing shares of system '{0}'", e, systemId);
        transaction.Rollback();
        throw;
      }
      Logger.Info("MediaLibrary: All shares for system '{0}' removed ({1} ms)", systemId, swDelete.ElapsedMilliseconds);
    }

    public int UpdateShare(Guid shareId, ResourcePath baseResourcePath, string shareName, bool useShareWatcher,
        IEnumerable<string> mediaCategories, RelocationMode relocationMode)
    {
      Logger.Info("MediaLibrary: Updating share '{0}': Setting name '{1}', base resource path '{2}' and media categories '{3}'",
          shareId, shareName, baseResourcePath, StringUtils.Join(", ", mediaCategories));
      Share share = GetShare(shareId);
      TryCancelLocalImportJobs(share);

      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        Share originalShare = GetShare(transaction, shareId);

        using (IDbCommand command = MediaLibrary_SubSchema.UpdateShareCommand(transaction, shareId, baseResourcePath, shareName, useShareWatcher))
          command.ExecuteNonQuery();

        // Update media categories
        ICollection<string> formerMediaCategories = GetShareMediaCategories(transaction, shareId);

        ICollection<string> newCategories = new List<string>();
        foreach (string mediaCategory in mediaCategories)
        {
          newCategories.Add(mediaCategory);
          if (!formerMediaCategories.Contains(mediaCategory))
            AddMediaCategoryToShare(transaction, shareId, mediaCategory);
        }

        foreach (string mediaCategory in formerMediaCategories)
          if (!newCategories.Contains(mediaCategory))
            RemoveMediaCategoryFromShare(transaction, shareId, mediaCategory);

        int numAffected = 0;
        // Relocate media items
        switch (relocationMode)
        {
          case RelocationMode.Relocate:
            numAffected = RelocateMediaItems(transaction, originalShare.SystemId, originalShare.BaseResourcePath, baseResourcePath);
            Logger.Info("MediaLibrary: Relocated {0} media items during share update", numAffected);
            break;
          case RelocationMode.Remove:
            numAffected = _relationshipManagement.DeletePathAndRelationships(transaction, originalShare.SystemId, originalShare.BaseResourcePath, true);
            MediaLibraryMessaging.SendMediaItemsDeletedMessage();
            Logger.Info("MediaLibrary: Deleted {0} media items during share update (will be re-imported)", numAffected);
            Share updatedShare = GetShare(transaction, shareId);
            TryScheduleLocalShareImport(updatedShare);
            break;
        }
        transaction.Commit();

        ContentDirectoryMessaging.SendRegisteredSharesChangedMessage();

        return numAffected;
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error updating share '{0}'", e, shareId);
        transaction.Rollback();
        throw;
      }
    }

    public IDictionary<Guid, Share> GetShares(string systemId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int shareIdIndex;
        int systemIdIndex;
        int pathIndex;
        int shareNameIndex;
        int shareWatcherIndex;
        IDbCommand command;
        if (string.IsNullOrEmpty(systemId))
        {
          command = MediaLibrary_SubSchema.SelectSharesCommand(transaction, out shareIdIndex,
            out systemIdIndex, out pathIndex, out shareNameIndex, out shareWatcherIndex);
        }
        else
          command = MediaLibrary_SubSchema.SelectSharesBySystemCommand(transaction, systemId, out shareIdIndex,
              out systemIdIndex, out pathIndex, out shareNameIndex, out shareWatcherIndex);
        IDictionary<Guid, Share> result = new Dictionary<Guid, Share>();
        try
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              Guid shareId = database.ReadDBValue<Guid>(reader, shareIdIndex);
              result.Add(shareId, new Share(shareId, database.ReadDBValue<string>(reader, systemIdIndex),
                  ResourcePath.Deserialize(database.ReadDBValue<string>(reader, pathIndex)),
                  database.ReadDBValue<string>(reader, shareNameIndex),
                  database.ReadDBValue<int>(reader, shareWatcherIndex) == 1, null));
            }
          }
          // Init share categories later to avoid opening new result sets inside reader loop (issue with MySQL)
          foreach (var share in result)
          {
            ICollection<string> mediaCategories = GetShareMediaCategories(transaction, share.Key);
            CollectionUtils.AddAll(share.Value.MediaCategories, mediaCategories);
          }
          return result;
        }
        finally
        {
          command.Dispose();
        }
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public Share GetShare(Guid shareId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        return GetShare(transaction, shareId);
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public void SetupDefaultLocalShares()
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      foreach (Share share in mediaAccessor.CreateDefaultShares())
        RegisterShare(share);
    }

    #endregion

    #region Client online registration

    public IDictionary<string, SystemName> OnlineClients
    {
      get
      {
        lock (_syncObj)
          return new Dictionary<string, SystemName>(_systemsOnline);
      }
    }

    public void NotifySystemOnline(string systemId, SystemName currentSystemName)
    {
      Logger.Info("MediaLibrary: Client '{0}' is online at system '{1}'", systemId, currentSystemName);
      lock (_syncObj)
        _systemsOnline[systemId] = currentSystemName;
    }

    public void NotifySystemOffline(string systemId)
    {
      Logger.Info("MediaLibrary: Client '{0}' is offline", systemId);
      lock (_syncObj)
        _systemsOnline.Remove(systemId);
    }

    #endregion

    #endregion

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
