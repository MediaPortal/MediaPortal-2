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
using Avalonia;

namespace MediaPortal.Frontend.Input
{
  /// <summary>
  /// Whether the mouse pointer has earned to be seen: a hand that has not moved it for a while does not need it,
  /// and a pointer that comes back for every shiver of a touchpad is a pointer that argues with the remote.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A media centre is steered from a sofa. The pointer that sits on the tile it was last left on is not only in
  /// the way of the picture - it keeps that tile dressed as if something stood on it, which is exactly what a
  /// viewer walking the menu with the remote then sees. So after a quiet spell the pointer goes out of sight, and
  /// it comes back when someone reaches for it again.
  /// </para>
  /// <para>
  /// Both halves of that turn on one fact a desk machine keeps proving: it reports movements nobody made. Measured
  /// on this front end, a machine with a hand perfectly still on its lap sends a mouse movement about twice a
  /// second, all of them carrying the position the pointer has not left - so a watch that restarted its count at
  /// every report waited for a quiet that could never arrive, and the pointer stayed. Every report is therefore
  /// weighed rather than believed: a movement has to leave the place the pointer was last seen at by
  /// <see cref="StillDip"/> before the quiet starts again, and - once the pointer is away - it has to cover
  /// <see cref="WakeDip"/> from where it came to rest before it is taken for somebody reaching for the machine.
  /// The first threshold is a couple of device-independent pixels, which no shiver of a touchpad crosses and no
  /// still machine reports; the second is a distance a real hand covers in the first few milliseconds of a
  /// movement, so the wait for the pointer is never noticeable.
  /// </para>
  /// <para>
  /// Like <see cref="IdleWatch"/>, this knows nothing about windows: it is told where the pointer is and when the
  /// clock says so, and it answers whether the pointer is to be seen. The clock is a delegate so that the ten
  /// seconds and the thresholds can be asked about without anybody waiting for either.
  /// </para>
  /// </remarks>
  public sealed class PointerRest
  {
    /// <summary>The quiet the pointer is given before it goes out of sight.</summary>
    public const int DefaultQuietMs = 10000;

    /// <summary>
    /// How far the pointer has to have come from where it was last seen before a report is taken for a movement
    /// at all, in device-independent pixels. A machine that reports a position nobody changed is what this is for.
    /// </summary>
    public const double DefaultStillDip = 2;

    /// <summary>
    /// How far the pointer has to travel before a movement is taken for a hand rather than for a shiver, in
    /// device-independent pixels - about six millimetres on a television panel's worth of scaling.
    /// </summary>
    public const double DefaultWakeDip = 24;

    private readonly Func<DateTime> _now;
    private DateTime _lastStir;
    private Point _at;
    private Point _cameToRestAt;

    public PointerRest(int quietMs = DefaultQuietMs, double stillDip = DefaultStillDip,
        double wakeDip = DefaultWakeDip, Func<DateTime>? clock = null)
    {
      _now = clock ?? (() => DateTime.UtcNow);
      QuietMs = quietMs;
      StillDip = stillDip;
      WakeDip = wakeDip;
      _lastStir = _now();
    }

    /// <summary>How long the pointer must have been untouched before it goes out of sight; zero puts it away never.</summary>
    public int QuietMs { get; set; }

    /// <summary>The least distance a report has to show to count as a movement. See the type's remarks.</summary>
    public double StillDip { get; }

    /// <summary>The distance a movement has to cover to bring the pointer back. See the type's remarks.</summary>
    public double WakeDip { get; }

    /// <summary>Whether the pointer is out of sight right now, as the last answer went.</summary>
    public bool Resting { get; private set; }

    /// <summary>
    /// The pointer moved to <paramref name="to"/>. Answers whether that changed whether it is to be seen - which
    /// while it is resting means the threshold was covered and a hand is to be believed.
    /// </summary>
    public bool Move(Point to)
    {
      if (!Resting)
      {
        double settled = FromHereTo(to);
        if (settled < StillDip * StillDip)
        {
          // The same place again, or a hair off it: the machine reporting what it reported a moment ago. The
          // quiet is none the older for it, which is the only way a pointer on a still desk ever rests.
          return false;
        }

        _at = to;
        _lastStir = _now();
        return false;
      }

      // Measured from where it came to rest, not from the last report: a shiver here and a shiver there in the
      // same direction do add up to somebody sliding a finger across the pad, and that is meant to be believed.
      double reached = FromHereTo(to, _cameToRestAt);
      if (reached < WakeDip * WakeDip)
        return false;

      _at = to;
      _lastStir = _now();
      Resting = false;
      return true;
    }

    /// <summary>
    /// A press or a wheel. Whatever the pointer has drifted since, a hand that clicks wants to see it now, so this
    /// brings it back over any threshold; answers whether that changed anything.
    /// </summary>
    public bool Stir()
    {
      _lastStir = _now();

      if (!Resting)
        return false;

      Resting = false;
      return true;
    }

    /// <summary>
    /// The beat: puts the pointer out of sight once the quiet has run long enough, and remembers the place it came
    /// to rest at - the threshold for coming back is measured from there. Answers whether this changed anything,
    /// which is "no" for every beat after the first.
    /// </summary>
    public bool Poll()
    {
      if (Resting || QuietMs <= 0)
        return false;

      if (_now() - _lastStir < TimeSpan.FromMilliseconds(QuietMs))
        return false;

      _cameToRestAt = _at;
      Resting = true;
      return true;
    }

    /// <summary>Squared distance, because a threshold only ever asks "further than" and a square root is a waste.</summary>
    private double FromHereTo(Point to)
    {
      return FromHereTo(to, _at);
    }

    private static double FromHereTo(Point to, Point from)
    {
      double dx = to.X - from.X;
      double dy = to.Y - from.Y;
      return dx * dx + dy * dy;
    }
  }
}
