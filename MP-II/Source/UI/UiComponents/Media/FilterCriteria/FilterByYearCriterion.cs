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
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.MLQueries;

namespace UiComponents.Media.FilterCriteria
{
  /// <summary>
  /// Filter criterion which filters by the year of the media item's recording time.
  /// </summary>
  public class FilterByYearCriterion : MLFilterCriterion
  {
    public const int MIN_YEAR = 2000;

    #region Base overrides

    public override ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter filter)
    {
      // There is currently no way to get all available years from the content directory...
      ICollection<FilterValue> result = new List<FilterValue>
        {
            new FilterValue(VALUE_EMPTY_TITLE,
                new EmptyFilter(MediaAspect.ATTR_RECORDINGTIME), this),
            new FilterValue(string.Format("< {0}", MIN_YEAR),
                new RelationalFilter(
                    MediaAspect.ATTR_RECORDINGTIME, RelationalOperator.LT, new DateTime(MIN_YEAR, 1, 1)), this),
        };
      int maxYear = DateTime.Now.Year;
      for (int year = MIN_YEAR; year < maxYear; year++)
        result.Add(new FilterValue(string.Format("{0}", year),
            new BooleanCombinationFilter(BooleanOperator.And, new IFilter[]
              {
                  new RelationalFilter(
                      MediaAspect.ATTR_RECORDINGTIME, RelationalOperator.GE, new DateTime(year, 1, 1)),
                  new RelationalFilter(
                      MediaAspect.ATTR_RECORDINGTIME, RelationalOperator.LT, new DateTime(year + 1, 1, 1)),
              }), this));
      return result;
    }

    public override IFilter CreateFilter(FilterValue filterValue)
    {
      return (IFilter) filterValue.Value;
    }

    #endregion
  }
}
