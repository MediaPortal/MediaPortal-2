#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System.Collections.Generic;

namespace MediaPortal.Core.PluginManager
{
  /// <summary>
  /// Interface for a change listener which gets called when plugin item registrations change.
  /// </summary>
  public interface IItemRegistrationChangeListener
  {
    /// <summary>
    /// Called when items at the given <paramref name="location"/> were added.
    /// </summary>
    /// <param name="location">Plugin tree location where items were added.</param>
    /// <param name="items">Collection of items which were added.</param>
    void ItemsWereAdded(string location, ICollection<PluginItemMetadata> items);

    /// <summary>
    /// Called when items at the given <paramref name="location"/> were removed.
    /// </summary>
    /// <param name="location">Plugin tree location where items were removed.</param>
    /// <param name="items">Collection of items which were removed.</param>
    void ItemsWereRemoved(string location, ICollection<PluginItemMetadata> items);
  }
}
