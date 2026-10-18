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
using MediaPortal.Common.Logging;
using MediaPortal.Common.TaskScheduler;

namespace MediaPortal.Common.Services.TaskScheduler
{
  /// <summary>
  /// Portable <see cref="ITaskWakeUpTimer"/>: fires when the due time is reached, on a thread pool
  /// thread, using nothing but <see cref="System.Threading.Timer"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the scheduling half of what <c>TaskWaitableTimer</c> did, without the Win32 handle.
  /// The one thing it cannot do is wake a sleeping machine, which is why it reports
  /// <see cref="WakesSystemFromSuspend"/> as false and says so in the log the first time it is
  /// armed - see <see cref="ITaskWakeUpTimer"/> for why that distinction is worth a log line.
  /// </para>
  /// <para>
  /// The original blocked the caller on a handshake event until its dedicated thread had armed the
  /// Win32 timer, and cancelled that thread with a <see cref="CancellationToken"/> to re-arm.
  /// <see cref="Timer"/> arms synchronously in its constructor and its callbacks run on the thread
  /// pool, so neither the handshake thread nor the cancellation token is needed; what has to be
  /// preserved is the rule the handshake and the token existed to enforce, namely that a timer which
  /// was re-armed, disarmed or closed never calls <see cref="OnTimerExpired"/> afterwards.
  /// </para>
  /// <para>
  /// That rule is enforced by deciding whether to fire <em>inside</em> <see cref="_sync"/>, against
  /// the identity of the pending arming (<see cref="_current"/>). Disposing a <see cref="Timer"/>
  /// does not wait for a callback that is already running, so a callback may reach this method a
  /// moment after its arming was replaced; it then finds <c>_current</c> pointing elsewhere and does
  /// nothing. Comparing a generation counter instead would leave a window in which a callback from
  /// the replaced arming sees the new counter and fires.
  /// </para>
  /// </remarks>
  public sealed class PortableTaskWakeUpTimer : ITaskWakeUpTimer
  {
    #region Constants

    /// <summary>
    /// Upper bound for <see cref="SecondsToWait"/>. A due time further away than this is clamped,
    /// because a wait longer than ten years cannot be distinguished from "no task will ever run" and
    /// <see cref="TimeSpan"/> would start throwing long before it.
    /// </summary>
    private static readonly TimeSpan MaxDueTime = TimeSpan.FromDays(3650);

    #endregion

    #region Nested types

    /// <summary>
    /// One arming of this timer. Instances are compared by identity to decide whether a callback
    /// still belongs to the pending arming, which is why each arm gets its own object.
    /// </summary>
    private sealed class Arm
    {
      public Timer Timer;
    }

    #endregion

    #region Fields

    private readonly object _sync = new object();

    /// <summary>
    /// The arming that is currently allowed to fire, null while the timer is disarmed.
    /// </summary>
    private Arm _current;

    private bool _closed;

    private bool _noWakeReported;

    #endregion

    #region Events

    public event ITaskWakeUpTimer.TimerExpiredHandler OnTimerExpired;

    public event ITaskWakeUpTimer.TimerExceptionHandler OnTimerException;

    #endregion

    #region Private methods

    /// <summary>
    /// Disposes a timer that is no longer the pending arming. Never called with <see cref="_sync"/>
    /// held: <see cref="Timer.Dispose()"/> may wait for a callback, and a callback wants the lock.
    /// </summary>
    private static void DisposeStaleTimer(Timer stale)
    {
      if (stale != null)
        stale.Dispose();
    }

    /// <summary>
    /// <see cref="Timer"/> callback. Fires the event only if this arming is still the pending one.
    /// </summary>
    private void Expired(object state)
    {
      Arm arm = state as Arm;
      lock (_sync)
      {
        if (_closed || !ReferenceEquals(_current, arm))
          return;
        // One-shot: an expiry consumes the arming, so a later Close() or re-arm cannot make it fire twice.
        _current = null;
      }

      ITaskWakeUpTimer.TimerExpiredHandler handler = OnTimerExpired;
      if (handler == null)
        return;

      try
      {
        handler();
      }
      catch (Exception e)
      {
        // A callback that throws must not take the process down. It would if it escaped: this runs on
        // a thread pool thread, and an unhandled exception on one terminates the runtime. The Win32
        // implementation had the same exposure on its own waiting thread, so this is not a behaviour
        // change the portable timer is getting away with - it is the seam's error channel being used
        // for the one thing that can actually go wrong here.
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Error("TaskWakeUpTimer: the scheduled wake-up handler threw, the task scheduler is unaffected", e);

        ITaskWakeUpTimer.TimerExceptionHandler errorHandler = OnTimerException;
        if (errorHandler != null)
          errorHandler(this, new TimerException("The wake-up handler threw an exception", e));
      }
    }

    /// <summary>
    /// Says once that expiring this timer does not wake the machine. Warn, not Info: on a machine
    /// that sleeps this turns "record at 4 a.m." into a missing recording, and the user can only act
    /// on it (disable suspend, or run the Windows build) if it is in the log.
    /// </summary>
    private void ReportNoWake()
    {
      if (_noWakeReported)
        return;
      _noWakeReported = true;
      ILogger logger = ServiceRegistration.Get<ILogger>(false);
      if (logger != null)
        logger.Warn("TaskWakeUpTimer: this platform build can wake the application at a scheduled time but not the machine, so a scheduled task only runs if the machine is awake (see the 'power backend' entry in docs/multiplatform/ROADMAP.md).");
    }

    #endregion

    #region ITaskWakeUpTimer implementation

    public bool WakesSystemFromSuspend
    {
      get { return false; }
    }

    public double SecondsToWait
    {
      set
      {
        Timer stale;
        bool armed = false;
        lock (_sync)
        {
          if (_closed)
            throw new ObjectDisposedException(nameof(PortableTaskWakeUpTimer));

          stale = _current != null ? _current.Timer : null;
          _current = null;

          if (!double.IsNaN(value) && value > 0)
          {
            TimeSpan due = TimeSpan.FromSeconds(Math.Min(value, MaxDueTime.TotalSeconds));
            Arm arm = new Arm();
            arm.Timer = new Timer(Expired, arm, due, Timeout.InfiniteTimeSpan);
            _current = arm;
            armed = true;
          }
        }

        DisposeStaleTimer(stale);
        if (armed)
          ReportNoWake();
      }
    }

    public void Close()
    {
      Timer stale;
      lock (_sync)
      {
        _closed = true;
        stale = _current != null ? _current.Timer : null;
        _current = null;
      }

      DisposeStaleTimer(stale);
    }

    #endregion
  }
}
