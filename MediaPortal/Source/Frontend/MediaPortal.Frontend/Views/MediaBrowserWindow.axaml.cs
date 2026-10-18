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

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;
using System;
using System.Collections.Generic;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The titles of one part of the library, and the way out of them into the player.
  /// </summary>
  public partial class MediaBrowserWindow : Window
  {
    private readonly Func<Window>? _nowPlayingScreen;

    public MediaBrowserWindow()
        : this(new MediaBrowserViewModel(), null)
    {
    }

    /// <param name="browser">The section to show.</param>
    /// <param name="nowPlayingScreen">
    /// How to reach the playback screen, or <c>null</c> on a machine with no player. The application owns the
    /// player, so the screen it shows is built there - a title opened from here and a title opened from the home
    /// screen are the same film at the same position, not two players.
    /// </param>
    public MediaBrowserWindow(MediaBrowserViewModel browser, Func<Window>? nowPlayingScreen = null)
    {
      DataContext = browser;
      _nowPlayingScreen = nowPlayingScreen;
      InitializeComponent();

      playButton.IsVisible = browser.Selected != null;

      var ring = new List<Control> { titlesList };
      if (browser.CanLoadMore)
        ring.Add(moreButton);
      DpadNavigation.Attach(this, ring);

      Opened += delegate
      {
        if (DataContext is MediaBrowserViewModel viewModel)
        {
          viewModel.OpenCommand.Execute(null);
          viewModel.PlayRequested += OnPlayRequested;
        }
      };

      Closed += delegate
      {
        if (DataContext is MediaBrowserViewModel viewModel)
          viewModel.PlayRequested -= OnPlayRequested;
      };

      // OK on the remote is Enter here, and Avalonia hands it to the focused control before anyone else; the
      // list does nothing with it, so it is caught on the way in and turns into "play this", which is what a
      // viewer means by it on a screen whose whole content is a list of things to watch.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key == Key.Escape || args.Key == Key.Back)
            {
              Close();
              args.Handled = true;
            }
            else if (args.Key == Key.Enter || args.Key == Key.Return || args.Key == Key.MediaPlayPause)
            {
              if (DataContext is MediaBrowserViewModel viewModel)
                viewModel.PlayCommand.Execute(null);
              args.Handled = true;
            }
          },
          RoutingStrategies.Tunnel);
    }

    private void OnPlayRequested(object? sender, TitleViewModel title)
    {
      if (_nowPlayingScreen == null)
        return;

      Window screen = _nowPlayingScreen();
      if (!screen.IsVisible)
        screen.Show();
      screen.Activate();
    }
  }
}
