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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Caching;
using MediaPortal.Frontend.Skins.Nereus;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The home screen: which server this is, and what its media database is made of.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The view model is the half of the front end that can be tested without a screen, which is why the reading
  /// happens here and nothing about Avalonia appears in this file. It takes a <see cref="ServerApiClient"/> and
  /// nothing else, so a test can hand it a client pointed at a server that is really running.
  /// </para>
  /// <para>
  /// The awaits deliberately keep their context instead of calling <c>ConfigureAwait(false)</c>: the code after
  /// them touches <see cref="Aspects"/>, which the list on screen is bound to, and that has to happen on the
  /// thread that owns the visual tree.
  /// </para>
  /// </remarks>
  public partial class HomeViewModel : ObservableObject
  {
    private ServerApiClient? _client;
    private CancellationTokenSource? _runningRefresh;
    private CancellationTokenSource? _pendingContentSelection;
    private CancellationTokenSource? _contentLoadCancellation;
    private readonly NereusHomeMenuModel _nereusMenu = new NereusHomeMenuModel();

    /// <summary>
    /// How long the highlight has to stand on one tile before its wall is put up - the reference's own
    /// <c>HomeMenuModel.UPDATE_DELAY_MS</c>, which is what stopped its pane from being refilled for every tile a
    /// walker passed over. The reading is not held up by this (see <see cref="QueueContentForSelection"/>); only
    /// the wall going up is.
    /// </summary>
    private const int CONTENT_SELECTION_DELAY_MS = 500;

    // One run at a time may touch Aspects. Cancelling the older run is not enough for that: a cancelled run
    // carries on until its next check, and until then it is clearing and adding to the same collection the newer
    // run is writing. This is a chain of tasks rather than a SemaphoreSlim, because a Task needs no disposal -
    // a semaphore owned by a view model that gets thrown away is a fresh way to hand ObjectDisposedException to
    // a run that is still waiting on it, which is the very failure mode this class already documents.
    private readonly object _gateLock = new object();
    private Task _gate = Task.CompletedTask;

    [ObservableProperty]
    private string _status;

    [ObservableProperty]
    private string? _serverDescription;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private AspectViewModel? _selectedAspect;

    /// <summary>
    /// The menu entry the viewer is standing on, which is the entry whose lists the content pane next to the menu
    /// shows. Set by the screen's selection, and by <see cref="RebuildMenu"/> when the menu was rebuilt under it.
    /// </summary>
    [ObservableProperty]
    private MainMenuEntry? _selectedEntry;

    [ObservableProperty]
    private string _signInName = string.Empty;

    [ObservableProperty]
    private string? _signedInAs;

    /// <summary>
    /// Whether this server has refused the front end for not carrying a token. The reference's living rooms
    /// asked nobody who they were - login is a switch a household turns on for its server, not a turnstile in
    /// front of the library - so the sign-in boxes wait for the server to actually ask for them.
    /// </summary>
    [ObservableProperty]
    private bool _serverRefusesStrangers;

    /// <summary>
    /// Whether the sign-in boxes have a place on the screen: only while the server asks for a token and nobody
    /// has handed one over yet.
    /// </summary>
    public bool SignInOffered => ServerRefusesStrangers && SignedInAs == null;

    partial void OnSignedInAsChanged(string? value)
    {
      OnPropertyChanged(nameof(SignInOffered));
    }

    partial void OnServerRefusesStrangersChanged(bool value)
    {
      OnPropertyChanged(nameof(SignInOffered));
      if (value && !_signInNamesAsked)
        _ = LoadSignInNamesAsync();
    }

    /// <summary>
    /// The names the server keeps, as buttons beside the sign-in boxes: the reference let the household pick
    /// WHO was walking in instead of making them spell it. It is an offer, not a gate - the typed boxes stay,
    /// because a server without the users endpoint (or a household that would rather type) still signs in the
    /// old way. Filled once per server, the first time the server asks who is calling.
    /// </summary>
    public System.Collections.ObjectModel.ObservableCollection<string> SignInNames { get; } =
        new System.Collections.ObjectModel.ObservableCollection<string>();

    private bool _signInNamesAsked;

    private async Task LoadSignInNamesAsync()
    {
      ServerApiClient? client = _client;
      if (client == null)
        return;

      _signInNamesAsked = true;
      try
      {
        Api.UsersAnswer answer = await client.GetUsersAsync(CancellationToken.None);
        foreach (Api.UserInfo user in answer.Users ?? Array.Empty<Api.UserInfo>())
          if (user.Kind == "User")
            SignInNames.Add(user.Name);
      }
      catch (Exception)
      {
        // The picker is a convenience on top of the typed boxes; a server that will not name its users
        // leaves the boxes to speak, which is what they have always done.
      }
    }

    /// <summary>Picking a name from the server's list puts it in the box; the password still has to be said.</summary>
    [RelayCommand]
    public void PickSignInName(string? name)
    {
      if (!string.IsNullOrWhiteSpace(name))
        SignInName = name;
    }

    /// <summary>
    /// For the XAML designer and for the tests that only check what the screen looks like before anything was
    /// read. A view model built this way says so in its status line instead of pretending to be connected.
    /// </summary>
    public HomeViewModel()
        : this(null)
    {
    }

    /// <summary>
    /// The posters of the titles in the content pane, and their fanart for the background.
    /// </summary>
    /// <remarks>
    /// The reference's home pane is a wall of poster covers, not a list of names - <c>Nereus</c> drew the very
    /// same pictures the media grid draws, through the same per-title picture fetch MP2 cached by media item id.
    /// The two caches here are that mechanism: one asks for <c>Poster</c>, one for <c>FanArt</c>, and both ride
    /// on the server's fanart endpoint, which falls back to the pictures lying next to the files. A title the
    /// server has no poster for keeps its ground image, exactly as the grid's tile does.
    /// </remarks>
    private readonly Artwork? _posters;

    private readonly Artwork? _fanart;

    /// <summary>
    /// This machine's shelf for the pictures of the library, or <c>null</c> for a run that keeps them in memory
    /// only. Handed on to every screen that draws titles, and read by the settings screen that changes it.
    /// </summary>
    public PictureCache? Pictures { get; }

    public HomeViewModel(ServerApiClient? client, PictureCache? pictures = null)
    {
      _client = client;
      Pictures = pictures;
      _status = client == null ? "No server configured" : "Not connected yet";
      if (client != null)
      {
        _posters = PicturesOf(client, "Poster", id => client.GetFanArtAsync(id, "Poster"));
        _fanart = PicturesOf(client, "FanArt", id => client.GetFanArtAsync(id, "FanArt"));
      }

      // The face on the machine can change while this screen sits open - the skin settings are one move
      // away, and so is the skin screen - and the content column follows the new face at once. The reference
      // hung that on a binding that never went stale; these two events are this port's moment of change.
      FrontendTheme.Changed += delegate { RaiseFaceChanged(); };
      SkinEngine.Changed += delegate { RaiseFaceChanged(); };

      // The menu before anything has been read. A viewer who turns a television on before the server has finished
      // starting up is looking at this state, and it has to say what a media centre is - and what is missing from
      // it - rather than offering nothing to press.
      RebuildMenu(false);

      // The skin-settings screen is a remote control, not an application form: a viewer who turns the
      // fanart dimmer while the home stands there sees the television answer at once.
      SkinSettingsBase.Saved += delegate
      {
        OnPropertyChanged(nameof(FanartOverlay));

        // And the blur is answered by the standing backdrop itself: the copy it wears was thrown out of focus
        // at the turn before, so a new turn means making it again - away from the thread that draws, the way
        // everything of that size is done here.
        _ = WearBackgroundAsync(_backgroundRaw);
      };
    }

    /// <summary>
    /// One holder of one kind of picture: asked of this server, and looked for on this machine's shelf first
    /// when the machine keeps one. Without a shelf it is the memory-only holder this front end had before.
    /// </summary>
    private Artwork PicturesOf(ServerApiClient client, string kind, Func<Guid, Task<byte[]?>> ask)
    {
      return Pictures == null ? new Artwork(ask) : Pictures.ArtworkFor(kind, client.BaseUrl, ask);
    }

    public ObservableCollection<AspectViewModel> Aspects { get; } = new ObservableCollection<AspectViewModel>();

    /// <summary>
    /// Whether the home pane may say what pressing an entry does: the reference guarded that one label with
    /// Nereus' <c>EnableHelpTexts</c> ("help texts for menues on the home screen") and with nothing else -
    /// the continue-playing lists beside the menu are content and stay whatever the switch says. A machine
    /// wearing a face without that switch keeps the help it always showed.
    /// </summary>
    private static bool HelpTextsShown
    {
      get
      {
        return WornSkinSettingsOrNull() is not NereusSkinSettings nereus || nereus.EnableHelpTexts;
      }
    }

    /// <summary>The worn skin's own answers, or null while the machine wears a face that ships none.</summary>
    private static SkinSettingsBase? WornSkinSettingsOrNull()
    {
      Application? app = Application.Current;
      return app == null
          ? null
          : SkinSettingsBase.LoadFor(FrontendTheme.CurrentName(app), null, out _);
    }

    /// <summary>
    /// Whether the pane beside the menu follows the walking highlight. Nereus kept two switches for when it
    /// must not: <c>EnableMenuSelection</c> ("a menu item has to be selected before it works") and its
    /// touch-screen twin <c>EnableTouchDisplay</c> both made the reference fill the pane only when the viewer
    /// stood and pressed a tile - HomeMenuModel updated the content on the explicit selection and left the
    /// pane alone while the highlight moved. With both switches off it followed the highlight, after a
    /// moment's delay; this port follows it at once, because its lists fill themselves in as the answers
    /// arrive and there is no waiting worth hiding behind.
    /// </summary>
    private static bool ContentFollowsHighlight
    {
      get
      {
        return WornSkinSettingsOrNull() is not NereusSkinSettings nereus
            || (!nereus.EnableMenuSelection && !nereus.EnableTouchDisplay);
      }
    }

    /// <summary>
    /// Which folder of the worn skin the home tiles ask their pictures from: Nereus keeps its home tiles
    /// twice, square in <c>HomeMenu</c> and round in <c>HomeMenuRound</c>, and
    /// <c>EnableCircleMenuImages</c> is the reference's one style over exactly that choice. Every other
    /// face, and Nereus with the switch off, reads the square folder it has always read.
    /// </summary>
    public static string HomeTileFolder
    {
      get
      {
        return WornSkinSettingsOrNull() is NereusSkinSettings { EnableCircleMenuImages: true }
            ? "HomeMenuRound"
            : "HomeMenu";
      }
    }

    /// <summary>
    /// Whether a tile has to be pressed before anything moves - the same answer the content pane is steered
    /// by, which the mouse also asks for: HomeMenuModel executed a clicked tile at once on a desktop (both
    /// switches off) and made a touch viewer select first and press again to work the tile.
    /// </summary>
    public static bool WaitsForAPress
    {
      get { return !ContentFollowsHighlight; }
    }

    /// <summary>
    /// Whether the home may put content beside its menu at all. WMC was the skin built on those panels, and
    /// its settings screen carried exactly one switch for them: with <c>EnableHomeContent</c> off,
    /// master_homemenu hid the entire HomeContentGrid and left the menu standing on an empty set. Nereus
    /// never shipped such a switch; a machine wearing a face without the switch keeps the content it always
    /// had, because a switch that was never offered must not silently answer for it.
    /// </summary>
    public bool HomeContentShown
    {
      get { return HomeContentShownFor(WornSkinSettingsOrNull()); }
    }

    internal static bool HomeContentShownFor(SkinSettingsBase? worn)
    {
      return worn is not WMCSkinSettings wmc || wmc.EnableHomeContent;
    }

    /// <summary>
    /// Which home arrangement the worn face asks for: "nereus", "wmc", or "plain".
    /// </summary>
    /// <remarks>
    /// The reference skins are not only pictures - each carries its own home screen of
    /// <c>master_homemenu.xaml</c> and its own <c>HomeMenuModel</c>, and the two differ in where things
    /// stand, not just in how a tile looks. Nereus slides a narrow tile rail beside a content grid that
    /// fills the rest of the screen under two black gradients; WMC stands its menu well in from the edge
    /// and hangs its content pane top-right, 1000x615 out of 1280x720. A face that ships no home of its
    /// own gets the plain arrangement this port drew before any skin was worn. The view reads this and
    /// moves its own areas around; the lists, the entries, and the keys that walk them stay the same.
    /// </remarks>
    public string HomeLayout
    {
      get { return HomeLayoutFor(WornSkinSettingsOrNull()); }
    }

    /// <summary>The arrangement the given worn settings ask for - the answer <see cref="HomeLayout"/> gives.</summary>
    internal static string HomeLayoutFor(SkinSettingsBase? worn)
    {
      return worn is NereusSkinSettings
          ? "nereus"
          : worn is WMCSkinSettings ? "wmc" : "plain";
    }

    /// <summary>Nereus' arrangement: the tile rail at the edge, the content beside it, two black gradients.</summary>
    public bool IsNereusHome
    {
      get { return HomeLayout == "nereus"; }
    }

    /// <summary>WMC's arrangement: the menu stood in from the edge, the content pane top-right.</summary>
    public bool IsWmcHome
    {
      get { return HomeLayout == "wmc"; }
    }

    /// <summary>
    /// How wide a main menu tile stands on this screen, and how wide the column it stands in is.
    ///
    /// <para>
    /// A menu tile is square, so one number sizes it, and the column that carries the rail has to follow it or the
    /// tiles hang out over the ground the skin paints behind them. Both default to the numbers the Nereus face drew
    /// (<c>170</c> in a <c>200</c> column, at 720 lines), which is what a face that says nothing about its rail gets.
    /// A face that does say sizes its rail by a share of the screen's height in its own metrics - the screen scales
    /// that number when it is laid out, because only the screen knows how many lines it has.
    /// </para>
    /// </summary>
    [ObservableProperty]
    private double _menuTileSize = MENU_TILE_DRAWN;

    /// <inheritdoc cref="MenuTileSize"/>
    [ObservableProperty]
    private double _menuRailWidth = MENU_RAIL_DRAWN;

    /// <summary>The square Nereus drew its main menu tiles at, which is also their size when no metric says otherwise.</summary>
    public const double MENU_TILE_DRAWN = 170;

    /// <summary>The column Nereus drew its main menu rail in, beside the tiles' own margins.</summary>
    public const double MENU_RAIL_DRAWN = 200;

    /// <summary>The gap master_home.xaml leaves between the rail and everything standing beside it.</summary>
    public const double CONTENT_GAP = 40;

    /// <summary>
    /// What the band of leftover plugins, and the last tile of the main menu that opens it, are called. The
    /// reference keeps the name in one string - <c>Nereus.Home.OtherPlugins</c> - and spells it "Others" in both
    /// places, so the tile in the rail and the head over the mosaic say the same word.
    /// </summary>
    public const string OTHER_PLUGINS_HEADING = "Others";

    /// <summary>
    /// Where the content begins: the rail as this panel made it, and the gap the reference leaves beside it.
    ///
    /// <para>
    /// The wall, the line of text under it and the buttons at the top edge all begin here, so a rail that grows
    /// carries them along instead of leaving them under its tiles. A face writes this into its own markup with
    /// <c>SkinInsetConverter</c>, which keeps the vertical offsets the face drew and moves only the left edge.
    /// </para>
    /// </summary>
    public double HomeContentLeft => Math.Round(MenuRailWidth) + CONTENT_GAP;

    partial void OnMenuRailWidthChanged(double value)
    {
      OnPropertyChanged(nameof(HomeContentLeft));
    }

    /// <summary>Neither rescued skin: the arrangement this port had before either face was worn.</summary>
    public bool IsPlainHome
    {
      get { return HomeLayout == "plain"; }
    }

    /// <summary>
    /// The parts of this server a viewer can sit down and watch, in the order the tiles are arranged.
    /// </summary>
    /// <remarks>
    /// Filled at the end of a refresh, after the counts arrived, because a tile that said "?" until somebody
    /// looked at the aspect list below it would be a tile that lies about the library half the time.
    /// </remarks>
    public ObservableCollection<HomeSection> Sections { get; } = new ObservableCollection<HomeSection>();

    /// <summary>
    /// The main menu: the parts a media centre is made of, each one either openable or saying what is missing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what the home screen shows, and it is on screen before any server has answered, which is the whole
    /// point of it. The tiles used to be the server's own aspect list filtered down to the watchable ones, so a
    /// front end that had not reached a server - a server still booting, a machine whose address changed, a
    /// television that was switched on first - offered nothing to press and looked broken rather than idle.
    /// </para>
    /// <para>
    /// The names and the order are MP2's (see <see cref="MainMenu"/>), and the counts come from the same read that
    /// fills <see cref="Sections"/>, so a tile saying "4 films" is still the server's own number rather than one
    /// this screen kept around.
    /// </para>
    /// </remarks>
    public ObservableCollection<MainMenuEntry> Menu { get; } = new ObservableCollection<MainMenuEntry>();

    /// <summary>The worn skin's menu; Nereus projects its own configured list, while other skins use the shared list.</summary>
    public IEnumerable<MainMenuEntry> DisplayMenu
    {
      get { return IsNereusHome ? _nereusMenu.MainMenuItems : Menu; }
    }

    /// <summary>
    /// The row of the content pane the viewer is standing on.
    /// </summary>
    /// <remarks>
    /// The reference's home screen always has one poster picked out, and everything else on the screen answers
    /// to that choice: the line at the bottom left names its title, and the picture behind the whole screen is
    /// its fanart. Binding the list's selection here is what lets those two follow the walk rather than the
    /// last thing that was read.
    /// </remarks>
    public HomeRow? SelectedContent
    {
      get { return _selectedContent; }
      set
      {
        if (ReferenceEquals(_selectedContent, value))
          return;
        _selectedContent = value;
        OnPropertyChanged(nameof(SelectedContent));
        OnPropertyChanged(nameof(ContentInfo));
        OnPropertyChanged(nameof(HasContentInfo));
        _ = ShowFanartAsync(value?.Title);
      }
    }

    private HomeRow? _selectedContent;

    /// <summary>
    /// What the reference shows at the bottom left of the home screen about the chosen title: its name, the year,
    /// how long it runs, and how much of it has been watched - the facts the row itself carries, since asking the
    /// detail reader for a rating would put a second request behind every step of the walk.
    /// </summary>
    public string ContentInfo
    {
      get
      {
        TitleViewModel? title = _selectedContent?.Title;
        if (title == null)
          return string.Empty;

        var parts = new List<string>();
        if (title.Title.Year is int year)
          parts.Add(year.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (title.Title.RuntimeMinutes is int minutes && minutes > 0)
          parts.Add(minutes + " min");
        if (title.ResumeText.Length > 0)
          parts.Add(title.ResumeText);

        return parts.Count == 0 ? title.Name : title.Name + "   ·   " + string.Join("   ·   ", parts);
      }
    }

    public bool HasContentInfo
    {
      get { return _selectedContent?.Title != null; }
    }

    /// <summary>
    /// The fanart of the chosen title, behind the whole home screen, or null when the screen has none to show.
    /// </summary>
    /// <remarks>
    /// Both skins put the chosen title's fanart behind the home screen - Nereus full and dimmed by the top
    /// gradient, WMC beneath its own background - and both offer a switch for it, which is obeyed here: a face
    /// that says <c>EnableFanart = false</c> gets the plain background it asks for.
    /// </remarks>
    public byte[]? BackgroundArtwork
    {
      get { return _backgroundArtwork; }
      private set
      {
        if (ReferenceEquals(_backgroundArtwork, value))
          return;
        _backgroundArtwork = value;
        OnPropertyChanged(nameof(BackgroundArtwork));
        OnPropertyChanged(nameof(HasBackgroundArtwork));
      }
    }

    private byte[]? _backgroundArtwork;

    public bool HasBackgroundArtwork
    {
      get { return _backgroundArtwork != null; }
    }

    /// <summary>
    /// How strongly the standing title's fanart shows: the skin settings' number is worn as the picture's
    /// own opacity - the slider on the skin-settings screen is the fanart's visibility, and the ground
    /// behind it shows through what the number leaves away. A face that ships no settings wears Nereus'
    /// own default; WMC's settings say 1.0, which is its way of wearing its fanart plain.
    /// </summary>
    public double FanartOverlay
    {
      get { return WornSkinSettingsOrNull()?.FanartOverlayOpacity ?? 0.85; }
    }

    /// <summary>
    /// How far the standing fanart is thrown out of focus, in pixels: Nereus' own dial, because Nereus is the
    /// face that wears a title's picture across the whole screen. A face that speaks no blur leaves the
    /// picture as the library drew it, which is what the plain arrangement and WMC have always shown.
    /// </summary>
    public double FanartBlurRadius
    {
      // Nought while the screen wears a copy that was already thrown out of focus: such a picture says so by
      // itself, and blurring it a second time would buy nothing but another sixth of a second.
      get { return _backgroundBaked ? 0 : FanartBlurFor(WornSkinSettingsOrNull()); }
    }

    /// <summary>
    /// The blur the face worn asks for: only Nereus speaks it, because only Nereus wears a title's picture
    /// across the whole screen, and every other face - including a machine wearing no face at all - leaves the
    /// fanart as the library drew it.
    /// </summary>
    public static double FanartBlurFor(SkinSettingsBase? worn)
    {
      return worn is NereusSkinSettings nereus ? nereus.FanartBlurRadius : 0;
    }

    private int _fanartGeneration;

    /// <summary>
    /// The chosen title's fanart, fetched and put behind the screen - unless the worn face or the plain layout
    /// says it should not be there at all.
    /// </summary>
    private async Task ShowFanartAsync(TitleViewModel? title)
    {
      int generation = ++_fanartGeneration;

      Artwork? fanart = _fanart;
      SkinSettingsBase? worn = WornSkinSettingsOrNull();
      if (title == null || fanart == null || IsPlainHome || worn?.EnableFanart == false)
      {
        _ = WearBackgroundAsync(null);
        return;
      }

      try
      {
        byte[]? picture = await fanart.LoadAsync(title.Title.Id);
        // Only a picture that came replaces what stands there. The reference's background image simply
        // kept its last source when a title carried no fanart, and so this one does: walking past a title
        // without art should darken the screen back to bare background only once the next title proves it
        // has no art either - which it cannot prove by answering nothing.
        if (picture != null && generation == Volatile.Read(ref _fanartGeneration))
          _ = WearBackgroundAsync(picture);
      }
      catch (ServerApiException)
      {
        // A fanart the server will not hand over leaves the background as it was. The names beside the menu are
        // the point of this pane; the picture behind them is a guest.
      }
    }

    private byte[]? _backgroundRaw;
    private bool _backgroundBaked;
    private int _backgroundTurn;

    /// <summary>
    /// Putting a fetched backdrop on the screen, thrown out of focus by however far the worn face asks for -
    /// which is arithmetic of about a sixth of a second, so it is done away from the thread that draws and the
    /// screen keeps wearing what it wore until the copy is ready.
    /// </summary>
    /// <remarks>
    /// Walking on while a copy is being made is answered by the turn the picture was worn for: the copy of a
    /// backdrop the viewer has left behind is never put on the screen, however quickly it comes. A copy that is
    /// already in <see cref="BackgroundBlur"/> answers at once, which is what makes walking back over a wall as
    /// quick as walking over it the first time.
    /// </remarks>
    private async Task WearBackgroundAsync(byte[]? raw)
    {
      int turn = ++_backgroundTurn;
      _backgroundRaw = raw;

      double radius = FanartBlurFor(WornSkinSettingsOrNull());
      if (raw == null || radius <= 0)
      {
        ShowBackground(raw, false);
        return;
      }

      byte[]? blurred = await BackgroundBlur.BlurredAsync(raw, radius);

      // The viewer walked on, or the face - and with it the question - changed in the meantime.
      if (turn != Volatile.Read(ref _backgroundTurn))
        return;

      ShowBackground(blurred ?? raw, blurred != null);
    }

    /// <summary>
    /// What the screen wears behind everything. Said together with how far it still has to be thrown out of
    /// focus, because the two are one answer: a copy that comes already blurred is worn at nought.
    /// </summary>
    private void ShowBackground(byte[]? bytes, bool baked)
    {
      _backgroundBaked = baked;
      BackgroundArtwork = bytes;
      OnPropertyChanged(nameof(FanartBlurRadius));
    }

    /// <summary>
    /// One poster of the wall: fetched through the shared cache and shown on its row once it arrives, provided
    /// the pane has not been rebuilt in the meantime.
    /// </summary>
    private async Task PutPosterAsync(TitleViewModel row, int generation)
    {
      Artwork? posters = _posters;
      if (posters == null)
        return;

      try
      {
        byte[]? picture = await posters.LoadAsync(row.Title.Id);
        if (generation == Volatile.Read(ref _contentGeneration))
          row.ShowArtwork(picture);
      }
      catch (ServerApiException)
      {
        // The row keeps the skin's ground image, which is what a library without a picture looks like.
      }
    }

    /// <summary>
    /// The face on the machine changed: which content column may show and which folder the tiles wear their
    /// pictures from are both the worn face's answers, so every open question gets asked again.
    /// </summary>
    private void RaiseFaceChanged()
    {
      RebuildNereusMenu();
      if (IsNereusHome && !_nereusMenu.ContainsMainEntry(SelectedEntry))
        SelectedEntry = RememberedEntry() ?? _nereusMenu.MainMenuItems.FirstOrDefault();

      OnPropertyChanged(nameof(HomeContentShown));
      OnPropertyChanged(nameof(HomeLayout));
      OnPropertyChanged(nameof(IsNereusHome));
      OnPropertyChanged(nameof(IsWmcHome));
      OnPropertyChanged(nameof(IsPlainHome));
      OnPropertyChanged(nameof(FanartOverlay));
      OnPropertyChanged(nameof(DisplayMenu));
      foreach (MainMenuEntry entry in Menu)
        entry.RevealHomeTileFolder();

      // Which face is worn decides whether the standing backdrop is out of focus at all, so it is worn again
      // under the new face's answer.
      _ = WearBackgroundAsync(_backgroundRaw);
    }

    /// <summary>
    /// Pressing a menu entry: the part of the library to open, or null when this entry cannot be opened - in which
    /// case the status line says why.
    /// </summary>
    /// <remarks>
    /// Returning the section rather than raising an event keeps the decision and its reason in one place: the entry
    /// knows what it is missing, and this is the screen that knows what the viewer pressed. The window opens what
    /// comes back and does not have to work out whether a null meant "no" or "not yet".
    /// </remarks>
    public HomeSection? Open(MainMenuEntry? entry)
    {
      if (entry == null)
      {
        Status = "Nothing selected";
        return null;
      }

      if (_nereusMenu.IsOtherPluginsEntry(entry))
        return null;

      HomeSection? section = entry.Section;
      if (section == null)
      {
        // A tile that opens a screen instead of a shelf has nothing for the status line to explain - the window
        // opens the screen, and a screen with no folders on it says more than this line could.
        if (entry.ScreenKey == null)
          Status = entry.Label + " - " + entry.Detail;
        return null;
      }

      return section;
    }

    /// <summary>
    /// Rebuilds the menu from what the server holds. Called wherever <see cref="Sections"/> is refilled, so that a
    /// tile's number and a tile's aspect can never come from two different readings.
    /// </summary>
    /// <summary>
    /// Raised after a connected read has laid the menu out: the wake-up answer "last shelf" can only be
    /// honoured once the library's shelves are known to hold (or not hold) the remembered one, and they
    /// are known nowhere earlier than here.
    /// </summary>
    public event EventHandler? MenuRebuilt;

    private void RebuildMenu(bool connected)
    {
      // Which entry the viewer was standing on, remembered across the rebuild. A refresh that left the selection at
      // the top of the menu would also refill the content pane with the wrong entry's lists, which is a long way
      // from what the viewer was looking at a second ago.
      string? wasSelected = SelectedEntry?.Mp2Action;
      string? wasSelectedId = SelectedEntry?.HomeImageId;

      Menu.Clear();
      foreach (MainMenuEntry entry in MainMenu.Build(Aspects, connected))
        Menu.Add(entry);
      RebuildNereusMenu();

      // The playing mark is a fact about playback, not about this reading, and it has to survive the rebuild -
      // a refresh landing in the middle of a film would otherwise take the mark off the tile without a word.
      ApplyPlaying();

      IEnumerable<MainMenuEntry> selectionCandidates = IsNereusHome ? _nereusMenu.MainMenuItems : Menu;
      foreach (MainMenuEntry entry in selectionCandidates)
      {
        if ((wasSelected != null && entry.Mp2Action == wasSelected)
            || (wasSelectedId != null && string.Equals(entry.HomeImageId, wasSelectedId, StringComparison.OrdinalIgnoreCase)))
        {
          // Going through the property, so the pane is refilled from the fresh reading; a menu rebuilt behind a
          // viewer who is standing on "Movies" has to show the counts that arrived in this reading.
          SelectedEntry = entry;
          RaiseMenuRebuilt(connected);
          return;
        }
      }

      // Nothing was selected and nothing survives the rebuild: the reference still did not start the menu at
      // its top - it restored the last pressed action from the settings, so that home opens on the shelf the
      // viewer last stood on rather than on whichever tile happens to be first.
      SelectedEntry = RememberedEntry();
      if (SelectedEntry == null && IsNereusHome)
        SelectedEntry = _nereusMenu.MainMenuItems.FirstOrDefault();
      if (SelectedEntry != null && !ContentFollowsHighlight)
        _ = ShowContentAsync(SelectedEntry);
      OnPropertyChanged(nameof(DisplayMenu));
      RaiseMenuRebuilt(connected);
    }

    private void RebuildNereusMenu()
    {
      NereusSkinSettings? settings = WornSkinSettingsOrNull() as NereusSkinSettings;
      _nereusMenu.Rebuild(Menu, settings?.HomeMenuActionIds ?? NereusSkinSettings.DefaultHomeMenuActionIds);
      OnPropertyChanged(nameof(DisplayMenu));
    }

    /// <summary>
    /// Lays the home menu out again after Nereus' own menu editor wrote new answers: the tiles come out of the
    /// file now, the tile the viewer was standing on keeps its place when it survived, and the wall beside it is
    /// read again because the lists it pulls its previews from may have changed while it stood there.
    /// </summary>
    /// <remarks>
    /// The reference needed no such call - its settings watcher saw the two arrays change and relaid the menu and
    /// the content by itself. This port's watcher is this method, and the editor calls it once, after its one
    /// write. Nothing here rebuilds the entries themselves: which tiles exist is the server's answer, and that did
    /// not change while the viewer was moving tiles about.
    /// </remarks>
    public void ApplyNereusMenuEdit()
    {
      string? wasAction = SelectedEntry?.Mp2Action;
      string? wasId = SelectedEntry?.HomeImageId;

      RebuildNereusMenu();

      MainMenuEntry? standing = _nereusMenu.MainMenuItems.FirstOrDefault(entry =>
          (wasAction != null && entry.Mp2Action == wasAction)
          || (wasId != null && string.Equals(entry.HomeImageId, wasId, StringComparison.OrdinalIgnoreCase)));
      SelectedEntry = standing ?? _nereusMenu.MainMenuItems.FirstOrDefault();

      // A tile that stayed the very same object tells the pane nothing - a property that did not change raises no
      // change - while the lists it reads may have been reordered, thinned out or emptied a moment ago. So the
      // wall is read again by hand, which is the whole point of a menu editor whose previews change under the eye.
      if (SelectedEntry != null)
        _ = ShowContentAsync(SelectedEntry);

      RaiseMenuRebuilt(true);
    }

    private void RaiseMenuRebuilt(bool connected)
    {
      if (connected)
        MenuRebuilt?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// What the application last said is playing, in the server's aspect spelling, or null when nothing is.
    /// Kept so a menu rebuilt under a running film repaints its mark from the truth rather than from amnesia.
    /// </summary>
    private string? _playingAspect;

    /// <summary>
    /// Marks the menu tile whose shelf is being watched and takes the mark off every other tile.
    /// </summary>
    /// <param name="aspect">
    /// The aspect of what is playing, or null when nothing is. An episode marks the series' tile as much as the
    /// series does - pressing an episode is using the series' shelf - while a mark that could only guess goes
    /// off entirely, because a tile claiming the wrong thing is worse than one claiming nothing.
    /// </param>
    public void ShowPlaying(string? aspect)
    {
      _playingAspect = aspect;
      ApplyPlaying();
    }

    private void ApplyPlaying()
    {
      foreach (MainMenuEntry entry in Menu)
      {
        string? playing = _playingAspect;
        entry.IsPlaying = !string.IsNullOrEmpty(playing)
            && entry.Aspects.Any(aspect => MediaCatalog.SameShelf(aspect, playing));
      }
    }

    /// <summary>
    /// The content pane: the lists of the menu entry the viewer is standing on, one after another, with their
    /// headings.
    /// </summary>
    /// <remarks>
    /// This is the half of MP2's home screen that the tiles never carried. <see cref="Menu"/> answers "which part of
    /// the library?", and the pane answers "what is in it, and what would you watch tonight?" - the same split the
    /// old client made between <c>MainMenu</c> and the <c>HomeContent</c> lists of the highlighted entry. It is one
    /// flat list of <see cref="HomeRow"/>s rather than four lists, because a remote can only stop on something it
    /// can walk into, and a heading nobody can stop on is decoration.
    /// </remarks>
    public ObservableCollection<HomeRow> ContentRows { get; } = new ObservableCollection<HomeRow>();

    /// <summary>The Nereus shortcut rail and horizontally arranged media mosaics.</summary>
    public ObservableCollection<HomeContentGroup> ContentGroups { get; } =
        new ObservableCollection<HomeContentGroup>();

    /// <summary>Whether the pane has anything to say, which is what keeps its heading off an empty screen.</summary>
    public bool HasContent
    {
      get { return ContentRows.Count > 0; }
    }

    /// <summary>
    /// Which reading of the pane is the current one. Only the newest is allowed to keep writing, because walking
    /// the menu with a remote fires a load per stop, and an older answer arriving late would append its titles under
    /// a newer entry's heading.
    /// </summary>
    private int _contentGeneration;

    /// <summary>
    /// Refills the content pane for <paramref name="entry"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A list is only put on screen once it has items - the reference's rule (AbstractHomeContent shows a
    /// home content wrapper only when it HasItems), which also means no heading, and no line of apology,
    /// ever stands for an empty shelf. The lists are read one at a time in their own order, so the pane
    /// still fills band by band while the answers come in.
    /// </para>
    /// <para>
    /// One list refusing leaves its own line and does not hide the others, which is the rule every other read on
    /// this screen follows. A 403 is worth naming out loud though: three refusals in a row on a server that wants a
    /// sign-in read as an empty library unless the refusal says what it is.
    /// </para>
    /// </remarks>
    public async Task ShowContentAsync(MainMenuEntry? entry)
    {
      CancellationTokenSource? pending = Interlocked.Exchange(ref _pendingContentSelection, null);
      pending?.Cancel();
      await ShowContentAsync(entry, CancellationToken.None);
    }

    private Task ShowContentAsync(MainMenuEntry? entry, CancellationToken cancellation)
      => ShowContentAsync(entry, cancellation, TimeSpan.Zero);

    /// <summary>
    /// Reads one entry's wall and puts it up, holding the wall back for <paramref name="settling"/> first.
    /// </summary>
    /// <remarks>
    /// The reading is asked for in the beat the tile was stepped onto, before anything is waited for. The reference
    /// waited before it read at all (<c>HomeMenuModel.EnqueueUpdate</c>), which suited a skin that would otherwise
    /// refill its pane for every tile a walker passed over; nothing is hidden by reading here, because the answer
    /// is only put on the screen when the settling has run. What that leaves out of the wait is the reading itself,
    /// which on a local server is a few tens of milliseconds per shelf.
    /// </remarks>
    private async Task ShowContentAsync(MainMenuEntry? entry, CancellationToken cancellation, TimeSpan settling)
    {
      CancellationTokenSource current = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
      CancellationTokenSource? previous = Interlocked.Exchange(ref _contentLoadCancellation, current);
      previous?.Cancel();
      int generation = Interlocked.Increment(ref _contentGeneration);

      // Read into collections nothing on screen is looking at, so that the settling and the reading run side by
      // side and the wall can go up whole in the first beat after the settling.
      var wall = new List<HomeContentGroup>();
      var rows = new List<HomeRow>();
      Task reading = ReadWallAsync(entry, generation, current.Token, wall, rows);

      try
      {
        if (settling > TimeSpan.Zero)
          await Task.Delay(settling, current.Token);

        await ShowContentCoreAsync(entry, generation, current.Token, reading, wall, rows);
      }
      finally
      {
        Interlocked.CompareExchange(ref _contentLoadCancellation, null, current);
        current.Dispose();
      }
    }

    /// <summary>
    /// What the view is asked to do between reading a wall and putting it up: take the wall the viewer is looking
    /// at down out of its light, and answer once that has been on the screen long enough to be seen. The model
    /// waits here, which is the whole point - a wall that goes up in the same beat the old one came down is a cut,
    /// and so is a fade the screen never got round to drawing. A view that fades nothing (no Nereus home, or no
    /// wall standing) answers at once, and the wall goes up immediately.
    /// </summary>
    public Func<Task>? WallHandover { get; set; }

    /// <summary>
    /// What the view is asked to do the moment a rail tile is stepped onto, while the settling has not run and
    /// nothing has been read yet: start the wall that stands there on its way down.
    /// </summary>
    /// <remarks>
    /// The step is the moment the viewer decided something, and a screen that answers a step a full settling later
    /// looks thoughtless however short the settling is. So the way down belongs to the step and the reading happens
    /// while the wall is already going out, which takes the fade's length out of the wait instead of adding it to
    /// the end. <see cref="WallHandover"/> then answers as soon as the curve has been played - usually before the
    /// reading has come back at all. A view that fades nothing leaves this unsaid, and a wall that is already down
    /// is not put down twice.
    /// </remarks>
    public Action? WallLeaving { get; set; }

    /// <summary>
    /// Reads one entry's wall and puts it up: once, whole, and in one beat.
    /// </summary>
    /// <remarks>
    /// What used to happen - the pane emptied in the frame the switch was made, and every list going onto the wall
    /// as its own answer arrived, each of them restarting the pane's fade - is the pulse every viewer sees when
    /// they walk along the menu. Neither half of it is needed. A wall is read into <paramref name="wall"/> and
    /// <paramref name="rows"/>, which nothing on screen is looking at, and goes up when the reading is through and
    /// the view has taken the wall before it down, so what is watched is one picture fading out and its successor
    /// fading up - never a wall being built, and never two walls on the screen at once.
    /// </remarks>
    private async Task ShowContentCoreAsync(MainMenuEntry? entry, int generation, CancellationToken cancellation,
        Task reading, List<HomeContentGroup> wall, List<HomeRow> rows)
    {
      // Asked for before the settling started, so this usually waits on an answer that arrived long ago.
      await reading;

      // Somebody walked on while this was being read. The wall it left standing belongs to the entry being looked
      // at now, and this reading has nothing left to say about it.
      if (generation != Volatile.Read(ref _contentGeneration) || cancellation.IsCancellationRequested)
        return;

      // The way down, which the reading cannot be waited for afterwards: the fade has to happen between two walls
      // that are both known, and by now the one being put up is. Waiting here costs the length of the fade and
      // nothing else - the server has already answered.
      Func<Task>? handover = WallHandover;
      if (handover != null)
        await handover();

      // And somebody can still have walked on during that fade, in which case the wall they left is not this one's
      // business either.
      if (generation != Volatile.Read(ref _contentGeneration) || cancellation.IsCancellationRequested)
        return;

      _paneEntry = entry;
      PutUpWall(wall, rows);

      // The reference's home screen never stands on nothing: the walker is put on the first cover the moment the
      // wall is up, which is also what picks the fanart behind the screen and the line at its bottom left.
      if (SelectedContent == null)
      {
        foreach (HomeRow row in ContentRows)
        {
          if (!row.IsTitle)
            continue;

          SelectedContent = row;
          break;
        }
      }

      OnPropertyChanged(nameof(HasContent));

      // The wall the viewer came for is up, which is the answer the wake waits for before it steps aside.
      LibraryReady?.Invoke();
    }

    /// <summary>
    /// Swaps the whole wall over in one beat, on the dark screen the view has just made: the bands go on complete
    /// and with their rows for the pane the other faces read, so the wall the viewer sees coming up in the fade is
    /// a finished wall rather than one being filled.
    /// </summary>
    private void PutUpWall(List<HomeContentGroup> wall, List<HomeRow> rows)
    {
      ContentRows.Clear();
      foreach (HomeRow row in rows)
        ContentRows.Add(row);

      ContentGroups.Clear();
      foreach (HomeContentGroup band in wall)
        ContentGroups.Add(band);

      SelectedContent = null;
    }

    /// <summary>
    /// Reads one entry's lists into the wall and rows handed in, and leaves them for <see cref="PutUpWall"/>.
    /// </summary>
    private async Task ReadWallAsync(MainMenuEntry? entry, int generation, CancellationToken cancellation,
        List<HomeContentGroup> wall, List<HomeRow> rows)
    {
      if (entry == null)
        return;

      // The leftover plugins get the band the reference gives them: one band of the face's widest measure, laid
      // out in the mosaic its SquareTileLayout5Template draws - nineteen places, the first, sixth and thirteenth
      // square twice as big as the rest - and nineteen is also the ceiling, because the reference hands the whole
      // list to that one grid and a twentieth leftover plugin simply has no square to stand on. The list beside
      // the menu still names every one of them (a list scrolls, a mosaic cannot), and the place a leftover wants
      // is made in the menu editor, where it is assigned to a menu entry.
      if (_nereusMenu.IsOtherPluginsEntry(entry))
      {
        int places = Math.Min(HomeContentGroup.PLUGINS_BAND_SLOTS, _nereusMenu.OtherMenuItems.Count);
        HomeContentGroup plugins = new HomeContentGroup(OTHER_PLUGINS_HEADING, HomeContentGroup.PLUGINS_LAYOUT,
            1500, places);
        for (int i = 0; i < _nereusMenu.OtherMenuItems.Count; i++)
        {
          MainMenuEntry otherEntry = _nereusMenu.OtherMenuItems[i];
          rows.Add(HomeRow.For(otherEntry));
          if (i < places)
            plugins.AddMenuEntry(otherEntry);
        }
        if (plugins.Tiles.Count > 0)
          wall.Add(plugins);
        return;
      }

      IReadOnlyList<NereusHomeShortcut> shortcuts = _nereusMenu.ShortcutsFor(entry);
      if (shortcuts.Count > 0)
      {
        HomeContentGroup shortcutGroup = new HomeContentGroup("Shortcuts", "shortcuts", 175, shortcuts.Count);
        foreach (NereusHomeShortcut shortcut in shortcuts)
          shortcutGroup.AddShortcut(shortcut);
        wall.Add(shortcutGroup);
      }

      string[] mediaListSettings = (WornSkinSettingsOrNull() as NereusSkinSettings)?.HomeMenuActionMediaLists
          ?? Array.Empty<string>();
      IReadOnlyList<HomeContentList> lists = HomeContentList.For(entry, mediaListSettings);
      if (lists.Count == 0)
      {
        // An entry that opens a whole screen, or one nothing backs. Saying what pressing does is the useful thing;
        // the pane would otherwise be an empty column next to a menu that is anything but empty. It is also the
        // reference's home help text, and Nereus lets its viewer switch help texts off - off is obeyed here and
        // only here, because the lists beside the menu are content, not help.
        if (HelpTextsShown)
          rows.Add(HomeRow.Note(entry.Openable ? "Press Enter: " + entry.Action : entry.Detail));
        return;
      }

      // Which lists the pane shows, the reference answers in one line of its own (AbstractHomeContent.cs:
      // "if (wrapper.HasItems)"). A list is handed to the home content - heading and all - only when it has
      // items; a shelf with nothing on it was never shown, under any heading, with no line of explanation.
      // That is why nothing can be promised a place before its answer has landed, and - since the wall may not be
      // watched while it is being built - why the answers are gathered and put up together.
      var groups = new HomeContentGroup[lists.Count];

      if (_client == null)
        return;

      HomeSection section = entry.Section!;
      int askedFor = -1;

      try
      {
        for (int i = 0; i < lists.Count; i++)
        {
          HomeContentList list = lists[i];
          // A list this server cannot answer at all - the favourites over HTTP - has no items, and a list
          // without items is not shown. Saying why it is empty would be the "nothing here" the reference
          // never wrote.
          if (list.Missing != null)
            continue;

          askedFor = i;
          MediaTitles titles = await _client.GetTitlesAsync(section.Aspect.Name, list.Sort, list.Descending,
              HomeContentList.CONTENT_LIST_LIMIT, 0, null, list.Seen, null, cancellation: cancellation);

          if (generation != Volatile.Read(ref _contentGeneration) || cancellation.IsCancellationRequested)
            return;

          IReadOnlyList<MediaTitle> arrived = titles.Titles ?? Array.Empty<MediaTitle>();
          // HasItems, the reference's one test: an answer with nothing in it leaves no heading, no note and
          // no group - as if the list had never been asked. "Last Added" for a shelf nobody has filled is
          // an empty band, and an empty band is no band.
          if (arrived.Count == 0)
            continue;

          // The band is planned for the covers that arrived - which is known before it is built, because nothing
          // of it can go up before it has all been read - and a band laid out for six and holding two would keep
          // the room of four empty covers standing, which the wall next to it shows as a gap.
          HomeContentGroup group = new HomeContentGroup(list.Heading, list.Layout, list.Width, arrived.Count);
          groups[i] = group;
          rows.Add(HomeRow.Heading(list.Heading));
          foreach (MediaTitle title in arrived)
          {
            // The wall reads through the entry's aspect, so it is the wall that knows whether a pressed
            // square is a thing to play or a thing to open - an album holds tracks, and the reference's
            // wall opened the album rather than asking it for a file it never has.
            HomeRow row = HomeRow.For(new TitleViewModel(title,
                MediaCatalog.HoldsTitles(section.Aspect.Name), _client));
            rows.Add(row);
            group.AddTitle(row.Title!);
            // The reference's pane is a wall of covers, and a cover arrives a moment after its name - the same
            // two beats the media grid uses, so the picture rides on the very same cached fetch.
            if (IsNereusHome && row.Title != null)
              _ = PutPosterAsync(row.Title, generation);
          }

          AddBand(wall, group);
        }

        // The list arrived without a token, so this server keeps its doors open to the room; whatever refusal
        // this screen saw before belonged to another server or to the switch before someone turned it off.
        ServerRefusesStrangers = false;
      }
      catch (ServerApiException ex)
      {
        if (generation != Volatile.Read(ref _contentGeneration) || cancellation.IsCancellationRequested)
          return;

        // A refusal of the titles is the server asking who is calling; the sign-in boxes appear because of it,
        // and the note below says so.
        if (ex.StatusCode == 403)
          ServerRefusesStrangers = true;

        // The loop stops, because every remaining list would be refused for the same reason. The list that
        // was asked for has no band standing yet - the read failed before anything of its was put up - so
        // its name goes up with the line that explains the failure; a refusal the viewer cannot attribute
        // to anything reads as a broken screen rather than as a server asking to be signed into.
        HomeRow note = HomeRow.Note(DescribeContentFailure(ex, askedFor < 0 ? null : lists[askedFor]));
        EnsureFailedBand(askedFor < 0 ? -1 : askedFor, lists, groups, rows);
        rows.Add(note);
        if (askedFor >= 0)
        {
          groups[askedFor].AddNote(note);
          AddBand(wall, groups[askedFor]);
        }
      }
      catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
      {
      }
      catch (Exception ex)
      {
        if (generation != Volatile.Read(ref _contentGeneration) || cancellation.IsCancellationRequested)
          return;

        HomeRow note = HomeRow.Note("reading failed: " + ex.Message);
        EnsureFailedBand(askedFor < 0 ? -1 : askedFor, lists, groups, rows);
        rows.Add(note);
        if (askedFor >= 0)
          groups[askedFor].AddNote(note);
      }
    }

    /// <summary>
    /// Gives a band its place on the wall being read, once. The failure of a list can land on a band that another
    /// answer already claimed, and a band standing twice is a wall with the same shelf on it twice.
    /// </summary>
    private static void AddBand(List<HomeContentGroup> wall, HomeContentGroup group)
    {
      if (!wall.Contains(group))
        wall.Add(group);
    }

    /// <summary>
    /// Keeps the band of a list whose reading failed, so the line saying so stands under the list's own name
    /// instead of loose at the bottom of the pane. A band already standing - one whose titles arrived - keeps its
    /// titles and merely takes the line.
    /// </summary>
    private void EnsureFailedBand(int askedFor, IReadOnlyList<HomeContentList> lists, HomeContentGroup[] groups,
        List<HomeRow> rows)
    {
      if (askedFor < 0 || groups[askedFor] != null)
        return;

      HomeContentList failed = lists[askedFor];
      HomeContentGroup group = new HomeContentGroup(failed.Heading, failed.Layout, failed.Width, 1);
      groups[askedFor] = group;
      rows.Add(HomeRow.Heading(failed.Heading));
    }

    private void QueueContentForSelection(MainMenuEntry? entry)
    {
      // The standing wall starts leaving with the step, not with the answer that comes after the settling and the
      // reading. Nothing here waits for it: the curve is written and the screen plays it out on its own.
      WallLeaving?.Invoke();

      CancellationTokenSource next = new CancellationTokenSource();
      CancellationTokenSource? previous = Interlocked.Exchange(ref _pendingContentSelection, next);
      previous?.Cancel();
      _ = ShowSelectedContentAfterDelayAsync(entry, next);
    }

    private async Task ShowSelectedContentAfterDelayAsync(MainMenuEntry? entry, CancellationTokenSource selection)
    {
      try
      {
        await ShowContentAsync(entry, selection.Token, TimeSpan.FromMilliseconds(CONTENT_SELECTION_DELAY_MS));
      }
      catch (OperationCanceledException) when (selection.IsCancellationRequested)
      {
      }
      finally
      {
        Interlocked.CompareExchange(ref _pendingContentSelection, null, selection);
        selection.Dispose();
      }
    }

    /// <summary>
    /// What one refused list says about itself, in the words a viewer needs: the refusal, and - when the server was
    /// refusing everybody rather than this list - the fact that signing in is the way past it.
    /// </summary>
    private static string DescribeContentFailure(ServerApiException failure, HomeContentList? list)
    {
      if (failure.StatusCode == 403)
        return "this server wants you to sign in first";
      return failure.Message;
    }

    /// <summary>
    /// Walking the menu with a remote: every stop refills the pane - unless Nereus is switched to wait for a
    /// press (ContentFollowsHighlight), in which case the pane holds the last pressed tile's lists and only
    /// <see cref="CommitEntry"/> moves it.
    /// </summary>
    partial void OnSelectedEntryChanged(MainMenuEntry? value)
    {
      foreach (MainMenuEntry entry in Menu.Concat(_nereusMenu.MainMenuItems))
        entry.IsSelected = ReferenceEquals(entry, value);

      if (!ContentFollowsHighlight)
        return;

      RememberLastAction(value);

      if (IsNereusHome)
        QueueContentForSelection(value);
      else
        _ = ShowContentAsync(value);
    }

    // The tile the content pane is currently filled from. With the walk-to-select switches off this is whatever
    // the highlight stands on; with them on it is the last tile that was actually pressed.
    private MainMenuEntry? _paneEntry;

    /// <summary>
    /// The entry the content pane is being filled from: the highlighted one while the pane follows the walk,
    /// the last pressed one while Nereus is switched to wait for a press.
    /// </summary>
    public MainMenuEntry? PaneEntry
    {
      get { return _paneEntry; }
    }

    /// <summary>
    /// The viewer pressed OK on a menu tile. Two things follow, both the reference's: the pane (when it was
    /// waiting for a press rather than following the walk) fills from the pressed tile, and the tile is
    /// remembered - HomeMenuModel wrote the pressed action into <c>LastSelectedHomeMenuActionId</c> every time,
    /// so the home screen a viewer returns to tomorrow opens on the shelf they last stood on.
    /// </summary>
    public void CommitEntry()
    {
      MainMenuEntry? entry = SelectedEntry;
      if (entry == null)
        return;

      RememberLastAction(entry);

      if (!ContentFollowsHighlight && !ReferenceEquals(entry, _paneEntry))
        _ = ShowContentAsync(entry);
    }

    /// <summary>
    /// The viewer picked a tile with the mouse: its wall is put up now, without the settling.
    /// </summary>
    /// <remarks>
    /// The reference answered an explicitly chosen tile at once and a tile the highlight had merely moved over
    /// after its settling (<c>HomeMenuModel.OnSelectedItemChanged</c>: "If touch display is enabled then the item
    /// was explicitly selected with a click so update the content immediately"). It needed a setting to tell the
    /// two apart because its menu could not see how it had been moved; a pointer on this rail can be told from a
    /// walker, and a pointer cannot overshoot - a tile under a cursor is the tile the viewer meant. So the mouse
    /// is the reference's explicit case, and the settling stays where it always belonged: on the walk.
    /// The standing wall is not cut in half by this - <see cref="WallHandover"/> waits for the fade the step has
    /// already started, exactly as it does when the settling runs to its end.
    /// </remarks>
    public void ChooseEntryNow()
    {
      MainMenuEntry? entry = SelectedEntry;
      if (entry == null || ReferenceEquals(entry, _paneEntry))
        return;

      // The settling this pick interrupted was waiting for the same answer; waiting for it as well would put the
      // wall up a moment after the viewer stopped waiting for it.
      CancellationTokenSource? pending = Interlocked.Exchange(ref _pendingContentSelection, null);
      pending?.Cancel();
      _ = ShowContentAsync(entry);
    }

    /// <summary>
    /// Writes the pressed tile into the worn skin's settings - only when a skin that has the setting is worn,
    /// and only when it is not already the remembered one, because a settings write per keystroke is a file
    /// that never rests.
    /// </summary>
    private void RememberLastAction(MainMenuEntry? entry)
    {
      if (entry == null || !Guid.TryParse(entry.HomeImageId, out Guid actionId))
        return;

      if (WornSkinSettingsOrNull() is not NereusSkinSettings nereus)
        return;

      string actionIdText = actionId.ToString("D");
      if (string.Equals(nereus.LastSelectedHomeMenuActionId, actionIdText, StringComparison.OrdinalIgnoreCase))
        return;

      nereus.LastSelectedHomeMenuActionId = actionIdText;
      nereus.Save(null, out _);
    }

    /// <summary>
    /// The remembered pressed tile, as long as today's menu still has it: the reference restored the saved
    /// action on startup and on every menu rebuild, and let the tile simply not come back when the menu had
    /// changed under it.
    /// </summary>
    private MainMenuEntry? RememberedEntry()
    {
      if (WornSkinSettingsOrNull() is not NereusSkinSettings nereus
          || string.IsNullOrEmpty(nereus.LastSelectedHomeMenuActionId))
        return null;

      foreach (MainMenuEntry entry in _nereusMenu.MainMenuItems)
      {
        if (Guid.TryParse(nereus.LastSelectedHomeMenuActionId, out Guid selectedActionId)
            && Guid.TryParse(entry.HomeImageId, out Guid actionId)
            && actionId == selectedActionId)
          return entry;

        // Earlier Avalonia builds wrote the workflow name into this setting instead of the action Guid.
        if (entry.Mp2Action == nereus.LastSelectedHomeMenuActionId)
          return entry;
      }

      return null;
    }

    // What somebody left half way is no strip of its own on this screen, and deliberately so. The reference
    // skin (Nereus' HomeMenuModel with its one HomeContent per home entry, MovieHomeContent's
    // "ContinuePlayMovies" beside SeriesHomeContent's "ContinuePlaySeries") answers "where was I" per content
    // type, inside the lists of the highlighted entry - a series left half way belongs to the series tile and
    // to nothing else. A single mixed strip beside the whole menu would offer series episodes under Movies and
    // under Weather alike, which is precisely what MP2's home never did; the pane that follows the selection
    // (ShowContentAsync, HomeContentList) is this front end's faithful shape of the same answer.

    /// <summary>
    /// Said when the viewer presses OK on a row of the strip.
    /// </summary>
    /// <remarks>
    /// The same shape as the library's <c>PlayRequested</c>, so the application opens the film through the one
    /// path it already has; the home screen does not know what a player is.
    /// </remarks>
    public event EventHandler<TitleViewModel>? ResumeRequested;

    /// <summary>
    /// The viewer pressed OK on one row of the strip.
    /// </summary>
    [RelayCommand]
    private void Resume(TitleViewModel? title)
    {
      if (title == null)
        return;
      ResumeRequested?.Invoke(this, title);
    }

    /// <summary>
    /// The client the screen reads through, or <c>null</c> before a server was chosen.
    /// </summary>
    /// <remarks>
    /// Screens that open from this one read from the same server, and "the same server" has to mean the client
    /// that is on screen rather than one built again from a setting that may have changed since.
    /// </remarks>
    public ServerApiClient? Client
    {
      get { return _client; }
    }

    /// <summary>
    /// Where the client is looking, shown on the screen so that "it says no media" can be answered without
    /// guessing which server was being talked to.
    /// </summary>
    public string ServerUrl
    {
      get { return _client == null ? "(none)" : _client.BaseUrl.ToString(); }
    }

    /// <summary>
    /// Points the screen at a different server. The library shown is thrown away with the old address - keeping the
    /// aspects of one server under the name of another is how a viewer ends up clicking a film that is not there.
    /// </summary>
    /// <remarks>
    /// The client being replaced is cancelled but deliberately <em>not</em> disposed: a refresh from it may be
    /// between awaits, and taking the <c>HttpClient</c> away from under it is exactly the
    /// <c>ObjectDisposedException</c> this class already documents twice. Its handler is released when the client is
    /// collected, and a viewer changes the server address a handful of times per installation, so the one client that
    /// is not disposed is not a leak that ever grows.
    /// </remarks>
    public void UseServer(ServerApiClient client)
    {
      CancellationTokenSource? running = _runningRefresh;
      running?.Cancel();

      // The old stream belongs to the old server. Cancelling rather than disposing the source, and clearing the
      // field rather than waiting for the watch to notice: the watch's own cleanup compares what it finds against
      // the source it was started with, so a newer watch that is already in place here is left alone.
      bool wasWatching = _watchCancellation != null;
      _watchCancellation?.Cancel();
      _watchCancellation = null;

      _client = client;
      SignedInAs = null;
      ServerRefusesStrangers = false;
      SignInNames.Clear();
      _signInNamesAsked = false;
      Aspects.Clear();
      Sections.Clear();
      RebuildMenu(false);
      SelectedAspect = null;
      ServerDescription = null;
      IsBusy = false;
      Status = "Server address set to " + client.BaseUrl;
      OnPropertyChanged(nameof(ServerUrl));

      if (wasWatching)
        WatchServerEvents();
    }

    /// <summary>
    /// What was typed into the password box, for as long as it takes to hand it over.
    /// </summary>
    /// <remarks>
    /// An ordinary property and not an observable one: nothing on screen displays it, and a screen that could
    /// display it would be a screen that shows the password. It is cleared as soon as the server has answered,
    /// so the front end does not go on holding a household's password while somebody is choosing a film.
    /// </remarks>
    public string SignInPassword
    {
      get;
      set;
    } = string.Empty;

    /// <summary>
    /// Tells the server who this is, so that the parts of the library it keeps for known callers open.
    /// </summary>
    /// <remarks>
    /// MP2's server asks for this by default: <c>ServerSettings.WebAutorizationEnabled</c> is on, and the
    /// endpoints that name titles answer a stranger with 403. The switch for a household that does not want it is
    /// on the server, not here; what the front end can do is say which of the two situations it is standing in.
    /// </remarks>
    [RelayCommand]
    public async Task SignInAsync()
    {
      if (_client == null)
      {
        Status = "No server configured";
        return;
      }

      string name = SignInName.Trim();
      if (name.Length == 0)
      {
        Status = "Say who you are first";
        return;
      }

      string password = SignInPassword;
      SignInPassword = string.Empty;
      OnPropertyChanged(nameof(SignInPassword));

      try
      {
        Status = "Asking " + _client.BaseUrl + " who " + name + " is";
        string signedIn = await _client.SignInAsync(name, password, CancellationToken.None);
        SignedInAs = signedIn;
        Status = "Signed in as " + signedIn;
        SignInNames.Clear();
        _signInNamesAsked = false;

        // The library was refused to a stranger a moment ago, so the screen has to ask again now that it is not.
        await RefreshAsync();

        // And so does the stream: a server that wants to know its callers refused this connection a minute ago and
        // stopped the watch. Starting it again is the difference between a screen that updates once, after the
        // sign-in, and one that goes on updating afterwards by itself.
        WatchServerEvents();
      }
      catch (ServerApiException ex)
      {
        SignedInAs = null;
        Status = DescribeSignInFailure(ex);
      }
      catch (OperationCanceledException)
      {
        Status = "Sign-in cancelled";
      }
      catch (Exception ex)
      {
        // Same reason as in RefreshAsync: this runs on a continuation from a button, and what escapes here would
        // end the process rather than the attempt.
        SignedInAs = null;
        Status = "Sign-in failed: " + ex.Message;
      }
    }

    /// <summary>
    /// What a refused sign-in means to the person standing in front of the screen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MP2's token endpoint has two failure answers, and one of them is a lie by omission: a wrong password gets
    /// "The user name or password is incorrect.", while a name that does not exist - and a server on which
    /// nobody has ever set profiles up - both fall through to "User management not available."
    /// (<c>ResourceServer.GrantResourceOwnerCredentials</c>). A viewer told the latter cannot tell a typo from a
    /// server that has no accounts at all, and the difference is the difference between typing again and going
    /// to the server to configure it.
    /// </para>
    /// <para>
    /// So that one answer is translated into what can actually be done about it. Turning off "Web authorization"
    /// is offered as the equal alternative rather than as a workaround, because on a home network it is a
    /// supported configuration, not a mistake - it is the setting the server itself suggests when it cannot
    /// identify anybody.
    /// </para>
    /// </remarks>
    public static string DescribeSignInFailure(ServerApiException failure)
    {
      if (failure.Message.IndexOf("User management not available", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        return "The server could not find that profile. MP2 says the same thing when a profile was never set up, "
            + "so either add one in the server's configuration, or switch \"Web authorization\" off there and the "
            + "library is open to the whole house.";
      }

      return failure.StatusCode == 400 || failure.StatusCode == 401
          ? "The server does not accept that name and password: " + failure.Message
          : failure.Message;
    }

    /// <summary>
    /// Whether the shelves standing here were read under the eyes of the person the room is watching as now,
    /// which is what lets a library screen opened under a wake that read the server already keep its answers
    /// instead of asking for them a second time. A read whose owner has moved since is no read at all: what a
    /// person is shown, and what is kept out of their lists, both hang on who was asking - so this answers no
    /// as soon as the name the read was made under is not the name standing here.
    /// </summary>
    public bool ReadUnderThisViewer
    {
      get
      {
        // Nobody signed in is an empty name, not an unknown one: a reading made for an open house is a reading
        // made for the whole household, and it is good for every one of them until a name moves in.
        return _readUnderOwner != null
            && string.Equals(_readUnderOwner, Settings.SkinSettingsBase.FileOwner ?? "", StringComparison.Ordinal);
      }
    }

    private string? _readUnderOwner;

    /// <summary>
    /// Said once the shelves this screen shows are full: the menu's numbers are in, and the wall of the entry it
    /// opens on is read and standing. The wake listens to this - it asks for the reading under its animation and
    /// then holds its ring until that reading answers, which is the only way the library takes the window in the
    /// beat after the ring has left it instead of a second and a half later. A read that ended in a complaint
    /// says it too: after a refusal there is nothing left to wait for either.
    /// </summary>
    public event Action? LibraryReady;

    /// <summary>
    /// Reads the server again: what it is, which aspects it has, and then how many items each aspect holds.
    /// </summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
      if (_client == null)
      {
        Status = "No server configured";
        // Nothing is on its way, so nobody standing at the wake should wait for something that will not come.
        LibraryReady?.Invoke();
        return;
      }

      // Pressing the button twice should not leave two runs writing to the same list, so the older run is
      // cancelled. It is deliberately *not* disposed here: every run disposes its own source in its finally
      // below, and disposing a source that another run is still between awaits on makes the next
      // `cancellation.Token` in that run throw ObjectDisposedException on the dispatcher. Measured: starting a
      // second refresh while the first was in flight killed the process with exactly that. The assignment also
      // happens before the cancel for the same family of reason - the older run's finally must no longer see its
      // own source as the current one, or it would clear IsBusy while the new run is still going.
      CancellationTokenSource? previous = _runningRefresh;
      CancellationTokenSource cancellation = new CancellationTokenSource();
      _runningRefresh = cancellation;
      previous?.Cancel();

      IsBusy = true;

      // Cancelling the older run is not enough to keep two runs out of the same list, so each run waits for the
      // one before it. No `ConfigureAwait(false)` anywhere in here - the body raises collection-changed events
      // that a bound screen listens to, so it has to resume the way the rest of the method does.
      Task earlierRun;
      TaskCompletionSource runFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      lock (_gateLock)
      {
        earlierRun = _gate;
        _gate = runFinished.Task;
      }

      try
      {
        await earlierRun;

        // Replaced by a newer press before it ever got the list. Checked here rather than relying on a token
        // throwing, because a run that was cancelled while it queued behind the gate never reached an await
        // that could throw.
        if (cancellation.IsCancellationRequested)
          return;

        Status = "Reading " + _client.BaseUrl;

        // The eyes this reading is being made under, written down before the first question goes out: the
        // screen opened under it, and the household's change of eyes, both ask this before they send a read
        // of their own after it. A run a newer press replaced says nothing about anybody - the newer one
        // answers for itself.
        _readUnderOwner = Settings.SkinSettingsBase.FileOwner ?? "";

        // Gone before the new numbers arrive. Rows of the previous server sitting under the name of the current
        // one would be the one thing on this screen that lies on its own.
        ServerInfo info = await _client.GetServerInfoAsync(cancellation.Token);
        ServerDescription = info.Describe();

        LibraryAspects aspects = await _client.GetLibraryAspectsAsync(cancellation.Token);
        Aspects.Clear();
        foreach (MediaAspect aspect in aspects.Aspects)
          Aspects.Add(new AspectViewModel(aspect));
        if (SelectedAspect == null && Aspects.Count > 0)
          SelectedAspect = Aspects[0];

        // Whether an entry of the menu can be filled at all is settled by this list alone, so the menu stops
        // saying "no server connected" the moment the answer is in; the counts come later, in this same run.
        RebuildMenu(true);

        Status = string.Format("{0} aspects on {1}", aspects.Count, info.Name);

        // Each count is its own request, and one that fails leaves that row at "?" rather than failing the
        // screen: an aspect the server refuses to count is information, not a reason to show nothing.
        foreach (AspectViewModel aspect in Aspects)
        {
          try
          {
            MediaCount count = await _client.GetMediaCountAsync(aspect.Name, cancellation.Token);
            aspect.Count = count.Count;
          }
          catch (ServerApiException)
          {
            aspect.Count = null;
          }
        }

        // The tiles come last: they are the first thing a viewer looks at, and a tile that already knows how
        // many titles it holds answers the question the viewer was about to ask.
        Sections.Clear();
        foreach (HomeSection section in HomeSection.For(Aspects))
          Sections.Add(section);

        // From the same aspects, in the same breath, so that the menu's "4 films" is the number that arrived in
        // this reading and not the one that was on screen before the server was switched.
        RebuildMenu(true);
      }
      catch (ServerApiException ex)
      {
        Status = ex.NotReady
            ? "The server is not ready yet: " + ex.Message
            : ex.Message;
      }
      catch (OperationCanceledException)
      {
        // Replaced by a newer press of the same button, which reports its own status.
      }
      catch (Exception ex)
      {
        // The last resort is here for one specific reason: the command hands this method to an `async void`
        // continuation that rethrows whatever escapes it on the thread that pressed the button. Anything nobody
        // thought of therefore does not end the read, it ends the front end - measured once already, with the
        // cancellation source above. Reporting it costs one status line and keeps the screen alive.
        Status = "Reading failed: " + ex.Message;
      }
      finally
      {
        // Lets the next press in. The chain never faults - every exception is reported in Status above - so a
        // waiting run is never handed someone else's failure through `await earlierRun`.
        runFinished.SetResult();
        if (ReferenceEquals(_runningRefresh, cancellation))
        {
          _runningRefresh = null;
          IsBusy = false;
        }
        cancellation.Dispose();

        // The wake's signal, for every read that never reaches a wall: a server that refused, a menu with
        // nothing in it - after either there is nothing left to wait for, and the wake should not sit out its
        // patience finding that out. Where a wall IS being read the wall itself says the word, a few tens of
        // milliseconds later, so that the home screen is never opened on a wall still on its way in.
        if (_contentLoadCancellation == null)
          LibraryReady?.Invoke();
      }
    }

    #region What the server says without being asked

    /// <summary>
    /// How long a burst of news from the server is allowed to gather before the screen asks again.
    /// </summary>
    /// <remarks>
    /// Longer than the server's own half-second flush, or the debounce would fire once per server frame and the
    /// whole point - one question to the server instead of one per directory the importer walked through - would be
    /// lost. Short enough that a viewer who added a film sees it appear while still standing near the television.
    /// </remarks>
    public static TimeSpan EVENT_REFRESH_DEBOUNCE = TimeSpan.FromSeconds(2);

    [ObservableProperty]
    private string? _serverActivity;

    /// <summary>
    /// Whether there is anything to say about what the server is doing, which is what puts the line on the screen
    /// and what takes it off again.
    /// </summary>
    public bool HasServerActivity
    {
      get { return !string.IsNullOrEmpty(ServerActivity); }
    }

    partial void OnServerActivityChanged(string? value)
    {
      OnPropertyChanged(nameof(HasServerActivity));
    }

    /// <summary>
    /// Which of the server states the head line should draw. The sentence above is what a person reads; this is the
    /// one thing the glyph stands for, and the two are said together by <see cref="SayActivity"/> so that they cannot
    /// disagree.
    /// </summary>
    [ObservableProperty]
    private Theming.ServerActivityState _serverState = Theming.ServerActivityState.NotListening;

    /// <summary>
    /// How far the server has got with its library, when a frame said one: the reference printed this number next to
    /// its importer's turning circle, and the head line does the same.
    /// </summary>
    [ObservableProperty]
    private int? _serverFilingPercent;

    /// <summary>
    /// The queue that news the head line has no room for is filed in: dropped frames, and a server that would not let
    /// this screen listen without signing in first. The reference kept such things in a queue rather than in a line of
    /// text, and so does this - the application hands its queue in, and the head line's hint counts it. Left out, this
    /// screen still says everything it can about the server and simply has nowhere to put the rest, which is what a
    /// test without a household wants.
    /// </summary>
    private Notifications.NotificationQueue? _notices;

    /// <summary>Says where the notifications this screen hears about belong.</summary>
    public void AttachNotifications(Notifications.NotificationQueue queue)
    {
      _notices = queue;
    }

    /// <summary>
    /// Says what the server is doing, once, in all the ways the head line wants to hear it.
    /// </summary>
    private void SayActivity(Theming.ServerActivityState state, string? sentence, int? percent = null)
    {
      ServerState = state;
      ServerFilingPercent = percent;
      ServerActivity = sentence;
    }

    private Action<Action> _screenThread = delegate (Action action) { action(); };
    private CancellationTokenSource? _watchCancellation;
    private int _refreshArmed;

    /// <summary>
    /// Starts listening to the server's event stream, which is how the library on screen changes while nobody
    /// presses anything.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Without this the front end is correct but slow: it shows what it read last, and the only way to make it
    /// look again is to press refresh. Everything the server does on its own - an import that finishes at two in
    /// the morning, a share added from another client, a file deleted from the disk - happened silently here.
    /// </para>
    /// <para>
    /// Called by whoever owns the screen, once, because only that code knows how to get back onto the thread the
    /// visual tree belongs to; like the playback screen, the running front end passes
    /// <c>action =&gt; Dispatcher.UIThread.Post(action)</c>. Left out, what is heard is applied where it arrived,
    /// which is what a test without a screen wants.
    /// </para>
    /// </remarks>
    public void WatchServerEvents(Action<Action>? screenThread = null)
    {
      if (_client == null || _watchCancellation != null)
        return;

      if (screenThread != null)
        _screenThread = screenThread;

      CancellationTokenSource cancellation = new CancellationTokenSource();
      _watchCancellation = cancellation;
      SayActivity(Theming.ServerActivityState.Connecting, "Connecting to the server");

      // Deliberately not awaited and not stored: the watch is the lifetime of the screen, the only thing that ends
      // it is UseServer or the process, and every way it can fail is reported through ServerActivity rather than
      // through a task nobody collects.
      _ = WatchAsync(cancellation.Token, cancellation);
    }

    /// <summary>Whether the screen is currently hearing the server's news.</summary>
    public bool IsWatchingEvents
    {
      get { return _watchCancellation != null; }
    }

    /// <summary>
    /// Stops listening. Called when the front end closes; switching servers does it on its own.
    /// </summary>
    public void StopWatchingEvents()
    {
      CancellationTokenSource? source = _watchCancellation;
      _watchCancellation = null;
      SayActivity(Theming.ServerActivityState.NotListening, null);
      source?.Cancel();
    }

    private async Task WatchAsync(CancellationToken cancellation, CancellationTokenSource source)
    {
      ServerApiClient client = _client!;
      try
      {
        await client.WatchMediaEventsAsync(OnEventAsync, OnConnection, cancellation).ConfigureAwait(false);
      }
      catch (ServerApiException failure)
      {
        // Refused at the door: the server wants to know its callers and this one has not signed in. Saying that
        // is the difference between a screen that quietly stops updating and one that says what to do about it.
        _screenThread(delegate
        {
          SayActivity(Theming.ServerActivityState.Disconnected,
              "The server would not let this screen listen. Sign in to see changes as they happen.");
          // Refused at the door is the one thing a viewer has to do something about, so it is filed where it waits to
          // be read rather than only flashed along the head line: the reference handed exactly this to a queue as
          // "somebody has to act" (CommonNotificationService and its UserInteractionRequired type).
          _notices?.Enqueue(Notifications.NotificationType.UserInteractionRequired,
              "The server would not let this screen listen",
              "Sign in, and the library on screen starts changing on its own again.", "server");
        });
      }
      catch (OperationCanceledException)
      {
        // The viewer switched servers. The expected ending.
      }
      catch (Exception ex)
      {
        // Stopped listening is the other half of "lost the server": the stream ended for a reason it could not
        // recover from, so the head line says "disconnected" and this sentence - which of the two it was - rides on
        // the tooltip, because it is the one a person needs when they go looking for the cause.
        _screenThread(delegate
        {
          SayActivity(Theming.ServerActivityState.Disconnected, "Stopped listening: " + ex.Message);
        });
      }
      finally
      {
        // Only the watch this one started, or a watch started by UseServer while this was unwinding would have its
        // own cancellation thrown away by the older run.
        lock (_gateLock)
        {
          if (ReferenceEquals(_watchCancellation, source))
          {
            _watchCancellation = null;
            source.Dispose();
          }
        }
      }
    }

    /// <summary>
    /// One frame, on the watcher's thread. Nothing here touches the screen; what it decides is posted.
    /// </summary>
    private Task OnEventAsync(MediaEvent mediaEvent)
    {
      _screenThread(delegate { ApplyEvent(mediaEvent); });
      return Task.CompletedTask;
    }

    private void OnConnection(MediaEventConnection connection)
    {
      _screenThread(delegate
      {
        // The two words the head line draws, "connected" and "disconnected", hang on exactly this frame; the
        // sentence says what the glyph cannot, which is how long until the next try.
        if (connection.IsConnected)
          SayActivity(Theming.ServerActivityState.Connected, "Listening to the server");
        else
          SayActivity(Theming.ServerActivityState.Disconnected, "Lost the server. Trying again in " +
              (int)Math.Round(connection.RetryAfter.TotalSeconds) + " seconds.");
      });
    }

    /// <summary>
    /// What a frame means on screen: what the server is doing, and - when the frame says the titles may have
    /// changed - one refresh, after the burst around it has finished.
    /// </summary>
    private void ApplyEvent(MediaEvent mediaEvent)
    {
      if (mediaEvent.Kind == MediaEvent.KIND_PROGRESS && mediaEvent.Jobs.Count > 0)
      {
        MediaImportJob job = mediaEvent.Jobs[0];
        SayActivity(Theming.ServerActivityState.Filing, DescribeJob(job), PercentFiled(job));
      }
      else if (mediaEvent.Kind == MediaEvent.KIND_IMPORT)
      {
        if (mediaEvent.State == "started" || mediaEvent.State == "scheduled")
          SayActivity(Theming.ServerActivityState.Filing, "The server is filing its library");
        else if (mediaEvent.State == "completed" || mediaEvent.State == "canceled")
          SayActivity(Theming.ServerActivityState.Connected, "Listening to the server");
      }
      else if (mediaEvent.Kind == MediaEvent.KIND_GAP && mediaEvent.Dropped > 0)
      {
        // A frame the server threw away because this screen was not reading fast enough is not something a head line
        // can show and nothing the viewer should have missed: the library is read again from the start below, so all
        // that is left to say is that it happened. The reference kept such news in its queue rather than in a
        // sentence, and so this goes there - where it waits, unread, until somebody looks.
        _notices?.Enqueue(Notifications.NotificationType.Info,
            "The server was quicker than this screen",
            "It had " + mediaEvent.Dropped + " changes to report at once, so the library has been read again.");
      }

      if (mediaEvent.ShouldRefresh)
        ArmRefresh();
    }

    /// <summary>
    /// How far a filing job has got, as the whole percentage the reference printed next to its importer's circle, or
    /// null when the job has not said how many there are to file - "of 0" is not a percentage.
    /// </summary>
    private static int? PercentFiled(MediaImportJob job)
    {
      if (job.Identified <= 0)
        return null;

      int percent = (int)(job.Completed * 100L / job.Identified);
      return percent < 0 ? 0 : (percent > 100 ? 100 : percent);
    }

    private static string DescribeJob(MediaImportJob job)
    {
      string where = job.Path ?? "";
      string local;
      string? reason;
      if (MediaLibraryPath.TryAsLocalFile(job.Path, out local, out reason))
        where = local;

      return where.Length == 0
          ? "Filing " + job.Completed + " of " + job.Identified
          : "Filing " + job.Completed + " of " + job.Identified + " in " + where;
    }

    /// <summary>
    /// Asks the server once, a moment after its news stopped arriving.
    /// </summary>
    /// <remarks>
    /// One pending refresh at a time, counted with an <see cref="Interlocked"/> rather than a timer: an importer
    /// finishing produces a handful of frames inside a second, and each of them asking the library for its whole
    /// shelf would be a screen that hammers the server because something happened on the wire. The flag is cleared
    /// before the refresh starts, so news that arrives while a read is in flight arms the next one.
    /// </remarks>
    private void ArmRefresh()
    {
      if (Interlocked.CompareExchange(ref _refreshArmed, 1, 0) != 0)
        return;

      _ = ArmAfterBurstAsync();
    }

    private async Task ArmAfterBurstAsync()
    {
      try
      {
        await Task.Delay(EVENT_REFRESH_DEBOUNCE).ConfigureAwait(false);
      }
      catch (Exception)
      {
        // A delay does not fail. Reaching the reset below is the only thing that matters.
      }

      Interlocked.Exchange(ref _refreshArmed, 0);
      _screenThread(delegate { _ = RefreshAsync(); });
    }

    #endregion
  }
}
