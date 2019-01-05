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
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Certifications;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Settings;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Microsoft.Owin;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  internal class ResourceAccessUtils
  {
    internal static Guid? DefaultGuid = null;
    internal static string DefaultUser = "MP2Extended";

    internal static byte[] GetBytes(string str)
    {
      //ASCIIEncoding enc = new ASCIIEncoding();
      UTF8Encoding enc = new UTF8Encoding();
      return enc.GetBytes(str);
    }

    internal static Guid? GetUser(IOwinContext context)
    {
      var claim = context.Authentication.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Sid);
      var id = claim?.Value != null ? Guid.Parse(claim.Value) : (Guid?)null;
      if (id != null)
        return id;
      if (DefaultGuid.HasValue)
        return DefaultGuid;

      var userManagement = ServiceRegistration.Get<IUserProfileDataManagement>(false);
      if (userManagement != null)
      {
        var user = userManagement.GetProfileByNameAsync(DefaultUser).Result;
        if (user.Success)
          DefaultGuid = user.Result.ProfileId;
        else
          DefaultGuid = userManagement.CreateProfileAsync(DefaultUser, UserProfileType.UserProfile, "").Result;
      }
      return DefaultGuid;
    }

    internal static IList<IChannelGroup> FilterGroups(Guid? userId, IList<IChannelGroup> channelGroups)
    {
      UserProfile userProfile = null;
      IUserProfileDataManagement userProfileDataManagement = ServiceRegistration.Get<IUserProfileDataManagement>();
      if (userProfileDataManagement != null)
      {
        userProfile = (userProfileDataManagement.GetProfileAsync(userId.Value).Result)?.Result;
        if (userProfile != null)
        {
          IList<IChannelGroup> filteredGroups = new List<IChannelGroup>();
          foreach (IChannelGroup channelGroup in channelGroups)
          {
            IUserRestriction restriction = channelGroup as IUserRestriction;
            if (restriction != null && !userProfile.CheckUserAccess(restriction))
              continue;
            filteredGroups.Add(channelGroup);
          }
          return filteredGroups;
        }
      }
      return channelGroups;
    }

    internal static bool CheckUserAccess(UserProfile userProfile, IUserRestriction restrictedElement)
    {
      if (!userProfile.EnableRestrictionGroups || string.IsNullOrEmpty(restrictedElement.RestrictionGroup))
        return true;

      foreach (var group in restrictedElement.RestrictionGroup.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
        if (userProfile.RestrictionGroups.Contains(group))
          return true;
      return false;
    }

    internal static IFilter AppendUserFilter(Guid? userId, IFilter filter, IEnumerable<Guid> filterMias)
    {
      IFilter userFilter = null;
      if (userId.HasValue)
      {
        IUserProfileDataManagement userProfileDataManagement = ServiceRegistration.Get<IUserProfileDataManagement>();
        var res = userProfileDataManagement.GetProfileAsync(userId.Value).Result;
        if (res.Success)
        {
          IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
          ICollection<Share> shares = library.GetShares(null)?.Values;
          userFilter = res.Result.GetUserFilter(filterMias, shares);
        }
      }
      return filter == null ? userFilter : userFilter != null ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, userFilter) : filter;
    }

    public static async Task<bool> AddPreferredLanguagesAsync(Guid? userId, IList<string> preferredAudioLanguuages, IList<string> preferredSubtitleLanguuages)
    {
      IUserProfileDataManagement userProfileDataManagement = ServiceRegistration.Get<IUserProfileDataManagement>();
      if (userId.HasValue)
      {
        var res = userProfileDataManagement.GetProfileAsync(userId.Value).Result;
        if (res.Success)
        {
          await userProfileDataManagement.LoginProfileAsync(userId.Value);
          foreach (var lang in res.Result.GetPreferredAudioLanguages())
            preferredAudioLanguuages.Add(lang);
          foreach (var lang in res.Result.GetPreferredSubtitleLanguages())
            preferredSubtitleLanguuages.Add(lang);
        }
      }
      if (preferredAudioLanguuages.Count == 0)
        preferredAudioLanguuages = new List<string>() { "EN" };
      if (preferredSubtitleLanguuages.Count == 0)
        preferredSubtitleLanguuages = new List<string>() { "EN" };

      return true;
    }

    internal static WebMediaType GetWebMediaType(MediaItem mediaItem)
    {
      if (mediaItem.Aspects.ContainsKey(VideoAspect.ASPECT_ID) || mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
      {
        return WebMediaType.Movie;
      }

      if (mediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
      {
        return WebMediaType.TVEpisode;
      }

      if (mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        return WebMediaType.MusicTrack;
      }

      return WebMediaType.File;
    }
  }
}
