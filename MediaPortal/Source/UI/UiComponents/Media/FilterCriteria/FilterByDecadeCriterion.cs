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
using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Settings;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  /// <summary>
  /// Filter criterion which filters by the decade of the media item's recording time.
  /// </summary>
  public class FilterByDecadeCriterion : MLFilterCriterion
  {
    // We produce hardcoded titles for this filter criterion like "< 1950", "1950-1960", ... Should we use language
    // resources for them?

    #region Base overrides

    public override ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      // We'll do the grouping here at the client. We could also implement a grouping function for decades at the server,
      // but that seems to be not better since it increases the server code size, the server workload, it complicates the
      // call structure and it doesn't bring us an advantage
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        throw new NotConnectedException("The MediaLibrary is not connected");

      HomogenousMap valueGroups = cd.GetValueGroups(MediaAspect.ATTR_RECORDINGTIME, null, ProjectionFunction.DateToYear,
          necessaryMIATypeIds, filter, true, ShowVirtualSetting.ShowVirtualMedia(necessaryMIATypeIds));
      IList<FilterValue> result = new List<FilterValue>(valueGroups.Count);
      int numEmptyEntries = 0;
      IDictionary<int, int> decadesToNumItems = new Dictionary<int, int>();
      foreach (KeyValuePair<object, object> group in valueGroups)
      {
        int? year = (int?) group.Key;
        if (year.HasValue)
        {
          int yearVal = year.Value;
          int decade = yearVal / 10;
          int numItems;
          if (!decadesToNumItems.TryGetValue(decade, out numItems))
            numItems = 0;
          decadesToNumItems[decade] = numItems + (int) group.Value;
        }
        else
          numEmptyEntries += (int) group.Value;
      }
      if (numEmptyEntries > 0)
        result.Insert(0, new FilterValue(Consts.RES_VALUE_EMPTY_TITLE, new EmptyFilter(MediaAspect.ATTR_RECORDINGTIME), null, numEmptyEntries, this));
      for (int decade = 0; decade < 300; decade++)
      {
        int year = decade * 10;
        int numItems;
        if (!decadesToNumItems.TryGetValue(decade, out numItems))
          continue;
        result.Add(new FilterValue(year.ToString(),
            new BooleanCombinationFilter(BooleanOperator.And, new IFilter[]
              {
                  new RelationalFilter(MediaAspect.ATTR_RECORDINGTIME, RelationalOperator.GE, new DateTime(year, 1, 1)),
                  new RelationalFilter(MediaAspect.ATTR_RECORDINGTIME, RelationalOperator.LT, new DateTime(year + 10, 1, 1)),
              }), null, numItems, this));
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
