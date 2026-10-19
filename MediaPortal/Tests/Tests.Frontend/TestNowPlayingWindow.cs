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
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The playback screen as a viewer uses it: on screen, with the transport buttons pressed and the remote's
  /// keys pressed, in front of a player that answers.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The player here is the fake one. What is under test is the screen - which text it shows, which key does
  /// what, whether the bar follows the engine - and the player's own behaviour has its own tests, one with a
  /// fake and one with the real libVLC. Testing the real engine through a window would only test it twice while
  /// making it unclear which of the two broke.
  /// </para>
  /// <para>
  /// The marshaller is the real one (<see cref="Dispatcher.UIThread"/>), because that is what the application
  /// hands the screen; the tests then run the dispatcher's queue the way a running application does.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestNowPlayingWindow
  {
    private readonly List<Window> _windows = new List<Window>();
    private readonly List<NowPlayingViewModel> _screens = new List<NowPlayingViewModel>();

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
          // Closed by the test on purpose, which is what the close-button test does.
        }
      }

      // The application owns the playback state, and in these tests that job is the fixture's.
      foreach (NowPlayingViewModel screen in _screens)
        screen.Dispose();

      _windows.Clear();
      _screens.Clear();
    }

    private static void Pump()
    {
      Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// Shows a playback screen over a player the test controls. The view model is built here the way the
    /// application builds it - with the dispatcher as marshaller - so that a test which hands the player an
    /// event is testing the same route the product uses.
    /// </summary>
    private ShellWindow ShowPlayback(FakePlayer player, out NowPlayingViewModel screen)
    {
      screen = new NowPlayingViewModel(player, action => Dispatcher.UIThread.Post(action));
      _screens.Add(screen);

      var window = new ShellWindow(new NowPlayingView(screen));
      window.Show();
      _windows.Add(window);
      Pump();
      return window;
    }

    private static IEnumerable<string> VisibleTexts(Window window)
    {
      return window.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text ?? string.Empty);
    }

    private static Button ButtonNamed(Window window, string name)
    {
      return window.GetVisualDescendants().OfType<Button>().SingleOrDefault(button => button.Name == name)
          ?? throw new AssertionException("the screen has no button named \"" + name + "\"");
    }

    private static ProgressBar ProgressBarOn(Window window)
    {
      return window.GetVisualDescendants().OfType<ProgressBar>().SingleOrDefault()
          ?? throw new AssertionException("the screen has no progress bar");
    }

    private static void Press(Button button)
    {
      button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
      Pump();
    }

    /// <summary>
    /// Raises a key on the focused control the way Avalonia does, and reports whether anybody claimed it.
    /// </summary>
    private static bool PressKey(Control on, Key key)
    {
      var args = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = on };
      on.RaiseEvent(args);
      Pump();
      return args.Handled;
    }

    private static MediaLocation Film(string name)
    {
      return MediaLocation.FromFile(System.IO.Path.Combine(
          System.IO.Path.GetTempPath(), "mp2-playback-window", name));
    }

    [AvaloniaTest]
    public void TheScreenShowsWhatIsPlayingAndWhatThePlayerIsDoing()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);

      Assert.That(VisibleTexts(window), Does.Contain("Nothing playing"),
          "a screen that has never played anything says so instead of showing a blank");

      Assert.That(screen.Play(Film("the-big-film.mkv")), Is.True, screen.StatusLine);
      Pump();

      IEnumerable<string> texts = VisibleTexts(window);
      Assert.That(texts, Does.Contain("the-big-film.mkv"), "the name of the media is on screen");
      Assert.That(texts.Any(text => text.Contains("Playing")), Is.True,
          "what the player is doing is on screen too");
    }

    [AvaloniaTest]
    public void TheBarAndTheClockFollowWhatTheEngineReported()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      screen.Play(Film("film.mkv"));
      Pump();

      player.RaisePosition(45_000, 90_000);
      Pump();

      ProgressBar bar = ProgressBarOn(window);
      Assert.That(bar.Value, Is.EqualTo(0.5).Within(0.001), "half of the media played, half the bar filled");
      Assert.That(VisibleTexts(window).Any(text => text.Contains("0:45") && text.Contains("1:30")), Is.True,
          "the clock shows where the viewer is");
    }

    [AvaloniaTest]
    public void APositionFromTheEnginesThreadReachesTheBarOnTheScreensThread()
    {
      // The engine's events arrive on its own thread. This is that journey with the marshaller the application
      // uses: nothing on the screen may move while the engine's thread is the one running, and everything moves
      // once the screen's own queue has been worked through. The first half is the crash, not the second.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      screen.Play(Film("film.mkv"));
      Pump();

      player.RaiseStateFromAnotherThread(PlayerState.Paused);
      Assert.That(screen.IsPlaying, Is.True,
          "the screen did not change while a thread that is not its own was still in the middle of raising an event");
      Assert.That(screen.TransportLabel, Is.EqualTo("Pause"), "and its button still said what it did before");

      Pump();
      Assert.That(screen.IsPaused, Is.True, "the state moved once the screen ran its own queue");
      Assert.That(screen.TransportLabel, Is.EqualTo("Resume"), "and the screen says so now");

      player.RaisePosition(9_000, 60_000);
      Pump();
      Assert.That(ProgressBarOn(window).Value, Is.EqualTo(0.15).Within(0.001));
    }

    [AvaloniaTest]
    public void TheTransportButtonsReachThePlayer()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      screen.Play(Film("film.mkv"));
      Pump();

      Button pause = ButtonNamed(window, "pauseButton");
      Assert.That(pause.Content, Is.EqualTo("Pause"), "while it plays, the button offers to pause");

      Press(pause);
      Assert.That(player.Calls, Does.Contain("Pause"), "the pause button paused");
      Assert.That(pause.Content, Is.EqualTo("Resume"),
          "and the button on screen changed with it - which is the binding working, not just the view model");

      Press(pause);
      Assert.That(player.Calls, Does.Contain("Play"), "the same button plays again");

      Press(ButtonNamed(window, "volumeDownButton"));
      Assert.That(player.Volume, Is.EqualTo(95), "one press of the volume key is five percent");

      Press(ButtonNamed(window, "muteButton"));
      Assert.That(player.Muted, Is.True, "the mute button muted");

      // Where the film is before the jump keys are tried, because a transport that only works from the middle of
      // a media has never been tested where a viewer uses it.
      player.RaisePosition(40_000, 60_000);
      Pump();

      Press(ButtonNamed(window, "backButton"));
      Assert.That(player.TimeInMs, Is.EqualTo(10_000), "the back button went back half a minute");

      Press(ButtonNamed(window, "forwardButton"));
      Assert.That(player.TimeInMs, Is.EqualTo(40_000), "and the forward button came back again");

      // A jump never runs past the media: a viewer holding the key ends up at the end, not past it.
      Press(ButtonNamed(window, "forwardButton"));
      Assert.That(player.TimeInMs, Is.EqualTo(60_000), "forward stopped at the end of the media");

      Press(ButtonNamed(window, "stopButton"));
      Assert.That(player.Calls, Does.Contain("Stop"), "the stop button stopped");
      Assert.That(screen.IsPlaying, Is.False, "and the screen knows it");
    }

    [AvaloniaTest]
    public void TheLeftAndRightKeysClimbTheSkipLadderInsteadOfWalkingTheButtons()
    {
      // On the home screen the d-pad's left and right move between areas. Here the same keys belong to the
      // transport, and a viewer who presses right in the middle of a film must not watch the highlight move.
      // What they do is the reference's escalating ladder: the press names its step and waits, and the film
      // moves when the waiting runs out.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      screen.Play(Film("film.mkv"));
      Pump();
      player.RaisePosition(40_000, 60_000);
      Pump();

      Button pause = ButtonNamed(window, "pauseButton");
      Assert.That(pause.Focus(), Is.True, "the pause button takes the focus, so the ring has somewhere to go");
      Pump();

      Assert.That(PressKey(pause, Key.Right), Is.True, "the screen claimed the key");
      Assert.That(player.TimeInMs, Is.EqualTo(40_000), "the first press holds its step and waits - the ladder has not run out yet");
      Assert.That(pause.IsFocused, Is.True, "and the focus stayed where it was");
      screen.SkipStepElapsed();
      Assert.That(player.TimeInMs, Is.EqualTo(55_000), "the first rung of the reference ladder is fifteen seconds");

      Assert.That(PressKey(pause, Key.Left), Is.True, "the screen claimed the other key as well");
      screen.SkipStepElapsed();
      Assert.That(player.TimeInMs, Is.EqualTo(40_000), "turning around at the bottom lands back where the run started");

      // Up belongs to the ring, and the ring walks the reference's row: rewind first, play/pause next. From the
      // first button of the row it wraps to the last. A row of buttons that could only be walked in one
      // direction would leave a viewer stuck at the end of it. This media has no tracks to pick, so the sound
      // and subtitle buttons are hidden - and a hidden button is not an area, which is why the wrap still
      // lands on Close.
      Assert.That(PressKey(pause, Key.Up), Is.True, "the ring used the key");
      Assert.That(ButtonNamed(window, "backButton").IsFocused, Is.True,
          "up from play walks to the rewind button, which the reference puts first");
      Assert.That(PressKey(ButtonNamed(window, "backButton"), Key.Up), Is.True, "the ring used the key again");
      Assert.That(ButtonNamed(window, "closeButton").IsFocused, Is.True,
          "and from the first button of the row the focus wrapped to the last");
      Assert.That(player.TimeInMs, Is.EqualTo(40_000), "while the film did not move at all");
    }

    [AvaloniaTest]
    public void TheTrackButtonsAreWalkedAsSoonAsTheFilmHasTracks()
    {
      // The two pickers join the same ring rather than a second one, because a remote has one d-pad and a viewer
      // who presses up from Mute expects to keep walking the screen, not to be told the key does nothing.
      var player = new FakePlayer();
      player.Audio = new PlaybackTrack[]
      {
          new PlaybackTrack(TrackKind.Audio, 1, "Language (eng)", "eng"),
          new PlaybackTrack(TrackKind.Audio, 2, "Language (ger)", "ger")
      };
      player.CurrentAudioId = 1;
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      screen.Play(Film("film.mkv"));
      Pump();

      Button close = ButtonNamed(window, "closeButton");
      Assert.That(close.Focus(), Is.True);
      Pump();

      Assert.That(PressKey(close, Key.Down), Is.True, "the ring took the key past the end of the transport row");
      Assert.That(ButtonNamed(window, "audioButton").IsFocused, Is.True,
          "and lands on the sound picker, which the visible track row just put into the ring");

      Assert.That(PressKey(ButtonNamed(window, "audioButton"), Key.Down), Is.True);
      Assert.That(ButtonNamed(window, "subtitleButton").IsFocused, Is.True, "the next area is the subtitle picker");

      Assert.That(PressKey(ButtonNamed(window, "subtitleButton"), Key.Down), Is.True);
      Assert.That(ButtonNamed(window, "backButton").IsFocused, Is.True,
          "and one further still wraps to the first button, so the pickers are part of the same loop");
    }

    [AvaloniaTest]
    public void ClosingTheScreenLeavesTheFilmRunning()
    {
      // The playback belongs to the application. A viewer who goes back to the library while music plays
      // expects it to carry on playing, so nothing in closing the window may touch the player.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      screen.Play(Film("film.mkv"));
      Pump();
      bool closed = false;
      window.Closed += delegate { closed = true; };

      Press(ButtonNamed(window, "closeButton"));

      Assert.That(closed, Is.True, "the close button closed the screen");
      Assert.That(player.Calls, Has.None.EqualTo("Stop"), "nothing was stopped");
      Assert.That(player.Calls, Has.None.EqualTo("Pause"), "nothing was paused");
      Assert.That(player.Calls, Has.None.EqualTo("Dispose"), "the player was not released either");
      Assert.That(screen.IsPlaying, Is.True, "and the playback is still the playback");

      _windows.Remove(window);
    }

    [AvaloniaTest]
    public void AScreenBuiltByTheXamlLoaderSaysThereIsNothingPlaying()
    {
      // The parameterless constructor is the one Avalonia itself uses (the designer, and any code path that
      // builds a window by type). It has no player, and what it must do with that is say so.
      var window = new ShellWindow(new NowPlayingView());
      window.Show();
      _windows.Add(window);
      Pump();

      Assert.That(VisibleTexts(window), Does.Contain("Nothing playing"));
      Assert.That(ProgressBarOn(window).Value, Is.EqualTo(0));
    }
  }
}
