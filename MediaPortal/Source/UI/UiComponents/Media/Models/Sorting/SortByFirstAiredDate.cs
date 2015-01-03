#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public class SortByFirstAiredDate : SeriesSortByEpisode
  {
    public override string DisplayName
    {
      get { return Consts.RES_SORT_BY_FIRST_AIRED_DATE; }
    }

    public override int Compare(MediaItem x, MediaItem y)
    {
      SingleMediaItemAspect seriesAspectX;
      SingleMediaItemAspect seriesAspectY;
      if (MediaItemAspect.TryGetAspect(x.Aspects, SeriesAspect.Metadata, out seriesAspectX) && MediaItemAspect.TryGetAspect(y.Aspects, SeriesAspect.Metadata, out seriesAspectY))
      {
        DateTime? firstAiredX = (DateTime?) seriesAspectX.GetAttributeValue(SeriesAspect.ATTR_FIRSTAIRED);
        DateTime? firstAiredY = (DateTime?) seriesAspectY.GetAttributeValue(SeriesAspect.ATTR_FIRSTAIRED);
        return ObjectUtils.Compare(firstAiredX, firstAiredY);
      }
      return base.Compare(x, y);
    }
  }
}
