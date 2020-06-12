using Emulators.LibRetro.Controllers.Mapping;
using MediaPortal.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Settings
{
  public class LibRetroMappingSettings
  {
    protected List<RetroPadMapping> _mappings;
    protected List<PortMapping> _ports;

    [Setting(SettingScope.User, null)]
    public List<RetroPadMapping> Mappings
    {
      get
      {
        if (_mappings == null)
          _mappings = new List<RetroPadMapping>();
        return _mappings;
      }
      set { _mappings = value; }
    }

    [Setting(SettingScope.User, null)]
    public List<PortMapping> Ports
    {
      get
      {
        if (_ports == null)
          _ports = new List<PortMapping>();
        return _ports;
      }
      set { _ports = value; }
    }
  }
}
