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
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using NUnit.Framework;
using MediaPortal.Frontend.Input;

namespace Tests.Frontend
{
  /// <summary>
  /// Moving focus with a remote's directional keys, on the headless platform - the same tests run on a machine
  /// with no display, which is what a build agent and a container are.
  /// </summary>
  [TestFixture]
  public class TestDpadNavigation
  {
    private readonly List<Window> _windows = new List<Window>();

    [TearDown]
    public void TearDown()
    {
      foreach (Window window in _windows)
        window.Close();
      _windows.Clear();
    }

    private (Window Window, List<Control> Ring) ShowRing(int size)
    {
      StackPanel panel = new StackPanel();
      List<Control> ring = new List<Control>();
      for (int i = 0; i < size; i++)
      {
        Button button = new Button { Content = "area " + i };
        panel.Children.Add(button);
        ring.Add(button);
      }

      Window window = new Window { Content = panel };
      DpadNavigation.Attach(window, ring);
      window.Show();
      Dispatcher.UIThread.RunJobs();
      _windows.Add(window);
      return (window, ring);
    }

    private static void Focus(Control control)
    {
      control.Focus();
      Dispatcher.UIThread.RunJobs();
      Assert.That(control.IsFocused, Is.True, "the test needs a focused control to move away from");
    }

    private static KeyEventArgs Press(Control source, Key key, bool handled = false)
    {
      var args = new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = key,
        Source = source,
        Handled = handled
      };
      source.RaiseEvent(args);
      return args;
    }

    [AvaloniaTest]
    public void RightMovesFocusToTheNextAreaOfTheRing()
    {
      (_, List<Control> ring) = ShowRing(3);
      Focus(ring[0]);

      KeyEventArgs args = Press(ring[0], Key.Right);

      Assert.That(ring[1].IsFocused, Is.True);
      Assert.That(args.Handled, Is.True, "a key the ring used must not also reach whatever is behind it");
    }

    [AvaloniaTest]
    public void LeftMovesBackwards()
    {
      (_, List<Control> ring) = ShowRing(3);
      Focus(ring[2]);

      Press(ring[2], Key.Left);
      Assert.That(ring[1].IsFocused, Is.True);
    }

    [AvaloniaTest]
    public void TheRingWrapsAtBothEnds()
    {
      // Four areas, no mouse: making the viewer walk back from the last one is a worse answer than wrapping.
      (_, List<Control> ring) = ShowRing(3);

      Focus(ring[2]);
      Press(ring[2], Key.Right);
      Assert.That(ring[0].IsFocused, Is.True, "past the end, the ring starts again");

      Focus(ring[0]);
      Press(ring[0], Key.Left);
      Assert.That(ring[2].IsFocused, Is.True, "before the start, it ends");
    }

    [AvaloniaTest]
    public void DownAndUpMoveBetweenAreasToo()
    {
      // The ring is the fallback for a direction nobody else wanted, so a screen whose areas are stacked
      // vertically still works with a remote that only has the four arrows.
      (_, List<Control> ring) = ShowRing(2);
      Focus(ring[0]);

      Press(ring[0], Key.Down);
      Assert.That(ring[1].IsFocused, Is.True);

      Press(ring[1], Key.Up);
      Assert.That(ring[0].IsFocused, Is.True);
    }

    [AvaloniaTest]
    public void AKeyTheFocusedControlAlreadyTookIsLeftToIt()
    {
      // This is the rule the whole design rests on: a list that moved its selection with Down does not also
      // move the focus out of the list.
      (_, List<Control> ring) = ShowRing(2);
      Focus(ring[0]);

      KeyEventArgs args = Press(ring[0], Key.Down, handled: true);

      Assert.That(ring[0].IsFocused, Is.True, "focus stayed where it was");
      Assert.That(ring[1].IsFocused, Is.False);
      Assert.That(args.Handled, Is.True);
    }

    [AvaloniaTest]
    public void AKeyThatIsNotADirectionIsNotTheRingsBusiness()
    {
      // F9, not Enter: a button in Avalonia takes Enter for itself (it invokes the click), and the claim here is
      // only about the ring - a key that is not a direction must leave the focus alone and unhandled.
      (_, List<Control> ring) = ShowRing(2);
      Focus(ring[0]);

      KeyEventArgs args = Press(ring[0], Key.F9);

      Assert.That(args.Handled, Is.False);
      Assert.That(ring[0].IsFocused, Is.True);
    }

    [AvaloniaTest]
    public void AnAreaThatCannotTakeTheFocusDoesNotSwallowTheKey()
    {
      // Measured on the first version of the real home screen: its list reported itself not focusable, so the
      // ring "handled" the Right key by moving the focus nowhere, and the viewer's remote did nothing at all.
      // A key that achieved nothing stays available for anything else to answer.
      var panel = new StackPanel();
      var first = new Button { Content = "first" };
      var unreachable = new Button { Content = "unreachable", Focusable = false };
      panel.Children.Add(first);
      panel.Children.Add(unreachable);

      Window window = new Window { Content = panel };
      DpadNavigation.Attach(window, new List<Control> { first, unreachable });
      window.Show();
      Dispatcher.UIThread.RunJobs();
      _windows.Add(window);

      Focus(first);
      KeyEventArgs args = Press(first, Key.Right);

      Assert.That(unreachable.IsFocused, Is.False);
      Assert.That(first.IsFocused, Is.True, "the focus stayed where it was");
      Assert.That(args.Handled, Is.False, "the ring did not move the focus, so it does not claim to have");
    }

    [AvaloniaTest]
    public void AnAreaThatIsDisabledIsWalkedPast()
    {
      // The first run of the server screen is what this is for: with an empty address the test and save buttons are
      // disabled, and a ring that insists on the next control in order traps the viewer in the text field. The areas
      // that are not there are walked over.
      var panel = new StackPanel();
      var field = new TextBox();
      var disabled = new Button { Content = "test", IsEnabled = false };
      var reachable = new Button { Content = "close" };
      panel.Children.Add(field);
      panel.Children.Add(disabled);
      panel.Children.Add(reachable);

      Window window = new Window { Content = panel };
      DpadNavigation.Attach(window, new List<Control> { field, disabled, reachable });
      window.Show();
      Dispatcher.UIThread.RunJobs();
      _windows.Add(window);

      Focus(field);
      KeyEventArgs args = Press(field, Key.Right);

      Assert.That(disabled.IsFocused, Is.False);
      Assert.That(reachable.IsFocused, Is.True, "the focus went to the next area that could take it");
      Assert.That(args.Handled, Is.True, "and the key really did move the focus, so the ring claims it");
    }

    [AvaloniaTest]
    public void ARingWithOnlyOneReachableAreaHasNowhereToGo()
    {
      // Walking past a disabled area must not turn into walking back into the one that already has the focus: that
      // would claim the key while achieving nothing, which is the failure the test above this one exists for.
      var panel = new StackPanel();
      var onlyOne = new Button { Content = "only" };
      var disabled = new Button { Content = "test", IsEnabled = false };
      panel.Children.Add(onlyOne);
      panel.Children.Add(disabled);

      Window window = new Window { Content = panel };
      DpadNavigation.Attach(window, new List<Control> { onlyOne, disabled });
      window.Show();
      Dispatcher.UIThread.RunJobs();
      _windows.Add(window);

      Focus(onlyOne);
      KeyEventArgs args = Press(onlyOne, Key.Right);

      Assert.That(onlyOne.IsFocused, Is.True, "the focus stayed where it was");
      Assert.That(args.Handled, Is.False, "a full lap with nowhere to go is not a move");
    }

    [AvaloniaTest]
    public void WithNothingInTheRingFocusedTheFirstAreaIsTheAnswer()
    {
      // A screen that has just appeared has no focus in the ring yet. The first key press should still land
      // somewhere sensible instead of doing nothing.
      (_, List<Control> ring) = ShowRing(3);
      Assert.That(ring[0].IsFocused, Is.False);

      Control? target = DpadNavigation.NextInRing(ring, Key.Right);
      Assert.That(target, Is.SameAs(ring[0]));
    }

    [AvaloniaTest]
    public void ARingWithNoControlsIsRejectedRatherThanLeavingTheViewerStuck()
    {
      Window window = new Window();
      Assert.That(() => DpadNavigation.Attach(window, new List<Control>()),
          Throws.ArgumentException.With.Message.Contains("no controls"));
      Assert.That(() => DpadNavigation.Attach(window, null!),
          Throws.InstanceOf<ArgumentNullException>());
    }
  }
}
