using Emulators.Common.Emulators;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.Settings
{
  public class CommonSettings
  {
    protected List<EmulatorConfiguration> _configuredEmulators = new List<EmulatorConfiguration>();

    [Setting(SettingScope.Global)]
    public List<EmulatorConfiguration> ConfiguredEmulators
    {
      get { return _configuredEmulators; }
      set { _configuredEmulators = value; }
    }
  }
}
