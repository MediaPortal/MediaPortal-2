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

using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UiComponents.Media.Settings;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  /// <summary>
  /// Represents a single filter criterion for filtering media items in the system's media library.
  /// </summary>
  public abstract class MLFilterCriterion
  {
    /// <summary>
    /// Gets the values which are available in the media library which can be used as a filter for this filter criterion.
    /// </summary>
    /// <param name="necessaryMIATypeIds">Media item aspects which need to be available in the media items, from which
    /// the available values will be collected.</param>
    /// <param name="selectAttributeFilter">Special filter which can be implemented by a special filter criterion which
    /// fills the <see cref="FilterValue.SelectAttributeFilter"/>.</param>
    /// <param name="filter">Base filter for the media items from which the available values will be collected.</param>
    /// <returns>Collection of filter value objects which hold a title for the particular filter value and which can
    /// create the actual filter to be used in a media item query.</returns>
    /// <exception cref="NotConnectedException">If the media library is currently not connected.</exception>
    public abstract ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds,
        IFilter selectAttributeFilter, IFilter filter);

    /// <summary>
    /// Builds value groups for the values created by this filter criterion.
    /// </summary>
    /// <param name="necessaryMIATypeIds">Media item aspects which need to be available in the media items, from which
    /// the available values will be collected.</param>
    /// <param name="selectAttributeFilter">Special filter which can be implemented by a special filter criterion which
    /// fills the <see cref="FilterValue.SelectAttributeFilter"/>.</param>
    /// <param name="filter">Base filter for the media items from which the available values will be collected.</param>
    /// <returns>Collection of filter value objects which hold a title for the particular filter value group and which can
    /// create the actual filter to be used in a media item query. If <c>null</c> is returned, this filter doesn't provide
    /// value groups. In that case, the caller can fall back to request all filter values via
    /// <see cref="GetAvailableValues"/>.</returns>
    /// <exception cref="NotConnectedException">If the media library is currently not connected.</exception>
    public abstract ICollection<FilterValue> GroupValues(ICollection<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter,
        IFilter filter);
  }
}
