#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using MediaPortal.UI.Views;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  /// <summary>
  /// Holds a GUI item which represents a view to navigate to.
  /// </summary>
  /// <remarks>
  /// Instances of this class represent view items to be displayed in a GUI view's items list.
  /// View's items lists contain view items (<see cref="NavigationItem"/>s) as well as
  /// playable items (<see cref="PlayableItem"/>).
  /// </remarks>
  public class NavigationItem : ListItem
  {
    public NavigationItem(View view, string overrideName, int? absNumItems)
    {
      ItemType = "Navigation";
      UpdateData(view, overrideName, absNumItems);
    }

    public void UpdateData(View view, string overrideName, int? absNumItems)
    {
      string name = string.IsNullOrEmpty(overrideName) ? view.DisplayName : overrideName;
      SetLabel(Consts.KEY_NAME, name);
      if (absNumItems.HasValue)
        SetLabel(Consts.KEY_NUM_ITEMS, "(" + absNumItems.Value + ")");

      // TODO: Other properties
    }

    public string ItemType
    {
      get { return this["ItemType"]; }
      set { SetLabel("ItemType", value); }
    }
  }
}