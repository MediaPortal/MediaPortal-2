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

using MediaPortal.Common.Localization;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public class BrowseMediaNavigationScreenData : AbstractBrowseMediaNavigationScreenData
  {
    public BrowseMediaNavigationScreenData(PlayableItemCreatorDelegate playableItemCreator) :
        base(Consts.SCREEN_BROWSE_MEDIA_NAVIGATION, null, Consts.RES_BROWSE_MEDIA_NAVIGATION_NAVBAR_DISPLAY_LABEL,
        playableItemCreator, true)
    {
      _availableMias = Consts.NECESSARY_BROWSING_MIAS;
    }

    public override string MoreThanMaxItemsHint
    {
      get { return LocalizationHelper.Translate(Consts.RES_MORE_THAN_MAX_ITEMS_BROWSE_HINT, Consts.MAX_NUM_ITEMS_VISIBLE); }
    }

    public override AbstractItemsScreenData Derive()
    {
      return new BrowseMediaNavigationScreenData(PlayableItemCreator);
    }
  }
}
