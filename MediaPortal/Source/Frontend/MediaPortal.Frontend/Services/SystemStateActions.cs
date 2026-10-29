/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.
*/

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MediaPortal.Frontend.Services
{
  /// <summary>
  /// The ways out of the machine, in the reference's spelling. The reference's SystemStateMenu plugin kept
  /// exactly these nine (<c>SystemStateAction</c>); this front end hangs the seven the reference's default
  /// shutdown menu shows - the two it hid by default, minimize and log off, stay hidden here too.
  /// </summary>
  public enum ShutdownAction
  {
    Suspend,
    Hibernate,
    Shutdown,
    SleepTimer,
    Restart,
    CloseMP,
    RestartMP
  }

  /// <summary>
  /// Which of the machine's ways out this front end offers. The reference never asked the firmware - its
  /// <c>CreateDefaultShutdownMenu</c> simply showed every way out and let its settings hide what the operator
  /// did not want - so this front end trusts the operating system the same way and lets the action itself
  /// fail quietly on a machine that refuses. The one honest question it does ask is the platform: on anything
  /// but Windows the machine's own power switches are nobody else's business, and only the application's own
  /// ways out are offered.
  /// </summary>
  /// <param name="Suspend">Whether the machine knows what "sleep" means.</param>
  /// <param name="Hibernate">Whether the machine has a hibernation file to write.</param>
  /// <param name="SystemPower">Whether this platform lets an application ask for shutdown and restart at all.</param>
  /// <param name="Application">Whether this platform lets the application end itself at all. A desktop
  /// lifetime can; a phone - where the operating system owns every window's life and death - cannot, and a
  /// row that promised leaving on a platform that forbids it would lie.</param>
  public sealed record SystemStateSupport(bool Suspend, bool Hibernate, bool SystemPower,
      bool Application = true)
  {
    /// <summary>
    /// The box we are standing on, asked once. The firmware's own answer (<c>GetPwrCapabilities</c>) is
    /// deliberately not consulted: on a current Windows it reports sleep and hibernation as missing on a
    /// machine whose <c>powercfg</c> lists both, and the reference never asked it either.
    /// </summary>
    public static SystemStateSupport ThisMachine
    {
      get
      {
        if (_thisMachine == null)
        {
          bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
          _thisMachine = new SystemStateSupport(windows, windows, windows);
        }
        return _thisMachine;
      }
    }

    private static SystemStateSupport? _thisMachine;
  }

  /// <summary>
  /// Doing the way out that was chosen, in the reference's division: the machine's power goes through the
  /// operating system (sleep and hibernate through the same <c>SetSuspendState</c> the reference's
  /// <c>ISystemStateService</c> called, shutdown and restart through the platform's own shutdown command),
  /// and the application's own ways out are handed back to the caller, because leaving and coming back is
  /// the application's business and nobody else's.
  /// </summary>
  public static class SystemStateActions
  {
    /// <summary>
    /// Carrying out one way out. <paramref name="leave"/> and <paramref name="restart"/> answer for the
    /// application itself. A machine asked for a way out it does not have (a Linux asked to hibernate) is
    /// left alone rather than half-slept. The reference closed its dialog before it acted
    /// (<c>BaseSystemStateModel.DoAction</c> began with <c>DoClose</c>); here the dialog has no machine
    /// under it any more either way, so nothing comes back to it.
    /// </summary>
    public static void Execute(ShutdownAction action, Action leave, Action restart)
    {
      switch (action)
      {
        case ShutdownAction.Suspend:
          if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            SetSuspendState(false, false, false);
          return;

        case ShutdownAction.Hibernate:
          if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            SetSuspendState(true, false, false);
          return;

        case ShutdownAction.Shutdown:
          RunShutdown("/s");
          return;

        case ShutdownAction.Restart:
          RunShutdown("/r");
          return;

        case ShutdownAction.CloseMP:
          leave();
          return;

        case ShutdownAction.RestartMP:
          restart();
          return;

        // The sleep timer is never "run" from here: setting it is the timer dialog's business and the timer
        // itself runs out on its own clock (the reference's DoAction answered SleepTimer the same way).
        case ShutdownAction.SleepTimer:
        default:
          return;
      }
    }

    /// <summary>
    /// The platform's own way out, at once and without a countdown window of its own. A machine that
    /// refuses (no right, no command) is answered with the screen staying as it is, because a dialog that
    /// vanished on a refused shutdown would leave the viewer standing in front of a television that never
    /// went dark, wondering.
    /// </summary>
    private static void RunShutdown(string argument)
    {
      if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        return;

      try
      {
        Process.Start(new ProcessStartInfo("shutdown.exe", argument + " /t 0")
        {
          UseShellExecute = false,
          CreateNoWindow = true
        });
      }
      catch (Exception)
      {
        // The machine said no. The reference's service asked the OS and carried on when the OS refused.
      }
    }

    [DllImport("powrprof.dll", SetLastError = true)]
    [SupportedOSPlatform("windows")]
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
  }
}
