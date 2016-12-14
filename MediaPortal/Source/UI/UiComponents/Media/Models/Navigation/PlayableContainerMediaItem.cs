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
using MediaPortal.Common.UserProfileDataManagement;

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

      int? currentPlayCount = null;
      SingleMediaItemAspect mediaAspect;
      if (MediaItemAspect.TryGetAspect(mediaItem.Aspects, MediaAspect.Metadata, out mediaAspect))
      {
        currentPlayCount = (int?)mediaAspect[MediaAspect.ATTR_PLAYCOUNT] ?? 0;
      }

      if (mediaItem.UserData.ContainsKey(UserDataKeysKnown.KEY_PLAY_PERCENTAGE))
      {
        WatchPercentage = mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_PERCENTAGE];
      }

      if (mediaItem.UserData.ContainsKey(UserDataKeysKnown.KEY_PLAY_COUNT))
      {
        PlayCount = Convert.ToInt32(mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_COUNT]);
      }
      else if (currentPlayCount.HasValue)
      {
        PlayCount = currentPlayCount.Value;
      }
    }

    public int PlayCount
    {
      get { return (int?)AdditionalProperties[Consts.KEY_PLAYCOUNT] ?? 0; }
      set { AdditionalProperties[Consts.KEY_PLAYCOUNT] = value; }
    }

    public string WatchPercentage
    {
      get { return this[Consts.KEY_WATCH_PERCENTAGE]; }
      set { SetLabel(Consts.KEY_WATCH_PERCENTAGE, value); }
    }
  }
}
