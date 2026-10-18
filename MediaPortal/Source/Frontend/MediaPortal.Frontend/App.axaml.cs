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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace MediaPortal.Frontend
{
  /// <summary>
  /// The application object: it decides which screen is shown and which server that screen talks to, and does
  /// nothing else.
  /// </summary>
  public partial class App : Application
  {
    private readonly HomeViewModel _home;
    private readonly IVideoPlayer? _player;
    private readonly ServerSettings? _settings;
    private readonly string? _settingsPath;
    private readonly bool _serverMayBeChanged;
    private readonly bool _startAtConnectionScreen;
    private NowPlayingViewModel? _nowPlaying;

    /// <summary>
    /// For the XAML tooling and for a client built without a server to talk to.
    /// </summary>
    public App()
        : this(new HomeViewModel())
    {
    }

    /// <summary>
    /// The constructor the program and the tests use. Handing in the view model rather than building it here is
    /// what lets a headless test show the real screen with a client pointed at a real server.
    /// </summary>
    /// <param name="player">
    /// The player to drive, built by the program because only it can say in one line why there is none. Without
    /// a player the home screen does not offer the playback screen at all, rather than offering one that cannot
    /// play.
    /// </param>
    /// <param name="settings">
    /// What was saved about the server, offered on the screen that chooses it. <c>null</c> is a first run: the
    /// field starts empty.
    /// </param>
    /// <param name="settingsPath">Where choosing a server writes. <c>null</c> is the file the platform picked.</param>
    /// <param name="serverMayBeChanged">
    /// <c>false</c> when the operator started the front end with an address on the command line or in the
    /// environment. Saying "this machine decides, not the television" is friendlier than offering a screen whose
    /// choice is then ignored.
    /// </param>
    /// <param name="startAtConnectionScreen">
    /// <c>true</c> when nothing was configured anywhere, in which case the first thing a viewer sees is the question
    /// "which server?" instead of a library screen that can only answer "no server configured".
    /// </param>
    public App(HomeViewModel home, IVideoPlayer? player = null, ServerSettings? settings = null,
        string? settingsPath = null, bool serverMayBeChanged = true, bool startAtConnectionScreen = false)
    {
      _home = home;
      _player = player;
      _settings = settings;
      _settingsPath = settingsPath;
      _serverMayBeChanged = serverMayBeChanged;
      _startAtConnectionScreen = startAtConnectionScreen && serverMayBeChanged;
    }

    public override void Initialize()
    {
      AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
      if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
      {
        // The playback state belongs to the application and not to a window: closing the playback screen must
        // not stop the film, and opening it again must show the same playback at the same position. It is built
        // here rather than in the program because only now does the dispatcher exist to marshal the engine's
        // events onto - the player raises them on its own thread.
        _nowPlaying = _player == null
            ? null
            : new NowPlayingViewModel(_player, action => Dispatcher.UIThread.Post(action));

        if (_nowPlaying != null)
        {
          // The player goes with it. Without this the engine and its threads stay behind when the last window
          // closes, which a viewer notices as a process that never goes away.
          desktop.Exit += delegate { _nowPlaying.Dispose(); };
        }

        desktop.MainWindow = _startAtConnectionScreen
            ? BuildConnectionScreen(desktop)
            : BuildHomeScreen(desktop);
      }

      base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// The library screen, with the screens it can open built on demand. It fills itself in when it opens, so
    /// nothing here asks the server.
    /// </summary>
    private Window BuildHomeScreen(IClassicDesktopStyleApplicationLifetime desktop)
    {
      return new HomeWindow(_home,
          _nowPlaying == null ? null : delegate { return new NowPlayingWindow(_nowPlaying); },
          _serverMayBeChanged ? delegate { return BuildConnectionScreen(desktop); } : null);
    }

    /// <summary>
    /// The "which server?" screen, wired so that saving an address on it moves the front end to that server.
    /// </summary>
    /// <remarks>
    /// The same screen does two jobs, and which one it was doing is read from the application's own state at the
    /// moment the address is saved: while it is the main window the front end had no server at all, so choosing one
    /// replaces it with the library; when it was opened from the library it is a change of mind, so the library stays
    /// on screen and is asked again. Closing it without saving changes nothing, which is the whole reason there is a
    /// separate screen for it.
    /// </remarks>
    private Window BuildConnectionScreen(IClassicDesktopStyleApplicationLifetime desktop)
    {
      ConnectionViewModel viewModel = new ConnectionViewModel(_settings, _settingsPath);
      ConnectionWindow screen = new ConnectionWindow(viewModel);

      viewModel.Saved += delegate (string url)
      {
        // Confirmed to be a server before the view model allowed the save, so reading the text back cannot fail on
        // anything a viewer could have got to this point.
        _home.UseServer(new ServerApiClient(new Uri(url, UriKind.Absolute)));

        if (ReferenceEquals(desktop.MainWindow, screen))
        {
          desktop.MainWindow = BuildHomeScreen(desktop);
          // Only now: closing it first would close the last window and shut the application down.
          screen.Close();
        }
        else
        {
          // The library is already open, so nothing will ask the new server by itself.
          _home.RefreshCommand.Execute(null);
        }
      };

      return screen;
    }
  }
}
