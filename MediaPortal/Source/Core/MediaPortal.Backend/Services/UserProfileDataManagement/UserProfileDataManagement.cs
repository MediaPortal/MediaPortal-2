#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Threading.Tasks;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Backend.Database;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;
using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Services.ServerCommunication;

namespace MediaPortal.Backend.Services.UserProfileDataManagement
{
  public class UserProfileDataManagement : IUserProfileDataManagement
  {
    #region Public methods

    public void Startup()
    {
      DatabaseSubSchemaManager updater = new DatabaseSubSchemaManager(UserProfileDataManagement_SubSchema.SUBSCHEMA_NAME);
      updater.AddDirectory(UserProfileDataManagement_SubSchema.SubSchemaScriptDirectory);
      int curVersionMajor;
      int curVersionMinor;
      if (!updater.UpdateSubSchema(out curVersionMajor, out curVersionMinor) ||
          curVersionMajor != UserProfileDataManagement_SubSchema.EXPECTED_SCHEMA_VERSION_MAJOR ||
          curVersionMinor != UserProfileDataManagement_SubSchema.EXPECTED_SCHEMA_VERSION_MINOR)
        throw new IllegalCallException(string.Format(
            "Unable to update the UserProfileDataManagement's subschema version to expected version {0}.{1}",
            UserProfileDataManagement_SubSchema.EXPECTED_SCHEMA_VERSION_MAJOR, UserProfileDataManagement_SubSchema.EXPECTED_SCHEMA_VERSION_MINOR));
    }

    public void Shutdown()
    {
      // Nothing to do, yet
    }

    #endregion

    #region Protected methods

    //TODO: DbCommand Async call?
    protected Task<ICollection<UserProfile>> GetProfiles(Guid? profileId, string name, bool loadData = true)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int profileIdIndex;
        int nameIndex;
        int idIndex;
        int dataIndex;
        int lastLoginIndex;
        int imageIndex;
        ICollection<UserProfile> result = new List<UserProfile>();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserProfilesCommand(transaction, profileId, name,
            out profileIdIndex, out nameIndex, out idIndex, out dataIndex, out lastLoginIndex, out imageIndex))
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              result.Add(new UserProfile(
                database.ReadDBValue<Guid>(reader, profileIdIndex),
                database.ReadDBValue<string>(reader, nameIndex),
                (UserProfileType)database.ReadDBValue<int>(reader, idIndex),
                database.ReadDBValue<string>(reader, dataIndex),
                database.ReadDBValue<DateTime?>(reader, lastLoginIndex),
                database.ReadDBValue<byte[]>(reader, imageIndex))
              );
            }
          }
        }

        if (loadData)
        {
          foreach (var user in result)
          {
            using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserAdditionalDataListCommand(transaction, user.ProfileId, null, false, SortDirection.Ascending,
                out nameIndex, out profileIdIndex, out dataIndex))
            {
              using (IDataReader reader = command.ExecuteReader())
              {
                while (reader.Read())
                {
                  string key = database.ReadDBValue<string>(reader, nameIndex);
                  if (!user.AdditionalData.ContainsKey(key))
                    user.AdditionalData.Add(key, new Dictionary<int, string>());
                  user.AdditionalData[key].Add(database.ReadDBValue<int>(reader, profileIdIndex), database.ReadDBValue<string>(reader, dataIndex));
                }
              }
            }
          }
        }

        return Task.FromResult(result);
      }
      finally
      {
        transaction.Dispose();
      }
    }

    #endregion

    #region IUserProfileDataManagement implementation

    #region User profiles management

    public async Task<ICollection<UserProfile>> GetProfilesAsync()
    {
      return await GetProfiles(null, null);
    }

    public async Task<AsyncResult<UserProfile>> GetProfileAsync(Guid profileId)
    {
      ICollection<UserProfile> profiles = await GetProfiles(profileId, null);
      var userProfile = profiles.FirstOrDefault();
      return new AsyncResult<UserProfile>(userProfile != null, userProfile);
    }

    public async Task<AsyncResult<UserProfile>> GetProfileByNameAsync(string profileName)
    {
      ICollection<UserProfile> profiles = await GetProfiles(null, profileName);
      var userProfile = profiles.FirstOrDefault();
      return new AsyncResult<UserProfile>(userProfile != null, userProfile);
    }

    public async Task<Guid> CreateProfileAsync(string profileName)
    {
      //Profile might already exist.
      var result = await GetProfileByNameAsync(profileName);
      if (result.Success)
        return result.Result.ProfileId;

      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      Guid profileId = Guid.NewGuid();
      try
      {
        using (IDbCommand command = UserProfileDataManagement_SubSchema.CreateUserProfileCommand(transaction, profileId, profileName))
          command.ExecuteNonQuery();
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error creating user profile '{0}')", e, profileName);
        transaction.Rollback();
        throw;
      }
      return profileId;
    }

    public Task<Guid> CreateClientProfileAsync(Guid profileId, string profileName)
    {
      var guid = CreateProfileInternal(profileId, profileName, UserProfileType.ClientProfile, null);
      return  Task.FromResult(guid);
    }

    public async Task<Guid> CreateProfileAsync(string profileName, UserProfileType profileType, string profilePassword)
    {
      //Profile might already exist.
      var result = await GetProfileByNameAsync(profileName);
      if (result.Success)
        return result.Result.ProfileId;

      Guid profileId = Guid.NewGuid();
      return CreateProfileInternal(profileId, profileName, profileType, profilePassword);
    }

    private Guid CreateProfileInternal(Guid profileId, string profileName, UserProfileType profileType, string profilePassword)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = UserProfileDataManagement_SubSchema.CreateUserProfileCommand(transaction, profileId, profileName, profileType, profilePassword))
          command.ExecuteNonQuery();
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error creating user profile '{0}')", e, profileName);
        transaction.Rollback();
        throw;
      }
      return profileId;
    }

    public Task<bool> UpdateProfileAsync(Guid profileId, string profileName, UserProfileType profileType, string profilePassword)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.UpdateUserProfileCommand(transaction, profileId, profileName, profileType, profilePassword))
          result = command.ExecuteNonQuery() > 0;
        transaction.Commit();

        return Task.FromResult(result);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error creating user profile '{0}')", e, profileName);
        transaction.Rollback();
        throw;
      }
    }

    public Task<bool> SetProfileImageAsync(Guid profileId, byte[] profileImage)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.SetUserProfileImageCommand(transaction, profileId, profileImage))
          result = command.ExecuteNonQuery() > 0;
        transaction.Commit();

        return Task.FromResult(result);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error creating user profile '{0}')", e, profileId);
        transaction.Rollback();
        throw;
      }
    }

    public Task<bool> RenameProfileAsync(Guid profileId, string newName)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.UpdateUserProfileNameCommand(transaction, profileId, newName))
          result = command.ExecuteNonQuery() > 0;
        transaction.Commit();

        return Task.FromResult(result);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error renaming profile '{0}'", e, profileId);
        transaction.Rollback();
        throw;
      }
    }

    public Task<bool> ChangeProfileIdAsync(Guid profileId, Guid newProfileId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        int nameIndex;
        string profileName;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserProfileNameCommand(transaction, profileId, out nameIndex))
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            if (reader.Read())
            {
              profileName = database.ReadDBValue<string>(reader, nameIndex);
            }
            else
            {
              transaction.Rollback();
              return Task.FromResult(false);
            }
          }
        }
        using (IDbCommand command = UserProfileDataManagement_SubSchema.UpdateUserProfileNameCommand(transaction, profileId, profileName + "_old"))
          command.ExecuteNonQuery();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.CopyUserProfileCommand(transaction, profileId, newProfileId, profileName))
          result = command.ExecuteNonQuery() > 0;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.CopyUserMediaItemDataCommand(transaction, profileId, newProfileId))
          command.ExecuteNonQuery();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.CopyUserPlaylistDataCommand(transaction, profileId, newProfileId))
          command.ExecuteNonQuery();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.CopyUserAdditionalDataCommand(transaction, profileId, newProfileId))
          command.ExecuteNonQuery();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserProfileCommand(transaction, profileId))
          command.ExecuteNonQuery();
        transaction.Commit();

        return Task.FromResult(result);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error changing profile Id '{0}'", e, profileId);
        transaction.Rollback();
        throw;
      }
    }

    public Task<bool> DeleteProfileAsync(Guid profileId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserProfileCommand(transaction, profileId))
          result = command.ExecuteNonQuery() > 0;
        transaction.Commit();

        return Task.FromResult(result);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error deleting profile '{0}'", e, profileId);
        transaction.Rollback();
        throw;
      }
    }

    public Task<bool> LoginProfileAsync(Guid profileId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.LoginUserProfileCommand(transaction, profileId))
          result = command.ExecuteNonQuery() > 0;
        transaction.Commit();

        return Task.FromResult(result);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error logging in profile '{0}'", e, profileId);
        transaction.Rollback();
        throw;
      }
    }

    #endregion

    #region User playlist data

    public Task<AsyncResult<string>> GetUserPlaylistDataAsync(Guid profileId, Guid playlistId, string key)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int dataIndex;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserPlaylistDataCommand(transaction, profileId, playlistId,
            key, out dataIndex))
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            if (reader.Read())
            {
              string data = database.ReadDBValue<string>(reader, dataIndex);
              return Task.FromResult(new AsyncResult<string>(true, data));
            }
          }
        }
        return Task.FromResult(new AsyncResult<string>(false, null));
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public Task<bool> SetUserPlaylistDataAsync(Guid profileId, Guid playlistId, string key, string data)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserPlaylistDataCommand(transaction, profileId, playlistId, key))
          command.ExecuteNonQuery();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.CreateUserPlaylistDataCommand(transaction, profileId, playlistId, key, data))
          result = command.ExecuteNonQuery() > 0;
        transaction.Commit();
        return Task.FromResult(result);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error setting playlist data '{0}' for playlist '{1}' in profile '{2}'", e, key, playlistId, profileId);
        transaction.Rollback();
        throw;
      }
    }

    #endregion

    #region User media item data

    public Task<AsyncResult<string>> GetUserMediaItemDataAsync(Guid profileId, Guid mediaItemId, string key)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int dataIndex;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserMediaItemDataCommand(transaction, profileId, mediaItemId,
            key, out dataIndex))
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            if (reader.Read())
            {
              var data = database.ReadDBValue<string>(reader, dataIndex);
              return Task.FromResult(new AsyncResult<string>(true, data));
            }
          }
        }
        return Task.FromResult(new AsyncResult<string>(false, null));
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public Task<bool> SetUserMediaItemDataAsync(Guid profileId, Guid mediaItemId, string key, string data)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserMediaItemDataCommand(transaction, profileId, mediaItemId, key))
          command.ExecuteNonQuery();

        // Allow "delete only", if new data is null. This is used to delete no longer required data.
        if (!string.IsNullOrEmpty(data))
        {
          using (IDbCommand command = UserProfileDataManagement_SubSchema.CreateUserMediaItemDataCommand(transaction, profileId, mediaItemId, key, data))
            result = command.ExecuteNonQuery() > 0;
        }
        else
          result = true;

        transaction.Commit();
        return Task.FromResult(result);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error setting media item data '{0}' for media item '{1}' in profile '{2}'", e, key, mediaItemId, profileId);
        transaction.Rollback();
        throw;
      }
    }

    #endregion

    #region User additional data

    public Task<AsyncResult<string>> GetUserAdditionalDataAsync(Guid profileId, string key, int dataNo = 0)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int dataIndex;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserAdditionalDataCommand(transaction, profileId,
            key, dataNo, out dataIndex))
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            if (reader.Read())
            {
              var data = database.ReadDBValue<string>(reader, dataIndex);
              return Task.FromResult(new AsyncResult<string>(true, data));
            }
          }
        }
        return Task.FromResult(new AsyncResult<string>(false, null));
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public Task<bool> SetUserAdditionalDataAsync(Guid profileId, string key, string data, int dataNo = 0)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        bool result;
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserAdditionalDataCommand(transaction, profileId, dataNo, key))
          command.ExecuteNonQuery();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.CreateUserAdditionalDataCommand(transaction, profileId, key, dataNo, data))
          result = command.ExecuteNonQuery() > 0;
        transaction.Commit();

        return Task.FromResult(result);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error setting additional data '{0}' in profile '{1}'", e, key, profileId);
        transaction.Rollback();
        throw;
      }
    }

    public Task<AsyncResult<IEnumerable<Tuple<int, string>>>> GetUserAdditionalDataListAsync(Guid profileId, string key, bool sortByKey = false, SortDirection sortDirection = SortDirection.Ascending, uint? offset = null, uint? limit = null)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int dataNoIndex;
        int dataIndex;
        List<Tuple<int, string>> list = new List<Tuple<int, string>>();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserAdditionalDataListCommand(transaction, profileId,
            key, sortByKey, sortDirection, out dataNoIndex, out dataIndex))
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            var records = reader.AsEnumerable();
            if (offset.HasValue)
              records = records.Skip((int)offset.Value);
            if (limit.HasValue)
              records = records.Take((int)limit.Value);
            foreach (var record in records)
            {
              list.Add(new Tuple<int, string>(database.ReadDBValue<int>(record, dataNoIndex), database.ReadDBValue<string>(record, dataIndex)));
            }
          }
        }
        IEnumerable<Tuple<int, string>> data = null;
        if (list.Count > 0)
          data = list;
        return Task.FromResult(new AsyncResult<IEnumerable<Tuple<int, string>>>(data != null, data));
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public Task<AsyncResult<IEnumerable<Tuple<string, int, string>>>> GetUserSelectedAdditionalDataListAsync(Guid profileId, string[] keys, bool sortByKey = false, SortDirection sortDirection = SortDirection.Ascending, 
      uint? offset = null, uint? limit = null)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int dataNoIndex;
        int dataIndex;
        int keyIndex;
        List<Tuple<string, int, string>> list = new List<Tuple<string, int, string>>();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.SelectUserAdditionalDataListCommand(transaction, profileId,
            keys, sortByKey, sortDirection, out keyIndex, out dataNoIndex, out dataIndex))
        {
          using (IDataReader reader = command.ExecuteReader())
          {
            var records = reader.AsEnumerable();
            if (offset.HasValue)
              records = records.Skip((int)offset.Value);
            if (limit.HasValue)
              records = records.Take((int)limit.Value);
            foreach (var record in records)
            {
              list.Add(new Tuple<string, int, string>(database.ReadDBValue<string>(record, keyIndex), database.ReadDBValue<int>(record, dataNoIndex),
                database.ReadDBValue<string>(record, dataIndex)));
            }
          }
        }
        IEnumerable<Tuple<string, int, string>> data = null;
        if (list.Count > 0)
          data = list;
        return Task.FromResult(new AsyncResult<IEnumerable<Tuple<string, int, string>>>(data != null, data));
      }
      finally
      {
        transaction.Dispose();
      }
    }

    #endregion

    #region Cleanup user data

    public Task<bool> ClearAllUserDataAsync(Guid profileId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserPlaylistDataCommand(transaction, profileId, null, null))
          command.ExecuteNonQuery();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserMediaItemDataCommand(transaction, profileId, null, null))
          command.ExecuteNonQuery();
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserAdditionalDataCommand(transaction, profileId, null, null))
          command.ExecuteNonQuery();
        transaction.Commit();

        return Task.FromResult(true);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error clearing user data for profile '{0}'", e, profileId);
        transaction.Rollback();
        throw;
      }
    }

    public Task<bool> ClearUserMediaItemDataKeyAsync(Guid profileId, string key)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserMediaItemDataCommand(transaction, profileId, null, key))
          command.ExecuteNonQuery();
        transaction.Commit();
        return Task.FromResult(true);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error clearing user media item data for profile '{0}'", e, profileId);
        transaction.Rollback();
        throw;
      }
    }

    public Task<bool> ClearUserAdditionalDataKeyAsync(Guid profileId, string key)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = UserProfileDataManagement_SubSchema.DeleteUserAdditionalDataCommand(transaction, profileId, null, key))
          command.ExecuteNonQuery();
        transaction.Commit();

        return Task.FromResult(true);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserProfileDataManagement: Error clearing user additional data for profile '{0}'", e, profileId);
        transaction.Rollback();
        throw;
      }
    }

    #endregion

    #endregion
  }
}
