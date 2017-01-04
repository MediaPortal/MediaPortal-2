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

namespace MediaPortal.Common.Services.Dokan.Native
{
  internal static class DokanNativeMethods
  {
    private const string DOKAN_DLL = "dokan1.dll";

    [DllImport(DOKAN_DLL, ExactSpelling = true)]
    public static extern int DokanMain(ref DOKAN_OPTIONS options, ref DOKAN_OPERATIONS operations);

    [DllImport(DOKAN_DLL, ExactSpelling = true, CharSet = CharSet.Auto)]
    public static extern int DokanUnmount(char driveLetter);

    [DllImport(DOKAN_DLL, ExactSpelling = true)]
    public static extern uint DokanVersion();

    [DllImport(DOKAN_DLL, ExactSpelling = true)]
    public static extern uint DokanDriveVersion();

    [DllImport(DOKAN_DLL, ExactSpelling = true, CharSet = CharSet.Auto)]
    public static extern int DokanRemoveMountPoint([MarshalAs(UnmanagedType.LPWStr)] string mountPoint);

    [DllImport(DOKAN_DLL, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DokanResetTimeout(uint timeout, DokanFileInfo rawFileInfo);

    [DllImport(DOKAN_DLL, ExactSpelling = true)]
    public static extern IntPtr DokanOpenRequestorToken(DokanFileInfo rawFileInfo);

    [DllImport(DOKAN_DLL, ExactSpelling = true)]
    public static extern void DokanMapKernelToUserCreateFileFlags(uint fileAttributes, uint createOptions, uint createDisposition, ref int outFileAttributesAndFlags, ref int outCreationDisposition);
  }
}
