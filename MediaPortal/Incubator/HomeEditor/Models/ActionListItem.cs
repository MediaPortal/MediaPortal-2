using HomeEditor.Groups;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.SkinBase.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeEditor.Models
{
  public class ActionListItem : ListItem
  {
    HomeMenuAction _action;

    public ActionListItem(HomeMenuAction action)
    {
      _action = action;
      SetLabel(Consts.KEY_NAME, LocalizationHelper.CreateResourceString(action.DisplayName));
    }

    public HomeMenuAction GroupAction
    {
      get { return _action; }
    }
  }
}
