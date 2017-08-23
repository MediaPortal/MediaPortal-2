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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public class SortByAddedDate : SortByTitle
  {
    public SortByAddedDate()
    {
      _includeMias = new[] { ImporterAspect.ASPECT_ID };
      _excludeMias = null;
    }

    public override string DisplayName
    {
      get { return Consts.RES_COMMON_BY_ADDED_DATE_MENU_ITEM; }
    }

    public override int Compare(MediaItem x, MediaItem y)
    {
      SingleMediaItemAspect mediaAspectX;
      SingleMediaItemAspect mediaAspectY;
      DateTime? recordingTimeX = null;
      DateTime? recordingTimeY = null;
      if (MediaItemAspect.TryGetAspect(x.Aspects, ImporterAspect.Metadata, out mediaAspectX))
      {
        recordingTimeX = (DateTime?) mediaAspectX.GetAttributeValue(ImporterAspect.ATTR_DATEADDED);        
      }
      if (MediaItemAspect.TryGetAspect(y.Aspects, ImporterAspect.Metadata, out mediaAspectY))
      {
        recordingTimeY = (DateTime?)mediaAspectY.GetAttributeValue(ImporterAspect.ATTR_DATEADDED);
      }
      return ObjectUtils.Compare(recordingTimeY, recordingTimeX);
    }

    public override string GroupByDisplayName
    {
      get { return Consts.RES_COMMON_BY_ADDED_DATE_MENU_ITEM; }
    }

    public override object GetGroupByValue(MediaItem item)
    {
      IList<MediaItemAspect> mediaAspect;
      if (item.Aspects.TryGetValue(ImporterAspect.ASPECT_ID, out mediaAspect))
      {
        return mediaAspect.First().GetAttributeValue(ImporterAspect.ATTR_DATEADDED);
      }
      return base.GetGroupByValue(item);
    }
  }
}
