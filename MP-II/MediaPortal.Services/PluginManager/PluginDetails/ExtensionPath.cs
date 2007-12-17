#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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

﻿#region Copyright (C) 2005-2007 Team MediaPortal

/* 
 *	Copyright (C) 2005-2007 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 *  Code modified from SharpDevelop AddIn code
 *  Thanks goes to: Mike Krüger
 */

#endregion

using System;
using System.Collections.Generic;
using System.Xml;
using MediaPortal.Services.PluginManager.PluginSpace;

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
    List<NodeItem> _items = new List<NodeItem>();
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
      get
      {
        return _plugin;
      }
    }

    public string Name
    {
      get
      {
        return _name;
      }
    }
    public List<NodeItem> Items
    {
      get
      {
        return _items;
      }
    }
    #endregion

    #region Public static Methods
    public static void SetUp(ExtensionPath extensionPath, XmlReader reader, string endElement)
    {
      //Stack<ICondition> conditionStack = new Stack<ICondition>();
      while (reader.Read())
      {
        switch (reader.NodeType)
        {
          case XmlNodeType.EndElement:
            //if (reader.LocalName == "Condition" || reader.LocalName == "ComplexCondition") {
            //  conditionStack.Pop();
            //} else 
            if (reader.LocalName == endElement)
            {
              return;
            }
            break;
          case XmlNodeType.Element:
            string elementName = reader.LocalName;
            //if (elementName == "Condition")
            //{
            //  conditionStack.Push(Condition.Read(reader));
            //}
            //else if (elementName == "ComplexCondition")
            //{
            //  conditionStack.Push(Condition.ReadComplexCondition(reader));
            //}
            //else
            //{
            NodeItem newItem = new NodeItem(extensionPath.Plugin, elementName, PluginProperties.ReadFromAttributes(reader)); //, conditionStack.ToArray());
              extensionPath._items.Add(newItem);
              if (!reader.IsEmptyElement)
              {
                ExtensionPath subPath = extensionPath.Plugin.GetExtensionPath(extensionPath.Name + "/" + newItem.Id);
                //foreach (ICondition condition in extensionPath.conditionStack) {
                //	subPath.conditionStack.Push(condition);
                //}
                SetUp(subPath, reader, elementName);
                //foreach (ICondition condition in extensionPath.conditionStack) {
                //	subPath.conditionStack.Pop();
                //}
              }
            //}
            break;
        }
      }
    }
    #endregion
  }
}
