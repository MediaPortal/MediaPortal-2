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

using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Caching;
using MediaPortal.Frontend.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// Which wall of people a screen shows.  Three walls rather than one, because the three are read from three
  /// shelves and remembered apart: the people who acted in the films, the people who acted in the series, and the
  /// people who made the records.  A wall of "all people" would be one answer to a question nobody asks - a viewer
  /// standing in front of their films wants to know who played in <em>them</em>.
  /// </summary>
  public enum PeopleShelf
  {
    FilmActors,
    SeriesActors,
    Artists
  }

  /// <summary>
  /// One person on the wall: the id the portrait, the life and that person's own works are filed under, the name
  /// the tile says, and the two dates the library was told about them.
  /// </summary>
  /// <remarks>
  /// Deliberately small.  The wall is read by its thousandth row - this household's films alone answer 7543 names -
  /// so a life of up to ten thousand characters is not something every row carries: that is asked for by id, for the
  /// one name the caret stands on.  A face is not waited for either; it arrives on a tile that already stands.
  /// </remarks>
  public sealed partial class PersonTile : ObservableObject
  {
    public PersonTile(MediaPerson person)
    {
      Id = person.Id;
      Name = person.Name ?? "";
      Occupation = person.Occupation ?? "";
      BornDate = person.BornDate;
      DeathDate = person.DeathDate;
    }

    /// <summary>The person's own media item id, which is what their works and their portrait are filed under.</summary>
    public Guid Id { get; }

    /// <summary>The name as the library spells it, which is what the tile says.</summary>
    public string Name { get; }

    /// <summary>What the library says this person does: <c>ACTOR</c>, <c>ARTIST</c>, ...</summary>
    public string Occupation { get; }

    /// <summary>When they were born, or null when the library was never told.</summary>
    public DateTime? BornDate { get; }

    /// <summary>When they died, or null while the library knows no death.</summary>
    public DateTime? DeathDate { get; }

    /// <summary>
    /// The plate this person stands under when the wall is grouped by first letter: the first letter of the name,
    /// written big.  A name the library wrote beginning with anything but a letter stands under the name's own
    /// first character rather than being hidden away, because a person the screen cannot place is still a person.
    /// </summary>
    public string Letter
    {
      get
      {
        string trimmed = Name.TrimStart();
        return trimmed.Length == 0 ? "?" : trimmed.Substring(0, 1).ToUpperInvariant();
      }
    }

    /// <summary>The birth year for the row's second word, or nothing when nobody was ever asked.</summary>
    public string BornWord
    {
      get { return BornDate == null ? "" : BornDate.Value.Year.ToString(CultureInfo.InvariantCulture); }
    }

    /// <summary>The plate a name nobody ever gave a year stands under when the wall is plated by decade.</summary>
    public const string NO_YEAR_PLATE = "No year given";

    /// <summary>
    /// The plate this person stands under when the wall is grouped by the decade they were born in: "1980s", or the
    /// standing word for a name the library never gave a year.  A name without a year is not left off the wall - the
    /// decade is how the wall is plated, not who is allowed on it - and this household's films are the honest case:
    /// two of its 7543 actors hold a year, so that wall plates two names under a decade and everyone else under one
    /// quiet plate, which is what the library knows rather than a screen giving up.
    /// </summary>
    public string DecadeLabel
    {
      get
      {
        if (BornDate == null)
          return NO_YEAR_PLATE;
        return (BornDate.Value.Year / 10 * 10).ToString(CultureInfo.InvariantCulture) + "s";
      }
    }

    /// <summary>
    /// The person's portrait, put on the tile after the tile was made - the same two states the shelf's people
    /// cards have, because a face is not there when a wall of three hundred names is built and a tile that waited
    /// for one would hold the wall up.
    /// </summary>
    /// <remarks>
    /// Null does not mean "has no face": it means "no face has arrived for this tile", which covers a library that
    /// keeps none, a question still on the way and a name nobody ever drew.  The tile draws the skin's standing
    /// figure in all three, as the reference's <c>Person.png</c> fallback did.
    /// </remarks>
    [ObservableProperty]
    private byte[]? _portrait;

    /// <summary>Whether the tile is drawing a face rather than the standing figure.</summary>
    public bool ShowsPortrait => Portrait != null;

    partial void OnPortraitChanged(byte[]? value) => OnPropertyChanged(nameof(ShowsPortrait));

    /// <summary>
    /// The picture holder this tile's face was asked of, which is the shelf's mark for "this tile has been asked"
    /// (<c>TitleViewModel.PaintedFrom</c>).  A library without a portrait is an ordinary library, and a wall whose
    /// caret walks over two hundred names without one would otherwise ask two hundred questions on its way back; a
    /// name that was asked and answered with nothing is not asked again.  Cleared when an errand goes wrong or
    /// lands on a wall that has been replaced, so that the wall standing here now is never left without a face.
    /// </summary>
    internal Artwork? PaintedFrom
    {
      get;
      set;
    }
  }

  /// <summary>
  /// A wall of people: the names one shelf was made by, walked like a media shelf is walked.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The wall answers the same questions a shelf of titles answers - which shape it wears, what it is read by, what
  /// it stands under and which end it starts at - and it wears the same chrome to be asked them in: the rail of
  /// <see cref="Views.ShelfRail"/>, the plates of <see cref="Views.ShelfFrameTemplate"/>, the fade of
  /// <see cref="Views.BandEndFade"/>.  What it does not answer is the picture question: a person has one picture,
  /// their portrait, so there is nothing to choose between, exactly as an episode shelf has nothing to choose.
  /// </para>
  /// <para>
  /// <b>Why this is its own view model</b> rather than a <see cref="MediaBrowserViewModel"/> wearing different
  /// words: the shelf's model is six thousand lines of details, fanart, watched reports, trailers and title
  /// actions, none of which a wall of names can be asked about, and its menu is built out of the categories a
  /// shelf of titles counts.  What the two screens share is the chrome, and that lives in its own files now.
  /// </para>
  /// <para>
  /// The people are the server's answer over the <em>relationships</em>, which means the wall holds the people who
  /// stand in this shelf at least once - a name nobody ever cast is not on the wall, and cannot be, because the
  /// only proof a person has of belonging here is a work.
  /// </para>
  /// </remarks>
  public partial class PeopleViewModel : ObservableObject, Views.IMenuRailOwner
  {
    /// <summary>How many names the opening question asks for, which is the number a wall shows before it reads on.</summary>
    private const int OPENING_PAGE = 300;

    /// <summary>
    /// How many names a question after the first asks for.  Measured on this household's films the whole wall of
    /// 7543 actors comes back in well under a second, so reading on is worth one big question rather than twenty
    /// small ones.
    /// </summary>
    private const int REST_IN_ONE_ASK = 2000;

    /// <summary>The two shapes this wall wears.  Cover, stacked and carousel are a shelf's shapes, not a wall's.</summary>
    public const string LAYOUT_LIST = "List";

    public const string LAYOUT_GRID = "Grid";

    /// <summary>What the letters page is called in the rail, and what a wall grouped by letter stands under.</summary>
    private const string GROUP_LETTER = "letter";

    /// <summary>What a wall grouped by the decade of birth stands under.</summary>
    private const string GROUP_BORN = "born";

    private enum MenuPage
    {
      Commands,
      View,
      Sort,
      Group,
      Letters
    }

    private readonly ServerApiClient? _client;
    private readonly PictureCache? _pictures;
    private readonly ScreenViewSettings _screens;
    private readonly Dictionary<string, int> _namesPerLetter = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The room behind the wall: the picture of whoever the caret stands on, worn by the same law the shelf of
    /// titles wears it by, and speaking the very same answers to the markup.
    /// </summary>
    private readonly FanartBackdrop _backdrop;

    private MenuPage _menuPage;
    private long _reading;
    private int _offset;
    private bool _atEnd;

    /// <summary>Counts the questions asked for the name the caret stands on, so a late answer is known as late.</summary>
    private long _detailsAsked;

    /// <summary>
    /// Opens a wall of people.
    /// </summary>
    /// <param name="client">The server the names are asked of, or null for a screen with nothing to read.</param>
    /// <param name="shelf">Which of the three walls this is.</param>
    /// <param name="pictures">
    /// This machine's shelf for the library's pictures, which the people's faces are asked through before the server
    /// is asked at all.  Null is a run that keeps pictures in memory only - the same answer the shelf of titles
    /// gives its own holders.
    /// </param>
    public PeopleViewModel(ServerApiClient? client, PeopleShelf shelf, PictureCache? pictures = null)
    {
      _client = client;
      _pictures = pictures;
      Shelf = shelf;
      _screens = ScreenViewSettings.Load(null, out _);
      _backdrop = new FanartBackdrop(PersonBackdrops());

      // The room's picture changes with the names the markup is bound to, and those are the shelf's own names.
      _backdrop.PropertyChanged += delegate
      {
        OnPropertyChanged(nameof(FanartOverlay));
        OnPropertyChanged(nameof(FanartBlurRadius));
        OnPropertyChanged(nameof(BackgroundArtwork));
        OnPropertyChanged(nameof(HasBackgroundArtwork));
      };

      RestoreRememberedReading();
      BuildMediaMenu();
    }

    /// <summary>The room's picture and the skin's three dials, which is what the fanart layer is bound to.</summary>
    public bool FanartShown { get { return _backdrop.FanartShown; } }

    /// <summary>How far the worn skin dims the room's picture.</summary>
    public double FanartOverlay { get { return _backdrop.FanartOverlay; } }

    /// <summary>How far the room's picture is thrown out of focus.</summary>
    public double FanartBlurRadius { get { return _backdrop.FanartBlurRadius; } }

    /// <summary>The bytes of the stood name's picture, or null while the room wears its plain ground.</summary>
    public byte[]? BackgroundArtwork { get { return _backdrop.BackgroundArtwork; } }

    /// <summary>Whether there is a room-picture to draw.</summary>
    public bool HasBackgroundArtwork { get { return _backdrop.HasBackgroundArtwork; } }

    /// <summary>Which of the three walls this screen shows.</summary>
    public PeopleShelf Shelf { get; }

    /// <summary>
    /// The wall a shortcut on the arm of Nereus' cross names, or nothing when the shortcut is for something else.
    /// </summary>
    /// <param name="screenKey">
    /// The key the skin's menu says for the shortcut, which is MP2's own words: <c>MediaMenu/actor</c> and
    /// <c>MediaMenu/artist</c>.
    /// </param>
    /// <param name="section">
    /// The shelf the viewer was standing on when they pressed, which decides which half of the word "actors" they
    /// meant: the shortcut is one tile in the skin, and the people who play in the films are not the people who
    /// play in the series. A viewer standing on Music who presses for actors is asking for the musicians, because
    /// that is the only kind of person a record holds.
    /// </param>
    public static bool IsWallShortcut(string? screenKey, HomeSection? section, out PeopleShelf shelf)
    {
      shelf = PeopleShelf.FilmActors;
      if (string.IsNullOrEmpty(screenKey))
        return false;

      // The word after the slash is the kind of person; MP2's own menu keys are the one place this is written down.
      int slash = screenKey!.LastIndexOf('/');
      string kind = (slash < 0 ? screenKey : screenKey.Substring(slash + 1)).Trim();

      if (string.Equals(kind, "artist", StringComparison.OrdinalIgnoreCase))
      {
        shelf = PeopleShelf.Artists;
        return true;
      }

      if (!string.Equals(kind, "actor", StringComparison.OrdinalIgnoreCase))
        return false;

      shelf = ShelfForAspect(section?.Aspect?.Name);
      return true;
    }

    /// <summary>
    /// The wall a shelf answers with: the people who play in its films, the people who play in its series, or the
    /// people who make its records.  A screen that has only the aspect's name to go on - the render probe, for
    /// instance, is handed <c>--aspect</c> and nothing else - asks this.
    /// </summary>
    public static PeopleShelf ShelfForAspect(string? aspectName)
    {
      if (string.Equals(aspectName, "SeriesItem", StringComparison.OrdinalIgnoreCase))
        return PeopleShelf.SeriesActors;
      if (string.Equals(aspectName, "AlbumItem", StringComparison.OrdinalIgnoreCase)
          || string.Equals(aspectName, "AudioItem", StringComparison.OrdinalIgnoreCase))
        return PeopleShelf.Artists;
      return PeopleShelf.FilmActors;
    }

    /// <summary>The shelf the people are asked from, in the server's name for it.</summary>
    public string AspectName
    {
      get
      {
        switch (Shelf)
        {
          case PeopleShelf.SeriesActors:
            return "SeriesItem";
          case PeopleShelf.Artists:
            return "AlbumItem";
          default:
            return "MovieItem";
        }
      }
    }

    /// <summary>
    /// The half of the memory's key this wall stands on, which is the same word the shelf of titles uses for the
    /// filter of the same name: a wall of film actors is remembered as <c>MovieItem#actors</c>, the way the shelf
    /// remembers its films cut to one actor as <c>MovieItem#actors</c> too - the same question, the same answer.
    /// </summary>
    public string FilterClass
    {
      get { return Shelf == PeopleShelf.Artists ? "artists" : "actors"; }
    }

    /// <summary>The part the people played, in the server's words, for the people this wall is made of.</summary>
    public IReadOnlyList<string> Roles
    {
      get
      {
        // A record's artist is its album artist, not the artist of its tracks: a wall of 376 singers would be a
        // wall of every song, while the records themselves are made by 37 people.
        return new[] { Shelf == PeopleShelf.Artists ? "albumartist" : "actor" };
      }
    }

    /// <summary>What the screen says it is, above the wall.</summary>
    public string WallLabel
    {
      get
      {
        switch (Shelf)
        {
          case PeopleShelf.SeriesActors:
            return "Series actors";
          case PeopleShelf.Artists:
            return "Artists";
          default:
            return "Film actors";
        }
      }
    }

    /// <summary>
    /// The rows of the wall: <see cref="PersonTile"/>s, with a <see cref="GroupHeaderRow"/> plate in front of every
    /// letter the moment the wall stands under its letters.
    /// </summary>
    public ObservableCollection<object> Rows { get; } = new ObservableCollection<object>();

    /// <summary>How many people this wall holds, which the server counts rather than guesses.</summary>
    [ObservableProperty]
    private int _total;

    /// <summary>What the wall has to say for itself above the rows: its name, and how many names it holds.</summary>
    public string Headline
    {
      get
      {
        if (Total == 0)
          return WallLabel;
        return WallLabel + " · " + Total.ToString("N0", CultureInfo.CurrentCulture);
      }
    }

    /// <summary>A word from the server that the wall cannot draw, or nothing.</summary>
    [ObservableProperty]
    private string _notice = "";

    /// <summary>Whether a question to the server is on its way.</summary>
    [ObservableProperty]
    private bool _busy;

    /// <summary>Which of the two shapes the wall wears.</summary>
    [ObservableProperty]
    private string _layout = LAYOUT_GRID;

    /// <summary>What the wall is read by: <c>name</c>, or <c>born</c> for the year a person was born.</summary>
    [ObservableProperty]
    private string _sort = "name";

    /// <summary>Whether the reading starts at its far end.</summary>
    [ObservableProperty]
    private bool _sortDescending;

    /// <summary>What the wall stands under: nothing while it is one wall, or <c>letter</c> for a plate per letter.</summary>
    [ObservableProperty]
    private string _grouping = "";

    /// <summary>One letter the names are narrowed to, or nothing while every name is wanted.</summary>
    [ObservableProperty]
    private string _letter = "";

    /// <summary>The words a viewer typed to look for a name.</summary>
    [ObservableProperty]
    private string _search = "";

    /// <summary>
    /// The words standing in the field on the screen, which is not the same thing as <see cref="Search"/>: the field
    /// holds whatever a hand has typed so far, while <c>Search</c> names the question the wall on screen was really
    /// read with.  A wall that asked the server again with every letter would ask seven questions for a
    /// seven-letter name and jump under the viewer's hand, so the asking waits for the pause at the end of typing.
    /// </summary>
    [ObservableProperty]
    private string _searchText = "";

    /// <summary>
    /// Whether the viewer asked to type.  The field stands while this holds or while a search stands on the wall -
    /// the shelf's law (MediaBrowserViewModel.ShowsSearch), and the reason the rail's "Search" row, the Find key and
    /// the arrow off the head of the wall all reach for <see cref="OpenSearch"/> rather than for a caret: putting
    /// the field on the screen and asking for its caret are one wish, and either half alone is the nothing-that-
    /// happens a dead button says.
    /// </summary>
    [ObservableProperty]
    private bool _searchOpen;

    /// <summary>The person the caret stands on, for the details area to speak of.</summary>
    [ObservableProperty]
    private PersonTile? _selected;

    /// <summary>
    /// Whether the column of words at the right hand end of the screen stands.  On from the first name, exactly as a
    /// shelf of titles opens: what is known about the standing name belongs beside the wall rather than behind a
    /// press, and only the viewer's own answer - the "Details" row on the screen's menu - takes it away, and for
    /// that visit only.
    /// </summary>
    [ObservableProperty]
    private bool _showDetails = true;

    /// <summary>What the rail's row says, in the shelf's own two words.</summary>
    public string DetailsLabel
    {
      get { return ShowDetails ? "Details: on" : "Details: off"; }
    }

    /// <summary>Turns the column away or lets it back: what the rail's row presses.</summary>
    [RelayCommand]
    public void ToggleDetails()
    {
      ShowDetails = !ShowDetails;

      // A row of this rail is written when its page is built and is not kept afterwards, so the row carrying the
      // answer has to be written again - otherwise it goes on saying "on" to a column that just went away.
      BuildMediaMenu();
    }

    /// <summary>Whether the column is up at all, which is the viewer's answer and nothing else.</summary>
    public bool ShowsSideDetails
    {
      get { return ShowDetails; }
    }

    /// <summary>Whether the column has somebody to speak of, which is when its words may be painted.</summary>
    public bool ShowsDetailsWords
    {
      get { return ShowDetails && DetailsName.Length > 0; }
    }

    /// <summary>The name the column speaks of - a word the wall's row carries before the server is ever asked.</summary>
    [ObservableProperty]
    private string _detailsName = "";

    /// <summary>The years as the reference wrote them under a person's name: "1904 - 1966", "1962", or nothing.</summary>
    [ObservableProperty]
    private string _detailsYears = "";

    /// <summary>Where the person comes from, when the library was ever told.  An offline one never was.</summary>
    [ObservableProperty]
    private string _detailsOrigin = "";

    /// <summary>
    /// The life as the library wrote it, whole.  Up to ten thousand characters, because that is how long the library
    /// lets one be - this household's longest runs eight thousand six hundred and thirty five - so the column that
    /// shows it is a column that reads, not a line that cuts.
    /// </summary>
    [ObservableProperty]
    private string _detailsBiography = "";

    /// <summary>The face of the standing name, asked under that person's own id.</summary>
    [ObservableProperty]
    private byte[]? _detailsPortrait;

    /// <summary>Whether the years have anything to say.</summary>
    public bool ShowsYears => DetailsYears.Length > 0;

    /// <summary>Whether the library named a place, which is a section that stays away for every offline person.</summary>
    public bool ShowsOrigin => DetailsOrigin.Length > 0;

    /// <summary>Whether there is a life to read - two of this household's 7757 actors have one, all of them musicians.</summary>
    public bool ShowsBiography => DetailsBiography.Length > 0;

    /// <summary>Whether the column draws a face rather than the skin's standing figure.</summary>
    public bool ShowsPortrait => DetailsPortrait != null;

    partial void OnDetailsNameChanged(string value) => OnPropertyChanged(nameof(ShowsDetailsWords));
    partial void OnDetailsYearsChanged(string value) => OnPropertyChanged(nameof(ShowsYears));
    partial void OnDetailsOriginChanged(string value) => OnPropertyChanged(nameof(ShowsOrigin));
    partial void OnDetailsBiographyChanged(string value) => OnPropertyChanged(nameof(ShowsBiography));
    partial void OnDetailsPortraitChanged(byte[]? value) => OnPropertyChanged(nameof(ShowsPortrait));

    /// <summary>
    /// The caret stood on another name, which is the one moment the column changes its subject: nothing here is
    /// opened, pressed or clicked - a media screen says what is stood on, all by itself.
    /// </summary>
    partial void OnSelectedChanged(PersonTile? value)
    {
      if (value == null)
      {
        ClearDetails();
        _backdrop.Quiet();
        OnPropertyChanged(nameof(ContentInfo));
        OnPropertyChanged(nameof(ShowsContentInfo));
        return;
      }

      // Two errands, none of them waited for: the words the column is made of, and the picture of the one name the
      // caret actually stands on.  A wall is walked a name at a time, so both arrive after the name is painted
      // rather than hold it back.  The faces of the names around the caret are NOT asked here: the screen asks for
      // those per tile it draws, the way a shelf of titles does, because only the screen knows what the viewer can
      // see.
      _ = ShowDetailsForAsync(value);
      _backdrop.WearFor(value.Id);

      // And the line at the foot changes its subject with the caret, out of what the row already carries.
      OnPropertyChanged(nameof(ContentInfo));
      OnPropertyChanged(nameof(ShowsContentInfo));
    }

    partial void OnShowDetailsChanged(bool value)
    {
      // The screen divides its room by this answer, so the answer has to be heard and not only read.
      OnPropertyChanged(nameof(ShowsSideDetails));

      //  ...and the line at the foot takes over the telling when the column is turned away.
      OnPropertyChanged(nameof(ShowsContentInfo));

      if (!value)
      {
        ClearDetails();
        return;
      }

      if (Selected != null)
        _ = ShowDetailsForAsync(Selected);
    }

    /// <summary>
    /// Which reading of the wall the face errands were sent for.  A wall that was re-read - another letter, another
    /// order, another wall altogether - throws away the answers that are still on their way, because a face that
    /// lands on a name the room no longer shows is a face nobody is looking at.  The shelf's own mark for the same
    /// question (<c>MediaBrowserViewModel._paintGeneration</c>).
    /// </summary>
    private long _paintGeneration;

    /// <summary>
    /// Takes the column's words away: the caret walked off the wall, the wall was re-read, or the viewer turned the
    /// column away.
    /// </summary>
    private void ClearDetails()
    {
      _detailsAsked++;
      DetailsName = "";
      DetailsYears = "";
      DetailsOrigin = "";
      DetailsBiography = "";
      DetailsPortrait = null;
    }

    /// <summary>
    /// Fills the column with what is known about one person: the name and the years the wall's row already carries,
    /// then the life and the place it does not, then the face.
    /// </summary>
    /// <remarks>
    /// The row's own words go up first on purpose.  A wall is walked faster than a server answers, and a column that
    /// filled itself only once an answer arrived would spend a whole walk standing empty beside a name that is
    /// plainly written three feet away from it.
    /// </remarks>
    private async Task ShowDetailsForAsync(PersonTile person)
    {
      long asked = ++_detailsAsked;
      DetailsName = person.Name;
      DetailsYears = YearsOf(person.BornDate, person.DeathDate);
      DetailsOrigin = "";
      DetailsBiography = "";
      DetailsPortrait = null;

      ServerApiClient? client = _client;
      if (client == null || !ShowDetails)
        return;

      // The face travels beside the words and not behind them: a life of eight thousand characters is a long
      // answer, a face is a short one, and a column that showed its face only once the life had arrived kept a
      // nameless plate in front of a viewer who had already read the name next to it.
      Task face_errand = ReadFaceAsync(person.Id, asked);

      try
      {
        MediaPersonAnswer answer = await client.GetPersonAsync(person.Id).ConfigureAwait(true);

        // The caret moved on while this was travelling, and the column speaks of another person now.
        if (asked != _detailsAsked)
          return;

        MediaPersonDetail? life = answer.Person;
        if (life == null)
          return;

        // The name and the two dates came over the wall already, and the record is believed for them anyway: a wall
        // and a record are one table read two ways, and a difference between them is a library rebuilt underneath
        // the screen - the record is the one that still exists.
        DetailsName = string.IsNullOrWhiteSpace(life.Name) ? person.Name : life.Name;
        DetailsYears = YearsOf(life.BornDate ?? person.BornDate, life.DeathDate ?? person.DeathDate);
        DetailsOrigin = life.Place;
        DetailsBiography = life.Life;
      }
      catch (Exception)
      {
        // A record that will not answer leaves the column with the row's own words on it.  The wall keeps walking;
        // a server that cannot tell one life is no reason to stop naming people.
      }
      finally
      {
        // Waited for on every road out of here - including the two that returned early - so that the picture is
        // never a task nobody owns.  It answers for itself either way; this only keeps the column's errands told.
        await face_errand.ConfigureAwait(true);
      }
    }

    /// <summary>Asks the one face the column is drawing, and only while the column still speaks of that person.</summary>
    private async Task ReadFaceAsync(Guid person, long asked)
    {
      Artwork? portraits = PersonPortraits();
      if (portraits == null)
        return;

      try
      {
        byte[]? face = await portraits.LoadAsync(person).ConfigureAwait(true);
        if (face == null || asked != _detailsAsked)
          return;

        // Decoded away from the thread that draws, the way every other picture of this front end is: a face the
        // binding has to decode first is a face the viewer sees arrive.
        if (!Views.ArtworkConverter.Instance.Remembers(face, Views.ArtworkConverter.POSTER_PARAMETER))
        {
          await Task.Run(delegate
          {
            Views.ArtworkConverter.Instance.Warm(face, Views.ArtworkConverter.POSTER_PARAMETER);
          }).ConfigureAwait(true);
        }

        if (asked == _detailsAsked)
          DetailsPortrait = face;
      }
      catch (ServerApiException)
      {
        // Nothing filed under this name, which is what almost every film actor of this household has in common.
      }
      catch (Exception)
      {
        // The same answer for anything else: the column keeps the standing figure it was drawing.
      }
    }

    /// <summary>
    /// Tells the wall that a name standing within the viewer's reach wants its face.  Asking the server is the wall's
    /// business, so the drawn tile is handed this instead of a client of its own; the answer arrives on the tile and
    /// is decoded there, off the thread that draws.
    /// </summary>
    /// <remarks>
    /// Asked for per tile by the screen rather than looped over by the wall, because only the screen knows which
    /// tiles it drew: a wall of seven thousand film actors holds seven thousand names and shows a couple of dozen on
    /// the television, and a face fetched for a tile nobody can see is a place in the picture queue taken from one
    /// nobody can.  The queue is what makes the difference the viewer feels - asked one after the other by one walk,
    /// a wall's faces arrive one per round trip and are seen arriving; asked by the tiles that stand in sight, the
    /// four the fetcher keeps on the wire carry them in as a stream, which is how a shelf of titles fills and how a
    /// wall of people is meant to fill.
    /// </remarks>
    public void PaintFace(PersonTile tile)
    {
      Artwork? portraits = PersonPortraits();
      if (portraits == null || ReferenceEquals(tile.PaintedFrom, portraits))
        return;

      tile.PaintedFrom = portraits;
      _ = PutFaceOnAsync(portraits, tile, Volatile.Read(ref _paintGeneration));
    }

    /// <summary>
    /// One tile's face, put on the tile when it arrives - unless the wall it was asked for was replaced underneath
    /// it, in which case this answer belongs to a wall nobody is looking at.
    /// </summary>
    private async Task PutFaceOnAsync(Artwork portraits, PersonTile tile, long generation)
    {
      byte[]? face;
      try
      {
        face = await portraits.LoadAsync(tile.Id).ConfigureAwait(true);
      }
      catch (Exception)
      {
        // Nothing filed under this name, which is what almost every film actor of this household has in common.
        // Left unmarked on purpose: one errand that came back empty is no reason to go without a face for the rest
        // of the evening when the server was only having a bad minute.
        tile.PaintedFrom = null;
        return;
      }

      if (generation != Volatile.Read(ref _paintGeneration))
      {
        // The wall this errand was sent for is not the wall the room wears now.  What MUST not happen is leaving the
        // tile stamped as though it had been painted: the stamp is what keeps a tile from being asked a second
        // time, and the wall that stands here now would then keep wearing the standing figure for ever.
        tile.PaintedFrom = null;
        return;
      }

      if (face == null)
        return;

      // Decoded away from the thread that draws, the way every other picture of this front end is: a JPEG that is
      // decoded between the frame the viewer waited for and the frame after it is experienced as a face popping
      // into a tile that was already standing there.
      byte[] toRead = face;
      if (!Views.ArtworkConverter.Instance.Remembers(toRead, Views.ArtworkConverter.POSTER_PARAMETER))
      {
        await Task.Run(delegate
        {
          Views.ArtworkConverter.Instance.Warm(toRead, Views.ArtworkConverter.POSTER_PARAMETER);
        }).ConfigureAwait(true);
      }

      if (generation == Volatile.Read(ref _paintGeneration))
        tile.Portrait = face;
    }


    private Artwork? _personPortraits;
    private bool _personPortraitsAsked;

    /// <summary>
    /// The holder of the people's faces, made once and kept apart from every other picture of this screen: these
    /// are asked for under a PERSON's id, and a holder shared with titles would hand one person's face to a title
    /// wearing the same number.
    /// </summary>
    /// <remarks>
    /// The kind is the shelf's own word for a person's face at the shelf's own small width, so the people bands of a
    /// title's details and this wall read the same machine's shelf: a face one of them fetched is not fetched twice
    /// by the other.  Asked as fanart of the type <c>Thumbnail</c> rather than as the thumbnail blob, because a
    /// person's picture is never the blob - it is the file the import wrote under the person's own id, which is
    /// where the reference asked a face from too.
    /// </remarks>
    private Artwork? PersonPortraits()
    {
      if (_personPortraitsAsked)
        return _personPortraits;

      _personPortraitsAsked = true;
      ServerApiClient? client = _client;
      if (client == null)
        return null;

      return _personPortraits = PicturesOf(client,
          PictureWidths.Shelf("PersonThumbnail", PictureWidths.SMALL),
          id => client.GetFanArtAsync(id, "Thumbnail", null, PictureWidths.SMALL));
    }

    /// <summary>
    /// The holder of the room's pictures: a person's fanart whole, across the width the screen is painted at.
    /// </summary>
    /// <remarks>
    /// The kind is the person's own and never the <c>"FanArt"</c> a title's backdrop is kept under, for the same
    /// reason the faces above are kept under <c>"PersonThumbnail"</c>: two libraries of pictures that happen to be
    /// asked with the same numbers would otherwise be laid on top of each other in the shelf, and a name would be
    /// answered with some film's wallpaper.  Behind a blur the width matters less than the asking - a household
    /// that imported its pictures at postage-stamp size gets the postage stamp back, since the server's ladder of
    /// sizes never grows a picture, only shrinks it.
    /// </remarks>
    private Artwork? PersonBackdrops()
    {
      ServerApiClient? client = _client;
      if (client == null)
        return null;

      int whole = PictureWidths.PaintedScreenWidth();
      return PicturesOf(client, PictureWidths.Shelf("PersonFanArt", whole),
          id => client.GetFanArtAsync(id, "FanArt", null, whole));
    }

    /// <summary>One holder of one kind of picture, on this machine's shelf when the machine keeps one.</summary>
    private Artwork PicturesOf(ServerApiClient client, string kind, Func<Guid, Task<byte[]?>> ask)
    {
      return _pictures == null ? new Artwork(ask) : _pictures.ArtworkFor(kind, client.BaseUrl, ask);
    }

    /// <summary>
    /// The two dates as one word of years, as the reference wrote them under the name: both years when the library
    /// knows both, one when it knows one, nothing when it knows neither.  Years rather than dates, because what an
    /// import keeps of a birth is a year and the day was never told anybody.
    /// </summary>
    private static string YearsOf(DateTime? born, DateTime? died)
    {
      if (born == null)
        return died == null ? "" : "died " + died.Value.Year.ToString(CultureInfo.InvariantCulture);

      string first = born.Value.Year.ToString(CultureInfo.InvariantCulture);
      return died == null
          ? first
          : first + " - " + died.Value.Year.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Whether the wall is one wall rather than a run of plates.</summary>
    public bool IsGrouped
    {
      get { return !string.IsNullOrEmpty(Grouping); }
    }

    /// <summary>Whether the wall is cut to one letter or searched, which is when "Every name" has a job to do.</summary>
    public bool IsNarrowed
    {
      get { return !string.IsNullOrEmpty(Letter) || !string.IsNullOrEmpty(Search); }
    }

    /// <summary>Whether the wall on screen is being looked through, which is what the mark at the field's hand appears for.</summary>
    public bool HasSearch
    {
      get { return !string.IsNullOrEmpty(Search); }
    }

    /// <summary>Whether the field stands on the screen: opened to type in, or still holding what was typed.</summary>
    public bool ShowsSearch
    {
      get { return SearchOpen || HasSearch; }
    }

    partial void OnSearchOpenChanged(bool value)
    {
      OnPropertyChanged(nameof(ShowsSearch));
    }

    partial void OnSearchChanged(string value)
    {
      OnPropertyChanged(nameof(HasSearch));
      OnPropertyChanged(nameof(ShowsSearch));
      OnPropertyChanged(nameof(IsNarrowed));
    }

    /// <summary>
    /// Puts the field on the screen and asks for the caret to be moved into it - the rail's "Search" row, the Find
    /// key, or the arrow lifted off the head of the wall while the field already stands there.
    /// </summary>
    /// <remarks>
    /// Both halves belong together: the field is only on the screen while a search is open, so asking for its caret
    /// without opening it hands the caret to a control that is not there - and opening it without asking leaves the
    /// viewer looking at a field that nothing of theirs can type into.  The screen owns the field, which is why the
    /// asking goes out as an event rather than this model reaching for a caret of its own.
    /// </remarks>
    public void OpenSearch()
    {
      SearchOpen = true;
      SearchFocusRequested?.Invoke();
    }

    /// <summary>
    /// Asked of the screen when the field has been opened and wants the caret, because the field is the screen's
    /// control and this model has no business reaching for a caret.
    /// </summary>
    public event Action? SearchFocusRequested;

    /// <summary>
    /// Whether a walk over the wall has read every name it has.  A wall that answers zero for the rest of itself
    /// stops asking, which is the only way a screen knows the difference between "nothing arrived yet" and
    /// "there is nothing after this".
    /// </summary>
    public bool AtEnd
    {
      get { return _atEnd; }
    }

    /// <summary>
    /// Reads the wall, from its beginning or on past what stands on it.
    /// </summary>
    /// <param name="fromTheStart">
    /// <c>true</c> to throw the rows away and ask again - the question a press on the rail asks - and <c>false</c>
    /// to put another page behind the rows that are already there.
    /// </param>
    public async Task ReadAsync(bool fromTheStart)
    {
      if (_client == null)
      {
        Notice = "This screen reads its people from a server, and no server is answering.";
        return;
      }

      long asked = ++_reading;

      // Who the column was speaking of, asked of the caret BEFORE its tile is thrown away with the rows.  The id is
      // remembered rather than the tile because a re-read builds every tile anew, and the caret has to go onto the
      // tile that stands here now - the details column follows the caret, and a caret dropped on every re-read would
      // blank the column over a person who is still plainly on the screen.
      Guid stoodFor = Selected?.Id ?? Guid.Empty;
      if (fromTheStart)
      {
        Rows.Clear();

        // Every tile of this wall is a new tile, so the faces still on their way belong to a wall that is gone: the
        // mark goes up, those answers are thrown away when they land, and their tiles are left un-stamped so that
        // the wall standing here now is asked for them again.
        _paintGeneration++;

        _namesPerLetter.Clear();
        _offset = 0;
        _atEnd = false;
        Notice = "";
      }
      else if (_atEnd)
      {
        return;
      }

      int from = _offset;
      int take = from == 0 ? OPENING_PAGE : REST_IN_ONE_ASK;
      Busy = true;
      try
      {
        MediaPeople answer = await _client.GetPeopleAsync(AspectName, Roles,
                string.IsNullOrWhiteSpace(Letter) ? null : Letter,
                string.IsNullOrWhiteSpace(Search) ? null : Search,
                Sort, SortDescending, take, from)
            .ConfigureAwait(true);

        // A newer question overtook this one - the viewer picked another letter while this page was travelling -
        // and its rows are the ones the wall wears now.
        if (asked != _reading)
          return;

        if (!string.IsNullOrWhiteSpace(answer.Error))
        {
          Notice = answer.Error!;
          if (fromTheStart)
            Selected = null;
          return;
        }

        Notice = "";
        Total = answer.Total;
        foreach (MediaPerson person in answer.Rows)
          AddRow(new PersonTile(person));

        int arrived = answer.Rows.Count;
        _offset += arrived;
        _atEnd = arrived == 0 || _offset >= answer.Total;
        OnPropertyChanged(nameof(Headline));

        // The names that just came onto the wall are asked for their faces by the screen, tile by tile, as its panel
        // stands them - which is where a wall knows what a viewer can see.  Nothing is asked for here.

        if (fromTheStart)
        {
          // The caret goes back onto the same person when the wall still holds them - which is every re-read of a
          // question that did not change the answer, and the sorting, grouping and letter of a wall that keeps its
          // names.  Where they are gone (a letter, a word, or a filter that left them out) the caret and with it the
          // column go quiet, because a column going on about a person who is not on the wall is a screen lying
          // about what is stood on.
          Selected = stoodFor == Guid.Empty
              ? null
              : Rows.OfType<PersonTile>().FirstOrDefault(row => row.Id == stoodFor);
        }
      }
      catch (Exception e)
      {
        if (asked == _reading)
        {
          Notice = "The server did not answer the wall: " + e.Message;
          if (fromTheStart)
            Selected = null;
        }
      }
      finally
      {
        if (asked == _reading)
          Busy = false;
      }
    }

    /// <summary>
    /// Puts one person's tile on the wall, with the plate it stands under when the wall is grouped - and counts the
    /// name for its letter, which is the only count a wall of names can honestly make: the server counts works, and
    /// a wall asks for none.
    /// </summary>
    private void AddRow(PersonTile tile)
    {
      // Counted for the alphabet whatever plates the wall: the letters are a question a viewer asks on their own, so
      // a wall plated by decade still has to know how many of its names begin with "K".
      string letter = tile.Letter;
      _namesPerLetter[letter] = _namesPerLetter.TryGetValue(letter, out int soFar) ? soFar + 1 : 1;

      if (IsGrouped)
      {
        string plate = ThePlate(tile);
        int stood = Rows.Count;
        if (stood == 0 || !(Rows[stood - 1] is PersonTile last)
            || !string.Equals(ThePlate(last), plate, StringComparison.OrdinalIgnoreCase))
        {
          Rows.Add(new GroupHeaderRow(plate, 0, false));
        }
      }

      Rows.Add(tile);
    }

    /// <summary>The plate one name stands under, which is the grouping's own word for that name.</summary>
    private string ThePlate(PersonTile tile)
    {
      return string.Equals(Grouping, GROUP_BORN, StringComparison.OrdinalIgnoreCase)
          ? tile.DecadeLabel
          : tile.Letter;
    }

    /// <summary>
    /// Reads on when a walk over the wall has eaten into the last page of names.  Asked by the screen from its
    /// caret, because the moment to ask is a hand reaching for the end rather than a number of rows drawn.
    /// </summary>
    public async Task KeepReadingAsync()
    {
      if (_atEnd || Busy || Rows.Count == 0)
        return;

      int left = Rows.Count - StandingRow();
      if (left > OPENING_PAGE / 4)
        return;

      await ReadAsync(false);
    }

    /// <summary>
    /// Where the caret stands, so far as this model knows it.  Overridden by nothing; the screen tells the model by
    /// setting <see cref="Selected"/>, which is what a walk over the wall does row by row.
    /// </summary>
    private int StandingRow()
    {
      PersonTile? stood = Selected;
      if (stood == null)
        return Rows.Count - 1;

      for (int at = Rows.Count - 1; at >= 0; at--)
      {
        if (Rows[at] == stood)
          return at;
      }

      return Rows.Count - 1;
    }

    // The three questions a rail asks of the screen it hangs in; see Views.IMenuRailOwner.
    /// <inheritdoc />
    [ObservableProperty]
    private bool _mediaMenuVisible;

    /// <inheritdoc />
    [ObservableProperty]
    private string _mediaMenuHeading = "Menu";

    private string _menuTileLabel = "";
    private string _menuTileDetail = "";

    /// <summary>The line at the screen's foot while the menu is out: the stood plate and what it adds to that.</summary>
    public string MediaMenuFootLine
    {
      get
      {
        if (_menuTileLabel.Length == 0)
          return MediaMenuHeading;
        return _menuTileDetail.Length == 0 ? _menuTileLabel : _menuTileLabel + " · " + _menuTileDetail;
      }
    }

    /// <summary>
    /// The one line this screen says at its foot about the name the caret stands on, in the shelf's own shape: the
    /// name, the years, and what the library says this person is.  Read off the row alone and asked of nobody - a
    /// foot line that had to ask the server would put one request behind every arrow key.
    /// </summary>
    /// <remarks>
    /// A shelf of titles says the same kind of line here when its details column is turned away (the name, the year,
    /// the kinds of thing it is), and for the same reason: the column went away, not the knowledge.  What the wall
    /// does NOT say here is its own name and its number of names - no shelf of titles stands under a caption, and a
    /// wall that needed one to know itself would be a screen explaining itself to a viewer who came to look at
    /// names.  See <see cref="Headline"/>, which the traces still ask.
    /// </remarks>
    public string ContentInfo
    {
      get
      {
        //  A word the server sent that no face could draw outranks a name, since a wall that was not answered has
        //  nothing to say about a name it never got.
        if (Notice.Length > 0)
          return Notice;

        PersonTile? person = Selected;
        if (person == null)
          return string.Empty;

        List<string> parts = new List<string> { person.Name };
        string years = YearsOf(person.BornDate, person.DeathDate);
        if (years.Length > 0)
          parts.Add(years);

        string trade = TradeWord(person.Occupation);
        if (trade.Length > 0)
          parts.Add(trade);

        return string.Join("   ·   ", parts);
      }
    }

    /// <summary>
    /// Whether that line stands.  It is the answer for the one moment in which the screen has nothing else to say
    /// about the stood-on name: the details column is turned away, and the rail's word is not speaking at this very
    /// place at the foot of the room.
    /// </summary>
    public bool ShowsContentInfo
    {
      get { return (Selected != null || Notice.Length > 0) && !ShowDetails && !MediaMenuVisible; }
    }

    partial void OnNoticeChanged(string value)
    {
      OnPropertyChanged(nameof(ContentInfo));
      OnPropertyChanged(nameof(ShowsContentInfo));
    }

    partial void OnMediaMenuVisibleChanged(bool value) => OnPropertyChanged(nameof(ShowsContentInfo));

    /// <summary>
    /// The library's word for what a person is - <c>ACTOR</c>, <c>ARTIST</c> - written as a word a screen can say
    /// out loud rather than as a table's constant.
    /// </summary>
    private static string TradeWord(string? occupation)
    {
      if (string.IsNullOrEmpty(occupation))
        return string.Empty;

      return char.ToUpperInvariant(occupation![0]) + occupation.Substring(1).ToLowerInvariant();
    }

    /// <inheritdoc />
    public void MenuTileStood(string? label, string? detail)
    {
      string came = label ?? string.Empty;
      string added = detail ?? string.Empty;
      if (came == _menuTileLabel && added == _menuTileDetail)
        return;

      _menuTileLabel = came;
      _menuTileDetail = added;
      OnPropertyChanged(nameof(MediaMenuFootLine));
    }

    /// <summary>A page of the rail being replaced took every plate the foot line spoke of with it.</summary>
    partial void OnMediaMenuHeadingChanged(string value)
    {
      _menuTileLabel = "";
      _menuTileDetail = "";
      OnPropertyChanged(nameof(MediaMenuFootLine));
    }

    /// <summary>The rows the rail wears for the page <see cref="_menuPage"/> says it is on.</summary>
    public ObservableCollection<MediaBrowserMenuEntry> MediaMenu { get; } = new ObservableCollection<MediaBrowserMenuEntry>();

    /// <summary>Puts the menu up.</summary>
    [RelayCommand]
    public void ShowMediaMenu()
    {
      _menuPage = MenuPage.Commands;
      BuildMediaMenu();
      MediaMenuVisible = true;
    }

    /// <summary>Takes the menu down again.</summary>
    [RelayCommand]
    public void HideMediaMenu()
    {
      MediaMenuVisible = false;
    }

    /// <summary>Puts another page of the menu up without reopening it, which is what a command's arrow does.</summary>
    private void GoToPage(MenuPage page)
    {
      _menuPage = page;
      BuildMediaMenu();
    }

    private void AddMenuRow(string label, string icon, System.Windows.Input.ICommand command, bool isCurrent = false,
        string detail = "")
    {
      MediaMenu.Add(new MediaBrowserMenuEntry(label, icon, command, isCurrent, detail));
    }

    /// <summary>The rail's rows, for the page the viewer stands on.</summary>
    private void BuildMediaMenu()
    {
      MediaMenu.Clear();
      switch (_menuPage)
      {
        case MenuPage.View:
          MediaMenuHeading = "View";
          AddMenuRow("List", "view_list", new AsyncRelayCommand(() => PickLayoutAsync(LAYOUT_LIST)),
              Layout == LAYOUT_LIST);
          // The square is the shape a person's portrait is drawn in, which is why it is the wall's own shape and
          // stands first: a name is what the wall says, but the picture is what a viewer walks by.
          AddMenuRow("Grid", "grid_view", new AsyncRelayCommand(() => PickLayoutAsync(LAYOUT_GRID)),
              Layout == LAYOUT_GRID);
          // No Cover, no Stacked, no 3D Cover: those three are a shelf of covers put down three ways, and a person
          // is not a cover.
          AddMenuRow(DetailsLabel, "details", new RelayCommand(ToggleDetails), ShowDetails);
          return;

        case MenuPage.Sort:
          MediaMenuHeading = "Sort";
          foreach ((string word, string key) choice in SortChoices())
          {
            string picked = choice.key;
            bool stands = string.Equals(Sort, picked, StringComparison.OrdinalIgnoreCase);
            // The standing row names the end the wall is read from; pressing it again is what turns that around.
            AddMenuRow(choice.word, "sort", new AsyncRelayCommand(() => PickSortAsync(picked)), stands,
                DirectionWord(stands && SortDescending));
          }
          return;

        case MenuPage.Group:
          MediaMenuHeading = "Group";
          AddMenuRow("None", "library_books", new AsyncRelayCommand(() => PickGroupAsync(null)), !IsGrouped);
          foreach ((string word, string key) choice in GroupChoices())
          {
            string picked = choice.key;
            AddMenuRow(choice.word, "group", new AsyncRelayCommand(() => PickGroupAsync(picked)),
                string.Equals(Grouping, picked, StringComparison.OrdinalIgnoreCase));
          }
          return;

        case MenuPage.Letters:
          MediaMenuHeading = "Letters";
          AddMenuRow("Every name", "library_books", new AsyncRelayCommand(() => PickLetterAsync(null)),
              Letter.Length == 0);
          foreach (char letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
          {
            string asked = letter.ToString();
            AddMenuRow(asked, "", new AsyncRelayCommand(() => PickLetterAsync(asked)),
                string.Equals(Letter, asked, StringComparison.OrdinalIgnoreCase));
          }
          return;

        default:
          MediaMenuHeading = "Menu";
          AddMenuRow("Search", "search", new RelayCommand(delegate
          {
            // The field is the screen's, so the row asks the screen for it - and takes the rail out of the way
            // first, because a rail standing over a field a hand is about to type into is a rail in the way.
            HideMediaMenu();
            OpenSearch();
          }));
          AddMenuRow("Letters", "filter", new RelayCommand(() => GoToPage(MenuPage.Letters)), false,
              Letter.Length == 0 ? "Every name" : Letter);
          AddMenuRow("Group", "group", new RelayCommand(() => GoToPage(MenuPage.Group)));
          AddMenuRow(SortLabel, "sort", new RelayCommand(() => GoToPage(MenuPage.Sort)), false,
              DirectionWord(SortDescending));
          AddMenuRow("View", "grid_view", new RelayCommand(() => GoToPage(MenuPage.View)));
          return;
      }
    }

    /// <summary>The word the commands page spends on the order, which names what the wall is read by.</summary>
    private string SortLabel
    {
      get { return string.Equals(Sort, "born", StringComparison.OrdinalIgnoreCase) ? "By born" : "By name"; }
    }

    /// <summary>The two orders this wall can be read by.  Nothing else is on a person's tile to read by.</summary>
    private static IReadOnlyList<(string Word, string Key)> SortChoices()
    {
      return new[] { ("Name", "name"), ("Born", "born") };
    }

    /// <summary>
    /// The plates a wall of names can stand under.  A letter and a decade are all a person's tile holds to stand
    /// under - a wall of people has no genre, no year of release and no studio; those belong to the works, which the
    /// wall opens when a name is pressed rather than plating itself with them.
    /// </summary>
    private static IReadOnlyList<(string Word, string Key)> GroupChoices()
    {
      return new[] { ("First letter", GROUP_LETTER), ("Birth decade", GROUP_BORN) };
    }

    /// <summary>
    /// Which end a reading starts at, for the line at the screen's foot.  A wall read by birth year is not read
    /// "A to Z", and saying so on a wall of birth years would be the screen talking past what it is doing.
    /// </summary>
    private string DirectionWord(bool descending)
    {
      if (string.Equals(Sort, "born", StringComparison.OrdinalIgnoreCase))
        return descending ? "Newest first" : "Oldest first";

      return descending ? "Z to A" : "A to Z";
    }

    /// <summary>
    /// Puts the wall into one of its two shapes.  The press writes the answer down at once, the way the shelf does:
    /// a viewer who changes the shape and walks away should not have to change it back tomorrow.
    /// </summary>
    public async Task PickLayoutAsync(string shape)
    {
      string wore = Layout;
      Layout = string.Equals(shape, LAYOUT_LIST, StringComparison.OrdinalIgnoreCase) ? LAYOUT_LIST : LAYOUT_GRID;
      Remember(answer => answer.Layout = Layout);
      if (wore != Layout)
        BuildMediaMenu();
      await Task.CompletedTask;
    }

    /// <summary>
    /// Reads the wall by another order, or - pressed on the order that already stands - reads it from its other
    /// end.  The same two-step the shelf's sort rows have, because a viewer who picks the order they are already
    /// standing on means "the other way".
    /// </summary>
    public async Task PickSortAsync(string sort)
    {
      if (string.Equals(Sort, sort, StringComparison.OrdinalIgnoreCase))
        SortDescending = !SortDescending;
      else
      {
        Sort = sort;
        SortDescending = false;
      }

      Remember(answer =>
      {
        answer.Sort = Sort;
        answer.Descending = SortDescending;
      });
      BuildMediaMenu();
      await ReadAsync(true);
    }

    /// <summary>
    /// Stands the wall under its plates, or takes the plates away again.  A wall plated by a key it is not READ by
    /// opens a new plate on every second name and is not a grouping at all, so the order a plate is cut by comes with
    /// the plate: letters plate a wall read by name, decades plate a wall read by born.  Taking the plates away
    /// leaves the order alone - a viewer who turns the grouping off did not ask to be sorted somewhere else.
    /// </summary>
    public async Task PickGroupAsync(string? grouping)
    {
      string goes = (grouping ?? "").Trim().ToLowerInvariant();
      Grouping = GroupChoices().Any(choice => string.Equals(choice.Key, goes, StringComparison.OrdinalIgnoreCase))
          ? goes
          : "";

      string reads = string.Equals(Grouping, GROUP_BORN, StringComparison.OrdinalIgnoreCase) ? "born" : "name";
      if (Grouping.Length > 0 && !string.Equals(Sort, reads, StringComparison.OrdinalIgnoreCase))
      {
        Sort = reads;
        SortDescending = false;
      }

      Remember(answer =>
      {
        answer.Grouping = Grouping;
        answer.Sort = Sort;
        answer.Descending = SortDescending;
      });
      BuildMediaMenu();
      await ReadAsync(true);
    }

    /// <summary>Keeps the names beginning with one letter, or asks for every name again.</summary>
    public async Task PickLetterAsync(string? letter)
    {
      Letter = letter ?? "";
      BuildMediaMenu();
      await ReadAsync(true);
    }

    /// <summary>
    /// How long a hand has to rest on the last letter before the wall is asked for the words so far.  The shelf's
    /// number for the same question (MediaBrowserViewModel.TYPING_PAUSE_MILLISECONDS) and its reason: one question
    /// per letter is seven questions for a seven-letter name, and every one of them lays a new wall under the
    /// viewer's hand while they are still spelling.
    /// </summary>
    private const int TYPING_PAUSE_MILLISECONDS = 300;

    /// <summary>The pause timer, made once and restarted with every letter.</summary>
    private DispatcherTimer? _typing;

    /// <summary>Whether the program is writing the box itself, which is never a viewer asking.</summary>
    private bool _writingWords;

    partial void OnSearchTextChanged(string value)
    {
      // Only a field that stands on the screen is somebody typing in it.
      if (_writingWords || !SearchOpen)
        return;

      _typing ??= new DispatcherTimer(DispatcherPriority.Background)
      {
        Interval = TimeSpan.FromMilliseconds(TYPING_PAUSE_MILLISECONDS)
      };

      _typing.Stop();
      _typing.Tick -= OnTypingPaused;
      _typing.Tick += OnTypingPaused;
      _typing.Start();
    }

    private async void OnTypingPaused(object? sender, EventArgs e)
    {
      _typing?.Stop();
      await FindTypedAsync();
    }

    /// <summary>Searches the names for the words a viewer typed.</summary>
    public async Task FindAsync(string words)
    {
      Search = words ?? "";
      await ReadAsync(true);
    }

    /// <summary>
    /// Asks the wall for the words that stand in the field: Enter on the field and the end of the typing pause both
    /// come here.  Empty words are the wish to see the whole wall again, which is the same answer the mark at the
    /// field's hand gives.
    /// </summary>
    public async Task FindTypedAsync()
    {
      // Enter answers at once, so a question that was already waiting out the pause is not asked twice.
      _typing?.Stop();

      string words = (SearchText ?? "").Trim();
      if (words.Length == 0)
      {
        await ClearSearchAsync();
        return;
      }

      await FindAsync(words);
    }

    /// <summary>
    /// Puts the whole wall back and leaves the field standing empty under the caret.  The mark at the field's hand
    /// and an Enter on an empty field come here; back and Escape ask for more than this and go to
    /// <see cref="CloseSearchAsync"/>.
    /// </summary>
    [RelayCommand]
    public async Task ClearSearchAsync()
    {
      // Taking the words away is one errand, not a viewer asking: the wall comes back here and now, and the box
      // being emptied by the program must not send a second question thirty hundredths of a second later.
      _typing?.Stop();
      _writingWords = true;
      try
      {
        SearchText = "";
      }
      finally
      {
        _writingWords = false;
      }

      if (HasSearch)
        Search = "";

      await ReadAsync(true);
    }

    /// <summary>
    /// Takes the search away whole: the field goes off the head band, its words and the question it made with it,
    /// and the wall stands full again.  Back and Escape come here, from wherever the caret stands.
    /// </summary>
    /// <remarks>
    /// Not <see cref="ClearSearchAsync"/> with the field hidden afterwards, and the difference is what a viewer can
    /// reach: clearing leaves an empty field under the caret, so the same key has to be pressed twice to be rid of
    /// it - which is the "there is no way out" a viewer with a remote reported about the shelf on 2026-11-10.
    /// </remarks>
    [RelayCommand]
    public async Task CloseSearchAsync()
    {
      SearchOpen = false;
      await ClearSearchAsync();
    }

    /// <summary>Takes the letter back off the wall, which is what "Every name" and the back key ask for.</summary>
    public async Task ClearLetterAsync()
    {
      if (Letter.Length == 0)
        return;

      Letter = "";
      BuildMediaMenu();
      await ReadAsync(true);
    }

    /// <summary>
    /// The names this letter holds so far, for a screen that wants to say how big a letter is.  Said out loud only
    /// for a wall that has been read whole: until the last page arrived the number is what has travelled, not what
    /// the wall holds, and a plate that lies about its size is worse than one that stays quiet.
    /// </summary>
    public int NamesUnderLetter(string letter)
    {
      return _namesPerLetter.TryGetValue(letter, out int count) ? count : 0;
    }

    /// <summary>Whether the wall has read everything it holds, which is when a plate may speak its number.</summary>
    public bool WallReadWhole
    {
      get { return _atEnd && Total > 0 && Rows.OfType<PersonTile>().Count() >= Total; }
    }

    // The remembered reading.  Keyed the way the shelf keys its screens - the media kind and the class of filter -
    // because a wall of film actors IS the films' screen standing on the actors class, and the three walls then
    // answer apart from one another without a new word being invented for any of them.
    private bool TryRemembered(out ScreenViewSettings.ScreenAnswer answer)
    {
      return _screens.TryAnswer(AspectName, FilterClass, out answer);
    }

    private void Remember(Action<ScreenViewSettings.ScreenAnswer> answer)
    {
      answer(_screens.AnswerFor(AspectName, FilterClass));
      _screens.Save(null, out _);
    }

    /// <summary>
    /// Puts the shape, the order and the plates back the way this wall's viewer last left them.  A remembered
    /// answer is only believed when this wall offers it - a wall that knows two shapes refuses a remembered
    /// "Cover" rather than wearing nothing - and an answer nobody ever gave leaves the wall as it was written.
    /// </summary>
    private void RestoreRememberedReading()
    {
      if (!TryRemembered(out ScreenViewSettings.ScreenAnswer answer))
        return;

      if (string.Equals(answer.Layout, LAYOUT_LIST, StringComparison.OrdinalIgnoreCase)
          || string.Equals(answer.Layout, LAYOUT_GRID, StringComparison.OrdinalIgnoreCase))
        Layout = answer.Layout!.Trim();

      if (string.Equals(answer.Sort, "born", StringComparison.OrdinalIgnoreCase)
          || string.Equals(answer.Sort, "name", StringComparison.OrdinalIgnoreCase))
        Sort = answer.Sort!.Trim().ToLowerInvariant();

      SortDescending = answer.Descending;

      // A grouping this screen does not offer is no grouping at all - the same test the shelf applies before it
      // believes a stored answer, because a grouping remembered by a newer screen would otherwise come back as
      // plates this wall cannot fill.
      if (GroupChoices().Any(choice =>
              string.Equals(answer.Grouping, choice.Key, StringComparison.OrdinalIgnoreCase)))
        Grouping = answer.Grouping!.Trim().ToLowerInvariant();
    }
  }
}
