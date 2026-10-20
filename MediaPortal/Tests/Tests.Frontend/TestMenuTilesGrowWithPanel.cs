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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The main menu's tiles are square, rounded to the same corner the covers on the wall wear, and as big as the
  /// panel they are drawn on - not as big as the television the reference was drawn for.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Three things hold that together, and each is a separate way to lose the shape: the face keeps its sizes in
  /// its own metrics as shares of the panel's height, the tile binds its width AND its height to the one number
  /// the screen works out of them (bind only one and the tile stops being a square), and the column of ground
  /// behind the rail reads the same number so that it never stops short of the tiles it carries.
  /// </para>
  /// <para>
  /// The corner is asserted too. A tile whose corner is rounded but whose content is not clipped shows the round
  /// corner as a bite out of a square picture, which is what a viewer sees as the rounding doing nothing.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestMenuTilesGrowWithPanel
  {
    /// <summary>The panel the front end is drawn on here, which is what the shares are read against.</summary>
    private const double PANEL_LINES = 1440;

    /// <summary>How much bigger than the drawn tile the face asks for.</summary>
    private const double GROWN_BY = 1.2;

    /// <summary>
    /// A screen that has not measured itself shows exactly what the face drew, rather than an empty rail.
    /// </summary>
    [Test]
    public void AFaceThatSaysNothingKeepsTheSizesItDrew()
    {
      HomeViewModel home = new HomeViewModel();

      Assert.That(home.MenuTileSize, Is.EqualTo(HomeViewModel.MENU_TILE_DRAWN),
          "a screen that has not measured itself yet must show what the face drew, not nothing");
      Assert.That(home.MenuRailWidth, Is.EqualTo(HomeViewModel.MENU_RAIL_DRAWN));
      Assert.That(home.MenuRailWidth, Is.GreaterThan(home.MenuTileSize),
          "the column has to hold the tile and its margin, or the tiles hang over the ground beside them");
    }

    /// <summary>
    /// The Nereus metrics size the rail off the height it is drawn on, and grow it by the fifth asked for.
    /// </summary>
    [AvaloniaTest]
    public void TheFaceSizesItsRailFromThePanelItIsDrawnOn()
    {
      Application app = Application.Current!;
      string previous = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      try
      {
        FrontendTheme.Apply(app, "nereus", out string? themeProblem);
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True,
            (skinProblem ?? string.Empty) + themeProblem);

        double tileShare = SkinEngine.WornNumber("HomeMenuTileHeightRatio") ?? 0;
        double railShare = SkinEngine.WornNumber("HomeMenuRailWidthRatio") ?? 0;

        Assert.That(tileShare, Is.GreaterThan(0),
            "the face writes no share for its menu tiles, so the rail stays the size a 2010 television was");
        Assert.That(railShare, Is.GreaterThan(0),
            "the face writes no share for its menu column, so the ground stops short of grown tiles");

        double grown = HomeViewModel.MENU_TILE_DRAWN * GROWN_BY;
        Assert.That(tileShare * PANEL_LINES, Is.GreaterThanOrEqualTo(grown * 0.99),
            "a tile on this panel should stand at least the grown " + grown.ToString("F0")
            + " lines tall, which is what the face promises");
        Assert.That(tileShare * PANEL_LINES, Is.LessThan(PANEL_LINES * 0.4),
            "a menu tile may not eat most of the screen; the share is written wrong");
        Assert.That(railShare, Is.GreaterThan(tileShare),
            "the column is wider than the square it carries, which is how the face drew it");
        Assert.That((railShare - tileShare) * PANEL_LINES, Is.GreaterThanOrEqualTo(12),
            "the column has to leave a tile its margin on both sides");
      }
      finally
      {
        SkinEngine.SkinsFolder = previous;
      }
    }

    /// <summary>
    /// The tile binds both of its sides to the number the screen hands over, is clipped, and wears the wall's ten.
    /// </summary>
    [Test]
    public void AMenuTileIsSquareAndCutToTheCoversCorner()
    {
      string template = ShippedTemplate("HomeMenuTileTemplate");
      string size = "Binding $parent[ListBox].DataContext.MenuTileSize";

      Assert.That(template, Does.Contain("Width=\"{" + size + "}\""),
          "a menu tile still carries a size of its own, so it cannot grow with the panel");
      Assert.That(template, Does.Contain("Height=\"{" + size + "}\""),
          "one side of the tile follows the screen while the other keeps its drawn length, so the tile is a "
          + "rectangle and not the square the menu was built out of");
      Assert.That(template, Does.Contain("ClipToBounds=\"True\""),
          "the tile paints its ground, artwork and label band over the whole of itself, so a rounded corner "
          + "without a clip is only a bite out of a square picture");

      // The two rows are shares of the square, or a grown tile would keep its drawn artwork and band lengths.
      Assert.That(template, Does.Contain("Height=\"130*\""),
          "the artwork over the label band has to be a share of the tile, or growing the tile leaves the band "
          + "behind at the height the face drew");
      Assert.That(template, Does.Contain("Height=\"40*\""),
          "the label band has to be a share of the tile, or growing the tile leaves the band behind");

      string styles = File.ReadAllText(Path.Combine(MetadataPath("FrontendSource"),
          "Skins", "Nereus", "themes", "default", "styles.xaml"));
      int at = styles.IndexOf("Selector=\"Border.nereus-menu-tile\"", StringComparison.Ordinal);
      Assert.That(at, Is.GreaterThanOrEqualTo(0),
          "nothing rounds a menu tile's corner, so the face would clip a square corner");
      string block = styles.Substring(at, Math.Min(400, styles.Length - at));
      Assert.That(block, Does.Contain("CornerRadius\" Value=\"10\""),
          "a menu tile would wear a different corner than the covers on the wall beside it");
    }

    /// <summary>
    /// The ground behind the rail is as wide as the rail the screen laid out, not as wide as the column the face
    /// happened to draw in 2010.
    /// </summary>
    [Test]
    public void TheGroundBehindTheRailFollowsTheTiles()
    {
      string home = File.ReadAllText(Path.Combine(MetadataPath("FrontendSource"),
          "Skins", "Nereus", "home.xaml"));

      Assert.That(home, Does.Contain("Width=\"{Binding MenuRailWidth}\""),
          "the column the menu stands on keeps a drawn width, so grown tiles hang past the ground the face "
          + "paints under them");
    }

    /// <summary>
    /// What stands beside the rail begins where the rail ends: the wall already does, and so do the buttons at
    /// the top edge and the line of text under the tiles.
    /// </summary>
    [Test]
    public void WhatStandsBesideTheRailFollowsIt()
    {
      HomeViewModel home = new HomeViewModel();

      Assert.That(home.HomeContentLeft,
          Is.EqualTo(HomeViewModel.MENU_RAIL_DRAWN + HomeViewModel.CONTENT_GAP),
          "before a screen has measured itself, the content begins where the face drew it");

      home.MenuRailWidth = 400;
      Assert.That(home.HomeContentLeft, Is.EqualTo(400 + HomeViewModel.CONTENT_GAP),
          "the content still begins at the drawn place while the rail grew past it - the rail would stand "
          + "over the wall, the top buttons and the line of text under the tiles");

      string markup = File.ReadAllText(Path.Combine(MetadataPath("FrontendSource"),
          "Skins", "Nereus", "home.xaml"));

      Assert.That(markup, Does.Not.Contain("Margin=\"240"),
          "the face pins something to the rail's drawn width again, so a grown rail lies over it");
      Assert.That(markup, Does.Contain("Binding HomeContentLeft"),
          "the overlays beside the rail keep a number of their own instead of the place the screen measured");

      // The face keeps its vertical offsets: only the left edge of a drawn margin moves.
      Assert.That(markup, Does.Contain("ConverterParameter='0,0,0,34'"),
          "the line under the tiles lost the thirty-four the face drew under it");
      Assert.That(markup, Does.Contain("ConverterParameter='0,10,0,0'"),
          "the buttons at the top edge lost the ten the face drew above them");
    }

    /// <summary>
    /// The converter moves the left edge of a margin and reads the face's numbers the way a skin file writes
    /// them, whatever culture the run happens to be in.
    /// </summary>
    [Test]
    public void TheInsetMovesOnlyTheLeftEdgeOfTheMarginItIsGiven()
    {
      IValueConverter converter = SkinInsetConverter.Instance;
      foreach (CultureInfo culture in new[] { CultureInfo.InvariantCulture, new CultureInfo("de-DE") })
      {
        Thickness line = (Thickness)converter.Convert(280.0, typeof(Thickness), "0,0,0,34", culture)!;
        Assert.That(line, Is.EqualTo(new Thickness(280, 0, 0, 34)),
            "the line under the tiles would sit at the wrong height or the wrong edge on a " + culture.Name);

        Thickness buttons = (Thickness)converter.Convert(280.0, typeof(Thickness), "0,10,0,0", culture)!;
        Assert.That(buttons, Is.EqualTo(new Thickness(280, 10, 0, 0)),
            "the top buttons would sit at the wrong height on a " + culture.Name);

        Thickness even = (Thickness)converter.Convert(120.0, typeof(Thickness), "8", culture)!;
        Assert.That(even, Is.EqualTo(new Thickness(120, 8, 8, 8)),
            "a one-number margin keeps its number on the three edges the screen was not asked about");

        Thickness pair = (Thickness)converter.Convert(120.0, typeof(Thickness), "6,14", culture)!;
        Assert.That(pair, Is.EqualTo(new Thickness(120, 6, 14, 6)),
            "a two-number margin keeps its vertical and right numbers");

        Thickness nothing = (Thickness)converter.Convert(null!, typeof(Thickness), "0,0,0,34", culture)!;
        Assert.That(nothing, Is.EqualTo(new Thickness(0, 0, 0, 34)),
            "a screen that has measured nothing still has to leave the text its own offsets");
      }
    }

    /// <summary>
    /// The band a main menu tile's name stands on fades like the strip under a cover's name does, and paints no
    /// plate across the picture.
    /// </summary>
    [Test]
    public void TheMenuTileBandFadesLikeTheCoversStrip()
    {
      string styles = File.ReadAllText(Path.Combine(MetadataPath("FrontendSource"),
          "Skins", "Nereus", "themes", "default", "styles.xaml"));
      string colors = File.ReadAllText(Path.Combine(MetadataPath("FrontendSource"),
          "Skins", "Nereus", "themes", "default", "colors.xaml"));
      string template = ShippedTemplate("HomeMenuTileTemplate");

      Assert.That(colors,
          Does.Contain("x:Key=\"TitleFadeBrush\" StartPoint=\"0%,100%\" EndPoint=\"0%,0%\""),
          "the fade has to run from the strip's bottom edge up to its top, and in percent: written as bare "
          + "fractions Avalonia reads it as absolute pixels, which is a fade one pixel long and a hard black "
          + "line where the strip begins");
      Assert.That(styles, Does.Contain("Selector=\"Rectangle.nereus-menu-band\""),
          "nothing dresses the band under a menu tile's name, so it stands where the flat plate always was");
      Assert.That(styles, Does.Contain("Fill\" Value=\"{DynamicResource TitleFadeBrush}\""),
          "the menu tile's band paints a fade of its own instead of the one the covers' strip paints");
      Assert.That(styles, Does.Contain("Background\" Value=\"{DynamicResource TitleFadeBrush}\""),
          "the covers' strip painted a fade of its own, and the two would drift apart from one another");
      Assert.That(template, Does.Contain("Classes=\"nereus-menu-band\""),
          "the menu tile's band is still drawn in the tile rather than dressed by the face");
      Assert.That(template, Does.Not.Contain("#66000000"),
          "the band lays a flat plate over the picture again instead of fading out of the tile's edge");
    }

    /// <summary>
    /// The fade both of them paint is the worn skin's own resource: black at ninety out of a hundred along the
    /// bottom edge, nothing at the top of the strip.
    /// </summary>
    [AvaloniaTest]
    public void TheFadeBothTitleStripsPaintIsTheFacesOwn()
    {
      Application app = Application.Current!;
      string previous = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      try
      {
        FrontendTheme.Apply(app, "nereus", out string? themeProblem);
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True,
            (skinProblem ?? string.Empty) + themeProblem);

        Assert.That(app.TryFindResource("TitleFadeBrush", out object? found), Is.True,
            "the face names no fade for a title to stand on, so the strips fall back to nothing");
        LinearGradientBrush? fade = found as LinearGradientBrush;
        Assert.That(fade, Is.Not.Null,
            "the title fade is no gradient brush but " + (found?.GetType().Name ?? "nothing"));

        Assert.That(fade!.GradientStops.Count, Is.EqualTo(3), "the reference's fade has three stops");
        Assert.That(fade.GradientStops[0].Color, Is.EqualTo(Color.Parse("#E6000000")),
            "the title stands on black at ninety out of a hundred, which is what makes it read on a bright cover");
        Assert.That(fade.GradientStops[0].Offset, Is.EqualTo(0),
            "and it stands on the fullest black at its own edge");
        Assert.That(fade.GradientStops[2].Color, Is.EqualTo(Color.Parse("#00000000")),
            "and on nothing at the top of the strip, so the picture stays whole");
        Assert.That(fade.GradientStops[2].Offset, Is.EqualTo(1));
      }
      finally
      {
        SkinEngine.SkinsFolder = previous;
      }
    }

    /// <summary>One face's markup for one key, cut out of its shipped template file.</summary>
    private static string ShippedTemplate(string key)
    {
      string file = File.ReadAllText(Path.Combine(MetadataPath("FrontendSource"),
          "Skins", "Nereus", "themes", "default", "templates.xaml"));
      int at = file.IndexOf("x:Key=\"" + key + "\"", StringComparison.Ordinal);
      Assert.That(at, Is.GreaterThanOrEqualTo(0), "the shipped Nereus carries no " + key);
      int end = file.IndexOf("</DataTemplate>", at, StringComparison.Ordinal);
      Assert.That(end, Is.GreaterThan(at), key + " is never closed");
      return file.Substring(at, end - at);
    }

    /// <summary>
    /// A directory the test project was told about at build time; see TestSkinFromDisk's helper of the same name
    /// for why a test cannot derive these from the working directory.
    /// </summary>
    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestMenuTilesGrowWithPanel).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));
      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project says nothing about the skin folder, so this test has nothing to read");
      string path = value!.Value!;
      Assert.That(Directory.Exists(path), Is.True, "the skin folder it names is not a directory");
      return path;
    }
  }
}
