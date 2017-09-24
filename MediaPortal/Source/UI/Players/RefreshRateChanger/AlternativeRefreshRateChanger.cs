#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Runtime.InteropServices;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Plugins.RefreshRateChanger
{
  public class AlternativeRefreshRateChanger : IRefreshRateChanger
  {
    #region Consts

    public const int CCHDEVICENAME = 32;
    public const int CCHFORMNAME = 32;
    private readonly uint _displayIndex;

    #endregion

    public AlternativeRefreshRateChanger(uint displayIndex)
    {
      _displayIndex = displayIndex;
    }

    [Flags]
    public enum DISPLAY_DEVICE_StateFlags : uint
    {
      None = 0x00000000,
      DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001,
      DISPLAY_DEVICE_MULTI_DRIVER = 0x00000002,
      DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004,
      DISPLAY_DEVICE_MIRRORING_DRIVER = 0x00000008,
      DISPLAY_DEVICE_VGA_COMPATIBLE = 0x00000010,
      DISPLAY_DEVICE_REMOVABLE = 0x00000020,
      DISPLAY_DEVICE_MODESPRUNED = 0x08000000,
      DISPLAY_DEVICE_REMOTE = 0x04000000,
      DISPLAY_DEVICE_DISCONNECT = 0x02000000
    }

    public enum EnumDisplaySettings_EnumMode : uint
    {
      ENUM_CURRENT_SETTINGS = uint.MaxValue,
      ENUM_REGISTRY_SETTINGS = uint.MaxValue - 1
    }

    public enum ChangeDisplaySettings_Result
    {
      DISP_CHANGE_SUCCESSFUL = 0,
      DISP_CHANGE_RESTART = 1,
      DISP_CHANGE_FAILED = -1,
      DISP_CHANGE_BADMODE = -2,
      DISP_CHANGE_NOTUPDATED = -3,
      DISP_CHANGE_BADFLAGS = -4,
      DISP_CHANGE_BADPARAM = -5,
      DISP_CHANGE_BADDUALVIEW = -6
    }

    [Flags]
    public enum ChangeDisplaySettings_Flags : uint
    {
      None = 0x00000000,
      CDS_UPDATEREGISTRY = 0x00000001,
      CDS_TEST = 0x00000002,
      CDS_FULLSCREEN = 0x00000004,
      CDS_GLOBAL = 0x00000008,
      CDS_SET_PRIMARY = 0x00000010,
      CDS_VIDEOPARAMETERS = 0x00000020,
      CDS_RESET = 0x40000000,
      CDS_NORESET = 0x10000000
    }

    [Flags]
    public enum DEVMODE_Fields : uint
    {
      None = 0x00000000,
      DM_POSITION = 0x00000020,
      DM_BITSPERPEL = 0x00040000,
      DM_PELSWIDTH = 0x00080000,
      DM_PELSHEIGHT = 0x00100000,
      DM_DISPLAYFLAGS = 0x00200000,
      DM_DISPLAYFREQUENCY = 0x00400000
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTL
    {
      public POINTL(int x, int y)
      {
        this.x = x;
        this.y = y;
      }

      public int x;
      public int y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class DISPLAY_DEVICE
    {
      public uint cb = (uint)Marshal.SizeOf(typeof(DISPLAY_DEVICE));
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName = "";
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString = "";
      public DISPLAY_DEVICE_StateFlags StateFlags = 0;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID = "";
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey = "";
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class DEVMODE_Display
    {
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)] public string dmDeviceName = null;
      public ushort dmSpecVersion = 0;
      public ushort dmDriverVersion = 0;
      public ushort dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE_Display));
      public ushort dmDriverExtra = 0;
      public DEVMODE_Fields dmFields = DEVMODE_Fields.None;
      public POINTL dmPosition = new POINTL();
      public uint dmDisplayOrientation = 0;
      public uint dmDisplayFixedOutput = 0;
      public short dmColor = 0;
      public short dmDuplex = 0;
      public short dmYResolution = 0;
      public short dmTTOption = 0;
      public short dmCollate = 0;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)] public string dmFormName = null;
      public ushort dmLogPixels = 0;
      public uint dmBitsPerPel = 0;
      public uint dmPelsWidth = 0;
      public uint dmPelsHeight = 0;
      public uint dmDisplayFlags = 0;
      public uint dmDisplayFrequency = 0;
      public uint dmICMMethod = 0;
      public uint dmICMIntent = 0;
      public uint dmMediaType = 0;
      public uint dmDitherType = 0;
      public uint dmReserved1 = 0;
      public uint dmReserved2 = 0;
      public uint dmPanningWidth = 0;
      public uint dmPanningHeight = 0;
    }

    #region DLL imports

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int EnumDisplaySettingsEx([In] string lpszDeviceName, [In] EnumDisplaySettings_EnumMode iModeNum,
      [In] [Out] DEVMODE_Display lpDevMode, [In] uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int EnumDisplayDevices([In] string lpDevice, [In] uint iDevNum,
      [In] [Out] DISPLAY_DEVICE lpDisplayDevice, [In] uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern ChangeDisplaySettings_Result
      ChangeDisplaySettingsEx([In] string lpszDeviceName, [In] DEVMODE_Display
          lpDevMode, [In] IntPtr hwnd,
        [In] ChangeDisplaySettings_Flags dwFlags, [In] IntPtr lParam);

    #endregion

    public double GetRefreshRate()
    {
      return 0;
    }

    public void SetRefreshRate(double refreshRate)
    {
      DISPLAY_DEVICE displayDevice = new DISPLAY_DEVICE();
      displayDevice.cb = (ushort)Marshal.SizeOf(displayDevice);
      DEVMODE_Display devMode = new DEVMODE_Display();
      devMode.dmSize = (ushort)Marshal.SizeOf(devMode);
      ChangeDisplaySettings_Result displayResult = ChangeDisplaySettings_Result.DISP_CHANGE_SUCCESSFUL;

      int result = EnumDisplayDevices(null, _displayIndex, displayDevice, 0);
  
      if (result != 0)
      {
        result = EnumDisplaySettingsEx(displayDevice.DeviceName, 0, devMode, 2);
        if (result != 0)
        {
          result = EnumDisplaySettingsEx(displayDevice.DeviceName, EnumDisplaySettings_EnumMode.ENUM_CURRENT_SETTINGS, devMode, 2); // EDS_RAWMODE = 2
          if (result != 0)
          {
            // Get current Value
            uint Width = devMode.dmPelsWidth;
            uint Height = devMode.dmPelsHeight;

            devMode.dmFields = (DEVMODE_Fields.DM_BITSPERPEL | DEVMODE_Fields.DM_PELSWIDTH |
                                DEVMODE_Fields.DM_PELSHEIGHT | DEVMODE_Fields.DM_DISPLAYFREQUENCY);
            devMode.dmBitsPerPel = 32;
            devMode.dmPelsWidth = Width;
            devMode.dmPelsHeight = Height;
            devMode.dmDisplayFrequency = (uint)refreshRate;

            // First set settings
            ChangeDisplaySettings_Result r = ChangeDisplaySettingsEx(displayDevice.DeviceName, devMode,
              IntPtr.Zero,
              (ChangeDisplaySettings_Flags
                 .CDS_NORESET |
               ChangeDisplaySettings_Flags
                 .CDS_UPDATEREGISTRY),
              IntPtr.Zero);

            if (r != displayResult)
            {
              ServiceRegistration.Get<ILogger>().Debug("AlternativeRefreshRateChanger.SetRefreshRate() failed");
            }
            else
            {
              // Apply settings
              r = ChangeDisplaySettingsEx(null, null, IntPtr.Zero, 0, IntPtr.Zero);
              ServiceRegistration.Get<ILogger>().Debug("AlternativeRefreshRateChanger.SetRefreshRate() set OK!!");
            }
          }
          else
          {
            ServiceRegistration.Get<ILogger>().Debug("AlternativeRefreshRateChanger.SetRefreshRate() third result not ok!");
          }
        }
        else
        {
          ServiceRegistration.Get<ILogger>().Debug("AlternativeRefreshRateChanger.SetRefreshRate() second result not ok!");
        }
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Debug("AlternativeRefreshRateChanger.SetRefreshRate() first result not ok!");
      }

      }

    public void Dispose()
    {
      
    }
  }
}
