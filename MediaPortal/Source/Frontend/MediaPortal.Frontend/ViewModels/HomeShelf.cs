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
using MediaPortal.Frontend.Api;
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

  /// <summary>
  /// What one of the weather and news bands' cards says: words and numbers rather than pictures, because a
  /// forecast and a headline have no artwork to wear.
  /// </summary>
  /// <remarks>
  /// The reference drew these as templates of their own (<c>HomeContentStyles.xaml</c>'s
  /// <c>CurrentWeatherControlTemplate</c> and <c>DayForecastControlTemplate</c>, its <c>FeedDataTemplate</c>
  /// and <c>TopNewsDataTemplate</c>) and dressed every one of them like a media tile - the same plate, the
  /// same fading title band - so a card here carries exactly what those templates showed and stands on the
  /// wall like the covers beside it. All the text arrives spelled out: the temperature has already been put
  /// in the viewer's units, the day already in the viewer's language, and a card that had to ask the screen
  /// for either would leave the screen knowing the weather.
  /// </remarks>
  public sealed class HomeInfoCard
  {
    public HomeInfoCard(string label, string detail = "", string bigText = "", string sub = "",
        string? iconName = null, bool iconAtRight = false, string? badgeName = null,
        string kind = "plain", IReadOnlyList<CardFact>? facts = null,
        IReadOnlyList<WeatherSlot>? slots = null, WeatherAir? air = null, string? radarUrl = null,
        string sub2 = "", string? imageUrl = null, string? logoUrl = null, string? story = null,
        bool hero = false, string? feedName = null)
    {
      Label = label;
      Detail = detail;
      BigText = bigText;
      Sub = sub;
      Sub2 = sub2;
      IconName = iconName;
      IconAtRight = iconAtRight;
      BadgeName = badgeName;
      Kind = kind;
      Facts = facts;
      Slots = slots;
      Air = air;
      RadarUrl = radarUrl;
      ImageUrl = imageUrl;
      LogoUrl = logoUrl;
      Story = story;
      Hero = hero;
      FeedName = feedName;
    }

    /// <summary>The name the card's title band carries - the place, the day, the feed, the headline.</summary>
    public string Label { get; }

    /// <summary>The small line under the title: which feed a headline came from, how far a day swings.</summary>
    public string Detail { get; }

    /// <summary>The huge number - today's temperature, in the reference's own 90-point measure. Empty elsewhere.</summary>
    public string BigText { get; }

    /// <summary>The small lines under the big number, one per row; empty on a card that has no big number.</summary>
    public string Sub { get; }

    /// <summary>A third reading of the day card - the millimetres under the chance - so a hairline can stand
    /// between them; empty on every other card.</summary>
    public string Sub2 { get; }

    /// <summary>The skin picture the card wears, spelled as the skin files it; null on a card that wears none.</summary>
    public string? IconName { get; }

    /// <summary>Whether the card's picture stands to the right of its numbers (today) or alone in the middle (day, feed).</summary>
    public bool IconAtRight { get; }

    public bool HasRightIcon => IconName != null && IconAtRight;
    public bool HasCenteredIcon => IconName != null && !IconAtRight;

    /// <summary>The small mark stamped into the card's top right corner - the news badge. Null when there is none.</summary>
    public string? BadgeName { get; }

    public bool HasBadge => BadgeName != null;

    /// <summary>
    /// Which of the wall's card faces this card wears. The reference drew each information card in a template
    /// of its own - today's weather is not the day's forecast is not a headline - and the designed wall added
    /// two faces the reference never had: the hours chart ("Today") and the air-and-radar card, both spelled
    /// out in the design review. "plain" is the face every news card and every old-shaped card wears.
    /// </summary>
    public string Kind { get; }

    public bool IsToday => Kind == "today";
    public bool IsChart => Kind == "chart";
    public bool IsDayFace => Kind == "day";
    public bool IsAir => Kind == "air";
    public bool IsPlain => Kind == "plain";

    /// <summary>A feed's square: its mark in the middle, its name and how many it handed over below.</summary>
    public bool IsFeedTile => Kind == "feedtile";

    /// <summary>A headline standing on its story's own picture, the feed's mark in the corner.</summary>
    public bool IsNewsPicture => Kind == "news" && HasImage;

    /// <summary>A headline without a picture: the words themselves on the wall's own glass.</summary>
    public bool IsNewsWords => Kind == "news" && !HasImage;

    /// <summary>The small two-column rows the weather and air cards are written in: name left, value right.</summary>
    public IReadOnlyList<CardFact>? Facts { get; }

    public bool HasFacts => Facts != null && Facts.Count > 0;

    /// <summary>The three-hourly hours the "Today" chart draws; null on every card that is not a chart.</summary>
    public IReadOnlyList<WeatherSlot>? Slots { get; }

    /// <summary>The air the place is breathing; null where it was not asked for or did not arrive.</summary>
    public WeatherAir? Air { get; }

    /// <summary>The index as a bare number - the huge figure on the air card. Empty when there is no air.</summary>
    public string AqiNumber => Air == null ? string.Empty : Air.Aqi.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The index in its one word ("good", "poor"). Empty when there is no air.</summary>
    public string AqiGrade => Air == null ? string.Empty : AirGrades.GradeOf(Air.Aqi);

    private Avalonia.Media.IBrush? _aqiBrush;

    /// <summary>The index's own colour - green for good air, red for bad - for the number and its dot alike.</summary>
    public Avalonia.Media.IBrush AqiBrush => _aqiBrush ??= Air == null
        ? Avalonia.Media.Brushes.Gray
        : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(AirGrades.ColorHexOf(Air!.Aqi)));

    /// <summary>The rain radar's picture address; null on every card that does not show the radar.</summary>
    public string? RadarUrl { get; }

    public bool HasRadar => RadarUrl != null;

    /// <summary>
    /// The story's own picture, asked of the internet by the tile it stands on; null on every card that
    /// shows no photograph. A headline whose feed names no picture wears the word face instead - the wall's
    /// own glass, the same ground a weather card stands on.
    /// </summary>
    public string? ImageUrl { get; }

    public bool HasImage => ImageUrl != null;

    /// <summary>
    /// The little mark of whoever speaks - the feed's logo in a headline's corner chip and in the middle of
    /// a feed's square. Null where the feed names neither a channel image nor an address to ask one from;
    /// the card then paints the initial instead, which is what the chip and the square do while the picture
    /// is on its way just the same.
    /// </summary>
    public string? LogoUrl { get; }

    public bool HasLogo => LogoUrl != null;

    /// <summary>The reverse, so a square paints its initial exactly where no mark was named - converterless.</summary>
    public bool NoLogo => LogoUrl == null;

    /// <summary>The words under a headline - the feed's summary - on the places that show them. Null elsewhere.</summary>
    public string? Story { get; }

    public bool HasStory => !string.IsNullOrWhiteSpace(Story);

    /// <summary>Whether this headline stands on one of the mosaic's big places; false on every other card.</summary>
    public bool Hero { get; }

    /// <summary>The reverse, so a template can hide what a hero hides without a converter of its own.</summary>
    public bool NotHero => !Hero;

    /// <summary>Whoever carried the story; the word a headline's chip prints and takes its initial from.</summary>
    public string? FeedName { get; }

    /// <summary>The feed's first letter, which is what a chip or a square paints where a logo did not arrive.</summary>
    public string Glyph => FeedName != null && FeedName.Length > 0
        ? FeedName.Substring(0, 1).ToUpperInvariant()
        : (Label.Length > 0 ? Label.Substring(0, 1).ToUpperInvariant() : "");
  }

  /// <summary>
  /// One row of a card's fact list: the thing's name on the left, its value on the right, both already in the
  /// viewer's units and language - the same rule the rest of the wall holds, that a card arrives spelled out.
  /// </summary>
  public sealed record CardFact(string Name, string Value);

  /// <summary>
  /// The air index told in words and colour, the way every air application tells it: the endpoint answers a
  /// bare 1..5 and the wall says "good · green" beside the number. The six words and six colours are the
  /// scale's own, not a skin's.
  /// </summary>
  public static class AirGrades
  {
    private static readonly string[] GRADES = { "", "good", "fair", "moderate", "poor", "very poor", "extreme" };
    private static readonly string[] COLORS =
    {
      "", "#00E400", "#E8D44D", "#FF9900", "#FF1E1E", "#8F3F97", "#7E0023"
    };

    public static string GradeOf(int aqi)
    {
      return aqi >= 1 && aqi < GRADES.Length ? GRADES[aqi] : "unknown";
    }

    public static string ColorHexOf(int aqi)
    {
      return aqi >= 1 && aqi < COLORS.Length ? COLORS[aqi] : "#9E9E9E";
    }
  }

  /// <summary>
  /// What a weather reader answers: the place the brief is for, its sky now, and the days ahead - plus, since
  /// the designed wall, the next hours standing on their own, the air the place is breathing (null when it
  /// refused), and the radar picture's address (null when there was no key to ask with).
  /// </summary>
  public sealed record WeatherWallBrief(string Place, WeatherNow? Now, IReadOnlyList<WeatherDay> Days, bool Metric,
      IReadOnlyList<WeatherSlot>? Slots = null, WeatherAir? Air = null, string? RadarUrl = null);

  /// <summary>One feed's standing in the news brief: how many headlines it handed over, or why it did not,
  /// and the mark it speaks under - its channel image, or the site's own favicon where it names none.</summary>
  public sealed record FeedWallRow(string Name, int HeadlineCount, string? Error, string? LogoUrl = null);

  /// <summary>What a news reader answers: every feed that was asked, and the newest headlines across them.</summary>
  public sealed record NewsWallBrief(IReadOnlyList<FeedWallRow> Feeds, IReadOnlyList<NewsHeadline> Headlines);

  /// <summary>A single focusable tile in the horizontal Nereus home-content wall.</summary>
  public sealed class HomeContentTile
  {
    private HomeContentTile(TitleViewModel? title, MainMenuEntry? menuEntry, NereusHomeShortcut? shortcut,
        HomeRow? note, HomeInfoCard? card, double x, double y, double width, double height)
    {
      Title = title;
      MenuEntry = menuEntry;
      Shortcut = shortcut;
      Note = note;
      Card = card;
      X = x;
      Y = y;
      Width = width;
      Height = height;
    }

    public TitleViewModel? Title { get; }
    public MainMenuEntry? MenuEntry { get; }
    public NereusHomeShortcut? Shortcut { get; }
    public HomeRow? Note { get; }

    /// <summary>
    /// What a weather or news card says, on the one kind of square that carries words rather than a picture.
    /// The reference's day, feed and headline tiles were focusable and did nothing when pressed - their screen
    /// was one press of the entry's own shortcut away - and a card here keeps that: it lights its edge, and a
    /// press of it asks nothing of anybody.
    /// </summary>
    public HomeInfoCard? Card { get; }

    /// <summary>
    /// The band this square stands in, which is the answer to "where does a viewer come back to when they leave
    /// this band and return to it". The reference keeps that per list - <c>ItemsControl.RestoreFocus</c> remembers
    /// the item that last had the focus and offers it again when the focus re-enters the list - and this wall is
    /// one wall of several bands rather than one list apiece, so the memory hangs on the band here. Set by the
    /// band itself as it lays the square out; nobody else.
    /// </summary>
    public HomeContentGroup? Group { get; internal set; }

    // Where the band puts the square. The band is planned for the covers it holds before it reaches the wall, so
    // a square is placed once, stands still, and never moves under the viewer's eyes. See HomeContentGroup.
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public bool IsTitle => Title != null;

    /// <summary>
    /// Whether this square is one of the leftover plugins. It is the only thing a menu entry is ever put on the
    /// wall for - the entries beside the main menu have their own tiles, and the menu editor moves entries
    /// between them - so "a menu entry's square" and "a plugin plaque" are the same square, and the plaque is
    /// what the reference dresses it in: the plugin's own picture stretched over a coloured ground, its name in
    /// a dark band, nothing else.
    /// </summary>
    public bool IsPlugin => MenuEntry != null;

    /// <summary>A cover, or the one line of explanation a band falls back to. Everything the wall draws that is
    /// neither an icon of glass, a plugin plaque nor a card of words.</summary>
    public bool IsCover => !IsShortcut && !IsPlugin && !IsCard;

    public bool IsCard => Card != null;

    public bool IsShortcut => Shortcut != null;
    public bool IsNote => Note != null;
    public bool IsFocusable => !IsNote;

    /// <summary>
    /// Whether the square has a picture of its own to stand on: a cover's artwork. A shortcut never has one (it
    /// is drawn, not photographed), a plugin plaque carries its picture over a ground of the face's own colour
    /// and needs no frost behind it, and a cover whose fanart has not arrived has none either - which is the one
    /// case the tile frosts itself for, since a plate carrying nothing but a name would read as a hole in the
    /// wall rather than as a tile waiting for its picture.
    /// </summary>
    public bool HasPicture => Title != null && Title.HasArtwork;

    public bool HasNoPicture => !HasPicture;

    public string Label => Title?.Name ?? MenuEntry?.Label ?? Shortcut?.Label ?? Card?.Label ?? Note?.Text ?? string.Empty;
    public string Detail => Title?.Detail ?? MenuEntry?.Detail ?? string.Empty;

    /// <summary>
    /// The icon a shortcut tile wears, by the name the skin files it under. Only a shortcut has one: a cover is
    /// its own picture and a menu entry carries its own, so the name is null for every tile but the icons'.
    /// </summary>
    public string? IconName => Shortcut?.IconName;

    /// <summary>
    /// The edge the shortcut's glyph is drawn at: thirty-two hundredths of the tile - the icon is still the
    /// tile's subject and not a stamp on it, but it stands back a fifth from its old size because the name
    /// under it shares the glass and the two together used to crowd the lower half of the tile. Taken from the
    /// tile's own width - which already follows the screen height - so the icon grows and shrinks with the
    /// square it stands in and needs no box around it to stay square.
    /// </summary>
    public double IconSize => System.Math.Round(Width * 0.32, 1);

    internal static HomeContentTile ForTitle(TitleViewModel title, double x, double y, double width, double height)
    {
      return new HomeContentTile(title, null, null, null, null, x, y, width, height);
    }

    internal static HomeContentTile ForMenuEntry(MainMenuEntry entry, double x, double y, double width, double height)
    {
      return new HomeContentTile(null, entry, null, null, null, x, y, width, height);
    }

    internal static HomeContentTile ForShortcut(NereusHomeShortcut shortcut, double x, double y, double width, double height)
    {
      return new HomeContentTile(null, null, shortcut, null, null, x, y, width, height);
    }

    internal static HomeContentTile ForNote(HomeRow note, double x, double y, double width, double height)
    {
      return new HomeContentTile(null, null, null, note, null, x, y, width, height);
    }

    internal static HomeContentTile ForCard(HomeInfoCard card, double x, double y, double width, double height)
    {
      return new HomeContentTile(null, null, null, null, card, x, y, width, height);
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

    /// <summary>
    /// The room between two squares of a shortcut or plugin column, the same ten that lies between two covers
    /// sideways. It is subtracted from the column's span before the squares are cut, so a rail of five ends on
    /// the bottom line of the band instead of running past it.
    /// </summary>
    private const double RAIL_GAP = 10;

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

    /// <summary>
    /// The mosaic the reference lays its "Others" band out in: nineteen places in seven columns and four rows,
    /// the first, sixth and thirteenth square twice as wide and high as the rest, in the order the reference
    /// names them - which is not reading order, and stays as it is because the order is the road the remote's
    /// ring walks. Tiled without a hole: every one of the twenty-eight cells is covered exactly once.
    /// </summary>
    private static readonly (int Column, int Row, int ColumnSpan, int RowSpan)[] SQUARE_LAYOUT_5 =
    {
        (0, 0, 2, 2), (2, 0, 1, 1), (2, 1, 1, 1),
        (0, 2, 1, 1), (0, 3, 1, 1), (1, 2, 2, 2),
        (3, 0, 1, 1), (3, 1, 1, 1), (3, 2, 1, 1), (3, 3, 1, 1),
        (4, 0, 1, 1), (5, 0, 1, 1), (4, 1, 2, 2),
        (4, 3, 1, 1), (5, 3, 1, 1),
        (6, 0, 1, 1), (6, 1, 1, 1), (6, 2, 1, 1), (6, 3, 1, 1)
    };

    /// <summary>
    /// How many plugins the "Others" band has places for, which is what the reference's own grid answers: its
    /// <c>SquareTileLayout5Template> is built out of nineteen controls and <c>ListToGridConverter> hands the whole
    /// list to that one grid, so a twentieth leftover plugin has no square to stand on and is not shown. The
    /// place a leftover wants is made in the menu editor, where it is assigned to a menu entry.
    /// </summary>
    public const int PLUGINS_BAND_SLOTS = 19;

    /// <summary>The name of that mosaic's table, which is what the band of leftover plugins is built with.</summary>
    public const string PLUGINS_LAYOUT = "square5";

    /// <summary>
    /// How many feeds the "cardfeed" grid has squares for: six, because the mockup the viewer approved sets its
    /// feed grid as "repeat(2,188px)" columns by "repeat(3,188px)" rows filled column first - two columns of
    /// three squares and no more. A seventh feed has no square and is not shown, the same answer the plugins'
    /// mosaic gives a twentieth plugin.
    /// </summary>
    public const int FEED_BAND_SLOTS = 6;

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

    /// <summary>
    /// Where a weather or news card stands. The reference gave each of these its own measure rather than a
    /// mosaic: today's weather is the whole of a Narrow group for itself, the forecast is four posters side by
    /// side in a Wide one, the feeds are a VeryNarrow column of nine squares, and the top news takes the same
    /// nineteen-place mosaic as the leftover plugins. So a band of cards keeps its table whatever it holds -
    /// four columns of cards, and the days that stayed away leave their column empty the way a short media
    /// band's missing covers leave theirs.
    /// </summary>
    private static readonly (int Column, int Row, int ColumnSpan, int RowSpan)[] CARD_LAYOUT_1 =
    {
        (0, 0, 3, 4)
    };

    private static readonly (int Column, int Row, int ColumnSpan, int RowSpan)[] CARD_LAYOUT_4 =
    {
        (0, 0, 1, 4), (1, 0, 1, 4), (2, 0, 1, 4), (3, 0, 1, 4)
    };

    private static readonly (int Column, int Row, int ColumnSpan, int RowSpan)[] CARD_LAYOUT_9 =
    {
        (0, 0, 1, 1), (1, 0, 1, 1), (2, 0, 1, 1),
        (0, 1, 1, 1), (1, 1, 1, 1), (2, 1, 1, 1),
        (0, 2, 1, 1), (1, 2, 1, 1), (2, 2, 1, 1)
    };

    /// <summary>
    /// The "Today" chart's band: one card, the whole of it, because an hours chart cannot be cut into
    /// columns - its own columns are the hours. Four cells wide only so that the card ends with the same
    /// ten-pixel air beside it as every other band's square.
    /// </summary>
    private static readonly (int Column, int Row, int ColumnSpan, int RowSpan)[] CARD_LAYOUT_CHART =
    {
        (0, 0, 4, 4)
    };

    /// <summary>
    /// The forecast as the design redrew it: two columns of two days, each day a narrow upright - the day's
    /// name, its picture between name and numbers, and precipitation in its own two lines. Four days stand,
    /// the same four the old row of posters held.
    /// </summary>
    private static readonly (int Column, int Row, int ColumnSpan, int RowSpan)[] CARD_LAYOUT_DAY4 =
    {
        (0, 0, 1, 2), (1, 0, 1, 2), (0, 2, 1, 2), (1, 2, 1, 2)
    };

    private readonly int _columns;
    private readonly int _rows;
    private readonly (int Column, int Row, int ColumnSpan, int RowSpan)[] _layout;
    private readonly double _inset = 12;
    private readonly double _cellWidth;
    private readonly double _cellHeight;

    /// <summary>The side of one feed square in the "cardfeed" grid - a square is a square, not a cell.</summary>
    private readonly double _feedSquare;

    /// <summary>How many feed squares stand in one column of the "cardfeed" grid before the next column starts.</summary>
    private readonly int _feedRows;

    /// <summary>
    /// The square a shortcut is cut at. Not a number kept for itself: a column of five at the
    /// old 159 ended twelve lines below the band's own height and twenty-nine below the lowest cover of the
    /// mosaic beside it, so the rail hung past the wall it belongs to - the one misalignment a viewer sees at a
    /// glance, because the bottom line of the content is the line every band is read against. Cut instead from
    /// the room the covers leave, the column stands on the same line as the first cover and finishes on the same
    /// line as the last one, whatever the band's height says.
    /// </summary>
    private readonly double _railTileSize;

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

      if (layout == "shortcuts")
      {
        _columns = 1;
        _rows = 5;
        _layout = Array.Empty<(int, int, int, int)>();
        _feedSquare = 0;
        _feedRows = 0;

        // The room the mosaic's squares have for themselves: from the line their first row stands on down to the
        // line the lowest cover of a full band ends on (a cover stands five off its cell, and the band keeps
        // twelve of inset below the last one). Five shortcuts at the square this column used to insist on ran
        // past both lines, so the rail hung below the wall; cut from this span instead, the column starts and
        // finishes on the very lines the covers beside it use. A rail shorter than the span it was drawn for
        // keeps its nominal square and stands at the top of the column - five tiles grown to fill a band built
        // for two would be a column of plaques, not a rail of shortcuts.
        double span = Height - _inset - 5 - (CONTENT_TOP + 5);
        _railTileSize = covers > 1
            ? Math.Min(159.0, Math.Round((span - (covers - 1) * RAIL_GAP) / covers, 1))
            : 159.0;
        return;
      }

      // The feeds as the mockup the viewer approved drew them: two columns of squares, filled from the top
      // down because the columns are the feeds and not the rows, three squares to a column - the mockup's
      // own "repeat(3,188px)" - so six feeds stand and a seventh is left out, the way the reference leaves
      // the twentieth plugin out of its nineteen-place mosaic. A square is cut from the column's own width,
      // so the grid reads as tiles and not as stretched cells.
      if (layout == "cardfeed")
      {
        _columns = 2;
        _layout = Array.Empty<(int, int, int, int)>();
        _feedSquare = Math.Round((Width - _inset * 2) / 2 - 10, 1);
        _feedRows = FEED_BAND_SLOTS / 2;
        _rows = _feedRows;
        _cellWidth = (Width - _inset * 2) / _columns;
        _cellHeight = _feedSquare + 10;
        _railTileSize = 0;
        return;
      }

      (_columns, _rows, _layout) = layout switch
      {
        "poster2" => (4, 3, POSTER_LAYOUT_2),
        "poster3" => (4, 3, POSTER_LAYOUT_3),
        "square1" => (3, 4, SQUARE_LAYOUT_1),
        "square2" => (3, 4, SQUARE_LAYOUT_2),
        "square5" => (7, 4, SQUARE_LAYOUT_5),
        "thumbnail1" => (3, 4, THUMBNAIL_LAYOUT_1),
        "thumbnail2" => (3, 4, THUMBNAIL_LAYOUT_2),
        "card1" => (3, 4, CARD_LAYOUT_1),
        "card4" => (4, 4, CARD_LAYOUT_4),
        "card9" => (3, 3, CARD_LAYOUT_9),
        "cardchart" => (4, 4, CARD_LAYOUT_CHART),
        "cardday4" => (2, 4, CARD_LAYOUT_DAY4),
        _ => (4, 3, POSTER_LAYOUT_1)
      };

      _cellWidth = (Width - _inset * 2) / _columns;
      _cellHeight = (Height - CONTENT_TOP - _inset) / _rows;

      // The leftover plugins' band is its mosaic whatever the server answered. The other bands pack their
      // squares from the left when the covers stay away, because a mosaic of six with four places kept warm
      // reads as a hole between the bands beside it; the plugins' grid is the opposite case - the reference
      // builds it out of nineteen fixed squares and hands the list to that one grid, so a fifth leftover
      // stands on the fifth square, a big one, and the empty squares are the wall's air, not a gap to close.
      // Packed instead, three leftovers became a row of slivers the viewer called a list, which is exactly
      // the face this band was drawn to not wear.
      _designedMosaic = covers >= _layout.Length || string.Equals(layout, PLUGINS_LAYOUT, StringComparison.Ordinal);
      _railTileSize = 0;
      _feedSquare = 0;
      _feedRows = 0;

      // The big first cover is the face's own, but only while the covers beside it arrive too: a cover over two
      // cells stands over 2 * columns - 4 cells in the row below it, and without those covers the cells it
      // should stand over are the gap again, only bigger.
      // A band of cards never gets it: a feed grid or a row of days is the reference's equal measure, not a
      // mosaic with a hero square - nine feeds are nine alike squares, and the first is no bigger than the last.
      _firstCoverSpan = !_designedMosaic && !layout.StartsWith("card", StringComparison.Ordinal)
          && covers > 1 && _columns >= 2 && _rows >= 2
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

    public void AddCard(HomeInfoCard card)
    {
      AddTile((x, y, width, height) => HomeContentTile.ForCard(card, x, y, width, height));
    }

    public void AddNote(HomeRow note)
    {
      AddSquareTo(HomeContentTile.ForNote(note, _inset, CONTENT_TOP + 5, Width - _inset * 2, 80));
      OccupiedWidth = Width;
    }

    /// <summary>The lowest point a square of this band reaches, which is what decides the band's box.</summary>
    private void AddSquareTo(HomeContentTile tile)
    {
      tile.Group = this;
      _bottom = Math.Max(_bottom, tile.Y + tile.Height);
      Tiles.Add(tile);
    }

    /// <summary>
    /// Puts one more square into the band, in the place the band's plan has for it, and stretches the band no
    /// wider than that square. A square is placed once and never moves.
    /// </summary>
    private void AddTile(Func<double, double, double, double, HomeContentTile> create)
    {
      if (Layout == "cardfeed")
      {
        // Column first: the first squares stand under each other in the left column, and only when it is
        // full do they begin at the top of the right one - the feeds are read down, not across.
        int place = Tiles.Count;
        if (_feedRows <= 0 || place >= _columns * _feedRows)
          return;

        double fx = _inset + (place / _feedRows) * (_feedSquare + 10) + 5;
        double fy = CONTENT_TOP + 5 + (place % _feedRows) * (_feedSquare + 10);
        _right = Math.Max(_right, fx + _feedSquare);
        AddSquareTo(create(fx, fy, _feedSquare, _feedSquare));
        OccupiedWidth = Math.Min(Width, _right + _inset);
        return;
      }

      if (Layout == "shortcuts")
      {
        double size = _railTileSize;
        double rail = 8;
        int index = Tiles.Count;
        // The first shortcut stands on the line the first cover of a mosaic stands on, and the ones under it
        // follow ten apart - the same ten that stands between two covers sideways. Sized off the band's own span
        // (see _railTileSize), the last one of the column ends where the lowest cover beside it ends.
        double top = CONTENT_TOP + 5 + index * (size + RAIL_GAP);
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
        // A mosaic has exactly as many places as the face drew squares, and a square beyond the last place has
        // nowhere to stand but on top of another one. The reference answers the same way: its grid is built out of
        // a fixed number of controls and one of its home contents hands that grid the whole list to fill them, so
        // what does not fit is not shown. Left out, then - never wrapped round onto the first cell.
        if (square >= _layout.Length)
          return;

        (column, row, columnSpan, rowSpan) = _layout[square];
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

    private HomeContentList(string key, string heading, string? sort, bool descending, string? seen,
        string? missing, string layout, double width)
    {
      Key = key;
      Heading = heading;
      Sort = sort;
      Descending = descending;
      Seen = seen;
      Missing = missing;
      Layout = layout;
      Width = width;
    }

    /// <summary>
    /// The word the settings file uses for this list, the same word MP2 wrote into
    /// <c>NereusSkinSettings.HomeMenuActionMediaLists</c>. The pane never looks at it - the home menu editor offers a
    /// list by its heading and stores it under this key.
    /// </summary>
    public string Key { get; }

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

    /// <summary>
    /// Every list one home menu action has, in the order the pane would draw them.
    /// </summary>
    /// <remarks>
    /// This is what the reference editor read from <c>AbstractHomeContent.Lists</c> when it built its list of
    /// possible entries (<c>MenuEditModel.GetAllMediaLists</c>): the tile's content decides what can be offered, the
    /// viewer decides what is shown. Here the unconfigured set is exactly that catalogue, so asking for it is
    /// asking for the tile's own lists with nothing configured away.
    /// </remarks>
    public static IReadOnlyList<HomeContentList> AvailableFor(Guid actionId) => ForAction(actionId);

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
          return new HomeContentList(key, "Latest added", "added", true, null, null, layout, width);
        case "ContinuePlayMovies":
        case "ContinuePlaySeries":
        case "ContinuePlayAlbum":
        case "ContinuePlayVideo":
          layout = audio ? "square2" : video ? "thumbnail2" : series ? "poster3" : "poster2";
          return new HomeContentList(key, "Continue playing", "lastplayed", true, "started", null,
              layout, audio ? 650 : video ? 900 : 760);
        case "UnplayedMovies":
        case "UnplayedSeries":
        case "UnplayedAlbum":
        case "UnplayedImages":
        case "UnplayedVideo":
        case "UnplayedGames":
          layout = audio ? "square2" : video || images ? "thumbnail2" : "poster2";
          return new HomeContentList(key, "Unplayed", "year", true, "unwatched", null,
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
          return new HomeContentList(key, "Favorites", null, false, null, "the server keeps no favourites over HTTP",
              layout, audio ? 650 : video || images ? 900 : 760);
        case "LastPlayTV":
        case "LastPlayRadio":
        case "LastPlayedGames":
        case "LastPlayedWebradio":
          return new HomeContentList(key, "Latest played", null, false, null, "this list is not available over HTTP",
              "poster1", 760);
        case "LastLaunchApps":
        case "LastOnlineVideoSites":
          return new HomeContentList(key, "Latest launched", null, false, null, "this list is not available over HTTP",
              "poster1", 760);
        case "CurrentTVPrograms":
        case "CurrentRadioPrograms":
          return new HomeContentList(key, "Current programs", null, false, null, "this list is not available over HTTP",
              "thumbnail1", 900);
        case "CurrentSchedules":
          return new HomeContentList(key, "Current schedules", null, false, null, "this list is not available over HTTP",
              "thumbnail1", 900);
        case "TopLanguagesOnlineVideoSites":
          return new HomeContentList(key, "Top languages", null, false, null, "this list is not available over HTTP",
              "square1", 650);
        default:
          return null;
      }
    }
  }
}
