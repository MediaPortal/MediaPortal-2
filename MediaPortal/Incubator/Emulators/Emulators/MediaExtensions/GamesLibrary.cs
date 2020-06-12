using Emulators.Common.GoodMerge;
using Emulators.Game;
using Emulators.Models.Navigation;
using Emulators.Models.Sorting;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.NavigationModel;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using MediaPortal.UiComponents.Media.Models.Sorting;
using MediaPortal.UiComponents.Media.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaExtensions
{
  public class GamesLibrary : BaseNavigationInitializer
  {
    public static void RegisterOnMediaLibrary()
    {
      MediaNavigationModel.RegisterMediaNavigationInitializer(new GamesLibrary());
    }

    public GamesLibrary()
    {
      _mediaNavigationMode = EmulatorsConsts.MEDIA_NAVIGATION_MODE;
      _mediaNavigationRootState = EmulatorsConsts.WF_MEDIA_NAVIGATION_ROOT_STATE;
      _viewName = EmulatorsConsts.RES_GAMES_VIEW_NAME;
      _necessaryMias = EmulatorsConsts.NECESSARY_GAME_MIAS;
    }

    protected override async Task PrepareAsync()
    {
      await base.PrepareAsync().ConfigureAwait(false);

      AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new GameItem(mi)
      {
        Command = new MethodDelegateCommand(() => ServiceRegistration.Get<IGameLauncher>().LaunchGame(mi))
      };
      _genericPlayableItemCreatorDelegate = picd;

      _defaultScreen = new GamesShowItemsScreenData(picd);
      _availableScreens = new List<AbstractScreenData>
        {
          _defaultScreen,
          new GameFilterByPlatformScreenData(),
          new GameFilterByYearScreenData(),
          new GameFilterByGenreScreenData(),
          new GameFilterByDeveloperScreenData()
        };

      _defaultSorting = new SortByTitle();
      _availableSortings = new List<Sorting>
        {
          _defaultSorting,
          new SortByYear(),
          new SortByRatingDesc()
        };

      var optionalMias = new Guid[]
      {
        GoodMergeAspect.ASPECT_ID
      }.Union(MediaNavigationModel.GetMediaSkinOptionalMIATypes(MediaNavigationMode));

      _customRootViewSpecification = new MediaLibraryQueryViewSpecification(_viewName, null, _necessaryMias, optionalMias, true);
    }
  }
}
