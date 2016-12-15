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

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public class SeriesSortBySeasonTitle : SortByTitle
  {
    public SeriesSortBySeasonTitle()
    {
      _includeMias = new[] { SeasonAspect.ASPECT_ID };
      _excludeMias = null;
    }

    public override int Compare(MediaItem item1, MediaItem item2)
    {
      SingleMediaItemAspect seasonAspectX;
      SingleMediaItemAspect seasonAspectY;
      if (MediaItemAspect.TryGetAspect(item1.Aspects, SeasonAspect.Metadata, out seasonAspectX) && MediaItemAspect.TryGetAspect(item2.Aspects, SeasonAspect.Metadata, out seasonAspectY))
      {
        int seasonX = (int)(seasonAspectX.GetAttributeValue(SeasonAspect.ATTR_SEASON) ?? 0);
        int seasonY = (int)(seasonAspectY.GetAttributeValue(SeasonAspect.ATTR_SEASON) ?? 0);
        return seasonX.CompareTo(seasonY);
      }
      return base.Compare(item1, item2);
    }
  }
}
