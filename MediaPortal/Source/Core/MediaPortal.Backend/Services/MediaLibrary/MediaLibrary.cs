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
          IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs)
      {
        try
        {
          return _parent.LoadItem(_parent.LocalSystemId, path, necessaryRequestedMIATypeIDs, optionalRequestedMIATypeIDs);
        }
        catch (Exception)
        {
          throw new DisconnectedException();
        }
      }

      public IList<MediaItem> Browse(Guid parentDirectoryId,
          IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs, uint? offset = null, uint? limit = null)
      {
        try
        {
          return _parent.Browse(parentDirectoryId, necessaryRequestedMIATypeIDs, optionalRequestedMIATypeIDs, offset, limit);
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
    }

    protected class ImportResultHandler : IImportResultHandler
    {
      protected MediaLibrary _parent;

      public ImportResultHandler(MediaLibrary parent)
      {
        _parent = parent;
      }

      public Guid UpdateMediaItem(Guid parentDirectoryId, ResourcePath path, IEnumerable<MediaItemAspect> updatedAspects)
      {
        try
        {
          return _parent.AddOrUpdateMediaItem(parentDirectoryId, _parent.LocalSystemId, path, updatedAspects);
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

    protected volatile bool _mediaPendingOpsAllowed = true;
    protected Thread _mediaReconcilerThread;
    protected BlockingCollection<Guid> _mediaReconcilerQueue = new BlockingCollection<Guid>();
    protected Thread _mediaDeleterThread;
    protected BlockingCollection<Guid> _mediaDeleterQueue = new BlockingCollection<Guid>();
    protected Dictionary<Guid, Guid> _virtualRoleDependencies = new Dictionary<Guid, Guid>();
    protected Dictionary<int, List<Guid>> _operationCache = new Dictionary<int, List<Guid>>();

    #endregion

    #region Ctor & dtor

    public MediaLibrary()
    {
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      _localSystemId = systemResolver.LocalSystemId;

      _operationCache.Add(MediaLibrary_SubSchema.MEDIA_ITEM_RECONCILE_OP, new List<Guid>());
      _operationCache.Add(MediaLibrary_SubSchema.MEDIA_ITEM_DELETE_OP, new List<Guid>());

      _mediaBrowsingCallback = new MediaBrowsingCallback(this);
      _importResultHandler = new ImportResultHandler(this);
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            ImporterWorkerMessaging.CHANNEL
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();

      _mediaReconcilerThread = new Thread(Reconcile) { Name = "MediaReconciler", Priority = ThreadPriority.Lowest };
      _mediaReconcilerThread.Start();

      _mediaDeleterThread = new Thread(Delete) { Name = "MediaDeleter", Priority = ThreadPriority.Lowest };
      _mediaDeleterThread.Start();
    }

    public void Dispose()
    {
      _messageQueue.Shutdown();

      lock(_syncObj)
      {
        _mediaReconcilerQueue.CompleteAdding();
        _mediaDeleterQueue.CompleteAdding();
      }
      _mediaPendingOpsAllowed = false;

      if (!_mediaReconcilerThread.Join(5000))
        _mediaReconcilerThread.Abort();

      _mediaReconcilerThread = null;

      if (!_mediaDeleterThread.Join(5000))
        _mediaDeleterThread.Abort();

      _mediaDeleterThread = null;
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
      Logger.Info("Creating media item {0}", mediaItemId);
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

        string path = StringUtils.RemoveSuffixIfPresent(basePath.Serialize(), "/");
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
        using (IDbCommand command = transaction.CreateCommand())
      {
          database.AddParameter(command, "ITEM_ID", parentId.Value, typeof(Guid));

          //Set virtual tag
          commandStr = "UPDATE " + _miaManagement.GetMIATableName(MediaAspect.Metadata) +
            " SET " + _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL) + " = 1" +
            " WHERE " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + " = @ITEM_ID";
          command.CommandText = commandStr;
          affectedRows = command.ExecuteNonQuery();

          //Delete all remaining resources except first one
          commandStr = "DELETE FROM " + providerAspectTable +
            " WHERE " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + " = @ITEM_ID"+
            " AND " + _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_INDEX) + " > 0";
          command.CommandText = commandStr;
          affectedRows += command.ExecuteNonQuery();

          //Convert first resource to virtual resource
          database.AddParameter(command, "VIRT_PATH", VirtualResourceProvider.ToResourcePath(parentId.Value).Serialize(), typeof(string));

          commandStr = "UPDATE " + providerAspectTable +
            " SET " + _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH) + " = @VIRT_PATH, " +
            _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_MIME_TYPE) + " = NULL, " +
            _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SIZE) + " = NULL, " +
            _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID) + " = NULL" +
            " WHERE " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + " = @ITEM_ID";
          command.CommandText = commandStr;
          affectedRows += command.ExecuteNonQuery();
        }

        //Check if new virtual media item should be deleted
        DeletePureVirtual(transaction.Database, transaction, parentId.Value);
      }
      return affectedRows;
    }

    protected int DeleteAllMediaItemsUnderPath(ITransaction transaction, string systemId, ResourcePath basePath, bool inclusive)
    {
      MediaItemAspectMetadata providerAspectMetadata = ProviderResourceAspect.Metadata;
      string providerAspectTable = _miaManagement.GetMIATableName(providerAspectMetadata);
      string systemIdAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SYSTEM_ID);
      string pathAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      string commandStr = "SELECT "+ MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " FROM " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN (" +
              // TODO: Replace this inner select statement by a select statement generated from an appropriate item query
              "SELECT " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + providerAspectTable +
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

        commandStr = commandStr + ")";

        command.CommandText = commandStr;

        using (IDataReader reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            Delete(transaction, database.ReadDBValue<Guid>(reader, 0));
            affectedRows++;
          }
        }
      }
      return affectedRows;
    }

    protected void DeleteMediaItem(ITransaction transaction, Guid mediaItemId)
    {
      string commandStr = "DELETE FROM " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID";

      using (IDbCommand command = transaction.CreateCommand())
      {
        transaction.Database.AddParameter(command, "ITEM_ID", mediaItemId, typeof(Guid));

        command.CommandText = commandStr;
        command.ExecuteNonQuery();
      }
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

      //Load pending operations
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      using (ITransaction transaction = database.BeginTransaction())
      {
        using (IDbCommand command = MediaLibrary_SubSchema.SelectMediaItemPendingOperationCommand(transaction, MediaLibrary_SubSchema.MEDIA_ITEM_RECONCILE_OP))
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              _operationCache[MediaLibrary_SubSchema.MEDIA_ITEM_RECONCILE_OP].Add(database.ReadDBValue<Guid>(reader, 0));
              _mediaReconcilerQueue.Add(database.ReadDBValue<Guid>(reader, 0));
            }
            reader.Close();
          }
        }

        using (IDbCommand command = MediaLibrary_SubSchema.SelectMediaItemPendingOperationCommand(transaction, MediaLibrary_SubSchema.MEDIA_ITEM_DELETE_OP))
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              _operationCache[MediaLibrary_SubSchema.MEDIA_ITEM_DELETE_OP].Add(database.ReadDBValue<Guid>(reader, 0));
              _mediaDeleterQueue.Add(database.ReadDBValue<Guid>(reader, 0));
            }
            reader.Close();
          }
        }
      }
    }

    public void ActivateImporterWorker()
    {
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();
      importerWorker.Activate(_mediaBrowsingCallback, _importResultHandler);
    }

    public void Shutdown()
    {
      NotifySystemOffline(_localSystemId);
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();
      importerWorker.Suspend();
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
        IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs)
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
        
        return result;
      }
    }

    public IList<MediaItem> Browse(Guid parentDirectoryId,
        IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs, uint? offset = null, uint? limit = null)
    {
      lock (_syncObj)
      {
        MediaItemQuery browseQuery = BuildBrowseQuery(parentDirectoryId);
        browseQuery.SetNecessaryRequestedMIATypeIDs(necessaryRequestedMIATypeIDs);
        browseQuery.SetOptionalRequestedMIATypeIDs(optionalRequestedMIATypeIDs);
        browseQuery.Limit = limit;
        browseQuery.Offset = offset;
        return Search(browseQuery, false);
      }
    }

    public IList<MediaItem> Search(MediaItemQuery query, bool filterOnlyOnline)
    {
      return Search(null, null , query, filterOnlyOnline);
    }

    public IList<MediaItem> Search(ISQLDatabase database, ITransaction transaction, MediaItemQuery query, bool filterOnlyOnline)
    {
      // We add the provider resource aspect to the necessary aspect types be able to filter online systems
      MediaItemQuery executeQuery = query;
      if (filterOnlyOnline)
      {
        executeQuery = new MediaItemQuery(query); // Use constructor by other query to make sure all properties are copied (including sorting and limits)
        executeQuery.NecessaryRequestedMIATypeIDs.Add(ProviderResourceAspect.ASPECT_ID);
        executeQuery.Filter = AddOnlyOnlineFilter(query.Filter);
      }
      CompiledMediaItemQuery cmiq = CompiledMediaItemQuery.Compile(_miaManagement, executeQuery);
      IList<MediaItem> items = null;
      if(database == null || transaction == null)
        items = cmiq.QueryList();
      else
        items = cmiq.QueryList(database, transaction);
      Logger.Debug("Found media items [{0}]", string.Join(",", items.Select(x => x.MediaItemId)));
      IList<MediaItem> result = new List<MediaItem>(items.Count);
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
        ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypeIDs, IFilter filter, bool filterOnlyOnline)
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
      CompiledGroupedAttributeValueQuery cdavq = CompiledGroupedAttributeValueQuery.Compile(_miaManagement,
          filterOnlyOnline ? necessaryMIATypeIDs.Union(new Guid[] {ProviderResourceAspect.ASPECT_ID}) :
          necessaryMIATypeIDs, attributeType, saf, selectProjectionFunctionImpl, projectionValueType, filterOnlyOnline ? AddOnlyOnlineFilter(filter) : filter);
      return cdavq.Execute();
    }

    public IList<MLQueryResultGroup> GroupValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IFilter selectAttributeFilter, ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypeIDs,
        IFilter filter, bool filterOnlyOnline, GroupingFunction groupingFunction)
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
          projectionFunction, necessaryMIATypeIDs, filter, filterOnlyOnline))
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

    public int CountMediaItems(IEnumerable<Guid> necessaryMIATypeIDs, IFilter filter, bool filterOnlyOnline)
    {
      CompiledCountItemsQuery cciq = CompiledCountItemsQuery.Compile(_miaManagement,
          necessaryMIATypeIDs, filterOnlyOnline ? AddOnlyOnlineFilter(filter) : filter);
      return cciq.Execute();
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
      foreach (MediaItem item in Search(query, false))
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

    public Guid AddOrUpdateMediaItem(Guid parentDirectoryId, string systemId, ResourcePath path, IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      return AddOrUpdateMediaItem(parentDirectoryId, systemId, path, null, mediaItemAspects, true);
    }

    private Guid AddOrUpdateMediaItem(Guid parentDirectoryId, string systemId, ResourcePath path, Guid? newMediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects, bool reconcile)
    {
#if DEBUG
      Logger.Info("Adding to {0} on {1} in {2}:\n{3}", parentDirectoryId, systemId, path, MediaItemAspect.GetInfo(mediaItemAspects, _miaManagement.ManagedMediaItemAspectTypes));
#endif
      // TODO: Avoid multiple write operations to the same media item
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        Guid? mediaItemId = GetMediaItemId(transaction, systemId, path);
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

        Guid? mergedMediaItem = MergeWithExisting(database, transaction, mediaItemAspects, pra);
        if (mergedMediaItem != null)
        {
          if (mergedMediaItem == Guid.Empty)
          {
            transaction.Rollback();

            Logger.Info("Media item cannot be saved. Needs to be merged");

            return Guid.Empty;
          }

          Logger.Info("Merged into media item {0}", mergedMediaItem.Value);

          if(mediaItemId.HasValue && mergedMediaItem.Value != mediaItemId.Value)
          {
            DeleteMediaItem(transaction, mediaItemId.Value);
          }

          //Aspects were merged into an existing media item. Discard the remaining aspects
          transaction.Commit();

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
            Logger.Warn("MediaLibrary.AddOrUpdateMediaItem: Client tried to update ImporterAspect");
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

        Logger.Info("Committed media item {0}", mediaItemId.Value);

        if(reconcile)
          Reconcile(mediaItemId.Value);

        return mediaItemId.Value;
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error adding or updating media item(s) in path '{0}'", e, (path != null ? path.Serialize() : null));
        transaction.Rollback();
        throw;
      }
    }

    private bool BeginOperation(int operationType, Guid mediaItemId)
    {
      lock (_operationCache[operationType])
      {
        if (!_operationCache[operationType].Contains(mediaItemId))
        {
          ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
          ITransaction transaction = database.BeginTransaction();

          try
          {
            using (IDbCommand command = MediaLibrary_SubSchema.InsertMediaItemPendingOperationCommand(transaction, mediaItemId, operationType))
              command.ExecuteNonQuery();
            transaction.Commit();
            _operationCache[operationType].Add(mediaItemId);
            return true;
          }
          catch (Exception e)
          {
            Logger.Error("MediaLibrary: Error queuing operation {0} for media item {1}", e, operationType, mediaItemId);
            transaction.Rollback();
          }
        }
      }
      return false;
    }

    private bool BeginOperation(ITransaction transaction, int operationType, Guid mediaItemId)
    {
      lock (_operationCache[operationType])
      {
        if (!_operationCache[operationType].Contains(mediaItemId))
        {
          try
          {
            using (IDbCommand command = MediaLibrary_SubSchema.InsertMediaItemPendingOperationCommand(transaction, mediaItemId, operationType))
              command.ExecuteNonQuery();
            _operationCache[operationType].Add(mediaItemId);
            return true;
          }
          catch (Exception e)
          {
            Logger.Error("MediaLibrary: Error queuing operation {0} for media item {1}", e, operationType, mediaItemId);
          }
        }
      }
      return false;
    }

    private bool EndOperation(int operationType, Guid mediaItemId)
    {
      lock (_operationCache[operationType])
      {
        ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
        ITransaction transaction = database.BeginTransaction();

        try
        {
          using (IDbCommand command = MediaLibrary_SubSchema.DeleteMediaItemPendingOperationCommand(transaction, mediaItemId, operationType))
            command.ExecuteNonQuery();
          transaction.Commit();
          _operationCache[operationType].Remove(mediaItemId);
          return true;
        }
        catch (Exception e)
        {
          Logger.Error("MediaLibrary: Error unqueuing operation {0} for media item {1}", e, operationType, mediaItemId);
          transaction.Rollback();
        }
      }
      return false;
    }

    protected virtual void Reconcile(Guid mediaItemId)
    {
      BeginOperation(MediaLibrary_SubSchema.MEDIA_ITEM_RECONCILE_OP, mediaItemId);
      _mediaReconcilerQueue.Add(mediaItemId);
    }

    protected virtual void Delete(Guid mediaItemId)
    {
      BeginOperation(MediaLibrary_SubSchema.MEDIA_ITEM_DELETE_OP, mediaItemId);
      _mediaDeleterQueue.Add(mediaItemId);
    }

    protected virtual void Delete(ITransaction transaction, Guid mediaItemId)
    {
      BeginOperation(transaction, MediaLibrary_SubSchema.MEDIA_ITEM_DELETE_OP, mediaItemId);
      _mediaDeleterQueue.Add(mediaItemId);
    }

    public void UpdateMediaItem(Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      UpdateMediaItem(mediaItemId, mediaItemAspects, true);
    }

    private void UpdateMediaItem(Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects, bool reconcile)
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
            Logger.Warn("MediaLibrary.AddOrUpdateMediaItem: Client tried to update either ImporterAspect or ProviderResourceAspect");
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
          Reconcile(mediaItemId);
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error updating media item with id '{0}'", e, mediaItemId);
        transaction.Rollback();
        throw;
      }
    }

    protected virtual Guid? MergeWithExisting(ISQLDatabase database, ITransaction transaction, IEnumerable<MediaItemAspect> extractedAspectList, MediaItemAspect extractedProviderResourceAspects)
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
          if (found)
          {
            Logger.Info("Found mergeable media item {0}", existingMediaItemId);

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

              UpdateMergedMediaItem(database, transaction, existingMediaItemId, existingAspects.Values.SelectMany(x => x));
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
          if (type != mergeHandler.ExternalIdType)
            continue;

          // Search using external identifiers
          BooleanCombinationFilter filter = new BooleanCombinationFilter(BooleanOperator.And, new[]
                {
                  new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                  new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                  new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
                });
          Logger.Info("Searching for existing items matching {0} / {1} / {2} with [{3}]", source, type, id, string.Join(",", mergeHandler.MergeableAspects.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));

          IList<MediaItem> existingItems = Search(database, transaction, new MediaItemQuery(mergeHandler.MergeableAspects, GetManagedMediaItemAspectMetadata().Keys.Except(mergeHandler.MergeableAspects), filter), false);
          foreach (MediaItem existingItem in existingItems)
          {
            Logger.Info("Checking existing item {0} with [{1}]", existingItem.MediaItemId, string.Join(",", existingItem.Aspects.Keys.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));
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

    private void Reconcile()
	  {
	    while (_mediaPendingOpsAllowed)
	    {
	      foreach (Guid mediaItemId in _mediaReconcilerQueue.GetConsumingEnumerable())
	      {
	        try
	        {
	          UpdateRelationships(mediaItemId);
            EndOperation(MediaLibrary_SubSchema.MEDIA_ITEM_RECONCILE_OP, mediaItemId);
          }
	        catch (Exception e)
	        {
	          Logger.Error("MediaLibrary: Cannot update relationships for {0}", e, mediaItemId);
	        }
	      }
	    }
	  }

    protected virtual void UpdateRelationships(Guid mediaItemId)
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();

      Logger.Info("Updating relationships for {0}", mediaItemId);
      MediaItem item = Search(new MediaItemQuery(null, GetManagedMediaItemAspectMetadata().Keys, new MediaItemIdFilter(mediaItemId)), false).FirstOrDefault();
      if (item == null)
      {
        // Item deleted on the main thread before the reconciler thread processes it - could happen?
        Logger.Warn("Cannot find {0}", mediaItemId);
        return;
      }
      //Logger.Info("Found item {0} with [{1}]", item.MediaItemId, string.Join(",", item.Aspects.Keys.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));

      // TODO: What happens to MIAs that the reconciler automatically adds which have been removed manually by the user?

      foreach (IRelationshipExtractor extractor in mediaAccessor.LocalRelationshipExtractors.Values)
      {
        foreach (IRelationshipRoleExtractor roleExtractor in extractor.RoleExtractors)
        {
          UpdateRelationship(roleExtractor, mediaItemId, item.Aspects);
        }
      }
    }

    private void UpdateRelationship(IRelationshipRoleExtractor roleExtractor, Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IList<Guid> roleAspectIds = new List<Guid>(roleExtractor.RoleAspects);
      roleAspectIds.Add(MediaAspect.ASPECT_ID);

      IList<Guid> linkedRoleAspectIds = new List<Guid>(roleExtractor.LinkedRoleAspects);
      linkedRoleAspectIds.Add(MediaAspect.ASPECT_ID);

      // Any usable item must contain all of roleExtractor.RoleAspects
      if (roleAspectIds.Except(aspects.Keys).Any())
        return;

      ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedItems;
      if (!roleExtractor.TryExtractRelationships(aspects, out extractedItems, false))
      {
        Logger.Info("Extractor {0} extracted {1} media items", roleExtractor.GetType().Name, 0);
        return;
      }
      Logger.Info("Extractor {0} extracted {1} media items", roleExtractor.GetType().Name, extractedItems == null ? 0: extractedItems.Count);

      // Match the extracted aspect data to any items already in the library
      foreach (IDictionary<Guid, IList<MediaItemAspect>> extractedItem in extractedItems)
      {
        bool found = MatchExternalItem(roleExtractor, mediaItemId, aspects, extractedItem, linkedRoleAspectIds);
        if (!found)
        {
          Logger.Info("Adding new media item for extracted item");

          AddRelationship(roleExtractor, mediaItemId, aspects, extractedItem);

          Guid newMediaItemId = NewMediaItemId();
          AddOrUpdateMediaItem(Guid.Empty, _localSystemId, VirtualResourceProvider.ToResourcePath(newMediaItemId), newMediaItemId, extractedItem.Values.SelectMany(x => x), true);
        }
      }
    }

    private bool MatchExternalItem(IRelationshipRoleExtractor roleExtractor, Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> extractedItem, IList<Guid> linkedRoleAspectIds)
    {
      IList<MultipleMediaItemAspect> externalAspects;

      if (MediaItemAspect.TryGetAspects(extractedItem, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type != roleExtractor.ExternalIdType)
            continue;

          // Search using external identifiers
          BooleanCombinationFilter filter = new BooleanCombinationFilter(BooleanOperator.And, new[]
                {
                  new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                  new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                  new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
                });
          //Logger.Info("Searching for external items matching {0} / {1} / {2} with [{3}]", source, type, id, string.Join(",", linkedRoleAspectIds.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));
          // Any potential linked item must contain all of LinkedRoleAspects
          IList<MediaItem> externalItems = Search(new MediaItemQuery(linkedRoleAspectIds, GetManagedMediaItemAspectMetadata().Keys.Except(linkedRoleAspectIds), filter), false);
          foreach (MediaItem externalItem in externalItems)
          {
            //Logger.Info("Checking external item {0} with [{1}]", externalItem.MediaItemId, string.Join(",", externalItem.Aspects.Keys.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));
            if (roleExtractor.TryMatch(extractedItem, externalItem.Aspects))
            {
              //Logger.Info("Merging extracted item into external item {0}", externalItem.MediaItemId);

              AddRelationship(roleExtractor, mediaItemId, aspects, extractedItem);

              //Update virtual flag
              extractedItem[MediaAspect.ASPECT_ID][0].SetAttribute(MediaAspect.ATTR_ISVIRTUAL, externalItem.Aspects[MediaAspect.ASPECT_ID][0].GetAttributeValue(MediaAspect.ATTR_ISVIRTUAL));

              UpdateMediaItem(externalItem.MediaItemId, extractedItem.Values.SelectMany(x => x), false);

              return true;
            }
          }
        }
      }

      return false;
    }

    private void AddRelationship(IRelationshipRoleExtractor roleExtractor, Guid itemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects)
    {
      int index;
      if (!roleExtractor.TryGetRelationshipIndex(aspects, linkedAspects, out index))
        index = 0;
      //Logger.Info("Adding a {0} / {1} relationship linked to {2} at {3}", roleExtractor.LinkedRole, roleExtractor.Role, itemId, index);
      MediaItemAspect.AddOrUpdateRelationship(linkedAspects, roleExtractor.LinkedRole, roleExtractor.Role, itemId, index);
    }

    private void Delete()
    {
      while (_mediaPendingOpsAllowed)
      {
        foreach (Guid mediaItemId in _mediaDeleterQueue.GetConsumingEnumerable())
        {
        try
        {
            DeleteMediaItemAndReleationships(mediaItemId);
            EndOperation(MediaLibrary_SubSchema.MEDIA_ITEM_DELETE_OP, mediaItemId);
          }
        catch (Exception e)
        {
            Logger.Error("Error deleting media item {0}", e, mediaItemId);
          }
        }
      }
    }

    public void DeleteMediaItemAndReleationships(Guid mediaItemId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = transaction.CreateCommand())
        {
          database.AddParameter(command, "ITEM_ID", mediaItemId, typeof(Guid));

          //Delete item
          command.CommandText = "DELETE FROM " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID";
          command.ExecuteNonQuery();

          //Delete relations
          command.CommandText = "DELETE FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
              " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID";
          command.ExecuteNonQuery();

          //Delete all virtual items with no relation
          command.Parameters.Clear();

          command.CommandText = "SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
          " FROM " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN (" +

          "SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " FROM " +
          _miaManagement.GetMIATableName(MediaAspect.Metadata) +
          " WHERE " + _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL) + " = 1" +

          ") AND " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " NOT IN (" +

          "SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " FROM " +
          _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +

          ") AND " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " NOT IN (" +

          "SELECT " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " FROM " +
          _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +

          ")";
          using (IDataReader reader = command.ExecuteReader())
          {
            if (reader.Read())
            {
              Delete(transaction, database.ReadDBValue<Guid>(reader, 0));
            }
          }
        }
        
        transaction.Commit();
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error deleting media item {0}", e, mediaItemId);
        transaction.Rollback();
        throw;
      }
    }

    public void DeletePureVirtual(ISQLDatabase database, ITransaction transaction, Guid mediaItemId)
    {
      try
      {
        using (IDbCommand command = transaction.CreateCommand())
        {
          //Find parent if possible
          Guid? parentId;
          Guid parentRole;
          Guid childRole;
          bool lookupReverse = true;
          bool itemIsVirtual = true;
          List<Guid> parentsToDelete = new List<Guid>();
          foreach (KeyValuePair<Guid, Guid> virtualDependency in _virtualRoleDependencies)
          {
            parentId = null;
            lookupReverse = false;
            parentRole = virtualDependency.Value;
            childRole = virtualDependency.Key;

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

            if (!parentId.HasValue)
            {
              lookupReverse = true;

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
            }

            if (parentId.HasValue)
            {
              command.Parameters.Clear();
              database.AddParameter(command, "ITEM_ID", parentId.Value, typeof(Guid));
              database.AddParameter(command, "ROLE_ID", childRole, typeof(Guid));
              database.AddParameter(command, "PARENT_ROLE_ID", parentRole, typeof(Guid));

              //Find all childs
              List<Guid> childs = new List<Guid>();
              if (!lookupReverse)
              {
                command.CommandText = "SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
                  " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
                  " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE) + " = @ROLE_ID" +
                  " AND " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE) + " = @PARENT_ROLE_ID" +
                  " AND " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID";
              }
              else
              {
                command.CommandText = "SELECT " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) +
                  " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
                  " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE) + " = @ROLE_ID" +
                  " AND " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE) + " = @PARENT_ROLE_ID" +
                  " AND " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID";
              }
              using (IDataReader reader = command.ExecuteReader())
              {
                while (reader.Read())
                  childs.Add(database.ReadDBValue<Guid>(reader, 0));
              }

              //Check if there are any non virtual childs
              if (childs.Count > 0)
              {
                command.Parameters.Clear();

                IList<string> bindVars = new List<string>();
                int ct = 0;
                foreach (Guid child in childs)
                {
                  string bindVar = "V" + ct++;
                  database.AddParameter(command, bindVar, child, typeof(Guid));
                  bindVars.Add("@" + bindVar);
                }

                command.CommandText = "SELECT COUNT(*) FROM " + _miaManagement.GetMIATableName(MediaAspect.Metadata) +
                  " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN (" + string.Join(",", bindVars) + ")" +
                  " AND " + _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL) + " = 0";
                int nonVirtualChildCount = Convert.ToInt32(command.ExecuteScalar());
                if (nonVirtualChildCount == 0)
                {
                  //All childs are virtual so delete them
                  foreach (Guid childId in childs)
                    Delete(transaction, childId);

                  if(!parentsToDelete.Contains(parentId.Value))
                    parentsToDelete.Add(parentId.Value);
                }
                else
                {
                  itemIsVirtual = false;
                }
              }
            }
          }

          foreach(Guid parent in parentsToDelete)
            Delete(transaction, parent);

          if (itemIsVirtual)
            Delete(transaction, mediaItemId);
        }
      }
      catch (Exception e)
      {
        Logger.Error("MediaLibrary: Error deleting pure virtual media item {0}", e, mediaItemId);
        throw;
      }
    }

    public void DeleteMediaItemOrPath(string systemId, ResourcePath path, bool inclusive)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        if (inclusive && path != null)
          DeleteMediaItemResourcePath(transaction, systemId, path);
        else
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

    public void NotifyPlayback(Guid mediaItemId)
    {
      MediaItem item = Search(new MediaItemQuery(new Guid[] {MediaAspect.ASPECT_ID}, null, new MediaItemIdFilter(mediaItemId)), false).FirstOrDefault();
      if (item == null)
        return;
      SingleMediaItemAspect mediaAspect;
	    MediaItemAspect.TryGetAspect(item.Aspects, MediaAspect.Metadata, out mediaAspect);
      mediaAspect.SetAttribute(MediaAspect.ATTR_LASTPLAYED, DateTime.Now);
      int playCount = (int) (mediaAspect.GetAttributeValue(MediaAspect.ATTR_PLAYCOUNT) ?? 0);
      mediaAspect.SetAttribute(MediaAspect.ATTR_PLAYCOUNT, playCount + 1);
      UpdateMediaItem(mediaItemId, new MediaItemAspect[] {mediaAspect});
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
        Logger.Info(
            "MediaLibrary: Media item aspect storage for MIA type '{0}' (name '{1}') was added", miam.AspectId, miam.Name);
        ContentDirectoryMessaging.SendMIATypesChangedMessage();
      }
    }

    public void AddMediaItemAspectStorage(MediaItemAspectMetadata miam, MediaItemAspectMetadata.AttributeSpecification[] fkSpecs, MediaItemAspectMetadata refMiam, MediaItemAspectMetadata.AttributeSpecification[] refSpecs)
    {
      if (_miaManagement.AddMediaItemAspectStorage(miam, fkSpecs, refMiam, refSpecs))
      {
        Logger.Info(
            "MediaLibrary: Media item aspect storage for MIA type '{0}' (name '{1}') with dependency on MIA type '{2}' (name '{3}') was added", miam.AspectId, miam.Name, refMiam.AspectId, refMiam.Name);
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

    public void RegisterMediaItemAspectRoleDependency(Guid role, Guid parentRole)
    {
      _virtualRoleDependencies.Add(role, parentRole);
    }

    #endregion

    #region Shares management

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
