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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Settings;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  public class FilterByImageSizeCriterion : MLFilterCriterion
  {
    #region Base overrides

    public override ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
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
      int numEmptyItems = cd.CountMediaItems(necessaryMIATypeIds, emptyFilter, true, ShowVirtualSetting.ShowVirtualMedia(necessaryMIATypeIds));
      int numSmallItems = cd.CountMediaItems(necessaryMIATypeIds, smallFilter, true, ShowVirtualSetting.ShowVirtualMedia(necessaryMIATypeIds));
      int numMediumItems = cd.CountMediaItems(necessaryMIATypeIds, mediumFilter, true, ShowVirtualSetting.ShowVirtualMedia(necessaryMIATypeIds));
      int numBigItems = cd.CountMediaItems(necessaryMIATypeIds, bigFilter, true, ShowVirtualSetting.ShowVirtualMedia(necessaryMIATypeIds));
      return new List<FilterValue>(new FilterValue[]
        {
            new FilterValue(Consts.RES_VALUE_EMPTY_TITLE, emptyFilter, null, numEmptyItems, this),
            new FilterValue(Consts.RES_IMAGE_FILTER_SMALL, smallFilter, null, numSmallItems, this),
            new FilterValue(Consts.RES_IMAGE_FILTER_MEDIUM, mediumFilter, null, numMediumItems, this),
            new FilterValue(Consts.RES_IMAGE_FILTER_BIG, bigFilter, null, numBigItems, this),
        }.Where(fv => !fv.NumItems.HasValue || fv.NumItems.Value > 0));
    }

    public override ICollection<FilterValue> GroupValues(ICollection<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      return null;
    }

    #endregion
  }
}
