#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Threading;

namespace MediaPortal.Utilities
{
  public static class ThreadingUtils
  {
    /// <summary>
    /// Starts a new thread using STA apartment state (<see cref="ApartmentState.STA"/>). This is required for accessing some windows features like the clipboard.
    /// </summary>
    /// <param name="threadStart">Thread to start.</param>
    /// <remarks>
    /// The thread is a background thread, which is what lets a caller that stopped waiting for it walk away: a
    /// thread nobody is waiting for must not be a reason for a process to stay alive. It used to be possible to
    /// stop such a thread from the outside with <c>Thread.Abort()</c>; on every .NET that is not Windows
    /// Framework that call throws <see cref="PlatformNotSupportedException"/>, so the choice is between waiting
    /// and leaving the thread to finish by itself. Callers that cannot wait forever - the clipboard, which
    /// freezes when another program holds it - leave it.
    /// </remarks>
    public static Thread RunSTAThreaded(ThreadStart threadStart)
    {
      Thread newThread = new Thread(threadStart) { IsBackground = true };
      // Apartment state is a COM concept. SetApartmentState is only available on Windows, and the
      // only thing that has ever needed STA here is SkinEngine's ClipboardManager talking to the OLE
      // clipboard, which does not exist elsewhere. On other platforms the thread runs MTA, which is
      // what every other thread in the process does anyway.
      // OperatingSystem.IsWindows() and not RuntimeInformation.IsOSPlatform(OSPlatform.Windows):
      // CA1416 recognises the first as a platform guard and stays silent, and does not recognise the
      // second - so the choice is what makes this branch verifiable by the compiler.
      if (OperatingSystem.IsWindows())
        newThread.SetApartmentState(ApartmentState.STA);
      newThread.Start();
      return newThread;
    }
  }
}