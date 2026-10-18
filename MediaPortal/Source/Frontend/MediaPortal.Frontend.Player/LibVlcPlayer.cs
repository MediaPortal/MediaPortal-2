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
  /// <b>What is not here.</b> Drawing into somebody else's window is implemented for a Win32 HWND only. On Linux
  /// the picture needs an X11/Wayland window and on Android a Surface, and both are a handle this class cannot
  /// validate from here; <see cref="AttachTo(nint)"/> reports false on those platforms instead of accepting a
  /// handle it cannot use. Handing the pictures to a <see cref="VideoFrameSink"/> instead needs no platform
  /// knowledge at all, which is why that is the road the screens take and why it works on all three - and why
  /// decoding and timing are measurable headless everywhere.
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

    // The last position pair handed to a listener, so that a tick which moved nothing says nothing.
    private long _lastPositionTimeMs = -1;
    private long _lastPositionLengthMs = -1;
    private string? _lastFailure;
    private bool _ended;
    private bool _disposed;

    // libVLC's own default is 100 and no mute; the fields start there so that a player that has never been told
    // anything reports what the engine would do, not silence.
    private int _volume = 100;
    private bool _muted;

    // Where the pictures go, if anywhere: a window handle or a sink, never both. Volatile because the engine's
    // video thread reads it in the frame callbacks while a screen's thread writes it in AttachTo.
    private volatile VideoFrameSink? _frameSink;
    private bool _drawsIntoWindow;

    // A memory video output cannot be refused at the moment it hands over a picture: measured with VLC 3.0.20,
    // answering its lock callback with the nothing it was not offered is an access violation inside the decoder,
    // not a dropped frame. So a picture with nowhere to go - the screen is gone, or a sink is being taken away and
    // the engine refused to stop - is written into this scratch of the player's own instead. It is the size the
    // engine was last told a picture is, and it is only ever built on that path, which in practice means during a
    // teardown that is happening anyway.
    private readonly object _overflowGate = new object();
    private byte[]? _overflow;
    private GCHandle _overflowPin;
    private int _overflowSize;
    private volatile int _frameBytes;

    /// <summary>
    /// What the lock callback hands back for a picture that went into the overflow rather than a sink: a number
    /// outside the range of sink buffer indexes, so that the unlock and display callbacks can tell the two apart.
    /// </summary>
    private const int OverflowTag = int.MaxValue - 1;

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

      // Position and length come as two native events but are one fact for a screen - a progress bar needs both
      // numbers together, and a length that arrives after the first position tick is worth publishing on its own.
      _player.TimeChanged += (s, e) => PublishPosition();
      _player.LengthChanged += (s, e) => PublishPosition();
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
    public event EventHandler<PlaybackPositionEventArgs>? PositionChanged;

    public bool Open(MediaLocation location)
    {
      lock (_gate)
      {
        if (_disposed)
          return Fail("this player has been disposed.");
        _lastFailure = null;
        _ended = false;
        _current = location;

        // A new media begins its own position story. Without this a second file of the same length would have its
        // opening tick suppressed as a duplicate of the previous media's, and a screen would keep showing the
        // position the last one ended at.
        _lastPositionTimeMs = -1;
        _lastPositionLengthMs = -1;
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

    public int CurrentTrackId(TrackKind kind)
    {
      try
      {
        // libVLC reports the off entry as -1, which is the same number the seam promises for "off".
        return kind == TrackKind.Audio ? _player.AudioTrack : _player.Spu;
      }
      catch (VLCException)
      {
        return -1;
      }
    }

    public bool AttachTo(nint videoTarget)
    {
      if (_frameSink != null)
      {
        Fail("this player already hands its pictures to a frame sink; a window and a sink are two answers "
             + "to the same question.");
        return false;
      }
      if (!OperatingSystem.IsWindows())
      {
        Fail("this build cannot draw into a window on this platform yet; the audio and timing half still runs.");
        return false;
      }
      try
      {
        _player.Hwnd = videoTarget;
        _drawsIntoWindow = true;
        return true;
      }
      catch (VLCException ex)
      {
        return Fail("libVLC refused the window handle: " + ex.Message);
      }
    }

    /// <summary>
    /// Hand this player's pictures to a sink. See <see cref="IVideoPlayer.AttachTo(VideoFrameSink)"/> for what
    /// the answer means; what follows is how libVLC is persuaded to do it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// libVLC's memory-output contract is two pairs of callbacks. The format pair is asked "what will you accept,
    /// and how big?" when the video's size becomes known - answering 0 there is how this class says no - and the
    /// frame pair is asked per picture: "here is a plane, give me somewhere to write it" and, later, "I have
    /// shown it". <see cref="VideoFrameSink"/> owns the buffers, so the answer to the first question is three
    /// pinned arrays and the answer to the second is one of their addresses.
    /// </para>
    /// <para>
    /// <b>Nothing is scaled here, on purpose.</b> The size the sink is given is the size the video is, and a
    /// screen that wants it smaller draws it smaller - which the compositor does on the graphics card instead of
    /// copying and resampling a few million bytes on a CPU thread.
    /// </para>
    /// </remarks>
    public bool AttachTo(VideoFrameSink frameSink)
    {
      if (frameSink == null)
        throw new ArgumentNullException(nameof(frameSink));

      if (ReferenceEquals(_frameSink, frameSink))
        return true;
      if (_frameSink != null)
        return Fail("this player already hands its pictures to another sink.");
      if (_drawsIntoWindow)
        return Fail("this player draws into a window; a window and a sink are two answers to one question.");

      try
      {
        // libVLC reads the format callbacks as the decision to stop using a video output plugin, so these two
        // calls are the whole of "give me the pictures instead of a window". They are wired once per player: the
        // format callback is asked again whenever a new media's size becomes known, which is what makes a
        // resolution change mid-film work without anything here noticing.
        _player.SetVideoFormatCallbacks(OnVideoFormat, OnVideoCleanup);
        _player.SetVideoCallbacks(OnVideoLock, OnVideoUnlock, OnVideoDisplay);
      }
      catch (VLCException ex)
      {
        return Fail("libVLC would not take over its own picture memory: " + ex.Message);
      }
      catch (EntryPointNotFoundException ex)
      {
        return Fail("this libVLC has no memory video output: " + ex.Message);
      }

      _frameSink = frameSink;

      // Whoever disposes the sink has to stop this player from writing before the buffers go away, because the
      // addresses the engine holds point inside arrays belonging to the sink. The sink asks, this class answers;
      // see VideoFrameSink.EngineWillStopWriting for the hazard that makes this handshake necessary.
      frameSink.EngineWillStopWriting = StopHandingPicturesOver;
      return true;
    }

    /// <summary>
    /// Stop the engine from writing into a sink's buffers any more, so that the sink can take its memory back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This stops playback, and it is the only honest way to do the job. The binding refuses to take the callbacks
    /// back (<c>SetVideoCallbacks(null, ...)</c> throws <see cref="ArgumentNullException"/>), and dropping the
    /// callbacks here in our own code would not help either: libVLC writes into the buffer address it was handed
    /// once, at the format callback, and carries on writing there whether or not the lock callback likes the next
    /// frame. <c>libvlc_media_player_stop</c> is the call that actually tears the video output down, and it waits
    /// for the threads it owns, which is exactly the guarantee a sink needs before unpinning.
    /// </para>
    /// <para>
    /// Stopping is also the truth of the situation: a player whose picture surface has been destroyed has nothing
    /// left to show, and running a decoder for an invisible picture would be the odd choice.
    /// </para>
    /// <para>
    /// Must not be called from one of libVLC's own callbacks - it waits for the very threads a callback is running
    /// on. Both callers (a sink being disposed, and <see cref="Dispose"/>) are threads of the front end.
    /// </para>
    /// </remarks>
    private void StopHandingPicturesOver()
    {
      try
      {
        _player.Stop();
      }
      catch (VLCException)
      {
        // libVLC could not stop. The sink's own wait on the writers it still has is what stands between a decoder
        // and unpinned memory, so a refusal here costs a late handover, not the heap.
      }
      catch (ObjectDisposedException)
      {
        // The player is already gone, and with it the engine that could have written.
      }

      // Only now may the sink be let go, and the order is not a detail. While <c>Stop</c> runs libVLC is still
      // locking and unlocking pictures - measured here, it locks the ones it has in flight on its way out - and a
      // lock callback with no sink to take the picture is an access violation inside the decoder rather than a
      // dropped frame. Until this call returns, those pictures keep going into the sink's buffers, which are
      // alive: the sink is waiting for this method.
      _frameSink = null;
    }

    /// <summary>
    /// libVLC's format callback: agree a pixel format and a size, and say how many buffers it may write at once.
    /// </summary>
    /// <remarks>
    /// "RV24" is four bytes a pixel with blue first on every platform this runs on, which is the byte order a
    /// screen's BGRA surface wants; agreeing on that here is what keeps a picture from coming out with its red
    /// and blue swapped, the most reliable way to make a film look like a fever dream.
    /// </remarks>
    private uint OnVideoFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height,
                               ref uint pitches, ref uint lines)
    {
      opaque = IntPtr.Zero;
      VideoFrameSink? sink = _frameSink;
      if (sink == null)
        return 0;

      if (sink.Allocate((int)width, (int)height, out int stride) == 0)
        return 0;

      WriteFourCC(chroma, 'R', 'V', '2', '4');
      pitches = (uint)stride;
      lines = height;

      // How large one picture is, remembered for the overflow buffer: these two are the numbers libVLC itself was
      // just given, so they are the only ones it can ever write.
      _frameBytes = stride * (int)lines;

      FitPictureToSource(sink);

      // One plane: a packed 32-bit picture has nothing else to plan.
      return 1;
    }

    /// <summary>
    /// Cut the padding out of the picture: tell the sink how much of the buffers the film actually fills.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The size libVLC asks a memory output for is the frame the decoder works in, and a codec rounds that up to
    /// the block it likes - the HEVC of the film this was found with decodes 1920x804 into 1920x832 because 832 is
    /// a whole number of 64-line coding tree units. libVLC itself knows both numbers, and only it knows which of
    /// them is the film, so the question is asked here once per video rather than guessed at by rounding.
    /// </para>
    /// <para>
    /// When libVLC declines to answer, or answers with a size that is not inside the buffers, the whole buffer is
    /// taken to be picture, which is what videos whose height happens to fit need and the most a wrong answer can
    /// cost: a few percent of stretch, not a crash.
    /// </para>
    /// </remarks>
    private void FitPictureToSource(VideoFrameSink sink)
    {
      try
      {
        uint sourceWidth = 0;
        uint sourceHeight = 0;
        if (!_player.Size(0, ref sourceWidth, ref sourceHeight))
          return;

        if (sourceWidth == 0 || sourceHeight == 0
            || sourceWidth == (uint)sink.Width && sourceHeight == (uint)sink.Height)
          return;

        sink.SetVisibleSize((int)sourceWidth, (int)sourceHeight);
      }
      catch (VLCException)
      {
        // No answer is an answer of its own, and it means "draw what you were given".
      }
    }

    /// <summary>libVLC is finished with this picture memory; let it go and wait to be asked again.</summary>
    private void OnVideoCleanup(ref IntPtr opaque)
    {
      _frameSink?.Abandon();
    }

    /// <summary>
    /// libVLC wants to decode a picture: hand it a buffer and remember which, because that number comes back with
    /// the finished frame.
    /// </summary>
    private IntPtr OnVideoLock(IntPtr opaque, IntPtr planes)
    {
      VideoFrameSink? sink = _frameSink;
      if (sink != null && sink.TryAcquireForWriting(out int index, out IntPtr address))
      {
        // One plane, so one address to write, at the front of the array libVLC handed over.
        Marshal.WriteIntPtr(planes, 0, address);

        // The value returned here is handed back to the display callback untouched, and 0 already means "no
        // buffer" to libVLC - hence the index plus one, which keeps index 0 usable.
        return new IntPtr(index + 1L);
      }

      // No screen for this picture. It still has to be told somewhere to put it, so it goes into the player's own
      // scratch (see _overflow) and the tags below make sure nobody mistakes it for a frame a screen should draw.
      if (TryOverflowAddress(out IntPtr spare))
      {
        Marshal.WriteIntPtr(planes, 0, spare);
        return new IntPtr(OverflowTag);
      }

      return IntPtr.Zero;
    }

    /// <summary>
    /// A pinned block the size of one picture, for the pictures that have no sink to go to.
    /// </summary>
    /// <returns>False only when the memory is not there, or no video size has ever been agreed.</returns>
    private bool TryOverflowAddress(out IntPtr address)
    {
      address = IntPtr.Zero;
      int bytes = _frameBytes;
      if (bytes <= 0)
        return false;

      lock (_overflowGate)
      {
        if (_overflowSize != bytes)
        {
          if (_overflowPin.IsAllocated)
            _overflowPin.Free();
          _overflow = null;
          _overflowSize = 0;

          try
          {
            _overflow = new byte[bytes];
            _overflowPin = GCHandle.Alloc(_overflow, GCHandleType.Pinned);
            _overflowSize = bytes;
          }
          catch (OutOfMemoryException)
          {
            return false;
          }
        }

        address = Marshal.UnsafeAddrOfPinnedArrayElement(_overflow!, 0);
        return true;
      }
    }

    /// <summary>
    /// Which sink buffer a picture was, or -1 when it was one of the ones nobody wanted.
    /// </summary>
    private static int SinkIndexOf(IntPtr picture)
    {
      long tag = picture.ToInt64();
      return tag >= 1 && tag <= VideoFrameSink.BufferCount ? (int)tag - 1 : -1;
    }

    /// <summary>
    /// libVLC has finished <i>writing</i> a picture. There is nothing to unlock, because the buffer was never
    /// locked: a screen copies out of it rather than holding it, which is the whole design of
    /// <see cref="VideoFrameSink"/>. What does happen here is the one thing the sink cannot see for itself - the
    /// moment a buffer stops being written into, which is what a sink waiting to let its memory go counts on.
    /// </summary>
    private void OnVideoUnlock(IntPtr opaque, IntPtr picture, IntPtr planes)
    {
      if (SinkIndexOf(picture) >= 0)
        _frameSink?.EndWrite();
    }

    /// <summary>The picture libVLC was told about is on screen; it is now the newest one a screen can get.</summary>
    private void OnVideoDisplay(IntPtr opaque, IntPtr picture)
    {
      int index = SinkIndexOf(picture);
      if (index < 0)
        return;

      VideoFrameSink? sink = _frameSink;
      sink?.Finish(index);
    }

    private static void WriteFourCC(IntPtr where, char first, char second, char third, char fourth)
    {
      Marshal.WriteByte(where, 0, (byte)first);
      Marshal.WriteByte(where, 1, (byte)second);
      Marshal.WriteByte(where, 2, (byte)third);
      Marshal.WriteByte(where, 3, (byte)fourth);
    }

    public PlayerStatistics Statistics
    {
      get
      {
        Media? media;
        lock (_gate)
          media = _media;
        if (media == null)
          return PlayerStatistics.None;

        try
        {
          // libVLC counts per media rather than per player, and restarts the counters when a new media is set -
          // which is the lifetime this property promises, so no adjusting is needed here.
          MediaStats counted = media.Statistics;
          return new PlayerStatistics(counted.DecodedVideo, counted.LostPictures, counted.DecodedAudio);
        }
        catch (VLCException)
        {
          return PlayerStatistics.None;
        }
        catch (ObjectDisposedException)
        {
          // Stop() or Dispose() released the media between the read above and this call. "Nothing is open" is
          // the truth, and it is a better answer than a crash in a property whose job is to never crash.
          return PlayerStatistics.None;
        }
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

      // The engine is stopped, so nothing can write any more: take the picture memory back before the native
      // objects go, which is the order that leaves a screen holding nothing but its own arrays.
      StopHandingPicturesOver();
      ReleaseMedia();
      _player.Dispose();
      _libVlc.Dispose();

      // The engine's own cleanup callback has already run by now and let the buffers go; this makes the leaving
      // final. A screen holding this sink stops being handed pictures rather than being handed ones nobody will
      // ever write again, and a screen that wants to keep playing builds a sink for the next player.
      _frameSink?.Release();

      // And the scratch the player kept for the pictures that had no screen to go to.
      lock (_overflowGate)
      {
        if (_overflowPin.IsAllocated)
          _overflowPin.Free();
        _overflow = null;
        _overflowSize = 0;
      }
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

    /// <summary>
    /// Reads the engine's position and length and tells whoever is watching, unless nothing moved.
    /// </summary>
    /// <remarks>
    /// Reading inside the callback is deliberate: the pair has to be the pair the engine believed at that moment,
    /// and taking them at two different times is how a progress bar ends up ahead of its own length. The
    /// duplicate suppression is not politeness - libVLC raises the time event per tick (measured: several times a
    /// second), and a screen that rebuilt its layout on every one of them would spend its frame budget on a
    /// slider that did not move.
    /// </remarks>
    private void PublishPosition()
    {
      long time;
      long length;
      try
      {
        time = _player.Time;
        length = _player.Length;
      }
      catch (ObjectDisposedException)
      {
        // The native object went away between the event being queued and it arriving. Nothing to publish.
        return;
      }

      if (time < 0)
        time = 0;
      if (length < 0)
        length = 0;

      lock (_gate)
      {
        if (_disposed)
          return;
        if (time == _lastPositionTimeMs && length == _lastPositionLengthMs)
          return;
        _lastPositionTimeMs = time;
        _lastPositionLengthMs = length;
      }

      PositionChanged?.Invoke(this, new PlaybackPositionEventArgs(time, length));
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

