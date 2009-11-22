#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace MediaPortal.UI.SkinEngine.Controls
{
  /// <summary>
  /// This interface marks UI elements which are used in searchable containers like
  /// <see cref="ListView"/>s and <see cref="TreeView"/>s. It provides a searchable string
  /// representation of the contents.
  /// </summary>
  public interface ISearchableItem
  {
    /// <summary>
    /// Returns a string representation for the contents of this element.
    /// </summary>
    string DataString { get; }
  }
}
