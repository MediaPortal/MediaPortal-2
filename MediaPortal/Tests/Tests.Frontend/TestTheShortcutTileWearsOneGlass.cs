/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
    General Public License for more details.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LiquidGlassAvaloniaUI;
using NUnit.Framework;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// The shortcut tiles of the home wall and the one glass they wear: liquid glass, and nothing under it or
  /// beside it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The tile wore a flat frost until the lens came, and the pair was briefly switchable; what is worth holding
  /// on its own is the pairing rule - exactly one glass per tile, because a tile frosted and lensed at once is
  /// two glasses nobody chose - and the size that glass paints at.
  /// </para>
  /// <para>
  /// The size is not a detail: a liquid glass surface is laid out to breathe, its statics switch ClipToBounds off
  /// so its own shadow can leave the control, and a rail of these standing twelve pixels apart then paints one
  /// plate over the whole rail, rings and gaps alike. That is what the first live run showed, and a screenshot
  /// was the only witness - so the rectangle is asserted here, where forgetting the clip is caught before a
  /// television shows it.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTheShortcutTileWearsOneGlass
  {
    private const string AUDIO_ACTION = "30715d73-4205-417f-80aa-e82f0834171f";

    /// <summary>
    /// The window the harness is standing in, kept so that a glass can be measured against it.
    /// </summary>
    private ShellWindow? _screen;

    /// <summary>
    /// Every shortcut tile on the wall wears the lens, wears it alone, and wears it exactly the size of the
    /// tile - not the tile and its neighbour as well.
    /// </summary>
    [AvaloniaTest]
    public void EveryShortcutTileWearsItsGlassAndNothingBesideIt()
    {
      OnShortcutTiles(delegate (IEnumerable<ShortcutGlass> tiles)
      {
        foreach (ShortcutGlass tile in tiles)
        {
          Assert.That(tile.Glass.IsEffectivelyVisible, Is.True,
              "the shortcut tile stands without its glass");
          // The frost is gone, and a guard is the only thing keeping a second surface from quietly coming back
          // into the template - it was found by the class the skin dresses it in, so it is still found that way.
          Assert.That(tile.Tile.GetVisualDescendants().OfType<Control>()
              .Count(control => control.Classes.Contains("home-shortcut-frost")), Is.EqualTo(0),
              "a second glass is laid under the lens again, so the tile is frosted and lensed at once");
          // The clip is asserted rather than the painted area: a liquid glass surface overflows its bounds
          // while its bounds stay tile-sized, so no rectangle measured on the visual tree can see the plate
          // that the overflow paints over the rail. The first live run showed that plate on the television.
          Assert.That(tile.Glass.ClipToBounds, Is.True,
              "the liquid glass is free to paint past its tile; the ring and the twelve pixels between two "
              + "shortcuts are painted over as well, and the rail becomes one plate");
          AssertCoversTheTile(tile);
        }
      });
    }

    /// <summary>
    /// A shortcut tile and the glass standing in it.
    /// </summary>
    private sealed class ShortcutGlass
    {
      public Button Tile = null!;
      public LiquidGlassSurface Glass = null!;
    }

    /// <summary>
    /// The glass is the tile. Painted past its edge it covers the twelve pixels that separate one shortcut
    /// from the next, and a rail of them becomes one plate with neither ring nor gap left to be seen.
    /// </summary>
    private void AssertCoversTheTile(ShortcutGlass tile)
    {
      Control painted = tile.Glass;
      Rect onScreen = OnTheScreen(painted);
      Rect tileOnScreen = OnTheScreen(tile.Tile);

      Assert.That(onScreen.Width, Is.GreaterThan(0), "the glass has nothing to be drawn on");
      Assert.That(onScreen.Height, Is.GreaterThan(0), "the glass has nothing to be drawn on");
      Assert.That(onScreen.Width, Is.LessThanOrEqualTo(tileOnScreen.Width + 0.5),
          "the glass is " + onScreen.Width.ToString("0.#") + " wide on a tile "
          + tileOnScreen.Width.ToString("0.#") + " wide, so it paints over its neighbour as well");
      Assert.That(onScreen.Height, Is.LessThanOrEqualTo(tileOnScreen.Height + 0.5),
          "the glass is " + onScreen.Height.ToString("0.#") + " tall on a tile "
          + tileOnScreen.Height.ToString("0.#") + " tall, so it paints over its neighbour as well");
      Assert.That(onScreen.X, Is.GreaterThanOrEqualTo(tileOnScreen.X - 0.5), "the glass starts off its tile");
      Assert.That(onScreen.Y, Is.GreaterThanOrEqualTo(tileOnScreen.Y - 0.5), "the glass starts off its tile");
      Assert.That(onScreen.Right, Is.LessThanOrEqualTo(tileOnScreen.Right + 0.5), "the glass ends off its tile");
      Assert.That(onScreen.Bottom, Is.LessThanOrEqualTo(tileOnScreen.Bottom + 0.5), "the glass ends off its tile");
      // And it is the tile's glass rather than a chip in one corner of it; the padding a Button keeps for
      // itself is the only leave-taking allowed.
      Assert.That(onScreen.Width, Is.GreaterThanOrEqualTo(tileOnScreen.Width * 0.8),
          "the glass covers a part of its tile only");
      Assert.That(onScreen.Height, Is.GreaterThanOrEqualTo(tileOnScreen.Height * 0.8),
          "the glass covers a part of its tile only");
    }

    /// <summary>
    /// Where a visual stands on the window. Both the tile and its glass are inside the wall's own scale, so
    /// measuring them against the window measures them in the same measure as each other.
    /// </summary>
    private Rect OnTheScreen(Visual visual)
    {
      Assert.That(_screen, Is.Not.Null, "the glass is being measured while its window is gone");
      Window screen = _screen!;
      Point? top = visual.TranslatePoint(new Point(), screen);
      Point? bottom = visual.TranslatePoint(new Point(visual.Bounds.Width, visual.Bounds.Height), screen);
      Assert.That(top, Is.Not.Null, "the glass has been laid out");
      Assert.That(bottom, Is.Not.Null, "the glass has been laid out");
      return new Rect(top!.Value, new Size(bottom!.Value.X - top.Value.X, bottom!.Value.Y - top.Value.Y));
    }

    /// <summary>
    /// The home as the wake leaves it - the Shortcuts band is read before a server is ever asked, so this needs
    /// no server - with the answer the test is about written into an otherwise empty skin-settings folder.
    /// </summary>
    private void OnShortcutTiles(Action<IEnumerable<ShortcutGlass>> assert)
    {
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      string? previousSettingsDir = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR");
      string ownDir = Path.Combine(Path.GetTempPath(), "mp2-glass-" + Guid.NewGuid().ToString("N"));
      string? previousScreens = Environment.GetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE");
      ShellWindow? window = null;
      try
      {
        Directory.CreateDirectory(ownDir);
        Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", ownDir);
        Environment.SetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE", Path.Combine(ownDir, "screens.json"));
        File.WriteAllText(Path.Combine(ownDir, "skin-nereus.json"),
            JsonSerializer.Serialize(new Dictionary<string, object>
            {
              { "SkinName", "nereus" },
              { "lastSelectedHomeMenuActionId", AUDIO_ACTION }
            }));

        SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
        Assert.That(FrontendTheme.Apply(app, "nereus", out string? themeProblem), Is.EqualTo("nereus"),
            themeProblem ?? "");
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        HomeViewModel home = new HomeViewModel();
        MainMenuEntry entry = home.DisplayMenu.Single(item => item.HomeImageId == AUDIO_ACTION);
        home.SelectedEntry = entry;
        home.ShowContentAsync(entry).GetAwaiter().GetResult();
        Pump();

        window = new ShellWindow(new HomeView(home, new WorkflowManager(),
            sectionScreen: delegate (HomeSection section) { return null!; }));
        window.Width = 1920;
        window.Height = 1080;
        window.Show();
        Pump();
        _screen = window;

        List<ShortcutGlass> tiles = ShortcutTilesIn(window);
        Assert.That(tiles, Is.Not.Empty,
            "the home wall shows no shortcut tile at all, so there is no glass here to test");
        assert(tiles);
      }
      finally
      {
        window?.Close();
        _screen = null;
        Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", previousSettingsDir);
        Environment.SetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE", previousScreens);
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out string? _);
        try
        {
          Directory.Delete(ownDir, true);
        }
        catch (IOException)
        {
        }
      }
    }

    /// <summary>Every shortcut tile of the wall, with the glass that stands in it.</summary>
    private static List<ShortcutGlass> ShortcutTilesIn(ShellWindow window)
    {
      List<ShortcutGlass> found = new List<ShortcutGlass>();
      foreach (Button tile in window.GetVisualDescendants().OfType<Button>()
          .Where(button => button.Classes.Contains("nereus-shortcut-tile")))
      {
        found.Add(new ShortcutGlass
        {
          Tile = tile,
          Glass = tile.GetVisualDescendants().OfType<LiquidGlassSurface>().Single()
        });
      }
      return found;
    }

    private static void Pump()
    {
      for (int round = 0; round < 10; round++)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(10);
      }
      Dispatcher.UIThread.RunJobs();
    }

    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestTheShortcutTileWearsOneGlass).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));
      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");
      return Path.GetFullPath(value!.Value!);
    }
  }
}
