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
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;

using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The part of the playback screen that shows the film: what it does when pictures arrive, and what it claims
  /// to have done with them.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These tests answer their part of "can I see it?" without a decoder, a display or a graphics driver: the
  /// pictures are pushed into the sink by hand, which is the same door the engine is handed a key to, and what is
  /// measured is what the surface made of them - that it drew them, how big it made them, how often it looked,
  /// and whether it ever drew something that never arrived.
  /// </para>
  /// <para>
  /// What is deliberately not measured here is the colour of the pixels. The test platform draws with a managed
  /// stub whose frame buffer is not the picture (measured: a buffer read back out of it holds neither what was
  /// written to it nor anything else), so a claim about colours written here would be a claim about a stub. The
  /// colours are checked where real rendering happens - the render probe pushes the same bars through the same
  /// surface on Windows and in the Linux container, finds them by their borders, and fails if bar three is where
  /// bar one should be or if a channel has been traded.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestVideoSurface
  {
    private VideoFrameSink? _sink;
    private readonly List<Window> _windows = new List<Window>();

    [TearDown]
    public void TearDown()
    {
      foreach (Window window in _windows)
      {
        try
        {
          window.Close();
        }
        catch (ObjectDisposedException)
        {
          // Already closed; nothing left to put down.
        }
      }
      _windows.Clear();
      _sink?.Dispose();
      _sink = null;
      Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// Runs the dispatcher for a while, the way an application that is being used does - layout and bindings need
    /// this. The picture does not: a timer never ticks inside the test platform (measured, not assumed, with a
    /// <c>DispatcherTimer</c> left running for a second while the queue was pumped), which is why <see cref="Draw"/>
    /// has to ask the surface for the picture the way its own timer does.
    /// </summary>
    private static void RunTheClockFor(TimeSpan span)
    {
      DateTime until = DateTime.UtcNow + span;
      while (DateTime.UtcNow < until)
      {
        Dispatcher.UIThread.RunJobs();
        System.Threading.Thread.Sleep(5);
      }
      Dispatcher.UIThread.RunJobs();
    }

    private static VideoSurface SurfaceOn(Visual root)
    {
      VideoSurface? found = root.GetVisualDescendants().OfType<VideoSurface>().SingleOrDefault();
      if (found == null)
      {
        string tree = string.Join(",", root.GetVisualDescendants()
            .Take(200)
            .Select(v => v.GetType().Name)
            .ToArray());
        throw new AssertionException("the playback screen has no video surface on it; tree was: " + tree);
      }
      return found;
    }

    /// <summary>
    /// Lets the screen look at the sink, which is what its own timer does sixty times a second. Everything after
    /// the asking is the code the running application goes through: the copy, the size the surface gives itself,
    /// and the frame counter that a gate later reads.
    /// </summary>
    /// <returns>How many pictures the surface drew just now.</returns>
    private static int Draw(Visual root)
    {
      VideoSurface surface = SurfaceOn(root);
      int drawn = 0;
      while (surface.DrawNow())
        drawn++;
      Dispatcher.UIThread.RunJobs();
      return drawn;
    }

    private static byte[] Picture(int width, int height)
    {
      byte[] pixels = new byte[width * 4 * height];
      for (int at = 0; at + 3 < pixels.Length; at += 4)
      {
        pixels[at] = 0x40;
        pixels[at + 1] = 0x80;
        pixels[at + 2] = 0xC0;
        pixels[at + 3] = 0xFF;
      }
      return pixels;
    }

    private NowPlayingView Show(FakePlayer player, out NowPlayingViewModel screen, bool withVideo)
    {
      screen = new NowPlayingViewModel(player, action => Dispatcher.UIThread.Post(action));
      _sink = withVideo ? new VideoFrameSink() : null;
      if (withVideo)
        Assert.That(screen.AttachVideo(_sink!), Is.True, "the screen refused the pictures");

      NowPlayingView view = new NowPlayingView(screen, _sink);
      ShellWindow window = new ShellWindow(view);
      window.Show();
      _windows.Add(window);
      RunTheClockFor(TimeSpan.FromMilliseconds(120));
      return view;
    }
    [AvaloniaTest]
    public void APictureFromThePlayerEndsUpOnTheScreen()
    {
      var player = new FakePlayer();
      NowPlayingView window = Show(player, out NowPlayingViewModel screen, true);

      Assert.That(window.FramesDrawn, Is.Zero, "a screen with nothing to show has drawn nothing");
      Assert.That(window.PictureSize, Is.Null);

      Assert.That(_sink!.Push(Picture(8, 6), 8, 6, 32), Is.True);
      Assert.That(Draw(window), Is.GreaterThan(0),
          "a picture was waiting in the sink and the surface said it had drawn nothing");

      Assert.That(window.FramesDrawn, Is.GreaterThan(0),
          "a picture arrived in memory and the screen never looked at it");
      Assert.That(window.PictureSize!.Value.Width, Is.EqualTo(8),
          "the screen sized itself to something other than the film that arrived");
      Assert.That(window.PictureSize!.Value.Height, Is.EqualTo(6));
      Assert.That(SurfaceOn(window).LastFailure, Is.Null,
          "the surface claims it drew a picture it could not fill");
    }

    [AvaloniaTest]
    public void TheLinesTheDecoderRoundedUpAreNotTheSizeTheScreenTakes()
    {
      var player = new FakePlayer();
      NowPlayingView window = Show(player, out NowPlayingViewModel screen, true);

      // Eight lines of film in a buffer of twelve: what 1920x804 arriving in a 1920x832 buffer looks like at
      // kitchen-table scale. A screen that sizes itself by the buffer stretches the film by three percent, which
      // is the kind of wrong that nobody can name but everybody sees as faces that are too long.
      Assert.That(_sink!.Push(Picture(8, 12), 8, 12, 32), Is.True);
      Assert.That(_sink.SetVisibleSize(8, 6), Is.True);
      Draw(window);

      Assert.That(window.PictureSize!.Value.Height, Is.EqualTo(6),
          "the screen sized itself to the decoder's buffer rather than to the picture inside it");
      Assert.That(window.PictureSize!.Value.Width, Is.EqualTo(8));
      Assert.That(SurfaceOn(window).LastFailure, Is.Null);
    }

    [AvaloniaTest]
    public void AFrameThatArrivesLaterReplacesTheOneAlreadyOnScreen()
    {
      var player = new FakePlayer();
      NowPlayingView window = Show(player, out NowPlayingViewModel screen, true);

      Assert.That(_sink!.Push(Picture(8, 6), 8, 6, 32), Is.True);
      Draw(window);
      long first = window.FramesDrawn;
      Assert.That(first, Is.GreaterThan(0));

      // A different size, because a size is the one thing about a picture this platform lets a test see: a screen
      // that had stopped looking would still report the old one.
      Assert.That(_sink.Push(Picture(4, 3), 4, 3, 16), Is.True);
      Draw(window);

      Assert.That(window.FramesDrawn, Is.GreaterThan(first),
          "the screen drew the first frame and then stopped looking, which is a film frozen on its first frame");
      Assert.That(window.PictureSize!.Value.Width, Is.EqualTo(4),
          "and it is still showing the earlier of the two pictures");
      Assert.That(window.PictureSize!.Value.Height, Is.EqualTo(3));
    }

    [AvaloniaTest]
    public void AScreenWithNoPicturesToDrawDoesNotClaimToHaveDrawnAny()
    {
      var player = new FakePlayer();
      NowPlayingView window = Show(player, out NowPlayingViewModel screen, false);

      Assert.That(player.Calls, Does.Not.Contain("AttachToFrames"),
          "a screen built without a sink did not ask the player for one");
      Assert.That(window.FramesDrawn, Is.Zero,
          "a frame counter that counted something here would let a gate pass a playback screen that can only ever "
          + "show black");
      Assert.That(window.PictureSize, Is.Null);

      RunTheClockFor(TimeSpan.FromMilliseconds(150));
      Assert.That(Draw(window), Is.Zero, "a screen with nothing to follow drew a picture out of nowhere");
      Assert.That(window.FramesDrawn, Is.Zero, "still nothing after the clock has run for a while");
    }

    [AvaloniaTest]
    public void AScreenDoesNotRedrawAPictureItHasAlreadyDrawn()
    {
      var player = new FakePlayer();
      NowPlayingView window = Show(player, out NowPlayingViewModel screen, true);

      Assert.That(_sink!.Push(Picture(8, 6), 8, 6, 32), Is.True);
      Draw(window);
      long drawn = window.FramesDrawn;
      Assert.That(drawn, Is.GreaterThan(0));

      // A paused film is the ordinary case for this: the engine stops delivering, and the screen should sit still
      // rather than copy the same two megabytes sixty times a second for as long as somebody is thinking. A
      // hundred asks is the same hundred ticks of the timer, and free.
      for (int i = 0; i < 100; i++)
        Assert.That(SurfaceOn(window).DrawNow(), Is.False, "the surface drew the same picture twice");
      Assert.That(window.FramesDrawn, Is.EqualTo(drawn), "a paused film should cost nothing to display");
    }
  }
}