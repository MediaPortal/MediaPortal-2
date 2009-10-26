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
using System.Data;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Database;
using MediaPortal.Exceptions;
using MediaPortal.MediaLibrary;
using MediaPortal.Services.Database;
using MediaPortal.Services.MediaLibrary.QueryEngine;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Services.MediaLibrary
{
  // TODO: Preparation of some SQL statements? We could use a lazy initialized DBCommand cache which prepares DBCommands
  // on the fly and holds up to N prepared commands.
  public class MediaLibrary : IMediaLibrary, IDisposable
  {
    protected MIAM_Management _miamManagement = null;
    
    public void Dispose()
    {
    }

    protected int RelocateMediaItems(ITransaction transaction,
        SystemName originalNativeSystem, Guid originalProviderId, string originalBasePath,
        SystemName newNativeSystem, Guid newProviderId, string newBasePath)
    {
      string providerAspectTable = _miamManagement.GetMIAMTableName(ProviderResourceAspect.Metadata);
      string sourceComputerAttribute = _miamManagement.GetMIAMAttributeColumnName(ProviderResourceAspect.ATTR_SOURCE_COMPUTER);
      string providerIdAttribute = _miamManagement.GetMIAMAttributeColumnName(ProviderResourceAspect.ATTR_PROVIDER_ID);
      string pathAttribute = _miamManagement.GetMIAMAttributeColumnName(ProviderResourceAspect.ATTR_PATH);

      originalBasePath = FileUtils.RemoveTrailingPathDelimiter(originalBasePath) + "/";
      newBasePath = FileUtils.RemoveTrailingPathDelimiter(newBasePath) + "/";

      IList<string> setTerms = new List<string>();
      IList<object> parameters = new List<object>();
      if (originalNativeSystem != newNativeSystem)
      {
        setTerms.Add(sourceComputerAttribute + " = ?");
        parameters.Add(newNativeSystem);
      }
      if (originalProviderId != newProviderId)
      {
        setTerms.Add(providerIdAttribute + " = ?");
        parameters.Add(newProviderId.ToString());
      }
      if (originalBasePath != newBasePath)
      {
        setTerms.Add(pathAttribute + " = ? || SUBSTRING(" + pathAttribute + " FROM CHAR_LENGTH(?) + 1)");
        parameters.Add(newBasePath);
        parameters.Add(originalBasePath);
      }

      IDbCommand command = transaction.CreateCommand();
      command.CommandText = "UPDATE " + providerAspectTable + " SET " + StringUtils.Join(", ", setTerms) +
          " WHERE " + sourceComputerAttribute + " = ? AND " + providerIdAttribute + " = ? AND " + 
          "SUBSTRING(" + pathAttribute + " FROM 1 FOR CHAR_LENGTH(?)) = ?";
      parameters.Add(originalNativeSystem.HostName);
      parameters.Add(originalProviderId.ToString());
      parameters.Add(originalBasePath);
      parameters.Add(originalBasePath);

      foreach (object paramValue in parameters)
      {
        IDbDataParameter param = command.CreateParameter();
        param.Value = paramValue;
        command.Parameters.Add(param);
      }

      return command.ExecuteNonQuery();
    }

    public int DeleteAllMediaItemsUnderPath(ITransaction transaction,
        SystemName nativeSystem, Guid mediaProviderId, string path)
    {
      MediaItemAspectMetadata providerAspectMetadata = ProviderResourceAspect.Metadata;
      string providerAspectTableName = _miamManagement.GetMIAMTableName(providerAspectMetadata);
      string providerAspectSourceComputerColName = _miamManagement.GetMIAMAttributeColumnName(ProviderResourceAspect.ATTR_SOURCE_COMPUTER);
      string providerAspectProviderIdColName = _miamManagement.GetMIAMAttributeColumnName(ProviderResourceAspect.ATTR_PROVIDER_ID);
      string providerAspectPathColName = _miamManagement.GetMIAMAttributeColumnName(ProviderResourceAspect.ATTR_PATH);
      IDbCommand command = transaction.CreateCommand();
      command.CommandText = "DELETE FROM " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN (" +
              // TODO: Replace this inner select statement by a select statement generated from an appropriate item query
              "SELECT " + MIAM_Management.MIAM_MEDIA_ITEM_ID_COL_NAME + " FROM " + providerAspectTableName +
              " WHERE " + providerAspectSourceComputerColName + " = ? AND " +
              providerAspectProviderIdColName + " = ? AND " +
              providerAspectPathColName + " LIKE ? ESCAPE '\\')";

      IDbDataParameter param = command.CreateParameter();
      param.Value = nativeSystem.HostName;
      command.Parameters.Add(param);

      param = command.CreateParameter();
      param.Value = mediaProviderId.ToString();
      command.Parameters.Add(param);

      param = command.CreateParameter();
      param.Value = SqlUtils.LikeEscape(StringUtils.CheckSuffix(path, "/"), '\\') + "%";
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

    protected ICollection<Guid> GetShareMetadataExtractors(ITransaction transaction, Guid shareId)
    {
      int metadataExtractorIndex;
      IDbCommand command = MediaLibrary_SubSchema.SelectShareMetadataExtractorsCommand(transaction, shareId, out metadataExtractorIndex);

      ICollection<Guid> result = new List<Guid>();
      IDataReader reader = command.ExecuteReader();
      try
      {
        while (reader.Read())
          result.Add(new Guid(reader.GetString(metadataExtractorIndex)));
      }
      finally
      {
        reader.Close();
      }
      return result;
    }

    protected void AddMetadataExtractorToShare(ITransaction transaction, Guid shareId, Guid metadataExtractorId)
    {
      IDbCommand command = MediaLibrary_SubSchema.InsertShareMetadataExtractorCommand(transaction, shareId, metadataExtractorId);
      command.ExecuteNonQuery();
    }

    protected void RemoveMetadataExtractorFromShare(ITransaction transaction, Guid shareId, Guid metadataExtractorId)
    {
      IDbCommand command = MediaLibrary_SubSchema.DeleteShareMetadataExtractorCommand(transaction, shareId, metadataExtractorId);
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
      _miamManagement = new MIAM_Management();
    }

    public void Shutdown()
    {
    }

    public IList<MediaItem> Search(MediaItemQuery query)
    {
      CompiledMediaItemQuery cmiq = CompiledMediaItemQuery.Compile(_miamManagement, query, GetManagedMediaItemAspectMetadata());
      return cmiq.Execute();
    }

    public ICollection<object> GetDistinctAssociatedValues(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IFilter filter)
    {
      CompiledDistinctAttributeValueQuery cdavq = CompiledDistinctAttributeValueQuery.Compile(
          _miamManagement, attributeType, filter, GetManagedMediaItemAspectMetadata());
      return cdavq.Execute();
    }

    public bool MediaItemAspectStorageExists(Guid aspectId)
    {
      return _miamManagement.MediaItemAspectStorageExists(aspectId);
    }

    public MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid aspectId)
    {
      return _miamManagement.GetMediaItemAspectMetadata(aspectId);
    }

    public void AddMediaItemAspectStorage(MediaItemAspectMetadata miam)
    {
      _miamManagement.AddMediaItemAspectStorage(miam);
    }

    public void RemoveMediaItemAspectStorage(Guid aspectId)
    {
      _miamManagement.RemoveMediaItemAspectStorage(aspectId);
    }

    public IDictionary<Guid, MediaItemAspectMetadata> GetManagedMediaItemAspectMetadata()
    {
      return _miamManagement.ManagedMediaItemAspectTypes;
    }

    public MediaItemAspectMetadata GetManagedMediaItemAspectMetadata(Guid aspectId)
    {
      return _miamManagement.GetMediaItemAspectMetadata(aspectId);
    }

    public void RegisterShare(Share share)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int shareIdIndex;
        IDbCommand command = MediaLibrary_SubSchema.SelectShareIdCommand(
            transaction, share.NativeSystem, share.MediaProviderId, share.Path, out shareIdIndex);
        IDataReader reader = command.ExecuteReader();
        try
        {
          if (reader.Read())
            throw new ShareExistsException("A share with the given system '{0}', provider '{1}' and path '{2}' already exists",
                share.NativeSystem.HostName, share.MediaProviderId.ToString(), share.Path);
        }
        finally
        {
          reader.Close();
        }
        command = MediaLibrary_SubSchema.InsertShareCommand(transaction, share.ShareId, share.NativeSystem, share.MediaProviderId,
            share.Path, share.Name, true);
        command.ExecuteNonQuery();

        foreach (string mediaCategory in share.MediaCategories)
          AddMediaCategoryToShare(transaction, share.ShareId, mediaCategory);

        foreach (Guid metadataExtractorId in share.MetadataExtractorIds)
          AddMetadataExtractorToShare(transaction, share.ShareId, metadataExtractorId);

        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("MediaLibrary: Error registering share '{0}'", e, share.ShareId);
        transaction.Rollback();
        throw;
      }
    }

    public Guid CreateShare(SystemName nativeSystem, Guid providerId, string path, string shareName,
        IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds)
    {
      Guid shareId = Guid.NewGuid();
      Share share = new Share(shareId, nativeSystem, providerId, path, shareName,mediaCategories, metadataExtractorIds);
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

    public int UpdateShare(Guid shareId, SystemName nativeSystem, Guid providerId, string path, string shareName,
        IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds, RelocationMode relocationMode)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        Share originalShare = relocationMode == RelocationMode.Relocate ? GetShare(shareId) : null;

        IDbCommand command = MediaLibrary_SubSchema.UpdateShareCommand(transaction, shareId, nativeSystem, providerId, path, shareName);
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

        // Update metadata extractors
        ICollection<Guid> formerMetadataExtractors = GetShareMetadataExtractors(transaction, shareId);

        ICollection<Guid> newMetadataExtractorIds = new List<Guid>();
        foreach (Guid metadataExtractorId in metadataExtractorIds)
        {
          newMetadataExtractorIds.Add(metadataExtractorId);
          if (!formerMetadataExtractors.Contains(metadataExtractorId))
            AddMetadataExtractorToShare(transaction, shareId, metadataExtractorId);
        }

        foreach (Guid metadataExtractorId in formerMetadataExtractors)
          if (!newMetadataExtractorIds.Contains(metadataExtractorId))
            RemoveMetadataExtractorFromShare(transaction, shareId, metadataExtractorId);

        // Relocate media items
        int numAffected;
        switch (relocationMode)
        {
          case RelocationMode.Relocate:
            numAffected = RelocateMediaItems(transaction,
                originalShare.NativeSystem, originalShare.MediaProviderId, originalShare.Path,
                nativeSystem, providerId, path);
            break;
          case RelocationMode.Remove:
            numAffected = DeleteAllMediaItemsUnderPath(transaction, originalShare.NativeSystem, originalShare.MediaProviderId, originalShare.Path);
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
        int providerIdIndex;
        int pathIndex;
        int shareNameIndex;
        int isOnlineIndex;
        IDbCommand command;
        if (system == null)
          command = MediaLibrary_SubSchema.SelectSharesCommand(transaction, out shareIdIndex,
              out nativeSystemIndex, out providerIdIndex, out pathIndex, out shareNameIndex, out isOnlineIndex);
        else
          command = MediaLibrary_SubSchema.SelectSharesByNativeSystemCommand(transaction, system, out shareIdIndex,
              out nativeSystemIndex, out providerIdIndex, out pathIndex, out shareNameIndex, out isOnlineIndex);
        IDataReader reader = command.ExecuteReader();
        IDictionary<Guid, Share> result = new Dictionary<Guid, Share>();
        try
        {
          while (reader.Read())
          {
            Guid shareId = new Guid(reader.GetString(shareIdIndex));
            ICollection<string> mediaCategories = GetShareMediaCategories(transaction, shareId);
            ICollection<Guid> metadataExtractors = GetShareMetadataExtractors(transaction, shareId);
            if (onlyConnectedShares && !reader.GetBoolean(isOnlineIndex))
              continue;
            result.Add(shareId, new Share(shareId, new SystemName(reader.GetString(nativeSystemIndex)),
                new Guid(reader.GetString(providerIdIndex)), reader.GetString(pathIndex), reader.GetString(shareNameIndex),
                mediaCategories, metadataExtractors));
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
        int providerIdIndex;
        int pathIndex;
        int shareNameIndex;
        IDbCommand command = MediaLibrary_SubSchema.SelectShareByIdCommand(transaction, shareId, out nativeSystemIndex,
            out providerIdIndex, out pathIndex, out shareNameIndex);
        IDataReader reader = command.ExecuteReader();
        try
        {
          if (!reader.Read())
            return null;
          ICollection<string> mediaCategories = GetShareMediaCategories(transaction, shareId);
          ICollection<Guid> metadataExtractors = GetShareMetadataExtractors(transaction, shareId);
          return new Share(shareId, new SystemName(reader.GetString(nativeSystemIndex)),
              new Guid(reader.GetString(providerIdIndex)), reader.GetString(pathIndex), reader.GetString(shareNameIndex),
              mediaCategories, metadataExtractors);
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
