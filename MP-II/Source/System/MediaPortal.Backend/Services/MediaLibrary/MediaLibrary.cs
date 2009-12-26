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
using MediaPortal.Utilities;
using MediaPortal.Utilities.DB;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Backend.Services.MediaLibrary
{
  // TODO: Preparation of some SQL statements? We could use a lazy initialized DBCommand cache which prepares DBCommands
  // on the fly and holds up to N prepared commands.
  public class MediaLibrary : IMediaLibrary, IDisposable
  {
    protected MIA_Management _miaManagement = null;
    protected IDictionary<string, SystemName> _systemsOnline = new Dictionary<string, SystemName>();
    protected object _syncObj = new object();
    
    public void Dispose()
    {
    }

    protected Int64? GetMediaItemId(ITransaction transaction, string systemId, ResourcePath resourcePath)
    {
      string providerAspectTable = _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata);
      string systemIdAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SYSTEM_ID);
      string pathAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);

      IDbCommand command = transaction.CreateCommand();
      command.CommandText = "SELECT " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + providerAspectTable +
          " WHERE " + systemIdAttribute + " = ? AND " + pathAttribute + " = ?";

      IDbDataParameter param = command.CreateParameter();
      param.Value = systemId;
      command.Parameters.Add(param);

      param = command.CreateParameter();
      param.Value = resourcePath.Serialize();
      command.Parameters.Add(param);

      object result = command.ExecuteScalar();
      return result == null ? new Int64?() : (Int64) result;
    }

    protected Int64 AddMediaItem(ISQLDatabase database, ITransaction transaction)
    {
      IDbCommand command = MediaLibrary_SubSchema.InsertMediaItemCommand(database, transaction);
      command.ExecuteNonQuery();

      command = MediaLibrary_SubSchema.GetLastGeneratedMediaLibraryIdCommand(database, transaction);
      return (Int64) command.ExecuteScalar();
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

      IDbCommand command = transaction.CreateCommand();
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
              " WHERE " + systemIdAttribute + " = ?";

      IDbCommand command = transaction.CreateCommand();

      IDbDataParameter param = command.CreateParameter();
      param.Value = systemId;
      command.Parameters.Add(param);

      if (basePath != null)
      {
        commandStr = commandStr + " AND " + pathAttribute + " LIKE ? ESCAPE '\\'";

        param = command.CreateParameter();
        param.Value = SqlUtils.LikeEscape(StringUtils.CheckSuffix(basePath.Serialize(), "/"), '\\') + "%";
        command.Parameters.Add(param);
      }

      commandStr = commandStr + ")";

      command.CommandText = commandStr;
      return command.ExecuteNonQuery();
    }

    protected ICollection<string> GetShareMediaCategories(ITransaction transaction, Guid shareId)
    {
      int mediaCategoryIndex;
      IDbCommand command = MediaLibrary_SubSchema.SelectShareCategoriesCommand(transaction, shareId, out mediaCategoryIndex);

      ICollection<string> result = new List<string>();
      IDataReader reader = command.ExecuteReader();
      try
      {
        while (reader.Read())
          result.Add(DBUtils.ReadDBValue<string>(reader, mediaCategoryIndex));
      }
      finally
      {
        reader.Close();
      }
      return result;
    }

    protected void AddMediaCategoryToShare(ITransaction transaction, Guid shareId, string mediaCategory)
    {
      IDbCommand command = MediaLibrary_SubSchema.InsertShareCategoryCommand(transaction, shareId, mediaCategory);
      command.ExecuteNonQuery();
    }

    protected void RemoveMediaCategoryFromShare(ITransaction transaction, Guid shareId, string mediaCategory)
    {
      IDbCommand command = MediaLibrary_SubSchema.DeleteShareCategoryCommand(transaction, shareId, mediaCategory);
      command.ExecuteNonQuery();
    }

    #region IMediaLibrary implementation

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
    }

    public void Shutdown()
    {
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

    public ICollection<MediaItem> Browse(string systemId, ResourcePath path, IEnumerable<Guid> necessaryRequestedMIATypeIDs,
        IEnumerable<Guid> optionalRequestedMIATypeIDs, bool filterOnlyOnline)
    {
      const char ESCAPE_CHAR = '!';
      string pathStr = StringUtils.CheckSuffix(path.Serialize(), "/");
      BooleanCombinationFilter filter = new BooleanCombinationFilter(BooleanOperator.And, new IFilter[]
          {
            // Compare system
            new RelationalFilter(ProviderResourceAspect.ATTR_SYSTEM_ID, RelationalOperator.EQ, systemId),
            // Compare parent folder
            new LikeFilter(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH,
                SqlUtils.LikeEscape(pathStr, ESCAPE_CHAR),
                ESCAPE_CHAR),
            // Exclude sub folders
            new NotFilter(
                new LikeFilter(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH,
                    StringUtils.Repeat("_", pathStr.LastIndexOf('/')) + "/%/%", ESCAPE_CHAR))
          });
      MediaItemQuery query = new MediaItemQuery(necessaryRequestedMIATypeIDs, optionalRequestedMIATypeIDs, filter);
      return Search(query, filterOnlyOnline);
    }

    public void AddOrUpdateMediaItem(string systemId, ResourcePath path, IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      // TODO: Avoid multiple write operations to the same media item
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        Int64? mediaItemId = GetMediaItemId(transaction, systemId, path);
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
        ServiceScope.Get<ILogger>().Error("MediaLibrary: Error deleting media item(s) in path '{0}'", e, path.Serialize());
        transaction.Rollback();
        throw;
      }
    }

    public void DeleteMediaItemOrPath(string systemId, ResourcePath path)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        DeleteAllMediaItemsUnderPath(transaction, systemId, path);
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("MediaLibrary: Error deleting media item(s) of system '{0}' in path '{1}'",
            e, systemId, path.Serialize());
        transaction.Rollback();
        throw;
      }
    }

    public HomogenousCollection GetDistinctAssociatedValues(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IEnumerable<Guid> necessaryMIATypeIDs, IFilter filter)
    {
      CompiledDistinctAttributeValueQuery cdavq = CompiledDistinctAttributeValueQuery.Compile(
          _miaManagement, necessaryMIATypeIDs, attributeType, filter);
      return cdavq.Execute();
    }

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

    public void RegisterShare(Share share)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int shareIdIndex;
        IDbCommand command = MediaLibrary_SubSchema.SelectShareIdCommand(
            transaction, share.SystemId, share.BaseResourcePath, out shareIdIndex);
        IDataReader reader = command.ExecuteReader();
        try
        {
          if (reader.Read())
            throw new ShareExistsException("A share with the given system '{0}' and path '{1}' already exists",
                share.SystemId, share.BaseResourcePath);
        }
        finally
        {
          reader.Close();
        }
        command = MediaLibrary_SubSchema.InsertShareCommand(transaction, share.ShareId, share.SystemId,
            share.BaseResourcePath, share.Name);
        command.ExecuteNonQuery();

        foreach (string mediaCategory in share.MediaCategories)
          AddMediaCategoryToShare(transaction, share.ShareId, mediaCategory);

        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("MediaLibrary: Error registering share '{0}'", e, share.ShareId);
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
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = MediaLibrary_SubSchema.DeleteSharesCommand(transaction, new Guid[] {shareId});
        command.ExecuteNonQuery();

        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("MediaLibrary: Error removing share '{0}'", e, shareId);
        transaction.Rollback();
        throw;
      }
    }

    public void RemoveSharesOfSystem(string systemId)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = MediaLibrary_SubSchema.DeleteSharesOfSystemCommand(transaction, systemId);
        command.ExecuteNonQuery();

        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("MediaLibrary: Error removing shares of system '{0}'", e, systemId);
        transaction.Rollback();
        throw;
      }
    }

    public int UpdateShare(Guid shareId, ResourcePath baseResourcePath, string shareName,
        IEnumerable<string> mediaCategories, RelocationMode relocationMode)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        Share originalShare = GetShare(shareId);

        IDbCommand command = MediaLibrary_SubSchema.UpdateShareCommand(transaction, shareId, baseResourcePath, shareName);
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
            break;
          default:
            throw new NotImplementedException(string.Format("RelocationMode {0} is not implemented", relocationMode));
        }
        transaction.Commit();
        return numAffected;
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("MediaLibrary: Error updating share '{0}'", e, shareId);
        transaction.Rollback();
        throw;
      }
    }

    public IDictionary<Guid, Share> GetShares(string systemId)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
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
        IDataReader reader = command.ExecuteReader();
        IDictionary<Guid, Share> result = new Dictionary<Guid, Share>();
        try
        {
          while (reader.Read())
          {
            Guid shareId = new Guid(DBUtils.ReadDBValue<string>(reader, shareIdIndex));
            ICollection<string> mediaCategories = GetShareMediaCategories(transaction, shareId);
            result.Add(shareId, new Share(shareId, DBUtils.ReadDBValue<string>(reader, systemIdIndex),
                ResourcePath.Deserialize(DBUtils.ReadDBValue<string>(reader, pathIndex)),
                DBUtils.ReadDBValue<string>(reader, shareNameIndex),
                mediaCategories));
          }
        }
        finally
        {
          reader.Close();
        }
        return result;
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public Share GetShare(Guid shareId)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int systemIdIndex;
        int pathIndex;
        int shareNameIndex;
        IDbCommand command = MediaLibrary_SubSchema.SelectShareByIdCommand(transaction, shareId, out systemIdIndex,
            out pathIndex, out shareNameIndex);
        IDataReader reader = command.ExecuteReader();
        try
        {
          if (!reader.Read())
            return null;
          ICollection<string> mediaCategories = GetShareMediaCategories(transaction, shareId);
          return new Share(shareId, DBUtils.ReadDBValue<string>(reader, systemIdIndex), ResourcePath.Deserialize(
              DBUtils.ReadDBValue<string>(reader, pathIndex)),
              DBUtils.ReadDBValue<string>(reader, shareNameIndex), mediaCategories);
        }
        finally
        {
          reader.Close();
        }
      }
      finally
      {
        transaction.Dispose();
      }
    }

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
  }
}
