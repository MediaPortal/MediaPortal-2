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
  /// case stays on it for the full <see cref="SplashWait"/> so the wake feels like a wake rather than a flash.
  /// The timer is a field rather than <c>Task.Delay</c> so a test can ask for a wait of milliseconds and the
  /// application can keep the one its household asked for.
  /// </para>
  /// </remarks>
  public partial class StartupViewModel : ObservableObject
  {
    /// <summary>Asks the server whether these four numbers belong to this name. True means signed in.</summary>
    public delegate Task<bool> PinChecker(string name, string pin);

    private readonly Func<IUsersApi?> _usersProvider;
    private readonly PinChecker _check;
    private readonly Action<string?> _finished;
    private readonly TimeSpan _splashWait;
    private bool _answered;
    private CancellationTokenSource? _waitCts;

    public StartupViewModel(Func<IUsersApi?> usersProvider, PinChecker check, Action<string?> finished,
        TimeSpan? splashWait = null)
    {
      _usersProvider = usersProvider;
      _check = check;
      _finished = finished;
      _splashWait = splashWait ?? TimeSpan.FromSeconds(4);
    }

    /// <summary>The four numbers typed so far. The screen draws it as its boxes; the model only knows digits.</summary>
    public const int PIN_LENGTH = 4;

    /// <summary>The people the server keeps - read at open, machine profiles left out as always.</summary>
    public ObservableCollection<UserInfo> Others { get; } = new ObservableCollection<UserInfo>();

    /// <summary>The animation phase: reading, or the no-PIN wake holding the wallpaper for its seconds.</summary>
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
      string remembered = ClientSettings.Load(null, out _).RememberedViewer;
      (UserInfo? person, bool serverSpoke) = await ReadPersonAsync(remembered);

      if (_answered)
        return;

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
        PersonName = asked.Name;
        Status = "";
        Busy = false;
        NeedsPin = true;
        return;
      }

      // A server that could not be reached cannot check anyone - and a PIN nobody can check is a door
      // welded shut, not a lock. The remembered person walks into a library that will show its own
      // complaint about the missing server; a read that only found the person deleted walks nobody in.
      // Nothing is said on the walk of a house that asks nothing - the wallpaper and the ring are the
      // whole screen then, exactly as the viewer asked for.
      string? opens = person?.Name ?? (!serverSpoke && remembered.Length > 0 ? remembered : null);
      await WaitThenFinishAsync(opens);
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
          Remember(who);
          Answer(who);
          return;
        }

        Status = string.Format("{0}, that was not the PIN. Try again.", who);
        PinRejected?.Invoke();
    }

    /// <summary>Opens the "someone else" overlay: the other people, each with their answer waiting behind them.</summary>
    [RelayCommand]
    public void SwitchUser()
    {
      if (Others.Count == 0)
      {
        Status = "This server keeps no other person.";
        return;
      }
      Switching = true;
    }

    [RelayCommand]
    public void StayAsWas()
    {
      Switching = false;
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
        PersonName = who.Name;
        Pin = "";
        Status = "";
        Busy = false;
        NeedsPin = true;
        return;
      }

      Remember(who.Name);
      Answer(who.Name);
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
        UserInfo? person = null;
        foreach (UserInfo user in answer.Users ?? Array.Empty<UserInfo>())
        {
          if (user.Kind == "Client")
            continue;
          if (string.Equals(user.Name, remembered, StringComparison.OrdinalIgnoreCase))
            person = user;
          else
            Others.Add(user);
        }
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
      CancellationTokenSource cts = _waitCts = new CancellationTokenSource();
      try
      {
        await Task.Delay(_splashWait, cts.Token);
      }
      catch (TaskCanceledException)
      {
        return;
      }

      if (_answered || !cts.IsCancellationRequested)
        Answer(name);
    }

    /// <summary>Writes who the room is watching for, so the next start asks the right person.</summary>
    private static void Remember(string name)
    {
      ClientSettings settings = ClientSettings.Load(null, out _);
      settings.RememberedViewer = name;
      settings.Save(null, out _);
      Settings.SkinSettingsBase.FileOwner = name;
    }

    /// <summary>The one answer, whoever brought it. Twice is a bug, so the second one never happens.</summary>
    private void Answer(string? name)
    {
      if (_answered)
        return;
      _answered = true;
      _finished(name ?? "");
    }

    /// <summary>Raised by whoever filled <see cref="Others"/>, since the collection cannot raise it itself.</summary>
    private void OnOthersFilled()
    {
      OnPropertyChanged(nameof(HasOthers));
    }
  }
}
