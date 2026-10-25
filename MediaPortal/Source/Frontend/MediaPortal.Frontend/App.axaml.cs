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
    /// The channel pictures, asked for once for this run and shared by every guide afterwards. Shared because the
    /// pictures of the same channels are wanted by the television guide, the radio guide and the channel lists, and
    /// a machine that is asked for the same icon three times is a machine that was asked twice too often.
    /// </summary>
    private Caching.ChannelLogos? _logos;

    /// <summary>
    /// The thing that watches the TV machine for recordings starting and ending. Lives as long as the window does,
    /// because a notice about a recording is only any good if the front end was awake when the recording happened.
    /// </summary>
    private ViewModels.TvRecordingWatcher? _recordings;

    /// <summary>
    /// The things the household has been told and has not read: the importer's news, a server that would not let this
    /// front end listen, a frame that was dropped. The head line's hint counts it and the dialog reads it, which is
    /// the arrangement the reference's notification bar had (<c>Skin/Nereus/screens/NotificationsBar.inc</c> and
    /// <c>screens/WatchSystemNotifications.xaml</c>).
    /// </summary>
    private readonly Notifications.NotificationQueue _notifications = new Notifications.NotificationQueue();

    /// <summary>
    /// The channel pictures a guide is to be built over, on the shelf this front end keeps its pictures on - which
    /// is the media settings' answer, and no shelf at all on a box whose viewer switched the remembering off.
    /// </summary>
    private Caching.ChannelLogos ChannelPictures()
    {
      if (_logos == null)
        _logos = new Caching.ChannelLogos(null, null, _home.Pictures);

      return _logos;
    }

    /// <summary>
    /// The tuners that are on the screen, the television and the radio each with their own. They are kept here so
    /// that the half somebody did not just touch can be told to step aside when the other one takes the picture:
    /// the reference owned two players and stopped one to start the other
    /// (<c>TV/SlimTvClient/TVHandler/SlimTvHandler.cs:213-221</c>), and with this front end's single player there
    /// is nothing to stop - only a tuner left believing its channel is still on.
    /// </summary>
    private readonly List<ViewModels.TvTunerViewModel> _tuners =
        new List<ViewModels.TvTunerViewModel>();

    /// <summary>
    /// Who is watching, and what the house allows them - the one place the wake screen, the head line's
    /// user picture, the users screen and the idle timer all read the same answer from. The reference kept
    /// this in <c>IUserManagement.CurrentUser</c>; here it is the front end's own observable.
    /// </summary>
    private readonly Theming.ViewerState _viewer = new Theming.ViewerState();

    /// <summary>The frame the head line's picture opens when the room wants another person.</summary>
    private Workflow.WorkflowFrame? _switchFrame;

    /// <summary>
    /// The home screen as built, while it stands - which is who the corner buttons of the worn face have to
    /// be asked about again when the person watching moves.
    /// </summary>
    private Views.HomeView? _homeView;

    /// <summary>The idle watch of the auto-logout - see <see cref="CheckIdle"/>.</summary>
    private Avalonia.Threading.DispatcherTimer? _idleTimer;
    private DateTime _lastViewerActivity = DateTime.Now;

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
      // The person this machine watches as belongs on the skin before the palette goes on: a name owns the
      // skin's answers - its accent, its pictures, the corner buttons that person is shown - and they are read
      // out of that name's own file. Hanging the skin first under nobody and again under the name half a
      // second later costs a second of nothing and lets the first screens draw themselves in colours that are
      // not the room's. The wake still writes this name when it walks somebody in; what it cannot do is make
      // the screens already drawn forget the colours they resolved.
      string watchingAtHome = Settings.ClientSettings.Load(null, out _).RememberedViewer;
      if (watchingAtHome.Length > 0)
        Settings.SkinSettingsBase.FileOwner = watchingAtHome;

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

          // A title picked on the home screen's "left half way" strip or its content wall is played by exactly
          // the same code as a title picked in the library - same player, same media id handed to the reporter,
          // so finishing it updates the same library the strip read from - except for the titles that hold
          // others, which are opened there just as they are opened in the shelf.
          _home.ResumeRequested += OnHomeTitlePressed;
          desktop.Exit += delegate { _home.ResumeRequested -= OnHomeTitlePressed; };

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
          if (args.PropertyName == nameof(ViewModels.HomeViewModel.ServerActivity)
              || args.PropertyName == nameof(ViewModels.HomeViewModel.ServerState)
              || args.PropertyName == nameof(ViewModels.HomeViewModel.ServerFilingPercent))
          {
            // The state is what the head line draws a picture of, the sentence is what the picture cannot say, and
            // the percentage goes where the reference printed its importer's number. All three come from one call in
            // the model, so they cannot disagree about what the server is doing.
            _shell?.Chrome.ReportServerActivity(_home.ServerState, _home.ServerActivity, _home.ServerFilingPercent);
          }

          // The skin's own answers belong to whoever is signed in, like they did in the reference: the
          // signed-in name names the skin settings file from this moment on, and every screen that reads
          // them again - which is every screen that reads them at all - reads that person's answers.
          if (args.PropertyName == nameof(ViewModels.HomeViewModel.SignedInAs))
            Settings.SkinSettingsBase.FileOwner = _home.SignedInAs;
        };

        // The queue of what has not been read yet, hooked to both ends it has: the screen that hears things worth
        // filing hands it over, and the head line counts it. Between them is the hint the reference's bar carried,
        // and pressing that reads the queue one at a time - which is what its notification button did.
        _home.AttachNotifications(_notifications);
        _shell.Chrome.AttachNotifications(_notifications);
        _shell.Chrome.NotificationsRequested += OpenNotifications;

        // The idle watch, so a living room that got up takes its person back to the door. The door itself
        // is the home's own corner button - the person glyph the reference hung in master_home's top row -
        // which BuildNamedFrame opens as a switch and the screen gate below hides whenever login is off.
        _viewer.ViewerChanged += delegate
        {
          // Every list the server filled was filled through this person's eyes; a new pair of eyes asks
          // again. And the home's corner buttons are hung or taken down for whoever is standing there now -
          // the reference's restricted user never saw the cog, and the grown-up who switches in finds it
          // back without a restart.
          // Only eyes nobody has read under yet send a read of their own: the wake asks for the library under
          // its animation, and a second run behind that one would cancel it and throw the answers it already
          // brought out of the menu and the wall with it.
          if (!_home.ReadUnderThisViewer)
            _ = _home.RefreshAsync();
          _homeView?.ReapplyScreenGates();
        };
        StartIdleWatch();
        desktop.Exit += delegate { _idleTimer?.Stop(); };
        _workflow.Changed += delegate
        {
          // While the queue is being read out, the hint that counts it is the same number said twice; the reference
          // hid it exactly then (SkinBase/Models/NotificationsModel.cs:104-112).
          _shell?.Chrome.HideNotificationHint(_workflow.Current?.StateId == "notifications");
        };

        desktop.MainWindow = _shell;

        // What the television machine does about recordings is said in the same header, from every screen: the
        // reference kept its "Recording started" and its recording mark in the master's notification bar, not behind
        // a television screen that has to be opened first. The watcher starts here rather than when a guide is
        // opened, because a recording that begins while nobody is looking at a guide is precisely the one a notice
        // is for; it asks the backend what it is recording and says whatever changed since it last asked.
        _recordings = new ViewModels.TvRecordingWatcher(marshal: action => Dispatcher.UIThread.Post(action));
        _recordings.NoticeChanged += delegate (string? notice)
        {
          _shell?.Chrome.ReportRecordingNotice(notice);
        };
        _recordings.RecordingChanged += delegate (bool recording)
        {
          _shell?.Chrome.ReportRecording(recording);
        };
        _recordings.Start();
        desktop.Exit += delegate
        {
          _recordings.Dispose();
          _recordings = null;
        };

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

      // A file on this machine or a stream off a server, which is the same question the library asks when a
      // viewer presses a title: --play is the harness for watching either one, and the stream is the case a
      // person cannot reproduce with a file at all.
      MediaLocation played = Uri.TryCreate(_playFile!, UriKind.Absolute, out Uri? asUri)
          && (asUri.Scheme == Uri.UriSchemeHttp || asUri.Scheme == Uri.UriSchemeHttps
              || asUri.Scheme == "rtsp" || asUri.Scheme == "udp")
          ? MediaLocation.FromUrl(asUri)
          : MediaLocation.FromFile(_playFile!);

      if (!_nowPlaying!.Play(played))
        Console.WriteLine("play:     refused - " + _nowPlaying.StatusLine);
      else
        Console.WriteLine("play:     " + (played.IsFile
            ? System.IO.Path.GetFileName(_playFile)
            : played.Describe()));

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

      // The format the engine offered is named beside the pixels because the two have to be read together: bytes
      // that are a colour picture in one format are a grey one in another, and a sentence about luminance cannot
      // say which of the two happened.
      string agreed = string.IsNullOrEmpty(sink.SourceFormat) ? "" : " [" + sink.SourceFormat + "]";
      return DescribePixels(pixels, width * 4, width, height) + agreed;
    }

    /// <summary>
    /// The two numbers, from bytes that a screen was given: how bright they are on average, and how many
    /// different colours they hold.
    /// </summary>
    private static string DescribePixels(byte[] pixels, int stride, int width, int height)
    {
      HashSet<int> colours = new HashSet<int>();
      long luminance = 0;
      long spread = 0;
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
          spread += Math.Abs(red - blue);
          colours.Add((red >> 3 << 10) | (green >> 3 << 5) | (blue >> 3));
          sampled++;
        }
      }

      if (sampled == 0)
        return "no picture to look at";

      // The distance between red and blue is on this line because "distinct colours" alone is not enough to tell a
      // grey picture from a film: a picture with no colour at all still has several hundred distinct greys, and a
      // person reading "475 distinct colours" and a mean luminance of 26 out of 255 has no way of noticing that
      // what arrived was a luma plane wearing three bytes of somebody else's pixels.
      return "mean luminance " + (luminance / sampled)
          + ", " + colours.Count.ToString("N0", CultureInfo.InvariantCulture)
          + " distinct colour(s), red and blue " + (spread / sampled) + " level(s) apart, in "
          + sampled.ToString("N0", CultureInfo.InvariantCulture) + " sampled pixels";
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

      // Who is watching, and with what four numbers: the wake screen wears the wallpaper, reads the
      // server's people, and either wakes after its four seconds of animation or stops and asks the
      // remembered person for their PIN. It stands at the root of the workflow - where the connection
      // screen stands on a first start - because a question one back key past is decoration, and the
      // the way back has nothing to walk over when there is nothing under it. Answering it hands the window to
      // the library; the remembered name owns the skin's answers, exactly as the "who is watching"
      // pick always left them.
      Settings.ClientSettings machine = Settings.ClientSettings.Load(null, out _);
      if (machine.RememberedViewer.Length > 0)
        Settings.SkinSettingsBase.FileOwner = machine.RememberedViewer;

      _workflow.Push(StartupFrame());

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
    /// <summary>
    /// Reads the queue out loud: raises the dialog over whatever screen the viewer was standing on, one notification
    /// at a time. Called by the head line's hint, which is the only way into it - the reference's bar's button was
    /// the only way into its dialog too.
    /// </summary>
    private void OpenNotifications()
    {
      if (_notifications.Count == 0)
        return;

      ViewModels.NotificationsViewModel model = new ViewModels.NotificationsViewModel(_notifications);
      model.HandleRequested += OpenNotifiedScreen;
      _workflow.Push(new Workflow.WorkflowFrame("notifications", "Notifications", model));
    }

    /// <summary>
    /// Goes to the screen a notification was about, which is what the reference's second button did: acknowledging a
    /// thing and going to deal with it are different answers, and the household should be allowed either.
    /// </summary>
    private void OpenNotifiedScreen(string screen)
    {
      // Only the screens the front end can name from here are answered. Asking for a screen this application does not
      // know is not an error - the notification has been read either way - so an unknown name is simply left alone.
      if (screen == "server")
        _workflow.Push(ConnectionFrame());
    }

    /// <summary>
    /// The wake of the front end: the wallpaper, the last person, and their four numbers when the server
    /// knows any. It is the root on a start that has a server, and its answer replaces it with the library
    /// the same way the connection screen's answer does on a start that has none. Checking the numbers is
    /// the server's own sign-in - the PIN is the person's entry in the store that has always answered that
    /// endpoint, so every television in the house asks the same question and gets the same answer.
    /// </summary>
    private Workflow.WorkflowFrame StartupFrame(bool switching = false)
    {
      // What the wake waits for beyond the ring's own turn: the shelves standing under the eyes it is about to
      // hand the television to. The wake asks for the reading itself (WarmLibraryRequested below) and holds its
      // animation until that reading answers, so the library takes the window in the beat after the ring has
      // left it rather than a second and a half after. The ask opens the gate: a reading that finished before
      // the wake knew who watches was made under other eyes, and what those eyes were shown is not this
      // wake's answer - a person with restrictions is not served a wall read for the whole house.
      TaskCompletionSource shelf = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      bool asked = false;
      void LibraryStood()
      {
        if (!asked || _home.IsBusy)
          return;
        if (!_home.ReadUnderThisViewer)
        {
          _ = _home.RefreshAsync();
          return;
        }

        _home.LibraryReady -= LibraryStood;
        shelf.TrySetResult();
      }
      void AsksForTheShelf()
      {
        asked = true;
        if (_home.IsBusy)
          return;
        if (_home.ReadUnderThisViewer)
        {
          LibraryStood();
          return;
        }

        _ = _home.RefreshAsync();
      }
      _home.LibraryReady += LibraryStood;
      StartupViewModel viewModel = new StartupViewModel(
          () => _home.Client,
          async delegate (string name, string pin)
          {
            ServerApiClient? client = _home.Client;
            if (client == null)
              return false;
            try
            {
              await client.SignInAsync(name, pin, System.Threading.CancellationToken.None);
              return true;
            }
            catch (Api.ServerApiException)
            {
              // A refused sign-in is one answer with two words: these are not those four numbers.
              return false;
            }
          },
          null,
          null,
          () => _home.Client,
          _viewer,
          () => shelf.Task);
      viewModel.ForSwitching = switching;
      // The reading of the library belongs to the animation, not to the home screen: the wake says the word
      // as soon as it knows who is watching, and the answer arrives while the ring is still turning.
      viewModel.WarmLibraryRequested += AsksForTheShelf;
      // The wake's answer replaces the wake; the switch's answer pops back over the library, and only
      // the sign-out reaches past both - which is why they are separate continuations of one screen.
      viewModel.Finished += switching ? FinishSwitch : FinishStartup;
      Workflow.WorkflowFrame frame = new Workflow.WorkflowFrame("startup", "Welcome", viewModel);
      if (switching)
        _switchFrame = frame;
      return frame;
    }

    /// <summary>
    /// The switch answered. A name means the television changes owners the way a sign-in always did - the
    /// name owns the skin's answers, the colours are read anew, and the library re-reads its shelves under
    /// the new person's eyes, which is what the reference's <c>ChangeUserContext</c> and its
    /// <c>UserChanged</c> message did together. Nobody means the room stayed put; and letting the
    /// television go closes the whole trail, because a signed-out machine that still remembers the way back
    /// into somebody's screens is not signed out.
    /// </summary>
    private void FinishSwitch(string name)
    {
      StartupViewModel? vm = (_switchFrame?.ViewModel as StartupViewModel)
          ?? (_workflow.Current?.ViewModel as StartupViewModel);
      bool signedOut = vm?.SignedOut == true;
      _switchFrame = null;

      if (signedOut)
      {
        SignOut();
        return;
      }

      if (_workflow.Stack.Count > 1)
        _workflow.Pop();
      if (name.Length == 0)
        return;

      // The person owns the skin's answers from this moment on; the screens themselves already heard the
      // move from the viewer state and re-read their shelves, so this half is only about the colours.
      Settings.SkinSettingsBase.FileOwner = name;
      Theming.FrontendTheme.Reapply(Application.Current!);
    }

    /// <summary>
    /// Letting the television go: the person steps down, the shared file owns the skin's answers again,
    /// and the workflow starts from the wake - which asks the next person straight away, because a house
    /// that wants names at the door does not stand open behind one. The remembered name stays written
    /// down; signing out today is not a statement about tomorrow.
    /// </summary>
    private void SignOut()
    {
      _switchFrame = null;
      _viewer.SetViewer(null);
      Settings.SkinSettingsBase.FileOwner = "";
      Theming.FrontendTheme.Reapply(Application.Current!);
      _workflow.Reset(StartupFrame());
    }

    /// <summary>
    /// The idle watch, which is the reference's auto-logout: a two-second poll of the input manager's
    /// last-key-and-mouse time there, a fifteen-second look at this window's own input here. A television
    /// that has not been touched for the household's minutes gives itself back to the door - but never in
    /// the middle of something: the reference refused to idle while its player was at work
    /// (<c>LoginModel.cs</c> idle check with the player guard), and so a paused film keeps the room's owner
    /// in their chair.
    /// </summary>
    private void StartIdleWatch()
    {
      ShellWindow window = _shell!;
      void Note(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
      {
        _lastViewerActivity = DateTime.Now;
      }

      // Tunnel and answered-or-not, because "the room is here" is also true of a key the current screen
      // swallowed and of a mouse that only moved across the guide.
      window.AddHandler(Avalonia.Input.InputElement.KeyDownEvent, Note,
          Avalonia.Interactivity.RoutingStrategies.Tunnel, true);
      window.AddHandler(Avalonia.Input.InputElement.PointerPressedEvent, Note,
          Avalonia.Interactivity.RoutingStrategies.Tunnel, true);
      window.AddHandler(Avalonia.Input.InputElement.PointerMovedEvent, Note,
          Avalonia.Interactivity.RoutingStrategies.Tunnel, true);

      _idleTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
      _idleTimer.Tick += delegate { CheckIdle(); };
      _idleTimer.Start();
    }

    /// <summary>
    /// One look at the idle watch: off the hook while no login is on, nobody is signed in, or the house
    /// wants no logout; otherwise the timeout is the server's, and the television being busy buys itself
    /// fresh minutes rather than arguing about them.
    /// </summary>
    private void CheckIdle()
    {
      Api.LoginPolicyAnswer policy = _viewer.Policy;
      if (!policy.AutoLogoutEnabled || _viewer.Viewer == null)
        return;

      if (TelevisionIsBusy())
      {
        _lastViewerActivity = DateTime.Now;
        return;
      }

      int minutes = policy.AutoLogoutIdleTimeoutInMin > 0 ? policy.AutoLogoutIdleTimeoutInMin : 30;
      if (DateTime.Now - _lastViewerActivity >= TimeSpan.FromMinutes(minutes))
        SignOut();
    }

    /// <summary>Whether something is being watched right now - a picture playing, or a channel open.</summary>
    private bool TelevisionIsBusy()
    {
      if (_nowPlaying != null && _nowPlaying.HasMedia)
        return true;
      foreach (Workflow.WorkflowFrame frame in _workflow.Stack)
      {
        if (frame.ViewModel is ViewModels.TvTunerViewModel)
          return true;
      }
      return false;
    }

    /// <summary>
    /// The wake answered. The name - or the empty word for "nobody" - owns the skin's answers from here,
    /// and the library takes the window the way it does after the connection screen: replaced, not pushed,
    /// because the wake is not a place anybody goes back to with the back key.
    /// </summary>
    private void FinishStartup(string? name)
    {
      if (string.IsNullOrEmpty(name))
        name = Settings.ClientSettings.Load(null, out _).RememberedViewer;

      // The skin's answers belong to the name the room watches as: its colours, its pictures, the corner
      // buttons that person is shown. So a name moving in means hanging the skin anew - half a second of it,
      // measured on the shown screen. A name that was already standing there is another matter: the machine
      // opens on the person it remembers, which is the way almost every start goes, and re-hanging the skin
      // over the same name throws a second of nothing away for no answer at all.
      string standing = Settings.SkinSettingsBase.FileOwner ?? "";
      Settings.SkinSettingsBase.FileOwner = name ?? "";
      if (!string.IsNullOrEmpty(name)
          && !string.Equals(standing, name, StringComparison.Ordinal))
        Theming.FrontendTheme.Reapply(Application.Current!);

      _workflow.ReplaceCurrent(HomeFrame());
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
    /// stop of the workflow and not a window, so the header names it and the back key steps off it like any
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
      ViewModels.TvTunerViewModel tuner = new ViewModels.TvTunerViewModel(kind, null, null,
          Views.ScreenThread.Run);

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
    /// means is decided here and nowhere else, so the workflow, the header over the screen and the back key all
    /// speak about the same thing.
    /// </summary>
    private Control? BuildViewFor(Workflow.WorkflowFrame frame)
    {
      string state = frame.StateId;

      if (state == "home")
      {
        HomeView homeView = new HomeView(_home, _workflow,
            _nowPlaying == null ? null : new Func<Workflow.WorkflowFrame>(NowPlayingFrame),
            _serverMayBeChanged ? new Func<Workflow.WorkflowFrame>(ConnectionFrame) : null,
            delegate (HomeSection section) { return BrowserFrame(section); },
            BuildNamedFrame,
            // The cog and its kin hide behind the person's restrictions, exactly as the reference's
            // gated elements did. The person button answers to one rule instead, the reference's own
            // visibility for its UserButton: it is there in a house that asks who is watching, and it
            // is not there in one that does not - there is nobody to switch to when nobody is asked.
            key => key.Equals("viewer", StringComparison.OrdinalIgnoreCase)
                ? _viewer.Policy.EnableUserLogin
                : _viewer.Allows(GateGroupFor(key)));
        _homeView = homeView;
        return homeView;
      }

      if (state == "nowplaying")
      {
        _playView = new NowPlayingView((NowPlayingViewModel)frame.ViewModel, _frames, _workflow);
        return _playView;
      }

      // The playback answers, raised over the film by the gear in its display. The settings' own list is a
      // bigger place a viewer walks to; what the panel opens is the one screen of answers about playing, and
      // Back brings the film back.
      if (state == "playbacksettings")
        return new PlaybackSettingsView();

      if (state == "connection")
        return new ConnectionView((ConnectionViewModel)frame.ViewModel, _workflow);

      // The wake, and the workflow it raises the household's wall of faces on. The wall is a dialog rather than
      // a panel on the wake, so the wake needs the way to ask for it.
      if (state == "startup")
        return new StartupView((StartupViewModel)frame.ViewModel, _workflow);

      // The wall itself, over the wake that asked: every face of the household on the skin's dialog frame.
      if (state == "household")
        return new UserPickerView((StartupViewModel)frame.ViewModel, _workflow);

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

      // The queue read out loud, wearing the skin's dialog frame: the shelf or the guide stays lit behind it, because
      // a notification is a word spoken to somebody who is doing something (WatchSystemNotifications.xaml).
      if (state == "notifications")
        return new NotificationsView((NotificationsViewModel)frame.ViewModel, _workflow);

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
              // press that was never received when all that happens is the stack growing one stop.
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
    /// A list of titles, named for the header: the tile's own words for a shelf opened whole, the title's name for
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
    /// The settings wall itself: which branches the hub offers, and the pages that stand behind each of them.
    /// The plates are built here because only the application has what their screens need - the theme picker
    /// changes colours in this application, the folders screen reads through the client the library was read
    /// with, and the server address page is the connection screen itself, lent to the hub's pane rather than
    /// rebuilt inside it.
    /// </summary>
    /// <remarks>
    /// Internal rather than private so that a test can walk the wall it builds: the plates name their pictures
    /// the reference spelled them, and a name that no shipped skin carries is a bare plate on the television -
    /// the one mistake of this kind a test can catch without a person looking at a screen.
    /// </remarks>
    internal SettingsViewModel SettingsModel()
    {
      SettingsViewModel model = new SettingsViewModel();

      // <b>The wall, not the list.</b> What stands on the first level is one plate per branch of the
      // reference's configuration tree - Appearance, General, Media, Players, Plugins, Shares, Television,
      // Users - and every one of them is a <b>door</b>: picked, it shows the wall of its pages, and a page is
      // what used to be a row of this list. The reference walked a household from plate to plate before it let
      // anybody read a field (ServerSettings.Client/plugin.xml:93 hung eight pages under /Configuration/
      // Settings/Media alone, and the pictures MP2 ships for the settings menu are carved after exactly those
      // paths: Appearance_General, General_System_Diagnostics, Media_FanArtSettings, Players_Image,
      // Shares_SharesConfiguration, TV_EPGGenreColors), and a screen of thirty rows beside it is the same
      // questions asked all at once, which is what this hub did before it had walls.
      //
      // <b>Which branches, and why not all of them.</b> A branch stands here when this front end can answer
      // for it. What the reference had and this machine has nothing to say about is left off the wall rather
      // than stood on it empty - InputDevices, whose buttons and remotes the skin reads and no screen of this
      // front end edits, is the one branch missing. Where the reference's branch had pages this front end has
      // no screen for, the door holds the pages it does have: Players asks the player its three questions
      // rather than the reference's five.
      //
      // The branch the reference drew as Appearance: which skin and which colours this machine wears, and the
      // switches the skin it chose brings with it. The second is not part of choosing a skin - the reference
      // filed it as its own node, /Appearance/SkinSettings, for the same reason.
      model.AddFolder("Appearance", "Frontend",
          "The skin and the colours this machine wears, and what the skin it chose lets you change",
          delegate
          {
            return SettingsViewModel.PagesHere(
                new SettingsSection("Skin and colours", "Frontend",
                    "Which skin and which palette this machine wears",
                    delegate { return new ThemeView(new ThemeViewModel(this)); },
                    "Appearance_General"),
                new SettingsSection("Skin settings", "Skin",
                    "What the worn skin itself lets you change",
                    delegate { return new SkinSettingsView(this); },
                    "Appearance_SkinSettings"));
          },
          "Appearance");

      // /General: the machine's own answers, the server it talks to, the server's own answers, and what both
      // are made of - the reference's Regional, Connectivity and System nodes, with the running state filed
      // under System exactly where its General_System_Diagnostics picture sits.
      model.AddFolder("General", "Frontend",
          "This machine's name and manners, which server it talks to, and what both are made of",
          delegate
          {
            return SettingsViewModel.PagesHere(
                new SettingsSection("This machine", "Frontend",
                    "Name, full screen, wake-up screen and language of this box",
                    delegate { return new ClientSettingsView(); },
                    "General_Regional"),
                new SettingsSection("Server address", "Frontend",
                    "Which machine holds the library",
                    ServerAddressPage,
                    "General_Connectivity"),
                new SettingsSection("Server settings", "Server",
                    "Login, importer schedule and port on the server",
                    delegate
                    {
                      // Read over the API the moment the page opens - there is nothing local to show before
                      // that. The provider is a delegate rather than the client itself because the viewer may
                      // point at a different server between visits.
                      ServerSettingsViewModel serverSettings = new ServerSettingsViewModel(() => _home.Client);
                      _ = serverSettings.ReloadAsync();
                      return new ServerSettingsView { DataContext = serverSettings };
                    },
                    "General_System"),
                new SettingsSection("System status", "Frontend",
                    "What this box and the server are made of, read while running",
                    delegate
                    {
                      // The reference's system state menu, filed under the branch its diagnostics picture
                      // stands on: what both machines are made of, read from each running thing the moment the
                      // page opens. This is also the version line the plan owed to "About" - here it stands
                      // beside the server's own answer rather than in a window of its own.
                      SystemStatusViewModel status = new SystemStatusViewModel(() => _home.Client, DescribeWorn);
                      return new SystemStatusView { DataContext = status };
                    },
                    "General_System_Diagnostics", "General_System"));
          },
          "General");

      // /Media: what this box keeps of the library's pictures, what the server asks the internet for, and what
      // its readers read. The pages behind it are asked for when the door opens - the server's sections, every
      // one of them, so that a section somebody opens in the server's catalog stands on the television by
      // itself. One door, because a household came here for the library's answers, whoever keeps them.
      model.AddFolder("Media", "Frontend",
          "What this machine keeps of the library's pictures, and what the server is told about the library",
          async delegate
          {
            SettingsSection machine = new SettingsSection("Kept pictures", "Frontend",
                "What this box keeps of the library's pictures on its own disk, and for how long",
                delegate
                {
                  MediaSettingsViewModel media = new MediaSettingsViewModel(_home.Pictures, null, _home.Client);
                  return new MediaSettingsView(media);
                },
                "Media_ImageMDESettings");

            SettingsSection fetch = new SettingsSection("What the server fetches from the internet", "Server",
                "Which pieces of information the server asks the internet for when it imports a title",
                delegate
                {
                  MetadataSettingsViewModel metadata = new MetadataSettingsViewModel(() => _home.Client);
                  _ = metadata.ReloadAsync();
                  return new MetadataSettingsView { DataContext = metadata };
                },
                "Media_LanguageSettings");

            // The machine's own page leads, then what the server fetches, then the server's readers and
            // picture store in the order the server lists them.
            IReadOnlyList<SettingsSection> wall =
                await MediaSettingsPages.PagesAsync(() => _home.Client, machine).ConfigureAwait(false);

            var pages = new List<SettingsSection>(wall);
            pages.Insert(Math.Min(1, pages.Count), fetch);
            return pages;
          },
          "Media");

      // /Players: the reference's own division of the same player - the film and music answers on one plate,
      // the picture answers on another, the watched-and-finished manners on a third - and the player's own
      // screen on a plate of its own, because a household that only wants the sound down should be able to
      // walk straight to it.
      model.AddFolder("Players", "Frontend",
          "What this machine tells its player, and the screen that plays what was chosen",
          delegate
          {
            return SettingsViewModel.PagesHere(
                new SettingsSection("Films and music", "Frontend",
                    "Skip length, start volume, volume steps, languages and subtitles",
                    delegate
                    {
                      // The screen saves each row at once; the player picks the answers up through the shared
                      // _nowPlaying, which is given the same file at startup.
                      return new PlaybackSettingsView(
                          new PlaybackSettingsViewModel(null, PlaybackSettingsViewModel.GROUP_PLAYERS));
                    },
                    "Players_VideoPlayer"),
                new SettingsSection("Pictures", "Frontend",
                    "How long one picture stands, and whether a slideshow slowly moves across it",
                    delegate
                    {
                      return new PlaybackSettingsView(
                          new PlaybackSettingsViewModel(null, PlaybackSettingsViewModel.GROUP_PICTURES));
                    },
                    "Players_Image"),
                new SettingsSection("Watched and finished", "Frontend",
                    "How much of a title counts as watched, and whether the player steps aside when it ends",
                    delegate
                    {
                      return new PlaybackSettingsView(
                          new PlaybackSettingsViewModel(null, PlaybackSettingsViewModel.GROUP_TITLES));
                    },
                    "Players_General"),
                new SettingsSection("The sound of this machine", "Frontend",
                    "The screen that plays what is chosen, with its volume, its position and its controls",
                    delegate { return new PlaybackView(_nowPlaying); },
                    "Players_AudioPlayer"));
          },
          "Players");

      // /Plugins: one plate per settings class the server's parts keep, read from the server every time the
      // door opens. This is the reference's Plugins branch plate for plugin - Plugins_Weather beside
      // Plugins_Trakt - and it is why the plugin screen no longer lists the sections in one column of its own:
      // the wall outside already did that.
      model.AddFolder("Plugins", "Server",
          "What the server lets a television change in what its parts keep",
          () => PluginSettingsPages.PagesAsync(() => _home.Client),
          "Plugins");

      // /Shares: the folders the library reads, and what the server answers when a shared folder asks who is
      // calling. The reference filed both under Shares - Shares_SharesConfiguration beside
      // Shares_SharesOverview - for the same reason this front end does: a share is the server's business and
      // no one machine's, and the dictionary underneath always held one credential per path, not just the root.
      model.AddFolder("Shares", "Server",
          "The folders the library reads, and what the server offers shared folders on the network",
          delegate
          {
            return SettingsViewModel.PagesHere(
                new SettingsSection("Media sources", "Server",
                    "The folders the library reads, and their imports",
                    delegate { return new FoldersView(new FoldersViewModel(_home.Client)); },
                    "Shares_SharesConfiguration", "Shares"),
                new SettingsSection("Network credentials", "Server",
                    "What the server answers when a shared folder asks who is calling",
                    delegate
                    {
                      NetworkCredentialsViewModel credentials = new NetworkCredentialsViewModel(() => _home.Client);
                      return new NetworkCredentialsView { DataContext = credentials };
                    },
                    // The reference drew this plate with its own picture but gave it no icon of its own - it
                    // lent the branch's icon, which is what the plate's band carries here.
                    "Shares_SharesOverview", "Shares"));
          },
          "Shares");

      // /TV: where the television comes from. The reference had no such page on this side of the network
      // because its TV service ran on the same box; here the channels, the programmes and the timers all
      // belong to a TvHeadend elsewhere, so the branch holds the one question this machine answers about it.
      model.AddFolder("Television", "Frontend",
          "Which machine the channels, the guide and the recording timers come from",
          delegate
          {
            return SettingsViewModel.PagesHere(
                new SettingsSection("Where the channels come from", "TV",
                    "The address of the television backend, and what it is asked for",
                    delegate { return new TvSettingsView(); },
                    "TV"));
          },
          "TV");

      // /Users: who may sign in, read from the server's own user store the moment the page opens. The names
      // live on the server, not on any one machine, which is why the reference filed this as
      // "Settings -> Benutzer" - and behind its own "Settings.UserProfile" gate, because the section that
      // hands out PINs and restrictions hides from the people who were handed them. That is the whole point of
      // a key that only one person in the house has.
      if (_viewer.Allows(Theming.RestrictionGroups.SettingsUserProfile))
      {
        model.AddFolder("Users", "Server",
            "Who can sign in on the server, and what they must say",
            delegate
            {
              return SettingsViewModel.PagesHere(
                  new SettingsSection("Who signs in", "Server",
                      "The names on the server, the numbers they must know, and what each may reach",
                      delegate
                      {
                        UsersSettingsViewModel users = new UsersSettingsViewModel(
                            () => _home.Client, () => _home.Client, _viewer);
                        return new UsersSettingsView { DataContext = users };
                      },
                      "Users"));
            },
            "Users");
      }

      // The way out, the one plate that is a leaf: it opens no page of settings but the close screen itself.
      // The reference front ends kept leaving the application in dialogSystemState, a dialog every viewer found
      // because a television without a mouse still has to be left somehow; the settings hub is where this one
      // advertises it, and it pushes the screen rather than embedding it - the header and the back key belong
      // to the close screen, not to the hub.
      model.Add("Close", "Frontend", "Leave the application, whatever is playing",
          null, open: delegate { _workflow.Push(CloseFrame()); });

      return model;
    }

    /// <summary>
    /// The connection screen, lent to the settings hub's pane like every other page: the very screen the start
    /// answers with, so a household changes the server's address in one place and not in two copies of a form.
    /// What a save does stays the application's business - swap the client and ask the library again - but the
    /// hub keeps its wall and its back key here; nothing is pushed.
    /// </summary>
    private Control? ServerAddressPage()
    {
      ConnectionViewModel viewModel = new ConnectionViewModel(_settings, _settingsPath);
      viewModel.Saved += delegate (string url)
      {
        _home.UseServer(new ServerApiClient(new Uri(url, UriKind.Absolute)));
        _home.RefreshCommand.Execute(null);
      };
      ConnectionView view = new ConnectionView(viewModel);
      // The workflow is what usually tells a screen it is being looked at; in the pane that moment is the
      // attach, and the screen starts its search and lays its focus in the same words.
      view.AttachedToVisualTree += delegate { view.OnScreenActivated(); };
      return view;
    }

    /// <summary>
    /// The restriction group a named screen hides behind, or the empty word for a screen everybody may
    /// reach. The reference registered these per workflow state and per settings entry
    /// (<c>WorkflowResourcesLoader.cs:209</c>); here it is the handful of corners this home screen can
    /// point at, and everything that names no group stays everybody's - exactly the reference's rule for an
    /// element that carries no attribute.
    /// </summary>
    private static string GateGroupFor(string screenKey)
    {
      if (screenKey.Equals("settings", StringComparison.OrdinalIgnoreCase)
          || screenKey.Equals("themes", StringComparison.OrdinalIgnoreCase))
        return Theming.RestrictionGroups.Settings;
      return "";
    }

    /// <summary>
    /// The frame a menu tile asks for by name.
    /// </summary>
    /// <remarks>
    /// Built afresh every time, because the folders of a library are read when the screen opens - a viewer who
    /// added a share with another tool in between has to see it, and a screen kept alive between visits would show
    /// the answer from the last one.
    /// </remarks>
    private Workflow.WorkflowFrame? BuildNamedFrame(string screenKey)
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
        TvGuideViewModel guide = new TvGuideViewModel(kind, null, null, Views.ScreenThread.Run,
            ChannelPictures());
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
        TvSchedulesViewModel schedules = new TvSchedulesViewModel(finishedFirst: files, marshal: Views.ScreenThread.Run,
            mayDelete: () => _viewer.Allows(Theming.RestrictionGroups.DeleteRecording));
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
        // The reference hung the whole configuration tree behind one group, "Settings", and a person whose
        // list does not carry it never sees the door at all. The corner button reads that from the home
        // model; a tile that was pointed at "settings" by grown-up hands gets answered with nothing rather
        // than with the machine room - a press that opens nothing is the same answer the hidden button gives.
        if (!_viewer.Allows(Theming.RestrictionGroups.Settings))
          return null;

        // A fresh model per visit, unlike the party queue: the screens behind the sections read their state
        // when they open - somebody may have started this run with a --theme that overrode the saved choice,
        // or added a share with another tool in between.
        return new Workflow.WorkflowFrame("settings", "Settings", SettingsModel());
      }

      // The person button of the reference's master_home - its UserButton, which raised the login screen -
      // is this corner. It opens the very wake the machine starts on, this time as a switch: the household
      // laid out, a tap to change seats, and the door to step out of the room. It never stacks on itself -
      // the reference never raised its login screen twice - so a press while the switch stands answers
      // nothing. The old asking picker is gone: the switch lays out the same names, asks the PIN the
      // server asks, and works whether or not login is on.
      if (screenKey.Equals("viewer", StringComparison.OrdinalIgnoreCase))
      {
        foreach (Workflow.WorkflowFrame standing in _workflow.Stack)
        {
          if (standing.StateId == "startup")
            return null;
        }
        return StartupFrame(true);
      }

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
    /// A title pressed on the home screen - the content wall or the "left half way" strip. What stands inside
    /// it opens, the same frame the shelf opens when its viewer presses OK on an album: the reference's wall
    /// was built of albums, seasons and collections, and a wall that answers "the server did not name a file"
    /// to a press on an album is a wall that does not know its own library. Everything else plays.
    /// </summary>
    private void OnHomeTitlePressed(object? sender, TitleViewModel title)
    {
      if (title.HoldsTitles && sender is HomeViewModel home
          && home.Client != null && home.SelectedEntry?.Section is HomeSection section)
      {
        Console.WriteLine("inside:   " + title.Name + " - the titles inside it, from " + section.Label);
        _workflow.Push(TitlesFrame(
            new MediaBrowserViewModel(home.Client, section, title, home.Pictures), title.Name));
        return;
      }

      OnTitleChosen(sender, title);
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

      if (_nowPlaying.Play(location.Value, title.Title.Id, title.ResumePercent ?? 0, title.Name))
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

      if (_nowPlaying.Play(location.Value, title.Title.Id, title.ResumePercent ?? 0, title.Name))
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
