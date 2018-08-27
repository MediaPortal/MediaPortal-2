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
      bool applyUserRestrictions = false;
      IUserProfileDataManagement userProfileDataManagement = ServiceRegistration.Get<IUserProfileDataManagement>();
      if (userProfileDataManagement != null)
      {
        userProfile = (userProfileDataManagement.GetProfileAsync(userId.Value).Result)?.Result;
        applyUserRestrictions = true;
      }
      if (userProfile == null || !applyUserRestrictions)
        return channelGroups;

      IList<IChannelGroup> filteredGroups = new List<IChannelGroup>();
      foreach (IChannelGroup channelGroup in channelGroups)
      {
        IUserRestriction restriction = channelGroup as IUserRestriction;
        if (restriction != null && !CheckUserAccess(userProfile, restriction))
          continue;
        filteredGroups.Add(channelGroup);
      }
      return filteredGroups;
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
      IFilter userFilter = GetUserFilter(userId, filterMias);
      if (userFilter != null)
        return filter != null ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, userFilter) : userFilter;

      return filter;
    }

    internal static IFilter GetUserFilter(Guid? userId, IEnumerable<Guid> necessaryMias)
    {
      if (!userId.HasValue)
        return null;

      UserProfile userProfile = null;
      bool applyUserRestrictions = false;
      IUserProfileDataManagement userProfileDataManagement = ServiceRegistration.Get<IUserProfileDataManagement>();
      if (userProfileDataManagement != null)
      {
        userProfile = (userProfileDataManagement.GetProfileAsync(userId.Value).Result)?.Result;
        applyUserRestrictions = true;
      }
      if (userProfile == null || !applyUserRestrictions)
        return null;

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      var shares = mediaLibrary?.GetShares(null);

      int? allowedAge = null;
      bool? includeParentalGuidedContent = null;
      bool? includeUnratedContent = null;
      bool allowAllShares = true;
      bool allowAllAges = true;
      List<IFilter> shareFilters = new List<IFilter>();
      foreach (var key in userProfile.AdditionalData)
      {
        foreach (var val in key.Value)
        {
          if (key.Key == UserDataKeysKnown.KEY_ALLOW_ALL_SHARES)
          {
            string allow = val.Value;
            if (!string.IsNullOrEmpty(allow) && Convert.ToInt32(allow) >= 0)
            {
              allowAllShares = Convert.ToInt32(allow) > 0;
            }
          }
          else if (key.Key == UserDataKeysKnown.KEY_ALLOWED_SHARE)
          {
            Guid shareId = new Guid(val.Value);
            if (shares == null || !shares.Values.Where(s => s.ShareId == shareId).Any())
              continue;
            shareFilters.Add(new LikeFilter(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, shares.Values.Where(s => s.ShareId == shareId).First().BaseResourcePath + "%", null, true));
          }
          else if (key.Key == UserDataKeysKnown.KEY_ALLOW_ALL_AGES)
          {
            string allow = val.Value;
            if (!string.IsNullOrEmpty(allow) && Convert.ToInt32(allow) >= 0)
            {
              allowAllAges = Convert.ToInt32(allow) > 0;
            }
          }
          else if (key.Key == UserDataKeysKnown.KEY_ALLOWED_AGE)
          {
            string age = val.Value;
            if (!string.IsNullOrEmpty(age) && Convert.ToInt32(age) >= 0)
            {
              allowedAge = Convert.ToInt32(age);
            }
          }
          else if (key.Key == UserDataKeysKnown.KEY_INCLUDE_PARENT_GUIDED_CONTENT)
          {
            string allow = val.Value;
            if (!string.IsNullOrEmpty(allow) && Convert.ToInt32(allow) >= 0)
            {
              includeParentalGuidedContent = Convert.ToInt32(allow) > 0;
            }
          }
          else if (key.Key == UserDataKeysKnown.KEY_INCLUDE_UNRATED_CONTENT)
          {
            string allow = val.Value;
            if (!string.IsNullOrEmpty(allow) && Convert.ToInt32(allow) >= 0)
            {
              includeUnratedContent = Convert.ToInt32(allow) > 0;
            }
          }
        }
      }

      List<IFilter> filters = new List<IFilter>();

      // Shares filter
      if (allowAllShares == false)
      {
        if (shareFilters.Count > 0)
          filters.Add(BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, shareFilters.ToArray()));
        else
          filters.Add(new RelationalFilter(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RelationalOperator.EQ, ""));
      }

      // Content filter
      if (allowedAge.HasValue && allowAllAges == false)
      {
        if (necessaryMias.Contains(MovieAspect.ASPECT_ID))
        {
          IEnumerable<CertificationMapping> certs = CertificationMapper.GetMovieCertificationsForAge(allowedAge.Value, includeParentalGuidedContent ?? false);
          if (certs.Count() > 0)
          {
            if (!includeUnratedContent ?? false)
              filters.Add(new InFilter(MovieAspect.ATTR_CERTIFICATION, certs.Select(c => c.CertificationId)));
            else
              filters.Add(BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
                new InFilter(MovieAspect.ATTR_CERTIFICATION, certs.Select(c => c.CertificationId)),
                new EmptyFilter(MovieAspect.ATTR_CERTIFICATION)));
          }
          else if (!includeUnratedContent ?? false)
          {
            filters.Add(new NotFilter(new EmptyFilter(MovieAspect.ATTR_CERTIFICATION)));
          }
        }
        else if (necessaryMias.Contains(SeriesAspect.ASPECT_ID))
        {
          //TODO: Should series filters reset the share filter? Series have no share dependency
          IEnumerable<CertificationMapping> certs = CertificationMapper.GetSeriesCertificationsForAge(allowedAge.Value, includeParentalGuidedContent ?? false);
          if (certs.Count() > 0)
          {
            if (!includeUnratedContent ?? false)
              filters.Add(new InFilter(SeriesAspect.ATTR_CERTIFICATION, certs.Select(c => c.CertificationId)));
            else
              filters.Add(BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
                new InFilter(SeriesAspect.ATTR_CERTIFICATION, certs.Select(c => c.CertificationId)),
                new EmptyFilter(SeriesAspect.ATTR_CERTIFICATION)));
          }
          else if (!includeUnratedContent ?? false)
          {
            filters.Add(new NotFilter(new EmptyFilter(SeriesAspect.ATTR_CERTIFICATION)));
          }
        }
        else if (necessaryMias.Contains(EpisodeAspect.ASPECT_ID))
        {
          IEnumerable<CertificationMapping> certs = CertificationMapper.GetSeriesCertificationsForAge(allowedAge.Value, includeParentalGuidedContent ?? false);
          if (certs.Count() > 0)
          {
            if (!includeUnratedContent ?? false)
              filters.Add(new FilteredRelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES, new InFilter(SeriesAspect.ATTR_CERTIFICATION, certs.Select(c => c.CertificationId))));
            else
              filters.Add(new FilteredRelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES,
                BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
                new InFilter(SeriesAspect.ATTR_CERTIFICATION, certs.Select(c => c.CertificationId)),
                new EmptyFilter(SeriesAspect.ATTR_CERTIFICATION))));
          }
          else if (!includeUnratedContent ?? false)
          {
            filters.Add(new FilteredRelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES,
                new NotFilter(new EmptyFilter(SeriesAspect.ATTR_CERTIFICATION))));
          }
        }
      }

      if (filters.Count > 1)
        return BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filters.ToArray());
      else if (filters.Count > 0)
        return filters[0];

      return null;
    }

    public static async Task<bool> AddPreferredLanguagesAsync(Guid? userId, IList<string> preferredAudioLanguuages, IList<string> preferredSubtitleLanguuages)
    {
      IUserProfileDataManagement userManager = ServiceRegistration.Get<IUserProfileDataManagement>();
      if (userId.HasValue)
      {
        await userManager.LoginProfileAsync(userId.Value);
        var audioList = await userManager.GetUserAdditionalDataListAsync(userId.Value, UserDataKeysKnown.KEY_PREFERRED_AUDIO_LANGUAGE);
        foreach (var lang in audioList.Result.Select(l => l.Item2))
          preferredAudioLanguuages.Add(lang);
        var subtitleList = await userManager.GetUserAdditionalDataListAsync(userId.Value, UserDataKeysKnown.KEY_PREFERRED_SUBTITLE_LANGUAGE);
        foreach(var lang in subtitleList.Result.Select(l => l.Item2))
          preferredSubtitleLanguuages.Add(lang);
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
