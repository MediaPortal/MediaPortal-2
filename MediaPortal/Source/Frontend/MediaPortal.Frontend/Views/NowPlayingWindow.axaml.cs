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
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;

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
  public partial class NowPlayingWindow : Window
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

    /// <summary>
    /// For the XAML loader: a playback screen with no player behind it, which is what it says on screen.
    /// </summary>
    public NowPlayingWindow()
        : this(new NowPlayingViewModel(null))
    {
    }

    public NowPlayingWindow(NowPlayingViewModel viewModel)
    {
      _viewModel = viewModel;
      DataContext = viewModel;
      InitializeComponent();

      // Left and right are the transport's seek keys on every remote, so the window takes them before the ring
      // gets them: the d-pad only ever moves between the buttons when the button row is the only thing there,
      // and a viewer who presses right expects the film to move rather than the highlight. Up and down are left
      // to the ring, which walks the row.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            long jump = args.Key == Key.Left ? -SkipLengthMs : args.Key == Key.Right ? SkipLengthMs : 0;
            if (jump == 0)
              return;
            _viewModel.Skip(jump);
            args.Handled = true;
          },
          RoutingStrategies.Tunnel);

      DpadNavigation.Attach(this, new List<Control>
          {
            pauseButton, backButton, forwardButton, stopButton,
            volumeDownButton, volumeUpButton, muteButton, closeButton
          });

      // Back closes the screen but changes nothing about the playback. Escape and Backspace are the two keys a
      // remote's back button sends, depending on the platform.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key == Key.Escape || args.Key == Key.Back)
              Close();
          },
          RoutingStrategies.Tunnel);
    }

    private void OnPause(object? sender, RoutedEventArgs e)
    {
      _viewModel.TogglePause();
    }

    private void OnBack(object? sender, RoutedEventArgs e)
    {
      _viewModel.Skip(-SkipLengthMs);
    }

    private void OnForward(object? sender, RoutedEventArgs e)
    {
      _viewModel.Skip(SkipLengthMs);
    }

    private void OnStop(object? sender, RoutedEventArgs e)
    {
      _viewModel.Stop();
    }

    private void OnVolumeDown(object? sender, RoutedEventArgs e)
    {
      _viewModel.SetVolume(_viewModel.Volume - VolumeStep);
    }

    private void OnVolumeUp(object? sender, RoutedEventArgs e)
    {
      _viewModel.SetVolume(_viewModel.Volume + VolumeStep);
    }

    private void OnMute(object? sender, RoutedEventArgs e)
    {
      _viewModel.ToggleMute();
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
      Close();
    }
  }
}
