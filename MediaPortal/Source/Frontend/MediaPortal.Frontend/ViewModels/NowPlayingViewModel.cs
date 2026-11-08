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
using System.Collections.ObjectModel;
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
    private string _fileName;
    private string _stateText;
    private string _stateGlyph = "";
    private string _positionText;
    private string _positionLabel = "--:--";
    private string _durationLabel = "--:--";
    private long _positionMs;
    private long _lengthMs;
    private string _statusLine;
    private double _progress;
    private bool _isPlaying;
    private bool _isPaused;
    private bool _hasMedia;
    private int _volume = 100;
    private bool _muted;

    // Whether the viewer named a loudness on this screen themselves. Said once and kept: what the viewer left the
    // sound at is what the next tune opens at, instead of the settings' start volume walking in again.
    private bool _volumeAsked;

    private List<PlaybackTrack> _audioTracks = new List<PlaybackTrack>();
    private List<PlaybackTrack> _subtitleTracks = new List<PlaybackTrack>();
    private int _realSubtitleCount;

    /// <summary>
    /// Whether the tracks are worth asking about once more, which is true only when the first ask - the moment the
    /// engine reached "Playing" - came back with nothing at all. An input that is still being opened over a
    /// network announces its elementary streams a second or two late, and a screen that asked exactly once would
    /// then spend the whole film without a subtitle switch on a film that has one.
    /// </summary>
    private bool _tracksWorthAskingAgain;
    private int _audioCurrent = -1;
    private int _subtitleCurrent = -1;

    // What the engine said about the media's chapters and the size and codec of its picture. Both arrive late and
    // both are often never said at all, so they are asked for a few times and then left alone: a film made of one
    // continuous picture has no chapter table, and a stream's container often names no codec.
    private readonly List<PlaybackChapter> _chapters = new List<PlaybackChapter>();
    private PlaybackQuality? _quality;
    private int _chaptersAsksLeft;
    private int _chaptersTicks;

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
      _fileName = "Nothing playing";
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

    /// <summary>
    /// What the file or the stream is called, whatever the viewer called the film by. Shown under the title on
    /// the display, because a film reached over the network is not the file it was cut from, and the person who
    /// wonders which of the three "movie.mkv" copies is playing can read it here.
    /// </summary>
    public string FileName
    {
      get { return _fileName; }
    }

    /// <summary>Whether a line about the file says anything the title line did not already say.</summary>
    public bool ShowsFileName
    {
      get { return _hasMedia && !string.Equals(_fileName, _title, StringComparison.Ordinal); }
    }

    /// <summary>
    /// The year the library gives the title, as the small line under its name. Empty when the library does not
    /// know one (or has not answered yet), which is what hides the line rather than showing a blank after the
    /// name. The reading arrives with the media info, so this says nothing until that has come back.
    /// </summary>
    public string YearText
    {
      get { return _mediaInfo?.Year is int year ? year.ToString(CultureInfo.InvariantCulture) : string.Empty; }
    }

    /// <summary>"Playing", "Paused", "Starting", "Finished", "Stopped", "Failed" - the word a screen shows.</summary>
    public string StateText
    {
      get { return _stateText; }
    }

    /// <summary>
    /// The shape the screen shows over the film instead of the word: the worn skin's own pause mark for a
    /// pause, its stop mark for a stop. Empty for the two states that have nothing to report - the film
    /// running and nothing ever played - because a mark over a running film covers the film.
    /// </summary>
    public string StateGlyph
    {
      get { return _stateGlyph; }
    }

    /// <summary>e.g. "0:12 / 1:04:05". The lengths come from the engine, never from a guess.</summary>
    public string PositionText
    {
      get { return _positionText; }
    }

    /// <summary>
    /// Where in the film the viewer stands, on its own: "0:12". The display puts this at one end of the position
    /// line and <see cref="DurationLabel"/> at the other, which is what a position line with two numbers on it is.
    /// </summary>
    public string PositionLabel
    {
      get { return _positionLabel; }
    }

    /// <summary>Where the film ends, or "--:--" while the engine has not said - a live stream has no end to name.</summary>
    public string DurationLabel
    {
      get { return _durationLabel; }
    }

    /// <summary>
    /// Where the picture stands, in milliseconds, as the engine last said it. The sleep timer counts its
    /// "after this one" in these numbers - the labels above are for the eye and round to whole seconds.
    /// </summary>
    public long PositionMilliseconds
    {
      get { return _positionMs; }
    }

    /// <summary>The length the engine last said, in milliseconds; nought while nothing answers.</summary>
    public long LengthMilliseconds
    {
      get { return _lengthMs; }
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

    /// <summary>
    /// The number that stands beside the volume slider. What the viewer set, which is not what the engine reads
    /// back - see <see cref="LibVlcPlayer"/>, whose native getter is one command behind on a real audio output.
    /// </summary>
    public string VolumeLabel
    {
      get { return _volume + " %"; }
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

    /// <summary>
    /// True when there is more than one sound to choose from. One sound is not a choice, and a button that can
    /// only do one thing is noise.
    /// </summary>
    public bool HasAudioChoice
    {
      get { return _audioTracks.Count > 1; }
    }

    /// <summary>
    /// True when subtitles could be switched on or off, which needs exactly one subtitle track, not two: the
    /// choice a viewer reaches for on a film with a single forced subtitle is "this, or nothing", and asking for
    /// a second track before showing the button was asking for a film to hide the only way back to no subtitles.
    /// </summary>
    public bool HasSubtitleChoice
    {
      get { return _realSubtitleCount >= 1; }
    }

    /// <summary>True when either of the two has something to pick, which is when a screen shows the row at all.</summary>
    public bool HasTrackChoice
    {
      get { return HasAudioChoice || HasSubtitleChoice; }
    }

    /// <summary>
    /// Which sound the film is on, as the engine names it, or -1 when it said nothing. The list of sounds marks
    /// its row by this rather than by what a screen last pressed, because a media picks a sound of its own when
    /// it starts and a screen that remembered its own last press would light the wrong row.
    /// </summary>
    public int CurrentAudioTrackId
    {
      get { return _audioCurrent; }
    }

    /// <summary>The subtitle the film is on, or -1 for "none of them". See <see cref="CurrentAudioTrackId"/>.</summary>
    public int CurrentSubtitleTrackId
    {
      get { return _subtitleCurrent; }
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
    /// The name the subtitle button wears: the track's own name, or "off" while nothing is shown. The button
    /// carries the name rather than a chevron alone because a viewer wants to read what is on right now without
    /// opening the list for it, and "OFF" is a word that says as much as a struck-through picture.
    /// </summary>
    public string SubtitleTrackName
    {
      get { return SubtitlesShown ? TrackName(_subtitleTracks, _subtitleCurrent) : "OFF"; }
    }

    /// <summary>The name the sound button wears, in the same voice as <see cref="SubtitleTrackName"/>.</summary>
    public string AudioTrackName
    {
      get { return TrackName(_audioTracks, _audioCurrent); }
    }

    /// <summary>
    /// What a track calls itself, at the length a button has room for: what the media wrote beside the stream
    /// when it wrote anything, the two letters of its language next, and the engine's whole line only when the
    /// media said neither.
    /// </summary>
    private static string TrackName(List<PlaybackTrack> tracks, int current)
    {
      foreach (PlaybackTrack track in tracks)
      {
        if (track.Id != current)
          continue;

        if (!string.IsNullOrEmpty(track.Description))
          return track.Description!;
        if (!string.IsNullOrEmpty(track.Language))
          return track.Language!.ToUpperInvariant();
        return track.Label;
      }

      return current < 0 ? "none" : "not known";
    }

    // ---- what the playback screen's buttons look like ----------------------------------------------
    //
    // The buttons are drawn, not lettered: a row of words along the foot of a film covers the film, while the
    // marks on a remote say the same thing in a quarter of the room. Which mark belongs to which button is the
    // worn skin's answer - the names are the reference's own <c>PlayerControl</c> icons, and a skin that carries
    // none of them draws its buttons without a glyph rather than borrowing a letter off the keyboard.

    /// <summary>The icon folder the playback marks live in, in every skin that carries them.</summary>
    public const string ICONS = "PlayerControl/";

    /// <summary>
    /// Every mark the foot of the playback screen can draw, by the name the skin knows it under. A screen that
    /// asks for a picture by name is only as good as the drawing behind the name, so this is the list the skin
    /// tests walk: a key with no vector behind it is an empty plate on screen, and nothing else would notice.
    /// </summary>
    public static IReadOnlyList<string> FootMarkIcons { get; } = new[]
    {
        ICONS + "play", ICONS + "pause", ICONS + "fast_rewind", ICONS + "fast_forward", ICONS + "stop",
        ICONS + "eject", ICONS + "volume_up", ICONS + "volume_down",
        ICONS + "volume_mute", ICONS + "volume_off", ICONS + "subtitles", ICONS + "subtitles_off",
        ICONS + "fullscreen"
    };

    /// <summary>The mark for the transport button: the film asks to be paused while it runs and played while it waits.</summary>
    public string TransportGlyph
    {
      get { return ICONS + (_isPlaying ? "pause" : "play"); }
    }

    /// <summary>
    /// How far one press of the on-screen skip buttons reaches, in milliseconds: the viewer's own instant skip,
    /// so the button beside the film and the key on the remote move the film to the same place.
    /// </summary>
    public long SkipButtonMs
    {
      get { return InstantSkipMs(1); }
    }

    /// <summary>
    /// The mark for the skip-back button: the two back-pointing triangles the skin carries for rewinding. The
    /// numbered round-arrow glyphs are on disk beside them, but they are the wrong picture here - this button
    /// jumps once by the viewer's own distance rather than winding a running tape back - and their figure is a
    /// smudge at the size a television reads. The distance belongs to the settings and to the mark's name.
    /// </summary>
    public string SkipBackGlyph
    {
      get { return ICONS + "fast_rewind"; }
    }

    /// <summary>The mark for the skip-forward button; see <see cref="SkipBackGlyph"/>.</summary>
    public string SkipForwardGlyph
    {
      get { return ICONS + "fast_forward"; }
    }

    /// <summary>What one skip press is worth, in the viewer's own words: "10 s", or "20 %" of this title.</summary>
    public string SkipCaption
    {
      get { return InstantSkipCaption; }
    }

    /// <summary>
    /// What the back mark is called, in the words the players use: "Rewind 20 %". It is the mark's name rather
    /// than a word painted beside it or over it - a television's screen has no hover, and a box that appears on
    /// top of a button hides the button.
    /// </summary>
    public string SkipBackCaption
    {
      get { return "Rewind " + InstantSkipCaption; }
    }

    /// <summary>What the forward mark is called; see <see cref="SkipBackCaption"/>.</summary>
    public string SkipForwardCaption
    {
      get { return "Forward " + InstantSkipCaption; }
    }

    /// <summary>
    /// The name under the loudspeaker mark, which carries the level as well: a television's OSD has no tooltip
    /// to hover for the number, and a viewer who turned the volume up from across the room wants to see that it
    /// worked without pressing anything else.
    /// </summary>
    public string VolumeCaption
    {
      get { return _muted || _volume <= 0 ? "Muted" : "Volume " + _volume; }
    }

    /// <summary>The name under the subtitle mark, said in full so the mark's own picture is not the only answer.</summary>
    public string SubtitleCaption
    {
      get { return SubtitlesShown ? "Subtitles on" : "Subtitles off"; }
    }

    /// <summary>Whether subtitles are on right now, which is the whole content of the subtitle button's mark.</summary>
    public bool SubtitlesShown
    {
      get { return _subtitleCurrent >= 0; }
    }

    /// <summary>The mark for the subtitle button: the caption box, struck through while nothing is shown.</summary>
    public string SubtitlesGlyph
    {
      get { return ICONS + (SubtitlesShown ? "subtitles" : "subtitles_off"); }
    }

    /// <summary>
    /// The mark for the mute button, which says how loud it is rather than what pressing it does: struck through
    /// when muted or silent, a bare speaker at a whisper, one wave at half, three at full. A viewer reading the
    /// picture's volume wants the state, and the button underneath it is the same one either way.
    /// </summary>
    public string VolumeGlyph
    {
      get
      {
        if (_muted || _volume <= 0)
          return ICONS + "volume_off";
        if (_volume < 34)
          return ICONS + "volume_mute";
        return _volume < 67 ? ICONS + "volume_down" : ICONS + "volume_up";
      }
    }

    /// <summary>What the mute button's name is at this moment: it offers the opposite of the state it shows.</summary>
    public string MuteCaption
    {
      get { return _muted ? "Unmute" : "Mute"; }
    }

    /// <summary>The sound's short name for the chip beside its button: "GER", or the track's own name in brief.</summary>
    public string AudioChip
    {
      get { return ShortName(_audioTracks, _audioCurrent); }
    }

    /// <summary>The subtitle's short name, or "OFF" while nothing is shown - which is what the chip is for.</summary>
    public string SubtitleChip
    {
      get { return SubtitlesShown ? ShortName(_subtitleTracks, _subtitleCurrent) : "OFF"; }
    }

    /// <summary>
    /// The two letters a track is called by, capitalised, because a chip beside a film has room for three
    /// characters; the track's own name in brief when the container carried no language, and nothing when it
    /// carried neither.
    /// </summary>
    private static string ShortName(IReadOnlyList<PlaybackTrack> tracks, int current)
    {
      foreach (PlaybackTrack track in tracks)
      {
        if (track.Id != current)
          continue;

        if (!string.IsNullOrEmpty(track.Language))
          return track.Language!.ToUpperInvariant();

        return string.IsNullOrEmpty(track.Description)
            ? ""
            : track.Description!.Length <= 4 ? track.Description.ToUpperInvariant() : "";
      }

      return "";
    }

    // ---- what the buttons may do ---------------------------------------------------------------------

    /// <summary>
    /// Whether anything is loaded that the transport could move. Every button that reaches the film answers to
    /// this, because a mark on the television that answers nothing is worse than the space where it would stand.
    /// </summary>
    public bool CanTransport
    {
      get { return _player != null && _hasMedia; }
    }

    /// <summary>Whether the position bar can be pressed at all, which needs a media with a known length.</summary>
    public bool CanSeek
    {
      get { return CanTransport && _player!.LengthInMs > 0; }
    }

    /// <summary>How far into the media the film stands, in milliseconds; 0 while the engine says nothing.</summary>
    public long PositionMs
    {
      get { return _player?.TimeInMs ?? 0; }
    }

    /// <summary>How long the media is, in milliseconds; 0 for a live stream and for nothing loaded.</summary>
    public long LengthMs
    {
      get { return _player?.LengthInMs ?? 0; }
    }

    /// <summary>
    /// Whether there is a library title behind the film to say something about. A file opened by path has no
    /// record to read, so its screen offers no information button rather than one that opens an empty panel.
    /// </summary>
    public bool CanInform
    {
      get { return _mediaItemId != null; }
    }

    /// <summary>
    /// Moves the film to the given share of its own length, which is what pressing the position bar asks for.
    /// Refuses when there is no length to take a share of, rather than jumping somewhere nobody pointed at.
    /// </summary>
    public bool SeekToFraction(double fraction)
    {
      if (!CanSeek)
        return Refuse("this stream has no position to move in.");

      double wanted = fraction;
      if (wanted < 0)
        wanted = 0;
      if (wanted > 1)
        wanted = 1;

      return Skip((long)(wanted * _player!.LengthInMs) - _player.TimeInMs);
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
      _tracksWorthAskingAgain = _audioTracks.Count == 0 && _realSubtitleCount == 0;
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

    /// <summary>
    /// Asks the engine what chapters the media carries and what its picture is coded with. Called beside
    /// <see cref="ReadTracks"/> when a media starts and again from the position events, because a container's
    /// chapter table and its codec are usually read while the media opens rather than before it.
    /// </summary>
    public void ReadChapters()
    {
      if (_disposed || _player == null || !_hasMedia)
      {
        if (_chapters.Count == 0 && _quality == null)
          return;
        _chapters.Clear();
        _quality = null;
        Raise();
        return;
      }

      List<PlaybackChapter> found = new List<PlaybackChapter>(_player.Chapters);
      PlaybackQuality? quality = _player.Quality;

      bool chaptersMoved = found.Count != _chapters.Count;
      if (!chaptersMoved)
      {
        for (int i = 0; i < found.Count; i++)
        {
          if (found[i].PositionMs != _chapters[i].PositionMs)
          {
            chaptersMoved = true;
            break;
          }
        }
      }

      bool qualityChanged = (quality == null) != (_quality == null)
          || (quality != null
              && (quality.Width != _quality!.Width || quality.Height != _quality.Height
                  || !string.Equals(quality.CodecFourcc, _quality.CodecFourcc, StringComparison.OrdinalIgnoreCase)));

      if (!chaptersMoved && !qualityChanged)
        return;

      _chapters.Clear();
      _chapters.AddRange(found);
      _quality = quality;
      Raise();
    }

    /// <summary>
    /// The same question again, at most every tenth position event and only until something has been heard or a
    /// dozen answers have gone by.
    /// </summary>
    /// <remarks>
    /// Position events come several a second, and the common media has neither chapters nor a named codec, so an
    /// unthrottled question here is a thousand questions a minute about two things the file will never say. Ticked
    /// rather than timed, because the events are the clock this model is kept on and a test can hand them out.
    /// </remarks>
    private void AskAgainAboutTheMedia()
    {
      if (_chaptersAsksLeft <= 0)
        return;
      if (++_chaptersTicks < 10)
        return;

      _chaptersTicks = 0;
      _chaptersAsksLeft--;
      ReadChapters();
      if (_chapters.Count > 0 || _quality != null)
        _chaptersAsksLeft = 0;
    }

    /// <summary>Whether the media names chapters at all. A film that names none draws no marks.</summary>
    public bool HasChapters
    {
      get { return _chapters.Count > 0 && LengthMs > 0; }
    }

    /// <summary>
    /// The chapter marks as places along the position line, each a share of its width. Which pixel a share of the
    /// line is belongs to the line, which knows how wide it got and is asked again when that changes.
    /// </summary>
    public IReadOnlyList<ChapterMark> ChapterMarks
    {
      get
      {
        long length = LengthMs;
        if (_chapters.Count == 0 || length <= 0)
          return Array.Empty<ChapterMark>();

        List<ChapterMark> marks = new List<ChapterMark>(_chapters.Count);
        foreach (PlaybackChapter chapter in _chapters)
        {
          // The first chapter begins the line and a chapter at the end of it stands off the line; neither is a
          // mark a viewer could see.
          if (chapter.PositionMs <= 0 || chapter.PositionMs >= length)
            continue;
          marks.Add(new ChapterMark((double)chapter.PositionMs / length, chapter.Name));
        }
        return marks;
      }
    }

    /// <summary>
    /// Which chapter the film stands in, or -1 when nothing can be said - no chapters, or no length to place them
    /// against. Worked out from the engine's own position every time it is asked.
    /// </summary>
    public int CurrentChapterIndex
    {
      get
      {
        long length = LengthMs;
        if (_chapters.Count == 0 || length <= 0)
          return -1;

        long position = PositionMs;
        int found = -1;
        for (int i = 0; i < _chapters.Count; i++)
        {
          if (_chapters[i].PositionMs > position)
            break;
          found = i;
        }
        return found;
      }
    }

    /// <summary>Where in the chapters the viewer stands, in words: "The Chase (2/5)". Empty when there is nothing to say.</summary>
    public string ChapterCaption
    {
      get
      {
        int index = CurrentChapterIndex;
        if (index < 0)
          return string.Empty;

        string name = _chapters[index].Name ?? string.Empty;
        return name.Length == 0
            ? "Chapter " + (index + 1) + " of " + _chapters.Count
            : name + " (" + (index + 1) + "/" + _chapters.Count + ")";
      }
    }

    /// <summary>How near a chapter's start counts as standing at its start. Two seconds, which is what every player uses.</summary>
    private const long ChapterRestartGraceMs = 2000;

    /// <summary>Whether the chapter mark backwards would move anything: an earlier chapter, or this one's own start.</summary>
    public bool CanChapterBack
    {
      get
      {
        int index = CurrentChapterIndex;
        return index >= 0
            && (index > 0 || PositionMs - _chapters[index].PositionMs > ChapterRestartGraceMs);
      }
    }

    /// <summary>Whether a chapter begins further along the film than where it stands now.</summary>
    public bool CanChapterForward
    {
      get
      {
        long length = LengthMs;
        if (_chapters.Count == 0 || length <= 0)
          return false;

        foreach (PlaybackChapter chapter in _chapters)
        {
          if (chapter.PositionMs > PositionMs + ChapterRestartGraceMs && chapter.PositionMs < length)
            return true;
        }
        return false;
      }
    }

    /// <summary>
    /// Back to the start of this chapter, or to the one before when the viewer is barely past this one's start,
    /// which is what a chapter key has always meant - otherwise the chapter one is in could never be reached again.
    /// </summary>
    public bool PreviousChapter()
    {
      int index = CurrentChapterIndex;
      if (index < 0)
        return Refuse("this media has no chapters.");

      long target = index > 0 && PositionMs - _chapters[index].PositionMs <= ChapterRestartGraceMs
          ? _chapters[index - 1].PositionMs
          : _chapters[index].PositionMs;
      return JumpToChapter(target);
    }

    /// <summary>To the start of the next chapter. False, with the reason on the status line, when there is no next.</summary>
    public bool NextChapter()
    {
      long length = LengthMs;
      if (_chapters.Count == 0 || length <= 0)
        return Refuse("this media has no chapters.");

      foreach (PlaybackChapter chapter in _chapters)
      {
        if (chapter.PositionMs > PositionMs + ChapterRestartGraceMs && chapter.PositionMs < length)
          return JumpToChapter(chapter.PositionMs);
      }
      return Refuse("this is the last chapter.");
    }

    /// <summary>Moves the film to an exact position, which is what the start of a chapter is.</summary>
    private bool JumpToChapter(long positionMs)
    {
      if (!CanSeek)
        return Refuse("this stream has no position to move in.");

      return Skip(positionMs - _player!.TimeInMs);
    }

    /// <summary>
    /// The size of the picture the surface has been drawing, which the screen reports when a frame arrives: a
    /// second way of learning how big a film is, for the case the container never said. A decoded picture cannot
    /// be a different size from the film it is a picture of.
    /// </summary>
    public void ReportPictureSize(int width, int height)
    {
      if (width <= 0 || height <= 0 || (width == _pictureWidth && height == _pictureHeight))
        return;

      _pictureWidth = width;
      _pictureHeight = height;
      Raise();
    }

    private int _pictureWidth;
    private int _pictureHeight;

    /// <summary>
    /// How big the picture is, in the one word a viewer reads it in: "4K", "1080p". Empty when neither the
    /// container nor a decoded frame has said, which is a normal answer rather than a gap to fill with a guess.
    /// </summary>
    public string ResolutionTag
    {
      get
      {
        int width = _quality != null && _quality.HasSize ? _quality.Width : _pictureWidth;
        int height = _quality != null && _quality.HasSize ? _quality.Height : _pictureHeight;
        if (width <= 0 || height <= 0)
          return string.Empty;
        if (height >= 2000 || width >= 3840)
          return "4K";
        if (height >= 1300)
          return "1440p";
        if (height >= 1000)
          return "1080p";
        if (height >= 700)
          return "720p";
        if (height >= 400)
          return "480p";
        return height + "p";
      }
    }

    /// <summary>
    /// What codes the picture, in the word the trade uses for it. Empty when the container did not name its codec:
    /// nothing here is inferred from the size of a picture, because a big picture is not proof of anything.
    /// </summary>
    public string CodecTag
    {
      get
      {
        PlaybackQuality? quality = _quality;
        if (quality == null || string.IsNullOrEmpty(quality.CodecFourcc))
          return string.Empty;
        if (quality.IsDolbyVision)
          return "Dolby Vision";
        if (quality.IsHevc)
          return "HEVC";

        string fourcc = quality.CodecFourcc!.ToLowerInvariant();
        if (fourcc == "avc1" || fourcc == "avc3" || fourcc == "h264")
          return "AVC";
        if (fourcc == "av01")
          return "AV1";
        if (fourcc == "mpg2")
          return "MPEG-2";
        if (fourcc == "mp4v" || fourcc == "mpg4")
          return "MPEG-4";
        return string.Empty;
      }
    }

    /// <summary>Whether anything stands beside the name of the film.</summary>
    public bool ShowsTags
    {
      get { return ResolutionTag.Length > 0 || CodecTag.Length > 0; }
    }

    /// <summary>Every tag in one line, for a tooltip: "4K · Dolby Vision".</summary>
    public string QualityCaption
    {
      get
      {
        List<string> parts = new List<string>();
        if (ResolutionTag.Length > 0)
          parts.Add(ResolutionTag);
        if (CodecTag.Length > 0)
          parts.Add(CodecTag);
        return string.Join(" · ", parts);
      }
    }

    /// <summary>
    /// One chapter as the position line needs it: how far along the line it stands, and what the container called
    /// it. Nothing draws the name out here - a mark is four pixels wide - but the tooltip and the remote's caption
    /// read it, and a mark that could not say which chapter it marked would be decoration.
    /// </summary>
    public sealed class ChapterMark
    {
      public ChapterMark(double fraction, string? name)
      {
        Fraction = fraction;
        Name = name;
      }

      /// <summary>How far along the position line this mark stands, from 0 to 1.</summary>
      public double Fraction { get; }

      /// <summary>What the container named this chapter, or null when it named none.</summary>
      public string? Name { get; }
    }

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
        FillStars();
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

    // ---- the audio arrangement ----------------------------------------------------------------------
    //
    // A song has no picture of its own, so the screen that plays one has nothing to show but itself: the cover,
    // the record it came off, what the file is made of, and what the library knows about the title. All of it is
    // asked of the library and none of it is worked out here, and every piece of it stands only where the library
    // had something to say - which is the same rule the film's arrangement follows about its tags.

    /// <summary>
    /// The shelf the playing title was pressed in, in the server's spelling ("AudioItem", "AlbumItem", "Video"),
    /// or null when nobody named one. The application sets it where it already knows the answer - beside the
    /// <see cref="Play(MediaLocation, Guid?, int, string?)"/> call a library press turns into - and every new media
    /// starts without one again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what tells the screen that no picture is expected. Without it a song is played onto a black
    /// rectangle and the screen says "nothing is playing" about the very thing that is playing; with the film's
    /// controls over that blackness, a viewer has no way to see that the machine is not broken.
    /// </para>
    /// <para>
    /// The shelf is asked first because it knows before anything has been read over the network: a title pressed
    /// in the music shelf wears the audio arrangement from the first frame, not a heartbeat after the library
    /// answers. Where no shelf was named, the library's own statement about the file decides - see
    /// <see cref="IsAudio"/>.
    /// </para>
    /// </remarks>
    public string? PlayingAspect
    {
      get { return _playingAspect; }
      set
      {
        if (string.Equals(_playingAspect, value, StringComparison.OrdinalIgnoreCase))
          return;
        _playingAspect = value;
        Raise();
      }
    }

    private string? _playingAspect;

    /// <summary>
    /// The artist the wall of the library said about the row that was pressed, handed over with the name of the
    /// song by <c>App.OnTitleChosen</c> and <c>App.OnPartySongChosen</c>. A press knows this before the library has
    /// answered anything, and a server older than the record fields of the details answer says nothing else; the
    /// line under the title then stands on this name. <see cref="Play(MediaLocation, Guid?, int, string?)"/> keeps
    /// it through one title to the next of the same record and clears it when a new run begins, the way
    /// <see cref="PlayingAspect"/> is kept and cleared.
    /// </summary>
    public string? PlayingArtist
    {
      get { return _playingArtist; }
      set
      {
        if (string.Equals(_playingArtist, value, StringComparison.Ordinal))
          return;
        _playingArtist = value;
        Raise();
      }
    }

    private string? _playingArtist;

    /// <summary>
    /// Whether the library files a shelf of this name under music. The two names are the aspects themselves
    /// (<c>AudioAspect.Metadata</c> is "AudioItem", <c>AudioAlbumAspect</c> is "AlbumItem"), and they are the same
    /// two the browser's wall squares its music tiles by
    /// (<c>MediaBrowserViewModel.IsMusic</c>, <c>MediaBrowserViewModel.cs:1685-1687</c>).
    /// </summary>
    private static bool NamesMusic(string? aspect)
    {
      return string.Equals(aspect, "AlbumItem", StringComparison.OrdinalIgnoreCase)
          || string.Equals(aspect, "AudioItem", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether the screen wears the audio arrangement instead of the film's. Two answers are believed and nothing
    /// else: the shelf the viewer pressed the title in, and - when no shelf was named - the library's own
    /// statement that the file is an audio file, which is the codec the importer wrote
    /// (<see cref="MediaItemDetails.AudioEncoding"/>, written only from the audio aspect; a film's title carries
    /// no such aspect and is therefore answered null by the server).
    /// </summary>
    /// <remarks>
    /// A file opened by <c>--play</c> on the command line names no shelf and belongs to no library, so nobody can
    /// say what it is, and it keeps the film's arrangement. The file's ending would be a third rule nobody wrote
    /// down, and a stream address carries no ending to read.
    /// </remarks>
    public bool IsAudio
    {
      get
      {
        if (NamesMusic(_playingAspect))
          return true;

        MediaItemDetails? details = _mediaInfo;
        return details != null && details.Error == null
            && !string.IsNullOrWhiteSpace(details.AudioEncoding);
      }
    }

    /// <summary>
    /// Who sings it: the file's own tag, the record's name when the track credited nobody of its own, and what the
    /// wall of the library said about the row that was pressed when the library's answer holds neither. Empty when
    /// nobody names anyone, which is what leaves the line under the title away rather than blank.
    /// </summary>
    /// <remarks>
    /// The row's answer is not a guess invented for this screen: the titles answer of the library carries an
    /// <c>artist</c> for every music row (<c>ServerApiDto.TitleResult.Artist</c>, read from
    /// <c>AudioAspect.ATTR_ARTIST</c>), and a server older than the record fields of the details answer says
    /// nothing at all there - so the name the viewer pressed the song by is the name the screen has until a newer
    /// server answers the question itself.
    /// </remarks>
    public string ArtistName
    {
      get
      {
        MediaItemDetails? details = _mediaInfo;
        if (details != null)
        {
          if (!string.IsNullOrWhiteSpace(details.Artist))
            return details.Artist!.Trim();
          if (!string.IsNullOrWhiteSpace(details.AlbumArtist))
            return details.AlbumArtist!.Trim();
        }

        return string.IsNullOrWhiteSpace(PlayingArtist) ? string.Empty : PlayingArtist!.Trim();
      }
    }

    /// <summary>Whether there is a name to stand under the title.</summary>
    public bool ShowsArtist
    {
      get { return IsAudio && ArtistName.Length > 0; }
    }

    /// <summary>The record this track came off, or empty when its tag never named one.</summary>
    public string AlbumName
    {
      get
      {
        MediaItemDetails? details = _mediaInfo;
        return details == null || string.IsNullOrWhiteSpace(details.Album)
            ? string.Empty
            : details.Album!.Trim();
      }
    }

    /// <summary>
    /// The kinds of story this title is told in, said in the one line a cell of the board has room for. The
    /// library keeps them as a list and the list is said whole, the way the info panel says it.
    /// </summary>
    public string GenreName
    {
      get
      {
        IReadOnlyList<string>? genres = _mediaInfo?.Genres;
        if (genres == null || genres.Count == 0)
          return string.Empty;

        List<string> named = new List<string>();
        foreach (string genre in genres)
          if (!string.IsNullOrWhiteSpace(genre))
            named.Add(genre.Trim());

        return string.Join(", ", named);
      }
    }

    /// <summary>
    /// The number the stars and the brackets beside them are lit by: what the viewers on this server gave the
    /// title, or what this viewer gave it when nobody else has. Null when neither happened, which is most titles
    /// and leaves the whole line away - a row of five dark stars is a question, not an answer.
    /// </summary>
    public double? Rating
    {
      get
      {
        MediaItemDetails? details = _mediaInfo;
        if (details == null)
          return null;
        if (details.TotalRating.HasValue && details.TotalRating.Value > 0)
          return details.TotalRating.Value;
        if (details.Rating.HasValue && details.Rating.Value > 0)
          return details.Rating.Value;
        return null;
      }
    }

    /// <summary>Whether there is a rating to draw stars for at all.</summary>
    public bool ShowsRating
    {
      get { return IsAudio && Rating.HasValue; }
    }

    /// <summary>
    /// The five star pictures and how much of their light the rating leaves them: whole for a star the rating
    /// filled, a quarter for one it never reached, the part between for one it filled half way. Ten points to five
    /// stars, so a point is half a star - the arithmetic and its reason are the browser panel's
    /// (<c>MediaBrowserViewModel.FillDetailsStars</c>, <c>MediaBrowserViewModel.cs:4113-4129</c>), and the two
    /// screens of one library must not light one title two different ways.
    /// </summary>
    public ObservableCollection<double> Stars { get; } =
        new ObservableCollection<double> { 0.25, 0.25, 0.25, 0.25, 0.25 };

    /// <summary>
    /// The number the brackets speak it with, "(7.6)", as the browser's panel writes it
    /// (<c>MediaBrowserViewModel.DetailsRatingWords</c>). Empty with no rating, which is what hides it.
    /// </summary>
    public string RatingWords
    {
      get
      {
        double? rating = Rating;
        return rating.HasValue
            ? "(" + rating.Value.ToString("0.0", CultureInfo.InvariantCulture) + ")"
            : string.Empty;
      }
    }

    /// <summary>
    /// What the file is made of, as the plaques of the browser's audio row say it: the codec, the channels, the
    /// disc, the two rates. Read from the same answer the browser reads, so one file is described one way.
    /// </summary>
    public AudioFileLine AudioLine
    {
      get { return MediaStreamLogos.Audio(_mediaInfo); }
    }

    /// <summary>
    /// Whether the row of plaques above the cover costs its height. The disc of a set is left out of this row: the
    /// board under the plaques is the record's own answer, and which of two discs a file sits on is not asked of a
    /// screen a viewer looks at for three minutes.
    /// </summary>
    public bool ShowsAudioPlaques
    {
      get
      {
        if (!IsAudio)
          return false;

        AudioFileLine line = AudioLine;
        return line.ShowsCodec || line.ShowsChannels || line.ShowsRates;
      }
    }

    /// <summary>
    /// Tells the five star pictures how much of their light the library's number leaves them. Written into the
    /// collection rather than answered by a getter, because a picture that is handed the same collection again has
    /// to be told that a piece of it changed.
    /// </summary>
    private void FillStars()
    {
      // Read through the property rather than the field, so that the one place deciding which of the two numbers
      // a title is rated by stays the only one. The argument is what the caller has just stored.
      double stars = Math.Clamp(Rating ?? 0, 0, 10) / 2.0;
      for (int star = 0; star < Stars.Count; star++)
        Stars[star] = 0.25 + 0.75 * Math.Clamp(stars - star, 0, 1);
    }

    /// <summary>
    /// Whether the row of buttons along the foot offers its two track lists. Two answers are needed and both are
    /// said here: nothing that is not playing has no tracks to pick, and a song is one sound with no picture to be
    /// dubbed, so on the audio arrangement the two buttons that open a list of tracks are put away - a button that
    /// opens a list holding one answer asks a question nobody has, and the reference's audio screen offered neither.
    /// </summary>
    public bool ShowsTrackLists
    {
      get { return HasMedia && !IsAudio; }
    }

    /// <summary>
    /// Whether the title's lettering stands by the transport. The audio arrangement wears its own picture where
    /// the lettering stands - the cover, four hundred and twenty lines square - and a name drawn twice within a
    /// hand's breadth is the reading twice, which is what this screen was once cleared of.
    /// </summary>
    public bool ShowsClearArt
    {
      get { return _clearArt != null && !IsAudio; }
    }

    /// <summary>
    /// How to ask the library for the picture of the disc a record was pressed on, or null while there is no
    /// server. The audio arrangement lays it behind the cover and turns it, which is the one thing a screen about
    /// a song can do that a screen about a film cannot: the thing playing is visible.
    /// </summary>
    public Func<Guid, Task<byte[]?>>? DiscReader { get; set; }

    private byte[]? _disc;

    /// <summary>Which title <see cref="_disc"/> was read for, so the same title is asked once.</summary>
    private Guid? _discFor;

    /// <summary>The bytes of the disc's picture, or null while none has arrived - or none exists.</summary>
    public byte[]? Disc
    {
      get { return _disc; }
      private set
      {
        _disc = value;
        Raise();
      }
    }

    /// <summary>
    /// Whether the record is drawn at all, turning behind the cover. The reference shows the carrier only while the
    /// song plays (FullScreenContentAudio.xaml:100-147) and the mockup follows it by hiding the plate on pause: a
    /// record that stands still in the middle of the screen reads as a picture of a record rather than as the
    /// thing that is playing, and while the song waits the screen has the mark of the pause to show instead.
    /// </summary>
    public bool ShowsDisc
    {
      get { return IsAudio && IsPlaying; }
    }

    /// <summary>
    /// Asks the library once for the disc's picture, guarded as the cover is: not twice for one title, never for a
    /// file with no library id, and a library that keeps no disc art is an ordinary library rather than a failure.
    /// </summary>
    public async Task ReadDiscAsync()
    {
      // Only a song wears the record, so only a song is asked for one: a film's screen has nowhere to paint it,
      // and a question nobody will look at is a picture nobody needed to be sent.
      if (!IsAudio)
        return;

      Guid? id = _mediaItemId;
      if (id == null || DiscReader == null || _discFor == id)
        return;

      try
      {
        byte[]? bytes = await DiscReader(id.Value);

        // Remembered as asked whatever the answer was. The memo is the only brake on this question: publishing an
        // answer raises the change that every open playback screen answers by asking the same question again, so a
        // "no picture" that left the memo open would be asked again by its own answer - in the render probe, whose
        // readers answer in the same breath, until the stack gives out, and on the network as one request per
        // answer for as long as the screen is open. And a library without the picture is the ordinary case, not a
        // corner: a track of a running library answers '404' for its disc art. Only a question that could not be
        // asked at all is left open below, so that a server arriving late is asked again.
        _discFor = id;
        if (_mediaItemId == id)
          Disc = bytes;
      }
      catch (Exception)
      {
        _discFor = null;
      }
    }

    /// <summary>
    /// How to ask the library for the picture the stage of a song stands on - the album's fanart - or null while
    /// there is no server. A film brings its own pictures and needs no ground; a song has none, and the reference's
    /// audio screen therefore laid the record's own artwork across the whole screen behind everything
    /// (screens/FullScreenContentAudio.xaml, and the mockup's <c>.fanart</c> as its first layer).
    /// </summary>
    public Func<Guid, Task<byte[]?>>? FanartReader { get; set; }

    private byte[]? _fanart;

    /// <summary>Which title <see cref="_fanart"/> was read for, so the same title is asked once.</summary>
    private Guid? _fanartFor;

    /// <summary>
    /// The bytes of the stage's picture, or null while none has arrived - or none exists, which is most libraries
    /// and is drawn as the plain shade the screen wears anyway rather than as a hole.
    /// </summary>
    public byte[]? Fanart
    {
      get { return _fanart; }
      private set
      {
        _fanart = value;
        Raise();
      }
    }

    /// <summary>
    /// Asks the library once for the stage's picture, guarded as the cover and the record are. Only a song is
    /// asked: over a film the picture would stand behind the film's own frames, where nobody would see it.
    /// </summary>
    public async Task ReadFanartAsync()
    {
      if (!IsAudio)
        return;

      Guid? id = _mediaItemId;
      if (id == null || FanartReader == null || _fanartFor == id)
        return;

      try
      {
        byte[]? bytes = await FanartReader(id.Value);

        // Written before published, for the reason the cover's read gives: publishing raises the change that
        // reopens this question, and a reader that answers at once would otherwise be asked again by its own answer.
        // Remembered as asked whatever it answered - see the record's read for why a 'no picture' closes the
        // question rather than leaving it open.
        _fanartFor = id;
        if (_mediaItemId == id)
          Fanart = bytes;
      }
      catch (Exception)
      {
        _fanartFor = null;
      }
    }

    /// <summary>
    /// What the bars beside the record ask their numbers of: the engine that is playing, when it can hand out the
    /// loudness of the bands the control itself divides the sound into. Null while it cannot - which is every
    /// engine this frontend has except libVLC, whose own spectrum picture is what the band source reads - and the
    /// bars then stand at their floor with their caps fallen, which is the same answer the reference's control
    /// gives a player that is not active (Controls/AudioSpectrumAnalyzer.cs:490-493). Set once by the application,
    /// before any screen is built.
    /// </summary>
    public IAudioSpectrumSource? Spectrum { get; set; }

    /// <summary>
    /// The engine's bands, ready to be bound to <c>AudioSpectrumBars.Bands</c>: the same player the screen already
    /// drives, seen as the thing that can hand out levels. Null for an engine that hands out none, which leaves the
    /// bars in their floor.
    /// </summary>
    public IAudioBandSource? BandSpectrum
    {
      get { return _player as IAudioBandSource; }
    }

    /// <summary>
    /// Asks the engine to look at the sound the bars are drawn from. Only the screen that actually wears the audio
    /// arrangement asks, and only while it wears it: listening costs a second decoder, and a film's screen - which
    /// shows its own picture and has no bars at all - would pay that price for nothing.
    /// </summary>
    public void WatchSpectrum()
    {
      (_player as IAudioSpectrumWatcher)?.WatchSpectrum();
    }

    /// <summary>
    /// Stops that listening. Asking a player that is not listening is permitted, so this is called whenever the
    /// screen leaves the audio arrangement, not only when it had really started the listener.
    /// </summary>
    public void StopWatchingSpectrum()
    {
      (_player as IAudioSpectrumWatcher)?.StopWatchingSpectrum();
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

        // The record is written before the answer is published: publishing raises the change that every open
        // playback screen answers by asking this same question again, so a reader that answers in the same breath
        // - a cached one, or a test's - would otherwise find the question not yet on record and ask itself until
        // the stack gives out. Which is not hypothetical: the render probe's readers answer at once.
        //
        // And it is written for a 'no picture' too, not only for a picture. Most titles of most libraries have no
        // poster and no fanart, so "nothing" is the ordinary answer rather than the rare one; leaving the record
        // open in that case asks the question again on every answer, which in a probe is a stack overflow and in
        // front of a server is one request per answer for as long as the screen is open.
        _coverFor = id;
        if (_mediaItemId == id)
          Cover = bytes;
      }
      catch (Exception)
      {
        _coverFor = null;
      }
    }

    /// <summary>
    /// How to ask the library for a title's lettering - its name drawn as a picture, the ClearArt - or null while
    /// there is no server. The playback screen stands it over the film at the side where it does not cover the
    /// controls, the way the reference hung the title's lettering by the transport.
    /// </summary>
    public Func<Guid, Task<byte[]?>>? ClearArtReader { get; set; }

    private byte[]? _clearArt;

    /// <summary>Which title <see cref="_clearArt"/> was read for, so the same title is asked once.</summary>
    private Guid? _clearArtFor;

    /// <summary>
    /// The bytes of the playing title's lettering, or null while none has arrived. The screen paints a picture
    /// from them when there are some and nothing at all when there are none, which is most libraries.
    /// </summary>
    public byte[]? ClearArt
    {
      get { return _clearArt; }
      private set
      {
        _clearArt = value;
        Raise();
      }
    }

    /// <summary>
    /// Asks the library once for the playing title's lettering, guarded exactly as the cover is: the same title
    /// is not asked twice, a file with no library id is not asked at all, and a refused read - which is what a
    /// library without this kind of picture answers - leaves the screen lettering-less rather than interrupting
    /// the film.
    /// </summary>
    public async Task ReadClearArtAsync()
    {
      Guid? id = _mediaItemId;
      if (id == null || ClearArtReader == null || _clearArtFor == id)
        return;

      try
      {
        byte[]? bytes = await ClearArtReader(id.Value);

        // Guarded before publishing, as the cover is: the picture arriving is the signal to ask again, and a
        // library that keeps no lettering - which is most libraries - closes the question with its 'nothing'
        // rather than leaving it open to be asked again by its own answer.
        _clearArtFor = id;
        if (_mediaItemId == id)
          ClearArt = bytes;
      }
      catch (Exception)
      {
        _clearArtFor = null;
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
        // Guarded before publishing, as the cover is: the answer arriving is itself the signal that makes an open
        // screen ask this question again, and a reader that answers at once must not be answered with a new
        // question that the record has not yet closed. An answer that carries an error closes it too - the server
        // did answer, and asking a question it answered badly over and over is a storm, not a retry. The catch
        // below is where a question that never got through stays open.
        _mediaInfoFor = id;
        // The film may have moved on while the question was in flight, and an answer about the previous title
        // does not belong on the next title's info panel.
        if (_mediaItemId == id)
          MediaInfo = details;
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
      return Play(location, mediaItemId, resumePercent, null);
    }

    /// <summary>
    /// As <see cref="Play(MediaLocation, Guid?, int)"/>, with the name the viewer chose the media by.
    /// </summary>
    /// <param name="presentedAs">
    /// What the library calls this title, when somebody picked it out of the library: "Big Buck Bunny". A screen
    /// that showed the address instead would show a stream url, which is how a machine reaches the film and not
    /// what a person calls it. Null - a file opened by hand - falls back to the file's own name.
    /// </param>
    public bool Play(MediaLocation location, Guid? mediaItemId, int resumePercent, string? presentedAs)
    {
      if (_disposed)
        return Refuse("this screen is closed.");
      if (_player == null)
        return Refuse("no player on this machine.");

      // Named before the engine is asked, because a real engine can raise its first state event while the call is
      // still running, and a listener that woke up on "Playing" next to the previous media's name would be seeing
      // the same one-frame lie as a progress bar holding the last media's position.
      string previousTitle = _title;
      string previousFileName = _fileName;
      bool previousHasMedia = _hasMedia;
      Guid? previousId = _mediaItemId;

      // How far the viewer got in the previous media, measured before the new one is opened: the engine forgets
      // the length of what it was playing when it is told to open something else, and "how far" without a length
      // has no answer.
      int previousPercentage = PercentageReached(_furthestMs);

      // The name the viewer picked the film by, and beneath it the file it is actually made of. A stream has no
      // file this machine can name, so it says nothing rather than repeating the address the player dials.
      _title = string.IsNullOrWhiteSpace(presentedAs) ? ShortName(location) : presentedAs!;
      _fileName = location.IsFile ? ShortName(location) : _title;
      _hasMedia = true;

      // The old media's tracks are not this media's tracks. Emptied before the engine is asked, so that a
      // viewer looking at the screen while a file opens cannot pick a subtitle out of the previous film.
      ClearTracks();
      _trackPreferencesDone = false;

      // Where the sound begins, when the viewer said where it begins and has not named a number of their own
      // since. Set before the engine is asked, so the first frame is already as loud as the answer says. It is a
      // start volume and not a per-media one: an album is dozens of media, and a start volume handed out at every
      // tune would undo whatever the viewer turned the loudness to during the previous one. The screen's own
      // number moves with it, or the label beside the slider would go on saying what nobody set.
      if (Preferences != null && !_volumeAsked)
      {
        _volume = Preferences.InitialVolume;
        _player.Volume = Preferences.InitialVolume;
      }

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
      // And which shelf it came from is asked of the caller again, for the same reason: whoever pressed a song
      // into the player says so, and whoever pressed a film says so too. Nothing heard here carries over.
      _playingAspect = null;
      _playingArtist = null;
      _disc = null;
      _discFor = null;
      _fanart = null;
      _fanartFor = null;
      _cover = null;
      _coverFor = null;
      _clearArt = null;
      _clearArtFor = null;
      _furthestMs = 0;
      _positionMs = 0;
      _lengthMs = 0;
      _reported = false;
      // The chapters and what codes the picture belong to the media that is over; the new one names its own,
      // and the question starts again from the beginning rather than showing the last film's marks.
      _chapters.Clear();
      _quality = null;
      _chaptersAsksLeft = 12;
      _chaptersTicks = 0;

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
      _volumeAsked = true;
      RememberVolume();
      Raise();
      return true;
    }

    /// <summary>
    /// Writes the loudness the viewer just picked into the answers, so that the next media and the next start of
    /// this machine begin where they left it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing was ever written down: the shipped start volume is 100 (Settings/PlaybackSettings.cs:121), a viewer
    /// turning the sound down changed one living moment only, and the next start of the frontend opened at 100
    /// again. Measured on the machine this was reported from: the settings folder holds the server address and no
    /// answer file at all, because nothing ever asked for one to be written.
    /// </para>
    /// <para>
    /// The answer's own sentence is dropped rather than shown - a volume key that scribbled "Saved in ..." across
    /// the screen after every press would be worse than the silence it complains about. A machine whose file cannot
    /// be written still plays at the number the viewer picked and forgets it at the next start, which is the most
    /// this screen can honestly promise.
    /// </para>
    /// </remarks>
    private void RememberVolume()
    {
      Settings.PlaybackSettings? prefs = Preferences;
      if (prefs == null || prefs.InitialVolume == _volume)
        return;

      prefs.InitialVolume = _volume;
      prefs.Save(null, out _);
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
      // The bars stop being looked after with the screen. Stopping the transport is not the answer here - the song
      // carries on over other screens - but nothing may keep a second decoder running for a stage nobody is
      // looking at any more.
      StopWatchingSpectrum();
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
        {
          ReadTracks();
          ReadChapters();
        }
      });
    }

    private void OnPositionChanged(object? sender, PlaybackPositionEventArgs args)
    {
      // The resume seek, applied the first time an event says how long the media is. Out here rather than inside
      // the marshaller on purpose: the seek is the engine's business and can be issued from the thread the engine
      // is on, while waiting for a UI turn would have the viewer watch the first minutes of a film they had
      // already seen before the seek landed at all. (An engine that hands its events from a thread which must
      // not block inside them - libVLC - defers such calls to its own thread; see LibVlcPlayer.)
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
        _positionLabel = Format(args.TimeInMs);
        _durationLabel = args.LengthInMs > 0 ? Format(args.LengthInMs) : "--:--";
        _positionText = _positionLabel + " / " + _durationLabel;
        _positionMs = args.TimeInMs;
        _lengthMs = args.LengthInMs;
        double fraction = args.Fraction;
        _progress = fraction < 0 ? 0 : fraction;
        Raise();

        // Asked again here, and only once, when the first ask heard nothing: a length is the sign that the input
        // is really open, which is the moment an engine that was still reading a stream's header can answer for
        // its tracks. Once per media rather than once per tick, because the events come several a second and a
        // list nobody is looking at is not worth a thousand questions a minute.
        if (_tracksWorthAskingAgain && args.LengthInMs > 0)
        {
          _tracksWorthAskingAgain = false;
          ReadTracks();
        }
        AskAgainAboutTheMedia();
      });
    }

    private void OnPlayerFailed(object? sender, PlayerFailedEventArgs args)
    {
      _marshal(delegate
      {
        _stateText = "Failed";
        _stateGlyph = "Notification/error";
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

      // The same seven states as shapes, named out of the skin's own player marks so a screen can say the
      // state over the picture without laying a word over it. A running film and an idle player are left
      // empty: over a film that is doing what it should there is nothing to point at.
      _stateGlyph = state switch
      {
        PlayerState.Opening => "PlayerControl/play",
        PlayerState.Paused => "PlayerControl/pause",
        PlayerState.Stopped => "PlayerControl/stop",
        PlayerState.Ended => "PlayerControl/stop",
        PlayerState.Failed => "Notification/error",
        _ => ""
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
      _tracksWorthAskingAgain = false;
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
