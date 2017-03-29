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
  public class ImageSortBySize : SortByTitle
  {
    public ImageSortBySize()
    {
      _includeMias = null;
      _excludeMias = null;
    }

    public override string DisplayName
    {
      get { return Consts.RES_COMMON_BY_SIZE_MENU_ITEM; }
    }

    public override int Compare(MediaItem item1, MediaItem item2)
    {
      SingleMediaItemAspect imageAspectX;
      SingleMediaItemAspect imageAspectY;
      if (MediaItemAspect.TryGetAspect(item1.Aspects, ImageAspect.Metadata, out imageAspectX) && MediaItemAspect.TryGetAspect(item2.Aspects, ImageAspect.Metadata, out imageAspectY))
      {
        int? x = (int?) imageAspectX.GetAttributeValue(ImageAspect.ATTR_WIDTH);
        int? y = (int?) imageAspectX.GetAttributeValue(ImageAspect.ATTR_HEIGHT);
        int smallestX = x.HasValue && y.HasValue ? (x.Value < y.Value ? x.Value : y.Value) : 0;
        x = (int?) imageAspectY.GetAttributeValue(ImageAspect.ATTR_WIDTH);
        y = (int?) imageAspectY.GetAttributeValue(ImageAspect.ATTR_HEIGHT);
        int smallestY = x.HasValue && y.HasValue ? (x.Value < y.Value ? x.Value : y.Value) : 0;
        return smallestX - smallestY;
      }
      return base.Compare(item1, item2);
    }

    public override string GroupByDisplayName
    {
      get { return Consts.RES_COMMON_BY_SIZE_MENU_ITEM; }
    }

    public override object GetGroupByValue(MediaItem item)
    {
      IList<MediaItemAspect> imageAspect;
      if (item.Aspects.TryGetValue(ImageAspect.ASPECT_ID, out imageAspect))
      {
        int? x = (int?)imageAspect.First().GetAttributeValue(ImageAspect.ATTR_WIDTH);
        int? y = (int?)imageAspect.First().GetAttributeValue(ImageAspect.ATTR_HEIGHT);
        return x.HasValue && y.HasValue ? (x.Value < y.Value ? x.Value : y.Value) : 0;
      }
      return base.GetGroupByValue(item);
    }
  }
}
