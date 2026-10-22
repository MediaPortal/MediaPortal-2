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
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using NUnit.Framework;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// That a small picture of the home is a blur and not a mosaic is worth its own tests, because the difference
  /// is not in a number like "one fifth of the screen" - it is only in whether a sharp edge of the home comes
  /// back as an edge or as a square.
  /// </summary>
  /// <remarks>
  /// The softening is held to here as bytes and not as a bitmap, on purpose: the headless platform the tests run
  /// on hands out bitmap memory that is not the memory that went into it, so pixels read back out of one under
  /// test mean nothing. The shell's own picture is proven separately, in a real window. Every field below is
  /// grey - the same value in all three colours - so that the order the platform keeps the bytes in cannot
  /// matter to a test about how far a value travels.
  /// </remarks>
  [TestFixture]
  public class TestASoftenedPictureIsNotAMosaic
  {
    /// <summary>An empty field of the given size.</summary>
    private static byte[] Field(int width, int height)
    {
      return new byte[width * height * 4];
    }

    /// <summary>A half-white, half-black field: the sharpest thing a home has, one hard edge down the middle.</summary>
    private static byte[] HalfAndHalf(int width, int height)
    {
      byte[] field = Field(width, height);
      for (int y = 0; y < height; y++)
      {
        for (int x = 0; x < width; x++)
        {
          int at = (y * width + x) * 4;
          byte lit = (byte) (x < width / 2 ? 255 : 0);
          field[at] = lit;
          field[at + 1] = lit;
          field[at + 2] = lit;
          field[at + 3] = 255;
        }
      }

      return field;
    }

    /// <summary>How bright one pixel of a field is.</summary>
    private static int LightAt(byte[] field, int width, int x, int y)
    {
      return field[(y * width + x) * 4];
    }

    /// <summary>How much light a whole field holds, which softening is not allowed to lose or invent.</summary>
    private static long LightIn(byte[] field, int width, int height)
    {
      long total = 0;
      for (int y = 0; y < height; y++)
      {
        for (int x = 0; x < width; x++)
        {
          total += LightAt(field, width, x, y);
        }
      }

      return total;
    }

    /// <summary>
    /// A hard edge comes back as a fall, not as an edge. This is the whole difference between blurring a home and
    /// shrinking it: shrunk, a column of the white half stays white and the next one stays black; softened, the
    /// two halves lean into each other over the width of the softening.
    /// </summary>
    [Test]
    public void AHardEdgeComesBackAsAFall()
    {
      byte[] field = SoftFocus.Soften(HalfAndHalf(64, 9), 64, 9, 2);

      Assert.That(LightAt(field, 64, 2, 4), Is.EqualTo(255), "far from the edge the white half is still white");
      Assert.That(LightAt(field, 64, 61, 4), Is.EqualTo(0), "and the black half still black");

      int atTheEdge = LightAt(field, 64, 32, 4);
      Assert.That(atTheEdge, Is.InRange(70, 185),
          "at the edge the two halves have leaned into each other, which is what an out-of-focus edge is; the "
          + "fall measured " + atTheEdge);

      int besideTheEdge = LightAt(field, 64, 28, 4);
      Assert.That(besideTheEdge, Is.LessThan(250),
          "the fall is felt before the edge too - a shrunken picture leaves that pixel at full white, and that "
          + "is exactly the pixel the eye counts as a block: " + besideTheEdge);

      int sevenAside = LightAt(field, 64, 25, 4);
      Assert.That(sevenAside, Is.EqualTo(255),
          "but the softening has a width and does not reach across the home: " + sevenAside);
    }

    /// <summary>
    /// One bright pixel is spread over its neighbourhood, which a shrunken picture never does: shrinking keeps a
    /// bright pixel bright inside its own block, and that block is the pixelation.
    /// </summary>
    [Test]
    public void OneBrightPixelIsSpreadAround()
    {
      byte[] before = Field(32, 32);
      int centre = (16 * 32 + 16) * 4;
      before[centre] = 255;
      before[centre + 1] = 255;
      before[centre + 2] = 255;
      before[centre + 3] = 255;

      byte[] field = SoftFocus.Soften(before, 32, 32, 2);

      Assert.That(LightAt(field, 32, 16, 16), Is.LessThan(240), "the bright pixel gave some of itself away");
      Assert.That(LightAt(field, 32, 19, 16), Is.GreaterThan(0),
          "and some of it arrived three pixels to the right, which a mosaic would never do");
      Assert.That(LightAt(field, 32, 26, 16), Is.EqualTo(0),
          "but it did not reach the far side - the home stays where it was");
      Assert.That(LightIn(field, 32, 32), Is.InRange(240, 270),
          "and the light is spread, not lost: it sums to " + LightIn(field, 32, 32));
    }

    /// <summary>
    /// A picture that is bright all over stays bright all over. The edges of a photograph have no pixels beyond
    /// them, and if they were taken as black, a cover touching the border of the screen would come back with a
    /// dark band beside it.
    /// </summary>
    [Test]
    public void TheEdgeOfThePictureIsNotTakenAsBlack()
    {
      byte[] field = Field(24, 9);
      for (int i = 0; i < field.Length; i += 4)
      {
        field[i] = 255;
        field[i + 1] = 255;
        field[i + 2] = 255;
        field[i + 3] = 255;
      }

      field = SoftFocus.Soften(field, 24, 9, 3);

      Assert.That(LightAt(field, 24, 0, 4), Is.EqualTo(255), "along the left border");
      Assert.That(LightAt(field, 24, 23, 4), Is.EqualTo(255), "along the right border");
      Assert.That(field[3], Is.EqualTo(255), "and the picture keeps being opaque, so the scrim stays the scrim");
    }

    /// <summary>
    /// Asked to soften by nothing, the field comes back as it was: the shell reads the skin's number and may land
    /// on one that says "no blur at all", and that road has to end where it started.
    /// </summary>
    [Test]
    public void NothingIsSoftenedWhenTheRadiusIsGone()
    {
      byte[] before = HalfAndHalf(16, 5);
      byte[] field = SoftFocus.Soften(before, 16, 5, 0);

      Assert.That(LightAt(field, 16, 3, 2), Is.EqualTo(255));
      Assert.That(LightAt(field, 16, 12, 2), Is.EqualTo(0));
    }

    /// <summary>
    /// A picture keeps its size and its dots per inch through the softening, because the shell stretches the
    /// result over the window by exactly those two and nothing else.
    /// </summary>
    [AvaloniaTest]
    public void ThePictureKeepsItsSizeAndItsDotsPerInch()
    {
      WriteableBitmap snapshot = new WriteableBitmap(new PixelSize(51, 29), new Vector(96, 24));
      using (snapshot)
      using (Bitmap soft = SoftFocus.Of(snapshot, 3))
      {
        Assert.That(soft.PixelSize, Is.EqualTo(new PixelSize(51, 29)));
        Assert.That(soft.Dpi.X, Is.EqualTo(96).Within(0.001));
        Assert.That(soft.Dpi.Y, Is.EqualTo(24).Within(0.001), "the dots per inch the photograph was taken at");
      }
    }
  }
}