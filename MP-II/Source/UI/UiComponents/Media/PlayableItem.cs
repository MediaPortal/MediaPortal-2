#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.Presentation.DataObjects;

namespace UiComponents.Media
{
  /// <summary>
  /// Holds a GUI item which encapsulates a playable media item.
  /// </summary>
  /// <remarks>
  /// Instances of this class represent playable items to be displayed in a GUI view's items list.
  /// View's items lists contain view items (<see cref="NavigationItem"/>s) as well as
  /// Playable items (<see cref="PlayableItem"/>).
  /// </remarks>
  public class PlayableItem : ListItem
  {
    #region Protected fields

    protected MediaItem _mediaItem;

    #endregion

    public PlayableItem(MediaItem mediaItem)
    {
      _mediaItem = mediaItem;
      UpdateData();
    }

    public void UpdateData()
    {
      SetLabel("Name", _mediaItem[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE] as string);
      // TODO: Other properties
      // TODO: Open ListItem to store ints (rating), dates (Date) and other objects in ListItems
    }

    public MediaItem MediaItem
    {
      get { return _mediaItem; }
    }
  }
}
