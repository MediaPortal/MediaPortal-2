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

using System;
using System.Collections;
using System.Collections.Generic;
using MediaPortal.Core.PluginManager.PluginSpace;

namespace MediaPortal.Core.PluginManager.PluginSpace
{
  /// <summary>
  /// Description of PluginTreeNode.
  /// </summary>
  internal class PluginTreeNode
  {
    #region Variables
    Dictionary<string, PluginTreeNode> _childNodes;
    List<PluginRegisteredItem> _items;
    bool _isSorted;
    #endregion

    #region Constructors/Destructors
    public PluginTreeNode()
    {
      _childNodes = new Dictionary<string, PluginTreeNode>();
      _items = new List<PluginRegisteredItem>();
      _isSorted = false;
    }
    #endregion

    #region Properties
    public Dictionary<string, PluginTreeNode> ChildNodes
    {
      get { return _childNodes; }
    }

    public List<PluginRegisteredItem> Items
    {
      get { return _items; }
    }
    #endregion

    #region Public Methods
    public List<T> BuildChildItems<T>()
    {
      List<T> items = new List<T>(_items.Count);
      if (!_isSorted)
      {
        _items = (new NodeItemSort(_items)).Execute();
        _isSorted = true;
      }
      foreach (PluginRegisteredItem item in _items)
      {
        ArrayList subItems = null;
        if (_childNodes.ContainsKey(item.Id))
        {
          subItems = _childNodes[item.Id].BuildChildItems();
        }
        object result = item.BuildItem();
        if (result == null)
          continue;
 
        if(result is T)
        {
          items.Add((T) result);
        }
        else
        {
          throw new InvalidCastException("The PluginTreeNode <" + item.BuilderName + " id='" + item.Id
                                         + "' ... /> returned an instance of " + result.GetType().FullName
                                         + " but the type " + typeof(T).FullName + " is expected.");
        }
      }
      return items;
    }

    public object BuildChildItem<T>(string id)
    {
      foreach (PluginRegisteredItem item in _items)
      {
        if (item.Id.ToLower() == id.ToLower())
        {
          object result = item.BuildItem();
          if (result == null)
            continue;

          if (result is T)
          {
            return result;
          }
          else
          {
            throw new InvalidCastException("The PluginTreeNode <" + item.BuilderName + " id='" + item.Id
                                           + "' returned an instance of " + result.GetType().FullName
                                           + " but the type " + typeof(T).FullName + " is expected.");
          }
        }
      }
      return null;
    }

    public ArrayList BuildChildItemsArrayList()
    {
      return BuildChildItems();
    }

    public ArrayList BuildChildItems()
    {
      ArrayList items = new ArrayList(_items.Count);
      if (!_isSorted)
      {
        _items = (new NodeItemSort(_items)).Execute();
        _isSorted = true;
      }
      foreach (PluginRegisteredItem item in _items)
      {
        ArrayList subItems = null;
        if (_childNodes.ContainsKey(item.Id))
        {
          subItems = _childNodes[item.Id].BuildChildItems();
        }
        object result = item.BuildItem();
        if (result == null)
          continue;

        items.Add(result);
      }
      return items;
    }

    public object BuildChildItem(string childItemID)
    {
      foreach (PluginRegisteredItem item in _items)
      {
        if (item.Id == childItemID)
        {
          return item.BuildItem();
        }
      }
      throw new TreePathNotFoundException(childItemID);
    }
    #endregion
  }
}