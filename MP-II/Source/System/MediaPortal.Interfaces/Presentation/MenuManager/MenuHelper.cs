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

using MediaPortal.Core;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.MenuManager;
using MediaPortal.Presentation.Commands;

namespace MediaPortal.Presentation.MenuManager
{
  /// <summary>
  /// Provides helper methods for the management of menus.
  /// </summary>
  public class MenuHelper
  {
    /// <summary>
    /// Wraps the given <paramref name="menu"/> into an <see cref="ItemsList"/> to be displayed
    /// in the UI.
    /// </summary>
    /// <param name="menu">The menu to wrap.</param>
    /// <returns>Wrapped menu.</returns>
    public static ItemsList WrapMenu(IMenu menu)
    {
      ItemsList result = new ItemsList();
      ICommandBuilder builder = ServiceScope.Get<ICommandBuilder>();
      foreach (IMenuItem item in menu.Items)
      {
        TreeItem treeItem = new TreeItem("Name", item.Text);
        treeItem.SetLabel("CoverArt", item.ImagePath);
        if (item.Command != "")
        {
          treeItem.Command = builder.BuildCommand(item.Command);
          if (item.CommandParameter != "")
            treeItem.CommandParameter = builder.BuildParameter(item.CommandParameter);
        }
        treeItem.SubItems.Add(treeItem);
        if (item.Items != null)
        {
          foreach (IMenuItem subitem in item.Items)
          {
            ListItem sublistItem = new ListItem("Name", subitem.Text);
            sublistItem.SetLabel("CoverArt", subitem.ImagePath);
            if (subitem.Command != "")
            {
              sublistItem.Command = builder.BuildCommand(subitem.Command);
              if (subitem.CommandParameter != "")
                sublistItem.CommandParameter = builder.BuildParameter(subitem.CommandParameter);
            }

            treeItem.SubItems.Add(sublistItem);
          }
        }
        result.Add(treeItem);
      }
      return result;
    }
  }
}
