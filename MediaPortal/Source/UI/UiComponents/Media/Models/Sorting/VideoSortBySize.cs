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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.General;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public class VideoSortBySize : SortByTitle
  {
    public override string DisplayName
    {
      get { return Consts.RES_SORT_BY_SIZE; }
    }

    public override int Compare(MediaItem item1, MediaItem item2)
    {
      IList<MultipleMediaItemAspect> videoAspectsX;
      IList<MultipleMediaItemAspect> videoAspectsY;
      if (MediaItemAspect.TryGetAspects(item1.Aspects, VideoStreamAspect.Metadata, out videoAspectsX) && MediaItemAspect.TryGetAspects(item2.Aspects, VideoStreamAspect.Metadata, out videoAspectsY))
      {
        int smallestX = -1;
        foreach (MultipleMediaItemAspect videoAspect in videoAspectsX)
        {
          int? x = (int?)videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_WIDTH);
          int? y = (int?)videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_HEIGHT);
          int size = x.HasValue && y.HasValue ? (x.Value < y.Value ? x.Value : y.Value) : 0;
          if (smallestX == -1 || size < smallestX)
            smallestX = size;
        }
        int smallestY = -1;
        foreach (MultipleMediaItemAspect videoAspect in videoAspectsY)
        {
          int? x = (int?)videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_WIDTH);
          int? y = (int?)videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_HEIGHT);
          int size = x.HasValue && y.HasValue ? (x.Value < y.Value ? x.Value : y.Value) : 0;
          if (smallestY == -1 || size < smallestY)
            smallestY = size;
        }
        return smallestX - smallestY;
      }
      return base.Compare(item1, item2);
    }

    public override string GroupByDisplayName
    {
      get { return Consts.RES_GROUP_BY_SIZE; }
    }

    public override object GetGroupByValue(MediaItem item)
    {
      IList<MediaItemAspect> videoAspect;
      if (item.Aspects.TryGetValue(VideoAspect.ASPECT_ID, out videoAspect))
      {
        int? x = (int?)videoAspect.First().GetAttributeValue(VideoStreamAspect.ATTR_WIDTH);
        int? y = (int?)videoAspect.First().GetAttributeValue(VideoStreamAspect.ATTR_HEIGHT);
        return x.HasValue && y.HasValue ? (x.Value < y.Value ? x.Value : y.Value) : 0;
      }
      return base.GetGroupByValue(item);
    }
  }
}
