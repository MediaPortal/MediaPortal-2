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
  public class FilterByPictureSizeCriterion : MLFilterCriterion
  {
    public struct Size
    {
      public Size(int x, int y)
      {
        X = x;
        Y = y;
      }

      public int X;
      public int Y;
    }

    public static readonly Size MIN_SIZE = new Size(640, 480);

    public Size[] SIZES = new Size[]
      {
        new Size(1024, 768),
        new Size(1600, 1200),
      };

    #region Base overrides

    public override ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter filter)
    {
      ICollection<FilterValue> result = new List<FilterValue>(10)
        {
            new FilterValue(VALUE_EMPTY_TITLE, new BooleanCombinationFilter(BooleanOperator.Or, new IFilter[]
                {
                    new EmptyFilter(PictureAspect.ATTR_WIDTH),
                    new EmptyFilter(PictureAspect.ATTR_HEIGHT)
                }), this),
            new FilterValue(string.Format("< {0}x{1}", MIN_SIZE.X, MIN_SIZE.Y), new BooleanCombinationFilter(BooleanOperator.And, new IFilter[]
              {
                  new RelationalFilter(
                      PictureAspect.ATTR_WIDTH, RelationalOperator.LT, 800),
                  new RelationalFilter(
                      PictureAspect.ATTR_HEIGHT, RelationalOperator.LT, 600),
              }), this)
        };
      Size lastSize = MIN_SIZE;
      foreach (Size size in SIZES)
      {
        result.Add(new FilterValue(string.Format("{0}x{1} - {2}x{3}", lastSize.X, lastSize.Y, size.X, size.Y),
            new BooleanCombinationFilter(BooleanOperator.And, new IFilter[]
              {
                  new RelationalFilter(
                      PictureAspect.ATTR_WIDTH, RelationalOperator.GE, lastSize.X),
                  new RelationalFilter(
                      PictureAspect.ATTR_HEIGHT, RelationalOperator.GE, lastSize.Y),
                  new RelationalFilter(
                      PictureAspect.ATTR_WIDTH, RelationalOperator.LT, size.X),
                  new RelationalFilter(
                      PictureAspect.ATTR_HEIGHT, RelationalOperator.LT, size.Y),
              }), this));
        lastSize = size;
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
