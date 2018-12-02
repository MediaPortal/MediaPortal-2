#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
  public class AudioSortByCompilation : SortByTitle
  {
    public AudioSortByCompilation()
    {
      _includeMias = new[] { AudioAspect.ASPECT_ID };
    }

    public override string DisplayName
    {
      get { return Consts.RES_COMMON_BY_COMPILATION_MENU_ITEM; }
    }

    public override int Compare(MediaItem item1, MediaItem item2)
    {
      SingleMediaItemAspect audioAspectX;
      SingleMediaItemAspect audioAspectY;
      if (MediaItemAspect.TryGetAspect(item1.Aspects, AudioAspect.Metadata, out audioAspectX) && MediaItemAspect.TryGetAspect(item2.Aspects, AudioAspect.Metadata, out audioAspectY))
      {
        bool compilationX = (bool)(audioAspectX.GetAttributeValue(AudioAspect.ATTR_COMPILATION) ?? false);
        bool compilationY = (bool)(audioAspectY.GetAttributeValue(AudioAspect.ATTR_COMPILATION) ?? false);
        return compilationX.CompareTo(compilationY);
      }
      return base.Compare(item1, item2);
    }

    public override string GroupByDisplayName
    {
      get { return Consts.RES_COMMON_BY_COMPILATION_MENU_ITEM; }
    }

    public override object GetGroupByValue(MediaItem item)
    {
      SingleMediaItemAspect audioAspect;
      if (MediaItemAspect.TryGetAspect(item.Aspects, AudioAspect.Metadata, out audioAspect))
      {
        return (bool)(audioAspect.GetAttributeValue(AudioAspect.ATTR_COMPILATION) ?? false);
      }
      return base.GetGroupByValue(item);
    }
  }
}
