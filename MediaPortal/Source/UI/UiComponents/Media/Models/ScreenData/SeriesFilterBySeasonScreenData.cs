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
using MediaPortal.UiComponents.Media.FilterCriteria;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public class SeriesFilterBySeasonScreenData : AbstractVideosFilterScreenData<SeasonFilterItem>
  {
    public SeriesFilterBySeasonScreenData() :
      base(Consts.SCREEN_SERIES_FILTER_BY_SEASON, Consts.RES_COMMON_BY_SERIES_SEASON_MENU_ITEM,
        Consts.RES_FILTER_SERIES_SEASON_NAVBAR_DISPLAY_LABEL, new FilterBySeriesSeasonCriterion(EpisodeAspect.ROLE_EPISODE))
    {
      _availableMias = Consts.NECESSARY_SEASON_MIAS;
      if (Consts.OPTIONAL_SEASON_MIAS != null)
        _availableMias = _availableMias.Union(Consts.OPTIONAL_SEASON_MIAS);
    }

    public override AbstractFiltersScreenData<SeasonFilterItem> Derive()
    {
      return new SeriesFilterBySeasonScreenData();
    }

    protected override bool SetSelectedItem(IEnumerable<FilterItem> items)
    {
      //Set the first unwatched season as Selected so it has focus when [re]entering the view
      bool selected = false;
      foreach (PlayableContainerMediaItem item in items.OfType<PlayableContainerMediaItem>())
      {
        if (!selected)
          item.Selected = selected = item.WatchPercentage < 100;
        else
          item.Selected = false;
      }
      return selected;
    }
  }
}
