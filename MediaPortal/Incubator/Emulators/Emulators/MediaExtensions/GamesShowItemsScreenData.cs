using MediaPortal.UiComponents.Media.Models.ScreenData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaExtensions
{
  public class GamesShowItemsScreenData : AbstractItemsScreenData
  {
    public GamesShowItemsScreenData(PlayableItemCreatorDelegate playableItemCreator) :
      base(EmulatorsConsts.SCREEN_GAMES_SHOW_ITEMS, EmulatorsConsts.RES_SHOW_ALL_GAME_ITEMS_MENU_ITEM,
        EmulatorsConsts.RES_SHOW_ALL_GAME_NAVBAR_DISPLAY_LABEL, playableItemCreator, true)
    {
    }

    public override AbstractItemsScreenData Derive()
    {
      return new GamesShowItemsScreenData(PlayableItemCreator);
    }
  }
}
