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
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using MediaPortal.Frontend.Skins.Nereus;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// What one line of the home screen's content pane is: the heading of a list, a title inside one, or a line
  /// saying why a list is not there.
  /// </summary>
  public enum HomeRowKind
  {
    /// <summary>The name of one of the lists, drawn as a heading.</summary>
    Heading,

    /// <summary>A title somebody can start.</summary>
    Title,

    /// <summary>An action from Nereus' "Other Plugins" content.</summary>
    MenuEntry,

    /// <summary>A shortcut into the selected media shelf.</summary>
    Shortcut,

    /// <summary>Something the pane has to say that is not a title - a refusal, or what pressing the entry does.</summary>
    Note
  }

  /// <summary>
  /// One line of the content pane, which is why it is one row type rather than a list per shelf: the pane is
  /// walked with a remote, and a viewer cannot stop on a heading that belongs to a list of its own.
  /// </summary>
  public sealed class HomeRow
  {
    private HomeRow(HomeRowKind kind, string text, TitleViewModel? title, MainMenuEntry? menuEntry = null,
        NereusHomeShortcut? shortcut = null)
    {
      Kind = kind;
      Text = text;
      Title = title;
      MenuEntry = menuEntry;
      Shortcut = shortcut;
    }

    public HomeRowKind Kind { get; }

    /// <summary>The heading or the note; empty on a title row, which draws <see cref="Title"/> instead.</summary>
    public string Text { get; }

    /// <summary>The title, on a title row; null on every other kind.</summary>
    public TitleViewModel? Title { get; }

    /// <summary>The menu action, on a Nereus "Other Plugins" row; null on every other kind.</summary>
    public MainMenuEntry? MenuEntry { get; }

    public NereusHomeShortcut? Shortcut { get; }

    public bool IsHeading
    {
      get { return Kind == HomeRowKind.Heading; }
    }

    public bool IsTitle
    {
      get { return Kind == HomeRowKind.Title; }
    }

    public bool IsNote
    {
      get { return Kind == HomeRowKind.Note; }
    }

    public bool IsMenuEntry
    {
      get { return Kind == HomeRowKind.MenuEntry; }
    }

    public bool IsShortcut
    {
      get { return Kind == HomeRowKind.Shortcut; }
    }

    public static HomeRow Heading(string text)
    {
      return new HomeRow(HomeRowKind.Heading, text, null);
    }

    public static HomeRow Note(string text)
    {
      return new HomeRow(HomeRowKind.Note, text, null);
    }

    public static HomeRow For(TitleViewModel title)
    {
      return new HomeRow(HomeRowKind.Title, string.Empty, title);
    }

    public static HomeRow For(MainMenuEntry entry)
    {
      return new HomeRow(HomeRowKind.MenuEntry, string.Empty, null, entry);
    }

    public static HomeRow For(NereusHomeShortcut shortcut)
    {
      return new HomeRow(HomeRowKind.Shortcut, string.Empty, null, shortcut: shortcut);
    }
  }

  /// <summary>A single focusable tile in the horizontal Nereus home-content wall.</summary>
  public sealed class HomeContentTile
  {
    private HomeContentTile(TitleViewModel? title, MainMenuEntry? menuEntry, NereusHomeShortcut? shortcut,
        HomeRow? note, double x, double y, double width, double height)
    {
      Title = title;
      MenuEntry = menuEntry;
      Shortcut = shortcut;
      Note = note;
      X = x;
      Y = y;
      Width = width;
      Height = height;
    }

    public TitleViewModel? Title { get; }
    public MainMenuEntry? MenuEntry { get; }
    public NereusHomeShortcut? Shortcut { get; }
    public HomeRow? Note { get; }

    // Where the band puts the square. The band is planned for the covers it holds before it reaches the wall, so
    // a square is placed once, stands still, and never moves under the viewer's eyes. See HomeContentGroup.
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public bool IsTitle => Title != null;
    public bool IsMenuEntry => MenuEntry != null;
    public bool IsShortcut => Shortcut != null;
    public bool IsNote => Note != null;
    public bool IsFocusable => !IsNote;
    public string Label => Title?.Name ?? MenuEntry?.Label ?? Shortcut?.Label ?? Note?.Text ?? string.Empty;
    public string Detail => Title?.Detail ?? MenuEntry?.Detail ?? string.Empty;
    public string? ImagePath => Shortcut?.ImagePath;

    internal static HomeContentTile ForTitle(TitleViewModel title, double x, double y, double width, double height)
    {
      return new HomeContentTile(title, null, null, null, x, y, width, height);
    }

    internal static HomeContentTile ForMenuEntry(MainMenuEntry entry, double x, double y, double width, double height)
    {
      return new HomeContentTile(null, entry, null, null, x, y, width, height);
    }

    internal static HomeContentTile ForShortcut(NereusHomeShortcut shortcut, double x, double y, double width, double height)
    {
      return new HomeContentTile(null, null, shortcut, null, x, y, width, height);
    }

    internal static HomeContentTile ForNote(HomeRow note, double x, double y, double width, double height)
    {
      return new HomeContentTile(null, null, null, note, x, y, width, height);
    }
  }

  /// <summary>
  /// A reference-shaped group: a shortcut rail or one Nereus mosaic of covers.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The mosaic tables below are the reference's own, and they were drawn for a band the server filled - six
  /// covers, each in the place the skin wrote for it, the first cover the big one. Asked to lay two covers out
  /// from the same table they scatter across the band, because the table's next places are at its far side, and
  /// the room the missing covers would have filled keeps standing. The wall next to the band reads that room as
  /// a gap, and a gap is what a wall of covers may not have.
  /// </para>
  /// <para>
  /// So a band is planned when it is built, from the number of covers it will hold - which is known then,
  /// because a band is read whole before it is put on the wall. A band the server filled keeps the face's mosaic
  /// exactly as drawn; a short band has its squares packed in reading order from the left instead, so no cell
  /// stands empty while another one right of it is taken. Squares are placed once and stand still, and the band
  /// is as wide as the covers it holds - <see cref="OccupiedWidth"/> says that to the wall. Vertically a band
  /// always keeps the height the skin drew it in; only room sideways was ever the complaint.
  /// </para>
  /// </remarks>
  public sealed class HomeContentGroup
  {
    /// <summary>
    /// How far a cover's shadow reaches past the cover: the Nereus tile's shadow leans four pixels down and
    /// blurs sixteen, so twenty below a cover is where it has faded out. Bands are drawn this much taller than
    /// the height they lay their covers out in, so that the lowest cover's shadow is not cut off.
    /// </summary>
    private const double SHADOW_ROOM = 20;

    /// <summary>
    /// The line the first row of covers stands on, below the row the band's heading has for itself.
    ///
    /// <para>
    /// Every kind of band starts its squares on this line - a mosaic of covers as well as a rail of shortcuts -
    /// because the distance between a heading and what stands under it is one decision about the wall, not one
    /// per band. When the rail started higher than the mosaic, the two headings lined up while their squares did
    /// not, and the wall reads that as a heading sitting too close to its covers.
    /// </para>
    /// </summary>
    private const double CONTENT_TOP = 52;

    private static readonly (int Column, int Row, int ColumnSpan, int RowSpan)[] POSTER_LAYOUT_1 =
    {
        (0, 0, 2, 2), (2, 0, 1, 1), (3, 0, 1, 1),
        (0, 2, 1, 1), (1, 2, 1, 1), (2, 1, 2, 2)
    };

    private static readonly (int Column, int Row, int ColumnSpan, int RowSpan)[] POSTER_LAYOUT_2 =
    {
        (0, 0, 1, 1), (1, 0, 1, 1), (2, 0, 2, 2),
        (0, 1, 2, 2), (2, 2, 1, 1), (3, 2, 1, 1)
    };

    private static readonly (int Column, int Row, int ColumnSpan, int RowSpan)[] POSTER_LAYOUT_3 =
    {
        (0, 0, 2, 2), (2, 0, 2, 2), (0, 2, 1, 1),
        (1, 2, 1, 1), (2, 2, 1, 1), (3, 2, 1, 1)
    };

    private static readonly (int Column, int Row, int ColumnSpan, int RowSpan)[] SQUARE_LAYOUT_1 =
    {
        (0, 0, 2, 2), (2, 0, 1, 1), (2, 1, 1, 1),
        (0, 2, 1, 1), (0, 3, 1, 1), (1, 2, 2, 2)
    };

    private static readonly (int Column, int Row, int ColumnSpan, int RowSpan)[] SQUARE_LAYOUT_2 =
    {
        (0, 0, 1, 1), (0, 1, 1, 1), (1, 0, 2, 2),
        (0, 2, 2, 2), (2, 2, 1, 1), (2, 3, 1, 1)
    };

    private static readonly (int Column, int Row, int ColumnSpan, int RowSpan)[] THUMBNAIL_LAYOUT_1 =
    {
        (0, 0, 2, 2), (2, 0, 1, 1), (2, 1, 1, 1),
        (0, 2, 1, 1), (0, 3, 1, 1), (1, 2, 2, 2)
    };

    private static readonly (int Column, int Row, int ColumnSpan, int RowSpan)[] THUMBNAIL_LAYOUT_2 =
    {
        (0, 0, 1, 1), (0, 1, 1, 1), (1, 0, 2, 2),
        (0, 2, 2, 2), (2, 2, 1, 1), (2, 3, 1, 1)
    };

    private readonly int _columns;
    private readonly int _rows;
    private readonly (int Column, int Row, int ColumnSpan, int RowSpan)[] _layout;
    private readonly double _inset = 12;
    private readonly double _cellWidth;
    private readonly double _cellHeight;

    /// <summary>Whether the covers that arrived are the six the skin's mosaic table was drawn for.</summary>
    private readonly bool _designedMosaic;

    /// <summary>How many cells wide the band's first cover is when the band came back short.</summary>
    private readonly int _firstCoverSpan = 1;

    private readonly HashSet<(int Column, int Row)> _taken = new HashSet<(int, int)>();
    private double _right;
    private double _bottom;

    /// <param name="covers">
    /// How many squares the band will hold, which is known before it is built: a band is read whole and only
    /// then put on the wall. The number decides whether the skin's mosaic table can be honoured - it was drawn
    /// for a band that was filled - or whether the squares are packed from the left instead, so that the room
    /// of the covers that did not arrive is not kept standing between this band and the next.
    /// </param>
    public HomeContentGroup(string heading, string layout, double width, int covers)
    {
      Heading = heading;
      Layout = layout;
      Width = width;
      Height = 880;

      if (layout == "shortcuts" || layout == "plugins")
      {
        _columns = 1;
        _rows = 5;
        _layout = Array.Empty<(int, int, int, int)>();
        return;
      }

      (_columns, _rows, _layout) = layout switch
      {
        "poster2" => (4, 3, POSTER_LAYOUT_2),
        "poster3" => (4, 3, POSTER_LAYOUT_3),
        "square1" => (3, 4, SQUARE_LAYOUT_1),
        "square2" => (3, 4, SQUARE_LAYOUT_2),
        "thumbnail1" => (3, 4, THUMBNAIL_LAYOUT_1),
        "thumbnail2" => (3, 4, THUMBNAIL_LAYOUT_2),
        _ => (4, 3, POSTER_LAYOUT_1)
      };

      _cellWidth = (Width - _inset * 2) / _columns;
      _cellHeight = (Height - CONTENT_TOP - _inset) / _rows;
      _designedMosaic = covers >= _layout.Length;

      // The big first cover is the face's own, but only while the covers beside it arrive too: a cover over two
      // cells stands over 2 * columns - 4 cells in the row below it, and without those covers the cells it
      // should stand over are the gap again, only bigger.
      _firstCoverSpan = !_designedMosaic && covers > 1 && _columns >= 2 && _rows >= 2
          && covers - 1 >= 2 * _columns - 4 ? 2 : 1;
    }

    public string Heading { get; }
    public string Layout { get; }

    /// <summary>The width the face drew this band for, whether or not the server filled it.</summary>
    public double Width { get; }

    /// <summary>The height the face drew this band for, whether or not the server filled it.</summary>
    public double Height { get; }

    /// <summary>
    /// The band's box: its height, plus the room the lowest covers' shadows fall into. A cover's shadow leans
    /// four down and blurs sixteen, so a cover that stands at the bottom of the band's height would have its
    /// shadow cut off by whoever draws the band - which is exactly what happened to the lowest shortcut, the one
    /// cover whose shadow had nothing left below it. The wall grows by this much; the covers do not.
    /// </summary>
    public double BoxHeight => Math.Max(Height, _bottom) + SHADOW_ROOM;

    /// <summary>
    /// What the band really needs: the width its covers actually cover. A band the server answered with two
    /// titles is not shown as a column of six with four places kept warm for titles that never came, because
    /// the wall would then read that kept room as a gap between its bands.
    /// </summary>
    public double OccupiedWidth { get; private set; }

    public ObservableCollection<HomeContentTile> Tiles { get; } = new ObservableCollection<HomeContentTile>();

    public void AddTitle(TitleViewModel title)
    {
      AddTile((x, y, width, height) => HomeContentTile.ForTitle(title, x, y, width, height));
    }

    public void AddShortcut(NereusHomeShortcut shortcut)
    {
      AddTile((x, y, width, height) => HomeContentTile.ForShortcut(shortcut, x, y, width, height));
    }

    public void AddMenuEntry(MainMenuEntry entry)
    {
      AddTile((x, y, width, height) => HomeContentTile.ForMenuEntry(entry, x, y, width, height));
    }

    public void AddNote(HomeRow note)
    {
      AddSquareTo(HomeContentTile.ForNote(note, _inset, CONTENT_TOP + 5, Width - _inset * 2, 80));
      OccupiedWidth = Width;
    }

    /// <summary>The lowest point a square of this band reaches, which is what decides the band's box.</summary>
    private void AddSquareTo(HomeContentTile tile)
    {
      _bottom = Math.Max(_bottom, tile.Y + tile.Height);
      Tiles.Add(tile);
    }

    /// <summary>
    /// Puts one more square into the band, in the place the band's plan has for it, and stretches the band no
    /// wider than that square. A square is placed once and never moves.
    /// </summary>
    private void AddTile(Func<double, double, double, double, HomeContentTile> create)
    {
      if (Layout == "shortcuts" || Layout == "plugins")
      {
        double size = Layout == "shortcuts" ? 159 : 150;
        double rail = 8;
        int index = Tiles.Count;
        // The first shortcut stands on the line the first cover of a mosaic stands on, and the ones under it
        // follow ten apart - the same ten that stands between two covers sideways.
        double top = CONTENT_TOP + 5 + index * (size + 10);
        _right = Math.Max(_right, rail + size);
        AddSquareTo(create(rail, top, size, size));
        OccupiedWidth = Math.Min(Width, _right + _inset);
        return;
      }

      int square = Tiles.Count;
      int column;
      int row;
      int columnSpan;
      int rowSpan;

      if (_designedMosaic)
      {
        (column, row, columnSpan, rowSpan) = _layout[square % _layout.Length];
      }
      else
      {
        columnSpan = square == 0 ? _firstCoverSpan : 1;
        rowSpan = columnSpan;
        (column, row) = NextFreeCell(columnSpan);
      }

      for (int dx = 0; dx < columnSpan; dx++)
      {
        for (int dy = 0; dy < rowSpan; dy++)
          _taken.Add((column + dx, row + dy));
      }

      // A square stands five off its cell on every side, which is the ten between two covers on the wall.
      double x = _inset + column * _cellWidth + 5;
      double y = CONTENT_TOP + row * _cellHeight + 5;
      double width = _cellWidth * columnSpan - 10;
      double height = _cellHeight * rowSpan - 10;
      _right = Math.Max(_right, x + width);

      AddSquareTo(create(x, y, width, height));
      OccupiedWidth = _designedMosaic ? Width : Math.Min(Width, _right + _inset);
    }

    /// <summary>The first cell of the band's grid, in reading order, that a square of <paramref name="span"/>
    /// cells wide and high still fits into. Reading order is what keeps a short band free of holes: a square is
    /// never put into a cell while a cell left of it, or above it, stands empty.</summary>
    private (int Column, int Row) NextFreeCell(int span)
    {
      for (int row = 0; ; row++)
      {
        for (int column = 0; column + span <= _columns; column++)
        {
          bool free = true;
          for (int dx = 0; dx < span && free; dx++)
          {
            for (int dy = 0; dy < span && free; dy++)
              free = !_taken.Contains((column + dx, row + dy));
          }

          if (free)
            return (column, row);
        }
      }
    }
  }

  /// <summary>
  /// One of the lists MP2's home screen shows next to the main menu, for the entry that is highlighted.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The old client asked the skin to do this: <c>Nereus</c>' <c>HomeMenuModel</c>
  /// (<c>Source/UI/Skins/Nereus/Models/HomeMenuModel.cs</c>) handed the highlighted menu entry over to one of the
  /// <c>AbstractHomeContent</c> subclasses in <c>Models/HomeContent/</c>, and each of those offered the same four
  /// lists - <c>Latest*</c>, <c>ContinuePlay*</c>, <c>Favorite*</c> and <c>Unplayed*</c> - of which the skin drew at
  /// most six titles each (<c>MovieHomeContent.cs:35-73</c>). Those four names are therefore what a MediaPortal
  /// viewer expects to see, and they are the headings below: <c>Nereus.Home.LatestAdded</c>,
  /// <c>Nereus.Home.ContinuePlayed</c>, <c>Nereus.Home.Favorites</c> and <c>Nereus.Home.Unplayed</c>, in the
  /// plugin's own English words.
  /// </para>
  /// <para>
  /// What each list can be asked for over HTTP is written next to it. Three of the four can: the HTTP API sorts by
  /// title, year, the date the library first saw the file, last-played, track and episode, so "Latest added",
  /// "Continue playing" and "Unplayed" are all questions this server can answer. "Favorites" is the one that cannot,
  /// because nothing on the server keeps favourites, and it stays on the pane as one line saying so - a heading that
  /// quietly vanished would leave the viewer wondering whether the library has no favourites or the front end has
  /// lost the list.
  /// </para>
  /// <para>
  /// The four headings are therefore Nereus' four, in his order, and the dates tell them apart from each other the
  /// way MP2's own lists did: "Latest added" is a fact about this library (when did it learn of this file), while
  /// the year behind "Unplayed" is a fact about the work itself.
  /// </para>
  /// </remarks>
  public sealed class HomeContentList
  {
    /// <summary>
    /// How many titles one list contributes. This is MP2's own number: <c>AbstractHomeContent.CONTENT_LIST_LIMIT</c>
    /// (<c>Source/UI/Skins/Nereus/Models/HomeContent/AbstractHomeContent.cs</c>) is six, and the pane has to hold
    /// four lists of them above a television.
    /// </summary>
    public const int CONTENT_LIST_LIMIT = 6;

    private HomeContentList(string heading, string? sort, bool descending, string? seen, string? missing,
        string layout, double width)
    {
      Heading = heading;
      Sort = sort;
      Descending = descending;
      Seen = seen;
      Missing = missing;
      Layout = layout;
      Width = width;
    }

    /// <summary>What the pane calls the list, in Nereus' words.</summary>
    public string Heading { get; }

    /// <summary>The sort to ask the server for, or null to take whatever order it gives.</summary>
    public string? Sort { get; }

    /// <summary>Whether that sort runs from the newest down.</summary>
    public bool Descending { get; }

    /// <summary>The watched-state filter to ask for, or null for titles whatever their history.</summary>
    public string? Seen { get; }

    /// <summary>Why nothing can be listed here, or null when the list can be asked for.</summary>
    public string? Missing { get; }

    /// <summary>The reference mosaic used for this list.</summary>
    public string Layout { get; }

    /// <summary>The reference tile-group width for this list.</summary>
    public double Width { get; }

    private static readonly Guid MOVIES_ACTION_ID = new Guid("80d2e2cc-baaa-4750-807b-f37714153751");
    private static readonly Guid SERIES_ACTION_ID = new Guid("30f57cba-459c-4202-a587-09fff5098251");
    private static readonly Guid AUDIO_ACTION_ID = new Guid("30715d73-4205-417f-80aa-e82f0834171f");
    private static readonly Guid IMAGES_ACTION_ID = new Guid("55556593-9fe9-436c-a3b6-a971e10c9d44");
    private static readonly Guid VIDEOS_ACTION_ID = new Guid("a4df2df6-8d66-479a-9930-d7106525eb07");
    private static readonly Guid TV_ACTION_ID = new Guid("b4a9199f-6dd4-4bda-a077-de9c081f7703");
    private static readonly Guid RADIO_ACTION_ID = new Guid("e3bbc989-99db-40e9-a15f-ccb50b17a4c8");
    private static readonly Guid ONLINE_VIDEOS_ACTION_ID = new Guid("c33e39cc-910e-41c8-bffd-9eccd340b569");
    private static readonly Guid EMULATORS_ACTION_ID = new Guid("a24958e2-538a-455e-a1db-a7bb241af7ec");
    private static readonly Guid WEBRADIO_ACTION_ID = new Guid("2ded75c0-5eae-4e69-9913-6b50a9ab2956");
    private static readonly Guid LAUNCHER_ACTION_ID = new Guid("873eb147-c998-4632-8f86-d5e24062be2e");
    private static readonly Guid NEWS_ACTION_ID = new Guid("bb49a591-7705-408f-8177-45d633fdfad0");
    private static readonly Guid WEATHER_ACTION_ID = new Guid("e34fdb62-1f3e-4aa9-8a61-d143e0af77b5");

    private static readonly IReadOnlyDictionary<Guid, string[]> DEFAULT_LIST_KEYS =
        new Dictionary<Guid, string[]>
        {
          [MOVIES_ACTION_ID] = new[] { "LatestMovies", "ContinuePlayMovies", "FavoriteMovies", "UnplayedMovies" },
          [SERIES_ACTION_ID] = new[] { "LatestEpisodes", "ContinuePlaySeries", "FavoriteSeries", "UnplayedSeries" },
          [AUDIO_ACTION_ID] = new[] { "LatestAudio", "ContinuePlayAlbum", "FavoriteAudio", "UnplayedAlbum" },
          [IMAGES_ACTION_ID] = new[] { "LatestImages", "FavoriteImages", "UnplayedImages" },
          [VIDEOS_ACTION_ID] = new[] { "LatestVideo", "ContinuePlayVideo", "FavoriteVideo", "UnplayedVideo" },
          [TV_ACTION_ID] = new[] { "LastPlayTV", "FavoriteTV", "CurrentTVPrograms", "CurrentSchedules" },
          [RADIO_ACTION_ID] = new[] { "LastPlayRadio", "FavoriteRadio", "CurrentRadioPrograms", "CurrentSchedules" },
          [ONLINE_VIDEOS_ACTION_ID] = new[] { "TopLanguagesOnlineVideoSites", "LastOnlineVideoSites", "FavoriteOnlineVideoSites" },
          [EMULATORS_ACTION_ID] = new[] { "LastPlayedGames", "FavoriteGames", "LatestGames", "UnplayedGames" },
          [WEBRADIO_ACTION_ID] = new[] { "LastPlayedWebradio", "TopPlayedWebradio" },
          [LAUNCHER_ACTION_ID] = new[] { "LastLaunchApps", "FavoriteApps" },
          [NEWS_ACTION_ID] = Array.Empty<string>(),
          [WEATHER_ACTION_ID] = Array.Empty<string>()
        };

    /// <summary>
    /// The lists for one menu entry, in the order the pane draws them.
    /// </summary>
    /// <remarks>
    /// "Latest added" leads because that is the question MP2's own first list answered - "what arrived lately" -
    /// and since the server learned to sort by the date a title entered the library, it can be answered for real
    /// rather than by the stand-in by year that used to stand where it.
    /// </remarks>
    public static IReadOnlyList<HomeContentList> For(MainMenuEntry entry, IEnumerable<string>? configuredMediaLists = null)
    {
      // Entries without a media shelf have their own screen to show, and the pane says what pressing them does instead.
      if (entry == null || entry.Section == null)
        return Array.Empty<HomeContentList>();

      if (!Guid.TryParse(entry.HomeImageId, out Guid actionId))
        return Array.Empty<HomeContentList>();

      return ForAction(actionId, configuredMediaLists);
    }

    /// <summary>The configured Nereus list keys projected to rows the Avalonia home pane can represent.</summary>
    public static IReadOnlyList<HomeContentList> ForAction(Guid actionId, IEnumerable<string>? configuredMediaLists = null)
    {
      if (!DEFAULT_LIST_KEYS.TryGetValue(actionId, out string[]? defaultKeys))
        return Array.Empty<HomeContentList>();

      string[]? configuredKeys = FindConfiguredKeys(actionId, configuredMediaLists);
      IEnumerable<string> keys = configuredKeys ?? defaultKeys;
      List<HomeContentList> lists = new List<HomeContentList>();
      foreach (string key in keys)
      {
        HomeContentList? list = ForKey(actionId, key);
        if (list != null)
          lists.Add(list);
      }
      return lists;
    }

    private static string[]? FindConfiguredKeys(Guid actionId, IEnumerable<string>? configuredMediaLists)
    {
      string[]? configuredKeys = null;
      foreach (string setting in configuredMediaLists ?? Array.Empty<string>())
      {
        string[] parts = setting.Split(':');
        if (parts.Length == 2 && Guid.TryParse(parts[0].Trim(), out Guid configuredActionId)
            && configuredActionId == actionId)
          configuredKeys = parts[1].Split(',').Select(key => key.Trim()).ToArray();
      }
      return configuredKeys;
    }

    private static HomeContentList? ForKey(Guid actionId, string key)
    {
      bool series = actionId == SERIES_ACTION_ID;
      bool audio = actionId == AUDIO_ACTION_ID;
      bool images = actionId == IMAGES_ACTION_ID;
      bool video = actionId == VIDEOS_ACTION_ID;
      bool thumbnail = images || video || series && key == "LatestEpisodes";
      string layout = audio ? "square1" : thumbnail ? "thumbnail1" : "poster1";
      double width = audio ? 650 : thumbnail ? 900 : 760;

      switch (key)
      {
        case "LatestMovies":
        case "LatestEpisodes":
        case "LatestAudio":
        case "LatestImages":
        case "LatestVideo":
        case "LatestGames":
          if (audio)
            layout = "square1";
          return new HomeContentList("Latest added", "added", true, null, null, layout, width);
        case "ContinuePlayMovies":
        case "ContinuePlaySeries":
        case "ContinuePlayAlbum":
        case "ContinuePlayVideo":
          layout = audio ? "square2" : video ? "thumbnail2" : series ? "poster3" : "poster2";
          return new HomeContentList("Continue playing", "lastplayed", true, "started", null,
              layout, audio ? 650 : video ? 900 : 760);
        case "UnplayedMovies":
        case "UnplayedSeries":
        case "UnplayedAlbum":
        case "UnplayedImages":
        case "UnplayedVideo":
        case "UnplayedGames":
          layout = audio ? "square2" : video || images ? "thumbnail2" : "poster2";
          return new HomeContentList("Unplayed", "year", true, "unwatched", null,
              layout, audio ? 650 : video || images ? 900 : 760);
        case "FavoriteMovies":
        case "FavoriteSeries":
        case "FavoriteAudio":
        case "FavoriteImages":
        case "FavoriteVideo":
        case "FavoriteGames":
        case "FavoriteTV":
        case "FavoriteRadio":
        case "FavoriteApps":
        case "FavoriteOnlineVideoSites":
        case "TopPlayedWebradio":
          layout = audio ? "square1" : video || images ? "thumbnail1" : "poster1";
          return new HomeContentList("Favorites", null, false, null, "the server keeps no favourites over HTTP",
              layout, audio ? 650 : video || images ? 900 : 760);
        case "LastPlayTV":
        case "LastPlayRadio":
        case "LastPlayedGames":
        case "LastPlayedWebradio":
          return new HomeContentList("Latest played", null, false, null, "this list is not available over HTTP",
              "poster1", 760);
        case "LastLaunchApps":
        case "LastOnlineVideoSites":
          return new HomeContentList("Latest launched", null, false, null, "this list is not available over HTTP",
              "poster1", 760);
        case "CurrentTVPrograms":
        case "CurrentRadioPrograms":
          return new HomeContentList("Current programs", null, false, null, "this list is not available over HTTP",
              "thumbnail1", 900);
        case "CurrentSchedules":
          return new HomeContentList("Current schedules", null, false, null, "this list is not available over HTTP",
              "thumbnail1", 900);
        case "TopLanguagesOnlineVideoSites":
          return new HomeContentList("Top languages", null, false, null, "this list is not available over HTTP",
              "square1", 650);
        default:
          return null;
      }
    }
  }
}
