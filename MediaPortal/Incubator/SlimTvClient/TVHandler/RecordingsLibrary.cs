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
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.MetadataExtractors.Aspects;
using MediaPortal.Plugins.SlimTv.Client.Models.ScreenData;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.UiComponents.Media.Models.NavigationModel;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using MediaPortal.UiComponents.Media.Models.Sorting;
using MediaPortal.UiComponents.Media.Views;

namespace MediaPortal.Plugins.SlimTv.Client.TvHandler
{
  public class RecordingsLibrary : IMediaNavigationInitializer
  {
    public static void RegisterOnMediaLibrary()
    {
      MediaNavigationModel.RegisterMediaNavigationInitializer(new RecordingsLibrary());
      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(RecordingAspect.Metadata);
    }

    public string MediaNavigationMode
    {
      get { return SlimTvConsts.MEDIA_NAVIGATION_MODE; }
    }

    public Guid MediaNavigationRootState
    {
      get { return SlimTvConsts.WF_MEDIA_NAVIGATION_ROOT_STATE; }
    }

    public void InitMediaNavigation(out string mediaNavigationMode, out NavigationData navigationData)
    {
      IEnumerable<Guid> skinDependentOptionalMIATypeIDs = MediaNavigationModel.GetMediaSkinOptionalMIATypes(MediaNavigationMode);
      AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new VideoItem(mi)
        {
          Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi))
        };
      ViewSpecification rootViewSpecification = new MediaLibraryQueryViewSpecification(SlimTvConsts.RES_RECORDINGS_VIEW_NAME,
        null, SlimTvConsts.NECESSARY_RECORDING_MIAS, skinDependentOptionalMIATypeIDs, true)
        {
          MaxNumItems = Consts.MAX_NUM_ITEMS_VISIBLE
        };
      AbstractScreenData defaultScreen = new RecordingFilterByNameScreenData();
      ICollection<AbstractScreenData> availableScreens = new List<AbstractScreenData>
        {
          // C# doesn't like it to have an assignment inside a collection initializer
          defaultScreen,
          new VideosShowItemsScreenData(picd),
          new RecordingsFilterByChannelScreenData(),
          new VideosFilterByActorScreenData(),
          new VideosFilterByGenreScreenData(),
          new VideosFilterByYearScreenData(),
          new VideosFilterBySystemScreenData(),
          new VideosSimpleSearchScreenData(picd),
        };
      Sorting defaultSorting = new SortByRecordingDateDesc();
      ICollection<Sorting> availableSortings = new List<Sorting>
        {
          defaultSorting,
          new SortByTitle(),
          new VideoSortByFirstGenre(),
          new VideoSortByDuration(),
          new VideoSortByDirector(),
          new VideoSortByFirstActor(),
          new VideoSortBySize(),
          new VideoSortByAspectRatio(),
          new SortBySystem(),
        };

      navigationData = new NavigationData(null, Consts.RES_MOVIES_VIEW_NAME, MediaNavigationRootState,
        MediaNavigationRootState, rootViewSpecification, defaultScreen, availableScreens, defaultSorting)
        {
          AvailableSortings = availableSortings
        };
      mediaNavigationMode = MediaNavigationMode;
    }
  }
}
