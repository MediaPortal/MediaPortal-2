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
using MediaPortal.Core;
using MediaPortal.Interfaces.Core.PluginManager;
using MediaPortal.Services.PluginManager.PluginDetails;

namespace MediaPortal.Services.PluginManager.PluginSpace
{
  /// <summary>
  /// Represents a node in the add in tree that can produce an item.
  /// </summary>
  public class PluginRegisteredItem : IPluginRegisteredItem
  {
    #region Variables
    PluginInfo _plugin;
    string _builderName;
    PluginProperties _properties;
    #endregion

    #region Constructors/Destructors
    internal PluginRegisteredItem(string builderName, PluginInfo plugin, PluginProperties properties)
    {
      this._plugin = plugin;
      this._builderName = builderName;
      this._properties = properties;
    }
    #endregion

    #region Properties
    internal string InsertAfter
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

    internal string InsertBefore
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

    internal PluginProperties Properties
    {
      get { return _properties; }
    }
    #endregion

    #region Public Methods

    /// <summary>
    /// Builds the item using the correct factory.
    /// </summary>
    /// <returns>an instance of the item</returns>
    internal object BuildItem()
    {
      IPluginItemBuilder builder;
      if (!ServiceScope.Get<IPluginTree>().Builders.TryGetValue(_builderName, out builder))
        throw new PluginException("Builder " + _builderName + " not found!");

      return builder.BuildItem((IPluginRegisteredItem)this);
    }
    #endregion

    #region IPluginRegisteredItem Members
    #region Properties
    public string BuilderName
    {
      get { return _builderName; }
    }

    public IPluginInfo Plugin
    {
      get { return _plugin; }
    }

    public string Id
    {
      get { return _properties["id"]; }
    }

    public string this[string key]
    {
      get { return _properties[key]; }
    }
    #endregion

    #region Public Methods
    public bool Contains(string key)
    {
      return _properties.Contains(key);
    }
    #endregion
    #endregion

    #region <Base class> Overloads
    public override string ToString()
    {
      return String.Format("[PluginRegisteredItem: builderName = {0}]", _builderName);
    }
    #endregion
  }
}
