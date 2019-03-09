#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using MediaPortal.Utilities.Events;

namespace MediaPortal.UI.Services.RemovableMedia
{

  /// <summary>
  /// Delegate used to implement the class events.
  /// </summary>
  /// <param name="drive">Drive letter with a colon, for example <c>"D:"</c>.</param>
  public delegate void DeviceVolumeAction(string drive);

  public class DeviceVolumeMonitorException : ApplicationException
  {
    public DeviceVolumeMonitorException(string message) : base(message) { }
  }

  internal class _DeviceVolumeMonitor : NativeWindow, IDisposable
  {
    #region Protectded fields

    protected DeviceVolumeMonitor _monitor;
    protected bool _enabled = false;
    protected IEnumerable<char> _knownDrives = null;
    protected DelayedEvent _checkDrivesTimer = null;
    protected IList<char> _arrivedDrives = new List<char>();
    protected IList<char> _removedDrives = new List<char>();

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
      Custom = 0x8006,            // DBT_CUSTOMEVENT
      DeviceNodeChanged = 0x0007  // DBT_DEVNODES_CHANGED
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

    [StructLayout(LayoutKind.Sequential)]
    public struct BroadcastHeader     // _DEV_BROADCAST_HDR 
    {
      public int Size;                // dbch_size
      public DeviceType Type;         // dbch_devicetype
      private readonly int Reserved;  // dbch_reserved
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Volume              // _DEV_BROADCAST_VOLUME 
    {
      public int Size;                // dbcv_size
      public DeviceType Type;         // dbcv_devicetype
      private readonly int Reserved;  // dbcv_reserved
      public int Mask;                // dbcv_unitmask
      public int Flags;               // dbcv_flags
    }

    #endregion

    /// <summary>
    /// Creates and initializes a new internal <see cref="_DeviceVolumeMonitor"/> instance.
    /// </summary>
    /// <param name="monitor">The DeviceVolumeMonitor instance that ownes this object.</param>
    public _DeviceVolumeMonitor(DeviceVolumeMonitor monitor)
    {
      _monitor = monitor;
      _knownDrives = GetCurrentDriveLetters();
      _checkDrivesTimer = new DelayedEvent(2000);
      _checkDrivesTimer.OnEventHandler += CheckDrives;
    }

    /// <summary>
    /// WndProc method that traps all messages sent to the Handle.
    /// </summary>
    /// <param name="msg">A Windows message.</param>
    protected override void WndProc(ref Message msg)
    {
      base.WndProc(ref msg);
      if (!_monitor.Enabled)
        return;
      if (msg.Msg == WM_DEVICECHANGE)
      {
        DeviceEvent evt = (DeviceEvent) msg.WParam.ToInt32();
        if (evt == DeviceEvent.Arrival || evt == DeviceEvent.RemoveComplete)
        {
          BroadcastHeader broadcastHeader = (BroadcastHeader)Marshal.PtrToStructure(msg.LParam, typeof(BroadcastHeader));
          if (broadcastHeader.Type == DeviceType.Volume)
          {
            Volume volume = (Volume)Marshal.PtrToStructure(msg.LParam, typeof(Volume));
            if ((volume.Flags & (int)VolumeFlags.Media) != 0)
            {
              foreach (char drive in DeviceVolumeMonitor.MaskToDrives(volume.Mask).Select(d => d[0]))
              {
                if (evt == DeviceEvent.Arrival)
                {
                  //Force media inserted event because the drive might already exist but was just mounted (DVD, CD etc.)
                  if (!_arrivedDrives.Contains(drive))
                    _arrivedDrives.Add(drive);
                }
                else if (evt == DeviceEvent.RemoveComplete)
                {
                  //Force media removed event because the drive might already exist but was just unmounted (DVD, CD etc.)
                  if (!_removedDrives.Contains(drive))
                    _removedDrives.Add(drive);
                }
              }
              //The Arrival and RemoveComplete events might be accompanied by DeviceNodeChanged events so have a delay to filter them out
              //Also the known drives list needs to be updated which is handled by the delayed event
              _checkDrivesTimer.EnqueueEvent(null, EventArgs.Empty);
            }
          }
        }
        else if (evt == DeviceEvent.DeviceNodeChanged)
        {
          //A removal of a drive might cause multiple DeviceNodeChanged events so use a delay to filter them out
          //Also the drive is not yet mounted when receiving this event so delay the event trigger
          _checkDrivesTimer.EnqueueEvent(null, EventArgs.Empty);
        }
      }
    }

    private void CheckDrives(object sender, EventArgs e)
    {
      IEnumerable<char> currentDrives = GetCurrentDriveLetters();
      int removedMask = DrivesToMask(_knownDrives.Except(currentDrives));
      int addedMask = DrivesToMask(currentDrives.Except(_knownDrives));
      removedMask |= DrivesToMask(_removedDrives);
      addedMask |= DrivesToMask(_arrivedDrives);

      _removedDrives.Clear();
      _arrivedDrives.Clear();
      _knownDrives = currentDrives;

      if (removedMask > 0)
        _monitor.TriggerEvents(false, removedMask);
      if (addedMask > 0)
        _monitor.TriggerEvents(true, addedMask);
    }

    /// <summary>
    /// Enumerates all currently mounted non-network drive letters.
    /// </summary>
    private IEnumerable<char> GetCurrentDriveLetters()
    {
      List<char> driveLetters = new List<char>();
      foreach (var drive in DriveInfo.GetDrives())
      {
        if (drive.DriveType == DriveType.CDRom || drive.DriveType == DriveType.Fixed || drive.DriveType == DriveType.Removable)
          driveLetters.Add(drive.Name[0]);
      }
      return driveLetters;
    }

    /// <summary>
    /// Converts drive letters to a mask.
    /// </summary>
    public static int DrivesToMask(IEnumerable<char> drives)
    {
      int mask = 0;
      foreach(var drive in drives)
      {
        mask |= (1 << (drive - 65));
      }
      return mask;
    }

    public void Dispose()
    {
      _checkDrivesTimer.Dispose();
      _checkDrivesTimer = null;
    }

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
          if (Handle == IntPtr.Zero) { AssignHandle(_monitor.WindowHandle); }
          _enabled = true;
        }
        if (_enabled && !value)
        {
          if (Handle != IntPtr.Zero) { ReleaseHandle(); }
          _enabled = false;
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
    protected IntPtr _windowHandle;

    #endregion

    /// <summary>
    /// Creates and initializes a new <see cref="DeviceVolumeMonitor"/>.
    /// </summary>
    /// <param name="windowHandle">Window handle used to capture device events.</param>
    public DeviceVolumeMonitor(IntPtr windowHandle)
    {
      if (windowHandle == IntPtr.Zero)
        throw new DeviceVolumeMonitorException("Invalid handle");
      _windowHandle = windowHandle;
      Initialize();
    }

    public void Dispose()
    {
      if (_internalMonitorWindow == null)
        return;
      if (_internalMonitorWindow.Handle != IntPtr.Zero)
        _internalMonitorWindow.ReleaseHandle();
      _internalMonitorWindow = null;
      GC.SuppressFinalize(this);
    }

    ~DeviceVolumeMonitor()
    {
      Dispose();
    }

    /// <summary>
    /// Internal initialize method.
    /// Sets all the private fields initial values and enables message trapping.
    /// </summary>
    protected void Initialize()
    {
      _internalMonitorWindow = new _DeviceVolumeMonitor(this);
    }

    /// <summary>
    /// Invoked on media insertion.
    /// </summary>
    public event DeviceVolumeAction MediaInserted;

    /// <summary>
    /// Invoked on media removal.
    /// </summary>
    public event DeviceVolumeAction MediaRemoved;

    /// <summary>
    /// Enables or disables message trapping.
    /// </summary>
    public bool Enabled
    {
      get { return _internalMonitorWindow.Enabled; }
      set { _internalMonitorWindow.Enabled = value; }
    }

    public IntPtr WindowHandle
    {
      get { return _windowHandle; }
    }

    protected void FireMediaInserted(string drive)
    {
      DeviceVolumeAction dlgt = MediaInserted;
      if (dlgt != null)
        dlgt(drive);
    }

    protected void FireMediaRemoved(string drive)
    {
      DeviceVolumeAction dlgt = MediaRemoved;
      if (dlgt != null)
        dlgt(drive);
    }

    /// <summary>
    /// A removable media drive was inserted or removed. Internal method used by _DeviceVolumeMonitor.
    /// </summary>
    /// <param name="inserted">Flag is set to <c>true</c> if media inserted, else <c>false</c>.</param>
    /// <param name="mask">Bit vector returned by the field dbcv_unitmask in the _DEV_BROADCAST_VOLUME structure.</param>
    internal void TriggerEvents(bool inserted, int mask)
    {
      foreach (string drive in MaskToDrives(mask))
      {
        if (inserted)
          FireMediaInserted(drive);
        else
          FireMediaRemoved(drive);
      }
    }

    /// <summary>
    /// Returns an enumeration of drive letter strings from the given bit vector parameter.
    /// </summary>
    /// <param name="mask">Bit vector specifying a set of drives. Each drive in the drive set is encoded as a bit in the <paramref name="mask"/>
    /// bit vector, starting with drive A:. That bit vector is returned by the field dbcv_unitmask in the _DEV_BROADCAST_VOLUME structure.</param>
    /// <returns>Enumeration of strings containing the drives from the given <paramref name="mask"/>. Each drive string has a
    /// form like <c>"D:"</c>.</returns>
    public static IEnumerable<string> MaskToDrives(int mask)
    {
      int value = 0;

      if (mask > 0)
      {
        for (; mask != 0; mask >>= 1, value++)
          if ((mask & 1) != 0)
            yield return ((char) (65 + value)) + ":";
      }
      yield break;
    }
  }
}
