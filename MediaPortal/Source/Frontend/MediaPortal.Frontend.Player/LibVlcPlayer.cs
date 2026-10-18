using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;

namespace MediaPortal.Frontend.Player
{
  /// <summary>
  /// The libVLC engine behind <see cref="IVideoPlayer"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The only place in the tree that references LibVLCSharp, on purpose. Everything it can do is expressed in
  /// <see cref="IVideoPlayer"/>'s vocabulary, and the two mappings that are not one-to-one (the state names, the
  /// track descriptions) are the whole of what this class knows about libVLC.
  /// </para>
  /// <para>
  /// One instance owns one <c>libvlc_instance_t</c>. That is the expensive object (it loads the plugin module
  /// list once), so a screen that plays in sequence should keep one player and call <see cref="Open"/> again -
  /// not build a new player per item, which is what the old tree's players tended to encourage.
  /// </para>
  /// <para>
  /// <b>What is not here.</b> Rendering into a window is implemented for a Win32 HWND only. On Linux the picture
  /// needs an X11/Wayland window and on Android a Surface, and both are a handle this class cannot validate from
  /// here; <see cref="AttachTo"/> reports false on those platforms instead of accepting a handle it cannot use.
  /// Decoding and timing do not need any of that, which is why they are measurable headless on all three.
  /// </para>
  /// </remarks>
  public sealed class LibVlcPlayer : IVideoPlayer
  {
    private static readonly object NativeGate = new object();
    private static string? _initializedFrom;

    // What the loader hook should load, and whether it is installed. Both belong to the process, not to a player.
    private static string? _engineDirectory;
    private static bool _resolverRegistered;

    private readonly object _gate = new object();
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;

    private Media? _media;
    private PlayerState _state = PlayerState.Idle;
    private MediaLocation? _current;
    private string? _lastFailure;
    private bool _ended;
    private bool _disposed;

    // libVLC's own default is 100 and no mute; the fields start there so that a player that has never been told
    // anything reports what the engine would do, not silence.
    private int _volume = 100;
    private bool _muted;

    private LibVlcPlayer(LibVLC libVlc, MediaPlayer player, string engineVersion)
    {
      _libVlc = libVlc;
      _player = player;
      EngineDescription = "libVLC " + engineVersion;

      // libVLC raises one event per state rather than one state-changed event, so each is wired to the same
      // publisher. Reading State inside the handler is deliberate: it is the engine's own last word, and it keeps
      // this class from keeping a second copy of the truth that can disagree with the native one.
      _player.NothingSpecial += (s, e) => Publish(StateOf(VLCState.NothingSpecial));
      _player.Opening += (s, e) => PublishFromOpeningEvent();
      _player.Buffering += (s, e) => PublishFromOpeningEvent();
      _player.Playing += (s, e) => Publish(StateOf(VLCState.Playing));
      _player.Paused += (s, e) => Publish(StateOf(VLCState.Paused));
      _player.Stopped += (s, e) => Publish(StateOf(VLCState.Stopped));
      _player.EndReached += OnEndReached;
      _player.EncounteredError += OnEncounteredError;
    }

    /// <summary>e.g. "libVLC 3.0.20" - the version of the native library actually loaded, not of the binding.</summary>
    public string EngineDescription { get; }

    /// <summary>
    /// Build a player, or say in one sentence why there is none.
    /// </summary>
    /// <param name="player">The player, or null when <paramref name="reason"/> explains the refusal.</param>
    /// <param name="reason">
    /// What was looked at and what came of it, always filled: the folder the engine came from, or the folders
    /// that were searched.
    /// </param>
    /// <param name="options">How to build it; null for the defaults.</param>
    public static bool TryCreate(out LibVlcPlayer? player, out string reason, PlayerOptions? options = null)
    {
      player = null;
      options ??= new PlayerOptions();

      if (!NativeLibVlc.TryFind(options.LibraryDirectory, out string? directory, out reason))
        return false;

      try
      {
        if (directory != null)
          UseNativeDirectory(directory);

        List<string> arguments = new List<string>();
        if (options.Headless)
          arguments.AddRange(PlayerOptions.HeadlessArguments);
        foreach (string extra in options.ExtraArguments)
          arguments.Add(extra);

        LibVLC libVlc = new LibVLC(arguments.ToArray());
        MediaPlayer mediaPlayer = new MediaPlayer(libVlc);
        player = new LibVlcPlayer(libVlc, mediaPlayer, libVlc.Version);
        return true;
      }
      catch (VLCException ex)
      {
        // libVLC refused to start. Its own message is the useful part, so it goes into the sentence rather than
        // being wrapped in something a screen would have to unwrap.
        reason = "libVLC was found but would not start" +
                 (string.IsNullOrWhiteSpace(ex.Message) ? "." : ": " + ex.Message.Trim());
        return false;
      }
      catch (DllNotFoundException ex)
      {
        reason = "the libVLC library could not be loaded: " + ex.Message;
        return false;
      }
      catch (InvalidOperationException ex)
      {
        // Either the process already loaded the engine from a different folder, or the binding refused the
        // folder it was given - LibVLCSharp does that on Linux, where the loader, not the caller, picks the
        // library. Both are worth one honest sentence: they are the difference between "no player" and
        // "player from the wrong install".
        reason = "this process cannot use that libVLC: " + ex.Message;
        return false;
      }
      catch (EntryPointNotFoundException ex)
      {
        // The usual cause is a VLC 4 development install next to a binding built for 3.x.
        reason = "the libVLC on this machine does not match what the player expects: " + ex.Message;
        return false;
      }
    }

    /// <summary>
    /// Point the binding at a folder once per process. LibVLCSharp resolves its native library on first use and
    /// cannot be pointed somewhere else afterwards, so a second, different directory is refused rather than
    /// silently ignored.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only Windows is told the folder. On Linux and macOS LibVLCSharp refuses a directory outright - measured in
    /// the container: <c>InvalidOperationException: Using libvlcDirectoryPath is not supported on the Linux
    /// platform</c> - so the folder is honoured one level lower, by answering the loader's request for "libvlc"
    /// with the file that was actually found (see <see cref="LoadEngineFrom"/>).
    /// </para>
    /// <para>
    /// That resolver is not a nicety. Distribution packages install the versioned <c>libvlc.so.5</c> and leave
    /// the unversioned <c>libvlc.so</c> to a -dev package, so a plain loader search for "libvlc" finds nothing on
    /// a machine that plays every other video file. Without it the player would need <c>apt install libvlc-dev</c>
    /// to watch a film.
    /// </para>
    /// </remarks>
    private static void UseNativeDirectory(string directory)
    {
      lock (NativeGate)
      {
        if (_initializedFrom != null)
        {
          if (!string.Equals(_initializedFrom, directory, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"libVLC was already loaded from '{_initializedFrom}'; this process cannot also use '{directory}'.");
          return;
        }

        if (OperatingSystem.IsWindows())
        {
          Core.Initialize(directory);
        }
        else
        {
          LoadEngineFrom(directory);
          Core.Initialize();
        }
        _initializedFrom = directory;
      }
    }

    /// <summary>
    /// Register the loader hook that resolves "libvlc" out of <paramref name="directory"/>. Registered against
    /// the binding's own assembly, because that is the assembly whose imports the runtime is resolving.
    /// </summary>
    private static void LoadEngineFrom(string directory)
    {
      _engineDirectory = directory;
      if (!_resolverRegistered)
      {
        NativeLibrary.SetDllImportResolver(typeof(Core).Assembly, ResolveEngine);
        _resolverRegistered = true;
      }
    }

    /// <summary>
    /// The hook itself: the found folder, searched for the engine's file names. Anything else - libvlccore,
    /// which libvlc pulls in as a dependency, and every other import - is left to the loader.
    /// </summary>
    private static nint ResolveEngine(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
      if (!libraryName.Equals("libvlc", StringComparison.OrdinalIgnoreCase))
        return 0;

      // Read without the lock on purpose: the field is written only under NativeGate, and a reference read is
      // atomic. A resolver that took a lock during load would risk deadlocking the loader.
      string? directory = _engineDirectory;
      if (directory == null)
        return 0;

      foreach (string name in NativeLibVlc.LibraryFileNames)
      {
        if (NativeLibrary.TryLoad(Path.Combine(directory, name), assembly, searchPath, out nint handle))
          return handle;
      }
      return 0;
    }

    public PlayerState State
    {
      get
      {
        lock (_gate)
          return _state;
      }
    }

    public MediaLocation? Current
    {
      get
      {
        lock (_gate)
          return _current;
      }
    }

    public string? LastFailure
    {
      get
      {
        lock (_gate)
          return _lastFailure;
      }
    }

    public event EventHandler<PlayerStateChangedEventArgs>? StateChanged;
    public event EventHandler<PlayerFailedEventArgs>? Failed;
    public event EventHandler? EndOfMedia;

    public bool Open(MediaLocation location)
    {
      lock (_gate)
      {
        if (_disposed)
          return Fail("this player has been disposed.");
        _lastFailure = null;
        _ended = false;
        _current = location;
      }

      try
      {
        ReleaseMedia();
        FromType how = location.IsFile ? FromType.FromPath : FromType.FromLocation;
        Media media = new Media(_libVlc, location.EngineString, how);
        lock (_gate)
          _media = media;
        if (!_player.Play(media))
        {
          // Play() returning false is rare - most failures surface as EncounteredError a moment later - but it is
          // an answer, and a call that answered false must not leave the state at whatever the last media left.
          return Fail($"libVLC would not accept {location.Describe()}.");
        }
        return true;
      }
      catch (VLCException ex)
      {
        return Fail($"could not open {location.Describe()}: " +
                    (string.IsNullOrWhiteSpace(ex.Message) ? "libVLC gave no reason." : ex.Message.Trim()));
      }
      catch (ObjectDisposedException)
      {
        return Fail("this player has been disposed.");
      }
    }

    public bool Play()
    {
      try
      {
        return _player.Play();
      }
      catch (VLCException ex)
      {
        return Fail("libVLC could not resume playback: " + ex.Message);
      }
    }

    public bool Pause()
    {
      PlayerState now = State;
      if (now != PlayerState.Playing)
        return Fail($"cannot pause: the player is {Describe(now)}.");
      try
      {
        _player.Pause();
        return true;
      }
      catch (VLCException ex)
      {
        return Fail("libVLC could not pause: " + ex.Message);
      }
    }

    public bool Stop()
    {
      try
      {
        _player.Stop();
        ReleaseMedia();
        Publish(PlayerState.Stopped);
        return true;
      }
      catch (VLCException ex)
      {
        return Fail("libVLC could not stop: " + ex.Message);
      }
    }

    public long LengthInMs
    {
      get
      {
        try
        {
          // libVLC answers -1 for "unknown" - a live stream, or a media that is not parsed yet. The interface
          // promises 0, because a screen that formats -1 as a duration prints "-00:01" on a TV.
          long length = _player.Length;
          return length < 0 ? 0 : length;
        }
        catch (VLCException)
        {
          return 0;
        }
      }
    }

    public long TimeInMs
    {
      get
      {
        try
        {
          long time = _player.Time;
          return time < 0 ? 0 : time;
        }
        catch (VLCException)
        {
          return 0;
        }
      }
      set
      {
        try
        {
          _player.Time = value < 0 ? 0 : value;
        }
        catch (VLCException)
        {
          // A seek on a stream that does not support seeking is not an error worth a dialog; the position simply
          // does not move, which is what the screen will see when it reads TimeInMs next.
        }
      }
    }

    public int Volume
    {
      get => _volume;
      set
      {
        int clamped = Math.Clamp(value, 0, 100);
        _volume = clamped;
        PushToEngine(() => _player.Volume = clamped);
      }
    }

    public bool Muted
    {
      get => _muted;
      set
      {
        _muted = value;
        PushToEngine(() => _player.Mute = value);
      }
    }

    /// <summary>
    /// Volume and mute are what this class was last told, not what the engine reports back.
    /// </summary>
    /// <remarks>
    /// Measured on VLC 3.0.20 with the project's own probe: with the dummy audio output the native getter answers
    /// 0 for volume and false for mute whatever was set, because there is no audio output to hold the value; with
    /// the real audio output the getter is one command behind (set 42 while 100 was current, and it reads 100
    /// until the next call). A TV must show the viewer the number they set, not that. The engine is still told
    /// every change - only the read-back is not trusted.
    /// </remarks>
    private void PushToEngine(Action apply)
    {
      try
      {
        apply();
      }
      catch (VLCException)
      {
        // The value stays as set: when playback restarts the same value is pushed again by whatever call follows.
      }
    }

    public IReadOnlyList<PlaybackTrack> Tracks(TrackKind kind)
    {
      TrackDescription[]? descriptions = null;
      try
      {
        descriptions = kind == TrackKind.Audio ? _player.AudioTrackDescription : _player.SpuDescription;
      }
      catch (VLCException)
      {
        return Array.Empty<PlaybackTrack>();
      }

      if (descriptions == null)
        return Array.Empty<PlaybackTrack>();

      List<PlaybackTrack> tracks = new List<PlaybackTrack>(descriptions.Length);
      foreach (TrackDescription description in descriptions)
      {
        // libVLC's "Disable" entry is the id -1 the interface promises, so it is not listed as a track to pick.
        if (description.Id < 0)
          continue;
        tracks.Add(new PlaybackTrack(kind, description.Id, description.Name,
          PlaybackTrack.ParseLanguageFromEngineName(description.Name)));
      }
      return tracks;
    }

    public bool SelectTrack(PlaybackTrack track)
    {
      try
      {
        return track.Kind == TrackKind.Audio ? _player.SetAudioTrack(track.Id) : _player.SetSpu(track.Id);
      }
      catch (VLCException)
      {
        return false;
      }
    }

    public bool AttachTo(nint videoTarget)
    {
      if (!OperatingSystem.IsWindows())
      {
        Fail("this build cannot draw into a window on this platform yet; the audio and timing half still runs.");
        return false;
      }
      try
      {
        _player.Hwnd = videoTarget;
        return true;
      }
      catch (VLCException ex)
      {
        return Fail("libVLC refused the window handle: " + ex.Message);
      }
    }

    public void Dispose()
    {
      lock (_gate)
      {
        if (_disposed)
          return;
        _disposed = true;
      }
      try
      {
        _player.Stop();
      }
      catch (VLCException)
      {
        // Already on our way out; a native refusal here has nowhere to be reported.
      }
      ReleaseMedia();
      _player.Dispose();
      _libVlc.Dispose();
    }

    private void OnEndReached(object? sender, EventArgs e)
    {
      lock (_gate)
        _ended = true;
      Publish(PlayerState.Ended);
      EndOfMedia?.Invoke(this, EventArgs.Empty);
    }

    private void OnEncounteredError(object? sender, EventArgs e)
    {
      string engine = _libVlc.LastLibVLCError ?? string.Empty;
      string where = Current.HasValue ? Current.Value.Describe() : "the current media";
      string reason = "libVLC stopped playing " + where + (engine.Length == 0 ? "." : ": " + engine);
      Fail(reason);
      Publish(PlayerState.Failed);
      Failed?.Invoke(this, new PlayerFailedEventArgs(reason));
    }

    /// <summary>
    /// libVLC's state, translated. <c>Error</c> and <c>Ended</c> come only from the events above - the engine has
    /// no state event for them - and the three "not yet" states collapse into <see cref="PlayerState.Opening"/>.
    /// </summary>
    private PlayerState StateOf(VLCState native)
    {
      switch (native)
      {
        case VLCState.Playing:
          return PlayerState.Playing;
        case VLCState.Paused:
          return PlayerState.Paused;
        case VLCState.Ended:
          return PlayerState.Ended;
        case VLCState.Error:
          return PlayerState.Failed;
        case VLCState.NothingSpecial:
          lock (_gate)
          {
            // NothingSpecial is what libVLC reports after a stop *and* after an end. Which of the two it was is
            // only known from the events, so the answer here is the one EndReached already gave, if it did.
            if (_ended)
              return PlayerState.Ended;
            // Before anything was opened, "nothing special" is idle, not stopped.
            return _current.HasValue ? PlayerState.Stopped : PlayerState.Idle;
          }
        case VLCState.Stopped:
          lock (_gate)
          {
            if (_ended)
              return PlayerState.Ended;
          }
          return PlayerState.Stopped;
        default:
          return PlayerState.Opening;
      }
    }

    /// <summary>
    /// An <c>Opening</c> or <c>Buffering</c> event, with one measured exception.
    /// </summary>
    /// <remarks>
    /// On VLC 3.0.20 an <c>Opening</c> arrives <i>after</i> <c>Playing</c>, immediately before the end of a short
    /// file (the sequence <c>Opening, Playing, Opening, Ended</c> was printed by a probe against a 4-second clip).
    /// Taken at face value that makes a running player look like it restarted, which on a screen is a "loading"
    /// spinner flashing for a moment at the end of every item. So while the player is already running, those two
    /// events are ignored; before it runs, they are the truth and are published.
    /// </remarks>
    private void PublishFromOpeningEvent()
    {
      lock (_gate)
      {
        if (_state == PlayerState.Playing || _state == PlayerState.Paused)
          return;
      }
      Publish(PlayerState.Opening);
    }

    private void Publish(PlayerState newState)
    {
      PlayerState oldState;
      lock (_gate)
      {
        // A disposed player says nothing further. Without this guard the Stop() inside Dispose() publishes
        // Stopped to whoever still holds the instance, which turned a "disposed player" assertion into a race
        // against a native event on the first run of these tests.
        if (_disposed)
          return;
        oldState = _state;
        if (oldState == newState)
          return;
        _state = newState;
      }
      StateChanged?.Invoke(this, new PlayerStateChangedEventArgs(oldState, newState));
    }

    /// <summary>Records a failure and returns false, so that a guard reads as one statement.</summary>
    private bool Fail(string reason)
    {
      lock (_gate)
        _lastFailure = reason;
      return false;
    }

    private static string Describe(PlayerState state)
    {
      switch (state)
      {
        case PlayerState.Playing:
          return "playing";
        case PlayerState.Paused:
          return "paused";
        case PlayerState.Opening:
          return "still opening";
        case PlayerState.Ended:
          return "at its end";
        case PlayerState.Failed:
          return "broken";
        default:
          return "idle";
      }
    }

    private void ReleaseMedia()
    {
      Media? media;
      lock (_gate)
      {
        media = _media;
        _media = null;
      }
      media?.Dispose();
    }
  }
}
