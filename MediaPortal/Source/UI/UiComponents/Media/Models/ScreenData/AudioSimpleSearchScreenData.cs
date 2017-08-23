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
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UiComponents.Media.General;
using System.Linq;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public class AudioSimpleSearchScreenData : AbstractSearchScreenData
  {
    public AudioSimpleSearchScreenData(PlayableItemCreatorDelegate playableItemCreator) :
        base(Consts.SCREEN_AUDIO_SIMPLE_SEARCH, Consts.RES_SIMPLE_SEARCH_FILTER_MENU_ITEM, playableItemCreator)
    {
      _availableMias = Consts.NECESSARY_AUDIO_MIAS;
      if (Consts.OPTIONAL_AUDIO_MIAS != null)
        _availableMias = _availableMias.Union(Consts.OPTIONAL_AUDIO_MIAS);
    }

    public override AbstractItemsScreenData Derive()
    {
      return new AudioShowItemsScreenData(PlayableItemCreator);
    }

    protected override IFilter BuildTextSearchFilter()
    {
      // Search in both Artists and Title names
      var filter = new BooleanCombinationFilter(BooleanOperator.Or,
        new IFilter[]
        {
          new LikeFilter(AudioAspect.ATTR_ALBUMARTISTS, GetSearchTerm(), null),
          new LikeFilter(AudioAspect.ATTR_ARTISTS, GetSearchTerm(), null),
          new LikeFilter(MediaAspect.ATTR_TITLE, GetSearchTerm(), null),
          new LikeFilter(AudioAspect.ATTR_TRACKNAME, GetSearchTerm(), null)
        });
      return filter;
    }
  }
}
