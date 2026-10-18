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
using MediaPortal.Common.Services.TaskScheduler;
using MediaPortal.Common.TaskScheduler;
using NUnit.Framework;

namespace Tests.Server.Services.TaskScheduler
{
  /// <summary>
  /// Behaviour of the portable wake-up timer, i.e. the rules <see cref="ITaskWakeUpTimer"/> has to
  /// keep for the task scheduler to work.
  /// </summary>
  /// <remarks>
  /// These are the rules the Win32 waitable timer enforced with a handshake thread and a
  /// cancellation token, which is what the portable rewrite had to reproduce. The waits below use
  /// wide margins on purpose: this machine sometimes starves timing-sensitive tests (see the
  /// "Known flakes" note in scripts/ci/README.md), and a timer that fires late is not the bug this
  /// file is looking for. A timer that fires after being replaced or closed is.
  /// </remarks>
  [TestFixture]
  public class TestPortableTaskWakeUpTimer
  {
    /// <summary>
    /// How long a test waits for an expiry that should happen. Generous, see the class remarks.
    /// </summary>
    private const int ShouldFireTimeoutMs = 10000;

    private static ITaskWakeUpTimer CreateTimer(out ManualResetEventSlim expired)
    {
      ITaskWakeUpTimer timer = new PortableTaskWakeUpTimerFactory().CreateWakeUpTimer();
      // A local, not the out parameter: a delegate may not capture an out parameter (CS1628).
      ManualResetEventSlim signal = new ManualResetEventSlim(false);
      expired = signal;
      timer.OnTimerExpired += delegate
      {
        signal.Set();
      };
      return timer;
    }

    [Test]
    public void TimerFiresAfterTheRequestedDelay()
    {
      ManualResetEventSlim expired;
      ITaskWakeUpTimer timer = CreateTimer(out expired);
      try
      {
        timer.SecondsToWait = 1;
        Assert.IsFalse(expired.IsSet, "the timer must not have expired before its due time");
        Assert.IsTrue(expired.Wait(ShouldFireTimeoutMs), "the timer did not fire");
      }
      finally
      {
        timer.Close();
        expired.Dispose();
      }
    }

    [Test]
    public void RearmingPreventsThePreviousExpiryFromFiring()
    {
      ManualResetEventSlim expired;
      ITaskWakeUpTimer timer = CreateTimer(out expired);
      try
      {
        timer.SecondsToWait = 1;
        timer.SecondsToWait = 1000;
        Assert.IsFalse(expired.Wait(2000), "the replaced arming fired");

        // The second arming is the live one, so disarming it has to be enough to silence the timer.
        timer.SecondsToWait = -1;
        Assert.IsFalse(expired.Wait(1500), "the timer fired after it was disarmed");
      }
      finally
      {
        timer.Close();
        expired.Dispose();
      }
    }

    [Test]
    public void ZeroDisarmsTheTimer()
    {
      ManualResetEventSlim expired;
      ITaskWakeUpTimer timer = CreateTimer(out expired);
      try
      {
        timer.SecondsToWait = 1;
        timer.SecondsToWait = 0;
        Assert.IsFalse(expired.Wait(2000), "the timer fired although it was disarmed");
      }
      finally
      {
        timer.Close();
        expired.Dispose();
      }
    }

    [Test]
    public void ClosePreventsTheTimerFromFiring()
    {
      ManualResetEventSlim expired;
      ITaskWakeUpTimer timer = CreateTimer(out expired);
      timer.SecondsToWait = 1;
      timer.Close();
      Assert.IsFalse(expired.Wait(2000), "the timer fired after Close");
      expired.Dispose();
    }

    [Test]
    public void ArmingAfterCloseThrowsObjectDisposed()
    {
      ManualResetEventSlim expired;
      ITaskWakeUpTimer timer = CreateTimer(out expired);
      timer.Close();
      Assert.Throws<ObjectDisposedException>(delegate
      {
        timer.SecondsToWait = 1;
      });
      expired.Dispose();
    }

    [Test]
    public void ExpiringTwiceIsNotPossible()
    {
      ManualResetEventSlim expired;
      ITaskWakeUpTimer timer = CreateTimer(out expired);
      try
      {
        int count = 0;
        timer.OnTimerExpired += delegate
        {
          Interlocked.Increment(ref count);
        };
        timer.SecondsToWait = 1;
        Assert.IsTrue(expired.Wait(ShouldFireTimeoutMs), "the timer did not fire");
        Thread.Sleep(2000);
        Assert.AreEqual(1, count, "a one-shot timer fired more than once");
      }
      finally
      {
        timer.Close();
        expired.Dispose();
      }
    }

    [Test]
    public void AThrowingHandlerIsReportedAndDoesNotEscape()
    {
      // The handler runs on a thread pool thread, so an exception escaping from it would take the
      // process down and this test would fail by crashing the test host rather than by asserting.
      ITaskWakeUpTimer timer = new PortableTaskWakeUpTimer();
      TimerException reported = null;
      ManualResetEventSlim reportedEvent = new ManualResetEventSlim(false);
      timer.OnTimerExpired += delegate
      {
        throw new InvalidOperationException("handler failure");
      };
      timer.OnTimerException += delegate(ITaskWakeUpTimer sender, TimerException exception)
      {
        reported = exception;
        reportedEvent.Set();
      };
      try
      {
        timer.SecondsToWait = 1;
        Assert.IsTrue(reportedEvent.Wait(ShouldFireTimeoutMs), "the failure was not reported");
        Assert.IsNotNull(reported);
        Assert.IsInstanceOf<InvalidOperationException>(reported.InnerException);
      }
      finally
      {
        timer.Close();
        reportedEvent.Dispose();
      }
    }

    [Test]
    public void PortableFactoryReportsThatItCannotWakeTheMachine()
    {
      ITaskWakeUpTimer timer = new PortableTaskWakeUpTimerFactory().CreateWakeUpTimer();
      try
      {
        Assert.IsFalse(timer.WakesSystemFromSuspend);
      }
      finally
      {
        timer.Close();
      }
    }
  }
}
