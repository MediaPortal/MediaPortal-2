#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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

using System;
using System.Collections.Generic;

namespace MediaPortal.Common.PluginManager.Models
{
  public interface IPluginDependencyInfo
  {
    /// <summary>
    /// Returns the identifier of the plugin to which this information belongs.
    /// </summary>
    Guid PluginId { get; }

    /// <summary>
    /// Returns the current API level of this version of the plugin.
    /// </summary>
    int CurrentApi { get; }
		
    /// <summary>
    /// Returns the minimum API level of this plugin that is compatible with the current API level of this plugin's version.
    /// </summary>
    int MinCompatibleApi { get; }

    /// <summary>
    /// Returns a collection of plugins that this plugin depends on.
    /// </summary>
    IList<PluginDependency> DependsOn { get; }

    /// <summary>
    /// Returns a collection of plugin identifiers that this plugin is in conflict with.
    /// </summary>
	  ICollection<Guid> ConflictsWith { get; }
  }
}
