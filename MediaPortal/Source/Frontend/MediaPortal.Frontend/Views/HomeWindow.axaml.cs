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
  /// The screen that shows the server and its media aspects.
  /// </summary>
  public partial class HomeWindow : Window
  {
    private readonly Func<Window>? _nowPlayingScreen;
    private readonly Func<Window>? _connectScreen;

    /// <summary>
    /// For the XAML loader and the designer: a screen with a view model that has no server, which is what it
    /// says on screen.
    /// </summary>
    public HomeWindow()
        : this(new HomeViewModel())
    {
    }

    /// <param name="home">What the screen shows.</param>
    /// <param name="nowPlayingScreen">
    /// How to build the playback screen, or <c>null</c> on a machine with no player. The screen is built by the
    /// application because the playback belongs to the application, not to this window: closing the playback
    /// screen leaves the film running.
    /// </param>
    /// <param name="connectScreen">
    /// How to build the screen that picks the server, or <c>null</c> when the operator has taken that decision away
    /// by starting the front end with an address of its own.
    /// </param>
    public HomeWindow(HomeViewModel home, Func<Window>? nowPlayingScreen = null, Func<Window>? connectScreen = null)
    {
      DataContext = home;
      _nowPlayingScreen = nowPlayingScreen;
      _connectScreen = connectScreen;
      InitializeComponent();

      // A button that opens nothing is worse than no button, so the playback button is only offered - and only
      // walks with the d-pad - when the application can actually build the screen.
      nowPlayingButton.IsVisible = _nowPlayingScreen != null;
      connectButton.IsVisible = _connectScreen != null;

      // The ring is in the order the areas are arranged: the buttons on top, the list below it. Left/right (and
      // up/down between the two areas) walk it; the list keeps its own up/down over its items, because it
      // handles those keys before they reach the ring.
      var ring = new List<Control> { refreshButton };
      if (_nowPlayingScreen != null)
        ring.Add(nowPlayingButton);
      if (_connectScreen != null)
        ring.Add(connectButton);
      ring.Add(aspectsList);
      DpadNavigation.Attach(this, ring);

      // The screen fills itself in when it appears: a home screen that waits for someone to press "Refresh" shows
      // an empty list in front of a server that is answering perfectly, which is the worse of the two answers.
      // The command is the one the button on the screen uses, so opening and pressing run the same code.
      Opened += delegate
      {
        if (DataContext is HomeViewModel viewModel)
          viewModel.RefreshCommand.Execute(null);
      };

      // The back key of a remote closes the screen. There is nowhere else to go yet; when the front end has
      // deeper screens this becomes "go up one level".
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key == Key.Escape || args.Key == Key.Back)
              Close();
          },
          RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Opens the playback screen over the home screen. The window shown is the application's, so a viewer who
    /// goes back to the library and returns finds the same film at the same position.
    /// </summary>
    private void OnNowPlaying(object? sender, RoutedEventArgs e)
    {
      if (_nowPlayingScreen == null)
        return;

      Window screen = _nowPlayingScreen();
      if (!screen.IsVisible)
        screen.Show();
      screen.Activate();
    }

    /// <summary>
    /// Opens the screen that picks the server. Over the library rather than instead of it: a viewer who changes
    /// their mind and presses Back has to be left where they were, with the server they had not replaced still
    /// filling the screen.
    /// </summary>
    private void OnConnect(object? sender, RoutedEventArgs e)
    {
      if (_connectScreen == null)
        return;

      Window screen = _connectScreen();
      if (!screen.IsVisible)
        screen.Show();
      screen.Activate();
    }
  }
}
