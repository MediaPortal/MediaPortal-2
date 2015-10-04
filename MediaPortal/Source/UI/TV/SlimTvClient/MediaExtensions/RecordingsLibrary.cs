#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.MetadataExtractors.Aspects;
using MediaPortal.Plugins.SlimTv.Client.Models.Navigation;
using MediaPortal.Plugins.SlimTv.Client.Models.ScreenData;
using MediaPortal.Plugins.SlimTv.Client.TvHandler;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.NavigationModel;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using MediaPortal.UiComponents.Media.Models.Sorting;

namespace MediaPortal.Plugins.SlimTv.Client.MediaExtensions
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
    }

    protected override void Prepare()
    {
      base.Prepare();

      AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new RecordingItem(mi) { Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi)) };

      _defaultScreen = new VideosShowItemsScreenData(picd);
      _availableScreens = new List<AbstractScreenData>
        {
          _defaultScreen,
          new RecordingFilterByNameScreenData(),
          new RecordingsFilterByChannelScreenData(),
          //new VideosFilterByActorScreenData(),
          //new VideosFilterByDirectorScreenData(),
          //new VideosFilterByWriterScreenData(),
          //new VideosFilterByGenreScreenData(),
          //new VideosFilterByYearScreenData(),
          //new VideosFilterBySystemScreenData(),
          new VideosSimpleSearchScreenData(picd),
        };

      _defaultSorting = new SortByRecordingDateDesc();
      _availableSortings = new List<Sorting>
        {
          _defaultSorting,
          new SortByTitle(),
          //new VideoSortByFirstGenre(),
          //new VideoSortByDuration(),
          //new VideoSortByFirstActor(),
          //new VideoSortByFirstDirector(),
          //new VideoSortByFirstWriter(),
          //new VideoSortBySize(),
          //new VideoSortByAspectRatio(),
          //new SortBySystem(),
        };

      var optionalMias = new[]
      {
        MovieAspect.ASPECT_ID,
        SeriesAspect.ASPECT_ID,
        AudioAspect.ASPECT_ID,
        VideoAspect.ASPECT_ID,
        ImageAspect.ASPECT_ID
      }.Union(MediaNavigationModel.GetMediaSkinOptionalMIATypes(MediaNavigationMode));

      _customRootViewSpecification = new StackingViewSpecification(_viewName, null, _necessaryMias, optionalMias, true)
      {
        MaxNumItems = Consts.MAX_NUM_ITEMS_VISIBLE
      };
    }
  }
}
