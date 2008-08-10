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
  /// Reflects the state of a plugin during its lifetime.
  /// If the user doesn't manually disable a plugin, and if the plugin manager decides that there 
  /// are no conflicts to other plugins, the state of the plugin will be <see cref="Enabled"/>.
  /// Resources of this plugin may only be accessed if this plugin is <see cref="Enabled"/>.
  /// If any registered part of the plugin is instantiated, the <see cref="IPlugin"/> Initialise() mehtod will
  /// be run and the will be changed to <see cref="Initialised"/>
  /// </summary>
  public enum PluginState
  {
    Disabled,
    Enabled,
    Initialised
  }
}
