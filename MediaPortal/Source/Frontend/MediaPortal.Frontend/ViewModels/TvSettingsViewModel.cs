using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// Where the television lives, and whether anything is there.
  ///
  /// <para>
  /// The reference had no such screen, for the honest reason that its TV service was the machine's own: there
  /// was nothing to point at. Here the programmes, the tuners and the timers all belong to a TvHeadend on the
  /// network, so the address of that thing has to be said somewhere - and, because the guide and the recording
  /// lists are only as good as what it answers, the screen has to be able to ask it and say plainly what came
  /// back. That press is the reason the screen exists; a form that only wrote numbers into a file would leave
  /// a wrong port as silent as a wrong port left unfilled.
  /// </para>
  /// </summary>
  public sealed class TvSettingsViewModel : INotifyPropertyChanged
  {
    private readonly Func<TvBackendSettings, TvHeadendApiClient> _buildClient;
    private readonly Action<Action> _marshal;
    private TvBackendSettings _settings = new TvBackendSettings();
    private string _host = "";
    private string _port = "";
    private string _username = "";
    private string _password = "";
    private string _guideDays = "";
    private string _visibleHours = "";
    private string _visibleRows = "";
    private string _zapTimeout = "";
    private string _recordingNoticeSeconds = "";
    private bool _showChannelNames = true;
    private bool _showChannelNumbers = true;
    private bool _showChannelLogos = true;
    private bool _showGenreColors = true;
    private bool _showSeriesInfo = true;
    private bool _hideAllChannelsGroup;
    private bool _zapFromGuide = true;
    private bool _zapByChannelIndex = true;
    private bool _autoStartTV;
    private bool _showRecordingStarted = true;
    private bool _showRecordingEnded = true;
    private bool _demoData;
    private string _status = "";
    private bool _checking;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Builds the screen. The client factory exists for the tests, which hand the screen a backend scripted
    /// in the test file instead of one on the network; a screen whose "ask it" press could only ever ask the
    /// network could not be tested at all, and the sentence it says is the whole reason it exists.
    /// </summary>
    /// <param name="clientFactory">
    /// How to build the backend to ask. A screen under test hands in a scripted one.
    /// </param>
    /// <param name="marshal">
    /// How to get the answer onto the thread the screen lives on, or null to say it where it arrives. A backend
    /// across the network answers on a thread of its own, and the line the screen says it on belongs to the thread
    /// that built the screen - see <c>Views/ScreenThread.cs</c>.
    /// </param>
    public TvSettingsViewModel(Func<TvBackendSettings, TvHeadendApiClient>? clientFactory = null,
        Action<Action>? marshal = null)
    {
      _buildClient = clientFactory ?? delegate (TvBackendSettings settings)
      {
        return DemoTvBackend.Open(settings);
      };
      _marshal = marshal ?? delegate (Action action) { action(); };
    }

    /// <summary>
    /// A line under the heading. Worded as a question because the box's answer decides whether any of the
    /// television screens has anything to show.
    /// </summary>
    public string Subtitle
    {
      get
      {
        return "The programmes, the tuners and the recording timers all belong to a TvHeadend somewhere on the "
            + "network. This says which one, and asks it whether it is there. Underneath the address are the "
            + "answers the guide and the zap buttons ask with: how much of the evening to show, how many channels "
            + "to show at once, and which of the two a number typed on a remote means. At the bottom of the "
            + "address sits one switch with no counterpart in the reference: demo data, which lets every television "
            + "screen be looked at with no TV backend anywhere.";
      }
    }

    /// <summary>The machine answering api/* on the network. A bare name, or one with a domain after it.</summary>
    public string Host
    {
      get { return _host; }
      set { _host = value ?? ""; Raise(); }
    }

    /// <summary>The port it listens on, as text: a half-typed port is not an error, just not yet an answer.</summary>
    public string Port
    {
      get { return _port; }
      set { _port = value ?? ""; Raise(); }
    }

    /// <summary>The name it asks for, if it asks at all.</summary>
    public string Username
    {
      get { return _username; }
      set { _username = value ?? ""; Raise(); }
    }

    /// <summary>The word it asks for. Kept out of every sentence the screen says.</summary>
    public string Password
    {
      get { return _password; }
      set { _password = value ?? ""; Raise(); }
    }

    /// <summary>How many days the guide is read ahead.</summary>
    public string GuideDays
    {
      get { return _guideDays; }
      set { _guideDays = value ?? ""; Raise(); }
    }

    /// <summary>
    /// How many hours the guide is to show at once, as typed. The reference's <c>EpgNumberOfHours</c>, which its
    /// settings screen described as "Number of hours in EPG" and which its grid divided by its own seventy-five
    /// columns (<c>Controls/EpgGrid.cs:96-99</c>); this guide divides by its ninety, for the same reason that
    /// grid did - the columns are what a block is measured in.
    /// </summary>
    public string VisibleHours
    {
      get { return _visibleHours; }
      set { _visibleHours = value ?? ""; Raise(); }
    }

    /// <summary>
    /// How many channels the guide is to show at once, as typed. The reference's <c>EpgNumberOfRows</c>, and the
    /// number its grid divided its own height by (<c>EpgGrid.cs:428</c>), so asking for fewer rows makes the rows
    /// that are left taller rather than leaving the bottom of the screen empty.
    /// </summary>
    public string VisibleRows
    {
      get { return _visibleRows; }
      set { _visibleRows = value ?? ""; Raise(); }
    }

    /// <summary>
    /// How long somebody gets to finish typing a channel number, as typed. The reference's <c>ZapTimeout</c>:
    /// "Configures the delay until tuning starts."
    /// </summary>
    public string ZapTimeout
    {
      get { return _zapTimeout; }
      set { _zapTimeout = value ?? ""; Raise(); }
    }

    /// <summary>Whether a channel's name is written beside its row. The reference's <c>EpgShowChannelNames</c>.</summary>
    public bool ShowChannelNames
    {
      get { return _showChannelNames; }
      set { _showChannelNames = value; Raise(); }
    }

    /// <summary>Whether a channel's number is written beside its row. The reference's <c>EpgShowChannelNumbers</c>.</summary>
    public bool ShowChannelNumbers
    {
      get { return _showChannelNumbers; }
      set { _showChannelNumbers = value; Raise(); }
    }

    /// <summary>
    /// Whether a channel's own picture is drawn beside its row, out of the pictures its TV machine keeps. The
    /// reference's <c>EpgShowChannelLogos</c>; what it does not mean here is a collection of pictures of its own,
    /// because a TVHeadend already has the ones it has.
    /// </summary>
    public bool ShowChannelLogos
    {
      get { return _showChannelLogos; }
      set { _showChannelLogos = value; Raise(); }
    }

    /// <summary>
    /// Whether the corner of the screen says so when a recording begins. The reference's
    /// <c>ShowRecordingStartedNotifications</c>.
    /// </summary>
    public bool ShowRecordingStartedNotifications
    {
      get { return _showRecordingStarted; }
      set { _showRecordingStarted = value; Raise(); }
    }

    /// <summary>
    /// Whether the corner of the screen says so when a recording is through. The reference's
    /// <c>ShowRecordingEndedNotifications</c>.
    /// </summary>
    public bool ShowRecordingEndedNotifications
    {
      get { return _showRecordingEnded; }
      set { _showRecordingEnded = value; Raise(); }
    }

    /// <summary>
    /// How long such a sentence stands on the screen, as typed. The reference's
    /// <c>RecordingNotificationDuration</c>, which it counted in seconds and switched off below one; a recording
    /// that is not going to be mentioned is a recording the mark in the header still says something about.
    /// </summary>
    public string RecordingNoticeSeconds
    {
      get { return _recordingNoticeSeconds; }
      set { _recordingNoticeSeconds = value ?? ""; Raise(); }
    }

    /// <summary>
    /// Whether programmes are coloured by what they are. The reference's <c>ShowEpgGenreColors</c>, which it
    /// defaulted to off; here it is on, because a guide whose blocks are all one colour says nothing about the
    /// evening and the skin carries the eight colours anyway.
    /// </summary>
    public bool ShowGenreColors
    {
      get { return _showGenreColors; }
      set { _showGenreColors = value; Raise(); }
    }

    /// <summary>Whether a programme says which episode it is. The reference's <c>ShowSeriesInfo</c>.</summary>
    public bool ShowSeriesInfo
    {
      get { return _showSeriesInfo; }
      set { _showSeriesInfo = value; Raise(); }
    }

    /// <summary>
    /// Whether the bouquet called "All Channels" is left out of what a viewer is offered. The reference's
    /// <c>HideAllChannelsGroup</c>.
    /// </summary>
    public bool HideAllChannelsGroup
    {
      get { return _hideAllChannelsGroup; }
      set { _hideAllChannelsGroup = value; Raise(); }
    }

    /// <summary>
    /// Whether standing on a running programme and pressing sends the viewer to it. The reference's
    /// <c>ZapFromGuide</c>, worded there as "If enabled, OK on currently running programs tunes the channel".
    /// </summary>
    public bool ZapFromGuide
    {
      get { return _zapFromGuide; }
      set { _zapFromGuide = value; Raise(); }
    }

    /// <summary>
    /// Whether a typed number is a place in the bouquet or the number printed on a channel. The reference's
    /// <c>ZapByChannelIndex</c>.
    /// </summary>
    public bool ZapByChannelIndex
    {
      get { return _zapByChannelIndex; }
      set { _zapByChannelIndex = value; Raise(); }
    }

    /// <summary>
    /// Whether pressing the tile called "Live TV" starts a picture. The reference's <c>AutoStartTV</c>.
    /// </summary>
    public bool AutoStartTV
    {
      get { return _autoStartTV; }
      set { _autoStartTV = value; Raise(); }
    }

    /// <summary>
    /// Whether the television is to come from the invented backend instead of from a machine. Said plainly on the
    /// screen - "Demo data" with a sentence about what it is - because the one thing this switch must not do is
    /// leave somebody wondering later why their guide is full of channels that do not exist.
    /// </summary>
    public bool DemoData
    {
      get { return _demoData; }
      set { _demoData = value; Raise(); }
    }

    /// <summary>What the last press brought back, in words.</summary>
    public string Status
    {
      get { return _status; }
      private set { _status = value; Raise(); }
    }

    /// <summary>Whether a check is in flight, which is when the press must not answer twice.</summary>
    public bool Checking
    {
      get { return _checking; }
      private set { _checking = value; Raise(); Raise(nameof(CanCheck)); }
    }

    /// <summary>Whether the "ask it" press is for use now.</summary>
    public bool CanCheck
    {
      get { return !_checking; }
    }

    /// <summary>
    /// How many days the answers on the screen ask for. Fourteen when the box is empty or nonsense, because
    /// that is the reference's own number and a screen that refused to draw until every field was perfect
    /// would be worse than one that guessed the same way the settings file does.
    /// </summary>
    public int DaysAhead
    {
      get
      {
        int days;
        return int.TryParse(_guideDays.Trim(), out days) && days >= 1 && days <= 31 ? days : 14;
      }
    }

    /// <summary>
    /// How wide the guide's window is as typed, with the nonsense taken out by the same rule the file uses - which
    /// is why a half-typed "2." does not stop the rest of the screen from working.
    /// </summary>
    public double HoursToShow
    {
      get
      {
        TvBackendSettings probe = new TvBackendSettings();
        double hours;
        if (double.TryParse(AsNumber(_visibleHours), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out hours))
          probe.VisibleHours = hours;
        return probe.VisibleHoursOr;
      }
    }

    /// <summary>How many channels fit on the guide as typed, with the nonsense taken out.</summary>
    public int RowsToShow
    {
      get
      {
        TvBackendSettings probe = new TvBackendSettings();
        int rows;
        if (int.TryParse(AsNumber(_visibleRows), out rows))
          probe.VisibleRows = rows;
        return probe.VisibleRowsOr;
      }
    }

    /// <summary>How long a typed channel number is waited over, as typed, with the nonsense taken out.</summary>
    public double ZapSecondsToShow
    {
      get
      {
        TvBackendSettings probe = new TvBackendSettings();
        double seconds;
        if (double.TryParse(AsNumber(_zapTimeout), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out seconds))
          probe.ZapTimeoutSeconds = seconds;
        return probe.ZapTimeoutSecondsOr;
      }
    }

    /// <summary>
    /// How long a recording sentence was typed to stand, with the nonsense taken out. A number past a minute is
    /// taken as nobody meaning it, exactly as the reference refused to draw one for over five seconds - a sentence
    /// about a recording is not a poster.
    /// </summary>
    public double NoticeSecondsToShow
    {
      get
      {
        TvBackendSettings probe = new TvBackendSettings();
        double seconds;
        if (double.TryParse(AsNumber(_recordingNoticeSeconds), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out seconds))
          probe.RecordingNotificationSeconds = seconds;
        return probe.RecordingNotificationSecondsOr;
      }
    }

    /// <summary>
    /// What somebody typed in a number box, made into something a number can be read out of: the spaces taken off,
    /// and a comma let through as the nought-point it means, because the keyboard on this screen is the one on the
    /// remote in front of the television and a German household types 2,5 into a box that wants 2.5.
    /// </summary>
    private static string AsNumber(string typed)
    {
      return (typed ?? "").Trim().Replace(',', '.');
    }

    /// <summary>
    /// Puts the answers from the file - or from whatever was loaded - into the fields, and says whether the
    /// file had anything to complain about.
    /// </summary>
    public void Show(TvBackendSettings? settings, string? problem)
    {
      string? readProblem = null;
      _settings = settings ?? TvBackendSettings.Load(null, out readProblem);
      if (settings != null)
        readProblem = problem;
      else if (string.IsNullOrWhiteSpace(problem))
        problem = readProblem;
      _host = _settings.Host;
      _port = _settings.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
      _username = _settings.Username;
      _password = _settings.Password;
      _guideDays = _settings.GuideDays.ToString(System.Globalization.CultureInfo.InvariantCulture);
      _visibleHours = Words(_settings.VisibleHoursOr);
      _visibleRows = _settings.VisibleRowsOr.ToString(System.Globalization.CultureInfo.InvariantCulture);
      _zapTimeout = Words(_settings.ZapTimeoutSecondsOr);
      _recordingNoticeSeconds = Words(_settings.RecordingNotificationSecondsOr);
      _showChannelNames = _settings.ShowChannelNames;
      _showChannelNumbers = _settings.ShowChannelNumbers;
      _showChannelLogos = _settings.ShowChannelLogos;
      _showGenreColors = _settings.ShowGenreColors;
      _showSeriesInfo = _settings.ShowSeriesInfo;
      _hideAllChannelsGroup = _settings.HideAllChannelsGroup;
      _zapFromGuide = _settings.ZapFromGuide;
      _zapByChannelIndex = _settings.ZapByChannelIndex;
      _autoStartTV = _settings.AutoStartTV;
      _showRecordingStarted = _settings.ShowRecordingStartedNotifications;
      _showRecordingEnded = _settings.ShowRecordingEndedNotifications;
      _demoData = _settings.DemoData;
      if (!string.IsNullOrWhiteSpace(problem))
        Status = problem;
      else if (_settings.DemoData)
        Status = "Written down as demo data, so the television screens show channels and programmes this front "
            + "end invents. Turn the switch off and write a machine in to watch real television.";
      else if (_settings.IsConfigured)
        Status = "Written down to ask " + _settings.Describe() + ", which has not been asked yet.";
      else
        Status = "No television backend has been written down yet, so the television and radio screens have "
            + "nothing to show.";
      RaiseEverything();
    }

    /// <summary>The answers as typed, before anyone presses save - so a check can ask the same thing.</summary>
    public TvBackendSettings Typed()
    {
      TvBackendSettings typed = new TvBackendSettings();
      typed.Host = _host.Trim();
      int port;
      typed.Port = int.TryParse(_port.Trim(), out port) ? port : TvBackendSettings.DEFAULT_PORT;
      typed.Username = _username.Trim();
      typed.Password = _password;
      typed.GuideDays = DaysAhead;
      typed.VisibleHours = HoursToShow;
      typed.VisibleRows = RowsToShow;
      typed.ZapTimeoutSeconds = ZapSecondsToShow;
      typed.RecordingNotificationSeconds = NoticeSecondsToShow;
      typed.ShowChannelNames = _showChannelNames;
      typed.ShowChannelNumbers = _showChannelNumbers;
      typed.ShowChannelLogos = _showChannelLogos;
      typed.ShowGenreColors = _showGenreColors;
      typed.ShowSeriesInfo = _showSeriesInfo;
      typed.HideAllChannelsGroup = _hideAllChannelsGroup;
      typed.ZapFromGuide = _zapFromGuide;
      typed.ZapByChannelIndex = _zapByChannelIndex;
      typed.AutoStartTV = _autoStartTV;
      typed.ShowRecordingStartedNotifications = _showRecordingStarted;
      typed.ShowRecordingEndedNotifications = _showRecordingEnded;
      typed.DemoData = _demoData;
      return typed;
    }

    /// <summary>
    /// A number written the way the box shows it: two hours as "2" rather than "2", and two and a half as "2.5"
    /// rather than "2,5" or "2.5000000000000001".
    /// </summary>
    private static string Words(double value)
    {
      return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Writes the answers down, and says which file they went into.</summary>
    public void Save()
    {
      bool saved;
      Status = Typed().Save(null, out saved);
    }

    /// <summary>
    /// Asks the backend - the one on the screen, not necessarily the one already saved - what it is. Two
    /// questions are asked rather than one "am I working" call, because the honest answer distinguishes a
    /// backend that is not there at all from one that is there but has no channels yet, and the second of
    /// those is a job for the TvHeadend's own configuration and nothing this screen can fix.
    /// </summary>
    public async Task CheckAsync()
    {
      TvBackendSettings ask = Typed();
      if (!ask.IsConfigured)
      {
        _marshal(delegate
        {
          Status = "There is nothing to ask yet - first say which machine the television comes from.";
        });
        return;
      }

      _marshal(delegate
      {
        Checking = true;
        Status = "Asking " + ask.Describe() + "...";
      });

      string said;
      try
      {
        using (TvHeadendApiClient client = _buildClient(ask))
        {
          System.Collections.Generic.IReadOnlyList<TvChannelGroup> bouquets =
              await client.GetChannelGroupsAsync().ConfigureAwait(false);
          System.Collections.Generic.IReadOnlyList<TvChannel> all =
              await client.GetChannelsAsync().ConfigureAwait(false);
          int channels = 0;
          int radio = 0;
          TvChannel? first = null;
          foreach (TvChannel channel in all)
          {
            if (channel.MediaType == TvMediaType.Radio)
              radio++;
            else
            {
              channels++;
              if (first == null)
                first = channel;
            }
          }

          if (channels == 0)
          {
            said = bouquets.Count == 0
                ? string.Format("{0} answered, but knows no television channels and no bouquets of its own. "
                    + "It has to be set up with channels before a guide can show anything.", ask.Describe())
                : string.Format("{0} answered and has {1} bouquet(s), but no television channels are in them.",
                    ask.Describe(), bouquets.Count);
            if (radio > 0)
              said += string.Format(" It does have {0} radio channel(s).", radio);
          }
          else
          {
            said = string.Format("{0} answered. {1} television channel(s) in {2} bouquet(s)",
                ask.Describe(), channels, bouquets.Count);
            said += radio == 0
                ? ", and no radio."
                : string.Format(", and {0} radio channel(s).", radio);

            TvProgram? now = null;
            string extra = "";
            try
            {
              TvNowAndNext nowAndNext = await client.GetNowAndNextAsync(first!, DateTimeOffset.UtcNow)
                  .ConfigureAwait(false);
              now = nowAndNext.Now;
            }
            catch (TvBackendException ex)
            {
              // The guide is a second question, and a backend that has channels but no EPG is a common enough
              // state to be worth naming rather than failing the whole check.
              extra = " The programmes did not answer: " + ex.Message;
            }

            said += now != null
                ? string.Format(" The first channel is showing \"{0}\".", now.Title)
                : " The first channel has nothing in the programme table right now.";
            if (extra.Length > 0)
              said += extra;
          }
        }
      }
      catch (TvBackendException ex)
      {
        said = ex.Message + (ex.NeedsLogin
            ? " Fill the username and password below, then ask again." : "");
      }
      catch (Exception ex)
      {
        said = "Asking " + ask.Describe() + " went wrong: " + ex.Message;
      }

      // The sentence and the button's state are the screen's, and an answer from the network arrives on a thread
      // of its own; saying it where it arrived is how a screen being answered from the other side of the house
      // threw "a different thread owns it" instead of telling the viewer what its backend said.
      _marshal(delegate
      {
        Status = said;
        Checking = false;
      });
    }

    /// <summary>A press for the button on the screen: it says what it does when it finishes.</summary>
    public System.Windows.Input.ICommand CheckCommand
    {
      get { return new RelayCommand(delegate { _ = CheckAsync(); }); }
    }

    /// <summary>A press for the other button: it writes the file.</summary>
    public System.Windows.Input.ICommand SaveCommand
    {
      get { return new RelayCommand(delegate { Save(); }); }
    }

    private void Raise([CallerMemberName] string? name = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Says that every answer may have changed, so that everything bound to one looks again.
    /// </summary>
    /// <remarks>
    /// The answers are counted out of the class itself and not written down here. The list this method used to carry
    /// named five of its twenty answers by hand, and an answer missing from the list left its control showing what
    /// it had been built with: the switch for the demo data was one of them, so a household could turn it on, watch
    /// the guide fill itself with invented channels, open these settings again and be shown a switch that had
    /// stayed off. A list can only ever be as complete as the person who last added an answer remembered it.
    /// </remarks>
    public void RaiseEverything()
    {
      foreach (System.Reflection.PropertyInfo property in GetType()
          .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        Raise(property.Name);
    }

    /// <summary>A press that does one thing and can always be pressed.</summary>
    private sealed class RelayCommand : System.Windows.Input.ICommand
    {
      private readonly Action _act;

      public RelayCommand(Action act)
      {
        _act = act;
      }

      public event EventHandler? CanExecuteChanged
      {
        add { }
        remove { }
      }

      public bool CanExecute(object? parameter)
      {
        return true;
      }

      public void Execute(object? parameter)
      {
        _act();
      }
    }
  }
}
