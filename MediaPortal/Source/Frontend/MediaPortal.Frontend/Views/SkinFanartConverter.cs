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

      // Measuring switch, the neighbour of MP_NOART: MP_NOFANART=1 keeps the picture behind the wall away while
      // the covers stay painted.  A step costs the same whether the covers are handed over or not (measured), so
      // the question that is left is the backdrop - read whole, blurred and encoded again for every title the
      // caret stands on, which is the one piece of the screen that is rebuilt rather than moved.
      if (Environment.GetEnvironmentVariable("MP_NOFANART") == "1")
        return null;

      double radius = values.Count > 1 && values[1] is double number && !double.IsNaN(number)
          ? Math.Max(0, number)
          : 0;

      if (radius <= 0)
        return Plain(bytes);

      return Blurred(bytes, radius)
          ?? Plain(bytes);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      throw new NotSupportedException("A picture is read from the server, never sent back to it.");
    }

    // THE BACKDROPS KEPT READY, AT THE SIZES THEY ARE WORN AT.
    //
    // A screen wears one backdrop at a time, but it wears a different one at every step of a walk over the shelf,
    // and it asks for the same picture at TWO sizes: the plain one 1920 across that stands behind the wall, and
    // the third-size copy the blur is worked on.  Both used to be remembered in the shelf's poster cache, where a
    // plain backdrop is two megapixels standing among pictures that are a quarter of that: three of them filled
    // the whole budget of remembered pictures, and the covers the viewer was looking at were the ones thrown out
    // again - which is how a shelf wore grey plates from the sixtieth cover onwards while the backdrop behind
    // them was sitting pretty.  A backdrop is not a poster, so it is remembered here, where it cannot eat the
    // room the posters live in, and where "the sizes it is worn at" is the whole of what is kept: one plain copy
    // and one blurred copy per backdrop, a handful of backdrops deep.
    private readonly object _memo = new object();

    private readonly Dictionary<(byte[] bytes, double radius), Bitmap> _blurredRemembered =
        new Dictionary<(byte[], double), Bitmap>();

    private readonly List<(byte[] bytes, double radius)> _blurredAskedFor =
        new List<(byte[], double)>();

    /// <summary>
    /// How many backdrops are kept at each of their sizes.  Three: the title the screen stands on, the one it
    /// came from, and the one a viewer walking back a step is about to stand on.  A plain copy is two
    /// megapixels, so a deeper row than this is memory the shelf's covers were asking for.
    /// </summary>
    private const int BACKDROPS_REMEMBERED = 3;

    /// <summary>Puts one backdrop's copy into the row of them, and drops the oldest when the row is full.</summary>
    /// <remarks>
    /// Asked with the memo's lock held.  A copy that leaves the row is not disposed - the screen may still be
    /// painting it, and <see cref="_wornLately"/> keeps the last few out of the collector's reach anyway.
    /// </remarks>
    private static void Remember<TKey>(
        Dictionary<TKey, Bitmap> remembered, List<TKey> askedFor, TKey key, Bitmap picture)
    {
      remembered[key] = picture;
      askedFor.Remove(key);
      askedFor.Add(key);

      while (askedFor.Count > BACKDROPS_REMEMBERED)
      {
        TKey oldest = askedFor[0];
        askedFor.RemoveAt(0);
        remembered.Remove(oldest);
      }
    }

    /// <summary>Says that these bytes were looked at, so that the row forgets a different one first.</summary>
    private static void LookedAt<TKey>(List<TKey> askedFor, TKey key)
    {
      askedFor.Remove(key);
      askedFor.Add(key);
    }

    /// <summary>
    /// The backdrops worn lately, held on purpose so that the garbage collector cannot reach them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A picture the screen has just been turned away from is still being painted for a frame or two, and the
    /// compositor holds it by its memory rather than by a reference a collector can see.  Let such a backdrop
    /// go unreachable and the collector gives its memory back while a frame is still being drawn out of it,
    /// which a process does not answer with a complaint but by dying inside native code - seen here as an
    /// access violation (0xc0000005) in ntdll.dll a few seconds after a wall was opened.
    /// </para>
    /// <para>
    /// A handful is kept rather than everything: the walk over a wall changes the backdrop once a step, and a
    /// step is far more frames than lie between "turned away" and "not drawn any more".  What leaves this row
    /// has been off the screen for seconds by then.
    /// </para>
    /// </remarks>
    private readonly Queue<Bitmap> _wornLately = new Queue<Bitmap>();

    /// <summary>How many backdrops out of the collector's reach are kept.</summary>
    private const int WORN_LATELY = 4;

    /// <summary>
    /// Reads a backdrop - blurring it first when the worn face asks for that - away from the thread that draws,
    /// and remembers it here, so that the binding afterwards costs a lookup.
    /// </summary>
    /// <remarks>
    /// A backdrop is the size of the television, which makes reading one the dearest thing a step between two
    /// rows used to cost: about a tenth of a second, paid by the screen's own thread in front of the frame the
    /// viewer was waiting for, for every row of a thousand-film shelf. The caller who fetched the bytes hands
    /// them here instead, from a thread that has the time.
    /// </remarks>
    public void Warm(byte[]? bytes, double radius)
    {
      if (bytes == null || bytes.Length == 0)
        return;

      // Asked of the same two answers the binding asks of, so that warming and binding can never end up holding
      // a different picture for the same bytes - and so that whatever a picture is dressed in before it reaches
      // the screen (a blur, a size) is dressed the same way from either door.
      if (radius <= 0)
        Plain(bytes);
      else
        Blurred(bytes, radius);
    }

    /// <summary>
    /// Whether binding these bytes at this turn of the dial would cost nothing, because the picture is already
    /// remembered here or in the artwork cache.
    /// </summary>
    /// <remarks>
    /// A screen that is asked for the mood of the row it stands on should wear it in the same breath when the
    /// answer is lying to hand - walking back to a title the shelf has just worn is exactly that case, and the
    /// test of it is that the picture is there before the row is drawn.  Only when the answer still has to be
    /// made is it worth sending the reading somewhere else and telling the screen a moment later.
    /// </remarks>
    public bool StandsReady(byte[]? bytes, double radius)
    {
      if (bytes == null || bytes.Length == 0)
        return false;

      if (radius <= 0)
      {
        //  A plain backdrop is a big poster read at the covers' width, so the row that remembers the covers
        //  answers whether it lies ready.
        return ArtworkConverter.RemembersAt(bytes, BACKDROP_DECODE_WIDTH);
      }

      lock (_memo)
      {
        return _blurredRemembered.ContainsKey((bytes, radius));
      }
    }

    /// <summary>
    /// Keeps a backdrop that has just been put on the screen out of the collector's reach, for as long as a
    /// frame might still be painted out of it.  See <see cref="_wornLately"/>.
    /// </summary>
    /// <remarks>
    /// Asked with the memo's lock held.
    /// </remarks>
    private void WearLately(Bitmap picture)
    {
      _wornLately.Enqueue(picture);
      while (_wornLately.Count > WORN_LATELY)
        _wornLately.Dequeue();
    }

    /// <summary>
    /// The backdrop these bytes are, read at the size it is worn at, and held for the next time the very same
    /// backdrop is bound.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A screen wears one backdrop at a time, but it is bound again every time a wall is handed over: without a
    /// copy kept here, the picture behind the wall was pulled out of the file anew on each of those bindings,
    /// which is a good half of the time a new wall was felt as taking. The artwork cache remembers it, but that
    /// cache tracks a bounded number of pictures and the standing wall's posters compete for those places, so
    /// without this copy the repeat was likely, not merely possible. One copy is remembered because one backdrop
    /// is worn; the picture itself belongs to whoever binds it, so it is never disposed here.
    /// </para>
    /// <para>
    /// It is read at <see cref="BACKDROP_DECODE_WIDTH"/>, which is the big poster's width: a backdrop is not a
    /// third size of its own, it is the same picture read at the size the covers are read at and stretched by the
    /// compositor to fill the room behind them.  Reading it at the file's own size was measured at three thousand
    /// one hundred and twenty-six by one thousand seven hundred and fifty-eight pixels, decoded and remembered
    /// twice over for a picture that is drawn behind a wall of posters and through a veil.
    /// </para>
    /// <para>
    /// It is asked of the picture cache rather than decoded here on purpose: a picture inside that cache's budget
    /// is remembered and never thrown away, which is how the front end keeps a backdrop alive while the screen
    /// might still be drawing it.  Read outside that care, the old backdrop is picked up by the garbage collector
    /// some seconds after the next one arrives, and a background whose memory was given back while it was still
    /// being drawn crashes the process.
    /// </para>
    /// </remarks>
    private Bitmap? Plain(byte[] bytes)
    {
      //  A backdrop at the covers' own width has no reason to a cache of its own any more, so it has none: the
      //  poster cache remembers it by its bytes and its width like every other picture of the front end, and a
      //  backdrop that arrives while the wall of covers is being read finds its entry standing there already.
      //  What this class keeps for itself is only what costs something to make twice - the blurred copy - and the
      //  handful of pictures the screen might still be painting, in <see cref="_wornLately"/>.
      Bitmap? picture = ArtworkConverter.ReadCached(bytes, BACKDROP_DECODE_WIDTH);
      if (picture == null)
        return null;

      lock (_memo)
      {
        WearLately(picture);
      }

      return picture;
    }

    /// <summary>
    /// The width the backdrop is read at - which is the <em>big poster</em> width and nothing else, so that the
    /// whole front end works with two picture sizes: this one and the grid's small one.
    /// </summary>
    /// <remarks>
    /// The reference asks its fanart for <c>1920;1080</c> (backgrounds/default-background.xaml:58-62, turned into
    /// <c>MaxWidth</c>/<c>MaxHeight</c> by FanArtImageSourceConverter.cs:51-67), and reading it at that width was
    /// measured here at two megapixels decoded, two megapixels remembered and a frame of 211 ms the first time the
    /// picture was drawn - while the backdrop it is worn behind is dimmed, in most skins blurred, and seen from
    /// three metres on a television.  Read at six hundred and forty and stretched by the compositor - which scales
    /// a picture it is already holding for the price of one quad, not for the price of a decode - the same
    /// backdrop costs a ninth of the pixels and no frame at all.  A backdrop asked for at this width is also the
    /// very picture the wall's covers are read at, so the two share one cache entry instead of competing for it.
    /// </remarks>
    internal const int BACKDROP_DECODE_WIDTH = ArtworkConverter.WALL_DECODE_WIDTH;

    /// <summary>
    /// The blurred copy of these bytes at this radius, made once and held until a different picture or a
    /// different turn asks for a different one.
    /// </summary>
    private Bitmap? Blurred(byte[] bytes, double radius)
    {
      (byte[], double) worn = (bytes, radius);

      lock (_memo)
      {
        if (_blurredRemembered.TryGetValue(worn, out Bitmap? ready))
        {
          LookedAt(_blurredAskedFor, worn);
          return ready;
        }
      }

      byte[]? blurred = BlurToPng(bytes, radius, out _, out _);
      if (blurred == null)
        return null;

      Bitmap? picture = PictureOf(blurred);
      if (picture == null)
        return null;

      lock (_memo)
      {
        Remember(_blurredRemembered, _blurredAskedFor, worn, picture);
        WearLately(picture);
      }

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
    /// <remarks>
    /// Through the poster cache's reader rather than <c>new Bitmap(stream)</c>, because a bitmap built from a
    /// stream only gets its pixels when the first frame draws it - and the frame that shows a new backdrop has
    /// enough else to do.
    /// </remarks>
    private static Bitmap? PictureOf(byte[] encoded)
    {
      return ArtworkConverter.ReadWhole(encoded);
    }
  }
}
