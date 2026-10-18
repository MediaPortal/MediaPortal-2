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
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.Utilities.Process
{
  public class SingleInstanceHelper
  {
    /// <summary>
    /// Windows message id to bring MP2-Client to front.
    /// </summary>
    /// <remarks>
    /// Ideally this field should be kept within it's parent application, not within <see cref="MediaPortal.Utilities"/>.
    /// It has been added here to keep <c>MediaPortal.Client</c> and <c>SkinEngine</c> projects independent from each other.
    /// </remarks>
    public static readonly uint SHOW_MP2_CLIENT_MESSAGE = RegisterWindowMessageIfWindows("SHOW_MP2_CLIENT_MESSAGE");

    /// <summary>
    /// Windows message id to bring MP2-ServiceMonitor to front.
    /// </summary>
    /// <remarks>
    /// Ideally this field should be kept within it's parent application, not within <see cref="MediaPortal.Utilities"/>.
    /// It has been added here to be consistent to <c>MediaPortal.Client</c>.
    /// </remarks>
    public static readonly uint SHOW_MP2_SERVICEMONITOR_MESSAGE = RegisterWindowMessageIfWindows("SHOW_MP2_SERVICEMONITOR_MESSAGE");

    /// <summary>
    /// Registers a Win32 message id, or returns 0 on a platform that has no window message table.
    /// </summary>
    /// <remarks>
    /// These are static fields, so the call runs the first time anything touches this class - on a
    /// platform without user32.dll that is a <see cref="System.DllNotFoundException"/> out of a type
    /// initializer, i.e. it cannot be caught at the place that looks innocent. CA1416 never reported
    /// it, because a P/Invoke is a string to the analyzer; see scripts/ci/README.md, "How a core
    /// assembly loses its -windows target".
    /// </remarks>
    private static uint RegisterWindowMessageIfWindows(string name)
    {
      return OperatingSystem.IsWindows() ? WindowsAPI.RegisterWindowMessage(name) : 0;
    }

    /// <summary>
    /// Check if an application is running or not
    /// </summary>
    /// <returns>returns true if already running</returns>
    public static bool IsAlreadyRunning(string MUTEX_ID, out Mutex mutex)
    {
      // Allow only one instance
      mutex = new Mutex(false, MUTEX_ID);

      // The ACL is what lets a *different* user's process open the same named mutex, which matters on
      // Windows because the service and the interactive user are different accounts. Off Windows there
      // is no MutexSecurity to set (the type is Windows-only), while the named mutex itself works:
      // measured in the Linux container by NamedPrimitiveBehaviourTests (test-canary-linux.ps1), a
      // held named mutex blocks another thread and a second handle created from the same name sees it,
      // which are the two properties this method relies on. So the single-instance check keeps
      // working and only the cross-account grant is skipped.
      if (OperatingSystem.IsWindows())
      {
        var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                                                    MutexRights.FullControl, AccessControlType.Allow);
        var securitySettings = new MutexSecurity();
        securitySettings.AddAccessRule(allowEveryoneRule);
        mutex.SetAccessControl(securitySettings);
      }
      
      bool hasHandle = false;
      try
      {
        // Check if we can start the application
        hasHandle = mutex.WaitOne(500, false);
      }
      catch (AbandonedMutexException)
      {
        // The mutex was abandoned in another process, it will still get aquired
        hasHandle = true;
      }
      return !hasHandle;
    }

    /// <summary>
    /// Broadcasts a <param name="messageId">message</param> through the system to inform an existing instance to show up.
    /// </summary>
    /// <param name="messageId">The id to a static windows message, which should be broadcasted.</param>
    public static void SwitchToCurrentInstance(uint messageId)
    {
      // HWND_BROADCAST is a Win32 window-message broadcast; there is no equivalent to call into
      // another process's UI from here on other platforms. Returning without doing anything keeps the
      // single-instance *detection* intact and only drops the "raise the existing window" step, which
      // is a step no non-Windows caller in this tree performs. A portable frontend will ask for that
      // over the API instead of over a window message.
      if (!OperatingSystem.IsWindows())
        return;

      // Send our Win32 message to make the currently running instance
      // Jump on top of all the other windows
      WindowsAPI.PostMessage((IntPtr)WindowsAPI.HWND_BROADCAST, messageId, IntPtr.Zero, IntPtr.Zero);
    }
  }
}
