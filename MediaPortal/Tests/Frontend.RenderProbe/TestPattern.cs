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

using MediaPortal.Frontend.Player;

namespace FrontendProbe
{
  /// <summary>
  /// A picture with nothing behind it: eight bars and a border, written into a frame sink by hand.
  /// </summary>
  /// <remarks>
  /// The playback screen's frame used to be a black rectangle with a word under it, which proved that the screen
  /// draws but said nothing about whether a film could. Filling the sink here - rather than capturing from a real
  /// decoder - is what lets the same picture arrive at the screen on Windows, in a Linux container, and on a
  /// machine with no video library installed at all, byte for byte the same; a captured frame of whatever a
  /// decoder happened to reach would be a different picture every run and evidence about nothing.
  /// </remarks>
  internal static class TestPattern
  {
    /// <summary>The bars, as blue-green-red-alpha, in the order a screen reads them left to right.</summary>
    /// <remarks>
    /// The first bar is grey and not white on purpose: the border round the picture is white, and a check that
    /// finds the picture by its white borders cannot tell them apart from the widest bar in it. Everything else
    /// here is asymmetric under a red-blue swap on purpose too - a path that trades the two channels has to be
    /// caught by looking, not by counting.
    /// </remarks>
    private static readonly byte[][] Bars =
    {
      new byte[] { 0xC8, 0xC8, 0xC8, 0xFF },   // grey
      new byte[] { 0x00, 0xFF, 0xFF, 0xFF },   // yellow
      new byte[] { 0xFF, 0xFF, 0x00, 0xFF },   // cyan
      new byte[] { 0x00, 0xFF, 0x00, 0xFF },   // green
      new byte[] { 0xFF, 0x00, 0xFF, 0xFF },   // magenta
      new byte[] { 0x00, 0x00, 0xFF, 0xFF },   // red
      new byte[] { 0xFF, 0x00, 0x00, 0xFF },   // blue
      new byte[] { 0x00, 0x00, 0x00, 0xFF }    // black
    };

    /// <summary>
    /// Write one picture of this pattern into <paramref name="sink"/>.
    /// </summary>
    /// <returns>False when the sink refused it, which means there is no picture to render.</returns>
    public static bool Write(VideoFrameSink sink, int width, int height)
    {
      int stride = width * 4;
      byte[] pixels = new byte[stride * height];

      int barWidth = width / Bars.Length;
      for (int row = 0; row < height; row++)
      {
        for (int column = 0; column < Bars.Length; column++)
        {
          int from = column * barWidth;
          int to = column == Bars.Length - 1 ? width : (column + 1) * barWidth;
          byte[] colour = Bars[column];
          for (int x = from; x < to; x++)
          {
            int at = row * stride + x * 4;
            pixels[at] = colour[0];
            pixels[at + 1] = colour[1];
            pixels[at + 2] = colour[2];
            pixels[at + 3] = colour[3];
          }
        }
      }

      // A border, so that the edge of the picture is visible against the black of the letterbox: without it, a
      // frame where the video was drawn at the wrong size and a frame where it was never drawn at all are hard
      // to tell apart by eye, which is the one job these pictures are there to do.
      for (int x = 0; x < width; x++)
      {
        for (int edge = 0; edge < 4; edge++)
          WriteWhite(pixels, stride, edge * width + x);
        for (int edge = height - 4; edge < height; edge++)
          WriteWhite(pixels, stride, edge * width + x);
      }
      for (int y = 0; y < height; y++)
      {
        for (int edge = 0; edge < 4; edge++)
          WriteWhite(pixels, stride, y * width + edge);
        for (int edge = width - 4; edge < width; edge++)
          WriteWhite(pixels, stride, y * width + edge);
      }

      return sink.Push(pixels, width, height, stride);
    }

    private static void WriteWhite(byte[] pixels, int stride, int pixel)
    {
      int at = pixel * 4;
      pixels[at] = 0xFF;
      pixels[at + 1] = 0xFF;
      pixels[at + 2] = 0xFF;
      pixels[at + 3] = 0xFF;
    }
  }
}
