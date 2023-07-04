#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UiComponents.Media.FilterCriteria;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.SlimTv.Client.Models.ScreenData.FilterCriteria
{
  /// <summary>
  /// Criterion for filtering recordings by media type, i.e. TV or radio.
  /// </summary>
  public class FilterByMediaTypeCriterion : MLFilterCriterion
  {
    public override Task<ICollection<FilterValue>> GetAvailableValuesAsync(IEnumerable<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      return Task.FromResult<ICollection<FilterValue>>(new List<FilterValue>
      {
        // To determine whether a recording is TV or radio it is necessary to test for the presence of a VideoStreamAspect or AudioAspect resepctively,
        // however there isn't a ML filter for filtering by the presence of an entire aspect as this is usually done by including the aspect in the list
        // of necessary MIAs but there isn't a way to modify the list of necessary MIAs for a set of media navigation screens when applying a filter criterion.
        // Instead this criterion simply tests for whether an attribute that should always be set if the aspect is present is empty and assumes that if it is
        // then the entire aspect is not present. 
        new FilterValue("[SlimTvClient.Tv]", new EmptyFilter(AudioAspect.ATTR_DURATION), null, this),
        new FilterValue("[SlimTvClient.Radio]", new EmptyFilter(VideoStreamAspect.ATTR_DURATION), null, this)
      });
    }

    public override Task<ICollection<FilterValue>> GroupValuesAsync(ICollection<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      return Task.FromResult<ICollection<FilterValue>>(null);
    }
  }
}
