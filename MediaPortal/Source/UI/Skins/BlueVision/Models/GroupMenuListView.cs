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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Xaml;
using SharpDX;

namespace MediaPortal.UiComponents.BlueVision.Models
{
  /// <summary>
  /// Specialized ListView that checks the last selected item from HomeMenuModel and defaults to this when selection comes back by keyboard
  /// </summary>
  public class GroupMenuListView : ListView
  {
    /// <summary>
    /// Gets or sets the <see cref="HomeMenuModel"/>
    /// </summary>
    public HomeMenuModel HomeMenuModel { get; set; }

    public override void AddPotentialFocusableElements(RectangleF? startingRect, ICollection<FrameworkElement> elements)
    {
      // if the focus comes from outside the ListView, then we add only the lastSelectedItem to the PotentialFocusableElements
      // if the focus is inside the ListViewAlreay, then act as normal
      if (HomeMenuModel != null)
      {
        lock (_itemsHostPanel.Children.SyncRoot)
        {
          // find ListViewItem of LastSelectedItem
          foreach (FrameworkElement child in _itemsHostPanel.Children)
          {
            var item = child as ListViewItem;
            if (item != null)
            {
              // get the real ListItem from the DataContext and check if this is the currently focused item
              IDataDescriptor listItem;
              var lastSelectedItem =  HomeMenuModel.MainMenuGroupList.OfType<GroupMenuListItem>().FirstOrDefault(i => i.IsActive);
              if (item.DataContext.Evaluate(out listItem) && ReferenceEquals(listItem.Value, lastSelectedItem))
              {
                // if LastSelectedItem is not currently focused, then the focus comes from the outside, and it's the focus candidate then
                if (!ReferenceEquals(item, Screen.FocusedElement))
                {
                  elements.Add(item);
                  return;
                }
                break;
              }
            }
          }
        }
      }
      base.AddPotentialFocusableElements(startingRect, elements);
    }
  }
}
