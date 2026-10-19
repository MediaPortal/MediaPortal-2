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
    private readonly Func<HomeSection, Window>? _sectionScreen;
    private readonly Func<string, Window>? _screenFactory;
    private readonly HomeViewModel _home;

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
    /// <param name="screenFactory">
    /// How to build the screen a tile asks for by name - <c>"folders"</c> today - or <c>null</c> when this build has
    /// no screens behind the menu at all. A name rather than a factory per tile, because the menu is assembled from
    /// what the server reports and the application is the one that knows what it can build.
    /// </param>
    public HomeWindow(HomeViewModel home, Func<Window>? nowPlayingScreen = null, Func<Window>? connectScreen = null,
        Func<HomeSection, Window>? sectionScreen = null, Func<string, Window>? screenFactory = null)
    {
      DataContext = home;
      _home = home;
      _nowPlayingScreen = nowPlayingScreen;
      _connectScreen = connectScreen;
      _sectionScreen = sectionScreen;
      _screenFactory = screenFactory;
      InitializeComponent();

      // A button that opens nothing is worse than no button, so the playback button is only offered - and only
      // walks with the d-pad - when the application can actually build the screen.
      nowPlayingButton.IsVisible = _nowPlayingScreen != null;
      connectButton.IsVisible = _connectScreen != null;

      // A list of films that cannot be started is worse than no list, so on a machine with no player the strip
      // stays out of the way entirely; the view model still read it, and the aspects below still say what the
      // library holds.
      if (_nowPlayingScreen == null)
        unfinishedPanel.IsVisible = false;

      // The ring is in the order the areas are arranged on the screen: the menu on the left, its content to the
      // right of it, the server's aspect list on the far side, and the buttons underneath. Left/right walk it; each
      // list keeps its own up/down over its items, because it handles those keys before they reach the ring.
      // The menu first: it is where MP2's home screen starts, and where a viewer with a remote expects to land.
      var ring = new List<Control>();
      if (_sectionScreen != null)
        ring.Add(menuList);
      ring.Add(contentList);
      if (_nowPlayingScreen != null)
        ring.Add(unfinishedList);
      ring.Add(aspectsList);
      ring.Add(refreshButton);
      if (_nowPlayingScreen != null)
        ring.Add(nowPlayingButton);
      if (_connectScreen != null)
        ring.Add(connectButton);
      DpadNavigation.Attach(this, ring);

      // The same screen with a mouse on a desk: double-clicking a tile, a title or a watched row sends the
      // Enter that a remote would have sent on that same list, and this window's own handler below - which
      // already knows which lists open what and which rows merely explain something - decides the rest.
      MouseRemote.Attach(this, menuList, unfinishedList, contentList);

      // A vertical list takes left and right for its own selection - Avalonia moves the highlight with every
      // arrow key, not just up and down - so a viewer standing on the menu and pressing Right would only walk
      // their highlight one entry further and the ring would never see the key. Claimed on the window while the
      // key tunnels in, exactly where the connection screen had to learn the same lesson: left and right are how
      // a viewer leaves one of these lists, while up and down stay its own.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key != Key.Left && args.Key != Key.Right)
              return;
            if (!menuList.IsKeyboardFocusWithin && !contentList.IsKeyboardFocusWithin
                && !aspectsList.IsKeyboardFocusWithin && !unfinishedList.IsKeyboardFocusWithin)
              return;

            Control? next = DpadNavigation.NextReachableInRing(ring, args.Key);
            if (next != null && next.Focus())
              args.Handled = true;
          },
          RoutingStrategies.Tunnel);

      // The screen fills itself in when it appears: a home screen that waits for someone to press "Refresh" shows
      // an empty list in front of a server that is answering perfectly, which is the worse of the two answers.
      // The command is the one the button on the screen uses, so opening and pressing run the same code.
      Opened += delegate
      {
        _home.ResumeRequested += OnResumeRequested;
        if (DataContext is HomeViewModel viewModel)
          viewModel.RefreshCommand.Execute(null);

        // The menu takes the focus the moment the screen shows. Every action on this screen is Enter asking
        // "who is focused", and on a machine with no mouse - a television, or a desktop where somebody only
        // dares press keys - a screen that opens with nothing focused answers no key at all until an arrow
        // happens to wake the ring up. The ring starts at the menu; so does the focus.
        menuList.Focus();
      };

      // Same reason the library screen does it: the view model outlives this window, and a handler left on it
      // would have a closed window opening a playback screen from a key press nobody made on it.
      Closed += delegate
      {
        _home.ResumeRequested -= OnResumeRequested;
      };

      // The back key of a remote closes the screen. There is nowhere else to go yet; when the front end has
      // deeper screens this becomes "go up one level".
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key == Key.Escape || args.Key == Key.Back)
              Close();
            else if (args.Key == Key.Enter || args.Key == Key.Return)
            {
              if (menuList.IsKeyboardFocusWithin && menuList.SelectedItem is MainMenuEntry entry)
              {
                // The view model decides whether this entry can be opened and says why not when it cannot; the
                // screen only opens what comes back. A press on "TV" therefore gets the same sentence on screen
                // that the tile already carries, instead of the remote clicking against nothing.
                HomeSection? section = _home.Open(entry);
                if (section != null)
                  OpenSection(section);
                else if (entry.ScreenKey != null)
                  OpenScreen(entry.ScreenKey);
                args.Handled = true;
              }
              else if (unfinishedList.IsKeyboardFocusWithin
                  && unfinishedList.SelectedItem is TitleViewModel unfinished)
              {
                _home.ResumeCommand.Execute(unfinished);
                args.Handled = true;
              }
              else if (contentList.IsKeyboardFocusWithin && contentList.SelectedItem is HomeRow row
                  && row.Title != null)
              {
                // The same path the "left half way" rows use, so a title pressed in one of MP2's lists starts the
                // same way it does anywhere else on this screen - through the view model, which tells the server
                // who is watching. Headings and the lines explaining a missing list swallow nothing: they are in the
                // walk so that the eye has somewhere to stop, not because pressing them could do anything.
                _home.ResumeCommand.Execute(row.Title);
                args.Handled = true;
              }
            }
          },
          RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Opens one tile: the titles of that part of the library, over the home screen.
    /// </summary>
    private void OpenSection(HomeSection section)
    {
      if (_sectionScreen == null)
        return;

      Window screen = _sectionScreen(section);
      if (!screen.IsVisible)
        screen.Show();
      screen.Activate();
    }

    /// <summary>
    /// Opens one of the screens the menu names, "folders" today.
    /// </summary>
    /// <remarks>
    /// Opened over the home screen rather than instead of it, like every other screen here: pressing Back has to
    /// leave the viewer where they were.
    /// </remarks>
    private void OpenScreen(string screenKey)
    {
      if (_screenFactory == null)
        return;

      Window screen = _screenFactory(screenKey);
      if (!screen.IsVisible)
        screen.Show();
      screen.Activate();
    }

    /// <summary>
    /// A row of the "left half way" strip was pressed. The application is the one that starts the film - it owns
    /// the player - so this screen only gets out of the way and shows the playback, the same screen the
    /// "Now playing" button shows.
    /// </summary>
    private void OnResumeRequested(object? sender, TitleViewModel title)
    {
      if (_nowPlayingScreen == null)
        return;

      Window screen = _nowPlayingScreen();
      if (!screen.IsVisible)
        screen.Show();
      screen.Activate();
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
