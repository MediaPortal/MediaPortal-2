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
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// Softens a bitmap's pixels, which is what turns a small picture of a screen into a blurred one.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Taking a screen small and stretching it back is only half of a blur. Done alone it is a mosaic: the
  /// screen's sharp edges land on the small picture as one-pixel steps, and stretching those steps back gives
  /// squares the eye reads as broken glass, however careful the stretching is about interpolation. Softening
  /// the small picture first is what removes the steps - every pixel then agrees with its neighbours, so there
  /// is nothing left for the stretch to turn into blocks.
  /// </para>
  /// <para>
  /// The softening is three passes over the rows and three over the columns, which is the standard cheap road to
  /// a Gaussian: box-blurred once, the edges of things stay squared; box-blurred three times, the passes stack
  /// up into the round falloff a lens makes. Done on the small picture, where it costs a few hundred thousand
  /// additions instead of a few hundred million, which is why the shell can afford to throw a whole home out of
  /// focus once per question rather than once per frame.
  /// </para>
  /// <para>
  /// The picture's own edges are clamped outward rather than fed in as black, so a bright cover at the border of
  /// the screen does not come back with a dark band beside it.
  /// </para>
  /// </remarks>
  public static class SoftFocus
  {
    /// <summary>How often the picture is gone through in each direction. Three is where a box becomes a Gaussian.</summary>
    private const int PASSES = 3;

    /// <summary>
    /// Softens <paramref name="source"/> by <paramref name="radius"/> of its own pixels and answers with a new
    /// bitmap of the same size at the same dots per inch. The source is left as it was, so the caller stays free
    /// to hand it back.
    /// </summary>
    public static Bitmap Of(Bitmap source, int radius)
    {
      if (source == null)
        throw new ArgumentNullException(nameof(source));

      int width = source.PixelSize.Width;
      int height = source.PixelSize.Height;
      byte[] pixels = Soften(Read(source, width, height), width, height, radius);
      return Write(pixels, width, height, source.Dpi);
    }

    /// <summary>
    /// Softens a field of blue, green, red, alpha pixels - <paramref name="width"/> times
    /// <paramref name="height"/> of them, in rows without padding - by <paramref name="radius"/> of its own
    /// pixels, and answers with the field, which is softened in place. A radius of nothing hands the field back
    /// untouched.
    /// </summary>
    /// <remarks>
    /// The pixels go in and out as plain bytes, and not through a bitmap, because that is the part worth
    /// holding to: whether an edge of the home comes back as a fall or as a square is decided here, and a
    /// picture the platform is only pretending to keep in memory - which the headless test platform does, it
    /// hands out buffers that are not the ones that went in - says nothing about either.
    /// </remarks>
    public static byte[] Soften(byte[] pixels, int width, int height, int radius)
    {
      if (pixels == null)
        throw new ArgumentNullException(nameof(pixels));
      if (pixels.Length < width * height * 4)
        throw new ArgumentException("the field is one byte per corner per pixel", nameof(pixels));
      if (radius <= 0)
        return pixels;

      byte[] other = new byte[pixels.Length];
      for (int pass = 0; pass < PASSES; pass++)
      {
        Along(pixels, other, width, height, radius, rows: false);
        Along(other, pixels, width, height, radius, rows: true);
      }

      return pixels;
    }

    /// <summary>Takes the pixels out of a bitmap, whatever stride the platform kept them in.</summary>
    private static byte[] Read(Bitmap source, int width, int height)
    {
      int stride = width * 4;
      byte[] pixels = new byte[stride * height];
      GCHandle pinned = GCHandle.Alloc(pixels, GCHandleType.Pinned);
      try
      {
        source.CopyPixels(new PixelRect(0, 0, width, height), pinned.AddrOfPinnedObject(), pixels.Length, stride);
      }
      finally
      {
        pinned.Free();
      }

      return pixels;
    }

    /// <summary>Puts pixels into a bitmap of the same size, honouring the stride the platform asks for.</summary>
    private static WriteableBitmap Write(byte[] pixels, int width, int height, Vector dpi)
    {
      WriteableBitmap result = new WriteableBitmap(new PixelSize(width, height), dpi);
      using ILockedFramebuffer target = result.Lock();
      if (target.RowBytes == width * 4)
      {
        Marshal.Copy(pixels, 0, target.Address, pixels.Length);
        return result;
      }

      for (int row = 0; row < height; row++)
        Marshal.Copy(pixels, row * width * 4, target.Address + row * target.RowBytes, width * 4);

      return result;
    }

    /// <summary>
    /// Averages every pixel together with the ones around it along one direction, clamping at the edge of the
    /// picture. The values are premultiplied, as the platform keeps them, and averaging premultiplied colours is
    /// what blurring light - rather than blurring colour over black - is.
    /// </summary>
    private static void Along(byte[] from, byte[] to, int width, int height, int radius, bool rows)
    {
      int span = radius * 2 + 1;
      int along = rows ? width : height;
      int lines = rows ? height : width;

      for (int line = 0; line < lines; line++)
      {
        for (int at = 0; at < along; at++)
        {
          int blue = 0;
          int green = 0;
          int red = 0;
          int alpha = 0;

          for (int step = -radius; step <= radius; step++)
          {
            int sample = at + step;
            if (sample < 0)
              sample = 0;
            else if (sample >= along)
              sample = along - 1;

            // Going along a row, the walking index is the column and the line index is the row; going down a
            // column it is the other way round. Both offsets below are the same pixel, once read and once
            // written, only taken from the two directions.
            int here = ((rows ? line : sample) * width + (rows ? sample : line)) * 4;
            blue += from[here];
            green += from[here + 1];
            red += from[here + 2];
            alpha += from[here + 3];
          }

          int into = ((rows ? line : at) * width + (rows ? at : line)) * 4;
          int half = span / 2;
          to[into] = (byte) ((blue + half) / span);
          to[into + 1] = (byte) ((green + half) / span);
          to[into + 2] = (byte) ((red + half) / span);
          to[into + 3] = (byte) ((alpha + half) / span);
        }
      }
    }
  }
}