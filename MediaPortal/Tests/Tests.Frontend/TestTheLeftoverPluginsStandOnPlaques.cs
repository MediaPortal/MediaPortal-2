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
  /// it as a plaque. Its ground is the middle colour of the family the viewer chose and its own picture is
  /// stretched over that ground rather than cropped to it - but in its answer to a viewer it behaves exactly like
  /// the covers on the same wall: it brightens, its picture steps nearer, its edge lights, and its name stands in
  /// the very same fade a cover's title stands on rather than in a flat band of its own.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <see cref="TestHomeBandTakesItsRoom"/> holds the geometry of that band (seven columns, four rows, nineteen
  /// places). What this file holds is the dress: that a square the model says is a plugin is actually dressed as
  /// one on a shown screen, and that the fade its name stands on belongs to the worn face instead of to the view.
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
    /// A plaque keeps no numbers of its own any more - its name stands on the shared fade every cover's title
    /// stands on, so what the skin has to own is that fade: dark at the tile's edge, thinned to nothing above it.
    /// The head over a band is still spelled with the reference's SmallerFontSize.
    /// </summary>
    [AvaloniaTest]
    public void TheSkinOwnsTheFadeAPlaqueSharesWithTheCovers()
    {
      WearTheShippedSkin();

      IGradientBrush? fade = SkinEngine.Worn(Application.Current!)?.Resources["TitleFadeBrush"]
          as IGradientBrush;
      Assert.That(fade, Is.Not.Null,
          "the skin names no fade for a title to stand on, and a plaque's name stands on it now too");
      GradientBrush? stops = fade as GradientBrush;
      Assert.That(stops, Is.Not.Null, "the fade a title stands on has no stops to read");
      Assert.That(stops!.GradientStops.Any(s => s.Offset <= 0.01 && s.Color.A > 0xC0), Is.True,
          "the fade does not start dark at the tile's bottom edge, so a name would stand on nothing");
      Assert.That(stops.GradientStops.Any(s => s.Offset >= 0.99 && s.Color.A == 0), Is.True,
          "the fade does not thin to nothing above the name, so it would be a plate and not a fade");

      Assert.That(SkinEngine.WornNumber("HomeBandHeadingFontSize") ?? 0, Is.GreaterThanOrEqualTo(24),
          "the head over a band of the wall is written big enough to read across the room, as the reference's "
          + "SmallerFontSize is");
    }

    /// <summary>
    /// On the shown home screen a plugin's square is dressed as a plaque and not as a cover: it stands on the
    /// middle colour of the chosen family, it wears no glass and no second line - and beside that it answers a
    /// viewer exactly like a cover, which is what its button wearing the cover's class beside its own buys it:
    /// the same lift to full light, the same stepping picture, the same lit edge and the same fade under the
    /// name, written in the same size as a cover's title.
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

        TextBlock coverTitle = covers[0].GetVisualDescendants().OfType<TextBlock>()
            .Single(line => line.Classes.Contains("home-tile-caption-title"));

        foreach (Button plaque in plaques)
        {
          Assert.That(((plaque.Background as ISolidColorBrush)?.Color), Is.EqualTo(ground!.Color),
              "a plugin's plaque does not stand on the middle colour of the family the viewer chose, which is what "
              + "the reference called NereusColorMedium");

          // The cover's class beside its own is the whole of the plaque's answer to a viewer: the cover's styles
          // lift the tile to full light, step its picture and light its edge, and no plugin of its own dresses
          // any of that - so if the class were ever dropped, the focus frame would silently go with it.
          Assert.That(plaque.Classes.Contains("nereus-content-tile"), Is.True,
              "a plaque no longer wears the cover's class, so it stopped answering a viewer the way the covers do");

          TextBlock name = plaque.GetVisualDescendants().OfType<TextBlock>()
              .Single(line => line.Classes.Contains("home-tile-caption-title"));
          Assert.That(name.FontSize, Is.EqualTo(coverTitle.FontSize),
              "the name is not written in the size a cover's title is written in");
          Assert.That(name.Bounds.Height, Is.GreaterThan(0), "the name was never laid out");

          Border caption = plaque.GetVisualDescendants().OfType<Border>()
              .Single(face => face.Classes.Contains("home-tile-caption"));
          Assert.That(caption.Bounds.Height, Is.GreaterThanOrEqualTo(40),
              "the fade the name stands on did not come to the height the covers wear it at");
          Assert.That(plaque.GetVisualDescendants().OfType<TextBlock>().Count(), Is.EqualTo(1),
              "a plugin's square carries more than its name, and the reference shows nothing beside the name - "
              + "nineteen squares cannot hold a sentence");

          // The one dressing that belongs to a shortcut and to no plaque: the glass.
          Assert.That(plaque.GetVisualDescendants().OfType<LiquidGlassSurface>().Any(), Is.False,
              "a plaque wears no glass: it has a ground of the face's own colour, so a lens behind it would "
              + "sample the wall through a colour that was never meant to be see-through");
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
