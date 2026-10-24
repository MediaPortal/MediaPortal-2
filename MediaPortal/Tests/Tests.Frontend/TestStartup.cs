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
using System.Diagnostics;
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
  /// The wake of the front end: the wallpaper is the screen's business, but what the machine asks, when it
  /// asks and what an answer means are here. The four promises these tests keep: a start with nobody to ask
  /// holds the animation for its wait and then opens; a remembered person with four numbers standing behind
  /// them gets asked, and the fourth digit alone is the answer; a wrong one shakes, empties and asks again
  /// without ever opening; and whoever the wake ends with - remembered, switched to, or nobody - is written
  /// where the next start reads it and is the one word the application re-hangs the skin on.
  /// </summary>
  [TestFixture]
  public class TestStartup
  {
    private string _file = "";
    private string _previousClientFile = "";

    [SetUp]
    public void SetUp()
    {
      _file = Path.Combine(Path.GetTempPath(), "mp2-startup-test-" + Guid.NewGuid().ToString("N") + ".json");
      _previousClientFile = Environment.GetEnvironmentVariable("MP2_CLIENT_FILE") ?? "";
      Environment.SetEnvironmentVariable("MP2_CLIENT_FILE", _file);
    }

    [TearDown]
    public void TearDown()
    {
      Environment.SetEnvironmentVariable("MP2_CLIENT_FILE", _previousClientFile.Length == 0 ? null : _previousClientFile);
      // An answered wake names the skin-settings file's owner for the whole process; a fixture that runs
      // after this one must find the shared file where it left it, not a person's file of its own.
      MediaPortal.Frontend.Settings.SkinSettingsBase.FileOwner = null;
      if (File.Exists(_file))
        File.Delete(_file);
    }

    /// <summary>A server whose people the test names, and whose sign-in answer the test writes down.</summary>
    private sealed class FakeWakeServer : IUsersApi
    {
      public UserInfo[] People = Array.Empty<UserInfo>();
      public bool SignInAnswers = true;
      public readonly List<(string Name, string Pin)> Asked = new List<(string, string)>();

      public Task<UsersAnswer> GetUsersAsync(CancellationToken cancellation = default)
      {
        return Task.FromResult(new UsersAnswer { Users = People });
      }

      public Task<UserAnswer> CreateUserAsync(CreateUserWish wish, CancellationToken cancellation = default)
          => throw new NotSupportedException();

      public Task<UserAnswer> UpdateUserAsync(Guid profileId, UpdateUserWish wish,
          CancellationToken cancellation = default)
          => throw new NotSupportedException();

      public Task<UserAnswer> DeleteUserAsync(Guid profileId, CancellationToken cancellation = default)
          => throw new NotSupportedException();

      public Task<bool> Check(string name, string pin)
      {
        Asked.Add((name, pin));
        return Task.FromResult(SignInAnswers);
      }
    }

    private static UserInfo Person(string name, bool pin = false, string kind = "User")
    {
      return new UserInfo { ProfileId = Guid.NewGuid(), Name = name, Kind = kind, HasPassword = pin };
    }

    private static void Remember(string name)
    {
      ClientSettings settings = ClientSettings.Load(null, out _);
      settings.RememberedViewer = name;
      settings.Save(null, out _);
    }

    private static StartupViewModel Wake(FakeWakeServer? server, List<string?> answers,
        bool acceptPin = true, int splashMs = 30)
    {
      return new StartupViewModel(
          () => server,
          acceptPin ? server!.Check : (StartupViewModel.PinChecker)delegate { return Task.FromResult(false); },
          answers.Add,
          TimeSpan.FromMilliseconds(splashMs));
    }

    [Test]
    public async Task AWakeNobodyHasToAnswerHoldsItsWaitAndThenOpens()
    {
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Petra"), Person("Wohnzimmer", kind: "Client") };
      List<string?> answers = new List<string?>();
      StartupViewModel wake = Wake(server, answers);

      await wake.StartAsync();

      Assert.That(answers, Is.EquivalentTo(new string?[] { "" }),
          "nobody to ask is not a question - the wake answers itself with an empty name and opens");
      Assert.That(wake.NeedsPin, Is.False, "there was nothing to ask, so nothing is asked");
    }

    [Test]
    public async Task APersonWithoutNumbersIsWokenByName()
    {
      Remember("Ralf");
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Ralf") };
      List<string?> answers = new List<string?>();
      StartupViewModel wake = Wake(server, answers);

      await wake.StartAsync();

      Assert.That(answers, Is.EquivalentTo(new string?[] { "Ralf" }),
          "the remembered person who needs no PIN is greeted by name and the wake ends with them");
      Assert.That(wake.Status, Is.Empty, "the wait says nothing - the wallpaper and the ring are the whole screen");
    }

    [Test]
    public async Task APinHouseWithNoHabitYetGetsAsked()
    {
      // No remembered viewer at all - a fresh client in a house where somebody wears four numbers.
      // The door needs a PIN, so the wake asks rather than waving the viewer through unasked.
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Ralf"), Person("Petra", pin: true) };
      List<string?> answers = new List<string?>();
      StartupViewModel wake = Wake(server, answers);

      await wake.StartAsync();

      Assert.That(answers, Is.Empty, "a house with a PIN is not walked into unasked, habit or no habit");
      Assert.That(wake.NeedsPin, Is.True);
      Assert.That(wake.PersonName, Is.EqualTo("Petra"), "the first person standing behind numbers is asked");
      Assert.That(wake.HasOthers, Is.True, "and Ralf can still be switched to");
    }

    [Test]
    public async Task ADeletedRememberedPersonLeavesThePinAsked()
    {
      // The household's habit outlived the person; the PIN the house wears does not go with them.
      Remember("Gone");
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Petra", pin: true) };
      List<string?> answers = new List<string?>();
      StartupViewModel wake = Wake(server, answers);

      await wake.StartAsync();

      Assert.That(answers, Is.Empty);
      Assert.That(wake.NeedsPin, Is.True);
      Assert.That(wake.PersonName, Is.EqualTo("Petra"));
    }

    [Test]
    public async Task AHouseWithNoNumbersAnywhereOpensAfterItsWait()
    {
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Ralf"), Person("Petra") };
      List<string?> answers = new List<string?>();
      StartupViewModel wake = Wake(server, answers);

      await wake.StartAsync();

      Assert.That(answers, Is.EquivalentTo(new string?[] { "" }),
          "when nobody wears a PIN there is nothing to ask and nobody to ask it of");
      Assert.That(wake.NeedsPin, Is.False);
      Assert.That(wake.Status, Is.Empty);
    }

    [Test]
    public async Task TheRememberedPersonWithNumbersGetsAsked()
    {
      Remember("Petra");
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Petra", pin: true), Person("Ralf") };
      List<string?> answers = new List<string?>();
      StartupViewModel wake = Wake(server, answers);

      await wake.StartAsync();

      Assert.That(answers, Is.Empty, "the wake waits for the four numbers instead of opening past them");
      Assert.That(wake.NeedsPin, Is.True);
      Assert.That(wake.Busy, Is.False);
      Assert.That(wake.PersonName, Is.EqualTo("Petra"));
      Assert.That(wake.Initials, Is.EqualTo("P"), "the round container wears the letter until a picture does");
      Assert.That(wake.HasOthers, Is.True, "one other person standing by is reason enough for the switch");
      Assert.That(wake.Others, Has.Count.EqualTo(1));
      Assert.That(wake.Others[0].Name, Is.EqualTo("Ralf"));
    }

    [Test]
    public async Task TheFourthNumberIsTheAnswerNotAnotherDigit()
    {
      Remember("Petra");
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Petra", pin: true) };
      List<string?> answers = new List<string?>();
      StartupViewModel wake = Wake(server, answers);
      await wake.StartAsync();

      wake.Press("1");
      wake.Press("2");
      wake.Press("3");
      Assert.That(server.Asked, Is.Empty, "three digits are a question forming, not an answer given");
      Assert.That(wake.Pin, Is.EqualTo("123"));

      wake.Press("4");

      Assert.That(server.Asked, Has.Count.EqualTo(1), "the fourth digit asks the server once");
      Assert.That(server.Asked[0], Is.EqualTo(("Petra", "1234")));
      Assert.That(answers, Is.EquivalentTo(new string?[] { "Petra" }),
          "right numbers open, by themselves, without an OK to press");
      Assert.That(ClientSettings.Load(null, out _).RememberedViewer, Is.EqualTo("Petra"));
    }

    [Test]
    public async Task ANoAnswerShakesEmptyAndStaysAsking()
    {
      Remember("Petra");
      FakeWakeServer server = new FakeWakeServer();
      server.SignInAnswers = false;
      server.People = new[] { Person("Petra", pin: true) };
      List<string?> answers = new List<string?>();
      StartupViewModel wake = Wake(server, answers);
      await wake.StartAsync();

      int shakes = 0;
      wake.PinRejected += delegate { shakes++; };
      wake.Press("9");
      wake.Press("9");
      wake.Press("9");
      wake.Press("9");

      Assert.That(shakes, Is.EqualTo(1), "the screen is told once, and answers with the shake");
      Assert.That(wake.Pin, Is.Empty, "the boxes stand empty for the next try");
      Assert.That(answers, Is.Empty, "a wrong four opens nothing");
      Assert.That(wake.NeedsPin, Is.True, "and the asking simply goes on");

      wake.Press("1");
      wake.Press("2");
      wake.Press("3");
      wake.Press("4");
      Assert.That(server.Asked, Has.Count.EqualTo(2), "unlimited tries - the shake is the whole punishment");
    }

    [Test]
    public async Task BackspaceTakesTheNewestNumberBack()
    {
      Remember("Petra");
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Petra", pin: true) };
      StartupViewModel wake = Wake(server, new List<string?>());
      await wake.StartAsync();

      wake.Press("1");
      wake.Press("2");
      wake.Backspace();
      Assert.That(wake.Pin, Is.EqualTo("1"), "undo is newest-first, which is what Del means everywhere else");
      wake.Backspace();
      Assert.That(wake.Pin, Is.Empty);
      wake.Backspace();
      Assert.That(wake.Pin, Is.Empty, "backspace on an empty field undoes nothing, and does not throw");
    }

    [Test]
    public async Task ADigitPastFourIsNoDigitAtAll()
    {
      Remember("Petra");
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Petra", pin: true) };
      StartupViewModel wake = Wake(server, new List<string?>());
      await wake.StartAsync();

      wake.Press("1");
      wake.Press("2");
      wake.Press("3");
      wake.Press("4");
      wake.Press("5");
      Assert.That(server.Asked, Has.Count.EqualTo(1), "the fourth digit was the answer; the fifth is noise");
    }

    [Test]
    public void SwitchingToAPersonWithoutNumbersWalksThemIn()
    {
      Remember("Petra");
      FakeWakeServer server = new FakeWakeServer();
      UserInfo ralf = Person("Ralf");
      server.People = new[] { Person("Petra", pin: true), ralf };
      List<string?> answers = new List<string?>();
      StartupViewModel wake = Wake(server, answers, splashMs: 600_000);
      _ = wake.StartAsync();

      wake.SwitchUser();
      Assert.That(wake.Switching, Is.True);
      wake.PickOther(ralf);

      Assert.That(wake.Switching, Is.False);
      Assert.That(answers, Is.EquivalentTo(new string?[] { "Ralf" }),
          "a person without numbers who is stood on arrives without being asked for any");
      Assert.That(ClientSettings.Load(null, out _).RememberedViewer, Is.EqualTo("Ralf"),
          "and the next start asks Ralf, not Petra");
    }

    [Test]
    public void SwitchingToAPersonWithNumbersAsksThem()
    {
      Remember("Ralf");
      FakeWakeServer server = new FakeWakeServer();
      UserInfo petra = Person("Petra", pin: true);
      server.People = new[] { petra, Person("Ralf") };
      List<string?> answers = new List<string?>();
      StartupViewModel wake = Wake(server, answers, splashMs: 600_000);

      // Ralf needs no numbers, so the wake is holding its wait - six hundred seconds of it, so the wait
      // can be seen standing there. The switch interrupts it, honestly.
      _ = wake.StartAsync();
      Assert.That(wake.Busy, Is.True);

      wake.SwitchUser();
      wake.PickOther(petra);

      Assert.That(answers, Is.Empty, "the interrupted wait must not open behind the new question");
      Assert.That(wake.NeedsPin, Is.True);
      Assert.That(wake.PersonName, Is.EqualTo("Petra"));

      wake.Press("1");
      wake.Press("2");
      wake.Press("3");
      wake.Press("4");
      Assert.That(server.Asked, Has.Exactly(1).EqualTo(("Petra", "1234")));
      Assert.That(answers, Is.EquivalentTo(new string?[] { "Petra" }));
    }

    [Test]
    public async Task AServerThatCannotBeNamedIsNoWall()
    {
      Remember("Petra");
      List<string?> answers = new List<string?>();
      FakeWakeServer server = new FakeWakeServer();
      StartupViewModel wake = new StartupViewModel(
          delegate { throw new InvalidOperationException("unreachable"); },
          delegate { return Task.FromResult(false); },
          answers.Add,
          TimeSpan.FromMilliseconds(30));

      await wake.StartAsync();

      Assert.That(answers, Is.EquivalentTo(new string?[] { "Petra" }),
          "a server that will not talk is not a lock - the wake holds its seconds and lets the person through,");
      Assert.That(wake.Status, Does.Contain("unreachable"),
          "and says out loud why it could not check the PIN");
    }

    [Test]
    public async Task TheLibraryIsReadUnderTheWakesAnimation()
    {
      // The animation exists so that the machine has something to do while it is on the screen: the library is
      // asked for here, inside the wake's seconds, so that the home screen arrives with its answers instead of
      // filling itself in under the viewer's eyes.
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Petra"), Person("Wohnzimmer", kind: "Client") };
      List<string> order = new List<string>();
      StartupViewModel wake = new StartupViewModel(
          () => server,
          server.Check,
          delegate (string? name) { order.Add("opened"); },
          TimeSpan.FromMilliseconds(300));
      wake.WarmLibraryRequested += delegate { order.Add("read"); };

      await wake.StartAsync();

      Assert.That(order, Is.EqualTo(new[] { "read", "opened" }),
          "the reading is asked for while the animation still has seconds left, and the wake opens after it");
    }

    [Test]
    public async Task AWakeGivenItsNumbersReadsTheLibraryOnceBeforeItOpens()
    {
      // A typed PIN leaves no animation to read under, but the answer is known the moment the server accepts the
      // four numbers, and the reading starts there rather than when the library screen is built. Once only,
      // because a second run behind the first would cancel it and throw its answers away with the menu.
      Remember("Sabine");
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Sabine", pin: true) };
      List<string> order = new List<string>();
      StartupViewModel wake = new StartupViewModel(
          () => server,
          server.Check,
          delegate (string? name) { order.Add("opened"); },
          TimeSpan.FromMilliseconds(30));
      wake.WarmLibraryRequested += delegate { order.Add("read"); };

      await wake.StartAsync();
      Assert.That(order, Is.Empty, "a wake that is still waiting for numbers has read nothing for anybody");

      wake.Press("1");
      wake.Press("2");
      wake.Press("3");
      wake.Press("4");

      Assert.That(order, Is.EqualTo(new[] { "read", "opened" }),
          "the accepted PIN sends the reading out before the wake opens");
      Assert.That(order.FindAll(delegate (string one) { return one == "read"; }).Count, Is.EqualTo(1),
          "and it sends exactly one reading");
    }

    [Test]
    public async Task AWakeWhoseShelfFillsLateHoldsItsScreenForIt()
    {
      // The ring's turn is what the wake waits for, not what it waits until: a library still reading when the
      // turn is done holds the screen, because opening on a half-built wall is the thing the wait exists to stop.
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Petra") };
      List<string?> answers = new List<string?>();
      TaskCompletionSource shelf = new TaskCompletionSource();
      StartupViewModel wake = new StartupViewModel(
          () => server, server.Check, answers.Add, TimeSpan.FromMilliseconds(20), null, null,
          () => shelf.Task, TimeSpan.FromSeconds(30));

      Task running = wake.StartAsync();
      await Task.Delay(120);
      Assert.That(answers, Is.Empty, "its turn is behind it, but the shelves are not full yet");

      shelf.SetResult();
      await running;
      Assert.That(answers, Is.EquivalentTo(new string?[] { "" }),
          "the shelf being full is what opens the wake, not its own seconds running out");
    }

    [Test]
    public async Task AWakeIsNotWeldedShutByAShelfThatStaysEmpty()
    {
      // A server that will not answer is a complaint on the library screen, not a television parked on its wake
      // forever. Past its patience the wake answers whatever the shelves look like.
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Petra") };
      List<string?> answers = new List<string?>();
      StartupViewModel wake = new StartupViewModel(
          () => server, server.Check, answers.Add, TimeSpan.FromMilliseconds(20), null, null,
          delegate { return new TaskCompletionSource().Task; }, TimeSpan.FromMilliseconds(120));

      Task running = wake.StartAsync();
      Task finished = await Task.WhenAny(running, Task.Delay(TimeSpan.FromSeconds(5)));

      Assert.That(finished, Is.SameAs(running), "the wake answered rather than waiting on a silent server");
      Assert.That(answers, Has.Count.EqualTo(1));
    }

    [Test]
    public void TheWakesOwnSecondsAreTheRingsTurn()
    {
      // The second every viewer counts: the wake's wait used to be four seconds of its own, so the ring faded
      // out and the wallpaper stood there another second and a half for nothing. Its floor is the animation now
      // - the turn and the way out - and the reading is what may lengthen it, never a number on a stopwatch.
      Assert.That(StartupViewModel.WAKE_SECONDS,
          Is.EqualTo(TimeSpan.FromSeconds(StartupViewModel.PASS_SECONDS + StartupViewModel.EXIT_SECONDS)));
      Assert.That(StartupViewModel.WAKE_SECONDS, Is.LessThan(TimeSpan.FromSeconds(3)),
          "a wake that outlasts its own animation is waiting for nothing");
    }
}
}
