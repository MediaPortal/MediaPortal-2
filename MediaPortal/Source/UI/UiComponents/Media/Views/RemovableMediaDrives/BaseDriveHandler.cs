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
using System.Collections;
using MediaPortal.Utilities;
using MediaPortal.Common.Logging;
using MediaPortal.Common.UserManagement;

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
    public static void MatchWithStubs(DriveInfo driveInfo, IEnumerable<MediaItem> mediaItems)
    {
      try
      {
        IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
        if (cd == null)
          return;

        IList<MediaItem> existingItems;
        IFilter filter = null;
        Guid? userProfile = null;
        IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
        IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
        if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
        {
          userProfile = userProfileDataManagement.CurrentUser.ProfileId;
        }

        //Try stub label
        if (mediaItems.Where(mi => mi.MediaItemId == Guid.Empty).Any())
        {
          filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
              new RelationalFilter(StubAspect.ATTR_DISC_NAME, RelationalOperator.EQ, driveInfo.VolumeLabel),
              new RelationalFilter(MediaAspect.ATTR_ISSTUB, RelationalOperator.EQ, true));
          existingItems = cd.SearchAsync(new MediaItemQuery(Consts.NECESSARY_BROWSING_MIAS, Consts.OPTIONAL_MEDIA_LIBRARY_BROWSING_MIAS, filter), false, userProfile, false).Result;
          foreach (var mediaItem in mediaItems.Where(mi => mi.MediaItemId == Guid.Empty))
          {
            MediaItem match = null;
            if (existingItems.Count == 1)
            {
              //Presume that it is a match
              match = existingItems.First();
            }
            else if (AllExistingSameSeason(existingItems))
            {
              //Presume that it is a match to a season disc
              match = existingItems.First();
            }
            else
            {
              foreach (var existingItem in existingItems)
              {
                int miNo = 0;
                int existingNo = 0;
                string miText = null;
                string existingText = null;
                IEnumerable collection;
                IEnumerable existingCollection;

                if (mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID) && existingItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID) &&
                  MediaItemAspect.TryGetAttribute(mediaItem.Aspects, AudioAspect.ATTR_TRACK, 0, out miNo) && miNo > 0 &&
                  MediaItemAspect.TryGetAttribute(existingItem.Aspects, AudioAspect.ATTR_TRACK, 0, out existingNo) && existingNo > 0 &&
                  miNo == existingNo &&
                  MediaItemAspect.TryGetAttribute(mediaItem.Aspects, AudioAspect.ATTR_ALBUM, out miText) && !string.IsNullOrEmpty(miText) &&
                  MediaItemAspect.TryGetAttribute(existingItem.Aspects, AudioAspect.ATTR_ALBUM, out existingText) && !string.IsNullOrEmpty(existingText) &&
                  existingItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID) && mediaItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID))
                {
                  match = existingItem;
                }
                else if (mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID) && existingItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID) &&
                  existingItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID) && mediaItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID))
                {
                  match = existingItem;
                }
                else if (mediaItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID) && existingItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID) &&
                  MediaItemAspect.TryGetAttribute(mediaItem.Aspects, EpisodeAspect.ATTR_SEASON, -1, out miNo) && miNo >= 0 &&
                  MediaItemAspect.TryGetAttribute(existingItem.Aspects, EpisodeAspect.ATTR_SEASON, -1, out existingNo) && existingNo >= 0 &&
                  miNo == existingNo &&
                  mediaItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(mediaItem.Aspects, EpisodeAspect.ATTR_EPISODE, out collection) &&
                  existingItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(existingItem.Aspects, EpisodeAspect.ATTR_EPISODE, out existingCollection) &&
                  existingItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID) && mediaItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID))
                {
                  List<int> episodes = new List<int>();
                  List<int> existingEpisodes = new List<int>();
                  CollectionUtils.AddAll(episodes, collection.Cast<int>());
                  if (episodes.Intersect(existingEpisodes).Any())
                    match = existingItem;
                }
                if (match != null)
                  break;
              }
            }

            if (match != null)
            {
              foreach (IMediaMergeHandler mergeHandler in mediaAccessor.LocalMergeHandlers.Values)
              {
                if (mergeHandler.MergeableAspects.All(g => match.Aspects.Keys.Contains(g)))
                {
                  if (mergeHandler.TryMerge(match.Aspects, mediaItem.Aspects))
                  {
                    mediaItem.AssignMissingId(match.MediaItemId);
                    break;
                  }
                }
              }
            }
          }
        }

        //Try merge handlers
        foreach (var mediaItem in mediaItems.Where(mi => mi.MediaItemId == Guid.Empty && mi.Aspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID)))
        {
          foreach (IMediaMergeHandler mergeHandler in mediaAccessor.LocalMergeHandlers.Values)
          {
            if (mergeHandler.MergeableAspects.All(g => mediaItem.Aspects.Keys.Contains(g)))
            {
              filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
                mergeHandler.GetSearchFilter(mediaItem.Aspects),
                new RelationalFilter(MediaAspect.ATTR_ISSTUB, RelationalOperator.EQ, true));
              if (filter != null)
              {
                List<Guid> necessaryAspects = Consts.NECESSARY_BROWSING_MIAS.Union(mergeHandler.MergeableAspects).ToList();
                if (!necessaryAspects.Contains(ExternalIdentifierAspect.ASPECT_ID))
                  necessaryAspects.Add(ExternalIdentifierAspect.ASPECT_ID);
                existingItems = cd.SearchAsync(new MediaItemQuery(necessaryAspects, Consts.OPTIONAL_MEDIA_LIBRARY_BROWSING_MIAS, filter), false, userProfile, false).Result;
                bool merged = false;
                if (existingItems.Count == 1)
                {
                  MediaItem macth = existingItems.First();
                  if ((merged = mergeHandler.TryMerge(macth.Aspects, mediaItem.Aspects)))
                  {
                    mediaItem.AssignMissingId(macth.MediaItemId);
                    break;
                  }
                }
                else
                {
                  foreach (MediaItem existingItem in existingItems)
                  {
                    if (mergeHandler.TryMatch(existingItem.Aspects, mediaItem.Aspects))
                    {
                      if ((merged = mergeHandler.TryMerge(existingItem.Aspects, mediaItem.Aspects)))
                      {
                        mediaItem.AssignMissingId(existingItem.MediaItemId);
                        break;
                      }
                    }
                  }
                }
                if (merged)
                  break;
              }
            }
          }
        }

        //Try audio search
        if (mediaItems.Where(mi => mi.MediaItemId == Guid.Empty && mi.Aspects.ContainsKey(AudioAspect.ASPECT_ID)).Any())
        {
          foreach (var mediaItem in mediaItems.Where(mi => mi.MediaItemId == Guid.Empty && mi.Aspects.ContainsKey(AudioAspect.ASPECT_ID)))
          {
            string album;
            int trackNo;
            if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, AudioAspect.ATTR_ALBUM, out album) && !string.IsNullOrEmpty(album) &&
              MediaItemAspect.TryGetAttribute(mediaItem.Aspects, AudioAspect.ATTR_TRACK, 0, out trackNo) && trackNo > 0)
            {
              filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
              new RelationalFilter(AudioAspect.ATTR_ALBUM, RelationalOperator.EQ, album),
              new RelationalFilter(MediaAspect.ATTR_ISSTUB, RelationalOperator.EQ, true));
              existingItems = cd.SearchAsync(new MediaItemQuery(Consts.NECESSARY_BROWSING_MIAS, Consts.OPTIONAL_MEDIA_LIBRARY_BROWSING_MIAS, filter), false, userProfile, false).Result;
              foreach (var existingItem in existingItems)
              {
                int miNo = 0;
                int existingNo = 0;
                if (mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID) && existingItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID) &&
                  MediaItemAspect.TryGetAttribute(mediaItem.Aspects, AudioAspect.ATTR_TRACK, 0, out miNo) && miNo > 0 &&
                  MediaItemAspect.TryGetAttribute(existingItem.Aspects, AudioAspect.ATTR_TRACK, 0, out existingNo) && existingNo > 0 &&
                  miNo == existingNo &&
                  existingItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID) && mediaItem.Aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID))
                {
                  foreach (IMediaMergeHandler mergeHandler in mediaAccessor.LocalMergeHandlers.Values)
                  {
                    if (mergeHandler.MergeableAspects.All(g => existingItem.Aspects.Keys.Contains(g)))
                    {
                      if (mergeHandler.TryMerge(existingItem.Aspects, mediaItem.Aspects))
                      {
                        mediaItem.AssignMissingId(existingItem.MediaItemId);
                        break;
                      }
                    }
                  }
                  break;
                }
              }
            }
          }
        }
      }
      catch(Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error matching disc items with stubs", ex);
      }
    }
    public static bool AllExistingSameSeason(IEnumerable<MediaItem> mediaItems)
    {
      int previousNo = -1;
      int seasonNo = 0;
      foreach(var mediaItem in mediaItems)
      {
        if (mediaItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(mediaItem.Aspects, EpisodeAspect.ATTR_SEASON, -1, out seasonNo) && seasonNo >= 0 &&
            previousNo == -1 || previousNo == seasonNo)
        {
          previousNo = seasonNo;
        }
        else
        {
          return false;
        }
      }
      return previousNo >= 0;
    }
  }
}
