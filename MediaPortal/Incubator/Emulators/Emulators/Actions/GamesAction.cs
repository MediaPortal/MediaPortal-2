using MediaPortal.UiComponents.Media.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Actions
{
  public class GamesAction : VisibilityDependsOnServerConnectStateAction
  {
    #region Consts

    public static readonly Guid GAMES_CONTRIBUTOR_MODEL_ID = new Guid("9929690F-BFB9-47CF-8FFE-11617D3B8B44");
    public const string RES_GAMES_MENU_ITEM = "[Emulators.MenuItem]";

    #endregion

    public GamesAction() :
      base(true, EmulatorsConsts.WF_MEDIA_NAVIGATION_ROOT_STATE, RES_GAMES_MENU_ITEM)
    { }
  }
}
