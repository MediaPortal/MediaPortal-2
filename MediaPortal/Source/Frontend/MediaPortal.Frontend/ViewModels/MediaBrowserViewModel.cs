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
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// How the browser shows its titles: the screen's own rows, or the tiles the worn skin draws.
  /// </summary>
  public enum BrowserLayout
  {
    /// <summary>One row per title, the way the screen was written before any skin was asked.</summary>
    List,

    /// <summary>A grid of tiles, each drawn by the skin's <c>MediaTile…Template</c>.</summary>
    Grid,

    /// <summary>
    /// The skin's covers: large posters with the name standing under them, which is the third view the
    /// reference's view mode knew besides list and grid (<c>LayoutType.CoverLayout</c>).
    /// </summary>
    Cover
  }
  /// <summary>
  /// One row of the title list.
  /// </summary>
  /// <remarks>
  /// The row decides here whether it can be watched, because the answer belongs to the row: a library of two
  /// hundred titles on a machine that can reach only half of them is not a screen that should let the viewer
  /// press their way through the other half to find out. There are two ways to reach a title now - the file
  /// itself, when this machine can open it, and the server's stream of it, when there is a server to ask - and
  /// the row knows both, because it is the row that has to say why it cannot be watched when neither works.
  /// </remarks>
  public sealed class TitleViewModel : INotifyPropertyChanged
  {
    private readonly string? _unplayableReason;

    /// <summary>
    /// Whether this row is one of the titles that hold others, so that it is counted and opened rather than timed
    /// and played. Decided by whoever made the row - the list knows what aspect it read - because nothing the
    /// server says about a single title can tell an album from a film whose file went missing.
    /// </summary>
    private readonly bool _holdsTitles;

    private MediaTitle _title;

    public TitleViewModel(MediaTitle title, bool holdsTitles = false, ServerApiClient? stream = null)
    {
      _title = title;
      _holdsTitles = holdsTitles;
      if (!MediaLibraryPath.TryAsLocalFile(title.Path, out string localPath, out string? reason))
        _unplayableReason = reason;
      else
        LocalPath = localPath;

      // The server's address for the same title. Asked for whenever the server named something the endpoint could
      // carry: paths this machine cannot open included, because whoever can open them is the server, and guessing
      // at that from the client side is how a title that plays fine ends up marked unplayable.
      //
      // What keeps a reason is a title no stream can carry - nothing named at all, or a chain of providers with
      // one file behind none of them. That is a hole in the library or a feature the endpoint does not have, and
      // offering a stream for it would only move the disappointment from the row to a black screen.
      if (stream != null)
      {
        if (MediaLibraryPath.CanBeStreamed(title.Path, out string? notStreamable))
        {
          StreamAddress = stream.GetStreamUri(title.Id);
          _unplayableReason = null;
        }
        else
        {
          _unplayableReason = notStreamable;
        }
      }
    }

    /// <summary>
    /// The answer from the server, with one exception: a viewing reported while this row was on screen is folded
    /// into it, because the library has just been changed and the row would otherwise be the only place on the
    /// front end still describing the old state.
    /// </summary>
    public MediaTitle Title
    {
      get { return _title; }
      private set
      {
        _title = value;
        Raise();
      }
    }

    /// <summary>
    /// Folds the library's own answer to a playback report into this row.
    /// </summary>
    /// <param name="after">
    /// What the server said when the viewing was reported to it - not what the playback screen thinks it watched,
    /// which is the difference between a row that agrees with the library and a row that agrees with a guess.
    /// </param>
    /// <remarks>
    /// The tick, the date and the position are all written from that one answer, which is also the only way the
    /// row can be right about a title the viewer finished: a finished title has no position left to offer, and a
    /// row that kept showing "42% watched" next to a tick would be describing two different viewings at once.
    /// </remarks>
    internal void ApplyPlayback(MediaPlayback after)
    {
      Title = _title with
      {
        PlayCount = after.PlayCount,
        LastPlayed = after.LastPlayed,
        Resume = after.Resume
      };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// The one notification this row sends: a name of <c>null</c> means "read all of them again", which is what a
    /// row whose figures moved has to say, since the tick, the date and the position are all derived from the same
    /// answer.
    /// </summary>
    private static readonly PropertyChangedEventArgs AllProperties = new PropertyChangedEventArgs(null);

    private void Raise()
    {
      PropertyChanged?.Invoke(this, AllProperties);
    }

    /// <summary>The file to open, when this machine can open it.</summary>
    public string? LocalPath { get; }

    /// <summary>
    /// Where to ask the server for this title's bytes, or null when there is no server to ask.
    /// </summary>
    /// <remarks>
    /// An address rather than the bytes: a film is opened by the player, which seeks about in it, and a front end
    /// that read the file into memory to hand it over would be a proxy server with a progress bar.
    /// </remarks>
    public Uri? StreamAddress { get; }

    /// <summary>
    /// What to hand the player: the file on this machine when there is one, the server's stream when there is not.
    /// </summary>
    /// <remarks>
    /// The file wins when both exist, and not for speed alone. Playing the local file needs nothing of the API, so
    /// a viewer watching on the machine that holds the library is not depending on a loopback port for the next
    /// two hours, and a server restart mid-film does not reach them.
    /// </remarks>
    public MediaLocation? PlaybackLocation
    {
      get
      {
        if (LocalPath != null)
          return MediaLocation.FromFile(LocalPath);
        if (StreamAddress != null)
          return MediaLocation.FromUrl(StreamAddress);
        return null;
      }
    }

    public bool Playable
    {
      get { return LocalPath != null || StreamAddress != null; }
    }

    /// <summary>Why this title cannot be watched here, or <c>null</c> when it can.</summary>
    public string? UnplayableReason
    {
      get { return _unplayableReason; }
    }

    /// <summary>
    /// The bytes of this title's picture, or null while they are on their way and when the server has none.
    /// </summary>
    /// <remarks>
    /// Bytes rather than an image, because a row is drawn as soon as its name arrives and its poster comes a moment
    /// later - and because turning bytes into a picture needs the toolkit, which a row that is tested without a
    /// window has no business doing. The screen converts them, and remembers the picture it made.
    /// </remarks>
    public byte[]? Artwork
    {
      get { return _artwork; }
      private set
      {
        _artwork = value;
        Raise();
      }
    }

    private byte[]? _artwork;

    /// <summary>
    /// Puts the server's answer about this title's picture on the row.
    /// </summary>
    /// <param name="picture">The bytes, or null when this server has no picture for the title.</param>
    internal void ShowArtwork(byte[]? picture)
    {
      Artwork = picture;
    }

    /// <summary>
    /// The name, or a word that says there is none. An empty row reads as a rendering bug; "Untitled" reads as
    /// a file nobody named, which is what it is.
    /// </summary>
    public string Name
    {
      get { return string.IsNullOrWhiteSpace(Title.Title) ? "Untitled" : Title.Title!.Trim(); }
    }

    /// <summary>The name a viewer scans for, with the article moved to the back - what the server sorted by.</summary>
    public string SortName
    {
      get
      {
        string? sort = Title.SortTitle;
        return string.IsNullOrWhiteSpace(sort) ? Name : sort!.Trim();
      }
    }

    /// <summary>
    /// Whether the library was told this title was watched to the end. The play count is the only evidence there
    /// is: it is the number this server itself wrote when a front end reported the end of the film, so a row is
    /// marked seen exactly when the library says it was finished and not when a file happens to exist.
    /// </summary>
    public bool Seen
    {
      get { return Title.PlayCount > 0; }
    }

    /// <summary>
    /// The mark in front of a row that was watched, nothing in front of one that was not. An empty string rather
    /// than a "not seen" mark, because ninety unmarked rows out of a shelf of three thousand are the information;
    /// a cross on every one of them would be a wall of ink saying nothing.
    /// </summary>
    public string SeenMark
    {
      // Written as an escape rather than as the character, so that the mark does not depend on the encoding of
      // this source file surviving a checkout on another machine.
      get { return Seen ? "\u2713" : ""; }
    }

    /// <summary>
    /// Whether a tile may wear the watched corner: the library was told this one was finished. The tiles draw
    /// the skin's own <c>watched.png</c>; the list keeps its quiet tick, and both read this one answer.
    /// </summary>
    public bool IsWatched
    {
      get { return Seen; }
    }

    /// <summary>
    /// Whether a tile may wear the partly-watched corner: somebody started it and the server knows how far.
    /// A finished title is never partly one - the reference drew the full flag alone at a hundred percent,
    /// and two corners on one tile would say two different things about the same viewing.
    /// </summary>
    public bool IsPartlyWatched
    {
      get { return !Seen && Title.Resume.HasValue && Title.Resume.Value > 0; }
    }

    /// <summary>
    /// Whether this title is one of the library's virtual entries - registered, but with no file behind it.
    /// That is what the reference's <c>Virtual</c> meant, and it is why the tiles drew the corner flag with
    /// the offline mark over such a row: the shelf lists the title, but there is nothing to watch.
    /// </summary>
    public bool IsOffline
    {
      get { return Title.Stub; }
    }

    /// <summary>Whether this row's picture has arrived from the server yet (or exists at all).</summary>
    public bool HasArtwork
    {
      get { return _artwork != null; }
    }

    /// <summary>
    /// The same answer facing the other way, for the templates' fallback layers: a tile without a picture
    /// wears the skin's ground and its type mark instead of an artwork, and the binding wants that as a word
    /// of its own rather than as a negation it would have to perform.
    /// </summary>
    public bool HasNoArtwork
    {
      get { return _artwork == null; }
    }

    /// <summary>
    /// Whether this row's tile may show a watched corner at all - the worn skin's setting for the layout the
    /// shelf is standing in, carried down from the shelf onto the row. The template binds it on the row because
    /// the compiled skin XAML asks the row for everything it paints; the shelf is where the answer comes from,
    /// both when the row is made and when the viewer switches the shelf from grid to cover and back.
    /// </summary>
    public bool WatchFlagsShown
    {
      get { return _watchFlagsShown; }
      internal set
      {
        if (_watchFlagsShown == value)
          return;

        _watchFlagsShown = value;
        Raise();
      }
    }

    private bool _watchFlagsShown = true;

    /// <summary>
    /// When the library says this title was last opened, or nothing when it never was.
    /// </summary>
    /// <remarks>
    /// Written out in full with no culture involved: the date is the one thing on the row that has to be readable
    /// the same way on a machine set up in Berlin and one set up in Tokyo, and a "02/12" that means two different
    /// days depending on a regional setting is worse on a television than a long date.
    /// </remarks>
    public string LastPlayedText
    {
      get
      {
        DateTime? when = Title.LastPlayed;
        return when.HasValue ? when.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "";
      }
    }

    /// <summary>
    /// How far this viewer got in the title, as the server says it, or <c>null</c> when the server has no such
    /// figure - which is also the case for every title of a server that was not told who is asking.
    /// </summary>
    public int? ResumePercent
    {
      get { return Title.Resume; }
    }

    /// <summary>
    /// The resume column: how far the viewer got, or nothing. Nothing rather than "from the start" because a
    /// title that was never opened and a title whose position the server does not know look the same here, and
    /// both are honestly "nothing to report" - the offer to resume is the row itself, not this column.
    /// </summary>
    public string ResumeText
    {
      get { return Title.Resume.HasValue ? Title.Resume.Value + "% watched" : ""; }
    }

    /// <summary>What the left column of the row says: where it sits in what holds it, then the name, then the year.</summary>
    /// <remarks>
    /// <para>
    /// A track is numbered and an episode is addressed, because those are the names people give them - "track 4",
    /// "S02E05" - and a list of a series' episodes in which nothing says which is which leaves the viewer reading
    /// the whole list to find out what they stopped watching. Both numbers come from the server rather than from
    /// parsing the name, because importers put the number in the file name, in the metadata, in both, or in
    /// neither, and the column the server read is the one the library is sorted by.
    /// </para>
    /// <para>
    /// A year still follows the name, and for a track or an episode it usually will not be there at all: the importer
    /// that files a song does not usually know when it was recorded.
    /// </para>
    /// </remarks>
    public string Line
    {
      get
      {
        string where = Position;
        string what = Title.Year.HasValue ? Name + " (" + Title.Year.Value + ")" : Name;
        return string.IsNullOrEmpty(where) ? what : where + " " + what;
      }
    }

    /// <summary>
    /// The number or address this title has inside the one that holds it: <c>4.</c> for the fourth track,
    /// <c>S02E05</c> for an episode. Empty for the titles that are not inside anything, which is most of what a
    /// television shows.
    /// </summary>
    public string Position
    {
      get
      {
        if (Title.Track.HasValue)
          return Title.Track.Value.ToString(CultureInfo.InvariantCulture) + ".";

        if (!Title.Season.HasValue && !Title.Episode.HasValue)
          return "";

        string season = Title.Season.HasValue
            ? "S" + Title.Season.Value.ToString("00", CultureInfo.InvariantCulture)
            : "";
        string episode = Title.Episode.HasValue
            ? "E" + Title.Episode.Value.ToString("00", CultureInfo.InvariantCulture)
            : "";
        return season + episode;
      }
    }

    /// <summary>
    /// The right column: how long it is, how many things are on it, and what is wrong with it if something is.
    /// </summary>
    /// <remarks>
    /// A title that holds others is counted rather than timed. Saying "not playable here" about an album would be
    /// true of the file and wrong about the row: the row is not a thing to play, it is a thing to open, and a
    /// viewer who is told it cannot be watched from here stops looking for the music that is on it.
    /// </remarks>
    public string Detail
    {
      get
      {
        if (_holdsTitles)
          return Title.TrackCount.HasValue
              ? Title.TrackCount.Value + " tracks"
              : "";

        if (!Playable)
          return "not playable here";
        string length = Title.RuntimeMinutes.HasValue
            ? Title.RuntimeMinutes.Value + " min"
            : "";
        if (Title.Stub)
          return string.IsNullOrEmpty(length) ? "stub" : length + " · stub";
        return length;
      }
    }

    /// <summary>
    /// Who made this, for the music that says so, and nothing for the media that does not. A second line rather
    /// than a longer name, because the name is what a viewer scans for and the artist is what they check after.
    /// </summary>
    public string Artist
    {
      get { return Title.Artist ?? ""; }
    }
  }

  /// <summary>
  /// The list of titles behind one home tile, and the reading that fills it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Pages are read one at a time and never all at once. A library of 40 000 items is a real household, and a
  /// screen that fetched it to show twenty would be waiting minutes for a list nobody scrolls through in one
  /// evening; "more" is a row on the screen because the viewer is the one who decides whether to ask again.
  /// </para>
  /// <para>
  /// Like the home screen, this view model knows nothing about Avalonia, so the whole of it is testable against
  /// a server that is really answering.
  /// </para>
  /// </remarks>
  public partial class MediaBrowserViewModel : ObservableObject
  {
    /// <summary>
    /// Titles per request. A television shows around ten rows, and reading ahead of what fits on the screen twice
    /// over keeps the remote from waiting on the network without fetching a whole season.
    /// </summary>
    public const int PAGE_SIZE = 60;

    private readonly ServerApiClient? _client;
    private CancellationTokenSource? _running;

    [ObservableProperty]
    private string _status;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _canLoadMore;

    [ObservableProperty]
    private TitleViewModel? _selected;

    /// <summary>
    /// Whether the selected title's menu stands over the shelf. The reference opened its media list menu
    /// (<c>DialogEditMenu</c> and its siblings) over the list rather than beside it, and this screen keeps
    /// that shape: the shelf stays where it is, the menu names the row it was opened for, and Back puts
    /// the menu away and the shelf's focus back.
    /// </summary>
    [ObservableProperty]
    private bool _menuVisible;

    /// <summary>
    /// Whether the details panel is on screen right now - which it is whenever the screen wears details and a
    /// title has been read about. The viewer's standing choice for the screen is <see cref="ShowDetails"/>;
    /// this is that choice meeting a list with nothing selected.
    /// </summary>
    [ObservableProperty]
    private bool _detailsVisible;

    /// <summary>
    /// Whether this screen shows the selected title's details, as the viewer last answered it <em>on this
    /// screen</em> and remembered in <see cref="ScreenViewSettings"/>. The reference kept the question in the
    /// worn skin's settings (<c>EnableMediaItemDetailsView</c>) and so answered it only while a skin wearing
    /// those settings was on; this screen asks it of the viewer whatever it wears, on every screen.
    /// </summary>
    [ObservableProperty]
    private bool _showDetails = true;

    [ObservableProperty]
    private string _detailsHeading = "";
 
    [ObservableProperty]
    private string _detailsTagline = "";
 
    [ObservableProperty]
    private string _detailsLine = "";

    [ObservableProperty]
    private string _detailsText = "";

    [ObservableProperty]
    private string _detailsPath = "";

    /// <summary>The title the last details read was for, so that opening it again costs nothing.</summary>
    private Guid _detailsFor;

    /// <summary>
    /// The title the newest details read was asked for. Walking a shelf with the details panel on fires a
    /// read per row, and only the last row's answer may reach the panel - a plot that arrives after the
    /// viewer walked on would otherwise sit under the next title's name.
    /// </summary>
    private Guid _detailsAskFor;

    /// <summary>The shapes the viewer picked for each screen; the constructor loads them.</summary>
    private readonly ScreenViewSettings _screens;

    /// <summary>
    /// The pictures of the titles on this screen: asked for once per title, remembered across pages and searches,
    /// and never asked for more than a few at a time.
    /// </summary>
    private readonly Artwork? _artwork;

    /// <summary>
    /// The picture of the title the details panel is about, or null while it is on its way and when the server has
    /// none. Null is drawn as nothing, which leaves the placeholder the panel drew behind it.
    /// </summary>
    [ObservableProperty]
    private byte[]? _detailsArtwork;

    /// <summary>
    /// What the viewer has typed into the search box, which is not yet what the list is filtered by - the list
    /// moves when they press Enter or the Find button, not on every keystroke, because every keystroke would be a
    /// round trip to the server while the box is still being written in.
    /// </summary>
    [ObservableProperty]
    private string _searchText = "";

    /// <summary>The words the list on screen was read with, or "" when the whole shelf is showing.</summary>
    [ObservableProperty]
    private string _activeSearch = "";

    /// <summary>
    /// Which half of the shelf is on screen: "" for both halves, <c>unwatched</c> for the titles nobody has
    /// finished, <c>watched</c> for the ones somebody has. The server's own two words, so that what a log line
    /// says, what the answer echoes back and what this screen are one and the same thing.
    /// </summary>
    /// <remarks>
    /// Like <see cref="_activeSearch"/> this is what the list on screen answers, not what a button was last
    /// pressed with - a filter that failed to load is not a filter the viewer is looking through.
    /// </remarks>
    [ObservableProperty]
    private string _activeSeen = "";

    public MediaBrowserViewModel()
        : this(null, null, null)
    {
    }

    public MediaBrowserViewModel(ServerApiClient? client, HomeSection? section)
        : this(client, section, null)
    {
    }

    /// <summary>
    /// The titles behind a home tile, or - when <paramref name="parent"/> is named - the titles inside one title.
    /// </summary>
    /// <param name="client">The server to ask.</param>
    /// <param name="section">The tile this list came from.</param>
    /// <param name="parent">
    /// The album or series that was pressed, whose tracks or episodes are what this list is. Null for a whole
    /// shelf, which is what opening a tile gives.
    /// </param>
    /// <remarks>
    /// A list opened from a parent asks for a different aspect than the tile it came from - the tracks of an album
    /// are audio items, not album items - and asks for them in the order they are listened to rather than
    /// alphabetically, because an album whose third track is listed under "B" is not the thing the viewer sat down
    /// for. Both come from <see cref="MediaCatalog"/> rather than from a guess about the parent's name.
    /// </remarks>
    public MediaBrowserViewModel(ServerApiClient? client, HomeSection? section, TitleViewModel? parent)
    {
      _client = client;
      _artwork = client == null ? null : new Artwork(id => client.GetThumbnailAsync(id));
      Section = section;
      Parent = parent;
      AspectName = parent == null
          ? section?.Aspect.Name ?? ""
          : MediaCatalog.ChildAspectFor(section?.Aspect.Name);
      Inside = parent == null ? null : parent.Title.Id;
      // A shelf opens in the order its tile promised, and "Latest media" is the one tile whose promise is an order;
      // every other top-level shelf - and the name order has always been it - reads by name.
      SortOrder = parent == null ? section?.Sort ?? "title" : MediaCatalog.ChildSortFor(section?.Aspect.Name);
      SortDescending = parent == null && (section?.SortDescending ?? false);
      // The browse screen's picked value travels the same way the picked order did: with the section. A list
      // opened out of one title is the titles inside it, and the genre of the shelf they were browsed under does
      // not follow them down - the tracks of an Action album are nobody's Action genre.
      GroupBy = parent == null ? section?.By : null;
      GroupValue = parent == null ? section?.Value : null;
      DrillsIn = MediaCatalog.HoldsTitles(AspectName);
      Heading = parent == null ? section?.Label ?? "Titles" : parent.Name;
      // The two numbers at the shelf's foot speak whenever the shelf itself changes shape beneath them.
      Titles.CollectionChanged += (_, _) => RaiseShelfCounters();
      _status = client == null ? "No server configured"
          : section == null ? "No section chosen"
          : "Reading " + Heading;

      // A shelf opens in the shape the worn skin draws it, the way the reference screens opened in the layout
      // their view-mode carried: a skin is worn, it answers the tile keys, and the skin's own settings say what
      // shape this media's tiles take - Nereus keeps a grid type for films, one for series and one for seasons,
      // and the shelf follows them exactly as the reference's bindings followed <c>[Nereus].MovieGridViewType</c>.
      // A machine wearing its own face has nobody to answer for tiles and stays the list it was written as;
      // the View button, where tiles exist, puts that list one press away.
      _skinSettings = WornSkinSettings();
      _screens = ScreenViewSettings.Load(null, out _);
      // The picture a tile wears opens with this screen's own remembered answer first and the worn skin's
      // setting behind it - the Media button puts the question on the screen itself, next to the View
      // button, and a screen keeps the answer its viewer gave it there.
      _tileKind = RememberedMedia() ?? (_skinSettings == null ? GridViewType.Poster : TileKindFor(AspectName, _skinSettings));
      HasTiles = SkinEngine.TryTileTemplate(TileKey(_tileKind)) != null;
      // The cover is the third view of the reference's view mode, and since the viewer asked for it on every
      // screen it is a view rather than a privilege of one skin: a skin that draws its own covers gets its
      // own, and every other screen gets this front end's cover frame. The remembered shape below is what
      // decides whether a shelf opens as rows, tiles or covers.
      HasCover = SkinEngine.TryTileTemplate(COVER_TEMPLATE_KEY) != null;
      _layout = RememberedLayout() ?? (HasTiles ? BrowserLayout.Grid : BrowserLayout.List);
      // The details answer is the viewer's own for this screen first; where this screen was never asked,
      // the worn skin's setting speaks - which is the reference's <c>EnableMediaItemDetailsView</c>, whose
      // meaning now lives on the media screen itself rather than only behind a skin's settings page.
      _showDetails = _screens.HasDetails(AspectName)
          ? _screens.Details[AspectName]
          : _skinSettings?.EnableMediaItemDetailsView ?? true;
      // The corners obey the setting of the layout they appear in - the reference gated the grid's corners on
      // extEnableGridWatchFlags and the cover's on extEnableCoverWatchFlags - and the list's quiet tick is the
      // screen's own old promise, which the list's setting speaks for since the reference gave it one too.
      _watchFlagsShown = WatchFlagsFor(_layout);
      // The fanart behind the shelf is the reference's FanArtBackground_Template read honestly: the picture
      // follows the selected row, the whole layer stands or falls with EnableFanart, and the dimming layer on
      // top of it wears FanartOverlayOpacity as its opacity - the two settings the skin-settings screen writes.
      // A machine wearing its own face has no setting to read and no fanart, which is also what the reference's
      // default skin did: no background model, no picture.
      FanartShown = _skinSettings?.EnableFanart ?? false;
      FanartOverlay = _skinSettings?.FanartOverlayOpacity ?? 0.85;
      _fanart = client == null ? null : new Artwork(id => client.GetFanArtAsync(id, "FanArt"));
      // What a tile wears is the picture of its own kind, and the reference asked for it kinded too: its banner
      // grid bound FanArtType="Banner", its thumbnail grid FanArtType="Thumbnail", and neither ever drew a
      // poster in the other's frame - without the kind's own picture they drew the skin's "no banner" layer.
      // So a banner or thumbnail shelf reads that kind's bytes for its rows; a poster shelf keeps drawing the
      // thumbnail blob MP2's importers store in the database, which is the poster. The pictures are made per
      // kind on first use (TileArtworkFor) so that the Media button can walk between kinds without throwing
      // either kind's already-read bytes away.
    }

    private readonly SkinSettingsBase? _skinSettings;
    private GridViewType _tileKind;
    private readonly Dictionary<GridViewType, Artwork?> _tileArtworks =
        new Dictionary<GridViewType, Artwork?>();

    /// <summary>
    /// The source of this tile kind's own pictures, made once per kind and kept: a viewer who walks the
    /// Media button round twice repaints from the bytes already read rather than asking the server again.
    /// The poster kind has no source of its own - it is the database's thumbnail blob, which every shelf
    /// already carries - and neither has a shelf with no server to ask.
    /// </summary>
    private Artwork? TileArtworkFor(GridViewType kind)
    {
      if (kind == GridViewType.Poster)
        return null;

      if (_tileArtworks.TryGetValue(kind, out Artwork? known))
        return known;

      Artwork? created = _client == null
          ? null
          : new Artwork(id => _client.GetFanArtAsync(id, kind == GridViewType.Banner ? "Banner" : "Thumbnail"));
      _tileArtworks.Add(kind, created);
      return created;
    }

    /// <summary>
    /// What the worn skin keeps as settings, or <c>null</c> when the machine wears its own face (or the tests
    /// run it without an application). Read once when the shelf opens - a setting changed while the shelf is
    /// on screen takes its shape the next time it opens, which is how the reference's screens did it too.
    /// </summary>
    private static SkinSettingsBase? WornSkinSettings()
    {
      Application? app = Application.Current;
      return app == null ? null : SkinSettingsBase.LoadFor(FrontendTheme.CurrentName(app), null, out _);
    }

    /// <summary>
    /// The shape this screen's viewer last picked <em>on this screen</em>, or <c>null</c> when nobody ever
    /// picked one. The reference opened each shelf in its skin's view mode; a screen here remembers the way
    /// its viewer left it, which is what "the settings on the media screen are stored by screen" means.
    /// </summary>
    private BrowserLayout? RememberedLayout()
    {
      return _screens.Layouts.TryGetValue(AspectName, out string? word)
          && Enum.TryParse<BrowserLayout>(word, true, out BrowserLayout shape)
          ? shape
          : null;
    }

    /// <summary>The tile picture this screen's viewer last picked, or <c>null> when they never did.</summary>
    private GridViewType? RememberedMedia()
    {
      return _screens.Media.TryGetValue(AspectName, out string? word)
          && Enum.TryParse<GridViewType>(word, true, out GridViewType kind)
          ? kind
          : null;
    }

    /// <summary>The tile shape this shelf's media takes in this skin's grid.</summary>
    /// <remarks>
    /// The reference's settings class holds a grid type for exactly three media - films, series, seasons - and
    /// everything else wore the skin's plain poster tile; this maps the same three and posts the rest.
    /// </remarks>
    internal static GridViewType TileKindFor(string? aspectName, SkinSettingsBase settings)
    {
      if (string.Equals(aspectName, "MovieItem", StringComparison.OrdinalIgnoreCase)
          || string.Equals(aspectName, "MovieCollectionItem", StringComparison.OrdinalIgnoreCase))
        return settings.MovieGridViewType;

      if (string.Equals(aspectName, "SeriesItem", StringComparison.OrdinalIgnoreCase))
        return settings.SeriesGridViewType;

      if (string.Equals(aspectName, "SeasonItem", StringComparison.OrdinalIgnoreCase))
        return settings.SeasonGridViewType;

      return GridViewType.Poster;
    }

    /// <summary>The template name the grid asks the skin for at this tile shape.</summary>
    internal static string TileKey(GridViewType kind)
    {
      return kind switch
      {
        GridViewType.Banner => "MediaTileBannerTemplate",
        GridViewType.Thumbnail => "MediaTileThumbnailTemplate",
        _ => "MediaTilePosterTemplate"
      };
    }

    /// <summary>The key a skin answers when it draws covers at all.</summary>
    internal const string COVER_TEMPLATE_KEY = "MediaTileCoverTemplate";

    /// <summary>The template name the list on screen asks the skin for, whichever layout it stands in.</summary>
    public string GridTemplateKey
    {
      get
      {
        return Layout switch
        {
          BrowserLayout.Cover => COVER_TEMPLATE_KEY,
          BrowserLayout.List => "TitleRowTemplate",
          _ => TileKey(_tileKind)
        };
      }
    }

    /// <summary>Whether the worn skin draws tiles for this shelf at all; without them there is nothing to view.</summary>
    public bool HasTiles { get; }

    /// <summary>Whether the worn skin draws its own covers; a skin that does not gets this front end's frame.</summary>
    public bool HasCover { get; }

    /// <summary>What the screen's details button says, so that the label and the state cannot drift apart.</summary>
    public string DetailsLabel
    {
      get { return ShowDetails ? "Details: on" : "Details: off"; }
    }

    /// <summary>
    /// The viewer answered the details question for this screen. It is written down where it is answered -
    /// on the screen, by the button or the info key - and the shelf obeys it from then on, reading the
    /// selected title as the selection walks.
    /// </summary>
    partial void OnShowDetailsChanged(bool value)
    {
      OnPropertyChanged(nameof(DetailsLabel));
      OnPropertyChanged(nameof(ShowsFloorGradient));

      if (value)
        _ = ShowDetailsForSelectedAsync();
      else
        DetailsVisible = false;

      _screens.Details[AspectName] = value;
      _screens.Save(null, out _);
    }

    /// <summary>The corners' answer for one layout, from the setting that governs exactly that layout.</summary>
    private bool WatchFlagsFor(BrowserLayout layout)
    {
      SkinSettingsBase? settings = _skinSettings;
      if (settings == null)
        return true;

      return layout switch
      {
        BrowserLayout.Cover => settings.EnableCoverWatchedFlags,
        BrowserLayout.Grid => settings.EnableGridWatchedFlags,
        _ => settings.EnableListWatchedFlags
      };
    }

    /// <summary>How the titles are drawn right now.</summary>
    [ObservableProperty]
    private BrowserLayout _layout = BrowserLayout.List;

    /// <summary>Whether a tile may show its watched corner at all - the skin setting's answer.</summary>
    [ObservableProperty]
    private bool _watchFlagsShown = true;

    /// <summary>
    /// Whether the shelf wears the worn skin's fanart at all - the reference's <c>EnableFanart</c>, read once
    /// when the shelf opens. The view binds the whole background layer to this one answer.
    /// </summary>
    public bool FanartShown { get; }

    /// <summary>
    /// How far the skin dims the fanart - the reference drew its dark backdrop over the picture with exactly
    /// this opacity, and the skin-settings screen writes exactly this number.
    /// </summary>
    public double FanartOverlay { get; }

    /// <summary>The fanart pictures of this shelf's titles, or null with no server to ask.</summary>
    private readonly Artwork? _fanart;

    /// <summary>
    /// The bytes of the fanart behind the shelf: the selected title's picture from the server's own fanart
    /// cache, the way the reference's background model followed the selection.
    /// </summary>
    [ObservableProperty]
    private byte[]? _backgroundArtwork;

    /// <summary>Whether there is a fanart to draw - the layer asks this rather than testing the bytes.</summary>
    public bool HasBackgroundArtwork
    {
      get { return BackgroundArtwork != null; }
    }

    /// <summary>The title the last fanart read was for, so a fast walk through rows cannot race.</summary>
    private Guid _fanartFor;

    partial void OnBackgroundArtworkChanged(byte[]? value)
    {
      OnPropertyChanged(nameof(HasBackgroundArtwork));
    }

    /// <summary>
    /// The fanart follows the selected row, which is the reference's rule to the letter: walking the shelf
    /// changes the room's mood one row at a time. Off with the setting off, off without a server, and a
    /// picture already remembered costs no second request - the walk back over a shelf asks nothing at all.
    /// </summary>
    partial void OnSelectedChanged(TitleViewModel? value)
    {
      OnPropertyChanged(nameof(SelectedNumber));
      // The menu names the row it was opened for and offers what that row can take, so it has to be told
      // when the walk moves onto a different title - the reference's edit menu always spoke about the
      // highlighted row, never about whatever was highlighted when the key was once pressed.
      RaiseMenuState();
      // A screen that wears details reads every title the selection walks onto - the reference's details view
      // followed the highlighted item exactly this way, and a panel that only answered on demand was the
      // "nothing but the title" the viewer rightly calls a missing feature.
      if (value == null)
        DetailsVisible = false;
      else if (ShowDetails)
        _ = ShowDetailsForSelectedAsync();

      Artwork? fanart = _fanart;
      if (!FanartShown || fanart == null || value == null)
      {
        _fanartFor = Guid.Empty;
        BackgroundArtwork = null;
        return;
      }

      Guid id = value.Title.Id;
      _fanartFor = id;
      if (fanart.TryPeek(id, out byte[]? held))
      {
        BackgroundArtwork = held;
        return;
      }

      BackgroundArtwork = null;
      _ = ReadFanartAsync(fanart, id);
    }

    /// <summary>
    /// One title's fanart, put behind the shelf when it arrives - unless the viewer has walked on, which the
    /// remembered id says. A server without the picture, or without the fanart service at all, is answered
    /// with no picture and no word: the plain screen behind is the right drawing for that.
    /// </summary>
    private async Task ReadFanartAsync(Artwork fanart, Guid id)
    {
      try
      {
        byte[]? picture = await fanart.LoadAsync(id);
        if (_fanartFor == id)
          BackgroundArtwork = picture;
      }
      catch (ServerApiException)
      {
        if (_fanartFor == id)
          BackgroundArtwork = null;
      }
    }

    public string LayoutLabel
    {
      get
      {
        return Layout switch
        {
          BrowserLayout.Grid => "View: Grid",
          BrowserLayout.Cover => "View: Cover",
          _ => "View: List"
        };
      }
    }

    partial void OnLayoutChanged(BrowserLayout value)
    {
      OnPropertyChanged(nameof(LayoutLabel));
      OnPropertyChanged(nameof(GridTemplateKey));
      OnPropertyChanged(nameof(ShowsFloorGradient));

      // The corners belong to the layout the viewer just walked into, and the covers wear the poster rather
      // than the grid's kinded picture - so the rows the shelf already holds are repainted with what this
      // layout draws, from caches that make the second visit to a layout cost no request at all.
      WatchFlagsShown = WatchFlagsFor(value);
      foreach (TitleViewModel row in new List<TitleViewModel>(Titles))
        row.WatchFlagsShown = WatchFlagsShown;
      _ = PaintRowsAsync(ArtworkForLayout);
    }

    /// <summary>
    /// Bumped every time the shelf decides which pictures its rows wear. A repaint that finished after the
    /// viewer switched layout again is thrown away rather than painted over the newer layout's pictures.
    /// </summary>
    private int _paintGeneration;

    /// <summary>
    /// Whose pictures the rows wear right now: a cover is a poster the reference sized generously, so the
    /// cover view draws the database's poster blob even on a shelf whose grid wears banners or thumbnails.
    /// </summary>
    private Artwork? ArtworkForLayout
    {
      get { return Layout == BrowserLayout.Cover ? _artwork : TileArtworkFor(_tileKind) ?? _artwork; }
    }

    /// <summary>
    /// Rows or tiles or covers, whichever way the screen wears them. The button and the <c>V</c> key lead
    /// here, on every screen whatever it wears: a skin draws its own tiles and covers, a screen without a
    /// skin - or whose skin answers none of the tile keys - is laid out in this front end's own frames.
    /// The tile picture inside the grid's frames is still the skin's business: its own settings say what
    /// shape this media's tiles take, and the skin settings screen is where a viewer changes that.
    /// </summary>
    [RelayCommand]
    public void SwitchView()
    {
      // The reference's view mode walked list → grid → cover, and the cycle closes back onto the rows it
      // started from; all three steps belong to every screen now.
      Layout = Layout switch
      {
        BrowserLayout.List => BrowserLayout.Grid,
        BrowserLayout.Grid => BrowserLayout.Cover,
        _ => BrowserLayout.List
      };

      _screens.Layouts[AspectName] = Layout.ToString();
      _screens.Save(null, out _);
    }

    /// <summary>
    /// Which picture the tiles wear on this screen: the poster the database holds, the banner, or the
    /// thumbnail. The button and nothing else changes it; the worn skin's grid setting is only the answer
    /// for a screen whose viewer never pressed the button.
    /// </summary>
    public GridViewType MediaKind => _tileKind;

    public string MediaLabel
    {
      get { return "Media: " + _tileKind; }
    }

    /// <summary>
    /// One step round the pictures a tile can wear - poster, banner, thumbnail, and round again - asked on
    /// the screen itself rather than behind a skin's settings page, and remembered for this screen alone.
    /// The shelf repaints its rows with the kind's own bytes, from the cache that kind already has.
    /// </summary>
    [RelayCommand]
    public void SwitchMedia()
    {
      _tileKind = _tileKind switch
      {
        GridViewType.Poster => GridViewType.Banner,
        GridViewType.Banner => GridViewType.Thumbnail,
        _ => GridViewType.Poster
      };

      _screens.Media[AspectName] = _tileKind.ToString();
      _screens.Save(null, out _);

      OnPropertyChanged(nameof(MediaLabel));
      OnPropertyChanged(nameof(MediaKind));
      // The grid asks the skin for a different frame per kind, so the screen has to dress its rows again.
      OnPropertyChanged(nameof(GridTemplateKey));
      _ = PaintRowsAsync(ArtworkForLayout);
    }

    /// <summary>The tile this list came from, or <c>null</c> in the designer.</summary>
    public HomeSection? Section { get; }

    /// <summary>
    /// The album or series whose titles this list is, or <c>null</c> when it is a whole shelf. What the screen says
    /// at the top when a viewer wonders how they got here.
    /// </summary>
    public TitleViewModel? Parent { get; }

    /// <summary>The id the server is told to look inside of, or <c>null</c> for the whole shelf.</summary>
    public Guid? Inside { get; }

    /// <summary>
    /// Whether pressing a row of this list opens what it holds rather than playing it, which is decided by the
    /// aspect rather than by whether the row has a file: an album never has one, and neither has a film whose
    /// disc is out of the drive, and only one of those two is a row worth opening.
    /// </summary>
    public bool DrillsIn { get; }

    /// <summary>What this list is ordered by, in the server's own word.</summary>
    public string SortOrder { get; }

    /// <summary>Whether <see cref="SortOrder"/> runs newest-first, which only a shelf opened by a tile can do.</summary>
    public bool SortDescending { get; }

    /// <summary>
    /// The category this list was browsed in - <c>genre</c>, <c>year</c>, <c>actors</c>, <c>directors</c> or
    /// <c>writers</c> - or null when it is a whole shelf. With <see cref="GroupValue"/> it is the second half of
    /// the browse screen: that screen counted the shelf, the viewer picked a value, and this list is the titles
    /// that carry it.
    /// </summary>
    public string? GroupBy { get; }

    /// <summary>The value picked on the browse screen, or null. Only means anything with <see cref="GroupBy"/>.</summary>
    public string? GroupValue { get; }

    /// <summary>The aspect the server is asked for, in its own spelling.</summary>
    public string AspectName { get; }

    /// <summary>What the screen is called at the top.</summary>
    public string Heading { get; }

    public ObservableCollection<TitleViewModel> Titles { get; } = new ObservableCollection<TitleViewModel>();

    /// <summary>
    /// The shelf's own two numbers, as the reference's <c>MediaItemsList.inc</c> drew them bottom right:
    /// where the highlight stands, and how many titles the server says this shelf holds in all - which is
    /// more than are on screen until every page has been read, and exactly what the "|" between the two is
    /// for. Zero stands for "nothing is highlighted", which a viewer reads as an empty shelf anyway.
    /// </summary>
    public int SelectedNumber => Selected == null ? 0 : Titles.IndexOf(Selected) + 1;

    /// <summary>Every title the server counted for this shelf's filter, not merely the pages read so far.</summary>
    public int TotalNumber { get; private set; }

    /// <summary>Whether the shelf holds anything at all - which is when the two numbers have a right to speak.</summary>
    public bool HasTitles => Titles.Count > 0;

    /// <summary>
    /// Whether the shelf draws the reference's floor gradient along its bottom edge: the dark band that
    /// lifts the names off the fanart. The reference laid it under grid and list but never under covers,
    /// and never while the details stood open beside the shelf - both of which this obeys.
    /// </summary>
    public bool ShowsFloorGradient => Layout != BrowserLayout.Cover && !ShowDetails && Titles.Count > 0;

    private void RaiseShelfCounters()
    {
      OnPropertyChanged(nameof(HasTitles));
      OnPropertyChanged(nameof(SelectedNumber));
      OnPropertyChanged(nameof(TotalNumber));
      OnPropertyChanged(nameof(ShowsFloorGradient));
    }

    /// <summary>
    /// Raised when a viewer asked to watch a title and this view model decided it is watchable. The application
    /// owns the player, so it decides what that means; this class only says which file it handed over.
    /// </summary>
    public event EventHandler<TitleViewModel>? PlayRequested;

    /// <summary>
    /// Raised when a viewer pressed a row that holds other titles - an album, a series - instead of playing
    /// anything. The application owns the windows, so it decides what "go inside" means; this class only says
    /// which row was pressed and that pressing it could not have meant play.
    /// </summary>
    /// <remarks>
    /// A separate event rather than the same one with a flag, because the two things a viewer can mean by pressing
    /// OK lead to two different windows, and a handler that has to ask a question of the argument to know which it
    /// was given is a handler that gets it wrong on the day the server reports a series with a path on it.
    /// </remarks>
    public event EventHandler<TitleViewModel>? ChildrenRequested;

    /// <summary>
    /// Reads the first page. Called when the screen appears, so that opening a tile and pressing Refresh on the
    /// home screen both lead to a filled list rather than an empty one.
    /// </summary>
    [RelayCommand]
    public async Task OpenAsync()
    {
      Titles.Clear();
      Selected = null;
      CanLoadMore = false;
      await ReadAsync(0);
    }

    /// <summary>
    /// Reads the page after the ones already on screen.
    /// </summary>
    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        await ReadAsync(Titles.Count);
    }

    /// <summary>
    /// Whether a search is on this list, which is what the "Clear" button appears and disappears for.
    /// </summary>
    public bool HasSearch
    {
      get { return !string.IsNullOrEmpty(ActiveSearch); }
    }

    partial void OnActiveSearchChanged(string value)
    {
      OnPropertyChanged(nameof(HasSearch));
    }

    /// <summary>
    /// Filters the list to the titles carrying the words in the search box. Enter on the box and the Find button
    /// both lead here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The search is sent as a question about the whole aspect rather than run over the titles already on screen,
    /// because a shelf of two thousand films has the one the viewer means on a page nobody has reached yet. Paging
    /// then walks through the matches, not through the shelf: <c>More</c> asks for the next page of the same
    /// question.
    /// </para>
    /// <para>
    /// An empty box means the whole shelf again. Someone who deleted the words to look at everything should not
    /// have to hunt for a second control to say so, which is also why this does not treat empty as "no matches".
    /// </para>
    /// </remarks>
    [RelayCommand]
    public async Task FindAsync()
    {
      string words = (SearchText ?? "").Trim();
      if (words.Length == 0)
      {
        await ClearSearchAsync();
        return;
      }

      if (!string.Equals(words, ActiveSearch, StringComparison.Ordinal))
      {
        ActiveSearch = words;
        ClearDetailsForNewList();
      }

      await ReadAsync(0);
    }

    /// <summary>Puts the whole shelf back. Escape on the search box comes here.</summary>
    [RelayCommand]
    public async Task ClearSearchAsync()
    {
      SearchText = "";
      if (HasSearch)
      {
        ActiveSearch = "";
        ClearDetailsForNewList();
      }

      await ReadAsync(0);
    }

    /// <summary>
    /// What the watched filter on the button in front of the list says. The three states are named here rather than
    /// in the markup because the same three words belong in a log line, in a test and in a screenshot.
    /// </summary>
    public string SeenLabel
    {
      get
      {
        if (ActiveSeen == "unwatched")
          return "Unwatched only";
        if (ActiveSeen == "watched")
          return "Watched only";
        return "All titles";
      }
    }

    /// <summary>Whether the list is being looked through, which is what puts a word about it in the status line.</summary>
    public bool HasSeenFilter
    {
      get { return !string.IsNullOrEmpty(ActiveSeen); }
    }

    partial void OnActiveSeenChanged(string value)
    {
      OnPropertyChanged(nameof(SeenLabel));
      OnPropertyChanged(nameof(HasSeenFilter));
    }

    /// <summary>
    /// Walks the watched filter one step round: everything, then what nobody has finished, then what somebody
    /// has, then everything again. The key <c>W</c> and the button above the list both lead here.
    /// </summary>
    /// <remarks>
    /// Three states on one control rather than two checkboxes, because a remote has no way of pressing two things
    /// at once and "watched and unwatched" is the same question as "everything", which a viewer can always reach
    /// with one more press.
    /// <para>
    /// The filter is asked of the server rather than applied to the rows on screen, for the same reason the search
    /// is: on a shelf of three thousand films the twenty the viewer has not finished are on pages nobody has read.
    /// </para>
    /// </remarks>
    [RelayCommand]
    public async Task CycleSeenAsync()
    {
      ActiveSeen = ActiveSeen == "" ? "unwatched"
          : ActiveSeen == "unwatched" ? "watched"
          : "";
      await ReaskAsync();
    }

    /// <summary>
    /// Takes the watched filter off the list, which is what Escape does when there is nothing newer on the screen
    /// to close. Nothing is re-read when no filter was on, because the list already answers that question.
    /// </summary>
    [RelayCommand]
    public async Task ClearSeenAsync()
    {
      if (!HasSeenFilter)
        return;

      ActiveSeen = "";
      await ReaskAsync();
    }

    /// <summary>
    /// Asks the server the current question again from the first page, because the question changed under the list.
    /// </summary>
    private async Task ReaskAsync()
    {
      ClearDetailsForNewList();
      await ReadAsync(0);
    }

    /// <summary>
    /// Puts the list back to its beginning because a different question was asked of the server.
    /// </summary>
    private void ClearDetailsForNewList()
    {
      // A details panel left open is about a title from the list that is on its way away, and a row a viewer can no
      // longer see is not a row they are reading about.
      DetailsVisible = false;
      Selected = null;
      Titles.Clear();
      CanLoadMore = false;
    }

    /// <summary>
    /// The viewer pressed OK on a row: play it, open what it holds, or say on the status line why neither is
    /// possible. All three are said in the same place, so "nothing happened" is never the answer.
    /// </summary>
    [RelayCommand]
    public void Play()
    {
      Play(Selected);
    }

    public void Play(TitleViewModel? title)
    {
      if (title == null)
      {
        Status = "Nothing selected";
        return;
      }

      // Asked before the file is looked at, because on a list of albums the question "where is its file?" was
      // never the question. An album is opened; the tracks inside it are what gets played.
      if (DrillsIn)
      {
        Selected = title;
        ChildrenRequested?.Invoke(this, title);
        return;
      }

      if (!title.Playable)
      {
        Status = title.Name + " cannot be watched from here: " + title.UnplayableReason;
        return;
      }

      Selected = title;
      PlayRequested?.Invoke(this, title);
    }

    /// <summary>
    /// Opens the selected title's menu over the shelf, the way the reference opened its media list menu
    /// (<c>DialogEditMenu</c> and its siblings) over the list it was opened for. The pick is refused with a
    /// word on the status line when nothing is selected, so a press never opens an empty dialog.
    /// </summary>
    [RelayCommand]
    public void ShowMenu()
    {
      if (Selected == null)
      {
        Status = "Nothing selected";
        return;
      }

      RaiseMenuState();
      MenuVisible = true;
    }

    /// <summary>
    /// Puts the menu away - the back key's answer while a menu is the newest thing on the screen, and the
    /// last button in the menu itself.
    /// </summary>
    [RelayCommand]
    public void CloseMenu()
    {
      MenuVisible = false;
    }

    /// <summary>
    /// Tells the library that the selected title was watched to the end, and folds the server's answer into
    /// the row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the only mark this front end can make, and the reality of the API says so: the one write the
    /// server has about watched state is the playback report, whose hundred means "finished" and whose
    /// counts only ever move forward - <c>MediaLibrary.NotifyPlayback</c> adds a play and stamps the date,
    /// and nothing subtracts a play or clears a date. So the menu offers exactly "mark watched" and no
    /// invented sibling for the way back; a screen that hid the second button has nothing to lie about,
    /// which is the rule this whole front end was written with.
    /// </para>
    /// <para>
    /// The row is rewritten from the server's answer and not from the press - the same rule the end of a
    /// real film follows through <see cref="ApplyPlayback"/> - so the tick says what the library believes.
    /// </para>
    /// </remarks>
    [RelayCommand]
    public async Task MarkWatchedAsync()
    {
      TitleViewModel? title = Selected;
      ServerApiClient? client = _client;
      if (title == null || client == null)
      {
        Status = "There is no server to tell about " + (title == null ? "a title" : title.Name);
        return;
      }

      try
      {
        MediaPlayback after = await client.ReportPlaybackAsync(title.Title.Id, 100);
        ApplyPlayback(after);
        Status = after.Watched
            ? title.Name + " is watched now as far as the library is concerned"
            : title.Name + " - the server took the report but does not count it as finished";
      }
      catch (ServerApiException ex)
      {
        if (ex.StatusCode == 403)
          Status = "The server wants to know who is reporting before it marks a title watched. Sign in on the server screen.";
        else if (ex.NotReady)
          Status = "The server is not ready yet: " + ex.Message;
        else
          Status = ex.Message;
        return;
      }
      catch (Exception ex)
      {
        Status = "Telling the server failed: " + ex.Message;
        return;
      }

      RaiseMenuState();
      MenuVisible = false;
    }

    /// <summary>The title the menu was opened for, or nothing while no row is picked.</summary>
    public string MenuTitle
    {
      get { return Selected == null ? "" : Selected.Name; }
    }

    /// <summary>
    /// Whether the menu may offer the mark: a server has to be there to be told, and the library has to
    /// have something to learn - a title it already counts as finished has nothing left the report can do
    /// for it, short of padding the play count of a film nobody watched again.
    /// </summary>
    public bool CanMarkWatched
    {
      get { return _client != null && Selected != null && !Selected.Seen; }
    }

    /// <summary>
    /// The words the menu says instead of offering a button: why there is nothing to press. Every case is
    /// said, so the menu is never a frame with a hole where its purpose was.
    /// </summary>
    public string MenuWords
    {
      get
      {
        if (Selected == null)
          return "Nothing is selected.";
        if (Selected.Seen)
          return "The library already says this title was watched through, and it has no way to be told otherwise.";
        if (_client == null)
          return "There is no server to tell.";
        return "";
      }
    }

    private void RaiseMenuState()
    {
      OnPropertyChanged(nameof(MenuTitle));
      OnPropertyChanged(nameof(CanMarkWatched));
      OnPropertyChanged(nameof(MenuWords));
    }

    /// <summary>
    /// Turns the screen's details on or off - the info key and the button above the list both lead here, and
    /// the answer is remembered for this screen. Whether anything is read is not asked here: turning the
    /// panel on reads the selected title, and every title the selection walks onto while it is on is read
    /// when the selection arrives.
    /// </summary>
    [RelayCommand]
    public void ToggleDetails()
    {
      ShowDetails = !ShowDetails;
    }

    /// <summary>
    /// The selected title's details, asked of the server when the screen wears details and the selection
    /// lands. A read already made for this title is not made again - walking off a row and back to it costs
    /// nothing - and a failure leaves the panel as it was, because an empty panel about the wrong title is a
    /// worse answer than no panel.
    /// </summary>
    private async Task ShowDetailsForSelectedAsync()
    {
      TitleViewModel? title = Selected;
      if (!ShowDetails || title == null || _client == null)
        return;

      if (_detailsFor == title.Title.Id)
      {
        DetailsVisible = true;
        return;
      }

      _detailsAskFor = title.Title.Id;
      await ReadDetailsAsync(title);
    }

    /// <summary>
    /// Turns the panel off, which is what Escape does when there is nothing newer on the screen to close.
    /// The screen's remembered answer goes with it, because on this screen the panel and the answer are the
    /// same thing; <c>I</c> or the button puts both back.
    /// </summary>
    public void CloseDetails()
    {
      ShowDetails = false;
    }

    /// <summary>
    /// Forgets what was read about one title, so that the next look at it asks the server again.
    /// </summary>
    /// <remarks>
    /// A viewing that has just been reported changes the two numbers the panel shows - how often the title was
    /// played and when it was last watched - so a panel that answers from the read made before the film started is
    /// showing something the viewer knows to be wrong. Called from whichever thread the report came back on, which
    /// is why this only touches a field and says nothing to the screen; the next read for that title
    /// does the talking.
    /// </remarks>
    public void ForgetDetails(Guid mediaItemId)
    {
      if (_detailsFor == mediaItemId)
        _detailsFor = Guid.Empty;
    }

    /// <summary>
    /// Tells the list what the library now says about one of the titles on it, so that the row a viewer has just
    /// watched reads the same as the library instead of waiting for somebody to press Refresh.
    /// </summary>
    /// <param name="after">The library's answer to the report of that viewing.</param>
    /// <returns>
    /// True when one of these rows was that title. False means it is not on this screen - the ordinary case when a
    /// film was picked on the home screen while a list of something else is open - and nothing is changed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The row is rewritten from the server's answer rather than from what the playback screen believes it watched.
    /// That is the difference between a row that agrees with the library and a row that agrees with a guess, and it
    /// is the same reason the watched mark reads the play count rather than a memory of the film having ended.
    /// </para>
    /// <para>
    /// A row is left where it is, including on a filtered list: it stays in the order it arrived in, because
    /// re-sorting rows under a viewer who is halfway down a shelf of three hundred films loses the place they were
    /// looking for. The next question they ask the server is answered with the new figures.
    /// </para>
    /// </remarks>
    public bool ApplyPlayback(MediaPlayback after)
    {
      TitleViewModel? row = null;
      foreach (TitleViewModel candidate in Titles)
      {
        if (candidate.Title.Id == after.Id)
        {
          row = candidate;
          break;
        }
      }

      if (row == null)
        return false;

      row.ApplyPlayback(after);

      // The details panel holds numbers of its own, read from the server earlier, and they are now about a title
      // the library has changed. Dropped rather than corrected by hand, because the panel says more than these
      // three figures do and only a read can bring the rest of it up to date.
      if (_detailsFor == after.Id)
        _detailsFor = Guid.Empty;

      // Nothing is announced at the list's own level: it is the row that changed, and the row says so itself.
      return true;
    }

    private async Task ReadDetailsAsync(TitleViewModel title)
    {
      if (_client == null)
        return;

      Guid asked = title.Title.Id;
      try
      {
        MediaItemDetails details = await _client.GetItemAsync(asked);
        if (_detailsAskFor != asked)
          return;

        _detailsFor = asked;

        string name = string.IsNullOrWhiteSpace(details.Title) ? title.Name : details.Title!.Trim();
        DetailsHeading = details.Year.HasValue ? name + " (" + details.Year.Value + ")" : name;
        // The reference's detail screens put the tagline under the title and above the facts - the sentence the
        // studio wrote, before the sentences about length and kind. A title without one simply shows no line.
        DetailsTagline = details.Tagline?.Trim() ?? "";
        DetailsLine = DescribeLine(details, title);
        DetailsText = details.Description ?? "Nothing was written about this title on this server.";
        DetailsPath = details.Path ?? title.Title.Path ?? "This title has no resource the server can name.";
        DetailsVisible = true;
        await ReadDetailsArtworkAsync(title);
      }
      catch (ServerApiException ex)
      {
        if (ex.StatusCode == 403)
          Status = "The details are only for signed-in users on this server. Sign in on the server screen.";
        else if (ex.NotReady)
          Status = "The server is not ready yet: " + ex.Message;
        else
          Status = ex.Message;
      }
      catch (Exception ex)
      {
        Status = "Reading the details failed: " + ex.Message;
      }
    }

    /// <summary>
    /// The picture of the title the details panel is now about.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read apart from the details themselves, because the two answers arrive at different speeds and mean
    /// different things when they fail: a details panel with no plot is still the film's details, while a poster
    /// that is not there is the ordinary state of a library and not a complaint. The panel is therefore shown as
    /// soon as the words are, and the picture goes into it when it comes.
    /// </para>
    /// <para>
    /// The last question asked is the one that may write: a viewer pressing the info key on two rows in quick
    /// succession must not end up with the first title's poster above the second title's plot.
    /// </para>
    /// </remarks>
    private async Task ReadDetailsArtworkAsync(TitleViewModel title)
    {
      Artwork? artwork = _artwork;
      if (artwork == null)
        return;

      DetailsArtwork = null;
      try
      {
        byte[]? picture = await artwork.LoadAsync(title.Title.Id);
        if (_detailsFor == title.Title.Id)
          DetailsArtwork = picture;
      }
      catch (ServerApiException)
      {
        // Nothing is said. The panel is already answering the question the viewer asked - what this film is about
        // - and a poster the server could not hand over changes nothing about that answer.
      }
    }

    /// <summary>
    /// Asks the server for the picture of every row that was just read, a few requests at a time.
    /// </summary>
    /// <remarks>
    /// The whole page is asked for rather than only the rows on screen, because the rows on screen are not
    /// something a view model knows - the screen decides that as it lays them out - and because the page is
    /// already bounded (<see cref="PAGE_SIZE"/>), so the most this can queue is one page of small reads. Whoever
    /// scrolls a row into view a second later finds its poster there rather than watching it arrive.
    /// </remarks>
    private async Task ReadArtworkAsync()
    {
      await PaintRowsAsync(ArtworkForLayout);
    }

    /// <summary>
    /// Asks the given picture source for the picture of every row on the shelf, a few requests at a time.
    /// </summary>
    private async Task PaintRowsAsync(Artwork? artwork)
    {
      if (artwork == null)
        return;

      int generation = ++_paintGeneration;
      List<Task> asking = new List<Task>();
      foreach (TitleViewModel row in new List<TitleViewModel>(Titles))
        asking.Add(PutArtworkOnAsync(artwork, row, generation));

      await Task.WhenAll(asking);
    }

    /// <summary>
    /// One row's picture, put on the row when it arrives - unless the viewer has since switched the shelf to
    /// wear another layout's pictures, in which case this answer belongs to a shape nobody is looking at.
    /// </summary>
    private async Task PutArtworkOnAsync(Artwork artwork, TitleViewModel row, int generation)
    {
      try
      {
        byte[]? picture = await artwork.LoadAsync(row.Title.Id);
        if (generation == Volatile.Read(ref _paintGeneration))
          row.ShowArtwork(picture);
      }
      catch (ServerApiException)
      {
        // A shelf whose server went quiet mid-picture keeps the rows it has. The list itself was read
        // successfully - that is what the status line is already about - and a row without a poster is a row a
        // viewer can still read, select and play.
      }
    }

    /// <summary>
    /// Whether the worn face wants the age certification shown: <c>EnableAgeCertificationLogos</c> is a
    /// switch every skin with settings carries, and the reference hung the certification logo on the poster
    /// with it. The shipped skins have no logo pictures, so what this machine shows is the certification in
    /// words - and the switch says yes or no to exactly the same fact either way.
    /// </summary>
    private static bool AgeLogosShown
    {
      get
      {
        Application? app = Application.Current;
        if (app == null)
          return true;

        return SkinSettingsBase.LoadFor(FrontendTheme.CurrentName(app), null, out _)
            is not SkinSettingsBase settings || settings.EnableAgeCertificationLogos;
      }
    }

    /// <summary>
    /// The one line under the title: what a viewer decides with, in the order they decide with it - which episode
    /// it is, how long, what it costs to watch in years, and whether they have seen it.
    /// </summary>
    private static string DescribeLine(MediaItemDetails details, TitleViewModel title)
    {
      List<string> parts = new List<string>();

      if (details.Season.HasValue || details.Episode.HasValue)
      {
        string number = "S" + (details.Season ?? 0).ToString("00", CultureInfo.InvariantCulture)
            + "E" + (details.Episode ?? 0).ToString("00", CultureInfo.InvariantCulture);
        parts.Add(string.IsNullOrWhiteSpace(details.EpisodeName) ? number : number + " · " + details.EpisodeName!.Trim());
      }
      else if (!string.IsNullOrWhiteSpace(details.SeriesName))
      {
        parts.Add(details.SeriesName!.Trim());
      }

      if (!string.IsNullOrWhiteSpace(details.Collection))
        parts.Add(details.Collection!.Trim());

      // The reference puts the show's kinds of story and its first aired date between the name and the length,
      // and so here: what a viewer weighs a film with comes in the order they weigh it - what kind, when it
      // first ran, how long.
      if (details.Genres != null && details.Genres.Count > 0)
      {
        List<string> kinds = new List<string>();
        foreach (string genre in details.Genres)
          if (!string.IsNullOrWhiteSpace(genre))
            kinds.Add(genre.Trim());
        if (kinds.Count > 0)
          parts.Add(string.Join(", ", kinds));
      }

      if (details.FirstAired.HasValue)
        parts.Add("first aired " + details.FirstAired.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

      int? minutes = details.RuntimeMinutes ?? title.Title.RuntimeMinutes;
      if (minutes.HasValue && minutes.Value > 0)
        parts.Add(minutes.Value + " min");

      // The age a viewer has to be, shown where the reference showed its age certification logo - and obeying
      // the same switch (EnableAgeCertificationLogos), because the reference's logos and this machine's word
      // answer the same question and one off means the other off.
      if (!string.IsNullOrWhiteSpace(details.Certification) && AgeLogosShown)
        parts.Add(details.Certification!.Trim());

      if (details.Rating.HasValue && details.Rating.Value > 0)
        parts.Add("rated " + details.Rating.Value + "/10");
      else if (details.TotalRating.HasValue && details.TotalRating.Value > 0)
        parts.Add("rated " + details.TotalRating.Value.ToString("0.#", CultureInfo.InvariantCulture) + "/10");

      if (details.PlayCount.HasValue && details.PlayCount.Value > 0)
      {
        string played = "watched " + details.PlayCount.Value + "×";
        if (details.LastPlayed.HasValue)
          played += ", last " + details.LastPlayed.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        parts.Add(played);
      }

      if (details.Stub || title.Title.Stub)
        parts.Add("stub");

      return parts.Count == 0 ? "No more is known about this title" : string.Join(" · ", parts);
    }

    private async Task ReadAsync(int offset)
    {
      if (_client == null)
      {
        Status = "No server configured";
        return;
      }

      // One read at a time. A viewer holding down the arrow key on the "more" row can queue a dozen requests;
      // the older ones are cancelled rather than allowed to append pages out of order.
      CancellationTokenSource? previous = _running;
      CancellationTokenSource cancellation = new CancellationTokenSource();
      _running = cancellation;
      previous?.Cancel();

      IsBusy = true;
      try
      {
        MediaTitles answer = await _client.GetTitlesAsync(AspectName, SortOrder, SortDescending, PAGE_SIZE, offset,
            string.IsNullOrEmpty(ActiveSearch) ? null : ActiveSearch,
            string.IsNullOrEmpty(ActiveSeen) ? null : ActiveSeen, Inside, GroupBy, GroupValue, cancellation.Token);

        if (offset == 0)
          Titles.Clear();

        foreach (MediaTitle title in answer.Titles ?? Array.Empty<MediaTitle>())
          Titles.Add(new TitleViewModel(title, DrillsIn, _client) { WatchFlagsShown = WatchFlagsShown });

        Selected = Selected ?? (Titles.Count > 0 ? Titles[0] : null);
        CanLoadMore = answer.Remaining > 0;
        // The shelf's foot counts what the server counted, not merely what has been read so far - the same
        // answer the status line's "12 of 244" is built from, so the two can never disagree.
        TotalNumber = answer.Total;
        RaiseShelfCounters();

        // The pictures come after the names rather than before them. A viewer reads a shelf by its titles, and a
        // list that waited for sixty downloads before drawing its first row would look like a front end that had
        // stopped. Not awaited, and not cancelled when a newer page arrives: a picture that is already on its way
        // is remembered, so the shelf it was read for can be scrolled back to without asking again.
        _ = ReadArtworkAsync();

        // The filter in front of the count rather than behind it, because the number is the answer and the filter
        // is the reason the answer is that small: "12 of 40 unwatched films" reads as one statement, while
        // "12 of 40 films, unwatched" reads as two and can be misread as a description of the twelve.
        string seeing = ActiveSeen == "unwatched" ? "unwatched "
            : ActiveSeen == "watched" ? "watched "
            : "";

        // What the rows are called. A shelf is named by the tile it came from, because that is the word the viewer
        // pressed; a list opened out of one title is named by what it holds, because "12 of 12 the dark side of the
        // moon" is not a sentence and the moon does not have twelve of itself.
        string about = Parent == null ? Heading.ToLowerInvariant() : MediaCatalog.RowNounFor(AspectName);

        // A browsed shelf says what it was browsed by, so that "12 of 40 films" cannot be misread as the whole
        // shelf when it is the twelve that carry a picked genre. "under" rather than "whose genre is" because the
        // category might be a year, or a person, and "whose year is 2001" is not how anyone says it.
        if (!string.IsNullOrEmpty(GroupBy) && !string.IsNullOrEmpty(GroupValue))
          about += " under " + GroupValue;

        if (answer.Total == 0)
        {
          // Two different nothings. "no films" and "no films called 'alien'" need different next moves from a
          // viewer, and only the second one has a search box to clear.
          Status = string.IsNullOrEmpty(ActiveSearch)
              ? "This server has no " + seeing + about
              : "Nothing in " + Heading.ToLowerInvariant() + " matches '" + ActiveSearch + "'";
        }
        else if (string.IsNullOrEmpty(ActiveSearch))
        {
          Status = string.Format("{0} of {1} {2}{3}, sorted by {4}", Titles.Count, answer.Total, seeing,
              about, answer.Sort ?? "the server's order");
        }
        else if (Titles.Count < answer.Total)
        {
          Status = string.Format("{0} of {1} {2}{3} match '{4}', sorted by {5}", Titles.Count, answer.Total,
              seeing, about, ActiveSearch, answer.Sort ?? "the server's order");
        }
        else
        {
          // Every match is already on screen, so "1 of 1" would be a stranger's way of putting it.
          Status = string.Format("{0} {1}{2} {3} {4} '{5}'", answer.Total, seeing,
              answer.Total == 1 ? "title in" : "titles in", Heading.ToLowerInvariant(),
              answer.Total == 1 ? "matches" : "match", ActiveSearch);
        }
        if (!string.IsNullOrEmpty(answer.Error))
          Status = answer.Error;
      }
      catch (ServerApiException ex)
      {
        if (ex.StatusCode == 403)
        {
          // MP2 asks for a token when the server was configured to know who is asking. Saying so is the whole
          // answer available from this screen, and it is a better one than an empty list.
          Status = Heading + " is only for signed-in users on this server. Sign in on the server screen.";
        }
        else if (ex.NotReady)
          Status = "The server is not ready yet: " + ex.Message;
        else
          Status = ex.Message;
      }
      catch (OperationCanceledException)
      {
        // A newer read was asked for; it reports its own status.
      }
      catch (Exception ex)
      {
        // Reported rather than thrown: this method is reached from a button on screen, and an exception that
        // escapes an async command ends the process, not just the read.
        Status = "Reading failed: " + ex.Message;
      }
      finally
      {
        if (ReferenceEquals(_running, cancellation))
        {
          _running = null;
          IsBusy = false;
        }
        cancellation.Dispose();
      }
    }
  }
}
