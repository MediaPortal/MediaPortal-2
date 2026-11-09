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
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Api;
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
    /// Work the queue while the clock runs, which is the only way to see the end of something the screen was
    /// told to ease into: a plate whose mark fades in answers a bare <see cref="Pump"/> with the opacity it
    /// started at, and a test that then expects the finished word is testing a stopwatch rather than a screen.
    /// </summary>
    private static void PumpFor(int milliseconds)
    {
      int until = Environment.TickCount + milliseconds;
      while (Environment.TickCount < until)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(10);
      }
    }

    /// <summary>
    /// Work the queue until the screen settled on an answer, or the patience ran out. Something the screen eases
    /// into finishes on the dispatcher's own clock, which in a headless test has nothing to do with the wall
    /// clock: a test that waits a fixed while can easily catch the middle of the journey and call it a failure.
    /// </summary>
    private static bool PumpUntil(Func<bool> settled, int milliseconds)
    {
      int until = Environment.TickCount + milliseconds;
      while (Environment.TickCount < until)
      {
        Dispatcher.UIThread.RunJobs();
        if (settled())
          return true;
        Thread.Sleep(10);
      }

      return settled();
    }

    /// <summary>
    /// A named control of the playback screen, searched inside that screen rather than in the whole window -
    /// see <see cref="ButtonNamed"/> for why the shell is stepped through first.
    /// </summary>
    private static T ControlNamed<T>(Avalonia.Visual root, string name) where T : Control
    {
      Avalonia.Visual screen = root is ShellWindow shell ? PlaybackView(shell) : root;
      return screen.GetVisualDescendants().OfType<T>().SingleOrDefault(control => control.Name == name)
          ?? throw new AssertionException(
              "the screen has no " + typeof(T).Name + " named \"" + name + "\"");
    }

    /// <summary>The chapter marks standing on the position line, in the window's own coordinates.</summary>
    private static List<Avalonia.Point> TicksOn(Window window, Avalonia.Visual screen)
    {
      return screen.GetVisualDescendants().OfType<Border>()
          .Where(border => border.Classes.Contains("tick"))
          .Select(border => TopLeftOf(border, window))
          .OrderBy(at => at.X)
          .ToList();
    }

    /**
     * Where a control stands on the window. A mark's own bounds are reckoned from the row it happens to be in,
     * so two rows side by side cannot be compared that way; what a viewer sees is the place on the screen.
     */
    private static Avalonia.Point TopLeftOf(Avalonia.Visual control, Avalonia.Visual window)
    {
      double x = 0;
      double y = 0;
      for (Avalonia.Visual? at = control; at is not null && !ReferenceEquals(at, window); at = at.GetVisualParent())
      {
        x += at.Bounds.X;
        y += at.Bounds.Y;
      }
      return new Avalonia.Point(x, y);
    }

    /// <summary>What a mark of the foot band is called - the word a viewer hears when a reader gets to it.</summary>
    private static string MarkName(Button plate)
    {
      return plate.GetValue(AutomationProperties.NameProperty) as string ?? string.Empty;
    }

    /// <summary>The word under a mark, which the screen shows while the mark has the focus.</summary>
    private static string TipOn(Control control)
    {
      return ToolTip.GetTip(control) as string ?? string.Empty;
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

    /// <summary>
    /// A mark on the playback screen. Searched inside that screen rather than in the whole window, because the
    /// shell keeps other screens alive beside it and the television guide carries a button of the same name as
    /// one of the marks - asking the window for "backButton" would answer with the guide's.
    /// </summary>
    private static Button ButtonNamed(Avalonia.Visual root, string name)
    {
      Avalonia.Visual screen = root is ShellWindow shell ? PlaybackView(shell) : root;
      return screen.GetVisualDescendants().OfType<Button>().SingleOrDefault(button => button.Name == name)
          ?? throw new AssertionException("the screen has no button named \"" + name + "\"");
    }

    /// <summary>
    /// The dialog of the sound, walked mark by mark: it opens on its topmost mark, the keys down the column
    /// step from one mark to the next over the line of the level, and a key aimed past an edge leans the whole
    /// dialog against that edge.
    /// </summary>
    private static VolumePicker VolumeDialogOn(ShellWindow window)
    {
      // The shell keeps a dialog of the sound of its own for the compact transport row, so this asks the
      // playback screen for the one it raises itself.
      return PlaybackView(window).GetVisualDescendants().OfType<VolumePicker>().Single();
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
    /// Call the on-screen controls over, the way a person does: the Info key. The playback screen opens
    /// without them, like the reference's full screen, and tests that walk the buttons ask for them first.
    /// </summary>
    private static void OpenOsd(ShellWindow window)
    {
      Assert.That(PressKey(PlaybackView(window), Key.I), Is.True, "the playback screen answers the Info key");
      Pump();
    }

    /// <summary>
    /// The screen inside the window. Keys are raised where the viewer's remote would let them arrive: at a
    /// control inside the screen, so the route runs over the window down into the screen and back.
    /// </summary>
    private static NowPlayingView PlaybackView(ShellWindow window)
    {
      return window.GetVisualDescendants().OfType<NowPlayingView>().Single();
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

    /// <summary>
    /// Raises a key the way a windowing system that only knows the key under the button does it: no logical key
    /// at all, and the physical key of the button the remote reported.
    /// </summary>
    private static void PressPhysicalKey(Control on, PhysicalKey physical)
    {
      var args = new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.None,
        PhysicalKey = physical,
        Source = on
      };
      on.RaiseEvent(args);
      Pump();
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
      OpenOsd(window);

      Assert.That(VisibleTexts(window), Does.Contain("Nothing playing"),
          "a screen that has never played anything says so instead of showing a blank");

      Assert.That(screen.Play(Film("the-big-film.mkv")), Is.True, screen.StatusLine);
      Pump();

      IEnumerable<string> texts = VisibleTexts(window);
      Assert.That(texts, Does.Contain("the-big-film.mkv"), "the name of the media is on screen");

      // What the player is doing was spelled out as a word ("Playing") in the chrome's footer, and that footer
      // is empty now - the wide strip which painted the running title over every screen was taken down, and the
      // reference's stub at the foot of a navigation screen carries no letters either, only plates
      // (OtherControlsOverrides.xaml:1267-1323). The plate answers instead: a running film offers "Pause" on
      // it, a paused one offers "Resume", so the state is still readable from the screen.
      Assert.That(ButtonNamed(window, "pauseButton").GetValue(AutomationProperties.NameProperty),
          Is.EqualTo("Pause"),
          "what the player is doing is on screen too - the plate asking for the pause means the film is running");
    }

    [AvaloniaTest]
    public void TheBarAndTheClockFollowWhatTheEngineReported()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      OpenOsd(window);
      screen.Play(Film("film.mkv"));
      Pump();

      player.RaisePosition(45_000, 90_000);
      Pump();

      ProgressBar bar = ProgressBarOn(window);
      Assert.That(bar.Value, Is.EqualTo(0.5).Within(0.001), "half of the media played, half the bar filled");
      IEnumerable<string> texts = VisibleTexts(window);
      Assert.That(texts, Does.Contain("0:45"),
          "the number at the head of the position line says where in the film the viewer stands");
      Assert.That(texts, Does.Contain("1:30"),
          "and the number at its end says how long the film is, the two of them read at either end of the line "
          + "they belong to rather than in a caption above it");
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

      // The line of the film lives on the display, so the display is called over before the line is looked at.
      OpenOsd(window);
      player.RaisePosition(9_000, 60_000);
      Pump();
      Assert.That(ProgressBarOn(window).Value, Is.EqualTo(0.15).Within(0.001));
    }

    [AvaloniaTest]
    public void TheLineOfTheSoundAnswersTheHandWhereverItPoints()
    {
      // The line of the sound is pressed rather than clicked, and it lives inside a button - a button takes the
      // press it is given for its own click and marks it done with, so the line asks for the routed press with
      // the handled flag honoured. The keys above cannot tell whether that still holds, and a viewer holding a
      // mouse in front of the television has no keys: pointing at half the line means half the sound.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      window.Width = 1920;
      window.Height = 1080;
      Pump();
      screen.Play(Film("film.mkv"));
      Pump();
      OpenOsd(window);

      Press(ButtonNamed(window, "volumeButton"));
      Pump();

      Button volume = ButtonNamed(window, "volumeControl");
      var row = ControlNamed<Panel>(window, "volumeRow");
      Assert.That(row.Bounds.Height, Is.GreaterThan(8), "the line of the sound was never laid out");

      // Raised as the window hands a press over, and raised already marked done with: the control it lands in is
      // a button, and a button takes the press for its own click first. The line hears it anyway. The line stands
      // upright, so the foot of it is the press for nothing of the sound - aimed well past the line's own end,
      // because the point a press carried by hand brings in is read in the window's coordinates rather than the
      // line's, and a fraction of the sound that falls off either end of the line is clamped to that end.
      var press = new PointerPressedEventArgs(volume, null!, row,
          new Point(0, window.Bounds.Height + row.Bounds.Height), 0,
          new PointerPointProperties(), KeyModifiers.None)
      {
        RoutedEvent = InputElement.PointerPressedEvent,
        Handled = true
      };
      volume.RaiseEvent(press);
      Pump();
      Assert.That(player.Volume, Is.EqualTo(0),
          "a press the button had already taken for its own click never reached the line of the sound");

      // And which way an upright line runs: three quarters down it is a quarter of the sound, which a flat
      // reading of the same press would have answered with three quarters.
      var threeQuartersDown = new PointerPressedEventArgs(volume, null!, row,
          new Point(0, TopLeftOf(row, window).Y + row.Bounds.Height * 0.75), 0,
          new PointerPointProperties(), KeyModifiers.None)
      {
        RoutedEvent = InputElement.PointerPressedEvent,
        Handled = true
      };
      volume.RaiseEvent(threeQuartersDown);
      Pump();
      Assert.That(player.Volume, Is.EqualTo(25),
          "an upright line is read from its foot upward: three quarters down it is a quarter of the sound");

      // And the place of a press along the line is the sound itself: half the line is half the sound.
      PlaybackView(window).SetVolumeAtFraction(0.5);
      Pump();
      Assert.That(player.Volume, Is.EqualTo(50), "half the line asked for, and got");
      Assert.That(TextOn(window, "50 %"), Is.Not.Null, "and the number beside the line says the same");
      Assert.That(ControlNamed<Border>(window, "volumePlayed").Height, Is.EqualTo(row.Bounds.Height * 0.5).Within(1),
          "with the painted part of the line standing at half");
      Assert.That(ControlNamed<Border>(window, "volumeKnob").Margin.Bottom, Is.GreaterThan(0),
          "and its knob at the head of it");
    }

    [AvaloniaTest]
    public void TheTransportButtonsReachThePlayer()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      // A television's width, because the sound's dialog lays its line out across the room it is given: over a
      // headless window's default width the line would be a hand's breadth and its knob would have nowhere to go.
      window.Width = 1920;
      window.Height = 1080;
      Pump();
      screen.Play(Film("film.mkv"));
      Pump();
      // The controls are called over first, the way a viewer does it: while the display is away the screen has
      // no buttons to stand on, and a test that focuses one of them would be standing on nothing.
      OpenOsd(window);

      Button pause = ButtonNamed(window, "pauseButton");
      Assert.That(MarkName(pause), Is.EqualTo("Pause"), "while it plays, the button offers to pause");

      Press(pause);
      Assert.That(player.Calls, Does.Contain("Pause"), "the pause button paused");
      Assert.That(MarkName(pause), Is.EqualTo("Resume"),
          "and the button on screen changed with it - which is the binding working, not just the view model");
      Press(pause);
      Assert.That(player.Calls, Does.Contain("Play"), "the same button plays again");
      Assert.That(MarkName(pause), Is.EqualTo("Pause"), "and the button offers to pause again");

      // The loudspeaker is a line of the sound rather than two marks: a hand presses it wherever it wants the
      // sound to be, and a remote standing on it steps it by the same five percent the room's own volume keys
      // move it. Its knob and its blue part are painted, so this looks at the number, the word beside it, and the
      // width the painting left behind.
      // The loudspeaker is a mark that asks for the sound rather than a line lying among the buttons: the level
      // lives in a dialog of its own, where its line, its number, its two steps and its mute stand together and
      // the keys along the line can only mean the sound.
      Press(ButtonNamed(window, "volumeButton"));
      Pump();
      Assert.That(ControlNamed<Control>(window, "volumeDialog").IsVisible, Is.True,
          "the loudspeaker raised the sound's dialog");

      Button volume = ButtonNamed(window, "volumeControl");
      Assert.That(PressKey(volume, Key.Left), Is.True, "the left key on the line belongs to the sound");
      Assert.That(player.Volume, Is.EqualTo(95), "and it took the sound down by the step the remote uses");
      Assert.That(TextOn(window, "95 %"), Is.Not.Null, "and the number beside the line says the same");

      double played = ControlNamed<Border>(window, "volumePlayed").Height;
      Assert.That(PressKey(volume, Key.Right), Is.True);
      Assert.That(player.Volume, Is.EqualTo(100), "the right key brought it back");
      Assert.That(ControlNamed<Border>(window, "volumePlayed").Height, Is.GreaterThan(played),
          "and the painted part of the line grew with the sound, which is how a viewer reads it from a sofa");
      Assert.That(ControlNamed<Border>(window, "volumeKnob").Margin.Bottom, Is.GreaterThan(0),
          "with its knob standing at the head of it");

      Press(ButtonNamed(window, "muteButton"));
      Assert.That(player.Muted, Is.True, "the loudspeaker button muted");
      Press(ButtonNamed(window, "muteButton"));
      Assert.That(player.Muted, Is.False, "and standing on it again gave the sound back");

      Press(ButtonNamed(window, "volumeDownButton"));
      Assert.That(player.Volume, Is.EqualTo(95), "the mark at the head of the line took one step off the sound");
      Press(ButtonNamed(window, "volumeUpButton"));
      Assert.That(PressKey(PlaybackView(window), Key.Escape), Is.True, "and Back sends the dialog away");
      Pump();
      Assert.That(ControlNamed<Control>(window, "volumeDialog").IsVisible, Is.False,
          "the sound went back under the loudspeaker");

      // Where the film is before the jump keys are tried, because a transport that only works from the middle of
      // a media has never been tested where a viewer uses it.
      player.RaisePosition(40_000, 60_000);
      Pump();

      // The jump buttons are the reference's instant skip: the viewer's saved answer, and by default a fifth
      // of the media - twelve seconds of this minute-long one, not the fixed half minute the transport once
      // carried.
      Press(ButtonNamed(window, "backButton"));
      Assert.That(player.TimeInMs, Is.EqualTo(28_000), "the back button jumped back the saved distance");

      Press(ButtonNamed(window, "forwardButton"));
      Assert.That(player.TimeInMs, Is.EqualTo(40_000), "and the forward button came back again");

      // A jump never runs past the media: a viewer holding the key ends up at the end, not past it.
      Press(ButtonNamed(window, "forwardButton"));
      Press(ButtonNamed(window, "forwardButton"));
      Assert.That(player.TimeInMs, Is.EqualTo(60_000), "forward stopped at the end of the media");

      // The remote's stop key and the bar's stop button answer the same question, and the key is still the
      // keyboard's way of it: the film ends outright and the screen leaves - here, where the window held
      // nothing but the player, that means the window itself goes.
      Assert.That(PressKey(PlaybackView(window), Key.MediaStop), Is.True, "the screen answers the stop key");
      Assert.That(player.Calls, Does.Contain("Stop"), "which stopped the film");
      Assert.That(screen.IsPlaying, Is.False, "and the screen knows it");
      Assert.That(window.IsVisible, Is.False, "and the screen left with it");
    }

    [AvaloniaTest]
    public void TheStopButtonEndsTheFilmAndLeavesTheScreen()
    {
      // Stop is a button on the bar as much as a key on the remote: the viewer working with a mouse asked for
      // the same door next to play/pause. It stops the film outright - no "Stopped" left to read - and leaves
      // the screen the way Back leaves it: onto whatever the window holds underneath, or - as here, where the
      // window held nothing but the player - closing the window.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      screen.Play(Film("film.mkv"));
      Pump();
      OpenOsd(window);

      Press(ButtonNamed(window, "stopButton"));
      Pump();

      Assert.That(player.Calls, Does.Contain("Stop"), "the button stopped the film");
      Assert.That(screen.IsPlaying, Is.False, "and the screen knows it");
      Assert.That(window.IsVisible, Is.False, "and the screen went with the film, not the film with the screen");
    }

    /// <summary>
    /// The same screen driven by a television's own remote rather than by a keyboard. What such a remote's button
    /// reports is not a guess: the kernel turns a CEC remote's buttons into ordinary input keys with its CEC
    /// keymap (drivers/media/rc/keymaps/rc-cec.c), and the names pressed below are the Avalonia keys of those
    /// input keys. That the screen and the window answer them is the whole of the first tier of the CEC plan -
    /// see docs/multiplatform/Hdmi-Cec-Plan.md.
    /// </summary>
    [AvaloniaTest]
    public void ATelevisionSOwnRemoteDrivesTheButtonsTheFilmAndTheSound()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      window.PlaybackOverride = screen;
      screen.Play(Film("film.mkv"));
      Pump();

      NowPlayingView view = PlaybackView(window);

      // The set's Info button, which the kernel reports as KEY_INFO and this code reads as MediaInfo. Whether the
      // buttons stood out before the press is not the question; that this one button turns them round is.
      bool stood = view.OsdVisible;
      Assert.That(PressKey(view, Key.MediaInfo), Is.True, "the screen answers the television's Info button");
      Assert.That(view.OsdVisible, Is.Not.EqualTo(stood), "and the buttons changed with it");
      Assert.That(PressKey(view, Key.MediaInfo), Is.True);
      Assert.That(view.OsdVisible, Is.EqualTo(stood), "and the same button found them as they were");

      // Its play key means the same as the mark on the bar, and it answers whatever the screen is showing,
      // because a remote carries that button even with the buttons away.
      Assert.That(PressKey(view, Key.Play), Is.True, "the remote's play key reached the screen");
      Assert.That(player.Calls, Does.Contain("Pause"), "and it paused the film");
      Assert.That(screen.IsPaused, Is.True, "which the screen knows");
      PressKey(view, Key.Play);
      Assert.That(player.Calls, Does.Contain("Play"), "and the same key played it again");

      // The louder, quieter and silence keys belong to the room rather than to the screen, so the window answers
      // them - over the film, without the viewer having to call the sound's dialog first.
      int was = player.Volume;
      PressKey(view, Key.VolumeDown);
      Assert.That(player.Volume, Is.EqualTo(was - screen.VolumeStep), "the room's quieter key took one step off");
      PressKey(view, Key.VolumeUp);
      Assert.That(player.Volume, Is.EqualTo(was), "and the louder key brought the sound back");

      PressKey(view, Key.VolumeMute);
      Assert.That(player.Muted, Is.True, "and the room's silence key took the sound away");
      PressKey(view, Key.VolumeMute);
      Assert.That(player.Muted, Is.False, "and gave it back");

      // Its stop button ends the film, which leaves the screen as the keyboard's stop key leaves it.
      Assert.That(PressKey(view, Key.MediaStop), Is.True, "and its stop button ends the film");
      Assert.That(player.Calls, Does.Contain("Stop"));
      Assert.That(window.IsVisible, Is.False, "and the screen went with it");
    }

    /// <summary>
    /// The same remote on a windowing system that does not know its buttons. Avalonia's X11 key table
    /// (src/Avalonia.X11/X11KeyTransform.cs) leaves the media and volume keys unmapped while its physical key
    /// table covers them, and an evdev device's scan code is the kernel's key code plus eight, so
    /// <c>KEY_PLAYPAUSE</c> (164) arrives as the physical key <c>MediaPlayPause</c> (0xAC) with no logical key
    /// beside it. The binding file names both spellings of those buttons, which is what keeps a remote working
    /// there - and where neither spelling arrives, as under X11 for the set's Info button, the viewer's own
    /// <c>keybindings.json</c> hands the button a key the front end does hear.
    /// </summary>
    [AvaloniaTest]
    public void ARemoteWhoseButtonsTheWindowingSystemDoesNotNameStillAnswers()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      window.PlaybackOverride = screen;
      screen.Play(Film("film.mkv"));
      Pump();

      NowPlayingView view = PlaybackView(window);

      PressPhysicalKey(view, PhysicalKey.MediaPlayPause);
      Assert.That(player.Calls, Does.Contain("Pause"), "the unnamed pause button paused the film");
      PressPhysicalKey(view, PhysicalKey.MediaPlayPause);
      Assert.That(player.Calls, Does.Contain("Play"), "and played it again");

      int was = player.Volume;
      PressPhysicalKey(view, PhysicalKey.AudioVolumeDown);
      Assert.That(player.Volume, Is.EqualTo(was - screen.VolumeStep), "the unnamed quieter key took one step off");
      PressPhysicalKey(view, PhysicalKey.AudioVolumeUp);
      Assert.That(player.Volume, Is.EqualTo(was), "and the louder key brought the sound back");
      PressPhysicalKey(view, PhysicalKey.AudioVolumeMute);
      Assert.That(player.Muted, Is.True, "and the unnamed silence key took the sound away");

      PressPhysicalKey(view, PhysicalKey.MediaStop);
      Assert.That(player.Calls, Does.Contain("Stop"), "and the unnamed stop button ended the film");
      Assert.That(screen.IsPlaying, Is.False, "and the screen knows it");
    }

    /// <summary>
    /// A screen that claims every key it is offered - which is what the home wall does, and what any screen may
    /// do - must still leave the room's sound to the room. The wall was exactly where this broke: the volume key
    /// was swallowed by the screen and the window never heard it, so a viewer standing in the wall could not turn
    /// the sound down. The window hears these keys with handled events too, which is how the reference's handler
    /// works (WindowMessageHandler.cs:298, asked of every key before any screen has spoken).
    /// </summary>
    [AvaloniaTest]
    public void TheRoomsSoundIsHeardEvenOverAScreenThatClaimsEveryKey()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      window.PlaybackOverride = screen;
      screen.Play(Film("film.mkv"));
      Pump();

      NowPlayingView view = PlaybackView(window);
      view.AddHandler(InputElement.KeyDownEvent,
          delegate (object? sender, KeyEventArgs args) { args.Handled = true; },
          RoutingStrategies.Bubble, true);

      int was = player.Volume;
      PressKey(view, Key.VolumeDown);
      Assert.That(player.Volume, Is.EqualTo(was - screen.VolumeStep),
          "the room answered the quieter key although the screen had claimed it");

      Assert.That(PressKey(view, Key.Back), Is.True, "the screen kept the back key it claimed");
      Assert.That(window.IsVisible, Is.True, "and the window is still standing behind it");
    }

    /// <summary>
    /// The keyboard's stand-ins for the television's louder and quieter buttons are the printable "+" and "-",
    /// so a hand that is writing a server address in a field keeps its characters: a claimed printable key is
    /// left alone while the caret stands in a text field.
    /// </summary>
    [AvaloniaTest]
    public void AHandThatIsTypingKeepsItsPlusAndMinusForTheText()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      window.PlaybackOverride = screen;
      screen.Play(Film("film.mkv"));
      Pump();

      var field = new TextBox();
      Panel host = PlaybackView(window).GetVisualDescendants().OfType<Panel>().First();
      host.Children.Add(field);
      Pump();
      field.Focus();
      Pump();

      // What a text field does with a printable key: it writes the character and marks the key used. Said here
      // rather than got from the field, because a headless field has no input method to write with and so claims
      // nothing on its own.
      field.AddHandler(InputElement.KeyDownEvent,
          delegate (object? sender, KeyEventArgs args) { args.Handled = true; },
          RoutingStrategies.Bubble, true);

      int was = player.Volume;
      PressKey(field, Key.OemPlus);
      Assert.That(player.Volume, Is.EqualTo(was),
          "the room did not turn the sound up on a hand that was writing");

      // The very same key, claimed by the very same screen, does move the sound the moment the caret is out of
      // the field - the guard is about writing, not about the screen underneath.
      // The caret leaves the field - to a mark beside it, since a focus scope keeps the field it was left with -
      // and a claimed key of the same kind belongs to the room again.
      var elsewhere = new Button { Content = "somewhere else" };
      host.Children.Add(elsewhere);
      Pump();
      elsewhere.Focus();
      Pump();
      PressKey(field, Key.OemMinus);
      Assert.That(player.Volume, Is.EqualTo(was - screen.VolumeStep),
          "and away from a field the claimed quieter key reached the room");
    }

    [AvaloniaTest]
    public void TheGrowthOfAKnobStaysInsideTheLineItStandsOn()
    {
      // The toolkit cuts a control's picture at the control's own bounds, so a knob that grows when its line is
      // stood on has to find that growth inside the control. It did not: stood on at either end of either line,
      // the knob was drawn as half a knob, which is a broken dial to anybody watching from the sofa.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      window.Width = 1920;
      window.Height = 1080;
      Pump();
      screen.Play(Film("film.mkv"));
      Pump();
      player.RaisePosition(30_000, 60_000);
      Pump();
      OpenOsd(window);
      Press(ButtonNamed(window, "volumeButton"));
      Pump();

      double growth = 1.0;
      foreach (Style style in PlaybackView(window).Styles.OfType<Style>())
      {
        string selector = style.Selector.ToString();
        if (!selector.Contains("Border.knob", StringComparison.Ordinal)
            || !selector.Contains(":focus", StringComparison.Ordinal))
          continue;

        foreach (Setter setter in style.Setters.OfType<Setter>())
        {
          if (setter.Property.Name == "RenderTransform" && setter.Value is ITransform transform)
            growth = Math.Max(growth, transform.Value.M11);
        }
      }

      Assert.That(growth, Is.GreaterThan(1.0), "a stood-on line grows its knob rather than framing itself");

      void KeepsItsGrowth(string rowName, string hostName, string knobName, Action<double> standTheKnobAt,
          bool upright)
      {
        Button host = ButtonNamed(window, hostName);
        Panel row = ControlNamed<Panel>(window, rowName);
        Border knob = ControlNamed<Border>(window, knobName);
        // the distance the line is measured along, and the distance the knob stands in from the control that
        // cuts: an upright line is measured downward from its head and its knob hangs from its foot
        double along = upright ? row.Bounds.Height : row.Bounds.Width;
        double reach = upright ? knob.Bounds.Height : knob.Bounds.Width;
        double hostAlong = upright ? host.Bounds.Height : host.Bounds.Width;
        double inset = (hostAlong - along) / 2.0;
        double over = reach * (growth - 1.0) / 2.0;

        foreach (double end in new[] { 0.0, 1.0 })
        {
          standTheKnobAt(end);
          Pump();
          double from = inset + (upright ? knob.Margin.Bottom : knob.Margin.Left);
          Assert.That(from - over, Is.GreaterThanOrEqualTo(0.0),
              "the knob of " + rowName + " keeps the growth it grows at the foot of its line inside " + hostName);
          Assert.That(from + reach + over, Is.LessThanOrEqualTo(hostAlong + 0.5),
              "and so does the knob standing at the head of " + rowName);
        }
      }

      KeepsItsGrowth("progressRow", "seekBar", "scrubKnob",
          fraction => player.RaisePosition((long)Math.Round(fraction * 60_000), 60_000), false);
      KeepsItsGrowth("volumeRow", "volumeControl", "volumeKnob",
          fraction => screen.SetVolume((int)Math.Round(fraction * 100)), true);
    }

    [AvaloniaTest]
    public void TheSoundStandsUprightAtTheRightHandSideAboveTheButtons()
    {
      // A volume is thought of as a height, and the dialog that carries it is stood where a hand reaching for
      // the loudspeaker mark already is: at the right hand side of the screen and above the row of buttons, so
      // that it never covers the transport a viewer is watching the film by.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      window.Width = 1920;
      window.Height = 1080;
      Pump();
      screen.Play(Film("film.mkv"));
      Pump();
      OpenOsd(window);
      Press(ButtonNamed(window, "volumeButton"));
      Pump();

      TextBlock number = TextOn(window, "100 %")!;
      Button up = ButtonNamed(window, "volumeUpButton");
      Button line = ButtonNamed(window, "volumeControl");
      Button down = ButtonNamed(window, "volumeDownButton");
      Button mute = ButtonNamed(window, "muteButton");
      Control host = ControlNamed<Control>(window, "volumeDialog");
      Control buttons = ControlNamed<Control>(window, "osdArea");

      double Top(Control control) => TopLeftOf(control, window).Y;
      double Bottom(Control control) => TopLeftOf(control, window).Y + control.Bounds.Height;

      Assert.That(number, Is.Not.Null, "the number of the sound is read at the head of the dialog");
      Assert.That(Top(number), Is.LessThan(Top(up)), "the number is read first");
      Assert.That(Bottom(up), Is.LessThanOrEqualTo(Top(line)), "then the mark that lifts the sound");
      Assert.That(Bottom(line), Is.LessThanOrEqualTo(Top(down)), "then the line a hand slides along");
      Assert.That(Bottom(down), Is.LessThanOrEqualTo(Top(mute)), "then the mark that lowers it");
      Assert.That(Bottom(mute), Is.LessThanOrEqualTo(Bottom(host)), "and last the mark that silences the film");

      Assert.That(line.Bounds.Height, Is.GreaterThan(line.Bounds.Width),
          "the line of the sound is turned upright rather than laid flat on its end");
      Assert.That(TopLeftOf(line, window).X + line.Bounds.Width / 2,
          Is.EqualTo(TopLeftOf(up, window).X + up.Bounds.Width / 2).Within(1),
          "and it stands on the same middle as the marks above and below it");

      Assert.That(TopLeftOf(host, window).X + host.Bounds.Width,
          Is.EqualTo(window.Bounds.Width - 48).Within(1), "the dialog stands at the right hand edge");
      Assert.That(Bottom(host), Is.LessThan(Top(buttons)),
          "and it is lifted above the row of buttons rather than standing over them");

      // And the level wears the film's own line colour: the part of the sound that is there is read in the same
      // white as the part of the film that is played, rather than in a colour of the dialog's own.
      var played = (ISolidColorBrush)ControlNamed<Border>(window, "volumePlayed").Background;
      var filmLine = (ISolidColorBrush)ProgressBarOn(window).Foreground;
      Assert.That(played.Color, Is.EqualTo(filmLine.Color),
          "the line of the sound wears the position line's white");
    }

    [AvaloniaTest]
    public void TheNodOfTheSoundDialogIsNotCutByTheBoxesItStandsIn()
    {
      // The dialog of the sound is drawn by a control whose own bounds are exactly the dialog's, and a
      // UserControl is a ContentControl, which hands its ClipToBounds default over as TRUE - the same trap that
      // squared the halo off round a focused player button. A dialog that cuts at its own bounds cannot lean
      // twenty-two points out of them to answer a key aimed past its edge, so nothing between the dialog and
      // the window may cut tighter than its deepest nod.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      window.Width = 1920;
      window.Height = 1080;
      Pump();
      screen.Play(Film("film.mkv"));
      Pump();
      OpenOsd(window);
      Press(ButtonNamed(window, "volumeButton"));
      Pump();

      Border dialog = ControlNamed<Border>(window, "volumeDialog");
      VolumePicker picker = VolumeDialogOn(window);
      Assert.That(picker.ClipToBounds, Is.False,
          "the control that draws the dialog does not cut it at the dialog's own bounds");

      // How much room each box that cuts leaves the dialog above and below it, measured while it still rests.
      var rooms = new List<(string Box, double Below, double Above)>();
      double at = TopLeftOf(dialog, window).Y;
      for (Avalonia.Visual visual = dialog; visual != null && visual is not Window;
           visual = visual.GetVisualParent())
      {
        if (visual is not Control box || !box.ClipToBounds)
          continue;

        double top = TopLeftOf(box, window).Y;
        rooms.Add((box.GetType().Name + (string.IsNullOrEmpty(box.Name) ? "" : "#" + box.Name),
            top + box.Bounds.Height - (at + dialog.Bounds.Height),
            at - top));
      }

      // The nod itself, in both directions.
      PressKey(ButtonNamed(window, "volumeUpButton"), Key.Up);
      double dive = Math.Abs(picker.VolumeBounceDiveY);
      PressKey(ButtonNamed(window, "muteButton"), Key.Down);
      dive = Math.Max(dive, Math.Abs(picker.VolumeBounceDiveY));

      Assert.That(dive, Is.GreaterThan(0), "the dialog nodded");
      Assert.That(rooms, Is.Not.Empty, "the dialog stands in boxes that could cut it");
      foreach ((string name, double below, double above) in rooms)
      {
        TestContext.Progress.WriteLine("ROOM " + name + " below=" + below + " above=" + above
            + " dive=" + dive);
        Assert.That(below, Is.GreaterThanOrEqualTo(dive),
            "the nod down is not cut at " + name + ", which cuts at its own bounds");
        Assert.That(above, Is.GreaterThanOrEqualTo(dive),
            "and neither is the nod up cut at " + name);
      }
    }

    [AvaloniaTest]
    public void TheDialogOfTheSoundIsWalkedByItsMarksAndNodsAtItsEdges()
    {
      // The dialog of the sound is an upright column of marks with the line of the level between two of them.
      // It rises standing on its topmost mark, one key down is the next mark rather than the line - which a
      // hand points at rather than stands on - and a key aimed past any of its four edges leans the whole
      // dialog against that edge, which is how the player's panel says the same thing.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      window.Width = 1920;
      window.Height = 1080;
      Pump();
      screen.Play(Film("film.mkv"));
      Pump();
      OpenOsd(window);
      Press(ButtonNamed(window, "volumeButton"));
      Pump();

      VolumePicker dialog = VolumeDialogOn(window);
      Button up = ButtonNamed(window, "volumeUpButton");
      Button line = ButtonNamed(window, "volumeControl");
      Button down = ButtonNamed(window, "volumeDownButton");
      Button mute = ButtonNamed(window, "muteButton");

      Assert.That(up.IsFocused, Is.True, "the dialog rises standing on the topmost of its marks");

      Assert.That(PressKey(up, Key.Down), Is.True, "the dialog answered the downward key");
      Assert.That(down.IsFocused, Is.True, "and the key stepped to the mark under the line");
      Assert.That(line.IsFocused, Is.False, "the line of the level is stepped over, not walked onto");

      Assert.That(PressKey(down, Key.Down), Is.True);
      Assert.That(mute.IsFocused, Is.True, "and one step further came to the mark that silences the film");

      Assert.That(PressKey(mute, Key.Down), Is.True);
      Assert.That(mute.IsFocused, Is.True, "past the foot of the dialog nothing stands to be walked to");
      Assert.That(dialog.VolumeBounceDiveY, Is.GreaterThan(0),
          "and the whole dialog nodded down against its bottom edge, ready to spring back");

      Assert.That(PressKey(mute, Key.Up), Is.True);
      Assert.That(down.IsFocused, Is.True, "and the way back climbs the marks again");

      Assert.That(up.Focus(), Is.True);
      Pump();
      Assert.That(PressKey(up, Key.Up), Is.True);
      Assert.That(up.IsFocused, Is.True, "over the head of the dialog nothing stands either");
      Assert.That(dialog.VolumeBounceDiveY, Is.LessThan(0), "and the dialog nodded up against its top edge");

      // Across a dialog that is one mark wide there is nothing to walk to in either direction.
      Assert.That(PressKey(up, Key.Right), Is.True);
      Assert.That(up.IsFocused, Is.True, "the key across the column left the mark it stood on");
      Assert.That(dialog.VolumeBounceDiveX, Is.GreaterThan(0), "and the dialog answered it by nodding right");

      Assert.That(PressKey(up, Key.Left), Is.True);
      Assert.That(up.IsFocused, Is.True);
      Assert.That(dialog.VolumeBounceDiveX, Is.LessThan(0), "and the same answer to the left");

      // A hand that was pointed at the line of the level still gets out of it with the keys, and the keys
      // across it still belong to the sound.
      Assert.That(line.Focus(), Is.True);
      Pump();
      int level = player.Volume;
      Assert.That(PressKey(line, Key.Left), Is.True);
      Assert.That(player.Volume, Is.EqualTo(level - 5),
          "standing on the line, the key across it moves the sound as it always did");
      Assert.That(PressKey(line, Key.Down), Is.True);
      Assert.That(down.IsFocused, Is.True, "and the key down reaches the nearest mark under the line");
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

      Assert.That(PressKey(PlaybackView(window), Key.Right), Is.True, "the screen claimed the key");
      Assert.That(player.TimeInMs, Is.EqualTo(40_000), "the first press holds its step and waits - the ladder has not run out yet");
      screen.SkipStepElapsed();
      Assert.That(player.TimeInMs, Is.EqualTo(55_000), "the first rung of the reference ladder is fifteen seconds");

      Assert.That(PressKey(PlaybackView(window), Key.Left), Is.True, "the screen claimed the other key as well");
      screen.SkipStepElapsed();
      Assert.That(player.TimeInMs, Is.EqualTo(40_000), "turning around at the bottom lands back where the run started");

      // Up belongs to the ring once the buttons are on screen - and to the instant skip while they are away,
      // exactly like the reference's IsOSDVisible conditions. The ring walks the panel in the order the controls
      // stand on it: the name of the film at one end of the top row, then the transport across its middle. This
      // media has no chapters and no tracks to pick, and nothing is known about it beyond the file - so the
      // chapter marks are not drawn and the name of the film is not a button a viewer would press for news. A
      // control that is not there is not an area, which is why walking back from the rewind mark wraps.
      OpenOsd(window);
      Button pause = ButtonNamed(window, "pauseButton");
      Assert.That(pause.Focus(), Is.True, "with the display open, the play button takes the focus");
      Pump();

      // Up and down walk the panel by the rows its controls stand in, rather than along the one-line ring the
      // horizontal keys still use. The play button wears the top row, so a key up asks for a row that is not
      // there: the whole panel nods against the top edge and the ring stays where it stands - the honest answer
      // that this is the top of the panel, not a sideways jump to a button that lies beside rather than above.
      Assert.That(PressKey(pause, Key.Up), Is.True, "the panel answered the upward key");
      Assert.That(pause.IsFocused, Is.True, "the ring stayed on play, for no row stands above the transport");
      Assert.That(PlaybackView(window).OsdBounceDiveY, Is.LessThan(0),
          "and the whole panel nodded up against the top edge, ready to spring back");
      Assert.That(ButtonNamed(window, "infoButton").IsEnabled, Is.False,
          "the name of this film is still no button, because a file nobody put in a library has nothing to say");

      // A key down from the transport steps onto the position line that stands directly under it.
      Assert.That(PressKey(pause, Key.Down), Is.True, "the panel used the downward key");
      Button line = ButtonNamed(window, "seekBar");
      Assert.That(line.IsFocused, Is.True, "down from the transport stands the viewer on the position line");

      // And back up climbs a row again - off the position line and onto the transport - without moving the film.
      // Which transport button it lands on depends on where the line's middle sits over the row, so only the row
      // change is asserted, not the exact button.
      Assert.That(PressKey(line, Key.Up), Is.True, "and up again climbs a row off the line");
      Assert.That(line.IsFocused, Is.False, "the caret left the position line");
      Assert.That(player.TimeInMs, Is.EqualTo(40_000), "while the film did not move at all");
    }

    [AvaloniaTest]
    public void TheTrackButtonsAreWalkedAsSoonAsTheFilmHasTracks()
    {
      // The two pickers join the same ring rather than a second one, because a remote has one d-pad and a viewer
      // who presses down from the transport expects to keep walking the screen, not to be told the key does
      // nothing. Nothing is known about this film beyond its tracks, so the name of the film stays a plain name.
      var player = new FakePlayer();
      player.Audio = new PlaybackTrack[]
      {
          new PlaybackTrack(TrackKind.Audio, 1, "Language (eng)", "eng"),
          new PlaybackTrack(TrackKind.Audio, 2, "Language (ger)", "ger")
      };
      player.CurrentAudioId = 1;
      player.Subtitles = new PlaybackTrack[]
      {
          new PlaybackTrack(TrackKind.Subtitle, 1, "Language (eng)", "eng")
      };
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      OpenOsd(window);
      screen.Play(Film("film.mkv"));
      // Give the line a length so it is drawn (a zero-length stream has no seek line) while leaving the playhead
      // at the very start, so the later "the film did not jump about" check still reads zero.
      player.RaisePosition(0, 60_000);
      Pump();

      Button subtitles = ButtonNamed(window, "subtitleButton");
      Assert.That(subtitles.Focus(), Is.True);
      Pump();

      // The pickers and the switches all stand in the one bottom row, so the key across the row walks them as the
      // ring always has - Right steps to the next, and the row wraps. (The key down is no longer this walk; the
      // vertical keys walk the rows now, tested at the end of this method.)
      Assert.That(PressKey(subtitles, Key.Right), Is.True, "the ring took the key along the bottom row");
      Assert.That(ButtonNamed(window, "audioButton").IsFocused, Is.True,
          "the subtitle picker is followed by the sound picker, which the visible track row just put into the ring");

      Assert.That(PressKey(ButtonNamed(window, "audioButton"), Key.Right), Is.True);
      Assert.That(ButtonNamed(window, "volumeButton").IsFocused, Is.True,
          "and past the pickers comes the loudspeaker, which every film has whether it has a choice of tracks or not");

      // The loudspeaker no longer carries its line of sound beside it on the row, so the keys across the row
      // belong to the ring everywhere along it. One key used to mean two things - the volume where the line
      // stood and the next button beside it - and that is exactly the conflict the sound's own dialog ended.
      Assert.That(PressKey(ButtonNamed(window, "volumeButton"), Key.Right), Is.True);
      Assert.That(ButtonNamed(window, "fullscreenButton").IsFocused, Is.True,
          "the row walks on past the loudspeaker to the switches of the machine");

      Assert.That(PressKey(ButtonNamed(window, "fullscreenButton"), Key.Right), Is.True);
      Assert.That(ButtonNamed(window, "settingsButton").IsFocused, Is.True,
          "which ends at the gear: how this machine plays is asked for where the rest of the player is");

      // The gear is the rightmost control on the row, so one further right asks past the edge: rather than wrap
      // into another row as the old one-line ring did, the whole panel nods against the right edge and the gear
      // keeps the ring - the same answer the top and bottom rows give a key pointed off their edge.
      Assert.That(PressKey(ButtonNamed(window, "settingsButton"), Key.Right), Is.True,
          "the panel answered the rightward key");
      Assert.That(ButtonNamed(window, "settingsButton").IsFocused, Is.True,
          "the ring stayed on the gear, for no control stands further right on the row");
      Assert.That(PlaybackView(window).OsdBounceDiveX, Is.GreaterThan(0),
          "and the whole panel nodded right against the right edge, ready to spring back");

      // The vertical keys walk the rows instead. A key down from this bottom row asks for a row that is not
      // there, so the whole panel nods against the bottom edge and the ring holds its place; a key up steps
      // onto the position line that stands directly above the pickers.
      Button audio = ButtonNamed(window, "audioButton");
      Assert.That(audio.Focus(), Is.True);
      Pump();
      Assert.That(PressKey(audio, Key.Down), Is.True, "the panel answered the downward key");
      Assert.That(audio.IsFocused, Is.True, "the ring stayed on the bottom row, for no row stands below it");
      Assert.That(PlaybackView(window).OsdBounceDiveY, Is.GreaterThan(0),
          "and the whole panel nodded down against the bottom edge, ready to spring back");

      Assert.That(PressKey(audio, Key.Up), Is.True, "and the upward key climbs one row");
      Assert.That(ButtonNamed(window, "seekBar").IsFocused, Is.True,
          "onto the position line that stands over the pickers");

      // And the sound itself, which is asked for by the loudspeaker and answered by a dialog the keys of their
      // own belong to. The dialog stands on its topmost mark when it rises: a viewer who came for the sound is
      // put where a press does something, not on the line that only shows the level.
      Press(ButtonNamed(window, "volumeButton"));
      Pump();
      Assert.That(ButtonNamed(window, "volumeUpButton").IsFocused, Is.True,
          "the loudspeaker raised its dialog and stood the viewer on its topmost mark");

      int before = player.Volume;
      Assert.That(PressKey(ButtonNamed(window, "volumeControl"), Key.Left), Is.True,
          "standing on the line, the key across it belongs to the sound");
      Assert.That(player.Volume, Is.EqualTo(before - 5),
          "and it moved the sound by the same step the remote's volume key moves it");
      Assert.That(player.TimeInMs, Is.EqualTo(0), "while the film did not jump about");
      Assert.That(PressKey(ButtonNamed(window, "volumeControl"), Key.Right), Is.True);
      Assert.That(player.Volume, Is.EqualTo(before), "and the key the other way brought it back");

      Press(ButtonNamed(window, "volumeDownButton"));
      Assert.That(player.Volume, Is.EqualTo(before - 5), "the mark at the head of the line is one step quieter");
      Press(ButtonNamed(window, "volumeUpButton"));
      Assert.That(player.Volume, Is.EqualTo(before), "and the mark at its end one step louder again");

      Press(ButtonNamed(window, "muteButton"));
      Assert.That(player.Muted, Is.True, "and the mute stands in the dialog beside the level it belongs to");
      Press(ButtonNamed(window, "muteButton"));
      Assert.That(player.Muted, Is.False, "and pressing it again gave the sound back");

      Assert.That(PressKey(PlaybackView(window), Key.Escape), Is.True, "and Back sends the dialog away");
      Pump();
      Assert.That(ControlNamed<Control>(window, "volumeDialog").IsVisible, Is.False,
          "the sound went back where it came from");
    }

    [AvaloniaTest]
    public void AFilmWithOneSoundAndOneSubtitleOffersOnlyTheSubtitleSwitch()
    {
      var player = new FakePlayer();
      player.Audio = new PlaybackTrack[] { new PlaybackTrack(TrackKind.Audio, 1, "Language (ger)", "ger") };
      player.CurrentAudioId = 1;
      player.Subtitles = new PlaybackTrack[] { new PlaybackTrack(TrackKind.Subtitle, 1, "Language (ger)", "ger") };
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      OpenOsd(window);
      screen.Play(Film("film.mkv"));
      Pump();

      // Both round marks of the last row are drawn as soon as a film is playing, which is what the mockup asks
      // for and what a viewer reading the row expects: the chevron on each of them promises a list, and the
      // question "which sound is this" is worth asking of a film that carries exactly one. What such a film
      // gets in the list is its own sound and nothing else, so there is nothing that could change behind the
      // viewer's back - which is the same safety the ring has always promised, reached a different way.
      Assert.That(ButtonNamed(window, "audioButton").IsVisible, Is.True,
          "the sound of a film can always be asked about, even the sound of a film that carries exactly one");

      Press(ButtonNamed(window, "audioButton"));
      StackPanel soundRows = ControlNamed<StackPanel>(window, "trackDialogRows");
      Assert.That(soundRows.Children, Has.Count.EqualTo(1),
          "and the list holds only the one sound the film already plays");
      Assert.That(player.CurrentAudioId, Is.EqualTo(1), "which is the sound still playing");
      Assert.That(PressKey(PlaybackView(window), Key.Escape), Is.True, "and Back closes the list again");
      Pump();

      Button loudspeaker = ButtonNamed(window, "volumeButton");
      Assert.That(loudspeaker.Focus(), Is.True);
      Pump();

      Assert.That(PressKey(loudspeaker, Key.Left), Is.True, "the ring took the key back off the loudspeaker");
      Assert.That(ButtonNamed(window, "audioButton").IsFocused, Is.True,
          "back from the loudspeaker is the round mark of the sound, which this film now carries on its ring");

      Assert.That(PressKey(ButtonNamed(window, "audioButton"), Key.Left), Is.True, "and the ring took it again");
      Assert.That(ButtonNamed(window, "subtitleButton").IsFocused, Is.True,
          "which is preceded by the subtitle switch, the two marks standing side by side as they do on the row");

      // The subtitle switch is the leftmost control of the row, so a further left asks past the edge and the whole
      // panel nods against the left edge rather than wrapping around to the row's far end.
      Assert.That(PressKey(ButtonNamed(window, "subtitleButton"), Key.Left), Is.True,
          "and one further left asks past the left-hand edge of the row");
      Assert.That(ButtonNamed(window, "subtitleButton").IsFocused, Is.True,
          "the ring stays on the subtitle switch, for nothing stands to its left on the row");
      Assert.That(PlaybackView(window).OsdBounceDiveX, Is.LessThan(0),
          "and the whole panel nods left against the left edge, ready to spring back");

      // Standing on it and pressing it raises the list rather than changing anything behind the viewer's back:
      // a subtitle that switches itself while a film plays is a subtitle the viewer did not choose.
      Press(ButtonNamed(window, "subtitleButton"));
      AssertDialogIsUp(window);

      Assert.That(player.CurrentSubtitleId, Is.EqualTo(-1),
          "and nothing was chosen, let alone switched, by opening it");

      // Back closes the list again and leaves the film exactly where it was.
      Assert.That(PressKey(PlaybackView(window), Key.Escape), Is.True, "the screen answered Back");
      Pump();
      Assert.That(ControlNamed<Border>(window, "trackDialog").IsVisible, Is.False,
          "and the list went away, back to the film");
    }

    private static void AssertDialogIsUp(ShellWindow window)
    {
      Border dialog = ControlNamed<Border>(window, "trackDialog");
      Assert.That(dialog.IsVisible, Is.True, "the button that points onward raised its list");
      StackPanel rows = ControlNamed<StackPanel>(window, "trackDialogRows");
      Assert.That(rows.Children, Has.Count.EqualTo(2),
          "the list holds the subtitle the film carries and the way out of subtitles, in that order");
      Button first = (Button)rows.Children[0];
      Assert.That((string?)first.Content, Does.Contain("playing"),
          "and the row the film is on says so, because a list that does not name what is current leaves the "
          + "viewer guessing what a choice would change");
    }

    [AvaloniaTest]
    public void TheMarksStandOnOneLineUnderOnePositionLine()
    {
      // "The buttons are not vertically aligned, and the bottom was left as it was" is not a thing a
      // screenshot answers. A film with everything a film can offer - two sounds, a subtitle, chapters, a
      // picture the engine described - is opened, the panel called over, and the three rows are measured: the
      // transport stands on one line, the choices on another, and the position line and its chapter marks have
      // the places they were given.
      var player = new FakePlayer();
      player.Audio = new PlaybackTrack[]
      {
          new PlaybackTrack(TrackKind.Audio, 1, "Language (eng)", "eng"),
          new PlaybackTrack(TrackKind.Audio, 2, "Language (ger)", "ger")
      };
      player.CurrentAudioId = 1;
      player.Subtitles = new PlaybackTrack[] { new PlaybackTrack(TrackKind.Subtitle, 1, "Language (ger)", "ger") };
      player.Chapters = new PlaybackChapter[]
      {
          new PlaybackChapter(0, "The beginning"),
          new PlaybackChapter(45_000, "The middle"),
          new PlaybackChapter(60_000, "The end")
      };
      player.Quality = new PlaybackQuality(3840, 2160, "hev1");
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      // Measuring a row needs a room the row fits in: the panel carries its rows at a television's width, and a
      // headless window's default one is not it.
      window.Width = 1920;
      window.Height = 1080;
      Pump();
      screen.Play(Film("film.mkv"));
      Pump();
      player.RaisePosition(30_000, 90_000);
      Pump();
      OpenOsd(window);
      NowPlayingView view = PlaybackView(window);

      var transport = new List<Button>();
      foreach (string name in new[]
      {
          "chapterBackButton", "backButton", "pauseButton", "forwardButton", "chapterForwardButton"
      })
      {
        Button mark = ButtonNamed(window, name);
        Assert.That(mark.IsVisible, Is.True, "'" + name + "' is there to be used on a film like this");
        transport.Add(mark);
      }

      var choices = new List<Control>();
      foreach (string name in new[] { "subtitleButton", "audioButton", "volumeButton", "fullscreenButton", "settingsButton" })
        choices.Add(ButtonNamed(window, name));

      Assert.That(transport[1].Bounds.Width, Is.GreaterThan(0), "the panel was laid out");

      // A button's own bounds are reckoned from the row it happens to stand in, so what a viewer judges - the
      // place the button lands on the screen - is compared in the window's coordinates. And what a viewer calls
      // "not aligned" when the middle of a row is bigger than its ends is the middle of the buttons: the play
      // mark is drawn larger than the jumps beside it on purpose, and a row that lines their tops up would hang
      // the big one below the line.
      double transportLine = -1;
      foreach (Button mark in transport)
      {
        Avalonia.Point at = TopLeftOf(mark, window);
        double middle = at.Y + mark.Bounds.Height / 2;
        if (transportLine < 0)
          transportLine = middle;

        Assert.That(middle, Is.EqualTo(transportLine).Within(0.5),
            "'" + mark.Name + "' is centred on the same line as its neighbours: its middle is at " + middle +
            ", the first one's at " + transportLine);
      }

      Assert.That(transport[1].Bounds.Height, Is.EqualTo(transport[0].Bounds.Height).Within(0.5),
          "and the transport is one measure of button throughout - the play mark is picked out by what stands "
          + "behind it, not by a size of its own that would have to be centred against the others");

      double choiceLine = -1;
      foreach (Control choice in choices)
      {
        Avalonia.Point at = TopLeftOf(choice, window);
        double middle = at.Y + choice.Bounds.Height / 2;
        if (choiceLine < 0)
          choiceLine = middle;

        Assert.That(middle, Is.EqualTo(choiceLine).Within(0.5),
            "'" + choice.Name + "' is centred on the same line as the other choices on the bottom row");
      }

      // The rows over one another: the film's name, then the transport beside it, then the position line, then
      // the choices. Nothing that belongs under the line creeps above it.
      double line = TopLeftOf(ProgressBarOn(window), window).Y;
      Assert.That(transportLine, Is.LessThan(line), "the transport is above the position line");
      Assert.That(choiceLine, Is.GreaterThan(line), "and the choices are under it");

      Assert.That(window.GetVisualDescendants().OfType<ProgressBar>().Count(), Is.EqualTo(1),
          "one film, one position line");

      // The two numbers of the film's length stand at either end of the line rather than in a caption over it,
      // and they are read level with the line they belong to.
      TextBlock position = TextOn(window, "0:30")!;
      TextBlock length = TextOn(window, "1:30")!;
      Assert.That(position, Is.Not.Null, "the number at the head of the line says where the viewer stands");
      Assert.That(length, Is.Not.Null, "and the number at its end says how long the film is");
      Assert.That(TopLeftOf(position, window).X,
          Is.LessThan(TopLeftOf(ProgressBarOn(window), window).X),
          "the first is read before the line begins");
      Assert.That(TopLeftOf(length, window).X,
          Is.GreaterThan(TopLeftOf(ProgressBarOn(window), window).X + ProgressBarOn(window).Bounds.Width),
          "and the second after it ends");

      // The chapters are the file's own, and they keep their places: the mark for the chapter that begins at
      // half of a film ninety seconds long stands at half of the line, whatever else the panel does. The
      // chapter that begins the film needs no mark - the beginning of the line is that mark - but a chapter
      // that begins two thirds along it does, however late it is.
      List<Avalonia.Point> ticks = TicksOn(window, view);
      double barLeft = TopLeftOf(ProgressBarOn(window), window).X;
      double barWidth = ProgressBarOn(window).Bounds.Width;
      Assert.That(ticks, Has.Count.EqualTo(2),
          "of three chapters the line carries two marks: the first chapter begins where the line begins");
      Assert.That(ticks[0].X - barLeft, Is.EqualTo(barWidth * 0.5).Within(barWidth * 0.05),
          "the chapter that begins at half the film stands at half the line");
      Assert.That(ticks[1].X - barLeft, Is.EqualTo(barWidth * 2 / 3).Within(barWidth * 0.05),
          "and the one two thirds along the film stands two thirds along the line");

      // A mark's place is its whole point, so it has to survive the panel being put away and called back: the
      // film goes on turning over while the buttons are gone, and marks whose places were only borrowed came
      // back standing at the start of the line.
      PressKey(view, Key.I);
      PumpFor(400);
      PressKey(view, Key.I);
      List<Avalonia.Point> again = TicksOn(window, view);
      Assert.That(again, Has.Count.EqualTo(ticks.Count), "the same marks are standing there again");
      for (int i = 0; i < ticks.Count; i++)
      {
        Assert.That(again[i].X, Is.EqualTo(ticks[i].X).Within(1),
            "mark " + i + " keeps the place the film gave it through a panel that closes and opens");
      }
    }

    [AvaloniaTest]
    public void TheNameReadsUnderTheLineAndTheFileIsNotWrittenOnTheFilm()
    {
      // The name of the film used to stand at the top with the file it was cut from under it and the picture's
      // size and codec spelled as two pills beside it. It reads at the bottom-left now, and the file's name and
      // the two pills are gone - the lettering above the transport is the name's picture form, and a name does
      // not have to be written three times over the film.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      window.Width = 1920;
      window.Height = 1080;
      Pump();
      Assert.That(screen.Play(Film("big_buck_bunny_4k.mkv"), Guid.NewGuid(), 0, "Big Buck Bunny"),
          Is.True, screen.StatusLine);
      Pump();
      OpenOsd(window);

      var title = TextOn(window, "Big Buck Bunny");
      Assert.That(title, Is.Not.Null,
          "a film picked out of the library is named by what the viewer called it, not by the path to it");
      Assert.That(TextOn(window, "big_buck_bunny_4k.mkv"), Is.Null,
          "the file the film is made of is no longer written under the name");

      var track = ProgressBarOn(window);
      Assert.That(TopLeftOf(title!, window).Y, Is.GreaterThan(TopLeftOf(track, window).Y),
          "the name reads under the position line now, at the foot of the panel");

      // The size and the codec of the picture are no longer spelled as pills, even once the engine has said them.
      player.Quality = new PlaybackQuality(3840, 2160, "hev1");
      screen.Play(Film("big_buck_bunny_4k.mkv"), Guid.NewGuid(), 0, "Big Buck Bunny");
      Pump();
      PumpFor(200);
      Assert.That(TextOn(window, "4K"), Is.Null, "the picture's size is no longer a pill on the panel");
      Assert.That(TextOn(window, "HEVC"), Is.Null, "and neither is the word for the codec it is cut in");
    }

    [AvaloniaTest]
    public void TheChaptersTheFileCarriesStandOnTheLineAndAreWalked()
    {
      // A chapter is not something this front end invents: the marks on the line and the two buttons that jump
      // between them are the positions the file itself carries, and a file without them gets neither - a jump
      // button with nowhere to jump to is a button that lies.
      var player = new FakePlayer();
      player.Chapters = new PlaybackChapter[]
      {
          new PlaybackChapter(0, "The beginning"),
          new PlaybackChapter(30_000, "The middle"),
          new PlaybackChapter(60_000, "The end")
      };
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      window.Width = 1280;
      window.Height = 720;
      Pump();
      screen.Play(Film("film.mkv"));
      Pump();
      player.RaisePosition(45_000, 90_000);
      Pump();
      OpenOsd(window);

      Assert.That(screen.HasChapters, Is.True, "a file that names its chapters says so on screen");
      Assert.That(ButtonNamed(window, "chapterBackButton").IsVisible, Is.True,
          "and the two marks that jump between them are drawn");
      Assert.That(ButtonNamed(window, "chapterForwardButton").IsVisible, Is.True);

      Press(ButtonNamed(window, "chapterBackButton"));
      Assert.That(player.TimeInMs, Is.EqualTo(30_000), "back landed on the chapter the film is standing in front of");

      player.RaisePosition(30_000, 90_000);
      Pump();
      Press(ButtonNamed(window, "chapterBackButton"));
      Assert.That(player.TimeInMs, Is.EqualTo(0),
          "and pressing it again at the start of a chapter goes back to the one before it, which is what a viewer "
          + "who has just watched the first seconds of a chapter means");

      player.RaisePosition(45_000, 90_000);
      Pump();
      Press(ButtonNamed(window, "chapterForwardButton"));
      Assert.That(player.TimeInMs, Is.EqualTo(60_000), "and forward goes to the next chapter the file names");

      // A file with no chapters of its own gets no marks that would jump to nothing. The panel shows only what
      // works - which is the rule the marks on the line and the two pickers of tracks already follow - and a
      // dimmed button is still a button in a row a viewer reads: "why are previous and next drawn when they have
      // no function" is the question, and taking them away is the answer. Everything the clip can actually do
      // keeps its place.
      var plain = new FakePlayer();
      ShellWindow plainWindow = ShowPlayback(plain, out NowPlayingViewModel plainScreen);
      plainScreen.Play(Film("short-clip.mp4"));
      Pump();
      plain.RaisePosition(10_000, 60_000);
      Pump();
      OpenOsd(plainWindow);

      Assert.That(plainScreen.HasChapters, Is.False, "a file that names no chapters is not asked to have any");
      Assert.That(ButtonNamed(plainWindow, "chapterBackButton").IsVisible, Is.False,
          "so the mark that would jump back into nothing is not drawn");
      Assert.That(ButtonNamed(plainWindow, "chapterForwardButton").IsVisible, Is.False,
          "and neither is the one that would jump on into nothing");

      foreach (string name in new[] { "backButton", "pauseButton", "forwardButton", "seekBar" })
      {
        Assert.That(ButtonNamed(plainWindow, name).IsVisible, Is.True,
            "'" + name + "' does something to this clip, so it is on the panel");
      }
    }

    private static TextBlock? TextOn(Window window, string text)
    {
      return window.GetVisualDescendants().OfType<TextBlock>()
          .FirstOrDefault(t => (t.Text ?? string.Empty) == text);
    }

    [AvaloniaTest]
    public void AScreenWithNothingPlayingDrawsNoMarksThatWouldDoNothing()
    {
      // A button a viewer can press for no reason is worse than no button: it promises a film that is not there.
      // With nothing loaded, the whole transport - the chapters, the jumps, play and pause - stays away, and so
      // do the pickers of tracks no film brought along, and the position line a viewer could press into nothing.
      // What is left are the things that mean something whatever is playing: the loudspeaker, the whole
      // television, and the gear that asks how this machine plays.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      OpenOsd(window);
      Pump();

      foreach (string name in new[]
      {
          "chapterBackButton", "backButton", "pauseButton", "forwardButton", "chapterForwardButton",
          "subtitleButton", "audioButton", "seekBar"
      })
      {
        Assert.That(ButtonNamed(window, name).IsVisible, Is.False, "'" + name + "' was drawn over no film at all");
      }

      Assert.That(ButtonNamed(window, "volumeButton").IsVisible, Is.True,
          "the sound of this machine is settable whether a film plays or not");
      Assert.That(ButtonNamed(window, "fullscreenButton").IsVisible, Is.True,
          "and so is the way to the whole television");
      Assert.That(ButtonNamed(window, "settingsButton").IsVisible, Is.True,
          "and the way to the answers about how the playback works");

      // The name of the film is the one line the panel always has an answer for, and its answer to nothing is
      // the plain words for it rather than a blank where a name should be.
      Assert.That(TextOn(window, "Nothing playing"), Is.Not.Null,
          "a screen that has never played anything says so instead of showing a blank");

      // And the line a viewer clicks belongs to a length nobody reported, so pressing it may not move a player.
      Assert.That(screen.SeekToFraction(0.9), Is.False, "a click on the line of an unknown media is refused");
      Assert.That(player.TimeInMs, Is.EqualTo(0), "and the player was not told anything");
    }

    [AvaloniaTest]
    public void ClickingTheProgressLineTakesTheFilmWhereTheLineWasPressed()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      screen.Play(Film("film.mkv"));
      Pump();
      player.RaisePosition(10_000, 100_000);
      Pump();

      Assert.That(screen.SeekToFraction(0.25), Is.True, "a quarter of a known media is a place one can go to");
      Assert.That(player.TimeInMs, Is.EqualTo(25_000), "and the film went there");

      Assert.That(screen.SeekToFraction(1.5), Is.True, "a press past the end is answered too");
      Assert.That(player.TimeInMs, Is.EqualTo(100_000), "and it lands at the end of the media, never past it");
    }

    [AvaloniaTest]
    public void ClosingTheScreenLeavesTheFilmRunning()
    {
      // The playback belongs to the application, not to the panel over it. There is no "close" mark on the
      // panel for the same reason: a viewer who wants the display away presses Back, and a viewer who wants the
      // film gone stops it or leaves the screen the way they came to it. Nothing that Back does may touch the
      // player, because a viewer who goes back to the library while music plays expects it to carry on.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      screen.Play(Film("film.mkv"));
      Pump();
      OpenOsd(window);

      Assert.That(PressKey(PlaybackView(window), Key.Escape), Is.True, "the screen answered Back");
      Assert.That(ControlNamed<Control>(window, "osdArea").IsVisible, Is.False,
          "and what it answered was to put the display away, leaving the film on the screen behind it");

      PressKey(PlaybackView(window), Key.I);
      Assert.That(ControlNamed<Control>(window, "osdArea").IsVisible, Is.True,
          "and asking for the display again brings it back over the same film");

      Assert.That(player.Calls, Has.None.EqualTo("Stop"), "nothing was stopped");
      Assert.That(player.Calls, Has.None.EqualTo("Pause"), "nothing was paused");
      Assert.That(player.Calls, Has.None.EqualTo("Dispose"), "the player was not released either");
      Assert.That(screen.IsPlaying, Is.True, "and the playback is still the playback");
    }

    [AvaloniaTest]
    public void AScreenBuiltByTheXamlLoaderSaysThereIsNothingPlaying()
    {
      // The parameterless constructor is the one Avalonia itself uses (the designer, and any code path that
      // builds a window by type). It has no player, and what it must do with that is say so - on the display,
      // which is where the screen is read from, and which a viewer calls over with the Info key as anywhere.
      var window = new ShellWindow(new NowPlayingView());
      window.Show();
      _windows.Add(window);
      Pump();
      OpenOsd(window);

      Assert.That(VisibleTexts(window), Does.Contain("Nothing playing"));
      // The line a viewer would press into nothing stays away altogether rather than standing there at nought,
      // so what this screen proves about the position line is that there is none to read.
      Assert.That(ButtonNamed(window, "seekBar").IsVisible, Is.False,
          "over no film at all there is no position line to press");
      Assert.That(window.GetVisualDescendants().OfType<ProgressBar>().Any(p => p.IsVisible), Is.False,
          "and no bar of it is drawn");
    }

    [AvaloniaTest]
    public void TheLineOfTheFilmIsStoodOnAndMovedByTheHand()
    {
      // The position line used to be a bar a mouse could stab at and a keyboard could not reach at all. It is a
      // control of its own now: it is stood on in the ring, it answers a press anywhere along itself, and it is
      // thick enough to be aimed at. A viewer who complained the line could not be moved means exactly this.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      window.Width = 1920;
      window.Height = 1080;
      Pump();
      screen.Play(Film("film.mkv"));
      Pump();
      player.RaisePosition(30_000, 90_000);
      Pump();
      OpenOsd(window);
      NowPlayingView view = PlaybackView(window);

      Button bar = ButtonNamed(window, "seekBar");
      Assert.That(bar.IsVisible, Is.True, "a film whose length is known carries a line to press");
      Assert.That(bar.Focus(), Is.True, "and the line is a control a viewer can stand on");
      Pump();
      Assert.That(bar.IsFocused, Is.True, "the ring stands on the position line");

      Panel line = ControlNamed<Panel>(window, "progressRow");
      Assert.That(line.Bounds.Height, Is.GreaterThanOrEqualTo(40),
          "the line is a place to aim at rather than a hair to miss");
      // The line fills the room between the two numbers of the film's length. A button keeps to the measure of
      // what is painted on it unless it is told to stretch, and a film's line that shrank to the breadth of its
      // own knob would be neither pressable nor readable.
      Assert.That(bar.Bounds.Width, Is.GreaterThan(600),
          "the line of the film reaches from one number of its length to the other");
      Assert.That(ControlNamed<ProgressBar>(window, "progressBar").MinHeight, Is.GreaterThanOrEqualTo(12),
          "and the bar drawn on it is thick enough to be read from a sofa");

      // A press anywhere on the line, raised already marked done with, because the line lies in a button and a
      // button takes the press it is given for its own click first. The line hears it anyway, and the place
      // along it is the place in the film.
      var press = new PointerPressedEventArgs(bar, null!, line, new Point(0, 0), 0,
          new PointerPointProperties(), KeyModifiers.None)
      {
        RoutedEvent = InputElement.PointerPressedEvent,
        Handled = true
      };
      bar.RaiseEvent(press);
      Pump();

      view.SeekAlongTheLine(0.75);
      Pump();
      Assert.That(player.TimeInMs, Is.EqualTo(67_500).Within(1_000),
          "three quarters along the line is three quarters into the film");

      // The engine answers a jump with the place it took the film to, and the line stands there with it.
      player.RaisePosition(67_500, 90_000);
      Pump();
      Assert.That(ControlNamed<ProgressBar>(window, "progressBar").Value, Is.EqualTo(0.75).Within(0.001),
          "and the bar stands there with it");
      Assert.That(ControlNamed<Border>(window, "scrubKnob").Margin.Left, Is.GreaterThan(0),
          "with the knob of the line at its end, so the place is seen and not only remembered");

      // The numbers of the film's length are read from the sofa too.
      Assert.That(TextOn(window, "1:07") ?? TextOn(window, "1:08"), Is.Not.Null,
          "and the number at the head of the line moved with the film");
    }

    [AvaloniaTest]
    public void TheMarkUnderTheHandLooksLikeTheMarkUnderTheRing()
    {
      // "Use a common focus effect for mouse and keyboard": the pointer used to paint a paler box than the ring
      // did, so moving the mouse off a button took its highlight away and the keyboard gave a different one back.
      // Both are one answer now, and the dotted rectangle the platform draws over them is switched off.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      screen.Play(Film("film.mkv"));
      Pump();
      OpenOsd(window);

      NowPlayingView view = PlaybackView(window);

      // No mouse can be pointed at a headless screen, so what is compared is the answer each of the two states
      // is written to give: every setter that paints a box for the pointer must have a twin that paints the same
      // box for the ring. The two are written as two rules rather than one because the pointer's half has to
      // carry the window's "the pointer is about" gate to outrank the worn skin's rule for every button - which
      // is exactly the arrangement the skin itself uses, and the reason a shorter selector used to lose.
      var unmatched = new List<string>();
      var ungated = new List<string>();
      List<string> selectors = view.Styles.OfType<Style>().Select(s => s.Selector.ToString()).ToList();
      var paintboxes = new HashSet<string>(new[]
      {
        TemplatedControl.BackgroundProperty.Name, TemplatedControl.BorderBrushProperty.Name,
        TemplatedControl.BorderThicknessProperty.Name
      });
      int boxes = 0;
      foreach (Style style in view.Styles.OfType<Style>())
      {
        string selector = style.Selector.ToString();
        bool hand = selector.Contains(":pointerover", StringComparison.Ordinal);
        bool ring = selector.Contains(":focus", StringComparison.Ordinal);
        if (!hand && !ring)
          continue;

        List<string> painted = style.Setters.OfType<Setter>()
            .Where(s => paintboxes.Contains(s.Property.Name))
            .Select(s => s.Property.Name + "=" + s.Value)
            .ToList();
        if (painted.Count == 0)
          continue;

        boxes += painted.Count;
        if (hand == ring)
          continue;

        // Its twin: the same rule without the pointer's gate, with the other state's word in it. The gate is
        // only ever on the pointer's half - a ring has to stay lit when the pointer is put away - so it is not
        // part of what the two have to agree on.
        string core = selector.Replace("Window:not(.pointer-resting) ", string.Empty);
        string twin = hand ? core.Replace(":pointerover", ":focus") : core.Replace(":focus", ":pointerover");
        Style? other = view.Styles.OfType<Style>().FirstOrDefault(s =>
            s.Selector.ToString().Replace("Window:not(.pointer-resting) ", string.Empty) == twin);
        List<string> there = other?.Setters.OfType<Setter>()
              .Where(s => paintboxes.Contains(s.Property.Name))
              .Select(s => s.Property.Name + "=" + s.Value)
              .ToList() ?? new List<string>();
        if (!there.SequenceEqual(painted))
          unmatched.Add(selector + " paints " + string.Join(",", painted) + " for " +
              (hand ? "the hand; the ring's rule (" + twin + ") paints " + string.Join(",", there) : "the ring alone"));

        if (hand && !selector.Contains("pointer-resting", StringComparison.Ordinal))
          ungated.Add(selector);
      }

      Assert.That(boxes, Is.GreaterThan(0),
          "the panel does paint a box under the states a viewer can put a button in");
      Assert.That(unmatched, Is.Empty,
          "the hand and the ring are answered by one and the same box:" + Environment.NewLine +
          string.Join(Environment.NewLine, unmatched));
      Assert.That(ungated, Is.Empty,
          "a button the put-away pointer was left on must not go on claiming somebody stands at it, so every" +
          " rule that dresses the pointer's state stands behind the window's own gate:" + Environment.NewLine +
          string.Join(Environment.NewLine, ungated));

      Button mark = ButtonNamed(window, "pauseButton");
      Assert.That(mark.Focus(), Is.True);
      Pump();
      Assert.That(mark.IsFocused, Is.True);
      Assert.That(mark.Background, Is.Not.Null, "the ring paints a box behind the button it stands on");
      Assert.That(mark.BorderBrush, Is.Not.Null, "and an edge round it");
      Assert.That(mark.BorderThickness.Left, Is.GreaterThan(0), "and the edge has a weight to be seen from a sofa");
      Assert.That(mark.FocusAdorner, Is.Null,
          "and there is no second effect: the box the button grows is the focus effect for mouse and keyboard alike");
    }

    [AvaloniaTest]
    public void TheInsideOfTheMarkWearsWhatTheMarkWears()
    {
      // Measured off the real screen and invisible to every test written before it: the theme dresses the
      // ContentPresenter INSIDE a button rather than the button itself, so its grey square - at rest and again
      // under a pointer - was painted over the glass this screen draws. The buttons measured 70x70 with a radius
      // of 35 and were seen as grey tiles with a small corner, and the ring only showed because the theme
      // paints nothing for that state. What the button wears therefore has to be what its inside paints.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      screen.Play(Film("film.mkv"));
      Pump();
      OpenOsd(window);

      Button mark = ButtonNamed(window, "fullscreenButton");
      ContentPresenter? inside = mark.GetVisualDescendants().OfType<ContentPresenter>()
          .FirstOrDefault(p => p.Name == "PART_ContentPresenter");
      Assert.That(inside, Is.Not.Null, "the button stands in its template, or nothing of it is drawn at all");

      Assert.That(mark.CornerRadius.TopLeft, Is.GreaterThan(30),
          "the mark is a circle by itself");
      Assert.That(inside!.CornerRadius.TopLeft, Is.EqualTo(mark.CornerRadius.TopLeft),
          "and the box its inside paints is round the same way - not the small corner the theme keeps for itself");
      Assert.That(inside.Background, Is.SameAs(mark.Background),
          "what the button wears is what its inside paints");

      mark.Focus();
      Pump();
      Assert.That(mark.IsFocused, Is.True);
      Assert.That(inside.Background, Is.SameAs(mark.Background),
          "and when the ring stands on it, the inside is lit with the button rather than left in the theme's grey");
      Assert.That(inside.BorderThickness, Is.EqualTo(mark.BorderThickness),
          "and the edge the ring puts round the button is drawn, not hidden behind an edge-less presenter");
    }

    [AvaloniaTest]
    public void TheLineOfTheFilmGrowsItsKnobRatherThanAFrame()
    {
      // A frame round a line that already reaches from one number of a film's length to the other is a frame
      // over the film, so the position line stopped drawing one. What says where a viewer stands on the line is
      // the knob of the playhead growing - the same word for a pointer and for the ring, and nothing else.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      window.Width = 1920;
      window.Height = 1080;
      Pump();
      screen.Play(Film("film.mkv"));
      Pump();
      player.RaisePosition(30_000, 90_000);
      Pump();
      OpenOsd(window);

      Button bar = ButtonNamed(window, "seekBar");
      Border knob = ControlNamed<Border>(window, "scrubKnob");
      double rest = knob.Width;
      Assert.That(rest, Is.GreaterThan(0), "the line stands with its knob on it before anything stands on it");

      var framed = new List<string>();
      var widened = new List<string>();
      double grown = 1.0;
      foreach (Style style in PlaybackView(window).Styles.OfType<Style>())
      {
        string selector = style.Selector.ToString();
        if (!selector.Contains("Button.seek", StringComparison.Ordinal)
            && !selector.Contains("Button.hit", StringComparison.Ordinal))
          continue;
        if (!(selector.Contains(":focus", StringComparison.Ordinal)
              || selector.Contains(":pointerover", StringComparison.Ordinal)))
          continue;

        foreach (Setter setter in style.Setters.OfType<Setter>())
        {
          string property = setter.Property.Name;
          if (property == "Background" || property == "BorderBrush" || property == "BorderThickness")
            framed.Add(selector + " paints " + property);
          else if (property == "Width" || property == "Height")
            widened.Add(selector + " takes " + property + " to " + setter.Value);
          else if (property == "RenderTransform" && setter.Value is ITransform transform)
            grown = Math.Max(grown, transform.Value.M11);
        }
      }

      Assert.That(framed, Is.Empty,
          "nothing dresses the line itself while a viewer stands on it; the only thing drawn is its knob:" +
          Environment.NewLine + string.Join(Environment.NewLine, framed));
      Assert.That(widened, Is.Empty,
          "and it grows out of its own picture rather than by becoming wider in the measure, because the " +
          "playhead is placed by that measure and would step aside while it grew:" +
          Environment.NewLine + string.Join(Environment.NewLine, widened));

      double restScale = ScaleOf(knob);
      double middle = knob.Bounds.X + knob.Bounds.Width / 2.0;
      Assert.That(bar.Focus(), Is.True, "the line is a control the ring can stand on");
      Pump();
      // The growth is eased, so it is watched for rather than read off the first frame after the ring landed.
      Assert.That(PumpUntil(() => ScaleOf(knob) > restScale, 800), Is.True,
          "the knob of the playhead grows out of its own picture when the ring or the hand is on the line");
      Assert.That(knob.Width, Is.EqualTo(rest), "standing on the line leaves the knob's own measure alone");
      Assert.That(knob.Bounds.X + knob.Bounds.Width / 2.0, Is.EqualTo(middle).Within(0.5),
          "and the playhead keeps the place it was painted at instead of hopping to it");
      Assert.That(bar.FocusAdorner, Is.Null, "and the platform draws no rectangle of its own round it");
    }

    /// <summary>How wide a control's own picture is drawn: one is the untouched measure.</summary>
    private static double ScaleOf(Control control)
    {
      return control.RenderTransform is ITransform transform ? transform.Value.M11 : 1.0;
    }

    [AvaloniaTest]
    public void TheNameOfTheFilmIsNotStoodOn()
    {
      // The name of what is playing is a name. It used to light up and grow a frame like the marks beside it,
      // which said "button in the row" about a line of text that no key of the row's belongs to.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      screen.Play(Film("film.mkv"));
      Pump();
      OpenOsd(window);

      Button title = ButtonNamed(window, "infoButton");
      Assert.That(title.Focusable, Is.False, "the ring cannot stand on the name of the film");
      Assert.That(title.Focus(), Is.False, "and trying to stand on it does nothing at all");
      Assert.That(title.BorderThickness.Left, Is.EqualTo(0), "and it wears no edge at rest");

      var dressed = new List<string>();
      foreach (Style style in PlaybackView(window).Styles.OfType<Style>())
      {
        string selector = style.Selector.ToString();
        if (!selector.Contains("Button.title", StringComparison.Ordinal)
            || !(selector.Contains(":focus", StringComparison.Ordinal)
                 || selector.Contains(":pointerover", StringComparison.Ordinal)
                 || selector.Contains(":pressed", StringComparison.Ordinal)))
          continue;

        foreach (Setter setter in style.Setters.OfType<Setter>())
          dressed.Add(selector + " paints " + setter.Property.Name);
      }

      Assert.That(dressed, Is.Empty,
          "the name of the film grows no box under a hand, a ring or a press:" + Environment.NewLine +
          string.Join(Environment.NewLine, dressed));
    }

    [AvaloniaTest]
    public void NoMarkOfThePanelCarriesAWordOverIt()
    {
      // A tooltip is a grey box that lands on top of the button it explains; measured off the real screen it
      // covered the left half of the mark a viewer was aiming at. Nothing on this screen wears a word over
      // itself any more - what a mark is called stands in its name, which a reader and a remote read without
      // covering anything up.
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      screen.Play(Film("film.mkv"));
      Pump();
      OpenOsd(window);

      var carrying = new List<string>();
      foreach (Control control in PlaybackView(window).GetVisualDescendants().OfType<Control>())
      {
        string tip = TipOn(control);
        if (tip.Length > 0)
          carrying.Add(((control as Button)?.Name ?? control.GetType().Name) + ": '" + tip + "'");
      }

      Assert.That(carrying, Is.Empty,
          "no control of the playback screen carries a word over itself:" + Environment.NewLine +
          string.Join(Environment.NewLine, carrying));

      Assert.That(MarkName(ButtonNamed(window, "backButton")), Does.StartWith("Rewind"),
          "and the jump still says how far it jumps - in the name it is read by, not in a box over it");
      Assert.That(MarkName(ButtonNamed(window, "forwardButton")), Does.StartWith("Forward"),
          "and so does the other one");
    }

    [AvaloniaTest]
    public void TheTwoMarksOfTheTracksSayWhichTrackTheyStandOn()
    {
      // A viewer choosing between two sounds wants to read what is on right now without opening a list for it,
      // and the round marks of the last version showed nothing but a shape and a chevron too small to see. Each
      // of them carries the name of its current track in its own body now, and is a capsule wide enough for it.
      var player = new FakePlayer();
      player.Audio = new PlaybackTrack[]
      {
          new PlaybackTrack(TrackKind.Audio, 1, "Language (eng)", "eng"),
          new PlaybackTrack(TrackKind.Audio, 2, "Language (ger)", "ger")
      };
      player.CurrentAudioId = 2;
      player.Subtitles = new PlaybackTrack[] { new PlaybackTrack(TrackKind.Subtitle, 1, "Language (ger)", "ger") };
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      window.Width = 1920;
      window.Height = 1080;
      Pump();
      screen.Play(Film("film.mkv"));
      Pump();
      OpenOsd(window);

      Assert.That(screen.AudioTrackName, Is.EqualTo("Language (ger)"),
          "the sound button wears the sound that is playing");
      Assert.That(screen.SubtitleTrackName, Is.EqualTo("OFF"),
          "and the subtitle button says it is showing nothing, which is what a viewer needs to know");

      Assert.That(TextOn(window, "Language (ger)"), Is.Not.Null, "and the name is read on the button itself");
      Assert.That(TextOn(window, "OFF"), Is.Not.Null, "and so is the way out of subtitles");

      Button sound = ButtonNamed(window, "audioButton");
      Button jump = ButtonNamed(window, "backButton");
      Assert.That(sound.Bounds.Width, Is.GreaterThan(jump.Bounds.Width * 2),
          "a mark that carries a name is a capsule rather than a round mark of the same width as the jumps");
      Assert.That(TextOn(window, "›"), Is.Not.Null, "and the chevron that promises the list is still on it");

      Press(sound);
      StackPanel rows = ControlNamed<StackPanel>(window, "trackDialogRows");
      Assert.That(rows.Children, Has.Count.EqualTo(2), "pressing it opens the list the chevron promised");
    }

    /// <summary>
    /// The list of tunes beside the transport, walked with the remote. The list belongs to the audio arrangement
    /// and is not part of the panel, so neither the shared ring nor the panel's row walker ever steps into it on
    /// its own: the transport hands the caret across at its right end, the choices at the foot's right hand hand
    /// it up, its rows are climbed up and down, and a key aimed at the wall beside it nods the list itself -
    /// which is the answer the panel gives too, and the only one that says the walk ended here rather than that
    /// nobody heard the key.
    /// </summary>
    [AvaloniaTest]
    public void TheRemoteWalksIntoTheListOfTunesAndBackOut()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      OpenOsd(window);

      List<TitleViewModel> shelf = new List<TitleViewModel>();
      for (int number = 1; number <= 3; number++)
      {
        shelf.Add(new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Tune " + number, null, 2024, null,
            "file:///D:/Musik/" + number.ToString("00") + ".mp3", false, Track: number)));
      }

      screen.Play(Film("the-long-night.mp3"));
      screen.PlayingAspect = "AudioItem";
      screen.LoadAudioQueue(shelf, 0, "AudioItem", "Nightglass");
      Pump();

      Border tunes = ControlNamed<Border>(window, "audioQueue");
      Assert.That(tunes.IsVisible, Is.True, "a record of three tunes drew no list beside the player");

      // The rows of the list, in the order they stand: the buttons its template realized. (The container the
      // list wraps a template in is not the row and cannot take the caret, so this asks for the buttons.)
      List<Button> Rows() => tunes.GetVisualDescendants().OfType<Button>().ToList();
      Assert.That(Rows(), Has.Count.EqualTo(3), "the list drew no row for every tune of the record");

      // From the row of buttons, the key across the row walks to its end and then into the list, standing the
      // viewer on the tune the room is hearing rather than on the list's head.
      Button endOfTransport = ButtonNamed(window, "repeatButton");
      Assert.That(endOfTransport.Focus(), Is.True);
      Pump();
      Assert.That(PressKey(endOfTransport, Key.Right), Is.True, "the transport answered the rightward key");
      Assert.That(tunes.IsKeyboardFocusWithin, Is.True,
          "past the last button of the transport the remote steps into the list of tunes");
      Assert.That(Rows()[0].IsFocused, Is.True, "the caret stands on the tune that is playing");

      Assert.That(PressKey(Rows()[0], Key.Down), Is.True, "the list answered the downward key");
      Assert.That(Rows()[1].IsFocused, Is.True, "down climbs one tune");
      Assert.That(PressKey(Rows()[1], Key.Up), Is.True);
      Assert.That(Rows()[0].IsFocused, Is.True, "up climbs back to the tune it came from");

      // Beyond its own top and its own side the list nods rather than wraps: the caret holds and the list says out
      // loud that the walk ends here. Above the list the screen holds nothing, and beside it nothing stands either.
      // Its bottom end is different and nods nothing - the choices along the foot stand under it, and the test for
      // that door is further down, after the walk has climbed the record by the key itself.
      Assert.That(PressKey(Rows()[0], Key.Up), Is.True, "the list answered the key at its top end");
      Assert.That(Rows()[0].IsFocused, Is.True, "the caret stayed on the tune it was standing on");
      Assert.That(PlaybackView(window).TunesBounceDiveY, Is.LessThan(0),
          "and the list nodded up against its own top edge, ready to spring back");

      Assert.That(PressKey(Rows()[0], Key.Down), Is.True);
      Assert.That(Rows()[1].IsFocused, Is.True);
      Assert.That(PressKey(Rows()[1], Key.Down), Is.True);
      Assert.That(Rows()[2].IsFocused, Is.True, "the key walks on to the last tune of the record");

      Assert.That(PressKey(Rows()[2], Key.Right), Is.True, "the list answered the key at its right edge");
      Assert.That(Rows()[2].IsFocused, Is.True, "and the caret held there");
      Assert.That(PlaybackView(window).TunesBounceDiveX, Is.GreaterThan(0),
          "while it nodded right, the direction in which a list of tunes has nothing beside it");

      // Down at the bottom end is not an end: the choices along the foot's right hand stand under the list, so the
      // key steps onto whichever of them the row left stands most over. That is the same door the upward key walks
      // into the list from, and the list owes its viewer a step down as much as a step up.
      Assert.That(PressKey(Rows()[2], Key.Down), Is.True, "the list answered the key at its bottom end");
      Assert.That(tunes.IsKeyboardFocusWithin, Is.False,
          "and the caret left the list, because the screen has something under it to stand on");
      Assert.That(ButtonNamed(window, "volumeButton").IsFocused, Is.True,
          "the caret stands on the loudspeaker, the choice that stands under the row it left");
      Assert.That(PlaybackView(window).TunesBounceDiveY, Is.Not.GreaterThan(0),
          "and the list nodded no down at its bottom end");

      Assert.That(PressKey(ButtonNamed(window, "volumeButton"), Key.Up), Is.True,
          "the choice answered the upward key");
      Assert.That(tunes.IsKeyboardFocusWithin, Is.True,
          "and the same road back stands the caret on a tune again");

      // The choices along the foot's right hand stand under the list, so the key up from them steps into it too
      // rather than onto the position line under nothing.
      Button gear = ButtonNamed(window, "settingsButton");
      Assert.That(gear.Focus(), Is.True);
      Pump();
      Assert.That(PressKey(gear, Key.Up), Is.True, "the foot's right hand answered the upward key");
      Assert.That(tunes.IsKeyboardFocusWithin, Is.True,
          "one key up from the choices at the foot's right hand stands in the list of tunes");

      // And the way back is the key the list has nothing on its other side of: left steps onto the transport.
      Assert.That(PressKey(tunes, Key.Left), Is.True, "the list answered the leftward key");
      Assert.That(ButtonNamed(window, "repeatButton").IsFocused, Is.True,
          "the list hands the caret back to the row of buttons it stands beside");
    }

    /// <summary>
    /// The list of tunes is a window that walks with the player, and a window that walks takes its rows away
    /// under the viewer's feet. Two things are owed here and neither is the other: a repaint that empties the
    /// list and refills it may not drop the caret onto the window - a viewer pressing keys then sends the window
    /// away instead of moving a row - and the caret that survives has to be stood somewhere that still means
    /// what it meant. The rule is the viewer first: the tune they were standing on if the walking window still
    /// shows it, and the tune that has just started if it did not.
    /// </summary>
    [AvaloniaTest]
    public void TheCaretHoldsATuneWhileTheListOfTunesWalksOn()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      OpenOsd(window);

      // Sixteen tunes, which is more than any panel ever draws, so every step the record takes walks the window
      // on and drops the tune it came from off the head of the list.
      List<TitleViewModel> shelf = new List<TitleViewModel>();
      for (int number = 1; number <= 16; number++)
      {
        shelf.Add(new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Tune " + number, null, 2024, null,
            "file:///D:/Musik/" + number.ToString("00") + ".mp3", false, Track: number)));
      }

      // The tune the player opened has to be the tune the queue stands on, or the end of it is an end nobody
      // waited for and the record stays where it is.
      screen.Play(Film("the-long-night.mp3"), shelf[0].Title.Id, 0, "Nightglass");
      screen.PlayingAspect = "AudioItem";
      screen.LoadAudioQueue(shelf, 0, "AudioItem", "Nightglass");
      Pump();

      Border tunes = ControlNamed<Border>(window, "audioQueue");
      List<Button> Rows() => tunes.GetVisualDescendants().OfType<Button>().ToList();
      Button Row(string name) => Rows().First(row =>
          row.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == name));
      Button? UnderCaret() => Rows().FirstOrDefault(row => row.IsFocused);

      // The caret walks in from the row of buttons onto the tune that plays, as the walk test above already
      // proves, and from there climbs two tunes down the list.
      Assert.That(ButtonNamed(window, "repeatButton").Focus(), Is.True);
      Pump();
      Assert.That(PressKey(ButtonNamed(window, "repeatButton"), Key.Right), Is.True);
      Pump();
      Assert.That(Row("Tune 1").IsFocused, Is.True, "the caret walked in onto the tune that plays");
      Assert.That(PressKey(Row("Tune 1"), Key.Down), Is.True);
      Assert.That(PressKey(Row("Tune 2"), Key.Down), Is.True);
      Pump();
      Assert.That(Row("Tune 3").IsFocused, Is.True, "the caret stands two tunes below the head of the list");

      // The first tune runs out. The record walks on, the panel is repainted, and the caret - standing on a
      // tune the new window still shows - keeps that very tune.
      player.RaiseEndOfMedia();
      Pump();
      Assert.That(screen.AudioQueuePosition, Is.EqualTo("2 / 16"), "the record did not walk on to its second tune");
      Assert.That(tunes.IsKeyboardFocusWithin, Is.True,
          "repainting the list of tunes dropped the caret onto the window");
      Assert.That(UnderCaret(), Is.SameAs(Row("Tune 3")),
          "the caret left the tune it was standing on, which the walked window still shows");

      // Now onto the head of the window. Up there is not the head of the record - the window is a window - so the
      // key rolls it: the tune before the window comes in at the top and the caret stands on that tune.
      Assert.That(PressKey(Row("Tune 3"), Key.Up), Is.True);
      Pump();
      Assert.That(Row("Tune 2").IsFocused, Is.True, "the caret climbed to the head of the window");
      Assert.That(PressKey(Row("Tune 2"), Key.Up), Is.True, "the key at the head of the window was dropped");
      Pump();
      Assert.That(Row("Tune 1").IsFocused, Is.True,
          "up at the head of the window rolled the tune before it in and stood the caret on it");

      // Let the record walk once more with the window rolled. The tune that plays is still inside the rolled
      // window, so the window keeps the place somebody browsed it to - and the caret keeps its own tune instead
      // of being dragged back to the player.
      player.RaiseEndOfMedia();
      Pump();
      Assert.That(screen.AudioQueuePosition, Is.EqualTo("3 / 16"), "the record did not walk on again");
      Assert.That(Rows().Any(row => row.IsFocused), Is.True, "the caret fell out of the list over the repaint");
      Assert.That(UnderCaret(), Is.SameAs(Row("Tune 1")),
          "a rolled window snapped back to the player and took the caret with it");
    }

    /// <summary>
    /// A row of the list writes three things in three columns: the number the library gave the tune on its record,
    /// the tune's own name, and - once the library has answered it - its minutes. The number stands in a column of
    /// its own rather than in front of the name, because the name is what a viewer reads and the library's own name
    /// has that number cut out of it; and the column is a width rather than a minimum because every row lays its
    /// own grid, so a column measured row by row would slide the names of the rows with fewer digits sideways.
    /// </summary>
    [AvaloniaTest]
    public void TheRowsOfTheListOfTunesWriteTheirNumbersBesideTheirNames()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      OpenOsd(window);

      List<TitleViewModel> shelf = new List<TitleViewModel>();
      for (int number = 1; number <= 3; number++)
      {
        shelf.Add(new TitleViewModel(new MediaTitle(Guid.NewGuid(),
            "Nightglass: " + number + " - Tune " + number, null, 2024, null,
            "file:///D:/Musik/" + number.ToString("00") + ".mp3", false, Track: number)));
      }

      screen.Play(Film("the-long-night.mp3"), shelf[0].Title.Id, 0, "Nightglass");
      screen.PlayingAspect = "AudioItem";
      screen.LoadAudioQueue(shelf, 0, "AudioItem", "Nightglass");
      Pump();

      Border tunes = ControlNamed<Border>(window, "audioQueue");
      List<Button> rows = tunes.GetVisualDescendants().OfType<Button>().ToList();
      Assert.That(rows, Has.Count.EqualTo(3), "the list drew no row for every tune of the record");

      List<string> Words(Button row) => row.GetVisualDescendants().OfType<TextBlock>()
          .Select(text => text.Text ?? string.Empty)
          .ToList();

      // Number, name, minutes - in that order, and the record's name gone out of the name. Nobody asked the
      // library for its minutes here, so the third column stands empty rather than at "0:00".
      Assert.That(Words(rows[0]), Is.EqualTo(new[] { "1", "Tune 1", string.Empty }),
          "the first row does not write its number, its tune and its minutes");
      Assert.That(Words(rows[2]), Is.EqualTo(new[] { "3", "Tune 3", string.Empty }),
          "the last row does not write its number, its tune and its minutes");

      // The names of all three rows begin at the same line of the screen, whatever their numbers weigh.
      double namesAt = Words(rows[0]).Count == 3
          ? rows[0].GetVisualDescendants().OfType<TextBlock>().ElementAt(1).Bounds.X
          : 0;
      Assert.That(namesAt, Is.GreaterThan(0), "the name column was never laid out");
      foreach (Button row in rows)
      {
        Assert.That(Words(row), Has.Count.EqualTo(3), "a row of the list writes fewer than three things");
        Assert.That(row.GetVisualDescendants().OfType<TextBlock>().ElementAt(1).Bounds.X, Is.EqualTo(namesAt),
            "the number column is measured per row and slides the names of the rows sideways");
      }
    }

    /// <summary>
    /// A record long enough to number its tunes with three digits, because a number column is only ever measured
    /// against the widest number that has ever stood in it. What the yardstick is: a row's number may be as wide as
    /// it wants, since a number that does not fit is cut sideways and reads as a smudge - and a viewer cannot see
    /// the difference between a cut number and a whole one in a photograph, so this is the place where it is
    /// measured rather than looked at.
    /// </summary>
    [AvaloniaTest]
    public void ANumberColumnDoesNotCutTheWidestNumberARecordCanWrite()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      OpenOsd(window);

      const int TUNES = 120;
      const int PRESSED = 101;
      List<TitleViewModel> shelf = new List<TitleViewModel>();
      for (int number = 1; number <= TUNES; number++)
      {
        shelf.Add(new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Tune " + number, null, 2024, null,
            "file:///D:/Musik/" + number + ".mp3", false, Track: number)));
      }

      screen.Play(Film("the-long-night.mp3"), shelf[PRESSED].Title.Id, 0, "Nightglass");
      screen.PlayingAspect = "AudioItem";
      screen.LoadAudioQueue(shelf, PRESSED, "AudioItem", "Nightglass");
      Pump();

      Border tunes = ControlNamed<Border>(window, "audioQueue");
      List<Button> rows = tunes.GetVisualDescendants().OfType<Button>().ToList();
      Assert.That(rows, Is.Not.Empty, "the panel drew no rows to measure");

      foreach (Button row in rows)
      {
        TextBlock digits = row.GetVisualDescendants().OfType<TextBlock>().First();
        Assert.That(int.TryParse(digits.Text, out int written) && written >= 100, Is.True,
            "the window of a record standing at tune " + (PRESSED + 1) + " does not draw three digit numbers");

        // Asked for its own width with no room to give, a word reports what it would wear; the width it was laid
        // out in is what it may wear. Cut the difference away and the number is standing cut in the frame.
        digits.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double wanted = digits.DesiredSize.Width - digits.Margin.Left - digits.Margin.Right;
        Assert.That(digits.Bounds.Width, Is.GreaterThanOrEqualTo(wanted - 0.5),
            "the number column cuts \"" + digits.Text + "\": it wants " + wanted.ToString("0.#")
            + " lines and stands in " + digits.Bounds.Width.ToString("0.#"));
      }
    }

    /// <summary>
    /// More tunes than the panel has rows for, walked with the remote from top to bottom. The rows beside the
    /// equalizer are a window into the record rather than the record itself, so the key that reaches the bottom of
    /// the window rolls it on - which is the only way a viewer of a twenty tune record on a twelve row panel ever
    /// hears its thirteenth tune - and the caret goes with the row it is standing on. Only under the last tune of
    /// the record is the bottom of the list its end, and there the step goes on to the choices standing under it.
    /// </summary>
    [AvaloniaTest]
    public void TheListOfTunesRollsToTunesItDoesNotDrawYet()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      OpenOsd(window);

      const int TUNES = 20;
      List<TitleViewModel> shelf = new List<TitleViewModel>();
      for (int number = 1; number <= TUNES; number++)
      {
        shelf.Add(new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Tune " + number, null, 2024, null,
            "file:///D:/Musik/" + number.ToString("00") + ".mp3", false, Track: number)));
      }

      screen.Play(Film("the-long-night.mp3"), shelf[0].Title.Id, 0, "Nightglass");
      screen.PlayingAspect = "AudioItem";
      screen.LoadAudioQueue(shelf, 0, "AudioItem", "Nightglass");
      Pump();

      Border tunes = ControlNamed<Border>(window, "audioQueue");
      List<Button> Rows() => tunes.GetVisualDescendants().OfType<Button>().ToList();
      string TuneOf(Button row) => row.GetVisualDescendants().OfType<TextBlock>()
          .Select(text => text.Text ?? string.Empty)
          .First(text => text.StartsWith("Tune ", StringComparison.Ordinal));

      int drawn = Rows().Count;
      Assert.That(drawn, Is.GreaterThan(1), "the panel drew no rows to walk");
      Assert.That(drawn, Is.LessThan(TUNES),
          "the panel drew a whole record longer than its rows, so this frame tested no rolling");

      // The caret walks in from the row of buttons onto the tune that plays, which is the head of the window.
      Button repeat = ButtonNamed(window, "repeatButton");
      Assert.That(repeat.Focus(), Is.True);
      Pump();
      Assert.That(PressKey(repeat, Key.Right), Is.True, "the transport answered the rightward key");
      Pump();
      Assert.That(tunes.IsKeyboardFocusWithin, Is.True, "the caret did not step into the list of tunes");
      Assert.That(TuneOf(Rows()[0]), Is.EqualTo("Tune 1"), "the window does not lead with the tune that plays");
      Assert.That(TuneOf(Rows()[drawn - 1]), Is.EqualTo("Tune " + drawn),
          "the window does not end where the room the screen has ends");

      // Down along the rows and then off the bottom of them: the record runs further than the window does, so the
      // window rolls and the caret stands on the tune that came in.
      for (int climbed = 1; climbed < drawn; climbed++)
        Assert.That(PressKey(Rows()[climbed - 1], Key.Down), Is.True, "the list answered the downward key");
      Pump();
      Assert.That(Rows()[drawn - 1].IsFocused, Is.True, "the caret is at the bottom of the window");

      Assert.That(PressKey(Rows()[drawn - 1], Key.Down), Is.True, "the key at the bottom of the window was dropped");
      Pump();
      Assert.That(tunes.IsKeyboardFocusWithin, Is.True,
          "the caret left the list although the record has tunes behind its window");
      Assert.That(TuneOf(Rows()[drawn - 1]), Is.EqualTo("Tune " + (drawn + 1)),
          "the window rolled no tune in at its bottom end");
      Assert.That(Rows()[drawn - 1].IsFocused, Is.True, "the caret stayed on the row it stood on instead of the tune");

      // On to the end of the record, one key at a time. Nothing is played by walking the list - pointing at a tune
      // is not pressing it - and the caret is never let go on the way.
      for (int reached = drawn + 2; reached <= TUNES; reached++)
      {
        Assert.That(PressKey(Rows()[drawn - 1], Key.Down), Is.True, "the list dropped a key on the way to the end");
        Pump();
        Assert.That(tunes.IsKeyboardFocusWithin, Is.True,
            "the caret left the list on a tune that is not the record's last one");
        Assert.That(TuneOf(Rows()[drawn - 1]), Is.EqualTo("Tune " + reached),
            "rolling lost the thread of the record");
      }

      // Under the last tune of the record the list really is the last thing on the screen above the choices along
      // its foot's right hand, and the key steps onto them.
      Assert.That(PressKey(Rows()[drawn - 1], Key.Down), Is.True, "the last tune of the record dropped the key");
      Pump();
      Assert.That(tunes.IsKeyboardFocusWithin, Is.False,
          "the caret held in the list under the last tune of the record, with the foot's choices standing under it");
      Assert.That(ButtonNamed(window, "volumeButton").IsFocused, Is.True,
          "the caret stands on the loudspeaker, the choice that stands under the row it left");
      Assert.That(screen.AudioQueuePosition, Is.EqualTo("1 / " + TUNES),
          "walking the list of tunes started playing the record");

      // And the way back in. The tune that plays outranks wherever browsing left the window, so stepping up out
      // of the foot's choices calls the window back to the player and stands the caret on the tune the room hears.
      Assert.That(PressKey(ButtonNamed(window, "volumeButton"), Key.Up), Is.True);
      Pump();
      Assert.That(tunes.IsKeyboardFocusWithin, Is.True, "up from the foot's choices stepped back into the list");
      Assert.That(TuneOf(Rows()[0]), Is.EqualTo("Tune 1"),
          "walking back into the list left the window rolled away from the tune that plays");
      Assert.That(Rows()[0].IsFocused, Is.True, "the caret did not come back onto the tune that plays");

      // Above the first tune of the record there is nothing left to roll in, so the key is answered by nodding the
      // list against its own beginning instead of moving it.
      Assert.That(PressKey(Rows()[0], Key.Up), Is.True, "the key above the first tune of the record was dropped");
      Pump();
      Assert.That(Rows()[0].IsFocused, Is.True, "the caret left the tune it stood on at the record's beginning");
      Assert.That(PlaybackView(window).TunesBounceDiveY, Is.LessThan(0),
          "and the list nodded up against its own beginning, ready to spring back");
    }

    /// <summary>
    /// The wheel over the list of tunes, which is the second way into a record longer than its panel. What a turn
    /// costs depends on where the caret stands, and the two cases are kept apart on purpose: a caret standing in
    /// the list is walked by the wheel the way the arrow keys walk it - so that it never ends up on a tune the
    /// window rolled away from - while a caret standing out by the loudspeaker is left there, because reading
    /// through a record with the wheel is nobody's business out there. Neither turn plays anything: turning is
    /// looking, and only a press on a row plays its tune.
    /// </summary>
    [AvaloniaTest]
    public void TheWheelOverTheListOfTunesTurnsItsWindow()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      OpenOsd(window);

      const int TUNES = 20;
      List<TitleViewModel> shelf = new List<TitleViewModel>();
      for (int number = 1; number <= TUNES; number++)
      {
        shelf.Add(new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Tune " + number, null, 2024, null,
            "file:///D:/Musik/" + number.ToString("00") + ".mp3", false, Track: number)));
      }

      screen.Play(Film("the-long-night.mp3"), shelf[0].Title.Id, 0, "Nightglass");
      screen.PlayingAspect = "AudioItem";
      screen.LoadAudioQueue(shelf, 0, "AudioItem", "Nightglass");
      Pump();

      Border tunes = ControlNamed<Border>(window, "audioQueue");
      List<Button> Rows() => tunes.GetVisualDescendants().OfType<Button>().ToList();
      string TuneOf(Button row) => row.GetVisualDescendants().OfType<TextBlock>()
          .Select(text => text.Text ?? string.Empty)
          .First(text => text.StartsWith("Tune ", StringComparison.Ordinal));

      void Wheel(double notches)
      {
        tunes.RaiseEvent(new PointerWheelEventArgs(tunes, null!, tunes, new Point(0, 0), 0,
            new PointerPointProperties(), KeyModifiers.None, new Vector(0, notches))
        {
          RoutedEvent = InputElement.PointerWheelChangedEvent
        });
      }

      int drawn = Rows().Count;
      Assert.That(drawn, Is.GreaterThan(1), "the panel drew no rows to turn");
      Assert.That(drawn, Is.LessThan(TUNES), "the panel drew a whole record longer than its rows");
      Assert.That(TuneOf(Rows()[0]), Is.EqualTo("Tune 1"), "the window does not lead with the tune that plays");

      // (a) The caret stands out by the loudspeaker. A turn of the wheel rolls a later tune into the window and
      //      leaves the caret exactly where it stood.
      Button speaker = ButtonNamed(window, "volumeButton");
      Assert.That(speaker.Focus(), Is.True);
      Pump();
      Wheel(-1);
      Pump();
      Assert.That(TuneOf(Rows()[drawn - 1]), Is.EqualTo("Tune " + (drawn + 1)),
          "the wheel rolled no tune of the record into the window");
      Assert.That(tunes.IsKeyboardFocusWithin, Is.False, "the wheel dragged the caret into the list of tunes");
      Assert.That(speaker.IsFocused, Is.True, "the wheel took the caret off the choice it stood on");
      Assert.That(screen.AudioQueuePosition, Is.EqualTo("1 / " + TUNES),
          "turning the wheel started playing the record");

      // A turn the other way takes the window back, and the tune that plays comes into view again.
      Wheel(1);
      Pump();
      Assert.That(TuneOf(Rows()[drawn - 1]), Is.EqualTo("Tune " + drawn),
          "the wheel turned the window back no tune");

      // (b) The caret walks into the list, over the last button of the transport, and stands on the tune that
      //      plays: the head of the window and of the record both.
      Button repeat = ButtonNamed(window, "repeatButton");
      Assert.That(repeat.Focus(), Is.True);
      Pump();
      Assert.That(PressKey(repeat, Key.Right), Is.True, "the transport answered the rightward key");
      Pump();
      Assert.That(tunes.IsKeyboardFocusWithin, Is.True, "the caret did not step into the list of tunes");
      Assert.That(Rows()[0].IsFocused, Is.True, "the caret did not stand on the tune that plays");

      //      Above the first tune of the record there is nothing to roll in, so a turn there moves nothing - the
      //      same answer the upward key gives with a nod of its own.
      Wheel(1);
      Pump();
      Assert.That(TuneOf(Rows()[0]), Is.EqualTo("Tune 1"),
          "a turn above the first tune of the record rolled the window off the beginning of the record");
      Assert.That(Rows()[0].IsFocused, Is.True, "a turn at the record's beginning let go of the caret");

      //      Down along the rows, and off the bottom of them: the caret walks with the wheel, and where the window
      //      runs out it rolls - the caret standing on the tune that came in at the end the wheel was turned to.
      for (int climbed = 1; climbed < drawn; climbed++)
        Wheel(-1);
      Pump();
      Assert.That(Rows()[drawn - 1].IsFocused, Is.True, "the caret did not walk down with the wheel");
      Wheel(-1);
      Pump();
      Assert.That(tunes.IsKeyboardFocusWithin, Is.True,
          "the wheel left the list although the record has tunes behind its window");
      Assert.That(TuneOf(Rows()[drawn - 1]), Is.EqualTo("Tune " + (drawn + 1)),
          "the wheel rolled the window without carrying the caret along");
      Assert.That(Rows()[drawn - 1].IsFocused, Is.True, "the caret was left on a row the window rolled away");
      Assert.That(screen.AudioQueuePosition, Is.EqualTo("1 / " + TUNES),
          "walking the list with the wheel started playing the record");

      //      And back up: the window rolls earlier tunes in and the caret climbs with it.
      Wheel(1);
      Pump();
      Assert.That(TuneOf(Rows()[drawn - 2]), Is.EqualTo("Tune " + drawn),
          "the wheel climbed the caret no tune up the list");
      Assert.That(Rows()[drawn - 2].IsFocused, Is.True, "the caret did not climb with the wheel");
    }

    /// <summary>
    /// A record with more tunes than the screen has room for rows. The list stands with its foot a fixed distance
    /// above the floor, so on a short window the rows that would fall over the top of the screen are left out and
    /// are a key away down the list instead - while the head of the panel goes on saying how many tunes the record
    /// really has, because a small window does not make a short record.
    /// </summary>
    [AvaloniaTest]
    public void ALongRecordDoesNotDrawItsListOfTunesPastTheTopOfTheScreen()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);

      List<TitleViewModel> shelf = new List<TitleViewModel>();
      for (int number = 1; number <= 12; number++)
      {
        shelf.Add(new TitleViewModel(new MediaTitle(Guid.NewGuid(),
            "Nightglass: " + number + " - Tune " + number, null, 2024, null,
            "file:///D:/Musik/" + number.ToString("00") + ".mp3", false, Track: number)));
      }

      screen.Play(Film("the-long-night.mp3"), Guid.NewGuid(), 0, "Nightglass");
      screen.PlayingAspect = "AudioItem";
      screen.LoadAudioQueue(shelf, 5, "AudioItem", "Nightglass");
      Pump();

      Border tunes = ControlNamed<Border>(window, "audioQueue");
      Assert.That(tunes.IsVisible, Is.True, "a record of twelve tunes drew no list beside the player");
      Assert.That(tunes.Bounds.Height, Is.LessThanOrEqualTo(window.Bounds.Height),
          "the list of tunes is taller than the screen it stands on");
      Assert.That(TopLeftOf(tunes, window).Y, Is.GreaterThanOrEqualTo(0),
          "the head of the list of tunes hangs over the top of the screen");
      Assert.That(tunes.GetVisualDescendants().OfType<Button>(), Is.Not.Empty,
          "and the window it shrank to still shows the tune that is playing");
      Assert.That(screen.AudioQueuePosition, Is.EqualTo("6 / 12"),
          "the panel told the viewer the record was shorter than the record is");
    }

    /// <summary>
    /// The picture the stage of a song stands on, asked again for the tune after the first one. Opening a tune
    /// wipes every picture the library had said - the next tune may come from another record - so the screen's
    /// every-change question has to include this picture and not only the cover, the lettering and the record.
    /// Asked once when the screen opened, the second tune of a record played on a bare shade.
    /// </summary>
    [AvaloniaTest]
    public void TheStageGetsItsPictureAgainForTheTuneAfterTheFirstOne()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);

      List<Guid> asked = new List<Guid>();
      screen.FanartReader = delegate (Guid mediaItemId)
      {
        asked.Add(mediaItemId);
        return Task.FromResult<byte[]?>(new byte[] { 1, 2, 3 });
      };

      Guid first = Guid.NewGuid();
      Guid second = Guid.NewGuid();

      screen.Play(Film("the-first-song.mp3"), first, 0, "Nightglass");
      screen.PlayingAspect = "AudioItem";
      Pump();
      Assert.That(asked, Is.EqualTo(new[] { first }), "the stage of a song was never asked for its picture");

      screen.Play(Film("the-second-song.mp3"), second, 0, "Nightglass");
      screen.PlayingAspect = "AudioItem";
      Pump();
      Assert.That(asked, Is.EqualTo(new[] { first, second }),
          "the tune after the first one was left standing on a stage without its picture");
    }

    /// <summary>
    /// The tune the room is hearing stands on a white plate with its words in black, which is the strongest mark
    /// this list has: every other row is a hole cut in the shade of the panel. Only the tunes wear it - the film's
    /// pickers of sound and subtitles are dressed by the same rows and keep their own lit row.
    /// </summary>
    [AvaloniaTest]
    public void TheTuneThatPlaysStandsOnAWhitePlateInBlackWords()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      OpenOsd(window);

      List<TitleViewModel> shelf = new List<TitleViewModel>();
      for (int number = 1; number <= 12; number++)
      {
        shelf.Add(new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Tune " + number, null, 2024, null,
            "file:///D:/Musik/" + number.ToString("00") + ".mp3", false, Track: number)));
      }

      screen.Play(Film("the-long-night.mp3"));
      screen.PlayingAspect = "AudioItem";
      screen.LoadAudioQueue(shelf, 4, "AudioItem", "Nightglass");
      Pump();

      Border tunes = ControlNamed<Border>(window, "audioQueue");
      List<Button> rows = tunes.GetVisualDescendants().OfType<Button>().ToList();
      // How many rows the panel draws is what the height of the stage affords, so the test asks for the rows it
      // drew rather than for a number the stage was free to decide.
      Assert.That(rows, Is.Not.Empty, "the panel of a twelve tune record drew no row");

      List<Button> playing = rows.Where(r => r.Classes.Contains("current")).ToList();
      Assert.That(playing, Has.Count.EqualTo(1), "one row of the list names the tune that plays");
      Assert.That(((ISolidColorBrush)playing[0].Background!).Color, Is.EqualTo(Colors.White),
          "the tune that plays stands on a white plate");
      Assert.That(((ISolidColorBrush)playing[0].Foreground!).Color, Is.EqualTo(Colors.Black),
          "and says its name in black");
      Assert.That(rows.Where(r => !r.Classes.Contains("current"))
                      .All(r => ((ISolidColorBrush)r.Background!).Color != Colors.White), Is.True,
          "every tune that does not play keeps the shade of the panel");
      Assert.That(((ISolidColorBrush)rows.First(r => !r.Classes.Contains("current")).Background!).Color,
          Is.EqualTo(Color.Parse("#182E4050")),
          "and keeps it as the blue hole it always was, which is what the pickers of a film still wear");
    }

    /// <summary>
    /// The dark caret belongs to the ring and to nothing else: a row that is merely the one that plays stands on
    /// its white plate with no rim at all, and only when the ring walks onto it does the dark edge come - which is
    /// also when the words have to stay black, since a white word on a white plate is no word.
    /// </summary>
    [AvaloniaTest]
    public void TheDarkCaretOfThePlayingTuneComesOnlyWithTheRing()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      OpenOsd(window);

      List<TitleViewModel> shelf = new List<TitleViewModel>();
      for (int number = 1; number <= 12; number++)
      {
        shelf.Add(new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Tune " + number, null, 2024, null,
            "file:///D:/Musik/" + number.ToString("00") + ".mp3", false, Track: number)));
      }

      screen.Play(Film("the-long-night.mp3"));
      screen.PlayingAspect = "AudioItem";
      screen.LoadAudioQueue(shelf, 4, "AudioItem", "Nightglass");
      Pump();

      Border tunes = ControlNamed<Border>(window, "audioQueue");
      Button playing = tunes.GetVisualDescendants().OfType<Button>().First(r => r.Classes.Contains("current"));

      // At rest, with the ring standing somewhere else: a white plate, no dark edge. The list is not a place the
      // caret can stand until a key walks into it, so neither the first focus of a fresh window - which has nothing
      // else to choose and would take the list, being the first thing on the screen it could - nor the tune that
      // plays of its own accord puts a ring here.
      Assert.That(tunes.IsKeyboardFocusWithin, Is.False, "a fresh window leaves the caret out of the list of tunes");
      Assert.That(playing.IsFocused, Is.False, "and above all not on the tune that plays");
      Assert.That(((ISolidColorBrush)playing.BorderBrush!).Color, Is.Not.EqualTo(Color.Parse("#B3000000")),
          "the tune that plays wears no dark rim while nothing points at it");
      Assert.That(playing.BorderThickness, Is.EqualTo(new Thickness(1)),
          "and keeps the one line of rim every other row wears");

      // The viewer walks in over the end of the row of buttons: now the dark edge, and the words stay readable.
      Button endOfTransport = ButtonNamed(window, "repeatButton");
      Assert.That(endOfTransport.Focus(), Is.True);
      Pump();
      Assert.That(PressKey(endOfTransport, Key.Right), Is.True, "the transport answered the rightward key");

      Assert.That(playing.IsFocused, Is.True, "the ring stands on the row now");
      Assert.That(((ISolidColorBrush)playing.BorderBrush!).Color, Is.EqualTo(Color.Parse("#B3000000")),
          "and only now the dark caret edge comes");
      Assert.That(playing.BorderThickness, Is.EqualTo(new Thickness(3)), "three lines of it, as every caret is");
      Assert.That(((ISolidColorBrush)playing.Foreground!).Color, Is.EqualTo(Colors.Black),
          "and the words stay black, since a white word on a white plate is no word");
      Assert.That(((ISolidColorBrush)playing.Background!).Color, Is.EqualTo(Colors.White),
          "on the white plate it always stood on");

      // The face is what paints the words, and a theme dresses a focused button's face in the screen's white of
      // its own accord; on a white plate that is no word at all, so the tunes' rule has to answer the face and not
      // only the button.
      ContentPresenter face = playing.GetVisualDescendants().OfType<ContentPresenter>()
          .First(p => p.Name == "PART_ContentPresenter");
      Assert.That(((ISolidColorBrush)face.Foreground!).Color, Is.EqualTo(Colors.Black),
          "and the face that paints the words paints them in black too");
    }

    /// <summary>
    /// The card the tunes stand on answers a caret standing inside it with the glow this screen's glass already
    /// knows - the same twenty two of blur and four of spread a plate of the transport wears under a hand or a ring
    /// - and lets the glow go again when the caret steps back out over the end of the row.
    /// </summary>
    [AvaloniaTest]
    public void TheCardOfTunesGlowsWhileTheCaretStandsInsideIt()
    {
      var player = new FakePlayer();
      ShellWindow window = ShowPlayback(player, out NowPlayingViewModel screen);
      OpenOsd(window);

      List<TitleViewModel> shelf = new List<TitleViewModel>();
      for (int number = 1; number <= 12; number++)
      {
        shelf.Add(new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Tune " + number, null, 2024, null,
            "file:///D:/Musik/" + number.ToString("00") + ".mp3", false, Track: number)));
      }

      screen.Play(Film("the-long-night.mp3"));
      screen.PlayingAspect = "AudioItem";
      screen.LoadAudioQueue(shelf, 4, "AudioItem", "Nightglass");
      Pump();

      Border tunes = ControlNamed<Border>(window, "audioQueue");
      Assert.That(tunes.IsKeyboardFocusWithin, Is.False, "nothing stands in the list to begin with");
      Assert.That(tunes.BoxShadow.Count, Is.Zero, "and the card of tunes wears no glow");
      double cardHeight = tunes.Bounds.Height;

      Button endOfTransport = ButtonNamed(window, "repeatButton");
      Assert.That(endOfTransport.Focus(), Is.True);
      Pump();
      Assert.That(PressKey(endOfTransport, Key.Right), Is.True, "the transport answered the rightward key");
      Pump();

      Assert.That(tunes.IsKeyboardFocusWithin, Is.True, "the caret stands in the list of tunes now");
      Assert.That(tunes.BoxShadow.Count, Is.EqualTo(1), "and the card glows while it does");
      Assert.That(tunes.BoxShadow.ToString(), Does.Contain("22"),
          "with the twenty two of blur a plate of the transport glows with");
      Assert.That(tunes.Bounds.Height, Is.EqualTo(cardHeight),
          "and the rim the row took on is paid out of the row, not out of the card");

      // And the glow is the caret's, not a decoration the card keeps: the key back onto the buttons takes it away.
      Assert.That(PressKey(tunes, Key.Left), Is.True, "the list answered the key back to the buttons");
      Pump();
      Assert.That(tunes.IsKeyboardFocusWithin, Is.False, "the caret stepped out of the list again");
      Assert.That(tunes.BoxShadow.Count, Is.Zero, "and the glow went with it");
    }

  }
}
