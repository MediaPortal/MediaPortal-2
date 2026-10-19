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
using NUnit.Framework;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// The Ken Burns port, measured against hand-worked numbers from the reference's own calculation. The
  /// picture used throughout is 4000x1500 and the output 1600x900 - wider than the screen it stands on, so
  /// the reference calls it landscape and its normalisation fits the width: the frame is 4000 picture-pixels
  /// wide and 2250 tall, which is a third taller than the picture. That overflowing height is where the
  /// reference's black borders came from, and these tests guard it on purpose.
  /// </summary>
  [TestFixture]
  public class TestKenBurns
  {
    private static readonly Size IMAGE = new Size(4000, 1500);
    private static readonly Size OUTPUT = new Size(1600, 900);

    [Test]
    public void ThePictureIsLandscapeOnlyAgainstTheScreenItFills()
    {
      Assert.That(KenBurnsAnimator.IsLandscape(IMAGE, OUTPUT), Is.True);
      Assert.That(KenBurnsAnimator.IsLandscape(new Size(1500, 4000), OUTPUT), Is.False);
      // 3:2 is wide for paper and tall for a 16:9 television - the reference compared ratios, not sides.
      Assert.That(KenBurnsAnimator.IsLandscape(new Size(3000, 2000), OUTPUT), Is.False);
    }

    [Test]
    public void TimeFlattensTheReferenceWay()
    {
      Assert.That(KenBurnsAnimator.Flatten(0), Is.EqualTo(0));
      Assert.That(KenBurnsAnimator.Flatten(-4), Is.EqualTo(0), "a clock that ran backwards is simply not behind");
      Assert.That(KenBurnsAnimator.Flatten(1), Is.EqualTo(1 - 1.0 / 6).Within(1e-9));
      Assert.That(KenBurnsAnimator.Flatten(0.5), Is.EqualTo(1 - 1.0 / 2.25).Within(1e-9));
      Assert.That(KenBurnsAnimator.Flatten(1000), Is.LessThan(1),
          "the easing settles toward the end without ever quite reaching a stop");
    }

    [Test]
    public void TheZoomStartsOnTheWholeFrame()
    {
      // Zoom factor nought-extra: the window is the whole letterbox frame, centred. Width fits (4000 of 4000),
      // height overflows by a quarter above and below - the reference's borders, kept.
      Rect at = KenBurnsAnimator.ZoomRectangle(1, 0, IMAGE, OUTPUT);
      Assert.That(at.Width, Is.EqualTo(1.0).Within(1e-9));
      Assert.That(at.X, Is.EqualTo(0).Within(1e-9));
      Assert.That(at.Height, Is.EqualTo(2250.0 / 1500).Within(1e-9));
      Assert.That(at.Y, Is.EqualTo(-0.25).Within(1e-9));
    }

    [Test]
    public void TheZoomEndsOneTenthInAtItsPoint()
    {
      // The reference's full zoom at the height-centred left point (spot 8, what a landscape "top-left"
      // class becomes): the left edge does not move, the height shrinks about the middle.
      Rect at = KenBurnsAnimator.ZoomRectangle(1.1, 8, IMAGE, OUTPUT);
      Assert.That(at.X, Is.EqualTo(0).Within(1e-9));
      Assert.That(at.Width, Is.EqualTo(1 / 1.1).Within(1e-9));
      Assert.That(at.Height, Is.EqualTo(2250 / 1.1 / 1500).Within(1e-9));
      Assert.That(at.Y, Is.EqualTo((1500 - 2250 / 1.1) / 2 / 1500).Within(1e-9));
    }

    [Test]
    public void TheEffectPicksItsSpotByThePicturesOwnLongWay()
    {
      // Class nought is "top left": on a landscape picture that is the middle-left point (8), which keeps
      // the left edge and drifts upward into the border; on a portrait picture the same class is the
      // top-centre point (2). The reference chose along the long way round, and so this.
      Rect landscape = KenBurnsAnimator.EffectAt(1, true, 0, 0, IMAGE, OUTPUT);
      Assert.That(landscape.X, Is.EqualTo(0).Within(1e-9), "spot 8 leaves the left edge alone");

      Size portraitImage = new Size(1500, 4000);
      Rect portrait = KenBurnsAnimator.EffectAt(1, true, 0, 0, portraitImage, OUTPUT);
      Assert.That(portrait.Y, Is.EqualTo(0).Within(1e-9), "spot 2 leaves the top edge alone");
    }

    [Test]
    public void ThePanTravelsItsRouteAtAFixedZoom()
    {
      // Landscape route nought runs from point 1 (top left) to point 4 (middle right) at the reference's
      // standing zoom of one tenth. At the start the window holds its top left corner; at the end it has
      // slid to the right point's corner.
      Rect start = KenBurnsAnimator.PanRectangleAt(0, 0, IMAGE, OUTPUT);
      Rect end = KenBurnsAnimator.PanRectangleAt(1, 0, IMAGE, OUTPUT);

      double frameWidth = 1600 * 2.5 / 1.1;
      double frameHeight = 900 * 2.5 / 1.1;

      Assert.That(start.Width, Is.EqualTo(end.Width), "the pan does not zoom, it slides");
      Assert.That(start.Width, Is.EqualTo(frameWidth / 4000).Within(1e-9));
      Assert.That(start.X, Is.EqualTo(0).Within(1e-9), "point 1 holds the left edge, negative width and all");
      Assert.That(start.Y, Is.EqualTo(0).Within(1e-9), "and the top");
      Assert.That(end.X, Is.EqualTo((4000 - frameWidth) * 1.0 / 4000).Within(1e-9));
      Assert.That(end.Y, Is.EqualTo((1500 - frameHeight) * 0.5 / 1500).Within(1e-9),
          "point 4 sits at the middle of the height");
    }

    [Test]
    public void TheAnimatorSettlesIntoItsZoomAndStaysThere()
    {
      // Whatever the dice picked, the animation must move toward the reference's tenth and never past it.
      for (int seed = 0; seed < 40; seed++)
      {
        KenBurnsAnimator animator = new KenBurnsAnimator(new Random(seed)) { Duration = 4 };
        DateTime begin = new DateTime(2026, 5, 1, 21, 0, 0);
        animator.Initialize(begin);

        Rect first = animator.RectAt(begin, IMAGE, OUTPUT);
        Rect middle = animator.RectAt(begin.AddSeconds(2), IMAGE, OUTPUT);
        Rect muchLater = animator.RectAt(begin.AddSeconds(400), IMAGE, OUTPUT);

        Assert.That(middle.Width, Is.LessThanOrEqualTo(first.Width + 1e-9), "the window shrinks, never swells");
        Assert.That(muchLater.Width, Is.LessThanOrEqualTo(middle.Width + 1e-9));
        if (Math.Abs(middle.Width - first.Width) > 1e-9)
        {
          // A zoom: it deepens toward the reference's tenth and never dives past it.
          Assert.That(muchLater.Width, Is.EqualTo(1 / 1.1).Within(1e-4),
              "long after its four seconds, the zoom has settled at exactly the reference's tenth");
        }
        else
        {
          // A pan: it slides at a standing zoom, so its window never changes size at all.
          Assert.That(muchLater.Width, Is.EqualTo(first.Width).Within(1e-9),
              "the pan keeps the frame the reference gave it, one tenth in, start to end");
        }
      }
    }

    [Test]
    public void ThePanRoutesAreTheFourteensTheReferenceShipped()
    {
      // Spelled out from the reference one more time so a retype of the table cannot pass: the first three
      // landscape and portrait routes, as the skin engine's fields held them.
      Assert.That(LandscapeRoute(0), Is.EqualTo((1, 4)));
      Assert.That(LandscapeRoute(1), Is.EqualTo((1, 5)));
      Assert.That(LandscapeRoute(2), Is.EqualTo((8, 3)));
      Assert.That(PortraitRoute(0), Is.EqualTo((1, 6)));
      Assert.That(PortraitRoute(1), Is.EqualTo((1, 5)));
      Assert.That(PortraitRoute(2), Is.EqualTo((2, 7)));
    }

    private static (int Start, int End) LandscapeRoute(int index)
    {
      var table = (System.Collections.Generic.IReadOnlyList<(int Start, int End)>)
          typeof(KenBurnsAnimator)
              .GetField("LANDSCAPE_PAN_SPOTS", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
              .GetValue(null)!;
      return table[index];
    }

    private static (int Start, int End) PortraitRoute(int index)
    {
      var table = (System.Collections.Generic.IReadOnlyList<(int Start, int End)>)
          typeof(KenBurnsAnimator)
              .GetField("PORTRAIT_PAN_SPOTS", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
              .GetValue(null)!;
      return table[index];
    }
  }
}
