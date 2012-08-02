#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MP2_PluginWizard.Model
{
  /// <summary>
  /// Holds the data for ConflictsWith and DependsOn records.
  /// </summary>
  public class PluginNameId
  {
    #region Ctor

    public PluginNameId()
    {
    }

    public PluginNameId(string pluginName, Guid pluginId)
    {
      Name = pluginName;
    	Id = pluginId;
    }

    #endregion

    #region Public properties
    ///<summary>
    ///Returns the name of the plugin.
    ///</summary>
    public string Name { get; set; }

    /// <summary>
    /// Returns the plugin's id.
    /// </summary>
    public Guid Id { get; set; }

    
    #endregion

    #region Base overrides

    public override string ToString()
    {
      return string.Format("{0}  {{{1}}}", Name, Id);
    }

    #endregion
  }
}
