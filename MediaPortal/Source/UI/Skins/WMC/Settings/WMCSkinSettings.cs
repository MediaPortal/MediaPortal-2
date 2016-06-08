using MediaPortal.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.WMCSkin.Settings
{
  public class WMCSkinSettings
  {
    [Setting(SettingScope.User, true)]
    public bool EnableFanart { get; set; }
  }
}
