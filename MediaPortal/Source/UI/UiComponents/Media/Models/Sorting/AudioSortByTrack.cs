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

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public class AudioSortByTrack : AbstractSortByComparableValueAttribute<int>
  {
    public AudioSortByTrack() : base(Consts.RES_COMMON_BY_TRACK_MENU_ITEM, Consts.RES_COMMON_BY_TRACK_MENU_ITEM, AudioAspect.ATTR_TRACK)
    {
      _includeMias = new[] { AudioAspect.ASPECT_ID };
    }

    public override int Compare(MediaItem x, MediaItem y)
    {
      SingleMediaItemAspect audioAspectX;
      SingleMediaItemAspect audioAspectY;
      if (MediaItemAspect.TryGetAspect(x.Aspects, AudioAspect.Metadata, out audioAspectX) && MediaItemAspect.TryGetAspect(y.Aspects, AudioAspect.Metadata, out audioAspectY))
      {
        //Sort by disc number
        int? discIdX = (int?)audioAspectX.GetAttributeValue(AudioAspect.ATTR_DISCID);
        int? discIdY = (int?)audioAspectY.GetAttributeValue(AudioAspect.ATTR_DISCID);
        int res = CompareDiscNumbers(discIdX, discIdY);
        if (res != 0)
          return res;
      }
      //Sort by track
      return base.Compare(x, y);
    }

    protected int CompareDiscNumbers(int? discIdX, int? discIdY)
    {
      //Treat empty or 0 disc number as equal to disc number 1 when sorting
      if ((discIdX.HasValue && discIdX.Value == 1 && (!discIdY.HasValue || discIdY.Value == 0)) ||
        (discIdY.HasValue && discIdY.Value == 1 && (!discIdX.HasValue || discIdX.Value == 0)))
        return 0;
      return ObjectUtils.Compare(discIdX, discIdY);
    }
  }
}
