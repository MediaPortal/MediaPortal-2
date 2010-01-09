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

using System;
using System.Collections.Generic;
using MediaPortal.Core.MediaManagement.MLQueries;

namespace UiComponents.Media.FilterCriteria
{
  /// <summary>
  /// Represents a single filter criterion for filtering media items in the system's media library.
  /// </summary>
  public abstract class MLFilterCriterion
  {
    public const string VALUE_EMPTY_TITLE = "[Media.ValueEmptyTitle]";

    /// <summary>
    /// Gets the values which are available in the media library which can be used as a filter for this filter criterion.
    /// </summary>
    /// <param name="necessaryMIATypeIds">Media item aspects which need to be available in the media items, from which
    /// the available values will be collected.</param>
    /// <param name="filter">Base filter for the media items from which the available values will be collected.</param>
    /// <returns>Collection of filter value objects which hold a title for the particular filter value and which can
    /// create the actual filter to be used in a media item query.</returns>
    public abstract ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter filter);

    /// <summary>
    /// Creates a media item filter from the given <paramref name="filterValue"/> to be used in a media item query.
    /// </summary>
    /// <param name="filterValue">Filter value instance which was returned by <see cref="GetAvailableValues"/>.</param>
    /// <returns>Filter instance. The returned filter instance doesn't inculde the filter used when calling method
    /// <see cref="GetAvailableValues"/>, i.e. the return value should be combined with that filter.</returns>
    public abstract IFilter CreateFilter(FilterValue filterValue);
  }
}
