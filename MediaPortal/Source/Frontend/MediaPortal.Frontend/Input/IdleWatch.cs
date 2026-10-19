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

namespace MediaPortal.Frontend.Input
{
  /// <summary>
  /// How long the room has been silent, and whether that silence has run long enough for the screen to go
  /// away.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference asked Windows how long its keyboard had been untouched, which is an answer only Windows
  /// gives. This asks the front end's own window instead: whatever press, click or move arrives calls
  /// <see cref="Touch"/>, and a poll says whether the quiet since then has passed the configured wait. That
  /// is the same question portable to every platform the front end runs on, and it errs in the direction a
  /// media centre should err in - a window that hears nothing is a window nobody is watching.
  /// </para>
  /// <para>
  /// The clock is a delegate so the waiting can be tested without anybody waiting: a test hands a clock it
  /// turns by hand and sees the watch trip at the configured minute and not before.
  /// </para>
  /// </remarks>
  public sealed class IdleWatch
  {
    private readonly Func<DateTime> _now;
    private DateTime _lastTouch;

    public IdleWatch(int waitMinutes, Func<DateTime>? clock = null)
    {
      _now = clock ?? (() => DateTime.UtcNow);
      _lastTouch = _now();
      WaitMinutes = waitMinutes;
    }

    /// <summary>How long the silence must run; zero - the shipped answer - means the screen never goes away.</summary>
    public int WaitMinutes { get; set; }

    /// <summary>Whether anything here is switched on at all.</summary>
    public bool Armed
    {
      get { return WaitMinutes > 0; }
    }

    /// <summary>Whether the screen is black right now, as the last poll answered.</summary>
    public bool Asleep { get; private set; }

    /// <summary>The room touched something: the wait starts again and the screen comes back.</summary>
    public void Touch()
    {
      _lastTouch = _now();
      Asleep = false;
    }

    /// <summary>
    /// Whether the screen should be black as of now. Stays asleep once asleep - only a <see cref="Touch"/>
    /// wakes it - so a poll from a timer cannot wake the picture on its own.
    /// </summary>
    public bool Poll()
    {
      if (!Armed)
      {
        Asleep = false;
        return false;
      }

      if (!Asleep && _now() - _lastTouch >= TimeSpan.FromMinutes(WaitMinutes))
        Asleep = true;

      return Asleep;
    }
  }
}
