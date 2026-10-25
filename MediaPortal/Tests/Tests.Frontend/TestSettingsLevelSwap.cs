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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// What a level change leaves behind in the list showing it.
  /// </summary>
  /// <remarks>
  /// The hub walked into a wall of plates: the viewer pressed the last plate of nine, the door on it answered
  /// with seven pages, and the next press on one of those seven killed the client. Windows wrote the reason
  /// into its own log: <c>ArgumentOutOfRangeException: Index was out of range (Parameter 'index')</c> inside
  /// <c>Avalonia.Controls.Selection.SelectedItems.GetEnumerator()</c>, reached from
  /// <c>SelectingItemsControl.UpdateSelection</c> from <c>ListBoxItem.OnPointerPressed</c>. The list had kept a
  /// number from the level the viewer had just left - eight - and reached for it in a level that had never had
  /// more than seven places on it. A number a control remembers is ours to drop when the level under it
  /// changes, because nothing an Avalonia release does about it can be waited for.
  /// </remarks>
  [TestFixture]
  public class TestSettingsLevelSwap
  {
    private static void Pump()
    {
      for (int round = 0; round < 20; round++)
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// A hub of nine plates whose last plate is a door over seven pages - the numbers the reference's own
    /// first level and its Appearance branch have, and the shape the crash was measured on.
    /// </summary>
    private static SettingsViewModel NineAndSeven()
    {
      SettingsViewModel model = new SettingsViewModel();
      for (int place = 0; place < 8; place++)
      {
        string name = "Ort " + place;
        model.Add(name, "Frontend", "eine Seite",
            delegate { return new Label { Name = "seite-" + name, Content = name }; });
      }

      model.AddFolder("Neunter Ort", "Server", "sieben Seiten", delegate
      {
        var pages = new List<SettingsSection>();
        for (int page = 0; page < 7; page++)
        {
          string name = "Seite " + page;
          pages.Add(new SettingsSection(name, "Server", "eine Antwort",
              delegate { return new Label { Name = "antwort-" + name, Content = name }; }));
        }
        return Task.FromResult<IReadOnlyList<SettingsSection>>(pages);
      });

      return model;
    }

    private static ListBox Wall(ShellWindow window) =>
        window.GetVisualDescendants().OfType<ListBox>().Single(l => l.Classes.Contains("settings-tile-wall"));

    /// <summary>The plate the wall shows at one place, as the control a pointer lands on.</summary>
    private static ListBoxItem Plate(ShellWindow window, int index) =>
        Wall(window).GetVisualDescendants().OfType<ListBoxItem>().ElementAt(index);

    /// <summary>
    /// Handing a plate a press the way the window does it - moved to, pressed, let go - rather than the model's
    /// <c>Selected</c>, which never touches a container and therefore never lets a container keep a number it
    /// should have forgotten.
    /// </summary>
    private static void Press(ShellWindow window, ListBoxItem plate)
    {
      Point? at = plate.TranslatePoint(new Point(plate.Bounds.Width / 2, plate.Bounds.Height / 2), window);
      Assert.That(at, Is.Not.Null, "the plate has a place on the window to press");
      window.MouseMove(at.Value);
      window.MouseDown(at.Value, MouseButton.Left, RawInputModifiers.None);
      window.MouseUp(at.Value, MouseButton.Left, RawInputModifiers.None);
    }

    [AvaloniaTest]
    public void PressingAPlateOfTheLevelAfterADoorReachesForNothingOfTheLevelBefore()
    {
      SettingsViewModel model = NineAndSeven();
      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Width = 1920;
      window.Height = 1080;
      window.Show();
      Pump();

      Assert.That(Plate(window, 8), Is.Not.Null, "nine plates stand on the first level");
      Press(window, Plate(window, 8)); // the door, last of the nine: eight is the number the list will remember
      Pump();

      Assert.That(model.Sections.Count, Is.EqualTo(7),
          "the door answered with its seven pages, which is the level the viewer is looking at now");

      Assert.DoesNotThrow(delegate
      {
        Press(window, Plate(window, 0));
        Pump();
      }, "a press on a plate of the smaller level may not reach for the place the level before had");

      Assert.That(model.CurrentView, Is.Not.Null, "and the press did what a press is for: the page opened");

      window.Close();
    }

    /// <summary>
    /// The press of the door itself, and not only the press after it. A door whose pages are already known
    /// answers while the list is still committing its press, which is the moment the real window died on
    /// (<c>ArgumentOutOfRangeException</c> out of <c>SelectedItems.GetEnumerator</c>); the hub answers a
    /// dispatcher step later for that reason. A headless window does not reproduce the death - only the real
    /// backend enumerates a selection mid-commit - so what is held here is the behaviour the deferral must
    /// keep: the door's pages do arrive, and the press that opened them is not lost.
    /// </summary>
    [AvaloniaTest]
    public void PressingTheDoorItselfSurvivesTheLevelItHandsOver()
    {
      SettingsViewModel model = NineAndSeven();
      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Width = 1920;
      window.Height = 1080;
      window.Show();
      Pump();

      ListBoxItem door = Plate(window, 8);
      Assert.DoesNotThrow(delegate
      {
        Press(window, door);
        Pump();
      }, "a door that answers at once may not change the list under the press that opened it");

      Assert.That(model.Sections.Count, Is.EqualTo(7), "and the answer arrived all the same");

      window.Close();
    }

    /// <summary>
    /// The one page of a door is opened instead of being stood in front of, so the wall of a single plate never
    /// appears - which also means the press that walked through the door left the <i>outer</i> level on screen.
    /// </summary>
    [AvaloniaTest]
    public void PressingADoorOfOnePageOpensItsPageAndKeepsTheDoorsOnScreen()
    {
      SettingsViewModel model = new SettingsViewModel();
      model.AddFolder("Appearance", "Frontend", "colours", delegate
      {
        IReadOnlyList<SettingsSection> one = new List<SettingsSection>
        {
          new SettingsSection("Television", "Frontend", "the set",
              delegate { return new Label { Name = "tv", Content = "tv" }; })
        };
        return Task.FromResult(one);
      });
      model.Add("Close", "Frontend", "out", delegate { return new Label { Name = "close", Content = "out" }; });

      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Width = 1920;
      window.Height = 1080;
      window.Show();
      Pump();

      ListBoxItem door = Plate(window, 0);
      Assert.DoesNotThrow(delegate
      {
        Press(window, door);
        Pump();
      }, "walking through a door of one page is a level change too, as far as the list is concerned");

      Assert.That(((Label) model.CurrentView!).Name, Is.EqualTo("tv"));
      Assert.That(Plate(window, 1).Content, Is.InstanceOf<SettingsSection>());
      Assert.That(model.Sections.Select(s => s.Name), Is.EqualTo(new[] { "Appearance", "Close" }));

      window.Close();
    }

    [AvaloniaTest]
    public void WalkingBackOutOfADoorAndPressingAgainHoldsToo()
    {
      SettingsViewModel model = NineAndSeven();
      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Width = 1920;
      window.Height = 1080;
      window.Show();
      Pump();

      Press(window, Plate(window, 8));
      Pump();
      Assert.That(model.Sections.Count, Is.EqualTo(7));

      model.GoBack(); // out of the door, back onto the nine plates
      Pump();
      Assert.That(model.Sections.Count, Is.EqualTo(9));

      Assert.DoesNotThrow(delegate
      {
        Press(window, Plate(window, 0));
        Pump();
      }, "the level the viewer came back to has its own places, and the door's pages are not among them");

      window.Close();
    }
  }
}
