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

namespace MediaPortal.Frontend.Player
{
  /// <summary>
  /// Reads the spectrum out of the picture libVLC's visualisation filter draws, which is the only shape the
  /// engine will hand the sound over in. Nothing here knows about engines or memory: a picture of bytes goes in,
  /// one level per slice of that picture's width comes out, so the reading can be asked about on its own.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>What the picture means was measured, not assumed</b>, by playing known signals through the same filter
  /// (640×300, RV32) and reading the bytes back: the horizontal axis is frequency with the deep end on the left.
  /// Eighty hertz filled slice nought, one kilohertz slice six, eight kilohertz slice twenty-seven, and
  /// broadband noise filled every slice alike, so what the picture offers is one level per equal slice of its
  /// width, in the engine's own order.
  /// </para>
  /// <para>
  /// The same run said why the axis is not the reference's: the reference sorts its thirty-two bars
  /// logarithmically between twenty hertz and twenty kilohertz (UI/UiComponents/Media/Controls/
  /// AudioSpectrumAnalyzer.cs:411-425), while neither a logarithmic nor an even spread fits those six tone
  /// readings - the worst is off by five slices. Forcing the reference's spread onto this picture would move a
  /// tone's bar to a place the engine never put it, so the picture is read as it is drawn: thirty-two equal
  /// slices of its width, in the engine's own order. What is kept from the reference is the bar count, the
  /// spacing, the decibel-free height and the falling cap, which are the parts a viewer sees.
  /// </para>
  /// <para>
  /// A slice's level is its <em>brightness</em> and not the height of its top edge, which is the difference
  /// between a strip that moves and a strip that does not. Both were read off the same pictures. Over a one
  /// kilohertz tone falling six decibels a second for sixty-six decibels the top edge went from 0.49 to 0.22
  /// of the picture - and by only 0.07 over the forty decibels where music actually lives - while the
  /// brightness went from 0.26 to 0.004, a factor of sixty. Over three seconds of digital silence between loud
  /// passages the top edge still stood at 0.42 of the 0.49 it had held, because the filter paints a slowly
  /// falling peak marker above every bar and the edge reads that marker, while the brightness fell to a
  /// quarter of its loud value. Two signals a hundred and fifty times apart in level therefore move the edge
  /// by a seventh of a bar and the brightness by most of its range.
  /// </para>
  /// <para>
  /// Silence is not nought brightness either. Peak marker, base line and grid together leave 0.066 of
  /// brightness at the deep end, rising to 0.082 at the high end (same run), while material of any kind that
  /// is loud - one tone, one band of noise, broadband noise - reaches 0.26. So the floor silence leaves is
  /// subtracted band by band and what remains is multiplied by the gain that fills a bar at 0.26. Both numbers
  /// are those measurements, not a tuning by eye.
  /// </para>
  /// </remarks>
  public static class SpectrumPicture
  {
    /// <summary>
    /// How bright a pixel has to be, as the sum of its red, green and blue parts, to count as part of a bar. The
    /// filter paints its bars brighter than five hundred of these and leaves everything else - background, grid,
    /// antialiasing - far below a hundred and twenty, so the number sits between the two and cannot make the two
    /// trade places.
    /// </summary>
    public const int LitSum = 120;

    /// <summary>
    /// Brightness the picture keeps when the sound has stopped, measured over three seconds of digital silence:
    /// the peak marker above the bars, the base line and the grid.
    /// </summary>
    public const float SilenceBrightness = 0.066f;

    /// <summary>
    /// How much brighter silence stands at the high end of the picture than at the deep end (same run), which is
    /// why the floor is subtracted band by band rather than once.
    /// </summary>
    public const float SilenceBrightnessRise = 0.016f;

    /// <summary>
    /// Brightness loud material of any kind reaches, measured for a single tone, for one band of noise and for
    /// broadband noise alike, so this is where a bar stands at full height.
    /// </summary>
    public const float LoudBrightness = 0.26f;

    /// <summary>
    /// Fills <paramref name="bands"/> with one level per equal slice of the picture's width, lowest frequency
    /// first, each level how bright that slice is: the brightness left above what silence leaves, times the gain
    /// that puts loud material at full height.
    /// </summary>
    /// <param name="picture">The picture, blue-green-red-padding per pixel, rows one after another.</param>
    /// <param name="width">How many pixels wide the picture is.</param>
    /// <param name="height">How many pixels tall the picture is.</param>
    /// <param name="bands">Where the levels go, one per slice.</param>
    /// <exception cref="ArgumentException">
    /// When the picture is too small for the size it was described by, or when no slice was offered to fill.
    /// </exception>
    public static void BandsFrom(byte[] picture, int width, int height, float[] bands)
    {
      if (picture == null)
        throw new ArgumentNullException(nameof(picture));
      if (width <= 0 || height <= 0)
        throw new ArgumentException($"a picture {width} by {height} is not a picture.", nameof(width));
      if (bands == null || bands.Length == 0)
        throw new ArgumentException("there is no slice to fill.", nameof(bands));
      if (picture.Length < width * height * 4)
        throw new ArgumentException(
            $"the picture holds {picture.Length} bytes, which is not {width} by {height} of them.",
            nameof(picture));

      int slices = bands.Length;
      for (int slice = 0; slice < slices; slice++)
      {
        int first = slice * width / slices;
        int last = (slice + 1) * width / slices;
        if (last <= first)
          last = first + 1;
        if (last > width)
          last = width;

        long brightness = 0;
        for (int x = first; x < last; x++)
        {
          for (int y = 0; y < height; y++)
          {
            int at = (y * width + x) * 4;
            int lit = picture[at] + picture[at + 1] + picture[at + 2];
            if (lit >= LitSum)
              brightness += lit;
          }
        }

        // What lit, against the whole slice painted as bright as it can be.
        float fraction = brightness / ((float) (last - first) * height * 765f);

        // Silence leaves a floor standing, and stands it higher at the high end where the markers of every bar
        // pile up; loud material is what fills a bar. Both numbers are the measurements above.
        float floor = SilenceBrightness
            + (slices > 1 ? SilenceBrightnessRise * slice / (slices - 1) : 0f);
        float gain = 1f / (LoudBrightness - SilenceBrightness);

        bands[slice] = Math.Clamp((fraction - floor) * gain, 0f, 1f);
      }
    }
  }
}
