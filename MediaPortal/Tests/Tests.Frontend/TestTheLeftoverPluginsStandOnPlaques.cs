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
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LiquidGlassAvaloniaUI;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// A plugin that was not given a place in the main menu is not shown as a little cover: the reference dresses
  /// it as a plaque. Its ground is the middle colour of the family the viewer chose, its own picture is stretched
  /// over that ground rather than cropped to it, and its name stands centred in a flat band of black at the
  /// bottom of the square - no second line explaining the plugin, no glass in front of it, and no picture
  /// stepping nearer when a viewer stands on it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <see cref="TestHomeBandTakesItsRoom"/> holds the geometry of that band (seven columns, four rows, nineteen
  /// places). What this file holds is the dress: that a square the model says is a plugin is actually dressed as
  /// one on a shown screen, and that the numbers of the dress belong to the worn face instead of to the view.
  /// Both halves matter, because the failure that happened here was silent either way - the view used to hand a
  /// plugin's square to the cover's markup, which painted a dark plate, cropped the emblem and added a line of
  /// description the reference never shows, while the band itself shrank its squares to slivers in one column.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTheLeftoverPluginsStandOnPlaques
  {
    /// <summary>The videos entry, whose shortcuts give the wall something beside the plugins' band.</summary>
    private const string MOVIES_ACTION = "80d2e2cc-baaa-4750-807b-f37714153751";

    /// <summary>
    /// Every number of a plaque's dress is the face's, and the reference's own: forty lines for the band the name
    /// stands on, SmallerFontSize for the name, and a name that rests dimmed and comes to full light under a
    /// viewer. The head over a band is spelled with the same SmallerFontSize.
    /// </summary>
    [AvaloniaTest]
    public void TheSkinOwnsTheNumbersOfAPlaque()
    {
      WearTheShippedSkin();

      Assert.That(SkinEngine.WornNumber("PluginTileBandHeight") ?? 0, Is.EqualTo(40),
          "the band a plugin's name stands on is forty of the reference's 1080 lines");
      Assert.That(SkinEngine.WornNumber("PluginTileNameFontSize") ?? 0, Is.EqualTo(27),
          "and the name is written in the size the reference set for it (SmallerFontSize)");

      double resting = SkinEngine.WornNumber("PluginTileNameRestingOpacity") ?? 1;
      Assert.That(resting, Is.GreaterThanOrEqualTo(0.3).And.LessThan(1),
          $"a plaque's name rests dimmed at {resting} and comes to full light when a viewer stands on it; at one "
          + "there is nothing left to come up to");

      Assert.That(SkinEngine.WornNumber("HomeBandHeadingFontSize") ?? 0, Is.GreaterThanOrEqualTo(24),
          "the head over a band of the wall is written big enough to read across the room, as the reference's "
          + "SmallerFontSize is");

      ISolidColorBrush? band = SkinEngine.Worn(Application.Current!)?.Resources["PluginTileBandBrush"]
          as ISolidColorBrush;
      Assert.That(band, Is.Not.Null, "the skin names no brush for the band a plugin's name stands on");
      Assert.That(band!.Color.A, Is.InRange(0x40, 0x8C),
          $"the band is black the picture shows through ({band.Color.A:X2} of 255), not a plate painted over it");
      Assert.That(band.Color.R + band.Color.G + band.Color.B, Is.LessThanOrEqualTo(30),
          "and it is black, which is what makes a name in any colour read on it");
    }

    /// <summary>
    /// On the shown home screen a plugin's square is dressed as a plaque and not as a cover: it stands on the
    /// middle colour of the chosen family, it wears no glass and no second line, and its name carries the size
    /// the skin says.
    /// </summary>
    [AvaloniaTest]
    public void APluginStandsOnAPlaqueAndNotOnACover()
    {
      RunOnHome(delegate (Window window, List<Button> plaques, List<Button> covers)
      {
        Assert.That(plaques, Is.Not.Empty, "the wall put up no plugin's plaque at all");
        Assert.That(covers, Is.Not.Empty, "and no cover beside them to tell the two dresses apart");

        ISolidColorBrush? ground = SkinEngine.Worn(Application.Current!)?.Resources["PanelRaisedBrush"]
            as ISolidColorBrush;
        Assert.That(ground, Is.Not.Null, "the skin names no middle colour for a plaque to stand on");

        foreach (Button plaque in plaques)
        {
          Assert.That(((plaque.Background as ISolidColorBrush)?.Color), Is.EqualTo(ground!.Color),
              "a plugin's plaque does not stand on the middle colour of the family the viewer chose, which is what "
              + "the reference called NereusColorMedium");

          TextBlock name = plaque.GetVisualDescendants().OfType<TextBlock>()
              .Single(line => line.Classes.Contains("plugin-tile-name"));
          Assert.That(name.FontSize, Is.EqualTo(27),
              "the name is not written in the size the worn skin answers");
          Assert.That(name.Bounds.Height, Is.GreaterThan(0), "the name was never laid out");

          Border band = plaque.GetVisualDescendants().OfType<Border>()
              .Single(face => face.Classes.Contains("plugin-tile-band"));
          Assert.That(band.Bounds.Height, Is.GreaterThanOrEqualTo(40),
              "the band the name stands on did not come to the height the skin drew it in");
          Assert.That(plaque.GetVisualDescendants().OfType<TextBlock>().Count(), Is.EqualTo(1),
              "a plugin's square carries more than its name, and the reference shows nothing beside the name - "
              + "nineteen squares cannot hold a sentence");

          // The two dressings that belong to a cover and to no plaque: the glass a shortcut wears, and the fade a
          // cover's title stands on.
          Assert.That(plaque.GetVisualDescendants().OfType<LiquidGlassSurface>().Any(), Is.False,
              "a plaque wears no glass: it has a ground of the face's own colour, so a lens behind it would "
              + "sample the wall through a colour that was never meant to be see-through");
          Assert.That(plaque.GetVisualDescendants().OfType<Border>()
              .Any(face => face.Classes.Contains("home-tile-caption")), Is.False,
              "a plugin's name stands on the fade a cover's title stands on; the reference lays a flat band there");
        }

        // A cover beside the plaques is untouched by any of this, and the dresses really are apart: every tile's
        // markup carries all three, so what proves them apart is that the dress a tile does not wear stays down.
        foreach (Button cover in covers)
        {
          Assert.That(OnScreen(BranchOf(cover, "nereus-plugin-tile")), Is.False,
              "a cover's tile put up a plugin's plaque as well, so the two dresses are not apart after all");
          Assert.That(cover.GetVisualDescendants().OfType<Border>()
              .Any(face => face.Classes.Contains("home-tile-caption")), Is.True,
              "and the cover beside the plaques lost the strip its own title stands on");
        }
      });
    }

    /// <summary>
    /// The picture a plaque wears is the plugin's own, asked for by the name the reference used - the action's Id
    /// under <c>HomeMenu</c> - and it is stretched to the square rather than cropped, because an emblem loses a
    /// wing when it is cropped while a little slack at its sides asks nothing of the viewer.
    /// </summary>
    [AvaloniaTest]
    public void APlaqueWearsItsOwnPictureStretched()
    {
      RunOnHome(delegate (Window window, List<Button> plaques, List<Button> covers)
      {
        Assert.That(plaques, Is.Not.Empty, "the wall put up no plugin's plaque at all");

        foreach (Button plaque in plaques)
        {
          Image[] art = plaque.GetVisualDescendants().OfType<Image>()
              .Where(piece => piece.Classes.Contains("plugin-tile-art"))
              .ToArray();
          Assert.That(art, Has.Length.EqualTo(1), "a plugin's square wears no picture of its own");
          Assert.That(art[0].Stretch, Is.EqualTo(Stretch.Fill),
              "a plaque's picture is not stretched over its ground, so the emblem is cropped instead");
        }
      });
    }

    /// <summary>
    /// The home screen with a band of leftover plugins on it, shown, and the two kinds of square handed over: the
    /// plaques and the covers beside them. Taken out of a shown window rather than built by hand, because the
    /// bargain is that the <i>screen</i> wears the skin - a template that quietly went back to the cover's markup
    /// dresses nothing until a window is up.
    /// </summary>
    private static void RunOnHome(Action<Window, List<Button>, List<Button>> assert)
    {
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      ShellWindow? window = null;
      try
      {
        SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
        Assert.That(FrontendTheme.Apply(app, "nereus", out string? themeProblem), Is.EqualTo("nereus"),
            themeProblem ?? "");
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        HomeViewModel home = new HomeViewModel();
        MainMenuEntry entry = home.DisplayMenu.Single(item => item.HomeImageId == MOVIES_ACTION);
        home.SelectedEntry = entry;
        home.ShowContentAsync(entry).GetAwaiter().GetResult();

        // Without a server to ask, no shelf answered with titles, so the wall is handed the squares the real
        // screen builds when the plugins are counted out and the covers arrive.
        HomeContentGroup plaques = new HomeContentGroup(HomeViewModel.OTHER_PLUGINS_HEADING,
            HomeContentGroup.PLUGINS_LAYOUT, 1500, 6);
        for (int i = 0; i < 6; i++)
        {
          plaques.AddMenuEntry(new MainMenuEntry(null, null, "Plugin " + (i + 1), Array.Empty<string>(),
              "opens the plugin", null, homeImageId: Guid.NewGuid().ToString("D")));
        }

        HomeContentGroup covers = new HomeContentGroup("Latest added", "poster1", 1500, 2);
        covers.AddTitle(new TitleViewModel(
            new MediaTitle(Guid.NewGuid(), "Alien", null, 1979, null, null, false)));
        covers.AddTitle(new TitleViewModel(
            new MediaTitle(Guid.NewGuid(), "Blade Runner", null, 1982, null, null, false)));

        home.ContentGroups.Add(plaques);
        home.ContentGroups.Add(covers);

        window = new ShellWindow(new HomeView(home, new WorkflowManager(),
            sectionScreen: delegate (HomeSection section) { return null!; }));
        window.Show();
        Pump();

        ScrollViewer content = window.GetVisualDescendants().OfType<ScrollViewer>()
            .Single(viewer => viewer.Name == "nereusContentPane");

        // Every tile's markup carries all three dresses and shows the one it is, so a dress is only worn by a tile
        // that is on the screen - a tile standing in a band whose dress is hidden is not wearing it.
        List<Button> dressed = BranchButtons(content, "nereus-plugin-tile").Where(OnScreen).ToList();
        List<Button> plates = BranchButtons(content, "nereus-content-tile")
            .Where(button => ((HomeContentTile) button.Tag!).IsTitle && OnScreen(button))
            .OrderBy(button => ((HomeContentTile) button.Tag!).X)
            .ToList();

        Assert.That(dressed, Has.Count.EqualTo(6),
            "six plugins were put on the wall and " + dressed.Count + " came back dressed as plaques; groups: "
            + string.Join(" | ", home.ContentGroups.Select(group =>
                group.Heading + "/" + group.Layout + "=" + group.Tiles.Count)));

        assert(window, dressed, plates);
      }
      finally
      {
        window?.Close();
        SkinEngine.Clear(app);
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out _);
      }
    }

    /// <summary>
    /// The buttons one dress is made of, wherever they stand in the pane. A tile's markup carries every dress the
    /// wall knows and shows the one its square is, so this finds the dresses a tile holds rather than the one it
    /// wears - <see cref="OnScreen"/> answers which.
    /// </summary>
    private static IEnumerable<Button> BranchButtons(Visual pane, string className)
    {
      return pane.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains(className));
    }

    /// <summary>The button the same tile's markup holds under another dress, or null when it holds none.</summary>
    private static Button? BranchOf(Button tile, string className)
    {
      Visual? branch = tile.GetVisualParent()?.GetVisualParent();
      return branch?.GetVisualDescendants().OfType<Button>()
          .FirstOrDefault(button => button.Classes.Contains(className));
    }

    /// <summary>
    /// Whether a control is really on the screen: <c>IsVisible</c> is a control's own answer and is not handed
    /// down, so a control standing in a branch its tile turned off still says it is visible.
    /// </summary>
    private static bool OnScreen(Visual? control)
    {
      for (Visual? node = control; node != null; node = node.GetVisualParent())
      {
        if (!node.IsVisible)
          return false;
      }

      return true;
    }

    private static void Pump()
    {
      for (int round = 0; round < 20; round++)
        Dispatcher.UIThread.RunJobs();
    }

    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestTheLeftoverPluginsStandOnPlaques).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      string path = Path.GetFullPath(value!.Value!);
      Assert.That(Directory.Exists(path), Is.True, name + " points at '" + path + "', which is not a directory");
      return path;
    }

    /// <summary>The skin this front end ships, read from the source folder rather than from a build's copy.</summary>
    private static WornSkin WearTheShippedSkin()
    {
      Application app = Application.Current!;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");

      FrontendTheme.Apply(app, "nereus", out _);
      Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");

      WornSkin? worn = SkinEngine.Worn(app);
      Assert.That(worn, Is.Not.Null, "the shipped skin came off the disk and onto the application");
      return worn!;
    }
  }
}
