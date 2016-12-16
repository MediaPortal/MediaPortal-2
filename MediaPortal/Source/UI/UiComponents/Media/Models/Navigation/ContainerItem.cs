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

using MediaPortal.Common.MediaManagement;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  /// <summary>
  /// Base class for all navigation items which represent containers for more items.
  /// </summary>
  public class ContainerItem : NavigationItem
  {
    public ContainerItem()
    { }

    public ContainerItem(int? absNumItems)
    {
      NumItems = absNumItems;
    }

    public string Id
    {
      get { return (string)AdditionalProperties[Consts.KEY_ID]; }
      set { AdditionalProperties[Consts.KEY_ID] = value; }
    }

    public int? NumItems
    {
      get { return (int?) AdditionalProperties[Consts.KEY_NUM_ITEMS]; }
      set { AdditionalProperties[Consts.KEY_NUM_ITEMS] = value; }
    }

    public MediaItem FirstMediaItem
    {
      get { return (MediaItem)AdditionalProperties[Consts.KEY_MEDIA_ITEM]; }
      set { AdditionalProperties[Consts.KEY_MEDIA_ITEM] = value; }
    }
  }
}
