#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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

using System;
using System.Collections.Generic;
using MediaPortal.Common.Commands;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using MediaPortal.UiComponents.Media.Models.Sorting;
using MediaPortal.UiComponents.Media.Views;

namespace MediaPortal.UiComponents.Media.Models.NavigationModel
{
  class AudioNavigationInitializer : IMediaNavigationInitializer
  {
    public string MediaNavigationMode
    {
      get { return Models.MediaNavigationMode.Audio; }
    }

    public Guid MediaNavigationRootState
    {
      get { return Consts.WF_STATE_ID_AUDIO_NAVIGATION_ROOT; }
    }

    public void InitMediaNavigation(out string mediaNavigationMode, out NavigationData navigationData)
    {
      IEnumerable<Guid> skinDependentOptionalMIATypeIDs = MediaNavigationModel.GetMediaSkinOptionalMIATypes(MediaNavigationMode);
      AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new AudioItem(mi)
      {
        Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi))
      };
      ViewSpecification rootViewSpecification = new MediaLibraryQueryViewSpecification(Consts.RES_AUDIO_VIEW_NAME,
        null, Consts.NECESSARY_AUDIO_MIAS, skinDependentOptionalMIATypeIDs, true)
      {
        MaxNumItems = Consts.MAX_NUM_ITEMS_VISIBLE
      };
      AbstractScreenData filterByArtist = new AudioFilterByArtistScreenData();

      ICollection<AbstractScreenData> availableScreens = new List<AbstractScreenData>
        {
          new AudioShowItemsScreenData(picd),
          filterByArtist,
          // C# doesn't like it to have an assignment inside a collection initializer
          new AudioFilterByAlbumScreenData(),
          new AudioFilterByGenreScreenData(),
          new AudioFilterByDecadeScreenData(),
          new AudioFilterBySystemScreenData(),
          new AudioSimpleSearchScreenData(picd),
        };
      Sorting.Sorting sortByAlbumTrack = new AudioSortByAlbumTrack();
      ICollection<Sorting.Sorting> availableSortings = new List<Sorting.Sorting>
        {
          sortByAlbumTrack,
          new SortByTitle(),
          new AudioSortByFirstGenre(),
          new AudioSortByFirstArtist(),
          new AudioSortByAlbum(),
          new AudioSortByTrack(),
          new SortByYear(),
          new SortBySystem(),
        };
      navigationData = new NavigationData(null, Consts.RES_AUDIO_VIEW_NAME, MediaNavigationRootState,
        MediaNavigationRootState, rootViewSpecification, filterByArtist, availableScreens, sortByAlbumTrack)
      {
        AvailableSortings = availableSortings
      };
      mediaNavigationMode = MediaNavigationMode;
    }
  }
}
