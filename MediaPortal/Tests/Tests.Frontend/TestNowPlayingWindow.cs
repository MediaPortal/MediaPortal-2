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

    private static void AssertThatVolumeLineIsFocused(Window window, string because)
    {
      Assert.That(ButtonNamed(window, "volumeControl").IsFocused, Is.True, because);
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
      Assert.That(texts.Any(text => text.Contains("Playing")), Is.True,
          "what the player is doing is on screen too");
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

      // There is no Stop button on the panel: a film that is watched to the end is finished, and a viewer who
      // means to end it early presses the stop key of the remote, which is what a keyboard and a remote both
      // have. The panel gives up nothing by leaving it out, and gains the room the transport is made of.
      Assert.That(PressKey(PlaybackView(window), Key.MediaStop), Is.True, "the screen answers the stop key");
      Assert.That(player.Calls, Does.Contain("Stop"), "which stopped the film");
      Assert.That(screen.IsPlaying, Is.False, "and the screen knows it");
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
      Assert.That(pause.Focus(), Is.True, "with the display open, the pause button takes the focus");
      Pump();

      Assert.That(PressKey(pause, Key.Up), Is.True, "the ring used the key");
      Assert.That(ButtonNamed(window, "backButton").IsFocused, Is.True,
          "up from play walks to the rewind button, which stands beside it");
      Assert.That(ButtonNamed(window, "infoButton").IsEnabled, Is.False,
          "and the name of this film is no button, because a file nobody put in a library has nothing to say");

      Assert.That(PressKey(ButtonNamed(window, "backButton"), Key.Up), Is.True, "the ring used the key again");
      Assert.That(ButtonNamed(window, "settingsButton").IsFocused, Is.True,
          "with nothing before it to stop on, the ring wrapped to the last control a viewer can reach");
      Assert.That(player.TimeInMs, Is.EqualTo(40_000), "while the film did not move at all");

      Assert.That(PressKey(ButtonNamed(window, "settingsButton"), Key.Down), Is.True, "and it wraps the other way");
      Assert.That(ButtonNamed(window, "backButton").IsFocused, Is.True,
          "back onto the transport rather than into the controls this film cannot offer");
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
      Pump();

      Button subtitles = ButtonNamed(window, "subtitleButton");
      Assert.That(subtitles.Focus(), Is.True);
      Pump();

      Assert.That(PressKey(subtitles, Key.Down), Is.True, "the ring took the key past the end of the transport");
      Assert.That(ButtonNamed(window, "audioButton").IsFocused, Is.True,
          "the subtitle picker is followed by the sound picker, which the visible track row just put into the ring");

      Assert.That(PressKey(ButtonNamed(window, "audioButton"), Key.Down), Is.True);
      Assert.That(ButtonNamed(window, "volumeButton").IsFocused, Is.True,
          "and past the pickers comes the loudspeaker, which every film has whether it has a choice of tracks or not");

      // The loudspeaker no longer carries its line of sound beside it on the row, so the keys across the row
      // belong to the ring everywhere along it. One key used to mean two things - the volume where the line
      // stood and the next button beside it - and that is exactly the conflict the sound's own dialog ended.
      Assert.That(PressKey(ButtonNamed(window, "volumeButton"), Key.Down), Is.True);
      Assert.That(ButtonNamed(window, "fullscreenButton").IsFocused, Is.True,
          "the row walks on past the loudspeaker to the switches of the machine");

      Assert.That(PressKey(ButtonNamed(window, "fullscreenButton"), Key.Down), Is.True);
      Assert.That(ButtonNamed(window, "settingsButton").IsFocused, Is.True,
          "which ends at the gear: how this machine plays is asked for where the rest of the player is");

      Assert.That(PressKey(ButtonNamed(window, "settingsButton"), Key.Down), Is.True);
      Assert.That(ButtonNamed(window, "backButton").IsFocused, Is.True,
          "one further still wraps to the beginning of the loop, over the name of a film nobody catalogued, so "
          + "the pickers are part of the same loop rather than a screen of their own");

      // And the sound itself, which is asked for by the loudspeaker and answered by a dialog the keys of their
      // own belong to.
      Press(ButtonNamed(window, "volumeButton"));
      Pump();
      AssertThatVolumeLineIsFocused(window,
          "the loudspeaker raised its dialog and put the viewer on the line of the sound, where the level is set");

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

      Assert.That(PressKey(loudspeaker, Key.Up), Is.True, "the ring took the key back off the loudspeaker");
      Assert.That(ButtonNamed(window, "audioButton").IsFocused, Is.True,
          "up from the loudspeaker is the round mark of the sound, which this film now carries on its ring");

      Assert.That(PressKey(ButtonNamed(window, "audioButton"), Key.Up), Is.True, "and the ring took it again");
      Assert.That(ButtonNamed(window, "subtitleButton").IsFocused, Is.True,
          "which is followed by the subtitle switch, the two marks standing side by side as they do on the row");

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
    public void TheBandNamesTheFilmAndReadsTheFileItIsMadeOf()
    {
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
      var file = TextOn(window, "big_buck_bunny_4k.mkv");
      Assert.That(file, Is.Not.Null, "and the file it is made of is read underneath, once");
      Assert.That(TopLeftOf(file!, window).Y, Is.GreaterThan(TopLeftOf(title!, window).Y),
          "underneath, not beside it - the name is the line a viewer reads first");

      // And the two lines are read above the position line, which is above the marks.
      var track = ProgressBarOn(window);
      Assert.That(TopLeftOf(file, window).Y + file.Bounds.Height,
          Is.LessThan(TopLeftOf(track, window).Y),
          "the reading of the film is finished before the position line begins");

      // What the file is made of is read beside its name, but only once the engine has said it: the name of a
      // film whose picture nobody described carries no size, because a size invented for it would be a lie
      // about the viewer's own file.
      Assert.That(TextOn(window, "4K"), Is.Null, "nothing was reported about this file, so no size is claimed");

      player.Quality = new PlaybackQuality(3840, 2160, "hev1");
      screen.Play(Film("big_buck_bunny_4k.mkv"), Guid.NewGuid(), 0, "Big Buck Bunny");
      Pump();
      PumpFor(400);

      Assert.That(TextOn(window, "4K"), Is.Not.Null,
          "and when the file does say what it is, the name of the picture it carries stands beside its name");
      Assert.That(TextOn(window, "HEVC"), Is.Not.Null, "and so does the word for the codec it is cut in");
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

  }
}
