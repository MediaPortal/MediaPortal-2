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
using System.Threading;

using MediaPortal.Frontend.Player;

using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The hand-off between an engine that has decoded a picture and a screen that wants to draw one.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Everything here runs without libVLC, without a codec and without a display, and it covers the part of video
  /// playback that is easiest to get wrong and impossible to see in a log: which bytes a screen ends up drawing.
  /// A slanted picture, a film stretched by the padding a decoder rounded its height up to, a black rectangle
  /// that a frame counter insists is moving - all of them are failures of this class and none of them is a crash.
  /// </para>
  /// <para>
  /// Most of this is the public road, which is the road Push and TryCopyLatest are. The engine's shorter road -
  /// being handed a buffer and writing into it - is the same hand-off with the copy taken out, and it is covered
  /// by the tests that play a real file. The two at the end of the file step through the friend declaration into
  /// that road, because the state they set up (a buffer an engine is holding) is one the public surface cannot
  /// create and disposal must survive.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestVideoFrameSink
  {
    /// <summary>A pixel as the sink holds it: blue first, and a fourth byte nobody promised anything to.</summary>
    private static void WritePixel(byte[] where, int at, byte blue, byte green, byte red)
    {
      where[at] = blue;
      where[at + 1] = green;
      where[at + 2] = red;
      where[at + 3] = 0;
    }

    /// <summary>
    /// A picture in which every row is one grey, the first row black and the last one white, so that a copy which
    /// lost rows, gained rows, or started reading at the wrong place in a row shows up in the numbers.
    /// </summary>
    private static byte[] Rows(byte[] pixels, int width, int height, int stride)
    {
      for (int y = 0; y < height; y++)
      {
        byte shade = (byte)(height <= 1 ? 0 : y * 255 / (height - 1));
        for (int x = 0, at = y * stride; x < width; x++, at += 4)
          WritePixel(pixels, at, shade, shade, shade);
      }
      return pixels;
    }

    private static byte[] Push(VideoFrameSink sink, int width, int height, int stride, byte[] pixels)
    {
      Assert.That(sink.Push(pixels, width, height, stride), Is.True,
          "the sink refused a picture that is exactly the size it was told to take");
      return pixels;
    }

    private static byte[] Push(VideoFrameSink sink, int width, int height, int stride)
    {
      return Push(sink, width, height, stride, Rows(new byte[stride * height], width, height, stride));
    }

    /// <summary>The picture a copier gets back, as the grey value each of its rows was drawn from.</summary>
    private static int[] Copy(VideoFrameSink sink, int width, int height)
    {
      int stride = width * 4;
      byte[] target = new byte[stride * height];
      Assert.That(sink.TryCopyLatest(target, stride, width, height), Is.True,
          "nothing could be copied out of a sink that had a picture pushed into it");

      int[] shades = new int[height];
      for (int y = 0; y < height; y++)
        shades[y] = target[y * stride + 2];
      return shades;
    }
    [Test]
    public void ANewSinkHasNoPictureAndSaysSo()
    {
      using VideoFrameSink sink = new VideoFrameSink();

      Assert.That(sink.Width, Is.Zero);
      Assert.That(sink.Height, Is.Zero);
      Assert.That(sink.PictureWidth, Is.Zero, "a screen sizes itself by the picture, so that has to be 0 too");
      Assert.That(sink.FramesArrived, Is.Zero);

      byte[] target = new byte[64 * 64 * 4];
      Assert.That(sink.TryCopyLatest(target, 64 * 4, 64, 64), Is.False,
          "and copying out of it says there is nothing there rather than handing over a black frame, which no "
          + "screen could tell apart from a film that happens to be black");
    }

    [Test]
    public void APushedPictureIsTheOneThatComesBackOut()
    {
      using VideoFrameSink sink = new VideoFrameSink();
      byte[] pushed = Push(sink, 8, 6, 8 * 4);

      int[] shades = Copy(sink, 8, 6);
      for (int y = 0; y < 6; y++)
      {
        Assert.That(shades[y], Is.EqualTo(pushed[y * 8 * 4 + 2]),
            "row " + y + " came back as something else than what went in");
      }

      Assert.That(sink.Width, Is.EqualTo(8));
      Assert.That(sink.Height, Is.EqualTo(6));
      Assert.That(sink.Stride, Is.EqualTo(8 * 4));
      Assert.That(sink.FramesArrived, Is.EqualTo(1));
    }

    [Test]
    public void AProducerWhoseRowsArePaddedIsStillCopiedStraight()
    {
      using VideoFrameSink sink = new VideoFrameSink();

      // Sixteen bytes of slack at the end of every row, filled with a colour that must not turn up anywhere in the
      // picture. This is what an engine that owns its own frame memory hands over, and the classic way to draw it
      // wrong is a picture sheared into a parallelogram.
      const int Width = 6;
      const int Height = 5;
      const int Stride = Width * 4 + 16;
      byte[] pixels = Rows(new byte[Stride * Height], Width, Height, Stride);
      for (int y = 0; y < Height; y++)
      {
        for (int at = y * Stride + Width * 4; at < (y + 1) * Stride; at++)
          pixels[at] = 0xFF;
      }
      Push(sink, Width, Height, Stride, pixels);

      byte[] target = new byte[Width * 4 * Height];
      Assert.That(sink.TryCopyLatest(target, Width * 4, Width, Height), Is.True);
      for (int at = 0; at < target.Length; at += 4)
      {
        Assert.That(target[at], Is.EqualTo(target[at + 2]),
            "the slack bytes of a padded row turned up in the picture, which means the rows were copied at the "
            + "wrong pitch");
      }
    }

    [Test]
    public void AFrameShorterThanItClaimsOrOfANonsenseSizeIsRefused()
    {
      using VideoFrameSink sink = new VideoFrameSink();

      Assert.That(sink.Push(new byte[100], 10, 10, 40), Is.False,
          "a frame that is shorter than its own size would be read past its end");
      Assert.That(sink.Push(new byte[4000], 0, 10, 40), Is.False, "a picture with no width is not a picture");
      Assert.That(sink.Push(new byte[4000], 10, -4, 40), Is.False);
      Assert.That(sink.Push(new byte[4000], 10, 10, 8), Is.False,
          "a row cannot hold ten pixels in eight bytes; believing that would read outside every row");

      Assert.That(sink.FramesArrived, Is.Zero, "and none of those refusals counted as a frame");
      Assert.That(sink.TryCopyLatest(new byte[4000], 40, 10, 10), Is.False);
    }

    [Test]
    public void APlaceTooSmallToPutAPictureInIsRefused()
    {
      using VideoFrameSink sink = new VideoFrameSink();
      Push(sink, 20, 12, 80);

      byte[] tooShort = new byte[80 * 11];
      Assert.That(sink.TryCopyLatest(tooShort, 80, 20, 12), Is.False,
          "the last row would have been written past the end of the caller's array");

      byte[] tooNarrow = new byte[70 * 12];
      Assert.That(sink.TryCopyLatest(tooNarrow, 70, 20, 12), Is.False,
          "a row of 20 pixels does not fit in 70 bytes, and pretending otherwise writes the next row over it");
    }
    [Test]
    public void AskingForAnotherSizeThanTheVideoHasIsRefusedRatherThanScaled()
    {
      using VideoFrameSink sink = new VideoFrameSink();
      Push(sink, 16, 9, 64);

      Assert.That(sink.TryCopyLatest(new byte[8 * 4 * 5], 8 * 4, 8, 5), Is.False,
          "the sink does not scale: a screen that asked for 8x5 and silently received the middle of a 16x9 frame "
          + "would show a picture that is neither the film nor the size it laid out for");
      Assert.That(sink.TryCopyLatest(new byte[16 * 9 * 4], 64, 16, 8), Is.False,
          "one pixel short in height is the same mistake in the other direction");
    }

    [Test]
    public void TheLinesTheDecoderRoundedUpAreNotDrawn()
    {
      using VideoFrameSink sink = new VideoFrameSink();

      // A film 8 lines high whose decoder works in blocks of 16, so the buffer holds eight more lines of whatever
      // the codec left there. At full size this is 1920x804 arriving as 1920x832, which is how it was found.
      const int Width = 8;
      const int PictureHeight = 8;
      const int BufferHeight = 16;
      byte[] pixels = Rows(new byte[Width * 4 * BufferHeight], Width, BufferHeight, Width * 4);
      for (int y = PictureHeight; y < BufferHeight; y++)
      {
        for (int x = 0, at = y * Width * 4; x < Width; x++, at += 4)
          WritePixel(pixels, at, 0, 0, 0xFF);
      }
      Push(sink, Width, BufferHeight, Width * 4, pixels);

      Assert.That(sink.SetVisibleSize(Width, PictureHeight), Is.True);
      Assert.That(sink.PictureWidth, Is.EqualTo(Width));
      Assert.That(sink.PictureHeight, Is.EqualTo(PictureHeight),
          "the sink keeps both sizes on purpose: Height is what the engine wrote, PictureHeight is what to draw");

      int[] shades = Copy(sink, Width, PictureHeight);
      Assert.That(shades[0], Is.EqualTo(0), "the first line of the film is still the first line out");
      foreach (int shade in shades)
      {
        Assert.That(shade, Is.Not.EqualTo(255),
            "a padding line came through, which stretches the film by the ratio between the two heights - a few "
            + "percent at this size, and a face that is too long on a real one");
      }

      Assert.That(sink.TryCopyLatest(new byte[Width * 4 * BufferHeight], Width * 4, Width, BufferHeight), Is.False,
          "and asking for the buffer's size is no longer something a caller can do by accident");
    }

    [Test]
    public void AVisibleSizeThatIsNotInsideTheBuffersIsRefused()
    {
      using VideoFrameSink sink = new VideoFrameSink();
      Push(sink, 16, 9, 64);

      Assert.That(sink.SetVisibleSize(17, 9), Is.False);
      Assert.That(sink.SetVisibleSize(16, 10), Is.False);
      Assert.That(sink.SetVisibleSize(0, 9), Is.False);
      Assert.That(sink.SetVisibleSize(16, -1), Is.False);
      Assert.That(sink.PictureWidth, Is.EqualTo(16),
          "a refused size left the picture exactly as it was; clamping it would crop a film nobody asked to crop");

      Assert.That(sink.SetVisibleSize(16, 9), Is.True, "the whole buffer being the picture is not a special case");
    }

    [Test]
    public void AVideoOfAnotherSizeReplacesTheOneBeforeIt()
    {
      using VideoFrameSink sink = new VideoFrameSink();
      Push(sink, 16, 9, 64);
      Assert.That(sink.SetVisibleSize(16, 8), Is.True);

      Push(sink, 32, 18, 128);

      Assert.That(sink.Width, Is.EqualTo(32));
      Assert.That(sink.PictureHeight, Is.EqualTo(18),
          "new video, new geometry: a visible size kept from the previous film would crop the new one");

      Assert.That(sink.TryCopyLatest(new byte[128 * 18], 128, 32, 18), Is.True);
      Assert.That(sink.TryCopyLatest(new byte[64 * 9], 64, 16, 9), Is.False,
          "and the old size is gone rather than still answering with the previous film's last frame");
    }

    [Test]
    public void EveryFinishedPictureIsAnnouncedWithTheSizeItShouldBeDrawnAt()
    {
      using VideoFrameSink sink = new VideoFrameSink();

      int announcements = 0;
      int announcedWidth = 0;
      int announcedHeight = 0;
      sink.FrameArrived += delegate(object? sender, VideoFrameEventArgs e)
      {
        announcements++;
        announcedWidth = e.Width;
        announcedHeight = e.Height;
      };

      Push(sink, 12, 5, 48);

      Assert.That(announcements, Is.EqualTo(1));
      Assert.That(announcedWidth, Is.EqualTo(12));
      Assert.That(announcedHeight, Is.EqualTo(5));
      Assert.That(sink.FramesArrived, Is.EqualTo(1),
          "the count a screen compares to decide whether to pull is the same count the announcements add up to");
    }
    [Test]
    public void PicturesFromSeveralThreadsArriveAndTheLastOneWins()
    {
      using VideoFrameSink sink = new VideoFrameSink();

      // Four producers, fifty pictures each, all pushing at once: what this has to survive is a decoder thread and
      // a screen copying while it writes, which is every frame of every film. What must not happen is an exception
      // escaping into the producer, a frame counted twice, or a copy reading a buffer that has been let go of.
      const int Threads = 4;
      const int Each = 50;
      Exception? failure = null;
      Thread[] workers = new Thread[Threads];
      for (int t = 0; t < Threads; t++)
      {
        int which = t;
        workers[t] = new Thread(new ThreadStart(delegate
        {
          try
          {
            byte[] pixels = new byte[16 * 9 * 4];
            for (int i = 0; i < Each; i++)
            {
              if (!sink.Push(pixels, 16, 9, 64))
                throw new InvalidOperationException("producer " + which + " was refused a frame");
              byte[] scratch = new byte[16 * 9 * 4];
              sink.TryCopyLatest(scratch, 64, 16, 9);
            }
          }
          catch (Exception ex)
          {
            Interlocked.CompareExchange(ref failure, ex, null);
          }
        }));
        workers[t].IsBackground = true;
        workers[t].Start();
      }

      for (int t = 0; t < Threads; t++)
        Assert.That(workers[t].Join(TimeSpan.FromSeconds(30)), Is.True, "a producer thread never finished");

      Assert.That(failure, Is.Null, "pushing from several threads threw: " + failure?.Message);
      Assert.That(sink.FramesArrived, Is.EqualTo(Threads * Each),
          "every picture that was pushed was counted, and none of them twice");

      Assert.That(sink.TryCopyLatest(new byte[16 * 9 * 4], 64, 16, 9), Is.True,
          "and after all that there is still a whole picture to copy");
    }

    [Test]
    public void ASinkThatHasBeenDisposedTakesNothingAndGivesNothing()
    {
      VideoFrameSink sink = new VideoFrameSink();
      Push(sink, 16, 9, 64);
      sink.Dispose();

      Assert.That(sink.TryCopyLatest(new byte[64 * 9], 64, 16, 9), Is.False,
          "a disposed sink hands out nothing; a screen that kept drawing from one would be drawing from memory "
          + "the runtime has already let go of");
      Assert.That(sink.Push(new byte[64 * 9], 16, 9, 64), Is.False,
          "and it takes nothing either, so an engine that outlives its screen cannot write into freed buffers");
      Assert.That(sink.Width, Is.Zero);

      sink.Dispose();
      Assert.That(sink.Push(new byte[64 * 9], 16, 9, 64), Is.False, "and being disposed twice is not an event");
    }

    /// <summary>
    /// The handshake that keeps a decoder out of memory that has stopped being pinned.
    /// </summary>
    /// <remarks>
    /// These two reach inside the assembly on purpose (see the friend declaration in MediaPortal.Frontend.Player):
    /// a buffer that an engine has taken and not handed back cannot be created through the public surface, and it
    /// is precisely the state that ends in a corrupted heap rather than an exception if disposal unpins it. The
    /// crash this was written for was real - disposing the sink of a playing front-end player did exactly that.
    /// </remarks>
    [Test]
    public void ASinkBeingTakenAwayTellsItsEngineToStopWritingFirst()
    {
      VideoFrameSink sink = new VideoFrameSink();
      Push(sink, 16, 9, 64);

      // An engine with a buffer in its hand, mid-write: the state libVLC is in between its lock and unlock
      // callbacks, which is the only moment at which disposal can go wrong in a way nobody can catch.
      Assert.That(sink.TryAcquireBufferForWriting(out int held, out byte[]? buffer), Is.True,
          "an engine could not take a buffer to write into");
      Assert.That(buffer, Is.Not.Null);
      Assert.That(held, Is.GreaterThanOrEqualTo(0));

      bool engineWasAsked = false;
      sink.EngineWillStopWriting = delegate
      {
        engineWasAsked = true;
        sink.EndWrite();
      };

      sink.Dispose();

      Assert.That(engineWasAsked, Is.True,
          "the sink handed its buffers back without ever asking the engine to stop writing, which leaves a "
          + "decoder writing into an array the collector is free to move");
      Assert.That(sink.ReleasedCleanly, Is.True,
          "the sink refused to let go although its engine had put the frame down");
      Assert.That(sink.TryCopyLatest(new byte[64 * 9], 64, 16, 9), Is.False,
          "and after letting go there is nothing to draw");
    }

    [Test]
    public void ASinkWhoseEngineWillNotLetGoOfAFrameKeepsItsBuffersPinned()
    {
      VideoFrameSink sink = new VideoFrameSink();
      Push(sink, 16, 9, 64);
      Assert.That(sink.TryAcquireBufferForWriting(out _, out byte[]? buffer), Is.True);
      Assert.That(buffer, Is.Not.Null);

      // An engine that never finishes the frame it took: a decoder that died, or a callback that was never wired
      // up. The sink still has to end up disposed - a screen is waiting to be torn down - but it must not take the
      // memory back from underneath the writer.
      sink.EngineWillStopWriting = delegate { };

      sink.Dispose();

      Assert.That(sink.ReleasedCleanly, Is.False,
          "the pins were freed while something was still writing into the buffer, which is a decoder writing "
          + "wherever the collector has since moved that array to; keeping them costs a few megabytes instead");
      Assert.That(sink.TryCopyLatest(new byte[64 * 9], 64, 16, 9), Is.False,
          "the sink is still disposed, so a screen gets nothing out of it either way");
    }
  }
}