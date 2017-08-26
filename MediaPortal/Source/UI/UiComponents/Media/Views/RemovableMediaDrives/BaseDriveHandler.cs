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
    private const int MAX_MERGE_THRESHOLD = 20;

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
    public static void MatchWithStubs(DriveInfo driveInfo, IEnumerable<MediaItem> mediaItems)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return;

      IList<MediaItem> existingItems;
      IFilter filter = null;
      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;
      }

      if (mediaItems.Count() <= MAX_MERGE_THRESHOLD)
      {
        //Try merge handlers
        IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
        foreach (var mediaItem in mediaItems)
        {
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
                existingItems = cd.Search(new MediaItemQuery(mergeHandler.MergeableAspects, Consts.OPTIONAL_MEDIA_LIBRARY_BROWSING_MIAS, filter), false, userProfile, false);
                bool merged = false;
                foreach (MediaItem existingItem in existingItems)
                {
                  if (mergeHandler.TryMatch(extractedAspects, existingItem.Aspects))
                  {
                    merged = mergeHandler.TryMerge(extractedAspects, existingItem.Aspects);
                    break;
                  }
                }
                if (merged)
                  break;
              }
            }
          }
        }
      }

      //Try stub label
      filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
          new RelationalFilter(StubAspect.ATTR_DISC_NAME, RelationalOperator.EQ, driveInfo.VolumeLabel),
          new RelationalFilter(MediaAspect.ATTR_ISSTUB, RelationalOperator.EQ, true));
      existingItems = cd.Search(new MediaItemQuery(Consts.NECESSARY_BROWSING_MIAS, Consts.OPTIONAL_MEDIA_LIBRARY_BROWSING_MIAS, filter), false, userProfile, false);
      foreach (var existingItem in existingItems)
      {
        foreach (var mediaItem in mediaItems.Where(mi => mi.PrimaryResources.Count == 0))
        {
          int miNo = 0;
          int existingNo = 0;
          bool merge = false;
          if (mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID) && existingItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID) &&
            MediaItemAspect.TryGetAttribute(mediaItem.Aspects, AudioAspect.ATTR_TRACK, 0, out miNo) && miNo > 0 &&
            MediaItemAspect.TryGetAttribute(existingItem.Aspects, AudioAspect.ATTR_TRACK, 0, out existingNo) && existingNo > 0 &&
            miNo == existingNo &&
            existingItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID) && mediaItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID))
          {
            merge = true;
          }
          else if (mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID) && existingItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID) && 
            existingItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID) && mediaItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID))
          {
            merge = true;
          }
          else if (mediaItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID) && existingItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID) && 
            MediaItemAspect.TryGetAttribute(mediaItem.Aspects, EpisodeAspect.ATTR_SEASON, -1, out miNo) && miNo >= 0 &&
            MediaItemAspect.TryGetAttribute(existingItem.Aspects, EpisodeAspect.ATTR_SEASON, -1, out existingNo) && existingNo >= 0 &&
            miNo == existingNo &&
            mediaItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(mediaItem.Aspects, EpisodeAspect.ATTR_EPISODE, 0, out miNo) && miNo > 0 &&
            existingItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(existingItem.Aspects, EpisodeAspect.ATTR_EPISODE, 0, out existingNo) && existingNo > 0 &&
            miNo == existingNo &&
            existingItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID) && mediaItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID))
          {
            merge = true;
          }
          if (merge)
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
        }
      }
    }
  }
}
