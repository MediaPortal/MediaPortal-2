#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using SkiaSharp;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// The picture of a title at the width a front end said it would paint it, made once and kept on this server's
  /// own disk.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Why this exists.</b> What a library holds behind a title is the picture somebody downloaded: a thousand
  /// or two thousand pixels across, six hundred and fifty kilobytes on an ordinary shelf.  A front end draws that
  /// picture three hundred pixels wide.  Serving the file means the read crosses the house for a twelfth of its
  /// pixels, the front end stands behind a decoder for forty milliseconds to shrink it, and the second viewer in
  /// the household pays the whole of it again.  Measured on a walk over a shelf of films: thirty-one posters read
  /// for one walk, two and four tenths megapixels the piece, seventy five megapixels of pixels in all.
  /// </para>
  /// <para>
  /// <b>This is the reference's own answer, not an invention.</b> MP2's old client asked
  /// <c>/FanartService?mediatype=…&amp;fanarttype=…&amp;name=…&amp;width=…&amp;height=…</c>
  /// (FanArtImageSource.cs:236) and <c>FanArtImage.ResizeImage</c> answered it out of a folder of resized
  /// pictures under <c>&lt;DATA&gt;\Thumbs\FanArt</c>, so that a size was made once for the whole server and
  /// every client afterwards read the small one.  The port of this front end lost both halves - the width in the
  /// question and the folder of answers - and that is the whole of "the covers arrive late".
  /// </para>
  /// <para>
  /// <b>The ladder, and why there is one.</b> Every size is a file of its own on this server, and a front end that
  /// asked 322, 328 and 341 for the same poster would keep three copies of it for a difference nobody sees.  So the
  /// sizes are the reference's steps - an eighth, a quarter, a half, and so up - with 640 added, which is what this
  /// front end's wall paints its first cover at (573 device pixels on the screen this was measured on, so a 512
  /// step would already be stretched).  The step asked for is the smallest one that covers the width asked for, and
  /// a picture narrower than that is answered with itself: a variant blown up from a small picture is a blurry
  /// poster and a bigger file, which is the one thing a resize is supposed to end.
  /// </para>
  /// <para>
  /// <b>Named by what the bytes are, not by who asked.</b> The reference named a variant after the original
  /// file's name; this names it after the bytes, because the two doors a picture can come in at (the fanart
  /// cache and the folders beside the media) do not both have a file name to offer, and because a poster
  /// replaced under the same name then simply asks for a variant nobody has made yet instead of being served the
  /// old one for a year.
  /// </para>
  /// <para>
  /// <b>Never a reason to fail.</b> No thumbnailer registered, a decoder that would not open these bytes, a disk
  /// that is full, a folder another program holds: all of them end in the same answer as no cache at all, which
  /// is the original bytes.  A server that cannot resize is a server that is not helping, not a library without
  /// pictures.
  /// </para>
  /// </remarks>
  public sealed class FanArtVariants
  {
    /// <summary>
    /// The one server-wide shelf of resized pictures.
    /// </summary>
    public static readonly FanArtVariants Worn = new FanArtVariants();

    /// <summary>
    /// The sizes a variant is ever made at, biggest first - the reference's own steps
    /// (<c>FanArtImage.IMAGE_SIZES</c>) with 640 between 1024 and 512.
    /// </summary>
    /// <remarks>
    /// Descending because <see cref="BestSize"/> walks the row and keeps the last step still big enough, which is
    /// the smallest answer to the question.  640 is the width this front end's wall reads its covers at and the
    /// ceiling of the row it paints by hand; 512 is the picture the import already keeps in the database, so a
    /// shelf whose tiles are drawn two hundred and seventeen pixels across is answered by 256 and needs no step of
    /// its own.
    /// </remarks>
    internal static readonly int[] IMAGE_SIZES = { 4096, 2048, 1024, 640, 512, 256, 128 };

    /// <summary>
    /// How much room the shelf of variants may take before the oldest of them go.
    /// </summary>
    /// <remarks>
    /// One gigabyte, which at the sizes of the ladder is about two thousand and five hundred titles at all three
    /// widths this front end asks at - a whole film library of a thousand two hundred and forty one titles fits
    /// into it with room over.  The reference's folder had no end to it at all, which is fine for a step ladder
    /// and not fine for a server that is also a television's home.  A lid matters all the same, but a lid a real
    /// library does not fit under would throw out what was made a minute ago: the shelf would eat itself while it
    /// is being filled, and a variant that is gone again before anything asks for it a second time warmed nothing.
    /// </remarks>
    private const long LONGEST_ROW_OF_BYTES = 1024L * 1024 * 1024;

    /// <summary>
    /// The extensions a variant is ever written under, and the ones that are looked for before making one.
    /// </summary>
    /// <remarks>
    /// A picture with an alpha channel is answered with PNG and everything else with JPEG - the reference's own
    /// rule, and the reason a transparent logo survives the trip - so a variant is one or the other and the name
    /// carries which.
    /// </remarks>
    private static readonly string[] VARIANT_EXTENSIONS = { ".jpg", ".png" };

    /// <summary>
    /// The picture to hand a caller who said how wide they draw it: the resized copy where this server has one
    /// or can make one, the picture itself whenever resizing would be a loss or a guess.
    /// </summary>
    /// <param name="picture">The bytes as the library holds them.</param>
    /// <param name="width">
    /// How wide the caller paints the picture, or nought for "as it came".  Nought is the state every caller that
    /// says nothing is in, so an old front end against a new server behaves exactly as it did before.  The number
    /// is read as a width and not as the side of a square: a poster of two by three parts asked at six hundred and
    /// forty comes back six hundred and forty pixels across and nine hundred and sixty down, because the caller
    /// paints six hundred and forty across whichever way the picture lies.
    /// </param>
    public byte[] Sized(byte[]? picture, int width)
    {
      if (picture == null || picture.Length == 0 || width <= 0)
        return picture!;

      int step = BestSize(width);

      string? folder = Folder();
      if (folder == null)
        return picture;

      string stem = Path.Combine(folder,
          step.ToString(CultureInfo.InvariantCulture) + "x" + step.ToString(CultureInfo.InvariantCulture)
          + "_" + Fingerprint(picture));

      try
      {
        foreach (string extension in VARIANT_EXTENSIONS)
        {
          string made = stem + extension;
          if (!File.Exists(made))
            continue;

          return System.IO.File.ReadAllBytes(made);
        }

        if (!TryResize(picture, step, out byte[]? resized, out bool asPng)
            || resized == null || resized.Length == 0 || resized.Length >= picture.Length)
        {
          //  Nothing made - or bigger out than in, which happens when the library's picture was already about the
          //  step asked for.  The library's own bytes are the better answer either way, and no file is kept.
          return picture;
        }

        string answer = stem + (asPng ? ".png" : ".jpg");
        //  Written beside itself and moved over, the way this tree writes every file a reader might be reading:
        //  a warming pass asks for the same poster from several requests at once, and a variant that was seen
        //  half written is served to the whole house for as long as it stands there.
        string temporary = answer + ".new";
        System.IO.File.WriteAllBytes(temporary, resized);
        System.IO.File.Move(temporary, answer, true);
        MakeRoom(folder, resized.LongLength);

        return resized;
      }
      catch (Exception ex)
      {
        ILogger? logger = ServiceRegistration.Get<ILogger>(false);
        logger?.Error("MediaController: the shelf of resized pictures could not answer, handing out the "
            + "original: {0}", ex);
        return picture;
      }
    }

    /// <summary>
    /// Making the picture at the width asked for, and saying which container the answer needs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>SkiaSharp, and why not the reference's thumbnail service.</b> The reference asked
    /// <c>IThumbnailGenerator</c>, which walks the providers an installation happens to have. In this tree every
    /// one of those providers is a Windows assembly (<c>GDIThumbnailProvider</c>, <c>WICThumbnailProvider</c>,
    /// <c>ShellThumbnailProvider</c>) and the transformer <c>IImageTransformer</c> is registered by
    /// <c>MediaPortal.Common.Windows</c> - so the headless server, which is what this server is, has neither.  The
    /// tests in this repo already wrote that down the hard way: &quot;on the headless server, where no transformer
    /// can be registered: a library of 459 titles lost 887 pictures to the missing service&quot;.  A resize built
    /// on that seam would be a resize that never happens here.  SkiaSharp is the codec this front end has been
    /// painting with all along (same version, 3.119.4, already in the package cache), it runs on every platform
    /// the server runs on, and this assembly stays a plain <c>net10.0</c> one.
    /// </para>
    /// <para>
    /// <b>Read down the decoder's own ladder, then shrink what is left.</b> A JPEG decoder reads a picture at an
    /// eighth, a quarter or a half of its size for about that much of the work, because it skips the blocks it was
    /// not asked for instead of reading them and throwing them away - but it answers at those rungs ONLY, and
    /// asking it for a third of a picture is answered with <c>InvalidScale</c> (which is what the first version of
    /// this method did, and did silently).  So the rung still big enough is asked for first - the same climb this
    /// front end's <c>ArtworkConverter.ReadScanAtLadder</c> does, ArtworkConverter.cs:721 - the pixels are read
    /// there, and only the last stretch down to the exact step is arithmetic.  Measured in this front end on the
    /// largest picture the library holds, two thousand by three thousand, the read alone is three times the speed
    /// and a third of the memory of reading the whole thing.
    /// </para>
    /// </remarks>
    private static bool TryResize(byte[] picture, int step, out byte[]? resized, out bool asPng)
    {
      resized = null;
      asPng = false;

      using SKData bytes = SKData.CreateCopy(picture);
      using SKCodec? codec = SKCodec.Create(bytes);
      if (codec == null)
        return false;

      SKImageInfo source = codec.Info;
      if (source.Width <= 0 || source.Height <= 0)
        return false;

      if (source.Width <= step)
      {
        //  No wider than the step asked for, so the answer is the picture itself.  A copy blown up from a small
        //  picture is a blurry poster and a bigger file at once, which is the one thing a resize exists to end.
        //  The codec cannot be asked whether it would enlarge - here the bytes can be asked what they are, for
        //  the price of reading a header.
        return false;
      }

      //  The step is read as a WIDTH, not as the side of a square the picture has to fit into, which is what the
      //  reference's pair of <c>width=&amp;height=</c> questions meant.  A front end says how wide it paints, and a
      //  wall's cover is five hundred and seventy three device pixels wide whether the poster standing in it is
      //  landscape or portrait.  Fitting a portrait poster into a 640 square answers with four hundred and twenty
      //  seven pixels of width - a twelfth of the scan and still a blur where it is painted, which is the one thing
      //  this shelf exists to end.  Measured on a library poster of two thousand by three thousand: four hundred
      //  and twenty seven across from the square reading, six hundred and forty from this one.
      //
      //  The long side is capped at the biggest step all the same, so that a panorama two by six parts cannot make
      //  the server file a ten megapixel variant for a caller that asked for a thumbnail.
      double scale = (double) step / source.Width;
      if (source.Height * scale > IMAGE_SIZES[0])
        scale = (double) IMAGE_SIZES[0] / source.Height;

      int width = Math.Max(1, (int) Math.Round(source.Width * scale));
      int height = Math.Max(1, (int) Math.Round(source.Height * scale));

      //  The rung of the decoder's own ladder that still covers the width wanted.  The codec rounds DOWN to its
      //  next rung, so the rung is stepped up until it reaches, which is the climb this front end already does at
      //  ArtworkConverter.cs:729.  A codec offering no rung at all - a PNG cannot be read part way - is read whole
      //  and shrunk afterwards, which is again what the front end does with those.
      SKSizeI rung = codec.GetScaledDimensions((float) width / source.Width);
      for (int steps = 0;
           rung.Width > 0 && rung.Width < width && rung.Width * 2 <= source.Width && steps < 6;
           ++steps)
      {
        SKSizeI higher = codec.GetScaledDimensions(Math.Min(1f, (float) rung.Width * 2f / source.Width));
        if (higher.Width <= rung.Width || higher.Width > source.Width)
          break;

        rung = higher;
      }

      SKImageInfo atRung = rung.Width > 0 && rung.Width < source.Width
          ? new SKImageInfo(rung.Width, rung.Height, SKColorType.Bgra8888, SKAlphaType.Premul)
          : new SKImageInfo(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

      using SKBitmap sheet = new SKBitmap(atRung);
      if (codec.GetPixels(sheet.Info, sheet.GetPixels()) != SKCodecResult.Success)
      {
        //  A codec that would not give these pixels - a file truncated by a half-finished download, say.  The
        //  library's own bytes stay the answer; a half-read poster filed under a fingerprint would be served to
        //  every viewer in the house for as long as it stood there.
        return false;
      }

      //  The last stretch down to the exact step is arithmetic on at most twice the pixels wanted, and it is why
      //  the step stays a step: the rung it starts from is thrown out here rather than filed.
      SKBitmap? shrunk = sheet.Width == width && sheet.Height == height
          ? null
          : sheet.Resize(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul),
              new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

      try
      {
        //  Alpha is kept by keeping the container that can carry it: a ClearArt that came as PNG goes out as PNG,
        //  and a title's lettering that came as WebP with transparency does too.  Everything else becomes a JPEG at
        //  ninety, which at these widths is a difference of a few kilobytes and no difference anybody can see.
        asPng = source.AlphaType != SKAlphaType.Opaque || codec.EncodedFormat == SKEncodedImageFormat.Png;
        using SKImage image = SKImage.FromBitmap(shrunk ?? sheet);
        using SKData? made = image.Encode(asPng ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg,
            asPng ? 100 : 90);
        if (made == null || made.Size == 0)
          return false;

        resized = made.ToArray();
        return true;
      }
      finally
      {
        shrunk?.Dispose();
      }
    }

    /// <summary>
    /// The smallest step of the ladder that still covers the width asked for, or - when the caller wants more
    /// than the ladder holds - the biggest step it has.
    /// </summary>
    internal static int BestSize(int width)
    {
      // Walked from the smallest step upward, which is the order the row is in when read back to front: the first
      // step still big enough is the answer, and taking a later one would be paying for pixels nobody paints.
      for (int i = IMAGE_SIZES.Length - 1; i >= 0; --i)
      {
        if (IMAGE_SIZES[i] >= width)
          return IMAGE_SIZES[i];
      }

      // Asked for more than the ladder holds - the picture is handed out at its biggest step, which is the
      // original size for everything a library keeps.
      return IMAGE_SIZES[0];
    }

    /// <summary>
    /// The folder the shelf lives in, or <c>null</c> when this host has no path manager to ask - which is the
    /// state a unit test runs in.
    /// </summary>
    /// <remarks>
    /// The reference's own folder, so that one place on a server holds the resized pictures.  The files inside it
    /// are not the reference's names, though: it named a variant after the original file's name and a CRC of it,
    /// this names it after the bytes - so a shelf an old client filled is simply not read by this server, which is
    /// right, because what the reference made inside a square this server makes across.  Made on first asking
    /// because a server that never serves a picture has no business creating directories.
    /// </remarks>
    private static string? Folder()
    {
      try
      {
        IPathManager? paths = ServiceRegistration.Get<IPathManager>(false);
        if (paths == null)
          return null;

        string folder = paths.GetPath(@"<DATA>\Thumbs\FanArt");
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);

        return folder;
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// The sixteen hex characters that stand for these bytes.
    /// </summary>
    /// <remarks>
    /// A fingerprint, not a signature: what matters is that the same bytes land on the same file and different
    /// bytes on different ones.  MD5 over the length of a picture is a fraction of a millisecond, which is less
    /// than the file read that came before it.
    /// </remarks>
    private static string Fingerprint(byte[] bytes)
    {
      byte[] hash = MD5.HashData(bytes);
      return Convert.ToHexString(hash.AsSpan(0, 8));
    }

    /// <summary>
    /// What the shelf is believed to hold, and the lock around believing it.  Minus one means nobody has looked
    /// at the disk since this server came up.
    /// </summary>
    private static long _worn = -1;

    /// <summary>
    /// The lock around the shelf's bookkeeping, so that a warming pass over a thousand titles does not have five
    /// requests sweeping the same folder at once.
    /// </summary>
    private static readonly object ROOM_LOCK = new object();

    /// <summary>
    /// Throwing out the least recently written variants until the shelf is inside its room again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Least recently WRITTEN, not least recently read, on purpose: reading a variant touches nothing, so a
    /// picture that is asked for every evening for a year keeps the date it was made with and can be thrown out
    /// while it is being looked at.  That costs one resize when it happens, which is what the room bought.
    /// </para>
    /// <para>
    /// <b>An estimate of the room, not a walk of the folder.</b> The first version of this walked the whole shelf
    /// after every single variant it wrote, and a warming pass over a library is five thousand writes - so the
    /// five thousandth of them opened five thousand files to add up their lengths, which costs more than the
    /// resize it follows.  Now what is written is added to a running total and the folder is only walked when
    /// that total says the room is used up, which is once per filling rather than once per picture.  The total
    /// is wrong by whatever another program moved about, and every sweep weighs the shelf again, so it cannot
    /// drift upward unheard.
    /// </para>
    /// </remarks>
    private static void MakeRoom(string folder, long justWritten)
    {
      lock (ROOM_LOCK)
      {
        try
        {
          if (_worn < 0)
            _worn = Weighed(folder);

          _worn += justWritten;
          if (_worn <= LONGEST_ROW_OF_BYTES)
            return;

          FileInfo[] pictures = new DirectoryInfo(folder).GetFiles("*", SearchOption.AllDirectories);
          Array.Sort(pictures, (left, right) => left.LastWriteTimeUtc.CompareTo(right.LastWriteTimeUtc));

          long left = 0;
          for (int i = 0; i < pictures.Length; i++)
            left += pictures[i].Length;

          for (int i = 0; i < pictures.Length && left > LONGEST_ROW_OF_BYTES; ++i)
          {
            try
            {
              left -= pictures[i].Length;
              pictures[i].Delete();
            }
            catch (Exception)
            {
              // A file another program is holding is not a reason to stop making room for the ones that are free.
            }
          }

          _worn = left;
        }
        catch (Exception)
        {
          // A shelf that could not be read this time is looked at again by the next picture.
          _worn = -1;
        }
      }
    }

    /// <summary>
    /// What the shelf holds right now, weighed on the disk.
    /// </summary>
    private static long Weighed(string folder)
    {
      long total = 0;
      foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
      {
        try
        {
          total += new FileInfo(file).Length;
        }
        catch (Exception)
        {
          // Gone between the listing and the weighing - which is what making room looks like from the other side.
        }
      }

      return total;
    }
  }
}
