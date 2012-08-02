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

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MP2_PluginWizard.Model
{
  /// <summary>
  /// Holds the item data for a plugin register item.
  /// </summary>
  public class PluginRegisterItem
  {
    #region Ctor
    public PluginRegisterItem(string builderName, string id, bool redundant)
    {
      BuilderName = builderName;
      Id = id;
      IsRedundant = redundant;
      Attributes = new ObservableCollection<PluginRegisterAttribute>();
    }



    public PluginRegisterItem(string builderName, string id, bool redundant, IEnumerable<PluginRegisterAttribute> attributeList)
    {
      BuilderName = builderName;
      Id = id;
      IsRedundant = redundant;
      Attributes = new ObservableCollection<PluginRegisterAttribute>(attributeList);
    }

    #endregion

    #region Public properties
    ///<summary>
    ///Returns the name of builder required to build this item.
    ///</summary>
    public string BuilderName { get; set; }

    /// <summary>
    /// Returns the item's id. The id is the default identification property for an item.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Returns the information if this plugin item is a "redundant item".
    /// </summary>
    /// <remarks>
    /// A redundant item is is omitted when trying to add it to the plugin tree and another plugin item is
    /// already registered there. This can be used for structural items like virtual folders (for example
    /// config sections), where the plugin needs such an item registration but doesn't know if another
    /// plugin did register this needed structural item.
    /// </remarks>
    public bool IsRedundant { get; set; }

    /// <summary>
    /// Returns the items attributes.
    /// </summary>
    public ObservableCollection<PluginRegisterAttribute> Attributes { get; private set; }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return string.Format("{0}  {{{1}}}", BuilderName, Id);
    }

    #endregion
  }
}
