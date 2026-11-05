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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// The caret on the settings wall. The wall's plates light their edge the way the covers on the home wall
  /// do, which means the keyboard has to be able to stand one of them - and standing on a plate here must
  /// NOT be the same as picking one, because in this hub a picked plate is an opened section. The caret is
  /// therefore a pure focus caret, the arrow keys move it without touching the list's selection, and the
  /// press of the plate stood on opens it.
  /// </summary>
  [TestFixture]
  public class TestSettingsWallCaret
  {
    private static void Pump()
    {
      for (int round = 0; round < 20; round++)
        Dispatcher.UIThread.RunJobs();
    }

    private static SettingsViewModel NinePlaces()
    {
      SettingsViewModel model = new SettingsViewModel();
      for (int place = 0; place < 9; place++)
      {
        string name = "Ort " + place;
        model.Add(name, "Frontend", "eine Seite",
            delegate { return new Label { Name = "seite-" + name, Content = name }; });
      }

      return model;
    }

    private static ListBox Wall(ShellWindow window) =>
        window.GetVisualDescendants().OfType<ListBox>().Single(l => l.Classes.Contains("settings-tile-wall"));

    private static void SendKey(Control source, Key key)
    {
      source.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = key,
        Source = source
      });
      Pump();
    }

    private static ListBoxItem FocusedPlate(ShellWindow window) =>
        Wall(window).GetVisualDescendants().OfType<ListBoxItem>().First(p => p.IsKeyboardFocusWithin);

    [AvaloniaTest]
    public void TheWallStandsItsCaretOnAPlateAndTheArrowsWalkItWithoutOpeningAnything()
    {
      SettingsViewModel model = NinePlaces();
      SettingsView hub = new SettingsView(model);
      ShellWindow window = new ShellWindow(hub);
      window.Width = 1920;
      window.Height = 1080;
      window.Show();
      Pump();

      hub.OnScreenActivated();
      Pump();

      Assert.That(Wall(window).SelectedIndex, Is.EqualTo(-1),
          "a wall lands on no selection - picking a plate is an opening, and the hub may not guess");
      ListBoxItem first = FocusedPlate(window);
      Assert.That(Wall(window).Items.IndexOf(first.DataContext), Is.EqualTo(0),
          "and the caret stands on the first plate, so the first key of the visit walks from somewhere");

      SendKey(first, Key.Down);
      ListBoxItem below = FocusedPlate(window);
      Assert.That(below, Is.Not.SameAs(first), "Down walked the caret off the first plate");
      Assert.That(below.Bounds.Top, Is.GreaterThan(first.Bounds.Top),
          "and Down walked it to a plate of a lower row, not to the neighbour beside it");
      Assert.That(Wall(window).SelectedIndex, Is.EqualTo(-1),
          "walking the caret is not a pick - the wall opens nothing while the arrows move");
      Assert.That(model.CurrentView, Is.Null, "no section opened on the way");

      SendKey(below, Key.Up);
      Assert.That(FocusedPlate(window), Is.SameAs(first),
          "and Up walks back to the plate the caret came from");

      window.Close();
    }

    [AvaloniaTest]
    public void PressingThePlateTheCaretStandsOnOpensIt()
    {
      SettingsViewModel model = NinePlaces();
      SettingsView hub = new SettingsView(model);
      ShellWindow window = new ShellWindow(hub);
      window.Width = 1920;
      window.Height = 1080;
      window.Show();
      Pump();

      hub.OnScreenActivated();
      Pump();

      SendKey(FocusedPlate(window), Key.Enter);

      Assert.That(model.CurrentView, Is.Not.Null,
          "Enter is the press of the plate stood on - the page behind the first plate opened");
      Assert.That(model.ShowingContents, Is.True, "and the hub shows its contents beside the list");

      window.Close();
    }
  }
}
