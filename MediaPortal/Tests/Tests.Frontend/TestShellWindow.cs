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
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// The one window of the front end, measured on the workflow it shows: which screen is on top, what the back
  /// key does, where a click on the trail lands - and what a screen that answers the back key itself changes for
  /// nobody else. The stack's arithmetic is <see cref="TestWorkflowManager"/>'s; this fixture is about the window
  /// that has to obey it.
  /// </summary>
  [TestFixture]
  public class TestShellWindow
  {
    private readonly List<Window> _windows = new List<Window>();

    [TearDown]
    public void TearDown()
    {
      foreach (Window window in _windows)
      {
        try
        {
          window.Close();
        }
        catch (ObjectDisposedException)
        {
          // Closed by the test itself, which the root-back test does on purpose.
        }
      }
      _windows.Clear();
    }

    private static void Pump()
    {
      Dispatcher.UIThread.RunJobs();
    }

    /// <summary>A stand-in screen: a label saying which stop it is.</summary>
    private class ScreenStub : TextBlock
    {
      public ScreenStub(string text)
      {
        Text = text;
      }
    }

    private sealed class ActivatingScreen : ScreenStub, IWorkflowScreen
    {
      public int Activations;
      public int Closures;

      public ActivatingScreen(string text)
          : base(text)
      {
      }

      public void OnScreenActivated()
      {
        Activations++;
      }

      public void OnScreenClosed()
      {
        Closures++;
      }
    }

    /// <summary>A screen that answers the back key about its own business, the way the library panel does.</summary>
    private sealed class EscapeKeepingScreen : ScreenStub
    {
      public int Escapes;

      public EscapeKeepingScreen(string text)
          : base(text)
      {
        AddHandler(KeyDownEvent,
            delegate (object? sender, KeyEventArgs args)
            {
              if (args.Key != Key.Escape)
                return;
              Escapes++;
              args.Handled = true;
            },
            RoutingStrategies.Tunnel);
      }
    }

    private static WorkflowFrame Frame(string stateId, string label, Control view)
    {
      return new WorkflowFrame(stateId, label, new object()) { View = view };
    }

    private ShellWindow ShowShell(out WorkflowManager workflow, WorkflowFrame root)
    {
      workflow = new WorkflowManager();
      workflow.Push(root);
      ShellWindow shell = new ShellWindow(workflow, delegate(WorkflowFrame frame) { return null; });
      shell.Show();
      _windows.Add(shell);
      Pump();
      return shell;
    }

    private static Control ShownScreen(ShellWindow shell)
    {
      return shell.GetVisualDescendants().OfType<ContentControl>()
          .First(area => area.Content is Control).Content as Control;
    }

    private static void PressEscape(Control source)
    {
      source.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.Escape,
        Source = source
      });
      Pump();
    }

    [AvaloniaTest]
    public void TheShellShowsTheScreenOnTopOfTheStack()
    {
      WorkflowManager workflow;
      ShellWindow shell = ShowShell(out workflow, Frame("home", "Home", new ScreenStub("home screen")));

      Assert.That((ShownScreen(shell) as ScreenStub).Text, Is.EqualTo("home screen"));

      workflow.Push(Frame("browser", "Filmbibliothek", new ScreenStub("browser screen")));
      Pump();
      Assert.That((ShownScreen(shell) as ScreenStub).Text, Is.EqualTo("browser screen"),
          "pushing a stop puts its screen on the window - without opening a second window");

      workflow.Pop();
      Pump();
      Assert.That((ShownScreen(shell) as ScreenStub).Text, Is.EqualTo("home screen"),
          "and popping shows the stop underneath, the very same control the viewer left");
    }

    [AvaloniaTest]
    public void TheBackKeyPopsTheStackRatherThanClosingAWindow()
    {
      WorkflowManager workflow;
      ShellWindow shell = ShowShell(out workflow, Frame("home", "Home", new ScreenStub("home screen")));
      workflow.Push(Frame("browser", "Filmbibliothek", new ScreenStub("browser screen")));
      Pump();

      PressEscape(ShownScreen(shell));

      Assert.That(shell.IsVisible, Is.True, "back left a screen open, it did not close the window");
      Assert.That((ShownScreen(shell) as ScreenStub).Text, Is.EqualTo("home screen"));
      Assert.That(workflow.Stack.Count, Is.EqualTo(1));
    }

    [AvaloniaTest]
    public void BackFromTheRootClosesTheApplicationScreen()
    {
      WorkflowManager workflow;
      ShellWindow shell = ShowShell(out workflow, Frame("home", "Home", new ScreenStub("home screen")));
      bool closed = false;
      shell.Closed += delegate { closed = true; };

      PressEscape(ShownScreen(shell));

      Assert.That(closed, Is.True, "with nowhere left to pop to, back means leave - as it did on the lone home screen");
      _windows.Remove(shell);
    }

    [AvaloniaTest]
    public void AScreenThatClaimsTheBackKeyKeepsItsOwnBusinessAndTheStack()
    {
      // The library closes its details panel with Escape; the stack must not move under it. The shell hears the
      // key last, after every screen has declined it.
      WorkflowManager workflow;
      ShellWindow shell = ShowShell(out workflow, Frame("home", "Home", new ScreenStub("home screen")));
      EscapeKeepingScreen keeper = new EscapeKeepingScreen("browser screen");
      workflow.Push(Frame("browser", "Filmbibliothek", keeper));
      Pump();

      PressEscape(ShownScreen(shell));

      Assert.That(keeper.Escapes, Is.EqualTo(1), "the screen saw the key");
      Assert.That(workflow.Stack.Count, Is.EqualTo(2), "and the way back stayed whole");
      Assert.That((ShownScreen(shell) as ScreenStub).Text, Is.EqualTo("browser screen"));
    }

    [AvaloniaTest]
    public void AReturnedScreenIsToldToTakeItsFocusAgain()
    {
      WorkflowManager workflow;
      ActivatingScreen home = new ActivatingScreen("home screen");
      ShellWindow shell = ShowShell(out workflow, Frame("home", "Home", home));
      Assert.That(home.Activations, Is.EqualTo(1), "a screen appearing for the first time is activated");

      workflow.Push(Frame("browser", "Filmbibliothek", new ScreenStub("browser screen")));
      Pump();
      workflow.Pop();
      Pump();

      Assert.That(home.Activations, Is.EqualTo(2),
          "and the screen the viewer pops back to is activated too - it has to take the focus again, having lost it on the way out");
      Assert.That(home.Closures, Is.Zero,
          "covering a screen is not closing it - the viewer can be back, and finds it alive");

      workflow.Push(Frame("browser", "Filmbibliothek", new ScreenStub("browser screen")));
      workflow.Push(Frame("browse", "Genres", new ScreenStub("browse screen")));
      Pump();
      workflow.PopToState("home");
      Pump();

      Assert.That(home.Closures, Is.Zero, "the screen still on the stack hears nothing");
      Assert.That(home.Activations, Is.EqualTo(3));
    }

    [AvaloniaTest]
    public void AScreenClosedByTheWayBackIsToldOnce()
    {
      WorkflowManager workflow;
      ShellWindow shell = ShowShell(out workflow, Frame("home", "Home", new ScreenStub("home screen")));
      ActivatingScreen gone = new ActivatingScreen("browser screen");
      workflow.Push(Frame("browser", "Filmbibliothek", gone));
      workflow.Push(Frame("browse", "Genres", new ScreenStub("browse screen")));
      Pump();

      workflow.PopToState("home");
      Pump();

      Assert.That(gone.Closures, Is.EqualTo(1),
          "the screens the way back walked over are done with, and each learns it exactly once - that is where a screen lets go of its reading");
    }

    [AvaloniaTest]
    public void ATrailIsDrawnOnlyWhenThereIsAWayBack()
    {
      WorkflowManager workflow;
      ShellWindow shell = ShowShell(out workflow, Frame("home", "Home", new ScreenStub("home screen")));
      StackPanel trail = shell.GetVisualDescendants().OfType<StackPanel>().First();
      Assert.That(trail.IsVisible, Is.False, "at home nobody needs to be told they are at home");

      workflow.Push(Frame("browser", "Filmbibliothek", new ScreenStub("browser screen")));
      Pump();
      Assert.That(trail.IsVisible, Is.True);
      List<Button> crumbs = trail.GetVisualDescendants().OfType<Button>().ToList();
      Assert.That(crumbs.Select(crumb => crumb.Content), Is.EqualTo(new[] { "Home", "Filmbibliothek" }));    }

    [AvaloniaTest]
    public void ClickingACrumbReturnsTheViewerToStraightThere()
    {
      WorkflowManager workflow;
      ShellWindow shell = ShowShell(out workflow, Frame("home", "Home", new ScreenStub("home screen")));
      workflow.Push(Frame("browser", "Filmbibliothek", new ScreenStub("browser screen")));
      workflow.Push(Frame("browse", "Genres", new ScreenStub("browse screen")));
      Pump();

      StackPanel trail = shell.GetVisualDescendants().OfType<StackPanel>().First();
      Button homeCrumb = trail.GetVisualDescendants().OfType<Button>().First(b => (b.Content as string) == "Home");
      homeCrumb.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, homeCrumb));
      Pump();

      Assert.That(workflow.Stack.Count, Is.EqualTo(1), "the whole way in closed at once - the viewer asked for home, not for one step");
      Assert.That((ShownScreen(shell) as ScreenStub).Text, Is.EqualTo("home screen"));
    }
  }
}
