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
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// The settings hub: the wall of places a household configures, the screen a place opens beside the narrowed
  /// list, the door that opens a wall of pages of its own, and the way back out of all of it.
  /// </summary>
  /// <remarks>
  /// The shapes are the reference's. Its configuration screen carried one list whose style turned on whether
  /// anything was open - a wrap of plates while nothing was (<c>master_configuration_menu.xaml:143-147</c>), the
  /// narrowed list with the open section beside it once something was - and a place with pages underneath,
  /// <c>/Configuration/Settings/Media</c> above its eight pages, opened the wall of those pages rather than a
  /// screen. What follows holds both shapes and the walking between them.
  /// </remarks>
  [TestFixture]
  public class TestSettings
  {
    private static void Pump()
    {
      for (int round = 0; round < 20; round++)
        Dispatcher.UIThread.RunJobs();
    }

    private static SettingsViewModel Model(int[] openings)
    {
      SettingsViewModel model = new SettingsViewModel();
      model.Add("Appearance", "Frontend", "colours", delegate { return new Label { Name = "appearance", Content = "colours" }; });
      model.Add("Sources", "Server", "folders", delegate { return new Label { Name = "sources", Content = "folders" }; });
      model.Add("Server address", "Frontend", "the connection screen", null, open: delegate { openings[0]++; });
      return model;
    }

    /// <summary>
    /// A hub of two levels: the top one an appearance screen and a door, the door standing over two pages of
    /// its own. <c>asked</c> counts how often the door was asked for its pages.
    /// </summary>
    /// <remarks>
    /// Two pages rather than one, because a door with a single page is not a level at all - see
    /// <see cref="ADoorWithOnePageIsWalkedThroughAndThatPageOpens"/> - and what is being walked here is the
    /// wall of plates a door stands in front of.
    /// </remarks>
    private static SettingsViewModel TwoLevels(int[] asked)
    {
      SettingsViewModel model = new SettingsViewModel();
      model.Add("Appearance", "Frontend", "colours",
          delegate { return new Label { Name = "appearance", Content = "colours" }; });
      model.AddFolder("Media", "Server", "the library's own answers", delegate
      {
        asked[0]++;
        IReadOnlyList<SettingsSection> pages = new List<SettingsSection>
        {
          new SettingsSection("Pictures", "Server", "how many a title keeps",
              delegate { return new Label { Name = "pictures", Content = "pictures" }; }),
          new SettingsSection("Fanart", "Server", "which pictures a title keeps",
              delegate { return new Label { Name = "fanart", Content = "fanart" }; })
        };
        return Task.FromResult(pages);
      });
      return model;
    }

    /// <summary>
    /// The same hub, but the door holds exactly one page - the shape that is walked through rather than stood
    /// in front of. <c>asked</c> counts the times it was asked for its pages.
    /// </summary>
    private static SettingsViewModel OnePageDoor(int[] asked)
    {
      SettingsViewModel model = new SettingsViewModel();
      model.Add("Appearance", "Frontend", "colours",
          delegate { return new Label { Name = "appearance", Content = "colours" }; });
      model.AddFolder("TV", "TV", "where the channels come from", delegate
      {
        asked[0]++;
        IReadOnlyList<SettingsSection> pages = new List<SettingsSection>
        {
          new SettingsSection("TV", "TV", "the television backend",
              delegate { return new Label { Name = "tv", Content = "tv" }; })
        };
        return Task.FromResult(pages);
      });
      return model;
    }

    // ---------------------------------------------------------------------------------------------
    // Arriving: a wall of plates, which is what the reference showed and what a screen with nothing open is.
    // ---------------------------------------------------------------------------------------------

    [AvaloniaTest]
    public void TheHubArrivesAsAWallOfPlates()
    {
      SettingsViewModel model = Model(new int[1]);
      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Show();
      Pump();

      Assert.That(model.IsWall, Is.True, "nothing is open, so the places stand as plates");
      Assert.That(model.CurrentView, Is.Null, "and no plate was opened before anybody picked one");
      Assert.That(model.CanGoBack, Is.False, "at the first level, with nothing open, back has nothing to do here");

      Assert.That(Plates(window), Is.EqualTo(new[] { "Appearance", "Sources", "Server address" }),
          "the wall is the places themselves, in the order the application filed them");
      Assert.That(model.Location, Is.EqualTo("Settings"), "and the heading says where the viewer is");

      window.Close();
    }

    /// <summary>The names the wall of the screen shows, in the order it shows them.</summary>
    private static string[] Plates(ShellWindow window)
    {
      return window.GetVisualDescendants().OfType<TextBlock>()
          .Where(t => t.Classes.Contains("settings-tile-name"))
          .Select(t => t.Text)
          .ToArray();
    }

    [AvaloniaTest]
    public void PickingAPlateOpensItsScreenBesideTheNarrowedList()
    {
      SettingsViewModel model = Model(new int[1]);
      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Show();
      Pump();

      model.Selected = model.Sections[1];
      Pump();

      Assert.That(model.CurrentView, Is.Not.Null);
      Assert.That(((Label)model.CurrentView!).Name, Is.EqualTo("sources"));
      Assert.That(model.IsWall, Is.False, "with a screen open the plates narrow to a list");
      Assert.That(model.Status, Is.Empty, "a section that opened has nothing to complain about");

      Label? shown = window.GetVisualDescendants().OfType<Label>().FirstOrDefault(l => l.Name == "sources");
      Assert.That(shown, Is.Not.Null, "and the screen is on the window, not merely in the model");

      window.Close();
    }

    [AvaloniaTest]
    public void ASectionThatNavigatesHandsThePickOverAndLeavesTheHubShowing()
    {
      int[] openings = new int[1];
      SettingsViewModel model = Model(openings);
      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Show();
      Pump();

      model.Selected = model.Sections[0];
      Pump();
      Control? before = model.CurrentView;

      model.Selected = model.Sections[2];
      Pump();

      Assert.That(openings[0], Is.EqualTo(1), "the section asked somebody else to open its screen");
      Assert.That(model.CurrentView, Is.SameAs(before),
          "and the hub keeps showing where the back key will land the viewer again");

      window.Close();
    }

    [AvaloniaTest]
    public void ASectionWhoseScreenIsNotThereSaysSoInsteadOfShowingNothing()
    {
      SettingsViewModel model = new SettingsViewModel();
      model.Add("Playback", "Frontend", "sound", delegate { return null; });
      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Show();
      Pump();

      model.Selected = model.Sections[0];
      Pump();

      Assert.That(model.CurrentView, Is.Null);
      Assert.That(model.Status, Does.Contain("Playback"), "the screen names what is missing rather than going blank");

      window.Close();
    }

    // ---------------------------------------------------------------------------------------------
    // Doors: a place with pages underneath opens the wall of its pages, the way the reference's Media node did.
    // ---------------------------------------------------------------------------------------------

    [AvaloniaTest]
    public void ADoorIsNotOpenedBeforeAnybodyAsksForIt()
    {
      int[] asked = new int[1];
      SettingsViewModel model = TwoLevels(asked);
      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Show();
      Pump();

      Assert.That(model.Sections.Count, Is.EqualTo(2), "the wall of the first level stands on the screen");
      Assert.That(asked[0], Is.EqualTo(0),
          "a door whose pages were read before anybody picked it is a door that was walked through on its own");

      window.Close();
    }

    [AvaloniaTest]
    public void PickingADoorOpensItsPagesAsTheLevelTheViewerStandsOn()
    {
      int[] asked = new int[1];
      SettingsViewModel model = TwoLevels(asked);
      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Show();
      Pump();

      model.Selected = model.Sections[1];
      Pump();

      Assert.That(asked[0], Is.EqualTo(1), "the door asked whose pages it has when it was picked");
      Assert.That(model.Sections.Select(s => s.Name), Is.EqualTo(new[] { "Pictures", "Fanart" }),
          "and the pages are what the viewer now stands in front of");
      Assert.That(model.Location, Is.EqualTo("Settings › Media"),
          "the heading says where they are, the way the reference printed its path above the list");
      Assert.That(model.IsWall, Is.True, "a door has no screen of its own, so its level is a wall again");
      Assert.That(model.Status, Does.Contain("2 page(s)"), "and the wall says what it opened");
      Assert.That(Plates(window), Is.EqualTo(new[] { "Pictures", "Fanart" }));

      window.Close();
    }

    [AvaloniaTest]
    public void APageBehindADoorOpensItsScreenBesideTheNarrowedList()
    {
      SettingsViewModel model = TwoLevels(new int[1]);
      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Show();
      Pump();

      model.Selected = model.Sections[1];
      Pump();
      model.Selected = model.Sections[0];
      Pump();

      Assert.That(model.CurrentView, Is.Not.Null);
      Assert.That(((Label)model.CurrentView!).Name, Is.EqualTo("pictures"));
      Assert.That(model.IsWall, Is.False, "with a page open the list narrows and the screen stands beside it");
      Assert.That(model.CanGoBack, Is.True);

      Label? shown = window.GetVisualDescendants().OfType<Label>().FirstOrDefault(l => l.Name == "pictures");
      Assert.That(shown, Is.Not.Null, "and the page is on the screen, not merely in the model");

      window.Close();
    }

    [AvaloniaTest]
    public void BackGoesOutOfAScreenIntoItsWallAndOutOfTheWallIntoTheLevelAbove()
    {
      SettingsViewModel model = TwoLevels(new int[1]);
      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Show();
      Pump();

      model.Selected = model.Sections[1];
      Pump();
      model.Selected = model.Sections[0];
      Pump();

      Assert.That(model.GoBack(), Is.True, "an open screen folds away first");
      Assert.That(model.IsWall, Is.True);
      Assert.That(model.Sections.Select(s => s.Name), Is.EqualTo(new[] { "Pictures", "Fanart" }),
          "and what stands where the screen was is the wall of the door it came from");

      Assert.That(model.GoBack(), Is.True, "the wall of a door goes up one level next");
      Assert.That(model.Sections.Select(s => s.Name), Is.EqualTo(new[] { "Appearance", "Media" }),
          "the level the viewer walked out of, with the same entries they left it with");
      Assert.That(model.Location, Is.EqualTo("Settings"));

      Assert.That(model.GoBack(), Is.False,
          "and at the first level the hub has nowhere left to go, which is the shell's cue to leave");

      window.Close();
    }

    /// <summary>
    /// The reference's rule for a screen with one item on it: the screen is skipped and the item's own action
    /// runs instead (<c>AbstractScreenData.SkipForSingleItem</c>, worked at
    /// <c>MediaNavigationModel.cs:470</c>), because a wall of a single plate asks a viewer to count to one.
    /// </summary>
    [AvaloniaTest]
    public void ADoorWithOnePageIsWalkedThroughAndThatPageOpens()
    {
      int[] asked = new int[1];
      SettingsViewModel model = OnePageDoor(asked);
      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Show();
      Pump();

      model.Selected = model.Sections[1];
      Pump();

      Assert.That(asked[0], Is.EqualTo(1), "the door was asked, as every door is");
      Assert.That(model.CurrentView, Is.Not.Null, "and the page underneath it opened on the same press");
      Assert.That(((Label)model.CurrentView!).Name, Is.EqualTo("tv"), "the page, not the door");
      Assert.That(model.IsWall, Is.False, "which means the hub is showing a screen, not a wall of one plate");
      Assert.That(Plates(window), Is.EquivalentTo(new[] { "Appearance", "TV" }),
          "what stands behind the opened screen is the wall of doors the viewer pressed in, never a wall of one");
      Assert.That(model.Sections.Select(s => s.Name), Is.EqualTo(new[] { "Appearance", "TV" }),
          "the level behind the screen is the level the door stood on, not a level of one");
      Assert.That(model.Location, Is.EqualTo("Settings"),
          "and the path says so too - the skipped door is not a place anybody walked into");

      Assert.That(model.GoBack(), Is.True, "the back key folds the screen away");
      Assert.That(model.IsWall, Is.True, "and the viewer stands again before the doors they came from");
      Assert.That(model.GoBack(), Is.False, "because the skipped door never added a level to walk back out of");

      window.Close();
    }

    [AvaloniaTest]
    public void AChainOfDoorsWithOnePageEachIsWalkedThroughToItsLeaf()
    {
      // The reference's own shape in a few places - /Appearance/Skin holds one group, SkinAndTheme, and that
      // group holds the settings - and a rule that stops at the first door would leave a wall of one plate
      // two steps in.
      SettingsViewModel model = new SettingsViewModel();
      model.AddFolder("Appearance", "Frontend", "colours", delegate
      {
        IReadOnlyList<SettingsSection> skin = new List<SettingsSection>
        {
          new SettingsSection("Skin", "Frontend", "which skin", delegate
          {
            IReadOnlyList<SettingsSection> theme = new List<SettingsSection>
            {
              new SettingsSection("Skin and theme", "Frontend", "the worn skin and its colours",
                  delegate { return new Label { Name = "theme", Content = "theme" }; })
            };
            return Task.FromResult(theme);
          })
        };
        return Task.FromResult(skin);
      });

      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Show();
      Pump();

      model.Selected = model.Sections[0];
      Pump();

      Assert.That(((Label)model.CurrentView!).Name, Is.EqualTo("theme"),
          "both single doors were walked through and the leaf is what stands on the screen");
      Assert.That(model.Location, Is.EqualTo("Settings"), "neither of them became a level of one plate");

      Assert.That(model.GoBack(), Is.True);
      Assert.That(model.GoBack(), Is.False, "one back key is all it takes to be out of all of it again");

      window.Close();
    }

    [AvaloniaTest]
    public void ADoorThatCannotAnswerLeavesTheViewerWhereTheyStood()
    {
      SettingsViewModel model = new SettingsViewModel();
      model.Add("Appearance", "Frontend", "colours",
          delegate { return new Label { Name = "appearance", Content = "colours" }; });
      model.AddFolder("Media", "Server", "the library's own answers",
          delegate { throw new InvalidOperationException("the server did not answer"); });
      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Show();
      Pump();

      model.Selected = model.Sections[1];
      Pump();

      Assert.That(model.Sections.Select(s => s.Name), Is.EqualTo(new[] { "Appearance", "Media" }),
          "a wall that did not get an answer is the wall that was there before");
      Assert.That(model.Status, Does.Contain("did not answer"), "and the reason is said where a viewer reads it");

      window.Close();
    }

    [AvaloniaTest]
    public void ADoorAnswersForThePlateThatWasPickedAndNotForAnOlderPick()
    {
      // A door whose pages arrive late, and a viewer who lost patience and picked the plate beside it.
      var slow = new TaskCompletionSource<IReadOnlyList<SettingsSection>>();
      SettingsViewModel model = new SettingsViewModel();
      model.AddFolder("Media", "Server", "the library's own answers", delegate { return slow.Task; });
      model.Add("Appearance", "Frontend", "colours",
          delegate { return new Label { Name = "appearance", Content = "colours" }; });

      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Show();
      Pump();

      model.Selected = model.Sections[0];
      Pump();
      model.Selected = model.Sections[1];
      Pump();

      slow.SetResult(new List<SettingsSection>
      {
        new SettingsSection("Late", "Server", "an answer that arrived after the viewer left",
            delegate { return new Label { Name = "late", Content = "late" }; })
      });
      Pump();

      Assert.That(model.Sections.Select(s => s.Name), Is.EqualTo(new[] { "Media", "Appearance" }),
          "the wall the late answer would have made is not put in front of a viewer who moved on");
      Assert.That(model.Location, Is.EqualTo("Settings"), "the door the viewer left was never walked through");
      Assert.That(((Label)model.CurrentView!).Name, Is.EqualTo("appearance"),
          "and the screen the viewer opened by hand stands untouched");

      Assert.That(model.GoBack(), Is.True, "back folds the screen the viewer opened");
      Assert.That(model.GoBack(), Is.False,
          "and there is no level above, because the late answer never moved the viewer anywhere");

      window.Close();
    }
  }
}