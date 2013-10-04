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

using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.MetadataExtractors.Aspects;
using MediaPortal.Plugins.SlimTv.Client.Models.ScreenData;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.UiComponents.Media.Models.NavigationModel;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using MediaPortal.UiComponents.Media.Models.Sorting;

namespace MediaPortal.Plugins.SlimTv.Client.TvHandler
{
  public class RecordingsLibrary : BaseNavigationInitializer
  {
    public static void RegisterOnMediaLibrary()
    {
      MediaNavigationModel.RegisterMediaNavigationInitializer(new RecordingsLibrary());
      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(RecordingAspect.Metadata);
    }

    public RecordingsLibrary()
    {
      _mediaNavigationMode = SlimTvConsts.MEDIA_NAVIGATION_MODE;
      _mediaNavigationRootState = SlimTvConsts.WF_MEDIA_NAVIGATION_ROOT_STATE;
      _viewName = SlimTvConsts.RES_RECORDINGS_VIEW_NAME;
      _necessaryMias = SlimTvConsts.NECESSARY_RECORDING_MIAS;

      AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new VideoItem(mi) { Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi)) };

      _defaultScreen = new RecordingFilterByNameScreenData();
      _availableScreens = new List<AbstractScreenData>
        {
          new VideosShowItemsScreenData(picd),
          _defaultScreen,
          new RecordingsFilterByChannelScreenData(),
          new VideosFilterByActorScreenData(),
          new VideosFilterByDirectorScreenData(),
          new VideosFilterByWriterScreenData(),
          new VideosFilterByGenreScreenData(),
          new VideosFilterByYearScreenData(),
          new VideosFilterBySystemScreenData(),
          new VideosSimpleSearchScreenData(picd),
        };

      _defaultSorting = new SortByRecordingDateDesc();
      _availableSortings = new List<Sorting>
        {
          _defaultSorting,
          new SortByTitle(),
          new VideoSortByFirstGenre(),
          new VideoSortByDuration(),
          new VideoSortByFirstActor(),
          new VideoSortByFirstDirector(),
          new VideoSortByFirstWriter(),
          new VideoSortBySize(),
          new VideoSortByAspectRatio(),
          new SortBySystem(),
        };
    }
  }
}
