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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The house rules: what a signed-in person may reach, what the server's five policy fields do to the
  /// wake, and what the head line says about who is watching. The reference kept these in
  /// <c>IUserManagement</c> and <c>UserProfile.CheckUserAccess</c>; here they are the viewer state, the
  /// group registry and the wake's policy read - and every promise the reference made about them is one of
  /// these tests.
  /// </summary>
  [TestFixture]
  public class TestUserRules
  {
    private string _file = "";
    private string _previousClientFile = "";

    [SetUp]
    public void SetUp()
    {
      _file = Path.Combine(Path.GetTempPath(), "mp2-userrules-test-" + Guid.NewGuid().ToString("N") + ".json");
      _previousClientFile = Environment.GetEnvironmentVariable("MP2_CLIENT_FILE") ?? "";
      Environment.SetEnvironmentVariable("MP2_CLIENT_FILE", _file);
    }

    [TearDown]
    public void TearDown()
    {
      Environment.SetEnvironmentVariable("MP2_CLIENT_FILE", _previousClientFile.Length == 0 ? null : _previousClientFile);
      SkinSettingsBase.FileOwner = null;
      if (!File.Exists(_file))
        return;

      // A save that was still on its way out of the application holds the file for a moment after the screen that
      // asked for it is through, and Windows calls that "used by another process". The run has no more use for the
      // file either way, so the clean-up waits out the handle rather than reporting a failure of the screen.
      for (int attempt = 0; attempt < 50; attempt++)
      {
        try
        {
          File.Delete(_file);
          return;
        }
        catch (IOException)
        {
          Thread.Sleep(20);
        }
      }
    }

    private static UserInfo Person(string name, bool pin = false)
    {
      return new UserInfo { ProfileId = Guid.NewGuid(), Name = name, Kind = "User", HasPassword = pin };
    }

    /// <summary>A PNG one pixel across, written out rather than generated: the picture a profile carries.</summary>
    private const string ONE_PIXEL_PNG =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFAAH/q842iQAAAABJRU5ErkJggg==";

    private static async Task WaitUntil(Func<bool> done)
    {
      for (int i = 0; i < 200 && !done(); i++)
        await Task.Delay(10);
      Assert.That(done(), Is.True, "the answer never arrived");
    }

    /// <summary>A policy answer the test writes down, standing in for the server's five house rules.</summary>
    private sealed class FakePolicyApi : IServerSettingsApi
    {
      public LoginPolicyAnswer Policy = new LoginPolicyAnswer();
      public LoginPolicyWish? LastWish { get; private set; }
      public int Puts { get; private set; }

      public Task<ServerSettingsAnswer> GetServerSettingsAsync(CancellationToken cancellation = default)
          => throw new NotSupportedException();

      public Task<ServerSettingsAnswer> PutServerSettingsAsync(ServerSettingsUpdate update,
          CancellationToken cancellation = default)
          => throw new NotSupportedException();

      public Task<LoginPolicyAnswer> GetLoginPolicyAsync(CancellationToken cancellation = default)
          => Task.FromResult(Policy);

      public Task<LoginPolicyAnswer> PutLoginPolicyAsync(LoginPolicyWish wish,
          CancellationToken cancellation = default)
      {
        LastWish = wish;
        Puts++;
        return Task.FromResult(new LoginPolicyAnswer
        {
          EnableUserLogin = wish.EnableUserLogin ?? Policy.EnableUserLogin,
          EnableUserLoginScreen = wish.EnableUserLoginScreen ?? Policy.EnableUserLoginScreen,
          AutoLoginUser = wish.AutoLoginUser ?? Policy.AutoLoginUser,
          AutoLogoutEnabled = wish.AutoLogoutEnabled ?? Policy.AutoLogoutEnabled,
          AutoLogoutIdleTimeoutInMin = wish.AutoLogoutIdleTimeoutInMin ?? Policy.AutoLogoutIdleTimeoutInMin
        });
      }
    }

    /// <summary>The wake's fake: the people it names and the sign-ins it was asked to check.</summary>
    private sealed class FakeWakeServer : IUsersApi
    {
      public UserInfo[] People = Array.Empty<UserInfo>();
      public readonly List<(string Name, string Pin)> Asked = new List<(string, string)>();

      public Task<UsersAnswer> GetUsersAsync(CancellationToken cancellation = default)
      {
        if (StandsStill)
          throw new InvalidOperationException("the server stands still");
        return Task.FromResult(new UsersAnswer { Users = People });
      }

      /// <summary>Set when this server cannot be reached at all - the door is locked and the key is elsewhere.</summary>
      public bool StandsStill;

      public Task<UserAnswer> CreateUserAsync(CreateUserWish wish, CancellationToken cancellation = default)
          => throw new NotSupportedException();

      public Task<UserAnswer> UpdateUserAsync(Guid profileId, UpdateUserWish wish,
          CancellationToken cancellation = default)
          => throw new NotSupportedException();

      public Task<UserAnswer> DeleteUserAsync(Guid profileId, CancellationToken cancellation = default)
          => throw new NotSupportedException();

      public bool Accepts = true;

      public Task<bool> Check(string name, string pin)
      {
        Asked.Add((name, pin));
        return Task.FromResult(Accepts);
      }
    }

    // ---- what a signed-in person may reach ---------------------------------------------------------------

    [Test]
    public void AnElementThatNamesNoGroupBelongsToEverybody()
    {
      Assert.That(RestrictionGroups.Allows(true, "", null), Is.True);
      Assert.That(RestrictionGroups.Allows(true, "", ""), Is.True,
          "the reference's first rule: an element without a group is everybody's");
    }

    [Test]
    public void RestrictionsOffAndGetThereAndAnEmptyTelevisionReachEverything()
    {
      Assert.That(RestrictionGroups.Allows(false, "", RestrictionGroups.Settings), Is.True,
          "restrictions switched off means off, not secretly on");

      ViewerState television = new ViewerState();
      Assert.That(television.Allows(RestrictionGroups.Settings), Is.True,
          "with nobody signed in there is no person to be strict about");
    }

    [Test]
    public void AChildSeesOnlyWhatItsCarries()
    {
      Assert.That(RestrictionGroups.Allows(true, "Settings.UserProfile, TV: Group: Kids",
          RestrictionGroups.Settings), Is.False, "the machine room stays shut");
      Assert.That(RestrictionGroups.Allows(true, "Settings.UserProfile, TV: Group: Kids",
          "tv: group: kids"), Is.True, "the name is the name, whatever the case was written in");
      Assert.That(RestrictionGroups.Allows(true, "Settings", "Settings, " + RestrictionGroups.DeleteRecording),
          Is.True, "one of the element's groups is enough, exactly as the reference checked it");
    }

    [Test]
    public void TheOfferedGroupsAreTheOnesSomebodyHereObeys()
    {
      IList<RestrictionGroupInfo> catalog = RestrictionGroups.Catalog(new[] { "Kids", " Sports ", "" });
      Assert.That(catalog.Select(g => g.Name),
          Contains.Item(RestrictionGroups.Settings));
      Assert.That(catalog.Select(g => g.Name), Contains.Item("TV: Group: Sports"),
          "a channel group the television actually looks at joins the registry, trimmed");
      Assert.That(catalog.Any(g => g.Name.Contains("\"\"")), Is.False, "an empty name registers nothing");
    }

    [Test]
    public void TheViewerStateSaysARealMoveAndOnlyARealMove()
    {
      ViewerState television = new ViewerState();
      List<UserInfo?> moves = new List<UserInfo?>();
      television.ViewerChanged += moves.Add;

      UserInfo gerrit = Person("Gerrit");
      television.SetViewer(gerrit);
      television.SetViewer(new UserInfo { ProfileId = gerrit.ProfileId, Name = "Gerrit", Kind = "User" });
      Assert.That(moves, Has.Count.EqualTo(1), "the same person again is not a move, and the second start "
          + "reads them from the server afresh - an event per copy would say the room changed when it did not");

      television.SetViewer(null);
      Assert.That(moves, Has.Count.EqualTo(2));
      Assert.That(moves[1], Is.Null, "a sign-out is said as nobody, not kept quiet");
    }

    [Test]
    public void AnAgeCeilingCountsOnlyWhenItIsSwitchedOn()
    {
      ViewerState television = new ViewerState();
      television.SetViewer(new UserInfo { ProfileId = Guid.NewGuid(), Name = "Kim", RestrictAges = false, AllowedAge = 6 });
      Assert.That(television.AllowedAge, Is.Null, "a written-down ceiling behind a switch that is off is no ceiling");

      television.SetViewer(new UserInfo { ProfileId = Guid.NewGuid(), Name = "Kim", RestrictAges = true, AllowedAge = 6 });
      Assert.That(television.AllowedAge, Is.EqualTo(6));
    }

    // ---- what the five house rules do to the wake ----------------------------------------------------------

    private static StartupViewModel Wake(FakeWakeServer server, FakePolicyApi? policy,
        List<string?> answers, ViewerState television, bool switching = false)
    {
      StartupViewModel wake = new StartupViewModel(
          () => server,
          server.Check,
          answers.Add,
          TimeSpan.FromMilliseconds(30),
          policy == null ? null : new Func<IServerSettingsApi>(() => policy),
          television);
      wake.ForSwitching = switching;
      return wake;
    }

    private static void Remember(string name)
    {
      Remember(name, false);
    }

    private static void Remember(string name, bool hasPin)
    {
      ClientSettings settings = ClientSettings.Load(null, out _);
      settings.RememberedViewer = name;
      settings.RememberedViewerHasPin = hasPin;
      settings.Save(null, out _);
    }

    [Test]
    public async Task AHouseThatWantsNoNamesAsksNobodyEvenWhenItsPersonCarriesAPin()
    {
      Remember("Gerrit");
      FakeWakeServer server = new FakeWakeServer();
      UserInfo gerrit = Person("Gerrit", pin: true);
      server.People = new[] { gerrit };
      FakePolicyApi policy = new FakePolicyApi();
      policy.Policy = new LoginPolicyAnswer { EnableUserLogin = false };
      ViewerState television = new ViewerState();
      List<string?> answers = new List<string?>();

      await Wake(server, policy, answers, television).StartAsync();

      Assert.That(answers, Is.EqualTo(new[] { "" }), "no login means no person, and no person means no PIN");
      Assert.That(television.Viewer, Is.Null);
    }

    [Test]
    public async Task TheExpectedPersonWalksRightIn_ButStillSaysTheirNameAtTheDoor()
    {
      FakeWakeServer server = new FakeWakeServer();
      UserInfo martina = Person("Martina");
      server.People = new[] { Person("Gerrit", pin: true), martina };
      FakePolicyApi policy = new FakePolicyApi();
      policy.Policy = new LoginPolicyAnswer { AutoLoginUser = martina.ProfileId.ToString() };
      ViewerState television = new ViewerState();
      List<string?> answers = new List<string?>();

      await Wake(server, policy, answers, television).StartAsync();

      Assert.That(answers, Is.EqualTo(new[] { "Martina" }), "the expected person is not asked");
      Assert.That(server.Asked, Has.Some.EqualTo(("Martina", "")),
          "a PIN-less walk-in still signs in - the sign-in is what tells the server WHO is asking, and the "
          + "person's shelf of half-watched titles hangs on that answer");
      Assert.That(television.Viewer?.Name, Is.EqualTo("Martina"));
      Assert.That(ClientSettings.Load(null, out _).RememberedViewer, Is.EqualTo("Martina"));
    }

    [Test]
    public async Task AnExpectedPersonWithNumbersStillGetsTheQuestion()
    {
      FakeWakeServer server = new FakeWakeServer();
      UserInfo gerrit = Person("Gerrit", pin: true);
      server.People = new[] { gerrit };
      FakePolicyApi policy = new FakePolicyApi();
      policy.Policy = new LoginPolicyAnswer { AutoLoginUser = gerrit.ProfileId.ToString() };
      ViewerState television = new ViewerState();
      List<string?> answers = new List<string?>();

      StartupViewModel wake = Wake(server, policy, answers, television);
      await wake.StartAsync();

      Assert.That(answers, Has.Count.EqualTo(0), "a PIN nobody typed is a PIN nobody checked");
      Assert.That(wake.NeedsPin, Is.True);
      Assert.That(wake.PersonName, Is.EqualTo("Gerrit"));
    }

    [Test]
    public async Task AScreenSwitchedOffKeepsItsQuestion_ButWalksNobodyInBlind()
    {
      Remember("Gerrit");
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Gerrit", pin: true) };
      FakePolicyApi policy = new FakePolicyApi();
      policy.Policy = new LoginPolicyAnswer { EnableUserLogin = true, EnableUserLoginScreen = false };
      ViewerState television = new ViewerState();
      List<string?> answers = new List<string?>();

      await Wake(server, policy, answers, television).StartAsync();

      Assert.That(answers, Is.EqualTo(new[] { "" }),
          "no screen and no expected person means the television stays the household's - signing a PIN "
          + "person in behind a switched-off question would check nothing and hide their rules");
      Assert.That(television.Viewer, Is.Null);
    }

    [Test]
    public async Task AServerThatWillNotSayItsRulesLeavesTheWakeAsItWas()
    {
      Remember("Gerrit");
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Gerrit", pin: true) };
      ViewerState television = new ViewerState();
      List<string?> answers = new List<string?>();

      StartupViewModel wake = Wake(server, null, answers, television);
      await wake.StartAsync();

      Assert.That(wake.NeedsPin, Is.True, "an unreachable server welds no door shut and unlocks nothing");
    }

    [Test]
    public async Task AStilleServerKeepsTheNumberedDoorQuestioned_NotUnlocked()
    {
      Remember("Gerrit", hasPin: true);
      FakeWakeServer server = new FakeWakeServer();
      server.StandsStill = true;
      ViewerState television = new ViewerState();
      List<string?> answers = new List<string?>();

      StartupViewModel wake = Wake(server, null, answers, television);
      await wake.StartAsync();

      Assert.That(wake.NeedsPin, Is.True,
          "a remembered person with numbers is not walked in unchecked because the server is silent - "
          + "that was the door the whole wake exists for");
      Assert.That(wake.PersonName, Is.EqualTo("Gerrit"));
      Assert.That(wake.Status, Does.Contain("could not be reached"),
          "the screen says honestly that nothing can be checked right now");
      Assert.That(answers, Is.Empty, "the wake keeps its question, it does not answer it");
      Assert.That(television.Viewer, Is.Null);
    }

    [Test]
    public async Task APersonWithoutNumbersStillWalksIn_AgainstAStilleServer()
    {
      Remember("Petra", hasPin: false);
      FakeWakeServer server = new FakeWakeServer();
      server.StandsStill = true;
      ViewerState television = new ViewerState();
      List<string?> answers = new List<string?>();

      await Wake(server, null, answers, television).StartAsync();

      Assert.That(answers, Is.EqualTo(new[] { "Petra" }),
          "there is nothing to check about a PIN-less person whether the server speaks or not");
      Assert.That(ClientSettings.Load(null, out _).RememberedViewer, Is.EqualTo("Petra"));
    }

    [Test]
    public async Task TheWakeLearnsFromTheServerWhetherTheDoorCarriesNumbers()
    {
      Remember("Gerrit");
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { Person("Gerrit", pin: true) };
      ViewerState television = new ViewerState();
      List<string?> answers = new List<string?>();

      await Wake(server, new FakePolicyApi(), answers, television).StartAsync();

      Assert.That(ClientSettings.Load(null, out _).RememberedViewerHasPin, Is.True,
          "what the server just said about the remembered person is what the machine knows for the next "
          + "start - possibly against a silent server");
    }

    [Test]
    public async Task TheRightNumbersTellTheMachineTheDoorCarriesNumbers()
    {
      FakeWakeServer server = new FakeWakeServer();
      UserInfo gerrit = Person("Gerrit", pin: true);
      server.People = new[] { gerrit };
      ViewerState television = new ViewerState();
      List<string?> answers = new List<string?>();

      StartupViewModel wake = Wake(server, new FakePolicyApi(), answers, television);
      await wake.StartAsync();
      wake.Press("1");
      wake.Press("2");
      wake.Press("3");
      wake.Press("4");
      await WaitUntil(delegate { return answers.Count > 0; });

      ClientSettings after = ClientSettings.Load(null, out _);
      Assert.That(after.RememberedViewer, Is.EqualTo("Gerrit"));
      Assert.That(after.RememberedViewerHasPin, Is.True,
          "the person who just typed four numbers is a person the next silent start must not walk in");
    }

    // ---- the switch, opened by the home's person button --------------------------------------------------

    [Test]
    public async Task TheSwitchIsTheStartPageAndAsksThePersonStandingThere()
    {
      UserInfo gerrit = Person("Gerrit", pin: true);
      ViewerState television = new ViewerState();
      television.SetViewer(gerrit);
      Remember("Gerrit");

      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { gerrit, Person("Martina") };
      List<string?> answers = new List<string?>();

      StartupViewModel wake = Wake(server, new FakePolicyApi(), answers, television, switching: true);
      await wake.StartAsync();

      Assert.That(wake.NeedsPin, Is.True,
          "the person button guides to the start page, which asks the numbers of whoever stands there - "
          + "it is not a page of its own with the household printed on it");
      Assert.That(wake.Switching, Is.False);
      Assert.That(wake.PersonName, Is.EqualTo("Gerrit"));

      // The household is the one press of the round picture away, exactly as on the start page.
      wake.SwitchUserCommand.Execute(null);
      Assert.That(wake.Switching, Is.True);
      Assert.That(wake.Others.Select(o => o.Name), Is.EqualTo(new[] { "Martina" }));

      wake.StayAsWasCommand.Execute(null);
      Assert.That(answers, Is.EqualTo(new[] { "Gerrit" }),
          "staying is an answer, and the answer steps back over the library");
    }

    [Test]
    public async Task ASwitchWithNobodyToAskLaysTheHouseholdOut()
    {
      UserInfo gerrit = Person("Gerrit");
      ViewerState television = new ViewerState();
      television.SetViewer(gerrit);
      Remember("Gerrit");

      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { gerrit, Person("Martina") };
      List<string?> answers = new List<string?>();

      StartupViewModel wake = Wake(server, new FakePolicyApi(), answers, television, switching: true);
      await wake.StartAsync();

      Assert.That(wake.Switching, Is.True,
          "a person who wears no numbers has no question to answer, so the switch shows the household - "
          + "a face with no question and no way to anybody else would be a dead end");
      Assert.That(wake.NeedsPin, Is.False);
      Assert.That(wake.Others.Select(o => o.Name), Is.EqualTo(new[] { "Martina" }));

      wake.StayAsWasCommand.Execute(null);
      Assert.That(answers, Is.EqualTo(new[] { "Gerrit" }));
      Assert.That(wake.SignedOut, Is.False);
      Assert.That(television.Viewer, Is.SameAs(gerrit));
    }

    [Test]
    public async Task TheWayOutOfASwitchIsReachableEvenWhenNobodyElseIsInHouse()
    {
      UserInfo gerrit = Person("Gerrit", pin: true);
      ViewerState television = new ViewerState();
      television.SetViewer(gerrit);
      Remember("Gerrit");

      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { gerrit };
      List<string?> answers = new List<string?>();

      StartupViewModel wake = Wake(server, new FakePolicyApi(), answers, television, switching: true);
      await wake.StartAsync();

      wake.SwitchUserCommand.Execute(null);
      Assert.That(wake.Switching, Is.True,
          "the wall of faces opens even when the house holds a single person, because that person has to be "
          + "able to answer the question by staying - otherwise the person button has no way out");
      Assert.That(wake.Household.Select(f => f.Name), Is.EqualTo(new[] { "Gerrit" }));

      wake.StayAsWasCommand.Execute(null);
      Assert.That(answers, Is.EqualTo(new[] { "Gerrit" }),
          "the back key is the way out of a wall that carries no buttons of its own");
    }

    [Test]
    public async Task TheWallCarriesEveryFaceInTheHouseholdAndThePictureTheyWear()
    {
      UserInfo gerrit = Person("Gerrit", pin: true);
      gerrit.Image = ONE_PIXEL_PNG;
      UserInfo martina = Person("Martina");
      ViewerState television = new ViewerState();
      television.SetViewer(gerrit);
      Remember("Gerrit");

      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { gerrit, martina };
      List<string?> answers = new List<string?>();

      StartupViewModel wake = Wake(server, new FakePolicyApi(), answers, television, switching: true);
      await wake.StartAsync();
      wake.SwitchUserCommand.Execute(null);

      Assert.That(wake.Household.Select(f => f.Name), Is.EqualTo(new[] { "Gerrit", "Martina" }),
          "the wall is the whole household: the person standing there stands on it too, so that a household "
          + "can be answered by staying and not only by changing");
      Assert.That(wake.Household[0].Person.Image, Is.EqualTo(ONE_PIXEL_PNG),
          "a face carries the picture the household gave that person along with the name, which is all the "
          + "dialog needs to paint the round container with it - the toolkit decodes the bytes, and this "
          + "headless run has no decoder, so the bytes are what a test can hold on to");
      Assert.That(wake.Household[1].HasPicture, Is.False);
      Assert.That(wake.Household[1].Initials, Is.EqualTo("M"),
          "a face without a picture wears the name's first letter, not nothing");
    }

    [Test]
    public async Task APersonWhoPressesTheirOwnFaceStaysStandingThere()
    {
      UserInfo gerrit = Person("Gerrit", pin: true);
      UserInfo martina = Person("Martina");
      ViewerState television = new ViewerState();
      television.SetViewer(gerrit);
      Remember("Gerrit");

      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { gerrit, martina };
      List<string?> answers = new List<string?>();

      StartupViewModel wake = Wake(server, new FakePolicyApi(), answers, television, switching: true);
      await wake.StartAsync();
      wake.SwitchUserCommand.Execute(null);

      wake.PickFaceCommand.Execute(wake.Household.First(f => f.Name == "Gerrit"));

      Assert.That(answers, Is.EqualTo(new[] { "Gerrit" }));
      Assert.That(wake.Switching, Is.False, "a pressed face steps the wall aside");
      Assert.That(server.Asked, Is.Empty,
          "pressing one's own face is the answer 'stay as before', not a request to be asked for one's own "
          + "numbers a second time");
    }

    [Test]
    public async Task APressOnAnotherFaceOnTheWallPutsThatPersonAtTheQuestion()
    {
      UserInfo gerrit = Person("Gerrit", pin: true);
      UserInfo martina = Person("Martina", pin: true);
      martina.Image = ONE_PIXEL_PNG;
      ViewerState television = new ViewerState();
      television.SetViewer(gerrit);
      Remember("Gerrit");

      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { gerrit, martina };
      List<string?> answers = new List<string?>();

      StartupViewModel wake = Wake(server, new FakePolicyApi(), answers, television, switching: true);
      await wake.StartAsync();
      wake.SwitchUserCommand.Execute(null);
      wake.PickFaceCommand.Execute(wake.Household.First(f => f.Name == "Martina"));

      Assert.That(wake.Switching, Is.False);
      Assert.That(wake.NeedsPin, Is.True);
      Assert.That(wake.PersonName, Is.EqualTo("Martina"));
      Assert.That(wake.AskedPerson, Is.SameAs(martina),
          "the question is asked of the picked person, and the round container's picture comes off that "
          + "person - a wake that carried only the name would paint a stranger's initial over their face");
      Assert.That(wake.AskedPerson?.Image, Is.EqualTo(ONE_PIXEL_PNG));
    }

    [Test]
    public async Task LettingTheTelevisionGoAnswersNobodyAndSaysIt()
    {
      UserInfo gerrit = Person("Gerrit");
      ViewerState television = new ViewerState();
      television.SetViewer(gerrit);
      Remember("Gerrit");

      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { gerrit };
      List<string?> answers = new List<string?>();

      StartupViewModel wake = Wake(server, new FakePolicyApi(), answers, television, switching: true);
      await wake.StartAsync();
      wake.SignOutCommand.Execute(null);

      Assert.That(wake.SignedOut, Is.True, "the application must tell a sign-out from a stay");
      Assert.That(answers, Is.EqualTo(new[] { "" }));
      Assert.That(television.Viewer, Is.Null);
    }

    [Test]
    public async Task TheRightNumbersHandTheTelevisionToThePersonTheyBelongTo()
    {
      UserInfo gerrit = Person("Gerrit", pin: true);
      FakeWakeServer server = new FakeWakeServer();
      server.People = new[] { gerrit };
      ViewerState television = new ViewerState();
      List<string?> answers = new List<string?>();

      StartupViewModel wake = Wake(server, new FakePolicyApi(), answers, television);
      await wake.StartAsync();
      Assert.That(wake.NeedsPin, Is.True);

      wake.Press("1");
      wake.Press("2");
      wake.Press("3");
      wake.Press("4");
      await WaitUntil(delegate { return answers.Count > 0; });

      Assert.That(answers, Is.EqualTo(new[] { "Gerrit" }));
      Assert.That(television.Viewer?.Name, Is.EqualTo("Gerrit"),
          "the head line's picture, the obeys-restrictions answers and the token all read the person "
          + "the PIN just checked - so the wake has to hand them over, not only tell the application");
    }

    // ---- the door to the household ---------------------------------------------------------------------
    //
    // The door is the home screen's own corner button - the person glyph the reference hung in
    // master_home's top row - not a picture in the head line. Its one rule is the reference's rule for
    // that button, EnableUserLogin: it stands in a house that asks who is watching and it is taken away
    // in one that does not. What stands behind the door - the switch laying out the household, the PIN,
    // the stay and the sign-out - is the wake, tested above as the wake.

    [Test]
    public void AHouseThatAsksNobodyTakesThePersonButtonAway()
    {
      // The gate's whole answer is the policy's flag, read afresh at every lay-out and every press:
      // login on is the button, login off is no button - there is nobody to switch to when nobody is asked.
      ViewerState television = new ViewerState();
      Assert.That(television.Policy.EnableUserLogin, Is.True,
          "a house the server has said nothing about is asked for names until it says otherwise");
      television.Policy = new LoginPolicyAnswer { EnableUserLogin = false };
      Assert.That(television.Policy.EnableUserLogin, Is.False);
      television.Policy = new LoginPolicyAnswer { EnableUserLogin = true };
      Assert.That(television.Policy.EnableUserLogin, Is.True,
          "and the button comes back the moment the house asks again - no restart involved");
    }

    [Test]
    public void APictureThatIsNoPictureIsNoCrash()
    {
      Assert.That(UserPicture.FromBase64(null), Is.Null);
      Assert.That(UserPicture.FromBase64(""), Is.Null);
      Assert.That(UserPicture.FromBase64("this is not base64 at all!"), Is.Null,
          "a half-written base64 is a missing picture, not a broken head line");
    }

    // ---- the house rules at the users screen -----------------------------------------------------------------

    [Test]
    public async Task TheUsersScreenReadsTheHouseRulesOutOfTheServer()
    {
      FakePolicyApi policy = new FakePolicyApi();
      policy.Policy = new LoginPolicyAnswer
      {
        EnableUserLogin = true,
        EnableUserLoginScreen = false,
        AutoLogoutEnabled = true,
        AutoLogoutIdleTimeoutInMin = 12
      };
      ViewerState television = new ViewerState();
      UsersSettingsViewModel screen = new UsersSettingsViewModel(() => null, () => policy, television);

      await screen.LoadPolicyAsync();

      Assert.That(screen.HasPolicy, Is.True);
      Assert.That(screen.AskLoginChecked, Is.True);
      Assert.That(screen.ShowLoginScreenChecked, Is.False);
      Assert.That(screen.AutoLogoutChecked, Is.True);
      Assert.That(screen.TimeoutText, Is.EqualTo("12"));
      Assert.That(television.Policy.AutoLogoutIdleTimeoutInMin, Is.EqualTo(12),
          "the wake and the idle watch read the rules off the viewer state, so the screen has to leave "
          + "what it just read there");
    }

    [Test]
    public async Task ANonsensicalIdleTimeoutIsRefusedBeforeTheServerHearsIt()
    {
      FakePolicyApi policy = new FakePolicyApi();
      UsersSettingsViewModel screen = new UsersSettingsViewModel(() => null, () => policy, new ViewerState());
      await screen.LoadPolicyAsync();

      screen.TimeoutText = "once upon a time";
      await screen.SavePolicyAsync();
      Assert.That(policy.Puts, Is.EqualTo(0), "a number the server would refuse never reaches the server");
      Assert.That(screen.Status, Does.Contain("whole number"));

      screen.TimeoutText = "15";
      screen.AskLoginChecked = false;
      await screen.SavePolicyAsync();
      Assert.That(policy.Puts, Is.EqualTo(1));
      Assert.That(policy.LastWish!.EnableUserLogin, Is.False);
      Assert.That(policy.LastWish!.AutoLoginUser, Is.EqualTo(Guid.Empty.ToString()),
          "\"nobody\" is said as the empty Guid, which is what the server treats as nobody");
      Assert.That(screen.Status, Does.Contain("kept"));
    }
  }
}
