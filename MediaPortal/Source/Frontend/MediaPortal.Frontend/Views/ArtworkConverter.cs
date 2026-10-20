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
  /// are kept until the budget runs out, oldest first, and an evicted one is dropped rather than disposed: the
  /// picture on screen and the picture in the cache are one and the same object, and a cache that disposes what a
  /// live Image is still painting takes the whole front end down with it. Avalonia's bitmap returns its memory
  /// when the garbage collector finds nothing painting with it any more, which is the honest moment to let go.
  /// </para>
  /// <para>
  /// <b>A tile asks for a smaller picture than the photograph is.</b> A photograph from a camera is twelve
  /// megapixels; the tile it is drawn in on the pictures screen is two hundred and six logical pixels across.
  /// Decoding twelve megapixels to paint two hundred pixels costs the same memory as the raised picture and the
  /// same time, twenty-four times over, and the budget below then throws almost all of it away again - so a
  /// caller that knows the picture is going into a tile says so with the converter parameter
  /// <c>"tile"</c> and gets it decoded at <see cref="TILE_DECODE_WIDTH"/>. The same bytes asked for at two
  /// sizes are remembered as two pictures, because the raised photograph of the very same file has to stay
  /// full-size while its tile is small.
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
    /// The instance a skin template binds with, by name, the way the skin image converters are bound. A skin
    /// file is read at runtime and reaches the application's resources by key; <c>x:Static</c> is the door that
    /// needs no key. The converter remembers pictures per instance, so tiles drawn from a skin file share this
    /// one cache rather than warming a second copy of the same photographs.
    /// </summary>
    public static readonly ArtworkConverter Instance = new ArtworkConverter();

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

    /// <summary>
    /// How wide a picture that is only ever drawn inside a tile is decoded.
    /// </summary>
    /// <remarks>
    /// Four hundred and thirty-two device pixels. A tile of the pictures screen is 216 logical pixels wide and
    /// draws its picture two hundred and six of those across, so this is twice the room the picture has - which
    /// keeps a tile sharp on a television driven at 200% scaling and stops there. The arithmetic that matters is
    /// what it does to the budget above: a decoded tile is about 130 thousand pixels, so all
    /// <see cref="PicturesViewModel.TILES_WITH_A_PICTURE"/> tiles of a folder fit at once, where the same
    /// twenty-four photographs at twelve megapixels each would be two hundred and eighty-eight million and would
    /// push each other out of the cache as fast as they were decoded.
    /// </remarks>
    public const int TILE_DECODE_WIDTH = 432;

    /// <summary>
    /// The converter parameter a template passes to ask for a tile-sized decode.
    /// </summary>
    public const string TILE_PARAMETER = "tile";

    /// <summary>
    /// One remembered picture: the bytes it came from, the width it was decoded for, and the picture.
    /// </summary>
    private sealed class Remembered
    {
      internal readonly byte[] Bytes;
      internal readonly int Width;
      internal readonly Bitmap Picture;

      internal Remembered(byte[] bytes, int width, Bitmap picture)
      {
        Bytes = bytes;
        Width = width;
        Picture = picture;
      }

      internal long Pixels
      {
        get { return (long) Picture.PixelSize.Width * Picture.PixelSize.Height; }
      }
    }

    /// <summary>Remembered pictures, least recently shown first.</summary>
    private readonly List<Remembered> _held = new List<Remembered>();

    private long _pixels;

    /// <summary>
    /// How many pixels may be held at once. The production budget is <see cref="MAX_HELD_PIXELS"/>; the setter
    /// exists because a headless test cannot decode a nine-megapixel picture to push the real budget over its
    /// limit - its toolkit answers every picture with a one-pixel stub - so eviction is provoked there by
    /// asking for a one-pixel budget instead.
    /// </summary>
    internal long HeldBudget
    {
      get { return _budget; }
      set { _budget = value; }
    }

    private long _budget = MAX_HELD_PIXELS;

    /// <summary>How many pictures this converter holds decoded, which is what the tests look at.</summary>
    public int Held
    {
      get { return _held.Count; }
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      if (value is not byte[] bytes || bytes.Length == 0)
        return null;

      int width = DecodeWidthFor(parameter);
      Bitmap? already = LookForHeld(bytes, width);
      if (already != null)
        return already;

      Bitmap? picture = Decode(bytes, width);
      if (picture == null)
        return null;

      Hold(bytes, width, picture);
      return picture;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      throw new NotSupportedException("A picture is read from the server, never sent to it.");
    }

    /// <summary>
    /// The width to decode for, or zero for the whole picture.
    /// </summary>
    /// <remarks>
    /// A template that names no size gets the whole picture, which is what the raised photograph, a poster on a
    /// details screen and every other existing caller want. A number is accepted as well, so a template whose
    /// tiles are a different size can ask for its own; anything unrecognised is the whole picture again, because
    /// guessing a size is how a picture ends up blurred for no reason.
    /// </remarks>
    public static int DecodeWidthFor(object? parameter)
    {
      if (parameter is string text)
        return string.Equals(text, TILE_PARAMETER, StringComparison.OrdinalIgnoreCase) ? TILE_DECODE_WIDTH : 0;

      return parameter is int requested && requested > 0 ? requested : 0;
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
    private static Bitmap? Decode(byte[] bytes, int width)
    {
      try
      {
        using (MemoryStream picture = new MemoryStream(bytes, false))
        {
          // Scaling is only ever downwards: a picture narrower than the width asked for comes back as it is,
          // which is what a small thumbnail in a big tile wants anyway.
          return width == 0
              ? new Bitmap(picture)
              : Bitmap.DecodeToWidth(picture, width, BitmapInterpolationMode.MediumQuality);
        }
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// The picture these very bytes were decoded to this width before, moved to the recent end, or null when
    /// they have not been decoded at this width yet.
    /// </summary>
    /// <remarks>
    /// Matched by the array itself rather than by its contents, because the bytes come out of the picture cache
    /// held by the view model: the same title hands out the same array every time its row is laid out, so
    /// reference equality is both enough and the cheapest question to ask.
    /// </remarks>
    private Bitmap? LookForHeld(byte[] bytes, int width)
    {
      for (int i = 0; i < _held.Count; i++)
      {
        if (!ReferenceEquals(_held[i].Bytes, bytes) || _held[i].Width != width)
          continue;

        Bitmap picture = _held[i].Picture;
        _held.RemoveAt(i);
        _held.Add(new Remembered(bytes, width, picture));
        return picture;
      }
      return null;
    }

    /// <summary>
    /// Remembers one decoded picture, making room for it by letting the oldest go.
    /// </summary>
    /// <remarks>
    /// The evicted picture is dropped, never disposed. The pictures this cache hands out are shared: an
    /// <see cref="Avalonia.Controls.Image"/> on screen holds the very bitmap the cache gave it and keeps
    /// painting it for as long as its binding stands, and a cache that disposes what another owner is
    /// still rendering kills the compositor mid-frame (ObjectDisposedException on Ref&lt;IBitmapImpl&gt;
    /// inside Image.Render - the crash this rule exists to prevent). Avalonia's Bitmap carries a
    /// finalizer, so a dropped picture whose last visual has moved on gives its memory back to the
    /// garbage collector by itself; the budget's job is to bound the references this cache holds, which
    /// is exactly what dropping does, not to rush a picture out of the world from underneath its
    /// viewers.
    /// </remarks>
    private void Hold(byte[] bytes, int width, Bitmap picture)
    {
      Remembered entry = new Remembered(bytes, width, picture);
      _held.Add(entry);
      _pixels += entry.Pixels;

      while (_pixels > _budget && _held.Count > 1)
      {
        Remembered oldest = _held[0];
        _held.RemoveAt(0);
        _pixels -= oldest.Pixels;
      }
    }
  }
}
