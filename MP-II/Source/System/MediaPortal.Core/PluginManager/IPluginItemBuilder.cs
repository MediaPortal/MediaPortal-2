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


namespace MediaPortal.Core.PluginManager
{
  /// <summary>
  /// Interface for plugin item builder classes. An item builder allows a plugin to
  /// bring in almost all kinds of functionality to the system by providing new item types
  /// which can then be used by plugin descriptors. In the plugin descriptor, only some
  /// registration data for an item is needed, together with a reference to a plugin item builder,
  /// which then will integrate the item into the system (= build it).
  /// </summary>
  /// <remarks>
  /// Plugin item builder classes are used to instantiate plugin items out of a "parameter set"
  /// from the plugin descriptor file, provided by the data from <see cref="PluginItemMetadata"/>.
  /// For every plugin, the name of the builder will be specified which is needed to build the item
  /// with the specified parameters.
  /// </remarks>
  public interface IPluginItemBuilder
  {
    /// <summary>
    /// Will build an item from the specified item data.
    /// The plugin is in state <see cref="PluginState.Enabled"/> or <see cref="PluginState.Active"/>
    /// when this method is called, depending on the return value of <see cref="NeedsPluginActive"/>.
    /// </summary>
    /// <param name="itemData">The plugin item's metadata to use.</param>
    /// <param name="plugin">The plugin runtime instance.</param>
    /// <returns>Item which was built by this item builder. If the item cannot be built, <c>null</c> should be
    /// returned.</returns>
    object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin);

    /// <summary>
    /// Revokes the item from the specified item data.
    /// This will release the item.
    /// </summary>
    /// <param name="item">The object to revoke.</param>
    /// <param name="itemData">The plugin item's metadata.</param>
    /// <param name="plugin">The plugin runtime instance.</param>
    void RevokeItem(object item, PluginItemMetadata itemData, PluginRuntime plugin);

    /// <summary>
    /// Returns the information if this builder needs the item's plugin in an active state to build
    /// the item.
    /// </summary>
    /// <remarks>
    /// For example, if the builder will access the item's plugin's assemblies, it is necessary that
    /// the plugin is active before the <see cref="BuildItem"/> method can be called.
    /// </remarks>
    /// <param name="itemData">The plugin item's metadata to use.</param>
    /// <param name="plugin">The plugin runtime instance.</param>
    /// <returns><c>true</c>, if the plugin needs to be active for the specified item to be built, else <c>false</c>.</returns>
    bool NeedsPluginActive(PluginItemMetadata itemData, PluginRuntime plugin);
	}
}
