using Emulators.LibRetro.Controllers.Hid;
using Emulators.LibRetro.Controllers.Keyboard;
using Emulators.LibRetro.Controllers.Mapping;
using Emulators.LibRetro.Controllers.XInput;
using SharpDX.XInput;
using SharpLib.Hid;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers
{
  public class DeviceProxy
  {
    protected static readonly UserIndex[] XINPUT_USER_INDEXES = new[] { UserIndex.One, UserIndex.Two, UserIndex.Three, UserIndex.Four };

    public List<IMappableDevice> GetDevices(bool connectedOnly)
    {
      List<IMappableDevice> deviceList = new List<IMappableDevice>();
      AddXInputDevices(deviceList, connectedOnly);
      AddHidDevices(deviceList);
      deviceList.Add(new KeyboardController());
      return deviceList;
    }

    public IMappableDevice GetDevice(Guid deviceId, string subDeviceId)
    {
      return GetDevice(deviceId, subDeviceId, GetDevices(false));
    }

    public IMappableDevice GetDevice(Guid deviceId, string subDeviceId, List<IMappableDevice> devices)
    {
      if (deviceId == Guid.Empty)
        return null;
      IMappableDevice device = devices.FirstOrDefault(d => d.DeviceId == deviceId && d.SubDeviceId == subDeviceId);
      if (device == null && deviceId == HidGameControl.DEVICE_ID)
        device = GetDisconnectedHidDevice(subDeviceId);
      return device;
    }

    protected IMappableDevice GetDisconnectedHidDevice(string subDeviceId)
    {
      if (string.IsNullOrEmpty(subDeviceId))
        return null;
      string[] ids = subDeviceId.Split('/');
      if (ids.Length != 2)
        return null;

      ushort vendorId;
      ushort productId;
      if (!ushort.TryParse(ids[0], NumberStyles.None, CultureInfo.InvariantCulture, out vendorId)
        || !ushort.TryParse(ids[1], NumberStyles.None, CultureInfo.InvariantCulture, out productId))
        return null;

      return new HidGameControl(vendorId, productId, null);
    }

    protected void AddXInputDevices(List<IMappableDevice> deviceList, bool connectedOnly)
    {
      foreach (UserIndex userIndex in XINPUT_USER_INDEXES)
      {
        XInputController controller = new XInputController(userIndex);
        if (!connectedOnly || controller.IsConnected())
          deviceList.Add(controller);
      }
    }

    protected void AddHidDevices(List<IMappableDevice> deviceList)
    {
      List<Device> devices = HidUtils.GetHidDevices();
      foreach (Device device in devices)
      {
        if (device.IsGamePad)
          deviceList.Add(new HidGameControl(device.VendorId, device.ProductId, device.FriendlyName));
      }
    }
  }
}
