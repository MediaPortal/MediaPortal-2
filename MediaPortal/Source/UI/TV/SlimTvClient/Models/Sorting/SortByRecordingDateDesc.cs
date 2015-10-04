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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.MetadataExtractors.Aspects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public class SortByRecordingDateDesc : SortByTitle
  {
    public override string DisplayName
    {
      get { return Consts.RES_SORT_BY_DATE; }
    }

    public override int Compare(MediaItem x, MediaItem y)
    {
      DateTime startTimeX;
      DateTime startTimeY;
      if (!MediaItemAspect.TryGetAttribute(x.Aspects, RecordingAspect.ATTR_STARTTIME, out startTimeX) || !MediaItemAspect.TryGetAttribute(y.Aspects, RecordingAspect.ATTR_STARTTIME, out startTimeY))
        return base.Compare(x, y);

      return -startTimeX.CompareTo(startTimeY);
    }
  }
}
