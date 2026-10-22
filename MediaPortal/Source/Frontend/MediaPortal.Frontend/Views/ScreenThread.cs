using System;
using Avalonia.Threading;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The way a screen hands an answer that arrived elsewhere to the thread its own controls live on.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Every screen that asks a machine in another room a question is told the answer on a thread of the .NET thread
  /// pool, and Avalonia keeps the two threads apart: a collection a screen is bound to belongs to the thread that
  /// made it, and opening it from anywhere else throws. A background reload that nobody is waiting on takes that
  /// throw with it, which is how pressing Live TV closed the whole front end - the channels were read, and the
  /// moment the model tried to put them where the screen could see them the process ended.
  /// </para>
  /// <para>
  /// So the work a backend's answer brings back is handed over here instead of being done where it happens to
  /// arrive. Standing on the screen's own thread is the common case, though - a backend scripted for a test
  /// answers while the question is still being asked - and then the work is simply done at once, in step with the
  /// question, so that no screen is ever left showing a thing it has already been answered about.
  /// </para>
  /// <para>
  /// A model never reaches for this itself: it is handed a way of getting work onto the screen's thread when it is
  /// built, and a model built without one - which is every test of a model on its own - does its work where it
  /// arrives, exactly as it did before any of this was needed.
  /// </para>
  /// </remarks>
  internal static class ScreenThread
  {
    /// <summary>
    /// Does the work where the screen lives: straight away when this is already that thread, and otherwise as the
    /// next thing the screen does.
    /// </summary>
    internal static void Run(Action work)
    {
      if (Dispatcher.UIThread.CheckAccess())
      {
        work();
        return;
      }

      Dispatcher.UIThread.Post(work);
    }
  }
}
