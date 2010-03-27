#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
#pragma warning disable 649
#pragma warning disable 169

namespace Components.Services.AutoPlay
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
    /// <summary>
    /// Private fields
    /// </summary>
    DeviceVolumeMonitor fMonitor;

    #region API constants and structures
    /// <summary>
    /// Constant defined for the WM_DEVICECHANGE message in WinUser.h
    /// </summary>
    const int WM_DEVICECHANGE = 0x0219;

    /// <summary>
    /// Constants and structs defined in DBT.h
    /// </summary>
    public enum DeviceEvent : int
    {
      Arrival = 0x8000,           //DBT_DEVICEARRIVAL
      QueryRemove = 0x8001,       //DBT_DEVICEQUERYREMOVE
      QueryRemoveFailed = 0x8002, //DBT_DEVICEQUERYREMOVEFAILED
      RemovePending = 0x8003,     //DBT_DEVICEREMOVEPENDING
      RemoveComplete = 0x8004,    //DBT_DEVICEREMOVECOMPLETE
      Specific = 0x8005,          //DBT_DEVICEREMOVECOMPLETE
      Custom = 0x8006             //DBT_CUSTOMEVENT
    }

    public enum DeviceType : int
    {
      OEM = 0x00000000,           //DBT_DEVTYP_OEM
      DeviceNode = 0x00000001,    //DBT_DEVTYP_DEVNODE
      Volume = 0x00000002,        //DBT_DEVTYP_VOLUME
      Port = 0x00000003,          //DBT_DEVTYP_PORT
      Net = 0x00000004            //DBT_DEVTYP_NET
    }

    public enum VolumeFlags : int
    {
      Media = 0x0001,             //DBTF_MEDIA
      Net = 0x0002                //DBTF_NET
    }

    public struct BroadcastHeader   //_DEV_BROADCAST_HDR 
    {
      public int Size;            //dbch_size
      public DeviceType Type;     //dbch_devicetype
      private int Reserved;       //dbch_reserved
    }

    public struct Volume            //_DEV_BROADCAST_VOLUME 
    {
      public int Size;            //dbcv_size
      public DeviceType Type;     //dbcv_devicetype
      private int Reserved;       //dbcv_reserved
      public int Mask;            //dbcv_unitmask
      public int Flags;           //dbcv_flags
    }
    #endregion

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="aMonitor">A DeviceVolumeMonitor instance that ownes the object</param>
    /// <param name="aHandle">The Windows handle to be used</param>
    public _DeviceVolumeMonitor(DeviceVolumeMonitor aMonitor)
    {
      fMonitor = aMonitor;
    }

    /// <summary>
    /// WndProc method that traps all messages sent to the Handle
    /// </summary>
    /// <param name="aMessage">A Windows message</param>
    protected override void WndProc(ref Message aMessage)
    {
      BroadcastHeader lBroadcastHeader;
      Volume lVolume;
      DeviceEvent lEvent;

      base.WndProc(ref aMessage);
      if (aMessage.Msg == WM_DEVICECHANGE && fMonitor.Enabled)
      {
        lEvent = (DeviceEvent)aMessage.WParam.ToInt32();
        if (lEvent == DeviceEvent.Arrival || lEvent == DeviceEvent.RemoveComplete)
        {
          lBroadcastHeader = (BroadcastHeader)Marshal.PtrToStructure(aMessage.LParam, typeof(BroadcastHeader));
          if (lBroadcastHeader.Type == DeviceType.Volume)
          {
            lVolume = (Volume)Marshal.PtrToStructure(aMessage.LParam, typeof(Volume));
            if ((lVolume.Flags & (int)VolumeFlags.Media) != 0)
            {
              fMonitor.TriggerEvents(lEvent == DeviceEvent.Arrival, lVolume.Mask);
            }
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

    /// <summary>
    /// Private fields
    /// </summary>
    _DeviceVolumeMonitor fInternal;
    IntPtr fHandle;
    bool fDisposed;
    bool fEnabled;
    bool fAsync;

    /// <summary>
    /// Events
    /// These events are invoked on Volume insertion an removal
    /// </summary>
    public event DeviceVolumeAction OnVolumeInserted;
    public event DeviceVolumeAction OnVolumeRemoved;

    /// <summary>
    /// Enables or disables message trapping
    /// </summary>
    public bool Enabled
    {
      get { return fEnabled; }
      set
      {
        if (!fEnabled && value)
        {
          if (fInternal.Handle == IntPtr.Zero) { fInternal.AssignHandle(fHandle); }
          fEnabled = true;
        }
        if (fEnabled && !value)
        {
          if (fInternal.Handle != IntPtr.Zero) { fInternal.ReleaseHandle(); }
          fEnabled = false;
        }
      }
    }

    /// <summary>
    /// Enables or disables asynchronous event calls
    /// </summary>
    public bool AsynchronousEvents
    {
      get { return fAsync; }
      set { fAsync = value; }
    }

    /// <summary>
    /// Preferred constructor, accepts a Window Handle as single parameter
    /// </summary>
    /// <param name="aHandle">Window handle to be captured</param>
    public DeviceVolumeMonitor(IntPtr aHandle)
    {
      if (aHandle == IntPtr.Zero)
        throw new DeviceVolumeMonitorException("Invalid handle!");
      else
        fHandle = aHandle;
      Initialize();
    }

    /// <summary>
    /// Internal initialize method
    /// Sets all the private fields initial values and enables message trapping
    /// </summary>
    private void Initialize()
    {
      fInternal = new _DeviceVolumeMonitor(this);
      fDisposed = false;
      fEnabled = false;
      fAsync = false;
      Enabled = true;
    }

    /// <summary>
    /// Internal method used by _DeviceVolumeMonitor
    /// </summary>
    /// <param name="aInserted">Flag set if volume inserted</param>
    /// <param name="aMask">bit vector returned by the field dbcv_unitmask in the _DEV_BROADCAST_VOLUME structure</param>
    internal void TriggerEvents(bool aInserted, int aMask)
    {
      if (AsynchronousEvents)
      {
        if (aInserted) { OnVolumeInserted.BeginInvoke(aMask, null, null); }
        else { OnVolumeRemoved.BeginInvoke(aMask, null, null); }
      }
      else
      {
        if (aInserted) { OnVolumeInserted(aMask); }
        else { OnVolumeRemoved(aMask); }
      }
    }

    /// <summary>
    /// Platform invoke the API function QueryDosDevice
    /// Fills aPath with the device path mapped to a DOS drive letter or device name in aName
    /// Returns the device path count (NOTE: used to retrieve a single device path)
    /// </summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern int QueryDosDevice(string aName, [Out] StringBuilder aPath, int aCapacity);

    /// <summary>
    /// Returns a comma delimited string with all the drive letters in the bit vector parameter
    /// </summary>
    /// <param name="aMask">bit vector returned by the field dbcv_unitmask in the _DEV_BROADCAST_VOLUME structure</param>
    /// <returns></returns>
    public string MaskToLogicalPaths(int aMask)
    {
      int lMask = aMask;
      int lValue = 0;
      StringBuilder lReturn = new StringBuilder(128);

      try
      {
        lReturn = new StringBuilder(128);
        if (aMask > 0)
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
      finally
      {
        lReturn = null;
      }
    }

    /// <summary>
    /// Returns a comma delimited string with all the device paths in the bit vector parameter
    /// </summary>
    /// <param name="aMask">bit vector returned by the field dbcv_unitmask in the _DEV_BROADCAST_VOLUME structure</param>
    /// <returns></returns>
    public string MaskToDevicePaths(int aMask)
    {
      string[] lLogical = MaskToLogicalPaths(aMask).Split(Convert.ToChar(","));
      StringBuilder lBuffer;
      StringBuilder lReturn;

      try
      {
        lBuffer = new StringBuilder(256);
        lReturn = new StringBuilder(256);
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
      finally
      {
        lBuffer = null;
        lReturn = null;
      }
    }

    /// <summary>
    /// IDisposable implementation acording to the preferred design pattern
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool aDisposing)
    {
      if (!this.fDisposed)
      {
        if (fInternal.Handle != IntPtr.Zero)
        {
          fInternal.ReleaseHandle();
          fInternal = null;
        }
      }
      fDisposed = true;
    }

    ~DeviceVolumeMonitor()
    {
      Dispose(false);
    }
  }
}
