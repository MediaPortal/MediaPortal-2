using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.Mapping
{
  public interface IDeviceMapper
  {
    bool SupportsDeadZone { get; }
    DeviceInput GetPressedInput();
  }
}
