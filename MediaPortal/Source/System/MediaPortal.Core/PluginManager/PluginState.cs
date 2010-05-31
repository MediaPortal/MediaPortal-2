#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

namespace MediaPortal.Core.PluginManager
{
  /// <summary>
  /// Enumeration of states which a plugin can assume during its lifetime.
  /// Resources of this plugin may only be accessed when this plugin is <see cref="Enabled"/> or
  /// <see cref="Active"/>.
  /// Some requests of plugin items at the plugin manager will change the plugin's state to
  /// <see cref="Active"/>, depending on the item's builder.
  /// If no activating plugin items are accessed and the plugin isn't auto-activated, the plugin
  /// will remain in state <see cref="Enabled"/>.
  /// When the plugin should be stopped, a two-phase stopping procedure will take place:
  /// First, the state will change to <see cref="EndRequest"/> and the
  /// <see cref="IPluginStateTracker">plugin state tracker instance</see>
  /// and all plugin item's clients will be asked to begin their stopping procedure. If all end requests
  /// succeeded, the plugin's state changes to <see cref="Stopping"/> and the plugin state tracker
  /// instance and all item's clients will be stopped. Then, the plugin's state will change to
  /// <see cref="Available"/>.
  /// If the end request didn't succeed, the plugin's activation state will be restored.
  /// </summary>
  public enum PluginState
  {
    /// <summary>
    /// The plugin was loaded into the system and was not enabled/disabled yet.
    /// The plugin state tracker instance might not be loaded in this state.
    /// No access to plugin items or classes will take place in this state.
    /// </summary>
    Available,

    /// <summary>
    /// The plugin is enabled.
    /// The plugin state tracker might not be instantiated yet in this state.
    /// Enabled plugins are ready to be activated at any time.
    /// Plugin items (classes, resources) may be requested from the plugin in this state.
    /// </summary>
    Enabled,

    /// <summary>
    /// The plugin is active. This means the plugin state tracker instance was loaded and
    /// its <see cref="IPluginStateTracker.Activated"/> method has already been called.
    /// Items (classes, resources) may be requested from the plugin in this state.
    /// </summary>
    Active,

    /// <summary>
    /// The plugin is in its first phase of the two-phase stopping procedure. In this state, the plugin's
    /// manifest instance and all clients are queried if they are ready to stop.
    /// In this state, item requests will be delayed until the plugin either continues its stopping
    /// procedure (which will make the item request fail) or the stopping process is cancelled
    /// (which will continue the item request).
    /// </summary>
    EndRequest,

    /// <summary>
    /// The plugin is performing its second phase of the two-phase stopping procedure. This means its
    /// <see cref="IPluginStateTracker.RequestEnd()"/> method and all item end requests succeeded.
    /// In this state, no more item requests are allowed.
    /// </summary>
    Stopping,

    /// <summary>
    /// The plugin is disabled (because it was explicitly disabled by the user or because the
    /// plugin manager decided to disable the plugin as a result of conflicts or unfulfilled dependencies).
    /// Disabled plugins neither expose any items (classes, resources) to the system, nor get the chance to
    /// execute code.
    /// </summary>
    Disabled
  }
}
