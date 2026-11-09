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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The screen that shows and drives what is playing.
  /// </summary>
  /// <remarks>
  /// The view model belongs to the application, not to this window: closing the window stops nobody from
  /// listening to music, which is what a media centre is expected to do, and the same playback shows up again
  /// the next time the screen is opened. The application therefore owns the view model and disposes it.
  /// </remarks>
  public partial class NowPlayingView : UserControl, IWorkflowScreen, Workflow.IFullScreenScreen,
      Workflow.IFilmScreen
  {
    private readonly NowPlayingViewModel _viewModel;
    private readonly WorkflowManager? _workflow;

    // What the panel's own list of tracks needs to know about itself: whether it is up, and how many chapter
    // marks stood on the position line last time it was painted. The sound has its own control now.
    private bool _tracksOpen;

    private int _marksPainted = -1;

    /// <summary>
    /// The record's turn, handed to the disc the XAML named the way the wake hands its ring to
    /// <see cref="StartupView"/>. Turned here rather than by a style's animation because the turn belongs to the
    /// engine: an animation a style starts has no way of hearing the pause, and a record that carries on turning
    /// over a song that stands still lies about the machine.
    /// </summary>
    /// <remarks>
    /// Hinged at its own middle without being told to: a visual's <c>RenderTransformOrigin</c> is the middle of
    /// the element by default in Avalonia - the frame of a screen that names no origin printed "origin=50%, 50%"
    /// - so naming a centre on the turn as well would hinge the record at its lower right corner, four hundred
    /// and six lines away from where it belongs.
    /// </remarks>
    private readonly RotateTransform _discTurn = new RotateTransform();

    /// <summary>The clock the turn runs on, or null while the record stands still.</summary>
    private DispatcherTimer? _discClock;

    /// <summary>When the record was last set turning, and how far it had already come by then.</summary>
    private DateTime _discTurnFrom;

    private double _discDegrees;

    /// <summary>Four and a half seconds to a revolution, which is the turn the audio screen was drawn with.</summary>
    private const double DISC_DEGREES_PER_SECOND = 360.0 / 4.5;

    /// <summary>
    /// The air left between a song's name and the switches at the row's right hand end: the same twenty four lines
    /// of air the mockup leaves between the record and what stands after it (index.html, the cover's middle axis
    /// plus the record's four hundred and six lines plus twenty four = the seven hundred and two the equalizer
    /// starts on).
    /// </summary>
    private const double SONG_NAME_AIR = 24;

    /// <summary>
    /// The narrowest a song's name is ever made: one of the row's glass plates, seventy lines across
    /// (PlayerGlassStyles.axaml, the plate the mockup's row is drawn with). Below that width a name is an ellipsis
    /// and nothing else, and a word of one letter is no answer to "what is playing".
    /// </summary>
    private const double SONG_NAME_FLOOR = 70;

    /// <summary>
    /// The band of shade the row of buttons carries over a film, kept in hand because the audio arrangement wears
    /// one shade over the whole screen instead and puts this one away - the row would else be darkened twice.
    /// </summary>
    private readonly IBrush? _osdShade;

    /// <summary>
    /// The controls the directional keys move between, in the order they stand on the panel. Held as well as
    /// handed to <see cref="DpadNavigation"/> because one of them - the slider - answers some of those keys
    /// itself and has to ask the ring where the viewer meant to go.
    /// </summary>
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>
    /// For the XAML loader: a playback screen with no player behind it, which is what it says on screen.
    /// </summary>
    public NowPlayingView()
        : this(new NowPlayingViewModel(null))
    {
    }

    public NowPlayingView(NowPlayingViewModel viewModel)
        : this(viewModel, null)
    {
    }

    /// <summary>
    /// The playback screen, optionally showing the frames a player is handing over.
    /// </summary>
    /// <param name="frames">
    /// The frames to draw, or null. Null is a normal answer and not a failure to check: it means this machine has
    /// no player or one that will not hand its pictures over, and the screen then shows its state text in the
    /// black frame rather than pretending there is a picture behind it.
    /// </param>
    /// <param name="workflow">
    /// The navigation this screen was opened through, or <c>null</c> when the playback screen is all the window
    /// holds - the <c>--play</c> start - where "close" means the window itself.
    /// </param>
    public NowPlayingView(NowPlayingViewModel viewModel, VideoFrameSink? frames, WorkflowManager? workflow = null)
    {
      _viewModel = viewModel;
      _workflow = workflow;
      DataContext = viewModel;
      InitializeComponent();

      // The surface belongs to this screen and follows the frames, which belong to the player. Leaving the screen
      // therefore stops the drawing and nothing else - the film carries on playing, and the next time this screen
      // opens it follows the same frames again.
      videoSurface.Follow(frames);

      // The record behind the cover is turned from here (see _discTurn), and the band of shade the row of buttons
      // wears over a film is taken down for a song, whose screen carries one shade for the whole stage.
      audioDisc.RenderTransform = _discTurn;
      _osdShade = osdArea.Background;

      // The remote's key map, ported from the reference's FullScreenContentVideoKeyBindings.inc and its
      // IsOSDVisible conditions: with the on-screen buttons away, left and right climb the escalating skip
      // ladder and up and down make one instant jump each - the film alone on the screen answers the skip keys
      // and its transport keys, and nothing else. With the buttons shown, the same four keys belong to the
      // d-pad ring and the skip bindings are off, exactly as the reference switches them, while the transport
      // keys keep answering because a remote has a pause button whatever the screen is showing. Which remote
      // button is which key is the binding table's business (Settings/KeyBindings.cs). The screen's own menu -
      // the buttons - is called by whatever the binding table calls the screen's menu key, which by default is
      // the keyboard's I, the remote's Apps key (what the reference's key map reported its Info button as) and
      // the television's Info and contents-menu buttons; back closes the buttons before it ever closes the
      // screen. The reference shows its buttons automatically for a mouse; this asks its own window, the same
      // portable question the screensaver watch asks.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (Settings.KeyBindings.Current.Is(Settings.KeyAction.MediaMenu, args))
            {
              ToggleOsd();
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Back, args) && StepBackOneLayer())
            {
              // Back goes back before it leaves: the list of subtitles first, then the display, then the screen.
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Stop, args))
            {
              // The remote's stop key and the bar's stop button say the same thing: the film stops and the
              // screen leaves, back to where the viewer came from. Which key a given remote reports its stop
              // button as is the table's business, not this screen's.
              StopAndLeave();
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.PlayPause, args))
            {
              // The remote's play/pause button means the same thing as the bar's pause mark. Nothing is said
              // here: the pause mark of the worn skin already rises in the middle of the picture from the
              // model's own state, and the bar's mark and its word change with it.
              _viewModel.TogglePause();
              args.Handled = true;
            }
            else if (!_osdVisible && args.Key == Key.Left)
            {
              ShowSkipStep(_viewModel.SkipStepBackward());
              args.Handled = true;
            }
            else if (!_osdVisible && args.Key == Key.Right)
            {
              ShowSkipStep(_viewModel.SkipStepForward());
              args.Handled = true;
            }
            else if (!_osdVisible && (args.Key == Key.Up || args.Key == Key.Down))
            {
              long jump = _viewModel.InstantSkipMs(args.Key == Key.Up ? 1 : -1);
              CancelSkipStep();
              _viewModel.Skip(jump);
              Say("Superlayer_SkipStep", SkipWords(jump), 1200);
              args.Handled = true;
            }
          },
          RoutingStrategies.Tunnel);

      // The right hand's button takes the back key's first step, because a mouse on a desk has no other way of
      // sending something away: the reference routed an unmapped right click to its context menu key
      // (Screen.cs:731-748) and its film screen answers no context menu at all, so what the reference left open is
      // settled here in the one direction a viewer expects - the thing standing over the film goes, the film
      // stays. Asked from the tunnel, before the line of the sound ever hears the press, so dismissing the dialog
      // never also moves the level to wherever the pointer happened to be. With nothing over the film the click
      // is left alone, exactly as the back key leaves the screen to the shell.
      AddHandler(PointerPressedEvent,
          delegate (object? sender, PointerPressedEventArgs args)
          {
            if (args.Properties.IsRightButtonPressed && StepBackOneLayer())
              args.Handled = true;
          },
          RoutingStrategies.Tunnel);

      AddHandler(PointerMovedEvent,
          delegate
          {
            if (!_osdVisible && !_osdOpenOnDemand && DateTime.UtcNow > _osdMouseSuppressedUntil)
              SetOsdVisible(true);
          },
          handledEventsToo: true);

      // ... and a click anywhere on the film calls the display too: the pointer's way of asking what the Info
      // key asks, and what a viewer does with a screen that shows no sign of itself - they touch it. Asked in
      // the tunnel with the handled flag honoured, because the transport buttons, the position line and the
      // line of the sound all take the press for themselves first, and the hand that pressed one of them
      // plainly wanted the display anyway. Left button alone: the right button is the back step (see above)
      // and must not call what it is about to send away.
      AddHandler(PointerPressedEvent,
          delegate (object? sender, PointerPressedEventArgs args)
          {
            if (!args.Properties.IsLeftButtonPressed)
              return;

            if (!_osdVisible && !_osdOpenOnDemand && DateTime.UtcNow > _osdMouseSuppressedUntil)
              SetOsdVisible(true);
          },
          RoutingStrategies.Tunnel,
          handledEventsToo: true);

      // The ring a remote walks, in the order the controls stand: the name of the film at the left of the top
      // row, the transport across the middle of it, the position line under it, and the choices along the
      // bottom. A control that is not drawn is not an area, so a film without a second sound walks straight from
      // the subtitle button to the loudspeaker, and the loop closes on whichever control the viewer could
      // actually reach.
      _ring = new List<Control>
          {
            infoButton, shuffleButton, chapterBackButton, backButton, tuneBackButton, pauseButton, stopButton,
            tuneForwardButton, forwardButton, chapterForwardButton, repeatButton,
            seekBar, subtitleButton, audioButton, volumeButton, fullscreenButton,
            settingsButton
          };
      DpadNavigation.Attach(this, _ring);

      // The ring above walks its controls as one line, so it reads a direction as "the next one along", which on
      // this panel - whose controls stand in three rows - sends a downward key to the button beside rather than the
      // one below and wraps the far edges into other rows. While the caret stands on one of the panel's own
      // controls, this earlier claim takes all four keys back for the rows: left and right step along the row, up
      // and down climb a row onto whichever control sits most under the one left, and where the row has no further
      // control or the panel no further row, the whole panel nods against that edge and springs back. Only on the
      // position line are left and right left alone - there they walk the film, which is the line's own purpose -
      // and the ring keeps its one-line walk for the first step onto the panel from elsewhere.
      AddHandler(KeyDownEvent, delegate (object? sender, KeyEventArgs args)
          {
            if (args.Handled)
              return;

            bool vertical = args.Key == Key.Up || args.Key == Key.Down;
            bool sideways = args.Key == Key.Left || args.Key == Key.Right;
            if (!vertical && !sideways)
              return;

            Visual? stood = (TopLevel.GetTopLevel(this) as Window)?.FocusManager.GetFocusedElement() as Visual;
            if (stood == null)
              return;

            // The list of tunes belongs to the audio arrangement and stays drawn while the panel is away, so it
            // answers its own keys whether or not the panel is up. Everything else here is the panel's business
            // and is only walked while the panel is to be seen.
            bool inTunes = audioQueue.IsVisible && IsWithinTunes(stood);
            if (!_osdVisible && !inTunes)
              return;

            // Only while the caret stands on the panel itself. The sound's dialog and a list of tracks are the
            // panel's own answers to a button but sit outside it, and both walk their keys themselves.
            if (!inTunes && !IsWithinOsd(stood))
              return;

            if (inTunes)
            {
              MoveTunes(args.Key);
              args.Handled = true;
              return;
            }

            // On the position line the keys across it belong to the film, not to the panel: standing on the line,
            // left and right walk it - the line's own answer (OnSeekBarKey) - so this hands them back untouched.
            if (sideways && seekBar.IsKeyboardFocusWithin)
              return;

            switch (args.Key)
            {
              case Key.Up:
                MoveVertical(-1);
                break;
              case Key.Down:
                MoveVertical(1);
                break;
              case Key.Left:
                MoveHorizontal(-1);
                break;
              case Key.Right:
                MoveHorizontal(1);
                break;
            }

            args.Handled = true;
          },
          RoutingStrategies.Tunnel);

      // The tune list wears a caret when the viewer stands on it. It must not wear one because the window had to
      // put its first focus somewhere and the list is the first thing it could stand on - which is what the audio
      // arrangement looked like from the first second: a dark frame round the tune that plays, with nobody at it.
      // So the rows offer no place for a caret at all until a key of the viewer's walks into them; see the note on
      // the row in NowPlayingView.axaml and the StandOnTune below that opens one.

      // Holding a skip mark keeps doing what one press does - see RepeatWhileHeld. The volume is not among them:
      // a line of sound follows the hand that is on it, which is a better answer than stepping.
      RepeatWhileHeld(backButton, SkipBack);
      RepeatWhileHeld(forwardButton, SkipForward);

      // The knob stands at a point measured along the position line, so a line that changes width - a window
      // that is resized, a television that is switched to another mode - has to be asked again where that
      // point is.
      progressRow.SizeChanged += delegate { PaintMeasurements(); };

      // The sound's dialog is lifted above the row of buttons rather than left standing over them, and the height
      // that row takes is what decides how far - so a window that changes its size moves the dialog with it.
      osdArea.SizeChanged += delegate { volumePicker.HoldAbove(osdArea.Bounds.Height); };

      // How many tunes the list beside the equalizer can show at once is a question of how tall the room is, so
      // a window that grows or shrinks is asked again.
      SizeChanged += delegate { GiveTheTunesTheirRoom(); };

      // A song's name has the room the row leaves it, which is not a number anybody can write down: it is the
      // width of the window less the switches standing at its right hand end, and those two change together with
      // the window and with the two lists a film's row also carries. Measured rather than guessed, because the
      // name a library really carries is longer than the one a mockup is drawn with - the record of a running
      // library is named "7: 1 - Don't Leave Me Alone (feat. Anne-Marie)", which wants seven hundred and
      // thirty-eight lines where the mockup's "Momentum" wanted two hundred.
      osdArea.SizeChanged += delegate { RoomForTheSongName(); };
      rowSwitches.SizeChanged += delegate { RoomForTheSongName(); };

      // The line of the film answers the hand the same way, and it drags as well: a press puts the film where it
      // was pressed and holding the button down walks the film along with the hand, which is what every position
      // bar a viewer has ever held does. Asking for the handled press is not a detail here - the bar is a button,
      // and a button keeps the press for itself.
      seekBar.AddHandler(PointerPressedEvent, OnSeek, RoutingStrategies.Bubble, true);
      seekBar.AddHandler(PointerMovedEvent, OnSeekMoved, RoutingStrategies.Bubble, true);
      seekBar.AddHandler(PointerCaptureLostEvent, OnSeekReleased, RoutingStrategies.Bubble, true);

      // The line of the sound, its knob, its drag and its reading of the room's volume keys all belong to the
      // sound's own control (Views/VolumePicker.axaml) now, which the compact row at the foot of every other
      // screen wears as well.

      // The cover question goes out while the screen can see its answer: opened, it asks once, and while it is
      // open every change of the player gets the same guarded question - a no-op until the id says otherwise.
      // Detaching unsubscribes, because the model outlives every visit to this screen and the stale visitor
      // has nothing left to draw.
      AttachedToVisualTree += delegate { OnFollowingPlayback(); };
      DetachedFromVisualTree += delegate { OnStoppingPlayback(); };

      // Back is the shell's key once the on-screen display is away: it pops this screen without changing
      // anything about the playback, and when this screen is all the window holds the shell closes the
      // window - the behaviour <c>--play</c> relies on. While the display is up, back closes it first.
      // Escape and Backspace are the two keys a remote's back button sends, depending on the platform.

      // And the reference's opening answer: the film begins alone. The display waits for the Info key, a
      // key in its room, or the mouse arriving at all.
      SetOsdVisible(false);
    }

    /// <summary>
    /// How many pictures this screen has drawn. Reported by <c>--play</c> when it is asked to leave after a while,
    /// and read by a test that wants to know a frame really arrived rather than only that a control was built.
    /// </summary>
    public long FramesDrawn
    {
      get { return videoSurface.FramesDrawn; }
    }

    /// <summary>
    /// Where the picture area ended up inside the window, in the units Avalonia lays out in.
    /// </summary>
    /// <remarks>
    /// Asked when a film is reported missing: pictures can fail to reach a person because nothing decoded them,
    /// because they were decoded and never drawn, or because they were drawn into an area so small that no
    /// amount of looking would find them. Only the second of those shows up in a frame counter.
    /// </remarks>
    public Rect SurfaceBounds
    {
      get { return videoSurface.Bounds; }
    }

    /// <summary>
    /// The picture this screen was given, written to a PNG. <c>--shot</c> uses it, because "frames drawn" and
    /// "a film was there" are different claims and only the file can settle the second one.
    /// </summary>
    public string SavePictureTo(string path)
    {
      return videoSurface.SavePicture(path);
    }

    /// <summary>The size of the picture on screen, or null while the frame is empty.</summary>
    public PixelSize? PictureSize
    {
      get { return videoSurface.PictureSize; }
    }

    private void OnPause(object? sender, RoutedEventArgs e)
    {
      _viewModel.TogglePause();
    }

    private void OnStop(object? sender, RoutedEventArgs e)
    {
      // Stop means: the film ends here and the viewer goes back to where they came from. Not pause, and not
      // the back key's "leave it running" - the whole point of pressing stop is that nothing plays on after
      // the screen is gone.
      StopAndLeave();
    }

    /// <summary>
    /// Stops playback outright and leaves the screen, landing where the back key lands: back onto the screen
    /// the viewer came from, or - when the player was all the window held - closing the window.
    /// </summary>
    private void StopAndLeave()
    {
      CancelSkipStep();
      // Stopping before the pop: the screen leaving on its own leaves the film running (that is the back
      // key's promise), so the stop has to happen while this screen still stands here.
      _viewModel.Stop();
      LeaveScreen();
    }

    /// <summary>Leaves the screen without touching playback: pop the workflow, or close an all-playing window.</summary>
    private void LeaveScreen()
    {
      if (_workflow != null && _workflow.Pop())
        return;

      (TopLevel.GetTopLevel(this) as Window)?.Close();
    }

    private void OnBack(object? sender, RoutedEventArgs e)
    {
      SkipBack();
    }

    /// <summary>
    /// The record's own order, asked of its mark: the tune that is playing carries on and what follows it is put
    /// in a different sequence. The mark lights and the list beside the equalizer rearranges itself, which is the
    /// whole of the answer - no line over the picture is needed to say that a mark is on.
    /// </summary>
    private void OnShuffle(object? sender, RoutedEventArgs e)
    {
      _viewModel.ToggleShuffle();
    }

    /// <summary>Nothing after the last tune, the record again, this tune for ever - one press per answer.</summary>
    private void OnRepeat(object? sender, RoutedEventArgs e)
    {
      _viewModel.CycleRepeat();
    }

    /// <summary>
    /// The tune before this one. At the very first tune this starts this tune again from its beginning, which is
    /// what the mark has always done when there is nothing behind it, so the press is never answered by nothing.
    /// </summary>
    private void OnTuneBack(object? sender, RoutedEventArgs e)
    {
      _viewModel.PreviousTune();
    }

    /// <summary>The next tune of the record, or the screen's own words for why there is none.</summary>
    private void OnTuneForward(object? sender, RoutedEventArgs e)
    {
      if (!_viewModel.NextTune())
        Say("Superlayer_SkipStep", _viewModel.StatusLine, 1200);
    }

    /// <summary>
    /// A row of the record's list was pressed: that tune now. The row itself goes over to the view model rather
    /// than its place in the list, because the rows on screen are a window out of the record that walks whenever
    /// the player does - a place counted against a window that has moved answers with the tune standing there
    /// now, which is a different tune from the one the hand reached for.
    /// </summary>
    private void OnQueueRow(object? sender, RoutedEventArgs e)
    {
      if (sender is not Button { DataContext: AudioQueueRow row })
        return;

      _viewModel.PlayQueuedTune(row);
    }

    private void OnForward(object? sender, RoutedEventArgs e)
    {
      SkipForward();
    }

    /// <summary>
    /// One instant skip back, the button's and the held repeat's own words for it: the distance the viewer
    /// answered in the playback settings, the same jump the remote's up and down keys make.
    /// </summary>
    private void SkipBack()
    {
      long jump = _viewModel.InstantSkipMs(-1);
      CancelSkipStep();
      _viewModel.Skip(jump);
      Say("Superlayer_SkipStep", SkipWords(jump), 1200);
    }

    /// <summary>One instant skip on, as <see cref="SkipBack"/> in the other direction.</summary>
    private void SkipForward()
    {
      long jump = _viewModel.InstantSkipMs(1);
      CancelSkipStep();
      _viewModel.Skip(jump);
      Say("Superlayer_SkipStep", SkipWords(jump), 1200);
    }

    /// <summary>
    /// Makes a mark do its thing again and again while the pointer is held on it: the press itself is the
    /// button's ordinary click, and every 350 ms after it the same action happens until the pointer is lifted or
    /// slides off. A viewer who wants five minutes forward holds one mark instead of tapping it twelve times,
    /// which is what a touch screen taught everybody to expect. The remote's keys keep their own escalating
    /// ladder untouched - a viewer pressing left four times means four presses.
    /// </summary>
    private static void RepeatWhileHeld(Control target, Action repeat)
    {
      DispatcherTimer? timer = null;

      void Stop()
      {
        timer?.Stop();
        timer = null;
      }

      target.AddHandler(PointerPressedEvent, delegate (object? sender, PointerPressedEventArgs args)
      {
        if (!args.GetCurrentPoint(target).Properties.IsLeftButtonPressed)
          return;

        Stop();
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        timer.Tick += delegate { repeat(); };
        timer.Start();
      }, handledEventsToo: true);

      target.AddHandler(PointerReleasedEvent, delegate { Stop(); }, handledEventsToo: true);
      target.AddHandler(PointerCaptureLostEvent, delegate { Stop(); }, handledEventsToo: true);
    }

    /// <summary>
    /// Pressing the position bar puts the film where the viewer pointed, which is what pressing a position bar
    /// has always meant, and holding the button down walks the film along with the hand.
    /// </summary>
    private void OnSeek(object? sender, PointerPressedEventArgs args)
    {
      _seekDragging = true;
      _seekDraggedToMs = long.MinValue;
      SeekWherePointed(args);
    }

    /// <summary>Dragging along the line while the press still holds moves the film with the hand.</summary>
    private void OnSeekMoved(object? sender, PointerEventArgs args)
    {
      if (!_seekDragging || !args.GetCurrentPoint(progressRow).Properties.IsLeftButtonPressed)
        return;

      SeekWherePointed(args);
    }

    /// <summary>
    /// The hand left the line. The reading of the new place goes up now rather than on every pixel of the drag,
    /// because a position that flickers eighty times across the screen while a hand moves says nothing.
    /// </summary>
    private void OnSeekReleased(object? sender, PointerCaptureLostEventArgs args)
    {
      if (!_seekDragging)
        return;

      _seekDragging = false;
      Say("Superlayer_SkipStep", NowPlayingViewModel.Format((long)(_viewModel.Progress * _viewModel.LengthMs)),
          1200);
    }

    private bool _seekDragging;
    private long _seekDraggedToMs;

    /// <summary>
    /// Put the film where the hand pointed along the position line. The share is taken from the line's own width;
    /// the caption is left to the release, because a bar this long is pointed at rather than aimed at. A drag
    /// asks the player for one jump per second of the film at most: a decoder that was asked for a new picture on
    /// every pixel of the drag stops drawing pictures altogether, which is the last thing a viewer dragging
    /// through a film wants to see.
    /// </summary>
    private void SeekWherePointed(PointerEventArgs args)
    {
      double width = progressRow.Bounds.Width;
      if (width <= 0)
        return;

      SeekAlongTheLine(args.GetPosition(progressRow).X / width);
    }

    /// <summary>
    /// Put the film at a fraction of its length: its beginning for a press at the line's very start, its end for
    /// a press at the line's end. Asked for by the hand through <see cref="SeekWherePointed"/>, and asked for
    /// directly by the tests, which can hand over a fraction of the line but not a place on a screen.
    /// </summary>
    public void SeekAlongTheLine(double fraction)
    {
      if (progressRow.Bounds.Width <= 0)
        return;

      fraction = Math.Max(0, Math.Min(1, fraction));
      long wanted = (long)(fraction * _viewModel.LengthMs);
      if (_seekDraggedToMs != long.MinValue && Math.Abs(wanted - _seekDraggedToMs) < 1000)
        return;

      _seekDraggedToMs = wanted;
      if (_viewModel.SeekToFraction(fraction))
        PaintMeasurements();
    }

    /// <summary>
    /// Fills the television, or goes back to the desktop: the window's own answer, and the saved one, because a
    /// viewer who leaves the front end over the whole television means it for the next start too.
    /// </summary>
    private void OnFullScreen(object? sender, RoutedEventArgs e)
    {
      Window? window = TopLevel.GetTopLevel(this) as Window;
      if (window == null)
        return;

      bool fullScreen;
      if (window is ShellWindow shell)
      {
        fullScreen = shell.ToggleFullScreen();
      }
      else
      {
        // A window nobody wrote - a test's, or a host's - still has the state to change, only not the file to
        // write the answer to.
        fullScreen = window.WindowState != WindowState.FullScreen;
        window.WindowState = fullScreen ? WindowState.FullScreen : WindowState.Maximized;
      }

      Say("Superlayer_SkipStep", fullScreen ? "Full screen" : "On the desktop", 1200);
    }

    private DispatcherTimer? _skipStepTimer;

    private bool _osdVisible = true;

    // The list of tunes offers no place for a caret until a key walks into it, so the window's first focus cannot
    // land on the tune that is playing and dress it in a dark caret with nobody pointing at it. See the note on the
    // row in NowPlayingView.axaml and StandOnTune, which opens a row for the caret.
    private bool _osdOpenOnDemand;
    private DateTime _osdMouseSuppressedUntil = DateTime.MinValue;

    /// <summary>
    /// Whether the on-screen controls are showing. The reference starts its full screen without them - just
    /// the film - and answers the Info key by putting them on and taking them away; a mouse in the room
    /// brings them back on its own. This screen does the same: everything except the picture, the centred
    /// state icon and the superlayers is the on-screen display, and with it away the skip keys own the
    /// remote.
    /// </summary>
    public bool OsdVisible
    {
      get { return _osdVisible; }
    }

    /// <summary>The reference's ToggleOSD behind its Info key.</summary>
    public void ToggleOsd()
    {
      SetOsdVisible(!_osdVisible);
      _osdOpenOnDemand = _osdVisible;
      if (!_osdVisible)
        SuppressMouseOsd();
    }

    /// <summary>The reference's CloseOSD behind its back key: the buttons go first, the screen only after.</summary>
    public void CloseOsd()
    {
      SetOsdVisible(false);
      _osdOpenOnDemand = false;
      SuppressMouseOsd();
    }

    public void SetOsdVisible(bool visible)
    {
      _osdVisible = visible;

      // The panel is one thing: the film's name, its position line and its choices come and go together, which
      // is what a viewer expects of a display laid over a film. Which control held the focus is left where it
      // was, so the panel returns with the ring on the control the viewer last stood on. While it is away the
      // ring cannot run anyway: the screen claims the skip keys itself first.
      //
      // A list of tracks that was open over the film goes with it. A picker left floating over a film whose
      // controls have just been hidden would be a dialog nobody called.
      if (!visible && _tracksOpen)
        CloseTrackDialog();
      if (!visible && volumePicker.IsOpen)
        CloseVolumeDialog();

      osdArea.IsVisible = visible;
      if (visible)
      {
        // The playhead, the chapter marks and the line of the sound all stand at distances measured along lines
        // whose width only the layout knows, so they are laid out again the moment the panel is put back on the
        // screen rather than left where the last film's width put them.
        PaintMeasurements();
      }
    }

    /// <summary>
    /// Draw the three things on the panel that are measurements rather than words: the playhead at the point of
    /// the film the viewer stands on, the chapter marks at their share of the line, and the line of the sound at
    /// the level the viewer set. All three are widths measured along a line whose own width only the layout
    /// knows once the panel has been laid out, so they are set here rather than bound, and all are skipped while
    /// the line has no width yet.
    /// </summary>
    private void PaintMeasurements()
    {
      double width = progressRow.Bounds.Width;
      if (width > 8)
      {
        double knob = scrubKnob.Bounds.Width;
        double left = _viewModel.Progress * width - knob / 2;
        if (left < 0)
          left = 0;
        if (left > width - knob)
          left = width - knob;
        scrubKnob.Margin = new Thickness(left, 0, 0, 0);

        PaintChapterMarks(width);
      }

      // The line of the sound, painted by its own control: the dialog keeps the height of its line and asks the
      // layout where its filled part and its knob go, which is a measure only the layout knows.
      volumePicker.Repaint();

      // What the film is, as the pictures themselves said: a decoded frame cannot be a different size than the
      // film it is a frame of, which is worth knowing for the file whose container never named its picture.
      PixelSize? picture = videoSurface.PictureSize;
      if (picture != null && picture.Value.Width > 0 && picture.Value.Height > 0)
        _viewModel.ReportPictureSize(picture.Value.Width, picture.Value.Height);
    }

    /// <summary>
    /// Stand every chapter mark at its share of the line's width. The model says how far along the line each
    /// chapter begins and the line says how wide it is, so nothing has to remember a width: a window dragged to
    /// another size gets its marks moved rather than left standing where the old width was. The marks are the
    /// canvas's own children rather than a list's containers, because a mark has to keep the place it was
    /// given - a list hands its containers back to the toolkit when the film turns over, the place went with
    /// them, and every mark of a chaptered film came to stand at the start of the line.
    /// </summary>
    private void PaintChapterMarks(double width)
    {
      IReadOnlyList<NowPlayingViewModel.ChapterMark> marks = _viewModel.ChapterMarks;
      if (marks.Count != _marksPainted)
      {
        // The list changed since the line was last painted, so a line that had no width yet has to be asked
        // again once it has one; that happens on the next layout, which is where this comes back.
        _marksPainted = marks.Count;
        Dispatcher.UIThread.Post(delegate
        {
          if (_osdVisible)
            PaintMeasurements();
        }, DispatcherPriority.Loaded);
      }

      while (chapterMarks.Children.Count > marks.Count)
        chapterMarks.Children.RemoveAt(chapterMarks.Children.Count - 1);
      while (chapterMarks.Children.Count < marks.Count)
      {
        Border mark = new Border();
        mark.Classes.Add("tick");
        chapterMarks.Children.Add(mark);
      }

      if (width <= 0)
        return;
      for (int i = 0; i < marks.Count; i++)
      {
        Border mark = (Border) chapterMarks.Children[i];
        Canvas.SetLeft(mark, marks[i].Fraction * width - mark.Width / 2);
      }
    }

    /// <summary>The reference gives the mouse a one-second grace after a deliberate close before its
    /// pointer movement may open the display again - the same hysteresis, same length.</summary>
    private void SuppressMouseOsd()
    {
      _osdMouseSuppressedUntil = DateTime.UtcNow.AddSeconds(1);
    }

    /// <summary>
    /// Show what this press of the ladder asks for and re-arm the wait: the jump is only made when the
    /// pressing stops for the configured moment (the reference's <c>SkipStepTimeout</c>, 1.5 seconds by
    /// default), so presses held one after the other climb the ladder and the film holds still until then.
    /// A refused step at either border shows the border instead of a number, and the wait ends with the
    /// film thrown to that border - "forward" pressed two minutes before the end means the end.
    /// </summary>
    private void ShowSkipStep(SkipStepAction action)
    {
      // The same words the reference's SkipStepOSD shows: FormatStepUnit for a step, the border's name
      // when a step was refused.
      string words = action switch
      {
        SkipStepAction.ToStart => SkipStepLadder.START_WORD,
        SkipStepAction.ToEnd => SkipStepLadder.END_WORD,
        _ => SkipStepLadder.FormatStep(_viewModel.PendingSkipStepSeconds)
      };
      Say("Superlayer_SkipStep", words, _viewModel.SkipStepTimeoutMs + 300);

      // The reference's centred overlay mark belongs to the seeking moment; on this engine the waiting
      // ladder is that moment, so the arrows answer the ladder.
      int pending = _viewModel.PendingSkipStepSeconds;
      if (pending > 0)
        ShowStateIcon("PlayerControl/fast_forward");
      else if (pending < 0)
        ShowStateIcon("PlayerControl/fast_rewind");
      else
        HideStateIcon();

      if (_skipStepTimer == null)
      {
        _skipStepTimer = new DispatcherTimer();
        _skipStepTimer.Tick += delegate
        {
          _skipStepTimer!.Stop();
          HideStateIcon();
          _viewModel.SkipStepElapsed();
        };
      }
      _skipStepTimer.Interval = TimeSpan.FromMilliseconds(_viewModel.SkipStepTimeoutMs);
      _skipStepTimer.Stop();
      _skipStepTimer.Start();
    }

    /// <summary>Drop a waiting ladder jump - the instant buttons below do their jump now, so a held ladder
    /// that is still waiting is dropped, exactly like the reference cancelling its skip timer.</summary>
    private void CancelSkipStep()
    {
      _skipStepTimer?.Stop();
      HideStateIcon();
      _viewModel.CancelSkipStep();
    }

    /// <summary>Put one of the worn skin's player marks as the centred overlay, if it carries one.</summary>
    private void ShowStateIcon(string vectorName)
    {
      stateIcon.Data = SkinImageConverter.WornPictures()?.Vectors.Load(vectorName);
      stateIcon.IsVisible = stateIcon.Data != null;
    }

    private void HideStateIcon()
    {
      stateIcon.IsVisible = false;
    }

    /// <summary>
    /// Back to the start of this chapter, or to the chapter before when the film barely left this one's start.
    /// Said out loud afterwards, because a film that jumps two minutes without a word looks like a player that
    /// lost its place.
    /// </summary>
    private void OnChapterBack(object? sender, RoutedEventArgs e)
    {
      if (_viewModel.PreviousChapter())
        SayChapter();
    }

    /// <summary>On to the next chapter the file named.</summary>
    private void OnChapterForward(object? sender, RoutedEventArgs e)
    {
      if (_viewModel.NextChapter())
        SayChapter();
    }

    private void SayChapter()
    {
      string words = _viewModel.ChapterCaption;
      Say("Superlayer_SkipStep", words.Length > 0 ? words : NowPlayingViewModel.Format(_viewModel.PositionMs), 1500);
    }

    /// <summary>
    /// Raise the sound's own dialog over the film. The level used to lie along the foot of the picture among the
    /// buttons, which made one key mean two things - the volume where the line was and the next button beside it -
    /// so the level moved into a question of its own, where left and right can only be the level. The dialog is
    /// the shared <see cref="VolumePicker"/>, which the compact transport row at the foot of every other screen
    /// raises the same way: one answer about one volume, two doors to it.
    /// </summary>
    private void OnVolume(object? sender, RoutedEventArgs e)
    {
      if (_tracksOpen)
        CloseTrackDialog();

      volumePicker.HoldAbove(osdArea.Bounds.Height);
      volumePicker.Open(_viewModel);
    }

    /// <summary>Send the sound's dialog back, and give the loudspeaker mark the standing back.</summary>
    private void CloseVolumeDialog()
    {
      volumePicker.Close();
    }

    /// <summary>Whether the dialog of the sound is standing over the film.</summary>
    public bool VolumeDialogOpen
    {
      get { return volumePicker.IsOpen; }
    }

    /// <summary>
    /// Take one step back over the film: the list of tracks first, then the line of the sound, then the row of
    /// buttons. Answers whether it put anything away, which is how the back key knows not to leave the screen at
    /// the same time and how a right click knows there was nothing to dismiss.
    /// </summary>
    private bool StepBackOneLayer()
    {
      if (_tracksOpen)
      {
        CloseTrackDialog();
        return true;
      }

      if (volumePicker.IsOpen)
      {
        CloseVolumeDialog();
        return true;
      }

      if (_osdVisible)
      {
        CloseOsd();
        return true;
      }

      return false;
    }

    // ---------------------------------------------------------------------------------------------
    // Walking the panel by rows, and nodding it against the screen's top and bottom edges.
    //
    // The shared ring (DpadNavigation) walks its controls as one line and reads Down as "the next one", which on
    // a panel of three rows is the button beside rather than the one below. The vertical keys are taken back here
    // for the span where the caret stands on the panel: they move a row up or down, onto whichever control of
    // that row sits most directly under (or over) the one being left, and where there is no row beyond the top or
    // bottom the whole panel nods against that edge and springs back - the same answer the home rails give a key
    // pointed at a wall, so a viewer can tell the end of the panel from a key nobody answered.
    // ---------------------------------------------------------------------------------------------

    /// <summary>How far the panel leans against an edge, and the lean and spring in milliseconds - the rail's own numbers.</summary>
    private const double OSD_BOUNCE_DIVE = 22;
    private const int OSD_BOUNCE_DIVE_MS = 90;
    private const int OSD_BOUNCE_SPRING_MS = 430;

    /// <summary>
    /// The direction the panel last nodded against an edge, in points: positive leaning off the bottom, negative
    /// off the top, zero when it rests. The rail's nod is read the same way by its tests and the render probe; this
    /// is the panel's equivalent, so a test can tell a nod against an edge from a key that did nothing at all.
    /// </summary>
    internal double OsdBounceDiveY { get; private set; }

    /// <summary>The horizontal mate of <see cref="OsdBounceDiveY"/>: positive leaning off the right, negative off the left.</summary>
    internal double OsdBounceDiveX { get; private set; }

    /// <summary>The same two answers for the list of tunes, which nods on its own beside the panel.</summary>
    internal double TunesBounceDiveY { get; private set; }

    internal double TunesBounceDiveX { get; private set; }

    /// <summary>Whether <paramref name="element"/> is the panel itself or stands inside it.</summary>
    private bool IsWithinOsd(Visual element)
    {
      for (Visual? at = element; at != null; at = at.GetVisualParent())
        if (ReferenceEquals(at, osdArea))
          return true;
      return false;
    }

    // ---------------------------------------------------------------------------------------------
    // Walking the list of tunes beside the panel.
    //
    // The list is not part of the panel - it belongs to the audio arrangement and stays drawn while the panel is
    // away - so the shared ring and the row walker above both walk past it. These few methods are its side of the
    // bargain: the transport hands the caret across to it at its right end and back at its left, its rows are
    // climbed up and down, and a key aimed at the wall beside it nods the list itself, which is the same answer
    // the panel gives and the only one that tells a viewer the walk ended here rather than went nowhere.
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Whether the caret stood inside the list of tunes the last time that list was taken away to be painted over,
    /// and on which of its tunes. Both are read at that one moment - <see cref="OnTunesLeaving"/> - because it is
    /// the last moment at which the caret is still standing where it stands: taking the rows away detaches them,
    /// and a detached row answers "no focus anywhere", which cannot be told apart from a caret that was never in
    /// the list. Together they are the answer to one question and nothing else: whose row to stand the caret on
    /// again once the new window of tunes is down.
    /// </summary>
    private bool _caretStoodInTunes;

    /// <summary>The tune the caret stood on, or null while it stood in the list but not on one of its rows.</summary>
    private AudioQueueRow? _tuneCaret;

    /// <summary>Whether <paramref name="element"/> is the list of tunes itself or stands inside it.</summary>
    private bool IsWithinTunes(Visual element)
    {
      for (Visual? at = element; at != null; at = at.GetVisualParent())
        if (ReferenceEquals(at, audioQueue))
          return true;
      return false;
    }

    /// <summary>Whether the list of tunes is drawn, which is the only case in which a key can walk into it.</summary>
    private bool TunesStanding()
    {
      return audioQueue.IsVisible;
    }

    /// <summary>
    /// The rows the list shows, in the order they stand: the buttons the list's own template realized. A panel of
    /// tunes is not named row by row from the code behind - the list makes its rows out of the view model - so
    /// they are read back off the list here instead of being kept in a second place nobody checks. The container
    /// the list wraps a template in is not focusable, so it is the button inside it the caret has to be put on.
    /// </summary>
    private List<Control> TuneRows()
    {
      List<Control> rows = new List<Control>();
      foreach (object? realized in audioQueueRows.GetRealizedContainers())
      {
        if (realized is not Visual container)
          continue;
        Button? row = RowOf(container);
        if (row != null)
          rows.Add(row);
      }
      return rows;
    }

    /// <summary>
    /// The row's own button, looked for under the container the list wrapped the template in. Written as a walk
    /// rather than a query because that is how this screen reads its own tree everywhere else.
    /// </summary>
    private static Button? RowOf(Visual container)
    {
      if (container is Button row)
        return row;

      foreach (Visual child in container.GetVisualChildren())
      {
        Button? found = RowOf(child);
        if (found != null)
          return found;
      }

      return null;
    }

    /// <summary>
    /// Stand the caret on a row of the tune list. A row offers no place for a caret until this moment - see the note
    /// on the row in NowPlayingView.axaml, why the list may not take the window's first focus - so the row is opened
    /// here, the one place that answers a key with a step into the list.
    /// </summary>
    private static void StandOnTune(Control row)
    {
      row.Focusable = true;
      row.Focus();
    }

    /// <summary>
    /// The row the list drawn from one particular tune, or null while no row of it stands on the screen. Asked of
    /// the rows themselves rather than of the tunes the list is showing, because while a window of tunes is being
    /// painted over, the rows still standing are the ones of the old window: the tune asked for has no row yet,
    /// which is a different answer from "its row is the third one".
    /// </summary>
    private Control? RowOfTune(AudioQueueRow tune)
    {
      foreach (Control row in TuneRows())
        if (ReferenceEquals(row.DataContext, tune))
          return row;

      return null;
    }

    /// <summary>
    /// Stand the caret on the row of one particular tune, which is the row of the list drawn from that tune.
    /// </summary>
    /// <returns>Whether a row of that tune was standing on the screen to be stood on.</returns>
    private bool StandOnRowOf(AudioQueueRow tune)
    {
      Control? row = RowOfTune(tune);
      if (row == null)
        return false;

      StandOnTune(row);
      return true;
    }

    /// <summary>
    /// Put the caret on the tune that is playing: whoever steps sideways out of the transport wants to be standing
    /// at the tune the room is hearing, not at whichever end the list happens to show. A list in which no row says
    /// it plays - a record whose tune was opened elsewhere, before this screen knew about it - is answered with its
    /// first row, because a key that came looking for the player is not to go unanswered.
    ///
    /// Browsing may have rolled the window of rows clean off the tune that plays, and that tune still outranks
    /// wherever browsing left the window: the window is called back to the player, and the caret is owed to that
    /// tune the moment its row is down.
    /// </summary>
    private void FocusCurrentTune()
    {
      AudioQueueRow? plays = _viewModel.TuneThatPlays;

      if (plays != null && RowOfTune(plays) == null)
      {
        _viewModel.ReturnTunesToThePlayer();
        StandOnTuneSoon(plays);
        return;
      }

      _tuneCaret = plays;
      StandOnTheTuneThatPlays();
    }

    /// <summary>
    /// Stand the caret on whichever row says it plays, and on the head row of a list in which none does.
    /// </summary>
    private void StandOnTheTuneThatPlays()
    {
      AudioQueueRow? plays = _viewModel.TuneThatPlays;
      if (plays != null && StandOnRowOf(plays))
        return;

      foreach (AudioQueueRow row in _viewModel.AudioQueueRows)
        if (row.IsCurrent && StandOnRowOf(row))
          return;

      List<Control> rows = TuneRows();
      if (rows.Count > 0)
        StandOnTune(rows[0]);
    }

    /// <summary>
    /// The rows beside the equalizer are about to be taken away and painted over with the window of tunes the
    /// player stands in, which the queue does by itself every time a tune runs out. This is the last moment at
    /// which the caret can be asked where it stands - see the two fields this writes - and it is asked at no
    /// other time.
    /// </summary>
    private void OnTunesLeaving(object? sender, EventArgs e)
    {
      _caretStoodInTunes = audioQueue.IsKeyboardFocusWithin;
      _tuneCaret = _caretStoodInTunes ? TuneUnderCaret() : null;
    }

    /// <summary>
    /// The tune the row under the caret is showing, or null while the caret stands somewhere in the list - its
    /// head, say - but not on one of its rows.
    /// </summary>
    private AudioQueueRow? TuneUnderCaret()
    {
      foreach (Control row in TuneRows())
        if (row.IsKeyboardFocusWithin && row.DataContext is AudioQueueRow tune)
          return tune;

      return null;
    }

    /// <summary>
    /// The new window of tunes is down, and the old rows took the caret standing on one of them with them. A caret
    /// that walks off over a repaint is worse than one that was never there - it is where the next key lands - so
    /// it is put down again in the same order of preference the list itself is built in: the tune it stood on,
    /// while that tune is still on the screen, and otherwise the tune that plays, which is the one tune the list
    /// always has room for. A caret that stood outside the list stays where it stands; repainting the list is
    /// nobody's business out by the loudspeaker.
    /// </summary>
    private void OnTunesRepainted(object? sender, EventArgs e)
    {
      if (!_caretStoodInTunes)
        return;

      _caretStoodInTunes = false;
      GiveTheCaretItsTuneSoon();
    }

    /// <summary>
    /// Put the caret back onto the tune it was owed to - now, or one beat later, which is when the rows of a
    /// window that was just painted over are buttons again. Where the beat still brings no row of that tune, the
    /// list stopped showing it for good - the record walked past it - and the caret stands on the tune that plays,
    /// which is the one tune a panel beside a player always has room for.
    /// </summary>
    private void GiveTheCaretItsTuneSoon()
    {
      if (FollowCaretOntoTheTunes())
        return;

      Dispatcher.UIThread.Post(delegate
      {
        if (!FollowCaretOntoTheTunes())
        {
          _tuneCaret = null;
          StandOnTheTuneThatPlays();
        }
      }, DispatcherPriority.Loaded);
    }

    /// <returns>
    /// Whether the caret is answered: stood on the tune it was owed to, or - while the list owes it nothing - on
    /// the tune that plays. False while nothing of the list is down to stand on, which is what the beat above is
    /// for, and false while the tune it owes the caret to has no row among the ones down.
    /// </returns>
    private bool FollowCaretOntoTheTunes()
    {
      if (_viewModel.AudioQueueRows.Count == 0)
        return true; // The record left the screen - stopped, or a film opened over it. No row is left to answer with.

      AudioQueueRow? owed = _tuneCaret;
      if (owed != null)
        return StandOnRowOf(owed);

      if (TuneRows().Count == 0)
        return false;

      StandOnTheTuneThatPlays();
      return true;
    }

    /// <summary>
    /// What the four keys answer while the caret stands in the list of tunes: up and down climb the rows, left
    /// steps back onto the transport, and right - the direction the list holds nothing in - nods the list against
    /// its own edge. Up and down do not stop at the rows that are drawn: the rows are a window out of the record,
    /// so a key at the top of the window rolls earlier tunes in and a key at its bottom rolls later ones in,
    /// which is the only way a record longer than its panel can be walked to any of its tunes. The two ends of
    /// the <em>record</em> answer differently, because they are not the same place: above its first tune there is
    /// nothing, and the key nods the list against its own edge - which is how the beginning of a record is felt as
    /// a beginning rather than as a key somebody dropped - while under its last tune the choices along the foot's
    /// right hand stand, and the key steps onto them (see StepDownOutOfTunes).
    /// </summary>
    private void MoveTunes(Key key)
    {
      List<Control> rows = TuneRows();
      int at = -1;
      for (int i = 0; i < rows.Count; i++)
        if (rows[i].IsKeyboardFocusWithin)
        {
          at = i;
          break;
        }

      if (at < 0)
      {
        // The caret is inside the list but on none of its rows - on its heading, say. The tune playing is where a
        // key pressed here belongs, rather than the key going unanswered.
        FocusCurrentTune();
        return;
      }

      switch (key)
      {
        case Key.Up:
          if (at > 0)
            StandOnTune(rows[at - 1]);
          else if (!RollTunes(-1))
            BounceTunes(0, -1);
          break;

        case Key.Down:
          if (at + 1 < rows.Count)
            StandOnTune(rows[at + 1]);
          else
            StepDownOutOfTunes(rows[at]);
          break;

        case Key.Right:
          BounceTunes(1, 0);
          break;

        case Key.Left:
          BackToTransport();
          break;
      }
    }

    /// <summary>
    /// Roll the window of rows the list shows, which is what a key at one of its ends does while the record runs
    /// further in that direction: the rows are a window out of the record and the record is the list, so the walk
    /// goes on into tunes that were not drawn a moment ago. The caret is handed to the tune that came in at that
    /// edge, which is the tune the key was aimed at.
    /// </summary>
    /// <returns>
    /// Whether the record had anything further in that direction to roll in. Where it had nothing - above its
    /// first tune - the key is the list's own edge to answer, and where it had nothing below its last tune the
    /// choices standing under the list are the next thing on the screen.
    /// </returns>
    private bool RollTunes(int rows)
    {
      AudioQueueRow? came = _viewModel.ScrollTunes(rows);
      if (came == null)
        return false;

      StandOnTuneSoon(came);
      return true;
    }

    /// <summary>
    /// What the wheel answers over the list of tunes, which is the same thing the two arrow keys answer and for the
    /// same reason: the rows are a window out of the record, and a record longer than its panel can only be read by
    /// rolling that window.
    ///
    /// Where the caret stands decides what a turn costs, because the two cases are not the same viewer doing the
    /// same thing. With the caret <em>in</em> the list the turn is the arrow key it points at - the caret walks a
    /// tune, and at either end of the window the walk rolls the window with it - since a caret left standing on a
    /// tune the window rolled away from is a caret the screen has quietly taken away. With the caret standing out
    /// by the loudspeaker a turn rolls the window alone and leaves the caret where it stands: reading through a
    /// record with the wheel is nobody's business out there, and it must not drag the caret into the list.
    /// </summary>
    private void OnTunesWheel(object? sender, PointerWheelEventArgs e)
    {
      if (e.Delta.Y == 0 || !audioQueue.IsEffectivelyVisible)
        return;

      Key towards = e.Delta.Y < 0 ? Key.Down : Key.Up;
      if (audioQueue.IsKeyboardFocusWithin)
        MoveTunes(towards);
      else
        _viewModel.ScrollTunes(e.Delta.Y < 0 ? 1 : -1);

      e.Handled = true;
    }

    /// <summary>
    /// Hand the caret to one particular tune of the list - now, or the moment its row is down. Rolling the window
    /// takes the rows away and paints others in their place, and how quickly those rows are buttons again is the
    /// list's own business, so the caret is owed to the tune rather than pushed at whatever is standing.
    /// </summary>
    private void StandOnTuneSoon(AudioQueueRow tune)
    {
      _tuneCaret = tune;
      _caretStoodInTunes = true;
      GiveTheCaretItsTuneSoon();
    }

    /// <summary>
    /// Step out of the list of tunes back onto the row of buttons it stands beside. The panel is raised first if
    /// the viewer had let it fall asleep, because the caret is about to stand on it and a caret nobody can see is
    /// worse than no caret at all.
    /// </summary>
    private void BackToTransport()
    {
      if (!_osdVisible)
        SetOsdVisible(true);

      List<List<Control>> rows = OsdRows();
      if (rows.Count == 0 || rows[0].Count == 0)
        return;

      List<Control> transport = rows[0];
      transport[transport.Count - 1].Focus();
    }

    /// <summary>
    /// Step out of the bottom end of the list of tunes - or on into the record, which is what that key mostly
    /// means: the rows are a window out of the record, so the first thing asked here is whether the record has a
    /// tune below the window, and if it has the window rolls and the caret stands on the tune that came in
    /// (<see cref="RollTunes"/>). Only under the last tune of the record is the bottom of the list its end, and
    /// then the step goes on to what stands under it.
    ///
    /// The list is not the last thing on the screen: its foot stands on the line the cover's foot stands on, two
    /// hundred and forty two lines above the floor, and the loudspeaker, the full screen and the settings stand at
    /// the foot's right hand under that foot (measured at a screen as tall as the picture: their middles at one
    /// thousand and nineteen lines, the list's foot at eight hundred and thirty eight). The way up from those
    /// choices into the list is already answered above, so this is the same door walked the other way, and the
    /// caret lands on whichever of them the row it left stands most over - the row and the choices are compared by
    /// the place on the screen a viewer sees them, for the same reason <see cref="AcrossTheScreen"/> exists. The
    /// panel is raised first if the viewer had let it fall asleep, because a caret nobody can see is worse than no
    /// caret at all. Only where nothing of the panel stands under the list does the list nod at its own end, which
    /// is the answer a list with nothing under it owes.
    /// </summary>
    private void StepDownOutOfTunes(Control left)
    {
      if (RollTunes(1))
        return;

      if (!_osdVisible)
        SetOsdVisible(true);

      double? across = AcrossTheScreen(left);
      double? tunesAcross = AcrossTheScreen(audioQueue);
      List<List<Control>> rows = OsdRows();
      Control? below = null;
      double best = double.MaxValue;

      if (across.HasValue && tunesAcross.HasValue)
      {
        foreach (List<Control> line in rows)
        {
          foreach (Control control in line)
          {
            // Only the controls standing under the list - at or right of its middle, the same border the upward
            // key uses - are a step down from it. Everything left of that border is beside the list, and the way
            // sideways out of it is the transport's.
            double? at = AcrossTheScreen(control);
            if (!at.HasValue || at.Value < tunesAcross.Value)
              continue;

            double distance = Math.Abs(at.Value - across.Value);
            if (distance < best)
            {
              best = distance;
              below = control;
            }
          }
        }
      }

      if (below == null)
      {
        BounceTunes(0, 1);
        return;
      }

      // Out of the list and onto a control standing under it. The repaint that follows a tune taking care of the
      // caret only ever answers a caret that is still inside the list, so nothing has to be unwound here.
      below.Focus();
    }

    // ---------------------------------------------------------------------------------------------
    // How many rows of the tune list the screen has room for.
    //
    // The list stands with its foot a fixed distance above the floor and may grow up to a limit near the top of
    // the screen; between those two lines it wears only the height its rows need. The numbers below are the ones
    // the panel is built with in NowPlayingView.axaml: the Border's bottom margin of two hundred and forty two
    // lines, the top its own bounds may not go over, the heading of sixty six, the row of fifty two lines plus its
    // two lines of margin above and below, the items' fourteen lines of bottom margin and the panel's two lines of
    // rim.
    //
    // The two lines are the two the viewer drew round the panel: its head under the top of the screen, and its
    // foot on the line the cover's foot stands on - which is why the panel is not tied to the equalizer's foot any
    // more, since the bars end eighty lines higher and the list was shorter than the room it had been given.
    // Nothing was squeezed for it: the rows and the head of the panel are the size they were.
    //
    // A screen as tall as the picture leaves room for twelve rows, a window of seven hundred and twenty for five,
    // and the tunes that do not fit are a key away down the list rather than painted over the top of the screen.
    // The record keeps all its tunes either way - the head of the panel still counts them.
    // ---------------------------------------------------------------------------------------------

    private const double TUNE_LIST_FOOT = 242;
    private const double TUNE_LIST_TOP = 76;
    private const double TUNE_LIST_HEADING = 66;
    private const double TUNE_LIST_ROW = 56;
    private const double TUNE_LIST_OTHER = 16;

    /// <summary>
    /// Hand the list the number of rows the current height has room for. Said again on every resize; the view
    /// model ignores the answer when nothing changed, so a window being dragged about does not rewrite the list
    /// row by row.
    /// </summary>
    private void GiveTheTunesTheirRoom()
    {
      double height = Bounds.Height;
      if (height <= 0)
        return;

      int rows = (int) ((height - TUNE_LIST_FOOT - TUNE_LIST_TOP - TUNE_LIST_HEADING - TUNE_LIST_OTHER)
          / TUNE_LIST_ROW);
      _viewModel.GiveTunesRoom(rows);
    }

    /// <summary>
    /// The panel's controls grouped into the rows they stand in, top row first, each row left to right, with the
    /// controls that are not drawn or cannot take the focus walked over. An empty row is left out, so a film with
    /// nothing to seek does not park a phantom row between the transport and the choices.
    /// </summary>
    private List<List<Control>> OsdRows()
    {
      List<Control> top = StandingAmong(shuffleButton, chapterBackButton, backButton, tuneBackButton,
          pauseButton, stopButton, tuneForwardButton, forwardButton, chapterForwardButton, repeatButton);
      List<Control> middle = StandingAmong(seekBar);
      List<Control> bottom = StandingAmong(subtitleButton, audioButton, volumeButton, fullscreenButton, settingsButton);

      List<List<Control>> rows = new List<List<Control>>();
      if (top.Count > 0)
        rows.Add(top);
      if (middle.Count > 0)
        rows.Add(middle);
      if (bottom.Count > 0)
        rows.Add(bottom);
      return rows;
    }

    private static List<Control> StandingAmong(params Control[] controls)
    {
      List<Control> standing = new List<Control>();
      foreach (Control control in controls)
        if (CanStand(control))
          standing.Add(control);
      return standing;
    }

    /// <summary>
    /// Whether the caret can be put on this control now: drawn, enabled and focusable. The effective answers are
    /// the ones that matter - a button inside the hidden panel is not somewhere to stand - matching what the ring
    /// already uses to decide which areas it may walk into.
    /// </summary>
    private static bool CanStand(Control control)
    {
      if (!control.IsEffectivelyEnabled || !control.Focusable)
        return false;
      for (Visual? at = control; at != null; at = at.GetVisualParent())
        if (!at.IsVisible)
          return false;
      return true;
    }

    /// <summary>
    /// Which way a vertical key moves: the control of <paramref name="row"/> whose middle stands nearest to
    /// <paramref name="centerX"/>, so stepping down from a button lands under the button and not at a row's end.
    /// </summary>
    private static Control? NearestInRow(List<Control> row, double centerX)
    {
      Control? nearest = null;
      double best = double.MaxValue;
      foreach (Control control in row)
      {
        double distance = Math.Abs(control.Bounds.Center.X - centerX);
        if (distance < best)
        {
          best = distance;
          nearest = control;
        }
      }
      return nearest;
    }

    /// <summary>
    /// Move the caret one row up (-1) or down (+1), landing on the control most under the one left; where there
    /// is no such row, nod the panel against that edge instead.
    /// </summary>
    private void MoveVertical(int downward)
    {
      Visual? stood = (TopLevel.GetTopLevel(this) as Window)?.FocusManager.GetFocusedElement() as Visual;
      List<List<Control>> rows = OsdRows();

      int row = -1;
      Control? left = null;
      for (int r = 0; r < rows.Count && row < 0; r++)
        for (int i = 0; i < rows[r].Count; i++)
          if (rows[r][i].IsKeyboardFocusWithin)
          {
            row = r;
            left = rows[r][i];
            break;
          }

      if (row < 0)
        return; // the caret stands on none of the panel's controls; the key is the ring's to answer

      double centerX = (stood ?? left)?.Bounds.Center.X ?? 0;

      // Above the right-hand end of the panel there is exactly one thing: the list of tunes, which stands over
      // the panel's own rows and reaches further right than any button. So a viewer climbing out of the bottom
      // row's right-hand choices - the loudspeaker, the full screen, the settings - steps into the list rather
      // than onto the position line under nothing. The list's own middle line is the border, which keeps the
      // transport buttons (all of which stand left of it) walking the panel as they always did. Measured in the
      // screen's own lines, because a button's Bounds are reckoned inside its own row and the list does not
      // share that row's parent.
      double? acrossTheScreen = AcrossTheScreen(stood ?? left);
      double? tunesAcrossTheScreen = AcrossTheScreen(audioQueue);
      if (downward < 0 && TunesStanding() && acrossTheScreen.HasValue && tunesAcrossTheScreen.HasValue
          && acrossTheScreen.Value >= tunesAcrossTheScreen.Value)
      {
        FocusCurrentTune();
        return;
      }

      int toward = row + downward;
      if (toward < 0 || toward >= rows.Count)
      {
        BounceOsd(0, downward);
        return;
      }

      Control? move = NearestInRow(rows[toward], centerX);
      move?.Focus();
    }

    /// <summary>
    /// How far across the screen a control stands, in the screen's own lines and at its middle. A control's
    /// <c>Bounds</c> are reckoned inside its own parent, and the panel's rows, the panel and the list of tunes do
    /// not share one parent, so two controls in different parts of the screen cannot be compared by their numbers
    /// alone - only by the place on the screen a viewer sees. Null for a control that was never laid out.
    /// </summary>
    private double? AcrossTheScreen(Visual? control)
    {
      if (control == null || control.Bounds.Width <= 0)
        return null;

      return control.TranslatePoint(
          new Avalonia.Point(control.Bounds.Width / 2, control.Bounds.Height / 2), this)?.X;
    }

    /// <summary>
    /// Move the caret one control left (-1) or right (+1) along its own row, running to the end of the row and no
    /// further; where the row has no control that way, nod the panel against that side edge instead. This is the
    /// panel walking itself as the rows it is built from rather than as one long line, so a right key never wraps
    /// from one row into another.
    /// </summary>
    private void MoveHorizontal(int rightward)
    {
      List<List<Control>> rows = OsdRows();

      int row = -1;
      int index = -1;
      for (int r = 0; r < rows.Count && row < 0; r++)
        for (int i = 0; i < rows[r].Count; i++)
          if (rows[r][i].IsKeyboardFocusWithin)
          {
            row = r;
            index = i;
            break;
          }

      if (row < 0)
        return; // the caret stands on none of the panel's controls; the key is the ring's to answer

      List<Control> line = rows[row];
      int toward = index + rightward;
      if (toward >= line.Count && rightward > 0 && row == 0 && TunesStanding())
      {
        // Past the last button of the transport row there is exactly one thing further right, and it is not
        // another button: the list of tunes stands beside the row, so the row walks into it. Every other row is
        // answerless that way and nods instead - the position line hands its keys back before ever getting here.
        FocusCurrentTune();
        return;
      }

      if (toward < 0 || toward >= line.Count)
      {
        BounceOsd(rightward, 0);
        return;
      }

      line[toward].Focus();
    }

    /// <summary>
    /// Nod the whole panel toward an edge of the screen and spring it back - the rail's nod (MediaBrowserView's
    /// <c>BounceRail</c>): ninety milliseconds of lean squared off so the first frames name the edge at once, and
    /// four hundred and thirty of easeOutBack home, which carries the panel a tenth of the lean past its rest before
    /// it settles, which is what makes it a bounce and not a slide. The lean is along whichever axis the key asked.
    /// </summary>
    /// <param name="directionX">+1 to lean off the right edge, -1 off the left, 0 for a vertical nod.</param>
    /// <param name="directionY">+1 to lean off the bottom edge, -1 off the top, 0 for a sideways nod.</param>
    private void BounceOsd(int directionX, int directionY)
    {
      NodPanel(_osdNod, osdArea, directionX, directionY);
      OsdBounceDiveX = _osdNod.DiveX;
      OsdBounceDiveY = _osdNod.DiveY;
    }

    /// <summary>
    /// The same nod for the list of tunes, which stands beside the panel rather than inside it and therefore
    /// leans on its own: a key aimed at a wall gets the same answer from the list the transport gives from
    /// itself, and the list is the thing that has to say "there is nothing further over here".
    /// </summary>
    private void BounceTunes(int directionX, int directionY)
    {
      NodPanel(_tunesNod, audioQueue, directionX, directionY);
      TunesBounceDiveX = _tunesNod.DiveX;
      TunesBounceDiveY = _tunesNod.DiveY;
    }

    /// <summary>
    /// The moving parts of one nod: whichever panel is leaning leans by its own transform and is driven by its
    /// own timer, so the transport and the list of tunes can nod at the same time without interrupting each
    /// other. The numbers of the last lean are kept on it so that a test can read what a key was answered with.
    /// </summary>
    private sealed class PanelNod
    {
      internal TranslateTransform? Transform;
      internal DispatcherTimer? Timer;
      internal double DiveX;
      internal double DiveY;
    }

    private readonly PanelNod _osdNod = new PanelNod();
    private readonly PanelNod _tunesNod = new PanelNod();

    /// <summary>
    /// Lean <paramref name="panel"/> toward an edge and spring it back. One nod at a time per panel; a key
    /// pressed while the panel is still travelling starts a fresh lean from wherever it stands, so a viewer
    /// leaning on the arrow sees the panel lean with every press.
    /// </summary>
    private static void NodPanel(PanelNod nod, Control panel, int directionX, int directionY)
    {
      nod.Timer?.Stop();

      TranslateTransform transform = panel.RenderTransform as TranslateTransform;
      if (transform == null)
      {
        transform = new TranslateTransform();
        panel.RenderTransform = transform;
      }
      nod.Transform = transform;

      double diveX = directionX * OSD_BOUNCE_DIVE;
      double diveY = directionY * OSD_BOUNCE_DIVE;
      nod.DiveX = diveX;
      nod.DiveY = diveY;
      double fromX = transform.X;
      double fromY = transform.Y;
      long started = Environment.TickCount64;

      DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render)
      {
        Interval = TimeSpan.FromMilliseconds(16)
      };
      nod.Timer = timer;

      timer.Tick += delegate
      {
        if (!ReferenceEquals(nod.Timer, timer))
          return; // a newer nod drives the panel from here on

        long elapsed = Environment.TickCount64 - started;
        if (elapsed >= OSD_BOUNCE_DIVE_MS + OSD_BOUNCE_SPRING_MS)
        {
          timer.Stop();
          transform.X = 0;
          transform.Y = 0;
          return;
        }

        if (elapsed <= OSD_BOUNCE_DIVE_MS)
        {
          double u = elapsed / (double)OSD_BOUNCE_DIVE_MS;
          double lean = 1 - (1 - u) * (1 - u);
          transform.X = fromX + (diveX - fromX) * lean;
          transform.Y = fromY + (diveY - fromY) * lean;
          return;
        }

        double v = (elapsed - OSD_BOUNCE_DIVE_MS) / (double)OSD_BOUNCE_SPRING_MS;
        const double c1 = 1.70158;
        const double c3 = c1 + 1;
        double back = 1 + c3 * Math.Pow(v - 1, 3) + c1 * Math.Pow(v - 1, 2);
        transform.X = diveX * (1 - back);
        transform.Y = diveY * (1 - back);
      };

      timer.Start();
    }

    /// <summary>
    /// Put the sound at a fraction of the line. The line itself, its knob, its drag and its keys stand in the
    /// sound's own control (Views/VolumePicker.axaml), which the compact transport row at the foot of every
    /// other screen wears as well; this is the door the room's own keys and the tests walk through.
    /// </summary>
    public void SetVolumeAtFraction(double fraction)
    {
      volumePicker.SetVolumeAtFraction(fraction);
    }

    /// <summary>
    /// Standing on the position line, left and right walk the film along it - one instant jump each, the same
    /// jump the remote makes while the film lies alone on the screen. The line is a control precisely so that a
    /// remote can stand on it: a position line a viewer cannot reach without a mouse is only half a control.
    /// </summary>
    private void OnSeekBarKey(object? sender, KeyEventArgs args)
    {
      if (args.Key != Key.Left && args.Key != Key.Right)
        return;

      long jump = _viewModel.InstantSkipMs(args.Key == Key.Right ? 1 : -1);
      CancelSkipStep();
      _viewModel.Skip(jump);
      Say("Superlayer_SkipStep", SkipWords(jump), 1200);
      args.Handled = true;
    }

    /// <summary>
    /// The playback answers - how far one jump reaches, how much one volume key moves, which languages to reach
    /// for - are a screen of their own already. The panel opens them over the film and the way back is the same
    /// key that gets a viewer out of anything else.
    /// </summary>
    private void OnSettings(object? sender, RoutedEventArgs e)
    {
      _workflow?.Push(new WorkflowFrame("playbacksettings", "Playback settings",
          new PlaybackSettingsViewModel()));
    }

    private void OnSubtitles(object? sender, RoutedEventArgs e)
    {
      OpenTrackDialog(TrackKind.Subtitle);
    }

    private void OnAudio(object? sender, RoutedEventArgs e)
    {
      OpenTrackDialog(TrackKind.Audio);
    }

    /// <summary>
    /// Raise the list of one kind of track over the middle of the film: every subtitle the media carries, with
    /// "no subtitles" at the head of them, or every sound. The row the film is already on is lit, because a
    /// viewer who opens a list to change it needs to know what they are changing - which is the complaint the
    /// single-button subtitle cycling used to answer badly.
    /// </summary>
    private void OpenTrackDialog(TrackKind kind)
    {
      if (volumePicker.IsOpen)
        CloseVolumeDialog();

      _viewModel.ReadTracks();

      bool audio = kind == TrackKind.Audio;
      IReadOnlyList<PlaybackTrack> tracks = audio ? _viewModel.AudioTracks : _viewModel.SubtitleTracks;
      int current = audio ? _viewModel.CurrentAudioTrackId : _viewModel.CurrentSubtitleTrackId;

      trackDialogTitle.Text = audio ? "Which sound to hear" : "Which subtitles to show, or none";
      trackDialogRows.Children.Clear();
      foreach (PlaybackTrack track in tracks)
      {
        bool isCurrent = track.Id == current && track.Kind == kind;
        // The label is the row's own text, so a reader reading the screen aloud gets the whole row from it; the
        // row already differs by light alone for the one in play, which is why it also carries the word
        // "playing" in its text when a remote has no colour to show with.
        Button row = new Button
        {
          Content = isCurrent ? track.Label + "  •  playing" : track.Label,
          Classes = { "choice" }
        };
        if (isCurrent)
          row.Classes.Add("current");

        PlaybackTrack picked = track;
        row.Click += delegate
        {
          _viewModel.SelectTrack(picked);
          CloseTrackDialog();
        };
        trackDialogRows.Children.Add(row);
      }

      _tracksOpen = true;
      trackDialog.IsVisible = true;
      if (trackDialogRows.Children.Count > 0)
        trackDialogRows.Children[0].Focus();
    }

    private void CloseTrackDialog()
    {
      _tracksOpen = false;
      trackDialog.IsVisible = false;
    }

    /// <summary>
    /// Call the media info over the picture, or send it back. Showing it also asks the library once - the
    /// reading is the view model's and survives this panel being closed and opened again.
    /// </summary>
    private void ToggleMediaInfo()
    {
      mediaInfo.IsVisible = !mediaInfo.IsVisible;
      if (mediaInfo.IsVisible)
        _ = _viewModel.ReadMediaInfoAsync();
    }

    /// <summary>
    /// While this screen is on its window it follows the playback far enough to ask for the cover - once when
    /// opened, and after every change of the player the same guarded question, which the model answers without
    /// a second read for the same title.
    /// </summary>
    private void OnFollowingPlayback()
    {
      _viewModel.Changed += OnPlaybackChanged;
      // The record's list beside the equalizer paints itself over whenever the player walks on, and the caret
      // standing among its rows has to be read before that and answered after it.
      _viewModel.AudioQueueRowsLeaving += OnTunesLeaving;
      _viewModel.AudioQueueRowsChanged += OnTunesRepainted;
      _ = _viewModel.ReadCoverAsync();
      // The record a song came off, asked the same guarded way as the cover: the audio arrangement lays it behind
      // the cover and turns it, and a library that keeps no picture of the disc shows the skin's own record.
      _ = _viewModel.ReadDiscAsync();
      // The picture the stage of a song stands on, asked the same guarded way and only ever for a song: a film
      // already shows its own pictures, and painting its fanart under them would only cost a decode.
      _ = _viewModel.ReadFanartAsync();
      // The lettering and the year are asked for as soon as the screen follows a film, not only when the viewer
      // calls the info panel: the lettering stands by the transport and the year under the name from the first
      // moment the panel is up. Both are guarded to ask a title once.
      _ = _viewModel.ReadClearArtAsync();
      _ = _viewModel.ReadMediaInfoAsync();

      // The two jump marks carry their reach in the name drawn under them - "Rewind 20 %" - which the view model
      // rewrites whenever the settings change, so nothing is written onto the marks here. Writing a text onto a
      // mark used to be how the reach was shown, and it replaced the mark's own plate and glyph with a word.
      LayTheStageForTheMedium();
      RestartOpenFade();
    }

    /// <summary>
    /// Replay the reference's opening fade: black over the picture, gone within half a second. Set back to
    /// black and released on the next dispatcher beat, because a transition only runs between two renders.
    /// </summary>
    private void RestartOpenFade()
    {
      openFade.Opacity = 1;
      if (openFade.Transitions == null)
      {
        openFade.Transitions = new Avalonia.Animation.Transitions
        {
          new Avalonia.Animation.DoubleTransition
          {
            Property = OpacityProperty,
            Duration = TimeSpan.FromSeconds(0.5)
          }
        };
      }
      Dispatcher.UIThread.Post(delegate { openFade.Opacity = 0; }, DispatcherPriority.Loaded);
    }

    /// <summary>The reference's PlayingInfoButton, as a button: the title's reading, called over
    /// the picture and sent back again.</summary>
    private void OnMediaInfo(object? sender, RoutedEventArgs e)
    {
      ToggleMediaInfo();
    }

    private void OnStoppingPlayback()
    {
      _viewModel.Changed -= OnPlaybackChanged;
      _viewModel.AudioQueueRowsLeaving -= OnTunesLeaving;
      _viewModel.AudioQueueRowsChanged -= OnTunesRepainted;

      // The record stops with the visit. The song carries on - this screen leaving is not the listener stopping
      // - but nothing here is left turning over a picture nobody is looking at.
      _discClock?.Stop();
      _discClock = null;
      _discDegrees = _discTurn.Angle;
    }

    private void OnPlaybackChanged(object? sender, EventArgs e)
    {
      if (_viewModel.HasMedia)
      {
        _ = _viewModel.ReadCoverAsync();
        _ = _viewModel.ReadClearArtAsync();
        _ = _viewModel.ReadDiscAsync();
        _ = _viewModel.ReadMediaInfoAsync();
        // The stage's picture belongs to the tune, not to the visit: opening the next one wipes every picture
        // the library had said, so this has to ask again here and not only when the screen started following the
        // playback. Guarded as every one of these reads is, so the tune that is already asked for is not asked
        // twice and a film is never asked at all.
        _ = _viewModel.ReadFanartAsync();
      }
      if (_osdVisible)
      {
        // The playhead has to follow the film, and the line of the sound follows whatever the number has become
        // - by a key in the room, by the dialog, or by another screen's buttons.
        PaintMeasurements();
      }

      // Which of the two arrangements the screen wears, whether the record behind the cover is turning, and
      // whether the row of buttons still carries its own band of shade: all three follow the same one answer -
      // is this music - and the answer can change while the screen is open, because the library's reply about a
      // file that named no shelf arrives after the file has been opened.
      LayTheStageForTheMedium();
    }

    /// <summary>
    /// Put the screen in the dress its medium asks for. A song wears the arrangement of its own - the shade that
    /// rises over the whole stage rather than the band under the row - and its record turns only while the engine
    /// says it is playing, because a record that turns over a paused song says the machine is doing something it
    /// stopped doing.
    /// </summary>
    private void LayTheStageForTheMedium()
    {
      bool audio = _viewModel.IsAudio;

      // The bars are only worth listening for while the stage actually wears the audio arrangement. Listening
      // costs a second look at the sound, and a film's screen - which shows the film and has no bars - must never
      // pay for them; the ask and the stop are both idle when nothing is to do.
      if (audio)
        _viewModel.WatchSpectrum();
      else
        _viewModel.StopWatchingSpectrum();

      IBrush? wanted = audio ? null : _osdShade;
      if (!ReferenceEquals(osdArea.Background, wanted))
        osdArea.Background = wanted;

      // The mark that says "this song is standing still" is the one thing on a black stage a viewer has to see
      // from across the room, and the hundred lines it is drawn with over a film - where a paused picture is
      // plainly a paused picture - is a speck here. The audio screen's own mark is two hundred and fifty, at the
      // same strength the mockup drew it.
      double mark = audio ? 250 : 100;
      if (Math.Abs(stateGlyph.Width - mark) > 0.5)
      {
        stateGlyph.Width = mark;
        stateGlyph.Height = mark;
        stateGlyph.Opacity = audio ? 0.7 : 0.8;
      }

      bool turning = audio && _viewModel.IsPlaying;
      if (turning == (_discClock != null))
        return;

      if (turning)
      {
        _discTurnFrom = DateTime.UtcNow;
        _discClock = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _discClock.Tick += delegate
        {
          _discTurn.Angle = (_discDegrees
              + (DateTime.UtcNow - _discTurnFrom).TotalSeconds * DISC_DEGREES_PER_SECOND) % 360.0;
        };
        _discClock.Start();
        return;
      }

      // Stood still where it stood, rather than jumped back to the mark it started from.
      _discClock!.Stop();
      _discClock = null;
      _discDegrees = _discTurn.Angle;
    }

    /// <summary>
    /// How wide the name of a song and the name under it may be: the room between where they stand and where the
    /// switches at the right hand end of the row begin, less the air between them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The mockup caps the two names at five hundred and sixty lines (<c>.nenne { max-width:560px }</c>,
    /// index.html:190) and drew them with a two-hundred-line word, so the cap was never met by the string a library
    /// actually writes: a running library names one of its tracks "7: 1 - Don't Leave Me Alone (feat. Anne-Marie)",
    /// which is measured above at seven hundred and thirty-eight lines, and the frame that cap produced ended the
    /// name with an ellipsis in the middle of a word.
    /// </para>
    /// <para>
    /// Measured off the switches themselves rather than answered with a bigger number, because the room a name has
    /// is exactly what is left of the row - it changes with the width of the window and with the two lists a film's
    /// row carries and a song's row does not, and a name that reached into a knob would be worse than a name that
    /// was trimmed. A name is never made narrower than one of the row's glass plates, since a name that short says
    /// nothing either.
    /// </para>
    /// </remarks>
    private void RoomForTheSongName()
    {
      if (rowSwitches.Bounds.Width <= 0)
        return;

      // Both edges asked of the row, because the row is inset in the screen and the two controls live in different
      // parents inside it.
      Avalonia.Point? nameAt = audioName.TranslatePoint(new Avalonia.Point(0, 0), osdArea);
      Avalonia.Point? switchAt = rowSwitches.TranslatePoint(new Avalonia.Point(0, 0), osdArea);
      if (nameAt == null || switchAt == null)
        return;

      double room = switchAt.Value.X - nameAt.Value.X - SONG_NAME_AIR;
      double wanted = Math.Max(SONG_NAME_FLOOR, room);
      if (Math.Abs(audioName.MaxWidth - wanted) > 0.5)
        audioName.MaxWidth = wanted;
    }

    /// <summary>
    /// What a jump is called on the screen: the arrow points where the film went, and the seconds say how far.
    /// </summary>
    private static string SkipWords(long jumpMs)
    {
      long seconds = Math.Abs(jumpMs) / 1000;
      string distance = seconds + (seconds == 1 ? " second" : " seconds");
      return jumpMs < 0 ? "« " + distance : distance + " »";
    }

    /// <summary>
    /// Have the window's superlayer say one thing for a moment in the worn skin's look - the skip the film
    /// just made, the volume the buttons just set. Without a skin the layers do not exist and this says
    /// nothing, which is the same honesty the header and footer keep; the screen keeps no second copy of its
    /// own, so there is nothing that could disagree with the skin.
    /// </summary>
    private void Say(string templateKey, string text, int autoHideMs)
    {
      (TopLevel.GetTopLevel(this) as ShellWindow)?.ShowSuperlayer(templateKey, text, autoHideMs);
    }

    /// <summary>
    /// Stops the drawing of the frames.  The playback itself belongs to the application and carries on; the next
    /// showing of this screen follows the same frames again.
    /// </summary>
    public void OnScreenClosed()
    {
      _skipStepTimer?.Stop();
      HideStateIcon();
      videoSurface.Dispose();
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
      // Leaving the playback screen leaves the film running; when this screen is all the window holds there is
      // nothing to pop, and the window itself goes. Back reached the panel first and the list before that, so
      // by the time it gets here the viewer means to leave the film to itself.
      LeaveScreen();
    }
  }
}
