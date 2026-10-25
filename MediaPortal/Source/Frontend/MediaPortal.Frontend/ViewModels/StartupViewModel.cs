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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The start of the front end, told as the household experiences it: the wallpaper appears, and either the
  /// machine simply wakes - a short animation while the last person is read, and then the library - or the last
  /// person is asked for their PIN. Four numbers, one box each; the fourth finishes the question, right or
  /// wrong. A wrong one shakes the field, empties it and asks again as often as it takes.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>The PIN is the server's answer, not this machine's.</b> The people are the ones <c>api/users</c> keeps,
  /// their PINs hashed in the server's store, and the question is checked by <c>SignInAsync</c> exactly like a
  /// password always was - which means the same PIN opens the same person on every television in the house, and
  /// there is no second list of secrets to keep in step. A person whose server entry carries no password is a
  /// person who needs no PIN, and their start is the animation and the library.
  /// </para>
  /// <para>
  /// <b>Why this is a screen and not a dialog over home.</b> The reference woke its viewers through a login
  /// screen too; what it asked was which person, and what this one asks is which person plus their four
  /// numbers. The frame sits over the library until it is answered - the back key is swallowed while it stands,
  /// because a lock you can press a key past is decoration - and its answer is handed to the application's
  /// <c>ApplyViewer</c>, the same receiver the "who is watching" pick always used: the name owns the skin's
  /// answers, is remembered for the next start, and the library underneath is what the viewer then walks into.
  /// </para>
  /// <para>
  /// <b>What the wait is for.</b> A start that must wait for the server has to show something while it waits,
  /// and showing the wallpaper alone reads as a hung program once more than a heartbeat passes. So every
  /// unanswered second of this screen wears the animation - the read of the user list included - and the no-PIN
  /// case stays on it for the animation's own turn, <see cref="WAKE_SECONDS"/>, and no longer: the ring is what
  /// the reading costs the viewer, and a wake that kept its wallpaper up a second past its own turn would only
  /// be a second lost. The one thing that can hold it past the turn is a library that has not answered yet, and
  /// even that only up to <see cref="ReadPatience"/> - a server that will not speak must not weld the wake shut.
  /// The timer is a field rather than <c>Task.Delay</c> so a test can ask for a wait of milliseconds and the
  /// application can keep the one its household asked for.
  /// </para>
  /// </remarks>
  public partial class StartupViewModel : ObservableObject
  {
    /// <summary>Asks the server whether these four numbers belong to this name. True means signed in.</summary>
    public delegate Task<bool> PinChecker(string name, string pin);

    /// <summary>
    /// One turn of the ring, in the screen's own seconds: 360° at the wake's 150° a second. The number lives
    /// here because the wait below and the ring on the screen have to mean one and the same turn of one and
    /// the same clock - a wake that held its wallpaper up past the ring's way out would only be waiting for
    /// nothing, which is exactly the second every viewer counts.
    /// </summary>
    public const double PASS_SECONDS = 2.4;

    /// <summary>The ring's way out: the third of a second it fades for before the panel is collapsed.</summary>
    public const double EXIT_SECONDS = 0.32;

    /// <summary>
    /// How long the wake wears its animation: the ring's turn and its way out, so that the library takes the
    /// window in the beat after the ring has left it. Nothing in this is padding - what the seconds are paying
    /// for is the reading of the library, which the wake itself asks for (see
    /// <see cref="WarmLibraryRequested"/>) - and a wait this length is a floor, not a schedule: a reading still
    /// out when the turn is done holds the wake to itself, up to <see cref="ReadPatience"/>.
    /// </summary>
    public static readonly TimeSpan WAKE_SECONDS = TimeSpan.FromSeconds(PASS_SECONDS + EXIT_SECONDS);

    /// <summary>
    /// How much longer than its own turn the wake waits for a library that has not answered, and no longer.
    /// Past this the answer is given whatever the shelves look like: a library screen that complains about its
    /// server is a worse-looking screen than a wake that never steps aside.
    /// </summary>
    public static readonly TimeSpan ReadPatience = TimeSpan.FromSeconds(8);

    private readonly Func<IUsersApi?> _usersProvider;
    private readonly Func<IServerSettingsApi?>? _policyProvider;
    private readonly Theming.ViewerState? _viewerState;
    private readonly PinChecker _check;
    private readonly TimeSpan _splashWait;
    private readonly TimeSpan _readPatience;

    /// <summary>
    /// Where the application keeps the answer to "has the library finished reading under the eyes this wake is
    /// about to hand the television to". Null means nobody owes this wake a reading - a test, or a wake opened
    /// for switching over a library that stands already - and then the wake's turn is simply its own.
    /// </summary>
    private readonly Func<Task>? _libraryReady;

    /// <summary>
    /// The one answer, said once - to whoever the application happens to have hung on it. The wake at the
    /// root wants a name to walk into the library under; the same screen opened for switching wants to pop
    /// back over the library with that name, or to let the television go altogether.
    /// </summary>
    public event Action<string?>? Finished;

    /// <summary>
    /// Asked the moment the house knows who is watching, while the wake still holds its seconds: the
    /// application reads the library here, so that the animation is what the reading costs the viewer
    /// instead of a home screen that stands half-built while it fills itself in. Raised once, and only
    /// where there is a wait left to read under - a question that was answered by a typed PIN has no
    /// animation in front of it any more.
    /// </summary>
    public event Action? WarmLibraryRequested;

    private bool _warmAsked;

    /// <summary>The ask, said once however many ways lead to the same answer.</summary>
    private void AskForTheLibrary()
    {
      if (_warmAsked)
        return;
      _warmAsked = true;
      WarmLibraryRequested?.Invoke();
    }

    /// <summary>Every person the server named, the remembered one and the others - the switcher's whole house.</summary>
    private readonly List<UserInfo> _seen = new List<UserInfo>();
    private UserInfo? _rememberedPerson;
    private bool _answered;
    private CancellationTokenSource? _waitCts;

    public StartupViewModel(Func<IUsersApi?> usersProvider, PinChecker check, Action<string?>? finished = null,
        TimeSpan? splashWait = null, Func<IServerSettingsApi?>? policyProvider = null,
        Theming.ViewerState? viewerState = null, Func<Task>? libraryReady = null, TimeSpan? readPatience = null)
    {
      _usersProvider = usersProvider;
      _policyProvider = policyProvider;
      _viewerState = viewerState;
      _check = check;
      _libraryReady = libraryReady;
      _readPatience = readPatience ?? ReadPatience;
      if (finished != null)
        Finished += finished;
      _splashWait = splashWait ?? WAKE_SECONDS;
    }

    /// <summary>
    /// Whether this wake was opened by the home's person button instead of by the machine starting. It is
    /// the same page and shows the same thing - the person standing there and their four numbers - and
    /// only its answer differs: it steps back over the library the way it came, and it offers the press
    /// that lets the television go. A switch with nothing to ask lays the household out instead.
    /// </summary>
    public bool ForSwitching { get; set; }

    /// <summary>The four numbers typed so far. The screen draws it as its boxes; the model only knows digits.</summary>
    public const int PIN_LENGTH = 4;

    /// <summary>The people the server keeps - read at open, machine profiles left out as always.</summary>
    public ObservableCollection<UserInfo> Others { get; } = new ObservableCollection<UserInfo>();

    /// <summary>
    /// One face on the switch dialog: the person, the name they are called, and the picture the household
    /// gave them. The dialog shows nothing beside these, so nothing else lives here.
    /// </summary>
    public sealed class HouseholdFace
    {
      public HouseholdFace(UserInfo person)
      {
        Person = person;
        Name = person.Name;
        Picture = Theming.UserPicture.FromBase64(person.Image);
      }

      public UserInfo Person { get; }
      public string Name { get; }
      public Avalonia.Media.Imaging.Bitmap? Picture { get; }
      public bool HasPicture { get { return Picture != null; } }

      /// <summary>What a face wears until somebody brings a picture of their own.</summary>
      public string Initials
      {
        get { return Name.Length == 0 ? "?" : Name.Substring(0, 1).ToUpperInvariant(); }
      }
    }

    /// <summary>
    /// The whole household as the switch dialog shows it - every face the server keeps, in the order it keeps
    /// them, the one standing there included, since a wall of the household that left somebody out would be
    /// a wall one could not answer by staying.
    /// </summary>
    public ObservableCollection<HouseholdFace> Household { get; } = new ObservableCollection<HouseholdFace>();

    /// <summary>
    /// The word the wall of faces is asked under. The skin's dialog frame reads its heading off the dialog's own
    /// body, so the wall names its question here and the shell's header says the very same word.
    /// </summary>
    public string Heading { get { return "Select User"; } }

    /// <summary>
    /// The reading phase: the wake speaks its words itself, and - when nobody has to be asked - holds the
    /// wallpaper for its seconds. The ring keeps turning after this says false, beside the question; only
    /// the words move into the card.
    /// </summary>
    [ObservableProperty]
    private bool _busy = true;

    /// <summary>The PIN phase: the circle, the name and the four boxes.</summary>
    [ObservableProperty]
    private bool _needsPin;

    /// <summary>The "someone else" overlay. While it stands the PIN boxes wait behind it.</summary>
    [ObservableProperty]
    private bool _switching;

    /// <summary>Whether the fourth digit has left its answer waiting. The boxes hold their shape through it.</summary>
    [ObservableProperty]
    private bool _checking;

    [ObservableProperty]
    private string _personName = "";

    [ObservableProperty]
    private string _pin = "";

    [ObservableProperty]
    private string _status = "";

    /// <summary>Raised when four numbers came back wrong; the screen answers it with the shake.</summary>
    public event Action? PinRejected;

    /// <summary>Whether another person is standing by - the switch button has no reason without one.</summary>
    public bool HasOthers => Others.Count > 0;

    /// <summary>
    /// The person standing at the question, or null when the door has only a name behind it - the server
    /// could not be reached, so nobody could be read. The face and the picture both come off this.
    /// </summary>
    public UserInfo? AskedPerson { get; private set; }

    private Avalonia.Media.Imaging.Bitmap? _personPicture;
    private UserInfo? _pictureFor;

    /// <summary>
    /// The asked person's own picture, decoded once per person rather than once per glance. The household put
    /// it there through the users screen, and the door is where a household looks for it, so the round
    /// container wears it; the initial below is only what a container wears until somebody brings one.
    /// </summary>
    public Avalonia.Media.Imaging.Bitmap? PersonPicture
    {
      get
      {
        if (!ReferenceEquals(_pictureFor, AskedPerson))
        {
          _personPicture = Theming.UserPicture.FromBase64(AskedPerson?.Image);
          _pictureFor = AskedPerson;
        }
        return _personPicture;
      }
    }

    /// <summary>Whether the round container wears a picture; without one it wears the name's initial.</summary>
    public bool HasPersonPicture
    {
      get { return PersonPicture != null; }
    }

    /// <summary>
    /// Puts a person at the question: their name, and their picture when the server carries one. Every road
    /// to the question comes through here, so a face is never forgotten on one of them.
    /// </summary>
    private void AskPerson(UserInfo? who, string name)
    {
      AskedPerson = who;
      PersonName = name;
      OnPropertyChanged(nameof(PersonPicture));
      OnPropertyChanged(nameof(HasPersonPicture));
    }

    /// <summary>The letter the round container wears until a person brings a picture of their own.</summary>
    public string Initials
    {
      get { return PersonName.Length == 0 ? "?" : PersonName.Substring(0, 1).ToUpperInvariant(); }
    }

    /// <summary>One digit of the PIN, or the empty box. The screen calls this once per box.</summary>
    public string DigitAt(int index)
    {
      return index >= 0 && index < Pin.Length ? Pin[index].ToString() : "";
    }

    partial void OnPersonNameChanged(string value)
    {
      OnPropertyChanged(nameof(Initials));
    }

    partial void OnPinChanged(string value)
    {
      for (int i = 0; i < PIN_LENGTH; i++)
        OnPropertyChanged(nameof(DigitAt));
    }


    /// <summary>
    /// The start, run once when the screen opens: read the people, then either the question or the wait.
    /// A server that cannot be reached is not a wall - it is the ordinary no-answer, the wake holds its
    /// seconds and the library says what it thinks of the missing server better than this screen could.
    /// </summary>
    public async Task StartAsync()
    {
      ClientSettings machine = ClientSettings.Load(null, out _);
      string remembered = machine.RememberedViewer;
      bool rememberedNumbered = machine.RememberedViewerHasPin;
      LoginPolicyAnswer? policy = await ReadPolicyAsync();
      (UserInfo? person, bool serverSpoke) = await ReadPersonAsync(remembered);

      if (_answered)
        return;

      // Whatever the server just said about the remembered person is what this machine knows about them;
      // it is written down now so the next start - possibly against a silent server - still knows whether
      // this door carries numbers.
      if (serverSpoke && person != null && person.HasPassword != rememberedNumbered)
      {
        machine.RememberedViewerHasPin = person.HasPassword;
        machine.Save(null, out _);
      }

      // Opened by the home's person button rather than by the machine starting. This is the start page,
      // not a page of its own: the person sitting there is shown exactly as the wake shows them - their
      // face and their four numbers - and the household is the one press of the round picture away that
      // it always was. Only where there is nothing to ask does it open on the household straight away,
      // because a picture with no question behind it and no way to anybody else would be a dead end.
      if (ForSwitching)
      {
        Busy = false;
        UserInfo? sitting = _viewerState?.Viewer ?? person;
        if (sitting != null && sitting.HasPassword)
        {
          AskPerson(sitting, sitting.Name);
          Status = "";
          NeedsPin = true;
        }
        else
        {
          Switching = true;
        }
        return;
      }

      // The house rules, from the server that keeps them for every television:
      // No login - nobody is asked, and the household watches together, exactly as it did before the
      // household had names. The rules about a person are rules about the signed-in one; with no login
      // there is no signed-in one, and the server sees an open request and answers with the whole library.
      if (policy != null && !policy.EnableUserLogin)
      {
        await WaitThenFinishAsync(null);
        return;
      }

      // The person the house expects walks in without being asked - but only a person whose door carries
      // no number. A PIN nobody typed is a PIN nobody checked, and letting a person with numbers through
      // unexamined would also let their restrictions stand unenforced at the server, which is the part
      // that actually protects somebody. So an expected person with numbers still gets the question.
      if (policy != null && Guid.TryParse(policy.AutoLoginUser, out Guid autoId) && autoId != Guid.Empty)
      {
        UserInfo? expected = _seen.FirstOrDefault(one => one.ProfileId == autoId);
        if (expected != null && !expected.HasPassword)
        {
          await WalkInAsync(expected);
          return;
        }
      }

      // A question switched off is not a question - with the screen off and no expected person, the
      // television is the household's and the wake keeps its seconds, nothing more. Walking even a PIN-less
      // remembered person in behind a switched-off interview would invent a viewer the house refused to ask
      // about; the reference left such a television with no current user at all, and so does this.
      bool asks = policy == null || policy.EnableUserLoginScreen;
      if (!asks)
      {
        await WaitThenFinishAsync(null);
        return;
      }

      // The question is asked whenever somebody standing behind four numbers deserves it: the remembered
      // person when they need numbers - and when nobody is remembered yet, or the remembered name walked
      // off the server, the first person who does. A house whose door needs a PIN does not stand open
      // merely because the habit of asking was not formed yet. A remembered person who wears no PIN
      // chose that at the users screen; their way in stays the fast one.
      UserInfo? asked = person != null
          ? (person.HasPassword ? person : null)
          : serverSpoke
              ? Others.FirstOrDefault(delegate(UserInfo one) { return one.HasPassword; })
              : null;
      if (asked != null)
      {
        AskPerson(asked, asked.Name);
        Status = "";
        Busy = false;
        NeedsPin = true;
        return;
      }

      // A door with numbers stays locked while the locksmith is away. Walking a numbered person in
      // unchecked is no lock at all - and this machine remembers whether the person it waits for carries
      // numbers, so the question stands up against a silent server and every answer honestly reports that
      // nothing can be checked. The PIN-less person's fast way in is untouched: there is nothing to check
      // about them either way.
      if (!serverSpoke && remembered.Length > 0 && rememberedNumbered)
      {
        AskPerson(null, remembered);
        Status = "The server could not be reached - " + remembered +
            " cannot be checked in right now.";
        Busy = false;
        NeedsPin = true;
        return;
      }

      // A server that could not be reached cannot check anyone - and a PIN nobody can check is a door
      // welded shut, not a lock. The remembered person walks into a library that will show its own
      // complaint about the missing server; a read that only found the person deleted walks nobody in.
      // Nothing is said on the walk of a house that asks nothing - the wallpaper and the ring are the
      // whole screen then, exactly as the viewer asked for.
      if (person != null && !person.HasPassword && serverSpoke)
      {
        await WalkInAsync(person);
        return;
      }

      string? opens = person?.Name ?? (!serverSpoke && remembered.Length > 0 ? remembered : null);
      await WaitThenFinishAsync(opens);
    }

    /// <summary>
    /// Reads the house rules beside the people they govern. A server that will not say them is answered
    /// with null, which every rule above reads as "as you were": an unreachable server welds no door shut
    /// and unlocks nothing either - it simply leaves the wake with the question it would have asked anyway.
    /// </summary>
    private async Task<LoginPolicyAnswer?> ReadPolicyAsync()
    {
      IServerSettingsApi? api;
      try
      {
        api = _policyProvider?.Invoke();
      }
      catch (Exception)
      {
        return null;
      }
      if (api == null)
        return null;

      try
      {
        LoginPolicyAnswer policy = await api.GetLoginPolicyAsync(CancellationToken.None);
        if (_viewerState != null)
          _viewerState.Policy = policy;
        return policy;
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
     /// The walk of a person whose door has no number: the client still says their name to the sign-in,
     /// because the sign-in is what tells the server <i>who is asking</i> - and their shelf of half-watched
     /// titles, and the age ceiling the server quietly keeps out of their lists, both hang on that answer.
     /// A refusal is swallowed: a server that will not hand a token to its own PIN-less person is a server
     /// that will complain about itself when the library is asked.
     /// </summary>
    private async Task WalkInAsync(UserInfo who)
    {
      try
      {
        await _check(who.Name, "");
      }
      catch (Exception)
      {
        // See above - the walk does not stop over a token this server would not give.
      }
      if (_answered)
        return;
      Remember(who.Name, who.HasPassword);
      await WaitThenFinishAsync(who.Name);
    }

    /// <summary>One more digit. The fourth one is not a digit like the others - it is the answer.</summary>
    public void Press(string digit)
    {
      if (!NeedsPin || Checking || digit.Length != 1 || char.IsWhiteSpace(digit[0]))
        return;
      if (Pin.Length >= PIN_LENGTH)
        return;

      Pin += digit;
      if (Pin.Length == PIN_LENGTH)
        _ = CheckAsync();
    }

    /// <summary>The Del key: one number back. The newest leaves first, which is what undo means here.</summary>
    public void Backspace()
    {
      if (!NeedsPin || Checking || Pin.Length == 0)
        return;
      Pin = Pin.Substring(0, Pin.Length - 1);
    }

    /// <summary>The four numbers went to the server. Right: the person owns the screen now. Wrong: shake, empty, again.</summary>
    private async Task CheckAsync()
    {
        string who = PersonName;
        string numbers = Pin;
        Checking = true;
        bool right = false;
        try
        {
          right = await _check(who, numbers);
        }
        catch (Exception ex)
        {
          Status = "The server could not be reached: " + ex.Message;
        }
        Checking = false;

        if (_answered)
          return;

        Pin = "";
        if (right)
        {
          Remember(who, true);
          // No animation is left to read under after a typed PIN, but the read still starts here rather than
          // when the library screen is built: the answer is known, and the ask costs nothing the viewer waits for.
          AskForTheLibrary();
          Answer(who);
          return;
        }

        Status = "Wrong PIN, try again.";
        PinRejected?.Invoke();
    }

    /// <summary>Opens the "someone else" overlay: the other people, each with their answer waiting behind them.</summary>
    [RelayCommand]
    public void SwitchUser()
    {
      // The overlay carries more than the other names: on a switch it also holds "stay as before" and the
      // press that lets the television go. So a household of one still opens it when the switch asked for
      // it - a person button that led to a sentence instead of a way out would be a dead end - while a
      // wake that has nobody else to offer and nobody signed in says so plainly.
      if (Others.Count == 0 && !ForSwitching)
      {
        Status = "This server keeps no other person.";
        return;
      }
      Switching = true;
    }

    [RelayCommand]
    public void StayAsWas()
    {
      // Opened for the switch, "stay" is an answer: the room keeps who it has and the screen steps aside.
      // Opened by the wake itself, it only closes the someone-else overlay and the question waits behind.
      // Either way the wall of faces goes away - a question that has been answered and keeps laying its faces
      // out would still be asking.
      Switching = false;
      if (ForSwitching)
        Answer(_viewerState?.Viewer?.Name ?? _rememberedPerson?.Name);
    }

    /// <summary>
    /// Set when the room pressed "let this television go" on the switch: the frame's answer is not a new
    /// person but nobody, and the application takes the whole trail of the signed-in person away with it.
    /// </summary>
    public bool SignedOut { get; private set; }

    /// <summary>The sign-out, asked from the switch overlay. The wake takes the window back as its root.</summary>
    [RelayCommand]
    public void SignOut()
    {
      if (!ForSwitching || _answered)
        return;
      SignedOut = true;
      Switching = false;
      Answer(null);
    }

    /// <summary>The other person that was picked. With numbers they get asked; without, they simply arrive.</summary>
    [RelayCommand]
    public void PickOther(UserInfo? who)
    {
      if (who == null)
        return;

      Switching = false;
      _waitCts?.Cancel();

      if (who.HasPassword)
      {
        AskPerson(who, who.Name);
        Pin = "";
        Status = "";
        Busy = false;
        NeedsPin = true;
        return;
      }

      Remember(who.Name, who.HasPassword);
      Answer(who.Name);
    }

    /// <summary>
    /// A face pressed on the switch dialog. The person already sitting there is not a change of seats: their
    /// own face on the wall is the answer "stay as before", not a request to be asked for four numbers again.
    /// </summary>
    [RelayCommand]
    public void PickFace(HouseholdFace? face)
    {
      if (face == null)
        return;

      if (ForSwitching && _viewerState?.Viewer != null
          && string.Equals(_viewerState.Viewer.Name, face.Name, StringComparison.OrdinalIgnoreCase))
      {
        StayAsWas();
        return;
      }

      PickOther(face.Person);
    }

    /// <summary>The remembered name, and the list of the others beside them. The second answer says
    /// whether the server spoke at all - an unreachable server is not a door that welds itself shut.</summary>
    private async Task<(UserInfo? person, bool serverSpoke)> ReadPersonAsync(string remembered)
    {
      bool serverSpoke = false;
      IUsersApi? api;
      try
      {
        api = _usersProvider();
      }
      catch (Exception ex)
      {
        Status = "The server could not be reached: " + ex.Message;
        return (null, false);
      }

      if (api == null)
        return (null, false);

      try
      {
        UsersAnswer answer = await api.GetUsersAsync(CancellationToken.None);
        serverSpoke = true;
        Others.Clear();
        _seen.Clear();
        UserInfo? person = null;
        foreach (UserInfo user in answer.Users ?? Array.Empty<UserInfo>())
        {
          if (user.Kind == "Client")
            continue;
          _seen.Add(user);
          if (string.Equals(user.Name, remembered, StringComparison.OrdinalIgnoreCase))
            person = user;
          else
            Others.Add(user);
        }
        _rememberedPerson = person;
        Household.Clear();
        foreach (UserInfo user in _seen)
          Household.Add(new HouseholdFace(user));
        OnOthersFilled();
        return (person, true);
      }
      catch (Exception ex)
      {
        Status = "The server could not name its people: " + ex.Message;
        return (null, false);
      }
    }

    /// <summary>The wake without a question: the animation, its seconds, then the library.</summary>
    private async Task WaitThenFinishAsync(string? name)
    {
      // The seconds this waits are the animation's, and a library read fits inside them - asked here, after
      // whoever is watching has been signed in and before the wait, so every answer the read gives is already
      // on the shelf when the home screen takes the window.
      AskForTheLibrary();
      CancellationTokenSource cts = _waitCts = new CancellationTokenSource();
      try
      {
        // The ring's turn is a floor - the wake never steps out in the middle of its own animation - but it is
        // not a schedule. What those seconds were holding the wallpaper up for was the reading; once the ring
        // has left the screen and the shelves are full there is nothing left here to wait for, and every extra
        // tenth is one the viewer counts. So a reading that outlasts the turn is waited for, and only up to its
        // patience: a silent server earns a complaint on the library screen, not a television welded to its wake.
        Task turn = Task.Delay(_splashWait, cts.Token);
        Task shelf = Task.CompletedTask;
        try
        {
          shelf = _libraryReady == null ? Task.CompletedTask : _libraryReady();
        }
        catch (Exception)
        {
          // A wake that cannot ask when the shelf is full answers on its own turn instead of not at all.
        }

        if (!shelf.IsCompleted)
          shelf = Task.WhenAny(shelf, Task.Delay(_readPatience));
        await Task.WhenAll(turn, shelf);
      }
      catch (TaskCanceledException)
      {
        return;
      }

      if (_answered || !cts.IsCancellationRequested)
        Answer(name);
    }

    /// <summary>
    /// Writes who the room is watching for - and whether that person's door carries numbers, which the next
    /// start cannot ask a silent server about. So the next start asks the right person and locks the right
    /// way, with the server there or not.
    /// </summary>
    private static void Remember(string name, bool hasPin)
    {
      ClientSettings settings = ClientSettings.Load(null, out _);
      settings.RememberedViewer = name;
      settings.RememberedViewerHasPin = hasPin;
      settings.Save(null, out _);
      Settings.SkinSettingsBase.FileOwner = name;
    }

    /// <summary>The one answer, whoever brought it. Twice is a bug, so the second one never happens.</summary>
    private void Answer(string? name)
    {
      if (_answered)
        return;
      _answered = true;

      // The application's half of the answer is the file that owns the skin's answers; this half is the
      // standing fact of who is watching - their name for the head line's picture, their restrictions for
      // every screen that obeys them, and the token's owner for every question asked of the server.
      if (_viewerState != null)
      {
        UserInfo? person = name == null
            ? null
            : _seen.FirstOrDefault(one => string.Equals(one.Name, name, StringComparison.OrdinalIgnoreCase));
        if (name == null || person != null)
          _viewerState.SetViewer(person);
      }

      Finished?.Invoke(name ?? "");
    }

    /// <summary>Raised by whoever filled <see cref="Others"/>, since the collection cannot raise it itself.</summary>
    private void OnOthersFilled()
    {
      OnPropertyChanged(nameof(HasOthers));
    }
  }
}
