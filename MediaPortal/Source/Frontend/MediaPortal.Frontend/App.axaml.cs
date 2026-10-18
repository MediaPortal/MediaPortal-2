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
using System.Globalization;
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
    private readonly string? _playFile;
    private readonly int _playSeconds;
    private NowPlayingViewModel? _nowPlaying;
    private NowPlayingWindow? _playWindow;

    /// <summary>
    /// The pictures the player was asked to hand over, for as long as the application lives. It belongs here and
    /// not to a window because the film outlives the screen it was watched on: close the playback screen, open it
    /// again, and the same frames are still arriving at the same place.
    /// </summary>
    private VideoFrameSink? _frames;

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
    /// <param name="playFile">
    /// A file to start playing as the application opens, skipping the library: <c>--play</c>. The screen is the
    /// normal playback screen with the normal transport, so what a person sees here is what they get from the
    /// library - which matters, because it is the only way a film can be judged by eye without first building a
    /// database.
    /// </param>
    /// <param name="playSeconds">
    /// How long to stay open when playing from the command line, 0 to stay open until somebody closes the window.
    /// A number here turns the run into a measurement: on leaving it prints how many frames arrived and the exit
    /// code says whether the picture was real, which is what a gate needs and no amount of looking at a window
    /// gives you.
    /// </param>
    public App(HomeViewModel home, IVideoPlayer? player = null, ServerSettings? settings = null,
        string? settingsPath = null, bool serverMayBeChanged = true, bool startAtConnectionScreen = false,
        string? playFile = null, int playSeconds = 0)
    {
      _home = home;
      _player = player;
      _settings = settings;
      _settingsPath = settingsPath;
      _serverMayBeChanged = serverMayBeChanged;
      _startAtConnectionScreen = startAtConnectionScreen && serverMayBeChanged;
      _playFile = playFile;
      _playSeconds = playSeconds;
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

          // Ask for the pictures now, before anything is open: an engine decides how it will deliver them when a
          // media starts, so a sink offered afterwards is a question asked after the answer. Refusing is not a
          // reason to stop the front end - the film still plays, and its sound still arrives - but it is a reason
          // to say so where a person will read it.
          VideoFrameSink sink = new VideoFrameSink();
          if (_nowPlaying.AttachVideo(sink))
            _frames = sink;
          else
          {
            Console.WriteLine("video:    no picture - "
                + (_player!.LastFailure ?? "this player hands out no frames"));
          }
        }

        if (_playFile != null && _nowPlaying != null)
          desktop.MainWindow = BuildPlaybackScreen(desktop);
        else
        {
          desktop.MainWindow = _startAtConnectionScreen
              ? BuildConnectionScreen(desktop)
              : BuildHomeScreen(desktop);
        }
      }

      base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// The playback screen, opened straight onto a file named on the command line.
    /// </summary>
    /// <remarks>
    /// This is the same screen the library opens, with the same transport under it; the only difference is that
    /// nobody had to browse anything to get here. That is deliberate: a film watched through a side door would
    /// prove nothing about the door people actually use.
    /// </remarks>
    private Window BuildPlaybackScreen(IClassicDesktopStyleApplicationLifetime desktop)
    {
      NowPlayingWindow window = new NowPlayingWindow(_nowPlaying!, _frames);
      _playWindow = window;

      if (!_nowPlaying!.Play(MediaLocation.FromFile(_playFile!)))
        Console.WriteLine("play:     refused - " + _nowPlaying.StatusLine);
      else
        Console.WriteLine("play:     " + System.IO.Path.GetFileName(_playFile));

      if (_playSeconds > 0)
      {
        // A run that knows when it is allowed to end is a run a gate can use. The number it leaves behind is not
        // "the window existed" but "this many pictures of this many pixels reached it", which is the only part of
        // a moving image an automated run can honestly claim.
        DispatcherTimer clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_playSeconds) };
        clock.Tick += delegate
        {
          clock.Stop();
          ReportPlayback();
          desktop.Shutdown((_playWindow?.FramesDrawn ?? 0L) > 0 ? 0 : 1);
        };
        clock.Start();
      }

      return window;
    }

    /// <summary>
    /// The two lines a timed <c>--play</c> leaves on the console: what the player delivered, and what the screen
    /// made of it. They are two lines because those are two different things that can fail separately - a player
    /// that decodes into a sink nobody reads and a screen that reads a sink nothing ever filled look the same from
    /// the outside and nothing like the same thing from here.
    /// </summary>
    private void ReportPlayback()
    {
      long drawn = _playWindow?.FramesDrawn ?? 0L;
      long delivered = _frames?.FramesArrived ?? 0L;
      PixelSize? size = _playWindow?.PictureSize;

      // The buffer size is named too when it differs, because "1920x804" is the answer to "did I get the film?"
      // and "1920x832" is the answer to "did I get the film and the padding around it?", and the difference between
      // the two is a stretch nobody asked for.
      string padding = "";
      if (size.HasValue && _frames != null && (_frames.Width != size.Value.Width
          || _frames.Height != size.Value.Height))
      {
        padding = " (from a " + _frames.Width + "x" + _frames.Height + " buffer)";
      }

      Console.WriteLine("video:    " + (drawn > 0 && size.HasValue
          ? drawn.ToString("N0", CultureInfo.InvariantCulture) + " frame(s) of " + size!.Value.Width + "x"
            + size.Value.Height + " drawn" + padding
          : "no frame reached the screen")
          + ", " + delivered.ToString("N0", CultureInfo.InvariantCulture) + " delivered by the player");
      Console.WriteLine("picture:  " + DescribePicture(_frames));
      if (_nowPlaying != null)
        Console.WriteLine("played:   " + _nowPlaying.PositionText + " (" + _nowPlaying.StateText + ")");
    }

    /// <summary>
    /// What the newest picture is made of: how bright it is and how many different colours it holds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A frame counter alone cannot tell a film from a black rectangle, and a black rectangle is exactly what a
    /// video path that half works produces - bytes arrive on time, the decoder is happy, and what it decoded is
    /// not what a person asked to see. Two numbers say the difference: mean luminance, and the count of distinct
    /// colours, where a test card is tens, a black frame is one, and a film scene is hundreds or thousands.
    /// </para>
    /// <para>
    /// Every eighth pixel of every fourth row is looked at, and colours are counted after being rounded to five
    /// bits a component, which is coarse on purpose: a compressed film is dithered, and an exact count would
    /// report ten thousand colours for a shot of a dark corridor and mean nothing by it.
    /// </para>
    /// </remarks>
    private static string DescribePicture(VideoFrameSink? sink)
    {
      int width = sink?.PictureWidth ?? 0;
      int height = sink?.PictureHeight ?? 0;
      if (sink == null || width <= 0 || height <= 0)
        return "no picture to look at";

      byte[] pixels = new byte[width * 4 * height];
      if (!sink.TryCopyLatest(pixels, width * 4, width, height))
        return "no picture to look at";

      HashSet<int> colours = new HashSet<int>();
      long luminance = 0;
      long sampled = 0;
      for (int y = 0; y < height; y += 4)
      {
        for (int x = 0, at = y * width * 4; x < width; x += 8, at += 8 * 4)
        {
          int blue = pixels[at];
          int green = pixels[at + 1];
          int red = pixels[at + 2];
          // The rounding of luma coefficients a video engineer would use, because the question is "is there
          // something to see", not "what does colour science say".
          luminance += (long)(red * 299 + green * 587 + blue * 114) / 1000;
          colours.Add((red >> 3 << 10) | (green >> 3 << 5) | (blue >> 3));
          sampled++;
        }
      }

      if (sampled == 0)
        return "no picture to look at";

      return "mean luminance " + (luminance / sampled)
          + ", " + colours.Count.ToString("N0", CultureInfo.InvariantCulture)
          + " distinct colour(s) in " + sampled.ToString("N0", CultureInfo.InvariantCulture) + " sampled pixels";
    }

    /// <summary>
    /// The library screen, with the screens it can open built on demand. It fills itself in when it opens, so
    /// nothing here asks the server.
    /// </summary>
    private Window BuildHomeScreen(IClassicDesktopStyleApplicationLifetime desktop)
    {
      return new HomeWindow(_home,
          _nowPlaying == null ? null : delegate { return new NowPlayingWindow(_nowPlaying, _frames); },
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
