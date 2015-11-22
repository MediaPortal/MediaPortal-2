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
  public class FilterBySeriesSeasonCriterion : MLFilterCriterion
  {
    #region Base overrides

    public override ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        throw new NotConnectedException("The MediaLibrary is not connected");

      IEnumerable<Guid> necessaryMIAs = new[] { MediaAspect.ASPECT_ID, ProviderResourceAspect.ASPECT_ID, SeasonAspect.ASPECT_ID };
      MediaItemQuery query = new MediaItemQuery(necessaryMIAs, filter)
      {
        SortInformation = new List<SortInformation> { new SortInformation(SeasonAspect.ATTR_SEASON, SortDirection.Ascending) }
      };
      var seasons = cd.Search(query, true);
      IList<FilterValue> result = new List<FilterValue>(seasons.Count);
      foreach (var seasonItem in seasons)
      {
        int season;
        string seriesName;
        MediaItemAspect.TryGetAttribute(seasonItem.Aspects, SeasonAspect.ATTR_SEASON, out season);
        MediaItemAspect.TryGetAttribute(seasonItem.Aspects, SeasonAspect.ATTR_SERIESNAME, out seriesName);
        // Todo: localized "season" name or abbreviation 
        string label = string.Format("{0} S{1}", seriesName, season);
        result.Add(new FilterValue(label,
          new RelationshipFilter(seasonItem.MediaItemId, SeasonAspect.ROLE_SEASON, EpisodeAspect.ROLE_EPISODE),
          null,
          seasonItem,
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
