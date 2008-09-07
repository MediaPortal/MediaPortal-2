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

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Presentation.MenuManager;
using MediaPortal.Presentation.Commands;

namespace MediaPortal.Presentation.DataObjects
{
  /// <summary>
  /// interface to a collection of items
  /// </summary>
  public class ItemsCollection : List<ListItem>
  {
    public ItemsCollection()
    {
    }

    public ItemsCollection(IMenu menu)
    {
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
        Add(listItem);
      }
    }

    public delegate void ItemsChangedHandler();

    /// <summary>
    /// Event which gets fired when the collection changes.
    /// </summary>
    public event ItemsChangedHandler Changed;

    public void FireChange()
    {
      if (Changed != null)
        Changed();
    }
  }
}
