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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.General;

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
      MediaItemAspect videoAspectX;
      MediaItemAspect videoAspectY;
      if (item1.Aspects.TryGetValue(VideoAspect.ASPECT_ID, out videoAspectX) && item2.Aspects.TryGetValue(VideoAspect.ASPECT_ID, out videoAspectY))
      {
        int? x = (int?) videoAspectX.GetAttributeValue(VideoAspect.ATTR_WIDTH);
        int? y = (int?) videoAspectX.GetAttributeValue(VideoAspect.ATTR_HEIGHT);
        int smallestX = x.HasValue && y.HasValue ? (x.Value < y.Value ? x.Value : y.Value) : 0;
        x = (int?) videoAspectY.GetAttributeValue(VideoAspect.ATTR_WIDTH);
        y = (int?) videoAspectY.GetAttributeValue(VideoAspect.ATTR_HEIGHT);
        int smallestY = x.HasValue && y.HasValue ? (x.Value < y.Value ? x.Value : y.Value) : 0;
        return smallestX - smallestY;
      }
      return base.Compare(item1, item2);
    }
  }
}
