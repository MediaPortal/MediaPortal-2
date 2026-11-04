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
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Server.Api;
using NUnit.Framework;
using SkiaSharp;

namespace Tests.Server.FanArt
{
  /// <summary>
  /// Tests how <see cref="FanArtVariants"/> answers a front end that said how wide it paints a picture, which is
  /// the half of the reference's fan art service that this server had lost.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A library holds the poster somebody downloaded - twelve hundred, two thousand pixels across - and a front end
  /// draws it three hundred pixels wide. Until this class existed the whole file crossed the wire and every viewer
  /// in the house paid a decoder for it again. The reference answered such a question out of a folder of resized
  /// pictures under <c>&lt;DATA&gt;\Thumbs\FanArt</c>, and these tests pin the three things that answer has to get
  /// right: it is made at a step of the ladder and not at an arbitrary width, it is never bigger than the picture
  /// the library holds, and a picture that cannot be resized honestly is handed out as it is.
  /// </para>
  /// <para>
  /// The pictures here are drawn rather than carried as test data, because what matters about them is their size
  /// and whether they carry alpha - both of which are said out loud in the test that uses them.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestFanArtVariants
  {
    #region Setting up a server with a data folder

    /// <summary>
    /// The data folder this run writes its shelf into.
    /// </summary>
    private string _dataFolder = string.Empty;

    /// <summary>
    /// Whatever path manager this process had before the test put its own in, so that the test can hand it back.
    /// </summary>
    private IPathManager? _pathsWereThere;

    [SetUp]
    public void ProvideADataFolder()
    {
      _pathsWereThere = ServiceRegistration.Get<IPathManager>(false);

      _dataFolder = Path.Combine(Path.GetTempPath(), "mp2-variants-" + Guid.NewGuid().ToString("N"));
      PathManager paths = new PathManager();
      paths.SetPath("DATA", _dataFolder);
      ServiceRegistration.Set<IPathManager>(paths);
    }

    [TearDown]
    public void TakeTheDataFolderAway()
    {
      ServiceRegistration.Remove<IPathManager>();
      if (_pathsWereThere != null)
        ServiceRegistration.Set<IPathManager>(_pathsWereThere);

      if (Directory.Exists(_dataFolder))
        Directory.Delete(_dataFolder, true);
    }

    /// <summary>
    /// The folder the shelf is expected to appear in, which is the reference's own place.
    /// </summary>
    private string ShelfFolder()
    {
      return Path.Combine(_dataFolder, "Thumbs", "FanArt");
    }

    #endregion

    #region Pictures of a size we choose

    /// <summary>
    /// Drawing a picture of the given size, in the given container.
    /// </summary>
    /// <param name="width">How wide the picture is to be.</param>
    /// <param name="height">How high the picture is to be.</param>
    /// <param name="format">The container the bytes are to be filed in.</param>
    /// <param name="transparent">Whether the right half is to be transparent.</param>
    private static byte[] Picture(int width, int height, SKEncodedImageFormat format, bool transparent = false)
    {
      using SKBitmap bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
      using (SKCanvas canvas = new SKCanvas(bitmap))
      {
        canvas.Clear(new SKColor(30, 60, 120, 255));
        using SKPaint band = new SKPaint { Color = new SKColor(220, 40, 40, 255) };
        canvas.DrawRect(0, 0, width / 4f, height, band);
        if (transparent)
        {
          using SKPaint hole = new SKPaint { Color = SKColors.Transparent };
          canvas.DrawRect(width * 0.5f, 0, width * 0.5f, height, hole);
        }
      }

      using SKImage image = SKImage.FromBitmap(bitmap);
      using SKData data = image.Encode(format, format == SKEncodedImageFormat.Png ? 100 : 90);
      return data.ToArray();
    }

    /// <summary>
    /// Asking a picture's bytes how big they are, which is what a front end finds out when it decodes them.
    /// </summary>
    private static SKImageInfo Dimensions(byte[] bytes)
    {
      using SKCodec codec = SKCodec.Create(new MemoryStream(bytes));
      Assert.IsNotNull(codec, "the answer was not a picture a codec would look at");
      return codec.Info;
    }

    /// <summary>
    /// The name the shelf keeps the answer for these bytes at the given step under, without its extension.
    /// </summary>
    private static string Stem(byte[] picture, int step)
    {
      return step.ToString(CultureInfo.InvariantCulture) + "x" + step.ToString(CultureInfo.InvariantCulture)
          + "_" + FingerprintOf(picture);
    }

    /// <summary>
    /// The fingerprint the shelf names its files by. Repeated here rather than reached into the class under test,
    /// so that a test which found a file by this name proves the file is where a reader of the code would look.
    /// </summary>
    private static string FingerprintOf(byte[] bytes)
    {
      return Convert.ToHexString(System.Security.Cryptography.MD5.HashData(bytes).AsSpan(0, 8));
    }

    #endregion

    #region Answering a width

    [Test]
    public void TheCodecIsAskedForARungItCanAnswerAndNotForAnArbitraryWidth()
    {
      //  A JPEG decoder reads a picture at an eighth, a quarter or a half of its size and at nothing in between, so
      //  "read me a third of this scan" is not a question it answers - it says InvalidScale and hands back nothing.
      //  That is the step the shelf has to go around, and it is read out here because a shelf that walked into it
      //  answers with the original picture and says nothing at all, which is exactly how the first version of this
      //  class failed.
      byte[] scan = Picture(1920, 1080, SKEncodedImageFormat.Jpeg);
      using SKData bytes = SKData.CreateCopy(scan);
      using SKCodec? codec = SKCodec.Create(bytes);
      Assert.IsNotNull(codec, "SkiaSharp's codec did not look at a JPEG it should have read");

      using SKBitmap third = new SKBitmap(new SKImageInfo(640, 360, SKColorType.Bgra8888, SKAlphaType.Premul));
      Assert.AreEqual(SKCodecResult.InvalidScale, codec!.GetPixels(third.Info, third.GetPixels()),
          "the codec answered an arbitrary width after all, so the ladder the shelf climbs can be shortened");

      SKSizeI rung = codec.GetScaledDimensions(640f / 1920f);
      Assert.Greater(rung.Width, 0, "the codec offered no rung to stand on");

      using SKBitmap sheet = new SKBitmap(
          new SKImageInfo(rung.Width, rung.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
      Assert.AreEqual(SKCodecResult.Success, codec.GetPixels(sheet.Info, sheet.GetPixels()),
          "the codec refused the very rung it had just named");
      Assert.Less(sheet.Width, 1920, "the rung was not a step down from the scan");
    }

    [Test]
    public void AQuestionWithoutAWidthIsThePictureItself()
    {
      // An old front end against a new server says nothing about width, and must get exactly what it got before.
      byte[] picture = Picture(1200, 800, SKEncodedImageFormat.Jpeg);

      byte[] answer = FanArtVariants.Worn.Sized(picture, 0);

      Assert.AreSame(picture, answer, "a caller who asked no size must not have its picture touched");
      Assert.IsFalse(Directory.Exists(ShelfFolder()),
          "a server that was never asked for a size has no business creating the shelf");
    }

    [Test]
    public void AScanIsAnsweredAtTheStepItWasAskedFor()
    {
      // The complaint this exists for: a poster scanned at two thousand pixels, painted at about six hundred and
      // forty. The answer has to BE six hundred and forty across, or the front end still decodes a scan.
      byte[] picture = Picture(1920, 1080, SKEncodedImageFormat.Jpeg);

      byte[] answer = FanArtVariants.Worn.Sized(picture, 640);

      SKImageInfo size = Dimensions(answer);
      Assert.AreEqual(640, size.Width, "the answer is not at the step the caller named");
      Assert.AreEqual(360, size.Height, "the picture was not kept in proportion");
      Assert.Less(answer.Length, picture.Length, "the smaller picture came out bigger than the scan");
    }

    [Test]
    public void APortraitPosterIsAnsweredAtItsWidthAndNotInsideASquare()
    {
      //  The wall's cover is five hundred and seventy three device pixels wide whether the poster standing in it is
      //  landscape or portrait, but the reference asked its two questions <c>width</c> and <c>height</c> together
      //  and fitted the picture inside the square that makes.  For a poster of two by three parts that answers a
      //  640 question with a picture four hundred and twenty seven pixels wide - a twelfth of the scan, and still
      //  stretched where it is painted, which is the whole complaint.  Seen on the live server with a library
      //  poster of two thousand by three thousand: 427x640 out of the square reading, 640x960 out of this one.
      byte[] poster = Picture(2000, 3000, SKEncodedImageFormat.Jpeg);

      byte[] answer = FanArtVariants.Worn.Sized(poster, 640);

      SKImageInfo size = Dimensions(answer);
      Assert.AreEqual(640, size.Width,
          "a portrait poster was fitted into the step instead of being measured across it");
      Assert.AreEqual(960, size.Height, "the poster was not kept in proportion");
    }

    [Test]
    public void TheAnswerIsKeptAndFoundAgainUnderTheReferenceName()
    {
      // One resize per title and size for the whole server, which is the half of the reference that was lost. The
      // name is the reference's own: the step, then what the library's bytes are.
      byte[] picture = Picture(1600, 900, SKEncodedImageFormat.Jpeg);

      byte[] first = FanArtVariants.Worn.Sized(picture, 512);
      byte[] second = FanArtVariants.Worn.Sized(picture, 512);

      string kept = Path.Combine(ShelfFolder(), Stem(picture, 512) + ".jpg");
      Assert.IsTrue(File.Exists(kept), "the variant was not kept in the reference's folder under the reference's name");
      Assert.AreEqual(first, second, "the second asking did not read the kept picture back");
      Assert.AreEqual(first, File.ReadAllBytes(kept));
    }

    [Test]
    public void TwoSizesOfOnePictureAreTwoPictures()
    {
      // The rail tile and the backdrop of one title are not the same ask, and neither may be handed out for the
      // other - a shelf that overwrote one with the other would show a tile inside the backdrop.
      byte[] picture = Picture(1400, 1000, SKEncodedImageFormat.Jpeg);

      byte[] tile = FanArtVariants.Worn.Sized(picture, 432);
      byte[] backdrop = FanArtVariants.Worn.Sized(picture, 640);

      Assert.AreEqual(512, Dimensions(tile).Width, "the tile ask did not land on its own step");
      Assert.AreEqual(640, Dimensions(backdrop).Width, "the backdrop ask did not land on its own step");
      Assert.AreEqual(2, Directory.GetFiles(ShelfFolder()).Length,
          "the two steps did not become two files in the shelf");
    }

    [Test]
    public void AWidthBetweenTwoStepsTakesTheSmallerThatStillCoversIt()
    {
      // A front end that says 322 must not be given the 512 step when 512 is the smallest that covers it... and
      // must not be given the 256 either. The ladder is the deal: the smallest rung that reaches.
      Assert.AreEqual(256, FanArtVariants.BestSize(256));
      Assert.AreEqual(512, FanArtVariants.BestSize(257));
      Assert.AreEqual(512, FanArtVariants.BestSize(432));
      Assert.AreEqual(1024, FanArtVariants.BestSize(641));
    }

    [Test]
    public void AQuestionLargerThanTheLadderIsAnsweredAtTheBiggestStep()
    {
      // Asking more than the ladder holds is a caller that wants the original, and the biggest rung is the
      // original for everything a library keeps.
      Assert.AreEqual(4096, FanArtVariants.BestSize(8192));
    }

    #endregion

    #region Not making a picture worse

    [Test]
    public void APictureNarrowerThanTheAskIsNotGrown()
    {
      // The reference refused to enlarge and so must this: a copy blown up is blurry and bigger at once, which is
      // the one thing a resize exists to end.
      byte[] picture = Picture(300, 200, SKEncodedImageFormat.Jpeg);

      byte[] answer = FanArtVariants.Worn.Sized(picture, 1024);

      Assert.AreSame(picture, answer, "a small picture was not handed out as it was");
      Assert.AreEqual(0, Directory.Exists(ShelfFolder()) ? Directory.GetFiles(ShelfFolder()).Length : 0,
          "a refused enlargement should leave nothing behind in the shelf");
    }

    [Test]
    public void APngWithTransparencyStaysAPng()
    {
      // What the reference's rule was, and what a WebP-less tree needs: alpha is kept by keeping the container
      // that carries it, or a title's lettering comes out of the server with a black square behind it.
      byte[] picture = Picture(1200, 600, SKEncodedImageFormat.Png, true);

      byte[] answer = FanArtVariants.Worn.Sized(picture, 256);

      using SKCodec codec = SKCodec.Create(new MemoryStream(answer));
      Assert.AreEqual(SKEncodedImageFormat.Png, codec.EncodedFormat,
          "a transparent picture was filed as a JPEG, which has no alpha to give back");
      Assert.AreNotEqual(SKAlphaType.Opaque, codec.Info.AlphaType,
          "the answer carries no transparency although the library's picture does");
      Assert.IsTrue(File.Exists(Path.Combine(ShelfFolder(), Stem(picture, 256) + ".png")),
          "a PNG variant is looked for under .png, so it has to be written there");
    }

    [Test]
    public void BytesThatAreNotAPictureAreHandedOutAsTheyAre()
    {
      // A truncated download, a zero-length file behind an empty NFO, a file a tag wrote badly. The picture a
      // front end gets for these is the bytes the library holds - it is that front end that shows a placeholder
      // when it cannot decode, and a server that answered a failure with an error page would hide the reason.
      byte[] notAPicture = { 0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE, 0xFD };

      byte[] answer = FanArtVariants.Worn.Sized(notAPicture, 512);

      Assert.AreEqual(notAPicture, answer);
    }

    [Test]
    public void NothingAskedForNothingIsNothingGiven()
    {
      Assert.IsNull(FanArtVariants.Worn.Sized(null!, 512));
      Assert.AreEqual(0, FanArtVariants.Worn.Sized(new byte[0], 512).Length);
    }

    #endregion
  }
}
