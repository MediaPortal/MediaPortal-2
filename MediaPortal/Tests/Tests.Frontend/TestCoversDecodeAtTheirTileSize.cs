/*
 * MediaPortal 2 - Avalonia front end
 *
 * This code distributed under the GPLv3. The source of the project is
 * located at https://github.com/Ge2301/multiplatform
 */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The covers a wall or a shelf lays out by the dozen are decoded at the size they are drawn at, never at the
  /// size the library happens to hold them.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A poster out of the server's fanart cache is a scan of two thousand by three thousand pixels. Decoded whole
  /// that is twenty-four megabytes of pixels per cover and seventy milliseconds of the thread that draws, and a
  /// home wall realises seventeen to nineteen covers in one beat - so every step along the menu decoded a whole
  /// wall again, which is the hitch a viewer feels as they walk the tiles. The decoded pictures cannot hide in
  /// the cache either: a whole scan is larger than what the converter's whole budget holds, and a picture too
  /// large for it is not remembered at all.
  /// </para>
  /// <para>
  /// Nothing at run time would notice a template that forgot the width, because a cover decoded whole looks
  /// exactly like one decoded small - it only costs. So this reads the templates themselves, the way the fanart
  /// blur is read, and names every binding that asks the converter for a picture without saying how wide it
  /// needs to be.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestCoversDecodeAtTheirTileSize
  {
    /// <summary>
    /// A picture a template asks for through the artwork converter, however it is wrapped: the converter is
    /// reached by <c>x:Static</c> in a skin's templates and by the resource key in a view's own.
    /// </summary>
    private static readonly Regex ARTWORK_BINDING = new Regex(
        "Source=\"\\{Binding[^\"]*(?:ArtworkConverter\\.Instance|StaticResource artwork)[^\"]*\\}\"",
        RegexOptions.Compiled);

    /// <summary>
    /// The pictures that are not drawn inside a tile and are therefore asked for whole: the details screen's
    /// poster, which is a third of that screen, and a backdrop that is worn across the whole of one.
    /// </summary>
    private static readonly string[] WHOLE_PICTURE_BINDINGS = { "DetailsArtwork", "BackgroundArtwork" };

    [Test]
    public void EveryCoverADrawersLaysOutNamesTheWidthItNeeds()
    {
      string source = MetadataPath("FrontendSource");

      foreach (string[] surface in new[]
      {
        new[] { "Views", "HomeView.axaml" },
        new[] { "Views", "MediaBrowserView.axaml" },
        new[] { "Skins", "Nereus", "themes", "default", "templates.xaml" },
        new[] { "Skins", "WMC", "themes", "default", "templates.xaml" },
      })
      {
        string file = File.ReadAllText(Path.Combine(new[] { source }.Concat(surface).ToArray()));
        string name = string.Join(Path.DirectorySeparatorChar.ToString(), surface);

        foreach (Match binding in ARTWORK_BINDING.Matches(file).Cast<Match>())
        {
          string text = binding.Value;
          if (WHOLE_PICTURE_BINDINGS.Any(whole => text.Contains(whole, StringComparison.Ordinal)))
            continue;

          Assert.That(text, Does.Contain("ConverterParameter"),
              name + " draws a cover inside a tile without telling the converter the width it is drawn at, which"
              + " decodes the library's whole scan for every one of them (" + Shorten(text) + ")");
        }
      }
    }

    [Test]
    public void TheWallCoversAskForTheWidthTheWallDrawsThemAt()
    {
      string file = File.ReadAllText(Path.Combine(MetadataPath("FrontendSource"), "Views", "HomeView.axaml"));

      Assert.That(file, Does.Contain(
              "Title.Artwork, Converter={x:Static views:ArtworkConverter.Instance}, ConverterParameter=wall"),
          "the home wall's covers are what a viewer walks past by the dozen; decoded whole they are the one cost"
          + " that makes the wall arrive in hitches, and decoded at the tile's width the two by two cover at a"
          + " band's head would be stretched instead of drawn");
    }

    /// <summary>
    /// And nothing frosts inside a cover. Measured over eight switches at 3440x1440, one frosting surface behind
    /// a cover whose poster had not arrived yet cost 51 frames slower than 40 ms and 5140 ms of their time; with
    /// it gone the same switches leave 25 such frames and 2855 ms, and a cover's plate is what the reference
    /// showed anyway - Nereus shipped no blur at all. A frosting surface costs nothing at run time that anyone
    /// can see, it only costs, so this is said to the templates.
    /// </summary>
    [Test]
    public void NothingFrostsInsideACover()
    {
      string file = File.ReadAllText(Path.Combine(MetadataPath("FrontendSource"), "Views", "HomeView.axaml"));

      Assert.That(file, Does.Not.Contain("home-tile-frost"),
          "a cover standing without its picture frosts the wall behind it again; that frosting is rendered in"
          + " software once per surface per frame, and a wall arrives with a couple of dozen covers whose posters"
          + " have all not landed yet");

      // The shortcut glass is a different matter and stays: a handful of tiles that do not move, measured as
      // nothing beside the same wall wearing no glass at all, unlike a wall of them behind arriving pictures.
      // One lens per tile, and only the shortcuts wear it - the count is what says so.
      Assert.That(Regex.Matches(file, "<lg:LiquidGlassSurface").Count, Is.EqualTo(1),
          "HomeView grew glass surfaces beyond the one shortcut rail it was measured with");
    }

    /// <summary>A binding shortened to what fits on a complaint line.</summary>
    private static string Shorten(string text)
    {
      string oneLine = string.Join(" ', '", text.Split('\n').Select(line => line.Trim()));
      return oneLine.Length <= 120 ? oneLine : oneLine.Substring(0, 120) + "...";
    }

    /// <summary>
    /// A directory the test project was told about at build time; see TestSkinFromDisk's helper of the same
    /// name for why a test cannot derive these from the working directory.
    /// </summary>
    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestCoversDecodeAtTheirTileSize).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));
      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");
      return Path.GetFullPath(value!.Value!);
    }
  }
}
