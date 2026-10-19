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
using Avalonia.Input;
using Avalonia.Interactivity;
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
  public partial class NowPlayingView : UserControl, IWorkflowScreen, Workflow.IFullScreenScreen
  {
    /// <summary>
    /// How far the left and right keys of a remote jump. Long enough to be useful over an advertisement, short
    /// enough to press twice by accident without losing the plot.
    /// </summary>
    private const long SkipLengthMs = 30 * 1000;

    /// <summary>
    /// How much one press of a volume key changes the volume, in the player's 0-100 scale.
    /// </summary>
    private const int VolumeStep = 5;

    private readonly NowPlayingViewModel _viewModel;
    private readonly WorkflowManager? _workflow;

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

      // Left and right are the transport's seek keys on every remote, so the screen takes them before the ring
      // gets them: the d-pad only ever moves between the buttons when the button row is the only thing there,
      // and a viewer who presses right expects the film to move rather than the highlight. Up and down are left
      // to the ring, which walks the row.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            long jump = args.Key == Key.Left ? -SkipLengthMs : args.Key == Key.Right ? SkipLengthMs : 0;
            if (jump == 0)
            {
              if (args.Key == Key.I)
              {
                ToggleMediaInfo();
                args.Handled = true;
              }
              return;
            }
            _viewModel.Skip(jump);
            Say("Superlayer_SkipStep", SkipWords(jump), 1200);
            args.Handled = true;
          },
          RoutingStrategies.Tunnel);

      DpadNavigation.Attach(this, new List<Control>
          {
            pauseButton, backButton, forwardButton, stopButton,
            volumeDownButton, volumeUpButton, muteButton, closeButton,
            audioButton, subtitleButton
          });

      // Back is the shell's key now: it pops this screen without changing anything about the playback, and when
      // this screen is all the window holds the shell closes the window - the behaviour <c>--play</c> relies on.
      // Escape and Backspace are the two keys a remote's back button sends, depending on the platform.
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
      _viewModel.Skip(-SkipLengthMs);
      Say("Superlayer_SkipStep", SkipWords(-SkipLengthMs), 1200);
    }

    private void OnForward(object? sender, RoutedEventArgs e)
    {
      _viewModel.Skip(SkipLengthMs);
      Say("Superlayer_SkipStep", SkipWords(SkipLengthMs), 1200);
    }

    private void OnStop(object? sender, RoutedEventArgs e)
    {
      _viewModel.Stop();
    }

    private void OnVolumeDown(object? sender, RoutedEventArgs e)
    {
      _viewModel.SetVolume(_viewModel.Volume - VolumeStep);
      Say("Superlayer_Volume", "Volume " + _viewModel.Volume + " %", 2000);
    }

    private void OnVolumeUp(object? sender, RoutedEventArgs e)
    {
      _viewModel.SetVolume(_viewModel.Volume + VolumeStep);
      Say("Superlayer_Volume", "Volume " + _viewModel.Volume + " %", 2000);
    }

    private void OnMute(object? sender, RoutedEventArgs e)
    {
      _viewModel.ToggleMute();
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

    private void OnNextAudioTrack(object? sender, RoutedEventArgs e)
    {
      _viewModel.NextAudioTrack();
    }

    private void OnNextSubtitleTrack(object? sender, RoutedEventArgs e)
    {
      _viewModel.NextSubtitleTrack();
    }

    /// <summary>
    /// Stops the drawing of the frames.  The playback itself belongs to the application and carries on; the next
    /// showing of this screen follows the same frames again.
    /// </summary>
    public void OnScreenClosed()
    {
      videoSurface.Dispose();
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
      // Leaving the playback screen leaves the film running; when this screen is all the window holds there is
      // nothing to pop, and the window itself goes.
      if (_workflow != null && _workflow.Pop())
        return;

      (TopLevel.GetTopLevel(this) as Window)?.Close();
    }
  }
}
