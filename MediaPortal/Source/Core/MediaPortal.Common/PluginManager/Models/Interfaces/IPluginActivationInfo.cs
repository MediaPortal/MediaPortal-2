#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;

namespace MediaPortal.Common.PluginManager.Models
{
  /// <summary>
  /// 
  /// </summary>
  public interface IPluginActivationInfo
  {
    /// <summary>
    /// Gets the information if this plugin should be automatically activated when enabled.
    /// </summary>
	  bool AutoActivate { get; }

    /// <summary>
    /// Returns the file paths of all assemblies to be loaded for this plugin.
    /// </summary>
    ICollection<string> Assemblies { get; }

    /// <summary>
    /// Gets all builders defined by this plugin. The value will contain a mapping of builder names
    /// to their builder class names.
    /// </summary>
	  IDictionary<string, string> Builders { get; }

    /// <summary>
    /// Gets the name of the state tracker class for this plugin. If no state tracker should be used,
    /// this value is <c>null</c>.
    /// </summary>
    string StateTrackerClassName { get; }

    /// <summary>
    /// Returns all plugin's item registration metadata, which contain the item's ids, the registration
    /// locations and the additional attributes of the item.
    /// </summary>
    ICollection<PluginItemMetadata> Items { get; }

    /// <summary>
    /// Returns the names of all builders which are necessary to build the items of this
    /// plugin. This is a convenience method for iterating over <see cref="Items"/>
    /// and collecting all builder names.
    /// </summary>
    /// <returns>Collection of builder names.</returns>
    ICollection<string> GetNecessaryBuilders();
  }
}
