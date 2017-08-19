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

using System.Collections.Generic;
using System.IO;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common;
using System.Linq;
using System;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Common.Certifications;

namespace MediaPortal.UiComponents.Media.Views.RemovableMediaDrives
{
  /// <summary>
  /// Base class for an <see cref="IRemovableDriveHandler"/>. Provides a default handling for the <see cref="VolumeLabel"/> property.
  /// </summary>
  public abstract class BaseDriveHandler : IRemovableDriveHandler
  {
    #region Protected fields

    protected DriveInfo _driveInfo;
    protected string _volumeLabel;

    #endregion

    protected BaseDriveHandler(DriveInfo driveInfo)
    {
      _driveInfo = driveInfo;
      _volumeLabel = null;
      if (_driveInfo.IsReady)
        try
        {
          _volumeLabel = _driveInfo.VolumeLabel;
        }
        catch (IOException)
        {
        }
      _volumeLabel = _driveInfo.RootDirectory.Name + (string.IsNullOrEmpty(_volumeLabel) ? string.Empty : ("(" + _volumeLabel + ")"));
    }

    /// <summary>
    /// Returns a string of the form <c>"D: (Videos)"</c> or <c>"D:"</c>.
    /// </summary>
    public virtual string VolumeLabel
    {
      get { return _volumeLabel; }
    }

    public abstract IList<MediaItem> MediaItems { get; }
    public abstract IList<ViewSpecification> SubViewSpecifications { get; }
    public abstract IEnumerable<MediaItem> GetAllMediaItems();
    public static MediaItem FindStub(DriveInfo driveInfo, MediaItem mediaItem)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return mediaItem;

      IList<MediaItem> existingItems;
      IFilter filter = null;
      Guid? userProfile = null;
      bool applyUserRestrictions = false;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;
        applyUserRestrictions = userProfileDataManagement.ApplyUserRestriction;
      }

      //Try merge handlers
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      foreach (IMediaMergeHandler mergeHandler in mediaAccessor.LocalMergeHandlers.Values)
      {
        IDictionary<Guid, IList<MediaItemAspect>> extractedAspects = mediaItem.Aspects;
        // Extracted aspects must contain all of mergeHandler.MergeableAspects
        if (mergeHandler.MergeableAspects.All(g => extractedAspects.Keys.Contains(g)))
        {
          filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, 
            mergeHandler.GetSearchFilter(extractedAspects), 
            new RelationalFilter(MediaAspect.ATTR_ISSTUB, RelationalOperator.EQ, true));
          if (filter != null)
          {
            existingItems = cd.Search(new MediaItemQuery(mergeHandler.MergeableAspects, Consts.OPTIONAL_MEDIA_LIBRARY_BROWSING_MIAS, filter), false, userProfile, true, applyUserRestrictions);
            foreach (MediaItem existingItem in existingItems)
            {
              if (mergeHandler.TryMatch(extractedAspects, existingItem.Aspects))
              {
                mergeHandler.TryMerge(extractedAspects, existingItem.Aspects);
                return existingItem;
              }
            }
          }
        }
      }

      //Try stub label
      filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
          new RelationalFilter(MediaAspect.ATTR_STUB_LABEL, RelationalOperator.EQ, driveInfo.VolumeLabel),
          new RelationalFilter(MediaAspect.ATTR_ISSTUB, RelationalOperator.EQ, true));
      int trackNo = 0;
      if(mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(mediaItem.Aspects, AudioAspect.ATTR_TRACK, 0, out trackNo) && trackNo > 0)
      {
        filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, new RelationalFilter(AudioAspect.ATTR_TRACK, RelationalOperator.EQ, trackNo));
      }
      existingItems = cd.Search(new MediaItemQuery(Consts.NECESSARY_BROWSING_MIAS, Consts.OPTIONAL_MEDIA_LIBRARY_BROWSING_MIAS, filter), false, userProfile, true, applyUserRestrictions);
      if (existingItems != null && existingItems.Count == 1)
      {
        MediaItem existingItem = existingItems.First();
        if (existingItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID) && mediaItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID))
        {
          int newResIndex = 0;
          foreach (MediaItemAspect mia in existingItem.Aspects[ProviderResourceAspect.ASPECT_ID])
          {
            if (newResIndex <= mia.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX))
              newResIndex = mia.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX) + 1;
          }
          foreach (MediaItemAspect mia in mediaItem.Aspects[ProviderResourceAspect.ASPECT_ID])
          {
            mia.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, newResIndex);
            existingItem.Aspects[ProviderResourceAspect.ASPECT_ID].Add(mia);
            newResIndex++;
          }
        }
        return existingItem;
      }

      return mediaItem;
    }
    public static bool ProcessMediaItem(MediaItem mediaItem, UserProfile user)
    {
      if (user == null)
        return true;

      string movieCertificationSystemCountry = null;
      string seriesCertificationSystemCountry = null;
      int? allowedAge = null;
      bool? includeParentalGuidedContent = null;
      bool allowAllAges = true;
      foreach (var key in user.AdditionalData)
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
          else if (key.Key == UserDataKeysKnown.KEY_MOVIE_CONTENT_CERTIFICATION_SYSTEM_COUNTRY)
          {
            movieCertificationSystemCountry = val.Value;
            if (string.IsNullOrEmpty(movieCertificationSystemCountry))
            {
              movieCertificationSystemCountry = null;
            }
          }
          else if (key.Key == UserDataKeysKnown.KEY_SERIES_CONTENT_CERTIFICATION_SYSTEM_COUNTRY)
          {
            seriesCertificationSystemCountry = val.Value;
            if (string.IsNullOrEmpty(seriesCertificationSystemCountry))
            {
              seriesCertificationSystemCountry = null;
            }
          }
        }
      }

      //Convert certification system if needed
      if (!string.IsNullOrEmpty(movieCertificationSystemCountry) || !string.IsNullOrEmpty(seriesCertificationSystemCountry))
      {
        //Find all possible matches
        string certification = null;
        CertificationMapping bestMatch = null;
        if (mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID) && !string.IsNullOrEmpty(movieCertificationSystemCountry))
        {
          if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MovieAspect.ATTR_CERTIFICATION, out certification))
            bestMatch = CertificationMapper.FindMatchingMovieCertification(movieCertificationSystemCountry, certification);
        }
        if (mediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID) && !string.IsNullOrEmpty(seriesCertificationSystemCountry))
        {
          if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, SeriesAspect.ATTR_CERTIFICATION, out certification))
            bestMatch = CertificationMapper.FindMatchingSeriesCertification(seriesCertificationSystemCountry, certification);
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

      return true;
    }
  }
}
