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

namespace MediaPortal.Common.TaskScheduler
{
  /// <summary>
  /// Raised by an <see cref="ITaskWakeUpTimer"/> that could not be armed or disarmed.
  /// </summary>
  /// <remarks>
  /// Used to be declared inside <c>TaskWaitableTimer</c>, the only implementation there was. It
  /// moved out with the interface because a portable timer has no failure modes to report and
  /// still has to be able to name the type in its event declaration.
  /// </remarks>
  public class TimerException : Exception
  {
    /// <summary>
    /// Create a new instance of this exception.
    /// </summary>
    public TimerException(string reason) : base(reason) { }

    /// <summary>
    /// Create a new instance of this exception, with the original exception as inner exception.
    /// </summary>
    public TimerException(string reason, Exception innerException) : base(reason, innerException) { }
  }

  /// <summary>
  /// A one-shot timer that fires at a point in time chosen by the caller, used by
  /// <see cref="ITaskScheduler"/> to run tasks whose scheduled moment is still ahead.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the seam between the task scheduler and whatever the operating system offers for
  /// "signal me then". The original was <c>TaskWaitableTimer</c>, a <see cref="System.Threading.WaitHandle"/>
  /// subclass over the Win32 waitable timer, which did two jobs at once:
  /// </para>
  /// <list type="bullet">
  ///   <item><description>fire when the due time is reached - pure scheduling, portable, and</description></item>
  ///   <item><description>wake the machine from standby to do it - a power feature, Win32's
  ///   <c>SetWaitableTimer(..., bResume: true)</c>, and not a thing a thread can do.</description></item>
  /// </list>
  /// <para>
  /// The first half lives in the core as <c>PortableTaskWakeUpTimer</c>; the second half is what
  /// <c>WindowsTaskWaitableTimer</c> in <c>MediaPortal.Common.Windows</c> is for, and
  /// <see cref="WakesSystemFromSuspend"/> is how a caller finds out which one it got. A recording
  /// scheduled for 4 a.m. on a machine that sleeps is the difference between a recording and an
  /// empty file, so the flag is deliberately not silent: the portable timer says in the log that
  /// the machine will have to stay awake by itself.
  /// </para>
  /// </remarks>
  public interface ITaskWakeUpTimer
  {
    /// <summary>
    /// Handler for the expiration of the timer.
    /// </summary>
    delegate void TimerExpiredHandler();

    /// <summary>
    /// Handler for errors reported by the timer.
    /// </summary>
    delegate void TimerExceptionHandler(ITaskWakeUpTimer sender, TimerException exception);

    /// <summary>
    /// Clients register here to be called when the timer expires.
    /// </summary>
    event TimerExpiredHandler OnTimerExpired;

    /// <summary>
    /// Clients register here to learn that the timer could not be armed, or that the expiry handler
    /// threw. The Win32 implementation reports the first kind (the API call failed); the portable one
    /// has no API to fail, so it reports only the second.
    /// </summary>
    event TimerExceptionHandler OnTimerException;

    /// <summary>
    /// Whether letting this timer expire also brings the machine out of standby. False means a
    /// suspended machine stays suspended and the task runs when something else wakes it.
    /// </summary>
    bool WakesSystemFromSuspend { get; }

    /// <summary>
    /// Arm the timer to expire after the given number of seconds from now, or disarm it when the
    /// value is zero or negative. Setting it again replaces the pending expiry, and an expired or
    /// replaced timer never fires afterwards.
    /// </summary>
    double SecondsToWait { set; }

    /// <summary>
    /// Disarm the timer and release everything it holds. Afterwards the timer is unusable.
    /// </summary>
    void Close();
  }

  /// <summary>
  /// Creates <see cref="ITaskWakeUpTimer"/> instances. A service, because <see cref="ITaskScheduler"/>
  /// needs one timer per instance and so cannot take the timer itself as a singleton service.
  /// </summary>
  /// <remarks>
  /// <c>ApplicationCore.RegisterCoreServices()</c> registers the portable factory; a Windows host
  /// registers <c>WindowsTaskWakeUpTimerFactory</c> from <c>MediaPortal.Common.Windows</c> before
  /// that call, which keeps the wake-from-standby behaviour for TV recordings.
  /// </remarks>
  public interface ITaskWakeUpTimerFactory
  {
    /// <summary>
    /// Create a timer that has not been armed yet. The caller owns it and must call
    /// <see cref="ITaskWakeUpTimer.Close"/>.
    /// </summary>
    ITaskWakeUpTimer CreateWakeUpTimer();
  }
}
