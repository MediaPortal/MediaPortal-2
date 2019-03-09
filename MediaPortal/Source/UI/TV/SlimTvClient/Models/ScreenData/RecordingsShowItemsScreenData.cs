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

using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.SlimTv.Client.TvHandler;
using MediaPortal.UiComponents.Media.FilterCriteria;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.UiComponents.Media.Models.ScreenData;


namespace MediaPortal.Plugins.SlimTv.Client.Models.ScreenData
{
  public class RecordingsShowItemsScreenData : AbstractItemsScreenData
  {
    public RecordingsShowItemsScreenData(PlayableItemCreatorDelegate playableItemCreator) :
      base(SlimTvConsts.SCREEN_RECORDINGS_SHOW_ITEMS, SlimTvConsts.RES_SHOW_ALL_RECORDINGS_ITEMS_MENU_ITEM,
        SlimTvConsts.RES_FILTER_RECORDINGS_ITEMS_NAVBAR_DISPLAY_LABEL, playableItemCreator, true)
    {
    }

    public override AbstractItemsScreenData Derive()
    {
      return new RecordingsShowItemsScreenData(PlayableItemCreator);
    }
  }
}
