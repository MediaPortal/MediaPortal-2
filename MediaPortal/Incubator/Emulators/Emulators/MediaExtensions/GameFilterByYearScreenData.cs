using Emulators.Common;
using Emulators.Common.Games;
using Emulators.Models.Navigation;
using MediaPortal.UiComponents.Media.FilterCriteria;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaExtensions
{
  public class GameFilterByYearScreenData : AbstractFiltersScreenData<GameFilterItem>
  {
    public GameFilterByYearScreenData()
      : base(EmulatorsConsts.SCREEN_GAMES_SHOW_ITEMS, EmulatorsConsts.RES_FILTER_BY_YEAR_MENU_ITEM, EmulatorsConsts.RES_FILTER_GAME_ITEMS_DISPLAY_LABEL,
          new SimpleMLFilterCriterion(GameAspect.ATTR_YEAR))
    {

    }

    public override AbstractFiltersScreenData<GameFilterItem> Derive()
    {
      return new GameFilterByYearScreenData();
    }
  }
}
