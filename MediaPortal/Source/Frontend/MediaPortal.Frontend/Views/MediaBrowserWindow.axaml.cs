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
    private readonly Func<TitleViewModel, Window>? _insideScreen;

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
    /// <param name="insideScreen">
    /// How to reach the list of titles inside one title - the tracks of the album that was pressed - or
    /// <c>null</c> on a server that has nothing of that kind. Pressing an album has to lead somewhere, and the
    /// window it leads to belongs to the application for the same reason the playback screen does.
    /// </param>
    public MediaBrowserWindow(MediaBrowserViewModel browser, Func<Window>? nowPlayingScreen = null,
        Func<TitleViewModel, Window>? insideScreen = null)
    {
      DataContext = browser;
      _nowPlayingScreen = nowPlayingScreen;
      _insideScreen = insideScreen;
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
          viewModel.ChildrenRequested += OnChildrenRequested;
        }
      };

      Closed += delegate
      {
        if (DataContext is MediaBrowserViewModel viewModel)
        {
          viewModel.PlayRequested -= OnPlayRequested;
          viewModel.ChildrenRequested -= OnChildrenRequested;
        }
      };

      // OK on the remote is Enter here, and Avalonia hands it to the focused control before anyone else; the
      // list does nothing with it, so it is caught on the way in and turns into "play this", which is what a
      // viewer means by it on a screen whose whole content is a list of things to watch.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (!(DataContext is MediaBrowserViewModel viewModel))
              return;

            if (args.Key == Key.I || args.Key == Key.Apps)
            {
              // The "info" key of a remote, which is the Apps key in everything that reaches this code, plus the
              // letter for a keyboard. It reads about the selected title rather than playing it, which is the one
              // question a title list cannot answer from its own rows.
              viewModel.ToggleDetailsCommand.Execute(null);
              args.Handled = true;
            }
            else if (args.Key == Key.F && !searchBox.IsFocused)
            {
              // Finding is the one thing on this screen that needs the keyboard, so it is the one key that moves
              // the focus out of the list - and Enter or Escape brings it straight back, because the next thing a
              // viewer does with a result is walk through it with the arrows.
              searchBox.Focus();
              args.Handled = true;
            }
            else if (args.Key == Key.W && !searchBox.IsFocused)
            {
              // W for watched: everything, then what nobody has finished, then what somebody has. One key round
              // three states rather than two switches, because a remote reaches its keyboard through one button
              // and a viewer should be able to get back to "everything" without remembering which switch it was.
              viewModel.CycleSeenCommand.Execute(null);
              args.Handled = true;
            }
            else if (searchBox.IsFocused && (args.Key == Key.Enter || args.Key == Key.Return))
            {
              // Enter means different things depending on where the caret is: "play this" on a row, "search for
              // this" in the box. The box gets asked first, or typing a title would start a film.
              viewModel.FindCommand.Execute(null);
              titlesList.Focus();
              args.Handled = true;
            }
            else if (args.Key == Key.Escape || args.Key == Key.Back)
            {
              // Back means "away with the newest thing on the screen", which is the panel when one is open, and
              // the search when there is one. A viewer who pressed back three times expects to be back where they
              // started, not outside the screen.
              if (viewModel.DetailsVisible)
              {
                viewModel.CloseDetails();
                args.Handled = true;
                return;
              }
              if (searchBox.IsFocused || viewModel.HasSearch)
              {
                viewModel.ClearSearchCommand.Execute(null);
                searchBox.Focus();
                args.Handled = true;
                return;
              }
              if (viewModel.HasSeenFilter)
              {
                // The watched filter goes after the search rather than before it: the thing asked for most
                // recently is the thing a viewer means by "back", and a filter set before typing a title is the
                // older of the two.
                viewModel.ClearSeenCommand.Execute(null);
                args.Handled = true;
                return;
              }
              Close();
              args.Handled = true;
            }
            else if (args.Key == Key.Enter || args.Key == Key.Return || args.Key == Key.MediaPlayPause)
            {
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

    /// <summary>
    /// A row that holds other titles was pressed: the titles inside it are opened in their own window over this
    /// one, and this one stays where it was.
    /// </summary>
    /// <remarks>
    /// The album list is left open underneath rather than closed, because "back" on a remote has to arrive back on
    /// the shelf the viewer came from, and a shelf that was closed has to be read from the server again to be
    /// seen. It is the same reason the playback screen is shown over a list instead of instead-of it.
    /// </remarks>
    private void OnChildrenRequested(object? sender, TitleViewModel parent)
    {
      if (_insideScreen == null)
        return;

      Window screen = _insideScreen(parent);
      if (!screen.IsVisible)
        screen.Show();
      screen.Activate();
    }
  }
}
