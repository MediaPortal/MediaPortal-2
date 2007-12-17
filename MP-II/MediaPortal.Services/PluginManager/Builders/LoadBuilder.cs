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
using MediaPortal.Core;
using MediaPortal.Core.PluginManager;
using MediaPortal.Services.PluginManager.PluginSpace;
using MediaPortal.Services.PluginManager.PluginDetails;

namespace MediaPortal.Services.PluginManager.Builders
{
  /// <summary>
  /// This doozer lazy-loads another doozer when it has to build an item.
  /// It is used internally to wrap doozers specified in addins.
  /// </summary>
  public class LoadBuilder : IPluginBuilder
  {
    #region Variables
    IPluginInfo _plugin;
    string _name;
    string _className;
    #endregion

    public LoadBuilder(IPluginInfo plugin, PluginProperties properties)
    {
      this._plugin = plugin;
      this._name = properties["name"];
      this._className = properties["class"];
    }

    public string Name
    {
      get { return _name; }
    }

    public string ClassName
    {
      get { return _className; }
    }

    ///// <summary>
    ///// Gets if the doozer handles codon conditions on its own.
    ///// If this property return false, the item is excluded when the condition is not met.
    ///// </summary>
    //public bool HandleConditions
    //{
    //  get
    //  {
    //    IDoozer doozer = (IDoozer)addIn.CreateObject(className);
    //    if (doozer == null)
    //    {
    //      return false;
    //    }
    //    AddInTree.Doozers[name] = doozer;
    //    return doozer.HandleConditions;
    //  }
    //}

    public object BuildItem(object caller, INodeItem item, ArrayList subItems)
    {
      IPluginBuilder builder = (IPluginBuilder)_plugin.CreateObject(_className);
      if (builder == null)
      {
        return null;
      }

      // replace LoadBuilder with instance of builder
      ServiceScope.Get<IPluginTree>().Builders[_name] =  builder;

      return builder.BuildItem(caller, item, subItems);
    }

    public override string ToString()
    {
      return String.Format("[LoadBuilder: className = {0}, name = {1}]",
                           _className,
                           _name);
    }

  }
}
