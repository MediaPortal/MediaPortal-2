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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UiComponents.Media.General;
using System;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  /// <summary>
  /// Holds a GUI item which encapsulates the basics of a playable media item.
  /// </summary>
  public abstract class PlayableContainerMediaItem : FilterItem
  {
    public override void Update(MediaItem mediaItem)
    {
      base.Update(mediaItem);
      if (mediaItem == null)
        return;

      int? playPct = null;
      int? playCnt = null;
      if (mediaItem.UserData.ContainsKey(UserDataKeysKnown.KEY_PLAY_PERCENTAGE))
      {
        playPct = Convert.ToInt32(mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_PERCENTAGE]);
      }

      if (mediaItem.UserData.ContainsKey(UserDataKeysKnown.KEY_PLAY_COUNT))
      {
        playCnt = Convert.ToInt32(mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_COUNT]);
        if (!playPct.HasValue && playCnt > 0)
          playPct = 100;
      }

      WatchPercentage = playPct ?? 0;
      PlayCount = playCnt ?? 0;
    }

    public int PlayCount
    {
      get { return (int?)AdditionalProperties[Consts.KEY_PLAYCOUNT] ?? 0; }
      set { AdditionalProperties[Consts.KEY_PLAYCOUNT] = value; }
    }

    public int WatchPercentage
    {
      get { return (int?)AdditionalProperties[Consts.KEY_WATCH_PERCENTAGE] ?? 0; }
      set { AdditionalProperties[Consts.KEY_WATCH_PERCENTAGE] = value; }
    }
  }
}
