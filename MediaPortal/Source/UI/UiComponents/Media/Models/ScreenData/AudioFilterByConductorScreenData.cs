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

using MediaPortal.UiComponents.Media.FilterCriteria;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
using System.Linq;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public class AudioFilterByConductorScreenData : AbstractAudioFilterScreenData<ConductorFilterItem>
  {
    public AudioFilterByConductorScreenData() :
        base(Consts.SCREEN_AUDIO_FILTER_BY_CONDUCTOR, Consts.RES_COMMON_BY_CONDUCTOR_MENU_ITEM,
        Consts.RES_FILTER_CONDUCTOR_NAVBAR_DISPLAY_LABEL, new FilterByConductorCriterion())
    {
      _availableMias = Consts.NECESSARY_PERSON_MIAS;
      if (Consts.OPTIONAL_PERSON_MIAS != null)
        _availableMias = _availableMias.Union(Consts.OPTIONAL_PERSON_MIAS);
    }

    public override AbstractFiltersScreenData<ConductorFilterItem> Derive()
    {
      return new AudioFilterByConductorScreenData();
    }
  }
}
