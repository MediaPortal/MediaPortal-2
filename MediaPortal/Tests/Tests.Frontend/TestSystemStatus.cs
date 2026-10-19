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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The status screen against servers that do and do not pick up. Everything the screen says about this
  /// machine it reads from the running runtime, so the tests check that it reads it and shows it - the
  /// version of the very assembly under test, the machine's own name - and that the server's lines are its
  /// own answer or its refusal, quoted, never an invented stand-in.
  /// </summary>
  [TestFixture]
  public class TestSystemStatus
  {
    /// <summary>A server that either describes itself or does not answer at all.</summary>
    private sealed class FakeInfoApi : IServerInfoApi
    {
      public ServerInfo? Info;
      public Exception? Throw;
      public int Asked;

      public Task<ServerInfo> GetServerInfoAsync(CancellationToken cancellation = default)
      {
        Asked++;
        if (Throw != null)
          throw Throw;
        return Task.FromResult(Info!);
      }
    }

    private static ServerInfo Server(string version)
    {
      return new ServerInfo("MediaPortal 2 Server", version, ".NET 10.0", "Linux", "X64", "X64",
          4242, null, null, null, true, false);
    }

    [Test]
    public async Task TheMachineAnswersAboutItselfEvenWithoutAServer()
    {
      SystemStatusViewModel model = new SystemStatusViewModel(() => null, () => "nereus (default), palette nereus");
      await model.ReloadAsync();

      Assert.That(model.Lines.Select(l => l.Label),
          Does.Contain("This machine").And.Contain("Frontend version").And.Contain("Wearing")
              .And.Contain("Frontend memory").And.Contain("Machine running for"),
          "the machine's own lines stand without any server");
      Assert.That(model.Lines.First(l => l.Label == "This machine").Value,
          Is.EqualTo(Environment.MachineName), "the runtime's own answer, not a guess");
      Assert.That(model.Lines.First(l => l.Label == "Frontend version").Value,
          Is.EqualTo(SystemStatusViewModel.FrontendVersion),
          "the version line is this very assembly's number");
      Assert.That(model.Lines.First(l => l.Label == "Wearing").Value, Does.Contain("nereus"),
          "the application says what it wears and the screen shows it");
      Assert.That(model.Lines.First(l => l.Label == "Server").Value, Does.Contain("Server address"),
          "the missing server names the row that fixes it, without the screen pretending otherwise");
    }

    [Test]
    public async Task TheServerJoinsWithItsOwnLines()
    {
      FakeInfoApi api = new FakeInfoApi { Info = Server("2.5.2305.14606") };
      SystemStatusViewModel model = new SystemStatusViewModel(() => api, () => "no skin, palette default");
      await model.ReloadAsync();

      Assert.That(api.Asked, Is.EqualTo(1), "one press reads the server exactly once");
      Assert.That(model.Lines.First(l => l.Label == "Server").Value,
          Is.EqualTo("MediaPortal 2 Server 2.5.2305.14606"), "the server speaks for itself");
      Assert.That(model.Lines.First(l => l.Label == "Server runtime").Value, Is.EqualTo(".NET 10.0"));
      Assert.That(model.Lines.First(l => l.Label == "Server process").Value, Does.Contain("4242"));
    }

    [Test]
    public async Task TheTwoVersionsAgreeOrSayThatTheyDoNot()
    {
      SystemStatusViewModel same = new SystemStatusViewModel(
          () => new FakeInfoApi { Info = Server(SystemStatusViewModel.FrontendVersion) }, () => "none");
      await same.ReloadAsync();
      Assert.That(same.Status, Does.Contain("same version"), "and it says so when they do");

      SystemStatusViewModel apart = new SystemStatusViewModel(
          () => new FakeInfoApi { Info = Server("2.0.0.0") }, () => "none");
      await apart.ReloadAsync();
      Assert.That(apart.Status, Does.Contain("do not report the same version"),
          "and it says so when they do not");
    }

    [Test]
    public async Task AServerThatStaysQuietIsQuotedAndNotReplaced()
    {
      FakeInfoApi api = new FakeInfoApi { Throw = new Exception("connection refused") };
      SystemStatusViewModel model = new SystemStatusViewModel(() => api, () => "none");
      await model.ReloadAsync();

      Assert.That(model.Lines.First(l => l.Label == "Server").Value, Does.Contain("connection refused"),
          "the refusal is the truth the line tells");
      Assert.That(model.Lines.Select(l => l.Label), Does.Contain("This machine"),
          "and the machine's own lines stand regardless");
    }
  }
}
