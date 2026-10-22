namespace MediaPortal.Frontend.ViewModels;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;

/// <summary>
/// The thing that notices a recording starting and a recording ending, and says so on the screen.
/// </summary>
/// <remarks>
/// <para>
/// <b>What it is for.</b> A television records things whether anybody is looking at it or not, and the reference
/// made sure a household heard about it: its TV client watched the TV server's pushed state, compared the set of
/// entries that were recording against the set it had seen a moment ago, and put a line on the screen for whatever
/// had begun or finished in between (<c>SlimTvClient/Notifications/SlimTvNotificationService.cs</c>). A viewer who
/// asked for the eleven o'clock film to be saved and then went to make tea finds out from the corner of the screen
/// whether the machine did it, without opening a screen to look.
/// </para>
/// <para>
/// <b>The one difference, and it is the backend's.</b> The reference was <i>told</i>: its TV server pushed a state
/// object at the client whenever anything changed, so its notice appeared the second a recording started. TVHeadend
/// answers questions and does not push anything at anybody over the interface this front end speaks, so the only way
/// to find out is to ask. It is asked every <see cref="LOOK_SECONDS"/> seconds, which means a notice can arrive up
/// to half a minute after the recording did. That is the honest cost of a backend that does not volunteer, and it is
/// paid gladly: a recording a viewer learns about within half a minute is still a recording they learned about, and
/// the question costs one small answer every half minute, on a machine that is already being asked for a programme
/// guide.
/// </para>
/// <para>
/// <b>The first look says nothing.</b> Kept exactly from the reference, and the reason it gave for it: a front end
/// that started up in the middle of an afternoon would otherwise announce every recording that had started before it
/// existed, as though it had just happened, and the first thing a viewer heard from a machine they had just switched
/// on would be a lie about when things happened. The first look only remembers what is recording. It does still
/// light the recording marker, because that one is not a claim about a moment - it is true right now.
/// </para>
/// <para>
/// <b>The answers are read again at every look.</b> Not once at startup: the switches for this are on the television
/// settings screen, and a household that switches the notices off should not have to restart the front end for the
/// screen to go quiet. The watching itself does not stop when the notices are switched off - the set of recordings
/// keeps being remembered - because a watcher that stopped comparing would announce the whole afternoon at once the
/// moment the switches were turned back on.
/// </para>
/// </remarks>
public sealed class TvRecordingWatcher : IDisposable
{
  /// <summary>
  /// How long the watcher waits between looks, in seconds. Half a minute, which is half the shortest programme
  /// TVHeadend's guides bother to list and short enough that a notice is still about the recording it names.
  /// </summary>
  public const int LOOK_SECONDS = 30;

  private readonly Func<TvBackendSettings> _settingsProvider;
  private readonly Func<TvBackendSettings, TvHeadendApiClient> _buildClient;
  private readonly Action<Action> _marshal;
  private readonly Action<Action, TimeSpan> _wake;

  /// <summary>
  /// The entries that were recording at the last look, by the backend's own number for them. What is in here and
  /// what is recording now is the whole of what the watcher knows.
  /// </summary>
  private readonly Dictionary<string, TvRecording> _seen = new Dictionary<string, TvRecording>();

  private bool _looked;
  private bool _running;
  private bool _recording;
  private bool _disposed;
  private string _notice = "";

  /// <param name="settingsProvider">
  /// Where the backend's address and the three answers about notices come from, read again at every look.
  /// </param>
  /// <param name="clientFactory">How to build the backend to ask. A test hands in a scripted one.</param>
  /// <param name="marshal">
  /// How to get a sentence onto the thread the screen lives on, or null to hand it over where it was noticed. A
  /// backend across the network answers on a thread of its own, and the corner of a window it is about to speak in
  /// belongs to the thread that made the window.
  /// </param>
  /// <param name="wake">
  /// How to be woken up for the next look, or null for the ordinary way: a task that waits and then carries on. A
  /// test hands in something it can fire by hand, because a test of a recording notice should not take half a minute
  /// and a test of a notice going away should not take five.
  /// </param>
  public TvRecordingWatcher(Func<TvBackendSettings>? settingsProvider = null,
      Func<TvBackendSettings, TvHeadendApiClient>? clientFactory = null, Action<Action>? marshal = null,
      Action<Action, TimeSpan>? wake = null)
  {
    _settingsProvider = settingsProvider ?? delegate { return TvBackendSettings.Load(null, out _); };
    _buildClient = clientFactory ?? delegate (TvBackendSettings settings)
    {
      return DemoTvBackend.Open(settings);
    };
    _marshal = marshal ?? delegate (Action action) { action(); };
    _wake = wake ?? WakeByWaiting;
  }

  /// <summary>
  /// A sentence to show at the corner of the screen, or null when the corner is to be cleared. The watcher shows one
  /// at a time and clears it again by itself after the time the viewer asked for.
  /// </summary>
  public event Action<string?>? NoticeChanged;

  /// <summary>
  /// Whether the backend is writing something to disk right now. True from the first look onwards while anything is
  /// recording, which is what the reference's recording marker did - it described the moment, not an event.
  /// </summary>
  public event Action<bool>? RecordingChanged;

  /// <summary>Whether the backend is recording anything, as far as the watcher knows.</summary>
  public bool IsRecording
  {
    get { return _recording; }
  }

  /// <summary>The sentence the watcher is showing, or an empty string when it is showing none.</summary>
  public string Notice
  {
    get { return _notice; }
  }

  /// <summary>
  /// Starts watching: one look straight away, and then one every <see cref="LOOK_SECONDS"/> seconds. Asking twice
  /// is asking once.
  /// </summary>
  public void Start()
  {
    if (_disposed || _running)
      return;

    _running = true;
    Look();
  }

  /// <summary>Stops watching. A notice already standing on the screen is left alone; whoever stopped this clears it.</summary>
  public void Stop()
  {
    _running = false;
  }

  /// <summary>
  /// Asks the backend what it is recording and says whatever has changed since the last look. Safe to call by hand,
  /// which is what a test and a "look now" button both do; nothing an answer can throw escapes from here.
  /// </summary>
  public void Look()
  {
    if (_disposed)
      return;

    TvBackendSettings settings;
    try
    {
      settings = _settingsProvider();
    }
    catch (Exception)
    {
      Schedule();
      return;
    }

    if (settings == null || !settings.IsConfigured)
    {
      // A box with no television machine written down has nothing to watch. Its recording marker goes out, because
      // the marker being lit would be a claim about a machine that is not there.
      SayRecording(false);
      Schedule();
      return;
    }

    try
    {
      AskAsync(settings);
    }
    catch (Exception)
    {
      // Nothing to do with a watcher that could not start asking; the next look is in half a minute anyway.
      Schedule();
    }
  }

  private async void AskAsync(TvBackendSettings settings)
  {
    List<TvRecording> recording = new List<TvRecording>();
    bool asked = false;

    try
    {
      using (TvHeadendApiClient client = _buildClient(settings))
      {
        // The table of what is planned and what is happening, which is the only one an entry that is being written
        // now can be found in. The finished ones are not looked at: a recording ends by leaving this table, and the
        // reference's notice for an ending was raised by exactly that disappearance.
        IReadOnlyList<TvRecording> entries = await client.GetRecordingsAsync(TvRecordingTable.Upcoming)
            .ConfigureAwait(false);

        foreach (TvRecording entry in entries)
        {
          if (entry.IsRecording)
            recording.Add(entry);
        }
      }

      asked = true;
    }
    catch (Exception)
    {
      // A television machine that will not answer is not a recording that ended. The set this remembers is left
      // alone, so the next look compares against what was last truly known rather than against an empty room, and
      // no household is told that eleven recordings ended because their router blinked.
      Schedule();
      return;
    }

    if (_disposed)
      return;

    _marshal(delegate { Compare(recording, asked, settings); });
    Schedule();
  }

  /// <summary>
  /// Compares what is recording now against what the last look saw, and says what began and what stopped. When both
  /// changed in the same breath, the ending is the older news, so the beginning is what stands on the screen.
  /// </summary>
  private void Compare(List<TvRecording> recording, bool asked, TvBackendSettings settings)
  {
    if (_disposed || !asked)
      return;

    Dictionary<string, TvRecording> now = new Dictionary<string, TvRecording>();
    foreach (TvRecording entry in recording)
    {
      if (!string.IsNullOrEmpty(entry.Uuid))
        now[entry.Uuid] = entry;
    }

    TvRecording? began = null;
    TvRecording? ended = null;

    foreach (KeyValuePair<string, TvRecording> still in now)
    {
      if (!_seen.ContainsKey(still.Key))
        began = still.Value;
    }

    foreach (KeyValuePair<string, TvRecording> before in _seen)
    {
      if (!now.ContainsKey(before.Key))
        ended = before.Value;
    }

    bool first = !_looked;
    _looked = true;
    _seen.Clear();
    foreach (KeyValuePair<string, TvRecording> entry in now)
      _seen[entry.Key] = entry.Value;

    SayRecording(now.Count > 0);
    if (first)
      return;

    if (!settings.WantsRecordingNotices)
      return;

    TvRecording? tell = began ?? ended;
    if (tell == null)
      return;

    if (began == null && !settings.ShowRecordingEndedNotifications)
      return;
    if (began != null && !settings.ShowRecordingStartedNotifications)
      return;

    Show(string.Format("{0}: {1} on {2}", began != null ? "Recording started" : "Recording ended",
        tell.Title, tell.ChannelName), settings.RecordingNotificationSecondsOr);
  }

  /// <summary>
  /// Puts a sentence at the corner of the screen and arranges for it to go away again. A notice already standing
  /// there is replaced - two recordings ending in one breath is one sentence about the last of them, which is what
  /// the reference's single notification slot did too.
  /// </summary>
  private void Show(string sentence, double seconds)
  {
    _notice = sentence;
    NoticeChanged?.Invoke(sentence);

    if (seconds <= 0)
      return;

    string about = sentence;
    _wake(delegate
    {
      // Only this notice goes away. A notice raised after this one was scheduled owns the corner by then, and an
      // older timer has no business clearing somebody newer's news.
      if (!_disposed && string.Equals(_notice, about, StringComparison.Ordinal))
        Clear();
    }, TimeSpan.FromSeconds(seconds));
  }

  private void Clear()
  {
    if (string.IsNullOrEmpty(_notice))
      return;

    _notice = "";
    NoticeChanged?.Invoke(null);
  }

  private void SayRecording(bool recording)
  {
    if (_disposed || _recording == recording)
      return;

    _recording = recording;
    RecordingChanged?.Invoke(recording);
  }

  /// <summary>Arranges the next look, unless somebody stopped this one.</summary>
  private void Schedule()
  {
    if (_disposed || !_running)
      return;

    // The guard is inside the thing being woken as well as here, because whoever was asked to do the waking may have
    // been asked a long time ago: a watcher that was stopped while a wait was outstanding should stay stopped rather
    /// than ask one last question when the wait happens to come due. Asking by hand still works either way, which is
    /// what a "look now" on a settings screen wants.
    _wake(delegate
    {
      if (_disposed || !_running)
        return;

      Look();
    }, TimeSpan.FromSeconds(LOOK_SECONDS));
  }

  private static void WakeByWaiting(Action action, TimeSpan after)
  {
    Task.Delay(after).ContinueWith(delegate (Task ignored)
    {
      try
      {
        action();
      }
      catch (Exception)
      {
        // A watcher whose next look threw is a watcher that stopped telling people about recordings. Nothing else
        // in the front end is affected, and there is nobody awake to report it to.
      }
    }, TaskScheduler.Default);
  }

  public void Dispose()
  {
    _disposed = true;
    _running = false;
  }
}
