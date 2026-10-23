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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Notifications;
using MediaPortal.Frontend.Skins.Nereus;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// A question raised over a screen has to hold the keyboard, or it is not a question but a picture lying over a
  /// screen the viewer is no longer standing on: every arrow they press walks the blurred shelf behind it, every
  /// Enter presses a tile nobody meant. That is what this checks, for the dialog the front end raises by itself and
  /// for one that never said a word about its keys.
  /// </summary>
  /// <remarks>
  /// The keys are raised as bubbling events from the control that holds the focus, which is how the remote's keys
  /// reach a screen in a running front end - see <c>TestHomeWindow.Press</c>, the same arrangement.
  /// </remarks>
  [TestFixture]
  public class TestTheDialogTakesTheKeyboard
  {
    private string _skinsFolderBefore = "";

    [SetUp]
    public void RememberWhereSkinsComeFrom()
    {
      _skinsFolderBefore = SkinEngine.SkinsFolder;
    }

    [TearDown]
    public void PutTheSkinBack()
    {
      Application? app = Application.Current;
      if (app != null)
      {
        SkinEngine.Clear(app);
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
      }

      SkinEngine.SkinsFolder = _skinsFolderBefore;
    }

    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestTheDialogTakesTheKeyboard).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      return value!.Value!;
    }

    /// <summary>The face whose dialog frame the questions are drawn in, off the disk rather than off a build's copy.</summary>
    private static void WearTheShippedSkin()
    {
      Application app = Application.Current!;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      FrontendTheme.Apply(app, "nereus", out _);
      Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");
    }

    private static void Pump()
    {
      for (int round = 0; round < 20; round++)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(5);
      }
    }

    private static bool StandsInside(Visual? node, Visual area)
    {
      while (node != null)
      {
        if (ReferenceEquals(node, area))
          return true;
        node = node.GetVisualParent();
      }

      return false;
    }

    private static void Press(Control on, Key key)
    {
      on.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = key,
        Source = on
      });
      Pump();
    }

    private static Visual? Focused(ShellWindow window)
    {
      return window.FocusManager.GetFocusedElement() as Visual;
    }

    /// <summary>A screen to be asked about: one button the viewer was standing on.</summary>
    private static Button Ground(out WorkflowManager workflow)
    {
      Button ground = new Button { Content = "the shelf behind" };
      workflow = new WorkflowManager();
      return ground;
    }

    /// <summary>
    /// The notification dialog takes the keyboard when it goes up, and gives it back to the screen behind when it
    /// comes down.
    /// </summary>
    [AvaloniaTest]
    public void ANotificationDialogHoldsTheKeyboardItIsAskedWith()
    {
      Button ground = Ground(out WorkflowManager workflow);
      ShellWindow window = new ShellWindow(workflow, null);
      workflow.Push(new WorkflowFrame("ground", "Ground", ground) { View = ground });
      window.Show();
      Pump();

      Assert.That(Focused(window), Is.SameAs(ground), "the viewer was standing on the screen behind");

      NotificationQueue queue = new NotificationQueue();
      queue.Enqueue(NotificationType.Info, "a word for the viewer", "while they were doing something");
      NotificationsViewModel model = new NotificationsViewModel(queue);
      NotificationsView dialog = new NotificationsView(model, workflow);
      workflow.Push(new WorkflowFrame("notifications", "Notifications", model) { View = dialog });
      Pump();

      Visual? held = Focused(window);
      Assert.That(held, Is.Not.Null, "the dialog left nobody holding the keyboard at all");
      Assert.That(StandsInside(held, dialog), Is.True,
          "a dialog standing over a screen without the keyboard is a picture, not a question: the arrows the viewer "
          + "presses walk the screen behind it, which is exactly what the shell has to prevent");

      // And the arrows walk the dialog's own buttons rather than anything behind it.
      Button? first = held as Button;
      Assert.That(first, Is.Not.Null, "the dialog's keys start on one of its buttons");
      Press(first!, Key.Right);
      Visual? walked = Focused(window);
      Assert.That(StandsInside(walked, dialog), Is.True,
          "an arrow inside a dialog leaves the dialog - it walked the screen behind, which the viewer cannot even "
          + "see sharply any more");
      Assert.That(walked, Is.Not.SameAs(ground), "and never lands back on the screen behind");
      Assert.That(walked, Is.InstanceOf<Button>(), "the arrow reached another of the dialog's buttons");

      workflow.Pop();
      Pump();
      Assert.That(Focused(window), Is.SameAs(ground),
          "and the keyboard goes back where it came from when the question is done with");
      window.Close();
    }

    /// <summary>
    /// A dialog that never said where its keys start still holds the keyboard: the shell's net, so that the next
    /// dialog written without an <see cref="IWorkflowScreen.OnScreenActivated"/> is a dialog that can be answered
    /// rather than a wall the viewer has to Escape out of.
    /// </summary>
    [AvaloniaTest]
    public void ADialogThatSaysNothingAboutItsKeysStillHoldsTheKeyboard()
    {
      Button ground = Ground(out WorkflowManager workflow);
      ShellWindow window = new ShellWindow(workflow, null);
      workflow.Push(new WorkflowFrame("ground", "Ground", ground) { View = ground });
      window.Show();
      Pump();

      QuietDialog quiet = new QuietDialog();
      workflow.Push(new WorkflowFrame("quiet", "Quiet", "nothing") { View = quiet });
      Pump();

      Visual? held = Focused(window);
      Assert.That(StandsInside(held, quiet), Is.True,
          "the quiet dialog has no idea where its keys start, and the shell should not have left the keyboard in "
          + "the screen behind it");
      window.Close();
    }

    /// <summary>
    /// The menu editor - the heaviest question this front end asks, and the one with the most buttons to walk -
    /// holds the keyboard as well, and on the control it asked for rather than on whatever the shell's net reached
    /// first.
    /// </summary>
    [AvaloniaTest]
    public void TheEditorDialogHoldsTheKeyboardOnTheControlItAskedFor()
    {
      WearTheShippedSkin();

      string file = Path.Combine(Path.GetTempPath(),
          "mp2-dialogkeys-" + Guid.NewGuid().ToString("N") + ".json");
      try
      {
        NereusMenuEditViewModel editor = new NereusMenuEditViewModel(
            MainMenu.Build(Array.Empty<AspectViewModel>(), connected: false), null, null, file);

        Button ground = Ground(out WorkflowManager workflow);
        ShellWindow window = new ShellWindow(workflow, null);
        workflow.Push(new WorkflowFrame("ground", "Ground", ground) { View = ground });
        window.Show();
        Pump();

        NereusMenuEditView dialog = new NereusMenuEditView(editor, NereusMenuEditScreen.Tiles, workflow,
            delegate (string key)
            {
              return new WorkflowFrame(key, key, editor);
            });
        workflow.Push(new WorkflowFrame("menuedit", "Edit menu", editor) { View = dialog });
        Pump();

        Visual? held = Focused(window);
        Assert.That(StandsInside(held, dialog), Is.True,
            "the editor went up over the screen and left the keyboard in that screen: every arrow the viewer presses "
            + "then moves the shelf behind the dialog, and a dialog about moving tiles cannot move a tile");
        Assert.That(held, Is.InstanceOf<ListBox>(),
            "and it holds it on the list of lines, where the editor said its keys start");

        window.Close();
      }
      finally
      {
        try { File.Delete(file); } catch (IOException) { }
      }
    }

    /// <summary>A dialog that never mentions focus: only a button standing in a panel.</summary>
    public sealed class QuietDialog : Panel, IWorkflowScreen, IDialogScreen
    {
      public QuietDialog()
      {
        Children.Add(new Button { Content = "an answer nobody pointed the keys at" });
      }
    }
  }
}
