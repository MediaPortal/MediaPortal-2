/*
    This file is part of MediaPortal 2
    Licensed under the GNU General Public License v3 or later.
*/

using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using NUnit.Framework;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// The window screensaver wake handlers against the pointer events as the live backend routes them.
  /// The frontend died again and again right after loading, and the event log said why: the wake handler on
  /// the move route had been written demanding the press event richer argument type, and the move route
  /// carries the plain pointer arguments - Avalonia invokes mismatched handlers through reflection and turns
  /// the mismatch into an ArgumentException on the dispatcher, which is to say a dead application. A finger on
  /// the mouse was enough. Every wake handler now speaks the base argument type, and this raises the very
  /// events over a live window.
  /// </summary>
  [TestFixture]
  public class TestShellWakeHandlers
  {
    [Test]
    [AvaloniaTest]
    public void AMouseMovementOverTheLiveWindowWakesWithoutDying()
    {
      HomeViewModel home = new HomeViewModel();
      ShellWindow window = new ShellWindow(new HomeView(home, new WorkflowManager()));
      try
      {
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // The headless backend routes the movement the way the live one does: through the input manager,
        // which hands the move route its plain pointer arguments - the exact shape that killed the live
        // frontend when the wake handler demanded the richer type.
        window.MouseMove(new Point(400, 300));
        Dispatcher.UIThread.RunJobs();
        window.MouseDown(new Point(400, 300), MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(new Point(400, 300), MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();

        Assert.That(window.IsVisible, Is.True, "the window took the mouse and lived");
      }
      finally
      {
        window.Close();
      }
    }

    /// <summary>
    /// The timestamp a real backend always carries. Avalonia keeps it write-protected on the event arguments,
    /// so the setter the framework itself uses gets it onto the synthetic event - without one, the gesture
    /// reader on the move route asks a null nullable for its value.
    /// </summary>
    private static void Stamp(RoutedEventArgs args)
    {
      typeof(RoutedEventArgs).GetProperty("Timestamp", System.Reflection.BindingFlags.Instance
          | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!
          .GetSetMethod(true)!.Invoke(args, new object[] { 1UL });
    }
  }
}
