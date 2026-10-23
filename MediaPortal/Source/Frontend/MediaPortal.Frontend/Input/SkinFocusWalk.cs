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
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;

namespace MediaPortal.Frontend.Input
{
  /// <summary>
  /// Which control lies next in the direction a remote or a keyboard asked for, measured the way the reference's
  /// own skin engine measures it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is a translation of <c>FrameworkElement.FindNextFocusElement</c> and its four <c>Located…</c>
  /// predicates (SkinEngine/Controls/Visuals/FrameworkElement.cs:1032-1215), which is what every MP2 screen -
  /// the Nereus home among them - uses to answer the arrow keys. The rules it encodes are not obvious and none
  /// of them is a refinement of anything; they are the reason a remote walks a wall the way a viewer expects:
  /// </para>
  /// <list type="bullet">
  ///   <item>
  ///     A candidate lies in the direction asked for if a line from the middle of the edge the viewer is moving
  ///     away from, to the middle of the candidate's facing edge, points that way within eighty-four degrees -
  ///     or if the two rects merely touch on that side. An angle, not a centre: a cover can be a whole band to
  ///     the side and still count as "above" as long as the eye reads the step as upward, while a tile whose
  ///     centre is a few lines above but which stands beside the current one does not.
  ///   </item>
  ///   <item>
  ///     Of the candidates that lie that way, the one the walk takes is the one whose near edge is best lined up
  ///     with the current rect along the axis being crossed (<c>topOrLeftDifference</c>, negative when the two
  ///     overlap), and between two equally lined up ones the one whose borders are nearest
  ///     (<c>BorderDistance</c>, the gap between the rects themselves - never the distance between their
  ///     centres).
  ///   </item>
  ///   <item>A candidate whose centre is exactly the current centre is not a move at all and is skipped.</item>
  /// </list>
  /// <para>
  /// Scoring centres instead - which is what the first version of this front end did - puts a small tile that
  /// merely happens to stand beside a big one ahead of the tile the viewer can see above it: on the Nereus wall
  /// the shortcut squares are a third of the size of the covers, so their centres sit nearer the middle of the
  /// screen than the centres of the covers above and below, and pressing up from the top row of covers landed on
  /// a shortcut instead of leaving the wall. Measuring rects, as the reference does, is what makes the same key
  /// go where it points.
  /// </para>
  /// </remarks>
  public static class SkinFocusWalk
  {
    /// <summary>The reference's <c>UIElement.DELTA_DOUBLE</c>: how near two numbers have to be to be the same.</summary>
    private const double DELTA = 0.01;

    /// <summary>
    /// The candidate that lies next to <paramref name="from"/> in <paramref name="direction"/>, or <c>null</c>
    /// when none of them does.
    /// </summary>
    public static T? Next<T>(IEnumerable<T> candidates, Func<T, Rect> boundsOf, Rect from, Key direction)
        where T : class
    {
      T? best = null;
      double bestDistance = double.MaxValue;
      double bestTopOrLeft = double.MaxValue;

      foreach (T candidate in candidates)
      {
        Rect rect = boundsOf(candidate);
        if (rect.Width <= DELTA || rect.Height <= DELTA)
          continue; // not laid out yet, so nothing can be said about where it stands

        double topOrLeft;
        if (!LiesIn(direction, rect, from, out topOrLeft))
          continue;

        double centerDistance = CenterDistance(rect, from);
        if (IsNear(centerDistance, 0))
          continue; // the reference's own rule: the same centre is not a move

        double distance = BorderDistance(rect, from);
        if (best == null
            || (distance < bestDistance && topOrLeft < 0)
            || (topOrLeft < bestTopOrLeft && (IsNear(distance, bestDistance) || bestTopOrLeft >= 0)))
        {
          best = candidate;
          bestDistance = distance;
          bestTopOrLeft = topOrLeft;
        }
      }

      return best;
    }

    /// <summary>
    /// Whether <paramref name="candidate"/> stands in <paramref name="direction"/> as seen from
    /// <paramref name="from"/>, which is the same test the walk above picks its winner out of.
    /// </summary>
    public static bool LiesIn(Key direction, Rect candidate, Rect from, out double topOrLeftDifference)
    {
      switch (direction)
      {
        case Key.Up:
          return LocatedAbove(candidate, from, out topOrLeftDifference);
        case Key.Down:
          return LocatedBelow(candidate, from, out topOrLeftDifference);
        case Key.Left:
          return LocatedLeftOf(candidate, from, out topOrLeftDifference);
        case Key.Right:
          return LocatedRightOf(candidate, from, out topOrLeftDifference);
        default:
          topOrLeftDifference = 0;
          return false;
      }
    }

    private static bool LocatedAbove(Rect candidate, Rect other, out double topOrLeftDifference)
    {
      bool isNear = IsNear(candidate.Bottom, other.Top);
      double alpha = Direction(
          new Point((candidate.Right + candidate.Left) / 2, candidate.Bottom),
          new Point((other.Right + other.Left) / 2, other.Top));
      topOrLeftDifference = HorizontalDistance(candidate, other);
      return isNear || (alpha > Math.PI + DELTA && alpha < 2 * Math.PI - DELTA);
    }

    private static bool LocatedBelow(Rect candidate, Rect other, out double topOrLeftDifference)
    {
      bool isNear = IsNear(candidate.Top, other.Bottom);
      double alpha = Direction(
          new Point((candidate.Right + candidate.Left) / 2, candidate.Top),
          new Point((other.Right + other.Left) / 2, other.Bottom));
      topOrLeftDifference = HorizontalDistance(candidate, other);
      return isNear || (alpha > DELTA && alpha < Math.PI - DELTA);
    }

    private static bool LocatedLeftOf(Rect candidate, Rect other, out double topOrLeftDifference)
    {
      bool isNear = IsNear(candidate.Right, other.Left);
      double alpha = Direction(
          new Point(candidate.Right, (candidate.Top + candidate.Bottom) / 2),
          new Point(other.Left, (other.Top + other.Bottom) / 2));
      topOrLeftDifference = VerticalDistance(candidate, other);
      return isNear || (alpha < Math.PI / 2 - DELTA || alpha > 3 * Math.PI / 2 + DELTA);
    }

    private static bool LocatedRightOf(Rect candidate, Rect other, out double topOrLeftDifference)
    {
      bool isNear = IsNear(candidate.Left, other.Right);
      double alpha = Direction(
          new Point(candidate.Left, (candidate.Top + candidate.Bottom) / 2),
          new Point(other.Right, (other.Top + other.Bottom) / 2));
      topOrLeftDifference = VerticalDistance(candidate, other);
      return isNear || (alpha > Math.PI / 2 + DELTA && alpha < 3 * Math.PI / 2 - DELTA);
    }

    /// <summary>
    /// The gap between two rects, measured between their borders rather than between their middles - two covers
    /// a hand's breadth apart are one hand's breadth apart however big they are.
    /// </summary>
    private static double BorderDistance(Rect r1, Rect r2)
    {
      double distX = r1.Left > r2.Right ? r1.Left - r2.Right
          : r1.Right < r2.Left ? r2.Left - r1.Right
          : 0;
      double distY = r1.Top > r2.Bottom ? r1.Top - r2.Bottom
          : r1.Bottom < r2.Top ? r2.Top - r1.Top
          : 0;
      return Math.Sqrt(distX * distX + distY * distY);
    }

    private static double CenterDistance(Rect r1, Rect r2)
    {
      double distX = Math.Abs((r1.Left + r1.Right) / 2 - (r2.Left + r2.Right) / 2);
      double distY = Math.Abs((r1.Top + r1.Bottom) / 2 - (r2.Top + r2.Bottom) / 2);
      return Math.Sqrt(distX * distX + distY * distY);
    }

    /// <summary>The horizontal gap between two rects, negative when they overlap sideways.</summary>
    private static double HorizontalDistance(Rect r1, Rect r2)
    {
      if (r1.Right <= r2.Left)
        return r2.Left - r1.Right;
      if (r1.Left >= r2.Right)
        return r1.Left - r2.Right;
      if (r1.Left < r2.Left && r1.Right < r2.Right)
        return r2.Left - r1.Right;
      if (r1.Right > r2.Right && r1.Left > r2.Left)
        return r1.Left - r2.Right;
      return -r1.Width;
    }

    /// <summary>The vertical gap between two rects, negative when they overlap up and down.</summary>
    private static double VerticalDistance(Rect r1, Rect r2)
    {
      if (r1.Bottom <= r2.Top)
        return r2.Top - r1.Bottom;
      if (r1.Top >= r2.Bottom)
        return r1.Top - r2.Bottom;
      if (r1.Top < r2.Top && r1.Bottom < r2.Bottom)
        return r2.Top - r1.Bottom;
      if (r1.Bottom > r2.Bottom && r1.Top > r2.Top)
        return r1.Top - r2.Bottom;
      return -r1.Height;
    }

    /// <summary>
    /// The bearing from one point to another, in radians clockwise from straight right, with the y axis pointing
    /// down the screen as it does on every window - the reference inverts its angle for exactly that.
    /// </summary>
    private static double Direction(Point start, Point end)
    {
      if (IsNear(start.X, end.X) && IsNear(start.Y, end.Y))
        return double.NaN;

      double x = end.X - start.X;
      double y = end.Y - start.Y;
      double alpha = Math.Acos(x / Math.Sqrt(x * x + y * y));
      if (end.Y > start.Y)
        alpha = -alpha;
      if (alpha < 0)
        alpha += 2 * Math.PI;
      return alpha;
    }

    private static bool IsNear(double x, double y)
    {
      return Math.Abs(x - y) < DELTA;
    }
  }
}
