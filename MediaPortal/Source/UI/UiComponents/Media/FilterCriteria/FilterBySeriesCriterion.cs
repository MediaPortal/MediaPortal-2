#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  /// <summary>
  /// Filter criterion which filters by the Series name.
  /// </summary>
  public class FilterBySeriesCriterion : MLFilterCriterion
  {
    #region Base overrides

    public override ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        throw new NotConnectedException("The MediaLibrary is not connected");

      IEnumerable<Guid> necessaryMIAs = new[] { MediaAspect.ASPECT_ID, ProviderResourceAspect.ASPECT_ID, SeriesAspect.ASPECT_ID };
      MediaItemQuery query = new MediaItemQuery(necessaryMIAs, filter)
      {
        SortInformation = new List<SortInformation> { new SortInformation(SeriesAspect.ATTR_SERIESNAME, SortDirection.Ascending) }
      };
      var series = cd.Search(query, true);
      IList<FilterValue> result = new List<FilterValue>(series.Count);
      foreach (var seriesItem in series)
      {
        string title;
        MediaItemAspect.TryGetAttribute(seriesItem.Aspects, SeriesAspect.ATTR_SERIESNAME, out title);
        result.Add(new FilterValue(title,
          new RelationshipFilter(seriesItem.MediaItemId, SeriesAspect.ROLE_SERIES, SeasonAspect.ROLE_SEASON),
          null,
          seriesItem,
          this));
      }
      return result;
    }

    public override ICollection<FilterValue> GroupValues(ICollection<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      return null;
    }

    #endregion
  }
}
