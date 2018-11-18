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

using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using MediaPortal.UiComponents.Media.Models.Sorting;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System;
using MediaPortal.UiComponents.Media.FilterTrees;

namespace MediaPortal.UiComponents.Media.Models.NavigationModel
{
  class AudioNavigationInitializer : BaseNavigationInitializer
  {
    internal static IEnumerable<string> RESTRICTED_MEDIA_CATEGORIES = new List<string> { Models.MediaNavigationMode.Audio }; // "Audio"

    protected AudioFilterByAlbumScreenData _albumScreen;

    public AudioNavigationInitializer()
    {
      _mediaNavigationMode = Models.MediaNavigationMode.Audio;
      _mediaNavigationRootState = Consts.WF_STATE_ID_AUDIO_NAVIGATION_ROOT;
      _viewName = Consts.RES_AUDIO_VIEW_NAME;
      _necessaryMias = Consts.NECESSARY_AUDIO_MIAS;
      _optionalMias = Consts.OPTIONAL_AUDIO_MIAS;
      _restrictedMediaCategories = RESTRICTED_MEDIA_CATEGORIES;
      _rootRole = AudioAspect.ROLE_TRACK;
    }

    public static void NavigateToAlbum(Guid albumId)
    {
      MediaNavigationConfig config = new MediaNavigationConfig
      {
        RootScreenType = typeof(AudioFilterByAlbumScreenData),
        DefaultScreenType = typeof(AudioShowItemsScreenData),
        FilterPath = new FilterTreePath(AudioAlbumAspect.ROLE_ALBUM),
        LinkedId = albumId
      };
      MediaNavigationModel.NavigateToRootState(Consts.WF_STATE_ID_AUDIO_NAVIGATION_ROOT, config);
    }

    protected override async Task PrepareAsync()
    {
      await base.PrepareAsync();

      _defaultScreen = new AudioFilterByArtistScreenData();
      _albumScreen = new AudioFilterByAlbumScreenData();
      _availableScreens = new List<AbstractScreenData>
        {
          new AudioShowItemsScreenData(_genericPlayableItemCreatorDelegate),
          // C# doesn't like it to have an assignment inside a collection initializer
          _defaultScreen,
          new AudioFilterByComposerScreenData(),
          new AudioFilterByConductorScreenData(),
          new AudioFilterByAlbumArtistScreenData(),
          _albumScreen,
          new AudioFilterByAlbumLabelScreenData(),
          new AudioFilterByDiscNumberScreenData(),
          new AudioFilterByCompilationScreenData(),
          new AudioFilterByAlbumCompilationScreenData(),
          new AudioFilterByGenreScreenData(),
          new AudioFilterByContentGroupScreenData(),
          new AudioFilterByDecadeScreenData(),
          new AudioFilterBySystemScreenData(),
          new AudioSimpleSearchScreenData(_genericPlayableItemCreatorDelegate),
        };

      _defaultSorting = new AudioSortByAlbumTrack();
      _availableSortings = new List<Sorting.Sorting>
        {
          _defaultSorting,
          new AudioSortByTitle(),
          new SortByTitle(),
          new SortBySortTitle(),
          new SortByName(),
          new AudioSortByCompilation(),
          new AudioAlbumSortByCompilation(),
          new AudioSortByFirstGenre(),
          new AudioAlbumSortByFirstArtist(),
          new AudioAlbumSortByFirstMusicLabel(),
          new AudioSortByFirstArtist(),
          new AudioSortByFirstComposer(),
          new AudioSortByFirstConductor(),
          new AudioSortByAlbum(),
          new AudioSortByTrack(),
          new AudioSortByContentGroup(),
          new SortByYear(),
          new SortByAddedDate(),
          new SortBySystem(),
        };

      _defaultGrouping = null;
      _availableGroupings = new List<Sorting.Sorting>
        {
          //_defaultGrouping,
          new AudioSortByAlbumTrack(),
          new AudioSortByTitle(),
          new SortByTitle(),
          new SortBySortTitle(),
          new SortByName(),
          new AudioSortByCompilation(),
          new AudioAlbumSortByCompilation(),
          new AudioSortByFirstGenre(),
          new AudioAlbumSortByFirstArtist(),
          new AudioAlbumSortByFirstMusicLabel(),
          new AudioSortByFirstArtist(),
          new AudioSortByFirstComposer(),
          new AudioSortByFirstConductor(),
          new AudioSortByAlbum(),
          new AudioSortByTrack(),
          new AudioSortByContentGroup(),
          new SortByYear(),
          new SortByAddedDate(),
          new SortBySystem(),
        };
    }
  }
}
