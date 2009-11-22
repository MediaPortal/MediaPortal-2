#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
  /// Plugin item registration class. Instances of this class are registered in the registry at the items
  /// registration location. This is a pure data object; its state is managed by the <see cref="PluginRuntime"/> class.
  /// </summary>
  public class PluginItemRegistration
  {
    #region Protected fields

    protected PluginItemMetadata _pluginItemMetadata;

    protected object _itemInstance = null;
    protected ICollection<IPluginItemStateTracker> _stateTrackers = new List<IPluginItemStateTracker>();
    protected ICollection<PluginItemMetadata> _additionalRedundantItemsMetadata =
        new List<PluginItemMetadata>();

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new plugin item registration data structure for the specified plugin item metadata
    /// instance.
    /// </summary>
    /// <param name="metaData">The metadata of the plugin item to create this registration
    /// structure for.</param>
    internal PluginItemRegistration(PluginItemMetadata metaData)
    {
      _pluginItemMetadata = metaData;
    }

    #endregion

    /// <summary>
    /// Returns the plugin item's metadata.
    /// </summary>
    public PluginItemMetadata Metadata
    {
      get { return _pluginItemMetadata; }
    }

    /// <summary>
    /// Gets the information if this plugin item was already built.
    /// </summary>
    public bool IsBuilt
    {
      get { return _itemInstance != null; }
    }

    /// <summary>
    /// Gets the item's instance, if it was already built. Else, the value will be <c>null</c>.
    /// </summary>
    public object Item
    {
      get { return _itemInstance; }
      internal set { _itemInstance = value; }
    }

    /// <summary>
    /// Gets the collection of the plugin item's state trackers.
    /// </summary>
    public ICollection<IPluginItemStateTracker> StateTrackers
    {
      get { return _stateTrackers; }
    }

    /// <summary>
    /// Returns a collection containing all redundantly registered plugin item metadata.
    /// </summary>
    /// <remarks>
    /// Redundantly registered items will be registered in place of this item's registration, if the plugin
    /// of this item will be removed.
    /// </remarks>
    public ICollection<PluginItemMetadata> AdditionalRedundantItemsMetadata
    {
      get { return _additionalRedundantItemsMetadata; }
    }
  }
}
