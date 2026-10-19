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

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using NUnit.Framework;
using MediaPortal.Frontend.Input;

namespace Tests.Frontend
{
  /// <summary>
  /// The idle watch that decides when the room has gone quiet enough for the screen to go black. The clock
  /// is a delegate precisely so that nobody has to sit and wait for these answers: the tests below turn a
  /// clock by hand and see the watch trip at the configured minute and not before.
  /// </summary>
  [TestFixture]
  public class TestIdleWatch
  {
    private DateTime _now = new DateTime(2026, 3, 1, 20, 0, 0, DateTimeKind.Utc);

    private DateTime Now()
    {
      return _now;
    }

    private void Pass(int minutes)
    {
      _now = _now.AddMinutes(minutes);
    }

    [Test]
    public void ZeroMeansTheScreenNeverGoesAway()
    {
      IdleWatch watch = new IdleWatch(0, Now);

      Assert.That(watch.Armed, Is.False);
      Pass(60 * 24 * 365);
      Assert.That(watch.Poll(), Is.False, "an off screensaver is off, however long the room is empty");
    }

    [Test]
    public void TheWaitIsMeasuredFromTheLastTouch()
    {
      IdleWatch watch = new IdleWatch(10, Now);

      Pass(9);
      Assert.That(watch.Poll(), Is.False, "five minutes early is five minutes too early for a black screen");
      Pass(1);
      Assert.That(watch.Poll(), Is.True, "the configured minute is the minute");

      Pass(10);
      watch.Touch();
      Assert.That(watch.Poll(), Is.False, "the touch of the remote that woke the screen restarts the wait");
      Pass(10);
      Assert.That(watch.Poll(), Is.True);
    }

    [Test]
    public void AWatchStaysAsleepUntilSomethingTouchesIt()
    {
      IdleWatch watch = new IdleWatch(5, Now);

      Pass(5);
      Assert.That(watch.Poll(), Is.True);
      Pass(1);
      Assert.That(watch.Poll(), Is.True,
          "a timer poll cannot wake the picture - only the room touching something can");
      watch.Touch();
      Assert.That(watch.Poll(), Is.False);
    }

    [Test]
    public void TurningItOffWhileItSleepsWakesTheScreen()
    {
      IdleWatch watch = new IdleWatch(5, Now);

      Pass(5);
      Assert.That(watch.Poll(), Is.True);

      watch.WaitMinutes = 0;
      Assert.That(watch.Poll(), Is.False, "turning the screensaver off means the black goes away, now");
    }
  }
}
