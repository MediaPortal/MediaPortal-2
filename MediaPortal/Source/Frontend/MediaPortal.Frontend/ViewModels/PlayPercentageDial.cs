/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
    General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Globalization;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The round progress figure the reference stood in the middle of its detail panels: a ring that says how far
  /// this viewer got, and the number of percent standing in its middle.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference drew this as forty pieces of XAML rather than as a control: twenty dark sector pairs that
  /// stand whether anything was watched or not, and twenty lit sectors, each one shown by its own condition on
  /// <c>PlayPercentage</c> (<c>Skin/Nereus/screens/MediaInfoMovieDetails.inc:216-338</c>, the styles
  /// <c>CircularBackgroundStyle_0</c> and <c>CircularIndicatorStyle_0</c> in
  /// <c>Skin/Nereus/themes/default/styles/CircularIndicator.xaml:8-45</c>). Every condition was written out by
  /// hand, which settles the question a dial usually gets asked wrong: a title at nought percent lights nothing
  /// at all - its first condition was written "> 0" and not ">= 0".
  /// </para>
  /// <para>
  /// This ring tells the same figure as one sweep instead of twenty steps, which is away from the reference and
  /// was asked for that way. What stayed the reference's is everything that can be measured: the box of a hundred
  /// and fifty points shrunk to seven tenths of itself, the band's outer edge on the edge of that box as the
  /// reference's bar stood on the edge of its own, the turning beginning at the top and going to the right, and
  /// the middle spelling its number "{0:F0}%". What changed is the grain of the answer - the reference counted
  /// fives, this counts every percent - and the colour, which the reference took from the skin's accent
  /// (<c>NereusColorLight</c>) and which is the skin's white here, because white is what this front end lights
  /// what is focused with.
  /// </para>
  /// <para>
  /// Both shapes are written down as coordinates rather than left to a control turning something about a centre,
  /// and that is the lesson of the first two dials: the first was twenty bars turned about a point the view had
  /// to place by a fraction of its own height, and the twenty marks did not agree on one circle - they landed in
  /// a heap at the top of the box with the number of percent standing under them. A path that names its own
  /// endpoints and its own radius cannot disagree with the screen about where it lies.
  /// </para>
  /// </remarks>
  public static class PlayPercentageDial
  {
    /// <summary>The ring's own size, and the box every one of its numbers is measured in.</summary>
    public const double SIZE = 105;

    /// <summary>
    /// How wide the band is that the ring is drawn in: the reference's bar was ten points of a box of a hundred
    /// and fifty, which at this box's seven tenths is seven, rounded up to eight so that the ring still reads at
    /// a distance over a moving picture.
    /// </summary>
    public const double BAND = 8.0;

    /// <summary>
    /// The radius the band is drawn on, which puts its outer edge on the edge of the box - half of the box less
    /// half of the band. The reference's bar reached the same edge of its box from the inside.
    /// </summary>
    public const double RADIUS = (SIZE - BAND) / 2.0;

    /// <summary>
    /// The whole ring, as the track the sweep turns in: it stands whether anything was watched or not, which is
    /// what lets the ring say how far is left as well as how far somebody got.
    /// </summary>
    /// <remarks>
    /// Two half circles rather than one whole one, because an arc cannot be asked for a full turn - its end would
    /// stand on top of its beginning and every screen has to guess which way round was meant.
    /// </remarks>
    public static string Track()
    {
      double centre = SIZE / 2.0;
      return string.Create(CultureInfo.InvariantCulture,
          $"M {centre:0.##} {centre - RADIUS:0.##} "
          + $"A {RADIUS:0.##} {RADIUS:0.##} 0 1 1 {centre:0.##} {centre + RADIUS:0.##} "
          + $"A {RADIUS:0.##} {RADIUS:0.##} 0 1 1 {centre:0.##} {centre - RADIUS:0.##}");
    }

    /// <summary>
    /// How far the sweep has turned for the figure the library holds, in degrees: a hundred percent is a whole
    /// turn, a nought is none. A figure out of that range is read as its nearest end, because a library that
    /// counted past a hundred means a hundred and a negative is a bug elsewhere rather than a ring drawn
    /// backwards.
    /// </summary>
    public static double Sweep(int percentage)
    {
      return Math.Min(Math.Max(percentage, 0), 100) * 3.6;
    }

    /// <summary>
    /// The watched part of the ring as one path: from the top of the box, turned <paramref name="degrees"/> to
    /// the right, in the coordinates of a box of <see cref="SIZE"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nought degrees answers a move without a line rather than nothing at all. The view binds this to a shape
    /// that has to be able to read what it is given, so the answer has to be a path - and it must not be a line
    /// of nought length either, because the round ends the sweep is drawn with would paint that as a dot at the
    /// top of an untouched ring. A title nobody started leaves the track bare, which is the reference's "> 0".
    /// </para>
    /// <para>
    /// A whole turn answers the track itself, for the same reason the track is two halves: an end standing on its
    /// own beginning is not a shape a screen can be asked about.
    /// </para>
    /// </remarks>
    public static string Arc(double degrees)
    {
      double centre = SIZE / 2.0;
      if (degrees <= 0.0)
        return string.Create(CultureInfo.InvariantCulture, $"M {centre:0.##} {centre:0.##}");
      if (degrees >= 360.0)
        return Track();

      double radians = degrees * Math.PI / 180.0;
      // Past half a circle the long way round is the way meant, and that is all this flag decides.
      int pastHalf = degrees > 180.0 ? 1 : 0;
      return string.Create(CultureInfo.InvariantCulture,
          $"M {centre:0.##} {centre - RADIUS:0.##} "
          + $"A {RADIUS:0.##} {RADIUS:0.##} 0 {pastHalf} 1 "
          + $"{centre + RADIUS * Math.Sin(radians):0.##} {centre - RADIUS * Math.Cos(radians):0.##}");
    }

    /// <summary>
    /// The number the middle of the ring speaks, in the reference's own words: the whole percent and a percent
    /// sign, no decimal places (<c>MediaInfoMovieDetails.inc:339-351</c>).
    /// </summary>
    public static string Words(int percentage)
    {
      return Math.Min(Math.Max(percentage, 0), 100).ToString("F0", CultureInfo.InvariantCulture) + "%";
    }
  }
}
