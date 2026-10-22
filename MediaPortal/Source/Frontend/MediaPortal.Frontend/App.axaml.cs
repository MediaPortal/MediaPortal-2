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
using System.ComponentModel;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Skins.Nereus;
using MediaPortal.Frontend.Theming;
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
    private readonly Workflow.WorkflowManager _workflow = new Workflow.WorkflowManager();
    private NowPlayingViewModel? _nowPlaying;
    private NowPlayingView? _playView;
    private ShellWindow? _shell;
    private MediaBrowserViewModel? _lastBrowser;
    private PartyViewModel? _party;

    /// <summary>
    /// The tuners that are on the screen, the television and the radio each with their own. They are kept here so
    /// that the half somebody did not just touch can be told to step aside when the other one takes the picture:
    /// the reference owned two players and stopped one to start the other
    /// (<c>TV/SlimTvClient/TVHandler/SlimTvHandler.cs:213-221</c>), and with this front end's single player there
    /// is nothing to stop - only a tuner left believing its channel is still on.
    /// </summary>
    private readonly List<ViewModels.TvTunerViewModel> _tuners =
        new List<ViewModels.TvTunerViewModel>();
    private PlaybackReporter? _reporter;
    private readonly string? _shotPath;
    private readonly TimeSpan _playStart;
    private readonly string? _theme;

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
    /// <param name="shotPath">
    /// A file to write a photograph of the playback screen to when a timed <c>--play</c> run ends.
    /// The counters say how many times the screen was asked to draw; this is what it drew, and the two have
    /// disagreed before. A person - or a gate - looking at one PNG cannot be told a black rectangle is a film.
    /// </param>
    /// <param name="playStart">
    /// How far into the film a command-line <c>--play</c> run should begin, from <c>--start</c>.
    /// Zero plays from the beginning. It exists because films open dark - a black title card or a fade from
    /// black tells you nothing about whether the picture path works, so a run that wants to prove it delivered a
    /// picture has to ask for a scene that actually has one.
    /// </param>
    /// <param name="theme">
    /// Which palette to draw with, from <c>--theme</c> or <c>MP2_THEME</c>: one of the two old skins
    /// <see cref="FrontendTheme"/> can reproduce, or the colours this front end has always used. <c>null</c> means
    /// nobody said, which is the default palette.
    /// </param>
    public App(HomeViewModel home, IVideoPlayer? player = null, ServerSettings? settings = null,
        string? settingsPath = null, bool serverMayBeChanged = true, bool startAtConnectionScreen = false,
        string? playFile = null, int playSeconds = 0, string? shotPath = null, TimeSpan playStart = default,
        string? theme = null)
    {
      _home = home;
      _player = player;
      _settings = settings;
      _settingsPath = settingsPath;
      _serverMayBeChanged = serverMayBeChanged;
      _startAtConnectionScreen = startAtConnectionScreen && serverMayBeChanged;
      _playFile = playFile;
      _playSeconds = playSeconds;
      _shotPath = shotPath;
      _playStart = playStart;
      _theme = theme;
    }

    public override void Initialize()
    {
      AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
      // The palette goes on before a single screen exists. Afterwards is too late: a screen that has already drawn
      // keeps the brushes it resolved, so half of it would stay in the old colours and the half that changed would
      // look like a bug in the new one. SelectedName is what makes MP2_THEME work for a device that was started with
      // no command line at all, and what the settings screen saved is its third source: typed outranks exported
      // outranks chosen outranks the factory, because the loudest voice about one run's colours is the one that
      // named them for that run.
      ThemeSettings savedTheme = ThemeSettings.Load(null, out string? savedThemeProblem);
      if (savedThemeProblem != null)
        Console.WriteLine("theme:    " + savedThemeProblem);

      string appliedTheme = FrontendTheme.Apply(this, FrontendTheme.SelectedName(_theme, savedTheme.Theme),
          out string? themeProblem);
      if (themeProblem != null)
        Console.WriteLine("theme:    " + themeProblem);
      else if (!string.Equals(appliedTheme, FrontendTheme.DEFAULT_NAME, StringComparison.Ordinal))
        Console.WriteLine("theme:    " + appliedTheme);

      // The witness line for a machine nobody can see: which folder the skins came from, whether a skin is
      // really worn on top of the palette, how many pictures it brought, and what the merged resources say
      // the accent colour and a tile template are. Every skin mystery on a live television starts here.
      Theming.WornSkin? wornNow = Theming.SkinEngine.Worn(this);
      Console.WriteLine("skin:     folder=" + (Theming.SkinEngine.SkinsFolder ?? "<built into this build>")
          + " worn=" + (wornNow == null ? "<none>"
              : wornNow.Manifest.Name + "/" + wornNow.Theme.Name + " images=" + wornNow.Images.Count
                  // Vectors belong on this line as much as pictures do: a screen whose tiles stand there without
                  // their icons is a screen whose vector catalog came back empty, and that is a fact the startup
                  // log should say out loud rather than one a photograph has to be taken to find.
                  + " vectors=" + wornNow.Vectors.Count));
      // Which of the two copies of a skin the screen is wearing, said separately from which skin it is, because
      // the two copies are the difference between an edit landing and an edit vanishing: a folder read off disk
      // and the copy built into this assembly are different files, and the one not being worn is exactly where
      // whoever is dressing a style file is typing. "<built into this build>" is the normal answer; a folder
      // means somebody named it in MP2_SKINS_DIR and the front end is wearing files that are not in the build.
      Console.WriteLine("skin:     markup=" + (Theming.SkinEngine.SkinsFolder == null
              ? "inside this assembly only - there is no skins folder to read"
              : string.Equals(Theming.SkinEngine.SkinsFolder, Theming.SkinEngine.BuiltInSkinsFolder,
                    StringComparison.OrdinalIgnoreCase)
                  ? "this build's own folder beside its executable, written from the source it was compiled from"
                  : "off disk, from " + Theming.SkinEngine.SkinsFolder + " - named in MP2_SKINS_DIR, so NOT the "
                    + "files this build was compiled from"));
      if (Theming.SkinEngine.SkinsFolderProblem != null)
        Console.WriteLine("skin:     " + Theming.SkinEngine.SkinsFolderProblem);
      if (wornNow != null)
      {
        object? accentNow = null;
        Resources.TryGetResource("AccentBrush", null, out accentNow);
        Console.WriteLine("skin:     accent=" + accentNow
            + " tiletemplates=" + (Theming.SkinEngine.TryTileTemplate("HomeMenuTileTemplate") != null)
            + " rowtemplates=" + (Theming.SkinEngine.TryTileTemplate("HomeContentRowTemplate") != null));
      }

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
          // What the viewer told the player: start volume and languages ride into every media from here,
          // and the skip and volume keys read their reach from the same answer. Read once at start-up -
          // the settings screen saves at once, and the next media picks the new answers up because the
          // player re-reads Preferences on every Play and ReadTracks.
          _nowPlaying.Preferences = Settings.PlaybackSettings.Load(null, out _);

          // And when an answer is saved, the player adopts the very instance that was saved - the settings
          // screen's rows and the keys of the playback screen never disagree about the next media.
          Settings.PlaybackSettings.Changed += delegate (object? sender, EventArgs args)
          {
            if (_nowPlaying != null && sender is Settings.PlaybackSettings saved)
              _nowPlaying.Preferences = saved;
          };
          // The media info asks the current server, whoever it is when the key is pressed - the reader reads
          // the home screen's client at call time rather than holding on to one build's copy.
          _nowPlaying.DetailsReader = delegate (Guid mediaItemId)
          {
            ServerApiClient? client = _home.Client;
            if (client == null)
              throw new InvalidOperationException("there is no server to ask");
            return client.GetItemAsync(mediaItemId);
          };

          // The cover: the album's poster first, its fanart when the importer never knew a poster. No server
          // is an ordinary answer here and says "no picture", not an exception - a song on a machine that
          // streams files needs no complaint over it.
          _nowPlaying.CoverReader = delegate (Guid mediaItemId)
          {
            ServerApiClient? client = _home.Client;
            if (client == null)
              return Task.FromResult<byte[]?>(null);
            return CoverFrom(client, mediaItemId);
          };

          // The player goes with it. Without this the engine and its threads stay behind when the last window
          // closes, which a viewer notices as a process that never goes away.
          desktop.Exit += delegate { _nowPlaying.Dispose(); };

          // Watching something is what a media centre is for, and the library only knows about it if somebody says
          // so. The playback screen is the one place that knows a viewing ended; this is the one place that knows
          // which server to say it to.
          _reporter = new PlaybackReporter(_nowPlaying, delegate { return _home.Client; },
              Console.WriteLine, HeardAboutPlayback);
          desktop.Exit += delegate { _reporter?.Dispose(); };

          // The reference's "close players when finished", shipped on there (MediaModelSettings'
          // DEFAULT_CLOSE_PLAYER_WHEN_FINISHED) and shipped on here: when the film the playback screen was
          // showing really ends, the screen steps aside onto the shelf it was opened from. Only the engine's
          // own ending does this - a viewer who pressed Stop stays exactly where they pressed it, and a film
          // that was merely watched past the mark stays too.
          _nowPlaying.PlaybackReported += delegate (object? sender, PlaybackReportedEventArgs args)
          {
            if (!args.EngineEnded)
              return;
            if (!(_nowPlaying.Preferences ?? Settings.PlaybackSettings.Load(null, out _)).ClosePlayerWhenFinished)
              return;

            Dispatcher.UIThread.Post(delegate
            {
              // Only over something: a playback screen that IS the whole workflow - a file named on the
              // command line - has nothing under it, and "close" there would mean closing the application.
              if (_workflow.Current is Workflow.WorkflowFrame current
                  && current.StateId == "nowplaying" && _workflow.Stack.Count > 1)
                _workflow.Pop();
            });
          };

          // A title picked on the home screen's "left half way" strip is played by exactly the same code as a
          // title picked in the library - same player, same media id handed to the reporter, so finishing it
          // updates the same library the strip read from.
          _home.ResumeRequested += OnTitleChosen;
          desktop.Exit += delegate { _home.ResumeRequested -= OnTitleChosen; };

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

        // What the server says without being asked. The home screen's library otherwise only changes when somebody
        // presses refresh, so an import that finishes while the viewer is in another room leaves the shelf showing
        // yesterday. Like the playback screen, this needs the dispatcher: the stream is read on its own thread and
        // the lists it updates belong to this one.
        _home.WatchServerEvents(action => Dispatcher.UIThread.Post(action));
        desktop.Exit += delegate { _home.StopWatchingEvents(); };

        // One window, always. What it shows - the library, the question about the server, or a file named on the
        // command line - is the first frame of the workflow; every screen the viewer opens from there is a frame
        // pushed over it, not another window standing beside it.
        if (_playFile != null && _nowPlaying != null)
        {
          _shell = BuildPlaybackScreen(desktop);
        }
        else
        {
          _shell = _startAtConnectionScreen
              ? BuildConnectionScreen()
              : BuildHomeScreen();
        }

        // The footer of the master chrome says whatever the player is doing, on every screen - the reference
        // kept its PlayerControl in the master rather than in the playback screen, so a film's title stayed
        // readable from the home shelf and the volume was visible without opening the door it played behind.
        if (_nowPlaying != null)
        {
          _nowPlaying.PropertyChanged += delegate
          {
            Dispatcher.UIThread.Post(FeedChromeFromPlayback);
            Dispatcher.UIThread.Post(FeedHomeFromPlayback);
          };
          FeedChromeFromPlayback();
        }

        // The reference's notification bar heard the server through the master, not through the home screen:
        // whatever the server does at the library is said in the header from every screen, and said away
        // when the server falls silent.
        _home.PropertyChanged += delegate (object? sender, PropertyChangedEventArgs args)
        {
          if (args.PropertyName == nameof(ViewModels.HomeViewModel.ServerActivity))
            _shell?.Chrome.ReportServerActivity(_home.ServerActivity);

          // The skin's own answers belong to whoever is signed in, like they did in the reference: the
          // signed-in name names the skin settings file from this moment on, and every screen that reads
          // them again - which is every screen that reads them at all - reads that person's answers.
          if (args.PropertyName == nameof(ViewModels.HomeViewModel.SignedInAs))
            Settings.SkinSettingsBase.FileOwner = _home.SignedInAs;
        };

        desktop.MainWindow = _shell;

        // The worn skin's own step and click sounds, heard through the one window every screen lives in.
        Theming.SkinSounds.Attach(_shell);
      }

      base.OnFrameworkInitializationCompleted();
    }

    /// <summary>The playback screen's model, or null while there is no player to speak of.</summary>
    public ViewModels.NowPlayingViewModel? Playback
    {
      get { return _nowPlaying; }
    }

    /// <summary>Tell the window's chrome what the player is doing right now - see the wiring above.</summary>
    private void FeedChromeFromPlayback()
    {
      if (_shell == null || _nowPlaying == null)
        return;

      if (_nowPlaying.HasMedia)
        _shell.Chrome.ReportPlaying(_nowPlaying.Title, _nowPlaying.Progress, _nowPlaying.StateText);
      else
        _shell.Chrome.ReportStopped();
    }

    /// <summary>
    /// The aspect of whatever was last started from the library, in the server's spelling, or null when nobody
    /// started anything from a shelf - a <c>--play</c> file names no shelf, and a tile would be guessing.
    /// </summary>
    private string? _playingAspect;

    /// <summary>
    /// Move the home screen's "this shelf is playing" mark along with the player: while media is loaded the tile
    /// of its shelf wears the mark, and when the player falls silent no tile wears it.
    /// </summary>
    private void FeedHomeFromPlayback()
    {
      _home.ShowPlaying(_nowPlaying != null && _nowPlaying.HasMedia ? _playingAspect : null);
    }

    /// <summary>
    /// The playback screen's picture for a title: the poster the importer knew, or the fanart when it did not.
    /// A server that has neither answers null, and the screen plays without a picture like before.
    /// </summary>
    private static async Task<byte[]?> CoverFrom(ServerApiClient client, Guid mediaItemId)
    {
      byte[]? poster = await client.GetFanArtAsync(mediaItemId, "Poster");
      return poster ?? await client.GetFanArtAsync(mediaItemId, "FanArt");
    }

    /// <summary>
    /// The playback screen, opened straight onto a file named on the command line.
    /// </summary>
    /// <remarks>
    /// This is the same screen the library opens, with the same transport under it; the only difference is that
    /// nobody had to browse anything to get here. That is deliberate: a film watched through a side door would
    /// prove nothing about the door people actually use. It is the whole workflow here - one frame - so Back
    /// still means what it means everywhere else, and at the root of the stack it leaves the application.
    /// </remarks>
    private ShellWindow BuildPlaybackScreen(IClassicDesktopStyleApplicationLifetime desktop)
    {
      NowPlayingView view = new NowPlayingView(_nowPlaying!, _frames, _workflow);
      _playView = view;
      ShellWindow shell = new ShellWindow(view);

      if (!_nowPlaying!.Play(MediaLocation.FromFile(_playFile!)))
        Console.WriteLine("play:     refused - " + _nowPlaying.StatusLine);
      else
        Console.WriteLine("play:     " + System.IO.Path.GetFileName(_playFile));

      // A --start is asked for a moment after playing rather than immediately, because the position only exists
      // once the player has read the file - setting it the instant Play returns is the same as asking for the
      // beginning, which the player answers by ignoring it.
      if (_playStart > TimeSpan.Zero && _player != null)
      {
        DispatcherTimer seek = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        seek.Tick += delegate
        {
          seek.Stop();
          try
          {
            _player!.TimeInMs = (long)_playStart.TotalMilliseconds;
            Console.WriteLine("start:    " + _playStart.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)
                + " asked for");
          }
          catch (Exception ex)
          {
            Console.WriteLine("start:    could not be reached - " + ex.Message);
          }
        };
        seek.Start();
      }

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
          SaveShot();
          desktop.Shutdown((_playView?.FramesDrawn ?? 0L) > 0 ? 0 : 1);
        };
        clock.Start();
      }

      return shell;
    }

    /// <summary>
    /// The two lines a timed <c>--play</c> leaves on the console: what the player delivered, and what the screen
    /// made of it. They are two lines because those are two different things that can fail separately - a player
    /// that decodes into a sink nobody reads and a screen that reads a sink nothing ever filled look the same from
    /// the outside and nothing like the same thing from here.
    /// </summary>
    private void ReportPlayback()
    {
      long drawn = _playView?.FramesDrawn ?? 0L;
      long delivered = _frames?.FramesArrived ?? 0L;
      PixelSize? size = _playView?.PictureSize;

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
      Console.WriteLine("layout:   " + DescribeLayout());
      if (_nowPlaying != null)
        Console.WriteLine("played:   " + _nowPlaying.PositionText + " (" + _nowPlaying.StateText + ")");
    }

    /// <summary>
    /// How big the window and the picture area inside it were, in the units Avalonia lays out in.
    /// </summary>
    /// <remarks>
    /// A film that arrives at the sink and never reaches the eyes has exactly two places it can get lost, and
    /// the counters in the other lines cannot tell them apart: either the picture area has no size - a layout
    /// that squeezed it to nothing shows "frames drawn" climbing while the pixels land in a rectangle nobody
    /// can see - or it has a size and the compositor still shows black. This line is what separates those two,
    /// and it is worth four words when the alternative is measuring somebody's desktop and guessing.
    /// </remarks>
    private string DescribeLayout()
    {
      if (_shell == null || _playView == null)
        return "no playback window";

      Rect screen = _playView.SurfaceBounds;
      return "window " + (int)_shell.Bounds.Width + "x" + (int)_shell.Bounds.Height
          + " at " + _shell.Position.X + "," + _shell.Position.Y
          + " scaled " + _shell.RenderScaling.ToString("0.##", CultureInfo.InvariantCulture)
          + ", picture area " + (int)screen.Width + "x" + (int)screen.Height
          + " at " + (int)screen.X + "," + screen.Y
          + (screen.Width < 160 || screen.Height < 90 ? "  <-- too small to be watched" : "");
    }

    /// <summary>
    /// Asks the window for a photograph of what it actually drew and writes it where <c>--shot</c> asked.
    /// </summary>
    /// <remarks>
    /// The window is asked, rather than the desktop photographed from outside: a screen capture of a maximised
    /// window is also a capture of whatever happened to be behind it, other windows are open while tests run,
    /// and a measurement of somebody's editor once looked exactly like a film. What the window says it drew is
    /// the only picture this program can be held to.
    /// </remarks>
    private void SaveShot()
    {
      if (_shotPath == null || _playView == null)
        return;

      // What is saved is the picture the screen was handed, not a photograph of the monitor: a screen capture of
      // a maximised window also captures whatever happened to be behind it, other programs are open while a gate
      // runs, and a measurement of somebody's editor once looked exactly like a film. Together with the `layout:`
      // line - which says the area it was drawn into was a watchable size - this is the picture this program can
      // honestly be held to.
      Console.WriteLine("shot:     " + _playView.SavePictureTo(_shotPath));
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

      return DescribePixels(pixels, width * 4, width, height);
    }

    /// <summary>
    /// The two numbers, from bytes that a screen was given: how bright they are on average, and how many
    /// different colours they hold.
    /// </summary>
    private static string DescribePixels(byte[] pixels, int stride, int width, int height)
    {
      HashSet<int> colours = new HashSet<int>();
      long luminance = 0;
      long sampled = 0;
      for (int y = 0; y < height; y += 4)
      {
        for (int x = 0, at = y * stride; x < width; x += 8, at += 8 * 4)
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
    /// The application's single window, opened with the library at the bottom of the workflow. The screens the
    /// viewer opens from here are frames pushed over it, not windows shown beside it - the arrangement the old
    /// client had and this front end is getting back. The library fills itself in when it opens, so nothing here
    /// asks the server.
    /// </summary>
    private ShellWindow BuildHomeScreen()
    {
      // Leaving a watching screen stops its channel. The reference let it play on - the picture went away and the
      // sound carried on behind the rest of the media centre, because nothing between its workflow states ever
      // stopped the player (Models/SlimTvClientModel.cs:1023-1031 disposes the model and leaves the player
      // alone). That is defensible on a box whose only job is being a television; a machine that is about to play
      // somebody's records should not be argued with by a channel they walked away from.
      _workflow.Removed += delegate (Workflow.WorkflowFrame frame)
      {
        if (frame.ViewModel is TvTunerViewModel tuner)
          StopWatching(tuner);
      };

      _workflow.Push(HomeFrame());

      // Who is watching, answered before the first screen draws: a remembered name owns the skin's answers
      // straight away - the reference's auto-login user, whose per-user settings were simply the settings
      // with that name on the file - and a household that turned the asking on but has answered nothing yet
      // finds the question standing over the library the moment the window opens. An off switch and a
      // "ask again" press live under Settings → Users, where the reference kept the option too.
      Settings.ClientSettings machine = Settings.ClientSettings.Load(null, out _);
      if (machine.RememberedViewer.Length > 0)
        Settings.SkinSettingsBase.FileOwner = machine.RememberedViewer;
      else if (machine.AskForViewer)
        _workflow.Push(ViewerFrame());

      // The wake-up answer "last shelf" opens its shelf the moment the menu first knows whether the
      // library still holds it - a remembered label whose shelf is gone opens the home screen instead,
      // which is the whole fallback, needing no word because the viewer never asked for that shelf today.
      _home.MenuRebuilt += OpenRememberedShelfOnce;

      return new ShellWindow(_workflow, BuildViewFor);
    }

    private void OpenRememberedShelfOnce(object? sender, EventArgs args)
    {
      Settings.ClientSettings settings = Settings.ClientSettings.Load(null, out _);
      if (settings.StartScreen != Settings.ClientSettings.START_LAST_SHELF
          || settings.LastShelf.Length == 0
          || _workflow.Stack.Count > 1)
        return;

      foreach (HomeSection section in _home.Sections)
      {
        if (string.Equals(section.Label, settings.LastShelf, StringComparison.OrdinalIgnoreCase))
        {
          _home.MenuRebuilt -= OpenRememberedShelfOnce;
          _workflow.Push(BrowserFrame(section));
          return;
        }
      }
    }

    /// <summary>The bottom of every way through the front end: the library screen.</summary>
    private Workflow.WorkflowFrame HomeFrame()
    {
      return new Workflow.WorkflowFrame("home", "Home", _home);
    }

    /// <summary>
    /// The question of who is watching, as a frame pushed over the library - the reference pushed its
    /// login screen over the master menu in exactly this way. The answer is the application's business:
    /// the name picked owns the skin's answers from here (and the colours change with it), while "nobody
    /// in particular" leaves them shared; either way the step aside is the same pop.
    /// </summary>
    private Workflow.WorkflowFrame ViewerFrame()
    {
      ViewerPickerViewModel viewModel = new ViewerPickerViewModel(() => _home.Client, ApplyViewer);
      return new Workflow.WorkflowFrame("viewer", "Who is watching?", viewModel);
    }

    /// <summary>
    /// The room's answer. A name makes that person the owner of the skin's own answers and the worn skin's
    /// colours are read anew under their file, the same way a sign-in re-hangs the skin on the signed-in
    /// name; "nobody" keeps the shared file. Either way the question steps aside, because the next start
    /// reads the answer from the client settings rather than asking twice.
    /// </summary>
    private void ApplyViewer(string? name)
    {
      Settings.SkinSettingsBase.FileOwner = name ?? "";
      if (name != null)
        Theming.FrontendTheme.Reapply(Application.Current!);

      if (_workflow.Stack.Count > 1)
        _workflow.Pop();
    }

    /// <summary>
    /// The screen that picks the server, as a frame.  What happens after an address has been chosen is the
    /// application's business: while the screen is the root the library replaces it outright, and when the viewer
    /// came from the library it pops back there and the library is asked again.  Closing it without saving changes
    /// nothing, which is the whole reason there is a separate screen for it.
    /// </summary>
    private Workflow.WorkflowFrame ConnectionFrame()
    {
      ConnectionViewModel viewModel = new ConnectionViewModel(_settings, _settingsPath);
      Workflow.WorkflowFrame frame = new Workflow.WorkflowFrame("connection", "Server", viewModel);

      viewModel.Saved += delegate (string url)
      {
        // Confirmed to be a server before the view model allowed the save, so reading the text back cannot fail on
        // anything a viewer could have got to this point.
        _home.UseServer(new ServerApiClient(new Uri(url, UriKind.Absolute)));

        if (_workflow.Stack.Count > 0 && ReferenceEquals(_workflow.Stack[0], frame))
        {
          // The front end had no server at all until this answer, so the library takes the window over.
          _workflow.ReplaceCurrent(HomeFrame());
        }
        else
        {
          // The viewer came from the library and changed their mind there; back goes back there, asked again.
          _workflow.Pop();
          _home.RefreshCommand.Execute(null);
        }
      };

      return frame;
    }

    /// <summary>
    /// The screen that leaves the application, as a frame - this front end's <c>dialogSystemState</c>. It is a
    /// stop of the workflow and not a window, so the trail names it and the back key steps off it like any
    /// other screen, and it says what leaving really does while something is playing before it lets anybody
    /// press the button that does it.
    /// </summary>
    private Workflow.WorkflowFrame CloseFrame()
    {
      return new Workflow.WorkflowFrame("close", "Close", new CloseViewModel(_nowPlaying));
    }

    /// <summary>
    /// The playback screen's frame.  The view model belongs to the application, not to this frame: a viewer who
    /// goes back to the library to fetch the next episode and returns has to find the same film at the same
    /// position, and the rebuilt screen shows exactly that because it is built over the same model again.
    /// </summary>
    private Workflow.WorkflowFrame NowPlayingFrame()
    {
      return new Workflow.WorkflowFrame("nowplaying", "Now playing", _nowPlaying!);
    }

    /// <summary>
    /// A watching screen, on whatever channel somebody pressed their way to - out of the guide, or out of the menu
    /// with nothing chosen, in which case the screen opens on its bouquet and waits for a zap.
    /// </summary>
    /// <remarks>
    /// The tuner is built here rather than kept by the application, which is the one place this front end departs
    /// from how it treats the player. The player is kept because a film outlives the screen it was watched on; a
    /// channel does not, because a channel is a thing a person walks away from and a television that carries on
    /// making noise behind the records is a television somebody unplugs. So the tuner lives as long as its frame
    /// does, and <see cref="Workflow.WorkflowManager.Removed"/> closes it when the frame goes.
    /// </remarks>
    private Workflow.WorkflowFrame WatchFrame(Api.TvMediaType kind, Api.TvChannel? channel)
    {
      ViewModels.TvTunerViewModel tuner = StartTuner(kind);
      _ = tuner.ReloadAsync();
      if (channel != null)
        tuner.Tune(channel);

      return new Workflow.WorkflowFrame(kind == Api.TvMediaType.Radio ? "radiowatch" : "tvwatch",
          tuner.Heading + " live", tuner);
    }

    /// <summary>
    /// A tuner that can ask for the picture. It is given no player, because it has no business owning one: it is
    /// told what the backend says a channel is and says which channel it wants on the screen, and this class -
    /// which owns the one player everything is played through - decides how.
    /// </summary>
    private ViewModels.TvTunerViewModel StartTuner(Api.TvMediaType kind)
    {
      ViewModels.TvTunerViewModel tuner = new ViewModels.TvTunerViewModel(kind);

      tuner.PlayRequested += delegate (Api.TvChannel channel, Player.MediaLocation location)
      {
        // The other half of the set is no longer on the screen. The reference stopped its television player to
        // start its radio player; here the player takes the new media and the old channel is simply gone, so all
        // that is left is not leaving a second screen claiming to be showing something.
        foreach (ViewModels.TvTunerViewModel other in _tuners)
        {
          if (!ReferenceEquals(other, tuner))
            other.TakenOver();
        }

        NowPlayingViewModel? player = _nowPlaying;
        if (player == null)
        {
          Console.WriteLine("tv:       " + channel.Name + " - this machine has no player");
          tuner.ReportFailure("This machine has no player, so " + channel.Name + " cannot be watched.");
          return;
        }

        // No media id travels with a live channel. What is being watched is not in the library, has no position
        // to resume from, and must not be reported as watched - the server's "continue watching" shelf is made of
        // things somebody chose from it, not of whatever the television happened to be showing.
        if (player.Play(location))
          Console.WriteLine("tv:       " + channel.Name + " - streamed from the backend");
        else
        {
          Console.WriteLine("tv:       refused - " + player.StatusLine);
          tuner.ReportFailure(player.StatusLine);
        }
      };

      tuner.StopRequested += delegate
      {
        _nowPlaying?.Stop();
      };

      _tuners.Add(tuner);
      return tuner;
    }

    /// <summary>
    /// What a press on a programme that is running does: puts that channel on the screen, over the guide, so that
    /// Back comes out where the viewer was standing.
    /// </summary>
    private void WatchChannel(Api.TvMediaType kind, Api.TvChannel channel)
    {
      Workflow.WorkflowFrame? on = _workflow.Current;
      if (on != null && on.ViewModel is ViewModels.TvTunerViewModel watching
          && watching.Kind == kind)
      {
        watching.Tune(channel);
        return;
      }

      _workflow.Push(WatchFrame(kind, channel));
    }

    /// <summary>
    /// A watching screen went away, so its channel does too. Said to the tuner rather than to the player, because
    /// the tuner is the one that asked for the picture, and because the other half of the set - which may well
    /// still be open on another frame - keeps its answer to itself.
    /// </summary>
    private void StopWatching(ViewModels.TvTunerViewModel tuner)
    {
      if (!_tuners.Remove(tuner))
        return;
      tuner.Dispose();
    }

    /// <summary>
    /// The control that shows one frame, built the first time the viewer reaches that stop.  Which screen a frame
    /// means is decided here and nowhere else, so the workflow, the trail above the screen and the back key all
    /// speak about the same thing.
    /// </summary>
    private Control? BuildViewFor(Workflow.WorkflowFrame frame)
    {
      string state = frame.StateId;

      if (state == "home")
      {
        return new HomeView(_home, _workflow,
            _nowPlaying == null ? null : new Func<Workflow.WorkflowFrame>(NowPlayingFrame),
            _serverMayBeChanged ? new Func<Workflow.WorkflowFrame>(ConnectionFrame) : null,
            delegate (HomeSection section) { return BrowserFrame(section); },
            BuildNamedFrame);
      }

      if (state == "nowplaying")
      {
        _playView = new NowPlayingView((NowPlayingViewModel)frame.ViewModel, _frames, _workflow);
        return _playView;
      }

      if (state == "connection")
        return new ConnectionView((ConnectionViewModel)frame.ViewModel, _workflow);

      if (state == "viewer")
        return new ViewerPickerView((ViewerPickerViewModel)frame.ViewModel);

      if (state == "close")
        return new CloseView((CloseViewModel)frame.ViewModel, _workflow);

      if (state == "folders")
        return new FoldersView((FoldersViewModel)frame.ViewModel);

      if (state == "pictures")
        return new PicturesView((PicturesViewModel)frame.ViewModel);

      if (state == "news")
        return new NewsView((NewsViewModel)frame.ViewModel);

      if (state == "weather")
        return new WeatherView((WeatherViewModel)frame.ViewModel);

      if (state == "browse")
      {
        // The shelves on offer are the home screen's tiles, taken as they stand so that a library that gained a
        // shelf since the menu was built has it here too; a browsed value opens the titles through the same door
        // the tiles use, pushed as its own stop so Back lands the viewer on the column they picked from.
        return new BrowseView((BrowseViewModel)frame.ViewModel, _workflow,
            delegate (HomeSection section) { return BrowserFrame(section); });
      }

      if (state == "tvguide" || state == "radioguide")
        return new TvGuideView((TvGuideViewModel)frame.ViewModel, _workflow);

      // The question a programme in either guide raises, wearing the skin's dialog frame. Its model is the guide's
      // own, which is the one that knows what was pressed and what the backend said about it.
      if (state == "programactions")
        return new TvProgramActionsView((TvGuideViewModel)frame.ViewModel, _workflow);

      if (state == "schedules" || state == "recordings")
        return new TvSchedulesView((TvSchedulesViewModel)frame.ViewModel);

      if (state == "tvwatch" || state == "radiowatch")
        return new TvWatchView((TvTunerViewModel)frame.ViewModel, _frames);

      if (state == "party")
      {
        return new PartyView((PartyViewModel)frame.ViewModel, _workflow,
            _nowPlaying == null ? null : new Func<Workflow.WorkflowFrame>(NowPlayingFrame));
      }

      if (state == "settings")
      {
        // The front end's settings as the reference kept them: the list of places a household configures -
        // its own machine and the server behind it - behind MP2's "Settings" tile.
        return new SettingsView((SettingsViewModel)frame.ViewModel);
      }

      if (state == "menuedit" || state == "menuadd" || state == "menulists" || state == "menulistadd")
      {
        // The four stops of Nereus' own menu editor, all of them over one model: the tiles as they stand, the
        // tiles that may be added, one tile's media lists, and the lists that tile may still take. One model,
        // because the answer is one answer - the arrangement the viewer finally presses OK on - and a screen
        // carrying its own copy of it could only disagree with the others.
        NereusMenuEditViewModel editor = (NereusMenuEditViewModel)frame.ViewModel;
        return new NereusMenuEditView(editor, MenuEditScreenFor(state), _workflow,
            delegate (string key) { return new Workflow.WorkflowFrame(key, MenuEditHeading(key), editor); });
      }

      if (state == "titles")
      {
        MediaBrowserViewModel browser = (MediaBrowserViewModel)frame.ViewModel;

        // The list of what-is-inside is built by the same frames, so a series opened from its tile and a
        // collection opened out of a collection are the same screen with the same keys and the same way back.
        Func<TitleViewModel, Workflow.WorkflowFrame>? inside = browser.Section == null
            ? null
            : delegate (TitleViewModel parent)
            {
              // Said on the console as well as shown, because a press that opens nothing looks exactly like a
              // press that was never received when all that happens is a trail growing one crumb.
              Console.WriteLine("inside:   " + parent.Name + " - the titles inside it, from "
                  + browser.Section!.Label);
              return TitlesFrame(new MediaBrowserViewModel(_home.Client, browser.Section, parent,
                  _home.Pictures), parent.Name);
            };

        return new MediaBrowserView(browser, _workflow,
            _nowPlaying == null ? null : new Func<Workflow.WorkflowFrame>(NowPlayingFrame), inside);
      }

      // A frame naming a screen nobody builds is a mismatch between the workflow and this method, which is a bug
      // in the front end rather than something the viewer can do anything about.
      throw new InvalidOperationException(
          "the workflow reached a state this build cannot show: '" + state + "'");
    }

    /// <summary>
    /// One tile of the home screen opened: its titles, read through the client the home screen is using.
    /// </summary>
    private Workflow.WorkflowFrame BrowserFrame(HomeSection section)
    {
      // The wake-up answer "last shelf" needs a memory, and every road to a shelf - tile, browse column,
      // the wake-up itself - walks through this one door, which is where the remembering happens.
      Settings.ClientSettings.RecordShelf(section.Label);
      return TitlesFrame(new MediaBrowserViewModel(_home.Client, section, null, _home.Pictures), section.Label);
    }

    /// <summary>
    /// A list of titles, named for the trail: the tile's own words for a shelf opened whole, the title's name for
    /// the titles inside a title.
    /// </summary>
    private Workflow.WorkflowFrame TitlesFrame(MediaBrowserViewModel browser, string label)
    {
      browser.PlayRequested += OnTitleChosen;
      _lastBrowser = browser;
      return new Workflow.WorkflowFrame("titles", label, browser);
    }

    /// <summary>
    /// The sentence the status screen shows for what this application is wearing: the skin and theme the
    /// engine has on, or the front end's own face when no skin is. The view model asks for a sentence and
    /// never for the Avalonia application, because knowing the skin means asking the application, and that
    /// question belongs here.
    /// </summary>
    private string DescribeWorn()
    {
      WornSkin? worn = SkinEngine.Worn(this);
      string skin = worn == null
          ? "the front end's own face"
          : worn.Manifest.Name + " (" + worn.Theme.Name + ")";
      return skin + ", palette " + FrontendTheme.CurrentName(this);
    }

    /// <summary>
    /// The settings list itself: which places the hub offers, and what each of them opens. The sections are
    /// built here because only the application has what their screens need - the theme picker changes colours
    /// in this application, the folders screen reads through the client the library was read with, and the
    /// server address belongs to the connection screen, which is pushed as the stop it already is rather than
    /// rebuilt inside the hub.
    /// </summary>
    private SettingsViewModel SettingsModel()
    {
      SettingsViewModel model = new SettingsViewModel();

      // The machine's own answers come first, the way the reference's client settings led its settings
      // tree: what this box is called, whether it fills the television, what it opens when it wakes.
      model.Add("This machine", "Frontend", "Name, full screen, wake-up screen and language of this box",
          delegate { return new ClientSettingsView(); });

      // The reference's system state menu, filed into the hub: what both machines are made of, read from
      // each running thing the moment the row opens. This is also the version line the plan owed to
      // "About" - here it stands beside the server's own answer rather than in a window of its own.
      model.Add("System status", "Frontend", "What this box and the server are made of, read while running",
          delegate
          {
            SystemStatusViewModel status = new SystemStatusViewModel(() => _home.Client, DescribeWorn);
            return new SystemStatusView { DataContext = status };
          });

      model.Add("Appearance", "Frontend", "The colours and the skin this machine wears",
          delegate { return new ThemeView(new ThemeViewModel(this)); });

      // The skin's own area, as the reference filed it at /Appearance/SkinSettings: not part of choosing a
      // skin, but the switches the skin that was chosen brings with it - Nereus's, WMC's, or none at all
      // when the machine wears the front end's own face.
      model.Add("Skin settings", "Skin", "What the worn skin itself lets you change",
          delegate { return new SkinSettingsView(this); });

      // What the viewer tells the player before it plays - the reference's playback settings, kept to the
      // answers this player can honour. The screen saves each row at once; the player picks the answers
      // up through the shared _nowPlaying, which is given the same file at startup.
      model.Add("Playback settings", "Frontend", "Skip length, start volume, languages and slideshow",
          delegate { return new PlaybackSettingsView(); });

      model.Add("Server address", "Frontend", "Which machine holds the library",
          null, delegate { _workflow.Push(ConnectionFrame()); });

      // Who may sign in, read from the server's own user store the moment the row opens - the reference
      // filed this as "Settings -> Benutzer" for exactly the same reason: the names live on the server,
      // not on any one machine.
      model.Add("Users", "Server", "Who can sign in on the server, and what they must say",
          delegate
          {
            UsersSettingsViewModel users = new UsersSettingsViewModel(() => _home.Client);
            return new UsersSettingsView { DataContext = users };
          });

      // What the server says when a shared folder asks who is calling. The reference kept these in the
      // server's own settings window for the same reason: shares are the server's business, not any one
      // machine's - and the dictionary underneath always held one credential per path, not just the root.
      model.Add("Network credentials", "Server", "What the server offers shared folders on the network",
          delegate
          {
            NetworkCredentialsViewModel credentials = new NetworkCredentialsViewModel(() => _home.Client);
            return new NetworkCredentialsView { DataContext = credentials };
          });

      // The server's own settings, read over the API the moment the row is opened - there is
      // nothing local to show before that. The provider is a delegate rather than the client
      // itself because the viewer may point at a different server between visits.
      model.Add("Server settings", "Server", "Login, importer schedule and port on the server",
          delegate
          {
            ServerSettingsViewModel serverSettings = new ServerSettingsViewModel(() => _home.Client);
            _ = serverSettings.ReloadAsync();
            return new ServerSettingsView { DataContext = serverSettings };
          });

      // What the extractors fetch, per kind of media - also read the moment the row opens, and a server
      // without the endpoint says so in the status line rather than offering an empty form.
      model.Add("Metadata", "Server", "What the server fetches from the internet for the library",
          delegate
          {
            MetadataSettingsViewModel metadata = new MetadataSettingsViewModel(() => _home.Client);
            _ = metadata.ReloadAsync();
            return new MetadataSettingsView { DataContext = metadata };
          });

      // Where the television comes from. The reference had no such row because its TV service ran on the same
      // box; here the channels, the programmes and the timers all belong to a TvHeadend elsewhere on the
      // network, so its address is this machine's own answer - filed as one row of its own, the way the
      // reference filed its TV settings under a TV branch of the settings tree.
      model.Add("Television", "TV", "Which machine the channels, the guide and the recording timers come from",
          delegate { return new TvSettingsView(); });

      // The settings a plugin or a server part keeps and a household used to edit in a window on the
      // server's own desktop. ONE row, not one row per section: which sections stand open is the server's
      // answer, read when the row opens, so a section somebody opens in the server's catalog appears on the
      // television by itself. A copy of that table here would only ever be the table as it used to be.
      model.Add("Plugin settings", "Server", "What the server lets a television change in what it keeps",
          delegate
          {
            PluginSettingsViewModel pluginSettings = new PluginSettingsViewModel(() => _home.Client);
            _ = pluginSettings.ReloadAsync();
            return new PluginSettingsView { DataContext = pluginSettings };
          });

      model.Add("Media sources", "Server", "The folders the library reads, and their imports",
          delegate { return new FoldersView(new FoldersViewModel(_home.Client)); });

      // What this machine does with the pictures those folders produce. Filed beside the sources on purpose -
      // one answers where the library comes from, the other what this box keeps of it - but owned by the front
      // end, because one viewer's television has a disk to keep pictures on and another's has none, while both
      // look at the same server. The server is handed over for the one row that is its answer rather than this
      // machine's: what it has learned about where its pictures lie.
      model.Add("Media settings", "Frontend", "What this machine keeps of the library's pictures, and for how long",
          delegate
          {
            MediaSettingsViewModel media = new MediaSettingsViewModel(_home.Pictures, null, _home.Client);
            return new MediaSettingsView(media);
          });

      model.Add("Playback", "Frontend", "The sound of this machine",
          delegate { return new PlaybackView(_nowPlaying); });

      // The way out. The reference front ends kept leaving the application in dialogSystemState, a dialog
      // every viewer found because a television without a mouse still has to be left somehow; the settings
      // hub is where this one advertises it, and it pushes the screen rather than embedding it, exactly as
      // the server address row does - the trail and the back key belong to the close screen, not to the hub.
      model.Add("Close", "Frontend", "Leave the application, whatever is playing",
          null, delegate { _workflow.Push(CloseFrame()); });

      return model;
    }

    /// <summary>
    /// The frame a menu tile asks for by name.
    /// </summary>
    /// <remarks>
    /// Built afresh every time, because the folders of a library are read when the screen opens - a viewer who
    /// added a share with another tool in between has to see it, and a screen kept alive between visits would show
    /// the answer from the last one.
    /// </remarks>
    private Workflow.WorkflowFrame BuildNamedFrame(string screenKey)
    {
      if (screenKey.Equals("folders", StringComparison.OrdinalIgnoreCase))
        return new Workflow.WorkflowFrame("folders", "Folders", new FoldersViewModel(_home.Client));

      if (screenKey.Equals("pictures", StringComparison.OrdinalIgnoreCase))
        return new Workflow.WorkflowFrame("pictures", "Images", new PicturesViewModel(_home.Client));

      if (screenKey.Equals("browse", StringComparison.OrdinalIgnoreCase))
      {
        return new Workflow.WorkflowFrame("browse", "Browse",
            new BrowseViewModel(_home.Client, HomeSection.For(_home.Aspects)));
      }

      if (screenKey.Equals("news", StringComparison.OrdinalIgnoreCase))
      {
        // No server in sight: the news is not the server's data, and the old plugin read it from the television
        // too. What the screen needs is an address to ask, and the feeds answer it themselves - MediaPortal's
        // list in the viewer's language, and whatever they saved in news-feeds.json on top of it.
        return new Workflow.WorkflowFrame("news", "News", new NewsViewModel(new NewsApiClient(),
            cultureName: Settings.ClientSettings.Load(null, out _).Culture,
            feedSettings: NewsFeedsSettings.Load(null, out _)));
      }

      if (screenKey.Equals("weather", StringComparison.OrdinalIgnoreCase))
      {
        // The other screen that talks to the internet rather than the server, because the weather was never the
        // server's data either. The viewer's own API key, if they saved one, rides on the client from the
        // settings file; the product's shared keys are behind it. The view model reads the same file again for
        // its places and its own complaint line, so a broken file is said by the screen that shows it.
        WeatherPlacesSettings places = WeatherPlacesSettings.Load(null, out _);
        return new Workflow.WorkflowFrame("weather", "Weather", new WeatherViewModel(
            new WeatherApiClient(userApiKey: places.ApiKey),
            cultureName: Settings.ClientSettings.Load(null, out _).Culture));
      }

      if (screenKey.Equals("tv", StringComparison.OrdinalIgnoreCase)
          || screenKey.Equals("radio", StringComparison.OrdinalIgnoreCase))
      {
        // The guide, which is where both of the reference's two television tiles led as well: its TV client's
        // home was a channel list and its guide was a grid, and both read one bouquet at a time from whatever
        // backend the viewer had named. Radio is the same screen read over the stations, because a TvHeadend
        // keeps one list of bouquets for both and only the channels inside them say which is which.
        TvMediaType kind = screenKey.Equals("radio", StringComparison.OrdinalIgnoreCase)
            ? TvMediaType.Radio
            : TvMediaType.Television;
        TvGuideViewModel guide = new TvGuideViewModel(kind);
        guide.WatchRequested += delegate (TvChannel channel) { WatchChannel(kind, channel); };
        _ = guide.ReloadAsync();
        return new Workflow.WorkflowFrame(kind == TvMediaType.Radio ? "radioguide" : "tvguide",
            guide.Heading, guide);
      }

      if (screenKey.Equals("schedules", StringComparison.OrdinalIgnoreCase)
          || screenKey.Equals("recordings", StringComparison.OrdinalIgnoreCase))
      {
        // The plans and the recordings, which the reference kept as two screens because they came out of two
        // different services there - the schedules from the TV service, the recordings out of the media library. A
        // TVHeadend answers both out of one table, so both tiles open one screen turned to the table each of them
        // means; the difference a viewer sees is which of the three tables is named at the top.
        bool files = screenKey.Equals("recordings", StringComparison.OrdinalIgnoreCase);
        TvSchedulesViewModel schedules = new TvSchedulesViewModel(finishedFirst: files);
        _ = schedules.ReloadAsync();
        return new Workflow.WorkflowFrame(files ? "recordings" : "schedules",
            files ? "Recordings" : "Schedules", schedules);
      }

      if (screenKey.Equals("tvwatch", StringComparison.OrdinalIgnoreCase)
          || screenKey.Equals("radiowatch", StringComparison.OrdinalIgnoreCase))
      {
        // Straight to the picture, with nothing chosen. The reference had this as its television's own front
        // screen - the player was showing a channel before anybody had looked at a grid - and its
        // <c>AutoStartTV</c> switch opened it at start-up; here it is the screen a household can hang a menu tile
        // on, and it opens on its bouquet so the first press of a channel button has something to walk.
        TvMediaType kind = screenKey.Equals("radiowatch", StringComparison.OrdinalIgnoreCase)
            ? TvMediaType.Radio
            : TvMediaType.Television;
        return WatchFrame(kind, null);
      }

      if (screenKey.Equals("party", StringComparison.OrdinalIgnoreCase))
      {
        return new Workflow.WorkflowFrame("party", "Party", EnsureParty());
      }

      if (screenKey.Equals("themes", StringComparison.OrdinalIgnoreCase)
          || screenKey.Equals("settings", StringComparison.OrdinalIgnoreCase))
      {
        // A fresh model per visit, unlike the party queue: the screens behind the sections read their state
        // when they open - somebody may have started this run with a --theme that overrode the saved choice,
        // or added a share with another tool in between.
        return new Workflow.WorkflowFrame("settings", "Settings", SettingsModel());
      }

      // The two corner buttons of the reference's master_home that name a screen rather than a section: the
      // person who is watching, and the way out of the room. Both are the same frames the application opens
      // on its own - the picker it raises at startup when the machine asks for a viewer, and the close
      // screen behind its own exit - so the corner button costs no second copy of either.
      if (screenKey.Equals("viewer", StringComparison.OrdinalIgnoreCase))
        return ViewerFrame();

      if (screenKey.Equals("close", StringComparison.OrdinalIgnoreCase))
        return CloseFrame();

      // Nereus' own home menu editor. The skin's screen rather than this application's: the tiles it can offer are
      // the ones the home menu holds, the answer goes into Nereus' own file, and the home screen is told to lay its
      // menu out again when the editor is closed with OK. The three stops behind this one - the tiles to add, one
      // tile's lists, and the lists to add - are pushed by the editor itself, over this same model, so that the
      // back key steps off one question at a time the way the reference's four dialogs did.
      if (screenKey.Equals("menuedit", StringComparison.OrdinalIgnoreCase))
      {
        Guid? standing = Guid.TryParse(_home.SelectedEntry?.HomeImageId, out Guid standingId) ? standingId : null;
        return new Workflow.WorkflowFrame("menuedit", "Edit menu",
            new NereusMenuEditViewModel(_home.Menu, standing, _home.ApplyNereusMenuEdit));
      }

      // A tile naming a screen nobody builds is a mismatch between the menu and this method, which is a bug in the
      // front end rather than something the viewer can do anything about.
      throw new InvalidOperationException(
          "the home menu asked for a screen this build does not have: '" + screenKey + "'");
    }

    /// <summary>Which of the editor's four stops a state names.</summary>
    private static NereusMenuEditScreen MenuEditScreenFor(string state)
    {
      return state switch
      {
        "menuadd" => NereusMenuEditScreen.PickTiles,
        "menulists" => NereusMenuEditScreen.Lists,
        "menulistadd" => NereusMenuEditScreen.PickLists,
        _ => NereusMenuEditScreen.Tiles
      };
    }

    /// <summary>The four headings Nereus wrote over its four editor dialogs, in its own words.</summary>
    private static string MenuEditHeading(string state)
    {
      return state switch
      {
        "menuadd" => "Add menu item",
        "menulists" => "Edit home content",
        "menulistadd" => "Add home content",
        _ => "Edit menu"
      };
    }

    /// <summary>
    /// The one party queue of the whole application, unlike every screen above: the queue belongs to the evening
    /// rather than to the screen, the way the playback does. The screen asks, the application plays - and the
    /// application's own playback reports back into the screen, because a party cannot hear its own music without
    /// those two ears.
    /// </summary>
    private PartyViewModel EnsureParty()
    {
      if (_party == null)
      {
        _party = new PartyViewModel(_home.Client);
        _party.PlayRequested += OnPartySongChosen;

        if (_nowPlaying != null)
        {
          NowPlayingViewModel playback = _nowPlaying;
          playback.PlaybackReported += delegate (object? sender, PlaybackReportedEventArgs args)
          {
            // Only an ending reaches the queue. A report raised by the watched mark - a song somebody stopped
            // at ninety-five per cent - is as finished-looking a hundred as the engine ever says, so the queue
            // listens to the engine's voice on the report, not to its number.
            if (args.EngineEnded)
              _party.TrackFinished(args.MediaItemId);
          };
          playback.PropertyChanged += delegate
          {
            if (playback.StateText == "Failed")
              _party.TrackRefused(playback.MediaItemId ?? Guid.Empty, playback.StatusLine);
          };
        }
      }

      return _party;
    }

    /// <summary>
    /// A viewer pressed OK on a title. This is the moment the library becomes a film.
    /// </summary>
    /// <remarks>
    /// Said on the console as well as put on the browser's status line, because the front end is also run from a
    /// terminal on a machine without a window manager, where a silent refusal is indistinguishable from a
    /// playback that is merely quiet.
    /// </remarks>
    private void OnTitleChosen(object? sender, TitleViewModel title)
    {
      if (_nowPlaying == null)
      {
        Console.WriteLine("play:     " + title.Name + " - this machine has no player");
        return;
      }

      MediaLocation? location = title.PlaybackLocation;
      if (location == null)
      {
        // Said here as well as on the screen's status line, because the screen most likely to be open when this
        // happens is one the viewer pressed OK on, and a press that opens nothing looks like a lost keypress.
        Console.WriteLine("play:     " + title.Name + " - " + title.UnplayableReason);
        return;
      }

      bool streamed = !location.Value.IsFile;
      string what = streamed
          ? location.Value.Describe()
          : System.IO.Path.GetFileName(location.Value.EngineString);

      if (_nowPlaying.Play(location.Value, title.Title.Id, title.ResumePercent ?? 0))
      {
        // The shelf this title was pressed in is the shelf that is now playing - said to the home screen, whose
        // tile for that shelf wears the mark until the player falls silent. A press from the home strip itself
        // names no shelf, and a tile would be guessing, so the mark simply goes off.
        _playingAspect = (sender as MediaBrowserViewModel)?.AspectName;
        _home.ShowPlaying(_playingAspect);
        Console.WriteLine("play:     " + what + (streamed ? " - streamed from the server" : " from the library")
            + (title.ResumePercent.HasValue ? " - resumed at " + title.ResumePercent.Value + "%" : ""));
      }
      else
        Console.WriteLine("play:     refused - " + _nowPlaying.StatusLine
            + (streamed ? " (a server that wants to know its caller cannot be handed to the player as an address)" : ""));
    }

    /// <summary>
    /// The party asked for a song. It is played by the same player as any other title and reported to the
    /// library the same way; what the party gets on top is the answer.
    /// </summary>
    /// <remarks>
    /// The title list never hears that its film would not open - the viewer is standing in front of the
    /// screen and presses again. The party is by definition not being watched, so every refusal has to come
    /// back to it, or a broken corner of the shelf would stop the evening in silence.
    /// </remarks>
    private void OnPartySongChosen(object? sender, TitleViewModel title)
    {
      if (_nowPlaying == null)
      {
        Console.WriteLine("play:     " + title.Name + " - this machine has no player");
        _party?.TrackRefused(title.Title.Id, "this machine has no player");
        return;
      }

      MediaLocation? location = title.PlaybackLocation;
      if (location == null)
      {
        Console.WriteLine("play:     " + title.Name + " - " + title.UnplayableReason);
        _party?.TrackRefused(title.Title.Id, title.UnplayableReason ?? "the title named no file at all");
        return;
      }

      bool streamed = !location.Value.IsFile;
      string what = streamed
          ? location.Value.Describe()
          : System.IO.Path.GetFileName(location.Value.EngineString);

      if (_nowPlaying.Play(location.Value, title.Title.Id, title.ResumePercent ?? 0))
      {
        // The party reads the audio shelf, whatever the tile it was opened from was called.
        _playingAspect = "AudioItem";
        _home.ShowPlaying(_playingAspect);
        Console.WriteLine("play:     party - " + what + (streamed ? " - streamed from the server" : " from the library"));
      }
      else
      {
        Console.WriteLine("play:     party refused - " + _nowPlaying.StatusLine);
        _party?.TrackRefused(title.Title.Id, _nowPlaying.StatusLine);
      }
    }

    /// <summary>
    /// Tells the browser screen that what it showed about one title is now out of date.
    /// </summary>
    /// <remarks>
    /// The screen most likely to still be showing the numbers from before the film is the one the viewer pressed
    /// play in, which is the last one this application built. A tile opened earlier and left open in the background
    /// keeps its old numbers until it is next opened; holding on to every browser screen - any of which can carry
    /// a thousand titles - to keep that one honest is the worse of the two mistakes.
    /// </remarks>
    /// <summary>
    /// Something was watched and the library took the report, so the screens that might still be describing that
    /// title from before the film are told what the library now says about it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rows are updated from the answer the report itself brought back rather than read again: that answer
    /// already names the count, the date and the position, so asking a second question at the moment a film ends
    /// would cost a round trip to learn what has just been said. The strip and the list also keep their order and
    /// the viewer's place in them, which a refresh would not.
    /// </para>
    /// <para>
    /// The details panel is still dropped, because it says more than those three figures - the title's attributes,
    /// what is next in its series - and only a read can bring the rest of it up to date.
    /// </para>
    /// </remarks>
    private void HeardAboutPlayback(Guid mediaItemId, MediaPlayback after)
    {
      bool onList = _lastBrowser != null && _lastBrowser.ApplyPlayback(after);
      _lastBrowser?.ForgetDetails(mediaItemId);

      Console.WriteLine("screens:  " + (!onList
          ? "nothing on screen was showing that title"
          : after.Watched
              ? "the library says it is watched through, and the screens say it too"
              : "the library says how far it got, and the screens say that too"));
    }

    /// <summary>
    /// The application's single window, opened with the "which server?" question at the bottom of the workflow:
    /// what a first start shows, before there is a server to ask about a library.  Saving an answer hands the
    /// window to the library - see <see cref="ConnectionFrame"/>.
    /// </summary>
    private ShellWindow BuildConnectionScreen()
    {
      _workflow.Push(ConnectionFrame());
      return new ShellWindow(_workflow, BuildViewFor);
    }
  }
}
