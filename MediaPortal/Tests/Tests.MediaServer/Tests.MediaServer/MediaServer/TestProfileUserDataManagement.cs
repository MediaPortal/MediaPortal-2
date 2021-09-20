using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.UserProfileDataManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Server.MediaServer
{
  class TestUserProfileDataManagement : IUserProfileDataManagement
  {
    public Task<bool> ChangeProfileIdAsync(Guid profileId, Guid newProfileId)
    {
      return Task.FromResult(true);
    }

    public Task<UsageStatistics> GetFeatureUsageStatisticsAsync(Guid profileId, string scope)
    {
      throw new NotImplementedException();
    }

    public Task<bool> ClearAllUserDataAsync(Guid profileId)
    {
      return Task.FromResult(true);
    }

    public Task<bool> ClearUserAdditionalDataKeyAsync(Guid profileId, string key)
    {
      return Task.FromResult(true);
    }

    public Task<bool> ClearUserMediaItemDataKeyAsync(Guid profileId, string key)
    {
      throw new NotImplementedException();
    }

    public Task<Guid> CreateClientProfileAsync(Guid profileId, string profileName)
    {
      return Task.FromResult(Guid.NewGuid());
    }

    public Task<Guid> CreateProfileAsync(string profileName)
    {
      return Task.FromResult(Guid.NewGuid());
    }

    public Task<Guid> CreateProfileAsync(string profileName, UserProfileType profileType, string profilePassword)
    {
      return Task.FromResult(Guid.NewGuid());
    }

    public Task<bool> DeleteProfileAsync(Guid profileId)
    {
      return Task.FromResult(true);
    }

    public Task<AsyncResult<UserProfile>> GetProfileAsync(Guid profileId)
    {
      return Task.FromResult(new AsyncResult<UserProfile>(true, new UserProfile(profileId, "Test")));
    }

    public Task<AsyncResult<UserProfile>> GetProfileByNameAsync(string profileName)
    {
      return Task.FromResult(new AsyncResult<UserProfile>(true, new UserProfile(Guid.NewGuid(), profileName)));
    }

    public Task<ICollection<UserProfile>> GetProfilesAsync()
    {
      return Task.FromResult((ICollection<UserProfile>)new List<UserProfile>());
    }

    public Task<AsyncResult<string>> GetUserAdditionalDataAsync(Guid profileId, string key, int dataNo = 0)
    {
      throw new NotImplementedException();
    }

    public Task<AsyncResult<IEnumerable<Tuple<int, string>>>> GetUserAdditionalDataListAsync(Guid profileId, string key, bool sortByKey = false, SortDirection sortDirection = SortDirection.Ascending, uint? offset = null, uint? limit = null)
    {
      throw new NotImplementedException();
    }

    public Task<AsyncResult<string>> GetUserMediaItemDataAsync(Guid profileId, Guid mediaItemId, string key)
    {
      throw new NotImplementedException();
    }

    public Task<AsyncResult<string>> GetUserPlaylistDataAsync(Guid profileId, Guid playlistId, string key)
    {
      throw new NotImplementedException();
    }

    public Task<AsyncResult<IEnumerable<Tuple<string, int, string>>>> GetUserSelectedAdditionalDataListAsync(Guid profileId, string[] keys, bool sortByKey = false, SortDirection sortDirection = SortDirection.Ascending, uint? offset = null, uint? limit = null)
    {
      throw new NotImplementedException();
    }

    public Task<bool> NotifyFeatureUsageAsync(Guid profileId, string scope, string usedItem)
    {
      throw new NotImplementedException();
    }

    public Task<bool> LoginProfileAsync(Guid profileId)
    {
      return Task.FromResult(true);
    }

    public Task<bool> RenameProfileAsync(Guid profileId, string newName)
    {
      return Task.FromResult(true);
    }

    public Task<bool> SetProfileImageAsync(Guid profileId, byte[] profileImage)
    {
      return Task.FromResult(true);
    }

    public Task<bool> SetUserAdditionalDataAsync(Guid profileId, string key, string data, int dataNo = 0)
    {
      return Task.FromResult(true);
    }

    public Task<bool> SetUserMediaItemDataAsync(Guid profileId, Guid mediaItemId, string key, string data)
    {
      return Task.FromResult(true);
    }

    public Task<bool> SetUserPlaylistDataAsync(Guid profileId, Guid playlistId, string key, string data)
    {
      return Task.FromResult(true);
    }

    public Task<bool> UpdateProfileAsync(Guid profileId, string profileName, UserProfileType profileType, string profilePassword)
    {
      return Task.FromResult(true);
    }
  }
}
