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
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Notifications;

namespace MediaPortal.Frontend.Theming
{
  /// <summary>
  /// What the skin's master chrome - the header and the footer the reference screens wrapped around every
  /// screen - has to say about the window it is drawn on: the time the header shows and what is playing that
  /// the footer shows.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference's master screens read these from the running application: the header's clock element read
  /// the system clock, and the footer's PlayerControl elements read what the player was doing and hid
  /// themselves (AutoVisibility) while nothing played. This class is the same information in one place: the
  /// window owns it, the skin's chrome.xaml templates bind to it, and nobody else has to know either exists.
  /// </para>
  /// <para>
  /// It is the data type the skin files promise with <c>x:DataType</c>, which is why its property names are
  /// its API: a skin that binds to a name not here shows up as a compile error in the skin file, not as a
  /// silently blank header.
  /// </para>
  /// </remarks>
  public partial class ChromeInfo : ObservableObject
  {
    private System.Timers.Timer? _clockTimer;
    private Action<Action>? _marshal;

    /// <summary>
    /// How the header writes the time, in the worn skin's words. Declared before the two properties it writes
    /// because a field's first answer is made in the order the fields stand, and the clock and the day are
    /// dressed with these.
    /// </summary>
    private string _timeFormat = Settings.SkinSettingsBase.DEFAULT_TIME_FORMAT;

    /// <summary>How the header writes the day. See <see cref="_timeFormat"/>.</summary>
    private string _dateFormat = Settings.SkinSettingsBase.DEFAULT_DATE_FORMAT;

    public ChromeInfo()
    {
      _clock = FormatClock(DateTime.Now, _timeFormat);
      _openNotifications = new OpenNotifications(this);
    }

    public ChromeInfo(Action<Action>? marshal)
        : this()
    {
      _marshal = marshal;
    }

    /// <summary>
    /// Start the header's clock ticking. Split from the constructor because a test may want the words without
    /// the timer, and a window wants both.
    /// </summary>
    public void StartClock()
    {
      if (_clockTimer != null)
        return;

      // The reference header's clock read the minute, not the second - a television ten feet away shows
      // minutes, and a timer that fires every second on a media player is a wake-up nobody asked for.
      _clockTimer = new System.Timers.Timer(TimeSpan.FromSeconds(15).TotalMilliseconds) { AutoReset = true };
      _clockTimer.Elapsed += (sender, e) => ReportClock();
      _clockTimer.Start();
    }

    /// <summary>
    /// Say the time and the day again, this moment's words: the language row on the client settings screen
    /// calls this after it saves, so a date that was English a keystroke ago is German on the next line of
    /// the head line, without waiting for the timer or a restart.
    /// </summary>
    public void RefreshClock()
    {
      ReportClock();
    }

    /// <summary>
    /// Put the new time where the header reads it. The timer rings on its own thread, and a bound property
    /// may only be spoken to on the one the window lives on - so the words are posted there, which is how the
    /// player's events arrive too.
    /// </summary>
    private void ReportClock()
    {
      // The header's clock is also this front end's heartbeat, so it is what makes a notification whose reason has
      // gone away stop being counted; the reference kept a timer of its own for that
      // (INotificationService.CheckForTimeouts), and a second timer here would only be a second thing to forget.
      _queue?.CheckTimeouts();

      string now = FormatClock(DateTime.Now, _timeFormat);
      string today = FormatDate(DateTime.Now, _dateFormat);
      if (_marshal == null)
      {
        Clock = now;
        Date = today;
      }
      else
        _marshal(() =>
        {
          Clock = now;
          Date = today;
        });
    }

    /// <summary>The header's clock, spelled the way the reference header spelled it.</summary>
    [ObservableProperty]
    private string _clock;

    /// <summary>
    /// Whether the head line may show its clock at all - the reference's <c>ShowTime</c>, one of Nereus'
    /// own settings. True while nobody wears a skin that has an opinion, because a header that hides its
    /// clock without being asked is a broken clock to everyone else.
    /// </summary>
    [ObservableProperty]
    private bool _showTime = true;

    /// <summary>Whether the head line may show its date - the reference's <c>ShowDate</c>, next to its twin.</summary>
    [ObservableProperty]
    private bool _showDate = true;

    /// <summary>
    /// The day the header's clock stands on - the reference showed the date under the time, and its skin
    /// settings made it the clock's own companion (ShowDate next to ShowTime), not a stranger's.
    /// </summary>
    [ObservableProperty]
    private string _date = FormatDate(DateTime.Now, Settings.SkinSettingsBase.DEFAULT_DATE_FORMAT);

    /// <summary>The name of the screen the window is on; the header shows it where the reference masters showed their title.</summary>
    [ObservableProperty]
    private string _screenTitle = "";

    /// <summary>Whether the footer has anything to show at all - the AutoVisibility of the reference.</summary>
    [ObservableProperty]
    private bool _hasMedia;

    /// <summary>The title of whatever is playing, as the footer's first column.</summary>
    [ObservableProperty]
    private string _nowPlayingTitle = "";

    /// <summary>How far into it we are, nought to a hundred, as the footer's progress line.</summary>
    [ObservableProperty]
    private double _nowPlayingProgress;

    /// <summary>Playing, paused, whatever the player says; the footer's quiet last column.</summary>
    [ObservableProperty]
    private string _nowPlayingState = "";

    /// <summary>
    /// Say what the player is doing - the footer appears with these words.
    /// </summary>
    public void ReportPlaying(string title, double progressPercent, string state)
    {
      NowPlayingTitle = title;
      NowPlayingProgress = progressPercent;
      NowPlayingState = state;
      HasMedia = true;
    }

    /// <summary>Nothing is playing; the footer takes itself off.</summary>
    public void ReportStopped()
    {
      HasMedia = false;
      NowPlayingTitle = "";
      NowPlayingProgress = 0;
      NowPlayingState = "";
    }

    /// <summary>
    /// The long sentence about what the server is doing down at the library, kept for the head line's tooltip. The bar
    /// itself says nothing in words: its importer was a turning circle with "43%" under it, its recording a blinking
    /// picture, and no sentence anywhere (<c>Skin/Nereus/screens/NotificationsBar.inc:19-39</c>).
    /// </summary>
    [ObservableProperty]
    private string? _serverNotice;

    /// <summary>Whether there is a sentence to show at all.</summary>
    public bool HasServerNotice
    {
      get { return !string.IsNullOrEmpty(ServerNotice); }
    }

    partial void OnServerNoticeChanged(string? value)
    {
      OnPropertyChanged(nameof(HasServerNotice));
    }

    /// <summary>
    /// What the front end's deal with the server is at this moment, which is what the head line's glyph is chosen
    /// from. Starts at "nothing is being listened to", so that a screen shown before anyone asked for anything draws
    /// no claim about a server.
    /// </summary>
    [ObservableProperty]
    private ServerActivityState _activity = ServerActivityState.NotListening;

    /// <summary>
    /// How far the server has got with its library, when it said: the reference printed exactly this number next to
    /// its turning circle. Null while nothing is being filed, or when the server only said that it had started.
    /// </summary>
    [ObservableProperty]
    private int? _filingPercent;

    /// <summary>Whether the head line has a server glyph to draw at all.</summary>
    public bool HasActivity
    {
      get { return Activity != ServerActivityState.NotListening; }
    }

    /// <summary>
    /// The percentage to stand under the glyph while the server is filing, or the empty string - which is all the
    /// worn skin ever wrote under its importer's picture (<c>NotificationsBar.inc:29-31</c>). The three connection
    /// states get nothing here: the reference's bar was pictures and no words, and a household that sees the
    /// disconnected glyph reads the glyph.
    /// </summary>
    public string ActivityPercent
    {
      get
      {
        if (Activity != ServerActivityState.Filing)
          return "";

        int? percent = FilingPercent;
        return percent.HasValue ? percent.Value + "%" : "";
      }
    }

    /// <summary>The worn skin's glyph standing for the state.</summary>
    public string ActivityIcon
    {
      get { return ServerActivity.Glyph(Activity); }
    }

    /// <summary>
    /// The word a state is read with by anything that wants words - a tooltip or a screen of its own. The head line's
    /// bar does not ask: the reference's bar was pictures only.
    /// </summary>
    public string ActivityWords
    {
      get { return ServerActivity.Words(Activity); }
    }

    /// <summary>Whether the server is filing its library, which is the only state whose circle is worth turning.</summary>
    public bool IsFiling
    {
      get { return Activity == ServerActivityState.Filing; }
    }

    partial void OnActivityChanged(ServerActivityState value)
    {
      OnPropertyChanged(nameof(HasActivity));
      OnPropertyChanged(nameof(ActivityIcon));
      OnPropertyChanged(nameof(ActivityPercent));
      OnPropertyChanged(nameof(ActivityWords));
      OnPropertyChanged(nameof(IsFiling));
    }

    partial void OnFilingPercentChanged(int? value)
    {
      OnPropertyChanged(nameof(ActivityPercent));
    }

    /// <summary>
    /// What the TV machine is doing about recordings, said at the corner of the screen: "Recording started: ..." and
    /// "Recording ended: ...". The reference put these in the same bar as the importer's progress
    /// (<c>Nereus/screens/NotificationsBar.inc</c>), out of the same reason - a household should hear about a
    /// recording from where it is already looking rather than by opening a screen to ask. Null or empty means the
    /// watcher has nothing to say, which is most of the time; whoever raised it takes it away again.
    /// </summary>
    [ObservableProperty]
    private string? _recordingNotice;

    /// <summary>Whether there is a recording notice to show.</summary>
    public bool HasRecordingNotice
    {
      get { return !string.IsNullOrEmpty(RecordingNotice); }
    }

    partial void OnRecordingNoticeChanged(string? value)
    {
      OnPropertyChanged(nameof(HasRecordingNotice));
    }

    /// <summary>
    /// Whether the TV machine is writing something to disk right now. The reference drew this as a standing mark in
    /// its notification bar rather than as a sentence (<c>IsRecording</c> there), because unlike a notice it is not
    /// news about a moment - it is true the whole time the recording runs, and a viewer who walks in halfway through
    /// still has to be able to see it.
    /// </summary>
    [ObservableProperty]
    private bool _isRecording;

    /// <summary>
    /// Whether the recording mark is showing this instant, which is the whole of its blinking.
    /// </summary>
    /// <remarks>
    /// The reference blinked its recording picture with a storyboard of its own
    /// (<c>Skin/Nereus/themes/default/styles/Buttons.xaml:1717</c>). A skin file in this front end is loose markup
    /// that is parsed when the screen is dressed, and an animation is one of the things such markup cannot carry - so
    /// the blink belongs to the model, which also means it can be asked about instead of being photographed.
    /// </remarks>
    [ObservableProperty]
    private bool _recordingMarkVisible = true;

    private System.Timers.Timer? _blinkTimer;

    /// <summary>
    /// How long the mark stays on one side of a blink. The reference's mark blinked rather faster than a heartbeat and
    /// never stopped being readable; 700 ms on a side is a mark that is noticed without being a strobe light.
    /// </summary>
    private static readonly TimeSpan BLINK_HALF = TimeSpan.FromMilliseconds(700);

    /// <summary>
    /// Turns the blink on with the recording and off with it, so that a mark which is not saying "recording" stands
    /// still and a recording that ended leaves nothing pulsing in the head line.
    /// </summary>
    partial void OnIsRecordingChanged(bool value)
    {
      if (value)
        StartRecordingBlink();
      else
        StopRecordingBlink();
    }

    /// <summary>
    /// Flips the mark to the other side of its blink, which is what the timer does and what a test asks about.
    /// </summary>
    public void BlinkRecordingMark()
    {
      RecordingMarkVisible = !RecordingMarkVisible;
    }

    /// <summary>Start the mark blinking, once per recording and not once per notice about it.</summary>
    private void StartRecordingBlink()
    {
      if (_blinkTimer != null)
        return;

      RecordingMarkVisible = true;
      _blinkTimer = new System.Timers.Timer(BLINK_HALF.TotalMilliseconds) { AutoReset = true };
      _blinkTimer.Elapsed += delegate
      {
        // The timer rings on its own thread, and a bound property may only be spoken to on the one the window lives on.
        if (_marshal == null)
          BlinkRecordingMark();
        else
          _marshal(BlinkRecordingMark);
      };
      _blinkTimer.Start();
    }

    /// <summary>Stop the blink and leave the mark showing, so that the next recording starts visibly.</summary>
    private void StopRecordingBlink()
    {
      System.Timers.Timer? timer = _blinkTimer;
      _blinkTimer = null;
      if (timer != null)
      {
        timer.Stop();
        timer.Dispose();
      }

      RecordingMarkVisible = true;
    }

    /// <summary>
    /// Says what the TV machine just did about a recording, or takes the sentence away when handed null. Handed over
    /// on the watcher's thread and put on this one, because a corner of a window belongs to the thread that made the
    /// window.
    /// </summary>
    public void ReportRecordingNotice(string? notice)
    {
      if (_marshal == null)
      {
        RecordingNotice = notice;
        return;
      }

      _marshal(() => RecordingNotice = notice);
    }

    /// <summary>Says whether the TV machine is recording right now, from the watcher's thread.</summary>
    public void ReportRecording(bool recording)
    {
      if (_marshal == null)
      {
        IsRecording = recording;
        return;
      }

      _marshal(() => IsRecording = recording);
    }

    // ---- the things the household has been told and has not read yet --------------------------------------

    /// <summary>
    /// What is waiting to be read. Null until <see cref="AttachNotifications"/> has been called, which is how a skin
    /// or a test that never heard of the queue keeps working: the hint is simply never there.
    /// </summary>
    private NotificationQueue? _queue;

    private int _notificationCount;
    private string _notificationIcon = "";
    private bool _hintsHidden;

    /// <summary>
    /// Said when somebody presses the hint in the head line, which means "read them to me". Whoever set this up - the
    /// application - opens the dialog; the head line does not know that a dialog exists, any more than the reference's
    /// notification button knew how <c>WatchSystemNotifications.xaml</c> was built.
    /// </summary>
    public event Action? NotificationsRequested;

    /// <summary>Bound to the hint.</summary>
    public ICommand OpenNotificationsCommand
    {
      get { return _openNotifications; }
    }

    private readonly OpenNotifications _openNotifications;

    /// <summary>
    /// Tells the head line's button that its answer to "could I be pressed right now" has moved, which the command
    /// itself has no way of noticing.
    /// </summary>
    internal void RaiseOpenNotificationsCanExecute()
    {
      _openNotifications.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// Tells the head line where the queue is, so that it can count it, pick the loudest glyph of what is in it, and
    /// be taken away again when the last one has been read. The reference's bar did all three
    /// (<c>Source/UI/UiComponents/SkinBase/Models/NotificationsModel.cs:114-135</c>).
    /// </summary>
    public void AttachNotifications(NotificationQueue queue)
    {
      if (queue == null)
        throw new ArgumentNullException(nameof(queue));

      _queue = queue;
      queue.Changed += delegate
      {
        // The queue speaks on whatever thread handed a notification in, which is nowhere near this window's.
        if (_marshal == null)
          RefreshNotifications();
        else
          _marshal(RefreshNotifications);
      };
      RefreshNotifications();
    }

    /// <summary>
    /// Takes the hint away while the dialog that reads the queue is open, and puts it back afterwards. The reference
    /// did exactly this: its model hid the hint the moment a notification was on the screen
    /// (<c>NotificationsModel.cs:104-112</c>), because a count of what is still waiting next to the one being read is
    /// the same number said twice.
    /// </summary>
    public void HideNotificationHint(bool hidden)
    {
      if (_hintsHidden == hidden)
        return;

      _hintsHidden = hidden;
      RefreshNotifications();
    }

    /// <summary>How many are waiting to be read, which is the number the hint carries.</summary>
    public int NotificationCount
    {
      get { return _notificationCount; }
    }

    /// <summary>Whether the head line has a hint to draw.</summary>
    public bool HasNotifications
    {
      get { return _notificationCount > 0; }
    }

    /// <summary>
    /// The count the hint carries, written the way the reference wrote it: as a number in brackets standing under the
    /// picture, and only once there is more than one thing waiting - one notification is a picture that says it all,
    /// and "(1)" under it only repeats the obvious
    /// (<c>Buttons.xaml:2023-2029</c> with <c>NotificationsModel.cs:109-112</c>).
    /// </summary>
    public string NotificationCountWords
    {
      get { return _notificationCount > 1 ? "(" + _notificationCount + ")" : ""; }
    }

    /// <summary>
    /// The glyph standing for the loudest thing waiting - an error outranks a warning, and a warning outranks a
    /// piece of information - so that a hint never quietly shows an information "i" while an error waits underneath.
    /// </summary>
    public string NotificationIcon
    {
      get { return _notificationIcon; }
    }

    /// <summary>Whether pressing the hint would open something.</summary>
    public bool CanOpenNotifications
    {
      get { return HasNotifications; }
    }

    /// <summary>Re-reads the queue and tells the head line whatever moved.</summary>
    private void RefreshNotifications()
    {
      NotificationQueue? queue = _queue;
      int count = queue == null || _hintsHidden ? 0 : queue.Count;
      NotificationType? loudest = queue == null ? null : queue.LoudestType;

      _notificationCount = count;
      _notificationIcon = loudest.HasValue ? UiNotification.IconFor(loudest.Value) : "";

      OnPropertyChanged(nameof(NotificationCount));
      OnPropertyChanged(nameof(HasNotifications));
      OnPropertyChanged(nameof(NotificationCountWords));
      OnPropertyChanged(nameof(NotificationIcon));
      OnPropertyChanged(nameof(CanOpenNotifications));
      RaiseOpenNotificationsCanExecute();
    }

    /// <summary>
    /// The head line's hint, which is a button rather than a picture because the reference's was one too: pressing it
    /// opened the screen that reads the queue (<c>Skin/Nereus/screens/NotificationsBar.inc:15-17</c> with
    /// <c>Buttons.xaml:1978</c>).
    /// </summary>
    private sealed class OpenNotifications : ICommand
    {
      private readonly ChromeInfo _chrome;

      public OpenNotifications(ChromeInfo chrome)
      {
        _chrome = chrome;
      }

      public event EventHandler? CanExecuteChanged;

      internal void RaiseCanExecuteChanged()
      {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
      }

      public bool CanExecute(object? parameter)
      {
        return _chrome.CanOpenNotifications;
      }

      public void Execute(object? parameter)
      {
        _chrome.NotificationsRequested?.Invoke();
      }
    }

    // Who is watching has no place in the head line. The reference said it through the UserButton of the
    // master's top row, and this front end says it through the very same door: the person glyph in the
    // home screen's corner row, which opens the wake as a switch. Nothing here binds to a person.

    // ---- the home head line's weather, the reference's master_home slot -----------------------------------

    /// <summary>
    /// The temperature the home head line shows - "8°C", the same spelling the weather screen uses. Empty
    /// while no watched place has been read; the head line then says nothing about weather.
    /// </summary>
    [ObservableProperty]
    private string _weatherTemperature = "";

    /// <summary>
    /// The day's weather as the worn skin's own picture: the name of the file the skin keeps under
    /// <c>images/Weather/128x128</c> ("3" for sun, "na" for a state the reference's tables never knew), which
    /// is what master_home.xaml stood in the middle of its head line in place of a sentence about the weather.
    /// Empty while no watched place has been read.
    /// </summary>
    [ObservableProperty]
    private string _weatherIcon = "";

    /// <summary>
    /// Whether the head line may show the temperature at all: the reference's <c>ShowTemperature</c>, one of
    /// Nereus' own settings, which guarded exactly this label on its home master.
    /// </summary>
    [ObservableProperty]
    private bool _showTemperature = true;

    /// <summary>
    /// The reference's <c>ShowWeatherCondition</c>, the dial that guarded the weather picture in the middle of
    /// its home head line - the picture, not a line of words, which is how the skin says what the sky is doing.
    /// </summary>
    [ObservableProperty]
    private bool _showWeatherCondition = true;

    /// <summary>
    /// Whether the screen on show is the one whose master carried the weather slot. The reference showed the
    /// reading on <c>master_home</c> and nowhere else, so the slot is the home screen's, not every screen's.
    /// </summary>
    [ObservableProperty]
    private bool _showWeatherSlot = true;

    /// <summary>Temperature half of the weather slot: on the home head line, dialled on, and actually read.</summary>
    public bool HasWeatherTemperature
    {
      get { return ShowWeatherSlot && ShowTemperature && WeatherTemperature.Length > 0; }
    }

    /// <summary>
    /// The picture half of the weather slot: on the screen whose master carried the slot, dialled on, and a
    /// picture the skin actually carries. Under its own dial - <c>ShowWeatherCondition</c> guarded
    /// <c>WeatherBigIcon</c> on the reference's home master.
    /// </summary>
    public bool HasWeatherIcon
    {
      get { return ShowWeatherSlot && ShowWeatherCondition && WeatherIcon.Length > 0; }
    }

    partial void OnWeatherTemperatureChanged(string value)
    {
      RaiseWeatherVisibility();
    }

    partial void OnWeatherIconChanged(string value)
    {
      RaiseWeatherVisibility();
    }

    partial void OnShowTemperatureChanged(bool value)
    {
      RaiseWeatherVisibility();
    }

    partial void OnShowWeatherConditionChanged(bool value)
    {
      RaiseWeatherVisibility();
    }

    partial void OnShowWeatherSlotChanged(bool value)
    {
      RaiseWeatherVisibility();
    }

    private void RaiseWeatherVisibility()
    {
      OnPropertyChanged(nameof(HasWeatherTemperature));
      OnPropertyChanged(nameof(HasWeatherIcon));
    }

    /// <summary>
    /// Put the reading where the head line shows it: the number in the left column's second line and the
    /// skin's own picture of the sky in the middle column. The weather timer speaks on its own thread, so the
    /// words cross to the window's thread the way the clock's and the server notice's do.
    /// </summary>
    public void ReportWeather(string temperature, string? icon)
    {
      string picture = icon ?? "";
      if (_marshal == null)
      {
        WeatherTemperature = temperature;
        WeatherIcon = picture;
        return;
      }

      _marshal(() =>
      {
        WeatherTemperature = temperature;
        WeatherIcon = picture;
      });
    }

    /// <summary>
    /// Tell the head line what the front end's deal with the server is at this moment: the state picks the glyph, the
    /// sentence is what the glyph cannot say on its own, and the percentage - when the server volunteered one - is put
    /// in place of the word, because that is where the reference printed its importer's "43%".
    /// </summary>
    /// <param name="state">Which of the server states to draw.</param>
    /// <param name="detail">The long sentence, shown when the household asks the head line about it; null or empty for
    /// none, which is what "nothing is being listened to" is handed.</param>
    /// <param name="percent">How far the filing has got, or null when nothing is being filed or the server only said
    /// that it had started.</param>
    /// <remarks>
    /// The event stream speaks on its own thread, so all three cross to the window's thread the way the clock's do.
    /// </remarks>
    public void ReportServerActivity(ServerActivityState state, string? detail, int? percent = null)
    {
      string? sentence = string.IsNullOrEmpty(detail) ? null : detail;

      if (_marshal == null)
      {
        ServerNotice = sentence;
        FilingPercent = percent;
        Activity = state;
        return;
      }

      _marshal(() =>
      {
        ServerNotice = sentence;
        FilingPercent = percent;
        Activity = state;
      });
    }

    /// <summary>
    /// The old one-line call, kept for whoever has no state to offer and only a sentence: the sentence is remembered
    /// for the tooltip and the head line carries on drawing whatever state it was already drawing.
    /// </summary>
    public void ReportServerActivity(string? notice)
    {
      ReportServerActivity(Activity, notice, null);
    }

    /// <summary>
    /// Put on the two formats the worn skin was written with, and say the time and the day again at once.
    /// </summary>
    /// <param name="timeFormat">The skin's <c>TimeFormat</c>; empty or unusable falls back to "t".</param>
    /// <param name="dateFormat">The skin's <c>DateFormat</c>; empty or unusable falls back to "D".</param>
    /// <remarks>
    /// The reference did the same thing with a shout across the skin - both of its format rows sent
    /// <c>SkinMessaging.DateTimeFormatChanged</c> after saving, and the header's clock listened - because a
    /// person who has just typed a new format is standing in front of the old one. This front end has no
    /// message bus, so the screen that saves the answer hands the formats to the only thing that reads them.
    /// </remarks>
    public void WearFormats(string? timeFormat, string? dateFormat)
    {
      _timeFormat = Settings.SkinSettingsBase.Written(timeFormat, Settings.SkinSettingsBase.DEFAULT_TIME_FORMAT);
      _dateFormat = Settings.SkinSettingsBase.Written(dateFormat, Settings.SkinSettingsBase.DEFAULT_DATE_FORMAT);
      ReportClock();
    }

    private static string FormatClock(DateTime now, string format)
    {
      // The language the interface reads in, because the reference's clock was written by the culture its
      // localisation had, and "t" means "14:30" in one language and "2:30 PM" in another - which is the whole
      // point of a skin that hands the format over to whoever wears it.
      return now.ToString(format, Settings.ClientSettings.UiCulture());
    }

    /// <summary>
    /// The day as this machine's calendar says it: the reference's client settings carried a language of
    /// their own, and a head line that reads "Montag" on a German box and "Monday" on an English one is
    /// that setting doing its one visible job.
    /// </summary>
    private static string FormatDate(DateTime now, string format)
    {
      return now.ToString(format, Settings.ClientSettings.UiCulture());
    }
  }
}
