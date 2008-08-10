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
  /// Interface for plugin item factory classes. A plugin factory a concept to allow a plugin to
  /// bring in almost all kinds of functionality to the system.
  /// </summary>
  /// <remarks>
  /// Plugin item factory classes are used to instantiate plugin items out of a sort of "parameter set"
  /// from the plugin descriptor file. Every plugin file can add as many "items" to the system as
  /// needed. Every item needs to be explicitly named in the plugin file, and every item needs a
  /// builder class which is able to load the item provided the item parameters from the plugin
  /// descriptor file.
  /// </remarks>
  public interface IPluginItemBuilder
  {
    /// <summary>
    /// Will build an item from the specified item parameter set.
    /// </summary>
    // object BuildItem(string name, IDictionary<string, string> parameters);

    object BuildItem(IPluginRegisteredItem item);
	}
}
