#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 *  Code modified from SharpDevelop AddIn code
 *  Thanks goes to: Mike Krüger
 */

#endregion

using System.Collections.Generic;
using System.Xml;
using MediaPortal.Core.PluginManager.PluginSpace;

namespace MediaPortal.Services.PluginManager.PluginDetails
{
  /// <summary>
  /// Description of Path.
  /// </summary>
  public class ExtensionPath
  {
    #region Variables
    string _name;
    PluginInfo _plugin;
    List<PluginRegisteredItem> _items = new List<PluginRegisteredItem>();
    #endregion

    #region Constructors/Destructors
    public ExtensionPath(string name, PluginInfo plugin)
    {
      this._plugin = plugin;
      this._name = name;
    }
    #endregion

    #region Properties
    public PluginInfo Plugin
    {
      get { return _plugin; }
    }

    public string Name
    {
      get { return _name; }
    }

    public List<PluginRegisteredItem> Items
    {
      get { return _items; }
    }
    #endregion

    #region Public static Methods
    public static void SetUp(ExtensionPath extensionPath, XmlReader reader, string endElement)
    {
      while (reader.Read())
      {
        switch (reader.NodeType)
        {
          case XmlNodeType.EndElement: 
            if (reader.LocalName == endElement)
            {
              return;
            }
            break;
          case XmlNodeType.Element:
            string elementName = reader.LocalName;

            PluginRegisteredItem newItem = new PluginRegisteredItem(elementName, extensionPath.Plugin, PluginProperties.ReadFromAttributes(reader)); //, conditionStack.ToArray());
              extensionPath._items.Add(newItem);
              if (!reader.IsEmptyElement)
              {
                ExtensionPath subPath = extensionPath.Plugin.GetExtensionPath(extensionPath.Name + "/" + newItem.Id);
                SetUp(subPath, reader, elementName);
              }
            break;
        }
      }
    }
    #endregion
  }
}
