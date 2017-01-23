#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  /// <summary>
  /// Interface for providing a grouping value
  /// </summary>
  public interface IGroupingValueProvider
  {
    /// <summary>
    /// Returns the grouping value of an item
    /// </summary>
    /// <param name="item">Item to get the grouping value from</param>
    /// <returns>Returns the grouping value</returns>
    object GetGroupingValue(object item);

    /// <summary>
    /// Gets if grouping is currently active
    /// </summary>
    bool IsGroupingActive { get; }
  }
}