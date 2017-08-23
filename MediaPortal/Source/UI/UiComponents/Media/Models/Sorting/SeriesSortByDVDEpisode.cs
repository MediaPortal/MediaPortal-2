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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public class SeriesSortByDVDEpisode : SortByTitle
  {
    public SeriesSortByDVDEpisode()
    {
      _includeMias = new[] { EpisodeAspect.ASPECT_ID };
      _excludeMias = null;
    }

    public override string DisplayName
    {
      get { return Consts.RES_COMMON_BY_DVD_EPISODE_MENU_ITEM; }
    }

    public override int Compare(MediaItem item1, MediaItem item2)
    {
      SingleMediaItemAspect episodeAspectX;
      SingleMediaItemAspect episodeAspectY;
      if (MediaItemAspect.TryGetAspect(item1.Aspects, EpisodeAspect.Metadata, out episodeAspectX) && MediaItemAspect.TryGetAspect(item2.Aspects, EpisodeAspect.Metadata, out episodeAspectY))
      {
        int seasonX = (int) (episodeAspectX.GetAttributeValue(EpisodeAspect.ATTR_SEASON) ?? 0);
        int seasonY = (int) (episodeAspectY.GetAttributeValue(EpisodeAspect.ATTR_SEASON) ?? 0);
        int seasonRes = seasonX.CompareTo(seasonY);
        if (seasonRes != 0)
          return seasonRes;

        IEnumerable<double> episodesX = episodeAspectX.GetCollectionAttribute<double>(EpisodeAspect.ATTR_DVDEPISODE);
        IEnumerable<double> episodesY = episodeAspectY.GetCollectionAttribute<double>(EpisodeAspect.ATTR_DVDEPISODE);
        
        double episodeX = 0;
        double episodeY = 0;
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

    public override string GroupByDisplayName
    {
      get { return Consts.RES_COMMON_BY_DVD_EPISODE_MENU_ITEM; }
    }

    public override object GetGroupByValue(MediaItem item)
    {
      IList<MediaItemAspect> episodeAspect;
      if (item.Aspects.TryGetValue(SeriesAspect.ASPECT_ID, out episodeAspect))
      {
        IEnumerable<double> episodes = episodeAspect.First().GetCollectionAttribute<double>(EpisodeAspect.ATTR_DVDEPISODE);

        return episodes.FirstOrDefault();
      }
      return base.GetGroupByValue(item);
    }
  }
}
