using MediaPortal.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Settings
{
  public class EmulatorsSettings
  {
    [Setting(SettingScope.User, false)]
    public bool MinimiseOnGameStart
    {
      get;
      set;
    }
  }
}