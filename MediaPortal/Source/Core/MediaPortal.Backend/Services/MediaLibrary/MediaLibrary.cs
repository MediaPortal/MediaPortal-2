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
using System.Data;
using System.Linq;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.Exceptions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemResolver;
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
    #region Inner classes

    protected class MediaBrowsingCallback : IMediaBrowsing
    {
      protected MediaLibrary _parent;

      public MediaBrowsingCallback(MediaLibrary parent)
      {
        _parent = parent;
      }

      public MediaItem LoadItem(ResourcePath path,
          IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs)
      {
        return _parent.LoadItem(_parent.LocalSystemId, path, necessaryRequestedMIATypeIDs, optionalRequestedMIATypeIDs);
      }

      public ICollection<MediaItem> Browse(Guid parentDirectoryId,
          IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs)
      {
        return _parent.Browse(parentDirectoryId, necessaryRequestedMIATypeIDs, optionalRequestedMIATypeIDs);
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
        return _parent.AddOrUpdateMediaItem(parentDirectoryId, _parent.LocalSystemId, path, updatedAspects);
      }

      public void DeleteMediaItem(ResourcePath path)
      {
        _parent.DeleteMediaItemOrPath(_parent.LocalSystemId, path, true);
      }

      public void DeleteUnderPath(ResourcePath path)
      {
        _parent.DeleteMediaItemOrPath(_parent.LocalSystemId, path, false);
      }
    }

    #endregion

    #region Protected fields

    protected MIA_Management _miaManagement = null;
    protected IDictionary<string, SystemName> _systemsOnline = new Dictionary<string, SystemName>(); // System ids mapped to system names
    protected object _syncObj = new object();
    protected string _localSystemId;
    protected IMediaBrowsing _mediaBrowsingCallback;
    protected IImportResultHandler _importResultHandler;

    #endregion

    #region Ctor & dtor

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
      using (IDbCommand command = MediaLibrary_SubSchema.SelectShareByIdCommand(transaction, shareId, out systemIdIndex,
          out pathIndex, out shareNameIndex))
      using (IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
      {
        if (!reader.Read())
          return null;
        ICollection<string> mediaCategories = GetShareMediaCategories(transaction, shareId);
        return new Share(shareId, database.ReadDBValue<string>(reader, systemIdIndex), ResourcePath.Deserialize(
            database.ReadDBValue<string>(reader, pathIndex)),
            database.ReadDBValue<string>(reader, shareNameIndex), mediaCategories);
      }
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

    protected int DeleteAllMediaItemsUnderPath(ITransaction transaction, string systemId, ResourcePath basePath, bool inclusive)
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
              pathAttribute + " LIKE @LIKE_PATH1 ESCAPE '\\' OR " +
              pathAttribute + " LIKE @LIKE_PATH2 ESCAPE '\\'" +
              ")";
          string path = StringUtils.RemoveSuffixIfPresent(basePath.Serialize(), "/");
          string escapedPath = SqlUtils.LikeEscape(path, '\\');
          if (inclusive)
          {
            // The path itself
            database.AddParameter(command, "EXACT_PATH", path, typeof(string));
            // Normal children and, if escapedPath ends with "/", the directory itself
            database.AddParameter(command, "LIKE_PATH1", escapedPath + "/%", typeof(string));
          }
          else
          {
            // Normal children, in any case excluding the escaped path, even if it is a directory which ends with "/"
            database.AddParameter(command, "LIKE_PATH1", escapedPath + "/_%", typeof(string));
          }
          // Chained children
          database.AddParameter(command, "LIKE_PATH2", escapedPath + ">_%", typeof(string));
        }

        commandStr = commandStr + ")";

        command.CommandText = commandStr;
        return command.ExecuteNonQuery();
      }
    }

    protected IFilter AddOnlyOnlineFilter(IFilter innerFilter)
    {
      IFilter onlineFilter = new BooleanCombinationFilter(BooleanOperator.Or, _systemsOnline.Select(
              systemEntry => new RelationalFilter(ProviderResourceAspect.ATTR_SYSTEM_ID, RelationalOperator.EQ, systemEntry.Key)).Cast<IFilter>());
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
              textFilters.Add(new LikeFilter(attrType, "%" + SqlUtils.LikeEscape(searchText, '\\') + "%", '\\', caseSensitive));
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
        MediaItemQuery loadItemQuery = BuildLoadItemQuery(systemId, path);
        loadItemQuery.SetNecessaryRequestedMIATypeIDs(necessaryRequestedMIATypeIDs);
        loadItemQuery.SetOptionalRequestedMIATypeIDs(optionalRequestedMIATypeIDs);
        return Search(loadItemQuery, false).FirstOrDefault();
      }
    }

    public ICollection<MediaItem> Browse(Guid parentDirectoryId,
        IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs)
    {
      lock (_syncObj)
      {
        MediaItemQuery browseQuery = BuildBrowseQuery(parentDirectoryId);
        browseQuery.SetNecessaryRequestedMIATypeIDs(necessaryRequestedMIATypeIDs);
        browseQuery.SetOptionalRequestedMIATypeIDs(optionalRequestedMIATypeIDs);
        return Search(browseQuery, false);
      }
    }

    public IList<MediaItem> Search(MediaItemQuery query, bool filterOnlyOnline)
    {
      // We add the provider resource aspect to the necessary aspect types be able to filter online systems
      MediaItemQuery executeQuery = filterOnlyOnline ? new MediaItemQuery(
              query.NecessaryRequestedMIATypeIDs.Union(new Guid[] {ProviderResourceAspect.ASPECT_ID}),
              query.OptionalRequestedMIATypeIDs, AddOnlyOnlineFilter(query.Filter)) : query;
      CompiledMediaItemQuery cmiq = CompiledMediaItemQuery.Compile(_miaManagement, executeQuery);
      IList<MediaItem> items = cmiq.Execute();
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

    public HomogenousMap GetValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType,
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
      CompiledGroupedAttributeValueQuery cdavq = CompiledGroupedAttributeValueQuery.Compile(_miaManagement,
          filterOnlyOnline ? necessaryMIATypeIDs.Union(new Guid[] {ProviderResourceAspect.ASPECT_ID}) :
          necessaryMIATypeIDs, attributeType, selectProjectionFunctionImpl, projectionValueType, filterOnlyOnline ? AddOnlyOnlineFilter(filter) : filter);
      return cdavq.Execute();
    }

    public IList<MLQueryResultGroup> GroupValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType,
        ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypeIDs, IFilter filter, bool filterOnlyOnline,
        GroupingFunction groupingFunction)
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
      foreach (KeyValuePair<object, object> resultItem in GetValueGroups(attributeType, projectionFunction, necessaryMIATypeIDs, filter, filterOnlyOnline))
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
          using (IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
            if (reader.Read())
            {
              playlistName = database.ReadDBValue<string>(reader, nameIndex);
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

          MediaItemAspect pra = new MediaItemAspect(ProviderResourceAspect.Metadata);
          pra.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, systemId);
          pra.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, path.Serialize());
          pra.SetAttribute(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID, parentDirectoryId);
          _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, pra, true);

          importerAspect = new MediaItemAspect(ImporterAspect.Metadata);
          importerAspect.SetAttribute(ImporterAspect.ATTR_DATEADDED, now);
        }
        else
          importerAspect = _miaManagement.GetMediaItemAspect(transaction, mediaItemId.Value, ImporterAspect.ASPECT_ID);
        importerAspect.SetAttribute(ImporterAspect.ATTR_DIRTY, false);
        importerAspect.SetAttribute(ImporterAspect.ATTR_LAST_IMPORT_DATE, now);

        _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, importerAspect, wasCreated);

        // Update
        foreach (MediaItemAspect mia in mediaItemAspects)
        {
          if (!_miaManagement.ManagedMediaItemAspectTypes.ContainsKey(mia.Metadata.AspectId))
            // Simply skip unknown MIA types. All types should have been added before import.
            continue;
          if (mia.Metadata.AspectId == ImporterAspect.ASPECT_ID ||
              mia.Metadata.AspectId == ProviderResourceAspect.ASPECT_ID)
          { // Those aspects are managed by the MediaLibrary
            ServiceRegistration.Get<ILogger>().Warn("MediaLibrary.AddOrUpdateMediaItem: Client tried to update either ImporterAspect or ProviderResourceAspect");
            continue;
          }
          if (wasCreated)
            _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, mia, true);
          else
            _miaManagement.AddOrUpdateMIA(transaction, mediaItemId.Value, mia);
        }
        transaction.Commit();
        return mediaItemId.Value;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaLibrary: Error adding or updating media item(s) in path '{0}'", e, path.Serialize());
        transaction.Rollback();
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
      if (_miaManagement.AddMediaItemAspectStorage(miam))
        ServiceRegistration.Get<ILogger>().Info("MediaLibrary: Media item aspect storage for MIA type '{0}' (name '{1}') was added", miam.AspectId, miam.Name);
    }

    public void RemoveMediaItemAspectStorage(Guid aspectId)
    {
      if (_miaManagement.RemoveMediaItemAspectStorage(aspectId))
        ServiceRegistration.Get<ILogger>().Info("MediaLibrary: Media item aspect storage for MIA type '{0}' was removed", aspectId);
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
      ServiceRegistration.Get<ILogger>().Info("MediaLibrary: Registering share '{0}' at system {1}: Setting name '{2}', base resource path '{3}' and media categories '{4}'",
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
      ServiceRegistration.Get<ILogger>().Info("MediaLibrary: Creating new share '{0}'", shareId);
      Share share = new Share(shareId, systemId, baseResourcePath, shareName, mediaCategories);
      RegisterShare(share);
      return shareId;
    }

    public void RemoveShare(Guid shareId)
    {
      ServiceRegistration.Get<ILogger>().Info("MediaLibrary: Removing share '{0}'", shareId);
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
      ServiceRegistration.Get<ILogger>().Info("MediaLibrary: Removing all shares of system '{0}'", systemId);
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
      ServiceRegistration.Get<ILogger>().Info("MediaLibrary: Updating share '{0}': Setting name '{1}', base resource path '{2}' and media categories '{3}'",
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
            ServiceRegistration.Get<ILogger>().Info("MediaLibrary: Relocated {0} media items during share update", numAffected);
            break;
          case RelocationMode.Remove:
            numAffected = DeleteAllMediaItemsUnderPath(transaction, originalShare.SystemId, originalShare.BaseResourcePath, true);
            ServiceRegistration.Get<ILogger>().Info("MediaLibrary: Deleted {0} media items during share update (will be re-imported)", numAffected);
            Share updatedShare = GetShare(transaction, shareId);
            TryScheduleLocalShareImport(updatedShare);
            break;
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
              Guid shareId = database.ReadDBValue<Guid>(reader, shareIdIndex);
              ICollection<string> mediaCategories = GetShareMediaCategories(transaction, shareId);
              result.Add(shareId, new Share(shareId, database.ReadDBValue<string>(reader, systemIdIndex),
                  ResourcePath.Deserialize(database.ReadDBValue<string>(reader, pathIndex)),
                  database.ReadDBValue<string>(reader, shareNameIndex),
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
      ServiceRegistration.Get<ILogger>().Info("MediaLibrary: Client '{0}' is online at system '{1}'", systemId, currentSystemName);
      lock (_syncObj)
        _systemsOnline[systemId] = currentSystemName;
    }

    public void NotifySystemOffline(string systemId)
    {
      ServiceRegistration.Get<ILogger>().Info("MediaLibrary: Client '{0}' is offline", systemId);
      lock (_syncObj)
        _systemsOnline.Remove(systemId);
    }

    #endregion

    #endregion
  }
}
