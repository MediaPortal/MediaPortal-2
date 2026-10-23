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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The who-is-watching question against a server of its own making. The reference asked this in a
  /// living room, not in a file, so what these tests hold is the promise around the asking: the server's
  /// people arrive as rows and its machines do not, a picked name is remembered where the next start will
  /// read it and handed to the application that re-hangs the skin, "watch on my own" ends the asking
  /// rather than pretending to answer it, and the switch that turns the whole question on lives with the
  /// users - which is where the reference kept it too.
  /// </summary>
  [TestFixture]
  public class TestViewerPicker
  {
    private string _file = "";
    private string _previousClientFile = "";

    [SetUp]
    public void SetUp()
    {
      _file = Path.Combine(Path.GetTempPath(), "mp2-viewer-test-" + Guid.NewGuid().ToString("N") + ".json");
      _previousClientFile = Environment.GetEnvironmentVariable("MP2_CLIENT_FILE") ?? "";
      Environment.SetEnvironmentVariable("MP2_CLIENT_FILE", _file);
    }

    [TearDown]
    public void TearDown()
    {
      Environment.SetEnvironmentVariable("MP2_CLIENT_FILE", _previousClientFile.Length == 0 ? null : _previousClientFile);
      if (File.Exists(_file))
        File.Delete(_file);
    }

    /// <summary>A server that answers whatever the test tells it to and remembers nothing.</summary>
    private sealed class FakeUsersApi : IUsersApi
    {
      public UsersAnswer ListAnswer = new UsersAnswer();
      public Exception? ThrowOnGet;

      public Task<UsersAnswer> GetUsersAsync(CancellationToken cancellation = default)
      {
        if (ThrowOnGet != null)
          throw ThrowOnGet;
        return Task.FromResult(ListAnswer);
      }

      public Task<UserAnswer> CreateUserAsync(CreateUserWish wish, CancellationToken cancellation = default)
      {
        throw new NotSupportedException("the picker only reads.");
      }

      public Task<UserAnswer> UpdateUserAsync(Guid profileId, UpdateUserWish wish,
          CancellationToken cancellation = default)
      {
        throw new NotSupportedException("the picker only reads.");
      }

      public Task<UserAnswer> DeleteUserAsync(Guid profileId, CancellationToken cancellation = default)
      {
        throw new NotSupportedException("the picker only reads.");
      }
    }

    private static UserInfo Person(string name, string kind = "User")
    {
      return new UserInfo { ProfileId = Guid.NewGuid(), Name = name, Kind = kind };
    }

    [Test]
    public async Task ThePickerListsPeopleAndNeverMachines()
    {
      FakeUsersApi api = new FakeUsersApi();
      api.ListAnswer.Users = new UserInfo[] { Person("Petra"), Person("Wohnzimmer", "Client"), Person("Ralf") };
      ViewerPickerViewModel picker = new ViewerPickerViewModel(() => api, delegate { });

      await picker.LoadAsync();

      Assert.That(picker.People, Has.Count.EqualTo(2), "a machine's profile is not somebody watching the telly");
      Assert.That(picker.People[0].Name, Is.EqualTo("Petra"));
      Assert.That(picker.Status, Does.Contain("everybody together"),
          "before anybody has answered, the room watches all together, and the screen says so");
    }

    [Test]
    public void APickedNameIsRememberedWhereTheNextStartWillReadIt()
    {
      List<string?> answers = new List<string?>();
      ViewerPickerViewModel picker = new ViewerPickerViewModel(() => null, answers.Add);

      picker.Pick("Petra");

      Assert.That(answers, Is.EqualTo(new string?[] { "Petra" }),
          "the application is told, because it is the application that re-hangs the skin");
      Assert.That(ClientSettings.Load(null, out _).RememberedViewer, Is.EqualTo("Petra"),
          "and the answer is remembered where tomorrow's start will look for it");
      Assert.That(picker.WatchingAs, Is.EqualTo("Petra"));
    }

    [Test]
    public void WatchingAloneEndsTheAskingInsteadOfPretendingToAnswerIt()
    {
      ClientSettings opening = ClientSettings.Load(null, out _);
      opening.AskForViewer = true;
      opening.Save(null, out _);

      List<string?> answers = new List<string?>();
      ViewerPickerViewModel picker = new ViewerPickerViewModel(() => null, answers.Add);
      picker.WatchOnOwn();

      Assert.That(answers, Is.EqualTo(new string?[] { null }), "nobody's name is still an answer the room gave");
      ClientSettings after = ClientSettings.Load(null, out _);
      Assert.That(after.RememberedViewer, Is.Empty);
      Assert.That(after.AskForViewer, Is.False,
          "\"nobody in particular\" said it for this box too - the next start must not ask again");
    }

    [Test]
    public async Task AServerWithNoPeopleOrNoNewsSaysSoInsteadOfShowingAnEmptyForm()
    {
      FakeUsersApi empty = new FakeUsersApi();
      empty.ListAnswer.Users = Array.Empty<UserInfo>();
      ViewerPickerViewModel nobody = new ViewerPickerViewModel(() => empty, delegate { });
      await nobody.LoadAsync();
      Assert.That(nobody.Status, Does.Contain("no people"));

      FakeUsersApi refusing = new FakeUsersApi();
      refusing.ThrowOnGet = new Exception("connection refused");
      ViewerPickerViewModel broken = new ViewerPickerViewModel(() => refusing, delegate { });
      await broken.LoadAsync();
      Assert.That(broken.Status, Does.Contain("connection refused"),
          "the server's own sentence is quoted, not argued with");
    }

    [Test]
    public async Task WithoutAServerTheRoomCanStillWatchTogether()
    {
      List<string?> answers = new List<string?>();
      ViewerPickerViewModel picker = new ViewerPickerViewModel(() => null, answers.Add);
      await picker.LoadAsync();

      Assert.That(picker.Status, Does.Contain("No server"));
      picker.WatchOnOwn();
      Assert.That(answers, Has.Count.EqualTo(1), "the escape does not need a server to be reachable");
    }

    [Test]
    public void TheAskingSwitchLeftForTheServerAndTheAnswerCanBeForgotten()
    {
      // The switch that decides whether the front end asks lives with the other house rules on the server
      // now, so the users screen has no asking checkbox of its own left to flip. What it keeps is the
      // local half: the remembered answer, which is this machine's business.
      UsersSettingsViewModel screen = new UsersSettingsViewModel(() => null);

      Assert.That(screen.HasPolicy, Is.False, "no server means no house rules to show, and no rules invented");
      ClientSettings remembered = ClientSettings.Load(null, out _);
      remembered.RememberedViewer = "";
      remembered.Save(null, out _);
      Assert.That(screen.WatchingSays, Does.Contain("all together"));

      remembered = ClientSettings.Load(null, out _);
      remembered.RememberedViewer = "Ralf";
      remembered.Save(null, out _);
      Assert.That(screen.WatchingSays, Does.Contain("Ralf"),
          "the users screen sees the answer the wake left behind");

      screen.AskAgainNextStart();
      Assert.That(ClientSettings.Load(null, out _).RememberedViewer, Is.Empty);
      Assert.That(screen.WatchingSays, Does.Contain("all together"));
    }

    [Test]
    public void TheViewerAnswersSurviveTheFile()
    {
      ClientSettings settings = ClientSettings.Load(null, out _);
      Assert.That(settings.AskForViewer, Is.False, "a household that was never asked is not asked");
      Assert.That(settings.RememberedViewer, Is.Empty);

      settings.AskForViewer = true;
      settings.RememberedViewer = "Petra";
      settings.Save(null, out bool saved);
      Assert.That(saved, Is.True);

      ClientSettings again = ClientSettings.Load(null, out _);
      Assert.That(again.AskForViewer, Is.True);
      Assert.That(again.RememberedViewer, Is.EqualTo("Petra"));
    }
  }
}
