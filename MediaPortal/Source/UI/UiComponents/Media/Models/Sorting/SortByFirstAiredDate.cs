#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
      SingleMediaItemAspect episodeAspectX;
      SingleMediaItemAspect episodeAspectY;
      if (MediaItemAspect.TryGetAspect(x.Aspects, EpisodeAspect.Metadata, out episodeAspectX) && MediaItemAspect.TryGetAspect(y.Aspects, EpisodeAspect.Metadata, out episodeAspectY))
      {
        DateTime? firstAiredX = (DateTime?) episodeAspectX.GetAttributeValue(EpisodeAspect.ATTR_FIRSTAIRED);
        DateTime? firstAiredY = (DateTime?) episodeAspectY.GetAttributeValue(EpisodeAspect.ATTR_FIRSTAIRED);
        return ObjectUtils.Compare(firstAiredX, firstAiredY);
      }
      return base.Compare(x, y);
    }

    public override string GroupByDisplayName
    {
      get { return Consts.RES_GROUP_BY_FIRST_AIRED_DATE; }
    }

    public override object GetGroupByValue(MediaItem item)
    {
      IList<MediaItemAspect> episodeAspect;
      if (item.Aspects.TryGetValue(EpisodeAspect.ASPECT_ID, out episodeAspect))
      {
        return episodeAspect.First().GetAttributeValue(EpisodeAspect.ATTR_FIRSTAIRED);
      }
      return base.GetGroupByValue(item);
    }
  }
}
