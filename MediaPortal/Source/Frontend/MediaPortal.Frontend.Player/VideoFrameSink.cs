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

namespace MediaPortal.Frontend.Player
{
  /// <summary>
  /// Pictures the engine decoded, held in memory the engine writes and a screen reads.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is how video reaches a screen without this assembly knowing what a window is. The engine asks for
  /// buffers of a given size once it knows the video's size, decodes into one of them, and says "that one is
  /// finished"; whatever draws the picture takes the newest finished one whenever it next has a frame to spare.
  /// Between the two there is this class, one copy of the bytes, and no platform type - which is the same path on
  /// Windows, on Linux, and in a container where nobody is watching at all.
  /// </para>
  /// <para>
  /// <b>Three buffers, deliberately.</b> The engine needs somewhere to decode while a screen is still copying the
  /// picture before, and three is the smallest number that lets a screen copy the picture that was current when it
  /// started: with two, the copier is always reading the frame immediately behind the one being written, which
  /// works, but a screen that is slow then shows the same frame twice instead of simply skipping it. The
  /// alternative - the decoder waiting for the screen - is worse where it counts: a UI thread busy with layout
  /// would stall the decoder, and a stalled decoder is a stuttering film. A torn frame, which is what the
  /// remaining race produces when a copy and a write do meet, is gone on the next refresh.
  /// </para>
  /// <para>
  /// <b>Why the buffers are pinned managed arrays rather than unmanaged memory.</b> So that a screen can be handed
  /// a <see cref="Span{T}"/> and copy with ordinary bounds-checked code, and so that forgetting to release cannot
  /// leak a block the garbage collector never sees. Pinning is the price: three frames of 1080p is about 25 MB of
  /// pinned memory for as long as a video is open, which a media centre can afford and a decoder needs anyway.
  /// </para>
  /// <para>
  /// Threading: <see cref="FrameArrived"/> fires on the engine's thread, like every other event in this assembly
  /// (see <see cref="IVideoPlayer"/>), and <see cref="TryCopyLatest"/> may be called from any thread. A screen that
  /// draws on its own thread notes the arrival and posts itself a frame; it does not draw inside the event.
  /// </para>
  /// </remarks>
  public sealed class VideoFrameSink : IDisposable
  {
    /// <summary>How many pictures are in rotation. See the remarks; three is chosen, not left over.</summary>
    internal const int BufferCount = 3;

    /// <summary>Four bytes a pixel: blue, green, red, and a fourth the engine leaves alone.</summary>
    internal const int BytesPerPixel = 4;

    /// <summary>Where a picture stops being believable, so that a broken header cannot ask for a gigabyte.</summary>
    private const int MaxDimension = 8192;

    private readonly object _gate = new object();

    /// <summary>One pinned picture buffer each. The engine is given the pinned address of these.</summary>
    private readonly byte[]?[] _pixels = new byte[]?[BufferCount];

    private readonly GCHandle[] _pinned = new GCHandle[BufferCount];

    private int _width;
    private int _height;
    private int _stride;

    // The part of the buffer that is picture. A decoder rounds its frame up to the block it works in - HEVC to
    // 64 lines, so a film whose picture is 804 lines high arrives in a buffer 832 lines long. The spare lines are
    // not a black border: they are whatever the decoder left there, and drawing them stretches the image by the
    // ratio between the two heights, which is nothing on a 1080p film and three and a half percent on this one.
    // See SetVisibleSize, which is how the engine says where the picture stops.
    private int _pictureWidth;
    private int _pictureHeight;

    /// <summary>Which buffer the engine should decode into next, and which was finished last.</summary>
    private int _nextToWrite;

    private int _newest = -1;
    private long _frames;
    private bool _released;

    // How many buffers an engine holds at this moment, and whether somebody has started taking this sink away.
    // Both exist for one hazard: the addresses handed to a decoder point *inside managed arrays*, and from the
    // moment their pin is removed the collector may move them. A decoder writing into an array that has moved is
    // not an exception anybody could catch - it is the heap of the whole process. So a sink that is let go tells
    // its engine to stop first and then waits for the writers it still has.
    private int _writing;
    private int _releasing;

    /// <summary>How long letting go waits for an engine to stop writing into buffers that are being taken away.</summary>
    private const int HandoverGraceMilliseconds = 500;

    /// <summary>
    /// What the engine attached to this sink is asked to do before the buffers go away: stop writing into them, so
    /// that no thread is about to write through an address that is about to stop being pinned. Set by
    /// <see cref="IVideoPlayer.AttachTo(VideoFrameSink)"/>; a sink nobody attached to simply has nothing to stop.
    /// </summary>
    internal Action? EngineWillStopWriting { get; set; }

    /// <summary>
    /// False when this sink was disposed while an engine was still writing into its buffers. The pins are left in
    /// place in that case rather than freed under the writer's feet - a few megabytes a process will not get back
    /// instead of a corrupted heap - and the fact is reported here rather than hidden.
    /// </summary>
    public bool ReleasedCleanly { get; private set; } = true;

    /// <summary>
    /// A sink that has seen no video yet. A screen may create one before a player exists; an engine fills it in
    /// when <see cref="IVideoPlayer.AttachTo(VideoFrameSink)"/> is called.
    /// </summary>
    public VideoFrameSink()
    {
    }

    /// <summary>
    /// Fires after a picture is complete, on the engine's thread, as often as the engine decodes. A screen that
    /// draws inside this handler will draw some frames twice; it should note the arrival and ask for the newest
    /// when it next has a frame to spare.
    /// </summary>
    public event EventHandler<VideoFrameEventArgs>? FrameArrived;

    /// <summary>
    /// Width of the buffers the engine writes, which is the width of the picture unless the engine said otherwise
    /// with <see cref="SetVisibleSize"/>. Zero until the engine has announced a video.
    /// </summary>
    public int Width
    {
      get { return Volatile.Read(ref _width); }
    }

    /// <summary>Height of the buffers the engine writes; see <see cref="Width"/>.</summary>
    public int Height
    {
      get { return Volatile.Read(ref _height); }
    }

    /// <summary>
    /// Width of what should be drawn, which is <see cref="Width"/> for a video whose width needed no padding.
    /// This is the number a screen sizes itself by.
    /// </summary>
    public int PictureWidth
    {
      get { return Volatile.Read(ref _pictureWidth); }
    }

    /// <summary>Height of what should be drawn; see <see cref="PictureWidth"/>.</summary>
    public int PictureHeight
    {
      get { return Volatile.Read(ref _pictureHeight); }
    }

    /// <summary>
    /// Tell the sink that the picture inside the buffers is smaller than the buffers, and where it therefore stops.
    /// </summary>
    /// <returns>
    /// False when the numbers do not fit inside the buffers - which is the only way this can be wrong, and the
    /// reason it returns false rather than clamping: a clamped picture is a cropped film that looks fine until
    /// somebody notices the subtitles are gone.
    /// </returns>
    /// <remarks>
    /// Call it before the first picture and the screen never sees the padding at all. Calling it with the buffer
    /// size says the same thing as having never called it, so an engine that measures every video and always gets
    /// a whole number of blocks needs no special case.
    /// </remarks>
    public bool SetVisibleSize(int width, int height)
    {
      int bufferWidth = Volatile.Read(ref _width);
      int bufferHeight = Volatile.Read(ref _height);
      if (bufferWidth <= 0 || width <= 0 || height <= 0)
        return false;
      if (width > bufferWidth || height > bufferHeight)
        return false;

      Volatile.Write(ref _pictureWidth, width);
      Volatile.Write(ref _pictureHeight, height);
      return true;
    }

    /// <summary>
    /// Bytes from the start of one row to the start of the next, which is not necessarily width &times; 4 because
    /// an engine may pad rows; a screen that assumed it were would draw a slanted picture.
    /// </summary>
    public int Stride
    {
      get { return Volatile.Read(ref _stride); }
    }

    /// <summary>Bytes per pixel, 4 today: blue, green, red, and an untouched fourth.</summary>
    public int BytesPerPixelValue
    {
      get { return BytesPerPixel; }
    }

    /// <summary>Pictures delivered since this sink existed, including the ones nobody looked at.</summary>
    public long FramesArrived
    {
      get { return Interlocked.Read(ref _frames); }
    }

    /// <summary>
    /// Copy the newest finished picture into <paramref name="destination"/>, row by row.
    /// </summary>
    /// <param name="destination">
    /// Where the bytes go. Must hold <c>destinationStride &times; Height</c> bytes. A <c>byte[]</c> converts here
    /// without a copy of its own.
    /// </param>
    /// <param name="destinationStride">Bytes per row in the destination, which may be its own pitch.</param>
    /// <param name="destinationWidth">
    /// Width the caller wants, which has to be <see cref="Width"/>. Asking for another size returns false rather
    /// than quietly scaling, because the engine scales properly and this class must not pretend to: a screen that
    /// wants a smaller picture draws the picture it copied into a smaller control.
    /// </param>
    /// <param name="destinationHeight">Height the caller wants; see <paramref name="destinationWidth"/>.</param>
    /// <returns>
    /// False when no picture has arrived yet, when the size asked for is not the size the video has, or when the
    /// destination is too small. Never throws.
    /// </returns>
    public bool TryCopyLatest(Span<byte> destination, int destinationStride, int destinationWidth,
                              int destinationHeight)
    {
      int stride = Volatile.Read(ref _stride);
      int width = Volatile.Read(ref _pictureWidth);
      int height = Volatile.Read(ref _pictureHeight);
      if (width <= 0 || height <= 0 || stride <= 0)
        return false;
      if (destinationWidth != width || destinationHeight != height)
        return false;

      int rowBytes = width * BytesPerPixel;
      if (destinationStride < rowBytes)
        return false;

      // Every row offset is below the destination's own length, which is an int, so the multiplication cannot
      // overflow for any destination this check lets through.
      long needed = (long)destinationStride * height;
      if (destination.Length < needed)
        return false;

      byte[]? source;
      lock (_gate)
      {
        // A disposed sink serves nothing, even in the one case where it had to keep its buffers: those buffers are
        // pinned because an engine is still writing into them, which is a reason not to free them and not a reason
        // to hand a half-written picture to a screen that has already been torn down.
        source = !_released && _newest >= 0 ? _pixels[_newest] : null;
        if (source == null)
          return false;
      }

      // Rows start at the buffer's pitch and stop at the picture's width, so both a padded row and a padded tail
      // are left behind without anyone having to know which of the two this video had.
      for (int row = 0, offset = 0; row < height; row++, offset += destinationStride)
      {
        source.AsSpan(row * stride, rowBytes).CopyTo(destination.Slice(offset, rowBytes));
      }
      return true;
    }

    /// <summary>
    /// Hand a finished picture to the sink: the newest one, from now on, is this.
    /// </summary>
    /// <param name="pixels">The picture, row after row, four bytes a pixel.</param>
    /// <param name="width">Width in pixels. A different width than last time announces a new video size.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="stride">Bytes per row in <paramref name="pixels"/>.</param>
    /// <returns>
    /// False when the numbers do not describe a picture, when <paramref name="pixels"/> is shorter than
    /// <c>stride &times; height</c>, or when there is nowhere to put it. Never throws.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the road every producer ends up on. libVLC does not come through here - it is handed the pinned
    /// buffer and writes into it itself, which saves the copy this method makes - but the road is public anyway
    /// for two good reasons: the next engine (an MPV binding, an Android surface, a hardware decoder that hands
    /// over bytes rather than pointers) has an engine-free way in, and a screen can be tested with pictures that
    /// no native library produced, which is the only way the drawing half is ever covered in a container.
    /// </para>
    /// </remarks>
    public bool Push(ReadOnlySpan<byte> pixels, int width, int height, int stride)
    {
      if (width <= 0 || height <= 0 || stride < width * BytesPerPixel)
        return false;
      if (pixels.Length < (long)stride * height)
        return false;

      if (Width != width || Height != height)
      {
        if (Allocate(width, height, out _) == 0)
          return false;
      }

      if (!TryAcquireBufferForWriting(out int index, out byte[]? buffer) || buffer == null)
        return false;

      int rowBytes = width * BytesPerPixel;
      int rowStride = Stride;
      try
      {
        for (int row = 0, offset = 0; row < height; row++, offset += stride)
        {
          // A bounds-checked copy into a buffer nobody else can reach by index - which is the difference between
          // this road and the one libVLC takes.
          pixels.Slice(offset, rowBytes).CopyTo(buffer.AsSpan(row * rowStride, rowBytes));
        }

        Finish(index);
        return true;
      }
      finally
      {
        // Whatever came of the copy, nobody is writing into this buffer any more, and a count left behind here
        // would make every later let-go wait half a second and then refuse to free the memory.
        EndWrite();
      }
    }

    /// <summary>
    /// Let the buffers go but stay willing to take video: a change of resolution mid-film, an engine that stops
    /// and starts. A screen reading during this window gets false from <see cref="TryCopyLatest"/> rather than a
    /// picture of the old size.
    /// </summary>
    internal void Abandon()
    {
      // The engine's own cleanup, which can arrive while a frame it locked earlier is still in its hands. Those
      // buffers are then left where they are rather than freed under it, and the next format callback allocates.
      if (!WritersAreQuiet())
        return;

      lock (_gate)
      {
        if (_released)
          return;
        ReleaseBuffersLocked();
      }
    }

    /// <summary>
    /// The engine has announced a video of this size; hand out buffers to decode into.
    /// </summary>
    /// <returns>
    /// How many buffers the engine may use at once, or 0 to refuse - which is what happens when the numbers do not
    /// describe a picture or the memory is not there. libVLC reads 0 as "no video output", which is the truth.
    /// </returns>
    /// <remarks>
    /// Called by the engine, on the engine's thread, before any picture. Being called again with a different size
    /// is a change of resolution mid-film; the old buffers are let go of here after waiting for the engine to put
    /// any frame it still holds down, which is what makes that safe - a screen never holds a buffer of this class
    /// at all, it copies into its own memory.
    /// </remarks>
    internal int Allocate(int width, int height, out int stride)
    {
      stride = 0;
      if (_released || width <= 0 || height <= 0 || width > MaxDimension || height > MaxDimension)
        return 0;

      // A new size means the buffers of the old one are handed back, and an engine still holding one of them would
      // be writing into memory that has just stopped being pinned. Refusing the new size is the cheaper of the two
      // outcomes: libVLC carries on with the picture it has, instead of the process losing its heap.
      if (!WritersAreQuiet())
        return 0;

      int rowBytes = width * BytesPerPixel;
      long size = (long)rowBytes * height;

      byte[]?[] pixels = new byte[]?[BufferCount];
      GCHandle[] pinned = new GCHandle[BufferCount];
      for (int i = 0; i < BufferCount; i++)
      {
        try
        {
          pixels[i] = new byte[size];
          pinned[i] = GCHandle.Alloc(pixels[i], GCHandleType.Pinned);
        }
        catch (OutOfMemoryException)
        {
          // Part of a set is no use to anybody: the engine was told it could have i buffers and would write into
          // a pointer that has already been handed back.
          for (int j = 0; j < i; j++)
          {
            if (pinned[j].IsAllocated)
              pinned[j].Free();
          }
          return 0;
        }
      }

      lock (_gate)
      {
        ReleaseBuffersLocked();
        Array.Copy(pixels, _pixels, BufferCount);
        Array.Copy(pinned, _pinned, BufferCount);
        _width = width;
        _height = height;
        _stride = rowBytes;
        _pictureWidth = width;
        _pictureHeight = height;
        _nextToWrite = 0;
      }

      stride = rowBytes;
      return BufferCount;
    }

    /// <summary>
    /// Where the engine should decode next: the buffer's index and its pinned address.
    /// </summary>
    /// <param name="index">The buffer's index, to hand back to <see cref="Finish"/>.</param>
    /// <param name="address">The address to write <see cref="Stride"/> &times; <see cref="Height"/> bytes into.</param>
    /// <returns>False when this sink is not taking video.</returns>
    internal bool TryAcquireForWriting(out int index, out IntPtr address)
    {
      if (!TryAcquireBufferForWriting(out index, out byte[]? buffer) || buffer == null)
      {
        address = IntPtr.Zero;
        return false;
      }
      address = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);
      return true;
    }

    /// <summary>
    /// The same hand-off as <see cref="TryAcquireForWriting"/>, for a producer that would rather write into an
    /// array than through a pointer.
    /// </summary>
    internal bool TryAcquireBufferForWriting(out int index, out byte[]? buffer)
    {
      index = -1;
      buffer = null;
      if (_released || Volatile.Read(ref _releasing) != 0)
        return false;

      lock (_gate)
      {
        int chosen = _nextToWrite;
        byte[]? candidate = _pixels[chosen];
        if (candidate == null || !_pinned[chosen].IsAllocated)
          return false;

        _nextToWrite = (chosen + 1) % BufferCount;
        index = chosen;
        buffer = candidate;
        Interlocked.Increment(ref _writing);
        return true;
      }
    }

    /// <summary>
    /// The engine is done writing into the buffer it acquired - the bytes are in, nobody is touching them any more.
    /// </summary>
    /// <remarks>
    /// Paired with the acquisition, and only the writer knows when its write is over: for libVLC that is the unlock
    /// callback, which happens <i>before</i> the picture is put on screen. Letting go waits on this count.
    /// </remarks>
    internal void EndWrite()
    {
      Interlocked.Decrement(ref _writing);
    }

    /// <summary>
    /// Wait until no engine holds a buffer, for at most <paramref name="milliseconds"/>.
    /// </summary>
    /// <returns>True when the buffers are quiet, false when a writer is still in one.</returns>
    private bool WaitForWriters(int milliseconds)
    {
      for (int waited = 0; waited < milliseconds; waited += 2)
      {
        if (Volatile.Read(ref _writing) <= 0)
          return true;
        Thread.Sleep(2);
      }
      return Volatile.Read(ref _writing) <= 0;
    }

    /// <summary>
    /// Whether nobody is writing into these buffers, waiting a bounded moment for the last frame to be released.
    /// Anyone about to hand the buffers back - a new size, a cleanup, a disposal - asks this first.
    /// </summary>
    private bool WritersAreQuiet()
    {
      return Volatile.Read(ref _writing) <= 0 || WaitForWriters(HandoverGraceMilliseconds);
    }

    /// <summary>
    /// The picture in <paramref name="index"/> is complete. From now on that is the one a screen gets.
    /// </summary>
    internal void Finish(int index)
    {
      if (index < 0 || index >= BufferCount)
        return;

      lock (_gate)
      {
        if (!_pinned[index].IsAllocated)
          return;
        _newest = index;
      }

      long count = Interlocked.Increment(ref _frames);
      FrameArrived?.Invoke(this, new VideoFrameEventArgs(PictureWidth, PictureHeight, Stride, count));
    }

    /// <summary>
    /// Stop taking video and let the buffers go. The engine must not be handed another picture after this.
    /// </summary>
    public void Dispose()
    {
      Release();
    }

    internal void Release()
    {
      // Idempotence is an Interlocked rather than the gate, because the wait below must not be carried while
      // holding the gate: the writer being waited for finishes inside a callback that takes that same gate.
      if (Interlocked.Exchange(ref _releasing, 1) != 0)
        return;

      // The engine goes first: once it has stopped there can be no new writer, and the ones already holding a
      // buffer have a bounded moment to leave it.
      EngineWillStopWriting?.Invoke();
      bool quiet = WritersAreQuiet();

      lock (_gate)
      {
        _released = true;
        if (quiet)
        {
          ReleaseBuffersLocked();
        }
        else
        {
          // Somebody is still in there. The pins stay where they are: an address the collector cannot move costs a
          // few megabytes that this process will not see again, and an address it can move costs the process its
          // heap. The choice is recorded rather than kept quiet.
          ReleasedCleanly = false;
        }
      }
    }

    private void ReleaseBuffersLocked()
    {
      for (int i = 0; i < BufferCount; i++)
      {
        if (_pinned[i].IsAllocated)
          _pinned[i].Free();
        _pixels[i] = null;
      }
      _newest = -1;
      _width = 0;
      _height = 0;
      _stride = 0;
      _pictureWidth = 0;
      _pictureHeight = 0;
    }
  }

  /// <summary>What one finished picture was: its size, and how many had gone by.</summary>
  public sealed class VideoFrameEventArgs : EventArgs
  {
    public VideoFrameEventArgs(int width, int height, int stride, long frameNumber)
    {
      Width = width;
      Height = height;
      Stride = stride;
      FrameNumber = frameNumber;
    }

    public int Width { get; }
    public int Height { get; }
    public int Stride { get; }

    /// <summary>1 for the first picture, so a screen can see that it skipped some.</summary>
    public long FrameNumber { get; }

    public override string ToString()
    {
      return "#" + FrameNumber + " " + Width + "x" + Height + " stride " + Stride;
    }
  }
}

