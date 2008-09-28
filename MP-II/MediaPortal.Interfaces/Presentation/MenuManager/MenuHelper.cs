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
    /// Wraps the given <paramref name="menu"/> into an <see cref="ItemsCollection"/> to be displayed
    /// in the UI.
    /// </summary>
    /// <param name="menu">The menu to wrap.</param>
    /// <returns>Wrapped menu.</returns>
    public static ItemsCollection WrapMenu(IMenu menu)
    {
      ItemsCollection result = new ItemsCollection();
      ICommandBuilder builder = ServiceScope.Get<ICommandBuilder>();
      foreach (IMenuItem item in menu.Items)
      {
        ListItem listItem = new ListItem("Name", item.Text);
        listItem.Add("CoverArt", item.ImagePath);
        if (item.Command != "")
        {
          listItem.Command = builder.BuildCommand(item.Command);
          if (item.CommandParameter != "")
            listItem.CommandParameter = builder.BuildParameter(item.CommandParameter);
        }
        listItem.SubItems.Add(listItem);
        if (item.Items != null)
        {
          foreach (IMenuItem subitem in item.Items)
          {
            ListItem sublistItem = new ListItem("Name", subitem.Text);
            sublistItem.Add("CoverArt", subitem.ImagePath);
            if (subitem.Command != "")
            {
              sublistItem.Command = builder.BuildCommand(subitem.Command);
              if (subitem.CommandParameter != "")
                sublistItem.CommandParameter = builder.BuildParameter(subitem.CommandParameter);
            }

            listItem.SubItems.Add(sublistItem);
          }
        }
        result.Add(listItem);
      }
      return result;
    }
  }
}
