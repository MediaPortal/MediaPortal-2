#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  public class FilterByImageSizeCriterion : MLFilterCriterion
  {
    public const int SMALL_SIZE_THRESHOLD = 640;
    public const int BIG_SIZE_THRESHOLD = 1200;

    #region Base overrides

    public override ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return new List<FilterValue>();
      IFilter emptyFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
          new EmptyFilter(ImageAspect.ATTR_WIDTH),
          new RelationalFilter(ImageAspect.ATTR_WIDTH, RelationalOperator.EQ, 0),
          new EmptyFilter(ImageAspect.ATTR_HEIGHT),
          new RelationalFilter(ImageAspect.ATTR_HEIGHT, RelationalOperator.EQ, 0));
      IFilter simpleSmallFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
          new RelationalFilter(ImageAspect.ATTR_WIDTH, RelationalOperator.LT, SMALL_SIZE_THRESHOLD),
          new RelationalFilter(ImageAspect.ATTR_HEIGHT, RelationalOperator.LT, SMALL_SIZE_THRESHOLD));
      IFilter smallFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
          simpleSmallFilter,
          new NotFilter(BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
            new RelationalFilter(ImageAspect.ATTR_WIDTH, RelationalOperator.EQ, 0),
            new RelationalFilter(ImageAspect.ATTR_HEIGHT, RelationalOperator.EQ, 0))));
      IFilter bigFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
          new RelationalFilter(ImageAspect.ATTR_WIDTH, RelationalOperator.GT, BIG_SIZE_THRESHOLD),
          new RelationalFilter(ImageAspect.ATTR_HEIGHT, RelationalOperator.GT, BIG_SIZE_THRESHOLD));
      IFilter mediumFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
          new NotFilter(simpleSmallFilter),
          new NotFilter(bigFilter));
      int numEmptyItems = cd.CountMediaItems(necessaryMIATypeIds, emptyFilter, true);
      int numSmallItems = cd.CountMediaItems(necessaryMIATypeIds, smallFilter, true);
      int numMediumItems = cd.CountMediaItems(necessaryMIATypeIds, mediumFilter, true);
      int numBigItems = cd.CountMediaItems(necessaryMIATypeIds, bigFilter, true);
      return new List<FilterValue>(new FilterValue[]
        {
            new FilterValue(Consts.VALUE_EMPTY_TITLE, emptyFilter, null, numEmptyItems, this),
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
