#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.FilterCriteria;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public class MusicFilterByArtistScreenData : AbstractMusicFilterScreenData
  {
    public MusicFilterByArtistScreenData() :
        base(Consts.SCREEN_MUSIC_FILTER_BY_ARTIST, Consts.RES_FILTER_BY_ARTIST_MENU_ITEM,
        Consts.RES_FILTER_ARTIST_NAVBAR_DISPLAY_LABEL, new SimpleMLFilterCriterion(AudioAspect.ATTR_ARTISTS))
    {
    }

    public override AbstractFiltersScreenData Derive()
    {
      return new MusicFilterByArtistScreenData();
    }
  }
}