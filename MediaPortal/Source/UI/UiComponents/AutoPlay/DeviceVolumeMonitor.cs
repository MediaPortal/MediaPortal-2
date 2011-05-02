#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace MediaPortal.UiComponents.AutoPlay
{

  /// <summary>
  /// Delegate used to implement the class events
  /// </summary>
  public delegate void DeviceVolumeAction(int aMask);

  /// <summary>
  /// Custom exception
  /// </summary>
  public class DeviceVolumeMonitorException : ApplicationException
  {
    public DeviceVolumeMonitorException(string aMessage) : base(aMessage) { }
  }

  internal class _DeviceVolumeMonitor : NativeWindow
  {
    #region Protectded fields

    protected DeviceVolumeMonitor _monitor;

    #endregion

    #region API constants and structures
    /// <summary>
    /// Constant defined for the WM_DEVICECHANGE message in WinUser.h
    /// </summary>
    const int WM_DEVICECHANGE = 0x0219;

    /// <summary>
    /// Constants and structs defined in DBT.h
    /// </summary>
    public enum DeviceEvent
    {
      Arrival = 0x8000,           // DBT_DEVICEARRIVAL
      QueryRemove = 0x8001,       // DBT_DEVICEQUERYREMOVE
      QueryRemoveFailed = 0x8002, // DBT_DEVICEQUERYREMOVEFAILED
      RemovePending = 0x8003,     // DBT_DEVICEREMOVEPENDING
      RemoveComplete = 0x8004,    // DBT_DEVICEREMOVECOMPLETE
      Specific = 0x8005,          // DBT_DEVICEREMOVECOMPLETE
      Custom = 0x8006             // DBT_CUSTOMEVENT
    }

    public enum DeviceType
    {
      OEM = 0x00000000,           // DBT_DEVTYP_OEM
      DeviceNode = 0x00000001,    // DBT_DEVTYP_DEVNODE
      Volume = 0x00000002,        // DBT_DEVTYP_VOLUME
      Port = 0x00000003,          // DBT_DEVTYP_PORT
      Net = 0x00000004            // DBT_DEVTYP_NET
    }

    public enum VolumeFlags
    {
      Media = 0x0001,             // DBTF_MEDIA
      Net = 0x0002                // DBTF_NET
    }

    public struct BroadcastHeader   // _DEV_BROADCAST_HDR 
    {
      public int Size;            // dbch_size
      public DeviceType Type;     // dbch_devicetype
      private int Reserved;       // dbch_reserved
    }

    public struct Volume            // _DEV_BROADCAST_VOLUME 
    {
      public int Size;            // dbcv_size
      public DeviceType Type;     // dbcv_devicetype
      private int Reserved;       // dbcv_reserved
      public int Mask;            // dbcv_unitmask
      public int Flags;           // dbcv_flags
    }

    #endregion

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="aMonitor">A DeviceVolumeMonitor instance that ownes the object</param>
    public _DeviceVolumeMonitor(DeviceVolumeMonitor aMonitor)
    {
      _monitor = aMonitor;
    }

    /// <summary>
    /// WndProc method that traps all messages sent to the Handle
    /// </summary>
    /// <param name="aMessage">A Windows message</param>
    protected override void WndProc(ref Message aMessage)
    {
      BroadcastHeader lBroadcastHeader;
      Volume lVolume;

      base.WndProc(ref aMessage);
      if (aMessage.Msg == WM_DEVICECHANGE && _monitor.Enabled)
      {
        DeviceEvent lEvent = (DeviceEvent) aMessage.WParam.ToInt32();
        if (lEvent == DeviceEvent.Arrival || lEvent == DeviceEvent.RemoveComplete)
        {
          lBroadcastHeader = (BroadcastHeader) Marshal.PtrToStructure(aMessage.LParam, typeof(BroadcastHeader));
          if (lBroadcastHeader.Type == DeviceType.Volume)
          {
            lVolume = (Volume) Marshal.PtrToStructure(aMessage.LParam, typeof(Volume));
            if ((lVolume.Flags & (int) VolumeFlags.Media) != 0)
              _monitor.TriggerEvents(lEvent == DeviceEvent.Arrival, lVolume.Mask);
          }
        }
      }
    }
  }

  /// <summary>
  /// DeviceVolumeMonitor class
  /// Derived from NativeWindow and implements IDisposable
  /// Built to monitor volume insertion and removal from devices which implement software ejection fro removable media
  /// </summary>
  public class DeviceVolumeMonitor : IDisposable
  {
    #region Protected fields

    internal _DeviceVolumeMonitor _internalMonitorWindow;
    protected IntPtr _handle;
    protected bool _disposed;
    protected bool _enabled;
    protected bool _async;

    #endregion

    /// <summary>
    /// Invoked on Volume insertion.
    /// </summary>
    public event DeviceVolumeAction OnVolumeInserted;

    /// <summary>
    /// Invoked on Volume removal.
    /// </summary>
    public event DeviceVolumeAction OnVolumeRemoved;

    /// <summary>
    /// Enables or disables message trapping.
    /// </summary>
    public bool Enabled
    {
      get { return _enabled; }
      set
      {
        if (!_enabled && value)
        {
          if (_internalMonitorWindow.Handle == IntPtr.Zero) { _internalMonitorWindow.AssignHandle(_handle); }
          _enabled = true;
        }
        if (_enabled && !value)
        {
          if (_internalMonitorWindow.Handle != IntPtr.Zero) { _internalMonitorWindow.ReleaseHandle(); }
          _enabled = false;
        }
      }
    }

    /// <summary>
    /// Enables or disables asynchronous event calls
    /// </summary>
    public bool AsynchronousEvents
    {
      get { return _async; }
      set { _async = value; }
    }

    /// <summary>
    /// Creates and initializes a new <see cref="DeviceVolumeMonitor"/>.
    /// </summary>
    /// <param name="handle">Window handle to be captured.</param>
    public DeviceVolumeMonitor(IntPtr handle)
    {
      if (handle == IntPtr.Zero)
        throw new DeviceVolumeMonitorException("Invalid handle!");
      _handle = handle;
      Initialize();
    }

    /// <summary>
    /// Internal initialize method
    /// Sets all the private fields initial values and enables message trapping
    /// </summary>
    private void Initialize()
    {
      _internalMonitorWindow = new _DeviceVolumeMonitor(this);
      _disposed = false;
      _enabled = false;
      _async = false;
      Enabled = true;
    }

    /// <summary>
    /// Internal method used by _DeviceVolumeMonitor.
    /// </summary>
    /// <param name="inserted">Flag set if volume inserted.</param>
    /// <param name="mask">Bit vector returned by the field dbcv_unitmask in the _DEV_BROADCAST_VOLUME structure.</param>
    internal void TriggerEvents(bool inserted, int mask)
    {
      if (_async)
      {
        if (inserted) { OnVolumeInserted.BeginInvoke(mask, null, null); }
        else { OnVolumeRemoved.BeginInvoke(mask, null, null); }
      }
      else
      {
        if (inserted) { OnVolumeInserted(mask); }
        else { OnVolumeRemoved(mask); }
      }
    }

    /// <summary>
    /// Platform invoke the API function QueryDosDevice.
    /// Fills <paramref name="aPath"/> with the device path mapped to a DOS drive letter or device name in <paramref name="aName"/>.
    /// Returns the device path count. (NOTE: used to retrieve a single device path)
    /// </summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern int QueryDosDevice(string aName, [Out] StringBuilder aPath, int aCapacity);

    /// <summary>
    /// Returns a comma delimited string with all the drive letters in the bit vector parameter.
    /// </summary>
    /// <param name="mask">bit vector returned by the field dbcv_unitmask in the _DEV_BROADCAST_VOLUME structure.</param>
    /// <returns></returns>
    public string MaskToLogicalPaths(int mask)
    {
      int lMask = mask;
      int lValue = 0;
      StringBuilder lReturn = new StringBuilder(128);

      if (mask > 0)
      {
        for (; lMask != 0; lMask >>= 1)
        {
          if ((lMask & 1) != 0)
          {
            lReturn.Append((char)(65 + lValue));
            lReturn.Append(":,");
          }
          lValue++;
        }
        lReturn.Remove(lReturn.Length - 1, 1);
      }
      return lReturn.ToString();
    }

    /// <summary>
    /// Returns a comma delimited string with all the device paths in the bit vector parameter.
    /// </summary>
    /// <param name="aMask">bit vector returned by the field dbcv_unitmask in the _DEV_BROADCAST_VOLUME structure.</param>
    /// <returns></returns>
    public string MaskToDevicePaths(int aMask)
    {
      string[] lLogical = MaskToLogicalPaths(aMask).Split(Convert.ToChar(","));

      StringBuilder lBuffer = new StringBuilder(256);
      StringBuilder lReturn = new StringBuilder(256);
      foreach (string lPath in lLogical)
      {
        if (QueryDosDevice(lPath, lBuffer, lBuffer.Capacity) > 0)
        {
          lReturn.Append(lBuffer.ToString());
          lReturn.Append(",");
        }
      }
      lReturn.Remove(lReturn.Length - 1, 1);
      return lReturn.ToString();
    }

    /// <summary>
    /// IDisposable implementation acording to the preferred design pattern
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (_internalMonitorWindow.Handle != IntPtr.Zero)
        {
          _internalMonitorWindow.ReleaseHandle();
          _internalMonitorWindow = null;
        }
      }
      _disposed = true;
    }

    ~DeviceVolumeMonitor()
    {
      Dispose(false);
    }
  }
}
