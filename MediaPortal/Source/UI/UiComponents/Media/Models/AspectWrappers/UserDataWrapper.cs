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
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.Services.Players;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Players.ResumeState;
using System.Linq;
using MediaPortal.Common.UserProfileDataManagement;

namespace MediaPortal.UiComponents.Media.Models.AspectWrappers
{
  /// <summary>
  /// UserDataWrapper wraps user data into properties that can be bound from xaml controls.
  /// </summary>
  public class UserDataWrapper : Control
  {
    #region Fields

    protected AbstractProperty _playPercentageProperty;
    protected AbstractProperty _playCountProperty;
    protected AbstractProperty _mediaItemProperty;

    #endregion

    #region Properties

    public AbstractProperty PlayPercentageProperty
    {
      get { return _playPercentageProperty; }
    }

    public int? PlayPercentage
    {
      get { return (int?)_playPercentageProperty.GetValue(); }
      set { _playPercentageProperty.SetValue(value); }
    }

    public AbstractProperty PlayCountProperty
    {
      get { return _playCountProperty; }
    }

    public int? PlayCount
    {
      get { return (int?)_playCountProperty.GetValue(); }
      set { _playCountProperty.SetValue(value); }
    }

    public AbstractProperty MediaItemProperty
    {
      get { return _mediaItemProperty; }
    }

    public MediaItem MediaItem
    {
      get { return (MediaItem)_mediaItemProperty.GetValue(); }
      set { _mediaItemProperty.SetValue(value); }
    }

    #endregion

    #region Constructor

    public UserDataWrapper()
    {
      _playPercentageProperty = new SProperty(typeof(int?));
      _playCountProperty = new SProperty(typeof(int?));
      _mediaItemProperty = new SProperty(typeof(MediaItem));
      _mediaItemProperty.Attach(MediaItemChanged);
    }

    #endregion

    #region Members

    private void MediaItemChanged(AbstractProperty property, object oldvalue)
    {
      Init(MediaItem);
    }

    public void Init(MediaItem mediaItem)
    {
      if (mediaItem == null)
      {
        SetEmpty();
        return;
      }

      int currentPlayCount;
      MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MediaAspect.ATTR_PLAYCOUNT, 0, out currentPlayCount);

      if (mediaItem.UserData.ContainsKey(UserDataKeysKnown.KEY_PLAY_PERCENTAGE))
      {
        PlayPercentage = Convert.ToInt32(mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_PERCENTAGE]);
      }
      else if (mediaItem.UserData.ContainsKey(PlayerContext.KEY_RESUME_STATE))
      {
        IResumeState resumeState = ResumeStateBase.Deserialize(mediaItem.UserData[PlayerContext.KEY_RESUME_STATE]);
        PositionResumeState positionResume = resumeState as PositionResumeState;
        if (positionResume != null)
        {
          TimeSpan resumePosition = positionResume.ResumePosition;
          TimeSpan duration = TimeSpan.FromSeconds(0);
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
          if (duration.TotalSeconds > 0)
            PlayPercentage = (int)(resumePosition.TotalSeconds * 100 / duration.TotalSeconds);
          else if (currentPlayCount > 0)
            PlayPercentage = 100;
          else
            PlayPercentage = 0;
        }
      }
      else
      {
        PlayPercentage = 0;
      }
      if (mediaItem.UserData.ContainsKey(UserDataKeysKnown.KEY_PLAY_COUNT))
      {
        PlayCount = Convert.ToInt32(mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_COUNT]);
      }
      else
      {
        PlayCount = currentPlayCount;
      }
    }

    public void SetEmpty()
    {
      PlayCount = null;
      PlayPercentage = null;
    }


    #endregion

  }

}
