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
