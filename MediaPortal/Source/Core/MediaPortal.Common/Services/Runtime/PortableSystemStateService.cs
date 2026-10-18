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
using MediaPortal.Common.Logging;
using MediaPortal.Common.Runtime;

namespace MediaPortal.Common.Services.Runtime
{
  /// <summary>
  /// Portable <see cref="ISystemStateService"/>: keeps the application's own state machine correct
  /// and leaves the machine-level power actions to a platform implementation.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <see cref="ISystemStateService"/> mixes two very different jobs. Half of it is pure in-process
  /// bookkeeping: which <see cref="SystemState"/> we are in, and which
  /// <see cref="SystemMessaging"/> notifications go out when that changes. That half is what the
  /// rest of the tree actually reacts to (AsynchronousMessageQueue pauses during shutdown, the sleep
  /// timer and the system state menu drive their flow through it), and this class does it in full.
  /// </para>
  /// <para>
  /// The other half is acting on the machine, and there it is Win32 through and through:
  /// <c>ExitWindowsEx</c> for shutdown/restart/logoff, <c>SetSuspendState</c> for suspend/hibernate,
  /// <c>SetThreadExecutionState</c> for keep-awake, plus a hidden message window listening for
  /// <c>WM_POWERBROADCAST</c> to notice the machine coming back from sleep. That half lives in
  /// <c>WindowsSystemStateService</c> in <c>MediaPortal.Common.Windows</c>.
  /// </para>
  /// <para>
  /// What this class deliberately does not do is pretend. Each machine action logs that it was
  /// requested and not performed, instead of returning silently and leaving the user staring at a
  /// machine that stayed on. The real replacements are per-platform and not interchangeable: a Linux
  /// HTPC would ask logind over DBus (or spawn <c>systemctl</c>, which needs the same polkit
  /// approval either way), Android gives an app no way to power the device off at all, and macOS
  /// asks for an administrator authorisation per call. That work is tracked in
  /// docs/multiplatform/ROADMAP.md; until it is done the honest description is "the application shuts
  /// itself down, the machine does not".
  /// </para>
  /// <para>
  /// The power-broadcast subscription is absent rather than stubbed: no <see cref="SystemState"/>
  /// transition is sent on resume and no suspend request is ever denied, which is what happens on a
  /// machine that never sleeps anyway.
  /// </para>
  /// </remarks>
  public sealed class PortableSystemStateService : ISystemStateService, IDisposable
  {
    #region Fields

    private SystemState _state = SystemState.Starting;
    private SuspendLevel _lastContinuousSuspendLevel = SuspendLevel.None;
    private bool _machineActionReported;

    #endregion

    #region Helpers

    /// <summary>
    /// Says that this build cannot drive the machine's power state. The first request gets a warning
    /// naming the missing backend, later ones a plain info line, so a sleep timer that retries every
    /// minute does not fill the log with warnings.
    /// </summary>
    private void ReportMachineActionUnsupported(string action)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>(false);
      if (logger == null)
        return;
      if (_machineActionReported)
        logger.Info("SystemStateService: {0} requested; this platform build cannot change the machine's power state", action);
      else
      {
        _machineActionReported = true;
        logger.Warn("SystemStateService: {0} requested, but no power backend is implemented for this platform - the application shuts down, the machine does not (see the 'power backend' entry in docs/multiplatform/ROADMAP.md).", action);
      }
    }

    #endregion

    #region ISystemStateService implementation

    public void SwitchSystemState(SystemState newState, bool sendMessage)
    {
      _state = newState;
      if (sendMessage)
        SystemMessaging.SendSystemStateChangeMessage(_state);
    }

    public SystemState CurrentState
    {
      get { return _state; }
    }

    public void Shutdown(bool force = false)
    {
      ServiceRegistration.Get<ILogger>().Info("SystemStateService: Shutting down");
      SystemMessaging.SendSystemStateChangeMessage(SystemState.ShuttingDown);
      ReportMachineActionUnsupported("shutdown");
    }

    public void Restart(bool force = false)
    {
      ServiceRegistration.Get<ILogger>().Info("SystemStateService: Restarting");
      SystemMessaging.SendSystemStateChangeMessage(SystemState.ShuttingDown);
      ReportMachineActionUnsupported("restart");
    }

    public void Suspend()
    {
      ServiceRegistration.Get<ILogger>().Info("SystemStateService: Suspending");
      SystemMessaging.SendSystemStateChangeMessage(SystemState.Suspending);
      ReportMachineActionUnsupported("suspend");
    }

    public void Hibernate()
    {
      ServiceRegistration.Get<ILogger>().Info("SystemStateService: Hibernating");
      SystemMessaging.SendSystemStateChangeMessage(SystemState.Hibernating);
      ReportMachineActionUnsupported("hibernate");
    }

    public void Logoff(bool force = false)
    {
      ServiceRegistration.Get<ILogger>().Info("SystemStateService: Logging off");
      SystemMessaging.SendSystemStateChangeMessage(SystemState.ShuttingDown);
      ReportMachineActionUnsupported("logoff");
    }

    /// <summary>
    /// Remembers the requested <see cref="SuspendLevel"/> without being able to apply it.
    /// </summary>
    /// <remarks>
    /// The value is still stored because callers read it back through the suspend-request path and
    /// the sleep timer compares against it. Only the effect is missing, and on the platforms we
    /// target that effect never came from a thread-local flag anyway: it is logind's idle inhibitor
    /// on Linux and a wake lock on Android.
    /// </remarks>
    public void SetCurrentSuspendLevel(SuspendLevel level, bool continuous = false)
    {
      if (continuous)
        _lastContinuousSuspendLevel = level;
    }

    /// <summary>
    /// The level last requested with <c>continuous = true</c>. Mirrors the state the Windows
    /// implementation keeps to deny suspend requests while media is playing.
    /// </summary>
    public SuspendLevel LastContinuousSuspendLevel
    {
      get { return _lastContinuousSuspendLevel; }
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      // Nothing to tear down - there is no message-window thread to signal, as the Windows
      // implementation has. Present so hosts can dispose their ISystemStateService without caring
      // which one they got.
    }

    #endregion
  }
}
