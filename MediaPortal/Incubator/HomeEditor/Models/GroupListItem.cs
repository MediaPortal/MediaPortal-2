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
  public class GroupListItem : ListItem
  {
    protected HomeMenuGroup _group;

    public GroupListItem(HomeMenuGroup group)
    {
      _group = group;
      SetLabel(Consts.KEY_NAME, LocalizationHelper.CreateResourceString(group.DisplayName));
    }

    public HomeMenuGroup Group
    {
      get { return _group; }
    }
  }
}
