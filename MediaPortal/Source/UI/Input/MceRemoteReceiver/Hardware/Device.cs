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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MediaPortal.Plugins.MceRemoteReceiver.Hardware
{
  public abstract class Device
  {
    #region Members

    protected Guid _deviceClass;
    protected FileStream _deviceStream;
    protected byte[] _deviceBuffer;
    internal DeviceWatcher _deviceWatcher;
    private static bool _logVerbose;
    internal IEnumerable<string> _eHomeTransceivers;

    #endregion Members

    #region Properties

    public static List<eHomeTransceiver> Transceivers { get; private set; }

    #endregion

    #region Ctor

    static Device()
    {
      Transceivers = new List<eHomeTransceiver>();
    }

    #endregion

    #region Implementation

    protected void OnDeviceArrival(object sender, EventArgs e)
    {
      if (_deviceStream != null)
      {
        return;
      }

      Open();

      if (DeviceArrival != null)
      {
        DeviceArrival(sender, e);
      }
    }

    protected abstract void Open();

    protected void OnDeviceRemoval(object sender, EventArgs e)
    {
      if (_deviceStream == null)
      {
        return;
      }

      try
      {
        _deviceStream.Close();
      }
      catch (IOException)
      {
        // we are closing the stream so ignore this
      }
      finally
      {
        _deviceStream = null;
      }

      if (DeviceRemoval != null)
      {
        DeviceRemoval(sender, e);
      }
    }

    protected string FindDevice(Guid classGuid)
    {
      if (Transceivers != null)
        _eHomeTransceivers = Transceivers.Select(t => t.DeviceID);

      IntPtr handle = SetupDiGetClassDevs(ref classGuid, 0, 0, 0x12);

      string devicePath = null;

      if (handle.ToInt32() == -1)
      {
        throw new Exception(string.Format("Failed in call to SetupDiGetClassDevs ({0})", GetLastError()));
      }

      for (int deviceIndex = 0;; deviceIndex++)
      {
        DeviceInfoData deviceInfoData = new DeviceInfoData();
        deviceInfoData.Size = Marshal.SizeOf(deviceInfoData);

        if (SetupDiEnumDeviceInfo(handle, deviceIndex, ref deviceInfoData) == false)
        {
          // out of devices or do we have an error?
          if (GetLastError() != 0x103 && GetLastError() != 0x7E)
          {
            SetupDiDestroyDeviceInfoList(handle);
            throw new Exception(string.Format("Failed in call to SetupDiEnumDeviceInfo ({0})", GetLastError()));
          }

          SetupDiDestroyDeviceInfoList(handle);
          break;
        }

        DeviceInterfaceData deviceInterfaceData = new DeviceInterfaceData();
        deviceInterfaceData.Size = Marshal.SizeOf(deviceInterfaceData);

        if (SetupDiEnumDeviceInterfaces(handle, ref deviceInfoData, ref classGuid, 0, ref deviceInterfaceData) == false)
        {
          SetupDiDestroyDeviceInfoList(handle);
          throw new Exception(string.Format("Failed in call to SetupDiEnumDeviceInterfaces ({0})", GetLastError()));
        }

        uint cbData = 0;

        if (SetupDiGetDeviceInterfaceDetail(handle, ref deviceInterfaceData, 0, 0, ref cbData, 0) == false &&
            cbData == 0)
        {
          SetupDiDestroyDeviceInfoList(handle);
          throw new Exception(string.Format("Failed in call to SetupDiGetDeviceInterfaceDetail ({0})", GetLastError()));
        }

        DeviceInterfaceDetailData deviceInterfaceDetailData = new DeviceInterfaceDetailData();
        deviceInterfaceDetailData.Size = 5;

        if (
          SetupDiGetDeviceInterfaceDetail(handle, ref deviceInterfaceData, ref deviceInterfaceDetailData, cbData, 0, 0) ==
          false)
        {
          SetupDiDestroyDeviceInfoList(handle);
          throw new Exception(string.Format("Failed in call to SetupDiGetDeviceInterfaceDetail ({0})", GetLastError()));
        }

        if (LogVerbose)
        {
          MceRemoteReceiver.LogInfo("Found: {0}", deviceInterfaceDetailData.DevicePath);
        }

        foreach (string deviceId in _eHomeTransceivers)
        {
          if ((deviceInterfaceDetailData.DevicePath.IndexOf(deviceId) != -1) ||
              (deviceInterfaceDetailData.DevicePath.StartsWith(@"\\?\hid#irdevice&col01#2")) ||
              // eHome Infrared Transceiver List XP
              (deviceInterfaceDetailData.DevicePath.StartsWith(@"\\?\hid#irdevicev2&col01#2")))
            // Microsoft/Philips 2005 (Vista)
          {
            SetupDiDestroyDeviceInfoList(handle);
            devicePath = deviceInterfaceDetailData.DevicePath;
          }
        }
        if (devicePath != null)
        {
          break;
        }
      }
      return devicePath;
    }

    #endregion Implementation

    #region Interop

    [DllImport("kernel32", SetLastError = true)]
    protected static extern SafeFileHandle CreateFile(string FileName,
                                                      [MarshalAs(UnmanagedType.U4)] FileAccess DesiredAccess,
                                                      [MarshalAs(UnmanagedType.U4)] FileShare ShareMode,
                                                      uint SecurityAttributes,
                                                      [MarshalAs(UnmanagedType.U4)] FileMode CreationDisposition,
                                                      FileFlag FlagsAndAttributes, int hTemplateFile);

    [DllImport("kernel32", SetLastError = true)]
    protected static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32", SetLastError = true)]
    protected static extern int GetLastError();

    protected enum FileFlag
    {
      Overlapped = 0x40000000,
    }

    [StructLayout(LayoutKind.Sequential)]
    protected struct DeviceInfoData
    {
      public int Size;
      public Guid Class;
      public uint DevInst;
      public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    protected struct DeviceInterfaceData
    {
      public int Size;
      public Guid Class;
      public uint Flags;
      public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    protected struct DeviceInterfaceDetailData
    {
      public int Size;

      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string DevicePath;
    }

    [DllImport("hid")]
    protected static extern void HidD_GetHidGuid(ref Guid guid);

    [DllImport("setupapi", SetLastError = true)]
    protected static extern IntPtr SetupDiGetClassDevs(ref Guid guid, int Enumerator, int hwndParent, int Flags);

    [DllImport("setupapi", SetLastError = true)]
    protected static extern bool SetupDiEnumDeviceInfo(IntPtr handle, int Index, ref DeviceInfoData deviceInfoData);

    [DllImport("setupapi", SetLastError = true)]
    protected static extern bool SetupDiEnumDeviceInterfaces(IntPtr handle, ref DeviceInfoData deviceInfoData,
                                                             ref Guid guidClass, int MemberIndex,
                                                             ref DeviceInterfaceData deviceInterfaceData);

    [DllImport("setupapi", SetLastError = true)]
    protected static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr handle,
                                                                 ref DeviceInterfaceData deviceInterfaceData,
                                                                 int unused1, int unused2, ref uint requiredSize,
                                                                 int unused3);

    [DllImport("setupapi", SetLastError = true)]
    protected static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr handle,
                                                                 ref DeviceInterfaceData deviceInterfaceData,
                                                                 ref DeviceInterfaceDetailData deviceInterfaceDetailData,
                                                                 uint detailSize, int unused1, int unused2);

    [DllImport("setupapi")]
    protected static extern bool SetupDiDestroyDeviceInfoList(IntPtr handle);

    #endregion Interop

    #region Events

    public static DeviceEventHandler DeviceArrival = null;
    public static DeviceEventHandler DeviceRemoval = null;

    #endregion Events

    #region Properties

    public static Guid HidGuid
    {
      get
      {
        Guid guid = new Guid();

        // ask the OS for the class (GUID) that represents human input devices
        HidD_GetHidGuid(ref guid);

        return guid;
      }
    }

    public static bool LogVerbose
    {
      set { _logVerbose = value; }
      get { return _logVerbose; }
    }

    #endregion Properties
  }
}