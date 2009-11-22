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
using MediaPortal.Backend.Services.Database;
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Backend.Services.MediaLibrary
{
  // TODO: Preparation of some SQL statements? We could use a lazy initialized DBCommand cache which prepares DBCommands
  // on the fly and holds up to N prepared commands.
  public class MediaLibrary : IMediaLibrary, IDisposable
  {
    protected MIA_Management _miaManagement = null;
    
    public void Dispose()
    {
    }

    protected Int64? GetMediaItemId(ITransaction transaction, SystemName nativeSystem, ResourcePath resourcePath)
    {
      string providerAspectTable = _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata);
      string sourceComputerAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SOURCE_COMPUTER);
      string pathAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);

      IDbCommand command = transaction.CreateCommand();
      command.CommandText = "SELECT " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + providerAspectTable +
          " WHERE " + sourceComputerAttribute + " = ? AND " + pathAttribute + " = ?";

      IDbDataParameter param = command.CreateParameter();
      param.Value = nativeSystem.HostName;
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

      command = MediaLibrary_SubSchema.GetLastGeneratedMediaItemIdCommand(database, transaction);
      return (Int64) command.ExecuteScalar();
    }

    protected int RelocateMediaItems(ITransaction transaction,
        SystemName originalNativeSystem, ResourcePath originalBasePath,
        SystemName newNativeSystem, ResourcePath newBasePath)
    {
      string providerAspectTable = _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata);
      string sourceComputerAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SOURCE_COMPUTER);
      string pathAttribute = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);

      string originalBasePathStr = StringUtils.CheckSuffix(originalBasePath.Serialize(), "/");
      string newBasePathStr = StringUtils.CheckSuffix(newBasePath.Serialize(), "/");

      IList<string> setTerms = new List<string>();
      IList<object> parameters = new List<object>();
      if (originalNativeSystem != newNativeSystem)
      {
        setTerms.Add(sourceComputerAttribute + " = ?");
        parameters.Add(newNativeSystem);
      }
      if (originalBasePathStr != newBasePathStr)
      {
        setTerms.Add(pathAttribute + " = ? || SUBSTRING(" + pathAttribute + " FROM CHAR_LENGTH(?) + 1)");
        parameters.Add(newBasePathStr);
        parameters.Add(originalBasePathStr);
      }

      if (setTerms.Count == 0)
        return 0;
      IDbCommand command = transaction.CreateCommand();
      command.CommandText = "UPDATE " + providerAspectTable + " SET " + StringUtils.Join(", ", setTerms) +
          " WHERE " + sourceComputerAttribute + " = ? AND " + 
          "SUBSTRING(" + pathAttribute + " FROM 1 FOR CHAR_LENGTH(?)) = ?";
      parameters.Add(originalNativeSystem.HostName);
      parameters.Add(originalBasePathStr.Length);
      parameters.Add(originalBasePathStr);

      foreach (object paramValue in parameters)
      {
        IDbDataParameter param = command.CreateParameter();
        param.Value = paramValue;
        command.Parameters.Add(param);
      }

      return command.ExecuteNonQuery();
    }

    public int DeleteAllMediaItemsUnderPath(ITransaction transaction,
        SystemName nativeSystem, ResourcePath basePath)
    {
      MediaItemAspectMetadata providerAspectMetadata = ProviderResourceAspect.Metadata;
      string providerAspectTableName = _miaManagement.GetMIATableName(providerAspectMetadata);
      string providerAspectSourceComputerColName = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SOURCE_COMPUTER);
      string providerAspectPathColName = _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      IDbCommand command = transaction.CreateCommand();
      command.CommandText = "DELETE FROM " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN (" +
              // TODO: Replace this inner select statement by a select statement generated from an appropriate item query
              "SELECT " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + providerAspectTableName +
              " WHERE " + providerAspectSourceComputerColName + " = ? AND " +
              providerAspectPathColName + " LIKE ? ESCAPE '\\')";

      IDbDataParameter param = command.CreateParameter();
      param.Value = nativeSystem.HostName;
      command.Parameters.Add(param);

      param = command.CreateParameter();
      param.Value = SqlUtils.LikeEscape(StringUtils.CheckSuffix(basePath.Serialize(), "/"), '\\') + "%";
      command.Parameters.Add(param);

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
          result.Add(reader.GetString(mediaCategoryIndex));
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

    public IList<MediaItem> Search(MediaItemQuery query)
    {
      CompiledMediaItemQuery cmiq = CompiledMediaItemQuery.Compile(_miaManagement, query, GetManagedMediaItemAspectMetadata());
      return cmiq.Execute();
    }

    public ICollection<MediaItem> Browse(SystemName system, ResourcePath path, IEnumerable<Guid> necessaryRequestedMIATypeIDs,
        IEnumerable<Guid> optionalRequestedMIATypeIDs)
    {
      const char ESCAPE_CHAR = '!';
      string pathStr = StringUtils.CheckSuffix(path.Serialize(), "/");
      BooleanCombinationFilter filter = new BooleanCombinationFilter(BooleanOperator.And, new IFilter[]
          {
            // Compare system
            new RelationalFilter(ProviderResourceAspect.ATTR_SOURCE_COMPUTER, RelationalOperator.EQ, system.HostName),
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
      return Search(query);
    }

    public void AddOrUpdateMediaItem(SystemName nativeSystem, ResourcePath path, IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      // TODO: Avoid multiple write operations to the same media item
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        Int64? mediaItemId = GetMediaItemId(transaction, nativeSystem, path);
        DateTime now = DateTime.Now;
        MediaItemAspect importerAspect;
        bool wasCreated = !mediaItemId.HasValue;
        if (wasCreated)
        {
          mediaItemId = AddMediaItem(database, transaction);

          MediaItemAspect providerResourceAspect = new MediaItemAspect(ProviderResourceAspect.Metadata);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SOURCE_COMPUTER, nativeSystem.HostName);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, path.Serialize());
          _miaManagement.InsertOrUpdateMIA(transaction, mediaItemId.Value, providerResourceAspect, true);

          importerAspect = new MediaItemAspect(ImporterAspect.Metadata);
          importerAspect.SetAttribute(ImporterAspect.ATTR_DATEADDED, now);
        }
        else
          importerAspect = _miaManagement.GetMediaItemAspect(transaction, mediaItemId.Value, ImporterAspect.ASPECT_ID);
        importerAspect.SetAttribute(ImporterAspect.ATTR_DIRTY, false);
        importerAspect.SetAttribute(ImporterAspect.ATTR_LAST_IMPORT_DATE, now);
        if (wasCreated)
          _miaManagement.InsertOrUpdateMIA(transaction, mediaItemId.Value, importerAspect, true);
        else
          _miaManagement.InsertOrUpdateMIA(transaction, mediaItemId.Value, importerAspect, false);

        // Update
        foreach (MediaItemAspect mia in mediaItemAspects)
        {
          if (!_miaManagement.ManagedMediaItemAspectTypes.ContainsKey(mia.Metadata.AspectId))
            // Simply skip unknown MIA types. All types should have been added before import.
            continue;
          if (wasCreated)
            _miaManagement.InsertOrUpdateMIA(transaction, mediaItemId.Value, mia, true);
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

    public void DeleteMediaItemOrPath(SystemName nativeSystem, ResourcePath path)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        DeleteAllMediaItemsUnderPath(transaction, nativeSystem, path);
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("MediaLibrary: Error deleting media item(s) in path '{0}'", e, path.Serialize());
        transaction.Rollback();
        throw;
      }
    }

    public HomogenousCollection GetDistinctAssociatedValues(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IFilter filter)
    {
      CompiledDistinctAttributeValueQuery cdavq = CompiledDistinctAttributeValueQuery.Compile(
          _miaManagement, attributeType, filter, GetManagedMediaItemAspectMetadata());
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
            transaction, share.NativeSystem, share.BaseResourcePath, out shareIdIndex);
        IDataReader reader = command.ExecuteReader();
        try
        {
          if (reader.Read())
            throw new ShareExistsException("A share with the given system '{0}' and path '{1}' already exists",
                share.NativeSystem.HostName, share.BaseResourcePath);
        }
        finally
        {
          reader.Close();
        }
        command = MediaLibrary_SubSchema.InsertShareCommand(transaction, share.ShareId, share.NativeSystem,
            share.BaseResourcePath, share.Name, true);
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

    public Guid CreateShare(SystemName nativeSystem, ResourcePath baseResourcePath, string shareName,
        IEnumerable<string> mediaCategories)
    {
      Guid shareId = Guid.NewGuid();
      Share share = new Share(shareId, nativeSystem, baseResourcePath, shareName,mediaCategories);
      RegisterShare(share);
      return shareId;
    }

    public void RemoveShare(Guid shareId)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = MediaLibrary_SubSchema.DeleteShareCommand(transaction, shareId);
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

    public int UpdateShare(Guid shareId, SystemName nativeSystem, ResourcePath baseResourcePath, string shareName,
        IEnumerable<string> mediaCategories, RelocationMode relocationMode)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        Share originalShare = relocationMode == RelocationMode.Relocate ? GetShare(shareId) : null;

        IDbCommand command = MediaLibrary_SubSchema.UpdateShareCommand(transaction, shareId, nativeSystem, baseResourcePath, shareName);
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
            numAffected = RelocateMediaItems(transaction,
                originalShare.NativeSystem, originalShare.BaseResourcePath,
                nativeSystem, baseResourcePath);
            break;
          case RelocationMode.Remove:
            numAffected = DeleteAllMediaItemsUnderPath(transaction, originalShare.NativeSystem, originalShare.BaseResourcePath);
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

    public IDictionary<Guid, Share> GetShares(SystemName system, bool onlyConnectedShares)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int shareIdIndex;
        int nativeSystemIndex;
        int pathIndex;
        int shareNameIndex;
        int isOnlineIndex;
        IDbCommand command;
        if (system == null)
          command = MediaLibrary_SubSchema.SelectSharesCommand(transaction, out shareIdIndex,
              out nativeSystemIndex, out pathIndex, out shareNameIndex, out isOnlineIndex);
        else
          command = MediaLibrary_SubSchema.SelectSharesByNativeSystemCommand(transaction, system, out shareIdIndex,
              out nativeSystemIndex, out pathIndex, out shareNameIndex, out isOnlineIndex);
        IDataReader reader = command.ExecuteReader();
        IDictionary<Guid, Share> result = new Dictionary<Guid, Share>();
        try
        {
          while (reader.Read())
          {
            Guid shareId = new Guid(reader.GetString(shareIdIndex));
            ICollection<string> mediaCategories = GetShareMediaCategories(transaction, shareId);
            if (onlyConnectedShares && !reader.GetBoolean(isOnlineIndex))
              continue;
            result.Add(shareId, new Share(shareId, new SystemName(reader.GetString(nativeSystemIndex)),
                ResourcePath.Deserialize(reader.GetString(pathIndex)), reader.GetString(shareNameIndex),
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
        int nativeSystemIndex;
        int pathIndex;
        int shareNameIndex;
        IDbCommand command = MediaLibrary_SubSchema.SelectShareByIdCommand(transaction, shareId, out nativeSystemIndex,
            out pathIndex, out shareNameIndex);
        IDataReader reader = command.ExecuteReader();
        try
        {
          if (!reader.Read())
            return null;
          ICollection<string> mediaCategories = GetShareMediaCategories(transaction, shareId);
          return new Share(shareId, new SystemName(reader.GetString(nativeSystemIndex)),
              ResourcePath.Deserialize(reader.GetString(pathIndex)), reader.GetString(shareNameIndex), mediaCategories);
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

    public void ConnectShares(ICollection<Guid> shareIds)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = MediaLibrary_SubSchema.SetSharesConnectionStateCommand(transaction, shareIds, true);
        command.ExecuteNonQuery();
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("MediaLibrary: Error connecting shares", e);
        transaction.Rollback();
        throw;
      }
    }

    public void DisconnectShares(ICollection<Guid> shareIds)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = MediaLibrary_SubSchema.SetSharesConnectionStateCommand(transaction, shareIds, false);
        command.ExecuteNonQuery();
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("MediaLibrary: Error disconnecting shares", e);
        transaction.Rollback();
        throw;
      }
    }

    #endregion
  }
}
