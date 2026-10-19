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
using System.Linq;
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
  /// The settings hub: the list of places a household configures, the screen each one opens on the right,
  /// and the section that does not embed a screen but walks the workflow to a screen of its own.
  /// </summary>
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
      model.Add("Server address", "Frontend", "the connection screen", null, delegate { openings[0]++; });
      return model;
    }

    [AvaloniaTest]
    public void AHubOpensItsFirstSectionRatherThanAnEmptyRight()
    {
      SettingsViewModel model = Model(new int[1]);
      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Show();
      Pump();

      Assert.That(model.CurrentView, Is.Not.Null, "the hub shows something before anybody picks");
      Label? shown = window.GetVisualDescendants().OfType<Label>().FirstOrDefault(l => l.Name == "appearance");
      Assert.That(shown, Is.Not.Null, "and what it shows is the first section's screen");

      window.Close();
    }

    [AvaloniaTest]
    public void PickingAnotherSectionSwapsTheScreenOnTheRight()
    {
      SettingsViewModel model = Model(new int[1]);
      ShellWindow window = new ShellWindow(new SettingsView(model));
      window.Show();
      Pump();

      model.Selected = model.Sections[1];
      Pump();

      Assert.That(model.CurrentView, Is.Not.Null);
      Assert.That(((Label)model.CurrentView!).Name, Is.EqualTo("sources"));
      Assert.That(model.Status, Is.Empty, "a section that opened has nothing to complain about");

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

      Assert.That(model.CurrentView, Is.Null);
      Assert.That(model.Status, Does.Contain("Playback"), "the screen names what is missing rather than going blank");

      window.Close();
    }
  }
}