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
  /// The fit of the settings wall. A level with more plates than the screen carries at full size shrinks
  /// the whole wall together - aspect locked - until every plate stands on the television; nobody is meant
  /// to scroll to see that a door exists. A level that fits keeps its full size and wears no transform.
  /// Only the tile views travel in the transform host; the page beside the narrowed list keeps its room.
  /// </summary>
  [TestFixture]
  public class TestSettingsWallFit
  {
    private static void Pump()
    {
      for (int round = 0; round < 20; round++)
        Dispatcher.UIThread.RunJobs();
    }

    private static SettingsViewModel Places(int count)
    {
      SettingsViewModel model = new SettingsViewModel();
      for (int place = 0; place < count; place++)
      {
        string name = "Ort " + place;
        model.Add(name, "Frontend", "eine Seite",
            delegate { return new Label { Name = "seite-" + name, Content = name }; });
      }

      return model;
    }

    private static ListBox Wall(ShellWindow window) =>
        window.GetVisualDescendants().OfType<ListBox>().Single(l => l.Classes.Contains("settings-tile-wall"));

    private static LayoutTransformControl Host(ShellWindow window)
    {
      LayoutTransformControl? host = Wall(window).Parent as LayoutTransformControl;
      Assert.That(host, Is.Not.Null, "the wall travels in a transform host");
      return host!;
    }

    [AvaloniaTest]
    public void ALevelWiderThanTheScreenStaysScaledIntoItWithoutScrolling()
    {
      SettingsViewModel model = Places(11);
      SettingsView hub = new SettingsView(model);
      ShellWindow window = new ShellWindow(hub);
      window.Width = 1920;
      window.Height = 1080;
      window.Show();
      Pump();

      ListBox wall = Wall(window);
      Assert.That(wall.GetVisualDescendants().OfType<ListBoxItem>().Count(), Is.EqualTo(11),
          "every plate of the level is laid out - the wall virtualises nothing and hides nothing");

      ScrollViewer? scroll = wall.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
      Assert.That(scroll, Is.Not.Null);
      Assert.That(scroll!.Extent.Height, Is.LessThanOrEqualTo(scroll.Viewport.Height + 2),
          "the wall has nothing left to scroll - a plate under the screen is what the fit exists to end");

      Assert.That(Host(window).LayoutTransform, Is.Not.Null,
          "eleven plates do not stand on a 1080-line television at full size - the wall wears its scale");
      window.Close();
    }

    [AvaloniaTest]
    public void AWallThatFitsKeepsTheFullSizeTheSkinDrew()
    {
      SettingsViewModel model = Places(4);
      SettingsView hub = new SettingsView(model);
      ShellWindow window = new ShellWindow(hub);
      window.Width = 1920;
      window.Height = 1080;
      window.Show();
      Pump();

      Assert.That(Host(window).LayoutTransform, Is.Null,
          "four plates fit a television at full size and are not shrunk for room nobody needed");
      window.Close();
    }

    [AvaloniaTest]
    public void TheWallStaysWithinTheHostThatStandsBetweenTheGradients()
    {
      SettingsViewModel model = Places(11);
      SettingsView hub = new SettingsView(model);
      ShellWindow window = new ShellWindow(hub);
      window.Width = 1920;
      window.Height = 1080;
      window.Show();
      Pump();

      LayoutTransformControl host = Host(window);
      Panel? room = host.Parent as Panel;
      Assert.That(room, Is.Not.Null);

      // The room between the bands, and the wall as the layout gives it its place: the scaled wall has to
      // stand inside the host's margin, so no gradient can ever lie over a plate.
      double wallTop = host.Bounds.Top;
      double wallBottom = wallTop + host.Bounds.Height;
      double roomBottom = room!.Bounds.Height;
      Avalonia.Thickness inset = host.Margin;
      Assert.That(wallTop, Is.GreaterThanOrEqualTo(inset.Top - 1),
          "the wall starts below the head band");
      Assert.That(wallBottom, Is.LessThanOrEqualTo(roomBottom - inset.Bottom + 1),
          "and ends above the foot band");
      window.Close();
    }
  }
}
