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

namespace MediaPortal.Common.PluginManager.Models
{
  /// <summary>
  /// Plugin metadata class representing a single plugin dependency declaration.
  /// </summary>
  public class PluginDependency
  {
    #region Dependency Details
    /// <summary>
    /// Returns the identifier of the referenced plugin, or Guid.Empty if this dependency is 
    /// for a named core dependency.
    /// </summary>
    public Guid PluginId { get; protected set; }

    /// <summary>
    /// Returns the name of the core dependency, or null if this dependency is for a normal 
    /// plugin dependency.
    /// </summary>
    public string CoreDependencyName { get; protected set; }

    /// <summary>
    /// Returns true if this reference is for a named core dependency.
    /// </summary>
    public bool IsCoreDependency { get { return !string.IsNullOrWhiteSpace( CoreDependencyName ); } }

    /// <summary>
    /// Returns the compatible API version of the referenced dependency.
    /// </summary>
    public int CompatibleApi { get; protected set; }    
    #endregion

    #region Ctor
    public PluginDependency( Guid pluginId, int compatibleApi )
    {
      PluginId = pluginId;
      CompatibleApi = compatibleApi;
    }

    public PluginDependency( string coreDependencyName, int compatibleApi )
    {
      CoreDependencyName = coreDependencyName;
      CompatibleApi = compatibleApi;
    }
    #endregion
  }
}
