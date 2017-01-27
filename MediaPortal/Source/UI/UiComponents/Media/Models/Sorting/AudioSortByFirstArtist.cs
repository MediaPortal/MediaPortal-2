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

using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public class AudioSortByFirstArtist : AbstractSortByFirstComparableAttribute<string>
  {
    public AudioSortByFirstArtist() : base(new[] { AudioAspect.ATTR_ARTISTS, AudioAlbumAspect.ATTR_ARTISTS })
    {
      _includeMias = new[] { AudioAspect.ASPECT_ID, AudioAlbumAspect.ASPECT_ID };
    }

    public override string DisplayName
    {
      get { return Consts.RES_COMMON_BY_ARTIST_MENU_ITEM; }
    }

    public override string GroupByDisplayName
    {
      get { return Consts.RES_COMMON_BY_ARTIST_MENU_ITEM; }
    }
  }
}
