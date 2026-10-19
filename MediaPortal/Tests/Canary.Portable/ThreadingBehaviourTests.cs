using System;
using System.Threading;
using MediaPortal.Utilities;
using NUnit.Framework;

namespace Canary.Portable
{
  /// <summary>
  /// Behaviour baseline for <see cref="ThreadingUtils"/>, the one threading helper SkinEngine's clipboard
  /// relies on.
  ///
  /// The helper used to come with a <c>CallWithTimeout</c> companion that stopped a worker thread by aborting
  /// it. That call answers with <see cref="PlatformNotSupportedException"/> on every .NET that is not the
  /// Windows Framework, so the helper is now limited to starting a thread nobody is obliged to wait for - which
  /// means the thread has to be a background thread, and that is what these tests pin down.
  /// </summary>
  [TestFixture]
  public class ThreadingBehaviourTests
  {
    private const int WAIT_MS = 5000;

    [Test]
    public void RunSTAThreaded_RunsTheActionOnADifferentThread()
    {
      Thread seen = null;
      using (var ran = new ManualResetEventSlim(false))
      {
        ThreadingUtils.RunSTAThreaded(() =>
        {
          seen = Thread.CurrentThread;
          ran.Set();
        });
        Assert.That(ran.Wait(WAIT_MS), Is.True, "the action never ran");
      }

      Assert.That(seen, Is.Not.Null);
      Assert.That(seen.ManagedThreadId, Is.Not.EqualTo(Thread.CurrentThread.ManagedThreadId),
          "the action was run on the calling thread instead of a new one");
    }

    [Test]
    public void RunSTAThreaded_ReturnsABackgroundThreadSoACallerThatGivesUpCanWalkAway()
    {
      Thread seen = null;
      using (var ran = new ManualResetEventSlim(false))
      {
        Thread returned = ThreadingUtils.RunSTAThreaded(() =>
        {
          seen = Thread.CurrentThread;
          ran.Set();
        });

        // A caller that stops waiting - the clipboard is held by another program, say - has to be able to leave
        // this thread behind. A foreground thread would keep the whole process alive until it finished.
        Assert.That(returned.IsBackground, Is.True, "the thread handed back is a foreground thread");
        Assert.That(ran.Wait(WAIT_MS), Is.True, "the action never ran");
      }

      Assert.That(seen.IsBackground, Is.True, "inside the action the thread is not a background thread");
    }

    [Test]
    public void RunSTAThreaded_SetsTheStaApartmentOnWindowsAndLeavesItAloneEverywhereElse()
    {
      // GetApartmentState is a Windows API in the shared framework, hence the guard around the only call.
      bool isWindows = OperatingSystem.IsWindows();
      ApartmentState seen = ApartmentState.Unknown;
      using (var ran = new ManualResetEventSlim(false))
      {
        ThreadingUtils.RunSTAThreaded(() =>
        {
          if (isWindows)
            seen = Thread.CurrentThread.GetApartmentState();
          ran.Set();
        });
        Assert.That(ran.Wait(WAIT_MS), Is.True, "the action never ran");
      }

      if (isWindows)
        // The OLE clipboard the only caller talks to refuses to work from anything but a single-threaded
        // apartment. Off Windows there is no apartment to set, so nothing is claimed and the thread simply runs.
        Assert.That(seen, Is.EqualTo(ApartmentState.STA));
      else
        Assert.That(seen, Is.EqualTo(ApartmentState.Unknown),
            "off Windows the helper must not pretend the thread has a COM apartment");
    }
  }
}
