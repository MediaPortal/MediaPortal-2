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
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using MediaPortal.Common.TaskScheduler;
using Microsoft.Win32.SafeHandles;

namespace MediaPortal.Common.Services.TaskScheduler
{
  /// <summary>
  /// Implements a timer which the process can be waiting on. The timer supports waking up the system from a suspended state.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This was <c>TaskWaitableTimer</c> in <c>MediaPortal.Common</c>, where it was the only
  /// implementation of everything the task scheduler needed from a timer. It is the last Kernel32
  /// <c>[DllImport]</c> in the core, and it is genuinely Win32 rather than merely written with it:
  /// <c>CreateWaitableTimer</c>/<c>SetWaitableTimer</c> with <c>bResume: true</c> is what brings a
  /// suspended machine out of standby in time for a recording, and no thread in a portable assembly
  /// can do that. So the class stayed Win32 and moved here, while <c>PortableTaskWakeUpTimer</c> in
  /// the core covers the scheduling half everywhere else; <see cref="ITaskWakeUpTimer"/> holds the
  /// reasoning.
  /// </para>
  /// <para>
  /// The implementation is unchanged from the core, including the dedicated waiting thread and the
  /// handshake event that used to block the caller until the Win32 timer was armed. What it now
  /// reports through <see cref="WakesSystemFromSuspend"/> is the capability the portable timer
  /// cannot offer.
  /// </para>
  /// </remarks>
  public class WindowsTaskWaitableTimer : WaitHandle, ITaskWakeUpTimer
  {
    /// <summary>
    /// Wrap the system function <i>SetWaitableTimer</i>.
    /// </summary>
    [DllImport("Kernel32.dll", EntryPoint = "SetWaitableTimer", SetLastError = true)]
    private static extern bool SetWaitableTimer(SafeWaitHandle hTimer, ref Int64 pDue, Int32 lPeriod, IntPtr rNotify, IntPtr pArgs, bool bResume);

    /// <summary>
    /// Wrap the system function <i>CreateWaitableTimer</i>.
    /// </summary>
    [DllImport("Kernel32.dll", EntryPoint = "CreateWaitableTimer")]
    private static extern SafeWaitHandle CreateWaitableTimer(IntPtr pSec, bool bManual, string szName);

    /// <summary>
    /// Wrap the system function <i>CancelWaitableTimer</i>.
    /// </summary>
    [DllImport("Kernel32.dll", EntryPoint = "CancelWaitableTimer")]
    private static extern bool CancelWaitableTimer(SafeWaitHandle hTimer);

    /// <summary>
    /// Clients can register for the expiration of this timer.
    /// </summary>
    public event ITaskWakeUpTimer.TimerExpiredHandler OnTimerExpired;

    public event ITaskWakeUpTimer.TimerExceptionHandler OnTimerException;

    /// <summary>
    /// This is the one implementation that does wake the machine: the Win32 waitable timer is armed
    /// with <c>bResume: true</c>.
    /// </summary>
    public bool WakesSystemFromSuspend
    {
      get { return true; }
    }

    /// <summary>
    /// The <see cref="CancellationTokenSource"/> will be created by <see cref="SecondsToWait"/> and
    /// can be used to cancel the <see cref="_waitThread"/>.
    /// </summary>
    private CancellationTokenSource _waitCancellationTokenSource = null;

    /// <summary>
    /// This <see cref="Thread"/> will be create by <see cref="SecondsToWait"/> and
    /// runs <see cref="WaitThread"/>.
    /// </summary>
    private Thread _waitThread = null;

    /// <summary>
    /// <see cref="DateTime.ToFileTime"/> of the time when the timer should
    /// expire.
    /// </summary>
    private long _interval = 0;

    /// <summary>
    /// Create the timer. The caller should call <see cref="Close"/> as soon as
    /// the timer is no longer needed.
    /// </summary>
    /// <remarks>
    /// <see cref="WaitHandle.SafeWaitHandle"/> will be used to store the system API handle of the newly created timer.
    /// </remarks>
    /// <exception cref="TimerException">When the timer could not be created.</exception>
    public WindowsTaskWaitableTimer()
    {
      // Create it
      SafeWaitHandle = CreateWaitableTimer(IntPtr.Zero, false, null);

      // Test
      if (SafeWaitHandle.Equals(IntPtr.Zero))
        throw new TimerException("Unable to create Waitable Timer");
    }

    /// <summary>
    /// Make sure that <see cref="Close"/> is called.
    /// </summary>
    ~WindowsTaskWaitableTimer()
    {
      // Forward
      Close();
    }

    /// <summary>
    /// Stop <see cref="_waitThread"/> if necessary. The thread is not aborted - which no .NET outside the
    /// Windows Framework allows - but asked to stop through <see cref="_waitCancellationTokenSource"/>, whose
    /// wait handle the thread waits on together with the timer, and then joined.
    /// <seealso cref="SecondsToWait"/>
    /// <seealso cref="Close"/>
    /// </summary>
    private void AbortWaiter()
    {
      if (_waitThread == null)
        return;

      // Terminate it
      try
      {
        _waitCancellationTokenSource.Cancel();
        _waitThread.Join();
      }
      catch { }

      _waitThread = null;
      _waitCancellationTokenSource.Dispose();
      _waitCancellationTokenSource = null;
    }

    /// <summary>
    /// Activate the timer to stop after the indicated number of seconds.
    /// </summary>
    /// <remarks>
    /// This method will always call <see cref="AbortWaiter"/>. If the number
    /// of seconds is positive a new <see cref="_waitThread"/> <see cref="Thread"/>
    /// will be created running <see cref="WaitThread"/>. Before calling
    /// <see cref="Thread.Start()"/> the <see cref="_interval"/> is initialized
    /// with the correct value. If the number of seconds is zero or negative
    /// the timer is canceled.
    /// </remarks>
    public double SecondsToWait
    {
      set
      {
        // Done with thread
        AbortWaiter();
        // Check mode
        if (value > 0)
        {
          // Calculate
          _interval = DateTime.UtcNow.AddSeconds(value).ToFileTimeUtc();

          // Cancellation token used to cancel the thread
          _waitCancellationTokenSource = new CancellationTokenSource();

          // Create thread
          _waitThread = new Thread(WaitThread) { Priority = ThreadPriority.BelowNormal, Name = "WaitTimer" };

          using (ManualResetEvent handshake = new ManualResetEvent(false))
          {
            // Run it
            _waitThread.Start(handshake);
            // wait until wakeup timer is set
            handshake.WaitOne();
          }
        }
        else
        {
          // No timer
          CancelWaitableTimer(SafeWaitHandle);
        }
      }
    }

    /// <summary>
    /// Initializes the timer with <see cref="_interval"/> and waits for it
    /// to expire. If the timer expires <see cref="OnTimerExpired"/> is fired.
    /// </summary>
    /// <remarks>
    /// The <see cref="Thread"/> may be terminated with a call to <see cref="AbortWaiter"/>
    /// before the time expires.
    /// </remarks>
    private void WaitThread(object arg)
    {
      ManualResetEvent initializedEvent = (ManualResetEvent) arg;
      // Ignore aborts
      try
      {
        // Interval to use
        long lInterval = _interval;

        // Start timer
        if (!SetWaitableTimer(SafeWaitHandle, ref lInterval, 0, IntPtr.Zero, IntPtr.Zero, true))
          throw new TimerException("Could not start Timer", new Win32Exception(Marshal.GetLastWin32Error()));

        // Notify caller that we initialized timer properly.
        Set(ref initializedEvent);

        // Wait for the timer to expire
        WaitAny(new WaitHandle[] { this, _waitCancellationTokenSource.Token.WaitHandle });
        _waitCancellationTokenSource.Token.ThrowIfCancellationRequested();

        // Forward
        var onTimerExpired = OnTimerExpired;
        if (onTimerExpired != null)
          onTimerExpired();
      }
      catch (TimerException e)
      {
        Set(ref initializedEvent);

        var onTimerException = OnTimerException;
        if (onTimerException != null)
          onTimerException(this, e);
      }
      catch (OperationCanceledException)
      {
        // We expect that the cancellation gets requested.
      }
      finally
      {
        Set(ref initializedEvent);
      }
    }

    protected void Set(ref ManualResetEvent handler)
    {
      if (handler == null)
        return;
      handler.Set();
      handler = null;
    }

    /// <summary>
    /// Calles <see cref="AbortWaiter"/> and forwards to the base <see cref="WaitHandle.Close"/>
    /// method.
    /// </summary>
    public override void Close()
    {
      // Abort timer
      SecondsToWait = -1;

      // Kill thread
      AbortWaiter();

      // Forward
      base.Close();
    }
  }
}
