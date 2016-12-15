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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dokan;

namespace MediaPortal.Common.Services.Dokan.Native
{
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  internal struct DOKAN_OPERATIONS
  {
    public Proxy.ZwCreateFileDelegate ZwCreateFile;
    public Proxy.CleanupDelegate Cleanup;
    public Proxy.CloseFileDelegate CloseFile;
    public Proxy.ReadFileDelegate ReadFile;
    public Proxy.WriteFileDelegate WriteFile;
    public Proxy.FlushFileBuffersDelegate FlushFileBuffers;
    public Proxy.GetFileInformationDelegate GetFileInformation;
    public Proxy.FindFilesDelegate FindFiles;

    public Proxy.FindFilesWithPatternDelegate FindFilesWithPattern;

    public Proxy.SetFileAttributesDelegate SetFileAttributes;
    public Proxy.SetFileTimeDelegate SetFileTime;
    public Proxy.DeleteFileDelegate DeleteFile;
    public Proxy.DeleteDirectoryDelegate DeleteDirectory;
    public Proxy.MoveFileDelegate MoveFile;
    public Proxy.SetEndOfFileDelegate SetEndOfFile;
    public Proxy.SetAllocationSizeDelegate SetAllocationSize;
    public Proxy.LockFileDelegate LockFile;
    public Proxy.UnlockFileDelegate UnlockFile;
    public Proxy.GetDiskFreeSpaceDelegate GetDiskFreeSpace;
    public Proxy.GetVolumeInformationDelegate GetVolumeInformation;
    public Proxy.MountedDelegate Mounted;
    public Proxy.UnmountedDelegate Unmounted;

    public Proxy.GetFileSecurityDelegate GetFileSecurity;
    public Proxy.SetFileSecurityDelegate SetFileSecurity;

    public Proxy.FindStreamsDelegate FindStreams;
  }
}
