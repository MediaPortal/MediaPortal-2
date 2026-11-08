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
using System.Runtime.InteropServices;
using System.Threading;
using LibVLCSharp.Shared;

namespace MediaPortal.Frontend.Player
{
  /// <summary>
  /// A second, silent player whose only job is to look at the sound: it is handed the same thing that is
  /// playing, libVLC draws its spectrum as that second player's picture, and this class reads the drawing and
  /// answers <see cref="IAudioBandSource"/> from it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Why a whole player of its own.</b> The drawing is switched on by <c>audio-visual</c>, and that word is
  /// read from the engine as a whole, not from one media: switching it on <em>for one medium</em> was tried
  /// against libVLC 3.0.19 by adding <c>:audio-visual=visual</c> and its four <c>:effect-</c> companions to the
  /// medium alone - the sound played to the end (position 3914 of 4000 ms) and not one picture arrived, nought of
  /// them. The same options given to the engine as a whole produced two hundred and twenty-two pictures from the
  /// same file inside seven seconds. Since the option belongs to the engine, and since the engine that plays a
  /// film for a viewer must not be handed an option that starts drawing pictures at it, the listener gets its
  /// own engine. The cost of that is stated plainly: the playing file is decoded a second time, silently
  /// (<c>--aout=dummy</c>), and building an engine scans libVLC's plugins, which on a machine whose plugin cache
  /// is out of date was measured at around seventeen seconds - which is why nobody waits for it:
  /// <see cref="LibVlcPlayer.WatchSpectrum"/> builds one on a thread of its own and the bars stand at their
  /// floor until the first picture arrives.
  /// </para>
  /// <para>
  /// <b>Why no video output option.</b> <c>--vout=dummy</c> keeps the drawing from happening at all: with it the
  /// sound plays and the picture count stays at nought, while without it the same file fills the buffers. The
  /// picture is asked for the way the film path asks for its pictures - <c>SetVideoFormat</c> and
  /// <c>SetVideoCallbacks</c>, no window involved - and as there, the callbacks cannot be taken back afterwards
  /// (see <see cref="LibVlcPlayer"/>'s <c>StopHandingPicturesOver</c>), so leaving is by stopping, and stopping
  /// is what <see cref="Dispose"/> does first.
  /// </para>
  /// <para>
  /// The size, 640 by 300, is the size the reading was calibrated at; the picture is only ever looked at in
  /// fractions of its width and height, so a different size would still be read, but the numbers this class is
  /// answerable for are that size's numbers.
  /// </para>
  /// </remarks>
  public sealed class VisualSpectrumTap : IAudioBandSource, IDisposable
  {
    /// <summary>How wide the drawing is asked for, in pixels.</summary>
    public const int PictureWidth = 640;

    /// <summary>How tall the drawing is asked for, in pixels.</summary>
    public const int PictureHeight = 300;

    /// <summary>
    /// How many slices the picture is read in, which is the reference control's bar count
    /// (UI/UiComponents/Media/Controls/AudioSpectrumAnalyzer.cs:119-136) rather than a number picked here.
    /// </summary>
    public const int Bands = 32;

    private const string Chroma = "RV32";

    /// <summary>
    /// The arguments the drawing needs, and nothing else. <c>--aout=dummy</c> is what keeps a second pair of
    /// speakers silent; <c>--audio-visual=visual</c> with <c>--effect-list=spectrum</c> is what makes libVLC draw
    /// at all; the two <c>--effect-</c> sizes decide the picture this class reads. There is deliberately no
    /// <c>--vout=dummy</c> in this list - see the remarks.
    /// </summary>
    private static readonly string[] ListeningArguments =
    {
      "-I", "dummy",
      "--quiet",
      "--no-media-library",
      "--no-stats",
      "--no-osd",
      "--no-video-title-show",
      "--no-spu",
      "--no-sub-autodetect-file",
      "--no-repeat",
      "--no-loop",
      "--aout=dummy",
      "--audio-visual=visual",
      "--effect-list=spectrum",
      "--effect-width=" + PictureWidth,
      "--effect-height=" + PictureHeight
    };

    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;

    /// <summary>The buffer libVLC writes into, pinned so that the address handed to the engine stays put.</summary>
    private readonly byte[] _writing = new byte[PictureWidth * PictureHeight * 4];
    private readonly GCHandle _pin;

    /// <summary>The newest finished picture, copied out for whoever asks, under <see cref="_pictureGate"/>.</summary>
    private byte[]? _newest;
    private readonly byte[] _reading = new byte[PictureWidth * PictureHeight * 4];
    private readonly object _pictureGate = new object();

    private Media? _media;
    private bool _disposed;

    private VisualSpectrumTap(LibVLC libVlc, MediaPlayer player)
    {
      _libVlc = libVlc;
      _player = player;
      _pin = GCHandle.Alloc(_writing, GCHandleType.Pinned);
      IntPtr area = _pin.AddrOfPinnedObject();

      // The three callbacks are the whole of "draw into this array instead of a window". Only the middle one has
      // anything to do: a picture that is finished being written is the one worth copying.
      _player.SetVideoFormat(Chroma, PictureWidth, PictureHeight, (uint) (PictureWidth * 4));
      _player.SetVideoCallbacks(
          delegate(IntPtr opaque, IntPtr planes) { Marshal.WriteIntPtr(planes, area); return area; },
          delegate(IntPtr opaque, IntPtr picture, IntPtr planes)
          {
            lock (_pictureGate)
            {
              if (_newest == null)
                _newest = new byte[_writing.Length];
              Buffer.BlockCopy(_writing, 0, _newest, 0, _writing.Length);
            }
          },
          delegate(IntPtr opaque, IntPtr picture) { });
    }

    /// <summary>
    /// Build a listener, or say in one sentence why there is none. Takes as long as libVLC needs to look through
    /// its plugins, which on an out-of-date cache is tens of seconds: call this where nobody is waiting.
    /// </summary>
    /// <param name="tap">The listener, or null when <paramref name="reason"/> explains the refusal.</param>
    /// <param name="reason">What was looked at and what came of it.</param>
    public static bool TryCreate(out VisualSpectrumTap? tap, out string reason)
    {
      tap = null;
      reason = "the listener was not tried.";
      try
      {
        LibVLC libVlc = new LibVLC(ListeningArguments);
        MediaPlayer player = new MediaPlayer(libVlc);
        tap = new VisualSpectrumTap(libVlc, player);
        reason = "the listener stands and has heard nothing yet.";
        return true;
      }
      catch (VLCException ex)
      {
        reason = "libVLC would not start a listener" +
                 (string.IsNullOrWhiteSpace(ex.Message) ? "." : ": " + ex.Message.Trim());
        return false;
      }
      catch (EntryPointNotFoundException ex)
      {
        reason = "this libVLC has no memory picture output to draw into: " + ex.Message;
        return false;
      }
      catch (Exception ex)
      {
        // A second engine is a thing that can fail in ways nobody predicted - a native library that refuses a
        // second instance, a runtime that runs out of something. One honest sentence is the answer either way,
        // and the screen's bars stay at their floor.
        reason = "the listener could not be built: " + ex.GetType().Name + ": " + ex.Message;
        return false;
      }
    }

    /// <inheritdoc/>
    public int BandCount => Bands;

    /// <summary>
    /// Where the listener stands in the file it looks at, as its own engine answers; nought while it holds
    /// nothing, which is the engine's own answer for a media that has not started.
    /// </summary>
    public long TimeInMs
    {
      get
      {
        if (_disposed)
          return 0;
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
    }

    /// <summary>
    /// Put the listener at a place in the file. The listener is its own engine and nothing about the sound a
    /// viewer hears paces it, so this is the only way the two can be told to look at one and the same moment;
    /// <see cref="TapAgreement"/> says when it is worth doing, since the engine drops the pictures it was
    /// drawing on the way.
    /// </summary>
    /// <returns>false when the engine would not move, which costs a strip standing still for a moment.</returns>
    public bool StandAt(long timeMs)
    {
      if (_disposed)
        return false;
      try
      {
        _player.Time = timeMs < 0 ? 0 : timeMs;
        return true;
      }
      catch (VLCException)
      {
        return false;
      }
    }

    /// <summary>
    /// Start looking at the same thing the playing player is looking at. Safe to call again for a new file.
    /// </summary>
    /// <returns>false with a reason in <paramref name="reason"/> when the engine would not take it.</returns>
    public bool Follow(MediaLocation location, out string reason)
    {
      reason = string.Empty;
      if (_disposed)
      {
        reason = "this listener has been disposed.";
        return false;
      }

      try
      {
        ReleaseMedia();
        FromType how = location.IsFile ? FromType.FromPath : FromType.FromLocation;
        Media media = new Media(_libVlc, location.EngineString, how);
        _media = media;
        if (!_player.Play(media))
        {
          reason = $"libVLC would not accept {location.Describe()} for its own looking.";
          ReleaseMedia();
          return false;
        }

        return true;
      }
      catch (VLCException ex)
      {
        reason = "the listener could not open the file: " +
                 (string.IsNullOrWhiteSpace(ex.Message) ? "libVLC gave no reason." : ex.Message.Trim());
        return false;
      }
    }

    /// <summary>Resume looking; the bars rise again where the music does.</summary>
    public void Resume()
    {
      if (_disposed)
        return;
      try
      {
        _player.Play();
      }
      catch (VLCException)
      {
        // The listener missing one resume costs a strip that stands still for a moment.
      }
    }

    /// <summary>Stop looking while the playing one is paused, so that the caps fall instead of hanging.</summary>
    public void Pause()
    {
      if (_disposed)
        return;
      try
      {
        _player.Pause();
      }
      catch (VLCException)
      {
        // As above.
      }
    }

    /// <summary>Stop looking at the file and let go of it.</summary>
    public void Stop()
    {
      if (_disposed)
        return;
      try
      {
        _player.Stop();
      }
      catch (VLCException)
      {
        // Nothing waiting on this answer.
      }

      ReleaseMedia();
      lock (_pictureGate)
        _newest = null;
    }

    /// <inheritdoc/>
    public bool GetBandLevels(float[] levels)
    {
      if (levels == null || levels.Length == 0)
        throw new ArgumentException("there is no slice to fill.", nameof(levels));

      byte[]? picture;
      lock (_pictureGate)
        picture = _newest;
      if (picture == null)
        return false;

      // Read from a copy: the engine is writing into its own buffer while a screen is asking, and the copy is
      // made under the gate rather than the read, so a slow screen costs a stale picture and nothing worse.
      Buffer.BlockCopy(picture, 0, _reading, 0, picture.Length);
      SpectrumPicture.BandsFrom(_reading, PictureWidth, PictureHeight, levels);
      return true;
    }

    private void ReleaseMedia()
    {
      Media? media = _media;
      _media = null;
      media?.Dispose();
    }

    /// <summary>
    /// Stop the engine - the only call that actually tears its picture output down, since the binding will not
    /// take callbacks back - and then let the engine and the pinned buffer go. Never from one of libVLC's own
    /// callbacks, for the reason the film path documents at <see cref="LibVlcPlayer"/>'s teardown.
    /// </summary>
    public void Dispose()
    {
      if (_disposed)
        return;
      _disposed = true;

      try
      {
        _player.Stop();
      }
      catch (VLCException)
      {
        // On the way out; a native refusal here has nowhere to be reported.
      }

      ReleaseMedia();
      _player.Dispose();
      _libVlc.Dispose();
      lock (_pictureGate)
        _newest = null;
      if (_pin.IsAllocated)
        _pin.Free();
    }
  }
}
