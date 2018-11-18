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

using MediaPortal.Common;
using MediaPortal.Common.Certifications;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common.UserManagement;

namespace MediaPortal.UiComponents.Media.Helpers
{
  public static class CertificationHelper
  {
    static CertificationHelper()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      MediaCertificationSettings settings = settingsManager.Load<MediaCertificationSettings>();
      DisplayMovieCertificationCountry = settings.DisplayMovieCertificationCountry;
      DisplaySeriesCertificationCountry = settings.DisplaySeriesCertificationCountry;
    }

    public static string DisplayMovieCertificationCountry { get; set; }
    public static string DisplaySeriesCertificationCountry { get; set; }

    public static IEnumerable<MediaItem> ProcessMediaItems(IEnumerable<MediaItem> mediaItems)
    {
      UserProfile userProfile = null;
      bool applyUserRestrictions = false;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser;
        applyUserRestrictions = userProfileDataManagement.ApplyUserRestriction;
      }

      int allowedAge = 5;
      bool includeParentalGuidedContent = false;
      bool includeUnratedContent = false;
      bool allowAllAges = true;
      if (userProfile != null && applyUserRestrictions)
      {
        foreach (var key in userProfile.AdditionalData)
        {
          foreach (var val in key.Value)
          {
            if (key.Key == UserDataKeysKnown.KEY_ALLOW_ALL_AGES)
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
      }

      List<MediaItem> allowedMedia = new List<MediaItem>();
      foreach (var mediaItem in mediaItems)
      {
        string certification = null;
        CertificationMapping bestMatch = null;
        CertificationMapping mediaMatch = null;
        if (mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
        {
          if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MovieAspect.ATTR_CERTIFICATION, out certification))
          {
            if (applyUserRestrictions && !allowAllAges)
            {
              if (CertificationMapper.TryFindMovieCertification(certification, out mediaMatch))
              {
                if (!CertificationMapper.IsAgeAllowed(mediaMatch, allowedAge, includeParentalGuidedContent))
                  continue;
              }
              if (certification == null && !includeUnratedContent)
                continue;
            }
            if (certification != null && !string.IsNullOrEmpty(DisplayMovieCertificationCountry))
              bestMatch = CertificationMapper.FindMatchingMovieCertification(DisplayMovieCertificationCountry, certification);
          }
        }
        if (mediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
        {
          if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, SeriesAspect.ATTR_CERTIFICATION, out certification))
          {
            if (applyUserRestrictions && !allowAllAges)
            {
              if (CertificationMapper.TryFindSeriesCertification(certification, out mediaMatch))
              {
                if (!CertificationMapper.IsAgeAllowed(mediaMatch, allowedAge, includeParentalGuidedContent))
                  continue;
              }
              if (certification == null && !includeUnratedContent)
                continue;
            }
            if (certification != null && !string.IsNullOrEmpty(DisplaySeriesCertificationCountry))
              bestMatch = CertificationMapper.FindMatchingSeriesCertification(DisplaySeriesCertificationCountry, certification);
          }
        }

        //Assign new certification value
        if (bestMatch != null)
        {
          if (mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
            MediaItemAspect.SetAttribute<string>(mediaItem.Aspects, MovieAspect.ATTR_CERTIFICATION, bestMatch.CertificationId);
          else if (mediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
            MediaItemAspect.SetAttribute<string>(mediaItem.Aspects, SeriesAspect.ATTR_CERTIFICATION, bestMatch.CertificationId);
        }
        else
        {
          if (applyUserRestrictions && !allowAllAges && !includeUnratedContent)
            continue;

          if (mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
            MediaItemAspect.SetAttribute<string>(mediaItem.Aspects, MovieAspect.ATTR_CERTIFICATION, null);
          else if (mediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
            MediaItemAspect.SetAttribute<string>(mediaItem.Aspects, SeriesAspect.ATTR_CERTIFICATION, null);
        }
        allowedMedia.Add(mediaItem);
      }

      return allowedMedia;
    }
    public static async Task<IFilter> GetUserCertificateFilter(IEnumerable<Guid> necessaryMias)
    {
      UserProfile userProfile = null;
      bool applyUserRestrictions = false;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser;
        applyUserRestrictions = userProfileDataManagement.ApplyUserRestriction;
      }
      if (userProfile == null || !applyUserRestrictions)
        return null;

      ICollection<Share> shares = null;
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory contentDirectory = serverConnectionManager.ContentDirectory;
      if (contentDirectory != null)
        shares = await contentDirectory.GetSharesAsync(null, SharesFilter.All);

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
            if (shares == null || !shares.Where(s => s.ShareId == shareId).Any())
              continue;
            shareFilters.Add(new LikeFilter(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, shares.Where(s => s.ShareId == shareId).First().BaseResourcePath + "%", null, true));
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
            if(!includeUnratedContent ?? false)
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

    public static void ConvertCertifications(IEnumerable<MediaItem> mediaItems)
    {
      if (mediaItems == null)
        return;

      //Convert certification system if needed
      if (!string.IsNullOrEmpty(DisplayMovieCertificationCountry) || !string.IsNullOrEmpty(DisplaySeriesCertificationCountry))
      {
        foreach (MediaItem mediaItem in mediaItems.Where(mi => mi.Aspects.ContainsKey(MovieAspect.ASPECT_ID) || mi.Aspects.ContainsKey(SeriesAspect.ASPECT_ID)))
        {
          //Find all possible matches
          string certification = null;
          CertificationMapping bestMatch = null;
          if (mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID) && !string.IsNullOrEmpty(DisplayMovieCertificationCountry))
          {
            if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MovieAspect.ATTR_CERTIFICATION, out certification))
              bestMatch = CertificationMapper.FindMatchingMovieCertification(DisplayMovieCertificationCountry, certification);
          }
          if (mediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID) && !string.IsNullOrEmpty(DisplaySeriesCertificationCountry))
          {
            if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, SeriesAspect.ATTR_CERTIFICATION, out certification))
              bestMatch = CertificationMapper.FindMatchingSeriesCertification(DisplaySeriesCertificationCountry, certification);
          }

          //Assign new certification value
          if (bestMatch != null)
          {
            if (mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
              MediaItemAspect.SetAttribute<string>(mediaItem.Aspects, MovieAspect.ATTR_CERTIFICATION, bestMatch.CertificationId);
            else if (mediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
              MediaItemAspect.SetAttribute<string>(mediaItem.Aspects, SeriesAspect.ATTR_CERTIFICATION, bestMatch.CertificationId);
          }
          else
          {
            if (mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
              MediaItemAspect.SetAttribute<string>(mediaItem.Aspects, MovieAspect.ATTR_CERTIFICATION, null);
            else if (mediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
              MediaItemAspect.SetAttribute<string>(mediaItem.Aspects, SeriesAspect.ATTR_CERTIFICATION, null);
          }
        }
      }
    }
  }
}
