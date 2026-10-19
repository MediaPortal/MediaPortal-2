#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Player;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One media item, as a screen shows it: what it is, where it has got to, and what the remote can do to it now.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the only place the front end and the player meet. It takes an <see cref="IVideoPlayer"/> and nothing
  /// else - no engine, no window, no Avalonia type - which is what lets the whole of playback be tested with a
  /// fake player, on a machine with no libVLC and no display, including in the Linux container.
  /// </para>
  /// <para>
  /// <b>Threading, which is the reason for the second constructor argument.</b> Every event of
  /// <see cref="IVideoPlayer"/> arrives on the engine's thread. A property this class raises ends up on a bound
  /// control, and touching a bound control from the wrong thread is the crash this project has already been
  /// killed by once (see the render probe's <c>ObjectDisposedException</c> - a different object, the same class of
  /// bug: work finishing on a thread that does not own the thing it is updating). So every <em>engine event</em>
  /// is handed to the caller's way onto the screen's thread - <c>action =&gt; Dispatcher.UIThread.Post(action)</c>
  /// from a window - and passing null runs it where it arrives, which is what a test or a designer wants and is
  /// said out loud rather than inferred.
  /// </para>
  /// <para>
  /// The other half of that rule matters just as much: a call the screen made itself - <see cref="Play"/>,
  /// <see cref="TogglePause"/>, <see cref="SetVolume"/>, and the refusal of any of them - is <em>not</em>
  /// marshalled, because it already runs on the screen's own thread. Wrapping those in a post is how a screen ends
  /// up answering a key press after the next one, which on a remote control reads as "nothing happened".
  /// </para>
  /// <para>
  /// Nothing here polls for position. The player raises <see cref="IVideoPlayer.PositionChanged"/> when it moves,
  /// so a screen that used a timer would be asking a question the engine had already answered.
  /// </para>
  /// </remarks>
  public sealed class NowPlayingViewModel : INotifyPropertyChanged, IDisposable
  {
    private readonly IVideoPlayer? _player;
    private readonly Action<Action> _marshal;
    private bool _disposed;

    private string _title;
    private string _stateText;
    private string _positionText;
    private string _statusLine;
    private double _progress;
    private bool _isPlaying;
    private bool _isPaused;
    private bool _hasMedia;
    private int _volume = 100;
    private bool _muted;

    private List<PlaybackTrack> _audioTracks = new List<PlaybackTrack>();
    private List<PlaybackTrack> _subtitleTracks = new List<PlaybackTrack>();
    private int _realSubtitleCount;
    private int _audioCurrent = -1;
    private int _subtitleCurrent = -1;

    /// <summary>
    /// The library title the current media came from, or null for a file that was opened without one. Only with an
    /// id is there anything to tell the server about.
    /// </summary>
    private Guid? _mediaItemId;

    /// <summary>
    /// How far into the current media the viewer got. The furthest, not the last: a viewer who watches to the end,
    /// rewinds two minutes to show somebody a scene, and then closes the screen did watch most of the film.
    /// </summary>
    private long _furthestMs;

    /// <summary>Whether the current media was already reported, so that stopping after the end says it once.</summary>
    private bool _reported;

    /// <summary>
    /// How far into the media being opened the viewer already got last time, 0 for "from the beginning".
    /// </summary>
    private int _resumePercent;

    /// <summary>
    /// Whether the seek for <see cref="_resumePercent"/> was carried out. A resume is applied once, on the first
    /// position event that brings a length with it, and never again for that media.
    /// </summary>
    private bool _resumeApplied;

    /// <summary>
    /// The entry that switches subtitles off. Id -1 is the engine's own convention for that (see
    /// <see cref="PlaybackTrack"/>), so the list a viewer picks from and the number the engine gets are the same
    /// idea rather than two.
    /// </summary>
    private static readonly PlaybackTrack SubtitleOff = new PlaybackTrack(TrackKind.Subtitle, -1, "no subtitles");

    /// <param name="player">The player to drive, or null for a screen that has no engine on this machine.</param>
    /// <param name="marshal">
    /// How to get work onto the thread the screen lives on. Null runs it where it arrives.
    /// </param>
    public NowPlayingViewModel(IVideoPlayer? player, Action<Action>? marshal = null)
    {
      _player = player;
      _marshal = marshal ?? delegate (Action action) { action(); };
      _title = "Nothing playing";
      _stateText = "Idle";
      _positionText = "--:-- / --:--";
      _statusLine = player == null
          ? "This build has no player"
          : "Nothing playing";
      _volume = player?.Volume ?? 100;
      _muted = player?.Muted ?? false;

      if (player != null)
      {
        player.StateChanged += OnPlayerEvent;
        player.PositionChanged += OnPositionChanged;
        player.Failed += OnPlayerFailed;
        player.EndOfMedia += OnEndOfMedia;
        // The state the player was built in is not Idle in the way a screen means it, so the first reading comes
        // from the player itself rather than from this class's own guess.
        DescribeState(player.State, null);
      }
    }

    /// <summary>Raised when any of the properties below changed - always through the marshaller.</summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Raised for the bindings in the XAML. The screen itself listens to <see cref="Changed"/>, which fires
    /// once; this fires for every property at once (a <c>null</c> property name), because keeping the two
    /// events in step property by property would be a second bookkeeping nobody checks.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// The one instance used for every notification: a <c>null</c> property name tells a binding to read all
    /// of them again.
    /// </summary>
    private static readonly PropertyChangedEventArgs AllProperties = new PropertyChangedEventArgs(null);

    /// <summary>What is loaded, in one line. Never empty.</summary>
    public string Title
    {
      get { return _title; }
    }

    /// <summary>"Playing", "Paused", "Starting", "Finished", "Stopped", "Failed" - the word a screen shows.</summary>
    public string StateText
    {
      get { return _stateText; }
    }

    /// <summary>e.g. "0:12 / 1:04:05". The lengths come from the engine, never from a guess.</summary>
    public string PositionText
    {
      get { return _positionText; }
    }

    /// <summary>
    /// The one line a viewer reads when something is wrong, including the engine's own sentence when it refused
    /// something. Never empty.
    /// </summary>
    public string StatusLine
    {
      get { return _statusLine; }
    }

    /// <summary>0 to 1 for a progress bar; 0 when nothing is loaded or the length is unknown.</summary>
    public double Progress
    {
      get { return _progress; }
    }

    public bool IsPlaying
    {
      get { return _isPlaying; }
    }

    public bool IsPaused
    {
      get { return _isPaused; }
    }

    /// <summary>False until something was opened, which is when the transport buttons stop being decoration.</summary>
    public bool HasMedia
    {
      get { return _hasMedia; }
    }

    public int Volume
    {
      get { return _volume; }
    }

    public bool Muted
    {
      get { return _muted; }
    }

    /// <summary>What the remote's OK button means right now: play nothing, resume, or pause.</summary>
    public string TransportLabel
    {
      get
      {
        if (!_hasMedia)
          return "Nothing to play";
        return _isPaused ? "Resume" : _isPlaying ? "Pause" : "Play";
      }
    }

    /// <summary>
    /// The audio tracks of what is loaded, as the engine named them. Empty before something plays, and empty for
    /// a media with a single track - which is the same answer, and the reason <see cref="HasTrackChoice"/> exists.
    /// </summary>
    public IReadOnlyList<PlaybackTrack> AudioTracks
    {
      get { return _audioTracks; }
    }

    /// <summary>
    /// The subtitle tracks, with the "no subtitles" entry first: a viewer who wants them back off has to be able
    /// to reach that too, and a list that only goes one way is a viewer stuck with subtitles.
    /// </summary>
    public IReadOnlyList<PlaybackTrack> SubtitleTracks
    {
      get { return _subtitleTracks; }
    }

    /// <summary>True when there is something to pick, which is when a screen shows the track buttons at all.</summary>
    public bool HasTrackChoice
    {
      get { return _audioTracks.Count > 1 || _realSubtitleCount > 1; }
    }

    /// <summary>e.g. "Sound: eng - English (Original)". The line a screen shows beside the buttons.</summary>
    public string AudioTrackLine
    {
      get { return "Sound: " + Describe(_audioTracks, _audioCurrent); }
    }

    /// <summary>e.g. "Subtitles: none".</summary>
    public string SubtitleTrackLine
    {
      get { return "Subtitles: " + Describe(_subtitleTracks, _subtitleCurrent); }
    }

    /// <summary>
    /// What the viewer told the player before this machine started: start volume and the languages to reach
    /// for. Null (the default, and what a test host wears) means the media's own answers stand untouched.
    /// </summary>
    public Settings.PlaybackSettings? Preferences { get; set; }

    /// <summary>How much one press of the volume key moves, as the viewer's answer or the shipped 5.</summary>
    public int VolumeStep => Preferences?.VolumeStep ?? 5;

    /// <summary>
    /// How far one instant skip reaches in the given direction, in milliseconds: the reference's
    /// <c>InstantSkip</c> semantics - a percentage of the whole title, unless the viewer prefers seconds.
    /// A stream without a length cannot be skipped by percentage, so seconds answer there.
    /// </summary>
    public long InstantSkipMs(int direction)
    {
      Settings.PlaybackSettings? prefs = Preferences;
      if (prefs != null && prefs.PreferInstantSkipSeconds)
        return 1000L * prefs.InstantSkipSeconds * direction;

      long length = _player?.LengthInMs ?? 0;
      if (length <= 0)
        return 1000L * (prefs?.InstantSkipSeconds ?? 5) * direction;

      return (long)(direction * ((prefs?.InstantSkipPercent ?? 20) / 100.0) * length);
    }

    /// <summary>What an instant-skip jump is worth right now, written the way the reference counts it.</summary>
    public string InstantSkipCaption
    {
      get
      {
        Settings.PlaybackSettings? prefs = Preferences;
        if (prefs != null && prefs.PreferInstantSkipSeconds)
          return prefs.InstantSkipSeconds + " s";
        return (prefs?.InstantSkipPercent ?? 20) + " %";
      }
    }

    /// <summary>
    /// Asks the engine what tracks the loaded media has. Called when a media starts playing, and safe to call
    /// again after a chapter change; it does nothing when there is no media, so that an idle screen keeps an
    /// empty list instead of the previous film's tracks.
    /// </summary>
    public void ReadTracks()
    {
      if (_disposed || _player == null || !_hasMedia)
        return;

      _audioTracks = new List<PlaybackTrack>(_player.Tracks(TrackKind.Audio));
      List<PlaybackTrack> subtitles = new List<PlaybackTrack>(_player.Tracks(TrackKind.Subtitle));
      _realSubtitleCount = subtitles.Count;
      subtitles.Insert(0, SubtitleOff);
      _subtitleTracks = subtitles;
      _audioCurrent = _player.CurrentTrackId(TrackKind.Audio);
      _subtitleCurrent = _player.CurrentTrackId(TrackKind.Subtitle);
      ApplyTrackPreferences(subtitles);
      Raise();
    }

    /// <summary>
    /// Honours the viewer's language answers once per media, the moment the media's tracks are known. A
    /// preference, never a command: a language nobody speaks with a label stays unheard, the media's own
    /// choice stands, and switching "Subtitles on" off switches the kind off only if the media had picked
    /// one for itself.
    /// </summary>
    private void ApplyTrackPreferences(List<PlaybackTrack> realSubtitles)
    {
      Settings.PlaybackSettings? prefs = Preferences;
      if (prefs == null || _trackPreferencesDone)
        return;
      _trackPreferencesDone = true;

      if (prefs.AudioLanguage.Length > 0 && !SpeaksLanguage(_audioTracks, _audioCurrent, prefs.AudioLanguage))
      {
        foreach (PlaybackTrack track in _audioTracks)
          if (string.Equals(track.Language, prefs.AudioLanguage, StringComparison.OrdinalIgnoreCase))
          {
            if (_player!.SelectTrack(track))
              _audioCurrent = track.Id;
            break;
          }
      }

      if (!prefs.SubtitlesEnabled)
      {
        if (_subtitleCurrent >= 0)
        {
          if (_player!.SelectTrack(SubtitleOff))
            _subtitleCurrent = -1;
        }
        return;
      }

      if (prefs.SubtitleLanguage.Length > 0 && !SpeaksLanguage(realSubtitles, _subtitleCurrent, prefs.SubtitleLanguage))
      {
        foreach (PlaybackTrack track in realSubtitles)
          if (string.Equals(track.Language, prefs.SubtitleLanguage, StringComparison.OrdinalIgnoreCase))
          {
            if (_player!.SelectTrack(track))
              _subtitleCurrent = track.Id;
            break;
          }
      }
    }

    /// <summary>Whether the track this player is using for a kind says the wanted language. -1 never does.</summary>
    private static bool SpeaksLanguage(List<PlaybackTrack> tracks, int currentId, string language)
    {
      foreach (PlaybackTrack track in tracks)
        if (track.Id == currentId)
          return string.Equals(track.Language, language, StringComparison.OrdinalIgnoreCase);

      return false;
    }

    private bool _trackPreferencesDone;

    /// <summary>Switches to the next audio track, wrapping around. False when the engine refused.</summary>
    public bool NextAudioTrack()
    {
      return SelectNext(TrackKind.Audio);
    }

    /// <summary>Switches to the next subtitle, wrapping around through "no subtitles".</summary>
    public bool NextSubtitleTrack()
    {
      return SelectNext(TrackKind.Subtitle);
    }

    /// <summary>Switches to one of the listed tracks. The engine's answer, with the reason on the status line.</summary>
    public bool SelectTrack(PlaybackTrack track)
    {
      if (_player == null || !_hasMedia)
        return Refuse("nothing is playing.");
      if (!_player.SelectTrack(track))
        return Refuse("the player would not switch to " + track.Label + ".");

      if (track.Kind == TrackKind.Audio)
        _audioCurrent = track.Id;
      else
        _subtitleCurrent = track.Id;
      Raise();
      return true;
    }

    /// <summary>
    /// Moves one entry along in the list for a kind, from wherever the engine says we are. Wraps, because a
    /// viewer with four audio tracks should not have to press the button four times to get back to the first.
    /// </summary>
    private bool SelectNext(TrackKind kind)
    {
      if (_player == null || !_hasMedia)
        return Refuse("nothing is playing.");

      List<PlaybackTrack> list = kind == TrackKind.Audio ? _audioTracks : _subtitleTracks;
      int current = kind == TrackKind.Audio ? _audioCurrent : _subtitleCurrent;
      if (list.Count == 0)
        return Refuse("this media has no " + (kind == TrackKind.Audio ? "sound tracks." : "subtitles."));

      int at = list.FindIndex(delegate (PlaybackTrack track) { return track.Id == current; });
      PlaybackTrack wanted = list[(at + 1) % list.Count];
      return SelectTrack(wanted);
    }

    /// <summary>The label of the track with <paramref name="current"/> id, or the same wording a picker shows.</summary>
    private static string Describe(List<PlaybackTrack> tracks, int current)
    {
      foreach (PlaybackTrack track in tracks)
      {
        if (track.Id == current)
          return track.Label;
      }
      // An engine can name a track it has not listed (a stream it added itself) or report -1 for a kind with no
      // off entry; "not known" is the truth then, and better than showing the first track as if it were chosen.
      return current < 0 ? "none" : "not known";
    }

    /// <summary>
    /// Loads and starts a media. False, with the reason in <see cref="StatusLine"/>, when there is no player or
    /// the engine refused it.
    /// </summary>
    /// <summary>
    /// Raised when a viewing has a final answer to "how far did they get": the media ended, the viewer pressed
    /// Stop, or another media was opened on top of it.
    /// </summary>
    /// <remarks>
    /// Only raised for media that were opened with a library id (<see cref="Play(MediaLocation, Guid?)"/>), and
    /// only once per media. Whoever listens owns what happens next: the screen has no idea whether this machine
    /// is talking to a server, and a report that fails is not the viewer's problem to see.
    /// </remarks>
    public event EventHandler<PlaybackReportedEventArgs>? PlaybackReported;

    /// <summary>
    /// The library title being played, or null for a file that was opened without one.
    /// </summary>
    public Guid? MediaItemId
    {
      get { return _mediaItemId; }
    }

    /// <summary>
    /// How to ask the library what it knows about a title, set by the application when there is a server to
    /// ask. A machine that only plays files has no library to read, and the media info then says exactly that
    /// rather than nothing - the reference's info screen was never silent on purpose, only on absence.
    /// </summary>
    public Func<Guid, Task<MediaItemDetails>>? DetailsReader { get; set; }

    private MediaItemDetails? _mediaInfo;

    /// <summary>Which title <see cref="_mediaInfo"/> was read for, so the same film is asked once.</summary>
    private Guid? _mediaInfoFor;

    /// <summary>
    /// What the library says about the title being played, or null while nobody has asked or the file belongs
    /// to no library.
    /// </summary>
    public MediaItemDetails? MediaInfo
    {
      get { return _mediaInfo; }
      private set
      {
        _mediaInfo = value;
        Raise();
      }
    }

    /// <summary>
    /// The whole of the media info as the screen reads it: the film's name and year, its series position if
    /// it has one, its kinds, dates, length, rating, the file the player is really reading, and the longest
    /// prose the library carries. Never empty, because an info panel that shows nothing looks broken rather
    /// than informed.
    /// </summary>
    public string MediaInfoText
    {
      get
      {
        MediaItemDetails? details = _mediaInfo;
        if (details == null)
          return _mediaItemId == null
              ? "this file belongs to no library"
              : "the library has not answered yet";
        if (details.Error != null)
          return details.Error;

        List<string> lines = new List<string>();

        string head = details.Title ?? _title;
        if (details.Year != null)
          head += " (" + details.Year.Value + ")";
        if (details.Season != null && details.Episode != null)
          head += "  S" + details.Season.Value.ToString("00", CultureInfo.InvariantCulture)
              + "E" + details.Episode.Value.ToString("00", CultureInfo.InvariantCulture);
        else if (!string.IsNullOrEmpty(details.SeriesName))
          head += "  " + details.SeriesName;
        lines.Add(head);

        if (details.Genres != null && details.Genres.Count > 0)
          lines.Add(string.Join(", ", details.Genres));
        if (details.FirstAired != null)
          lines.Add("first aired "
              + details.FirstAired.Value.ToString("d MMM yyyy", CultureInfo.InvariantCulture));
        if (details.RuntimeMinutes != null)
          lines.Add("runs " + Format(details.RuntimeMinutes.Value * 60_000L));
        if (!string.IsNullOrEmpty(details.Certification))
          lines.Add("rated " + details.Certification);
        if (details.Rating != null)
          lines.Add("your rating: " + details.Rating.Value + "/10");
        else if (details.TotalRating != null)
          lines.Add("rated " + details.TotalRating.Value.ToString("0.0", CultureInfo.InvariantCulture));
        if (!string.IsNullOrEmpty(details.Path))
          lines.Add(details.Path!);

        string? story = details.Description;
        if (!string.IsNullOrEmpty(story))
          lines.Add(story!);

        return string.Join(Environment.NewLine, lines);
      }
    }

    /// <summary>
    /// How to ask the library for the picture of a title - the album's cover, a film's poster - or null while
    /// there is no server. The playback screen shows it behind the frames, so a film hides its own picture by
    /// playing and a song, which has no frames, wears it: the same shape the reference's audio screen had,
    /// arrived at without a new signal for "no pictures expected".
    /// </summary>
    public Func<Guid, Task<byte[]?>>? CoverReader { get; set; }

    private byte[]? _cover;

    /// <summary>Which title <see cref="_cover"/> was read for, so the same title is asked once.</summary>
    private Guid? _coverFor;

    /// <summary>
    /// The bytes of the playing title's cover, or null while none has arrived. A binding turns them into a
    /// picture; this class never holds one, because a view model that held a bitmap could not be tested
    /// without a screen.
    /// </summary>
    public byte[]? Cover
    {
      get { return _cover; }
      private set
      {
        _cover = value;
        Raise();
      }
    }

    /// <summary>
    /// Asks the library once for the playing title's picture. As with the media info: the same title is not
    /// asked twice, a file without a library id is not asked at all, and a refused read is retried the next
    /// time rather than interrupting the film.
    /// </summary>
    public async Task ReadCoverAsync()
    {
      Guid? id = _mediaItemId;
      if (id == null || CoverReader == null || _coverFor == id)
        return;

      try
      {
        byte[]? bytes = await CoverReader(id.Value);
        if (_mediaItemId == id)
          Cover = bytes;
        _coverFor = bytes != null ? id : null;
      }
      catch (Exception)
      {
        _coverFor = null;
      }
    }

    /// <summary>
    /// Asks the library once what it says about the title being played. A second call for the same title is
    /// deliberately not a second read - the library does not rewrite a film's plot while it is watched - and
    /// a file without a library id is not asked at all, because the question would have no subject.
    /// </summary>
    public async Task ReadMediaInfoAsync()
    {
      Guid? id = _mediaItemId;
      if (id == null || DetailsReader == null || _mediaInfoFor == id)
        return;

      try
      {
        MediaItemDetails details = await DetailsReader(id.Value);
        // The film may have moved on while the question was in flight, and an answer about the previous title
        // does not belong on the next title's info panel.
        if (_mediaItemId == id)
          MediaInfo = details;
        _mediaInfoFor = details.Error == null ? id : null;
      }
      catch (Exception)
      {
        // A refused read says "the library has not answered" on the panel and tries again next time; the
        // viewer's film is nobody's business to interrupt over a failed question.
        _mediaInfoFor = null;
      }
    }

    public bool Play(MediaLocation location)
    {
      return Play(location, (Guid?) null);
    }

    /// <summary>
    /// As <see cref="Play(MediaLocation)"/>, for a file the viewer chose in the library.
    /// </summary>
    /// <param name="location">The media to open.</param>
    /// <param name="mediaItemId">
    /// The library title behind it, which is what a finished or abandoned viewing is reported against. Null plays
    /// the file without telling the library anything - which is what <c>--play</c> on the command line is for.
    /// </param>
    public bool Play(MediaLocation location, Guid? mediaItemId)
    {
      return Play(location, mediaItemId, 0);
    }

    /// <summary>
    /// As <see cref="Play(MediaLocation, Guid?)"/>, starting the media where the viewer left it.
    /// </summary>
    /// <param name="location">The media to open.</param>
    /// <param name="mediaItemId">The library title behind it, or null to tell the library nothing.</param>
    /// <param name="resumePercent">
    /// How far the server says this viewer got, 1 to 99. Anything else - nought, a hundred, a negative - starts
    /// the media at the beginning, because a hundred is a film that is finished and nought is a position nobody
    /// needs moved to.
    /// </param>
    /// <remarks>
    /// A percentage and not a time, because a percentage is what the library stores and what survives a re-encode:
    /// a viewer who watched 42% of a film last month is at 42% of it today even if the file was replaced with a
    /// longer cut. It is turned into a position as soon as the engine says how long the media is - see
    /// <see cref="OnPositionChanged"/> - because before that there is no length to take a fraction of, and a seek
    /// issued against a length of nought lands at the beginning with the confidence of a command that worked.
    /// </remarks>
    public bool Play(MediaLocation location, Guid? mediaItemId, int resumePercent)
    {
      if (_disposed)
        return Refuse("this screen is closed.");
      if (_player == null)
        return Refuse("no player on this machine.");

      // Named before the engine is asked, because a real engine can raise its first state event while the call is
      // still running, and a listener that woke up on "Playing" next to the previous media's name would be seeing
      // the same one-frame lie as a progress bar holding the last media's position.
      string previousTitle = _title;
      bool previousHasMedia = _hasMedia;
      Guid? previousId = _mediaItemId;

      // How far the viewer got in the previous media, measured before the new one is opened: the engine forgets
      // the length of what it was playing when it is told to open something else, and "how far" without a length
      // has no answer.
      int previousPercentage = PercentageReached(_furthestMs);

      _title = ShortName(location);
      _hasMedia = true;

      // The old media's tracks are not this media's tracks. Emptied before the engine is asked, so that a
      // viewer looking at the screen while a file opens cannot pick a subtitle out of the previous film.
      ClearTracks();
      _trackPreferencesDone = false;

      // Where the sound begins, when the viewer said where it begins. Set before the engine is asked, so
      // the first frame is already as loud as the answer says; mid-media turns by the viewer are never
      // overruled, because this line is only on the road INTO a media.
      if (Preferences != null)
        _player.Volume = Preferences.InitialVolume;

      // Armed before the engine is asked, not after: an engine can raise its first position event while the call
      // to open the media is still running, and a resume armed afterwards would be a seek that never happens for
      // the one media it was meant for.
      _resumePercent = resumePercent >= 1 && resumePercent <= 99 ? resumePercent : 0;
      _resumeApplied = false;

      if (!_player.Open(location))
      {
        _title = previousTitle;
        _hasMedia = previousHasMedia;
        _resumePercent = 0;
        return Refuse(_player.LastFailure ?? "the player gave no reason.");
      }

      // Whatever was playing before this is over now - the next episode, another film, or the same film from the
      // top. It is said here rather than left to the caller because this is the one place that knows how far the
      // viewer actually got.
      Report(previousId, previousPercentage);
      _mediaItemId = mediaItemId;
      // What the library said about the previous title is not what it says about this one.
      _mediaInfo = null;
      _mediaInfoFor = null;
      _cover = null;
      _coverFor = null;
      _furthestMs = 0;
      _reported = false;

      Raise();
      return true;
    }

    /// <summary>Pause when playing, resume when paused. Does nothing when nothing is loaded.</summary>
    public bool TogglePause()
    {
      if (_player == null || !_hasMedia)
        return false;

      // Decided from the state this class was told about, not by asking the engine: the answer must be the one the
      // viewer saw, and a player that has just run out is not pausable whatever this class thinks.
      if (_isPaused)
        return _player.Play() ? true : Refuse(_player.LastFailure ?? "the player gave no reason.");
      if (_isPlaying)
        return _player.Pause() ? true : Refuse(_player.LastFailure ?? "the player cannot pause this media.");
      return Refuse("nothing is playing.");
    }

    /// <summary>Stops and stays on the screen, so that the viewer can start the same item again.</summary>
    public bool Stop()
    {
      if (_player == null || !_hasMedia)
        return false;

      // Measured before the engine is stopped, because a stopped player no longer knows how long the film was,
      // and "43 minutes" without a length is not a percentage.
      int reached = PercentageReached(_furthestMs);
      if (!_player.Stop())
        return Refuse(_player.LastFailure ?? "the player gave no reason.");

      Report(_mediaItemId, reached);
      return true;
    }

    /// <summary>
    /// Moves by <paramref name="deltaMs"/>, clamped to the media. A length of 0 (live, or not known yet) means
    /// the request cannot be answered, and says so instead of jumping to a position nobody asked for.
    /// </summary>
    public bool Skip(long deltaMs)
    {
      if (_player == null || !_hasMedia)
        return false;

      long length = _player.LengthInMs;
      if (length <= 0)
        return Refuse("this stream has no position to move in.");

      long wanted = _player.TimeInMs + deltaMs;
      if (wanted < 0)
        wanted = 0;
      if (wanted > length)
        wanted = length;
      _player.TimeInMs = wanted;
      if (wanted > _furthestMs)
        _furthestMs = wanted;
      return true;
    }

    private SkipStepLadder? _skipLadder;
    private string? _skipLadderList;

    /// <summary>
    /// How long the screen waits after one skip-step press before committing the jump, in milliseconds -
    /// the reference's <c>SkipStepTimeout</c> answer.
    /// </summary>
    public int SkipStepTimeoutMs
    {
      get { return (int)Math.Round((Preferences?.SkipStepTimeoutSeconds ?? 1.5) * 1000); }
    }

    /// <summary>The signed step the last skip-step press landed on, in seconds; see <see cref="SkipStepLadder"/>.</summary>
    public int PendingSkipStepSeconds
    {
      get { return _skipLadder?.PendingStepSeconds ?? 0; }
    }

    /// <summary>One press of the forward skip-step key; see <see cref="SkipStepLadder.Press"/>.</summary>
    public SkipStepAction SkipStepForward()
    {
      return PressSkipStep(1);
    }

    /// <summary>One press of the backward skip-step key; see <see cref="SkipStepLadder.Press"/>.</summary>
    public SkipStepAction SkipStepBackward()
    {
      return PressSkipStep(-1);
    }

    /// <summary>
    /// The pause between skip-step presses ran out: commit the jump the ladder was holding, or the jump to
    /// the border a refused step was parked at. Nothing happens when no ladder is pending.
    /// </summary>
    public void SkipStepElapsed()
    {
      if (_skipLadder == null || _player == null)
        return;

      SkipStepAction action = _skipLadder.Elapsed();
      if (action == SkipStepAction.Skip)
        Skip(1000L * _lastSkipStepSeconds);
      else if (action == SkipStepAction.ToStart)
        Skip(-_player.TimeInMs);
      else if (action == SkipStepAction.ToEnd)
        Skip(_player.LengthInMs - _player.TimeInMs);
    }

    /// <summary>
    /// Drop a pending skip-step jump without making it - what an instant skip does to the ladder, exactly
    /// like the reference cancelling its skip timer when an instant skip arrives.
    /// </summary>
    public void CancelSkipStep()
    {
      _skipLadder?.Cancel();
    }

    private SkipStepAction PressSkipStep(int direction)
    {
      string list = Preferences?.SkipStepList ?? SkipStepLadder.DEFAULT_LIST;
      if (_skipLadder == null || !string.Equals(list, _skipLadderList, StringComparison.Ordinal))
      {
        _skipLadder = new SkipStepLadder(list);
        _skipLadderList = list;
      }

      SkipStepAction action = _skipLadder.Press(direction, CanSkipRelative);
      _lastSkipStepSeconds = _skipLadder.PendingStepSeconds;
      return action;
    }

    private int _lastSkipStepSeconds;

    /// <summary>
    /// Whether a jump of so many seconds stays inside the media - the same question the reference asks
    /// before it lets the ladder climb, and the reason a refused step parks at the border instead.
    /// </summary>
    private bool CanSkipRelative(int seconds)
    {
      if (_player == null || !_hasMedia)
        return false;
      long length = _player.LengthInMs;
      if (length <= 0)
        return false;
      long wanted = _player.TimeInMs + 1000L * seconds;
      return wanted >= 0 && wanted <= length;
    }

    /// <summary>Sets volume; values outside 0-100 are clamped, which is what a volume key should do.</summary>
    public bool SetVolume(int value)
    {
      if (_player == null)
        return false;
      int clamped = value < 0 ? 0 : (value > 100 ? 100 : value);
      _player.Volume = clamped;
      _volume = clamped;
      Raise();
      return true;
    }

    public bool ToggleMute()
    {
      if (_player == null)
        return false;
      bool wanted = !_muted;
      _player.Muted = wanted;
      _muted = wanted;
      Raise();
      return true;
    }

    /// <summary>
    /// Gives the screen the picture's window, or says why there is none. The window handle is a platform's
    /// business, so whoever owns one calls this - the view model could not check it any better than the player can.
    /// </summary>
    public bool AttachVideo(nint videoTarget)
    {
      if (_player == null)
        return Refuse("no player on this machine.");
      return _player.AttachTo(videoTarget)
          ? true
          : Refuse(_player.LastFailure ?? "this player draws into no window.");
    }

    /// <summary>
    /// Take the picture as frames instead of as somebody else's window, so that this screen's own
    /// <see cref="VideoSurface"/> can draw them. Ask before anything is playing: an engine decides how it will
    /// deliver pictures when a media starts, and a sink offered after that arrives too late to be used.
    /// </summary>
    /// <returns>
    /// True when the frames will come. False means the playback screen has no picture, and says so on its own
    /// status line - which is a worse screen and a better outcome than a black rectangle that might be a paused
    /// film.
    /// </returns>
    public bool AttachVideo(VideoFrameSink frames)
    {
      if (_player == null)
        return Refuse("no player on this machine.");
      return _player.AttachTo(frames)
          ? true
          : Refuse(_player.LastFailure ?? "this player hands out no pictures.");
    }

    public void Dispose()
    {
      if (_disposed)
        return;
      _disposed = true;
      if (_player != null)
      {
        _player.StateChanged -= OnPlayerEvent;
        _player.PositionChanged -= OnPositionChanged;
        _player.Failed -= OnPlayerFailed;
        _player.EndOfMedia -= OnEndOfMedia;
      }
    }

    private void OnPlayerEvent(object? sender, PlayerStateChangedEventArgs args)
    {
      _marshal(delegate
      {
        DescribeState(args.NewState, null);
        // A media names its own tracks when it starts, and an engine that was not told them yet reports none, so
        // the moment to ask is when it reached "Playing" - not on the position events, which come several a second.
        if (args.NewState == PlayerState.Playing)
          ReadTracks();
      });
    }

    private void OnPositionChanged(object? sender, PlaybackPositionEventArgs args)
    {
      // The resume seek, applied the first time an event says how long the media is. Out here rather than inside
      // the marshaller on purpose: the seek is the engine's business and can be issued from the thread the engine
      // is on, while waiting for a UI turn would have the viewer watch the first minutes of a film they had
      // already seen before the seek landed at all.
      long resumedToMs = 0;
      int resumedPercent = 0;
      if (_resumePercent > 0 && !_resumeApplied && args.LengthInMs > 0 && _player != null)
      {
        resumedPercent = _resumePercent;
        _resumeApplied = true;
        _resumePercent = 0;
        resumedToMs = args.LengthInMs * resumedPercent / 100;
        _player.TimeInMs = resumedToMs;
      }

      long resume = resumedToMs;
      int percent = resumedPercent;
      _marshal(delegate
      {
        if (resume > 0)
          // Said out loud because a film that starts twelve minutes in looks like a broken player unless the
          // screen says that was on purpose.
          _statusLine = "Resumed at " + percent + "% - " + Format(resume);

        if (args.TimeInMs > _furthestMs)
          _furthestMs = args.TimeInMs;
        _positionText = Format(args.TimeInMs) + " / " +
                        (args.LengthInMs > 0 ? Format(args.LengthInMs) : "--:--");
        double fraction = args.Fraction;
        _progress = fraction < 0 ? 0 : fraction;
        Raise();
      });
    }

    private void OnPlayerFailed(object? sender, PlayerFailedEventArgs args)
    {
      _marshal(delegate
      {
        _stateText = "Failed";
        _isPlaying = false;
        _isPaused = false;
        _statusLine = string.IsNullOrWhiteSpace(args.Reason)
            ? "The player stopped without saying why."
            : args.Reason!;
        Raise();
      });
    }

    private void OnEndOfMedia(object? sender, EventArgs args)
    {
      _marshal(delegate
      {
        // The engine also moves to Ended, so the state text arrives through the state event; what this adds is
        // the position of the very end, because a bar that stops short of the end reads as a stalled player.
        _statusLine = "Finished";

        // The one place a completed viewing is decided. Not "99% and then it stopped", which is a player that
        // ran out of stream, and not the last position, which after the credits is the end of a file rather than
        // the end of a film the viewer watched.
        Report(_mediaItemId, 100, engineEnded: true);
        Raise();
      });
    }

    /// <summary>
    /// Says once what the viewer got of a title, to whoever is listening.
    /// </summary>
    /// <param name="mediaItemId">The library title, or null when the media was opened without one.</param>
    /// <param name="percentage">
    /// 1 to 100. Zero means there is nothing worth saying: ninety seconds of a film somebody abandoned during the
    /// studio logos is not a fact about that film, and writing it down would make "never watched" unanswerable.
    /// </param>
    /// <param name="engineEnded">
    /// Whether the player itself said this media ended - the one voice that says "finished", carried on to
    /// whoever listens (see <see cref="PlaybackReportedEventArgs.EngineEnded"/>).
    /// </param>
    /// <remarks>
    /// The reference's rule for what leaves a mark lives here (<c>PlayerContextManager.NotifyPlayback</c>):
    /// a viewing that reached the configured percentage counts as watched, and since the library door
    /// understands "watched" as the hundred, the report is raised to it. The reference additionally wrote a
    /// watched-but-not-last-part media as fifty so the next part would resume; this front end reports one
    /// file at a time and has no "part" to ask, so only the first half of that rule applies.
    /// </remarks>
    private void Report(Guid? mediaItemId, int percentage, bool engineEnded = false)
    {
      if (_reported || percentage < 1 || mediaItemId == null || mediaItemId == Guid.Empty)
        return;

      int mark = Preferences?.WatchedMarkPercentage ?? DEFAULT_WATCHED_MARK;
      if (percentage >= mark)
        percentage = 100;

      EventHandler<PlaybackReportedEventArgs>? listeners = PlaybackReported;
      if (listeners == null)
        return;

      _reported = true;
      listeners(this, new PlaybackReportedEventArgs(_mediaItemId.Value, percentage, engineEnded));
    }

    /// <summary>The reference's own answer, used when this screen was handed no preferences at all.</summary>
    private const int DEFAULT_WATCHED_MARK = 90;

    /// <summary>
    /// How far the viewer got, as a whole percentage rounded down, and never a hundred.
    /// </summary>
    /// <remarks>
    /// Stopping at 99.5% of a file is a person who left, and the measured number itself never reads a
    /// hundred - only the engine saying "that media ended" produces one there (see
    /// <see cref="OnEndOfMedia"/>). What the library is told is a different question: a measurement past the
    /// watched mark travels as the hundred the library door understands (see <see cref="Report"/>). A length
    /// of zero - live TV, a stream still being probed, anything a player holds no position in - has no
    /// percentage to report and says so with a zero.
    /// </remarks>
    private int PercentageReached(long reachedMs)
    {
      if (_player == null || reachedMs <= 0)
        return 0;

      long length = _player.LengthInMs;
      if (length <= 0)
        return 0;

      long percent = reachedMs * 100 / length;
      return percent > 99 ? 99 : (int) percent;
    }

    /// <summary>Turns a state into the words a viewer reads. One place, so the words cannot disagree.</summary>
    private void DescribeState(PlayerState state, string? extra)
    {
      _stateText = state switch
      {
        PlayerState.Idle => "Idle",
        PlayerState.Opening => "Starting",
        PlayerState.Playing => "Playing",
        PlayerState.Paused => "Paused",
        PlayerState.Stopped => "Stopped",
        PlayerState.Ended => "Finished",
        PlayerState.Failed => "Failed",
        _ => state.ToString()
      };

      _isPlaying = state == PlayerState.Playing;
      _isPaused = state == PlayerState.Paused;
      if (state == PlayerState.Idle || state == PlayerState.Stopped)
        _hasMedia = false;
      if (state == PlayerState.Failed)
        _statusLine = extra ?? "The player failed.";
      if (extra != null && state != PlayerState.Failed)
        _statusLine = extra;

      Raise();
    }

    /// <summary>
    /// Records why a call the screen made did not happen. Never marshalled: the caller of a refusal already is the
    /// screen, and a viewer who pressed a key has to see the reason in the same frame as the key.
    /// </summary>
    private bool Refuse(string reason)
    {
      _statusLine = reason;
      Raise();
      return false;
    }

    private void ClearTracks()
    {
      _audioTracks = new List<PlaybackTrack>();
      _subtitleTracks = new List<PlaybackTrack>();
      _realSubtitleCount = 0;
      _audioCurrent = -1;
      _subtitleCurrent = -1;
    }

    private void Raise()
    {
      if (_disposed)
        return;
      Changed?.Invoke(this, EventArgs.Empty);

      // Every property at once rather than only the ones that moved. The alternative - comparing each field
      // with its old value - would need the old values, and this fan-out happens once per engine event at
      // most, which a screen with eight bound texts notices not at all.
      PropertyChanged?.Invoke(this, AllProperties);
    }

    /// <summary>
    /// "1:02:03" above an hour, "4:05" below it. Negative values - which an engine uses for "unknown" and the
    /// seam already turns into 0 - are shown as the same dashes as an unknown length, never as a subtraction.
    /// </summary>
    /// <remarks>
    /// The separator is deliberately the invariant one. A culture's time separator is not a piece of prose to
    /// translate here: a timer on a TV set reads as a duration, and on the cultures whose separator is a dot the
    /// same media would show "1.23" where every other build shows "1:23".
    /// </remarks>
    public static string Format(long milliseconds)
    {
      if (milliseconds < 0)
        return "--:--";
      TimeSpan span = TimeSpan.FromMilliseconds(milliseconds);
      string format = span.TotalHours >= 1
          ? @"h\:mm\:ss"
          : @"m\:ss";
      return span.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The last segment of a path or the host and path of a URL, because a title strip that shows
    /// "smb://server/films/..." in full is unreadable on a TV. Credentials are already gone - the location's own
    /// <c>Describe()</c> removes them, so this cannot reintroduce them.
    /// </summary>
    private static string ShortName(MediaLocation location)
    {
      string text = location.Describe();
      if (location.IsFile)
      {
        string name = System.IO.Path.GetFileName(text);
        return string.IsNullOrEmpty(name) ? text : name;
      }
      return text;
    }
  }
}
