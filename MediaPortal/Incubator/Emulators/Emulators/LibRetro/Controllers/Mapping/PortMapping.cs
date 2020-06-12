using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.Mapping
{
  public class PortMapping
  {
    public int Port { get; set; }
    public Guid DeviceId { get; set; }
    public string SubDeviceId { get; set; }
    public string DeviceName { get; set; }

    public void SetDevice(IMappableDevice device)
    {
      DeviceId = device.DeviceId;
      SubDeviceId = device.SubDeviceId;
      DeviceName = device.DeviceName;
    }
  }
}
