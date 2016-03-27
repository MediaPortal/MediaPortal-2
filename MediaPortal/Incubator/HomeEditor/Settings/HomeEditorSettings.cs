using HomeEditor.Groups;
using MediaPortal.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeEditor.Settings
{
  public class HomeEditorSettings
  {
    List<HomeMenuGroup> _groups = new List<HomeMenuGroup>();

    [Setting(SettingScope.User, DefaultGroups.DEFAULT_OTHERS_GROUP_NAME)]
    public string OthersGroupName
    {
      get;
      set;
    }

    [Setting(SettingScope.User)]
    public List<HomeMenuGroup> Groups
    {
      get { return _groups; }
      set { _groups = value; }
    }
  }
}
