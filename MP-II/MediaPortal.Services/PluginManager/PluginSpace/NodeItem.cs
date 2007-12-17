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
using System.Collections;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.PluginManager;
using MediaPortal.Services.PluginManager.PluginDetails;

namespace MediaPortal.Services.PluginManager.PluginSpace
{
  /// <summary>
  /// Represents a node in the add in tree that can produce an item.
  /// </summary>
  public class NodeItem : INodeItem
  {
    #region Variables
    PluginInfo _plugin;
    string _name;
    PluginProperties _properties;
    //ICondition[] conditions;
    #endregion

    #region Constructors/Destructors
    public NodeItem(PluginInfo plugin, string name, PluginProperties properties) //, ICondition[] conditions)
    {
      this._plugin = plugin;
      this._name = name;
      this._properties = properties;
      //this.conditions = conditions;
    }
    #endregion

    #region Properties
    public string Name
    {
      get { return _name; }
    }

    public IPluginInfo Plugin
    {
      get { return _plugin; }
    }

    public string Id
    {
      get { return _properties["id"]; }
    }

    public string InsertAfter
    {
      get
      {
        if (!_properties.Contains("insertafter"))
        {
          return "";
        }
        return _properties["insertafter"];
      }
      set
      {
        _properties["insertafter"] = value;
      }
    }

    public string InsertBefore
    {
      get
      {
        if (!_properties.Contains("insertbefore"))
        {
          return "";
        }
        return _properties["insertbefore"];
      }
      set
      {
        _properties["insertbefore"] = value;
      }
    }

    public string this[string key]
    {
      get { return _properties[key]; }
    }

    public PluginProperties Properties
    {
      get { return _properties; }
    }

    //public ICondition[] Conditions {
    //  get {
    //    return conditions;
    //  }
    //}
    #endregion

    #region Public Methods
    public bool Contains(string key)
    {
      return _properties.Contains(key);
    }

    //public ConditionFailedAction GetFailedAction(object caller)
    //{
    //  return Condition.GetFailedAction(conditions, caller);
    //}

    public object CreateObject(string className)
    {
      return _plugin.CreateObject(className);
    }

    public object BuildItem(object owner, ArrayList subItems)
    {
      IPluginBuilder builder;
      if (!ServiceScope.Get<IPluginTree>().Builders.TryGetValue(_name, out builder))
        throw new PluginException("Builder " + _name + " not found!");

      //if (!doozer.HandleConditions && conditions.Length > 0) {
      //  ConditionFailedAction action = GetFailedAction(owner);
      //  if (action != ConditionFailedAction.Nothing) {
      //    return null;
      //  }
      //}

      return builder.BuildItem(owner, (INodeItem) this, subItems);
    }
    #endregion

    #region <Base class> Overloads
    public override string ToString()
    {
      return String.Format("[NodeItem: name = {0}, addIn={1}]",
                           _name,
                           _plugin.FileName);
    }
    #endregion
  }
}
