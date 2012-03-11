#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
      MediaItemAspect mediaAspect;
      if (mediaItem.Aspects.TryGetValue(MediaAspect.ASPECT_ID, out mediaAspect))
      {
        Title = (string) mediaAspect[MediaAspect.ATTR_TITLE];
        Rating = (int?) mediaAspect[MediaAspect.ATTR_RATING] ?? 0;
      }
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
  }
}
