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

namespace MediaPortal.Common.PluginManager.Models
{
  /// <summary>
  /// Plugin metadata class representing a core component provided by MediaPortal itself.
  /// </summary>
  public class CoreComponent
  {
    #region Dependency Details
    /// <summary>
    /// Returns the fully qualified type name of the core component.
    /// </summary>
    public string Name { get; protected set; }

    /// <summary>
    /// Returns the current API level of this core component.
    /// </summary>
    public int CurrentApi { get; private set; }

    /// <summary>
    /// Specifies the minimum API level of this core component that is compatible with the current API level of this core component's version.
    /// </summary>
    public int MinCompatibleApi { get; private set; }
    #endregion

    #region Ctor
    public CoreComponent()
    {
    }

    public CoreComponent( string name, int currentApi )
    {
      Name = name;
      CurrentApi = currentApi;
      MinCompatibleApi = currentApi;
    }

    public CoreComponent( string name, int currentApi, int minCompatibleApi )
    {
      Name = name;
      CurrentApi = currentApi;
      MinCompatibleApi = minCompatibleApi;
    }
    #endregion
  }
}
