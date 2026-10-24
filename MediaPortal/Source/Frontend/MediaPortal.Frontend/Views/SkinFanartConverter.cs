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
using SkiaSharp;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The picture that stands behind a screen, thrown out of focus by however far the skin's setting asks for.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Bound with two hands: the picture's bytes, which the view model holds, and the blur the worn skin's setting
  /// asks for. With the dial at nought the answer is the very picture the artwork converter hands out, untouched
  /// and uncopied; with the dial turned the answer is a copy of it out of focus.
  /// </para>
  /// <para>
  /// <b>The blur is baked into the picture rather than asked of the window.</b> What this front end draws goes
  /// through a compositor, and a compositor is free to be unimpressed by a request to blur one of its visuals:
  /// the one this front end runs takes a blur of forty pixels on a full-screen picture and draws the picture as
  /// sharp as it arrived, without a word. A picture that reaches it already out of focus cannot be ignored, so
  /// the blur happens here, once per picture and per turn of the dial.
  /// </para>
  /// <para>
  /// <b>The blurred copy is drawn small on purpose.</b> Nobody reads a poster off a background that was just
  /// thrown out of focus, so the copy is blurred at <see cref="BLUR_SCALE"/>' share of its size and stretched
  /// back over the screen by the picture's own scaling - which costs a ninth of the pixels to blur and about a
  /// ninth of the memory to hold, and looks the same because a blur has no detail left to lose. The edges are
  /// clamped, not faded, so the blurred picture still reaches the corners of the screen instead of leaving a
  /// soft frame where it stopped.
  /// </para>
  /// <para>
  /// The copy is remembered for the picture and the turn it was made for. A binding is asked again whenever
  /// anything about the row it stands in changes - a selection, a layout pass, the dimming of the layer above -
  /// and a blur that answered every one of those questions by blurring again would be felt on the television.
  /// One copy is enough: the background is one picture at a time.
  /// </para>
  /// </remarks>
  public sealed class SkinFanartConverter : IMultiValueConverter
  {
    /// <summary>The instance a template binds with; see <see cref="ArtworkConverter.Instance"/>.</summary>
    public static readonly SkinFanartConverter Instance = new SkinFanartConverter();

    /// <summary>How much of each side the picture is blurred at.</summary>
    /// <remarks>
    /// A third, which turns a full-HD fanart into a six-hundred-wide picture to blur and stretches it back over
    /// the screen afterwards. The number is a trade of work for detail the viewer cannot see: at a third a blur
    /// of forty pixels has nothing left that a television could show.
    /// </remarks>
    internal const int BLUR_SCALE = 3;

    private SkinFanartConverter()
    {
    }

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
      if (values == null || values.Count == 0 || values[0] is not byte[] bytes || bytes.Length == 0)
        return null;

      double radius = values.Count > 1 && values[1] is double number && !double.IsNaN(number)
          ? Math.Max(0, number)
          : 0;

      if (radius <= 0)
        return Plain(bytes, culture);

      return Blurred(bytes, radius)
          ?? Plain(bytes, culture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      throw new NotSupportedException("A picture is read from the server, never sent back to it.");
    }

    // The one remembered copy: the bytes it came from, the turn it was blurred at, and the picture.
    private byte[]? _bytes;
    private double _radius;
    private Bitmap? _picture;

    private byte[]? _plainBytes;
    private Bitmap? _plainPicture;

    /// <summary>
    /// The backdrop these bytes are as the library drew it - whole, by naming no width - held for the next time
    /// the very same backdrop is bound.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A screen wears one backdrop at a time, but it is bound again every time a wall is handed over: without a
    /// copy kept here, the scan behind the wall was pulled out of the file anew on each of those bindings, which
    /// is a good half of the time a new wall was felt as taking. The artwork cache cannot keep such a picture
    /// either - it is larger than that cache's whole budget - so the repeat was certain, not merely likely. One
    /// copy is remembered because one backdrop is worn; the picture itself belongs to whoever binds it, so it is
    /// never disposed here.
    /// </para>
    /// <para>
    /// Asked for at a smaller width the backdrop was measured to be *slower* than read whole, because a JPEG
    /// decoder hands out halves and quarters for free and resamples anything else by hand: one hundred and six
    /// milliseconds at two and a half thousand pixels instead of eighty at the full 3812. It is also the one
    /// picture that is genuinely drawn at the size of the television.
    /// </para>
    /// </remarks>
    private Bitmap? Plain(byte[] bytes, CultureInfo culture)
    {
      if (ReferenceEquals(_plainBytes, bytes) && _plainPicture != null)
        return _plainPicture;

      if (ArtworkConverter.Instance.Convert(bytes, typeof(object), null, culture) is not Bitmap picture)
        return null;

      _plainBytes = bytes;
      _plainPicture = picture;
      return picture;
    }

    /// <summary>
    /// The blurred copy of these bytes at this radius, made once and held until a different picture or a
    /// different turn asks for a different one.
    /// </summary>
    private Bitmap? Blurred(byte[] bytes, double radius)
    {
      if (ReferenceEquals(_bytes, bytes) && _radius == radius && _picture != null)
        return _picture;

      byte[]? blurred = BlurToPng(bytes, radius, out _, out _);
      if (blurred == null)
        return null;

      Bitmap? picture = PictureOf(blurred);
      if (picture == null)
        return null;

      _bytes = bytes;
      _radius = radius;
      _picture = picture;
      return picture;
    }

    /// <summary>
    /// The blurred picture, encoded as it goes to the screen - and null when the bytes hold no picture to blur.
    /// </summary>
    /// <remarks>
    /// Kept apart from the picture it turns into so that a test can look at what the blur did to an edge
    /// without needing a window: the headless toolkit answers every picture with a one-pixel stub, while Skia
    /// blurs the bytes it is given whether or not there is a display anywhere.
    /// </remarks>
    internal static byte[]? BlurToPng(byte[] bytes, double radius, out int width, out int height)
    {
      width = 0;
      height = 0;

      try
      {
        using SKCodec? codec = SKCodec.Create(new MemoryStream(bytes, false));
        if (codec == null)
          return null;

        SKImageInfo source = codec.Info;
        if (source.Width <= 0 || source.Height <= 0)
          return null;

        // Skia will only give out a picture at its own size or at a half, a quarter or an eighth of it. The sheet
        // the blur is done on is a third of the size the picture is drawn at, so the quarter is the nearest step
        // the codec can hand out at or above that sheet, and drawing it up to the working size is the same three
        // lines of arithmetic as drawing the whole scan down to it - with a quarter of the pixels pulled out of
        // the file. What is lost is detail a thirty-five pixel blur was going to erase anyway; what is won is the
        // seventy-odd milliseconds a 3812 pixel wide scan costs on the thread the standing menu is being built on,
        // which is what a new wall was felt as.
        int raise = 1;
        while (raise < BLUR_SCALE)
          raise *= 2;

        SKBitmap? scan = null;
        if (source.Width / raise > 0 && source.Height / raise > 0)
        {
          SKImageInfo shrunk = new SKImageInfo(
              source.Width / raise, source.Height / raise, source.ColorType, source.AlphaType);
          var shrunkBitmap = new SKBitmap(shrunk);
          if (codec.GetPixels(shrunk, shrunkBitmap.GetPixels()) != SKCodecResult.Success)
            shrunkBitmap.Dispose();
          else
            scan = shrunkBitmap;
        }

        if (scan == null)
        {
          // A codec that will not hand out a smaller copy of itself: the whole scan, as before.
          var whole = new SKBitmap(source);
          if (codec.GetPixels(source, whole.GetPixels()) != SKCodecResult.Success)
          {
            whole.Dispose();
            return null;
          }

          scan = whole;
        }

        using SKBitmap raised = scan;

        var small = new SKImageInfo(
            Math.Max(1, source.Width / BLUR_SCALE),
            Math.Max(1, source.Height / BLUR_SCALE),
            SKColorType.Rgba8888,
            SKAlphaType.Premul);

        using SKSurface? smallSurface = SKSurface.Create(small);
        if (smallSurface == null)
          return null;

        using (SKImage full = SKImage.FromBitmap(raised))
        {
          smallSurface.Canvas.DrawImage(full, new SKRect(0, 0, small.Width, small.Height));
          smallSurface.Flush();
        }

        var dest = new SKImageInfo(small.Width, small.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface? surface = SKSurface.Create(dest);
        if (surface == null)
          return null;

        // The dial's number is the radius a visual blur would have been given; a gaussian wants half of that as
        // its spread, and this picture is a third of the size it is going to be drawn at.
        double sigma = Math.Max(0.5, radius / 2 / BLUR_SCALE);

        using SKImage from = smallSurface.Snapshot();
        using SKPaint paint = new SKPaint
        {
          ImageFilter = SKImageFilter.CreateBlur((float) sigma, (float) sigma)
        };

        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.DrawImage(from, 0, 0, paint);
        surface.Flush();

        using SKImage blurred = surface.Snapshot();
        using SKData? encoded = blurred.Encode(SKEncodedImageFormat.Png, 100);
        if (encoded == null || encoded.Size <= 0)
          return null;

        width = dest.Width;
        height = dest.Height;
        return encoded.ToArray();
      }
      catch (Exception)
      {
        // Bytes that hold no picture are no background, which is what the placeholder behind the picture is for
        // - the same silence the artwork converter keeps about a corrupt file.
        return null;
      }
    }

    /// <summary>A picture out of the encoded blur, or null when the screen will not have it.</summary>
    private static Bitmap? PictureOf(byte[] encoded)
    {
      try
      {
        using MemoryStream stream = new MemoryStream(encoded, false);
        return new Bitmap(stream);
      }
      catch (Exception)
      {
        return null;
      }
    }
  }
}
