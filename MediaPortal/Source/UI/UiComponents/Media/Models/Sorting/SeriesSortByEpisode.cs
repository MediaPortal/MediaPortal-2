#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public class SeriesSortByEpisode : SortByTitle
  {
    public override string DisplayName
    {
      get { return Consts.RES_SORT_BY_EPISODE; }
    }

    public override int Compare(MediaItem item1, MediaItem item2)
    {
      MediaItemAspect seriesAspectX;
      MediaItemAspect seriesAspectY;
      if (item1.Aspects.TryGetValue(SeriesAspect.ASPECT_ID, out seriesAspectX) && item2.Aspects.TryGetValue(SeriesAspect.ASPECT_ID, out seriesAspectY))
      {
        int seasonX = (int) (seriesAspectX.GetAttributeValue(SeriesAspect.ATTR_SEASON) ?? 0);
        int seasonY = (int) (seriesAspectY.GetAttributeValue(SeriesAspect.ATTR_SEASON) ?? 0);
        int seasonRes = seasonX.CompareTo(seasonY);
        if (seasonRes != 0)
          return seasonRes;

        IEnumerable<int> episodesX = seriesAspectX.GetCollectionAttribute<int>(SeriesAspect.ATTR_EPISODE);
        IEnumerable<int> episodesY = seriesAspectY.GetCollectionAttribute<int>(SeriesAspect.ATTR_EPISODE);
        
        int episodeX = 0;
        int episodeY = 0;
        if (episodesX != null)
          episodeX = episodesX.FirstOrDefault();
        if (episodesY != null)
          episodeY = episodesY.FirstOrDefault();

        int episodeRes = episodeX.CompareTo(episodeY);
        if (episodeRes != 0)
          return episodeRes;
      }
      return base.Compare(item1, item2);
    }
  }
}
