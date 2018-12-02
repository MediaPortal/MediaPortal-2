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
using System.Runtime.InteropServices;
using DirectShow;

namespace MediaPortal.UI.Players.Video.Native
{
  /// <summary>
  /// Wrapper class to access both x86 and x64 version depending on current process architecture.
  /// </summary>
  internal class EvrPresenterWrapper
  {
    internal static int EvrInit(IEVRPresentCallback callback, IntPtr dwD3DDevice, IBaseFilter evrFilter, IntPtr monitor, out IntPtr presenterInstance)
    {
      if (IntPtr.Size > 4)
        return EvrInit64(callback, dwD3DDevice, evrFilter, monitor, out presenterInstance);
      return EvrInit32(callback, dwD3DDevice, evrFilter, monitor, out presenterInstance);
    }

    internal static void EvrDeinit(IntPtr presenterInstance)
    {
      if (IntPtr.Size > 4)
        EvrDeinit64(presenterInstance);
      else
        EvrDeinit32(presenterInstance);
    }

    #region DLL imports

    [DllImport("x86\\EVRPresenter.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "EvrInit")]
    private static extern int EvrInit32(IEVRPresentCallback callback, IntPtr dwD3DDevice, IBaseFilter evrFilter, IntPtr monitor, out IntPtr presenterInstance);

    [DllImport("x86\\EVRPresenter.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void EvrDeinit32(IntPtr presenterInstance);

    [DllImport("x64\\EVRPresenter.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "EvrInit")]
    private static extern int EvrInit64(IEVRPresentCallback callback, IntPtr dwD3DDevice, IBaseFilter evrFilter, IntPtr monitor, out IntPtr presenterInstance);

    [DllImport("x64\\EVRPresenter.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void EvrDeinit64(IntPtr presenterInstance);

    #endregion
  }
}
