#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Helpers;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  public class FilterByImageSizeCriterion : MLFilterCriterion
  {
    #region Base overrides

    public override async Task<ICollection<FilterValue>> GetAvailableValuesAsync(IEnumerable<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        throw new NotConnectedException("The MediaLibrary is not connected");
      IFilter emptyFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
          new EmptyFilter(ImageAspect.ATTR_WIDTH),
          new RelationalFilter(ImageAspect.ATTR_WIDTH, RelationalOperator.EQ, 0),
          new EmptyFilter(ImageAspect.ATTR_HEIGHT),
          new RelationalFilter(ImageAspect.ATTR_HEIGHT, RelationalOperator.EQ, 0));
      IFilter simpleSmallFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
          new RelationalFilter(ImageAspect.ATTR_WIDTH, RelationalOperator.LT, Consts.SMALL_SIZE_THRESHOLD),
          new RelationalFilter(ImageAspect.ATTR_HEIGHT, RelationalOperator.LT, Consts.SMALL_SIZE_THRESHOLD));
      IFilter smallFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
          simpleSmallFilter,
          new NotFilter(BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
            new RelationalFilter(ImageAspect.ATTR_WIDTH, RelationalOperator.EQ, 0),
            new RelationalFilter(ImageAspect.ATTR_HEIGHT, RelationalOperator.EQ, 0))));
      IFilter bigFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
          new RelationalFilter(ImageAspect.ATTR_WIDTH, RelationalOperator.GT, Consts.BIG_SIZE_THRESHOLD),
          new RelationalFilter(ImageAspect.ATTR_HEIGHT, RelationalOperator.GT, Consts.BIG_SIZE_THRESHOLD));
      IFilter mediumFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
          new NotFilter(simpleSmallFilter),
          new NotFilter(bigFilter));

      bool showVirtual = VirtualMediaHelper.ShowVirtualMedia(necessaryMIATypeIds);

      var taskEmpty = cd.CountMediaItemsAsync(necessaryMIATypeIds, emptyFilter, true, showVirtual);
      var taskSmall = cd.CountMediaItemsAsync(necessaryMIATypeIds, smallFilter, true, showVirtual);
      var taskMedium = cd.CountMediaItemsAsync(necessaryMIATypeIds, mediumFilter, true, showVirtual);
      var taskBig = cd.CountMediaItemsAsync(necessaryMIATypeIds, bigFilter, true, showVirtual);

      var counts = await Task.WhenAll(taskEmpty, taskSmall, taskMedium, taskBig);

      return new List<FilterValue>(new FilterValue[]
        {
            new FilterValue(Consts.RES_VALUE_EMPTY_TITLE, emptyFilter, null, counts[0], this),
            new FilterValue(Consts.RES_IMAGE_FILTER_SMALL, smallFilter, null, counts[1], this),
            new FilterValue(Consts.RES_IMAGE_FILTER_MEDIUM, mediumFilter, null, counts[2], this),
            new FilterValue(Consts.RES_IMAGE_FILTER_BIG, bigFilter, null, counts[3], this),
        }.Where(fv => !fv.NumItems.HasValue || fv.NumItems.Value > 0));
    }

    public override Task<ICollection<FilterValue>> GroupValuesAsync(ICollection<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      return Task.FromResult((ICollection<FilterValue>)null);
    }

    #endregion
  }
}
