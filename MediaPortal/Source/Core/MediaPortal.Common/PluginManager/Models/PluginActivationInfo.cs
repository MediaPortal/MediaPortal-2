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
using System.Linq;
using MediaPortal.Common.PluginManager.Items;

namespace MediaPortal.Common.PluginManager.Models
{
  /// <summary>
  /// Plugin metadata class responsible for storing information required to activate a plugin.
  /// </summary>
  public class PluginActivationInfo
  {
    #region Activation Details
    /// <summary>
    /// Gets the information if this plugin should be automatically activated when enabled.
    /// </summary>
    public bool AutoActivate { get; internal set; }

    /// <summary>
    /// Returns the file paths of all assemblies to be loaded for this plugin.
    /// </summary>
    public ICollection<string> Assemblies { get; internal set; }

    /// <summary>
    /// Gets all builders defined by this plugin. The value will contain a mapping of builder names
    /// to their builder class names.
    /// </summary>
    public IDictionary<string, string> Builders { get; internal set; }

    /// <summary>
    /// Gets the name of the state tracker class for this plugin. If no state tracker should be used,
    /// this value is <c>null</c>.
    /// </summary>
    public string StateTrackerClassName { get; internal set; }

    /// <summary>
    /// Returns all plugin's item registration metadata, which contain the item's ids, the registration
    /// locations and the additional attributes of the item.
    /// </summary>
    public ICollection<PluginItemMetadata> Items { get; internal set; }    
    #endregion

    #region Ctor
    public PluginActivationInfo()
    {
      Assemblies = new List<string>();
      Builders = new Dictionary<string, string>();
      Items = new List<PluginItemMetadata>();
    }
    #endregion

    #region Builder Helper Methods (GetNecessaryBuilders)
		/// <summary>
    /// Returns the names of all builders which are necessary to build the items of this
    /// plugin. This is a convenience method for iterating over <see cref="Items"/>
    /// and collecting all builder names.
    /// </summary>
    /// <returns>Collection of builder names.</returns>
    public ICollection<string> GetNecessaryBuilders()
    {
      return Items.Select( itemMetadata => itemMetadata.BuilderName ).ToList();
    }
    #endregion  
  }
}
