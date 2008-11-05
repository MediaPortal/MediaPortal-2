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

namespace MediaPortal.Core.PluginManager
{
  /// <summary>
  /// Holds the item data for a plugin item. Every object brought into the system by
  /// the plugin via a registration in the plugin tree is called an item. Items can be instances
  /// of classes, settings, resources, ...
  /// This item specification is very open; almost everything can be added by a plugin as an item.
  /// The only demand on items is, they have to be registered at a location in the plugin tree.
  /// The item metadata instance itself only holds item creation data; the real item object will be
  /// created by the item builder, which is referenced via its builder name
  /// (see <see cref="BuilderName"/>).
  /// </summary>
  public class PluginItemMetadata
  {
    #region Protected fields

    protected string _registrationLocation;
    protected string _builderName;
    protected string _id;
    protected IDictionary<string, string> _attributes;
    protected PluginRuntime _pluginRuntime;

    #endregion

    #region Ctor

    public PluginItemMetadata(string registrationLocation, string builderName,
        string id, IDictionary<string, string> attributes)
    {
      _registrationLocation = registrationLocation;
      _builderName = builderName;
      _id = id;
      _attributes = new Dictionary<string, string>(attributes);
    }

    #endregion

    /// <summary>
    /// Returns the location in the plugin tree where this item is registered.
    /// </summary>
    public string RegistrationLocation
    {
      get { return _registrationLocation; }
    }

    ///<summary>
    ///Returns the name of builder required to build this item.
    ///</summary>
    public string BuilderName
    {
      get { return _builderName; }
    }

    /// <summary>
    /// Returns the item's id. The id is the default identification property for an item.
    /// </summary>
    public string Id
    {
      get { return _id; }
    }

    /// <summary>
    /// Returns the items attributes.
    /// </summary>
    public IDictionary<string, string> Attributes
    {
      get { return _attributes; }
    }

    /// <summary>
    /// Gets the runtime class of the plugin which registered this item.
    /// </summary>
    public PluginRuntime PluginRuntime
    {
      get { return _pluginRuntime; }
      set { _pluginRuntime = value; }
    }

    #region Base overrides

    public override string ToString()
    {
      return string.Format("Plugin item: location '{0}', id '{1}'", _registrationLocation, _id);
    }

    #endregion
  }
}
