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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public class VideoSortByDuration : AbstractSortByComparableValueAttribute<long>
  {
    public VideoSortByDuration() : base(Consts.RES_COMMON_BY_DURATION_MENU_ITEM, Consts.RES_COMMON_BY_DURATION_MENU_ITEM, VideoStreamAspect.ATTR_DURATION)
    {
      _includeMias = new[] { VideoStreamAspect.ASPECT_ID };
    }

    public override int Compare(MediaItem x, MediaItem y)
    {
      long? durationX = null;
      long? durationY = null;
      Dictionary<int, long> videoLengthX = new Dictionary<int, long>();
      Dictionary<int, long> videoLengthY = new Dictionary<int, long>();
      IList<MultipleMediaItemAspect> videoAspectsX;
      IList<MultipleMediaItemAspect> videoAspectsY;
      if (MediaItemAspect.TryGetAspects(x.Aspects, VideoStreamAspect.Metadata, out videoAspectsX))
      {
        foreach (MultipleMediaItemAspect videoAspect in videoAspectsX)
        {
          long? duration = (long?)videoAspect[VideoStreamAspect.ATTR_DURATION];
          int? partSet = (int?)videoAspect[VideoStreamAspect.ATTR_VIDEO_PART_SET];
          if (partSet.HasValue && duration.HasValue)
          {
            if (!videoLengthX.ContainsKey(partSet.Value))
              videoLengthX.Add(partSet.Value, 0);
            videoLengthX[partSet.Value] += duration.Value;
          }
        }

        if (videoLengthX.Count > 0)
          durationX = videoLengthX.First().Value;
      }
      if (MediaItemAspect.TryGetAspects(y.Aspects, VideoStreamAspect.Metadata, out videoAspectsY))
      {
        foreach (MultipleMediaItemAspect videoAspect in videoAspectsY)
        {
          long? duration = (long?)videoAspect[VideoStreamAspect.ATTR_DURATION];
          int? partSet = (int?)videoAspect[VideoStreamAspect.ATTR_VIDEO_PART_SET];
          if (partSet.HasValue && duration.HasValue)
          {
            if (!videoLengthY.ContainsKey(partSet.Value))
              videoLengthY.Add(partSet.Value, 0);
            videoLengthY[partSet.Value] += duration.Value;
          }
        }

        if (videoLengthY.Count > 0)
          durationY = videoLengthY.First().Value;
      }
      return ObjectUtils.Compare(durationX, durationY);
    }

    public override object GetGroupByValue(MediaItem item)
    {
      Dictionary<int, long> videoLength = new Dictionary<int, long>();
      IList<MultipleMediaItemAspect> videoAspects;
      if (MediaItemAspect.TryGetAspects(item.Aspects, VideoStreamAspect.Metadata, out videoAspects))
      {
        foreach (MultipleMediaItemAspect videoAspect in videoAspects)
        {
          long? duration = (long?)videoAspect[VideoStreamAspect.ATTR_DURATION];
          int? partSet = (int?)videoAspect[VideoStreamAspect.ATTR_VIDEO_PART_SET];
          if (partSet.HasValue && duration.HasValue)
          {
            if (!videoLength.ContainsKey(partSet.Value))
              videoLength.Add(partSet.Value, 0);
            videoLength[partSet.Value] += duration.Value;
          }
        }

        long? totalDuration = null;
        if (videoLength.Count > 0)
          totalDuration = videoLength.First().Value;
        if (totalDuration.HasValue)
          return totalDuration.Value;
      }
      return base.GetGroupByValue(item);
    }
  }
}
