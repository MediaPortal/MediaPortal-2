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
  /// Represents a registered item from a plugin.
  /// </summary>
  public interface IPluginRegisteredItem
  {
    #region Properties
     ///<summary>
     ///Returns the name of builder required to build this item
     ///</summary>
    string BuilderName
    {
      get;
    }

    /// <summary>
    /// Returns the IPluginInfo associated with this item
    /// </summary>
    IPluginInfo Plugin
    {
      get; 
    }

    /// <summary>
    /// Returns the item's Id
    /// </summary>
    string Id
    {
      get;
    }

    /// <summary>
    /// Returns items attributes
    /// </summary>
    string this[string key]
    {
      get;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Tests to see if an attrubute exists
    /// </summary>
    bool Contains(string key);

    #endregion
  }
}
