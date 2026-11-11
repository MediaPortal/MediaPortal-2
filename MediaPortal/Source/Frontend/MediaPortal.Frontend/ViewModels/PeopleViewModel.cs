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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
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
  /// so the biography stays where it is until a viewer presses one person and asks for it by id.
  /// </remarks>
  public sealed class PersonTile
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

    private enum MenuPage
    {
      Commands,
      View,
      Sort,
      Group,
      Letters
    }

    private readonly ServerApiClient? _client;
    private readonly ScreenViewSettings _screens;
    private readonly Dictionary<string, int> _namesPerLetter = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    private MenuPage _menuPage;
    private long _reading;
    private int _offset;
    private bool _atEnd;

    public PeopleViewModel(ServerApiClient? client, PeopleShelf shelf)
    {
      _client = client;
      Shelf = shelf;
      _screens = ScreenViewSettings.Load(null, out _);
      RestoreRememberedReading();
      BuildMediaMenu();
    }

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

    /// <summary>The person the caret stands on, for the details area to speak of.</summary>
    [ObservableProperty]
    private PersonTile? _selected;

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

    /// <summary>
    /// Asked of the screen when the "Search" row is pressed, because the field is the screen's control and this
    /// model has no business reaching for a caret.
    /// </summary>
    public event Action? SearchAsked;

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
      if (fromTheStart)
      {
        Rows.Clear();
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
      }
      catch (Exception e)
      {
        if (asked == _reading)
          Notice = "The server did not answer the wall: " + e.Message;
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
      if (IsGrouped)
      {
        string letter = tile.Letter;
        int stood = Rows.Count;
        if (stood == 0 || !(Rows[stood - 1] is PersonTile last)
            || !string.Equals(last.Letter, letter, StringComparison.OrdinalIgnoreCase))
        {
          Rows.Add(new GroupHeaderRow(letter, 0, false));
        }
        _namesPerLetter[letter] = _namesPerLetter.TryGetValue(letter, out int soFar) ? soFar + 1 : 1;
      }

      Rows.Add(tile);
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
          // is not a cover.  No details row either, because this wall wears its details whenever its screen has
          // room for them - see the people screen's own details area.
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
          AddMenuRow("First letter", "group", new AsyncRelayCommand(() => PickGroupAsync(GROUP_LETTER)),
              string.Equals(Grouping, GROUP_LETTER, StringComparison.OrdinalIgnoreCase));
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
            SearchAsked?.Invoke();
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

    /// <summary>Which end a reading starts at, for the line at the screen's foot.</summary>
    private static string DirectionWord(bool descending)
    {
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

    /// <summary>Stands the wall under its letters, or takes the plates away again.</summary>
    public async Task PickGroupAsync(string? grouping)
    {
      Grouping = string.IsNullOrEmpty(grouping) ? "" : GROUP_LETTER;
      Remember(answer => answer.Grouping = Grouping);
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

    /// <summary>Searches the names for the words a viewer typed.</summary>
    public async Task FindAsync(string words)
    {
      Search = words ?? "";
      await ReadAsync(true);
    }

    /// <summary>Stops searching, which puts the whole wall back.</summary>
    public async Task ClearSearchAsync()
    {
      if (Search.Length == 0)
        return;

      Search = "";
      await ReadAsync(true);
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

      if (string.Equals(answer.Grouping, GROUP_LETTER, StringComparison.OrdinalIgnoreCase))
        Grouping = GROUP_LETTER;
    }
  }
}
