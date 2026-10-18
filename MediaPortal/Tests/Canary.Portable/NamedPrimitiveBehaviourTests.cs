#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;
using System.Threading;
using NUnit.Framework;

namespace Canary.Portable
{
  /// <summary>
  /// SingleInstanceHelper.IsAlreadyRunning() is the tree's single-instance check, and on Windows it
  /// does two things at once: it creates a *named* mutex, and it grants every user access to it via
  /// MutexSecurity. Only the second is Windows-only, so the portable half was split off with a
  /// platform branch - and a split decided by reading documentation is a guess.
  ///
  /// These tests pin what the branch claims, and they ran on Linux before the code they describe was
  /// trusted - which corrected two assumptions made while writing them, both of them wrong on Windows
  /// as well as on Linux:
  ///
  ///   * a first draft asserted that a second WaitOne() on a held mutex returns false. It does not: a
  ///     Win32 mutex is recursive for its owning thread, so re-acquiring from the same thread always
  ///     succeeds. Contention only exists across threads.
  ///   * a second draft released the mutex from a helper thread, which threw
  ///     "Object synchronization method was called from an unsynchronized block of code" out of the
  ///     test host. Note what that did to the report: the run printed "Passed! - Failed: 0, Passed: 9"
  ///     and *then* "Test Run Aborted". A crashed host still prints a green summary, so only the exit
  ///     code is evidence - which is why test-canary-linux.ps1 checks the exit code and not the word.
  ///
  /// If a future runtime stops emulating named mutexes off Windows this fails, and the branch in
  /// SingleInstanceHelper has to become a seam.
  /// </summary>
  [TestFixture]
  public class NamedPrimitiveBehaviourTests
  {
    private static string UniqueName(string what)
    {
      return "MP2.Canary." + what + "." + Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Runs <paramref name="body"/> on its own thread and waits for it. Every assertion about who may
    /// release a mutex needs a real thread boundary: ReleaseMutex() from a thread other than the owner
    /// throws, on both platforms, which a first draft of these tests did and which crashed the Linux
    /// test host.
    /// </summary>
    private static void OnThread(ParameterizedThreadStart body, object state)
    {
      var worker = new Thread(body);
      worker.IsBackground = true;
      worker.Start(state);
      Assert.That(worker.Join(5000), Is.True, "worker thread finished");
    }

    [Test]
    public void HeldMutexBlocksAnotherThread()
    {
      string name = UniqueName("Mutex");
      using (var mutex = new Mutex(false, name))
      {
        Assert.That(mutex.WaitOne(500, false), Is.True, "first WaitOne on a fresh named mutex");

        bool workerGotIt = true;
        OnThread(_ => { workerGotIt = mutex.WaitOne(200, false); }, null);
        Assert.That(workerGotIt, Is.False, "another thread cannot take a held named mutex");

        // Only the owner may release; the acquisition afterwards happens on the worker, which then
        // releases what it owns.
        mutex.ReleaseMutex();

        bool afterRelease = false;
        OnThread(_ =>
        {
          afterRelease = mutex.WaitOne(500, false);
          if (afterRelease)
            mutex.ReleaseMutex();
        }, null);
        Assert.That(afterRelease, Is.True, "the mutex is obtainable once its owner releases it");
      }
    }

    [Test]
    public void SecondHandleOfTheSameNameIsTheSameObjectToAnotherThread()
    {
      // This is the property SingleInstanceHelper depends on: two handles created from the same name
      // refer to one object, so a second launcher can see that the first one holds it.
      string name = UniqueName("Pair");
      using (var first = new Mutex(false, name))
      using (var second = new Mutex(false, name))
      {
        Assert.That(first.WaitOne(500, false), Is.True, "first handle acquires");

        bool otherHandleWasFree = true;
        OnThread(_ => { otherHandleWasFree = second.WaitOne(200, false); }, null);
        Assert.That(otherHandleWasFree, Is.False, "the second handle reports the mutex as taken");

        first.ReleaseMutex();

        bool otherHandleAfterRelease = false;
        OnThread(_ =>
        {
          otherHandleAfterRelease = second.WaitOne(500, false);
          if (otherHandleAfterRelease)
            second.ReleaseMutex();
        }, null);
        Assert.That(otherHandleAfterRelease, Is.True, "the second handle acquires after the release");
      }
    }

    [Test]
    public void AbandonedNamedMutexIsAcquirable()
    {
      // IsAlreadyRunning() catches AbandonedMutexException and treats it as "we got the handle".
      // A thread that dies holding the mutex must not lock the next launcher out permanently.
      string name = UniqueName("Abandoned");
      var acquired = new ManualResetEventSlim(false);
      var holder = new Thread(() =>
      {
        // Deliberately not disposed: releasing or closing the handle here would destroy the mutex
        // instead of abandoning it, and abandoning is the case under test.
        var held = new Mutex(false, name);
        held.WaitOne(500, false);
        acquired.Set();
        // The thread exits while owning the mutex.
      });
      holder.IsBackground = true;
      holder.Start();
      Assert.That(acquired.Wait(2000), Is.True, "holder thread acquired the named mutex");
      holder.Join(2000);

      using (var second = new Mutex(false, name))
      {
        bool got;
        try
        {
          got = second.WaitOne(2000, false);
        }
        catch (AbandonedMutexException)
        {
          got = true;
        }
        Assert.That(got, Is.True, "an abandoned named mutex is acquirable again");
      }
    }
  }
}
