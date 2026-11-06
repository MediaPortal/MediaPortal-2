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
            infoButton, chapterBackButton, backButton, pauseButton, stopButton, forwardButton,
            chapterForwardButton, seekBar, subtitleButton, audioButton, volumeButton, fullscreenButton,
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
            if (args.Handled || !_osdVisible)
              return;

            bool vertical = args.Key == Key.Up || args.Key == Key.Down;
            bool sideways = args.Key == Key.Left || args.Key == Key.Right;
            if (!vertical && !sideways)
              return;

            // Only while the caret stands on the panel itself. The sound's dialog and a list of tracks are the
            // panel's own answers to a button but sit outside it, and both walk their keys themselves.
            Visual? stood = (TopLevel.GetTopLevel(this) as Window)?.FocusManager.GetFocusedElement() as Visual;
            if (stood == null || !IsWithinOsd(stood))
              return;

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

    private DispatcherTimer? _osdBounceTimer;
    private TranslateTransform? _osdBounceTransform;

    /// <summary>
    /// The direction the panel last nodded against an edge, in points: positive leaning off the bottom, negative
    /// off the top, zero when it rests. The rail's nod is read the same way by its tests and the render probe; this
    /// is the panel's equivalent, so a test can tell a nod against an edge from a key that did nothing at all.
    /// </summary>
    internal double OsdBounceDiveY { get; private set; }

    /// <summary>The horizontal mate of <see cref="OsdBounceDiveY"/>: positive leaning off the right, negative off the left.</summary>
    internal double OsdBounceDiveX { get; private set; }

    /// <summary>Whether <paramref name="element"/> is the panel itself or stands inside it.</summary>
    private bool IsWithinOsd(Visual element)
    {
      for (Visual? at = element; at != null; at = at.GetVisualParent())
        if (ReferenceEquals(at, osdArea))
          return true;
      return false;
    }

    /// <summary>
    /// The panel's controls grouped into the rows they stand in, top row first, each row left to right, with the
    /// controls that are not drawn or cannot take the focus walked over. An empty row is left out, so a film with
    /// nothing to seek does not park a phantom row between the transport and the choices.
    /// </summary>
    private List<List<Control>> OsdRows()
    {
      List<Control> top = StandingAmong(chapterBackButton, backButton, pauseButton, stopButton,
          forwardButton, chapterForwardButton);
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

      int toward = row + downward;
      if (toward < 0 || toward >= rows.Count)
      {
        BounceOsd(0, downward);
        return;
      }

      double centerX = (stood ?? left)?.Bounds.Center.X ?? 0;
      Control? move = NearestInRow(rows[toward], centerX);
      move?.Focus();
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
      // One nod at a time; a key pressed while the panel is still travelling starts a fresh lean from wherever it
      // stands, so a viewer leaning on the arrow sees the panel lean with every press.
      _osdBounceTimer?.Stop();

      TranslateTransform nod = osdArea.RenderTransform as TranslateTransform;
      if (nod == null)
      {
        nod = new TranslateTransform();
        osdArea.RenderTransform = nod;
      }
      _osdBounceTransform = nod;

      double diveX = directionX * OSD_BOUNCE_DIVE;
      double diveY = directionY * OSD_BOUNCE_DIVE;
      double fromX = nod.X;
      double fromY = nod.Y;
      long started = Environment.TickCount64;
      OsdBounceDiveX = diveX;
      OsdBounceDiveY = diveY;

      DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render)
      {
        Interval = TimeSpan.FromMilliseconds(16)
      };
      _osdBounceTimer = timer;

      timer.Tick += delegate
      {
        if (!ReferenceEquals(_osdBounceTimer, timer))
          return; // a newer nod drives the panel from here on

        long elapsed = Environment.TickCount64 - started;
        if (elapsed >= OSD_BOUNCE_DIVE_MS + OSD_BOUNCE_SPRING_MS)
        {
          timer.Stop();
          nod.X = 0;
          nod.Y = 0;
          return;
        }

        if (elapsed <= OSD_BOUNCE_DIVE_MS)
        {
          double u = elapsed / (double)OSD_BOUNCE_DIVE_MS;
          double lean = 1 - (1 - u) * (1 - u);
          nod.X = fromX + (diveX - fromX) * lean;
          nod.Y = fromY + (diveY - fromY) * lean;
          return;
        }

        double v = (elapsed - OSD_BOUNCE_DIVE_MS) / (double)OSD_BOUNCE_SPRING_MS;
        const double c1 = 1.70158;
        const double c3 = c1 + 1;
        double back = 1 + c3 * Math.Pow(v - 1, 3) + c1 * Math.Pow(v - 1, 2);
        nod.X = diveX * (1 - back);
        nod.Y = diveY * (1 - back);
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
      _ = _viewModel.ReadCoverAsync();
      // The lettering and the year are asked for as soon as the screen follows a film, not only when the viewer
      // calls the info panel: the lettering stands by the transport and the year under the name from the first
      // moment the panel is up. Both are guarded to ask a title once.
      _ = _viewModel.ReadClearArtAsync();
      _ = _viewModel.ReadMediaInfoAsync();

      // The two jump marks carry their reach in the name drawn under them - "Rewind 20 %" - which the view model
      // rewrites whenever the settings change, so nothing is written onto the marks here. Writing a text onto a
      // mark used to be how the reach was shown, and it replaced the mark's own plate and glyph with a word.
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
    }

    private void OnPlaybackChanged(object? sender, EventArgs e)
    {
      if (_viewModel.HasMedia)
      {
        _ = _viewModel.ReadCoverAsync();
        _ = _viewModel.ReadClearArtAsync();
        _ = _viewModel.ReadMediaInfoAsync();
      }
      if (_osdVisible)
      {
        // The playhead has to follow the film, and the line of the sound follows whatever the number has become
        // - by a key in the room, by the dialog, or by another screen's buttons.
        PaintMeasurements();
      }
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
