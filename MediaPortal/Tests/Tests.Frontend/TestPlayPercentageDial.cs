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
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The round progress figure the detail panel draws: how far its sweep turns for a given figure, where that
  /// sweep stands on the screen, and what the middle of the ring says.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference wrote this as twenty conditions by hand rather than as a formula
  /// (<c>Skin/Nereus/screens/MediaInfoMovieDetails.inc:239-338</c>), which leaves the boundary questions a formula
  /// answers silently: whether a title at nought paints anything, and whether a hundred closes the ring. This dial
  /// sweeps in one piece where the reference stepped in fives, so what is measured here against the reference is
  /// what did not change - the figure's spelling, the box the ring is measured in, the circle every part of it
  /// stands on, and the place and direction of the turning - together with the two figures a screen cannot invent
  /// for itself, the ends of the counted range.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestPlayPercentageDial
  {
    /// <summary>
    /// The places a path's pen is set down to, read out of it the way a screen reads it: command by command, with
    /// the five numbers an arc carries in front of its own place - its two radii, its turning, and the two answers
    /// of which of the ways round is meant - left out.
    /// </summary>
    private static List<double[]> Places(string path)
    {
      List<double[]> places = new List<double[]>();
      foreach (string part in path.Split(new[] { 'M', 'A', 'L' }, StringSplitOptions.RemoveEmptyEntries))
      {
        string[] pieces = part.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        double[] numbers = Array.ConvertAll(pieces, piece => double.Parse(piece, CultureInfo.InvariantCulture));
        int skip = numbers.Length == 7 ? 5 : 0;
        for (int at = skip; at + 1 < numbers.Length; at += 2)
          places.Add(new[] { numbers[at], numbers[at + 1] });
      }

      return places;
    }

    private static double FromCentre(double x, double y)
    {
      double centre = PlayPercentageDial.SIZE / 2.0;
      return Math.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre));
    }

    [Test]
    public void EveryPercentTurnsItsShareOfTheCircle()
    {
      // One percent of the figure is one percent of the turn. The reference arrived at the same rate by other
      // means - a twentieth of the circle per five percent is 3.6 degrees per percent too - and only stopped
      // there, without the steps it drew the marks with.
      Assert.That(PlayPercentageDial.Sweep(0), Is.EqualTo(0.0));
      Assert.That(PlayPercentageDial.Sweep(50), Is.EqualTo(180.0));
      Assert.That(PlayPercentageDial.Sweep(100), Is.EqualTo(360.0));
      Assert.That(PlayPercentageDial.Sweep(49), Is.EqualTo(176.4).Within(0.001),
          "the figure a viewer was seen at, without the reference's rounding to fives");
    }

    [Test]
    public void FiguresOutsideTheCountedRangeAreReadAsTheirNearestEnd()
    {
      // A library that wrote past a hundred means a hundred, and a negative is a bug elsewhere rather than a ring
      // drawn backwards.
      Assert.That(PlayPercentageDial.Sweep(140), Is.EqualTo(360.0));
      Assert.That(PlayPercentageDial.Sweep(-7), Is.EqualTo(0.0));
      Assert.That(PlayPercentageDial.Words(140), Is.EqualTo("100%"));
    }

    [Test]
    public void UntouchedTitlePaintsNothingAndFinishedTitleClosesTheRing()
    {
      // The reference's first condition was "> 0" and not ">= 0", so a title nobody started leaves the track bare
      // rather than painting a first mark for the nought the library wrote down. The answer is still a path the
      // view can read - a place and no line - because an empty string would be a parse the view has to survive,
      // and a line of nought length would be a dot that the sweep's round ends paint.
      string bare = PlayPercentageDial.Arc(PlayPercentageDial.Sweep(0));
      Assert.That(bare, Does.StartWith("M"));
      Assert.That(bare.Contains("A"), Is.False, "nothing is to be drawn over the track: " + bare);
      Assert.That(Places(bare), Has.Count.EqualTo(1));

      // A whole turn would end on top of where it began, which is no shape a screen can be asked about, so the
      // finished title is answered with the ring drawn whole - which is also what it looks like.
      Assert.That(PlayPercentageDial.Arc(PlayPercentageDial.Sweep(100)), Is.EqualTo(PlayPercentageDial.Track()));
    }

    [Test]
    public void EveryPlaceOfTheRingStandsOnThatOneCircle()
    {
      // The first dial turned its twenty marks about a point the view had to place itself, and they did not agree:
      // they landed in a heap at the top of the box with the number standing under them. Writing the radius down
      // settles it by construction, and this measures that construction - the track and every sweep, at every
      // figure, stand at one distance from the centre and inside the box they are drawn in.
      List<double[]> track = Places(PlayPercentageDial.Track());
      Assert.That(track, Has.Count.EqualTo(3), "one place to begin and two half turns: " + PlayPercentageDial.Track());

      // Nought is the one answer that names the centre rather than the ring: it paints nothing, and where its
      // untouched pen was set down is nobody's business. Every figure above nought is on the circle.
      foreach (int percent in new[] { 1, 7, 49, 50, 51, 75, 99, 100 })
      {
        foreach (double[] at in Places(PlayPercentageDial.Arc(PlayPercentageDial.Sweep(percent))))
        {
          Assert.That(FromCentre(at[0], at[1]), Is.EqualTo(PlayPercentageDial.RADIUS).Within(0.05),
              $"percent {percent} stands at {PlayPercentageDial.Arc(PlayPercentageDial.Sweep(percent))}");
        }
      }

      foreach (double[] at in track)
      {
        Assert.That(FromCentre(at[0], at[1]), Is.EqualTo(PlayPercentageDial.RADIUS).Within(0.05));
        Assert.That(at[0], Is.InRange(0, PlayPercentageDial.SIZE));
        Assert.That(at[1], Is.InRange(0, PlayPercentageDial.SIZE));
      }
    }

    [Test]
    public void SweepBeginsAtTheTopOfTheBoxAndTurnsToTheRight()
    {
      // Where the ring begins and which way it goes is what a viewer reads first, and the reference settled both
      // by laying its first sector at the top of its box and turning the rest from it (CircularIndicator.xaml:8-45
      // rotates by 0, 18, 36 ... degrees). So: the top, then the right hand side, then the bottom.
      double middle = PlayPercentageDial.SIZE / 2.0;
      double radius = PlayPercentageDial.RADIUS;

      double[] begins = Places(PlayPercentageDial.Track())[0];
      Assert.That(begins[0], Is.EqualTo(middle).Within(0.05));
      Assert.That(begins[1], Is.EqualTo(middle - radius).Within(0.05));

      foreach (int percent in new[] { 1, 49, 100 })
      {
        double[] sweepBegins = Places(PlayPercentageDial.Arc(PlayPercentageDial.Sweep(percent)))[0];
        Assert.That(sweepBegins[0], Is.EqualTo(middle).Within(0.05), $"percent {percent} begins elsewhere");
        Assert.That(sweepBegins[1], Is.EqualTo(middle - radius).Within(0.05), $"percent {percent} begins elsewhere");
      }

      double[] quarter = Places(PlayPercentageDial.Arc(PlayPercentageDial.Sweep(25)))[1];
      Assert.That(quarter[0], Is.EqualTo(middle + radius).Within(0.05), "a quarter stands to the right, not the left");
      Assert.That(quarter[1], Is.EqualTo(middle).Within(0.05));

      double[] half = Places(PlayPercentageDial.Arc(PlayPercentageDial.Sweep(50)))[1];
      Assert.That(half[0], Is.EqualTo(middle).Within(0.05));
      Assert.That(half[1], Is.EqualTo(middle + radius).Within(0.05), "halfway stands at the foot of the box");

      // Past half the box the long way round is meant, or a figure over fifty would come back the way it came.
      double[] threeQuarters = Places(PlayPercentageDial.Arc(PlayPercentageDial.Sweep(75)))[1];
      Assert.That(threeQuarters[0], Is.EqualTo(middle - radius).Within(0.05));
      Assert.That(threeQuarters[1], Is.EqualTo(middle).Within(0.05));
    }

    [Test]
    public void RingIsWiderThanALineAndNarrowerThanItsOwnHole()
    {
      // A band, not a hairline: the reference's bar was ten points of its box, and the hole the number stands in
      // has to stay bigger than the band that rings it, or the figure in the middle has nowhere to be read.
      Assert.That(PlayPercentageDial.BAND, Is.GreaterThanOrEqualTo(6).And.LessThanOrEqualTo(10));
      Assert.That(PlayPercentageDial.RADIUS - PlayPercentageDial.BAND / 2.0,
          Is.GreaterThan(PlayPercentageDial.BAND), "the ring's inside is open for the number");
      Assert.That(PlayPercentageDial.RADIUS + PlayPercentageDial.BAND / 2.0,
          Is.LessThanOrEqualTo(PlayPercentageDial.SIZE / 2.0), "and it does not grow over its own box");
    }

    [Test]
    public void MiddleOfTheRingSpeaksWholePercentWithASign()
    {
      // MediaInfoMovieDetails.inc:339-351 - "{0:F0}%", so the figure never arrives with decimal places and always
      // with its sign, whatever language the machine speaks.
      Assert.That(PlayPercentageDial.Words(0), Is.EqualTo("0%"));
      Assert.That(PlayPercentageDial.Words(42), Is.EqualTo("42%"));
      Assert.That(PlayPercentageDial.Words(100), Is.EqualTo("100%"));
    }
  }
}
