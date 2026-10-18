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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// Turns the bytes the server sent for a title's picture into something a screen can show.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The view model holds bytes and this class holds pictures, which is the division the front end uses wherever a
  /// screen has to show a thing a server described: a view model that held a <see cref="Bitmap"/> could not be
  /// tested without a window, and a view that fetched from the server would be a screen making decisions. The
  /// conversion is one way - a picture is never sent back - and null in or unpictureable in both give null out,
  /// which leaves the placeholder the template drew behind the image showing.
  /// </para>
  /// <para>
  /// <b>The decoded pictures are remembered and bounded.</b> A shelf binds sixty rows, a viewer scrolls away and
  /// back, and every one of those realisations would hand the same bytes to Skia again otherwise. Decoded pictures
  /// are kept until the budget runs out, oldest first, and an evicted one is disposed - a bitmap is native memory,
  /// and an application that runs a television for a week at a time cannot afford to wait for a garbage collector
  /// to hand it back.
  /// </para>
  /// <para>
  /// The budget is in pixels rather than titles because the pictures a library holds are not all one size: an
  /// album cover from an online provider and a frame the video thumbnailer grabbed can differ by a factor of four,
  /// and a count of remembered titles would be a guess at memory.
  /// </para>
  /// </remarks>
  public sealed class ArtworkConverter : IValueConverter
  {
    /// <summary>
    /// How many decoded pixels may be held at once.
    /// </summary>
    /// <remarks>
    /// Eight million, which is about 32 MB once a picture is in memory as pixels. That is a hundred and twenty
    /// MP2-sized thumbnails (256 across, which is what the server's database holds) or thirty full-cover scans,
    /// and it is deliberately less than a shelf of two hundred titles: whoever walks back through a library
    /// decodes a few pictures again, and a media centre that fills the memory of a television stick is worse than
    /// one that decodes a poster twice.
    /// </remarks>
    public const long MAX_HELD_PIXELS = 8L * 1024 * 1024;

    /// <summary>Remembered pictures, least recently shown first.</summary>
    private readonly List<KeyValuePair<byte[], Bitmap>> _held =
        new List<KeyValuePair<byte[], Bitmap>>();

    private long _pixels;

    /// <summary>How many pictures this converter holds decoded, which is what the tests look at.</summary>
    public int Held
    {
      get { return _held.Count; }
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      if (value is not byte[] bytes || bytes.Length == 0)
        return null;

      Bitmap? already = LookForHeld(bytes);
      if (already != null)
        return already;

      Bitmap? picture = Decode(bytes);
      if (picture == null)
        return null;

      Hold(bytes, picture);
      return picture;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      throw new NotSupportedException("A picture is read from the server, never sent to it.");
    }

    /// <summary>
    /// The picture these bytes hold, or null when they hold no picture.
    /// </summary>
    /// <remarks>
    /// A picture the decoder cannot read is answered with no picture rather than with a complaint. A library can
    /// hold a corrupt file - a download that was cut short, a JPEG that somebody renamed to PNG - and the viewer
    /// is better off with a shelf whose one broken row shows a placeholder than with a screen that refused to
    /// draw it. The bytes stay out of the cache either way, so a row that is realised twice asks the same
    /// question twice, which is the honest cost of not knowing any better.
    /// </remarks>
    private static Bitmap? Decode(byte[] bytes)
    {
      try
      {
        using (MemoryStream picture = new MemoryStream(bytes, false))
          return new Bitmap(picture);
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// The picture these very bytes were decoded into before, moved to the recent end, or null when they have not
    /// been decoded yet.
    /// </summary>
    /// <remarks>
    /// Matched by the array itself rather than by its contents, because the bytes come out of the picture cache
    /// held by the view model: the same title hands out the same array every time its row is laid out, so
    /// reference equality is both enough and the cheapest question to ask.
    /// </remarks>
    private Bitmap? LookForHeld(byte[] bytes)
    {
      for (int i = 0; i < _held.Count; i++)
      {
        if (!ReferenceEquals(_held[i].Key, bytes))
          continue;

        Bitmap picture = _held[i].Value;
        _held.RemoveAt(i);
        _held.Add(new KeyValuePair<byte[], Bitmap>(bytes, picture));
        return picture;
      }
      return null;
    }

    /// <summary>
    /// Remembers one decoded picture, making room for it by throwing away the oldest.
    /// </summary>
    private void Hold(byte[] bytes, Bitmap picture)
    {
      _held.Add(new KeyValuePair<byte[], Bitmap>(bytes, picture));
      _pixels += (long) picture.PixelSize.Width * picture.PixelSize.Height;

      while (_pixels > MAX_HELD_PIXELS && _held.Count > 1)
      {
        KeyValuePair<byte[], Bitmap> oldest = _held[0];
        _held.RemoveAt(0);
        _pixels -= (long) oldest.Value.PixelSize.Width * oldest.Value.PixelSize.Height;
        oldest.Value.Dispose();
      }
    }
  }
}
