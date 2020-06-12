using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.Mapping
{
  public interface IMappableDevice
  {
    Guid DeviceId { get; }
    string SubDeviceId { get; }
    string DeviceName { get; }
    RetroPadMapping DefaultMapping { get; }
    IDeviceMapper CreateMapper();
    void Map(RetroPadMapping mapping);
  }
}
