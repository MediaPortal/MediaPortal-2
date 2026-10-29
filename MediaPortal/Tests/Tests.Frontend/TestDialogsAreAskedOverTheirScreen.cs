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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using Avalonia.Threading;
using NUnit.Framework;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// A dialog is asked, not walked into: the screen it is a question about stays behind it, lit and where the
  /// viewer left it, and the master's furniture stands back for the length of the question. The reference kept
  /// these in its dialog system for exactly that reason, and a dialog that replaced the screen under it would
  /// hide the thing the viewer is being asked about.
  /// </summary>
  [TestFixture]
  public class TestDialogsAreAskedOverTheirScreen
  {
    private readonly Dictionary<string, Control> _views = new Dictionary<string, Control>();

    [SetUp]
    public void ForgetTheScreensOfThePreviousWindow()
    {
      // NUnit builds one fixture for all its tests, so a control that already stood in a window that has closed
      // would be a control with a parent the next window cannot take from.
      _views.Clear();
    }

    [TearDown]
    public void PutTheDefaultBack()
    {
      Application? app = Application.Current;
      if (app != null)
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
    }

    private static void Pump()
    {
      for (int round = 0; round < 20; round++)
        Dispatcher.UIThread.RunJobs();
    }

    private static ContentControl? Part(Window window, string name)
    {
      return window.GetVisualDescendants().OfType<ContentControl>().FirstOrDefault(c => c.Name == name)!;
    }

    /// <summary>A screen of the workflow, built once per stop and remembered so a test can point at it.</summary>
    private Control? BuildScreen(WorkflowFrame frame)
    {
      if (!_views.TryGetValue(frame.StateId, out Control? built))
      {
        built = frame.StateId.StartsWith("ask", StringComparison.Ordinal)
            ? new DialogMarker()
            : new ScreenMarker();
        _views[frame.StateId] = built;
      }

      return built;
    }

    [AvaloniaTest]
    public void ADialogIsRaisedOverTheScreenItWasAskedFrom()
    {
      ShellWindow window = new ShellWindow(new WorkflowManager(), BuildScreen);
      window.Show();
      Pump();

      window.Workflow.Push(new WorkflowFrame("home", "Home", new Label()));
      Pump();
      Control home = _views["home"];
      Assert.That(window.Screen, Is.SameAs(home), "the screen the viewer reached is the one on the television");

      window.Workflow.Push(new WorkflowFrame("ask", "Ask", new Label()));
      Pump();

      Assert.That(window.Screen, Is.SameAs(_views["ask"]),
          "the dialog is what the viewer is standing on now");
      Assert.That(window.GetVisualDescendants().Contains(home), Is.True,
          "and the screen behind it is still drawn - a dialog over a black ground is a screen, not a dialog");

      window.Workflow.Pop();
      Pump();

      Assert.That(window.Screen, Is.SameAs(home), "answering puts the viewer back on that screen");
      Assert.That(window.GetVisualDescendants().Contains(_views["ask"]), Is.False,
          "and the dialog is gone rather than merely hidden, so nothing of it is left to press");

      window.Close();
    }

    [AvaloniaTest]
    public void ASecondDialogStillStandsOverTheScreenAndNotOverTheFirst()
    {
      ShellWindow window = new ShellWindow(new WorkflowManager(), BuildScreen);
      window.Show();
      Pump();

      window.Workflow.Push(new WorkflowFrame("home", "Home", new Label()));
      window.Workflow.Push(new WorkflowFrame("ask", "Ask", new Label()));
      window.Workflow.Push(new WorkflowFrame("askagain", "Ask again", new Label()));
      Pump();

      Assert.That(window.Screen, Is.SameAs(_views["askagain"]));
      Assert.That(window.GetVisualDescendants().Contains(_views["home"]), Is.True,
          "the deeper question is still asked about the same screen, which is the one still drawn");

      window.Close();
    }

    [AvaloniaTest]
    public void TheMasterStandsBackWhileADialogIsUp()
    {
      FrontendTheme.Apply(Application.Current!, "nereus", out _);

      ShellWindow window = new ShellWindow(new WorkflowManager(), BuildScreen);
      window.Show();
      Pump();

      window.Workflow.Push(new WorkflowFrame("home", "Home", new Label()));
      window.Workflow.Push(new WorkflowFrame("photos", "Photos", new Label()));
      Pump();

      ContentControl header = Part(window, "ChromeHeader")!;
      Assert.That(header, Is.Not.Null);
      Assert.That(header.IsVisible, Is.True, "a screen deeper than the home wears the skin's header");
      string homeTitle = window.Chrome.ScreenTitle;

      window.Workflow.Push(new WorkflowFrame("ask", "Ask", new Label()));
      Pump();

      Assert.That(header.IsVisible, Is.False,
          "a dialog is a question about the screen behind it, not a new screen that wants the master's furniture");
      Assert.That(window.Chrome.ScreenTitle, Is.EqualTo(homeTitle),
          "and the header's word goes on saying what the screen behind is, because that is still what is showing");

      window.Workflow.Pop();
      Pump();

      Assert.That(header.IsVisible, Is.True, "the header comes back with the dialog");
      window.Close();
    }

    /// <summary>The marker a screen answers with: an ordinary stop of the workflow.</summary>
    private sealed class ScreenMarker : ContentControl
    {
    }

    /// <summary>The marker a dialog answers with: the same control, asking to be raised over what is behind.</summary>
    /// <summary>
    /// The dialog views of this front end, standing in the one law the shell reads: a view that says
    /// <see cref="IDialogScreen"/> is raised over its screen with the backdrop blurred and the chrome standing
    /// back; a view that forgets it is a full page wearing a dialog's clothes - the same dialog five times
    /// over, the viewer has said, and every forgetting was a new "invented design". So the law is checked
    /// against the real views, not only against markers.
    /// </summary>
    [Test]
    public void EveryDialogViewOfThisFrontEndSaysItIsADialog()
    {
      Type[] dialogs =
      {
          typeof(CloseView), typeof(SleepTimerView), typeof(NereusMenuEditView),
          typeof(NotificationsView), typeof(UserPickerView), typeof(WeatherPlacePickerView),
          typeof(TvProgramActionsView)
      };

      foreach (Type view in dialogs)
      {
        Assert.That(typeof(IDialogScreen).IsAssignableFrom(view), Is.True,
            view.Name + " opens as a dialog but does not say IDialogScreen - the shell would host it as a "
            + "full page with its own title and no blurred backdrop");
        Assert.That(typeof(IWorkflowScreen).IsAssignableFrom(view), Is.True,
            view.Name + " must take the keyboard when it is raised");
      }
    }

    private sealed class DialogMarker : ContentControl, IDialogScreen
    {
    }
  }
}
