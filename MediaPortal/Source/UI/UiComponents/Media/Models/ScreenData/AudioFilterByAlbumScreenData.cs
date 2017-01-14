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
using MediaPortal.UiComponents.Media.FilterCriteria;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
using System;
using System.Linq;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public class AudioFilterByAlbumScreenData : AbstractFiltersScreenData<AlbumFilterItem>
  {
    public AudioFilterByAlbumScreenData() :
        base(Consts.SCREEN_AUDIO_FILTER_BY_ALBUM, Consts.RES_COMMON_BY_ALBUM_MENU_ITEM,
        Consts.RES_FILTER_ALBUM_NAVBAR_DISPLAY_LABEL, new FilterByAlbumCriterion())
    {
      _itemMias = new Guid[] { AudioAspect.ASPECT_ID };
      _availableMias = Consts.NECESSARY_ALBUM_MIAS;
      if (Consts.OPTIONAL_ALBUM_MIAS != null)
        _availableMias = _availableMias.Union(Consts.OPTIONAL_ALBUM_MIAS);
    }

    public override AbstractFiltersScreenData<AlbumFilterItem> Derive()
    {
      return new AudioFilterByAlbumScreenData();
    }

    //Special case for album screen, it can support album filters and track filters
    public override bool IsAvailable(AbstractScreenData parentScreen)
    {
      return base.IsAvailable(parentScreen) || parentScreen.ItemMias.Contains(AudioAlbumAspect.ASPECT_ID);
    }
  }
}
