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

namespace MediaPortal.Plugins.SlimTv.Service3
{
  public class COMRegistration
  {
    // All COM DLLs must export the DllRegisterServer()
    // and the DllUnregisterServer() APIs for self-registration/unregistration.
    // They both have the same signature and so only one
    // delegate is required.
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate UInt32 DllRegUnRegAPI();

    [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall)]
    static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string strLibraryName);

    [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall)]
    static extern Int32 FreeLibrary(IntPtr hModule);

    [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall)]
    static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

    public static bool Register(string filterPath, bool register)
    {
      // Load the DLL.
      IntPtr hModuleDLL = LoadLibrary(filterPath);

      if (hModuleDLL == IntPtr.Zero)
      {
        ServiceRegistration.Get<ILogger>().Warn("COMRegistration: Could not find requested filter '{0}'", filterPath);
        return false;
      }

      // Obtain the required exported API.
      IntPtr pExportedFunction = GetProcAddress(hModuleDLL, register ? "DllRegisterServer" : "DllUnregisterServer");

      if (pExportedFunction == IntPtr.Zero)
      {
        ServiceRegistration.Get<ILogger>().Warn("COMRegistration: Unable to get required API from DLL '{0}'", filterPath);
        return false;
      }

      // Obtain the delegate from the exported function, whether it be
      // DllRegisterServer() or DllUnregisterServer().
      DllRegUnRegAPI pDelegateRegUnReg = (DllRegUnRegAPI)(Marshal.GetDelegateForFunctionPointer(pExportedFunction, typeof(DllRegUnRegAPI)));

      // Invoke the delegate.
      UInt32 hResult = pDelegateRegUnReg();

      if (hResult == 0)
      {
        ServiceRegistration.Get<ILogger>().Info("COMRegistration: {0} of {1} successful.", (register ? "Registration" : "Unregistration"), filterPath);
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Error("COMRegistration: {0} of {2} failed. Error: {1:X}", (register ? "Registration" : "Unregistration"), hResult, filterPath);
      }

      FreeLibrary(hModuleDLL);
      return true;
    }
  }
}
