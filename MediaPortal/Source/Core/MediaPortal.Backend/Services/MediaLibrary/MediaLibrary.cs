#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
    }

    protected class ImportResultHandler : IImportResultHandler
    {
      protected MediaLibrary _parent;

      public ImportResultHandler(MediaLibrary parent)
      {
        _parent = parent;
      }

      public Guid UpdateMediaItem(Guid parentDirectoryId, ResourcePath path, IEnumerable<MediaItemAspect> updatedAspects, bool isRefresh)
      {
        try
        {
          return _parent.AddOrUpdateMediaItem(parentDirectoryId, _parent.LocalSystemId, path, updatedAspects, isRefresh);
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
          if(scheduleImport)
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
        if(_fileChangeNotifier != null)
          _fileChangeNotifier.UnregisterChangeTracker(ShareWatcherPathChanged);

        if (_checkTimer != null)
          _checkTimer.Dispose();
      }
    }

    protected class ChildCountDefinition
    {
      public Guid ParentRole { get; set; }
      public MediaItemAspectMetadata ParentMiaType { get; set; }
      public MediaItemAspectMetadata.AttributeSpecification ChildCountAttribute { get; set; }
      public bool IncludeVirtual { get; set; }
    }

    #endregion

    #region Consts

    protected const string KEY_CURRENTLY_IMPORTING_SHARE_IDS = "CurrentlyImportingShareIds";
    protected const char ESCAPE_CHAR = '\\';

    #endregion

    #region Protected fields

    protected IDictionary<string, SystemName> _systemsOnline = new Dictionary<string, SystemName>(); // System ids mapped to system names

    protected MIA_Management _miaManagement = null;
    protected object _syncObj = new object();
    protected string _localSystemId;
    protected IMediaBrowsing _mediaBrowsingCallback;
    protected IImportResultHandler _importResultHandler;
    protected AsynchronousMessageQueue _messageQueue;
    protected bool _shutdown = false;

    protected Dictionary<Guid, List<Guid>> _virtualRoleHierarchy = new Dictionary<Guid, List<Guid>>();
    protected Dictionary<Guid, List<ChildCountDefinition>> _virtualRoleHierarchyChildCount = new Dictionary<Guid, List<ChildCountDefinition>>();
    protected Dictionary<Guid, ShareWatcher> _shareWatchers = new Dictionary<Guid, ShareWatcher>();
    protected object opsSync = new object();

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
            ImporterWorkerMessaging.CHANNEL
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    public void Dispose()
    {
      _messageQueue.Shutdown();
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ImporterWorkerMessaging.CHANNEL)
      {
        ImporterWorkerMessaging.MessageType messageType = (ImporterWorkerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ImporterWorkerMessaging.MessageType.ImportStarted:
          case ImporterWorkerMessaging.MessageType.ImportCompleted:
            ResourcePath path = (ResourcePath) message.MessageData[ImporterWorkerMessaging.RESOURCE_PATH];
            ICollection<Share> shares = GetShares(null).Values;
            Share share = shares.BestContainingPath(path);
            if (share == null)
              break;
            if (messageType == ImporterWorkerMessaging.MessageType.ImportStarted)
              ContentDirectoryMessaging.SendShareImportMessage(ContentDirectoryMessaging.MessageType.ShareImportStarted, share.ShareId);
            else
              ContentDirectoryMessaging.SendShareImportMessage(ContentDirectoryMessaging.MessageType.ShareImportCompleted, share.ShareId);
            break;

          case ImporterWorkerMessaging.MessageType.RefreshLocalShares:
            GetShares(null).Values.ToList().ForEach(TryScheduleLocalShareRefresh);
            break;
        }
      }
    }

    #endregion

    public string LocalSystemId
    {
      get { return _localSystemId; }
    }

    #region Protected methods

    protected MediaItemQuery BuildLoadItemQuery(string systemId, ResourcePath path)
    {
      return new MediaItemQuery(new List<Guid>(), new List<Guid>(),
          new BooleanCombinationFilter(BooleanOperator.And, new IFilter[]
            {
              new RelationalFilter(ProviderResourceAspect.ATTR_SYSTEM_ID, RelationalOperator.EQ, systemId),
              new RelationalFilter(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RelationalOperator.EQ, path.Serialize())
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
      ISQLDatabase database = transaction.Database;
      Share share;
      using (IDbCommand command = MediaLibrary_SubSchema.SelectShareByIdCommand(transaction, shareId, out systemIdIndex, out pathIndex, out shareNameIndex))
      using (IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
      {
        if (!reader.Read())
          return null;
        share = new Share(shareId, database.ReadDBValue<string>(reader, systemIdIndex), ResourcePath.Deserialize(
            database.ReadDBValue<string>(reader, pathIndex)),
            database.ReadDBValue<string>(reader, shareNameIndex), null);
      }
      // Init share categories later to avoid opening new result sets inside reader loop (issue with MySQL)
      ICollection<string> mediaCategories = GetShareMediaCategories(transaction, shareId);
      CollectionUtils.AddAll(share.MediaCategories, mediaCategories);
      return share;
    }

    protected Guid? GetMediaItemId(ITransaction transaction, string systemId, ResourcePath resourcePath)
    {
      string providerAspectTable = _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata);
      string systemIdAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SYSTEM_ID);
      string pathAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);

      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = transaction.CreateCommand())
      {
        command.CommandText = "SELECT " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + providerAspectTable +
            " WHERE " + systemIdAttribute + " = @SYSTEM_ID AND " + pathAttribute + " = @PATH";

        database.AddParameter(command, "SYSTEM_ID", systemId, typeof(string));
        database.AddParameter(command, "PATH", resourcePath.Serialize(), typeof(string));

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
      Logger.Debug("Creating media item {0}", mediaItemId);
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

    protected int DeleteMediaItemResourcePath(ITransaction transaction, string systemId, ResourcePath basePath)
    {
      MediaItemAspectMetadata providerAspectMetadata = ProviderResourceAspect.Metadata;
      string providerAspectTable = _miaManagement.GetMIATableName(providerAspectMetadata);
      string systemIdAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SYSTEM_ID);
      string pathAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      string primaryAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_PRIMARY);
      string commandStr = "SELECT " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + ", " + primaryAttribute + ", " + pathAttribute +
              " FROM " + providerAspectTable + " WHERE " + systemIdAttribute + " = @SYSTEM_ID" +
              " AND " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN (" +
              // TODO: Replace this inner select statement by a select statement generated from an appropriate item query
              "SELECT " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + providerAspectTable +
              " WHERE " + systemIdAttribute + " = @SYSTEM_ID" +
              " AND " + pathAttribute + " = @EXACT_PATH)";

      int affectedRows = 0;
      Guid? parentId = null;
      bool hasMorePrimaryResources = false;
      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = transaction.CreateCommand())
      {
        database.AddParameter(command, "SYSTEM_ID", systemId, typeof(string));

        //string path = StringUtils.RemoveSuffixIfPresent(basePath.Serialize(), "/");
        string path = basePath.Serialize();
        database.AddParameter(command, "EXACT_PATH", path, typeof(string));

        command.CommandText = commandStr;
        using (IDataReader reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            parentId = database.ReadDBValue<Guid?>(reader, 0);

            bool? isPrimary = database.ReadDBValue<bool?>(reader, 1);
            if (!isPrimary.HasValue) isPrimary = true;

            string resPath = database.ReadDBValue<string>(reader, 2);
            ResourcePath resourcePath = ResourcePath.Deserialize(resPath);
            if (resourcePath != basePath && isPrimary.Value)
            {
              hasMorePrimaryResources = true;
              break;
            }
          }
        }
      }

      if (hasMorePrimaryResources)
      {
        //Only delete the resource
        using (IDbCommand command = transaction.CreateCommand())
        {
          database.AddParameter(command, "SYSTEM_ID", systemId, typeof(string));

          string path = StringUtils.RemoveSuffixIfPresent(basePath.Serialize(), "/");
          database.AddParameter(command, "EXACT_PATH", path, typeof(string));

          commandStr = "DELETE FROM " + providerAspectTable +
            " WHERE " + systemIdAttribute + " = @SYSTEM_ID" +
            " AND " + pathAttribute + " = @EXACT_PATH";
          command.CommandText = commandStr;
          affectedRows = command.ExecuteNonQuery();
        }
      }
      else if(parentId.HasValue)
      {
        Logger.Debug("MediaLibrary: Set media item {0} virtual", parentId.Value);

        using (IDbCommand command = transaction.CreateCommand())
        {
          database.AddParameter(command, "ITEM_ID", parentId.Value, typeof(Guid));

          //Set virtual tag
          commandStr = "UPDATE " + _miaManagement.GetMIATableName(MediaAspect.Metadata) +
            " SET " + _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL) + " = 1" +
            " WHERE " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + " = @ITEM_ID";
          command.CommandText = commandStr;
          affectedRows = command.ExecuteNonQuery();

          //Delete all remaining resources so foreign keys delete linked rows
          commandStr = "DELETE FROM " + providerAspectTable +
            " WHERE " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + " = @ITEM_ID";
          command.CommandText = commandStr;
          affectedRows += command.ExecuteNonQuery();

          //Insert virtual resource
          database.AddParameter(command, "VIRT_PATH", VirtualResourceProvider.ToResourcePath(parentId.Value).Serialize(), typeof(string));
          database.AddParameter(command, "PARENT_DIR", Guid.Empty, typeof(Guid));

          commandStr = "INSERT INTO " + providerAspectTable + " (" +
            MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + ", " +
            _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH) + ", " +
            _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_PRIMARY) + ", " +
            _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_INDEX) + ", " +
            _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SYSTEM_ID) + ", " +
            _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID) +
            ") VALUES (@ITEM_ID, @VIRT_PATH, 1, 0, '" + _localSystemId + "', @PARENT_DIR)";
          command.CommandText = commandStr;
          affectedRows += command.ExecuteNonQuery();
        }

        //Check if new virtual media item should be deleted
        if(DeleteVirtualParents(transaction.Database, transaction, parentId.Value) == false)
        {
          //Check if new virtual parent should be updated
          UpdateVirtualParents(transaction.Database, transaction, parentId.Value);

          //Check if new virtual parent user data should be updated
          UpdateAllParentUserData(transaction.Database, transaction, parentId.Value);
        }
      }
      return affectedRows;
    }

    protected int DeleteAllMediaItemsUnderPath(ITransaction transaction, string systemId, ResourcePath basePath, bool inclusive)
    {
      MediaItemAspectMetadata providerAspectMetadata = ProviderResourceAspect.Metadata;
      string providerAspectTable = _miaManagement.GetMIATableName(providerAspectMetadata);
      string systemIdAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SYSTEM_ID);
      string pathAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      string commandStr = 
        // TODO: Replace this inner select statement by a select statement generated from an appropriate item query
        "SELECT " + pathAttribute + " FROM " + providerAspectTable +
        " WHERE " + systemIdAttribute + " = @SYSTEM_ID";

      int affectedRows = 0;
      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = transaction.CreateCommand())
      {
        database.AddParameter(command, "SYSTEM_ID", systemId, typeof(string));

        if (basePath != null)
        {
          commandStr += " AND (";
          if (inclusive)
            commandStr += pathAttribute + " = @EXACT_PATH OR ";
          commandStr +=
              pathAttribute + " LIKE @LIKE_PATH1 ESCAPE @LIKE_ESCAPE1 OR " +
              pathAttribute + " LIKE @LIKE_PATH2 ESCAPE @LIKE_ESCAPE2" +
              ")";
          string path = StringUtils.RemoveSuffixIfPresent(basePath.Serialize(), "/");
          string escapedPath = SqlUtils.LikeEscape(path, ESCAPE_CHAR);
          if (inclusive)
          {
            // The path itself
            database.AddParameter(command, "EXACT_PATH", path, typeof(string));
            // Normal children and, if escapedPath ends with "/", the directory itself
            database.AddParameter(command, "LIKE_PATH1", escapedPath + "/%", typeof(string));
            database.AddParameter(command, "LIKE_ESCAPE1", ESCAPE_CHAR, typeof(char));
          }
          else
          {
            // Normal children, in any case excluding the escaped path, even if it is a directory which ends with "/"
            database.AddParameter(command, "LIKE_PATH1", escapedPath + "/_%", typeof(string));
            database.AddParameter(command, "LIKE_ESCAPE1", ESCAPE_CHAR, typeof(char));
          }
          // Chained children
          database.AddParameter(command, "LIKE_PATH2", escapedPath + ">_%", typeof(string));
          database.AddParameter(command, "LIKE_ESCAPE2", ESCAPE_CHAR, typeof(char));
        }

        command.CommandText = commandStr;

        List<ResourcePath> childPaths = new List<ResourcePath>();
        using (IDataReader reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            childPaths.Add(ResourcePath.Deserialize(database.ReadDBValue<string>(reader, 0)));
            affectedRows++;
          }
        }

        foreach (ResourcePath childPath in childPaths)
        {
          Logger.Debug("MediaLibrary: Delete sub path {0}", childPath);

          DeleteMediaItemResourcePath(transaction, systemId, childPath);
        }
      }
      return affectedRows;
    }

    protected IFilter AddOnlyOnlineFilter(IFilter innerFilter)
    {
      IFilter onlineFilter = new BooleanCombinationFilter(BooleanOperator.Or, _systemsOnline.Select(
          systemEntry => new RelationalFilter(ProviderResourceAspect.ATTR_SYSTEM_ID, RelationalOperator.EQ, systemEntry.Key)));
      return innerFilter == null ? onlineFilter : BooleanCombinationFilter.CombineFilters(BooleanOperator.And, innerFilter, onlineFilter);
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
      NotifySystemOnline(_localSystemId, SystemName.GetLocalSystemName());
    }

    public void ActivateImporterWorker()
    {
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

    public MediaItem LoadItem(string systemId, ResourcePath path,
        IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs, Guid? userProfileId = null)
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
          removeProviderResourceAspect = true;
          necessaryRequestedMIATypeIDsWithProvierResourceAspect.Add(ProviderResourceAspect.ASPECT_ID);
        }
        
        MediaItemQuery loadItemQuery = BuildLoadItemQuery(systemId, path);
        loadItemQuery.SetNecessaryRequestedMIATypeIDs(necessaryRequestedMIATypeIDsWithProvierResourceAspect);
        loadItemQuery.SetOptionalRequestedMIATypeIDs(optionalRequestedMIATypeIDs);
        CompiledMediaItemQuery cmiq = CompiledMediaItemQuery.Compile(_miaManagement, loadItemQuery);
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

    public IList<MediaItem> Search(MediaItemQuery query, bool filterOnlyOnline, Guid? userProfileId, bool includeVirtual)
    {
      return Search(null, null , query, filterOnlyOnline, userProfileId, includeVirtual);
    }

    public IList<MediaItem> Search(ISQLDatabase database, ITransaction transaction, MediaItemQuery query, bool filterOnlyOnline, Guid? userProfileId, bool includeVirtual)
    {
      // We add the provider resource aspect to the necessary aspect types be able to filter online systems
      MediaItemQuery executeQuery = query;
      if (filterOnlyOnline)
      {
        executeQuery = new MediaItemQuery(query); // Use constructor by other query to make sure all properties are copied (including sorting and limits)
        executeQuery.NecessaryRequestedMIATypeIDs.Add(ProviderResourceAspect.ASPECT_ID);
        executeQuery.Filter = AddOnlyOnlineFilter(query.Filter);
      }

      if (includeVirtual == false)
      {
        if (executeQuery.Filter == null)
          executeQuery.Filter = new RelationalFilter(MediaAspect.ATTR_ISVIRTUAL, RelationalOperator.EQ, includeVirtual);
        else
          executeQuery.Filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, executeQuery.Filter, new RelationalFilter(MediaAspect.ATTR_ISVIRTUAL, RelationalOperator.EQ, includeVirtual));
      }

      CompiledMediaItemQuery cmiq = CompiledMediaItemQuery.Compile(_miaManagement, executeQuery);
      IList<MediaItem> items = null;
      if(database == null || transaction == null)
        items = cmiq.QueryList();
      else
        items = cmiq.QueryList(database, transaction);
      Logger.Debug("Found media items [{0}]", string.Join(",", items.Select(x => x.MediaItemId)));
      IList<MediaItem> result = new List<MediaItem>(items.Count);
      foreach (MediaItem item in items)
      {
        LoadUserDataForMediaItem(userProfileId, item);
      }

      if (filterOnlyOnline && !query.NecessaryRequestedMIATypeIDs.Contains(ProviderResourceAspect.ASPECT_ID))
      { // The provider resource aspect was not requested and thus has to be removed from the result items
        foreach (MediaItem item in items)
        {
          item.Aspects.Remove(ProviderResourceAspect.ASPECT_ID);
          result.Add(item);
        }
      }
      else
        result = items;
      return result;
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
        filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, new IFilter[] {filter, selectAttributeFilter});

      if (includeVirtual == false)
      {
        if (filter == null)
          filter = new RelationalFilter(MediaAspect.ATTR_ISVIRTUAL, RelationalOperator.EQ, includeVirtual);
        else
          filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, new RelationalFilter(MediaAspect.ATTR_ISVIRTUAL, RelationalOperator.EQ, includeVirtual));
      }

      CompiledGroupedAttributeValueQuery cdavq = CompiledGroupedAttributeValueQuery.Compile(_miaManagement,
          filterOnlyOnline ? necessaryMIATypeIDs.Union(new Guid[] {ProviderResourceAspect.ASPECT_ID}) : necessaryMIATypeIDs, 
          attributeType, saf, selectProjectionFunctionImpl, projectionValueType, 
          filterOnlyOnline ? AddOnlyOnlineFilter(filter) : filter);
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
        int resultGroupItemCount = (int) resultItem.Value;
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
      if (includeVirtual == false)
      {
        if (filter == null)
          filter = new RelationalFilter(MediaAspect.ATTR_ISVIRTUAL, RelationalOperator.EQ, includeVirtual);
        else
          filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, new RelationalFilter(MediaAspect.ATTR_ISVIRTUAL, RelationalOperator.EQ, includeVirtual));
      }

      CompiledCountItemsQuery cciq = CompiledCountItemsQuery.Compile(_miaManagement,
          necessaryMIATypeIDs, filterOnlyOnline ? AddOnlyOnlineFilter(filter) : filter);
      return cciq.Execute();
    }

    private void LoadUserDataForMediaItem(Guid? userProfileId, MediaItem mediaItem)
    {
      if (userProfileId.HasValue)
      {
        mediaItem.UserData.Clear();

        ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
        ITransaction transaction = database.BeginTransaction();
        try
        {
          int dataKeyIndex;
          int dataIndex;
          using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectAllUserMediaItemDataCommand(transaction,
            userProfileId.Value, mediaItem.MediaItemId, out dataKeyIndex, out dataIndex))
          {
            using (IDataReader reader = command.ExecuteReader())
            {
              while (reader.Read())
              {
                mediaItem.UserData.Add(database.ReadDBValue<string>(reader, dataKeyIndex), database.ReadDBValue<string>(reader, dataIndex));
              }
            }
          }
        }
        finally
        {
          transaction.Dispose();
        }
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
      return AddOrUpdateMediaItem(parentDirectoryId, systemId, path, null, mediaItemAspects, true, isRefresh);
    }

    private Guid AddOrUpdateMediaItem(Guid parentDirectoryId, string systemId, ResourcePath path, Guid? newMediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects, bool reconcile, bool isRefresh)
    {
      //Logger.Debug("Adding to {0} on {1} in {2}:\n{3}", parentDirectoryId, systemId, path, MediaItemAspect.GetInfo(mediaItemAspects, _miaManagement.ManagedMediaItemAspectTypes));

      // TODO: Avoid multiple write operations to the same media item
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        Guid? mediaItemId = GetMediaItemId(transaction, systemId, path);
        string name = path.FileName;
        foreach (MediaItemAspect mia in mediaItemAspects)
        {
          if (mia.Metadata.AspectId == MediaAspect.ASPECT_ID)
          {
            name = mia.GetAttributeValue<string>(MediaAspect.ATTR_TITLE);
            break;
          }
        }
        Logger.Debug("Adding media item {0} with name {1} ({2})", mediaItemId.HasValue ? mediaItemId : newMediaItemId, name ?? "[null]", Path.GetFileName(path.FileName));
        
        DateTime now = DateTime.Now;
        MediaItemAspect pra;
        MediaItemAspect importerAspect;
        bool wasCreated = !mediaItemId.HasValue;
        if (wasCreated)
        {
          pra = new MultipleMediaItemAspect(ProviderResourceAspect.Metadata);
          pra.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
          pra.SetAttribute(ProviderResourceAspect.ATTR_PRIMARY, true);
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

        Guid? mergedMediaItem = MergeWithExisting(database, transaction, mediaItemId, mediaItemAspects, pra);
        if (mergedMediaItem != null)
        {
          if (mergedMediaItem == Guid.Empty)
          {
            transaction.Rollback();

            Logger.Info("Media item {0} with name {1} ({2}) cannot be saved. Needs to be merged", mediaItemId.HasValue ? mediaItemId : newMediaItemId, name ?? "[null]", Path.GetFileName(path.FileName));

            return Guid.Empty;
          }

          if (mediaItemId.HasValue && mergedMediaItem.Value != mediaItemId.Value)
          {
            DeleteMediaItemAndReleationships(transaction, mediaItemId.Value);
          }

          //Aspects were merged into an existing media item. Discard the remaining aspects
          transaction.Commit();

          Logger.Info("Media item {0} with name {1} ({2}) was merged into {3}", mediaItemId.HasValue ? mediaItemId : newMediaItemId, name ?? "[null]", Path.GetFileName(path.FileName), mergedMediaItem.Value);
          return mergedMediaItem.Value;
        }

        if (wasCreated)
        {
          mediaItemId = AddMediaItem(database, transaction, newMediaItemId);
          _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, pra, true);
        }
        importerAspect.SetAttribute(ImporterAspect.ATTR_DIRTY, false);
        importerAspect.SetAttribute(ImporterAspect.ATTR_LAST_IMPORT_DATE, now);

        _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, importerAspect, wasCreated);

        // Update
        foreach (MediaItemAspect mia in mediaItemAspects)
        {
          if (!_miaManagement.ManagedMediaItemAspectTypes.ContainsKey(mia.Metadata.AspectId))
            // Simply skip unknown MIA types. All types should have been added before import.
            continue;
          if (mia.Metadata.AspectId == ProviderResourceAspect.ASPECT_ID)
          {
            // Only allow certain attributes to be overridden
            mia.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_SYSTEM_ID));
            string resourcePath = mia.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
            if (string.IsNullOrEmpty(resourcePath))
              mia.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH));
            object resourcePrimary = mia.GetAttributeValue<object>(ProviderResourceAspect.ATTR_PRIMARY);
            if (resourcePrimary == null)
              mia.SetAttribute(ProviderResourceAspect.ATTR_PRIMARY, pra.GetAttributeValue<bool>(ProviderResourceAspect.ATTR_PRIMARY));
            mia.SetAttribute(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID, pra.GetAttributeValue<Guid>(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID));

            _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, mia);
          }
          else if (mia.Metadata.AspectId == ImporterAspect.ASPECT_ID)
          { // Those aspects are managed by the MediaLibrary
            // When merging media items this aspect could by present so just ignore it
            // Logger.Warn("MediaLibrary.AddOrUpdateMediaItem: Client tried to update ImporterAspect");
          }
        }
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
          if (mia.Deleted)
            _miaManagement.RemoveMIA(transaction, mediaItemId.Value, mia.Metadata.AspectId);
          else if (wasCreated)
            _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, mia, true);
          else
            _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, mia);
        }

        transaction.Commit();

        Logger.Info("Committed media item {0} with name {1} ({2})", mediaItemId.Value, name ?? "[null]", Path.GetFileName(path.FileName));

        if (reconcile)
          Reconcile(mediaItemId.Value, isRefresh);

        return mediaItemId.Value;
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error adding or updating media item(s) in path '{0}'", e, (path != null ? path.Serialize() : null));
        transaction.Rollback();
        throw;
      }
    }

    protected virtual void Reconcile(Guid mediaItemId, bool isRefresh)
    {
      UpdateRelationships(mediaItemId, isRefresh);
      Logger.Info("Reconciled media item {0}", mediaItemId);
    }

    public void UpdateMediaItem(Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects, bool isRefresh)
    {
      UpdateMediaItem(mediaItemId, mediaItemAspects, false, isRefresh);
    }

    private void UpdateMediaItem(Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects, bool reconcile, bool isRefresh)
    {
      // TODO: Avoid multiple write operations to the same media item
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        foreach (MediaItemAspect mia in mediaItemAspects)
        {
          if (!_miaManagement.ManagedMediaItemAspectTypes.ContainsKey(mia.Metadata.AspectId))
            // Simply skip unknown MIA types. All types should have been added before update.
            continue;
          if (mia.Metadata.AspectId == ImporterAspect.ASPECT_ID ||
              mia.Metadata.AspectId == ProviderResourceAspect.ASPECT_ID)
          { // Those aspects are managed by the MediaLibrary
            //Logger.Warn("MediaLibrary.AddOrUpdateMediaItem: Client tried to update either ImporterAspect or ProviderResourceAspect");
            continue;
          }
          // For multiple MIAs let MIA management decide if it's and add or update
          if(mia is MultipleMediaItemAspect)
            _miaManagement.AddOrUpdateMIA(transaction, mediaItemId, mia);
          else
            _miaManagement.AddOrUpdateMIA(transaction, mediaItemId, mia, false);
        }
        transaction.Commit();

        if(reconcile)
          Reconcile(mediaItemId, isRefresh);
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error updating media item with id '{0}'", e, mediaItemId);
        transaction.Rollback();
        throw;
      }
    }

    protected virtual Guid? MergeWithExisting(ISQLDatabase database, ITransaction transaction, Guid? extractedMediaItemId, IEnumerable<MediaItemAspect> extractedAspectList, MediaItemAspect extractedProviderResourceAspects)
    {
      IDictionary<Guid, IList<MediaItemAspect>> extractedAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      foreach(MediaItemAspect aspect in extractedAspectList)
      {
        if (!extractedAspects.ContainsKey(aspect.Metadata.AspectId))
          extractedAspects.Add(aspect.Metadata.AspectId, new List<MediaItemAspect>());

        extractedAspects[aspect.Metadata.AspectId].Add(aspect);
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

            IList<MultipleMediaItemAspect> providerResourceAspects;
            if (MediaItemAspect.TryGetAspects(extractedAspects, ProviderResourceAspect.Metadata, out providerResourceAspects))
            {
              foreach (MultipleMediaItemAspect aspect in providerResourceAspects)
              {
                aspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, extractedProviderResourceAspects.GetAttributeValue(ProviderResourceAspect.ATTR_SYSTEM_ID));
                string resourcePath = aspect.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
                if (string.IsNullOrEmpty(resourcePath))
                  aspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, extractedProviderResourceAspects.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH));
                object resourcePrimary = aspect.GetAttributeValue<object>(ProviderResourceAspect.ATTR_PRIMARY);
                if (resourcePrimary == null)
                  aspect.SetAttribute(ProviderResourceAspect.ATTR_PRIMARY, extractedProviderResourceAspects.GetAttributeValue<bool>(ProviderResourceAspect.ATTR_PRIMARY));
                aspect.SetAttribute(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID, extractedProviderResourceAspects.GetAttributeValue(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID));
              }
            }

            if (mergeHandler.TryMerge(extractedAspects, existingAspects))
            {
              SingleMediaItemAspect importerAspect = MediaItemAspect.GetAspect(existingAspects, ImporterAspect.Metadata);
              importerAspect.SetAttribute(ImporterAspect.ATTR_DIRTY, false);
              importerAspect.SetAttribute(ImporterAspect.ATTR_LAST_IMPORT_DATE, DateTime.Now);

              //Logger.Debug("Merging extracted item with [{2}] into existing item {0} with [{1}]", existingMediaItemId, 
              //  string.Join(",", existingAspects.Keys.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)),
              //  string.Join(",", extractedAspects.Keys.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));

              UpdateMergedMediaItem(database, transaction, existingMediaItemId, existingAspects.Values.SelectMany(x => x));
              UpdateVirtualParents(database, transaction, existingMediaItemId);
              UpdateAllParentUserData(database, transaction, existingMediaItemId);
              return existingMediaItemId;
            }
          }
          if(mergeHandler.RequiresMerge(extractedAspects))
          {
            return Guid.Empty;
          }
        }
      }
      return null;
    }

    private bool MatchExistingItem(ISQLDatabase database, ITransaction transaction, IMediaMergeHandler mergeHandler, IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, out Guid existingMediaItemId, out IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);

          // Search using external identifiers
          BooleanCombinationFilter filter = new BooleanCombinationFilter(BooleanOperator.And, new[]
                {
                  new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                  new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                  new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
                });
          //Logger.Debug("Searching for existing items matching {0} / {1} / {2} with [{3}]", source, type, id, string.Join(",", mergeHandler.MergeableAspects.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));

          IList<Guid> optionalAspectIds = GetManagedMediaItemAspectMetadata().Keys.Except(mergeHandler.MergeableAspects).ToList();
          if(optionalAspectIds.Contains(RelationshipAspect.ASPECT_ID))
          {
            //Because relationships are loaded for both parties in the relationship (one the inverse of the other) saving the aspects will cause a duplication of the relationship.
            //So don't load it to avoid duplication. Merging will still work because the existing relationship is already persisted.
            optionalAspectIds.Remove(RelationshipAspect.ASPECT_ID);
          }
          IList<MediaItem> existingItems = Search(database, transaction, new MediaItemQuery(mergeHandler.MergeableAspects, optionalAspectIds, filter), false, null, true);
          foreach (MediaItem existingItem in existingItems)
          {
            //Logger.Debug("Checking existing item {0} with [{1}]", existingItem.MediaItemId, string.Join(",", existingItem.Aspects.Keys.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));
            if (mergeHandler.TryMatch(extractedAspects, existingItem.Aspects))
            {
              existingMediaItemId = existingItem.MediaItemId;
              existingAspects = existingItem.Aspects;
              return true;
            }
          }
        }
      }
      existingMediaItemId = Guid.Empty;
      existingAspects = null;
      return false;
    }

    private void UpdateMergedMediaItem(ISQLDatabase database, ITransaction transaction, Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      try
      {
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

    protected virtual void UpdateRelationships(Guid mediaItemId, bool isRefresh)
    {
      if (ShuttingDown == true)
        return;

      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();

      Logger.Debug("Updating relationships for {0}", mediaItemId);
      MediaItem item = Search(new MediaItemQuery(null, GetManagedMediaItemAspectMetadata().Keys, new MediaItemIdFilter(mediaItemId)), false, null, true).FirstOrDefault();
      if (item == null)
      {
        // Item deleted on the main thread before the reconciler thread processes it - could happen?
        Logger.Warn("Cannot find {0}", mediaItemId);
        return;
      }
      //Logger.Debug("Found item {0} with [{1}]", item.MediaItemId, string.Join(",", item.Aspects.Keys.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));

      // TODO: What happens to MIAs that the reconciler automatically adds which have been removed manually by the user?

      foreach (IRelationshipExtractor extractor in mediaAccessor.LocalRelationshipExtractors.Values)
      {
        foreach (IRelationshipRoleExtractor roleExtractor in extractor.RoleExtractors)
        {
          if(ShuttingDown == false)
            UpdateRelationship(roleExtractor, mediaItemId, item.Aspects, isRefresh);
        }
      }

      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      using (ITransaction transaction = database.BeginTransaction())
      {
        //Update parents
        UpdateVirtualParents(database, transaction, mediaItemId);
        UpdateAllParentUserData(database, transaction, mediaItemId);

        transaction.Commit();
      }
    }

    private void UpdateRelationship(IRelationshipRoleExtractor roleExtractor, Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, bool isRefresh)
    {
      IList<Guid> roleAspectIds = new List<Guid>(roleExtractor.RoleAspects);
      roleAspectIds.Add(MediaAspect.ASPECT_ID);

      IList<Guid> linkedRoleAspectIds = new List<Guid>(roleExtractor.LinkedRoleAspects);
      linkedRoleAspectIds.Add(MediaAspect.ASPECT_ID);

      // Any usable item must contain all of roleExtractor.RoleAspects
      if (roleAspectIds.Except(aspects.Keys).Any())
        return;

      ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedItems;
      if (!roleExtractor.TryExtractRelationships(aspects, out extractedItems, !isRefresh))
      {
        Logger.Debug("Extractor {0} extracted {1} media items from media item {2}", roleExtractor.GetType().Name, 0, mediaItemId);
        return;
      }
      Logger.Debug("Extractor {0} extracted {1} media items from media item {2}", roleExtractor.GetType().Name, extractedItems == null ? 0: extractedItems.Count, mediaItemId);

      // Match the extracted aspect data to any items already in the library
      foreach (IDictionary<Guid, IList<MediaItemAspect>> extractedItem in extractedItems)
      {
        if (ShuttingDown == true)
          return;

        bool found = MatchExternalItem(roleExtractor, mediaItemId, aspects, extractedItem, linkedRoleAspectIds, isRefresh);
        if (!found)
        {
          Guid newMediaItemId = NewMediaItemId();
          Logger.Debug("Adding new media item for extracted item {0}", newMediaItemId);
          AddRelationship(roleExtractor, mediaItemId, aspects, extractedItem);
          AddOrUpdateMediaItem(Guid.Empty, _localSystemId, VirtualResourceProvider.ToResourcePath(newMediaItemId), newMediaItemId, extractedItem.Values.SelectMany(x => x), true, isRefresh);
        }
      }
    }

    private bool MatchExternalItem(IRelationshipRoleExtractor roleExtractor, Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> extractedItem, IList<Guid> linkedRoleAspectIds, bool isRefresh)
    {
      IList<MultipleMediaItemAspect> externalAspects;

      if (MediaItemAspect.TryGetAspects(extractedItem, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);

          // Search using external identifiers
          BooleanCombinationFilter filter = new BooleanCombinationFilter(BooleanOperator.And, new[]
                {
                  new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                  new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                  new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
                });
          //Logger.Debug("Searching for external items matching {0} / {1} / {2} with [{3}]", source, type, id, string.Join(",", linkedRoleAspectIds.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));
          // Any potential linked item must contain all of LinkedRoleAspects
          IList<Guid> optionalAspectIds = GetManagedMediaItemAspectMetadata().Keys.Except(linkedRoleAspectIds).ToList();
          if (optionalAspectIds.Contains(RelationshipAspect.ASPECT_ID))
          {
            //Because relationships are loaded for both parties in the relationship (one the inverse of the other) saving the aspects will cause a duplication of the relationship.
            //So don't load it to avoid duplication. Merging will still work because the existing relationship is already persisted.
            optionalAspectIds.Remove(RelationshipAspect.ASPECT_ID);
          }
          IList<MediaItem> externalItems = Search(new MediaItemQuery(linkedRoleAspectIds, optionalAspectIds, filter), false, null, true);
          foreach (MediaItem externalItem in externalItems)
          {
            //Logger.Debug("Checking external item {0} with [{1}]", externalItem.MediaItemId, string.Join(",", externalItem.Aspects.Keys.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));
            if (roleExtractor.TryMatch(extractedItem, externalItem.Aspects))
            {
              //Logger.Debug("Merging extracted item with [{2}] into external item {0} with [{1}]", externalItem.MediaItemId,
              //  string.Join(",", externalItem.Aspects.Keys.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)),
              //  string.Join(",", extractedItem.Keys.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));

              AddRelationship(roleExtractor, mediaItemId, aspects, extractedItem);

              //Update virtual flag
              object isVirtual = externalItem.Aspects[MediaAspect.ASPECT_ID][0].GetAttributeValue(MediaAspect.ATTR_ISVIRTUAL);
              if(isVirtual != null)
                extractedItem[MediaAspect.ASPECT_ID][0].SetAttribute(MediaAspect.ATTR_ISVIRTUAL, isVirtual);

              UpdateMediaItem(externalItem.MediaItemId, extractedItem.Values.SelectMany(x => x), true, isRefresh);

              return true;
            }
          }
        }
      }

      return false;
    }

    private void AddRelationship(IRelationshipRoleExtractor roleExtractor, Guid itemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects)
    {
      if (!roleExtractor.BuildRelationship)
        return;

      int index;
      if (!roleExtractor.TryGetRelationshipIndex(aspects, linkedAspects, out index))
        index = 0;
      //Logger.Debug("Adding a {0} / {1} relationship linked to {2} at {3}", roleExtractor.LinkedRole, roleExtractor.Role, itemId, index);
      MediaItemAspect.AddOrUpdateRelationship(linkedAspects, roleExtractor.LinkedRole, roleExtractor.Role, itemId, index);
    }

    private void DeleteMediaItemAndReleationships(ITransaction transaction, Guid mediaItemId)
    {
      ISQLDatabase database = transaction.Database;

      try
      {
        using (IDbCommand command = transaction.CreateCommand())
        {
          database.AddParameter(command, "ITEM_ID", mediaItemId, typeof(Guid));

          //Find relations
          List<Guid> relations = new List<Guid>();
          command.CommandText = "SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
          " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
          " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID";
          using (IDataReader reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              Guid relationId = database.ReadDBValue<Guid>(reader, 0);
              if(!relations.Contains(relationId))
                relations.Add(relationId);
            }
          }
          command.CommandText = "SELECT " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) +
          " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID";
          using (IDataReader reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              Guid relationId = database.ReadDBValue<Guid>(reader, 0);
              if (!relations.Contains(relationId))
                relations.Add(relationId);
            }
          }
          Logger.Info("MediaLibrary: Delete media item {0} and {1} relations", mediaItemId, relations.Count);
          
          //Delete item
          command.CommandText = "DELETE FROM " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID";
          command.ExecuteNonQuery();

          //Delete relations
          command.CommandText = "DELETE FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
              " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID";
          command.ExecuteNonQuery();

          //Delete orphaned relations
          foreach (Guid relationId in relations)
            DeleteOrphan(database, transaction, relationId);
        }
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error deleting media item {0}", e, mediaItemId);
        throw;
      }
    }

    private bool TryFindParent(ISQLDatabase database, ITransaction transaction, Guid mediaItemId, Guid childRole, Guid parentRole, out Guid? parentId, 
      out string childIdColumn, out string childRoleColumn, out string parentIdColumn, out string parentRoleColumn)
    {
      parentId = null;
      parentIdColumn = null;
      parentRoleColumn = null;
      childIdColumn = null;
      childRoleColumn = null;

      try
      {
        using (IDbCommand command = transaction.CreateCommand())
        {
          command.Parameters.Clear();
          database.AddParameter(command, "ITEM_ID", mediaItemId, typeof(Guid));
          database.AddParameter(command, "ROLE_ID", childRole, typeof(Guid));
          database.AddParameter(command, "PARENT_ROLE_ID", parentRole, typeof(Guid));

          command.CommandText = "SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
            " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
            " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE) + " = @PARENT_ROLE_ID" +
            " AND " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE) + " = @ROLE_ID" +
            " AND " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID";
          using (IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
          {
            if (reader.Read())
              parentId = database.ReadDBValue<Guid>(reader, 0);
          }
          parentIdColumn = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
          parentRoleColumn = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE);
          childIdColumn = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID);
          childRoleColumn = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE);

          if (!parentId.HasValue)
          {
            //Try reverse lookup
            command.CommandText = "SELECT " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) +
            " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
            " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE) + " = @PARENT_ROLE_ID" +
            " AND " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE) + " = @ROLE_ID" +
            " AND " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID";
            using (IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
            {
              if (reader.Read())
                parentId = database.ReadDBValue<Guid>(reader, 0);
            }
            parentIdColumn = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID);
            parentRoleColumn = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE);
            childIdColumn = MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME;
            childRoleColumn = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE);
          }
        }

        //if(parentId.HasValue)
          //Logger.Debug("MediaLibrary: Found parent {0} with role {1} for child {2} with role {3}", parentId, parentRole, mediaItemId, childRole);
        return parentId.HasValue;
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error finding parent for media item {0}", e, mediaItemId);
        throw;
      }
    }

    private bool DeleteVirtualParents(ISQLDatabase database, ITransaction transaction, Guid mediaItemId)
    {
      try
      {
        using (IDbCommand command = transaction.CreateCommand())
        {
          //Find parent if possible
          Guid? parentId;
          string parentIdColumn = null;
          string parentRoleColumn = null;
          string childIdColumn = null;
          string childRoleColumn = null;
          bool hasParent = false;
          bool allParentsDeleted = true;
          List<Guid> parentsToDelete = new List<Guid>();
          List<Guid> childsToDelete = new List<Guid>();
          foreach (Guid childRole in _virtualRoleHierarchy.Keys)
          {
            foreach (Guid parentRole in _virtualRoleHierarchy[childRole])
            {
              parentId = null;
              parentIdColumn = null;
              parentRoleColumn = null;
              childIdColumn = null;
              childRoleColumn = null;

              if (TryFindParent(database, transaction, mediaItemId, childRole, parentRole, out parentId, out childIdColumn, out childRoleColumn, out parentIdColumn, out parentRoleColumn))
              {
                hasParent = true;

                command.Parameters.Clear();
                database.AddParameter(command, "ITEM_ID", parentId.Value, typeof(Guid));
                database.AddParameter(command, "ROLE_ID", childRole, typeof(Guid));
                database.AddParameter(command, "PARENT_ROLE_ID", parentRole, typeof(Guid));

                //Find all childs
                List<Guid> childs = new List<Guid>();
                bool? allChildsAreVirtual = null;
                command.CommandText = "SELECT R." + childIdColumn +
                    ", M." + _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL) +
                    " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) + " R" +
                    " JOIN " + _miaManagement.GetMIATableName(MediaAspect.Metadata) + " M" +
                    " ON M." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = R." + childIdColumn +
                    " WHERE R." + childRoleColumn + " = @ROLE_ID" +
                    " AND R." + parentRoleColumn + " = @PARENT_ROLE_ID" +
                    " AND R." + parentIdColumn + " = @ITEM_ID";
                using (IDataReader reader = command.ExecuteReader())
                {
                  while (reader.Read())
                  {
                    if (allChildsAreVirtual == null)
                      allChildsAreVirtual = true;

                    Guid childId = database.ReadDBValue<Guid>(reader, 0);
                    bool? childVirtual = database.ReadDBValue<bool?>(reader, 1);
                    childs.Add(childId);
                    if (childVirtual == false)
                    {
                      allChildsAreVirtual = false;
                      break;
                    }
                  }
                }

                if (allChildsAreVirtual == true)
                {
                  //Logger.Debug("MediaLibrary: All {0} children with role {1} of parent media item {2} with role {3} are virtual", childs.Count, childRole, parentId.Value, parentRole);

                  foreach (Guid childId in childs)
                  {
                    if (!childsToDelete.Contains(childId))
                      childsToDelete.Add(childId);
                  }

                  if (!parentsToDelete.Contains(parentId.Value))
                    parentsToDelete.Add(parentId.Value);
                }
                else
                {
                  allParentsDeleted = false;
                }
              }
            }
          }

          foreach (Guid childId in childsToDelete)
          {
            Logger.Debug("MediaLibrary: Delete virtual child media item {0}", childId);

            DeleteMediaItemAndReleationships(transaction, childId);
          }

          foreach (Guid childParentId in parentsToDelete)
          {
            Logger.Debug("MediaLibrary: Delete virtual parent media item {0}", childParentId);

            DeleteMediaItemAndReleationships(transaction, childParentId);
          }

          if (!hasParent)
            DeleteOrphan(database, transaction, mediaItemId);

          return allParentsDeleted;
        }
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error deleting virtual parent for child media item {0}", e, mediaItemId);
        throw;
      }
    }

    private void DeleteOrphan(ISQLDatabase database, ITransaction transaction, Guid mediaItemId)
    {
      try
      {
        using (IDbCommand command = transaction.CreateCommand())
        {
          database.AddParameter(command, "ITEM_ID", mediaItemId, typeof(Guid));
          database.AddParameter(command, "EXACT_PATH", VirtualResourceProvider.ToResourcePath(mediaItemId).Serialize(), typeof(string));

          command.CommandText = "SELECT COUNT(*) FROM " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME +
            " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN (" +
            " SELECT T0." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
            " FROM " + _miaManagement.GetMIATableName(MediaAspect.Metadata) + " T0" +
            " JOIN " + _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata) + " T1 ON " +
            " T1." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = " +
            " T0." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
            " WHERE T0." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID" +
            " AND T1." + _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH) + " = @EXACT_PATH" +
            " AND NOT EXISTS (" +
            "SELECT "+ MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + 
            " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
            " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID" +
            " OR " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID" +
            "))";
          if (Convert.ToInt32(command.ExecuteScalar()) > 0)
          {
            Logger.Debug("MediaLibrary: Deleted orphaned media item {0}", mediaItemId);
            DeleteMediaItemAndReleationships(transaction, mediaItemId);
          }
        }
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error deleting orphaned media item {0}", e, mediaItemId);
        throw;
      }
    }

    private void UpdateVirtualParents(ISQLDatabase database, ITransaction transaction, Guid mediaItemId)
    {
      try
      {
        using (IDbCommand command = transaction.CreateCommand())
        {
          //Find parent if possible
          Guid? parentId;
          string parentIdColumn = null;
          string parentRoleColumn = null;
          string childIdColumn = null;
          string childRoleColumn = null;
          List<Guid> parentsToDelete = new List<Guid>();
          List<Guid> childsToDelete = new List<Guid>();
          foreach (Guid childRole in _virtualRoleHierarchy.Keys)
          {
            foreach (Guid parentRole in _virtualRoleHierarchy[childRole])
            {
              parentId = null;
              parentId = null;
              parentIdColumn = null;
              parentRoleColumn = null;
              childIdColumn = null;
              childRoleColumn = null;

              if (TryFindParent(database, transaction, mediaItemId, childRole, parentRole, out parentId, out childIdColumn, out childRoleColumn, out parentIdColumn, out parentRoleColumn))
              {
                int totalCount = 0;
                int availableCount = 0;

                command.Parameters.Clear();
                database.AddParameter(command, "ITEM_ID", parentId.Value, typeof(Guid));
                database.AddParameter(command, "ROLE_ID", childRole, typeof(Guid));
                database.AddParameter(command, "PARENT_ROLE_ID", parentRole, typeof(Guid));

                //Find all childs
                List<Guid> childs = new List<Guid>();
                bool? allChildsAreVirtual = null;
                command.CommandText = "SELECT R." + childIdColumn +
                    ", M." + _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL) +
                    " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) + " R" +
                    " JOIN " + _miaManagement.GetMIATableName(MediaAspect.Metadata) + " M" +
                    " ON M." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = R." + childIdColumn +
                    " WHERE R." + childRoleColumn + " = @ROLE_ID" +
                    " AND R." + parentRoleColumn + " = @PARENT_ROLE_ID" +
                    " AND R." + parentIdColumn + " = @ITEM_ID";
                using (IDataReader reader = command.ExecuteReader())
                {
                  while (reader.Read())
                  {
                    if (allChildsAreVirtual == null)
                      allChildsAreVirtual = true;
                    Guid childId = database.ReadDBValue<Guid>(reader, 0);
                    bool? childVirtual = database.ReadDBValue<bool?>(reader, 1);
                    childs.Add(childId);
                    if (childVirtual == false)
                    {
                      availableCount++;
                      allChildsAreVirtual = false;
                    }
                    totalCount++;
                  }
                }

                int isVirtual = 0;
                if (allChildsAreVirtual == true)
                {
                  Logger.Debug("MediaLibrary: All children with role {0} of parent media item {1} with role {2} are virtual", childRole, parentId.Value, parentRole);
                  isVirtual = 1;
                }
                else
                {
                  Logger.Debug("MediaLibrary: Not all children with role {0} of parent media item {1} with role {2} are virtual", childRole, parentId.Value, parentRole);
                }

                command.Parameters.Clear();
                database.AddParameter(command, "PARENT_ITEM", parentId.Value, typeof(Guid));

                //Set parent virtual flag
                command.CommandText = "UPDATE " + _miaManagement.GetMIATableName(MediaAspect.Metadata) +
                " SET " + _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL) + " = " + isVirtual +
                " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @PARENT_ITEM";
                command.ExecuteNonQuery();

                if(_virtualRoleHierarchyChildCount.ContainsKey(childRole))
                {
                  foreach (ChildCountDefinition def in _virtualRoleHierarchyChildCount[childRole])
                  {
                    if(def.ParentRole == parentRole)
                    {
                      //Set parent child count
                      command.CommandText = "UPDATE " + _miaManagement.GetMIATableName(def.ParentMiaType) +
                      " SET " + _miaManagement.GetMIAAttributeColumnName(def.ChildCountAttribute) + " = " + 
                      (def.IncludeVirtual ? totalCount : availableCount) +
                      " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @PARENT_ITEM";
                      command.ExecuteNonQuery();
                    }
                  }
                }

                Logger.Debug("MediaLibrary: Set parent media item {0} with role {1} to virtual = {2}", parentId.Value, parentRole, isVirtual);
              }
            }
          }
        }
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error updating parent media item {0} virtual flag", e, mediaItemId);
        throw;
      }
    }

    private void UpdateAllParentUserData(ISQLDatabase database, ITransaction transaction, Guid mediaItemId)
    {
      try
      {
        using (IDbCommand command = transaction.CreateCommand())
        {
          foreach (Guid childRole in _virtualRoleHierarchy.Keys)
          {
            foreach (Guid parentRole in _virtualRoleHierarchy[childRole])
            {
              command.Parameters.Clear();
              database.AddParameter(command, "ITEM_ID", mediaItemId, typeof(Guid));
              database.AddParameter(command, "PARENT_ROLE_ID", parentRole, typeof(Guid));
              database.AddParameter(command, "ROLE_ID", childRole, typeof(Guid));

              //Find parents
              Dictionary<Guid, Guid> userDataParent = new Dictionary<Guid, Guid>();
              command.CommandText = "SELECT DISTINCT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
                ", " + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME +
                " FROM " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME +
                " WHERE " + UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + " = '" + UserDataKeysKnown.KEY_PLAY_PERCENTAGE + "'" +
                " AND " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN (" +
                " SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
                " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
                " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE) + " = @PARENT_ROLE_ID" +
                " AND " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE) + " = @ROLE_ID" +
                " AND " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID" +
                " UNION " +
                " SELECT " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) +
                " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
                " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE) + " = @PARENT_ROLE_ID" +
                " AND " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE) + " = @ROLE_ID" +
                " AND " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID" +
                ")";
              using (IDataReader reader = command.ExecuteReader())
              {
                while (reader.Read())
                {
                  userDataParent.Add(database.ReadDBValue<Guid>(reader, 0), database.ReadDBValue<Guid>(reader, 1));
                }
              }

              //Update parents
              foreach (var key in userDataParent)
              {
                //Find children
                command.Parameters.Clear();
                database.AddParameter(command, "ITEM_ID", key.Key, typeof(Guid));
                database.AddParameter(command, "PARENT_ROLE_ID", parentRole, typeof(Guid));
                database.AddParameter(command, "ROLE_ID", childRole, typeof(Guid));
                database.AddParameter(command, "USER_PROFILE_ID", key.Value, typeof(Guid));
                database.AddParameter(command, "USER_DATA_KEY", UserDataKeysKnown.KEY_PLAY_COUNT, typeof(string));

                float nonVirtualChildCount = 0;
                float watchedCount = 0;
                command.CommandText = "SELECT M." + _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL) +
                    ", U." + UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME +
                    ", M." + _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_PLAYCOUNT) +
                    " FROM " + _miaManagement.GetMIATableName(MediaAspect.Metadata) + " M" +
                    " LEFT OUTER JOIN " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME + " U" +
                    " ON U." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = M." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
                    " AND U." + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + " = @USER_PROFILE_ID" +
                    " AND U." + UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + " = @USER_DATA_KEY" +
                    " WHERE M." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN (" +
                    " SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
                    " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
                    " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE) + " = @ROLE_ID" +
                    " AND " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE) + " = @PARENT_ROLE_ID" +
                    " AND " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID" +
                    " UNION " +
                    " SELECT " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) +
                    " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
                    " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE) + " = @ROLE_ID" +
                    " AND " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE) + " = @PARENT_ROLE_ID" +
                    " AND " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID" +
                    ")";
                using (IDataReader reader = command.ExecuteReader())
                {
                  while (reader.Read())
                  {
                    bool? childVirtual = database.ReadDBValue<bool?>(reader, 0);
                    if (childVirtual == false)
                    {
                      nonVirtualChildCount++;
                    }
                    int playCount = 0;
                    if (int.TryParse(database.ReadDBValue<string>(reader, 1), out playCount))
                    {
                      if(playCount > 0)
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

                //Update parent
                command.Parameters.Clear();
                database.AddParameter(command, "ITEM_ID", key.Key, typeof(Guid));
                database.AddParameter(command, "USER_PROFILE_ID", key.Value, typeof(Guid));
                database.AddParameter(command, "USER_DATA_KEY", UserDataKeysKnown.KEY_PLAY_PERCENTAGE, typeof(string));

                int watchPercentage = nonVirtualChildCount <= 0 ? 100 : Convert.ToInt32((watchedCount * 100F) / nonVirtualChildCount);
                if (watchPercentage >= 100)
                  watchPercentage = 100;
                command.CommandText = "UPDATE " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME +
                  " SET " + UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME + " = " + watchPercentage +
                  " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID" +
                  " AND " + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + " = @USER_PROFILE_ID" +
                  " AND " + UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + " = @USER_DATA_KEY";
                if (command.ExecuteNonQuery() > 0)
                {
                  Logger.Debug("MediaLibrary: Set parent media item {0} with role {1} watch percentage = {2}", key.Key, parentRole, watchPercentage);
                }
              }
            }
          }
        }
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error updating media item {0} parents user data", e, mediaItemId);
        throw;
      }
    }

    private void UpdateParentUserData(Guid userProfileId, Guid mediaItemId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      
      try
      {
        Dictionary<Guid, int> parentPercentages = new Dictionary<Guid, int>();
        using (ITransaction transaction = database.BeginTransaction())
        {
          using (IDbCommand command = transaction.CreateCommand())
          {
            //Find parent if possible
            Guid? parentId;
            string parentIdColumn = null;
            string parentRoleColumn = null;
            string childIdColumn = null;
            string childRoleColumn = null;
            List<Guid> parentsToDelete = new List<Guid>();
            List<Guid> childsToDelete = new List<Guid>();
            foreach (Guid childRole in _virtualRoleHierarchy.Keys)
            {
              foreach (Guid parentRole in _virtualRoleHierarchy[childRole])
              {
                parentId = null;
                parentId = null;
                parentIdColumn = null;
                parentRoleColumn = null;
                childIdColumn = null;
                childRoleColumn = null;

                if (TryFindParent(database, transaction, mediaItemId, childRole, parentRole, out parentId, out childIdColumn, out childRoleColumn, out parentIdColumn, out parentRoleColumn))
                {
                  command.Parameters.Clear();
                  database.AddParameter(command, "ITEM_ID", parentId.Value, typeof(Guid));
                  database.AddParameter(command, "ROLE_ID", childRole, typeof(Guid));
                  database.AddParameter(command, "PARENT_ROLE_ID", parentRole, typeof(Guid));
                  database.AddParameter(command, "USER_PROFILE_ID", userProfileId, typeof(Guid));
                  database.AddParameter(command, "USER_DATA_KEY", UserDataKeysKnown.KEY_PLAY_COUNT, typeof(string));

                  //Find all childs
                  float nonVirtualChildCount = 0;
                  float watchedCount = 0;
                  command.CommandText = "SELECT M." + _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL) +
                      ", U." + UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME +
                      " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) + " R" +
                      " JOIN " + _miaManagement.GetMIATableName(MediaAspect.Metadata) + " M" +
                      " ON M." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = R." + childIdColumn +
                      " LEFT OUTER JOIN " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME + " U" +
                      " ON U." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = R." + childIdColumn +
                      " AND U." + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + " = @USER_PROFILE_ID" +
                      " AND U." + UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + " = @USER_DATA_KEY" +
                      " WHERE R." + childRoleColumn + " = @ROLE_ID" +
                      " AND R." + parentRoleColumn + " = @PARENT_ROLE_ID" +
                      " AND R." + parentIdColumn + " = @ITEM_ID";
                  using (IDataReader reader = command.ExecuteReader())
                  {
                    while (reader.Read())
                    {
                      bool? childVirtual = database.ReadDBValue<bool?>(reader, 0);
                      if (childVirtual == false)
                      {
                        nonVirtualChildCount++;
                      }
                      int playCount = 0;
                      if (int.TryParse(database.ReadDBValue<string>(reader, 1), out playCount) && playCount > 0)
                      {
                        watchedCount++;
                      }
                    }
                  }

                  int watchPercentage = nonVirtualChildCount <= 0 ? 100 : Convert.ToInt32((watchedCount * 100F) / nonVirtualChildCount);
                  if (watchPercentage >= 100)
                    watchPercentage = 100;
                  parentPercentages.Add(parentId.Value, watchPercentage);
                  Logger.Debug("MediaLibrary: Set parent media item {0} with role {1} watch percentage = {2}", parentId.Value, parentRole, watchPercentage);
                }
              }
            }
          }
        }

        IUserProfileDataManagement userManager = ServiceRegistration.Get<IUserProfileDataManagement>();
        foreach (var key in parentPercentages)
          userManager.SetUserMediaItemData(userProfileId, key.Key, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, key.Value.ToString());
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error updating parent media item {0} user data", e, mediaItemId);
        throw;
      }
    }

    public void DeleteMediaItemOrPath(string systemId, ResourcePath path, bool inclusive)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        DeleteAllMediaItemsUnderPath(transaction, systemId, path, inclusive);
        transaction.Commit();
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error deleting media item(s) of system '{0}' in path '{1}'",
            e, systemId, path.Serialize());
        transaction.Rollback();
        throw;
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
          ((ICollection<Guid>) value).Add(shareId);
        else
          client.Properties[KEY_CURRENTLY_IMPORTING_SHARE_IDS] = new List<Guid> {shareId};
      }
      ContentDirectoryMessaging.SendShareImportMessage(ContentDirectoryMessaging.MessageType.ShareImportStarted, shareId);
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
          ((ICollection<Guid>) value).Remove(shareId);
      }
      ContentDirectoryMessaging.SendShareImportMessage(ContentDirectoryMessaging.MessageType.ShareImportCompleted, shareId);
    }

    public ICollection<Guid> GetCurrentlyImportingShareIds()
    {
      ICollection<Guid> result = new List<Guid>();
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();
      // Shares of media library
      ICollection<Share> shares = GetShares(null).Values;
      CollectionUtils.AddAll(result, importerWorker.ImportJobs.Where(importJobInfo => importJobInfo.State == ImportJobState.Active).
          Select(importJobInfo => shares.BestContainingPath(importJobInfo.BasePath)).Where(share => share != null).Select(share => share.ShareId));
      // Client shares
      IClientManager clientManager = ServiceRegistration.Get<IClientManager>();
      lock (clientManager.SyncObj)
        CollectionUtils.AddAll(result, clientManager.ConnectedClients.Select(client =>
          {
            object value;
            return client.Properties.TryGetValue(KEY_CURRENTLY_IMPORTING_SHARE_IDS, out value) ? (ICollection<Guid>) value : null;
          }).Where(clientShares => clientShares != null).SelectMany(clientShares => clientShares).ToList());

      return result;
    }

    #endregion

    #region Playback

    public void NotifyPlayback(Guid mediaItemId, bool watched)
    {
      MediaItem item = Search(new MediaItemQuery(new Guid[] {MediaAspect.ASPECT_ID}, null, new MediaItemIdFilter(mediaItemId)), false, null, true).FirstOrDefault();
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
      UpdateMediaItem(mediaItemId, new MediaItemAspect[] {mediaAspect}, true);
    }

    #endregion

    #region User data management

    public void UserDataUpdated(Guid userProfileId, Guid mediaItemId, string userDataKey)
    {
      if (userDataKey != UserDataKeysKnown.KEY_PLAY_COUNT)
        return;
      UpdateParentUserData(userProfileId, mediaItemId);
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

    public void AddMediaItemAspectStorage(MediaItemAspectMetadata miam, MediaItemAspectMetadata.AttributeSpecification[] fkSpecs, MediaItemAspectMetadata refMiam, MediaItemAspectMetadata.AttributeSpecification[] refSpecs)
    {
      if (_miaManagement.AddMediaItemAspectStorage(miam, fkSpecs, refMiam, refSpecs))
      {
        Logger.Info("MediaLibrary: Media item aspect storage for MIA type '{0}' (name '{1}') with dependency on MIA type '{2}' (name '{3}') was added", miam.AspectId, miam.Name, refMiam.AspectId, refMiam.Name);
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

    public void RegisterMediaItemAspectRoleHierarchy(Guid childRole, Guid parentRole)
    {
      if(!_virtualRoleHierarchy.ContainsKey(childRole))
        _virtualRoleHierarchy.Add(childRole, new List<Guid>());
      _virtualRoleHierarchy[childRole].Add(parentRole);
    }

    public void RegisterMediaItemAspectRoleHierarchyChildCountAttribute(Guid childRole, Guid parentRole, MediaItemAspectMetadata parentMiaType,
      MediaItemAspectMetadata.AttributeSpecification childCountAttribute, bool includeVirtual)
    {
      if (!_virtualRoleHierarchyChildCount.ContainsKey(childRole))
        _virtualRoleHierarchyChildCount.Add(childRole, new List<ChildCountDefinition>());
      ChildCountDefinition def = new ChildCountDefinition()
      {
        ParentRole = parentRole,
        ParentMiaType = parentMiaType,
        ChildCountAttribute = childCountAttribute,
        IncludeVirtual = includeVirtual
      };
      _virtualRoleHierarchyChildCount[childRole].Add(def);
    }

    #endregion

    #region Shares management

    private void InitShareWatchers()
    {
      IDictionary<Guid, Share> shares = GetShares(_localSystemId);
      foreach (Share share in shares.Values)
      {
        try
        {
          ShareWatcher watcher = new ShareWatcher(share, this, true);
          _shareWatchers.Add(share.ShareId, watcher);
        }
        catch (Exception e)
        {
          Logger.Error("MediaLibrary: Error initializing share watcher for {0}", e, share.BaseResourcePath);
        }
      }
    }

    private void DeInitShareWatchers()
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
        Logger.Error("MediaLibrary: Error initializing shares", e);
        throw;
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
            share.BaseResourcePath, share.Name))
          command.ExecuteNonQuery();

        foreach (string mediaCategory in share.MediaCategories)
          AddMediaCategoryToShare(transaction, share.ShareId, mediaCategory);

        transaction.Commit();

        ContentDirectoryMessaging.SendRegisteredSharesChangedMessage();

        TryScheduleLocalShareImport(share);

        ShareWatcher watcher = new ShareWatcher(share, this, false);
        _shareWatchers.Add(share.ShareId, watcher);
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error registering share '{0}'", e, share.ShareId);
        transaction.Rollback();
        throw;
      }
    }

    public Guid CreateShare(string systemId, ResourcePath baseResourcePath, string shareName,
        IEnumerable<string> mediaCategories)
    {
      Guid shareId = Guid.NewGuid();
      Logger.Info("MediaLibrary: Creating new share '{0}'", shareId);
      Share share = new Share(shareId, systemId, baseResourcePath, shareName, mediaCategories);
      RegisterShare(share);
      return shareId;
    }

    public void RemoveShare(Guid shareId)
    {
      Logger.Info("MediaLibrary: Removing share '{0}'", shareId);
      Share share = GetShare(shareId);
      TryCancelLocalImportJobs(share);

      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = MediaLibrary_SubSchema.DeleteSharesCommand(transaction, new Guid[] {shareId}))
          command.ExecuteNonQuery();

        DeleteAllMediaItemsUnderPath(transaction, share.SystemId, share.BaseResourcePath, true);

        transaction.Commit();

        _shareWatchers[shareId].Dispose();
        _shareWatchers.Remove(shareId);

        ContentDirectoryMessaging.SendRegisteredSharesChangedMessage();
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error removing share '{0}'", e, shareId);
        transaction.Rollback();
        throw;
      }
    }

    public void RemoveSharesOfSystem(string systemId)
    {
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

        DeleteAllMediaItemsUnderPath(transaction, systemId, null, true);

        transaction.Commit();

        if(systemId == _localSystemId)
        {
          foreach (Guid shareId in _shareWatchers.Keys)
          {
            _shareWatchers[shareId].Dispose();
          }
          _shareWatchers.Clear();
        }
        
        ContentDirectoryMessaging.SendRegisteredSharesChangedMessage();
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error removing shares of system '{0}'", e, systemId);
        transaction.Rollback();
        throw;
      }
    }

    public int UpdateShare(Guid shareId, ResourcePath baseResourcePath, string shareName,
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

        using (IDbCommand command = MediaLibrary_SubSchema.UpdateShareCommand(transaction, shareId, baseResourcePath, shareName))
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
            numAffected = DeleteAllMediaItemsUnderPath(transaction, originalShare.SystemId, originalShare.BaseResourcePath, true);
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
        IDbCommand command;
        if (string.IsNullOrEmpty(systemId))
          command = MediaLibrary_SubSchema.SelectSharesCommand(transaction, out shareIdIndex,
              out systemIdIndex, out pathIndex, out shareNameIndex);
        else
          command = MediaLibrary_SubSchema.SelectSharesBySystemCommand(transaction, systemId, out shareIdIndex,
              out systemIdIndex, out pathIndex, out shareNameIndex);
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
                  database.ReadDBValue<string>(reader, shareNameIndex), null));
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
