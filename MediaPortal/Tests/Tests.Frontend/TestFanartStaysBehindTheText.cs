/*
 * MediaPortal 2 - Avalonia front end
 *
 * This code distributed under the GPLv3. The source of the project is
 * located at https://github.com/Ge2301/multiplatform
 */

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using NUnit.Framework;
using SkiaSharp;

namespace Tests.Frontend
{
  /// <summary>
  /// The dial that throws the fanart behind the media out of focus: six answers from nought to five, eight
  /// pixels of blur a turn, and forty at the top - and a picture that really is out of focus when it arrives.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This dial is not the reference's - it shipped no blur - so what it promises is written down here instead:
  /// a viewer who never touched it gets the library's picture unharmed, and a viewer who turns it gets the same
  /// number of pixels on the home as on a shelf, because both read one setting.
  /// </para>
  /// <para>
  /// The blur is asserted on pixels rather than on a request. The first version of this asked the window to
  /// blur the picture and drew the picture sharp, because a compositor may decline to blur one of its visuals
  /// and say nothing about it; so what is tested here is that the bytes that leave the converter are a picture
  /// whose edge has been softened, which no compositor can put right again.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestFanartStaysBehindTheText
  {
    /// <summary>The converter every surface that paints a fanart binds its picture through.</summary>
    private const string FANART_BINDING =
        "MultiBinding Converter=\"{x:Static views:SkinFanartConverter.Instance}\"";

    [Test]
    public void TheDialStartsAtPlainAndEveryTurnIsEightPixels()
    {
      NereusSkinSettings settings = new NereusSkinSettings();

      Assert.That(settings.FanartBlur, Is.Zero,
          "a picture the viewer never asked about arrives blurred otherwise, and nobody asks for that");
      Assert.That(settings.FanartBlurRadius, Is.Zero);

      for (int turn = 1; turn <= NereusSkinSettings.FANART_BLUR_MAX; turn++)
      {
        settings.FanartBlur = turn;
        Assert.That(settings.FanartBlurRadius, Is.EqualTo(turn * 8),
            "turn " + turn + " of the dial is not worth the eight pixels it was promised");
      }

      Assert.That(NereusSkinSettings.FANART_BLUR_MAX * NereusSkinSettings.FANART_BLUR_STEP_PIXELS,
          Is.EqualTo(40), "the top of the dial was asked for as forty pixels of blur");
    }

    [Test]
    public void ADialWrittenPastItsHighestStillBlursNoMoreThanItsHighest()
    {
      NereusSkinSettings settings = new NereusSkinSettings { FanartBlur = 9 };
      Assert.That(settings.FanartBlurRadius, Is.EqualTo(40),
          "a hand-edited settings file may not ask for a blur the dial never offers");

      settings.FanartBlur = -3;
      Assert.That(settings.FanartBlurRadius, Is.Zero, "and nought way past the bottom is still no blur");
    }

    [Test]
    public void TheTurnChosenOnceIsFoundAgainInTheFile()
    {
      string file = Path.Combine(Path.GetTempPath(), "mp2-skin-settings-" + Guid.NewGuid().ToString("N") + ".json");

      try
      {
        SkinSettingsBase settings = new NereusSkinSettings { FanartBlur = 4 };
        Assert.That(settings.Save(file, out bool saved), Does.Contain("Saved"));
        Assert.That(saved, Is.True);

        string written = File.ReadAllText(file);
        Assert.That(written, Does.Contain("\"fanartBlur\""),
            "the choice is filed under the name the settings screen and the viewer's own edits both use");

        NereusSkinSettings back = NereusSkinSettings.Load(file, out string? problem);
        Assert.That(problem, Is.Null);
        Assert.That(back.FanartBlur, Is.EqualTo(4), "four turns stayed four turns");
        Assert.That(back.FanartBlurRadius, Is.EqualTo(32));
      }
      finally
      {
        File.Delete(file);
      }
    }

    [Test]
    public void OnlyTheFaceThatWearsThePictureAcrossTheScreenSpeaksABlur()
    {
      Assert.That(HomeViewModel.FanartBlurFor(new NereusSkinSettings { FanartBlur = 2 }), Is.EqualTo(16));
      Assert.That(HomeViewModel.FanartBlurFor(new WMCSkinSettings()), Is.Zero,
          "WMC never blurred its fanart; the dial is not its dial");
      Assert.That(HomeViewModel.FanartBlurFor(null), Is.Zero,
          "a machine wearing its own face has no dial to read and shows the picture plain");
    }

    [AvaloniaTest]
    public void TheDialAtNoughtIsTheVeryPictureTheArtworkConverterHandsOut()
    {
      byte[] bytes = PictureOfTwoHalves(96, 96);
      object? plain = ArtworkConverter.Instance.Convert(bytes, typeof(object), null, CultureInfo.InvariantCulture);

      Assert.That(plain, Is.InstanceOf<Bitmap>());
      Assert.That(Worn(bytes, 0), Is.SameAs(plain),
          "a picture nobody asked to blur is copied, resized and blurred anyway, and the artwork converter's "
          + "remembered picture is thrown away with it");
      Assert.That(Worn(bytes, 0), Is.SameAs(Worn(bytes, -4)), "nought way below nought is still no work");
    }

    [Test]
    public void NoBytesIsNoBackground()
    {
      Assert.That(Worn(null, 24), Is.Null);
      Assert.That(Worn(Array.Empty<byte>(), 24), Is.Null);
      Assert.That(SkinFanartConverter.BlurToPng("this is not a picture"u8.ToArray(), 24, out _, out _), Is.Null,
          "bytes that hold no picture are no background, and say nothing about it");
    }

    [Test]
    public void ATurnOfTheDialSoftensAnEdgeItIsGiven()
    {
      byte[] bytes = PictureOfTwoHalves(96, 96);

      byte[]? png = SkinFanartConverter.BlurToPng(bytes, 24, out int width, out int height);

      Assert.That(png, Is.Not.Null, "a blur asked for a real picture and was given nothing");
      Assert.That(width, Is.EqualTo(96 / SkinFanartConverter.BLUR_SCALE),
          "the blurred copy is not drawn at the share the converter promises, so it costs more than it said");
      Assert.That(height, Is.EqualTo(96 / SkinFanartConverter.BLUR_SCALE));

      using SKBitmap blurred = SKBitmap.Decode(png!);
      Assert.That(blurred.Width, Is.EqualTo(width), "the picture handed to the screen is not the picture blurred");

      // The picture is white on its left half and black on its right, so an untouched pixel is 255 or 0 and
      // only a blurred one is anything in between.
      Assert.That(blurred.GetPixel(2, height / 2).Red, Is.GreaterThan(200),
          "the far side of a blurred picture is not its own colour any more");
      Assert.That(blurred.GetPixel(width / 2, height / 2).Red, Is.LessThan(215).And.GreaterThan(40),
          "the edge down the middle of the picture is as sharp as it arrived, so nothing was blurred");
      Assert.That(blurred.GetPixel(width - 3, height / 2).Red, Is.LessThan(55),
          "the dark half came back lighter than it was");
    }

    [AvaloniaTest]
    public void TheSamePictureAtTheSameTurnIsBlurredOnce()
    {
      byte[] bytes = PictureOfTwoHalves(96, 96);

      object? first = Worn(bytes, 24);
      object? again = Worn(bytes, 24);

      Assert.That(first, Is.InstanceOf<Bitmap>());
      Assert.That(again, Is.SameAs(first),
          "a binding is asked again on every layout pass, and a blur that answered each one would be felt as "
          + "the shelf is walked");
      Assert.That(Worn(bytes, 32), Is.Not.SameAs(first), "a different turn of the dial is a different picture");
    }

    [Test]
    public void EverySurfacePaintingAFanartAsksForTheBlurredPicture()
    {
      string source = MetadataPath("FrontendSource");

      foreach (string[] surface in new[]
      {
        new[] { "Skins", "Nereus", "home.xaml" },
        new[] { "Views", "HomeView.axaml" },
        new[] { "Views", "MediaBrowserView.axaml" },
      })
      {
        string[] parts = new[] { source }.Concat(surface).ToArray();
        string file = File.ReadAllText(Path.Combine(parts));
        string name = string.Join(Path.DirectorySeparatorChar.ToString(), surface);

        Assert.That(file, Does.Contain(FANART_BINDING),
            name + " paints a fanart straight off the bytes, so the skin's dial is not worn there");
        Assert.That(file, Does.Contain("<Binding Path=\"FanartBlurRadius\"/>"),
            name + " binds a fanart without telling the converter how far out of focus to put it");
        Assert.That(file, Does.Not.Contain("Margin=\"-60\""),
            name + " still over-scans its fanart; the blur carries its own edges now");
      }
    }

    /// <summary>What the converter answers for these bytes at this turn of the dial.</summary>
    private static object? Worn(byte[]? bytes, double radius)
    {
      return SkinFanartConverter.Instance.Convert(
          new object?[] { bytes, radius }, typeof(object), null, CultureInfo.InvariantCulture);
    }

    /// <summary>A picture that is white on its left half and black on its right, as bytes.</summary>
    private static byte[] PictureOfTwoHalves(int width, int height)
    {
      var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
      using SKSurface surface = SKSurface.Create(info)!;
      surface.Canvas.Clear(new SKColor(255, 255, 255));
      using SKPaint paint = new SKPaint { Color = new SKColor(0, 0, 0) };
      surface.Canvas.DrawRect(new SKRect(width / 2f, 0, width, height), paint);
      surface.Flush();

      using SKImage image = surface.Snapshot();
      using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
      return data.ToArray();
    }

    /// <summary>
    /// A directory the test project was told about at build time; see TestSkinFromDisk's helper of the same
    /// name for why a test cannot derive these from the working directory.
    /// </summary>
    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestFanartStaysBehindTheText).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));
      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project says nothing about the front end's source, so this test has nothing to read");
      string path = value!.Value!;
      Assert.That(Directory.Exists(path), Is.True, "the folder it names is not a directory");
      return path;
    }
  }
}
