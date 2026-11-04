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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

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
    /// Sixteen million, which is about 64 MB once a picture is in memory as pixels. Sized by what a wall of covers
    /// actually needs: a cover decoded at <see cref="WALL_DECODE_WIDTH"/> is six hundred and fourteen thousand
    /// pixels, so this holds twenty-six of them - a wall and a bit - while a tile decoded at
    /// <see cref="TILE_DECODE_WIDTH"/> is two hundred and eighty thousand, which is fifty-seven of those. It used
    /// to be eight million, measured against whole scans; a whole scan cannot enter this cache anymore at all (see
    /// <see cref="Hold"/>), so the number is spent on tiles from here. A media centre that fills the memory of a
    /// television stick is worse than one that decodes a poster twice, which is what bounds it.
    /// </remarks>
    public const long MAX_HELD_PIXELS = 16L * 1024 * 1024;

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
    /// How wide a cover of the standing wall is decoded.
    /// </summary>
    /// <remarks>
    /// Six hundred and forty device pixels, which is what the wall's own arithmetic asks for: the wall is drawn on
    /// an eight hundred and eighty line band whose first cover takes a two by two place, so that cover stands
    /// four hundred and thirty-eight design pixels across - five hundred and seventy-three on the screen this
    /// port is measured on - and the skin's focus morph steps a tenth nearer. A <see cref="TILE_DECODE_WIDTH"/>
    /// decode would have stretched the big cover's picture by a third to fill it, which on a television is the
    /// blur nobody asked for. Nineteen covers at this width are eleven megapixels, so one wall fits the budget
    /// above and a viewer walking between two walls decodes a couple of covers again at most.
    /// </remarks>
    public const int WALL_DECODE_WIDTH = 640;

    /// <summary>
    /// The converter parameter a template passes to ask for a wall-cover-sized decode.
    /// </summary>
    public const string WALL_PARAMETER = "wall";

    /// <summary>
    /// How wide a poster that is only ever drawn small is decoded: the grid's rows of posters, the album squares,
    /// the track rows.
    /// </summary>
    /// <remarks>
    /// Two hundred and fifty-six device pixels, which is the second of the <em>two</em> sizes this front end keeps
    /// - one picture big and one picture small, and everything between them scaled by the compositor instead of
    /// decoded a third time.  A grid poster is drawn a hundred and sixty-three logical pixels across, two hundred
    /// and seventeen on the screen this port is measured on, so this is the width it is drawn at with the morph's
    /// tenth on top.  What is saved is not the decoding alone: twenty of these at the tile width are two and a
    /// half megapixels of texture the graphics card holds in order to draw them two hundred and seventeen pixels
    /// wide.
    /// </remarks>
    public const int POSTER_DECODE_WIDTH = 256;

    /// <summary>
    /// The converter parameter a template passes to ask for a small-poster decode.
    /// </summary>
    public const string POSTER_PARAMETER = "small";

    /// <summary>
    /// What one picture is, as far as this converter is concerned: the bytes it came from, compared by their
    /// contents and not by which array happens to hold them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Keying by the array's identity used to be the cheap question and is usually the right one, because the
    /// view model hands the same array to every question about one title.  Usually is not always: a second
    /// reader of the same poster - the row and the panel, a reload after the library changed - hands over a
    /// new array of the same file, and the cache, asked by identity, answers "never seen it".  The consequence
    /// is not a wasted lookup but a JPEG decoded again on the thread that draws, which is the hitch the viewer
    /// counts: the ledger of a wall entering the screen read <c>asked=19 decoded=19/697ms</c>, every one of
    /// those decodes on the screen's own thread, while the picture was already decoded twice over.
    /// </para>
    /// <para>
    /// The fingerprint is only the bucket a picture is looked for in - head, middle and tail of the file plus
    /// its length, so that a two megabyte poster costs a hundred byte reads and not two megabyte ones.  Two
    /// different files can share a fingerprint, and when they do <see cref="Equals(PictureKey)"/> still says no,
    /// byte for byte.  Correctness never rests on the sample.
    /// </para>
    /// </remarks>
    private readonly struct PictureKey : IEquatable<PictureKey>
    {
      internal readonly byte[] Bytes;
      private readonly int _fingerprint;

      internal PictureKey(byte[] bytes)
      {
        Bytes = bytes;
        _fingerprint = Fingerprint(bytes);
      }

      public override int GetHashCode()
      {
        return _fingerprint;
      }

      public override bool Equals(object? other)
      {
        return other is PictureKey key && Equals(key);
      }

      public bool Equals(PictureKey other)
      {
        return _fingerprint == other._fingerprint
            && (ReferenceEquals(Bytes, other.Bytes)
                || System.Collections.StructuralComparisons.StructuralEqualityComparer
                    .Equals(Bytes, other.Bytes));
      }

      private static int Fingerprint(byte[] bytes)
      {
        uint hash = 2166136261u;
        hash = Fold(hash, (uint) bytes.Length);

        for (int part = 0; part < 3; ++part)
        {
          int from = part == 0
              ? 0
              : part == 1
                  ? Math.Max(0, bytes.Length / 2 - SAMPLE / 2)
                  : Math.Max(0, bytes.Length - SAMPLE);
          int until = Math.Min(bytes.Length, from + SAMPLE);

          for (int i = from; i < until; ++i)
            hash = Fold(hash, bytes[i]);
        }

        return (int) hash;
      }

      /// <summary>How many bytes of each end and of the middle take part in the bucket.</summary>
      private const int SAMPLE = 32;

      private static uint Fold(uint hash, uint value)
      {
        hash *= 16777619u;
        hash ^= value;
        return hash;
      }
    }

    /// <summary>
    /// One remembered picture: the bytes it came from, the width it was decoded for, and the picture.
    /// </summary>
    private sealed class Remembered
    {
      internal readonly PictureKey Key;
      internal readonly int Width;
      internal readonly Bitmap Picture;

      internal Remembered(PictureKey key, int width, Bitmap picture)
      {
        Key = key;
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
      get { lock (_cache) { return _held.Count; } }
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      if (value is not byte[] bytes || bytes.Length == 0)
        return null;

      return Decoded(bytes, DecodeWidthFor(parameter));
    }

    /// <summary>
    /// Decodes a picture and remembers it, from whomever's thread that suits, so that the screen finds it lying
    /// there when its turn comes.
    /// </summary>
    /// <remarks>
    /// A wall asked to walk over a thousand films pulls a new row of posters into the picture every few
    /// centimetres, and a poster is a JPEG: read from disk or network, decoded, and scaled down to the tile.
    /// Asked through <see cref="Convert"/> that work happens on the screen's own thread, between the frame the
    /// viewer is waiting for and the frame after it, which is what "everything hitches while scrolling" is made
    /// of.  A caller who already has the bytes - which is every caller who fetched them for a row - hands them
    /// here instead, and the binding afterwards costs a lookup.
    /// </remarks>
    public void Warm(byte[]? bytes, object? parameter)
    {
      if (bytes == null || bytes.Length == 0)
        return;

      Decoded(bytes, DecodeWidthFor(parameter));
    }

    /// <summary>The picture these bytes hold, from the cache or from the decoder.</summary>
    /// <remarks>
    /// <para>
    /// The lock is held for a lookup and for a bookkeeping step, never across a decode. It used to be held
    /// across the decode as well, so that one picture was decoded once; what that bought in practice was the
    /// screen's own thread standing behind every JPEG a row of posters had started, which is a hundredth of a
    /// second per keypress added up into the "it hitches and judders while scrolling" a viewer complains about.
    /// </para>
    /// <para>
    /// A picture nobody is decoding is therefore decoded outside the lock, and whoever asks for a picture that
    /// somebody else is already decoding waits for <em>that one</em> through <see cref="_beingMade"/> - which
    /// keeps the one-decode-per-picture the old code was after without making unrelated pictures anybody's
    /// business.
    /// </para>
    /// </remarks>
    private Bitmap? Decoded(byte[] bytes, int width)
    {
      PictureKey key = new PictureKey(bytes);
      TaskCompletionSource<Bitmap?> wanted;
      bool oursToMake;

      lock (_cache)
      {
        Bitmap? already = LookForHeld(bytes, width);
        if (already != null)
        {
          Interlocked.Increment(ref READ_HELD);
          return already;
        }

        Interlocked.Increment(ref READ_ASKED);

        if (!_beingMade.TryGetValue(key, out Dictionary<int, TaskCompletionSource<Bitmap?>>? forTheseWidths))
        {
          forTheseWidths = new Dictionary<int, TaskCompletionSource<Bitmap?>>();
          _beingMade[key] = forTheseWidths;
        }

        oursToMake = !forTheseWidths.TryGetValue(width, out wanted!);
        if (oursToMake)
        {
          wanted = new TaskCompletionSource<Bitmap?>(TaskCreationOptions.RunContinuationsAsynchronously);
          forTheseWidths[width] = wanted;
        }
      }

      if (!oursToMake)
      {
        // Somebody else is reading this very picture at this very width; their answer is the answer.
        //
        // Asking them costs this thread the asking, which is the picture's whole decode when the thread asking is
        // the one that draws - so it is counted, and a shelf that stalls on every step can be asked what it
        // stalled on (SERVICES.HomeTrace reads these numbers on each step of the caret).
        long waited_at = ReadingsClock.ElapsedMilliseconds;
        bool answered = wanted.Task.Wait(LONGEST_WAIT_FOR_OTHERS);
        Interlocked.Add(ref READ_WAIT_MS, ReadingsClock.ElapsedMilliseconds - waited_at);
        Interlocked.Increment(ref READ_WAITS);

        return answered
            ? wanted.Task.IsCompletedSuccessfully ? wanted.Task.Result : null
            : Decode(bytes, width);
      }

      long asked_at = ReadingsClock.ElapsedMilliseconds;
      bool on_the_pictures_thread = Avalonia.Threading.Dispatcher.UIThread.CheckAccess();
      Bitmap? made = Decode(bytes, width);
      long cost = ReadingsClock.ElapsedMilliseconds - asked_at;
      Interlocked.Increment(ref READ_DECODES);
      Interlocked.Add(ref READ_DECODE_MS, cost);

      if (WhoDecodesOnScreen)
      {
        // The station the ledger cannot answer on its own: it counts the decodes but not WHO asked, and a walk
        // that costs twelve reads a step still has to say which of its builders asked for them.  Asked once, by
        // hand, with MP_TRACE_DECODE=1 - a stack walk is far too dear to be anybody's standing expense.
        Services.HomeTrace.Log("decodesource",
            bytes.Length + "B@" + width + " cost=" + cost + "ms onscreen=" + (on_the_pictures_thread ? 1 : 0)
            + " by " + AskedBy());
      }

      if (on_the_pictures_thread)
      {
        // Read on the thread that draws: the shelf cannot paint while it waited, whatever the total says.
        Interlocked.Increment(ref READ_DECODES_ON_SCREEN);
        Interlocked.Add(ref READ_DECODE_MS_ON_SCREEN, cost);
      }

      lock (_cache)
      {
        if (_beingMade.TryGetValue(key, out Dictionary<int, TaskCompletionSource<Bitmap?>>? forTheseWidths))
        {
          forTheseWidths.Remove(width);
          if (forTheseWidths.Count == 0)
            _beingMade.Remove(key);
        }

        if (made != null)
          Hold(bytes, width, made);

        if (!DECODED_BY_WIDTH.TryGetValue(width, out long[] at_this_width))
        {
          at_this_width = new long[2];
          DECODED_BY_WIDTH[width] = at_this_width;
        }
        at_this_width[0]++;
        at_this_width[1] += cost;
      }

      // Told only after the picture is remembered, so that a waiter finding it in the cache and a waiter taking
      // it from the answer both get the same picture rather than one of each.
      wanted.TrySetResult(made);
      return made;
    }

    /// <summary>Guards the remembered pictures, which the screen's thread and the fetchers' threads both reach.</summary>
    private readonly object _cache = new object();

    // WHAT THE PICTURES COST.  A shelf that stutters can be stalling on its transforms, on the screen's painting,
    // or on the pictures themselves - which is a decode of a poster on the thread that draws, or a wait for a
    // poster another thread is already reading.  Those three cannot be told apart from a screen, so these counters
    // are kept: whoever walks the shelf (MediaBrowserView, on each step of the caret) reads them into the trace
    // with TakePictureReadings and they start again from zero for the step it is standing in.
    private static readonly Stopwatch ReadingsClock = Stopwatch.StartNew();
    private static long READ_HELD;
    private static long READ_ASKED;
    private static long READ_DECODES;
    private static long READ_DECODE_MS;
    private static long READ_DECODES_ON_SCREEN;
    private static long READ_DECODE_MS_ON_SCREEN;
    private static long READ_WAITS;
    private static long READ_WAIT_MS;

    /// <summary>
    /// Decodes since last reading, sorted by the width they were read at: a shelf that costs a hundred and forty
    /// milliseconds a step has to say WHICH pictures those are, since the sizes the row and the details panel
    /// read for are different questions with different answers.
    /// </summary>
    private static readonly Dictionary<int, long[]> DECODED_BY_WIDTH = new Dictionary<int, long[]>();

    /// <summary>
    /// What the pictures have cost since this was last asked, and the counts start again from zero.
    /// </summary>
    public static string TakePictureReadings()
    {
      long held = Interlocked.Exchange(ref READ_HELD, 0);
      long asked = Interlocked.Exchange(ref READ_ASKED, 0);
      long decodes = Interlocked.Exchange(ref READ_DECODES, 0);
      long decode_ms = Interlocked.Exchange(ref READ_DECODE_MS, 0);
      long on_screen = Interlocked.Exchange(ref READ_DECODES_ON_SCREEN, 0);
      long on_screen_ms = Interlocked.Exchange(ref READ_DECODE_MS_ON_SCREEN, 0);
      long waits = Interlocked.Exchange(ref READ_WAITS, 0);
      long wait_ms = Interlocked.Exchange(ref READ_WAIT_MS, 0);

      // What is standing in the cache right now, and how wide the widest picture among it is: a shelf whose tiles
      // ask for four hundred and thirty-two pixels but holds pictures of a thousand and two hundred is a shelf
      // whose compositor samples a poster where it should sample a tile, which is a thing the screen pays for on
      // every frame rather than once.
      int standing;
      long pixels;
      int widest;
      string standing_by_width;
      lock (Instance._cache)
      {
        standing = Instance._held.Count;
        pixels = Instance._pixels;
        widest = 0;

        //  What the standing pictures were decoded FOR, width by width.  A cache that sits full tells you nothing
        //  about who filled it, and whether a walk over a shelf is short of covers or short of ROOM is exactly
        //  the question a size has to be answered with.
        var widths = new SortedList<int, long>();
        var counts = new SortedList<int, int>();
        foreach (Remembered kept in Instance._held)
        {
          int width = kept.Picture.PixelSize.Width;
          widest = Math.Max(widest, width);
          widths[width] = widths.TryGetValue(width, out long sofar)
              ? sofar + (long)width * kept.Picture.PixelSize.Height : (long)width * kept.Picture.PixelSize.Height;
          counts[width] = counts.TryGetValue(width, out int been) ? been + 1 : 1;
        }
        var parts = new List<string>(widths.Count);
        foreach (KeyValuePair<int, long> w in widths)
          parts.Add(w.Key + ":" + counts[w.Key] + "x" + w.Value / (1024 * 1024) + "Mpx");
        standing_by_width = string.Join(" ", parts);
      }

      //  And what was DECODED since last time, again width by width: the widths the shelf and the details panel
      //  ask for are known, so a walk's worth of decodes says in one line which of them paid for what.
      var made = new List<string>();
      lock (Instance._cache)
      {
        foreach (KeyValuePair<int, long[]> entry in DECODED_BY_WIDTH)
        {
          if (entry.Value[0] == 0)
            continue;
          made.Add(entry.Key + ":" + entry.Value[0] + "x" + entry.Value[1] + "ms");
          entry.Value[0] = 0;
          entry.Value[1] = 0;
        }
      }

      return string.Format(CultureInfo.InvariantCulture,
          "art held={0} asked={1} decoded={2}/{3}ms onscreen={4}/{5}ms waited={6}/{7}ms "
          + "standing={8}/{9}Mpx widest={10} standing_by_width=[{11}] made_by_width=[{12}]",
          held, asked, decodes, decode_ms, on_screen, on_screen_ms, waits, wait_ms,
          standing, pixels / (1024 * 1024), widest, standing_by_width, string.Join(" ", made));
    }

    /// <summary>
    /// The decodes under way, reached by the picture they are reading and then by the width they are reading
    /// for, so that whoever asks for a picture already on its way can wait for <em>that</em> picture.
    /// </summary>
    /// <remarks>
    /// Reached by <see cref="PictureKey"/> rather than by the array: a row and a panel that both fetched the
    /// same poster are waiting for one decode of it, not for two.
    /// </remarks>
    private readonly Dictionary<PictureKey, Dictionary<int, TaskCompletionSource<Bitmap?>>> _beingMade =
        new Dictionary<PictureKey, Dictionary<int, TaskCompletionSource<Bitmap?>>>();

    /// <summary>
    /// How long a caller waits for a decode somebody else started before reading the picture itself rather than
    /// waiting any longer. A decode is milliseconds of work, so this is only ever reached by a thread that has
    /// stopped running, and a screen is better off decoding twice than waiting.
    /// </summary>
    private static readonly TimeSpan LONGEST_WAIT_FOR_OTHERS = TimeSpan.FromSeconds(5);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      throw new NotSupportedException("A picture is read from the server, never sent to it.");
    }

    /// <summary>
    /// The width to decode for, or zero for the whole picture.
    /// </summary>
    /// <remarks>
    /// A template that names no size gets the BIG of the two sizes this front end keeps, never the file's own size:
    /// a two-thousand-wide scan remembered under a question that did not ask for a width is the single biggest
    /// landlord of the picture budget, and measured here it was the reason the row of remembered pictures was full
    /// again while covers were going wanting - thirty-eight covers of a shelf standing on four megapixels instead
    /// of a fifth of that, and a step's worth of them landing in the large-object heap at once.  A number is
    /// accepted as well, so a template whose tiles are a different size can ask for its own.  The whole file is
    /// read by one door only, <see cref="ReadWhole"/>, for the few callers that really draw a picture as big as
    /// the screen; nothing that stands in a list or on a wall goes through it.
    /// </remarks>
    public static int DecodeWidthFor(object? parameter)
    {
      if (parameter is string text)
      {
        if (string.Equals(text, TILE_PARAMETER, StringComparison.OrdinalIgnoreCase))
          return TILE_DECODE_WIDTH;
        if (string.Equals(text, WALL_PARAMETER, StringComparison.OrdinalIgnoreCase))
          return WALL_DECODE_WIDTH;
        if (string.Equals(text, POSTER_PARAMETER, StringComparison.OrdinalIgnoreCase))
          return POSTER_DECODE_WIDTH;
        return int.TryParse(text, out int named) && named > 0 ? named : WALL_DECODE_WIDTH;
      }

      return parameter is int asked ? (asked > 0 ? asked : WALL_DECODE_WIDTH) : WALL_DECODE_WIDTH;
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
        //  Scaling is only ever downwards: a picture narrower than the width asked for comes back as it is,
        //  which is what a small thumbnail in a big tile wants anyway.
        return ReadAtWidth(bytes, width);
      }
      catch (InvalidOperationException)
      {
        //  Reading a picture at a width is not decoding alone: the platform shrinks what the decoder handed out
        //  through a render target, and a render target belongs to the thread that drew the first frame. Asked
        //  from a thread that fetches, that says so out loud.
        //
        //  The picture is still wanted at the width it was asked for, and it is the width that matters here:
        //  what this method hands back is REMEMBERED under the width that was asked, and a whole 3126x1758 scan
        //  standing under a 432 key takes twenty times the room of the copy the tile is drawn from. Measured on
        //  a shelf of covers: three of those whole scans filled the whole budget of remembered pictures, so
        //  every poster the viewer had walked past was thrown out again before they walked back, and the next
        //  binding paid for the read on the thread that draws - twenty-one reads, eight hundred and fifty-two
        //  milliseconds, one forty millisecond frame after another. That is what "it hitches on every step" was.
        //
        //  So the shrinking is done here, by hand, on the thread that may not use a render target: Skia draws
        //  the scan into a sheet of the asked width (the same sheet the blurred backdrop is made on, and for the
        //  same reason), and the sheet goes back through the door a picture comes in at.
        return ReadWholeAndShrink(bytes, width);
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// The picture these bytes hold, read at the width asked for - the decoder's own door, with nothing kept of
    /// the answer.  Read by whoever wants a picture of a size this cache has no business remembering: a backdrop
    /// is a different size from a tile and is kept by the screen that wears it, not by the shelf's posters.
    /// </summary>
    internal static Bitmap? ReadAtWidth(byte[] bytes, int width)
    {
      try
      {
        return ReadWholeAndShrink(bytes, width);
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// The picture these bytes hold at the width asked for, out of the one row of remembered pictures every
    /// picture of the front end shares - for code that has the bytes in hand rather than a binding to answer.
    /// A backdrop read at the covers' width belongs in that row, not in a row of its own.
    /// </summary>
    internal static Bitmap? ReadCached(byte[] bytes, int width)
    {
      return Instance.Decoded(bytes, width);
    }

    /// <summary>
    /// Whether that row already holds the picture these bytes were read at this width to, so that a caller can
    /// tell an answer that costs nothing from one that has to be made.
    /// </summary>
    internal static bool RemembersAt(byte[] bytes, int width)
    {
      lock (Instance._cache)
      {
        return Instance.LookForHeld(bytes, width) != null;
      }
    }

    /// <summary>
    /// The picture these bytes hold at the size it came in, with its pixels already made. For the blurred
    /// backdrop sheet, which is its own size and must not cost a frame the decoder on the way to the screen.
    /// </summary>
    internal static Bitmap? ReadWhole(byte[] bytes)
    {
      return ReadAtWidth(bytes, 0);
    }

    /// <summary>
    /// Reads a picture whole and hands back the width that was asked for, on a thread the platform will not let
    /// scale while it decodes.
    /// </summary>
    /// <remarks>
    /// Skia shrinks the scan on a sheet of the asked width - the same way the blurred backdrop is made, and for
    /// the same reason: a sheet of pixels needs nobody's render target - and the pixels of that sheet are copied
    /// straight into the bitmap here, so that what is remembered is a picture of the width its key says AND a
    /// picture that is finished before a tile wears it.  A bitmap built from a stream is not decoded until the
    /// first frame that draws it: measured on the backdrop, the three frames after a new one was shown cost 211,
    /// 228 and 267 milliseconds, which is twenty times what a frame may cost and is what "it hitches whenever a
    /// new picture appears" was.
    /// </remarks>
    private static Bitmap? ReadWholeAndShrink(byte[] bytes, int width)
    {
      try
      {
        //  The decoder's own ladder first (see ReadScanAtLadder): a two-thousand-wide scan costs forty-two
        //  milliseconds read whole and eleven read at a quarter, and every picture of this front end - tile,
        //  cover, backdrop - used to pay the forty-two and be shrunk afterwards.
        SKBitmap? rung = width > 0 ? ReadScanAtLadder(bytes, width) : null;

        using SKBitmap? scan = rung ?? SKBitmap.Decode(bytes);
        if (scan == null)
          return AsItCame(bytes);

        if (width <= 0 || scan.Width <= width)
        {
          //  Nothing to shrink - the file is no wider than the tile that asked for it, and scaling upwards was
          //  never what a width question meant.  The pixels are copied all the same, for the reason in the
          //  remark above: a stream holds a file, not a picture.
          return Eager(scan);
        }

        int high = Math.Max(1, (int) Math.Round((double) scan.Height * width / scan.Width));
        //  Bgra8888, not Rgba8888: the two are the same four colours in the opposite byte order, and the sheet is
        //  copied into an Avalonia Bgra8888 bitmap a few lines further down.
        using SKBitmap? shrunk = scan.Resize(
            new SKImageInfo(width, high, SKColorType.Bgra8888, SKAlphaType.Premul),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        //  No re-encoding into PNG on the way: the pixels are already in hand, and copying them keeps the
        //  transparency of a logo exactly as it is.
        return shrunk == null ? AsItCame(bytes) : Eager(shrunk);
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// The scan these bytes hold, read at the decoder's own ladder of sizes for the width asked - or no bitmap at
    /// all, when the file is no wider than the width or the decoder offers nothing smaller.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A decoder that knows its format can read a JPEG at a quarter of its size for about a quarter of the work,
    /// because it skips most of the blocks the picture is made of instead of reading them and throwing them away.
    /// It will only do it at the sizes of its own ladder - an eighth, a quarter, a half - and it rounds DOWN to
    /// the next rung, so the rung it answers with is stepped up until the width asked for is covered: a tile given
    /// a picture narrower than itself has to stretch it, which is the blur a viewer sees before he sees a hitch.
    /// </para>
    /// <para>
    /// Measured on a scan of two thousand pixels: forty-two milliseconds whole, eleven at a quarter, against the
    /// forty-two plus a shrink this front end used to pay for every tile, cover and backdrop it read.
    /// </para>
    /// </remarks>
    internal static SKBitmap? ReadScanAtLadder(byte[] bytes, int width)
    {
      try
      {
        using SKMemoryStream file = new SKMemoryStream(bytes);
        using SKCodec? codec = SKCodec.Create(file);

        if (codec == null || codec.Info.Width <= width)
          return null;

        SKSizeI rung = codec.GetScaledDimensions((float) width / codec.Info.Width);

        for (int steps = 0;
             rung.Width > 0 && rung.Width < width && rung.Width * 2 <= codec.Info.Width && steps < 6;
             ++steps)
        {
          SKSizeI higher = codec.GetScaledDimensions(Math.Min(1.0f, (float) rung.Width * 2.0f / codec.Info.Width));
          if (higher.Width <= rung.Width || higher.Width > codec.Info.Width)
            break;

          rung = higher;
        }

        if (rung.Width <= 0 || rung.Width >= codec.Info.Width)
          return null;

        //  Bgra8888 premultiplied: what every caller of this door copies into an Avalonia bitmap anyway.
        SKBitmap sheet = new SKBitmap(
            new SKImageInfo(rung.Width, rung.Height, SKColorType.Bgra8888, SKAlphaType.Premul));

        if (codec.GetPixels(sheet.Info, sheet.GetPixels()) != SKCodecResult.Success)
        {
          sheet.Dispose();
          return null;
        }

        return sheet;
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// The last door, for bytes Skia would not open: the platform's own reader. Kept because a library holds
    /// more formats than any one decoder, and a picture read late beats no picture at all - the cost being that
    /// its decoding waits for the first frame that draws it.
    /// </summary>
    private static Bitmap? AsItCame(byte[] bytes)
    {
      try
      {
        using MemoryStream sheet = new MemoryStream(bytes, false);
        return new Bitmap(sheet);
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// A bitmap whose pixels already exist, copied out of a sheet Skia decoded.
    /// </summary>
    /// <remarks>
    /// The sheet is first put into the byte order Avalonia's Bgra8888 bitmap holds its colours in - blue, green,
    /// red, alpha.  Skia's Rgba8888 is the same four colours in the <em>opposite</em> order, and copying one of
    /// those over unchanged paints every cover of the wall with its reds and its blues swapped; that swap is the
    /// "die Farben der Bilder stimmen nicht" a shaped shelf showed the day this copy was written.  Once the sheet
    /// is Bgra8888 premultiplied the two really are the same bytes, and apart from a differing row pitch the whole
    /// thing is one block copy.  Nothing is decoded later, no thread is asked whether it may decode, and the size
    /// of the answer is the size that was made here.
    /// </remarks>
    private static WriteableBitmap Eager(SKBitmap scan)
    {
      SKImageInfo ground = new SKImageInfo(scan.Width, scan.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
      SKBitmap sheet = scan;
      SKBitmap? turned = null;
      try
      {
        if (scan.ColorType != ground.ColorType || scan.AlphaType != ground.AlphaType)
        {
          //  A JPEG straight from the decoder usually already stands in this order - then nothing happens here at
          //  all - and a sheet in another one is turned over at its own size, which converts the pixels and
          //  resamples nothing.
          turned = scan.Resize(ground, new SKSamplingOptions())
              ?? throw new InvalidOperationException("the sheet would not turn to BGRA");
          sheet = turned;
        }

        var picture = new WriteableBitmap(new PixelSize(sheet.Width, sheet.Height), new Vector(96, 96),
            PixelFormats.Bgra8888, AlphaFormat.Premul);
        int pitch = sheet.RowBytes;
        byte[] pixels = new byte[pitch * sheet.Height];
        Marshal.Copy(sheet.GetPixels(), pixels, 0, pixels.Length);
        using ILockedFramebuffer surface = picture.Lock();
        long room = (long) surface.RowBytes * surface.Size.Height;
        if (surface.RowBytes == pitch && room >= pixels.Length)
        {
          Marshal.Copy(pixels, 0, surface.Address, pixels.Length);
        }
        else
        {
          for (int line = 0; line < sheet.Height; line++)
          {
            Marshal.Copy(pixels, line * pitch, surface.Address + line * surface.RowBytes, pitch);
          }
        }

        return picture;
      }
      finally
      {
        turned?.Dispose();
      }
    }

    /// <summary>
    /// Whether the picture for these bytes lies ready in the cache, so that a caller can tell an answer that
    /// costs nothing from one that has to be made.
    /// </summary>
    /// <remarks>
    /// Asked by whoever is about to send work away from the drawing thread: when the picture is already there,
    /// sending anybody anywhere only delays the frame the viewer is waiting for.
    /// </remarks>
    public bool Remembers(byte[]? bytes, object? parameter)
    {
     if (bytes == null || bytes.Length == 0)
       return false;

     lock (_cache)
     {
       return LookForHeld(bytes, DecodeWidthFor(parameter)) != null;
     }
    }


    /// <summary>
    /// Whether every decode the drawing thread pays for also names the caller that asked for it.  Off unless
    /// <c>MP_TRACE_DECODE=1</c>: a stack walk costs more than the lookup it is standing in the middle of, so
    /// this is an instrument for one measured run and never a standing expense.
    /// </summary>
    private static readonly bool WhoDecodesOnScreen =
        Environment.GetEnvironmentVariable("MP_TRACE_DECODE") == "1";

    /// <summary>The callers behind a decode, this file and the toolkit left out.</summary>
    private static string AskedBy()
    {
      System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(2, false);
      System.Text.StringBuilder asked = new System.Text.StringBuilder();

      for (int i = 0; i < trace.FrameCount && asked.Length < 220; i++)
      {
        string? name = trace.GetFrame(i)?.GetMethod()?.DeclaringType?.FullName;
        if (name == null
            || name.IndexOf("ArtworkConverter", StringComparison.Ordinal) >= 0
            || name.StartsWith("Avalonia", StringComparison.Ordinal)
            || name.StartsWith("System.", StringComparison.Ordinal))
          continue;

        if (asked.Length > 0)
          asked.Append(" < ");

        asked.Append(name);
      }

      return asked.Length == 0 ? "?" : asked.ToString();
    }

    /// <summary>
    /// The picture these very bytes were decoded to this width before, moved to the recent end, or null when
    /// they have not been decoded at this width yet.
    /// </summary>
    /// <remarks>
    /// Matched by the contents of the array rather than by which array holds them, because a poster that two
    /// readers fetched twice is still one poster, and answering "never seen it" to the second one costs a JPEG
    /// decoded on the thread that draws.
    /// </remarks>
    private Bitmap? LookForHeld(byte[] bytes, int width)
    {
      PictureKey key = new PictureKey(bytes);

      for (int i = 0; i < _held.Count; i++)
      {
        if (_held[i].Width != width || !_held[i].Key.Equals(key))
          continue;

        Bitmap picture = _held[i].Picture;
        _held.RemoveAt(i);
        _held.Add(new Remembered(key, width, picture));
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
      // A picture larger than the whole budget is handed out but not remembered: it would throw every cover of
      // the standing wall out of the cache on its way in and be thrown out itself by the next picture asked for,
      // so remembering it buys nothing and costs the lot. Whatever needs such a picture again keeps its own copy;
      // the home backdrop does, in SkinFanartConverter.
      if ((long)picture.PixelSize.Width * picture.PixelSize.Height > _budget)
        return;

      Remembered entry = new Remembered(new PictureKey(bytes), width, picture);
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
