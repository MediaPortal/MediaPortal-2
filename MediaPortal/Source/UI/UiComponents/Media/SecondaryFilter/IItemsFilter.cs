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

using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.Media.SecondaryFilter
{
  /// <summary>
  /// Interface for secondary filters that can be applied to <see cref="ItemsList"/>s.
  /// A "secondary filter" means here that it is applied after to original filtering by the database has been done.
  /// </summary>
  public interface IItemsFilter
  {
    /// <summary>
    /// Applies the given <paramref name="search"/> to the <paramref name="filterList"/>.
    /// </summary>
    /// <param name="filterList"></param>
    /// <param name="originalList"></param>
    /// <param name="search"></param>
    void Filter(ItemsList filterList, ItemsList originalList, string search);

    /// <summary>
    /// Indicates if a filter is active.
    /// </summary>
    bool IsFiltered { get; }

    /// <summary>
    /// Gets a filter message if active.
    /// </summary>
    string Text { get; }
  }
}
