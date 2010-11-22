#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.Exceptions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.SystemResolver;
using MediaPortal.Utilities;
using MediaPortal.Utilities.DB;
using MediaPortal.Utilities.Exceptions;
using RelocationMode=MediaPortal.Backend.MediaLibrary.RelocationMode;

namespace MediaPortal.Backend.Services.MediaLibrary
{
  // TODO: Preparation of some SQL statements? We could use a lazy initialized DBCommand cache which prepares DBCommands
  // on the fly and holds up to N prepared commands.
  public class MediaLibrary : IMediaLibrary, IDisposable
  {
    protected class MediaBrowsingCallback : IMediaBrowsing
    {
      protected MediaLibrary _parent;

      public MediaBrowsingCallback(MediaLibrary parent)
      {
        _parent = parent;
      }

      public ICollection<MediaItem> Browse(ResourcePath path, IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs)
      {
        return _parent.Browse(_parent.LocalSystemId, path, necessaryRequestedMIATypeIDs, optionalRequestedMIATypeIDs);
      }
    }

    protected class ImportResultHandler : IImportResultHandler
    {
      protected MediaLibrary _parent;

      public ImportResultHandler(MediaLibrary parent)
      {
        _parent = parent;
      }

      public void UpdateMediaItem(ResourcePath path, IEnumerable<MediaItemAspect> updatedAspects)
      {
        _parent.AddOrUpdateMediaItem(_parent.LocalSystemId, path, updatedAspects);
      }

      public void DeleteMediaItem(ResourcePath path)
      {
        _parent.DeleteMediaItemOrPath(_parent.LocalSystemId, path);
      }
    }

    protected MIA_Management _miaManagement = null;
    protected IDictionary<string, SystemName> _systemsOnline = new Dictionary<string, SystemName>();
    protected object _syncObj = new object();
    protected string _localSystemId;
    protected IMediaBrowsing _mediaBrowsingCallback;
    protected IImportResultHandler _importResultHandler;

    public MediaLibrary()
    {
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      _localSystemId = systemResolver.LocalSystemId;

      _mediaBrowsingCallback = new MediaBrowsingCallback(this);
      _importResultHandler = new ImportResultHandler(this);
    }

    public void Dispose()
    {
    }

    public string LocalSystemId
    {
      get { return _localSystemId; }
    }

    protected Share GetShare(ITransaction transaction, Guid shareId)
    {
      int systemIdIndex;
      int pathIndex;
      int shareNameIndex;
      using (IDbCommand command = MediaLibrary_SubSchema.SelectShareByIdCommand(transaction, shareId, out systemIdIndex,
          out pathIndex, out shareNameIndex))
      using (IDataReader reader = command.ExecuteReader())
      {
        if (!reader.Read())
          return null;
        ICollection<string> mediaCategories = GetShareMediaCategories(transaction, shareId);
        return new Share(shareId, DBUtils.ReadDBValue<string>(reader, systemIdIndex), ResourcePath.Deserialize(
            DBUtils.ReadDBValue<string>(reader, pathIndex)),
            DBUtils.ReadDBValue<string>(reader, shareNameIndex), mediaCategories);
      }
    }

    protected Guid? GetMediaItemId(ITransaction transaction, string systemId, ResourcePath resourcePath)
    {
      string providerAspectTable = _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata);
      string systemIdAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SYSTEM_ID);
      string pathAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);

      using (IDbCommand command = transaction.CreateCommand())
      {
        command.CommandText = "SELECT " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + providerAspectTable +
            " WHERE " + systemIdAttribute + " = @SYSTEM_ID AND " + pathAttribute + " = @PATH";

        DBUtils.AddParameter(command, "SYSTEM_ID", systemId, DBUtils.GetDBType(typeof(string)));
        DBUtils.AddParameter(command, "PATH", resourcePath.Serialize(), DBUtils.GetDBType(typeof(string)));

        return (Guid?) command.ExecuteScalar();
      }
    }

    protected Guid AddMediaItem(ISQLDatabase database, ITransaction transaction)
    {
      Guid mediaItemId;
      using (IDbCommand command = MediaLibrary_SubSchema.InsertMediaItemCommand(transaction, out mediaItemId))
        command.ExecuteNonQuery();

      return mediaItemId;
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

      using (IDbCommand command = transaction.CreateCommand())
      {
        command.CommandText = "UPDATE " + providerAspectTable + " SET " +
            pathAttribute + " = ? || SUBSTRING(" + pathAttribute + " FROM CHAR_LENGTH(?) + 1) " +
            "WHERE " + systemIdAttribute + " = ? AND " +
            "SUBSTRING(" + pathAttribute + " FROM 1 FOR CHAR_LENGTH(?)) = ?";
        command.Parameters.Add(newBasePathStr);
        command.Parameters.Add(originalBasePathStr.Length);
        command.Parameters.Add(systemId);
        command.Parameters.Add(originalBasePathStr.Length);
        command.Parameters.Add(originalBasePathStr);

        return command.ExecuteNonQuery();
      }
    }

    public int DeleteAllMediaItemsUnderPath(ITransaction transaction, string systemId, ResourcePath basePath)
    {
      MediaItemAspectMetadata providerAspectMetadata = ProviderResourceAspect.Metadata;
      string providerAspectTable = _miaManagement.GetMIATableName(providerAspectMetadata);
      string systemIdAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SYSTEM_ID);
      string pathAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      string commandStr = "DELETE FROM " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN (" +
              // TODO: Replace this inner select statement by a select statement generated from an appropriate item query
              "SELECT " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + providerAspectTable +
              " WHERE " + systemIdAttribute + " = @SYSTEM_ID";

      using (IDbCommand command = transaction.CreateCommand())
      {
        DBUtils.AddParameter(command, "SYSTEM_ID", systemId, DBUtils.GetDBType(typeof(string)));

        if (basePath != null)
        {
          commandStr = commandStr + " AND " + pathAttribute + " LIKE @PATH ESCAPE '\\'";
          DBUtils.AddParameter(command, "PATH",
              SqlUtils.LikeEscape(StringUtils.CheckSuffix(basePath.Serialize(), "/"), '\\') + "%", DBUtils.GetDBType(typeof(string)));
        }

        commandStr = commandStr + ")";

        command.CommandText = commandStr;
        return command.ExecuteNonQuery();
      }
    }

    protected ICollection<string> GetShareMediaCategories(ITransaction transaction, Guid shareId)
    {
      int mediaCategoryIndex;
      ICollection<string> result = new List<string>();
      using (IDbCommand command = MediaLibrary_SubSchema.SelectShareCategoriesCommand(transaction, shareId, out mediaCategoryIndex))
      using (IDataReader reader = command.ExecuteReader())
      {
        while (reader.Read())
          result.Add(DBUtils.ReadDBValue<string>(reader, mediaCategoryIndex));
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
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();
      importerWorker.Activate(_mediaBrowsingCallback, _importResultHandler);
      NotifySystemOnline(_localSystemId, SystemName.GetLocalSystemName());
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
              textFilters.Add(new LikeFilter(attrType, "%" + SqlUtils.LikeEscape(searchText, '\\') + "%", '\\', caseSensitive));
        resultFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
            new BooleanCombinationFilter(BooleanOperator.Or, textFilters), filter);
      }
      return new MediaItemQuery(necessaryMIATypes, optionalMIATypes, resultFilter);
    }

    public IList<MediaItem> Search(MediaItemQuery query, bool filterOnlyOnline)
    {
      MediaItemQuery innerQuery = new MediaItemQuery(query);
      innerQuery.NecessaryRequestedMIATypeIDs.Add(ProviderResourceAspect.ASPECT_ID);
      CompiledMediaItemQuery cmiq = CompiledMediaItemQuery.Compile(_miaManagement, innerQuery);
      IList<MediaItem> items = cmiq.Execute();
      IList<MediaItem> result = new List<MediaItem>(items.Count);
      bool removeProviderAspect = !query.NecessaryRequestedMIATypeIDs.Contains(ProviderResourceAspect.ASPECT_ID);
      foreach (MediaItem item in items)
        if (!filterOnlyOnline || (_systemsOnline.ContainsKey((string) item.Aspects[ProviderResourceAspect.ASPECT_ID][ProviderResourceAspect.ATTR_SYSTEM_ID])))
        {
          if (removeProviderAspect)
            item.Aspects.Remove(ProviderResourceAspect.ASPECT_ID);
          result.Add(item);
        }
      return result;
    }

    public IList<MLQueryResultGroup> GroupSearch(MediaItemQuery query, MediaItemAspectMetadata.AttributeSpecification groupingAttributeType,
        bool filterOnlyOnline, GroupingFunction groupingFunction)
    {
      IDictionary<string, MLQueryResultGroup> groups = new Dictionary<string, MLQueryResultGroup>();
      IGroupingFunctionImpl groupingFunctionImpl;
      switch (groupingFunction)
      {
        case GroupingFunction.FirstCharacter:
          groupingFunctionImpl = new FirstCharGroupingFunction(groupingAttributeType);
          break;
        default:
          groupingFunctionImpl = new FirstCharGroupingFunction(groupingAttributeType);
          break;
      }
      Guid groupingAspectId = groupingAttributeType.ParentMIAM.AspectId;
      MediaItemQuery innerQuery = new MediaItemQuery(query);
      innerQuery.NecessaryRequestedMIATypeIDs.Add(MediaAspect.ASPECT_ID);
      foreach (MediaItem item in Search(innerQuery, filterOnlyOnline))
      {
        string groupName;
        IFilter additionalFilter;
        groupingFunctionImpl.GetGroup(string.Format("{0}", item[groupingAspectId][groupingAttributeType]),
            out groupName, out additionalFilter);
        MLQueryResultGroup rg;
        if (groups.TryGetValue(groupName, out rg))
          rg.NumItemsInGroup++;
        else
          groups[groupName] = new MLQueryResultGroup(groupName, 1, additionalFilter);
      }
      List<MLQueryResultGroup> result = new List<MLQueryResultGroup>(groups.Values);
      result.Sort((a, b) => string.Compare(a.GroupName, b.GroupName));
      return result;
    }

    public ICollection<MediaItem> Browse(string systemId, ResourcePath path, IEnumerable<Guid> necessaryRequestedMIATypeIDs,
        IEnumerable<Guid> optionalRequestedMIATypeIDs)
    {
      const char ESCAPE_CHAR = '!';
      string pathStr = SqlUtils.LikeEscape(StringUtils.CheckSuffix(path.Serialize(), "/"), ESCAPE_CHAR);
      BooleanCombinationFilter filter = new BooleanCombinationFilter(BooleanOperator.And, new IFilter[]
          {
            // Compare system
            new RelationalFilter(ProviderResourceAspect.ATTR_SYSTEM_ID, RelationalOperator.EQ, systemId),
            // Compare parent folder
            new LikeFilter(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH,
                pathStr + '%', ESCAPE_CHAR),
            // Exclude sub folders
            new NotFilter(
                new LikeFilter(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH,
                    pathStr + "%/%", ESCAPE_CHAR))
          });
      MediaItemQuery query = new MediaItemQuery(necessaryRequestedMIATypeIDs, optionalRequestedMIATypeIDs, filter);
      return Search(query, false);
    }

    public HomogenousMap GetValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IEnumerable<Guid> necessaryMIATypeIDs, IFilter filter)
    {
      CompiledGroupedAttributeValueQuery cdavq = CompiledGroupedAttributeValueQuery.Compile(
          _miaManagement, necessaryMIATypeIDs, attributeType, filter);
      return cdavq.Execute();
    }

    public IList<MLQueryResultGroup> GroupValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IEnumerable<Guid> necessaryMIATypeIDs, IFilter filter, GroupingFunction groupingFunction)
    {
      IDictionary<string, MLQueryResultGroup> groups = new Dictionary<string, MLQueryResultGroup>();
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
      foreach (KeyValuePair<object, object> resultItem in GetValueGroups(attributeType, necessaryMIATypeIDs, filter))
      {
        string valueGroupItemName = (string) resultItem.Key;
        int resultGroupItemCount = (int) resultItem.Value;
        string groupName;
        IFilter additionalFilter;
        groupingFunctionImpl.GetGroup(valueGroupItemName, out groupName, out additionalFilter);
        MLQueryResultGroup rg;
        if (groups.TryGetValue(groupName, out rg))
          rg.NumItemsInGroup += resultGroupItemCount;
        else
          groups[groupName] = new MLQueryResultGroup(groupName, resultGroupItemCount, additionalFilter);
      }
      List<MLQueryResultGroup> result = new List<MLQueryResultGroup>(groups.Values);
      result.Sort((a, b) => string.Compare(a.GroupName, b.GroupName));
      return result;
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
              Guid playlistId = DBUtils.ReadDBValue<Guid>(reader, playlistIdIndex);
              string name = DBUtils.ReadDBValue<string>(reader, nameIndex);
              string playlistType = DBUtils.ReadDBValue<string>(reader, playlistTypeIndex);
              int playlistItemsCount = DBUtils.ReadDBValue<int>(reader, playlistItemsCountIndex);
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
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaLibrary: Error adding playlist '{0}' (id '{1}')", e, playlistData.Name, playlistId);
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
          using (IDataReader reader = command.ExecuteReader())
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
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaLibrary: Error deleting playlist '{0}'", e, playlistId);
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
          using (IDataReader reader = command.ExecuteReader())
            if (reader.Read())
            {
              name = DBUtils.ReadDBValue<string>(reader, nameIndex);
              playlistType = DBUtils.ReadDBValue<string>(reader, playlistTypeIndex);
            }
            else
              return null;

        IList<Guid> mediaItemIds = new List<Guid>();
        int mediaItemIdIndex;
        using (IDbCommand command = MediaLibrary_SubSchema.SelectPlaylistContentsCommand(transaction, playlistId, out mediaItemIdIndex))
          using (IDataReader reader = command.ExecuteReader())
            while (reader.Read())
              mediaItemIds.Add(DBUtils.ReadDBValue<Guid>(reader, mediaItemIdIndex));
        return new PlaylistRawData(playlistId, name, playlistType, mediaItemIds);
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public PlaylistContents LoadServerPlaylist(Guid playlistId,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int nameIndex;
        int playlistTypeIndex;
        string playlistName;
        string playlistType;
        using (IDbCommand command = MediaLibrary_SubSchema.SelectPlaylistIdentificationDataCommand(transaction, playlistId, out nameIndex, out playlistTypeIndex))
          using (IDataReader reader = command.ExecuteReader())
            if (reader.Read())
            {
              playlistName = DBUtils.ReadDBValue<string>(reader, nameIndex);
              playlistType = DBUtils.ReadDBValue<string>(reader, playlistTypeIndex);
            }
            else
              return null;

        IList<Guid> mediaItemIds = new List<Guid>();
        int mediaItemIdIndex;
        using (IDbCommand command = MediaLibrary_SubSchema.SelectPlaylistContentsCommand(transaction, playlistId, out mediaItemIdIndex))
          using (IDataReader reader = command.ExecuteReader())
            while (reader.Read())
              mediaItemIds.Add(DBUtils.ReadDBValue<Guid>(reader, mediaItemIdIndex));

        IList<MediaItem> mediaItems = LoadCustomPlaylist(mediaItemIds, necessaryMIATypes, optionalMIATypes);
        return new PlaylistContents(playlistId, playlistName, playlistType, mediaItems);
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public IList<MediaItem> LoadCustomPlaylist(IList<Guid> mediaItemIds,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes)
    {
      IFilter filter = new MediaItemIdFilter(mediaItemIds);
      MediaItemQuery query = new MediaItemQuery(necessaryMIATypes, optionalMIATypes, filter);
      return Search(query, false);
    }

    #endregion

    #region Media import

    public void AddOrUpdateMediaItem(string systemId, ResourcePath path, IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      // TODO: Avoid multiple write operations to the same media item
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        Guid? mediaItemId = GetMediaItemId(transaction, systemId, path);
        DateTime now = DateTime.Now;
        MediaItemAspect importerAspect;
        bool wasCreated = !mediaItemId.HasValue;
        if (wasCreated)
        {
          mediaItemId = AddMediaItem(database, transaction);

          MediaItemAspect providerResourceAspect = new MediaItemAspect(ProviderResourceAspect.Metadata);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, systemId);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, path.Serialize());
          _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, providerResourceAspect, true);

          importerAspect = new MediaItemAspect(ImporterAspect.Metadata);
          importerAspect.SetAttribute(ImporterAspect.ATTR_DATEADDED, now);
        }
        else
          importerAspect = _miaManagement.GetMediaItemAspect(transaction, mediaItemId.Value, ImporterAspect.ASPECT_ID);
        importerAspect.SetAttribute(ImporterAspect.ATTR_DIRTY, false);
        importerAspect.SetAttribute(ImporterAspect.ATTR_LAST_IMPORT_DATE, now);
        if (wasCreated)
          _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, importerAspect, true);
        else
          _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, importerAspect, false);

        // Update
        foreach (MediaItemAspect mia in mediaItemAspects)
        {
          if (!_miaManagement.ManagedMediaItemAspectTypes.ContainsKey(mia.Metadata.AspectId))
            // Simply skip unknown MIA types. All types should have been added before import.
            continue;
          if (wasCreated)
            _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, mia, true);
          else
            _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, mia);
        }
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaLibrary: Error adding or updating media item(s) in path '{0}'", e, path.Serialize());
        transaction.Rollback();
        throw;
      }
    }

    public void DeleteMediaItemOrPath(string systemId, ResourcePath path)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        DeleteAllMediaItemsUnderPath(transaction, systemId, path);
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaLibrary: Error deleting media item(s) of system '{0}' in path '{1}'",
            e, systemId, path.Serialize());
        transaction.Rollback();
        throw;
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
      _miaManagement.AddMediaItemAspectStorage(miam);
    }

    public void RemoveMediaItemAspectStorage(Guid aspectId)
    {
      _miaManagement.RemoveMediaItemAspectStorage(aspectId);
    }

    public IDictionary<Guid, MediaItemAspectMetadata> GetManagedMediaItemAspectMetadata()
    {
      return _miaManagement.ManagedMediaItemAspectTypes;
    }

    public MediaItemAspectMetadata GetManagedMediaItemAspectMetadata(Guid aspectId)
    {
      return _miaManagement.GetMediaItemAspectMetadata(aspectId);
    }

    #endregion

    #region Shares management

    public void RegisterShare(Share share)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int shareIdIndex;
        using (IDbCommand command = MediaLibrary_SubSchema.SelectShareIdCommand(
            transaction, share.SystemId, share.BaseResourcePath, out shareIdIndex))
        using (IDataReader reader = command.ExecuteReader())
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

        TryScheduleLocalShareImport(share);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaLibrary: Error registering share '{0}'", e, share.ShareId);
        transaction.Rollback();
        throw;
      }
    }

    public Guid CreateShare(string systemId, ResourcePath baseResourcePath, string shareName,
        IEnumerable<string> mediaCategories)
    {
      Guid shareId = Guid.NewGuid();
      Share share = new Share(shareId, systemId, baseResourcePath, shareName, mediaCategories);
      RegisterShare(share);
      return shareId;
    }

    public void RemoveShare(Guid shareId)
    {
      Share share = GetShare(shareId);
      TryCancelLocalImportJobs(share);

      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = MediaLibrary_SubSchema.DeleteSharesCommand(transaction, new Guid[] {shareId}))
          command.ExecuteNonQuery();

        DeleteAllMediaItemsUnderPath(transaction, share.SystemId, share.BaseResourcePath);
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaLibrary: Error removing share '{0}'", e, shareId);
        transaction.Rollback();
        throw;
      }
    }

    public void RemoveSharesOfSystem(string systemId)
    {
      IDictionary<Guid, Share> shares = GetShares(systemId);
      foreach (Share share in shares.Values)
        TryCancelLocalImportJobs(share);

      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = MediaLibrary_SubSchema.DeleteSharesOfSystemCommand(transaction, systemId))
          command.ExecuteNonQuery();

        DeleteAllMediaItemsUnderPath(transaction, systemId, null);

        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaLibrary: Error removing shares of system '{0}'", e, systemId);
        transaction.Rollback();
        throw;
      }
    }

    public int UpdateShare(Guid shareId, ResourcePath baseResourcePath, string shareName,
        IEnumerable<string> mediaCategories, RelocationMode relocationMode)
    {
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

        // Relocate media items
        int numAffected;
        switch (relocationMode)
        {
          case RelocationMode.Relocate:
            numAffected = RelocateMediaItems(transaction, originalShare.SystemId, originalShare.BaseResourcePath, baseResourcePath);
            break;
          case RelocationMode.Remove:
            numAffected = DeleteAllMediaItemsUnderPath(transaction, originalShare.SystemId, originalShare.BaseResourcePath);
            Share updatedShare = GetShare(transaction, shareId);
            TryScheduleLocalShareImport(updatedShare);
            break;
          default:
            throw new NotImplementedException(string.Format("RelocationMode {0} is not implemented", relocationMode));
        }
        transaction.Commit();
        return numAffected;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaLibrary: Error updating share '{0}'", e, shareId);
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
              Guid shareId = DBUtils.ReadDBValue<Guid>(reader, shareIdIndex);
              ICollection<string> mediaCategories = GetShareMediaCategories(transaction, shareId);
              result.Add(shareId, new Share(shareId, DBUtils.ReadDBValue<string>(reader, systemIdIndex),
                  ResourcePath.Deserialize(DBUtils.ReadDBValue<string>(reader, pathIndex)),
                  DBUtils.ReadDBValue<string>(reader, shareNameIndex),
                  mediaCategories));
            }
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
      lock (_syncObj)
        _systemsOnline[systemId] = currentSystemName;
    }

    public void NotifySystemOffline(string systemId)
    {
      lock (_syncObj)
        _systemsOnline.Remove(systemId);
    }

    #endregion

    #endregion
  }
}
