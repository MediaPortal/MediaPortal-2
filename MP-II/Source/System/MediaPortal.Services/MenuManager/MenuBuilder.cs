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

using System.IO;
using System.Xml;
using MediaPortal.Core;
using MediaPortal.Presentation.MenuManager;
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.Screen;

namespace MediaPortal.Services.MenuManager
{
  public class MenuBuilder : IMenuBuilder
  {
    #region IMenuBuilder Members

    /// <summary>
    /// Builds the specified menu .
    /// </summary>
    /// <param name="menuName">Name of the menu.</param>
    /// <returns></returns>
    public IMenu Build(string menuName)
    {
      Menu menu = new Menu(menuName);
      //load menu configurarion
      //for now we simply load it from an .xml file since 
      //i dont know yet how the pluginmanager could do this.

      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      string filePath = screenManager.SkinResourceContext.GetResourceFilePath(string.Format("menus\\{0}.xml", menuName));

      XmlDocument doc = new XmlDocument();
      doc.Load(filePath);
      XmlNodeList nodesItems = doc.SelectNodes("/menu/items/item");
      foreach (XmlNode nodeItem in nodesItems)
      {
        menu.Items.Add(GetMenuItem(nodeItem));
      }
      return menu;
    }

    IMenuItem GetMenuItem(XmlNode node)
    {
      string label = node.Attributes["text"].Value;
      string image = node.Attributes["image"].Value;
      string command = node.Attributes["command"].Value;
      string parameter = node.Attributes["parameter"].Value;
      StringId localized = new StringId(label);
      MenuItem item = new MenuItem(localized, image, command, parameter);

      XmlNodeList nodesItems = node.SelectNodes("items/item");
      foreach (XmlNode nodeItem in nodesItems)
      {
        item.Items.Add(GetMenuItem(nodeItem));
      }
      return item;
    }

    #endregion
  }
}
