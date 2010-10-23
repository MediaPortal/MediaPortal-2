#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core.Localization;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  /// <summary>
  /// Holds a GUI item which encapsulates a playable media item.
  /// </summary>
  /// <remarks>
  /// Instances of this class represent playable items to be displayed in a GUI view's items list.
  /// View's items lists contain view items (<see cref="NavigationItem"/>s) as well as
  /// playable items (<see cref="PlayableItem"/>).
  /// </remarks>
  public class PlayableItem : ListItem
  {
    #region Protected fields

    protected MediaItem _mediaItem;

    #endregion

    public PlayableItem(MediaItem mediaItem)
    {
      _mediaItem = mediaItem;
      MediaItemAspect mediaAspect;
      MediaItemAspect audioAspect;
      MediaItemAspect videoAspect;
      if (!mediaItem.Aspects.TryGetValue(MediaAspect.ASPECT_ID, out mediaAspect))
        mediaAspect = null;
      if (!mediaItem.Aspects.TryGetValue(AudioAspect.ASPECT_ID, out audioAspect))
        audioAspect = null;
      if (!mediaItem.Aspects.TryGetValue(VideoAspect.ASPECT_ID, out videoAspect))
        videoAspect = null;
      string title = mediaAspect == null ? null : mediaAspect[MediaAspect.ATTR_TITLE] as string;

      IEnumerable<string> artistsEnumer = audioAspect == null ? null : (IEnumerable<string>) audioAspect[AudioAspect.ATTR_ARTISTS];
      string artists = artistsEnumer == null ? null : StringUtils.Join(", ", artistsEnumer);
      string name = title + (string.IsNullOrEmpty(artists) ? string.Empty : (" (" + artists + ")"));
      long? duration = audioAspect == null ? null : (long?) audioAspect[AudioAspect.ATTR_DURATION];
      if (!duration.HasValue)
        duration = videoAspect == null ? null : (long?) videoAspect[VideoAspect.ATTR_DURATION];
      SetLabel(Consts.KEY_NAME, name);
      SetLabel(Consts.KEY_DURATION, duration.HasValue ? FormattingUtils.FormatMediaDuration(TimeSpan.FromSeconds((int) duration.Value)) : string.Empty);
      // TODO: Open ListItem to store ints (rating), dates (Date) and other objects in ListItems
    }

    public MediaItem MediaItem
    {
      get { return _mediaItem; }
    }

    public string Name
    {
      get { return this[Consts.KEY_NAME]; }
      set { SetLabel(Consts.KEY_NAME, value); }
    }

    public string Length
    {
      get { return this[Consts.KEY_DURATION]; }
      set { SetLabel(Consts.KEY_DURATION, value); }
    }
  }
}
