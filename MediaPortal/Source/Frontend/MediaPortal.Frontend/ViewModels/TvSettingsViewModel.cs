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
    private TvBackendSettings _settings = new TvBackendSettings();
    private string _host = "";
    private string _port = "";
    private string _username = "";
    private string _password = "";
    private string _guideDays = "";
    private string _status = "";
    private bool _checking;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Builds the screen. The client factory exists for the tests, which hand the screen a backend scripted
    /// in the test file instead of one on the network; a screen whose "ask it" press could only ever ask the
    /// network could not be tested at all, and the sentence it says is the whole reason it exists.
    /// </summary>
    public TvSettingsViewModel(Func<TvBackendSettings, TvHeadendApiClient>? clientFactory = null)
    {
      _buildClient = clientFactory ?? delegate (TvBackendSettings settings)
      {
        return new TvHeadendApiClient(settings);
      };
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
            + "network. This says which one, and asks it whether it is there.";
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
      if (!string.IsNullOrWhiteSpace(problem))
        Status = problem;
      else if (_settings.IsConfigured)
        Status = "Written down to ask " + _settings.Describe() + ", which has not been asked yet.";
      else
        Status = "No television backend has been written down yet, so the television and radio screens have "
            + "nothing to show.";
      RaiseAll();
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
      return typed;
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
        Status = "There is nothing to ask yet - first say which machine the television comes from.";
        return;
      }

      Checking = true;
      Status = "Asking " + ask.Describe() + "...";
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
            Status = bouquets.Count == 0
                ? string.Format("{0} answered, but knows no television channels and no bouquets of its own. "
                    + "It has to be set up with channels before a guide can show anything.", ask.Describe())
                : string.Format("{0} answered and has {1} bouquet(s), but no television channels are in them.",
                    ask.Describe(), bouquets.Count);
            if (radio > 0)
              Status += string.Format(" It does have {0} radio channel(s).", radio);
          }
          else
          {
            Status = string.Format("{0} answered. {1} television channel(s) in {2} bouquet(s)",
                ask.Describe(), channels, bouquets.Count);
            Status += radio == 0
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

            Status += now != null
                ? string.Format(" The first channel is showing \"{0}\".", now.Title)
                : " The first channel has nothing in the programme table right now.";
            if (extra.Length > 0)
              Status += extra;
          }
        }
      }
      catch (TvBackendException ex)
      {
        Status = ex.Message + (ex.NeedsLogin
            ? " Fill the username and password below, then ask again." : "");
      }
      catch (Exception ex)
      {
        Status = "Asking " + ask.Describe() + " went wrong: " + ex.Message;
      }
      finally
      {
        Checking = false;
      }
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

    private void RaiseAll()
    {
      foreach (string name in new[] { nameof(Host), nameof(Port), nameof(Username), nameof(Password),
          nameof(GuideDays), nameof(Status), nameof(CanCheck) })
        Raise(name);
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
