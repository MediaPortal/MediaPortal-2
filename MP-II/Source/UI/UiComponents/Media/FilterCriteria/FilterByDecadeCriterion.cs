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

using System;
using System.Collections.Generic;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.MLQueries;

namespace UiComponents.Media.FilterCriteria
{
  /// <summary>
  /// Filter criterion which filters by the decade of the media item's recording time.
  /// </summary>
  public class FilterByDecadeCriterion : MLFilterCriterion
  {
    // We produce hardcoded titles for this filter criterion like "< 1950", "1950-1960", ... Should we use language
    // resources for them?

    #region Base overrides

    public override ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter filter)
    {
      ICollection<FilterValue> result = new List<FilterValue>(10)
        {
            new FilterValue(VALUE_EMPTY_TITLE,
                new EmptyFilter(MediaAspect.ATTR_RECORDINGTIME), this),
            new FilterValue("< 1950",
                new RelationalFilter(
                    MediaAspect.ATTR_RECORDINGTIME, RelationalOperator.LT, new DateTime(1950, 1, 1)), this),
        };
      int startYear = 1950;
      DateTime now = DateTime.Now;
      while (true)
      {
        DateTime startDate = new DateTime(startYear, 1, 1);
        if (startDate >= now)
          break;
        result.Add(new FilterValue(string.Format("{0} - {1}", startYear, startYear + 10),
            BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
                new RelationalFilter(
                    MediaAspect.ATTR_RECORDINGTIME, RelationalOperator.GE, new DateTime(startYear, 1, 1)),
                new RelationalFilter(
                    MediaAspect.ATTR_RECORDINGTIME, RelationalOperator.LT, new DateTime(startYear + 10, 1, 1))), this));
        startYear += 10;
      }
      return result;
    }

    public override IFilter CreateFilter(FilterValue filterValue)
    {
      return (IFilter) filterValue.Value;
    }

    #endregion
  }
}
