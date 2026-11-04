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
using System.Linq;
using SkiaSharp;
using NUnit.Framework;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend;

/// <summary>
/// The drawn row is one control that computes where every cover stands and lays a flat poster onto it.  Both
/// halves are arithmetic that can be asked without a screen, and both have gone wrong in ways a viewer had to
/// point out: a poster laid onto its corners with the poster's size multiplied into the mapping instead of taken
/// back from it lands a hundred poster-widths off the television, so that every cover in the row collapses into
/// a rod, and a mapping built out of the one hand only leaves the other hand to be guessed at - which is how the
/// neighbours of the stood cover came to be of different sizes at equal distance.
/// </summary>
[TestFixture]
public class TestCoverCarousel
{
    private const double RowWidth = 1920.0;
    private const double RowHeight = 700.0;
    private const double MiddleX = RowWidth / 2.0;

    [Test]
    public void TheDrawnRowLaysItsPosterOnTheCornersTheLensShowed()
    {
        for (double apart = -4.0; apart <= 4.0; apart += 0.5)
        {
            CoverCarousel.Frame frame = CoverCarousel.FrameAt(apart, RowWidth, RowHeight, "cover");
            SKPoint[] corners = CoverCarousel.CornersOf(frame);
            SKMatrix onto = CoverCarousel.PerspectiveToQuad(
                corners, (float) frame.Width, (float) frame.Height);

            SKPoint[] poster =
            {
                new SKPoint(0.0f, 0.0f),
                new SKPoint((float) frame.Width, 0.0f),
                new SKPoint((float) frame.Width, (float) frame.Height),
                new SKPoint(0.0f, (float) frame.Height)
            };

            for (int corner = 0; corner < 4; ++corner)
            {
                SKPoint landed = onto.MapPoint(poster[corner]);
                Assert.That(landed.X, Is.EqualTo(corners[corner].X).Within(0.6f),
                    "the poster's corner " + corner + " has to land on the corner the lens showed, and does not "
                    + "for a cover " + apart + " places from the stood one");
                Assert.That(landed.Y, Is.EqualTo(corners[corner].Y).Within(0.6f),
                    "the poster's corner " + corner + " has to land on the corner the lens showed, and does not "
                    + "for a cover " + apart + " places from the stood one");
            }
        }
    }

    [Test]
    public void TheDrawnRowIsMirroredAboutTheCoverStoodOn()
    {
        foreach (double apart in new[] { 1.0, 2.0, 3.7 })
        {
            SKPoint[] left = CoverCarousel.CornersOf(
                CoverCarousel.FrameAt(-apart, RowWidth, RowHeight, "cover"));
            SKPoint[] right = CoverCarousel.CornersOf(
                CoverCarousel.FrameAt(apart, RowWidth, RowHeight, "cover"));

            for (int corner = 0; corner < 4; ++corner)
            {
                int mirror = corner % 2 == 0 ? corner + 1 : corner - 1;
                Assert.That(left[corner].X - MiddleX, Is.EqualTo(-(right[mirror].X - MiddleX)).Within(0.02f),
                    "a cover " + apart + " places to the left stands as far from the middle as its brother to the "
                    + "right, in corner " + corner);
                Assert.That(left[corner].Y, Is.EqualTo(right[mirror].Y).Within(0.02f),
                    "a cover " + apart + " places to the left stands at the same height as its brother to the "
                    + "right, in corner " + corner);
            }

            Assert.That(Math.Abs(left[2].Y - left[0].Y), Is.EqualTo(Math.Abs(right[3].Y - right[1].Y)).Within(0.02f),
                "both hands of the row are as tall on one side of the stood cover as on the other");
        }
    }

    [Test]
    public void TheDrawnRowTurnsItsCoversAwayFromTheCoverStoodOn()
    {
      CoverCarousel.Frame right = CoverCarousel.FrameAt(2.0, RowWidth, RowHeight, "cover");
      CoverCarousel.Frame left = CoverCarousel.FrameAt(-2.0, RowWidth, RowHeight, "cover");

      Assert.That(right.Lean, Is.GreaterThan(0.0),
          "a cover to the right of the stood one swings its outer edge towards the viewer, so that the row opens "
          + "away from the middle instead of leaning into it");
      Assert.That(left.Lean, Is.LessThan(0.0),
          "and a cover to the left swings its outer edge towards the viewer the other way");

      SKPoint[] right_corners = CoverCarousel.CornersOf(right);
      double inner_edge = Math.Abs(right_corners[0].X - MiddleX);
      double outer_edge = Math.Abs(right_corners[1].X - MiddleX);
      Assert.That(outer_edge, Is.GreaterThan(inner_edge),
          "the edge of a right-hand cover that stands further from the middle is the one nearer the viewer, so it "
          + "looks taller there");
    }

    [Test]
    public void TheDrawnRowKeepsTheStoodCoverUprightAndInTheMiddle()
    {
        CoverCarousel.Frame stood = CoverCarousel.FrameAt(0.0, RowWidth, RowHeight, "cover");
        SKPoint[] corners = CoverCarousel.CornersOf(stood);

        Assert.That(stood.Lean, Is.EqualTo(0.0), "the cover being stood on does not lean");
        Assert.That(stood.Light, Is.EqualTo(1.0).Within(0.001), "and it keeps all the light");

        double leftmost = corners.Min(corner => (double) corner.X);
        double rightmost = corners.Max(corner => (double) corner.X);
        Assert.That(leftmost, Is.EqualTo(MiddleX - stood.Width / 2.0).Within(0.02),
            "the stood cover is centred on the picture rather than pushed to one hand");
        Assert.That(rightmost, Is.EqualTo(MiddleX + stood.Width / 2.0).Within(0.02));
    }
}
