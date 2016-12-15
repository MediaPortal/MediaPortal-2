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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.General;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.Localization;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Players.ResumeState;
using MediaPortal.UI.Services.Players;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  /// <summary>
  /// Holds a GUI item which encapsulates a playable media item.
  /// </summary>
  /// <remarks>
  /// Instances of this class represent playable items to be displayed in a GUI view's items list.
  /// View's items lists contain view items (<see cref="ViewItem"/>s) as well as
  /// playable items (<see cref="PlayableMediaItem"/>).
  /// </remarks>
  public abstract class PlayableMediaItem : NavigationItem
  {
    #region Protected fields

    protected MediaItem _mediaItem;

    #endregion

    protected PlayableMediaItem(MediaItem mediaItem)
    {
      _mediaItem = mediaItem;
      Update(mediaItem);
    }

    public virtual void Update(MediaItem mediaItem)
    {
      if (!_mediaItem.Equals(mediaItem))
        throw new ArgumentException("Update can only be done for the same MediaItem!", "mediaItem");

      int? currentPlayCount = null;
      SingleMediaItemAspect mediaAspect;
      if (MediaItemAspect.TryGetAspect(mediaItem.Aspects, MediaAspect.Metadata, out mediaAspect))
      {
        Title = (string)mediaAspect[MediaAspect.ATTR_TITLE];
        SortString = (string)mediaAspect[MediaAspect.ATTR_SORT_TITLE];
        Rating = (int?)mediaAspect[MediaAspect.ATTR_RATING] ?? 0;
        currentPlayCount = (int?)mediaAspect[MediaAspect.ATTR_PLAYCOUNT] ?? 0;
        Virtual = (bool?)mediaAspect[MediaAspect.ATTR_ISVIRTUAL];
      }

      TimeSpan? duration = null;
      IList<MediaItemAspect> aspects;
      if (mediaItem.Aspects.TryGetValue(VideoStreamAspect.ASPECT_ID, out aspects))
      {
        var aspect = aspects.First();
        int? part = (int?)aspect[VideoStreamAspect.ATTR_VIDEO_PART];
        int? partSet = (int?)aspect[VideoStreamAspect.ATTR_VIDEO_PART_SET];
        long? dur = null;
        if (!part.HasValue || part < 0)
        {
          dur = (long?)aspect[VideoStreamAspect.ATTR_DURATION];
        }
        else if (partSet.HasValue)
        {
          dur = aspects.Where(a => (int?)a[VideoStreamAspect.ATTR_VIDEO_PART_SET] == partSet &&
          aspect[VideoStreamAspect.ATTR_DURATION] != null).Sum(a => (long)a[VideoStreamAspect.ATTR_DURATION]);
        }
        if (dur.HasValue)
          duration = TimeSpan.FromSeconds(dur.Value);
      }
      else if (mediaItem.Aspects.TryGetValue(AudioAspect.ASPECT_ID, out aspects))
      {
        var aspect = aspects.First();
        long? dur = aspect == null ? null : (long?)aspect[AudioAspect.ATTR_DURATION];
        if (dur.HasValue)
          duration = TimeSpan.FromSeconds(dur.Value);
      }
      else if (mediaItem.Aspects.TryGetValue(MovieAspect.ASPECT_ID, out aspects))
      {
        var aspect = aspects.First();
        int? dur = aspect == null ? null : (int?)aspect[MovieAspect.ATTR_RUNTIME_M];
        if (dur.HasValue)
          duration = TimeSpan.FromMinutes(dur.Value);
      }
      Duration = duration.HasValue ? FormattingUtils.FormatMediaDuration(duration.Value) : string.Empty;

      if (mediaItem.UserData.ContainsKey(UserDataKeysKnown.KEY_PLAY_PERCENTAGE))
      {
        WatchPercentage = mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_PERCENTAGE];
      }
      else if (mediaItem.UserData.ContainsKey(PlayerContext.KEY_RESUME_STATE))
      {
        IResumeState resumeState = ResumeStateBase.Deserialize(mediaItem.UserData[PlayerContext.KEY_RESUME_STATE]);
        PositionResumeState positionResume = resumeState as PositionResumeState;
        if (positionResume != null && duration.HasValue)
        {
          TimeSpan resumePosition = positionResume.ResumePosition;
          if (duration.Value.TotalSeconds > 0)
            WatchPercentage = ((int)(resumePosition.TotalSeconds * 100 / duration.Value.TotalSeconds)).ToString();
          else if (currentPlayCount > 0)
            WatchPercentage = "100";
          else
            WatchPercentage = "0";
        }
      }

      if (mediaItem.UserData.ContainsKey(UserDataKeysKnown.KEY_PLAY_COUNT))
      {
        PlayCount = Convert.ToInt32(mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_COUNT]);
      }
      else if(currentPlayCount.HasValue)
      {
        PlayCount = currentPlayCount.Value;
      }

      FireChange();
    }

    public static PlayableMediaItem CreateItem(MediaItem mediaItem)
    {
      if (mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
        return new AudioItem(mediaItem);
      if (mediaItem.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
        return new VideoItem(mediaItem);
      if (mediaItem.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
        return new ImageItem(mediaItem);
      throw new NotImplementedException("The given media item is of an unknown type");
    }

    public MediaItem MediaItem
    {
      get { return _mediaItem; }
    }

    public string Title
    {
      get { return this[Consts.KEY_TITLE]; }
      set { SetLabel(Consts.KEY_TITLE, value);}
    }

    public int Rating
    {
      get { return (int?) AdditionalProperties[Consts.KEY_RATING] ?? 0; }
      set { AdditionalProperties[Consts.KEY_RATING] = value; }
    }

    public int PlayCount
    {
      get { return (int?) AdditionalProperties[Consts.KEY_PLAYCOUNT] ?? 0; }
      set { AdditionalProperties[Consts.KEY_PLAYCOUNT] = value; }
    }

    public string WatchPercentage
    {
      get { return this[Consts.KEY_WATCH_PERCENTAGE]; }
      set { SetLabel(Consts.KEY_WATCH_PERCENTAGE, value); }
    }

    public string Duration
    {
      get { return this[Consts.KEY_DURATION]; }
      set { SetLabel(Consts.KEY_DURATION, value); }
    }

    public bool? Virtual
    {
      get { return (bool?)AdditionalProperties[Consts.KEY_VIRTUAL]; }
      set { AdditionalProperties[Consts.KEY_VIRTUAL] = value; }
    }
  }
}
