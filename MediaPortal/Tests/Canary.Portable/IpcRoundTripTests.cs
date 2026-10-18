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
using MediaPortal.Utilities.Process;
using NUnit.Framework;

namespace Canary.Portable
{
  /// <summary>
  /// Runs a real <see cref="IpcServer"/> and <see cref="IpcClient"/> against each other, in-process,
  /// over a real pipe.
  ///
  /// <see cref="IpcServer.OpenNewPipe"/> builds the pipe one way on Windows (message transmission mode
  /// plus an ACL) and another way everywhere else (byte mode, no ACL). That branch is the whole reason
  /// these tests exist: a guard that is never executed is not a port, and the only machine that executes
  /// the else-branch is the Linux container this suite also runs in. On Windows the tests exercise the
  /// Windows branch, so the same file is checked against both halves of the decision.
  ///
  /// What byte mode must not break is framing, because the two sides used to rely on message boundaries
  /// holding them together. They do not: both the server read loop and IpcClient.SendReceive loop until
  /// the 6-byte header and the announced payload are complete, and every response length is written into
  /// the header. That is what the round-trip assertions below demonstrate rather than assume - if the
  /// loops were not exact, these calls would hang until the response timeout or read garbage, on both
  /// platforms.
  /// </summary>
  [TestFixture]
  public class IpcRoundTripTests
  {
    /// <summary>
    /// Pipe names become file names off Windows (/tmp/CoreFxPipe_MP2-&lt;name&gt;), so each test uses a
    /// fresh name made only of characters that are safe in a path and that cannot collide with a real MP2
    /// instance or with another test running in parallel.
    /// </summary>
    private static string NewAppName()
    {
      return "Canary." + Guid.NewGuid().ToString("N");
    }

    private static IpcClient Connect(string appName)
    {
      var client = new IpcClient(appName) { ResponseTimeout = 10000 };
      client.Connect();
      Assert.That(client.IsConnected, Is.True,
        "IpcClient.Connect() swallows every exception and reports failure only through IsConnected.");
      return client;
    }

    [Test, Timeout(30000)]
    public void RoundTripReturnsTheServersProcessId()
    {
      string appName = NewAppName();
      using (var server = new IpcServer(appName))
      {
        server.Open();
        using (var client = Connect(appName))
        {
          // The server answers from its own Process.GetCurrentProcess(), which here is the test host -
          // so the expected value is known without any state shared between the two sides.
          Assert.That(client.GetProcessId(), Is.EqualTo(Environment.ProcessId));
          client.Disconnect();
        }
      }
    }

    [Test, Timeout(30000)]
    public void EveryConnectionIsServedByItsOwnPipeInstance()
    {
      string appName = NewAppName();
      using (var server = new IpcServer(appName))
      {
        server.Open();
        using (var first = Connect(appName))
        {
          Assert.That(first.GetProcessId(), Is.EqualTo(Environment.ProcessId));
          // OpenNewPipe() is called again from the connection callback before the first client is served,
          // so a second client must be able to connect while the first is still connected. If the
          // non-Windows branch broke that (by exceeding an instance limit, say), this is where it shows.
          using (var second = Connect(appName))
          {
            Assert.That(second.GetProcessId(), Is.EqualTo(Environment.ProcessId));
            Assert.That(first.GetProcessId(), Is.EqualTo(Environment.ProcessId));
            second.Disconnect();
          }
          first.Disconnect();
        }
      }
    }

    [Test, Timeout(30000)]
    public void RunningAppsListingFindsTheOpenServerAndLosesTheClosedOne()
    {
      string appName = NewAppName();
      using (var server = new IpcServer(appName))
      {
        server.Open();
        // The server registers itself by opening the pipe, so this asserts the platform-specific way of
        // answering "which MP2 applications are running?" against a server that is known to be alive.
        // On Windows that is a listing of \\.\pipe\; elsewhere it is a listing of the socket files .NET
        // leaves in the temp directory. Both are pinned here, on whichever platform the suite runs.
        Assert.That(IpcClient.GetRunningApps(), Does.Contain(appName));
      }
      Assert.That(IpcClient.GetRunningApps(), Does.Not.Contain(appName),
        "The listing must follow the pipe: an application that has shut down must not be offered to the user.");
    }

    [Test, Timeout(30000)]
    public void ShutdownCommandIsDeliveredToTheCustomCallback()
    {
      string appName = NewAppName();
      int callbackCalls = 0;
      using (var server = new IpcServer(appName))
      {
        // Without a callback the server would try Process.GetCurrentProcess().CloseMainWindow(), a no-op
        // off Windows; the callback is the seam the real hosts use and the thing worth proving.
        server.CustomShutdownCallback = () =>
        {
          callbackCalls++;
          return true;
        };
        server.Open();
        using (var client = Connect(appName))
        {
          // exitTimeout 0 keeps the client from looking the process up by id afterwards.
          Assert.That(client.ShutdownApplication(0, false), Is.True);
        }
      }
      Assert.That(callbackCalls, Is.EqualTo(1));
    }
  }
}
