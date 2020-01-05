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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class EjectOpticalDiscAction : IWorkflowContributor
  {
    private string _opticalDriveLetter;

    #region Native

    private const int OPEN_EXISTING = 3;
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint FSCTL_LOCK_VOLUME = 0x00090018;
    private const uint FSCTL_DISMOUNT_VOLUME = 0x00090020;
    private const uint IOCTL_STORAGE_EJECT_MEDIA = 0x002D4808;
    private const uint IOCTL_STORAGE_MEDIA_REMOVAL = 0x002D4804;
    private const long INVALID_HANDLE = -1;

    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr CreateFile(string filename, uint desiredAccess, uint shareMode, IntPtr securityAttributes, int creationDisposition, int flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(IntPtr deviceHandle, uint ioControlCode, byte[] inBuffer, int inBufferSize, byte[] outBuffer, int outBufferSize, ref int bytesReturned, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    #endregion

    #region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public IResourceString DisplayTitle
    {
      get { return LocalizationHelper.CreateResourceString(Consts.RES_EJECT_OPTICAL_DISC_RES); }
    }

    public void Initialize()
    {
      foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.CDRom))
      {
        _opticalDriveLetter = drive.Name.Substring(0, 1);
        break;
      }
    }

    public void Uninitialize()
    {
    }

    public bool IsActionVisible(NavigationContext context)
    {
      NavigationData navigationData = MediaNavigationModel.GetNavigationData(context, false);
      return navigationData != null && !string.IsNullOrEmpty(_opticalDriveLetter);
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return !string.IsNullOrEmpty(_opticalDriveLetter);
    }

    public void Execute()
    {
      EjectMedia(_opticalDriveLetter);
    }

    protected bool EjectMedia(string driveLetter)
    {
      bool success = false;
      string sPhysicalDrive = $@"\\.\{driveLetter}:";

      // Open drive (prepare for eject)
      IntPtr handle = CreateFile(sPhysicalDrive, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE,
        IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
      if (handle.ToInt64() == INVALID_HANDLE)
      {
        ServiceRegistration.Get<ILogger>().Error("Media eject failed. Drive {0} does not exist!", driveLetter);
        return false;
      }

      try
      {
        int dummy = 0;
        // Lock Volume (retry 4 times - 2 seconds)
        for (int i = 0; i < 4; i++)
        {
          success = DeviceIoControl(handle, FSCTL_LOCK_VOLUME, null, 0, null, 0, ref dummy, IntPtr.Zero);
          if (success)
            break;

          Thread.Sleep(500);
        }
        if (!success)
        {
          ServiceRegistration.Get<ILogger>().Error("Media eject failed. Drive {0} could not be locked!", driveLetter);
          return false;
        }

        // Volume dismount
        dummy = 0;
        success = DeviceIoControl(handle, FSCTL_DISMOUNT_VOLUME, null, 0, null, 0, ref dummy, IntPtr.Zero);
        if (!success)
        {
          ServiceRegistration.Get<ILogger>().Error("Media eject failed. Drive {0} could not be dismounted!", driveLetter);
          return false;
        }

        // Prevent Removal Of Volume
        byte[] flag = new byte[1];
        flag[0] = 0; // 0 = false
        dummy = 0;
        success = DeviceIoControl(handle, IOCTL_STORAGE_MEDIA_REMOVAL, flag, 1, null, 0, ref dummy, IntPtr.Zero);
        if (!success)
        {
          ServiceRegistration.Get<ILogger>().Error("Media eject failed. Drive {0} could not be suspended!", driveLetter);
          return false;
        }

        // Eject Media
        dummy = 0;
        success = DeviceIoControl(handle, IOCTL_STORAGE_EJECT_MEDIA, null, 0, null, 0, ref dummy, IntPtr.Zero);
        if (!success)
        {
          ServiceRegistration.Get<ILogger>().Error("Media eject failed. Drive {0} could not be ejected!", driveLetter);
          return false;
        }
      }
      finally
      {
        // Close Handle
        CloseHandle(handle);
      }
      return true;
    }

    #endregion
  }
}
