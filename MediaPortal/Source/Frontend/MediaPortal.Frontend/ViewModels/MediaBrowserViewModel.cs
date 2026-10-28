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
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Caching;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.Views;

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

    /// <summary>
    /// Whether pressing this row opens what stands inside it rather than playing it. Whoever reads a list
    /// knows the aspect it was read through and says it here; whoever presses a row - the shelf, the home
    /// wall, the watched strip - asks this instead of guessing from a title that has no file either way.
    /// </summary>
    public bool HoldsTitles
    {
      get { return _holdsTitles; }
    }

    /// <summary>
    /// The aspect the list this row stands in was read through: <c>SeriesItem</c> for a row of the series shelf,
    /// <c>SeasonItem</c> for a row of one series' seasons. Whoever reads a list knows it and says it here, because
    /// the row itself cannot always say what it is - a season and an episode both come back carrying a season
    /// number, and only the shelf they were listed under knows which of the two this row is.
    /// </summary>
    public string? ReadThrough { get; }

    /// <summary>
    /// The title this row's own answers have to be asked of: its details, its pictures, and the shelf inside it.
    /// </summary>
    /// <remarks>
    /// For every title the library holds this is the row's own id, which is what it has always been. One kind of row
    /// is different: a season the library holds as nothing but a number written on its episodes has no title of its
    /// own, and the shelf that counted it hands out a key to the list plus the name of the series the counting was
    /// done in (<see cref="MediaTitle.ParentId"/>) - so everything about that row is asked of the series, under
    /// <see cref="AskSeason"/>.
    /// </remarks>
    public Guid AskId
    {
      get { return _title.ParentId ?? _title.Id; }
    }

    /// <summary>
    /// The season number to ask <see cref="AskId"/> with, or null when the row is a title the library holds.
    /// </summary>
    public int? AskSeason
    {
      get { return _title.ParentId.HasValue ? _title.Season : null; }
    }

    private MediaTitle _title;

    public TitleViewModel(MediaTitle title, bool holdsTitles = false, ServerApiClient? stream = null,
        string? readThrough = null)
    {
      _title = title;
      _holdsTitles = holdsTitles;
      ReadThrough = readThrough;
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
    /// Which of the shelf's picture sources last answered this row, or <c>null</c> while none has.
    /// </summary>
    /// <remarks>
    /// A screen that repaints what it already shows - and it does that on every scroll, because that is how it
    /// notices rows coming into view - would otherwise ask the server for the same poster over and over. The shelf
    /// clears this the moment it decides to wear another kind of picture, which is the only moment the answer
    /// stops being current.
    /// </remarks>
    internal Artwork? PaintedFrom { get; set; }
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
      get
      {
        // MP2 names a season after the series it belongs to - "<series> S03" (SeasonInfo.SEASON_FORMAT_STR:54),
        // which is a name written for a list holding every season the house has. A viewer standing inside one
        // series has the series' name in the heading already, so the reference's season screens say the one thing
        // that is new: its word for a season and the number (MediaItemCoverSeasonDetails.inc:52-58, with
        // SeasonInfo.SHORT_FORMAT_STR as the short spelling). The row says the same here.
        if (MediaCatalog.IsSeason(ReadThrough) && Title.Season.HasValue)
          return "Season " + Title.Season.Value.ToString(CultureInfo.InvariantCulture);

        return string.IsNullOrWhiteSpace(Title.Title) ? "Untitled" : Title.Title!.Trim();
      }
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

        // A season's number is its name rather than an address inside something, and the row already says it;
        // writing "S03" in front of "Season 3" would be the same number twice on one line.
        if (MediaCatalog.IsSeason(ReadThrough))
          return "";

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
        // A title that holds others is counted, not timed - and counted in the things inside it, which are tracks
        // on an album and episodes in a season.
        if (_holdsTitles)
        {
          if (Title.TrackCount.HasValue)
            return Title.TrackCount.Value + " tracks";
          return Title.Episodes.HasValue ? Title.Episodes.Value + " episodes" : "";
        }

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
  /// Pages are read one at a time and the shelf keeps reading them without being asked. A library of 40 000 items
  /// is a real household, and a screen that fetched it to show twenty would be waiting minutes for a list nobody
  /// scrolls through in one evening; but a viewer who does scroll to the bottom should find the next page already
  /// there rather than a button to press, so the screen says when the viewer is near the end and
  /// <see cref="KeepReadingAsync"/> answers. <see cref="MAX_ROWS"/> is where the reading stops, which is the
  /// length the reference itself stops a shelf at.
  /// </para>
  /// <para>
  /// The rows are all kept once read, because a row of the list is a title's name and a few numbers rather than
  /// something the screen has to draw - which is how MP2's own panel treats the whole shelf it holds in memory.
  /// The pictures are the exception, and the reason the reading is page by page: a row asks for its own picture
  /// when the screen draws it (<see cref="PaintRow"/>) rather than the shelf fetching one per title it happens
  /// to know about.
  /// </para>
  /// <para>
  /// Like the home screen, this view model knows nothing about Avalonia, so the whole of it is testable against
  /// a server that is really answering.
  /// </para>
  /// </remarks>
  public partial class MediaBrowserViewModel : ObservableObject
  {
    /// <summary>
    /// Titles the opening question brings.
    /// </summary>
    /// <remarks>
    /// Measured against a shelf of eleven hundred films on the local network: the first hundred and twenty names are
    /// on the screen in about a sixth of a second, the whole shelf takes about a second and a half. The screen is
    /// therefore drawn from the opening answer and completed from the ones behind it - which is quicker on the eye
    /// than the reference's single query and lands on the same end state, a shelf that is whole before anybody has
    /// read a row of it.
    /// </remarks>
    public const int OPENING_PAGE = 120;

    /// <summary>
    /// Titles a question after the opening one brings: as much as the server will hand over in a single answer.
    /// </summary>
    /// <remarks>
    /// <c>MediaController.MAX_LIMIT</c> is 2000, and asking for the rest in one blow rather than in slices of sixty
    /// is the difference between two questions about a shelf and twenty - every one of which makes the server sort
    /// the whole shelf again on its way out. The reference asks once and gets everything
    /// (<c>MediaLibraryQueryViewSpecification</c> hands its query no limit at all); this is the same question, with
    /// the ceiling the server sets for itself and without a front end that has to wait for the largest answer
    /// before it is allowed to draw a title.
    /// </remarks>
    public const int REST_IN_ONE_ASK = 2000;

    /// <summary>
    /// How long the shelf waits between two questions while it finishes itself.
    /// </summary>
    /// <remarks>
    /// One frame or two, so that the names already in hand reach the television before the next batch is asked
    /// for. This is not throttling the server - a shelf of eleven hundred films is two questions - it is the screen
    /// being allowed to draw between them.
    /// </remarks>
    private const int DRAW_BETWEEN_ASKS_MILLISECONDS = 40;

    /// <summary>
    /// The most rows a shelf gathers, however big the library behind it.
    /// </summary>
    /// <remarks>
    /// The number the reference stops at: <c>Consts.MAX_NUM_ITEMS_VISIBLE</c> is 5000, past which MP2 splits the
    /// shelf into letters or says that there are too many items. A viewer with forty thousand songs is better
    /// served by the search box and the filters than by a list that has to scroll for an hour - so the reading
    /// stops here and the status line says that it did.
    /// </remarks>
    public const int MAX_ROWS = 5000;

    private readonly ServerApiClient? _client;

    /// <summary>
    /// This machine's shelf for the pictures of the library, or <c>null</c> for a run that only has its memory.
    /// Every holder of pictures this screen builds is asked of it first.
    /// </summary>
    private readonly PictureCache? _pictures;
    private CancellationTokenSource? _running;

    /// <summary>
    /// Which shelf the reads belong to. Bumped by every read that starts from the top - an opening, a search, a
    /// filter, a different shelf - which is how a run of <see cref="ReadTheRestAsync"/> learns that the shelf it
    /// was finishing has been replaced under it.
    /// </summary>
    private long _generation;

    [ObservableProperty]
    private string _status;

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Whether the server still has rows of this shelf that have not been read. Not a promise that they will be
    /// read - <see cref="MAX_ROWS"/> can stop the shelf first - but the answer to "is there more".
    /// </summary>
    [ObservableProperty]
    private bool _hasMore;

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

    /// <summary>
    /// Where the file stands, in the words the server wrote down.
    /// </summary>
    /// <remarks>
    /// The details column stopped painting this: a viewer who is choosing a film reads a story there, not a
    /// resource address, and the line lay across the foot of the screen like a footnote nobody asked for.  The
    /// answer is still read and still stripped of the media source id the server puts in front of it, because that
    /// is the rule the path itself is subject to.
    /// </remarks>
    [ObservableProperty]
    private string _detailsPath = "";

    /// <summary>
    /// The facts the details panel stands on, in the order the reference's own detail screens wrote them: what a
    /// title is made of and how long it runs before who made it, and the things only one kind of title answers
    /// (which season, which episode, how many episodes a season holds) first of all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reference laid these in three blocks under the plot with pictures and words beside each other - the
    /// director, the studio's logo, the number of votes. Only some of that is asked of this server, and what it
    /// does not answer is left out rather than drawn as an empty frame: a panel with a hole where "Regie" should
    /// stand tells the viewer nothing except that something is missing.
    /// </para>
    /// <para>
    /// Built as rows rather than as one sentence, because the reference laid them out in blocks a viewer's eye
    /// runs down, and a wall of facts joined with points is a different thing to read.
    /// </para>
    /// </remarks>
    public ObservableCollection<MediaBrowserFact> DetailsFacts { get; } = new ObservableCollection<MediaBrowserFact>();

    /// <summary>The selected title's rating on the ten points the library gave it, or null when nobody rated it.</summary>
    [ObservableProperty]
    private double? _detailsRating;

    /// <summary>
    /// The same number in the words the reference's panel put beside its stars: <c>(7.1)</c>, in brackets, and
    /// nothing at all when the library has no number to show.
    /// </summary>
    /// <remarks>
    /// Written here rather than with a binding format because the brackets are part of the sentence, not of the
    /// number: <c>MediaItemListMovieDetails.inc:104-108</c> prints TotalRating through the string format
    /// <c>"{}({0:F1})"</c> after the stars.
    /// </remarks>
    [ObservableProperty]
    private string _detailsRatingWords = "";

    /// <summary>
    /// Five stars, each one holding the light the rating left it: whole for a star the rating filled, a quarter of
    /// it for a star the rating never reached, and what lies between for the half star a rating of six and a half
    /// of ten is.
    /// </summary>
    /// <remarks>
    /// The reference drew five of the skin's own star pictures and lit as many of them as its number said
    /// (<c>MultiDetails*.inc</c>, and the same five under a tile on the wall). Worked out here rather than in a
    /// converter because a picture cannot be told "six and a half of ten" - it can only be told how much of its
    /// light to keep - and the arithmetic belongs with the number it comes from.
    /// </remarks>
    public ObservableCollection<double> DetailsStars { get; } =
        new ObservableCollection<double> { 0.25, 0.25, 0.25, 0.25, 0.25 };

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
    /// <param name="pictures">
    /// This machine's shelf for the pictures of the library, which every holder on this screen asks before it
    /// asks the server. <c>null</c> is a run that keeps pictures in memory only.
    /// </param>
    /// <remarks>
    /// A list opened from a parent asks for a different aspect than the tile it came from - the tracks of an album
    /// are audio items, not album items - and asks for them in the order they are listened to rather than
    /// alphabetically, because an album whose third track is listed under "B" is not the thing the viewer sat down
    /// for. Both come from <see cref="MediaCatalog"/> rather than from a guess about the parent's name.
    /// </remarks>
    public MediaBrowserViewModel(ServerApiClient? client, HomeSection? section, TitleViewModel? parent,
        PictureCache? pictures = null)
    {
      _client = client;
      _pictures = pictures;
      _artwork = client == null
          ? null
          : PicturesOf(client, "Thumbnail", id => client.GetThumbnailAsync(id));
      Section = section;
      Parent = parent;

      // The shelf a pressed title came out of, which is what decides what its contents are: a series' rows are
      // seasons and a season's rows are episodes, so a screen that asked its parent's tile would read a series'
      // seasons twice and never reach the episodes. A row read by a screen that never said which shelf it read
      // still falls back on the tile it was opened from.
      string cameFrom = parent?.ReadThrough ?? section?.Aspect.Name ?? "";
      AspectName = parent == null
          ? section?.Aspect.Name ?? ""
          : MediaCatalog.ChildAspectFor(cameFrom);
      Inside = parent == null ? null : parent.AskId;
      // The one shelf that cannot be opened by an id alone: a season the library only counted off its episodes has
      // no title to look inside of, so its shelf is the series' episodes narrowed to that number.
      SeasonFilter = parent?.AskSeason;
      // Every shelf opens by name, which is the order it has always had; a shelf read inside a title takes the
      // order its kind carries, the tracks of an album by their number. The one tile that used to promise another
      // order - MP2's "Latest media" - is gone, so nothing arrives with one.
      SortOrder = parent == null ? "title" : MediaCatalog.ChildSortFor(cameFrom);
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
      HasTiles = MusicTemplateKey != null || SkinEngine.TryTileTemplate(TileKey(_tileKind)) != null;
      // The cover is the third view of the reference's view mode, and since the viewer asked for it on every
      // screen it is a view rather than a privilege of one skin: a skin that draws its own covers gets its
      // own, and every other screen gets this front end's cover frame. The remembered shape below is what
      // decides whether a shelf opens as rows, tiles or covers.
      HasCover = SkinEngine.TryTileTemplate(COVER_TEMPLATE_KEY) != null;
      _layout = RememberedLayout() ?? (HasTiles ? BrowserLayout.Grid : BrowserLayout.List);
      // The shelf opens wearing its details, in every layout: SimpleShowItems.xaml lays its details column
      // unconditionally, so a screen that opened without them is a screen missing a part.  What the viewer
      // answers with D or with the button belongs to the visit - see OnShowDetailsChanged.
      _showDetails = DetailsAlwaysShown();
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
      FanartBlurRadius = BlurOf(_skinSettings);
      // A setting the viewer flips on the skin-settings screen while this shelf lives takes its effect on
      // the television at once - the screen is a remote control, not an application form.
      SkinSettingsBase.Saved += delegate { ReapplySkinSettings(); };
      _fanart = client == null
          ? null
          : PicturesOf(client, "FanArt", id =>
              {
                (Guid asked, int? season) = AskOf(id);
                return client.GetFanArtAsync(asked, "FanArt", season);
              });
      // What a tile wears is the picture of its own kind, and the reference asked for it kinded too: its banner
      // grid bound FanArtType="Banner", its thumbnail grid FanArtType="Thumbnail", and neither ever drew a
      // poster in the other's frame - without the kind's own picture they drew the skin's "no banner" layer.
      // So a banner or thumbnail shelf reads that kind's bytes for its rows, and so does a poster shelf: its
      // rows are posters, asked of the server as posters. The pictures are made per
      // kind on first use (TileArtworkFor) so that the Media button can walk between kinds without throwing
      // either kind's already-read bytes away.
    }

    private SkinSettingsBase? _skinSettings;
    private GridViewType _tileKind;
    private readonly Dictionary<GridViewType, Artwork?> _tileArtworks =
        new Dictionary<GridViewType, Artwork?>();

    /// <summary>
    /// The source of this tile kind's own pictures, made once per kind and kept: a viewer who walks the
    /// Media button round twice repaints from the bytes already read rather than asking the server again.
    /// Every kind asks its own picture - a poster shelf asks posters - and only a shelf with no server to ask
    /// has no source. A title the server keeps no poster of wears the skin's empty plate, which is what the
    /// reference's shelf did: a wide thumbnail hung in a portrait frame is cut to a slice.
    /// </summary>
    private Artwork? TileArtworkFor(GridViewType kind)
    {
      // A season has no thumbnail blob, because nothing ever wrote one for a title that is a shelf of episodes
      // rather than a file somebody thumbnailed - least of all for a season the library only counts off those
      // episodes. Its front picture is the one beside the media with the season's number written into its name
      // ("season01-poster.jpg"), so a season shelf asks its posters of the server by name and number instead.
      if (_tileArtworks.TryGetValue(kind, out Artwork? known))
        return known;

      string wanted = kind == GridViewType.Banner
          ? "Banner"
          : kind == GridViewType.Poster ? "Poster" : "Thumbnail";
      Artwork? created = _client == null
          ? null
          : PicturesOf(_client, wanted, id =>
              {
                (Guid asked, int? season) = AskOf(id);
                return _client.GetFanArtAsync(asked, wanted, season);
              });
      _tileArtworks.Add(kind, created);
      return created;
    }

    /// <summary>
    /// One holder of one kind of picture: asked of this server, and looked for on this machine's shelf first
    /// when the machine keeps one. Without a shelf it is the memory-only holder this front end had before.
    /// </summary>
    private Artwork PicturesOf(ServerApiClient client, string kind, Func<Guid, Task<byte[]?>> ask)
    {
      return _pictures == null ? new Artwork(ask) : _pictures.ArtworkFor(kind, client.BaseUrl, ask);
    }

    /// <summary>
    /// What the worn skin keeps as settings, or <c>null</c> when the machine wears its own face (or the tests
    /// run it without an application). Read when the shelf opens and again every time the skin-settings
    /// screen writes an answer, so the shelf follows the remote control rather than the last memory.
    /// </summary>
    private static SkinSettingsBase? WornSkinSettings()
    {
      Application? app = Application.Current;
      return app == null ? null : SkinSettingsBase.LoadFor(FrontendTheme.CurrentName(app), null, out _);
    }

    /// <summary>
    /// Takes the answers the skin-settings screen just wrote onto this live shelf: the fanart and its
    /// dimming, the watched corners of every row, and - while this screen has never been asked its own
    /// answer - the details column too. A choice the viewer made on the media screen itself outranks the
    /// skin's answer then and for good, which is what "stored by screen" promises.
    /// </summary>
    private void ReapplySkinSettings()
    {
      _skinSettings = WornSkinSettings();
      FanartShown = _skinSettings?.EnableFanart ?? false;
      FanartOverlay = _skinSettings?.FanartOverlayOpacity ?? 0.85;
      FanartBlurRadius = BlurOf(_skinSettings);
      _ = WearFanartAsync(_backgroundRaw);
      WatchFlagsShown = WatchFlagsFor(_layout);
      foreach (TitleViewModel row in Titles)
        row.WatchFlagsShown = WatchFlagsShown;
      if (!_viewerAnsweredDetails)
        DetailsDefaultShown(DetailsAlwaysShown());
      OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(null));
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

    /// <summary>The frame a skin draws for an album's square.</summary>
    internal const string ALBUM_TEMPLATE_KEY = "MediaTileAlbumTemplate";

    /// <summary>The frame a skin draws for a track's square.</summary>
    internal const string TRACK_TEMPLATE_KEY = "MediaTileTrackTemplate";

    /// <summary>
    /// Whether this shelf holds music, which is settled by the aspect and by nothing else: the reference drew
    /// its audio tiles as squares - <c>AUDIO_GRID_WIDTH</c> x <c>AUDIO_GRID_HEIGHT</c>, 205x195 - for every
    /// view mode and every skin setting, while its grid-type setting names films, series and seasons only.
    /// Music therefore never asks the poster question.
    /// </summary>
    private bool IsMusic
        => string.Equals(AspectName, "AlbumItem", StringComparison.OrdinalIgnoreCase)
        || string.Equals(AspectName, "AudioItem", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The square frame the worn skin draws for this shelf's music, or <c>null</c> when it draws none - in
    /// which case the shelf wears whatever frame the layout asks for, as every other skin's media does.
    /// </summary>
    private string? MusicTemplateKey
    {
      get
      {
        if (!IsMusic)
          return null;

        string key = string.Equals(AspectName, "AlbumItem", StringComparison.OrdinalIgnoreCase)
            ? ALBUM_TEMPLATE_KEY
            : TRACK_TEMPLATE_KEY;
        return SkinEngine.TryTileTemplate(key) != null ? key : null;
      }
    }

    /// <summary>
    /// Whether the viewer may pick the picture a tile wears. Music says no: its tile has one picture, the
    /// album's cover, and a button that offers three shapes to a shelf that answers one is a button that lies.
    /// </summary>
    public bool CanChooseMedia
    {
      get { return !IsMusic; }
    }

    /// <summary>The template name the list on screen asks the skin for, whichever layout it stands in.</summary>
    public string GridTemplateKey
    {
      get
      {
        // The rows come first, for music too: the reference drew its audio LIST as text - the title, its year,
        // its rating, its track count - and kept the squares for the grid and the cover view alone
        // (ListViewAudioItemTemplate against GridViewAudioItemTemplate). A list of squares is what the viewer
        // here rightly called "the cover as a list" - the reference's real list says the names.
        if (Layout == BrowserLayout.List)
          return "TitleRowTemplate";

        // The music square outranks every other frame the layout could ask for: the reference's audio cover
        // view is as square as its audio grid, so walking a music shelf from tiles into covers never makes
        // its covers grow a film case around them.
        return MusicTemplateKey ?? (Layout == BrowserLayout.Cover
            ? COVER_TEMPLATE_KEY
            : TileKey(_tileKind));
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
      RaiseShelfCounters();

      if (value)
        _ = ShowDetailsForSelectedAsync();
      else
        DetailsVisible = false;

      // The viewer's own answer stands for as long as the screen is open: the shelf stops asking the layouts
      // and the skin on its behalf.  It is deliberately NOT written down - the reference's media screen has no
      // memory of the question, and a screen that came back without its details column because somebody once
      // pressed a key is the screen missing its right hand.
      if (!_skinDefaultDetails)
        _viewerAnsweredDetails = true;
    }

    private bool _viewerAnsweredDetails;

    private bool _skinDefaultDetails;

    /// <summary>
    /// Sets the details column from a skin's or a layout's answer rather than from the viewer's. It is
    /// shown on the shelf and not remembered on the screen.
    /// </summary>
    private void DetailsDefaultShown(bool shown)
    {
      if (ShowDetails == shown)
        return;

      _skinDefaultDetails = true;
      try
      {
        ShowDetails = shown;
      }
      finally
      {
        _skinDefaultDetails = false;
      }
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

    /// <summary>
    /// The details answer a screen opens with.  The reference asked its skin settings here
    /// (extEnableGridDetails and friends) and those dials were off for the tile views, which is why that skin
    /// fell back to a strip of details along the foot.  The viewer rejected that fallback: the plot belongs
    /// beside the shelf - above the covers in the cover view - and nowhere else, so every layout opens with the
    /// details on and no dial is asked.  Only the viewer's own D answers the question, and for that visit.
    /// </summary>
    private static bool DetailsAlwaysShown()
      => true;

    /// <summary>How the titles are drawn right now.</summary>
    [ObservableProperty]
    private BrowserLayout _layout = BrowserLayout.List;

    /// <summary>Whether a tile may show its watched corner at all - the skin setting's answer.</summary>
    [ObservableProperty]
    private bool _watchFlagsShown = true;

    /// <summary>
    /// Whether the shelf wears the worn skin's fanart at all - the reference's <c>EnableFanart</c>. The view
    /// binds the whole background layer to this one answer, and the skin-settings screen rewrites it live.
    /// </summary>
    public bool FanartShown { get; private set; }

    /// <summary>
    /// How far the skin dims the fanart - the reference drew its dark backdrop over the picture with exactly
    /// this opacity, and the skin-settings screen writes exactly this number.
    /// </summary>
    public double FanartOverlay { get; private set; }

    /// <summary>
    /// How far the fanart behind the shelf is thrown out of focus, in pixels. Nereus' own dial - the face
    /// that wears a title's picture across the whole screen - and nought from every other face, which leaves
    /// the picture as the library drew it. Nought while the screen wears a copy that was already thrown out
    /// of focus away from the drawing thread, since such a picture says so by itself.
    /// </summary>
    public double FanartBlurRadius
    {
      get { return _backgroundBaked ? 0 : _fanartBlur; }
      private set
      {
        _fanartBlur = value;
        OnPropertyChanged(nameof(FanartBlurRadius));
      }
    }

    private double _fanartBlur;

    /// <summary>The worn settings' blur, or nought when the face worn speaks none.</summary>
    private static double BlurOf(SkinSettingsBase? settings)
    {
      return settings is NereusSkinSettings nereus ? nereus.FanartBlurRadius : 0;
    }

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
      else if (WantsDetails)
        _ = ShowDetailsForSelectedAsync();

      Artwork? fanart = _fanart;
      if (!FanartShown || fanart == null || value == null)
      {
        _fanartFor = Guid.Empty;
        _ = WearFanartAsync(null);
        return;
      }

      Guid id = value.Title.Id;
      _fanartFor = id;
      if (fanart.TryPeek(id, out byte[]? held))
      {
        //  Remembered, so wearing it costs nothing and the room may change its mood in this very breath.
        _ = WearFanartAsync(held);
        return;
      }

      //  Nothing is taken away and nothing is asked for yet. The reference makes a picture stand still half a
      //  second before it is worn at all - <c>ImageSourceWrapper Delay="0.5" DelayInOut="true"</c> on its
      //  background image (backgrounds/default-background.xaml:58-62), the same half second our own
      //  <see cref="DissolveImage.DelaySeconds"/> mirrors - and a viewer walking a wall covers a hundred
      //  titles in the time one backdrop takes to fetch, read and carry to the screen, of which at most the
      //  last one is ever worn. What is worn while it stands is the previous title's picture, which is also
      //  what the reference shows during those half a second rather than a blank room.
      _ = ReadFanartWhenStillAsync(fanart, id);
    }

    /// <summary>
    /// The reference's <c>Delay</c>: how long a title has to be stood on before its backdrop is fetched and
    /// worn.
    /// </summary>
    internal static readonly TimeSpan FANART_SETTLING = TimeSpan.FromMilliseconds(500);

    private int _fanartStillTurn;

    /// <summary>
    /// Asks the server for one title's backdrop once the viewer has stood on that title for
    /// <see cref="FANART_SETTLING"/>, and asks for nothing at all when they walked on.
    /// </summary>
    private async Task ReadFanartWhenStillAsync(Artwork fanart, Guid id)
    {
      int turn = ++_fanartStillTurn;

      //  Waited without ConfigureAwait(false) on purpose: what follows touches what the screen is bound to, and
      //  that belongs to the thread that drew the first frame.
      await Task.Delay(FANART_SETTLING);

      //  Walked on - and the title behind this one already put its own question on the same counter.
      if (turn != Volatile.Read(ref _fanartStillTurn) || _fanartFor != id)
        return;

      await ReadFanartAsync(fanart, id);
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
          _ = WearFanartAsync(picture);
      }
      catch (ServerApiException)
      {
        if (_fanartFor == id)
          _ = WearFanartAsync(null);
      }
    }

    private byte[]? _backgroundRaw;
    private int _backgroundTurn;
    private bool _backgroundBaked;

    /// <summary>
    /// Putting a fetched backdrop behind the shelf, thrown out of focus by however far the worn face asks
    /// for. That arithmetic takes about a sixth of a second on a full-size picture, which is a fifth of a
    /// step between rows, so it is done away from the thread that draws: the shelf keeps wearing what it
    /// wore until the copy is ready.
    /// </summary>
    /// <remarks>
    /// Walking on while a copy is being made is answered by the turn the picture was worn for, so the copy of
    /// a backdrop the viewer has left behind never reaches the screen. A copy already in
    /// <see cref="BackgroundBlur"/> answers at once, which is what makes the walk back over a shelf as quick
    /// as the walk over it the first time.
    /// </remarks>
    private async Task WearFanartAsync(byte[]? raw)
    {
      int turn = ++_backgroundTurn;
      _backgroundRaw = raw;

      double radius = _fanartBlur;
      if (raw == null || !FanartShown)
      {
        ShowBackground(raw, false);
        return;
      }

      // A backdrop is the size of the television, and reading it costs about a tenth of a second. Asked of the
      // binding, that tenth of a second is paid in front of the frame the viewer is waiting for, once for every
      // step of a walk over the shelf - which is what a juddering wall is made of. So the bytes are read here,
      // away from the thread that draws, and the screen is told about the picture afterwards; until then the
      // shelf keeps wearing the backdrop it wore, which is a better answer than a blank moment.
      byte[]? worn = raw;
      if (radius > 0)
        worn = (await BackgroundBlur.BlurredAsync(raw, radius)) ?? raw;

      // The viewer walked on, or the face - and with it the question - changed in the meantime.
      if (turn != Volatile.Read(ref _backgroundTurn))
        return;

      byte[]? toWear = worn;

      //  But only when it is not already lying there: a shelf walked back onto a title it has just worn has the
      //  answer in hand, and the room is expected to wear it in this very breath - a picture that arrives one
      //  turn later leaves a screen that is plainly, for one frame, wearing nothing.
      if (!SkinFanartConverter.Instance.StandsReady(toWear, 0))
      {
        await Task.Run(() => SkinFanartConverter.Instance.Warm(toWear, 0));

        if (turn != Volatile.Read(ref _backgroundTurn))
          return;
      }

      ShowBackground(toWear, !ReferenceEquals(toWear, raw));
    }

    /// <summary>
    /// What stands behind the shelf, said together with how far it still has to be thrown out of focus - the
    /// two are one answer, since a copy that comes already blurred is worn at nought.
    /// </summary>
    private void ShowBackground(byte[]? bytes, bool baked)
    {
      _backgroundBaked = baked;
      BackgroundArtwork = bytes;
      OnPropertyChanged(nameof(FanartBlurRadius));
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
      RaiseShelfCounters();

      // The corners belong to the layout the viewer just walked into, and the covers wear the poster rather
      // than the grid's kinded picture - so the rows the shelf already holds are repainted with what this
      // layout draws, from caches that make the second visit to a layout cost no request at all.
      WatchFlagsShown = WatchFlagsFor(value);
      foreach (TitleViewModel row in new List<TitleViewModel>(Titles))
        row.WatchFlagsShown = WatchFlagsShown;
      // Walking into another layout also walks into that layout's details dial - but only while the viewer
      // never answered the question on this visit; the viewer's own answer outranks every dial.
      if (!_viewerAnsweredDetails)
        DetailsDefaultShown(DetailsAlwaysShown());
      // The rows the viewer is looking at are painted again by the screen, which is the one that knows which rows
      // those are; the generation bump throws away the pictures this layout was waiting for.
      PictureSourceChanged();
    }

    /// <summary>
    /// Bumped every time the shelf decides which pictures its rows wear. A repaint that finished after the
    /// viewer switched layout again is thrown away rather than painted over the newer layout's pictures.
    /// </summary>
    private int _paintGeneration;

    /// <summary>
    /// Whose pictures the rows wear right now. Music first and on its own terms: a shelf of albums or tracks
    /// wears the album's cover, which is what the import stored and what the server hands a track out of its
    /// album - the database's thumbnail blob is no use here because MP2's audio importer deliberately leaves
    /// it empty and keeps the picture that came out of a file in the fanart cache. Otherwise a cover is a
    /// poster the reference sized generously, so the cover view draws the database's poster blob even on a
    /// shelf whose grid wears banners or thumbnails.
    /// </summary>
    private Artwork? ArtworkForLayout
    {
      get
      {
        // Music in the layouts that stand squares wears the album's cover; its rows stay the reference's
        // rows - names and counts, no picture - because the reference's audio list never wore one either.
        if (IsMusic)
          return Layout == BrowserLayout.List ? _artwork : CoverArtwork ?? _artwork;

        // A season's cover is the poster it wears on its tiles, which is not in the database - see TileArtworkFor.
        if (Layout == BrowserLayout.Cover)
          return MediaCatalog.IsSeason(AspectName)
              ? TileArtworkFor(GridViewType.Poster) ?? _artwork
              : _artwork;

        return TileArtworkFor(_tileKind) ?? _artwork;
      }
    }

    private Artwork? _coverArtwork;

    /// <summary>
    /// The music shelves' pictures, made once on first asking and kept after that: the cover, asked of the
    /// server by name rather than read out of the database.
    /// </summary>
    private Artwork? CoverArtwork
    {
      get
      {
        if (_coverArtwork != null || _client == null)
          return _coverArtwork;

        ServerApiClient client = _client;
        _coverArtwork = PicturesOf(client, "Cover", id => client.GetFanArtAsync(id, "Cover"));
        return _coverArtwork;
      }
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
      // Music answers one picture, so its screen says which one instead of offering a choice it cannot keep.
      get { return IsMusic ? "Media: cover" : "Media: " + _tileKind; }
    }

    /// <summary>
    /// One step round the pictures a tile can wear - poster, banner, thumbnail, and round again - asked on
    /// the screen itself rather than behind a skin's settings page, and remembered for this screen alone.
    /// The shelf repaints its rows with the kind's own bytes, from the cache that kind already has.
    /// </summary>
    [RelayCommand]
    public void SwitchMedia()
    {
      // Music is not in the rotation. Its square wears the album's cover in every skin and every layout, and
      // a banner shelf of albums would be a wall of "no banner" placeholders - which is why the button is not
      // on the screen at all. See CanChooseMedia.
      if (IsMusic)
        return;

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
      PictureSourceChanged();
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
    /// The season number the server is told to narrow this shelf to, or null for a shelf that is not one season.
    /// </summary>
    /// <remarks>
    /// Only a season the library holds as nothing but a number on its episodes needs asking this way: it has no id of
    /// its own to look inside of, so its shelf is the series' episodes narrowed by the number - the two parts the row
    /// it was opened from carries (<see cref="TitleViewModel.AskId"/> and <see cref="TitleViewModel.AskSeason"/>).
    /// </remarks>
    public int? SeasonFilter { get; }

    /// <summary>
    /// Where the answers about a row that is not a title of the library's own have to be asked: the series the row
    /// was counted out of, under its season number. Written as the rows are read and read by every ask that only has
    /// the row's own id to go by - the pictures, which are keyed by that id so that two seasons of one series do not
    /// share a cached poster.
    /// </summary>
    private readonly Dictionary<Guid, (Guid Id, int? Season)> _asks = new Dictionary<Guid, (Guid Id, int? Season)>();

    /// <summary>
    /// Turning the id of a row into the id and season number to ask the server with; for the thousand titles to one
    /// counted season this is the row's own id and nothing else.
    /// </summary>
    private (Guid Id, int? Season) AskOf(Guid rowId)
    {
      (Guid Id, int? Season) ask;
      return _asks.TryGetValue(rowId, out ask) ? ask : (rowId, null);
    }

    /// <summary>
    /// Whether pressing a row of this list opens what it holds rather than playing it, which is decided by the
    /// aspect rather than by whether the row has a file: an album never has one, and neither has a film whose
    /// disc is out of the drive, and only one of those two is a row worth opening.
    /// </summary>
    public bool DrillsIn { get; }

    /// <summary>
    /// What this list is ordered by, in the server's own word. The shelf starts out ordered the way its kind is
    /// always ordered - a season by episode number, an album by track - and the screen's menu hands the word over
    /// to the viewer afterwards (<see cref="PickSortAsync"/>), because "show me the newest ones" is a question
    /// every shelf has to answer.
    /// </summary>
    public string SortOrder { get; private set; }

    /// <summary>The word the screen says for the order it is reading in.</summary>
    public string SortLabel
    {
      get { return "Sort: " + SortWord(SortOrder); }
    }

    /// <summary>
    /// The category this list was cut by - <c>genre</c>, <c>year</c>, <c>actors</c>, <c>directors</c> or
    /// <c>writers</c> - or null when it is a whole shelf. With <see cref="GroupValue"/> it is the second half of
    /// the same question: the shelf was counted by a category, a value was picked out of that count, and this
    /// list is the titles that carry it.
    /// </summary>
    /// <remarks>
    /// Set from two places that ask the same question in two ways: the browse screen hands a cut over when it
    /// opens the shelf, and the screen's own menu makes or clears one where the viewer stands.
    /// </remarks>
    public string? GroupBy { get; private set; }

    /// <summary>The value picked out of the count, or null. Only means anything with <see cref="GroupBy"/>.</summary>
    public string? GroupValue { get; private set; }

    /// <summary>Whether the shelf on screen is a cut of the shelf rather than the whole of it.</summary>
    public bool HasShelfFilter => !string.IsNullOrEmpty(GroupBy) && !string.IsNullOrEmpty(GroupValue);

    /// <summary>The words the screen says about its cut, which is nothing when the whole shelf is showing.</summary>
    public string FilterLabel
    {
      get
      {
        if (!HasShelfFilter)
          return "Filter: All";
        return "Filter: " + CategoryWord(GroupBy!) + " = " + GroupValue;
      }
    }

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
    /// Whether this shelf is worn as covers, which is the one layout the reference laid out sideways: the covers
    /// in a row along the bottom, their details centred above them, and the screen's menu under them instead of
    /// beside them.
    /// </summary>
    public bool IsCoverLayout => Layout == BrowserLayout.Cover;

    /// <summary>
    /// Whether the shelf wants the details of its highlighted title at all - the one panel it has, beside the
    /// tiles or above the covers.  The reference also knew a strip of details along the foot for the tile views
    /// whose side panel had been switched off; the viewer rejected that place, so the strip is gone and this
    /// question is simply the viewer's answer to the details question.
    /// </summary>
    public bool WantsDetails => ShowDetails;

    /// <summary>
    /// Whether the details stand beside the shelf as the wide column the reference laid in the right-hand cell
    /// of <c>SimpleShowItems.xaml</c>: grid and list only, and only while the viewer asked for the panel. Covers
    /// answer the same question with <see cref="ShowsCoverDetails"/>.
    /// </summary>
    public bool ShowsSideDetails => ShowDetails && Layout != BrowserLayout.Cover;

    /// <summary>
    /// Whether the details lie centred above the covers, on the horizontal gradient the reference laid under
    /// <c>MultiDetailsCover.inc</c> - the cover layout's answer, which never has a panel beside its row.
    /// </summary>
    public bool ShowsCoverDetails => ShowDetails && Layout == BrowserLayout.Cover;

    /// <summary>
    /// Whether the shelf draws the reference's floor gradient along its bottom edge: the dark band that lifts the
    /// names off the fanart.  The reference's media master laid it under grid and list whatever the details said,
    /// and only the cover view - which lays its own shadows over the wall of posters - went without it.
    /// </summary>
    public bool ShowsFloorGradient => Layout != BrowserLayout.Cover && Titles.Count > 0;

    private void RaiseShelfCounters()
    {
      OnPropertyChanged(nameof(HasTitles));
      OnPropertyChanged(nameof(SelectedNumber));
      OnPropertyChanged(nameof(TotalNumber));
      OnPropertyChanged(nameof(ShowsFloorGradient));
      OnPropertyChanged(nameof(IsCoverLayout));
      OnPropertyChanged(nameof(WantsDetails));
      OnPropertyChanged(nameof(ShowsSideDetails));
      OnPropertyChanged(nameof(ShowsCoverDetails));
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
      HasMore = false;
      await ReadAsync(0);
    }

    /// <summary>
    /// Reads the next page because the bottom of the shelf is in sight.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There is no "more" row to press and no button beside the list. The reference has neither - it reads the whole
    /// shelf in one blow and lets the panel virtualise it - and a viewer who has scrolled to the bottom of what is
    /// on the screen is not asking a question that needs answering, they are telling the screen to go on.
    /// </para>
    /// <para>
    /// Normally nothing has to wait for a scroll at all, because the shelf finishes reading itself the moment it was
    /// opened (<see cref="ReadTheRestAsync"/>). This is the answer for the two cases that can leave a shelf short:
    /// a viewer standing at the bottom of one that stopped at <see cref="MAX_ROWS"/>'s page boundary, and a test
    /// that walks a selection without owning a television.
    /// </para>
    /// <para>
    /// A page already on its way is waited for rather than cancelled: the screen says "the bottom is in sight" from
    /// every scroll event and the selection says it from every step of the walk, so one page being read is
    /// accompanied by the demand for it several times over.
    /// </para>
    /// </remarks>
    public async Task KeepReadingAsync()
    {
      if (!HasMore || Titles.Count >= MAX_ROWS || _running != null)
        return;

      await ReadAsync(Titles.Count);
    }

    /// <summary>
    /// Reads the rest of the shelf, without being asked, so that the shelf on the screen is the shelf the server
    /// has rather than the first screenful of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the difference the viewer sees: a shelf whose count only grows while scrolling is a shelf that makes
    /// the reader stop and wait twice - once for the page and once for the scrolling they were doing anyway. The
    /// reference reads its shelf whole and shows it whole; one question of a screenful is answered first so that
    /// names appear at once, and what the server still has comes in behind it, in asks as big as the server allows.
    /// </para>
    /// <para>
    /// A shelf being finished is a background errand and dies quietly: <paramref name="generation"/> names the
    /// shelf this run belongs to, and a search, a filter or a different shelf started while a question was on its
    /// way bumps that number, which ends this run at its next pause. That is also why the run never cancels
    /// anything itself - the newer read cancels what it supersedes, and two readers of one shelf would only race
    /// each other over the same offset.
    /// </para>
    /// <para>
    /// Two ways this stops besides running out of shelf: the ceiling, and an answer that brought nothing while
    /// promising more - a shelf that argues with itself is not worth an endless question.
    /// </para>
    /// <para>
    /// Two runs of this over one shelf would ask the same offset twice and append the same titles twice, so the
    /// claim below keeps one run per shelf - and lets a newer shelf take the claim from the run that is about to
    /// notice it is gone anyway.
    /// </para>
    /// </remarks>
    private async Task ReadTheRestAsync(long generation)
    {
      long held = Interlocked.Read(ref _finishing);
      if (held == generation)
        return;

      // Either nobody is finishing anything (-1) or the shelf being finished is an older one, which is by now on
      // its way out on its own.
      Interlocked.Exchange(ref _finishing, generation);
      try
      {
        // A moment for the screen to draw what it has before the next question goes out.
        await Task.Delay(DRAW_BETWEEN_ASKS_MILLISECONDS);

        while (generation == Volatile.Read(ref _generation) && HasMore && Titles.Count < MAX_ROWS)
        {
          int read = Titles.Count;
          await ReadAsync(read);

          if (Titles.Count == read)
            break;

          await Task.Delay(DRAW_BETWEEN_ASKS_MILLISECONDS);
        }
      }
      finally
      {
        // Left alone if a newer shelf claimed it in the meantime; that run clears its own claim.
        Interlocked.CompareExchange(ref _finishing, -1, generation);
      }
    }

    /// <summary>The generation whose shelf is being finished, or -1 while nothing is.</summary>
    private long _finishing = -1;

    /// <summary>
    /// Whether a search is on this list, which is what the "Clear" button appears and disappears for.
    /// </summary>
    public bool HasSearch
    {
      get { return !string.IsNullOrEmpty(ActiveSearch); }
    }

    /// <summary>
    /// Whether the viewer asked to type: the <c>F</c> key and the menu's "Search" row both set it, and the field
    /// itself is on the screen while it holds. The reference kept its search behind a screen of its own with a
    /// keyboard on it; this screen has the field already and only has to get out of the shelf's way until the
    /// viewer is done typing.
    /// </summary>
    [ObservableProperty]
    private bool _searchOpen;

    /// <summary>Whether the search field is on screen: opened to type in, or still holding what was typed.</summary>
    public bool ShowsSearch => SearchOpen || HasSearch;

    partial void OnSearchOpenChanged(bool value)
    {
      OnPropertyChanged(nameof(ShowsSearch));
    }

    /// <summary>
    /// Puts the search field on the screen and asks for the caret to be moved into it - one key or one row of the
    /// menu, and the shelf is searched from there.
    /// </summary>
    public void OpenSearch()
    {
      SearchOpen = true;
      SearchFocusRequested?.Invoke(this, EventArgs.Empty);
    }

    partial void OnActiveSearchChanged(string value)
    {
      OnPropertyChanged(nameof(HasSearch));
      OnPropertyChanged(nameof(ShowsSearch));
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
      HasMore = false;
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

    // -------------------------------------------------------------------------
    // The screen's own menu, which the reference slid in from the left edge
    // -------------------------------------------------------------------------

    /// <summary>
    /// Which page the screen's menu is showing. The reference hung a dialog of its own behind every command in
    /// the media menu - <c>DialogSwitchSort</c>, <c>DialogSwitchFilter</c>, <c>DialogSwitchViewMode</c> - and put
    /// the menu away once a pick was made; this menu keeps those dialogs as pages of the one panel that slid in,
    /// because walking out of a panel that is already on screen is two arrow keys rather than a closed dialog and
    /// an opened one.
    /// </summary>
    private enum MenuPage
    {
      /// <summary>The commands themselves, which is what opening the menu shows.</summary>
      Commands,

      /// <summary>Rows, tiles, covers - and whether the details stand beside them.</summary>
      View,

      /// <summary>The picture a tile wears.</summary>
      Picture,

      /// <summary>The orders this shelf's kind can be read in.</summary>
      Sort,

      /// <summary>The categories the shelf has been counted by.</summary>
      Filter,

      /// <summary>The values of one category, as the server counted them.</summary>
      Values
    }

    /// <summary>
    /// Whether the screen's menu stands open. The reference kept it off the edge of the television and slid the
    /// whole shelf 200 pixels into the room it left behind (<c>master_mediamenu.xaml</c> and its
    /// <c>HiddenMenuSlideIn</c>, 350 ms), so that nothing is ever hidden under a panel and no command needs the
    /// screen to change underneath the viewer.
    /// </summary>
    [ObservableProperty]
    private bool _mediaMenuVisible;

    /// <summary>The word above the menu, which says what page of it the viewer is standing on.</summary>
    [ObservableProperty]
    private string _mediaMenuHeading = "Menu";

    /// <summary>The rows the menu shows right now, for the page <see cref="_menuPage"/> says it is on.</summary>
    public ObservableCollection<MediaBrowserMenuEntry> MediaMenu { get; } = new ObservableCollection<MediaBrowserMenuEntry>();

    /// <summary>Where in the menu the viewer stands.</summary>
    private MenuPage _menuPage;

    /// <summary>The category whose values are on the menu, while the values page is up.</summary>
    private string? _menuCategory;

    /// <summary>
    /// Asks the screen to put the cursor into its search field. The reference opened a screen of its own for the
    /// question (its filter menu's search entry led to a keyboard), and this screen has the field on it already -
    /// so the menu says where the typing happens and gets out of the way.
    /// </summary>
    public event EventHandler? SearchFocusRequested;

    /// <summary>Opens the menu at its first page, which is what the reference's Info key did.</summary>
    [RelayCommand]
    public void ShowMediaMenu()
    {
      _menuPage = MenuPage.Commands;
      BuildMediaMenu();
      MediaMenuVisible = true;
    }

    /// <summary>Slides the menu back off the edge of the screen.</summary>
    [RelayCommand]
    public void HideMediaMenu()
    {
      MediaMenuVisible = false;
    }

    /// <summary>
    /// The Info key, which is the reference's key for this: opens the menu over the shelf, and puts it away when
    /// it is already open.
    /// </summary>
    [RelayCommand]
    public void ToggleMediaMenu()
    {
      if (MediaMenuVisible)
        HideMediaMenu();
      else
        ShowMediaMenu();
    }

    /// <summary>
    /// What the back key does while the menu is open: a page steps back to the commands, and the commands put the
    /// menu away. The reference's dialogs closed on Escape, which is the same answer one level up.
    /// </summary>
    [RelayCommand]
    public void MediaMenuBack()
    {
      if (_menuPage == MenuPage.Commands)
        HideMediaMenu();
      else
        ShowMediaMenu();
    }

    /// <summary>
    /// Builds the menu for the page the viewer is on. Every label is written from the shelf's state at the moment
    /// the page goes up, which is why picking anything closes the menu: a menu whose row still says "View: Grid"
    /// after the viewer laid the shelf out in covers is a menu that lies.
    /// </summary>
    private void BuildMediaMenu()
    {
      MediaMenu.Clear();
      switch (_menuPage)
      {
        case MenuPage.View:
          MediaMenuHeading = "View";
          AddMenuRow("List", "view_list", new RelayCommand(() => PickLayout(BrowserLayout.List)), Layout == BrowserLayout.List);
          AddMenuRow("Grid", "grid_view", new RelayCommand(() => PickLayout(BrowserLayout.Grid)), Layout == BrowserLayout.Grid);
          AddMenuRow("Cover", "cover", new RelayCommand(() => PickLayout(BrowserLayout.Cover)), Layout == BrowserLayout.Cover);
          // The reference kept the details switch beside the view modes rather than among the media commands,
          // because details are the second half of the layout question and not a command of their own. The one
          // row the screen gives it is this one: the main page asking the same question a second time only
          // made the viewer choose which "Details" to press.
          AddMenuRow(DetailsLabel, "details", new RelayCommand(PickDetails), ShowDetails);
          return;

        case MenuPage.Picture:
          MediaMenuHeading = "Picture";
          AddMenuRow("Poster", "cover", new RelayCommand(() => PickPicture(GridViewType.Poster)), _tileKind == GridViewType.Poster);
          AddMenuRow("Banner", "banner", new RelayCommand(() => PickPicture(GridViewType.Banner)), _tileKind == GridViewType.Banner);
          AddMenuRow("Thumbnail", "media_format", new RelayCommand(() => PickPicture(GridViewType.Thumbnail)), _tileKind == GridViewType.Thumbnail);
          return;

        case MenuPage.Sort:
          MediaMenuHeading = "Sort";
          foreach ((string Word, string Key) choice in SortChoices())
          {
            string picked = choice.Key;
            AddMenuRow(choice.Word, SortIcon(picked), new AsyncRelayCommand(() => PickSortAsync(picked)),
                string.Equals(SortOrder, picked, StringComparison.OrdinalIgnoreCase));
          }
          return;

        case MenuPage.Filter:
          MediaMenuHeading = "Filter";
          if (HasShelfFilter)
            AddMenuRow("All", "library_books", new AsyncRelayCommand(() => PickFilterAsync(null, null)));
          // The watched question is a filter, so it stands among the filters now rather than on the commands
          // page. Its three answers keep their words; only the all-answers-one is named the way the reset
          // beside it is - and pressing the row cycles through the three the way the button in front of the
          // list always did.
          AddMenuRow(ActiveSeen == "" ? "All" : SeenLabel, ActiveSeen == "" ? "library_books" : "filter",
              new AsyncRelayCommand(async () =>
              {
                await CycleSeenAsync();
                HideMediaMenu();
              }));
          foreach (BrowseCategory category in BrowseViewModel.CATEGORIES)
          {
            string by = category.Word;
            AddMenuRow(category.Label, CategoryIcon(by), new AsyncRelayCommand(() => ShowFilterValuesAsync(by)));
          }
          return;

        case MenuPage.Values:
          MediaMenuHeading = Capitalise(CategoryWord(_menuCategory ?? "")) + "s";
          AddMenuRow("Everything", "library_books", new AsyncRelayCommand(() => PickFilterAsync(null, null)));
          return;

        default:
          MediaMenuHeading = "Menu";
          AddMenuRow("Search", "search", new RelayCommand(() =>
          {
            HideMediaMenu();
            SearchFocusRequested?.Invoke(this, EventArgs.Empty);
          }));
          AddMenuRow(FilterLabel, "filter", new RelayCommand(() => GoToPage(MenuPage.Filter)));
          AddMenuRow(SortLabel, "sort", new RelayCommand(() => GoToPage(MenuPage.Sort)));
          AddMenuRow(LayoutLabel, "grid_view", new RelayCommand(() => GoToPage(MenuPage.View)));
          if (CanChooseMedia)
            AddMenuRow(MediaLabel, "media_format", new RelayCommand(() => GoToPage(MenuPage.Picture)));
          // No watched row here: it is a filter, and it stands on the "Filter" page with the rest of them.
          // No details row here either: the switch stands on the "View" page, beside the layouts it belongs
          // to, and the same question asked twice in one rail only makes the viewer guess which is real.
          AddMenuRow("Title menu", "system", new RelayCommand(() =>
          {
            HideMediaMenu();
            ShowMenu();
          }));
          return;
      }
    }

    /// <summary>Puts another page of the menu up without reopening it, which is what a command's arrow does.</summary>
    private void GoToPage(MenuPage page)
    {
      _menuPage = page;
      BuildMediaMenu();
    }

    /// <summary>One row of the menu, with the skin's own picture beside it where the skin has one to give.</summary>
    private void AddMenuRow(string label, string icon, System.Windows.Input.ICommand command, bool isCurrent = false)
    {
      MediaMenu.Add(new MediaBrowserMenuEntry(label, icon, command, isCurrent));
    }

    /// <summary>A menu row that says words and has no picture of its own.</summary>
    private void AddMenuRow(string label, System.Windows.Input.ICommand command)
    {
      MediaMenu.Add(new MediaBrowserMenuEntry(label, "", command, false));
    }

    /// <summary>
    /// Lays the shelf out in the picked shape and puts the menu away over it. The cycle is walked rather than the
    /// property written, because walking it is what saves the viewer's answer for this screen.
    /// </summary>
    private void PickLayout(BrowserLayout layout)
    {
      for (int step = 0; step < 3 && Layout != layout; step++)
        SwitchViewCommand.Execute(null);

      HideMediaMenu();
    }

    /// <summary>Puts another picture on the tiles and puts the menu away.</summary>
    private void PickPicture(GridViewType kind)
    {
      for (int step = 0; step < 3 && _tileKind != kind; step++)
        SwitchMediaCommand.Execute(null);

      HideMediaMenu();
    }

    /// <summary>Turns the details panel on or off, from the menu's own switch.</summary>
    private void PickDetails()
    {
      ToggleDetails();
      HideMediaMenu();
    }

    /// <summary>
    /// The orders this kind of shelf can be read in, in the words the screen says them in. The four every shelf
    /// answers are the name, the year, the day the library got the title and the last time it was played; the
    /// orders that only mean something inside one series or one album are offered where they do, because a shelf
    /// of films sorted "by track" is a shelf of films sorted by nothing.
    /// </summary>
    /// <remarks>
    /// These are exactly the words the server answers (<c>MediaController.ReadSort</c>); one it did not know would
    /// come back silently ordered by name, which is a menu row that lies about what it did.
    /// </remarks>
    private List<(string Word, string Key)> SortChoices()
    {
      List<(string Word, string Key)> choices = new List<(string Word, string Key)>
      {
          ("by title", "title"),
          ("by year", "year"),
          ("by date added", "added"),
          ("by last played", "lastplayed")
      };

      if (MediaCatalog.IsSeason(AspectName))
        choices.Insert(1, ("by season number", "season"));
      else if (string.Equals(AspectName, "episodes", StringComparison.OrdinalIgnoreCase))
      {
        choices.Insert(1, ("by season number", "season"));
        choices.Insert(2, ("by episode number", "episode"));
      } else if (string.Equals(AspectName, "tracks", StringComparison.OrdinalIgnoreCase)
          || string.Equals(AspectName, "songs", StringComparison.OrdinalIgnoreCase))
        choices.Insert(1, ("by track number", "track"));

      return choices;
    }

    /// <summary>
    /// Reads the shelf again in another order. The list is thrown away rather than re-sorted on screen, because
    /// the screen only ever holds the pages it has read: eleven hundred films sorted by year have to be re-asked,
    /// not shuffled, or the first fifty the viewer is looking at are the wrong fifty.
    /// </summary>
    public async Task PickSortAsync(string sort)
    {
      if (string.IsNullOrWhiteSpace(sort)
          || string.Equals(SortOrder, sort, StringComparison.OrdinalIgnoreCase))
      {
        HideMediaMenu();
        return;
      }

      SortOrder = sort;
      OnPropertyChanged(nameof(SortLabel));
      HideMediaMenu();
      await ReaskAsync();
    }

    /// <summary>
    /// Asks the server how many titles carry each value of one category and puts them on the menu - the same
    /// count the browse screen shows in its middle column, reached from the shelf it belongs to.
    /// </summary>
    /// <remarks>
    /// The count is asked for when the page is opened rather than kept: a shelf gains titles while a viewer is
    /// sitting in front of it, and a menu of stale counts is a menu that sends them to titles that are not there.
    /// </remarks>
    public async Task ShowFilterValuesAsync(string by)
    {
      if (_client == null)
      {
        Status = "There is no server to count this shelf by " + CategoryWord(by);
        return;
      }

      _menuCategory = by;
      _menuPage = MenuPage.Values;
      BuildMediaMenu();
      MediaMenu.Insert(0, new MediaBrowserMenuEntry("Counting the " + CategoryWord(by) + "s...", "",
          new RelayCommand(() => { }), false));

      try
      {
        MediaGroups groups = await _client.GetGroupsAsync(AspectName, by);
        if (_menuPage != MenuPage.Values || !string.Equals(_menuCategory, by, StringComparison.OrdinalIgnoreCase))
          return;

        MediaMenu.RemoveAt(0);
        foreach (MediaGroup group in groups.Values)
        {
          string value = group.Value!.Trim();
          bool picked = string.Equals(GroupValue, value, StringComparison.OrdinalIgnoreCase)
              && string.Equals(GroupBy, by, StringComparison.OrdinalIgnoreCase);
          MediaMenu.Add(new MediaBrowserMenuEntry(value + "  (" + group.Count + ")", CategoryIcon(by),
              new AsyncRelayCommand(() => PickFilterAsync(by, value)), picked));
        }

        if (groups.Values.Count == 0)
          MediaMenu.Add(new MediaBrowserMenuEntry("Nothing on this server carries a " + CategoryWord(by), "",
              new RelayCommand(() => { }), false));
        if (!string.IsNullOrEmpty(groups.Error))
          Status = groups.Error;
      }
      catch (ServerApiException ex)
      {
        MediaMenu.Clear();
        Status = ex.Message;
      }
      catch (Exception ex)
      {
        MediaMenu.Clear();
        Status = "Counting the shelf failed: " + ex.Message;
      }
    }

    /// <summary>
    /// Cuts the shelf to one value of one category, or takes the cut off again when either half is missing. The
    /// list is a different list the moment the cut changes, so it starts again at its first title.
    /// </summary>
    public async Task PickFilterAsync(string? by, string? value)
    {
      bool same = string.Equals(GroupBy ?? "", by ?? "", StringComparison.OrdinalIgnoreCase)
          && string.Equals(GroupValue ?? "", value ?? "", StringComparison.OrdinalIgnoreCase);
      GroupBy = by;
      GroupValue = value;
      OnPropertyChanged(nameof(HasShelfFilter));
      OnPropertyChanged(nameof(FilterLabel));
      HideMediaMenu();
      if (!same)
        await ReaskAsync();
    }

    /// <summary>The screen's own words for a sort key, for the row that names the order the shelf is read in.</summary>
    private static string SortWord(string sort)
    {
      switch (sort)
      {
        case "year":
          return "year";
        case "added":
          return "date added";
        case "lastplayed":
          return "last played";
        case "season":
          return "season number";
        case "episode":
          return "episode number";
        case "track":
          return "track number";
        default:
          return "title";
      }
    }

    /// <summary>The skin's own icon for an order, where the skin carries one.</summary>
    private static string SortIcon(string sort)
    {
      switch (sort)
      {
        case "year":
          return "year";
        case "season":
        case "episode":
        case "track":
          return "track";
        default:
          return "sort";
      }
    }

    /// <summary>The skin's own icon for a filter category, where the skin carries one.</summary>
    private static string CategoryIcon(string by)
    {
      switch (by)
      {
        case "year":
          return "year";
        case "actors":
        case "directors":
        case "writers":
          return "actor";
        default:
          return "genre";
      }
    }

    /// <summary>What the screen calls a category in a sentence.</summary>
    private static string CategoryWord(string by)
    {
      switch (by)
      {
        case "year":
          return "year";
        case "actors":
          return "actor";
        case "directors":
          return "director";
        case "writers":
          return "writer";
        default:
          return "genre";
      }
    }

    /// <summary>Upper-cases the first letter of a word, for a heading the viewer reads.</summary>
    private static string Capitalise(string word)
    {
      if (string.IsNullOrEmpty(word))
        return word;
      return char.ToUpperInvariant(word[0]) + word.Substring(1);
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
      if (!WantsDetails || title == null || _client == null)
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
      // Two properties answer one question, and both have to go: the viewer's standing choice for the panel, and
      // the reading that is on the screen right now. A grid reads the selected title even while its panel is off
      // (its foot strip is laid out of the same reading), so putting only the choice away left the screen wearing
      // details still - and a screen that answers Back with "away with the details" while it is still wearing
      // them has taken the key away from the viewer for good.
      ShowDetails = false;
      DetailsVisible = false;
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
        // Asked of the title that holds the answers rather than of the row's own key, which for a counted season is
        // not a title at all; the guards below stay keyed by the row, because that is what the viewer is looking at.
        MediaItemDetails details = await _client.GetItemAsync(title.AskId, title.AskSeason);
        if (_detailsAskFor != asked)
          return;

        _detailsFor = asked;

        // A season is named by its row rather than by the server: what MP2 writes in the title column of a season
        // is the series' name, the series' year and the number ("Bones (2005) S01"), which is a heading for a shelf
        // of every season in the house. Above one season's episodes that says the series twice and the year twice,
        // and hides the one thing the panel is there to say - which season this is.
        bool isSeason = MediaCatalog.IsSeason(title.ReadThrough);
        string name = isSeason || string.IsNullOrWhiteSpace(details.Title)
            ? title.Name
            : details.Title!.Trim();
        DetailsHeading = details.Year.HasValue && !isSeason ? name + " (" + details.Year.Value + ")" : name;
        // The reference's detail screens put the tagline under the title and above the facts - the sentence the
        // studio wrote, before the sentences about length and kind. A title without one simply shows no line.
        DetailsTagline = details.Tagline?.Trim() ?? "";
        DetailsLine = DescribeLine(details, title);
        DetailsText = details.Description ?? "Nothing was written about this title on this server.";
        DetailsPath = ShownPath(details.Path ?? title.Title.Path);
        DetailsRating = ToldRating(details);
        DetailsRatingWords = DetailsRating.HasValue
            ? "(" + DetailsRating.Value.ToString("0.0", CultureInfo.InvariantCulture) + ")"
            : "";
        FillDetailsStars(DetailsRating);
        FillDetailsFacts(details, title);
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
    /// Tells the five star pictures how much of their light the rating leaves them: whole for a star the rating
    /// filled, a quarter for a star it never reached, and the part between for the star it filled half way.
    /// </summary>
    /// <param name="rating">
    /// The rating on the library's ten points, or <c>null</c> for a title nobody rated - which leaves the five
    /// pictures at the quarter of their light they wait at, and the number beside them is not drawn at all.
    /// </param>
    private void FillDetailsStars(double? rating)
    {
      // Ten points to five stars, so a point is half a star; the reference drew the fifth star the same size as
      // the first and let the light say the rest.
      double stars = Math.Clamp((rating ?? 0) / 2.0, 0, 5);
      for (int star = 0; star < DetailsStars.Count; star++)
        DetailsStars[star] = 0.25 + 0.75 * Math.Clamp(stars - star, 0, 1);
    }

    /// <summary>
    /// The number the panel speaks and lights its stars with, or <c>null</c> when the library has no rating for
    /// the title.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reference binds <c>TotalRating</c> and nothing else, and hides the stars and their number together when
    /// that number is missing (<c>MediaItemListMovieDetails.inc:92-108</c>, where both stand or fall with
    /// <c>TotalRating</c> through <c>ReferenceToBoolConverter</c>). What the server answers for a title nobody
    /// rated is not a missing number but a <c>0</c>, and taking that for a rating drew "0.0" and five dark stars
    /// under every film in the library - which is a screen claiming a film was rated nought. So a nought counts as
    /// no answer here, exactly as the reference's condition counts it.
    /// </para>
    /// <para>
    /// The viewer's own rating (<c>Rating</c>) is the second answer rather than the first: the reference never
    /// shows it at all, so it is only asked for when the library's own total is silent, and a nought is as silent
    /// there as it is for the total.
    /// </para>
    /// </remarks>
    private static double? ToldRating(MediaItemDetails details)
    {
      if (details.TotalRating.HasValue && details.TotalRating.Value > 0)
        return details.TotalRating.Value;
      if (details.Rating.HasValue && details.Rating.Value > 0)
        return details.Rating.Value;
      return null;
    }

    /// <summary>
    /// The words the screen speaks a title's resource out of, from the address the server wrote down for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What the server calls an item's path is a full resource address: the id of the media source the file was
    /// imported from, in braces, then the address itself - <c>{e88e64a8-…}://///HTPC-SERVER/Video/…mkv</c>. The
    /// source id is a key of the library's own books and no path a person reads, so it is dropped; the run of
    /// slashes a resource address opens with is the server's spelling of the two a shared folder opens with, and
    /// is spoken that way here.
    /// </para>
    /// <para>
    /// Everything past that first address is left exactly as the server wrote it, and an address without the
    /// source in front of it is left whole - the screen has nothing to guess from.
    /// </para>
    /// </remarks>
    /// <param name="path">What the server answered for the title, or <c>null</c> when it answered nothing.</param>
    internal static string ShownPath(string? path)
    {
      if (string.IsNullOrWhiteSpace(path))
        return "This title has no resource the server can name.";

      string spoken = path!.Trim();
      if (spoken.Length > 2 && spoken[0] == '{')
      {
        int afterId = spoken.IndexOf('}');
        if (afterId > 0)
        {
          spoken = spoken.Substring(afterId + 1);
          if (spoken.StartsWith(":", StringComparison.Ordinal))
            spoken = spoken.Substring(1);
          spoken = spoken.TrimStart();
        }
      }

      // A shared folder is spoken with two slashes; the resource address spells it with as many as it likes.
      if (spoken.StartsWith("///", StringComparison.Ordinal))
        spoken = "//" + spoken.TrimStart('/');

      return spoken.Length == 0 ? "This title has no resource the server can name." : spoken;
    }

    /// <summary>
    /// Builds the facts the details panel and the home strip stand on, in the order the reference's screens put
    /// them: the numbers that only this kind of title has (which season, how many episodes), then length, year,
    /// the day it first ran, the age it was passed for, and the kinds of story it is told in.
    /// </summary>
    /// <remarks>
    /// A fact the server did not answer is left out entirely rather than answered with a dash: the reference drew
    /// an empty frame for a missing studio logo because it had a logo frame to draw, while a screen of this
    /// front end's own showing "Studio: —" would be a screen asking the viewer to wonder about it.
    /// </remarks>
    private void FillDetailsFacts(MediaItemDetails details, TitleViewModel title)
    {
      DetailsFacts.Clear();
      bool isSeason = MediaCatalog.IsSeason(title.ReadThrough);

      if (isSeason)
      {
        if (!string.IsNullOrWhiteSpace(details.SeriesName))
          AddFact("Series", details.SeriesName!.Trim());
        if (details.Episodes.HasValue)
          AddFact("Episodes", details.Episodes.Value + (details.Episodes.Value == 1 ? " episode" : " episodes"));
      }
      else
      {
        if (details.Season.HasValue)
          AddFact("Season", details.Season.Value.ToString(CultureInfo.InvariantCulture));
        if (details.Episode.HasValue)
          AddFact("Episode", details.Episode.Value.ToString(CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(details.EpisodeName))
          AddFact("Episode name", details.EpisodeName!.Trim());
      }

      int? minutes = details.RuntimeMinutes ?? title.Title.RuntimeMinutes;
      if (minutes.HasValue && minutes.Value > 0)
      {
        int hours = minutes.Value / 60;
        string length = hours > 0
            ? hours + " h " + (minutes.Value % 60) + " min"
            : minutes.Value + " min";
        AddFact(isSeason || IsMusic ? "Length" : "Runtime", length);
      }

      if (details.Year.HasValue)
        AddFact("Year", details.Year.Value.ToString(CultureInfo.InvariantCulture));
      if (details.FirstAired.HasValue && !isSeason)
        AddFact(IsMusic ? "Released" : "First aired",
            details.FirstAired.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
      if (!string.IsNullOrWhiteSpace(details.Certification) && AgeLogosShown)
        AddFact("Age", details.Certification!.Trim());
      if (details.Genres != null && details.Genres.Count > 0)
      {
        List<string> kinds = new List<string>();
        foreach (string genre in details.Genres)
          if (!string.IsNullOrWhiteSpace(genre))
            kinds.Add(genre.Trim());
        if (kinds.Count > 0)
          AddFact("Genre", string.Join(", ", kinds));
      }
      if (!string.IsNullOrWhiteSpace(details.Collection))
        AddFact("Collection", details.Collection!.Trim());
      if (details.PlayCount.HasValue && details.PlayCount.Value > 0)
        AddFact("Played", details.PlayCount.Value
            + (details.PlayCount.Value == 1 ? " time" : " times"));
    }

    /// <summary>One fact on the panel's list, which is only added when it has something to say.</summary>
    private void AddFact(string label, string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return;
      DetailsFacts.Add(new MediaBrowserFact(label, value.Trim()));
    }

    /// <summary>
    /// The picture of the title the details panel is now about.
    /// </summary>    /// <remarks>
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
      // The panel's picture is the poster, whatever picture the shelf wears: the reference bound Poster into that
      // box and nothing else (MediaItemListMovieDetails.inc:33-36, ConverterParameter=Poster;294;412), and a wide
      // banner or a thumbnail hung in a portrait frame is cut to a slice.  A season has no poster blob of its own -
      // nothing ever wrote one for a shelf of episodes - so its ask travels with the season's number, which is what
      // the shelf's own poster picture already does.
      Artwork? artwork = TileArtworkFor(GridViewType.Poster) ?? _artwork;
      if (artwork == null)
        return;

      DetailsArtwork = null;
      try
      {
        byte[]? picture = await artwork.LoadAsync(title.Title.Id);
        if (picture != null)
        {
          // Read away from the thread that draws, for the same reason the backdrop is: the panel is looked at
          // while it is being filled, and a poster the binding has to decode is a frame the viewer waits for.
          byte[] toRead = picture;
          if (!ArtworkConverter.Instance.Remembers(toRead, null))
            await Task.Run(() => ArtworkConverter.Instance.Warm(toRead, null));
        }

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
    /// Puts the shelf's current picture on one row, asking the server for it when it is not already remembered.
    /// </summary>
    /// <param name="row">A row the screen has just laid out.</param>
    /// <remarks>
    /// <para>
    /// Asked for per row by the screen rather than looped over by the shelf, because only the screen knows which
    /// rows it drew: a shelf of eleven hundred films holds eleven hundred rows in memory and shows a dozen on the
    /// television, and a poster fetched for a row nobody can see is work nobody benefits from. The reference hangs
    /// its pictures the same way round - <c>FanArtImageSource</c> is composed into a container as that container is
    /// realised, and the texture is allocated asynchronously afterwards - and whether a big library scrolls or
    /// stalls comes down to exactly this.
    /// </para>
    /// <para>
    /// Nothing is cancelled when a row scrolls away, because the answer is what the picture caches are for: a
    /// request already on its way is joined rather than repeated, and walking back over a shelf asks for nothing.
    /// A row that already wears a picture from the source the shelf is using now is answered on the spot, which is
    /// what keeps the screen's repeated passes over its visible rows free.
    /// </para>
    /// </remarks>
    public void PaintRow(TitleViewModel row)
    {
      Artwork? artwork = ArtworkForLayout;
      if (artwork == null || ReferenceEquals(row.PaintedFrom, artwork))
        return;

      row.PaintedFrom = artwork;
      _ = PutArtworkOnAsync(artwork, row, Volatile.Read(ref _paintGeneration));
    }

    /// <summary>
    /// Says that the rows of this shelf now wear another kind of picture, so that answers still on their way are
    /// thrown away and the screen paints the rows it has drawn again.
    /// </summary>
    private void PictureSourceChanged()
    {
      Interlocked.Increment(ref _paintGeneration);

      // Every row's answer is now an answer about the wrong kind of picture, so none of them counts as painted.
      foreach (TitleViewModel row in Titles)
        row.PaintedFrom = null;
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

        // The picture is decoded here, on the errand's thread, and not by the binding when the row asks for it.
        // That the difference is felt by the viewer rather than only measured: a wall walked over a shelf pulls
        // a new row of posters every few centimetres, and a JPEG that is decoded between the frame the viewer
        // waited for and the frame after it is experienced as the wall catching on every step.  Every media tile
        // of the skin asks its picture at tile size (templates.xaml, ConverterParameter='tile' on all six), so
        // that is the size the picture is readied at, and the binding afterwards costs a lookup.
        if (generation == Volatile.Read(ref _paintGeneration))
        {
          if (picture != null && picture.Length > 0)
            await Task.Run(delegate { ArtworkConverter.Instance.Warm(picture, ArtworkConverter.TILE_PARAMETER); });

          row.ShowArtwork(picture);
        }
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

      // A season is not an episode and has no address inside one, so the "S03E00" the branch below builds would be
      // an invented episode zero. What the reference's season panel answered with was the series the season
      // belongs to and how many episodes the library counted inside it (MediaItemCoverSeasonDetails.inc:37-75),
      // and the row's own name already says which season this is.
      if (MediaCatalog.IsSeason(title.ReadThrough))
      {
        if (!string.IsNullOrWhiteSpace(details.SeriesName))
          parts.Add(details.SeriesName!.Trim());
        if (details.Episodes.HasValue)
          parts.Add(details.Episodes.Value + (details.Episodes.Value == 1 ? " episode" : " episodes"));
      }
      else if (details.Season.HasValue || details.Episode.HasValue)
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

      // The number the panel is lit with, said in words too - the same answer the stars are lit from, so that the
      // sentence beside the plot and the stars above it can never say two different things.
      double? rated = ToldRating(details);
      if (rated.HasValue)
        parts.Add("rated " + rated.Value.ToString("0.#", CultureInfo.InvariantCulture) + "/10");

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

      // A read from the top is a new shelf, whatever made it happen - the tile that was pressed, a word typed into
      // the find box, the watched key. Every run that was finishing an older shelf notices at its next pause.
      if (offset == 0)
        Interlocked.Increment(ref _generation);

      IsBusy = true;
      try
      {
        // Nothing asks for newest-first here any more: that order came with the "Latest media" tile alone. The
        // opening question is a screenful and every question after it is as much as the server will hand over, so
        // that a shelf of eleven hundred films costs two questions rather than twenty.
        MediaTitles answer = await _client.GetTitlesAsync(AspectName, SortOrder, false,
            offset == 0 ? OPENING_PAGE : REST_IN_ONE_ASK, offset,
            string.IsNullOrEmpty(ActiveSearch) ? null : ActiveSearch,
            string.IsNullOrEmpty(ActiveSeen) ? null : ActiveSeen, Inside, GroupBy, GroupValue, SeasonFilter,
            cancellation.Token);

        if (offset == 0)
          Titles.Clear();

        foreach (MediaTitle title in answer.Titles ?? Array.Empty<MediaTitle>())
        {
          TitleViewModel row = new TitleViewModel(title, DrillsIn, _client, AspectName)
          {
            WatchFlagsShown = WatchFlagsShown
          };

          // A season the library only counts has its answers somewhere else - the pictures, the details and the
          // shelf inside it all belong to the series it was counted from, under its number. The rows are painted and
          // asked about by their own id, so that is where the detour has to be remembered.
          if (title.ParentId.HasValue)
            _asks[title.Id] = (title.ParentId.Value, title.Season);

          Titles.Add(row);
        }

        // Whether the shelf has anything left, settled before the selection is placed: a walk landing on the
        // first of these rows is the viewer saying they are near the end, and the answer to "is there more" has
        // to be the fresh one by then. The ceiling is applied here rather than in the reading itself so that the
        // foot of the shelf, the status line and the walk all agree with one another - and so that a shelf which
        // stopped early can say so below instead of scrolling quietly forever.
        HasMore = answer.Remaining > 0 && Titles.Count < MAX_ROWS;

        Selected = Selected ?? (Titles.Count > 0 ? Titles[0] : null);
        // The shelf's foot counts what the server counted, not merely what has been read so far - the same
        // answer the status line's "12 of 244" is built from, so the two can never disagree.
        TotalNumber = answer.Total;
        RaiseShelfCounters();

        // The pictures come after the names rather than before them, and only for the rows the screen drew. A
        // viewer reads a shelf by its titles, and a list that waited for sixty downloads before drawing its first
        // row would look like a front end that had stopped. The screen asks for a row's picture as it lays the row
        // out (PaintRow), which is how the reference does it too - the picture is composed into the container when
        // the container is realised - and it is the reason a shelf of eleven hundred films costs no more bandwidth
        // than a shelf of eleven.
        //
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
        // A shelf that stopped short of the server's count says so, rather than letting "5000 of 41 000 films"
        // stand there as though the list were finished. What stopped it is the useful part: a viewer with a shelf
        // that tall is looking for the search box or a filter, and both are already on this screen.
        if (answer.Remaining > 0 && Titles.Count >= MAX_ROWS)
          Status = "The first " + MAX_ROWS + " of " + answer.Total + " " + seeing + about
              + ". Find or filter the shelf to narrow it down.";

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

          // Whatever this answer said, the shelf is not finished until the server says it is. Started here rather
          // than in the successful branch because a shelf with rows on it and a question that went wrong is still a
          // shelf somebody is going on reading - and because an answer that brought nothing new stops the run
          // below rather than starting an endless question.
          if (HasMore)
            _ = ReadTheRestAsync(Volatile.Read(ref _generation));
        }
        cancellation.Dispose();
      }
    }
  }

  /// <summary>
  /// One row of the library screen's menu, in the shape the reference's media menu had: the command's words, the
  /// skin's own round picture beside them, and a mark on the row that says what the screen is already doing.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference built these at runtime too - <c>MenuModel.MenuItems</c> of <c>ListItem{Label, Command,
  /// Selected}</c>, filled by the media plugin's workflow actions - and threw the list away when the menu closed,
  /// which is why its labels were always the truth rather than a control that had been forgotten. This class is
  /// that record: a row is written when the menu opens and is not kept afterwards.
  /// </para>
  /// <para>
  /// <see cref="Icon"/> is a name in the worn skin, not a path or a bitmap: the skin is what decides whether a
  /// command has a picture, and a screen that drew its own pictures would be a screen that changes when a
  /// different face is put on it. An empty name is drawn as no picture at all.
  /// </para>
  /// </remarks>
  public sealed class MediaBrowserMenuEntry
  {
    public MediaBrowserMenuEntry(string label, string icon, System.Windows.Input.ICommand command, bool isCurrent)
    {
      Label = label;
      Icon = icon;
      Command = command;
      IsCurrent = isCurrent;
    }

    /// <summary>What the row says, which includes what the screen already answers where the command is a switch.</summary>
    public string Label { get; }

    /// <summary>The skin's name for this command's icon, or "" when the command has none.</summary>
    public string Icon { get; }

    /// <summary>The command the row presses.</summary>
    public System.Windows.Input.ICommand Command { get; }

    /// <summary>
    /// Whether this row is what the screen is doing now - the grid in a list of layouts, the year in a list of
    /// orders. The reference marked the same thing with <c>ListItem.Selected</c> and its styles drew a bright band
    /// behind the row that carried it.
    /// </summary>
    public bool IsCurrent { get; }
  }

  /// <summary>
  /// One line of what is known about a title, said as a word and an answer: "Year" over "2009", "Genre" over
  /// "Krimi, Drama".
  /// </summary>
  /// <remarks>
  /// The reference's details panels never printed sentences about a title; they printed these pairs, grouped into
  /// blocks (<c>MediaItemListDetails.inc:109</c> and its cover sister), and a pair whose answer the library does
  /// not know was left out rather than answered with "unknown". This is the same pair, held long enough to be
  /// laid out - the reading code fills a fresh set for every title, so a pair never outlives the title it speaks
  /// about.
  /// </remarks>
  public sealed class MediaBrowserFact
  {
    public MediaBrowserFact(string label, string value)
    {
      Label = label;
      Value = value;
    }

    /// <summary>The word the reference printed under the answer, spelled as it spelled it.</summary>
    public string Label { get; }

    /// <summary>The answer itself, which is never empty - an unknown was left out instead.</summary>
    public string Value { get; }
  }
}
