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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using MediaPortal.Frontend.Player;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The part of the playback screen the film is drawn on.
  /// </summary>
  /// <remarks>
  /// <para>
  /// It follows a <see cref="VideoFrameSink"/>: whenever the engine has finished a picture, the newest one is
  /// copied onto a bitmap and shown. That is the whole design, and it is deliberately dumber than it looks -
  /// there is no per-frame callback into the renderer and no native window behind it, so the same control draws a
  /// 1080p film on Windows, on Linux and (once a screen exists to host it) on Android, and the same control can be
  /// handed a sink filled by a fake engine in a test with no native library anywhere near it.
  /// </para>
  /// <para>
  /// <b>It pulls, it does not push, and it drops frames rather than queueing them.</b> A timer asks the sink for
  /// the newest picture every frame tick and draws that one; the arrival of a picture only says "there is
  /// something newer than what I last drew". A screen that drew every frame the engine delivered would show a
  /// frame twice on a 60 Hz display playing 25 fps video, and - worse - a screen that was briefly busy would build
  /// up a queue and then play the last half-second of the film over again to catch up. A media centre that lags
  /// half a second behind the sound is unwatchable, and this shape cannot.
  /// </para>
  /// <para>
  /// <b>Nothing is scaled here either.</b> The bitmap is the size the video is, and <see cref="Image.Stretch"/>
  /// leaves the compositor to fit it into the layout - on the graphics card, at the picture's own aspect ratio,
  /// with the letterbox bars the black background shows through.
  /// </para>
  /// <para>
  /// Threading: the timer runs on the UI thread, which is the only thread allowed to touch the bitmap;
  /// <see cref="VideoFrameSink.TryCopyLatest"/> is safe to call from it while the engine writes a different
  /// buffer.
  /// </para>
  /// </remarks>
  public sealed class VideoSurface : Image, IDisposable
  {
    /// <summary>
    /// How often the newest picture is asked for. 60 Hz because a display that refreshes faster than the film
    /// should still show each picture as soon as it exists; the copy is skipped entirely when nothing new arrived.
    /// </summary>
    private static readonly TimeSpan PullInterval = TimeSpan.FromMilliseconds(16);

    private readonly DispatcherTimer _pull;

    private VideoFrameSink? _sink;
    private WriteableBitmap? _bitmap;
    private byte[]? _scratch;
    private long _lastPulled;
    private bool _disposed;

    public VideoSurface()
    {
      Stretch = Stretch.Uniform;
      // Black behind a film, rather than the theme's dark blue showing through the letterbox bars.
      // No Source yet: an Image with nothing in it takes up its space and draws nothing, which is exactly what
      // the screen wants before a video has announced its size.
      _pull = new DispatcherTimer { Interval = PullInterval };
      _pull.Tick += delegate { DrawNow(); };
    }

    /// <summary>Pictures this surface has drawn since it started following. Cumulative across videos.</summary>
    public long FramesDrawn
    {
      get; private set;
    }

    /// <summary>
    /// Why the last picture could not be drawn, or null when the last one went on screen. A surface that shows
    /// black has two reasons available - nothing arriving, and something arriving that it cannot use - and the
    /// viewer deserves to be told which of the two it is looking at.
    /// </summary>
    public string? LastFailure
    {
      get; private set;
    }

    /// <summary>
    /// The size of the picture currently on screen, or null while the surface is empty. A test looks at this to
    /// know that a video really arrived rather than only that a control exists.
    /// </summary>
    public PixelSize? PictureSize
    {
      get
      {
        WriteableBitmap? bitmap = _bitmap;
        return bitmap == null ? null : bitmap.PixelSize;
      }
    }

    /// <summary>
    /// Start showing the pictures this sink receives. Passing null - no player, or a player that refused to hand
    /// its pictures over - stops the surface and leaves it empty, which the playback screen shows as its
    /// "nothing to draw" message instead of a black hole.
    /// </summary>
    public void Follow(VideoFrameSink? sink)
    {
      if (_disposed)
        return;

      if (ReferenceEquals(_sink, sink))
        return;

      _sink = sink;
      _lastPulled = 0;

      if (sink == null)
      {
        _pull.Stop();
        Clear();
        return;
      }

      // One tick behind the picture is as good as none, so the first pull is not waited for.
      DrawNow();
      if (!_pull.IsEnabled)
        _pull.Start();
    }

    /// <summary>
    /// Stop drawing and let the bitmap go. Safe to call twice, and safe to call from a window's Closed handler,
    /// which is what it is normally called from.
    /// </summary>
    public void Dispose()
    {
      if (_disposed)
        return;
      _disposed = true;
      _pull.Stop();
      _sink = null;
      Clear();
    }

    /// <summary>
    /// Take the newest picture from the sink and put it on screen, if it is newer than the one already there.
    /// </summary>
    /// <returns>True when a picture was drawn.</returns>
    /// <remarks>
    /// The timer calls this sixty times a second, and <see cref="Follow"/> calls it once so that the first picture
    /// is not a tick late. It is public because the headless test platform never ticks a timer - a test that waits
    /// for one here waits forever - and a screen that can only be driven by a clock is a screen whose picture
    /// nobody can check on a host that has no clock to spare. A test calling it by hand goes through the same code
    /// the running application goes through; only the reason for looking is different.
    /// </remarks>
    public bool DrawNow()
    {
      if (_disposed)
        return false;

      VideoFrameSink? sink = _sink;
      if (sink == null)
        return false;

      long arrived = sink.FramesArrived;
      if (arrived == _lastPulled)
        return false;

      // What a screen draws is the picture, not the buffer the decoder happened to need: the two differ by the
      // lines a codec rounded up, and copying the buffer would stretch the film to fit them in.
      int width = sink.PictureWidth;
      int height = sink.PictureHeight;
      if (width <= 0 || height <= 0)
        return false;

      EnsureBitmap(width, height);
      WriteableBitmap bitmap = _bitmap!;
      byte[] scratch = _scratch!;

      // The frame number is only noted once the picture is really on the bitmap. A copy that failed because the
      // video changed size in the moment between reading the size and copying has to be tried again, and a screen
      // that had already written the number down would wait for a next frame that shows the old size anyway.
      if (!sink.TryCopyLatest(scratch, width * 4, width, height))
        return false;

      // The framebuffer may have a pitch wider than the picture, so the rows go in one at a time; assuming they
      // match is how a picture ends up sheared into a parallelogram.
      using (ILockedFramebuffer frame = bitmap.Lock())
      {
        if (frame.RowBytes < width * 4)
          return false;

        // The engine hands over blue-first bytes, and asked for a blue-first bitmap, and on some builds of
        // Avalonia that is not what arrives: red-first instead, which is the same picture with its red and blue
        // traded, so a forest goes to rust and a sky to copper. The format that actually came back is therefore
        // looked at rather than assumed, and the bytes are turned over when they need turning over. This costs one
        // pass over the frame on the platforms that need it and nothing on the others.
        if (frame.Format == PixelFormat.Rgba8888)
          SwapRedAndBlue(scratch, width * height * 4);
        else if (frame.Format != PixelFormat.Bgra8888)
        {
          LastFailure = "the screen handed out " + frame.Format + " for a video picture, and this surface only "
              + "knows how to fill 32-bit blue-first or red-first buffers.";
          return false;
        }

        for (int row = 0, source = 0, target = 0; row < height; row++, source += width * 4, target += frame.RowBytes)
        {
          Marshal.Copy(scratch, source, IntPtr.Add(frame.Address, target), width * 4);
        }
      }

      _lastPulled = arrived;
      LastFailure = null;
      FramesDrawn++;
      return true;
    }

    /// <summary>
    /// Turn blue-first bytes into red-first ones, in place.
    /// </summary>
    private static void SwapRedAndBlue(byte[] pixels, int length)
    {
      for (int at = 0; at + 3 < length; at += 4)
      {
        byte first = pixels[at];
        pixels[at] = pixels[at + 2];
        pixels[at + 2] = first;
      }
    }

    private void EnsureBitmap(int width, int height)
    {
      WriteableBitmap? bitmap = _bitmap;
      if (bitmap != null && bitmap.PixelSize.Width == width && bitmap.PixelSize.Height == height)
        return;

      // A change of size is a change of video (or a resolution change mid-stream, which a broadcast TS can do);
      // the old bitmap goes to the garbage collector, which is also what lets an Image still holding it render
      // one last valid frame rather than a torn one.
      bitmap?.Dispose();
      // The size of the video and no scaling of our own: the compositor fits it, at its own aspect ratio, on the
      // graphics card. AlphaFormat.Opaque is not a micro-optimisation - the engine's fourth byte is padding whose
      // value nobody promised, and a compositor told to trust it can be talked into drawing a film at zero
      // opacity, which looks exactly like a picture that never arrived.
      _bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96),
          PixelFormat.Bgra8888, AlphaFormat.Opaque);
      _scratch = new byte[height * width * 4];
      Source = _bitmap;
    }

    private void Clear()
    {
      Source = null;
      _bitmap?.Dispose();
      _bitmap = null;
      _scratch = null;
    }
  }
}
