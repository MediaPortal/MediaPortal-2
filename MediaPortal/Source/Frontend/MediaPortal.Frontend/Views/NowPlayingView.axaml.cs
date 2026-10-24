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

    // What the panel's own dialog and slider need to know about themselves: whether the list of tracks is up,
    // whether the slider is being set by the model rather than by a hand, and how many chapter marks stood on
    // the line last time it was painted.
    private bool _tracksOpen;

    /// <summary> How far one key on the line of the sound moves it - the same five percent the room's own
    /// volume keys move it by, so that the two answers agree. </summary>
    private const int VOLUME_STEP = 5;

    /// <summary> How far the sound's dialog stands from the right edge of the screen, and how far above the row
    /// of buttons it is lifted on top of the height that row takes. </summary>
    private const double VOLUME_DIALOG_SIDE_MARGIN = 48;

    private const double VOLUME_DIALOG_ABOVE_OSD = 24;

    private double _osdKeptHeight;

    private bool _volumeDragging;
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
      // ladder and up and down make one instant jump each - the film alone on the screen answers only skip
      // keys. With the buttons shown, the same keys belong to the d-pad ring and the skip bindings are off,
      // exactly as the reference switches them. Info (the keyboard's I, which is what its key map calls
      // Info) calls the buttons or sends them away; back closes the buttons before it ever closes the
      // screen. The reference shows its buttons automatically for a mouse; this asks its own window, the
      // same portable question the screensaver watch asks.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key == Key.I)
            {
              ToggleOsd();
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Back, args.Key) && _tracksOpen)
            {
              // Back goes back before it leaves: the list of subtitles first, then the display, then the screen.
              CloseTrackDialog();
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Back, args.Key) && _volumeOpen)
            {
              CloseVolumeDialog();
              args.Handled = true;
            }
            else if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Back, args.Key) && _osdVisible)
            {
              CloseOsd();
              args.Handled = true;
            }
            else if (args.Key == Key.MediaStop)
            {
              // Stop is not one of the three rows' buttons - no player puts a stop among play, the sound and the
              // subtitles - but the film still has to be stoppable, and the remote's stop key is the way it
              // always was. The film stops; the screen stays, so a viewer can see that it stopped.
              CancelSkipStep();
              _viewModel.Stop();
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

      AddHandler(PointerMovedEvent,
          delegate
          {
            if (!_osdVisible && !_osdOpenOnDemand && DateTime.UtcNow > _osdMouseSuppressedUntil)
              SetOsdVisible(true);
          },
          handledEventsToo: true);

      // The ring a remote walks, in the order the controls stand: the name of the film at the left of the top
      // row, the transport across the middle of it, the position line under it, and the choices along the
      // bottom. A control that is not drawn is not an area, so a film without a second sound walks straight from
      // the subtitle button to the loudspeaker, and the loop closes on whichever control the viewer could
      // actually reach.
      _ring = new List<Control>
          {
            infoButton, chapterBackButton, backButton, pauseButton, forwardButton, chapterForwardButton,
            seekBar, subtitleButton, audioButton, volumeButton, fullscreenButton, settingsButton
          };
      DpadNavigation.Attach(this, _ring);

      // Holding a skip mark keeps doing what one press does - see RepeatWhileHeld. The volume is not among them:
      // a line of sound follows the hand that is on it, which is a better answer than stepping.
      RepeatWhileHeld(backButton, SkipBack);
      RepeatWhileHeld(forwardButton, SkipForward);

      // The knob stands at a point measured along the position line, so a line that changes width - a window
      // that is resized, a television that is switched to another mode - has to be asked again where that
      // point is.
      progressRow.SizeChanged += delegate { PaintMeasurements(); };

      // The same for the line of the sound, which is a second line with a knob of its own.
      volumeRow.SizeChanged += delegate { PaintVolume(); };

      // The sound's dialog is lifted above the row of buttons rather than left standing over them, and the height
      // that row takes is what decides how far - so a window that changes its size moves the dialog with it.
      osdArea.SizeChanged += delegate { PlaceVolumeDialog(); };

      // The line of the sound answers the hand as well as the key, and it asks for the press with the handled
      // flag honoured: the control it lives in is a button, and a button takes the press for its own click first
      // and marks it done with. Asked the ordinary way, the line would sit there stone deaf to a pointer and
      // only a remote could move the volume.
      volumeControl.AddHandler(PointerPressedEvent, OnVolumePointer, RoutingStrategies.Bubble, true);
      volumeControl.AddHandler(PointerMovedEvent, OnVolumeMoved, RoutingStrategies.Bubble, true);
      volumeControl.AddHandler(PointerCaptureLostEvent, OnVolumeReleased, RoutingStrategies.Bubble, true);

      // The line of the film answers the hand the same way, and it drags as well: a press puts the film where it
      // was pressed and holding the button down walks the film along with the hand, which is what every position
      // bar a viewer has ever held does. Asking for the handled press is not a detail here - the bar is a button,
      // and a button keeps the press for itself.
      seekBar.AddHandler(PointerPressedEvent, OnSeek, RoutingStrategies.Bubble, true);
      seekBar.AddHandler(PointerMovedEvent, OnSeekMoved, RoutingStrategies.Bubble, true);
      seekBar.AddHandler(PointerCaptureLostEvent, OnSeekReleased, RoutingStrategies.Bubble, true);

      // The room's own volume keys - the ones on the remote the television answers - move the sound without ever
      // asking this panel, and the mute mark moves it too. The line follows whatever the number has become.
      _viewModel.PropertyChanged += delegate
      {
        if (_osdVisible)
          PaintVolume();
      };

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
      if (!visible && _volumeOpen)
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

      // The line of the sound, painted the same way as the line of the film: its filled part as wide as the
      // volume is high, and its knob at the end of that. Both are widths the layout has to have settled before
      // they can be set, so they are measured here rather than bound.
      PaintVolume();

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

      // The reference's centred overlay icon belongs to the seeking moment; on this engine the waiting
      // ladder is that moment, so the big half-transparent arrows answer the ladder.
      int pending = _viewModel.PendingSkipStepSeconds;
      if (pending > 0)
        ShowStateIcon("fastforward.png");
      else if (pending < 0)
        ShowStateIcon("fastrewind.png");
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

    /// <summary>Put one of the worn skin's pictures as the big centred state icon, if it carries one.</summary>
    private void ShowStateIcon(string pictureName)
    {
      var bitmap = SkinImageConverter.WornPictures()?.Images.Load(pictureName);
      stateIcon.Source = bitmap;
      stateIcon.IsVisible = bitmap != null;
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

    private void OnMute(object? sender, RoutedEventArgs e)
    {
      _viewModel.ToggleMute();
    }

    /// <summary>
    /// Raise the sound's own dialog over the film. The level used to lie along the foot of the picture among the
    /// buttons, which made one key mean two things - the volume where the line was and the next button beside it -
    /// so the level moved into a question of its own, where left and right can only be the level.
    /// </summary>
    private void OnVolume(object? sender, RoutedEventArgs e)
    {
      if (_tracksOpen)
        CloseTrackDialog();

      PlaceVolumeDialog();
      PaintVolume();
      _volumeOpen = true;
      volumeDialog.IsVisible = true;
      volumeControl.Focus();
    }

    /// <summary>Send the sound's dialog back, and give the loudspeaker mark the standing back.</summary>
    private void CloseVolumeDialog()
    {
      _volumeOpen = false;
      volumeDialog.IsVisible = false;
    }

    private bool _volumeOpen;

    /// <summary>Whether the dialog of the sound is standing over the film.</summary>
    public bool VolumeDialogOpen
    {
      get { return _volumeOpen; }
    }

    /// <summary>One step quieter, the step the viewer set in the playback answers.</summary>
    private void OnVolumeDown(object? sender, RoutedEventArgs e)
    {
      StepVolume(-VOLUME_STEP);
    }

    /// <summary>One step louder.</summary>
    private void OnVolumeUp(object? sender, RoutedEventArgs e)
    {
      StepVolume(VOLUME_STEP);
    }

    /// <summary>
    /// Move the sound by one step and say where it landed, which is what the keys standing on the line and the
    /// two marks at its ends all do.
    /// </summary>
    private void StepVolume(int step)
    {
      _viewModel.SetVolume(_viewModel.Volume + step);
      PaintVolume();
      Say("Superlayer_Volume", "Volume " + _viewModel.Volume + " %", 2000);
    }

    /// <summary>
    /// Where the viewer left the loudspeaker. The line of the sound answers a press and a drag anywhere along it,
    /// the arrow keys while a remote stands on it, and nothing else - the television's own volume keys belong to
    /// the room and reach it from anywhere, which is why this line does not have to answer the wheel as well.
    /// </summary>
    private void OnVolumePointer(object? sender, PointerPressedEventArgs args)
    {
      // The mark itself is not marked handled: the button keeps its own hold on the pointer for the length of the
      // press, which is what brings the moving and the releasing back here from anywhere on the screen.
      _volumeDragging = true;
      SetVolumeWherePointed(args);
    }

    /// <summary>
    /// Dragging along the line while the press still holds moves the sound with the hand.
    /// </summary>
    private void OnVolumeMoved(object? sender, PointerEventArgs args)
    {
      if (!_volumeDragging || !args.GetCurrentPoint(volumeRow).Properties.IsLeftButtonPressed)
        return;

      SetVolumeWherePointed(args);
    }

    /// <summary>
    /// The hand left the line. The reading of the new volume goes up now rather than on every pixel of the drag,
    /// because a number that flickers eighty times across the screen while a hand moves says nothing.
    /// </summary>
    private void OnVolumeReleased(object? sender, PointerCaptureLostEventArgs args)
    {
      if (!_volumeDragging)
        return;

      _volumeDragging = false;
      Say("Superlayer_Volume", "Volume " + _viewModel.Volume + " %", 2000);
    }

    /// <summary>
    /// Put the sound where the hand pointed, and follow the hand while it moves. The track, its filled part and
    /// the knob are all out of the way of the pointer, so the whole height of the line belongs to this: a viewer
    /// who aims at a third of the line means a third, not "one step quieter". The line stands upright, so the
    /// hand is read from its foot upward.
    /// </summary>
    private void SetVolumeWherePointed(PointerEventArgs args)
    {
      double height = volumeRow.Bounds.Height;
      if (height < 8)
        return;

      SetVolumeAtFraction(1.0 - args.GetPosition(volumeRow).Y / height);
    }

    /// <summary>
    /// Put the sound at a fraction of the line: nothing of it for a press at its foot, all of it for a press at
    /// its head. Asked for by the hand through <see cref="SetVolumeWherePointed"/>, and asked for directly by
    /// the tests, which can hand over a fraction but not a place on a screen.
    /// </summary>
    public void SetVolumeAtFraction(double fraction)
    {
      if (volumeRow.Bounds.Height < 8)
        return;

      if (fraction < 0)
        fraction = 0;
      if (fraction > 1)
        fraction = 1;

      _viewModel.SetVolume((int)Math.Round(fraction * 100));
      PaintVolume();
    }

    /// <summary>
    /// Standing on the line of the sound, the keys across it mean the sound: left takes the volume down, right
    /// brings it up, by the same five percent the television's own volume keys move it. Up and down are left
    /// alone - they belong to the ring, and a control that claimed all four keys would be a trap a remote could
    /// only leave by guessing.
    /// </summary>
    private void OnVolumeKey(object? sender, KeyEventArgs args)
    {
      int step;
      switch (args.Key)
      {
        case Key.Left:
          step = -VOLUME_STEP;
          break;
        case Key.Right:
          step = VOLUME_STEP;
          break;
        default:
          return;
      }

      StepVolume(step);
      args.Handled = true;
    }

    /// <summary>
    /// Lay the filled part of the sound's line and its knob where the sound stands. The line is upright, so both
    /// grow from its foot: the filled part is as tall as the sound is high, and the knob stands at the head of
    /// that. Asked for whenever the number moved - by a hand on the line, a key standing on it, or the room's
    /// own volume keys - and whenever the line was laid out again.
    /// </summary>
    private void PaintVolume()
    {
      double height = volumeRow.Bounds.Height;
      if (height < 8)
        return;

      int volume = Math.Clamp(_viewModel.Volume, 0, 100);
      double at = height * volume / 100.0;
      volumePlayed.Height = at;

      double knob = volumeKnob.Bounds.Height;
      double fromFoot = at - knob / 2;
      if (fromFoot < 0)
        fromFoot = 0;
      if (fromFoot > height - knob)
        fromFoot = height - knob;
      // the knob hangs from the foot of its line by that distance, which is what its bottom margin means to a
      // panel that lays its children from the top down
      volumeKnob.Margin = new Thickness(0, 0, 0, fromFoot);
    }

    /// <summary>
    /// Stand the sound's dialog clear of the row of buttons rather than over them: how much room the row takes
    /// is a height only the layout knows, so the dialog is lifted by that height and a margin of its own. The
    /// last height the row was seen at is kept, because the row goes away with the buttons while the dialog is
    /// still answering a hand.
    /// </summary>
    private void PlaceVolumeDialog()
    {
      if (osdArea.Bounds.Height > 8)
        _osdKeptHeight = osdArea.Bounds.Height;
      volumeDialog.Margin = new Thickness(VOLUME_DIALOG_SIDE_MARGIN, 0, VOLUME_DIALOG_SIDE_MARGIN,
          _osdKeptHeight + VOLUME_DIALOG_ABOVE_OSD);
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
      if (_volumeOpen)
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
        _ = _viewModel.ReadCoverAsync();
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
      if (_workflow != null && _workflow.Pop())
        return;

      (TopLevel.GetTopLevel(this) as Window)?.Close();
    }
  }
}
