using SharpLib.Hid;
using SharpLib.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.Hid
{
  static class HidUtils
  {
    public static List<Device> GetHidDevices()
    {
      List<Device> devices = new List<Device>();
      //Get our list of devices
      RAWINPUTDEVICELIST[] ridList = null;
      uint deviceCount = 0;
      int res = Function.GetRawInputDeviceList(ridList, ref deviceCount, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)));
      if (res == -1)
      {
        //Just give up then
        return devices;
      }

      ridList = new RAWINPUTDEVICELIST[deviceCount];
      res = Function.GetRawInputDeviceList(ridList, ref deviceCount, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)));
      if (res != deviceCount)
      {
        //Just give up then
        return devices;
      }

      foreach (RAWINPUTDEVICELIST device in ridList)
      {
        Device hidDevice;
        //Try create our HID device.
        try
        {
          hidDevice = new Device(device.hDevice);
        }
        catch /*(System.Exception ex)*/
        {
          //Just skip that device then
          continue;
        }
        devices.Add(hidDevice);
      }
      return devices;
    }

    /// <summary>
    /// Provide the type for the usage corresponding to the given usage page.
    /// </summary>
    /// <param name="aUsagePage"></param>
    /// <returns></returns>
    public static Type UsageType(SharpLib.Hid.UsagePage aUsagePage)
    {
      switch (aUsagePage)
      {
        case SharpLib.Hid.UsagePage.GenericDesktopControls:
          return typeof(SharpLib.Hid.Usage.GenericDesktop);

        case SharpLib.Hid.UsagePage.Consumer:
          return typeof(SharpLib.Hid.Usage.ConsumerControl);

        case SharpLib.Hid.UsagePage.WindowsMediaCenterRemoteControl:
          return typeof(SharpLib.Hid.Usage.WindowsMediaCenterRemoteControl);

        case SharpLib.Hid.UsagePage.Telephony:
          return typeof(SharpLib.Hid.Usage.TelephonyDevice);

        case SharpLib.Hid.UsagePage.SimulationControls:
          return typeof(SharpLib.Hid.Usage.SimulationControl);

        case SharpLib.Hid.UsagePage.GameControls:
          return typeof(SharpLib.Hid.Usage.GameControl);

        case SharpLib.Hid.UsagePage.GenericDeviceControls:
          return typeof(SharpLib.Hid.Usage.GenericDevice);

        default:
          return null;
      }
    }
  }
}
