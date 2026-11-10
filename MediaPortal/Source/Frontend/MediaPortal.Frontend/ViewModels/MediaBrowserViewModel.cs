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
using Avalonia.Controls;
using Avalonia.Threading;
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
    Cover,

    /// <summary>
    /// The same covers in a <em>stack</em>: the stood one in front, the ones still to come standing one behind
    /// the other to its right, the ones already walked gone out of the picture.  Nothing about the screen
    /// changes - only the way the row of covers is put down, and the fact that the stack holds its place in the
    /// middle instead of sliding along the floor.
    /// </summary>
    Stacked,

    /// <summary>
    /// The same covers in a <em>carousel</em>: the stood one facing the viewer, its neighbours turned away from
    /// it and overlapping it, which is the shape every cover wall was ever photographed in.
    /// </summary>
    Carousel
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
    /// The name the record above this row carries, when the shelf stands inside one: the tracks of an album are
    /// listed inside that album, and its name is what the importer wrote in front of every one of their names.
    /// Null for a row on a whole shelf.
    /// </summary>
    /// <remarks>
    /// This is a hint, not the gate: the importer wrote the tune's prefix from the tag's own word while the album's
    /// library title carries the year as well ("Wasting Light" against "Wasting Light (2011)"), so
    /// <see cref="TrackNameOnly"/> falls back to the number alone wherever the two words disagree.  That is how the
    /// panel's heading has always read a tune's name, and a song on a whole shelf is cut by the same reading.
    /// </remarks>
    internal string? InsideName
    {
      get { return _insideName; }
      set
      {
        if (_insideName == value)
          return;
        _insideName = value;
        // The name of the row above is what turns a season's or an episode's title into the series they belong
        // to, and what lets an episode be numbered by its number alone, so the row is read again as a whole.
        Raise();
      }
    }

    string? _insideName;

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
    private readonly ServerApiClient? _stream;

    public TitleViewModel(MediaTitle title, bool holdsTitles = false, ServerApiClient? stream = null,
        string? readThrough = null)
    {
      _title = title;
      _holdsTitles = holdsTitles;
      _stream = stream;
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
    /// The short films this title advertises itself with, in the order the library named them. Empty when the
    /// library knows none, which is the usual case: a trailer has to be tagged in the title's own file or stand
    /// in its folder under one of the names every tagging tool uses.
    /// </summary>
    public IReadOnlyList<MediaTrailer> Trailers
    {
      get { return _title.Trailers ?? Array.Empty<MediaTrailer>(); }
    }

    /// <summary>
    /// Whether the library knows a trailer of this title, which is what decides whether the question about the
    /// title offers a line for one.
    /// </summary>
    public bool HasTrailers
    {
      get { return _title.Trailers != null && _title.Trailers!.Count > 0; }
    }

    /// <summary>
    /// What to hand the player for one of this title's trailers: the file on this machine when this machine can
    /// open it, otherwise the address the server named.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same order of preference as <see cref="PlaybackLocation"/> and for the same reason: whoever holds the
    /// bytes should be the one handing them over, and a trailer is two minutes of picture nobody needs to fetch
    /// across a network when it is sitting in the folder next to them.
    /// </para>
    /// <para>
    /// Nothing comes back for a trailer whose address the server could not make sense of, or for a web address
    /// this machine cannot read as an address. The screen naming it says that out loud - see
    /// <see cref="MediaTitleActions.WordsFor"/>.
    /// </para>
    /// </remarks>
    public MediaLocation? TrailerLocation(MediaTrailer trailer)
    {
      if (trailer == null || !trailer.IsPlayable)
        return null;

      // A file trailer is named by the library in the server's own serialization, which this machine can or
      // cannot open; TryAsLocalFile is the one place that knows which, and it also says why not.
      if (trailer.Kind == 1 &&
          MediaLibraryPath.TryAsLocalFile(trailer.Source, out string localPath, out string? _))
        return MediaLocation.FromFile(localPath);

      string address = trailer.Address!;
      if (!address.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
      {
        // A web address, handed to the player as it stands. A trailer tagged by hand can hold anything, so an
        // address nothing can read as an address is turned into nothing rather than into an exception.
        return Uri.TryCreate(address, UriKind.Absolute, out Uri? web) ? MediaLocation.FromUrl(web) : null;
      }

      // The server's own address for a file it holds. Without a server to ask there is nothing to hand over.
      return _stream == null ? null : MediaLocation.FromUrl(new Uri(_stream.BaseUrl, address));
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
        Resume = after.Resume,
        // Folded only when the server named one: an answer from a request that named nobody carries no
        // percentage, and "nobody asked" must not overwrite the figure this row already had.
        Percentage = after.Percentage ?? _title.Percentage
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

        // The importer names an audio file "<record>: <number> - <song>" - measured on this library again after
        // the first try below failed on the live server: "Wasting Light (2011)" is the album's title, while its
        // first tune is titled "Wasting Light: 1 - Bridge Burning", so the record's word in the tune's name is the
        // bare tag word WITHOUT the year the importer stuck onto the album. Which is a name written for a shelf
        // holding every song the house has; a viewer standing inside the record has its cover, its heading and its
        // number in front of them already. So the row says the tune's own name, by the very same reading the
        // panel's heading uses - see <see cref="TrackNameOnly"/>.
        string named = string.IsNullOrWhiteSpace(Title.Title) ? "Untitled" : Title.Title!.Trim();
        if (MediaCatalog.IsMusic(ReadThrough) && Title.Track is int number)
          named = TrackNameOnly(named, YearlessName(InsideName ?? ""), number);
        if (MediaCatalog.IsEpisode(ReadThrough))
          named = EpisodeNameOnly(named, SeriesName);
        return named;
      }
    }

    /// <summary>
    /// The name with one trailing year taken out: "(2025)" at the end of a name is the year the facts row says
    /// anyway, and the reference's title line never carried it. Any other bracket stands, because a name like
    /// "Studio 54 (Live)" is that name and not a year.
    /// </summary>
    internal static string YearlessName(string name)
    {
      if (name.Length < 7 || !name.EndsWith(")", StringComparison.Ordinal))
        return name;
      int open = name.LastIndexOf(" (", StringComparison.Ordinal);
      if (open < 0 || name.Length != open + 7)
        return name;
      for (int digit = open + 2; digit < open + 6; digit++)
        if (!char.IsDigit(name[digit]))
          return name;
      return name.Substring(0, open).TrimEnd();
    }

    /// <summary>
    /// The name a tune is called by, without the record it lies on and the number it lies at.
    /// </summary>
    /// <param name="name">The name the library holds for the tune.</param>
    /// <param name="album">The record's word, where the screen knows it; empty when it does not.</param>
    /// <param name="placed">The tune's own number, or <c>null</c> where the library never numbered it.</param>
    /// <remarks>
    /// <para>
    /// <b>Why the name has to be taken apart at all.</b> The library holds no bare tune name in the fields this
    /// screen is answered from: the importer writes the composed string through
    /// <c>TrackInfo.TRACK_FORMAT_STR</c> "{0}: {1} - {2}" (TrackInfo.cs:59, spelled at :660-661) into
    /// <c>MediaAspect.ATTR_TITLE</c> (:305), and that column is what the row and the panel's heading both read.
    /// The clean name does exist in the library - <c>AudioAspect.ATTR_TRACKNAME</c>, written one line later at
    /// :307 - but neither the title list nor the panel's answer hands it over, so this screen cannot read what it
    /// was never given. A server asked in words is the way to fix that properly; until then this is the reading.
    /// </para>
    /// <para>
    /// <b>Why this is an un-formatting and not a guess.</b> Nothing here hunts for a dash in a string it does not
    /// understand: the number in the middle is checked against the library's own number for this tune
    /// (<c>AudioAspect.ATTR_TRACK</c>, which the shelf's row carries), the cut is made only where a tune's own name
    /// is left standing behind it, and a title the importer never spelled that way - a bare file name, a tune with
    /// no number, a tagger that wrote the song alone - walks through untouched.
    /// </para>
    /// <para>
    /// <b>Why the record's word is only a hint.</b> The importer wrote the tune's prefix from the tag's own word,
    /// which is not always the album's library title: measured here, the album is titled "Wasting Light (2011)"
    /// while its first tune is "Wasting Light: 1 - Bridge Burning". Where the two words disagree the number alone
    /// decides, which is what the panel's heading has always done; the row and the heading read one name because
    /// they ask this one method.
    /// </para>
    /// </remarks>
    internal static string TrackNameOnly(string name, string? album, int? placed)
    {
      if (!placed.HasValue || string.IsNullOrWhiteSpace(name))
        return name;

      // The importer's two spellings, in its own order: the record with the place on it in front of the tune when
      // the tag knew the record, and the bare place in front of the tune when it did not (TrackInfo.cs:660-665).
      string place = placed.Value.ToString(CultureInfo.InvariantCulture);
      string alone = BehindPlace(name, album ?? "", place);
      return string.IsNullOrEmpty(alone) ? name : alone;
    }

    /// <summary>
    /// What stands behind the record and the place in a tune's spelled title - <c>record: number - tune</c> or
    /// <c>number - tune</c> - or an empty word when the title is not spelled that way with this number.
    /// </summary>
    private static string BehindPlace(string name, string album, string place)
    {
      // The record's name is used wherever the answer knows it, because then nothing is worked out: the importer's
      // whole prefix is rebuilt out of the library's own two words and matched against the title as one piece.
      if (!string.IsNullOrWhiteSpace(album))
      {
        string known = album.Trim() + ": " + place + " - ";
        if (name.StartsWith(known, StringComparison.Ordinal))
          return name.Substring(known.Length).Trim();
      }

      // Where the record's name was not answered, the number alone is what has to agree with the library.  A record
      // whose own name carries a colon is spelled with that colon first and the importer's mark after it, so the
      // LAST mark of this shape is the importer's.
      string marked = ": " + place + " - ";
      int cut = name.LastIndexOf(marked, StringComparison.Ordinal);
      if (cut > 0)
        return name.Substring(cut + marked.Length).Trim();

      string bare = place + " - ";
      return name.StartsWith(bare, StringComparison.Ordinal) ? name.Substring(bare.Length).Trim() : "";
    }

    /// <summary>
    /// The episode's own name, taken out of the heading the importer wrote over it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What the library holds.</b> An episode's title column carries the whole sentence
    /// <c>EpisodeInfo.EPISODE_FORMAT_STR</c> "{0} S{1}E{2} - {3}" (EpisodeInfo.cs:67) - the series, the numbers and
    /// the name - which is a name written for a wall holding every episode the house has and needing to say whose
    /// it is in every row.  The bare name exists too (<c>EpisodeAspect.ATTR_EPISODE_NAME</c>, EpisodeAspect.cs:75)
    /// and the screen beside the list reads it, but the title list does not hand it over, so a row has to take the
    /// heading apart.  Same reason and same reading as <see cref="TrackNameOnly"/> for a tune.
    /// </para>
    /// <para>
    /// <b>Why this is an un-formatting and not a hunt for a dash.</b> The word in front of the separator is only
    /// believed when it ends on episode numbers - one word of the shape <c>S01E04</c>, or two when one file holds
    /// two episodes and the importer wrote both ("Prison Break - S04E23 S04E24 - ...") - and when what stands in
    /// front of those numbers is the series this row belongs to, which the shelf knows from the row above it.  An
    /// episode whose title was never spelled that way, or one whose series the shelf does not know, walks through
    /// with the name it was given.
    /// </para>
    /// </remarks>
    internal static string EpisodeNameOnly(string name, string series)
    {
      if (string.IsNullOrWhiteSpace(name))
        return name;

      int mark = name.LastIndexOf(" - ", StringComparison.Ordinal);
      if (mark <= 0)
        return name;

      string[] heading = name.Substring(0, mark).Trim().Split(' ');
      int counted = 0;
      for (int at = heading.Length - 1; at >= 0 && IsEpisodeMark(heading[at]); at--)
        counted++;
      if (counted == 0)
        return name;

      string before = string.Join(" ", heading, 0, heading.Length - counted).Trim(' ', '-', ':');
      string known = series.Trim(' ', '-', ':');
      if (known.Length > 0 && !string.Equals(before, known, StringComparison.Ordinal))
        return name;

      string own = name.Substring(mark + 3).Trim();
      return own.Length == 0 ? name : own;
    }

    /// <summary>
    /// Whether a word is one of the importer's episode numbers: <c>S01E04</c>, and nothing else.
    /// </summary>
    private static bool IsEpisodeMark(string word)
    {
      if (word.Length < 5 || (word[0] != 'S' && word[0] != 's'))
        return false;

      int at = 1;
      while (at < word.Length && char.IsDigit(word[at]))
        at++;
      if (at < 2 || at >= word.Length || (word[at] != 'E' && word[at] != 'e'))
        return false;

      int end = at + 1;
      while (end < word.Length && char.IsDigit(word[end]))
        end++;
      return end > at + 1 && end == word.Length;
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
    /// The name without the year the importer wrote onto its end. Importers name a series "Silo (2023)" while the
    /// row also carries the year as its own number; a screen that shows the number beside the name - or wants the
    /// name alone - says the year once and takes it off here. A trailing "(...)" that holds any other word stays
    /// part of the name, and so does everything the library gives no year for.
    /// </summary>
    public string NameWithoutYear
    {
      get
      {
        if (Title.Year is not int year)
          return Name;
        string worn = " (" + year.ToString(CultureInfo.InvariantCulture) + ")";
        return Name.EndsWith(worn, StringComparison.Ordinal)
            ? Name.Substring(0, Name.Length - worn.Length).TrimEnd()
            : Name;
      }
    }

    /// <summary>
    /// Whether this title was watched to the end: the viewer's own percentage at a hundred, and where the server
    /// named no percentage - an anonymous request has nobody's figure to give - the play count the library holds.
    /// </summary>
    /// <remarks>
    /// The two are not two rules but the reference's one rule with its own fallback: it reads the percentage of
    /// the person asking (<c>AbstractWatchedAction.cs:47-49</c>, which is also what decides which of its two
    /// watched entries a menu shows), and where no percentage was ever written it takes a play count above nought
    /// to mean a hundred (<c>PlayableMediaItem.cs:130-137</c>, <c>PlayableContainerMediaItem.cs:47-58</c>). The
    /// fallback is what an MP2 without profiles does, which is why a title marked through by one front end shows
    /// as watched to a second one that has not signed in.
    /// </remarks>
    public bool Seen
    {
      get
      {
        int? percentage = Title.Percentage;
        if (percentage.HasValue)
          return percentage.Value >= 100;
        return Title.PlayCount > 0;
      }
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
    /// the skin's own eye (<c>watched.svg</c>); the list keeps its quiet tick, and both read this one answer.
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
    /// <summary>
    /// Whether a tile may wear the partly-watched corner: somebody started it and the server knows how far - by
    /// the position it holds for this title, or by the figure it worked out of a container's children. A finished
    /// title is never partly one - the reference drew the full flag alone at a hundred percent, and two corners
    /// on one tile would say two different things about the same viewing.
    /// </summary>
    public bool IsPartlyWatched
    {
      get
      {
        if (Seen)
          return false;

        // A container is never paused - its episode, its track are - so it carries no position to return to, and
        // only its figure says anything about it. Both answers mean one thing to the corner: something was
        // watched here and is not finished.
        int? percentage = Title.Percentage;
        return percentage.HasValue && percentage.Value > 0
            || Title.Resume.HasValue && Title.Resume.Value > 0;
      }
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
    /// The name of the vector mark a tile without a picture wears: the medium the shelf holds, spelled as one
    /// of the skin's three media icons - audio, image or video. The shelf's aspect answers the question, since
    /// it is the aspect that says whether this wall is albums, pictures or films.
    /// </summary>
    public string FallbackIcon
    {
      get
      {
        if (string.Equals(ReadThrough, "AlbumItem", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ReadThrough, "AudioItem", StringComparison.OrdinalIgnoreCase))
          return "Media/audio";
        return string.Equals(ReadThrough, "ImageItem", StringComparison.OrdinalIgnoreCase)
            ? "Media/image"
            : "Media/video";
      }
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

    /// <summary>
    /// The series a season or an episode row belongs to, for the second line of the row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Neither kind of row carries the series' name of its own: a season is titled after the show
    /// ("Alien: Earth (2025) S01") and an episode after the show as well ("Alien: Earth S01E04 - Beobachtung",
    /// EpisodeInfo.EPISODE_FORMAT_STR:67), so the name has to be read back out of a sentence the importer wrote.
    /// A row never does that to its own name, where an importer spells a series however its tagger had it; it
    /// reads the name of the row it stands under, which for a season is the series' own row.
    /// </para>
    /// <para>
    /// An episode row stands under a season rather than under a series, and that row's name carries the season's
    /// number on its end as well as the series' name with its year behind it.  Both are taken off, the number
    /// first, because <see cref="YearlessName"/> only reads a year standing at the end of a word.  A shelf of
    /// episodes that was opened at nothing - a wall of every episode the house holds - answers nothing, because
    /// there is no name above the row to read.
    /// </para>
    /// </remarks>
    string SeriesName
    {
      get
      {
        string holding = (InsideName ?? "").Trim();
        if (MediaCatalog.IsEpisode(ReadThrough) && Title.Season.HasValue)
        {
          string mark = " S" + Title.Season.Value.ToString("00", CultureInfo.InvariantCulture);
          if (holding.EndsWith(mark, StringComparison.Ordinal))
            holding = holding.Substring(0, holding.Length - mark.Length);
        }
        return YearlessName(holding).Trim();
      }
    }

    /// <summary>
    /// The number this row stands at inside the thing above it, spelled as the plain number of it: <c>4</c> for
    /// the fourth episode.  Empty for every other kind of row and for an episode nobody numbered.
    /// </summary>
    /// <remarks>
    /// Inside one season the number is the whole address a viewer needs - the season is the screen they came
    /// from, and it says its own number, so writing it again in every row of it buries the one figure that
    /// changes from row to row.  <see cref="Position"/> still spells the season in front of it for the episodes
    /// of a wall that mixes every show of the house, where nothing above the row says which show this is.
    /// </remarks>
    public string EpisodeNumber
    {
      get
      {
        if (!MediaCatalog.IsEpisode(ReadThrough))
          return "";
        return Title.Episode.HasValue
            ? Title.Episode.Value.ToString(CultureInfo.InvariantCulture)
            : "";
      }
    }

    /// <summary>
    /// Whether this row of episodes stands under the season it belongs to, which is what lets it be numbered by
    /// its number alone.  A row knows that from the name of the row above it (see <see cref="InsideName"/>).
    /// </summary>
    bool StandsInsideItsSeason
    {
      get { return MediaCatalog.IsEpisode(ReadThrough) && !string.IsNullOrWhiteSpace(InsideName); }
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
        string what = Title.Year.HasValue && !SaysYearAlready(Name, Title.Year.Value)
            ? Name + " (" + Title.Year.Value + ")"
            : Name;
        return string.IsNullOrEmpty(where) ? what : where + " " + what;
      }
    }

    /// <summary>
    /// Whether the name already carries the year on its own end - <c>"Alien: Earth (2005) (2005)"</c> is the
    /// importers' doing (the tagger files a series under the name with the year ON it, and the year column
    /// agrees), and writing the year a second time says the same fact twice in one breath.
    /// </summary>
    private static bool SaysYearAlready(string name, int year)
    {
      return name.EndsWith(" (" + year.ToString(CultureInfo.InvariantCulture) + ")", StringComparison.Ordinal);
    }

    /// <summary>
    /// What a resting row says: where it sits in what holds it, then the name, and the year nowhere. The year
    /// belongs to the row a viewer stands on (see <see cref="FocusedLine"/>); behind every name of a wall of a
    /// thousand it is a bracket the eye walks past a thousand times to reach the next name.
    /// </summary>
    public string RestLine
    {
      get
      {
        string where = Position;
        return string.IsNullOrEmpty(where) ? NameWithoutYear : where + " " + NameWithoutYear;
      }
    }

    /// <summary>
    /// The stood-on row's first line: the name and, behind a dot, the year the library knows - spelled as an
    /// escape so it does not depend on this file's encoding, like <see cref="SeenMark"/>'s tick.
    /// </summary>
    public string FocusedLine
    {
      get
      {
        string what = Title.Year.HasValue && !SaysItsShow
            ? NameWithoutYear + " \u00B7 " + Title.Year.Value.ToString(CultureInfo.InvariantCulture)
            : NameWithoutYear;
        string where = Position;
        return string.IsNullOrEmpty(where) ? what : where + " " + what;
      }
    }

    /// <summary>
    /// Whether the row is one of a show, which does not repeat its year on the stood-on line.
    /// </summary>
    /// <remarks>
    /// The year of an episode is the year its episode was shown, and the year of a season is the year the season
    /// ran - both are said better by the show they belong to, and the second line of these rows is spent on that
    /// show.  A film stands for its year, so it keeps the year.
    /// </remarks>
    bool SaysItsShow
    {
      get
      {
        return MediaCatalog.IsSeries(ReadThrough) || MediaCatalog.IsSeason(ReadThrough)
            || MediaCatalog.IsEpisode(ReadThrough);
      }
    }

    /// <summary>
    /// The stood-on row's second line: the kinds the importer wrote on the title, which the list answer carries
    /// with every row (see <c>MediaController.ReadGenres</c>) - so the wall never waits on a second question.
    /// </summary>
    public string GenresLine
    {
      get
      {
        IReadOnlyList<string>? kinds = Title.Genres;
        if (kinds == null || kinds.Count == 0)
          return "";
        List<string> words = new List<string>();
        foreach (string kind in kinds)
        {
          if (!string.IsNullOrWhiteSpace(kind))
            words.Add(kind.Trim());
        }
        return string.Join(", ", words);
      }
    }

    /// <summary>
    /// What the stood-on row's second line says. For music it is who made the thing: an album answers with the
    /// record's album artist and a track with the voice on that track, which the server keeps in the same cell of
    /// each row because for a record that cell holds the album artist (measured: every one of this library's 43
    /// albums answers its artist, "Wasting Light (2011)" answers "Foo Fighters").  For a season and an episode it
    /// is the show they belong to, which is the one thing a row of a show never carries of its own.  For
    /// everything else it is the kinds the importer wrote, which is what a film is sorted by and what no song is.
    /// </summary>
    public string SecondLine
    {
      get
      {
        if (MediaCatalog.IsMusic(ReadThrough))
          return Artist.Trim();

        // A season's name is the series' name with the season's number written on it, so what is new under a
        // season row is the series; an episode's row carries its show's number on its first line but nothing that
        // says which season it stands in, so it says that behind the show's name.
        if (MediaCatalog.IsSeason(ReadThrough))
          return SeriesName;
        if (MediaCatalog.IsEpisode(ReadThrough))
        {
          string show = SeriesName;
          if (show.Length == 0)
            return GenresLine;
          return Title.Season.HasValue
              ? show + " \u00B7 Season " + Title.Season.Value.ToString(CultureInfo.InvariantCulture)
              : show;
        }

        return GenresLine;
      }
    }

    /// <summary>Whether the stood-on row has a second line to say at all.</summary>
    public bool ShowsSecondLine
    {
      get { return SecondLine.Length > 0; }
    }

    /// <summary>
    /// Whether this row's picture keeps its own shape instead of being cropped into the posters' portrait. A
    /// record and a track wear the album's square, which is the one front picture the audio import files (see
    /// <c>MediaCatalog.IsMusic</c>); an episode wears the thumbnail the video import took out of its file, which
    /// is a wide picture and the only one a single episode has. Both are drawn at the row's height with whatever
    /// width the picture brings; the film and series rows keep the poster's fixed portrait.
    /// </summary>
    public bool WearsOwnPicture
    {
      get { return MediaCatalog.IsMusic(ReadThrough) || MediaCatalog.IsEpisode(ReadThrough); }
    }

    /// <summary>
    /// The number or address this title has inside the one that holds it: <c>4 ·</c> for the fourth track,
    /// <c>S02E05</c> for an episode. Empty for the titles that are not inside anything, which is most of what a
    /// television shows.
    /// </summary>
    public string Position
    {
      get
      {
        if (Title.Track.HasValue)
        {
          // A dot rather than a full stop: the number and the song are two halves of one row, and a full stop
          // reads as the end of a sentence the row never began. The stood-on row spells its year the same way.
          return Title.Track.Value.ToString(CultureInfo.InvariantCulture) + " \u00B7";
        }

        // A season's number is its name rather than an address inside something, and the row already says it;
        // writing "S03" in front of "Season 3" would be the same number twice on one line.
        if (MediaCatalog.IsSeason(ReadThrough))
          return "";

        if (!Title.Season.HasValue && !Title.Episode.HasValue)
          return "";

        // Under its own season an episode is told apart by its number alone; on a wall that mixes the episodes
        // of every show in the house the season has to be spelled out in front of it.
        if (StandsInsideItsSeason)
          return EpisodeNumber + " \u00B7";

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
    /// An episode that the list could not time says the length its own screen read (see
    /// <see cref="ReadLengthFromItsAnswer"/>), so the column of the row the viewer stands on agrees with that
    /// screen.
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
        int? minutes = Title.RuntimeMinutes ?? _readLength;
        string length = minutes.HasValue
            ? minutes.Value + " min"
            : "";
        if (Title.Stub)
          return string.IsNullOrEmpty(length) ? "stub" : length + " · stub";
        return length;
      }
    }

    /// <summary>
    /// Tells an episode row how long it is, out of the answers this title's own screen read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The list cannot say how long an episode is, because the library does not know: an episode's aspect has no
    /// runtime attribute at all (<c>EpisodeAspect.cs:42-99</c>), an episode is not a film aspect either (measured
    /// on a running server: none of its 88 episode rows answers a length), and the importer that read the number
    /// off the metadata file put it into the stub and not into the record
    /// (<c>NfoSeriesEpisodeReader.cs:593-625</c>, where the film reader writes
    /// <c>MovieAspect.ATTR_RUNTIME_M</c> at <c>NfoMovieReader.cs:1749-1757</c>).
    /// </para>
    /// <para>
    /// The screen beside the list shows a length for the same episode all the same, because asking about ONE title
    /// makes the server read the file describing the media (<c>MediaController.FillMissingDetailsFromFiles</c>,
    /// reached as <c>details.RuntimeMinutes</c>).  That answer is already in hand when the row is standing under
    /// the viewer's caret, so the row takes it: the right column then says the same number as the screen next to
    /// it instead of nothing.  A row that the list already answered a length for is left alone, and so is every
    /// kind of row but this one.
    /// </para>
    /// </remarks>
    internal void ReadLengthFromItsAnswer(int? minutes)
    {
      if (!MediaCatalog.IsEpisode(ReadThrough) || !minutes.HasValue
          || Title.RuntimeMinutes.HasValue || _readLength.HasValue)
        return;

      _readLength = minutes;
      Raise();
    }

    int? _readLength;

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

    /// <summary>
    /// Whether the screen stands in its reading state: the wall of tiles put away and one title's facts left
    /// standing on that title's own picture, which is what the reference's <i>Media information</i> screen is
    /// (<c>MediaInfoAction.cs:69-90</c> pushing <c>MediaInfoVideo.xaml</c>). The reference had to push a screen
    /// because its media list kept its wall up while its info screen did not; here the facts are the same column
    /// the shelf already wears, so what the reading adds is the wall being gone.
    /// </summary>
    /// <remarks>
    /// Not remembered anywhere: it is one title being read, not a choice about how the screen is worn, and the
    /// next title the walk arrives at is standing on the wall again by itself.
    /// </remarks>
    [ObservableProperty]
    private bool _infoShown;

    /// <summary>
    /// What the last press inside the title's dialog answered, said in the dialog itself. A screen's status line
    /// is no place for it: on this screen that line only stands while the shelf is empty (<c>HasTitles</c> false),
    /// so every sentence written under a full wall - which is every sentence - is written where nobody is looking.
    /// Cleared when the dialog opens, so that the line never repeats an answer from an earlier title.
    /// </summary>
    [ObservableProperty]
    private string _menuAnswer = "";

    [ObservableProperty]
    private string _detailsHeading = "";
 
    [ObservableProperty]
    private string _detailsTagline = "";
 
    [ObservableProperty]
    private string _detailsLine = "";

    [ObservableProperty]
    private string _detailsText = "";

    /// <summary>
    /// The name one episode of a series bears, said in bold over its story. It stands there and nowhere else:
    /// a fact among the facts it is not - the story below is this episode's own sentence, and the name it
    /// opens belongs to the sentence's title, not to the shelf's catalogue numbers beside the rating.
    /// </summary>
    [ObservableProperty]
    private string _detailsEpisodeName = "";

    /// <summary>
    /// Whether the cover's foot wears its progress line: only between the ends, which is the home tiles' law -
    /// a title untouched has nothing to measure and a finished one is watched, which the watched mark says
    /// (<c>HomeContentStyles.xaml:203-207</c> hides the bar at nought and at a hundred).
    /// </summary>
    /// <remarks>
    /// The figure is the row's own, not the panel answer's, and that is the server's doing rather than an
    /// oversight here: <c>MediaItemDetails</c> carries the count of how often a title was finished but never how
    /// far the last attempt got, while every row of the list does carry it (<c>ServerApiDto.cs:139-181</c>). The
    /// row is the title the viewer is standing on, so its number is the honest one.
    /// </remarks>
    [ObservableProperty]
    private bool _detailsProgressShown;

    /// <summary>
    /// How far this viewer got, as the whole number the line at the cover's foot measures out of a hundred.
    /// </summary>
    [ObservableProperty]
    private double _detailsProgress;

    /// <summary>
    /// The title's own lettering without the studio's name in it, which is the picture the reference hung in its
    /// details head (<c>MediaInfoMovieDetails.inc:355-362</c>). Null when the library never filed one - the
    /// frame is not drawn for a picture that is not there.
    /// </summary>
    [ObservableProperty]
    private byte[]? _detailsClearArt;

    /// <summary>
    /// The lettering an album was released with, which stands over the album's story and not beside the cover:
    /// the reference asked for it by the name <c>ClearArt</c> and stood it above the description
    /// (<c>MediaItemListAudioAlbumDetails.inc:26-31</c>, the description itself at <c>:307</c>).
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="_detailsClearArt"/> on purpose. That one belongs to the head's lettering row,
    /// which is where the reference hung a film's lettering (<c>MediaInfoMovieDetails.inc:355-362</c>); letting
    /// one picture feed both places would draw an album's name twice on one screen.
    /// </remarks>
    [ObservableProperty]
    private byte[]? _detailsAlbumLettering;

    /// <summary>
    /// The disc an album was pressed on, which is laid <em>behind</em> the album's cover instead of beside it: the
    /// reference put its <c>DiscArt</c> into the cover's own box - four hundred and twenty lines of disc behind
    /// four hundred and forty of cover - and let it look out past the cover's edge
    /// (<c>MediaItemListAudioAlbumDetails.inc:41-44</c> for the disc, <c>:52</c> for the cover, the box at
    /// <c>:33</c>).  Null when the library keeps no picture of the disc, which is thirty four of the household's
    /// forty three albums; the panel then draws no disc at all.
    /// </summary>
    [ObservableProperty]
    private byte[]? _detailsAlbumDisc;

    /// <summary>
    /// Whether a disc lies under the cover.  It is asked as one answer because the panel is drawn in two pieces by
    /// it: the cover is then painted over the disc, and the story - which stood in the column beside the cover -
    /// gives that column to the disc and is told under the cover instead, in the viewer's words of 2026-11-03.
    /// A disc is never laid out without a cover to lie under, which is why the cover is part of this answer: a
    /// library that keeps the disc and not the cover shows its cover, not a disc floating where a cover should be.
    /// </summary>
    public bool ShowsDiscBehindCover => DetailsAlbumDisc != null && DetailsArtwork != null;

    /// <summary>
    /// The cover standing by itself, which is how every title without a disc and every film has always stood.
    /// </summary>
    public bool ShowsCoverAlone => DetailsArtwork != null && !ShowsDiscBehindCover;

    /// <summary>
    /// The album's writing over the story, which is where it was asked for as long as the story stands beside the
    /// cover (<c>MediaItemListAudioAlbumDetails.inc:26-31</c>, the description a row under it at <c>:307</c>).
    /// </summary>
    public bool ShowsLetteringOverStory => !ShowsDiscBehindCover && DetailsAlbumLettering != null;

    /// <summary>
    /// The album's writing over the row the cover stands in, which is the same writing seen from a panel whose
    /// story has moved under its cover: its top edge stays the cover's top edge either way, and a disc looking
    /// out to the left of the cover leaves the story's old column too narrow to write in.
    /// </summary>
    public bool ShowsLetteringOverCover => ShowsDiscBehindCover && DetailsAlbumLettering != null;

    /// <summary>
    /// The writing the gradient over the row of covers wears: an album's ClearArt, or the ClearArt of a film and
    /// of an episode, whichever family the shelf belongs to.  Both were read long since for the information screen
    /// (<see cref="ReadDetailsClearArtAsync"/>); only the row of covers never showed them, though a film's lettering
    /// asks for that row more than for any column.
    /// </summary>
    public byte[]? CoverLettering
        => IsMusic
            ? DetailsAlbumLettering
            : DetailsClearArt;

    /// <summary>
    /// Whether that writing arrived.  A title that wears none leaves the place empty, and the story takes the room.
    /// </summary>
    public bool ShowsCoverLettering
        => CoverLettering != null;

    /// <summary>
    /// The row the story is told in: its own row beside the cover, or - when a disc lies under the cover, or when
    /// the picture stands over its story as an episode's does - the row under it.  The panel asks for the number
    /// because a screen cannot weigh a condition by itself.
    /// </summary>
    public int DetailsStoryRow => ShowsDiscBehindCover || DetailsPictureStandsOverStory ? 4 : 3;

    /// <summary>
    /// How many of the panel's two columns the story is given: its own column while the cover stands beside it,
    /// and the whole width under a cover that stands over it.
    /// </summary>
    public int DetailsStoryColumns => ShowsDiscBehindCover || DetailsPictureStandsOverStory ? 2 : 1;

    /// <summary>
    /// Whether the picture stands over its story instead of beside it, which only an episode does: the viewer asked
    /// for the episode's sentence over the panel's whole width on 2026-11-10, because a column of reading with an
    /// empty column next to it is a hole and not a margin.  The episode's name keeps the picture's side of the row
    /// above, and the story is told under both.
    /// </summary>
    public bool DetailsPictureStandsOverStory => MediaCatalog.IsEpisode(AspectName);

    /// <summary>
    /// The row the picture stands in: it takes everything the head and the facts leave while the story stands
    /// beside it - which is what lets a poster of five hundred lines keep its foot on the story's first line - and
    /// asks for nothing but its own thumbnail once the story is told under it, so that the leftover belongs to the
    /// reading and never to an empty strip above it.
    /// </summary>
    public GridLength DetailsPictureRowHeight
        => DetailsPictureStandsOverStory ? GridLength.Auto : new GridLength(1, GridUnitType.Star);

    /// <summary>
    /// The title's own lettering over the story, which is where it stands as long as the story is told beside its
    /// picture: it is docked over the reading then, and the reading begins under it.
    /// </summary>
    public bool ShowsClearArtOverStory => !DetailsPictureStandsOverStory && DetailsClearArt != null;

    /// <summary>
    /// The title's own lettering in the row its picture stands in, which is what an episode gets: its story is told
    /// under the thumbnail, and a lettering that travelled with that story would hang a picture of the whole series
    /// over the episode's sentence - the viewer asked for the name beside the thumbnail and nothing else moved
    /// (word of 2026-11-10).  The two answers are one picture's two places, never both at once.
    /// </summary>
    public bool ShowsClearArtOverPicture => DetailsPictureStandsOverStory && DetailsClearArt != null;

    /// <summary>
    /// The row the story is told in when it stands under its picture: the counterpart of <see cref="DetailsPictureRowHeight"/>.
    /// </summary>
    public GridLength DetailsStoryRowHeight
        => DetailsPictureStandsOverStory ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;

    /// <summary>
    /// The floor the picture's row keeps for itself, which is the story's floor while the story stands in that row
    /// and nothing once the story has moved under the picture.  An empty row may not cost a hundred and sixty lines.
    /// </summary>
    public double DetailsPictureRowFloor => DetailsPictureStandsOverStory ? 0.0 : STORY_ROW_FLOOR;

    /// <summary>
    /// The floor the row under the picture keeps, which is nothing while the story is told above it and the story's
    /// floor as soon as the story is told in it.
    /// </summary>
    public double DetailsStoryRowFloor
        => DetailsPictureStandsOverStory || ShowsDiscBehindCover ? STORY_ROW_FLOOR : 0.0;

    /// <summary>
    /// What a row the story is told from never shrinks below, said once here and in the view: a row of "*" that is
    /// measured before its screen has a size leaves nothing at all, and a scroller with no room is not a short story.
    /// </summary>
    private const double STORY_ROW_FLOOR = 160;

    partial void OnDetailsAlbumDiscChanged(byte[]? value) => RaiseCoverLayout();

    partial void OnDetailsArtworkChanged(byte[]? value) => RaiseCoverLayout();

    partial void OnDetailsAlbumLetteringChanged(byte[]? value)
    {
      OnPropertyChanged(nameof(ShowsLetteringOverStory));
      OnPropertyChanged(nameof(ShowsLetteringOverCover));
      OnPropertyChanged(nameof(CoverLettering));
      OnPropertyChanged(nameof(ShowsCoverLettering));
    }

    /// <summary>
    /// Tells the panel that the ground under its cover moved.  The cover, the disc, the writing and the row the
    /// story is told in are all read off the same two pictures, and those two travel separately: the cover is
    /// asked for first, the disc after it, so every one of the four answers is asked again whenever either of them
    /// arrives - otherwise a panel that measured itself over the cover alone would keep the story in the cover's
    /// column after its disc had come in and needed that column.
    /// </summary>
    private void RaiseCoverLayout()
    {
      OnPropertyChanged(nameof(ShowsDiscBehindCover));
      OnPropertyChanged(nameof(ShowsCoverAlone));
      OnPropertyChanged(nameof(ShowsLetteringOverStory));
      OnPropertyChanged(nameof(ShowsLetteringOverCover));
      OnPropertyChanged(nameof(DetailsStoryRow));
      OnPropertyChanged(nameof(DetailsStoryColumns));
      OnPropertyChanged(nameof(DetailsStoryRowFloor));
    }

    partial void OnDetailsClearArtChanged(byte[]? value)
    {
      OnPropertyChanged(nameof(CoverLettering));
      OnPropertyChanged(nameof(ShowsCoverLettering));
      OnPropertyChanged(nameof(ShowsClearArtOverStory));
      OnPropertyChanged(nameof(ShowsClearArtOverPicture));
    }

    /// <summary>
    /// The row of pictures the file's picture is made of: whether it is stereoscopic, its codec, its size, its
    /// proportion, and - as words, which is how the reference wrote those two - its bitrate with its frame rate.
    /// </summary>
    /// <remarks>
    /// These are the cells the reference's information screen stood above its track lists
    /// (<c>MediaInfoVideo.xaml:152-255</c>), and they are drawn from the same logo folders the old skin shipped,
    /// which this front end now carries too (<c>Skins\Nereus\images\MediaLogos\</c>, asked for by
    /// <see cref="MediaStreamLogos"/>). What the library never wrote leaves its cell away, so the row can
    /// legitimately be empty - for a title whose importer never got inside the file it is the ordinary answer.
    /// </remarks>
    [ObservableProperty]
    private PictureLine _detailsPicture = PictureLine.Empty;

    /// <summary>
    /// The row of pictures an <b>audio</b> file is made of: its codec, its channels, its disc, and its bitrate over
    /// its sample rate.
    /// </summary>
    /// <remarks>
    /// These are the six cells the reference's audio screen stood above its details
    /// (<c>MediaInfoAudio.xaml:115-170</c>), read off the same aspect the worn skin read.  They arrive with the
    /// details answer rather than over a second question: an audio file's figures are written on the file's own
    /// item, so unlike a film's streams they cost the server nothing extra to hand over.
    /// </remarks>
    [ObservableProperty]
    private AudioFileLine _detailsAudio = AudioFileLine.Empty;

    /// <summary>Whether the file answered any of its four audio cells.</summary>
    [ObservableProperty]
    private bool _showsAudioLine;

    /// <summary>
    /// One card per audio track the file carries, in the order the tracks lie in the file.
    /// </summary>
    public ObservableCollection<AudioTrackCard> DetailsAudioTracks { get; } = new ObservableCollection<AudioTrackCard>();

    /// <summary>
    /// One card per subtitle track the file carries, same order.
    /// </summary>
    public ObservableCollection<SubtitleTrackCard> DetailsSubtitleTracks { get; } = new ObservableCollection<SubtitleTrackCard>();

    /// <summary>Whether there is anything to say about the file's picture.</summary>
    [ObservableProperty]
    private bool _showsPictureLine;

    /// <summary>
    /// The channels of the file's <b>first</b> audio track, as the picture's name ("6" for a 5.1 arrangement) and
    /// as the word a viewer reads ("5.1") - the two answers the first audio card carries at its top right corner.
    /// </summary>
    /// <remarks>
    /// Asked for by the viewer on 2026-11-02 to stand beside the stars in the details, where a viewer decides
    /// whether this is a surround file before reading a single track.  The first track and not the best one: the
    /// card the picture is taken from is the first card of the list, and a plaque that quietly picked a different
    /// track than the list's first would be a plaque a viewer cannot check.
    /// </remarks>
    [ObservableProperty]
    private string _firstAudioChannelsToken = "";

    /// <summary>The first audio track's channel count as the trade's word, for a count the skin has no picture of.</summary>
    [ObservableProperty]
    private string _firstAudioChannelsWord = "";

    /// <summary>Whether the file's first audio track counted channels at all.</summary>
    public bool ShowsFirstAudioChannels => FirstAudioChannelsToken.Length > 0 || FirstAudioChannelsWord.Length > 0;

    partial void OnFirstAudioChannelsTokenChanged(string value) => OnPropertyChanged(nameof(ShowsFirstAudioChannels));

    partial void OnFirstAudioChannelsWordChanged(string value) => OnPropertyChanged(nameof(ShowsFirstAudioChannels));

    /// <summary>Whether the file carries audio tracks the library can name.</summary>
    [ObservableProperty]
    private bool _showsAudioTracks;

    /// <summary>Whether the file carries subtitle tracks the library can name.</summary>
    [ObservableProperty]
    private bool _showsSubtitleTracks;

    /// <summary>
    /// The face of the subtitle plaque: the framed glyph when the file this reading wears carries text, the crossed
    /// one when it does not.  It is asked apart from <see cref="ShowsSubtitleTracks"/> because the two can differ:
    /// a series and a season are shelves rather than files, so their tracks lists are empty by nature while their
    /// plaque answers from the first episode, which does carry text or does not.
    /// </summary>
    [ObservableProperty]
    private bool _showsFileSubtitles;

    /// <summary>
    /// Whether this reading asks the subtitle question at all, which only a picture does.
    /// </summary>
    /// <remarks>
    /// A sound file has no subtitle to carry and never had one, so the plaque would answer a question nobody asked -
    /// and it answered it as a denial, "no subtitles", over a title that cannot have any.  The reference asked the
    /// question in its video screens only (<c>MediaInfoVideo.xaml:157-174</c>, <c>CurrentlyPlayingVideo.xaml:164-181</c>,
    /// both an <c>SubtitleAspectWrapper</c>), while its audio screen asks about the packing, the loudspeakers, the
    /// disc and the two rates and about nothing else (<c>MediaInfoAudio.xaml:115-170</c>).
    /// </remarks>
    public bool ShowsSubtitlePlaque => !IsMusic;

    /// <summary>
    /// One card per person the library names as an actor of this title, in the order the library answered them.
    /// </summary>
    /// <remarks>
    /// The reference stood these two bands under its two track bands, in the same left column and in the same shape
    /// of a head and a row of cards that runs sideways (<c>MediaInfoVideo.xaml:384-420</c> for the actors,
    /// <c>:421-457</c> for the director, both lists wearing <c>PersonCPListViewStyle</c>), and asked for them out
    /// of the title's own relationships.  They stand SIDE BY SIDE here rather than under one another, which the
    /// viewer agreed to on 2026-11-03: four bands of two hundred and sixty lines do not fit above the foot of a
    /// window that is a thousand and eighty lines tall, and a band a viewer cannot reach is not a band.
    /// </remarks>
    public ObservableCollection<PersonCard> DetailsActors { get; } = new ObservableCollection<PersonCard>();

    /// <summary>One card per person the library names as this title's director.</summary>
    public ObservableCollection<PersonCard> DetailsDirectors { get; } = new ObservableCollection<PersonCard>();

    /// <summary>
    /// One card per person the library names as the artist of this audio file, in the order it answered them.
    /// </summary>
    /// <remarks>
    /// The three music bands are the reference's (<c>MediaInfoAudio.xaml:204-296</c>: Artists, Album Artists,
    /// Composers, each a head and a sideways list of the same <c>PersonCPListViewStyle</c> the film's two bands
    /// wear).  They stand in place of the film's two rather than under them, because a title is either one or the
    /// other: an audio file names no actors, and a film names no composers.
    /// </remarks>
    public ObservableCollection<PersonCard> DetailsArtists { get; } = new ObservableCollection<PersonCard>();

    /// <summary>One card per person the library names as the album's artist.</summary>
    public ObservableCollection<PersonCard> DetailsAlbumArtists { get; } = new ObservableCollection<PersonCard>();

    /// <summary>One card per person the library names as the composer.</summary>
    public ObservableCollection<PersonCard> DetailsComposers { get; } = new ObservableCollection<PersonCard>();

    /// <summary>Whether the library named anybody in the acting part.</summary>
    [ObservableProperty]
    private bool _showsActors;

    /// <summary>Whether the library named anybody in the directing part.</summary>
    [ObservableProperty]
    private bool _showsDirectors;

    /// <summary>
    /// Whether the row of people costs its lines: either of its two bands has an answer. Needed because a window
    /// half as tall as a television cannot spare four bands, so the two people bands share one row - and a row
    /// whose both halves are empty would stand there as an empty head over nothing.
    /// </summary>
    public bool ShowsPersons => ShowsActors || ShowsDirectors;

    partial void OnShowsActorsChanged(bool value)
    {
      OnPropertyChanged(nameof(ShowsPersons));
      RaiseTheBands();
    }

    partial void OnShowsDirectorsChanged(bool value)
    {
      OnPropertyChanged(nameof(ShowsPersons));
      OnPropertyChanged(nameof(ShowsTracks));
    }

    /// <summary>Whether the library named anybody as the artist of this audio file.</summary>
    [ObservableProperty]
    private bool _showsArtists;

    /// <summary>Whether the library named an album artist.</summary>
    [ObservableProperty]
    private bool _showsAlbumArtists;

    /// <summary>Whether the library named a composer.</summary>
    [ObservableProperty]
    private bool _showsComposers;

    /// <summary>
    /// Whether the row of <b>music</b> people costs its lines: any of its three bands has an answer. The three
    /// share one row for the reason the film's two already do - the reading's column is a thousand lines tall and
    /// three bands of two hundred are not in it - and a row whose every half is empty would stand there as three
    /// heads over nothing.
    /// </summary>
    public bool ShowsMusicPersons => ShowsArtists || ShowsAlbumArtists || ShowsComposers;

    partial void OnShowsArtistsChanged(bool value) => RaiseTheMusicRow();

    partial void OnShowsAlbumArtistsChanged(bool value) => RaiseTheMusicRow();

    partial void OnShowsComposersChanged(bool value) => RaiseTheMusicRow();

    /// <summary>Says again what the row of music people and the whole lower half of the reading have to show.</summary>
    private void RaiseTheMusicRow()
    {
      OnPropertyChanged(nameof(ShowsMusicPersons));
      OnPropertyChanged(nameof(ShowsTracks));
      RaiseTheBands();
    }

    /// <summary>Whether the audio file's own row of plaques has an answer.</summary>
    partial void OnShowsAudioLineChanged(bool value)
    {
      OnPropertyChanged(nameof(ShowsTracks));
      RaiseTheBands();
    }

    /// <summary>
    /// Whether the whole tracks band costs its lines: any of its parts has an answer. One answer because a
    /// screen cannot join three conditions by itself, and because the header the band carries would otherwise
    /// stand over nothing.
    /// </summary>
    public bool ShowsTracks => ShowsPictureLine || ShowsAudioTracks || ShowsSubtitleTracks || ShowsPersons
        || ShowsMusicPersons || ShowsAudioLine;

    partial void OnShowsPictureLineChanged(bool value) => RaiseTheBands();

    partial void OnShowsAudioTracksChanged(bool value) => RaiseTheBands();

    partial void OnShowsSubtitleTracksChanged(bool value) => RaiseTheBands();

    /// <summary>
    /// Says again what the lower half of the reading has to show, because both of its properties are built from
    /// parts that arrive one by one while a title is read.
    /// </summary>
    private void RaiseTheBands()
    {
      OnPropertyChanged(nameof(ShowsTracks));
      OnPropertyChanged(nameof(ShowsBands));
    }

    /// <summary>
    /// Whether the reading has anything below the story at all: a band of tracks, a row of people, or the card of
    /// the collection the film belongs to.  The card shares the panel with the bands (the reference hung it under
    /// them, <c>MediaInfoVideo.xaml:457-530</c>), so a film that names a collection but carries no tracks of its
    /// own - a file the importer never counted - would otherwise lose its card to a panel that stayed away.
    /// </summary>
    public bool ShowsBands => ShowsTracks || ShowsCollection;

    /// <summary>
    /// The collection the read film belongs to, in the library's own words, or nothing when it belongs to none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The name is the head of the band, not a line inside a card: the reference's screen for a collection was
    /// headed by the collection's own name and stood its films underneath
    /// (<c>MediaItemListMovieCollectionDetails.inc:52-54</c>), which is what the row of posters below this head is.
    /// The framed button the reading used to carry (<c>MediaInfoVideo.xaml:457-530</c>) said the name beside a
    /// poster of its own and answered no key; the name alone could not say WHICH films, so the button is gone and
    /// the films stand in its place.
    /// </para>
    /// <para>
    /// The collection's own picture is not asked for any more.  It was never in a library: counted over 1136 films,
    /// 228 name one of the 164 collections this machine holds, and not one of those 164 collection titles carries a
    /// picture, because MP2 files a collection's artwork as <c>movieset-*</c> pictures beside its films and this
    /// library holds none of those either.  A head of words over a row of the films' own posters therefore says
    /// everything the picture would have said and nothing that is missing.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string _detailsCollection = "";

    /// <summary>Whether the film names a collection, which is the one question the whole band asks.</summary>
    public bool ShowsCollection => !string.IsNullOrWhiteSpace(DetailsCollection);

    partial void OnDetailsCollectionChanged(string value)
    {
      OnPropertyChanged(nameof(ShowsCollection));
      OnPropertyChanged(nameof(ShowsBands));
    }

    /// <summary>
    /// The films the named collection holds <em>in this library</em>, which the band wears as a row of their posters
    /// under its name.
    /// </summary>
    /// <remarks>
    /// Asked of the shelf and not read off the collection's own title: MP2 counts a collection's films on the
    /// collection aspect (<c>NUMMOVIES</c>), which says how many the site the importer asked knows and nothing about
    /// which of them are on this machine - counted here, the collection whose aspect counts one film holds one film
    /// of this library, and a collection aspecting <c>NUMMOVIES</c> of five can hold two.  A row of posters is a
    /// statement about the library, so the library is asked.
    /// </remarks>
    public ObservableCollection<CollectionFilmCard> DetailsCollectionFilms { get; } =
        new ObservableCollection<CollectionFilmCard>();

    /// <summary>Whether there is a row of films to draw under the name.</summary>
    public bool ShowsCollectionFilms => DetailsCollectionFilms.Count > 0;

    // The collection the row of films was read for, which is nothing while it was read for none.  Kept because the
    // row belongs to the COLLECTION and not to the film being read: opening the next film of the same collection
    // must leave the row standing (see ReadDetailsCollectionFilmsAsync), and a film of another collection is the
    // one answer that makes it start over.
    private string _collectionFilmsFor = "";

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

    /// <summary>
    /// The handful of facts the gradient over the row of covers has room for: what a title <em>is</em>, said above
    /// the plaques that say what its file <em>is</em>.  The whole band stays on the information screen; the shelf
    /// names only the answers a viewer reaches for while walking the row - of what year, of what kind, how long,
    /// how many - together with the two numbers of an episode and the two of a season, which are the only way a
    /// series says where it stands.
    /// </summary>
    public ObservableCollection<MediaBrowserFact> CoverFacts { get; } = new ObservableCollection<MediaBrowserFact>();

    /// <summary>
    /// The words of <see cref="DetailsFacts"/> the cover flow repeats.  Ordinal, because every one of them is one
    /// this front end wrote itself in <c>AddFact</c> and none came from a server.
    /// </summary>
    private static readonly HashSet<string> CoverFactLabels = new HashSet<string>(StringComparer.Ordinal)
    {
      "Season", "Episode", "Series", "Episodes", "Length", "Runtime", "Year", "Genre", "Tracks"
    };

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
    /// The first episode of every shelf this screen has opened, by the id the shelf was asked under together with
    /// the season number read through it.  A shelf is not a file, so its plaques answer from that episode; the
    /// answer is remembered because a viewer walking a series row stops at several series and one series is read
    /// more than once on the way.
    /// </summary>
    private readonly Dictionary<string, Guid?> _firstEpisodeOfShelf = new(StringComparer.Ordinal);

    /// <summary>
    /// The title the newest details read was asked for. Walking a shelf with the details panel on fires a
    /// read per row, and only the last row's answer may reach the panel - a plot that arrives after the
    /// viewer walked on would otherwise sit under the next title's name.
    /// </summary>
    private Guid _detailsAskFor;

    /// <summary>
    /// The title whose answers a read is on its way for right now, or <see cref="Guid.Empty"/> for none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The mark that a title's answers were read (<c>_detailsFor</c>) is only written once the story arrived,
    /// because a read that came back failing has to be askable again.  Until the answer lands, nothing stops the
    /// same title from being asked a second time: the shelf settles onto its row while a reading is being opened,
    /// and a screen that wears details asks every time the selection lands.  Each of those questions walks the
    /// whole way to the server and back.
    /// </para>
    /// <para>
    /// <b>What the duplicates cost.</b> Every read empties the bands before filling them, and the emptying happens
    /// on the way out while the filling happens after the answer.  So two overlapping reads for one title empty
    /// twice and fill twice, and the viewer is shown the same audio track two or three times in a row - which a
    /// slow server makes likely, since the answers stay in the air longer.  One read per title at a time is what
    /// keeps the bands the length of what the file carries.
    /// </para>
    /// </remarks>
    private Guid _detailsReadUnderway;

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
    /// follows a hand that has stopped typing (see <see cref="OnSearchTextChanged"/>), and Enter or the Find
    /// button answer at once without waiting for that pause.
    /// </summary>
    [ObservableProperty]
    private string _searchText = "";

    /// <summary>
    /// How long a hand may rest on the last letter before what it has typed so far is taken as a question.  Long
    /// enough that a word being spelled is not asked about in halves - "ali", "alin", "alien" - and short enough
    /// that the wall has the matches ready by the time the viewer looks up from the keys.
    /// </summary>
    private const int TYPING_PAUSE_MILLISECONDS = 300;

    /// <summary>The pause timer, made once and restarted with every letter.</summary>
    private DispatcherTimer? _typing;

    /// <summary>Whether the program is writing the box itself, which is never a viewer asking.</summary>
    private bool _writingWords;

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
    /// <param name="onlySeason">
    /// The one season of <paramref name="parent"/> whose episodes this shelf is to show, given when the series has
    /// exactly one: a shelf with a single row on it is a step walked through rather than a screen stood on, so the
    /// walk goes series &#8594; episodes and "back" arrives on the series shelf. Null - which is what a series with
    /// two or more seasons gets - opens the shelf the series holds, its seasons, as it always did.
    /// </param>
    public MediaBrowserViewModel(ServerApiClient? client, HomeSection? section, TitleViewModel? parent,
        PictureCache? pictures = null, TitleViewModel? onlySeason = null)
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
      // One season is not a screen. The shelf a series opens on is its seasons, but when the library holds exactly
      // one of them the screen would carry a single row that says the series' own name back at the viewer, so the
      // press goes through to that season's episodes and the walk keeps the shape the viewer expects of a house
      // with one season: series -> episodes.
      TitleViewModel? through = parent != null ? onlySeason : null;
      AspectName = parent == null
          ? section?.Aspect.Name ?? ""
          : through != null ? "EpisodeItem" : MediaCatalog.ChildAspectFor(cameFrom);
      Inside = parent == null ? null : through != null ? through.AskId : parent.AskId;
      // The one shelf that cannot be opened by an id alone: a season the library only counted off its episodes has
      // no title to look inside of, so its shelf is the series' episodes narrowed to that number.
      SeasonFilter = parent == null ? null : through != null ? through.AskSeason : parent.AskSeason;
      // Every shelf opens by name, which is the order it has always had; a shelf read inside a title takes the
      // order its kind carries, the tracks of an album by their number. The one tile that used to promise another
      // order - MP2's "Latest media" - is gone, so nothing arrives with one.
      SortOrder = parent == null
          ? "title"
          : MediaCatalog.ChildSortFor(through != null ? "SeasonItem" : cameFrom);
      // The browse screen's picked value travels the same way the picked order did: with the section. A list
      // opened out of one title is the titles inside it, and the genre of the shelf they were browsed under does
      // not follow them down - the tracks of an Action album are nobody's Action genre.
      GroupBy = parent == null ? section?.By : null;
      GroupValue = parent == null ? section?.Value : null;
      DrillsIn = MediaCatalog.HoldsTitles(AspectName);
      Heading = parent == null ? section?.Label ?? "Titles" : parent.Name;
      // The two numbers at the shelf's foot speak whenever the shelf itself changes shape beneath them.
      Titles.CollectionChanged += (_, _) => RaiseShelfCounters();
      //  A shelf that is filling says nothing.  The reference's shelf head carries its heading and no other
      //  word, and while it reads it spins the BusyCursor superlayer (Skin/Nereus/superlayers/BusyCursor.xaml)
      //  instead of writing a sentence over the empty wall - "Reading Movies" standing above a shelf that has
      //  not arrived yet is this front end's own invention.  What the line has to say are the screen's problems,
      //  and every one of them is said by the read itself: nothing found, a server that will not answer, a
      //  search that found too little.
      _status = client == null ? "No server configured"
          : section == null ? "No section chosen"
          : "";

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
      // button, and a screen keeps the answer its viewer gave it there. An episode shelf is the one shelf
      // with no question to answer: an episode has no poster and no banner - the importers never wrote
      // either - its only picture is the thumbnail standing on its tile, so the shelf opens in that shape
      // whatever some other shelf once remembered, and the rail keeps the choice out of the menu entirely.
      _tileKind = MediaCatalog.IsEpisode(AspectName)
          ? GridViewType.Thumbnail
          : RememberedMedia() ?? (_skinSettings == null ? GridViewType.Poster : TileKindFor(AspectName, _skinSettings));
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
      // default skin did: no background model, no picture.  Asked at the screen's own width, not the frames':
      // this picture's frame is the whole screen, and a copy asked at the wall's size arrives three times too
      // small for it - the blur the viewer named.
      FanartShown = _skinSettings?.EnableFanart ?? false;
      FanartOverlay = _skinSettings?.FanartOverlayOpacity ?? 0.85;
      FanartBlurRadius = BlurOf(_skinSettings);
      // A setting the viewer flips on the skin-settings screen while this shelf lives takes its effect on
      // the television at once - the screen is a remote control, not an application form.
      SkinSettingsBase.Saved += delegate { ReapplySkinSettings(); };
      int backdrop = PictureWidths.PaintedScreenWidth();
      _fanart = client == null
          ? null
          : PicturesOf(client, PictureWidths.Shelf("FanArt", backdrop), id =>
              {
                (Guid asked, int? season) = AskOf(id);
                return client.GetFanArtAsync(asked, "FanArt", season, backdrop);
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

      // How big this shelf paints the kind, which is the number the server resizes to. A poster is asked wide of a
      // cover wall and small of a grid tile - the same picture, two frames, and the wall's five hundred and
      // seventy three pixels are not the tile's two hundred and seventeen - so the width travels with the kind
      // into the name of the shelf the bytes lie in on this machine. Without that, a viewer who walks from the
      // wall into the grid is handed the wide copy by the local shelf and stands behind a decoder again for
      // picture after picture, which is the whole cost this was added to take away.
      int width = kind == GridViewType.Poster && IsCoverLayout
          ? PictureWidths.COVER
          : PictureWidths.TILE;

      Artwork? created = _client == null
          ? null
          : PicturesOf(_client, PictureWidths.Shelf(wanted, width), id =>
              {
                (Guid asked, int? season) = AskOf(id);
                return _client.GetFanArtAsync(asked, wanted, season, width);
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

    /// <summary>
    /// The frame a skin draws for a music shelf's cover in the third view.  One frame for albums and tracks,
    /// because the reference drew one: <c>CoverViewAudioItemTemplate</c> (MediaStyles.xaml:3599) is bound for
    /// audio as audio, at <c>AUDIO_COVER_WIDTH</c> x <c>AUDIO_COVER_HEIGHT</c> - 260x260 (MediaConsts.xaml:57f).
    /// </summary>
    internal const string MUSIC_COVER_TEMPLATE_KEY = "MediaTileAlbumCoverTemplate";

    /// <summary>The frame a skin draws for a track's square.</summary>
    internal const string TRACK_TEMPLATE_KEY = "MediaTileTrackTemplate";

    /// <summary>
    /// Whether this shelf holds music, which is settled by the aspect and by nothing else: the reference drew
    /// its audio tiles as squares - <c>AUDIO_GRID_WIDTH</c> x <c>AUDIO_GRID_HEIGHT</c>, 205x195 - for every
    /// view mode and every skin setting, while its grid-type setting names films, series and seasons only.
    /// Music therefore never asks the poster question.
    /// </summary>
    private bool IsMusic
        => MediaCatalog.IsMusic(AspectName);

    /// <summary>
    /// The frame the worn skin draws for this shelf's music, or <c>null</c> when it draws none - in which case
    /// the shelf wears whatever frame the layout asks for, as every other skin's media does.  The third view
    /// asks for its own frame first, because the reference's audio cover was a larger square than its audio
    /// tile; a skin that draws no such frame is answered with the grid's square, as it was before.
    /// </summary>
    private string? MusicTemplateKey
    {
      get
      {
        if (!IsMusic)
          return null;

        if (IsCoverLayout
            && SkinEngine.TryTileTemplate(MUSIC_COVER_TEMPLATE_KEY) != null)
          return MUSIC_COVER_TEMPLATE_KEY;

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
      // Music says no for want of shapes; the episode shelf says no for want of a choice - see SwitchMedia.
      get { return !IsMusic && !MediaCatalog.IsEpisode(AspectName); }
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

        // The music frames outrank every other frame the layout could ask for: the reference drew its audio
        // cover as a square too - a larger one than its audio tile, but never a film case - so walking a music
        // shelf from tiles into covers changes the size of the square and not its shape.
        return MusicTemplateKey ?? (IsCoverLayout
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
      OnPropertyChanged(nameof(ShowsContentInfo));

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
    /// The reading of one title went on or came to an end. The counters speak for a wall that is or is not on
    /// screen, and the details column is stood up by the reading itself whatever layout the screen wears - the
    /// reading is the one moment in which the facts are asked to be read rather than glanced at.
    /// </summary>
    partial void OnInfoShownChanged(bool value)
    {
      RaiseShelfCounters();
      OnPropertyChanged(nameof(ShowsContentInfo));
    }

    /// <summary>
    /// What the screen says at its bottom left about the title the walker stands on: the plain name, the year,
    /// and the kinds of thing it is - the home screen's line, in the home screen's words and letters, because a
    /// viewer who turned the details column off still wants to know what they are standing on.
    /// </summary>
    /// <remarks>
    /// The three pieces are what the row itself carries, read off the same row the home reads them off
    /// (<c>HomeViewModel.ContentInfo</c>), so walking a shelf of two hundred rows asks the server nothing: a line
    /// that had to ask would put one request behind every arrow key.  A name the importer wrote with its year
    /// ("Silo (2023)") says that year once, as its own piece, exactly as the home does.
    /// </remarks>
    public string ContentInfo
    {
      get
      {
        TitleViewModel? title = Selected;
        if (title == null)
          return string.Empty;

        List<string> parts = new List<string> { title.NameWithoutYear };
        if (title.Title.Year is int shown)
          parts.Add(shown.ToString(CultureInfo.InvariantCulture));
        IReadOnlyList<string> genres = title.Title.Genres ?? Array.Empty<string>();
        if (genres.Count > 0)
          parts.Add(string.Join(", ", genres));

        return string.Join("   ·   ", parts);
      }
    }

    /// <summary>
    /// Whether that line stands. It is the answer for the one moment in which the screen has nothing else to say
    /// about the stood-on title: the details column is off, no title is being read, and neither the menu's own
    /// word nor a dialog is speaking at the same place at the foot of the room.
    /// </summary>
    public bool ShowsContentInfo
    {
      get { return Selected != null && !ShowDetails && !InfoShown && !MediaMenuVisible && !MenuVisible; }
    }

    // The line stands or steps aside with all five answers above it, every one of which can change under a
    // viewer who never touches the line itself.
    partial void OnMenuVisibleChanged(bool value)
    {
      OnPropertyChanged(nameof(ShowsContentInfo));
    }

    partial void OnMediaMenuVisibleChanged(bool value)
    {
      if (!value)
      {
        //  The menu went away; the foot line must not keep naming a plate nobody is standing on.
        _menuTileLabel = "";
        _menuTileDetail = "";
      }
      OnPropertyChanged(nameof(MediaMenuFootLine));
      OnPropertyChanged(nameof(ShowsContentInfo));
      OnPropertyChanged(nameof(MenuMarkIcon));
    }

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
        // The stack and the carousel are the cover row wearing another shape, so the cover's own dial speaks for
        // their corners too - a viewer who switched the cover's corners off does not expect them back the moment
        // the same covers are stacked.
        BrowserLayout.Cover or BrowserLayout.Stacked or BrowserLayout.Carousel => settings.EnableCoverWatchedFlags,
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
    private BrowserLayout _layout = BrowserLayout.Carousel;

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
    /// <summary>
    /// Set while the READING itself moves its own selection - a press on a poster of the collection's band - and
    /// read by <see cref="OnSelectedChanged"/> so that the step does not send the wall back and take the caret with
    /// it.  See that method for the law and for the one case it answers to.
    /// </summary>
    private bool _readingHoldsTheSelection;

    partial void OnSelectedChanged(TitleViewModel? value)
    {
      OnPropertyChanged(nameof(SelectedNumber));
      OnPropertyChanged(nameof(ContentInfo));
      OnPropertyChanged(nameof(ShowsContentInfo));
      // A reading is about one title; walking onto another one, or off the end of the shelf, is the reading
      // ending by itself. The wall has to be back before the new row can be looked at.
      //
      // ONE reading answers that law to, and it is the reading's own: a film of the collection being read, asked
      // for by a press on that collection's poster, is a step ACROSS the reading and not a step back to the wall.
      // Sending the wall back for that moment took the caret with it (the shelf asks for it the moment the reading
      // goes away) and the new reading then stood it on the first card of its first band, so the viewer who pressed
      // a poster of the collection found the caret standing on somebody else's card (seen 2026-11-03).
      if (!_readingHoldsTheSelection)
        InfoShown = false;
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
      {
        Stopwatch blurred = Stopwatch.StartNew();
        worn = (await BackgroundBlur.BlurredAsync(raw, radius)) ?? raw;
        blurred.Stop();
        Services.HomeTrace.Log("fanartblur", "ms=" + (long)blurred.Elapsed.TotalMilliseconds + " bytes=" + raw.Length +
                                   " radius=" + radius.ToString("0.#"));
      }

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

      // A backdrop is the size of the television, so the frames this change costs are worth their line in the
      // ledger: a wall that stutters half a second after every stopped cover is stuttering here, not on the shelf.
      Services.HomeTrace.NextFrames("background", 3);
    }

    public string LayoutLabel
    {
      get
      {
        return Layout switch
        {
          BrowserLayout.Grid => "View: Grid",
          BrowserLayout.Cover => "View: Cover",
          BrowserLayout.Stacked => "View: Stacked",
          BrowserLayout.Carousel => "View: 3D Cover",
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
        // Music wears the album's cover in every layout, the layouts that stand squares and the rows alike:
        // a record calls its front picture a cover and not a poster (see ReadDetailsArtworkAsync), the cover
        // view's bytes are already warm for the panel, and the database's own blob is silent for most albums -
        // so the stood-on row's square is filled from the same source the square tiles and the panel read.
        if (IsMusic)
          return CoverArtwork ?? _artwork;

        // A season's cover is the poster it wears on its tiles, which is not in the database - see TileArtworkFor.
        // A movie's cover is the poster too: the reference sized its cover frame for a poster and asked
        // FanArtType="Poster" for it (MediaStyles.xaml:5253), and the database's thumbnail blob - which is what
        // _artwork reads - is a wide letterbox picture at best and empty for most of what the importers write,
        // which left the cover wall a row of bare grey plates.
        if (IsCoverLayout)
          return TileArtworkFor(GridViewType.Poster) ?? _artwork;

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
        _coverArtwork = PicturesOf(client, PictureWidths.Shelf("Cover", PictureWidths.COVER),
            id => client.GetFanArtAsync(id, "Cover", null, PictureWidths.COVER));
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
      // started from.  The two cover shapes this front end adds stand between the covers and the rows, so the
      // walk stays one loop over every shape the screen knows: a button that skipped two of five would leave a
      // viewer pressing it again for a shape they had already seen.
      Layout = Layout switch
      {
        BrowserLayout.List => BrowserLayout.Grid,
        BrowserLayout.Grid => BrowserLayout.Cover,
        BrowserLayout.Cover => BrowserLayout.Stacked,
        BrowserLayout.Stacked => BrowserLayout.Carousel,
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
      // on the screen at all. See CanChooseMedia. The episode shelf is not in it either, for a sharper reason:
      // an importer writes an episode a thumbnail and nothing else, so a poster shelf of episodes is a wall
      // of empty plates and a banner shelf the same wall sideways. The shape is not a choice here; it is
      // what an episode is.
      if (IsMusic || MediaCatalog.IsEpisode(AspectName))
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

    /// <summary>
    /// Whether the shelf is read against the direction of its order.  It is not a question of its own on the
    /// menu: picking the order that already stands turns it around, which is what a viewer does with a list they
    /// have just looked at - the same shelf, only the other way round.
    /// </summary>
    public bool SortDescending { get; private set; }

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
    /// What the wall lays out: the same titles as <see cref="Titles"/>, and - while the shelf is grouped -
    /// a <see cref="GroupHeaderRow"/> in front of every run of titles that carries one group's value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The typed <see cref="Titles"/> stays the shelf's memory - what is selected, counted, played and tested -
    /// and answers exactly the same questions it always answered. The wall, on the other hand, has to lay the
    /// groups down where the viewer can see them, and a divider that stands between two covers of one row can
    /// only be an item OF that row: the panels know nothing else, the carousel paints items, and the caret walks
    /// containers. So the wall reads this list, and this list is nothing but <see cref="Titles"/> with headers
    /// threaded through it - the same title objects, never copies, so a tile the wall wears IS the row the
    /// shelf remembers.
    /// </para>
    /// <para>
    /// Ungrouped the two lists hold exactly the same items in the same order, which is why nothing about an
    /// ungrouped shelf changed when this list went in. Grouped, a header is never selectable (the view hands
    /// those containers no caret) and never a title - the two lists differ only by those plates.
    /// </para>
    /// </remarks>
    public ObservableCollection<object> Rows { get; } = new ObservableCollection<object>();

    /// <summary>
    /// The category this shelf is grouped by - <c>genre</c>, <c>year</c>, <c>actors</c>, <c>directors</c> or
    /// <c>writers</c> - or null while it is read as one shelf. This is NOT <see cref="GroupBy"/>, which is the
    /// server's word for a CUT: a grouping keeps every title and only says which ones stand together.
    /// </summary>
    public string? Grouping { get; private set; }

    /// <summary>Whether the wall threads group plates through its titles right now.</summary>
    public bool IsGrouped => !string.IsNullOrEmpty(Grouping);

    /// <summary>The words the screen says about its grouping, which is "Group: None" while it groups nothing.</summary>
    public string GroupLabel
    {
      get { return IsGrouped ? "Group: " + Capitalise(CategoryWord(Grouping!)) : "Group: None"; }
    }

    /// <summary>
    /// Puts the shelf under one category's values, or takes the grouping off again. Every title stays - a
    /// grouping is a way of standing the shelf, not a way of shortening it - but the reading goes back to its
    /// beginning, because a grouped shelf is built group by group and half-grouped titles are no shelf at all.
    /// </summary>
    public async Task PickGroupAsync(string? by)
    {
      string goes = string.IsNullOrWhiteSpace(by) ? "" : by.Trim().ToLowerInvariant();
      if (string.Equals(Grouping ?? "", goes, StringComparison.OrdinalIgnoreCase))
      {
        HideMediaMenu();
        return;
      }

      Grouping = goes.Length == 0 ? null : goes;
      OnPropertyChanged(nameof(IsGrouped));
      OnPropertyChanged(nameof(GroupLabel));
      HideMediaMenu();
      await ReaskAsync();
    }

    /// <summary>
    /// One group of the grouped shelf: the value, what the server counted under it, how much of it this screen
    /// has read, and where its plates end in <see cref="Rows"/> - which is where its next page has to go.
    /// </summary>
    private sealed class ShelfGroupRun
    {
      public ShelfGroupRun(string value, int count)
      {
        Value = value;
        Count = count;
        Total = count;
      }

      public string Value { get; }
      public int Count { get; }

      /// <summary>
      /// What the group's own last answer called its total: the group AS THE SHELF IS NARROWED, which is the
      /// number the foot's sum may use. Until the group has answered, the whole-shelf count stands in for it.
      /// </summary>
      public int Total;

      /// <summary>Titles of this group read so far - the offset its next question goes out with.</summary>
      public int Offset;

      /// <summary>What the server's last answer still owed this group.</summary>
      public int Remaining;

      /// <summary>Where in <see cref="Rows"/> this group's titles end and the next plate begins.</summary>
      public int RowsEnd;
    }

    /// <summary>The groups of the shelf being read, in the order the server counted them (biggest first).</summary>
    private readonly List<ShelfGroupRun> _shelfGroups = new List<ShelfGroupRun>();

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
    /// Whether this shelf is worn as <em>covers</em>, which is the family of layouts the reference laid out
    /// sideways: the row of posters along the bottom edge, their details centred above them, and the screen's
    /// menu under them instead of beside them.  Three shapes wear that family - the walked row, the stack and the
    /// carousel - and everything the screen does besides putting the covers down is the same in all three, which
    /// is why they answer this one question together.
    /// </summary>
    public bool IsCoverLayout => Layout is BrowserLayout.Cover or BrowserLayout.Stacked or BrowserLayout.Carousel;

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
    public bool ShowsSideDetails => (ShowDetails && !IsCoverLayout) || InfoShown;

    /// <summary>
    /// Whether the details lie centred above the covers, on the horizontal gradient the reference laid under
    /// <c>MultiDetailsCover.inc</c> - the cover layout's answer, which never has a panel beside its row.
    /// </summary>
    public bool ShowsCoverDetails => ShowDetails && IsCoverLayout;

    /// <summary>
    /// Whether the shelf draws the reference's floor gradient along its bottom edge: the dark band that lifts the
    /// names off the fanart.  The reference's media master laid it under grid and list whatever the details said,
    /// and only the cover view - which lays its own shadows over the wall of posters - went without it.
    /// </summary>
    public bool ShowsFloorGradient => !IsCoverLayout && Titles.Count > 0;

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
      OnPropertyChanged(nameof(MenuMarkIcon));
    }

    /// <summary>
    /// Raised when a viewer asked to watch a title and this view model decided it is watchable. The application
    /// owns the player, so it decides what that means; this class only says which file it handed over.
    /// </summary>
    public event EventHandler<TitleViewModel>? PlayRequested;

    /// <summary>
    /// Raised when a viewer asked to see one of a title's short films. The application owns the player and the
    /// windows, so it decides what that means; this class only says whose trailer was asked for and which one.
    /// </summary>
    /// <remarks>
    /// A separate event from <see cref="PlayRequested"/> on purpose, and the difference is what happens
    /// afterwards: watching a title is reported to the library and leaves the viewer in the player, while a
    /// trailer is nothing the library should remember and ends back on the tile it was asked for. Folding the two
    /// into one event would mean a two minute advertisement counting as a viewing.
    /// </remarks>
    public event EventHandler<TrailerRequest>? TrailerRequested;

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
      Rows.Clear();
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
    /// Puts the search field on the screen and asks for the caret to be moved into it - one row of the shelf's
    /// menu, the Find key, or the arrow lifted off the head of the wall while the field already stands there.
    /// </summary>
    /// <remarks>
    /// Both halves belong together and neither alone is an answer: the field is only on the screen while a search
    /// is open, so asking for its caret without opening it moves the caret onto a control that is not there -
    /// which is exactly the nothing-that-happened the menu's Search row used to be.  Nothing on the screen waits
    /// for this call: the head line keeps empty until a viewer asks (2026-11-10), so there is no mark to press,
    /// and while no field is up the arrow at the head of the wall is the wall's own nod rather than a way in.
    /// </remarks>
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
    /// The wall follows the field while it is being written in: a hand that has rested on the last letter for
    /// <see cref="TYPING_PAUSE_MILLISECONDS"/> is asking a question, so the words so far are sent as one.  The
    /// round trip of the reference, which waited for Enter, left a viewer typing blind - the wall only moved
    /// after a key nobody presses when they can already see what they meant.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A timer rather than a question per letter, because a shelf of two thousand films is asked over the network
    /// and "alien" spelled by a remote is five questions otherwise.  Every letter restarts the pause, so it is
    /// the last letter that is timed.
    /// </para>
    /// <para>
    /// Nothing here has to survive a race: a read belongs to a generation (see <see cref="ReadTheRestAsync"/>),
    /// and the newer question bumps the number, so the answer to "alin" that arrives after "alien" was asked is
    /// thrown away by itself.
    /// </para>
    /// <para>
    /// What the program writes into the box is not a viewer asking.  Clearing takes the words and the filter
    /// together in one go (see <see cref="ClearSearchAsync"/>), and a second question thirty
    /// hundredths of a second later would ask for the whole shelf a second time.
    /// </para>
    /// </remarks>
    partial void OnSearchTextChanged(string value)
    {
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
      await FindAsync();
    }

    /// <summary>
    /// Filters the list to the titles carrying the words in the search box. Enter on the box and the Find button
    /// both lead here, and so does a hand that has stopped typing.
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
      // Enter answers at once, so a question that was already waiting out the typing pause is not wanted twice.
      _typing?.Stop();

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

    /// <summary>Puts the whole shelf back.  The X of the field and an Enter on an empty field come here.</summary>
    [RelayCommand]
    public async Task ClearSearchAsync()
    {
      // Taking the words away is one errand, not a viewer asking: the shelf comes back here and now, and the box
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
      {
        ActiveSearch = "";
        ClearDetailsForNewList();
      }

      await ReadAsync(0);
    }

    /// <summary>
    /// Takes the search away whole: the field goes off the head line, its words and the filter they made with it,
    /// and the wall stands full again.  Back and Escape come here.
    /// </summary>
    /// <remarks>
    /// This is not <see cref="ClearSearchAsync"/> with the field hidden afterwards, and the difference is what a
    /// viewer can reach: clearing leaves the field standing empty under the caret, so a second press of the same
    /// key is needed to be rid of it - which is the "there is no way out" a viewer with a remote reported on
    /// 2026-11-10, having no key that says "and now put the field away too".
    /// </remarks>
    [RelayCommand]
    public async Task CloseSearchAsync()
    {
      SearchOpen = false;
      await ClearSearchAsync();
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
      Rows.Clear();
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

      _menuAsksForTrailer = false;
      _menuAsksForQuality = null;
      RaiseMenuState();
      MenuAnswer = "";
      MenuVisible = true;
    }

    /// <summary>
    /// Puts the menu away - the back key's answer while a menu is the newest thing on the screen, and the
    /// last button in the menu itself.
    /// </summary>
    [RelayCommand]
    public void CloseMenu()
    {
      _menuAsksForTrailer = false;
      _menuAsksForQuality = null;
      MenuVisible = false;
    }

    /// <summary>
    /// Puts the wall of tiles back, which is what the back key answers while a reading is the newest thing on the
    /// screen, and what the reading's own button does. The selection stays where it was: the viewer walked onto a
    /// title, read it, and goes back onto that same title rather than to the head of the shelf.
    /// </summary>
    [RelayCommand]
    public void CloseInfo()
    {
      InfoShown = false;
    }

    /// <summary>
    /// The lines the title's dialog shows: the answers this title has, worked out by the same table the home wall
    /// uses (<see cref="MediaTitleActions"/>), in the reference's order. The dialog used to carry one hard-built
    /// button per answer, which is the reason the second answer had to be added by hand and a third one would be
    /// added the same way again.
    /// </summary>
    public ObservableCollection<MediaTitleAction> TitleActions { get; } =
        new ObservableCollection<MediaTitleAction>();

    /// <summary>
    /// Gives the answer that was stood on. Every line goes through here so that the dialog is put away in one
    /// place, and so that a line this screen cannot answer is a bug in the table rather than a dead button.
    /// </summary>
    [RelayCommand]
    private async Task RunTitleActionAsync(MediaTitleAction? action)
    {
      if (action == null || !action.IsCommand)
        return;

      switch (action.Kind)
      {
        case MediaTitleActionKind.Information:
          // The reference pushed a screen of its own for this reading (MediaInfoAction.cs:69-90 →
          // MediaInfoVideo.xaml): the title's picture filling the room, its facts beside it, and no wall of
          // tiles standing between the viewer and the reading. This screen already carries those facts in the
          // column beside the shelf, so the reading does not copy them - it takes the wall away, which is what
          // makes the reference's screen a screen and not a second copy of the same words.
          MenuVisible = false;
          await ReadSelectedAsync();
          break;

        case MediaTitleActionKind.SetPlayed:
          await MarkWatchedAsync();
          break;

        case MediaTitleActionKind.SetUnplayed:
          await MarkUnwatchedAsync();
          break;

        case MediaTitleActionKind.Refresh:
          await RefreshSelectedAsync();
          break;

        case MediaTitleActionKind.Trailer:
          // One trailer is an answer and is played; several make the line a question, which this dialog answers
          // by becoming the question - the same frame, now holding one line per film. A second dialog pushed over
          // the first would be a room the viewer never walked into, and the back key would have to know about it.
          MediaTitleAction? only = MediaTitleActions.TrailerToPlay(Selected);
          if (only == null)
          {
            _menuAsksForTrailer = true;
            RaiseMenuState();
            MenuAnswer = "Which trailer?";
          }
          else if (MediaTitleActions.AsksForQuality(only.Trailer))
          {
            // The same frame again, now holding one line per picture size, because the film behind a video site's
            // page comes in several of them and the page does not say which was meant. The lines are the question
            // on their own; a sentence over them would only have to be worded badly.
            _menuAsksForQuality = only.Trailer;
            RaiseMenuState();
            MenuAnswer = "";
          }
          else
          {
            PlayTrailer(only.Trailer);
          }
          break;

        case MediaTitleActionKind.TrailerChoice:
          PlayTrailer(action.Trailer);
          break;

        case MediaTitleActionKind.TrailerQuality:
          PlayTrailer(action.Trailer, action.Quality);
          break;
      }
    }

    /// <summary>
    /// Asks the application to show one of the selected title's trailers, and puts the menu away while it is at it.
    /// </summary>
    /// <remarks>
    /// The dialog is put away before the ask, exactly as every other line does it: what a press opens stands over
    /// the shelf, not over a question that has already been answered. Nothing about the trailer is said here -
    /// neither the wait nor the reason - because a trailer is answered minutes after the press and in whole
    /// sentences, which is what the application's own trailer dialog is for.
    /// </remarks>
    private void PlayTrailer(MediaTrailer? trailer, Services.TrailerQuality? quality = null)
    {
      TitleViewModel? title = Selected;
      _menuAsksForTrailer = false;
      _menuAsksForQuality = null;

      if (title == null || trailer == null)
        return;

      MenuVisible = false;
      TrailerRequested?.Invoke(this, new TrailerRequest(title, trailer, quality));
    }

    /// <summary>
    /// Whether the open menu is asking which of the selected title's trailers was meant, which decides whether it
    /// shows the title's answers or its films. Cleared whenever the menu is opened or put away.
    /// </summary>
    private bool _menuAsksForTrailer;

    /// <summary>
    /// The film the open menu is asking a picture size for, or nothing while it asks nothing. Third step of the
    /// same dialog: a film behind a video site's page stands in several sizes and the page does not name one.
    /// </summary>
    private MediaTrailer? _menuAsksForQuality;

    /// <summary>
    /// Fills <see cref="TitleActions"/> for the title the dialog was opened for.
    /// </summary>
    /// <remarks>
    /// The one line that only explains something is left out here on purpose: this dialog already speaks its
    /// reason through <see cref="MenuWords"/>, and a dialog that says the same sentence twice - once as a line
    /// that cannot be pressed and once under it - reads like a mistake rather than like an answer.
    /// </remarks>
    private void RebuildTitleActions()
    {
      TitleActions.Clear();
      IEnumerable<MediaTitleAction> lines = _menuAsksForQuality != null
          ? MediaTitleActions.QualityChoicesOf(_menuAsksForQuality)
          : _menuAsksForTrailer
              ? MediaTitleActions.TrailerChoicesOf(Selected)
              : MediaTitleActions.For(Selected, _client != null, true);
      foreach (MediaTitleAction action in lines)
      {
        if (action.Kind == MediaTitleActionKind.SaidOnly && !string.IsNullOrEmpty(MenuWords))
          continue;
        TitleActions.Add(action);
      }
    }

    /// <summary>
    /// Tells the library that the selected title was watched to the end, and folds the server's answer into
    /// the row.
    /// </summary>
    /// <remarks>
    /// The row is rewritten from the server's answer and not from the press - the same rule the end of a real
    /// film follows through <see cref="ApplyPlayback"/> - so the tick says what the library believes.
    /// </remarks>
    [RelayCommand]
    public async Task MarkWatchedAsync()
    {
      await ReportWatchedAsync(100);
    }

    /// <summary>
    /// Takes the watched mark back, as the reference's <i>Set unplayed</i> does, by reporting the position at
    /// nought. The library keeps the count and the date and clears the position
    /// (<c>MediaLibrary.cs:2852-2858</c>); a title nobody is signed in as cannot have its position cleared at all,
    /// and <see cref="ReportWatchedAsync"/> says so rather than letting the tick sit there unchanged.
    /// </summary>
    [RelayCommand]
    public async Task MarkUnwatchedAsync()
    {
      await ReportWatchedAsync(0);
    }

    /// <summary>
    /// The one road both marks walk: tell the server how far this title was watched and rebuild the row from
    /// what it answers back.
    /// </summary>
    private async Task ReportWatchedAsync(int percentage)
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
        MediaPlayback after = await client.ReportPlaybackAsync(title.Title.Id, percentage);
        ApplyPlayback(after);
        if (percentage >= 100)
          Status = after.Watched
              ? title.Name + " is watched now as far as the library is concerned"
              : title.Name + " - the server took the report but does not count it as finished";
        else if (after.Resume == null && after.PlayCount == 0)
          Status = title.Name + " is unwatched now, and the library holds no viewing of it";
        else if (!IsSignedInView())
          // Nothing was taken back: a viewing that belongs to nobody has no position to return to the start.
          Status = title.Name + " - the mark belongs to a viewer, and this screen speaks for nobody. Sign in on "
              + "the server screen and the mark can be taken back.";
        else
          Status = title.Name + " is unwatched now; the library still remembers " + after.PlayCount
              + (after.PlayCount == 1 ? " viewing" : " viewings") + " of it";
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

    /// <summary>
    /// Asks the library to read the selected title's file again, which is the reference's <i>Refresh</i> entry
    /// and its one call (<c>RefreshMediaItem.cs:60-69</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sentence says what was asked and not what was found, because at this moment nothing was found: the
    /// library marks the title dirty and hands its folder to the importer worker
    /// (<c>MediaLibrary.cs:1008-1034</c>), which reads the file when it comes to it. A screen that promised new
    /// figures here would show the row unchanged and read as though the press had done nothing.
    /// </para>
    /// <para>
    /// The dialog is left standing for this one answer, unlike the two watched marks: a mark rewrites the row the
    /// viewer is looking at and can therefore take the question away, while an ask that is only being given shows
    /// nothing moving anywhere - so it is said in the dialog's own line, under the buttons the press came from,
    /// and the viewer puts the dialog away when they have read it.
    /// </para>
    /// </remarks>
    private async Task RefreshSelectedAsync()
    {
      TitleViewModel? title = Selected;
      ServerApiClient? client = _client;
      if (title == null || client == null)
      {
        Status = "There is no server to ask about " + (title == null ? "a title" : title.Name);
        return;
      }

      // Said where the press came from (the dialog's own line) and on the screen's line as well, which is only
      // read while the shelf is empty.
      void Say(string words)
      {
        Status = words;
        MenuAnswer = words;
      }

      Say("Asking the library to look at " + title.Name + " again");
      try
      {
        RefreshAnswer answer = await client.RefreshTitleAsync(title.Title.Id);
        Say(string.IsNullOrEmpty(answer.Error)
            ? "The library will read " + title.Name + " again and say what it finds"
            : title.Name + " - " + answer.Error);
      }
      catch (ServerApiException ex)
      {
        if (ex.NotReady)
          Say("The server is not ready yet: " + ex.Message);
        else if (ex.StatusCode == 404)
          Say("The library no longer holds this title, so there is nothing left to read again");
        else
          Say(ex.Message);
      }
      catch (Exception ex)
      {
        Say("Asking the library failed: " + ex.Message);
      }
    }

    /// <summary>
    /// Whether this screen speaks for a viewer, which decides whether a watched mark can be taken back at all:
    /// the library keeps the mark on a person, so an anonymous report has nothing to clear
    /// (<c>MediaLibrary.cs:2852-2858</c> against <see cref="MediaLibrary.NotifyPlayback"/>).
    /// </summary>
    private bool IsSignedInView()
    {
      ServerApiClient? client = _client;
      return client != null && !string.IsNullOrEmpty(client.AccessToken);
    }

    /// <summary>The title the menu was opened for, or nothing while no row is picked.</summary>
    public string MenuTitle
    {
      get { return Selected == null ? "" : Selected.Name; }
    }

    /// <summary>
    /// The words the menu says instead of offering a button: why there is nothing to press. Every case is
    /// said, so the menu is never a frame with a hole where its purpose was. One of the two marks is offered
    /// for every title a server stands behind, so what is left to explain is the server being gone.
    /// </summary>
    public string MenuWords
    {
      get
      {
        if (Selected == null)
          return "Nothing is selected.";
        if (_client == null)
          return "There is no server to tell.";
        return "";
      }
    }

    private void RaiseMenuState()
    {
      OnPropertyChanged(nameof(MenuTitle));
      OnPropertyChanged(nameof(MenuWords));
      RebuildTitleActions();
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

      /// <summary>
      /// The categories the shelf can be GROUPED by - which is the same counting as <see cref="Filter"/>, only
      /// asked the other way round: a filter picks one value's titles out and throws the rest away, a grouping
      /// keeps every title and stands each one under its value's plate.
      /// </summary>
      Group,

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

    /// <summary>
    /// The line at the screen's foot while the menu is out: the plate of the rail the caret stands on, together
    /// with whatever that plate has to add.  A plate carries a picture and its name and has no room for more, so
    /// the second word a command has - the end an order reads the shelf from - is said down here, where a whole
    /// line is free.  Until the caret reaches a plate the line only names the page the rail is showing.
    /// </summary>
    public string MediaMenuFootLine
    {
      get
      {
        if (_menuTileLabel.Length == 0)
          return MediaMenuHeading;
        return _menuTileDetail.Length == 0 ? _menuTileLabel : _menuTileLabel + " · " + _menuTileDetail;
      }
    }

    private string _menuTileLabel = "";
    private string _menuTileDetail = "";

    /// <summary>
    /// Which plate of the rail the caret stands on.  Only the view knows: its arrows hand the caret from row to
    /// row by moving the focus, so the view reports the plate every time the caret walks.
    /// </summary>
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

    /// <summary>
    /// A page of the rail being replaced means every plate the foot line ever spoke of is gone with it; the line
    /// falls back to the page's name until the caret stands on a new plate.
    /// </summary>
    partial void OnMediaMenuHeadingChanged(string value)
    {
      _menuTileLabel = "";
      _menuTileDetail = "";
      OnPropertyChanged(nameof(MediaMenuFootLine));
    }

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
    /// The name of the icon the pointer's handle to this menu wears, which the view asks the worn skin about:
    /// the picture of the MOVE the click makes rather than of the state it finds, which is how the catalog these
    /// icons come out of names them - a panel that lies shut behind its edge is drawn with the arrow pointing
    /// the way the panel goes, and the same panel stood up is drawn with the arrow pointing back.
    /// </summary>
    /// <remarks>
    /// The axis travels with the shape of the shelf, because that is what decides where the menu lies: covers
    /// (walked, stacked or three dimensional) are opened off the room's bottom edge and everything else off its
    /// left. A handle whose arrow points at an edge this screen has no menu behind would send the viewer's eye
    /// to a place nothing ever comes out of.
    /// </remarks>
    public string MenuMarkIcon => IsCoverLayout
        ? MediaMenuVisible ? "bottom_panel_close" : "bottom_panel_open"
        : MediaMenuVisible ? "left_panel_close" : "left_panel_open";

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
          // The two shapes besides the walked row are the same covers put down differently, which is why they sit
          // beside "Cover" and not under a question of their own: a viewer looking for "the covers, but stacked"
          // looks where the covers are.
          AddMenuRow("Stacked", "view_stacked", new RelayCommand(() => PickLayout(BrowserLayout.Stacked)), Layout == BrowserLayout.Stacked);
          // The viewer's word for this shape is "3D Cover" - what it looks like, said to somebody standing at
          // the television - while the code keeps calling the layout by the ring of covers it lays out.
          AddMenuRow("3D Cover", "view_carousel", new RelayCommand(() => PickLayout(BrowserLayout.Carousel)), Layout == BrowserLayout.Carousel);
          // The reference kept the details switch beside the view modes rather than among the media commands,
          // because details are the second half of the layout question and not a command of their own. The one
          // row the screen gives it is this one: the main page asking the same question a second time only
          // made the viewer choose which "Details" to press.
          AddMenuRow(DetailsLabel, "details", new RelayCommand(PickDetails), ShowDetails);
          return;

        case MenuPage.Picture:
          MediaMenuHeading = "Picture";
          // An episode shelf never opens here - its rail row is gone and its rotation stands still - but a
          // page that could be reached by some other road would still have to lie about having no choice.
          if (!MediaCatalog.IsEpisode(AspectName))
          {
            AddMenuRow("Poster", "poster", new RelayCommand(() => PickPicture(GridViewType.Poster)), _tileKind == GridViewType.Poster);
            AddMenuRow("Banner", "banner", new RelayCommand(() => PickPicture(GridViewType.Banner)), _tileKind == GridViewType.Banner);
          }
          AddMenuRow("Thumbnail", "thumbnail", new RelayCommand(() => PickPicture(GridViewType.Thumbnail)), _tileKind == GridViewType.Thumbnail);
          return;

        case MenuPage.Sort:
          MediaMenuHeading = "Sort";
          foreach ((string Word, string Key) choice in SortChoices())
          {
            string picked = choice.Key;
            bool stands = string.Equals(SortOrder, picked, StringComparison.OrdinalIgnoreCase);
            // Only the order's name fits the plate; the end it starts at goes into the line at the screen's foot,
            // where there is room for it.  A row that stands answers with the end the shelf is read from now -
            // pressing it again is what turns that around.
            AddMenuRow(choice.Word, SortIcon(picked), new AsyncRelayCommand(() => PickSortAsync(picked)), stands,
                SortDirectionWord(picked, stands && SortDescending));
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

        case MenuPage.Group:
          MediaMenuHeading = "Group";
          // Grouping needs no values page: a filter asks WHICH value's titles to keep, and only the counting
          // knows the values worth asking for; a grouping keeps them all and stands them under their own
          // plates, which is a thing a whole category does at once.
          AddMenuRow("None", "library_books", new AsyncRelayCommand(() => PickGroupAsync(null)), !IsGrouped);
          foreach (BrowseCategory category in BrowseViewModel.CATEGORIES)
          {
            string grouped_by = category.Word;
            AddMenuRow(category.Label, CategoryIcon(grouped_by),
                new AsyncRelayCommand(() => PickGroupAsync(grouped_by)),
                IsGrouped && string.Equals(Grouping, grouped_by, StringComparison.OrdinalIgnoreCase));
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
            // The same call the Find key makes, and not merely the ask for the caret: the field is only on the
            // screen while a search is open, so a row that only asked for the caret moved it into a field that
            // was never put up - the row did nothing at all, which the viewer caught on 2026-11-10.
            HideMediaMenu();
            OpenSearch();
          }));
          AddMenuRow(FilterLabel, "filter", new RelayCommand(() => GoToPage(MenuPage.Filter)));
          // Grouping stands beside filtering because it is the same counting asked the other way round, and a
          // viewer who knows one looks for the other exactly where the other lives.
          AddMenuRow(GroupLabel, "group", new RelayCommand(() => GoToPage(MenuPage.Group)));
          AddMenuRow(SortLabel, "sort", new RelayCommand(() => GoToPage(MenuPage.Sort)), false,
              SortDirectionWord(SortOrder));
          AddMenuRow(LayoutLabel, "grid_view", new RelayCommand(() => GoToPage(MenuPage.View)));
          if (CanChooseMedia)
            AddMenuRow(MediaLabel, "media_format", new RelayCommand(() => GoToPage(MenuPage.Picture)));
          // No watched row here: it is a filter, and it stands on the "Filter" page with the rest of them.
          // No details row here either: the switch stands on the "View" page, beside the layouts it belongs
          // to, and the same question asked twice in one rail only makes the viewer guess which is real.
          // No title menu row either: a title's own dialog is asked of the title - a right press on its
          // tile, or the info key - and not of the screen's rail, which speaks about the shelf.
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
    private void AddMenuRow(string label, string icon, System.Windows.Input.ICommand command, bool isCurrent = false,
        string detail = "")
    {
      MediaMenu.Add(new MediaBrowserMenuEntry(label, icon, command, isCurrent, detail));
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
      // The walk is round, so the bound is a whole turn of the cycle: with five shapes, four steps can lie between
      // two of them, and a row that stopped one step short of the shape it names would be a row that lied.
      for (int step = 0; step < 5 && Layout != layout; step++)
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
    /// <para>
    /// The shelf is recognised by <see cref="MediaCatalog"/>'s own tests, which speak the server's names for it -
    /// "EpisodeItem" and "AudioItem".  Comparing against the plain word the screen itself says ("episodes", "tracks")
    /// matched nothing, which is how a shelf of episodes came to be offered every order except its own number.
    /// </para>
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
        // A shelf of seasons is one tile per season, so their number is the order the viewer walks them in.
        choices.Insert(1, ("by season number", "season"));
      else if (MediaCatalog.IsEpisode(AspectName))
        // An episode screen holds one season only, so asking its number would ask a question the screen already
        // answered by opening: every row carries the same season.
        choices.Insert(1, ("by episode number", "episode"));
      else if (MediaCatalog.IsTrack(AspectName))
        choices.Insert(1, ("by track number", "track"));

      return choices;
    }

    /// <summary>
    /// Reads the shelf again in another order - and, when the order that already stands is picked a second time,
    /// in the other direction.  The list is thrown away rather than re-sorted on screen, because the screen only
    /// ever holds the pages it has read: eleven hundred films sorted by year have to be re-asked, not shuffled,
    /// or the first fifty the viewer is looking at are the wrong fifty.
    /// </summary>
    public async Task PickSortAsync(string sort)
    {
      if (string.IsNullOrWhiteSpace(sort))
      {
        HideMediaMenu();
        return;
      }

      if (string.Equals(SortOrder, sort, StringComparison.OrdinalIgnoreCase))
      {
        // Asked twice is not the same question twice - it is the shelf the other way round.  A viewer standing in
        // a list read A to Z who picks "by title" again means Z to A; answering with "already done, menu away"
        // leaves the second half of the order unreachable, and no other row of this menu reaches it either.
        SortDescending = !SortDescending;
      }
      else
      {
        // A new order starts straight: "by year" said without more is a question about the shelf, not about its
        // end, and carrying a stale "reversed" over from the order before it would answer a question nobody asked.
        SortOrder = sort;
        SortDescending = false;
      }

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

    /// <summary>
    /// The screen's word for the end of the order the shelf starts at.  Spelled per kind of order rather than as
    /// one word for every one of them, because "Z to A" over a shelf of years is a sentence about letters the
    /// shelf holds none of.
    /// </summary>
    private string SortDirectionWord(string sort) => SortDirectionWord(sort, SortDescending);

    /// <summary>
    /// The word for the end of one order when it is read from a given end.  An order that already stands names the
    /// end it is read from today; an order nobody has picked yet starts at its front.
    /// </summary>
    private static string SortDirectionWord(string sort, bool reverse)
    {
      switch (sort)
      {
        case "title":
          return reverse ? "Z to A" : "A to Z";
        case "year":
        case "added":
          return reverse ? "newest first" : "oldest first";
        case "lastplayed":
          return reverse ? "least recent first" : "most recent first";
        default:
          //  The numbered orders - season, episode, track - which are watched from one end to the other.
          return reverse ? "last to first" : "first to last";
      }
    }

    /// <summary>The skin's own icon for an order, where the skin carries one.</summary>
    private static string SortIcon(string sort)
    {
      switch (sort)
      {
        case "year":
          return "year";
        case "added":
          return "added";
        case "lastplayed":
          return "lastplayed";
        // The numbered orders are read row by row from the shelf's first title to its last, which is what a list
        // with numbers in its margin says.  The disc's note stays where it is right: the place on a record.
        case "season":
        case "episode":
          return "episode";
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
    /// Stands the reading of the selected title up: the wall goes away (the view obeys <see cref="InfoShown"/>)
    /// and the title's facts are read for it, from the server if they were never read.
    /// </summary>
    /// <remarks>
    /// The reading does not wait on the viewer's standing answer to the details question the way a walk over the
    /// shelf does: somebody who pressed <i>Media information</i> asked to read this title whatever they usually
    /// wear on this screen, and a reading that came back as an empty frame because the column is switched off
    /// would be the reference's screen with nothing on it.
    /// </remarks>
    private async Task ReadSelectedAsync()
    {
      TitleViewModel? title = Selected;
      InfoShown = true;
      if (title == null || _client == null)
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
    /// Reads one of the collection's films, which is what a viewer standing on its poster in the collection's row
    /// and pressing it asks for.
    /// </summary>
    /// <param name="card">The poster that was stood on and pressed.</param>
    /// <remarks>
    /// <para>
    /// This is the screen's own way into a title and not a second one: the row is put on <see cref="Selected"/> and
    /// the reading is raised by <see cref="ReadSelectedAsync"/>, so the room's picture follows the new film as it
    /// follows a walk over the shelf, and the facts are read by the same errand.  Where the pages of the shelf
    /// already hold the film its own row is reused, which keeps the count at the shelf's foot telling the truth
    /// about which row is being read.
    /// </para>
    /// <para>
    /// Nothing is started, whatever else on this screen the same press means.  The reference's screen for a
    /// collection was a list of its films whose entries opened the film's information
    /// (<c>MediaItemListMovieCollectionDetails.inc</c>), and a viewer pressing a film of the collection from inside
    /// a reading asks what the library says about that film - which is the question every other card of this
    /// reading answers and never the question of playing one.
    /// </para>
    /// </remarks>
    public async Task OpenCollectionFilmAsync(CollectionFilmCard card)
    {
      ServerApiClient? client = _client;
      if (card == null || client == null)
        return;

      TitleViewModel? row = null;
      foreach (TitleViewModel stood in Titles)
      {
        if (stood.Title.Id == card.Id)
        {
          row = stood;
          break;
        }
      }

      // A film of the collection that the pages read so far do not carry is still a title of this shelf and is
      // stood on as the shelf would stand on it - the same server for its stream, the same aspect, the same answer
      // about whether this machine can open its file.  The foot's count then says nought for it, which is honest:
      // the row being read is not one of the rows the wall put up.
      row ??= new TitleViewModel(card.Source, DrillsIn, client, AspectName);

      // The reading steps ONTO one of its own collection's films, and that is a step inside the reading: the wall
      // is not asked to come back between the two, and the caret the viewer put on the pressed poster stays there.
      // Without this, OnSelectedChanged sends the reading away for one beat, the shelf takes the caret in that
      // beat, and the new reading stands it on the first card of its first band - see the law written there.
      _readingHoldsTheSelection = true;
      try
      {
        Selected = row;
        await ReadSelectedAsync();
      }
      finally
      {
        _readingHoldsTheSelection = false;
      }
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

      // A question about the same title that is already on its way is not asked a second time; see the field.
      if (_detailsReadUnderway == asked)
        return;

      _detailsReadUnderway = asked;
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
        // The heading says the series' name and nothing else: as big as it is, it is the line a viewer reads
        // first, and whatever else it carries it carries twice. A show keeps its name - free of the year, as the
        // importers write it - on its own aspect, which the server hands over as "seriesName" for a show, for one
        // of its seasons and for one of its episodes alike (MediaController:1296-1328); the reference's
        // cover-details templates drew that same yearless name (SeriesAspectWrapper.SeriesName). The year and the
        // episode's own number and name all stand in the facts band under the rating already, so the heading
        // leaves them there - "Alien: Earth" over "S01E01 · Nimmerland" in words, rather than the whole sentence
        // written twice. Only where a title belongs to no series at all - a film - does its own name head the
        // panel, and then without the year, which the facts name too.
        // A tune is headed by its own name and by nothing else.  MP2's importer spells a track's TITLE with its
        // record and its place in front of the tune - "<album>: <number> - <tune>" - which is what the library
        // holds as the title (TrackInfo.ToString at TrackInfo.cs:658-667, through TrackInfo.TRACK_FORMAT_STR at :59,
        // written into MediaAspect.ATTR_TITLE at :305), and this heading shows the library's title.  So a viewer
        // standing on one song read the record's name, a number and two dashes where the song's name belongs.  Both
        // facts the heading now leaves out are said by the band under it, as "Track" and as "Album".
        if (!isSeason && IsMusic && !MediaCatalog.IsAlbum(AspectName))
          name = TitleViewModel.TrackNameOnly(name, details.Album, title.Title.Track);

        DetailsHeading = isSeason
            ? name
            : TitleViewModel.YearlessName(!string.IsNullOrWhiteSpace(details.SeriesName)
                ? details.SeriesName!.Trim()
                : name);
        // The reference's detail screens put the tagline under the title and above the facts - the sentence the
        // studio wrote, before the sentences about length and kind. A title without one simply shows no line.
        DetailsTagline = details.Tagline?.Trim() ?? "";
        DetailsLine = DescribeLine(details, title);
        // The episode's own summary first. When the importer numbered the episodes but never wrote one, the next
        // honest sentence is the series' own - the server lends it as SeriesOverview - and only where the series
        // has nothing either does the panel admit that nothing was written.
        //
        // A piece of music is never admitted to anything, because it has no story to be missing: the reference
        // wrote a story block into its VIDEO screens only (StoryPlot through the VideoAspectWrapper at
        // CurrentlyPlayingVideoDetails.inc:59, CurrentlyPlayingMovieDetails.inc:88,
        // CurrentlyPlayingSeriesDetails.inc:339), while its audio screen has no prose block at all
        // (CurrentlyPlayingAudioDetails.inc).  Where a record does carry prose - the album description a tag
        // reader wrote, which the server now lends - it stands; where it carries none the panel says nothing,
        // because empty means empty (the viewer's word on 2026-11-03).
        DetailsText = details.Description ?? details.SeriesOverview
            ?? (IsMusic ? "" : "Nothing was written about this title on this server.");
        // The episode's name walks out of the fact list and stands in bold over its own story - the story is
        // the sentence of THIS episode, and its title belongs over the sentence, not among the shelf numbers.
        DetailsEpisodeName = !isSeason && !string.IsNullOrWhiteSpace(details.EpisodeName)
            ? details.EpisodeName!.Trim()
            : "";
        DetailsPath = ShownPath(details.Path ?? title.Title.Path);
        DetailsRating = ToldRating(details);
        DetailsRatingWords = DetailsRating.HasValue
            ? "(" + DetailsRating.Value.ToString("0.0", CultureInfo.InvariantCulture) + ")"
            : "";
        FillDetailsStars(DetailsRating);
        FillDetailsFacts(details, title);
        // An audio file describes itself in its details answer, so this row is filled from what is already in
        // hand; a film's answer carries none of the six figures and the row stays away.
        DetailsAudio = MediaStreamLogos.Audio(details);
        ShowsAudioLine = DetailsAudio.ShowsAnything;
        FillDetailsCollection(details);
        FillDetailsProgress(title);
        DetailsVisible = true;
        await ReadDetailsArtworkAsync(title);
        await ReadDetailsClearArtAsync(title);
        await ReadDetailsDiscAsync(title);
        await ReadDetailsStreamsAsync(title);
        await ReadDetailsPersonsAsync(title);
        _ = ReadDetailsCollectionFilmsAsync(DetailsCollection);
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
      finally
      {
        // Ended, however it ended.  Only this read's own title is cleared, because a newer question about a
        // different title may already have taken the mark over while this one was in the air.
        if (_detailsReadUnderway == asked)
          _detailsReadUnderway = Guid.Empty;
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

    /// <summary>
    /// Which record a tune came from: what the reading of this title answered, and failing that the record the
    /// importer wrote INTO the tune's title.
    /// </summary>
    /// <remarks>
    /// The answer's own field is <c>AudioAspect.ATTR_ALBUM</c> and is preferred.  It is a young field, though, and a
    /// server that predates it answers nothing, while the same library has been writing the record into the title
    /// column all along ("<c>record: number - tune</c>", <c>TrackInfo.TRACK_FORMAT_STR</c> at TrackInfo.cs:59) - so
    /// reading it back off the spelled title costs nothing and invents nothing.  Only a title that carries the
    /// library's own number for this tune can be read that way, because the number is what makes the mark certain.
    /// </remarks>
    private static string AlbumOfTrack(MediaItemDetails details, TitleViewModel title)
    {
      if (!string.IsNullOrWhiteSpace(details.Album))
        return details.Album!;

      int? placed = title.Title.Track;
      return placed.HasValue
          ? RecordNamedIn(title.Name, placed.Value.ToString(CultureInfo.InvariantCulture))
          : "";
    }

    /// <summary>
    /// The record standing in front of <c>"<number> - "</c> in a tune's spelled title, or an empty word when the
    /// title is not spelled that way with this number.
    /// </summary>
    private static string RecordNamedIn(string name, string place)
    {
      if (string.IsNullOrWhiteSpace(name))
        return "";

      // The LAST mark of this shape, because a record whose own name carries a colon is spelled with that colon
      // first and the importer's mark after it.
      int cut = name.LastIndexOf(": " + place + " - ", StringComparison.Ordinal);
      return cut > 0 ? name.Substring(0, cut).Trim() : "";
    }

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
    /// them: the numbers that only this kind of title has (which season, which episode, which place on the
    /// record), then length, who the music is credited to and which record it came from, the year, the day it
    /// first ran, the age it was passed for, and the kinds of story it is told in.
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
        // A tune's number is its address inside the record, so it stands where an episode's address stands.  The
        // panel's answer has no such field - it answers how many tunes a RECORD holds, never which place one
        // tune has - but the row the viewer is standing on carries the library's own number, which the shelf read
        // off AudioAspect.ATTR_TRACK (MediaController:4765-4769) and which the row's own left column is drawn from.
        if (title.Title.Track.HasValue)
          AddFact("Track", title.Title.Track.Value.ToString(CultureInfo.InvariantCulture));
        // The episode's name is no fact of the shelf's - it stands in bold over the story, where the story's
        // title belongs (see DetailsEpisodeName); the band keeps only the numbers, which are the catalogue.
      }

      // An audio file's length is said in its own spelling. The worn skin wrote a track's duration as minutes and
      // seconds (<c>MediaInfoAudioDetails.inc:308-315</c>, <c>Duration</c> through the skin's duration converter as
      // <c>mm\:ss</c>), and it is right to: "3 min" is a song rounded away, while "03:26" is the length a viewer is
      // about to listen to. Only an audio file answers the seconds, so nothing else changes its wording here.
      // <para>
      // An album is the one audio title that is NOT spelled that way, and the viewer asked for the difference: a
      // record is not about to be listened to in the way a song is, and "105:45" is a stopwatch while "1 h 45 min"
      // is how anybody says how long an album takes.  The album's seconds are the sum of its tracks
      // (MediaController.FillTheTracksInsideItsAlbum), and the minutes below are cut off whole - 105 minutes and
      // 45 seconds are one hour and forty-five minutes, and a screen that said "1 h 46 min" would promise a minute
      // the record does not have.
      // </para>
      bool isAlbum = MediaCatalog.IsAlbum(AspectName);
      int? seconds = isAlbum ? null : details.DurationSeconds;
      if (seconds.HasValue && seconds.Value > 0)
      {
        AddFact("Length",
            (seconds.Value / 60).ToString("00", CultureInfo.InvariantCulture) + ":"
            + (seconds.Value % 60).ToString("00", CultureInfo.InvariantCulture));
      }
      else
      {
        int? minutes = (isAlbum && details.DurationSeconds > 0 ? details.DurationSeconds.Value / 60 : (int?) null)
            ?? details.RuntimeMinutes ?? title.Title.RuntimeMinutes;
        if (minutes.HasValue && minutes.Value > 0)
        {
          int hours = minutes.Value / 60;
          string length = hours > 0
              ? hours + " h " + (minutes.Value % 60) + " min"
              : minutes.Value + " min";
          AddFact(isSeason || IsMusic ? "Length" : "Runtime", length);

          // The row's own right column is told the same number, which is the only way an episode ever gets one.
          title.ReadLengthFromItsAnswer(minutes);
        }
      }

      // Who made the music and which record it came from, which this band left out until now.  A tune is asked one
      // thing and an album another, because that is how the library keeps them: the singer of THIS file and the
      // record it was pressed onto stand on the tune's own audio aspect (AudioAspect.ATTR_ARTISTS, ATTR_ALBUM),
      // while the name a RECORD is credited to stands on the album's own aspect (AudioAlbumAspect.ATTR_ARTISTS) and
      // nowhere on a tune - which is why a record is never asked for its "album artist" out of a tune's tag: that
      // would name a guest musician as the record's own (the server spells the same warning at
      // MediaController:1858-1866), and an album item carries no audio aspect to read it from at all.
      // <para>
      // One name per cell, not the list: both columns are collections in MP2, and every person the library credits
      // stands in the reading below, where artists and album artists have bands of their own.
      // </para>
      if (isAlbum)
      {
        // The shelf already read the record's credited name into the row the viewer is standing on
        // (MediaController:4755-4758); the panel's answer carries a tune's "album artist" tag, which is the right
        // answer for a tune and the wrong one for a record.
        AddFact("Album artist", string.IsNullOrWhiteSpace(title.Artist) ? details.AlbumArtist : title.Artist);
      }
      else
      {
        AddFact("Artist", string.IsNullOrWhiteSpace(details.Artist) ? title.Artist : details.Artist);
        AddFact("Album", AlbumOfTrack(details, title));
      }

      if (details.Year.HasValue)
        AddFact("Year", details.Year.Value.ToString(CultureInfo.InvariantCulture));
      if (details.FirstAired.HasValue && !isSeason)
        AddFact(IsMusic ? "Released" : "First aired",
            details.FirstAired.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
      // How old the viewer has to be, as a plain number. What the library keeps is a mark - "US_TV14" - and a
      // row printing that shows the viewer a code instead of the fourteen years the code stands for, so the
      // server reads the years off its own certificate table and sends them. A mark asking for nothing ("FSK 0",
      // "G") arrives as 0 and stands as 0, because it is a verdict like any other; only a title whose mark the
      // table does not know - or which carries none - leaves the row out, which is what the panel does with
      // everything it does not know. Gated by the skin's switch, which hung the certification logo on the
      // poster in the reference.
      if (AgeLogosShown && details.Age.HasValue)
        AddFact("Age", details.Age.Value.ToString(CultureInfo.InvariantCulture));
      if (details.Genres != null && details.Genres.Count > 0)
      {
        List<string> kinds = new List<string>();
        foreach (string genre in details.Genres)
          if (!string.IsNullOrWhiteSpace(genre))
            kinds.Add(genre.Trim());
        if (kinds.Count > 0)
          AddFact("Genre", string.Join(", ", kinds));
      }
      // What stands on the album, which is the two facts the reference wrote under its album name: how many tracks
      // and on how many discs (MediaItemListAudioAlbumDetails.inc:138-153, the "track.png" row reading
      // AudioAlbumAspect.NUMTRACKS, and MediaInfoAudio.xaml:139-148 writing "{DiscId} | {NumDiscs}" beside a
      // picture of a disc).  The count is the one the library has rather than the one the tag remembers
      // (MediaController.ItemResult.TrackCount), and a set of one disc says no "Discs" at all - a number that
      // chooses nothing between one and one is not an answer, it is the absence of a set.
      if (details.AlbumTrackCount.HasValue && details.AlbumTrackCount.Value > 0)
        AddFact("Tracks", details.AlbumTrackCount.Value.ToString(CultureInfo.InvariantCulture));
      if (details.DiscCount.HasValue && details.DiscCount.Value > 1)
        AddFact("Discs", details.DiscCount.Value.ToString(CultureInfo.InvariantCulture));
      if (!string.IsNullOrWhiteSpace(details.Collection))
        AddFact("Collection", details.Collection!.Trim());
      // How often a title was watched is deliberately NOT a fact of this panel: the viewer asked for it in the home
      // screens only, where it is what a favourites wall is standing for. Here it would be the one number on the
      // panel that says nothing about the title and everything about the viewer's history.

      // What stands in the facts band is the whole answer; the gradient over the row of covers repeats the few of
      // them it can wear, in the order the band already has them and in no order of its own.
      CoverFacts.Clear();
      foreach (MediaBrowserFact fact in DetailsFacts)
        if (CoverFactLabels.Contains(fact.Label))
          CoverFacts.Add(fact);
    }

    /// <summary>
    /// The collection the read film names, and the id the library holds that collection under.
    /// </summary>
    /// <remarks>
    /// The name comes straight off the film, and the id is the server's second lookup - see
    /// <c>MediaController.FillCollectionTitle</c> for why the library has to be asked by name rather than over the
    /// film's relationship.  An id that does not read as one is treated as no id at all: the card still stands
    /// with its name, which is the part the library said for certain.
    /// </remarks>
    private void FillDetailsCollection(MediaItemDetails details)
    {
      string named = string.IsNullOrWhiteSpace(details.Collection) ? "" : details.Collection!.Trim();

      // The row of films was read for THIS collection, so opening a second film of it - which is exactly what a
      // viewer pressing one of those posters does - leaves the row where it is.  Tearing it down and asking again
      // would answer the same question a second time, take the posters back off the wall while the answer was on
      // the wire, and drop the caret, whose card had just been destroyed under it.
      if (named == DetailsCollection && named.Length > 0 && DetailsCollectionFilms.Count > 0)
        return;

      DetailsCollectionFilms.Clear();
      _collectionFilmsFor = "";
      OnPropertyChanged(nameof(ShowsCollectionFilms));
      DetailsCollection = named;
    }

    /// <summary>One fact on the panel's list, which is only added when it has something to say.</summary>
    private void AddFact(string label, string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return;
      DetailsFacts.Add(new MediaBrowserFact(label, value.Trim()));
    }

    /// <summary>
    /// Puts the figure on the panel: how far this viewer got, which the cover's own foot-line measures out of a
    /// hundred, shown only between the ends - the home tiles' law, where a title untouched has nothing to measure
    /// and a finished one is watched, which the watched mark says (<c>HomeContentStyles.xaml:203-207</c>).
    /// </summary>
    /// <remarks>
    /// The figure is the row's own, not the panel answer's, and that is the server's doing rather than an
    /// oversight here: <c>MediaItemDetails</c> carries the count of how often a title was finished but never how
    /// far the last attempt got, while every row of the list does carry it (<c>ServerApiDto.cs:139-181</c>). The
    /// row is the title the viewer is standing on, so its number is the honest one. A row whose shelf was read
    /// from a server that names nobody answers <c>null</c> there, and the cover then wears no line - the
    /// difference between "nobody watched this" and "nobody asked who watched this".
    /// </remarks>
    private void FillDetailsProgress(TitleViewModel title)
    {
      int? percentage = title.Title.Percentage;
      DetailsProgress = percentage ?? 0;
      DetailsProgressShown = percentage.HasValue && percentage.Value > 0 && percentage.Value < 100;
    }

    /// <summary>
    /// The lettering a title was released with, asked of the server under its own kind of picture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A kind of picture of its own and not a second copy of the poster: <c>ClearArt</c> is the film's name drawn
    /// as a logo, which is why the reference asked for it by that name and hung it in the details head rather
    /// than over the poster (<c>MediaInfoMovieDetails.inc:355-362</c>, <c>MediaInfoSeriesDetails.inc:297</c>).
    /// </para>
    /// <para>
    /// Quiet when it fails, like the poster: most libraries hold no clear art for most titles, and a panel that
    /// complained about every one of them would teach the viewer nothing except that the library is ordinary.
    /// </para>
    /// <para>
    /// <b>An album is asked twice, because an album's lettering is filed under two names.</b> A media manager
    /// leaving art beside the files writes <c>clearart.png</c> and <c>clearlogo.png</c>, and this library's
    /// importers file them apart: the first under <c>ClearArt</c>, the second under <c>Logo</c>
    /// (<c>LocalFanartHelper.LOGO_FILENAMES</c>, which names both words, filed by
    /// <c>VideoFanArtHandler.FANART_FILENAMES</c> and the audio handlers).  Measured over the household's 43
    /// albums: three answer <c>ClearArt</c>, thirty-nine answer <c>Logo</c>.  Asking only the reference's name
    /// would therefore leave thirty-six of thirty-nine albums standing without the lettering they have, which is
    /// why the album asks its second question when its first is answered with nothing.  A film asks once and keeps
    /// its lettering in the head's row, where the reference hung it.
    /// </para>
    /// </remarks>
    private async Task ReadDetailsClearArtAsync(TitleViewModel title)
    {
      DetailsClearArt = null;
      DetailsAlbumLettering = null;

      // The names in the order they are believed, which for a film is one name and for an album two.
      string[] kinds = IsMusic
          ? new[] { "ClearArt", "Logo" }
          : new[] { "ClearArt" };

      foreach (string kind in kinds)
      {
        Artwork? artwork = PanelArtworkFor(kind);
        if (artwork == null)
          return;

        byte[]? picture;
        try
        {
          picture = await artwork.LoadAsync(title.Title.Id);
        }
        catch (ServerApiException)
        {
          // See above: a library without this picture is a library, not a failure - and an album without this
          // kind of picture still has its other question to ask.
          continue;
        }

        if (picture == null)
          continue;

        if (!ArtworkConverter.Instance.Remembers(picture, ArtworkConverter.POSTER_PARAMETER))
        {
          //  The lettering is drawn a hundred and forty points high and never wider than two hundred and forty,
          //  so it is read at the small width: read at the wall width it stood on about five times the pixels it
          //  can ever paint, out of the one budget the shelf's covers are short of.
          byte[] toRead = picture;
          await Task.Run(() =>
              ArtworkConverter.Instance.Warm(toRead, ArtworkConverter.POSTER_PARAMETER));
        }

        // The last question asked is the one that may write, exactly as with the poster: a viewer walking two
        // albums in quick succession must not end up with the first album's lettering over the second's story.
        if (_detailsFor != title.Title.Id)
          return;

        if (IsMusic)
          DetailsAlbumLettering = picture;
        else
          DetailsClearArt = picture;
        return;
      }
    }

    /// <summary>
    /// The disc the album was pressed on, asked of the same picture question the cover and the writing are asked
    /// of, and laid behind the cover when the library has one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reference laid this picture in the cover's own box and let it look out past the cover's edge: four
    /// hundred and twenty lines of disc behind four hundred and forty of cover, pushed a hundred and ninety of its
    /// own four hundred and twenty beyond the cover it stood under
    /// (<c>MediaItemListAudioAlbumDetails.inc:41-44</c>, the cover at <c>:52</c>, the box of four hundred and
    /// fifty six at <c>:33</c>) - forty five percent of the disc showing.  This panel lays it out of the cover's
    /// side instead and shows forty percent of it, which is what the viewer asked for on 2026-11-03; the size,
    /// the order of the two pictures and the fact that the disc is never drawn without a cover over it are the
    /// reference's.
    /// </para>
    /// <para>
    /// Only a record asks the question.  A film's poster never had a disc behind it, and asking a film's id for
    /// one costs a round trip to answer nothing.  Measured over the household's library on 2026-11-03, one request
    /// per album: nine of forty three albums answer <c>DiscArt</c> and thirty four answer with a refusal, which is
    /// the answer every library gives for every picture it does not hold - and the panel then draws no disc,
    /// leaves the cover standing alone and tells the story in its own column beside it, exactly as it did before.
    /// </para>
    /// </remarks>
    private async Task ReadDetailsDiscAsync(TitleViewModel title)
    {
      DetailsAlbumDisc = null;

      if (!IsMusic)
        return;

      Artwork? artwork = PanelArtworkFor("DiscArt");
      if (artwork == null)
        return;

      byte[]? picture;
      try
      {
        picture = await artwork.LoadAsync(title.Title.Id);
      }
      catch (ServerApiException)
      {
        // See above: a library that keeps no picture of the disc is an ordinary library, and the cover stands
        // alone in a panel that never mentions the question.
        return;
      }

      if (picture == null)
        return;

      if (!ArtworkConverter.Instance.Remembers(picture, null))
      {
        byte[] toRead = picture;
        await Task.Run(() => ArtworkConverter.Instance.Warm(toRead, null));
      }

      // The last title asked about is the one that may write, exactly as with the cover and the writing: a viewer
      // walking two albums in quick succession must not end up with the first album's disc under the second's
      // cover.
      if (_detailsFor != title.Title.Id)
        return;

      DetailsAlbumDisc = picture;
    }

    /// <summary>
    /// The tracks the file carries, asked of the server and laid into the panel's three lists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Asked separately from the details themselves, and after them, for the same reason the pictures are: the
    /// story is what a viewer came for and arrives in one answer, while the tracks are a second question - and a
    /// title whose importer never looked inside its file answers it with three empty lists, which is the ordinary
    /// state of a library and not something to complain about on screen. The band is therefore stood up empty and
    /// filled when the answer comes, and it costs its lines only once it has something in it.
    /// </para>
    /// <para>
    /// The figures are the library's, written by the importer when the file was first read; nothing here is
    /// measured from the file now, and the screen says nothing the importer did not write.
    /// </para>
    /// </remarks>
    private async Task ReadDetailsStreamsAsync(TitleViewModel title)
    {
      DetailsAudioTracks.Clear();
      DetailsSubtitleTracks.Clear();
      DetailsPicture = PictureLine.Empty;
      ShowsPictureLine = false;
      ShowsAudioTracks = false;
      ShowsSubtitleTracks = false;
      ShowsFileSubtitles = false;
      FirstAudioChannelsToken = "";
      FirstAudioChannelsWord = "";

      if (_client == null)
      {
        FirstAudioChannelsToken = "";
        FirstAudioChannelsWord = "";
        return;
      }

      // A season the library only counts is read through its series and a number, and a series is a shelf of
      // seasons: neither pair has a title behind it that could carry a file, so its OWN tracks are not asked for.
      // What IS asked - on the viewer's word of 2026-11-03, "the plaques are missing on series" - is the file
      // answer of the series' FIRST episode, which is the answer to "what does this look like on the disk": the
      // packing, the size, the high dynamic range, the sound and whether text rides along.  The tracks lists stay
      // the title's own, and therefore empty; a series has no single file whose tracks could be named.
      bool standsOnAShelf = title.AskSeason.HasValue
          || MediaCatalog.IsSeries(ShelfAspectOf(title))
          || MediaCatalog.IsSeason(ShelfAspectOf(title));

      Guid asked = title.Title.Id;
      try
      {
        Guid? fileTitle = standsOnAShelf
            ? await ReadFirstEpisodeOfShelfAsync(title, asked)
            : title.AskId;
        if (fileTitle == null || _detailsFor != asked)
          return;

        MediaStreamsAnswer streams = await _client.GetMediaStreamsAsync(fileTitle.Value);
        if (_detailsFor != asked)
          return;

        DetailsPicture = MediaStreamLogos.Picture(
            streams.VideoStreams.Count > 0 ? streams.VideoStreams[0] : null);
        AudioStreamInfo? firstAudio = streams.AudioStreams.Count > 0 ? streams.AudioStreams[0] : null;

        if (!standsOnAShelf)
        {
          foreach (AudioStreamInfo audio in streams.AudioStreams)
          {
            AudioTrackCard? card = MediaStreamWords.AudioCard(audio);
            if (card != null)
              DetailsAudioTracks.Add(card);
          }

          foreach (SubtitleStreamInfo subtitle in streams.SubtitleStreams)
          {
            SubtitleTrackCard? card = MediaStreamWords.SubtitleCard(subtitle);
            if (card != null)
              DetailsSubtitleTracks.Add(card);
          }
        }

        ShowsPictureLine = DetailsPicture.ShowsAnything;
        ShowsAudioTracks = DetailsAudioTracks.Count > 0;
        ShowsSubtitleTracks = DetailsSubtitleTracks.Count > 0;
        // The plaque asks the file, not the list: a shelf's list is empty by nature, its file answer is episode one's.
        ShowsFileSubtitles = standsOnAShelf
            ? streams.SubtitleStreams.Count > 0
            : ShowsSubtitleTracks;
        AudioTrackCard? first = firstAudio == null ? null : MediaStreamWords.AudioCard(firstAudio);
        FirstAudioChannelsToken = first?.ChannelsToken ?? "";
        FirstAudioChannelsWord = first?.Channels ?? "";
      }
      catch (ServerApiException)
      {
        // An older server, or a title the library dropped between the two questions. Either way the panel keeps
        // the story it already shows; the tracks band simply does not appear.
      }
      catch (Exception ex)
      {
        Status = "Reading the tracks failed: " + ex.Message;
      }
    }

    /// <summary>
    /// The two parts the reading shows people for, in the server's words.
    /// </summary>
    /// <remarks>
    /// The reference's information screen showed exactly these two (<c>MediaInfoVideo.xaml:384-457</c>). The music
    /// screen shows three more - artist, album artist, composer (<c>MediaInfoAudio.xaml:204-296</c>) - which reach
    /// this front end when the audio reading is rebuilt, and the server already answers them.
    /// </remarks>
    private static readonly string[] PEOPLE_ASKED = { "actor", "director" };

    /// <summary>
    /// The parts a music screen asks its people in, which are the three the reference's audio screen named
    /// (<c>MediaInfoAudio.xaml:204-296</c>: Artists, Album Artists, Composers).  The server has answered these
    /// roles since the people endpoint was built (<c>MediaController.cs</c>, the roles table's
    /// <c>artist</c>/<c>albumartist</c>/<c>composer</c>); what was missing was a screen asking them.
    /// </summary>
    private static readonly string[] MUSIC_PEOPLE_ASKED = { "artist", "albumartist", "composer" };

    /// <summary>
    /// The people the title names, asked of the server and laid into the two bands of the reading.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why a second question.</b> A title's people are not written on the title; they stand in relationships of
    /// their own, which is how the reference read them too - its two lists were <c>RelationshipQuery</c> bindings
    /// over the title's id (<c>MediaInfoVideo.xaml:395-398</c> and <c>:432-435</c>). Asking the details endpoint for
    /// them as well would have made the server walk relationships for every title that only wants a story, so the
    /// reading asks once more, here, where the answer is looked at.
    /// </para>
    /// <para>
    /// <b>Names first, faces after.</b> A band of names is the answer; a portrait is a picture that may or may not
    /// be in the library and arrives per person. The reference let each card fetch its own and drew the skin's
    /// standing figure wherever the library had none
    /// (<c>themes/default/styles/CurrentlyPlayingStyles.xaml:216-222</c>), so that is what happens here as well -
    /// the names are on the screen before a single face has been asked for.
    /// </para>
    /// </remarks>
    /// <summary>
    /// The aspect of the shelf this row was listed in, which is what tells a season from an episode and a shelf
    /// from a thing to watch: whoever reads a list says it on the row (<see cref="TitleViewModel.ReadThrough"/>),
    /// and for a row of this screen's own shelf the screen's aspect is the same answer.
    /// </summary>
    private string ShelfAspectOf(TitleViewModel title)
    {
      return title.ReadThrough ?? AspectName;
    }

    /// <summary>
    /// The first episode inside the shelf the viewer stands on, which is what a shelf's file plaques answer from.
    /// </summary>
    /// <param name="title">The series or season the viewer stands on.</param>
    /// <param name="asked">The title the running details read is for; a late answer about another one is dropped.</param>
    /// <remarks>
    /// <para>
    /// The shelf is asked for the episodes filed inside it - <c>inside=series</c> for a series, and the same
    /// question with the season number added for a season, which is how the screen reads a season's own shelf
    /// too (a season row carries its series as parent and its number, <see cref="TitleViewModel.AskId"/> and
    /// <see cref="TitleViewModel.AskSeason"/>).  A season is <b>not</b> asked as a parent: the seasons of this
    /// library are counted rather than held, and such an id answers the episode question with 404 - which is why
    /// the walk over the season title was tried once, on the live server, and then dropped.
    /// </para>
    /// <para>
    /// Which of the answered episodes is <b>first</b> is settled here rather than by the server: <c>sort=episode</c>
    /// orders by the two numbers the episode aspect carries (MediaController.cs:4131-4134), but the running server
    /// was asked over every series of this library and answered Shōgun with S01E04, Raised by Wolves with S02E4 and
    /// Prison Break with a row carrying no number at all, because its import left those two columns empty for
    /// whole runs of episodes.  So the shelf's episodes are read once, remembered, and the lowest pair of numbers
    /// wins - the episode a viewer reaching for a series would play first, which is the file the plaques answer
    /// from.  A title with no numbers written on it loses to one that has them: no number is not a lower number.
    /// </para>
    /// <para>
    /// A shelf without a single episode answers nothing, and a server too old for the question says so: both leave
    /// the plaques away rather than guessing at a file.
    /// </para>
    /// </remarks>
    private async Task<Guid?> ReadFirstEpisodeOfShelfAsync(TitleViewModel title, Guid asked)
    {
      string key = title.AskId.ToString("D") + "/"
          + (title.AskSeason.HasValue
              ? title.AskSeason.Value.ToString(CultureInfo.InvariantCulture)
              : "-");
      if (_firstEpisodeOfShelf.TryGetValue(key, out Guid? known))
        return known;

      ServerApiClient? client = _client;
      if (client == null)
        return null;

      try
      {
        // Five hundred rather than one: the answer is picked from the whole shelf, and a shelf deeper than this
        // is deep enough that its first two hundred episodes are inside the page anyway.
        MediaTitles episodes = await client.GetTitlesAsync(
            "EpisodeItem", null, false, 500, 0, null, null, title.AskId, null, null, title.AskSeason);
        if (_detailsFor != asked)
          return null;

        IReadOnlyList<MediaTitle>? inside = episodes.Titles;
        if (inside == null || inside.Count == 0)
        {
          _firstEpisodeOfShelf[key] = null;
          return null;
        }

        MediaTitle first = inside[0];
        foreach (MediaTitle candidate in inside)
        {
          if (CompareEpisodeNumbers(candidate, first) < 0)
            first = candidate;
        }

        _firstEpisodeOfShelf[key] = first.Id;
        return first.Id;
      }
      catch (ServerApiException)
      {
        // A server too old for the question, or a shelf the library dropped between the questions.  Nothing is
        // guessed at: a shelf whose episode question went wrong wears no plaques.  Nothing is remembered either,
        // so a shelf that was merely asked at a bad moment is asked again the next time the viewer stands on it.
      }
      catch (Exception)
      {
        // The same answer for anything else that goes wrong over one shelf.
      }
      return null;
    }

    /// <summary>
    /// Whether one episode comes before another in the order a viewer watches a series in: season number first,
    /// episode number second.
    /// </summary>
    /// <remarks>
    /// A title whose numbers never arrived stands last rather than first: an empty column is not the smallest
    /// number, and a shelf that sorts nothing would otherwise hand out its most nameless row as the series' first
    /// episode.
    /// </remarks>
    private static int CompareEpisodeNumbers(MediaTitle a, MediaTitle b)
    {
      int bySeason = (a.Season ?? int.MaxValue).CompareTo(b.Season ?? int.MaxValue);
      return bySeason != 0
          ? bySeason
          : (a.Episode ?? int.MaxValue).CompareTo(b.Episode ?? int.MaxValue);
    }

    private async Task ReadDetailsPersonsAsync(TitleViewModel title)
    {
      DetailsActors.Clear();
      DetailsDirectors.Clear();
      DetailsArtists.Clear();
      DetailsAlbumArtists.Clear();
      DetailsComposers.Clear();
      ShowsActors = false;
      ShowsDirectors = false;
      ShowsArtists = false;
      ShowsAlbumArtists = false;
      ShowsComposers = false;

      if (_client == null)
        return;

      // A season the library counts is a shelf of episodes and carries no people of its own; the names stand on
      // the episodes, so asking about the shelf would ask about a different title than the one the viewer stands on.
      if (title.AskSeason.HasValue)
        return;

      // A song is not credited to an actor and a film is not credited to a composer, so the question itself turns on
      // what kind of wall the viewer is standing in - one question either way, never six names nobody asked for.
      bool music = IsMusic;
      Guid asked = title.Title.Id;
      try
      {
        MediaPersonsAnswer answer = await _client.GetMediaPersonsAsync(title.AskId,
            music ? MUSIC_PEOPLE_ASKED : PEOPLE_ASKED);
        if (_detailsFor != asked)
          return;

        List<PersonCard> faces = new List<PersonCard>();
        if (music)
        {
          foreach (PersonCard card in MediaPersons.CardsFor(answer.PeopleOf("artist")))
            DetailsArtists.Add(card);
          foreach (PersonCard card in MediaPersons.CardsFor(answer.PeopleOf("albumartist")))
            DetailsAlbumArtists.Add(card);
          foreach (PersonCard card in MediaPersons.CardsFor(answer.PeopleOf("composer")))
            DetailsComposers.Add(card);

          ShowsArtists = DetailsArtists.Count > 0;
          ShowsAlbumArtists = DetailsAlbumArtists.Count > 0;
          ShowsComposers = DetailsComposers.Count > 0;

          faces.AddRange(DetailsArtists);
          faces.AddRange(DetailsAlbumArtists);
          faces.AddRange(DetailsComposers);
        }
        else
        {
          foreach (PersonCard card in MediaPersons.CardsFor(answer.PeopleOf("actor")))
            DetailsActors.Add(card);
          foreach (PersonCard card in MediaPersons.CardsFor(answer.PeopleOf("director")))
            DetailsDirectors.Add(card);

          ShowsActors = DetailsActors.Count > 0;
          ShowsDirectors = DetailsDirectors.Count > 0;

          faces.AddRange(DetailsActors);
          faces.AddRange(DetailsDirectors);
        }

        _ = PaintPersonPortraitsAsync(asked, faces);
      }
      catch (ServerApiException)
      {
        // A server too old to answer this question, or a title the library dropped between the two questions. The
        // reading keeps the story and the tracks it already showed; the two bands simply do not appear.
      }
      catch (Exception ex)
      {
        Status = "Reading the people failed: " + ex.Message;
      }
    }

    /// <summary>
    /// The portraits of the people the bands now name, one errand per person, put behind the bands instead of in
    /// front of them.
    /// </summary>
    /// <param name="askedFor">
    /// The title the bands were filled for. A face that turns up after the viewer moved on to another title is a
    /// face nobody is looking at, and it is thrown away rather than written onto a card that is gone.
    /// </param>
    /// <param name="cards">The cards of both bands, in the order they are walked.</param>
    /// <remarks>
    /// Asked one after the other rather than all at once: a film's credit list is twenty people, and twenty
    /// pictures fetched together push the posters of the wall behind them off the wire. The holder of these
    /// pictures limits what is on the way at a moment anyway (<see cref="Artwork.MAX_AT_ONCE"/>).
    /// </remarks>
    private async Task PaintPersonPortraitsAsync(Guid askedFor, IList<PersonCard> cards)
    {
      Artwork? portraits = PersonPortraits();
      if (portraits == null)
        return;

      foreach (PersonCard card in cards)
      {
        try
        {
          byte[]? picture = await portraits.LoadAsync(card.Id);
          if (picture == null || _detailsFor != askedFor)
            continue;

          // Decoded away from the thread that draws, the way every other picture of this panel is: a name the
          // binding has to decode first is a card the viewer sees arrive.  At the SMALL width, which is what the
          // card paints: eleven faces at the tile width filled more of the shared picture budget than all the
          // covers of the shelf standing next to them, and the covers pushed out are the covers that arrive late.
          byte[] toRead = picture;
          if (!ArtworkConverter.Instance.Remembers(toRead, ArtworkConverter.POSTER_PARAMETER))
            await Task.Run(() => ArtworkConverter.Instance.Warm(toRead, ArtworkConverter.POSTER_PARAMETER));

          if (_detailsFor == askedFor)
            card.Portrait = picture;
        }
        catch (ServerApiException)
        {
          // A library without this person's face is an ordinary library; the card draws the standing figure.
        }
        catch (Exception)
        {
          // The same answer for anything else that goes wrong over one picture: the card stays as it stood.
        }
      }
    }

    private Artwork? _personPortraits;
    private bool _personPortraitsAsked;

    /// <summary>
    /// The holder of the people's portraits, made once. Kept apart from the panel's own pictures on purpose:
    /// these are asked for under a PERSON's id, and mixing them into a cache keyed by title ids would hand one
    /// person's face to a title with the same number.
    /// </summary>
    private Artwork? PersonPortraits()
    {
      if (_personPortraitsAsked)
        return _personPortraits;

      _personPortraitsAsked = true;
      ServerApiClient? client = _client;
      if (client == null)
        return null;

      // The kind is the one the reference asked a person's portrait under, by the person's own id
      // (themes/default/styles/CurrentlyPlayingStyles.xaml:220-222).
      return _personPortraits = PicturesOf(client, PictureWidths.Shelf("PersonThumbnail", PictureWidths.SMALL),
          id => client.GetFanArtAsync(id, "Thumbnail", null, PictureWidths.SMALL));
    }

    /// <summary>
    /// The films the read film's collection holds, asked by the collection's name and laid into the band's row
    /// before a single poster of theirs has been asked for.
    /// </summary>
    /// <param name="askedFor">
    /// The collection this row is read for.  An answer that arrives after the viewer moved to a film of
    /// <em>another</em> collection is an answer about a row nobody is looking at and is thrown away.
    /// </param>
    /// <remarks>
    /// Asked once per collection and not once per film: a row already standing - or already on the wire - for this
    /// collection is left alone, because the viewer opening the next film of the collection it belongs to is
    /// exactly the case <see cref="FillDetailsCollection"/> keeps the row standing for.  Asking a second time laid
    /// every film of the collection into the row AGAIN, which is how a collection of two films came to show four
    /// posters (seen 2026-11-03: Ghostbusters Collection, "Frozen Empire" and "Afterlife" standing twice over").
    /// A row that was asked and answered with nothing - an old server, a library that cannot answer - is given its
    /// asking back, so the next film of that collection asks once more rather than standing empty for good.
    /// </remarks>
    /// <remarks>
    /// <para>
    /// The question is the shelf's own question with one category more (<c>GET api/media/titles?by=collection</c>,
    /// which the server matches against the collection name written on every film), so the row is the same answer a
    /// shelf of that collection would show - same films, same order - and not a second list with its own rules.
    /// </para>
    /// <para>
    /// Asked by name and not by the collection's id because that is how a film is linked to its collection in a
    /// library whose films were imported with the name written on them and never joined by a relationship row:
    /// counted over a library of 1136 films, all 228 films naming a collection name one the library also holds as a
    /// title, while the relationship rows between the two number zero.
    /// </para>
    /// </remarks>
    private async Task ReadDetailsCollectionFilmsAsync(string askedFor)
    {
      string collection = DetailsCollection;
      ServerApiClient? client = _client;
      if (client == null || collection.Length == 0 || collection != askedFor)
        return;

      // The row is asked once for a collection.  A row of this collection standing OR on the wire means the answer
      // is already there or already coming, and a second laying of the same answer is what duplicated the row.
      if (_collectionFilmsFor == collection)
        return;

      // Asked before the answer is on the wire, so that the posters of the row started behind it have a row to
      // arrive in: their errand stops the moment the collection it was asked for is no longer the one on screen.
      _collectionFilmsFor = collection;

      try
      {
        // No limit: the row is the collection's films, and its posters are places the viewer walks and presses, so
        // a film left out of the row would be a film of the collection nobody can reach from the reading.  The band
        // slides to hold them rather than growing (see the band's scroller), and the server's own ceiling of five
        // hundred rows bounds the question.
        MediaTitles members = await client.GetTitlesAsync(AspectName, "title", false, by: "collection",
            value: collection);
        if (_collectionFilmsFor != askedFor)
          return;
        if (members.Error != null)
        {
          // Nothing was answered, so nothing stands in the row's name either: the asking goes back, and the next
          // film of this collection asks the question again instead of living with an empty band forever.
          _collectionFilmsFor = "";
          return;
        }

        // The whole answer, laid into an empty row: the row IS this collection's films, so what arrives replaces
        // whatever was in it rather than adding to it.
        DetailsCollectionFilms.Clear();
        foreach (CollectionFilmCard card in MediaCollectionFilms.CardsFor(
            members.Titles ?? Array.Empty<MediaTitle>()))
          DetailsCollectionFilms.Add(card);

        OnPropertyChanged(nameof(ShowsCollectionFilms));
        _ = PaintCollectionFilmPostersAsync(askedFor, new List<CollectionFilmCard>(DetailsCollectionFilms));
      }
      catch (ServerApiException)
      {
        // A server that does not know the category yet refuses the question: the band keeps its head and stands
        // without its row, which is exactly what it did before the question was ever thought of.
        if (_collectionFilmsFor == askedFor)
          _collectionFilmsFor = "";
      }
      catch (Exception)
      {
        // The same answer for anything else that goes wrong over one shelf query.  The reading's own story,
        // tracks and people were answered before this was asked, and a band that cannot say which films belong to
        // its collection says its name alone.
        if (_collectionFilmsFor == askedFor)
          _collectionFilmsFor = "";
      }
    }

    /// <summary>
    /// The posters of the films the collection row now names, one errand per film.
    /// </summary>
    /// <param name="askedFor">The collection the row was read for, as by <see cref="ReadDetailsCollectionFilmsAsync"/>.</param>
    /// <param name="cards">The cards of the row, in the order the server answered the films.</param>
    /// <remarks>
    /// Asked one after the other rather than all at once, and out of the shelf's own holder of posters
    /// (<see cref="TileArtworkFor"/>) rather than a holder of its own: these are ordinary films of this shelf filed
    /// under their own ids, so a poster the wall already hung is not fetched a second time for being small here.
    /// A film whose poster the library does not keep leaves its plate empty, which is what a plate behind a shelf
    /// tile with no poster does.
    /// </remarks>
    private async Task PaintCollectionFilmPostersAsync(string askedFor, IList<CollectionFilmCard> cards)
    {
      Artwork? posters = TileArtworkFor(GridViewType.Poster);
      if (posters == null)
        return;

      foreach (CollectionFilmCard card in cards)
      {
        try
        {
          byte[]? picture = await posters.LoadAsync(card.Id);
          if (picture == null || _collectionFilmsFor != askedFor)
            continue;

          //  The card is a hundred and three points across, so its poster is asked for at the small width too -
          //  see the person cards above for the arithmetic this saves the covers of the shelf.
          byte[] toRead = picture;
          if (!ArtworkConverter.Instance.Remembers(toRead, ArtworkConverter.POSTER_PARAMETER))
            await Task.Run(() => ArtworkConverter.Instance.Warm(toRead, ArtworkConverter.POSTER_PARAMETER));

          if (_collectionFilmsFor == askedFor)
            card.Poster = picture;
        }
        catch (ServerApiException)
        {
          // A library without this film's poster is an ordinary library; the tile stands on its dark plate.
        }
        catch (Exception)
        {
          // The same answer for anything else that goes wrong over one picture.
        }
      }
    }

    private readonly Dictionary<string, Artwork?> _panelArtworks = new Dictionary<string, Artwork?>();

    /// <summary>
    /// The panel's own picture of one kind, made once per kind and kept. Separate from the tiles' pictures
    /// (<see cref="TileArtworkFor"/>) because a panel asks for kinds no tile ever wears, and sharing one cache
    /// between them would put a title's logo in a tile's bytes and a tile's banner in the logo's.
    /// </summary>
    private Artwork? PanelArtworkFor(string kind)
    {
      if (_panelArtworks.TryGetValue(kind, out Artwork? known))
        return known;

      Artwork? created = _client == null
          ? null
          : PicturesOf(_client, PictureWidths.Shelf(kind, PictureWidths.SMALL), id =>
              {
                (Guid asked, int? season) = AskOf(id);
                return _client.GetFanArtAsync(asked, kind, season, PictureWidths.SMALL);
              });
      _panelArtworks.Add(kind, created);
      return created;
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
      //
      // Two titles are exceptions, and the library itself made both of them.
      //
      // A record calls its front picture a cover and not a poster, which is the one name the import files an
      // album's art under, so the panel asks a record's cover - the picture its tiles already wear, read from the
      // same bytes a moment before.  Asking a poster of an album goes unanswered: the server lends a track the
      // album's picture when a track asks for a poster, but an album has no album behind it to be lent from.  The
      // missing picture is not only a missing picture, because the listener's line is drawn at the foot of that
      // very frame - a record whose panel stands without a cover stands without its line too.
      //
      // An episode wears its own thumbnail, asked under that kind (MediaInfoSeriesDetails.inc:23-42,
      // FanArtType="Thumbnail" for the Episode media type), because nobody ever filed a poster for one episode of
      // a series. An episode shelf already reads that kind for its tiles, so the panel's picture is usually the
      // bytes the wall read a moment before.
      Artwork? artwork = IsMusic
          ? CoverArtwork ?? _artwork
          : MediaCatalog.IsEpisode(title.ReadThrough)
              ? TileArtworkFor(GridViewType.Thumbnail) ?? _artwork
              : TileArtworkFor(GridViewType.Poster) ?? _artwork;
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
          // The picture is readied at the size the tiles will paint it - except in the shape whose row is painted
          // by our own hand.  That row takes the bytes and decodes them to the cover it is about to draw, so a
          // tile picture readied for it is a JPEG decoded for a tile that does not exist: measured at fourteen
          // decodes a step of the caret, none of them on the screen, on the same threads the row's own covers
          // were waiting for.
          if (picture != null && picture.Length > 0 && Layout != BrowserLayout.Carousel)
            await Task.Run(delegate { ArtworkConverter.Instance.Warm(picture, ArtworkConverter.TILE_PARAMETER); });

          row.ShowArtwork(picture);
        }
        else
        {
          // The wall this errand was sent for is not the wall the room wears now, so nothing is shown.  What MUST not
          // happen is leaving the row stamped as though it had been painted: the stamp is what keeps a row from being
          // asked a second time, and a shape the viewer settled on afterwards would then keep wearing its backup
          // plate for the rest of the evening - which is what "from the sixtieth cover on there are only plates" is
          // made of, since a walk fires a hundred of these errands and only the last generation is kept.
          row.PaintedFrom = null;
        }
      }
      catch (ServerApiException)
      {
        // A shelf whose server went quiet mid-picture keeps the rows it has. The list itself was read
        // successfully - that is what the status line is already about - and a row without a poster is a row a
        // viewer can still read, select and play.  Left unmarked on purpose: one errand that came back empty is no
        // reason to go without a poster for the rest of the evening when the server was only having a bad minute.
        row.PaintedFrom = null;
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
      if (AgeLogosShown && details.Age.HasValue)
        parts.Add("age " + details.Age.Value.ToString(CultureInfo.InvariantCulture));

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

    /// <summary>
    /// The row a new shelf opens on: the one behind the last title the viewer finished.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Someone who has worked through part of a season or an album does not read it from its head, but the caret
    /// has always been put at the head.  The row that matters is the one standing behind the last title the
    /// library says was finished: a season of ten whose second and third episodes are watched opens on the
    /// fourth - not on the first, which was watched - and ten tracks with the first two played open on the
    /// third.  A partly watched title counts as not finished, which is what its own row already says with its
    /// resume figure (<see cref="TitleViewModel.IsPartlyWatched"/>), and whether a row was finished is the row's
    /// single answer (<see cref="TitleViewModel.Seen"/>: the asking person's percentage at a hundred, or - where
    /// the server was never told who was asking - a play count above nought).
    /// </para>
    /// <para>
    /// Two answers remain when nothing stands behind the last finished row, because a season watched to its last
    /// episode has no "next" to stand on: the first row that was never finished, so a hole left earlier in the
    /// season is found at once; and failing that - everything watched, or nothing watched at all - the first row,
    /// which is where a wall has always begun.
    /// </para>
    /// <para>
    /// Asked only while nothing is selected, so a shelf that comes in pages (the opening hundred and twenty rows
    /// first, <see cref="OPENING_PAGE"/>) decides from the rows it had when it opened and the rows landing later
    /// never pull a caret the viewer may already be walking.  For the two lists this was asked for - an album's
    /// tracks and a season's episodes - the opening page already holds the whole list.
    /// </para>
    /// </remarks>
    private TitleViewModel? RowTheShelfOpensOn()
    {
      int last_finished = -1;
      for (int i = 0; i < Titles.Count; i++)
      {
        if (Titles[i].Seen)
          last_finished = i;
      }

      if (last_finished >= 0 && last_finished + 1 < Titles.Count)
        return Titles[last_finished + 1];

      foreach (TitleViewModel row in Titles)
      {
        if (!row.Seen)
          return row;
      }

      return Titles.Count > 0 ? Titles[0] : null;
    }

    private async Task ReadAsync(int offset)
    {
      if (_client == null)
      {
        Status = "No server configured";
        return;
      }

      // A grouped shelf is a different reading rather than a different answer: it asks one question per group and
      // lays each group's titles under its own plate, which the one-question flat reading below has no part of a
      // mind to do. Everything the flat reading promises - one read at a time, generations, the finishing run,
      // the foot's numbers - the grouped reading promises too, out of its own methods.
      if (IsGrouped)
      {
        await ReadGroupedAsync(offset);
        return;
      }

      // One read at a time. A viewer holding down the arrow key on the "more" row can queue a dozen requests;
      // the older ones are cancelled rather than allowed to append pages out of order.
      CancellationTokenSource? previous = _running;
      CancellationTokenSource cancellation = new CancellationTokenSource();
      _running = cancellation;
      previous?.Cancel();

      Services.HomeTrace.Log("read.begin", "offset=" + offset + " titles=" + Titles.Count);

      // A read from the top is a new shelf, whatever made it happen - the tile that was pressed, a word typed into
      // the find box, the watched key. Every run that was finishing an older shelf notices at its next pause.
      if (offset == 0)
        Interlocked.Increment(ref _generation);

      IsBusy = true;
      try
      {
        // Nothing asks for newest-first here any more: that order came with the "Latest media" tile alone. The
        // opening question is a screenful and every question after it is as much as the server will hand over, so
        // that a shelf of eleven hundred films costs two questions rather than twenty.  Which END of the order the
        // shelf starts at is the viewer's answer, changed by picking the standing order again (PickSortAsync).
        MediaTitles answer = await _client.GetTitlesAsync(AspectName, SortOrder, SortDescending,
            offset == 0 ? OPENING_PAGE : REST_IN_ONE_ASK, offset,
            string.IsNullOrEmpty(ActiveSearch) ? null : ActiveSearch,
            string.IsNullOrEmpty(ActiveSeen) ? null : ActiveSeen, Inside, GroupBy, GroupValue, SeasonFilter,
            cancellation.Token);

        if (offset == 0)
        {
          Titles.Clear();
          Rows.Clear();
          _shelfGroups.Clear();
        }

        foreach (MediaTitle title in answer.Titles ?? Array.Empty<MediaTitle>())
        {
          TitleViewModel row = new TitleViewModel(title, DrillsIn, _client, AspectName)
          {
            WatchFlagsShown = WatchFlagsShown,
            InsideName = Parent?.Title.Title
          };

          // A season the library only counts has its answers somewhere else - the pictures, the details and the
          // shelf inside it all belong to the series it was counted from, under its number. The rows are painted and
          // asked about by their own id, so that is where the detour has to be remembered.
          if (title.ParentId.HasValue)
            _asks[title.Id] = (title.ParentId.Value, title.Season);

          Titles.Add(row);
          Rows.Add(row);
        }

        // Whether the shelf has anything left, settled before the selection is placed: a walk landing on the
        // first of these rows is the viewer saying they are near the end, and the answer to "is there more" has
        // to be the fresh one by then. The ceiling is applied here rather than in the reading itself so that the
        // foot of the shelf, the status line and the walk all agree with one another - and so that a shelf which
        // stopped early can say so below instead of scrolling quietly forever.
        HasMore = answer.Remaining > 0 && Titles.Count < MAX_ROWS;

        Services.HomeTrace.Log("read.page", "got=" + (answer.Titles?.Count ?? 0)
            + " titles=" + Titles.Count + " more=" + HasMore);

        // Where a new shelf stands the caret: behind the last title the viewer finished, rather than at its head.
        Selected = Selected ?? RowTheShelfOpensOn();
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

    /// <summary>
    /// Reads a grouped shelf: the categories values first, then one question per value, so that every title of
    /// the shelf stands under its value's plate and no title is lost on the way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The plates go up before their titles: the server's count answers the whole category in one question, so
    /// the viewer sees WHICH groups there are - and how many each holds - the moment the shelf opens, and the
    /// groups fill themselves in behind that skeleton the same way a flat shelf fills itself (<see
    /// cref="ReadTheRestAsync"/> keeps asking while anything is owed). The plates never move once stood; a page
    /// of titles is inserted where its own group ends, which is under its own plate and above the next group's.
    /// </remarks>
    /// <remarks>
    /// <para>
    /// One question per value rather than one question for the whole shelf re-sorted on screen, for the same
    /// reason a flat shelf re-asks rather than reshuffles (see <see cref="PickSortAsync"/>): the screen only
    /// ever holds the pages it has read, and sorting read pages is a shelf whose order is a lie about the
    /// unread rest. The server already knows how to answer "which titles carry this value" - that is what the
    /// filter asks - and a grouping is exactly those answers, all of them, end to end.
    /// </para>
    /// <para>
    /// The count a plate wears is the whole shelf counted by the category; the moment a search, a watched cut,
    /// a filter or a parent narrows the reading, plate and shelf answer different questions, so the plate
    /// keeps its number to itself and only says its word. The foot's total is always honest either way: it
    /// sums what the per-group answers themselves called their totals, which is the narrowed question counted
    /// per group.
    /// </para>
    /// </remarks>
    private async Task ReadGroupedAsync(int offset)
    {
      CancellationTokenSource? previous = _running;
      CancellationTokenSource cancellation = new CancellationTokenSource();
      _running = cancellation;
      previous?.Cancel();

      if (offset == 0)
        Interlocked.Increment(ref _generation);

      IsBusy = true;
      try
      {
        string by = Grouping!;
        bool whole_shelf = string.IsNullOrEmpty(ActiveSearch) && string.IsNullOrEmpty(ActiveSeen)
            && !HasShelfFilter && Inside == null;

        string? sorted_by = null;
        if (offset == 0)
        {
          MediaGroups counted = await _client!.GetGroupsAsync(AspectName, by, cancellation.Token);

          Titles.Clear();
          Rows.Clear();
          _shelfGroups.Clear();

          foreach (MediaGroup group in counted.Values)
          {
            ShelfGroupRun run = new ShelfGroupRun(group.Value!.Trim(), group.Count);
            Rows.Add(new GroupHeaderRow(run.Value, run.Count, whole_shelf));
            run.RowsEnd = Rows.Count;
            _shelfGroups.Add(run);
          }

          if (!string.IsNullOrEmpty(counted.Error))
            Status = counted.Error;

          if (_shelfGroups.Count == 0)
          {
            // Nothing carries any value of the category at all: the plates that would have stood are none,
            // and the honest sentence is the one the values page has always said about such a counting.
            HasMore = false;
            TotalNumber = 0;
            if (string.IsNullOrEmpty(counted.Error))
              Status = "Nothing on this server carries a " + CategoryWord(by);
            RaiseShelfCounters();
            return;
          }

          foreach (ShelfGroupRun run in _shelfGroups)
          {
            MediaTitles answer = await ReadGroupPageAsync(run, OPENING_PAGE, cancellation.Token);
            sorted_by ??= answer.Sort;
          }
        }
        else
        {
          ShelfGroupRun? owed = null;
          foreach (ShelfGroupRun run in _shelfGroups)
          {
            if (run.Remaining > 0)
            {
              owed = run;
              break;
            }
          }

          if (owed == null)
          {
            HasMore = false;
            return;
          }

          MediaTitles answer = await ReadGroupPageAsync(owed, REST_IN_ONE_ASK, cancellation.Token);
          sorted_by ??= answer.Sort;
        }

        int total = 0;
        foreach (ShelfGroupRun run in _shelfGroups)
          total += run.Total;
        TotalNumber = total;

        HasMore = Titles.Count < MAX_ROWS && AnyGroupOwesMore();

        // Where a new shelf stands the caret, the same answer a flat shelf gives - behind the last title the
        // viewer finished. A plate is never an answer to "where does a new shelf start", because nothing can
        // stand on it, so the walk is over the titles of the groups rather than over their plates.
        Selected = Selected ?? RowTheShelfOpensOn();
        RaiseShelfCounters();

        string seeing = ActiveSeen == "unwatched" ? "unwatched "
            : ActiveSeen == "watched" ? "watched "
            : "";
        string about = Parent == null ? Heading.ToLowerInvariant() : MediaCatalog.RowNounFor(AspectName);

        if (!string.IsNullOrEmpty(ActiveSearch))
        {
          Status = string.Format("{0} of {1} {2}{3} match '{4}', grouped by {5}", Titles.Count, TotalNumber,
              seeing, about, ActiveSearch, CategoryWord(by));
        }
        else
        {
          Status = string.Format("{0} of {1} {2}{3}, grouped by {4}", Titles.Count, TotalNumber, seeing, about,
              CategoryWord(by))
              + (sorted_by == null ? "" : ", sorted by " + sorted_by);
        }

        if (AnyGroupOwesMore() && Titles.Count >= MAX_ROWS)
          Status = "The first " + MAX_ROWS + " of " + TotalNumber + " " + seeing + about
              + ". Find or filter the shelf to narrow it down.";
      }
      catch (ServerApiException ex)
      {
        if (ex.StatusCode == 403)
          Status = Heading + " is only for signed-in users on this server. Sign in on the server screen.";
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
        Status = "Reading failed: " + ex.Message;
      }
      finally
      {
        if (ReferenceEquals(_running, cancellation))
        {
          _running = null;
          IsBusy = false;
          if (HasMore)
            _ = ReadTheRestAsync(Volatile.Read(ref _generation));
        }
        cancellation.Dispose();
      }
    }

    /// <summary>Whether any group is still owed titles by the server's own last answer.</summary>
    private bool AnyGroupOwesMore()
    {
      foreach (ShelfGroupRun run in _shelfGroups)
      {
        if (run.Remaining > 0)
          return true;
      }

      return false;
    }

    /// <summary>
    /// Reads one group's next page and threads it into the wall where that group stands: into the typed shelf at
    /// its end (the shelf's memory is group by group, which is the order the viewer reads it in) and into the
    /// wall at its own group's end, under its own plate and above the next group's.
    /// </summary>
    private async Task<MediaTitles> ReadGroupPageAsync(ShelfGroupRun run, int limit, CancellationToken cancellation)
    {
      MediaTitles answer = await _client!.GetTitlesAsync(AspectName, SortOrder, SortDescending,
          limit, run.Offset,
          string.IsNullOrEmpty(ActiveSearch) ? null : ActiveSearch,
          string.IsNullOrEmpty(ActiveSeen) ? null : ActiveSeen, Inside,
          Grouping, run.Value, SeasonFilter, cancellation);

      int insert_at = run.RowsEnd;
      foreach (MediaTitle title in answer.Titles ?? Array.Empty<MediaTitle>())
      {
        TitleViewModel row = new TitleViewModel(title, DrillsIn, _client, AspectName)
        {
          WatchFlagsShown = WatchFlagsShown,
          InsideName = Parent?.Title.Title
        };

        if (title.ParentId.HasValue)
          _asks[title.Id] = (title.ParentId.Value, title.Season);

        Titles.Add(row);
        Rows.Insert(insert_at, row);
        insert_at++;
      }

      int got = insert_at - run.RowsEnd;
      run.RowsEnd = insert_at;
      bool behind = false;
      foreach (ShelfGroupRun later in _shelfGroups)
      {
        if (!behind)
        {
          behind = ReferenceEquals(later, run);
          continue;
        }
        later.RowsEnd += got;
      }

      run.Offset += got;
      run.Remaining = answer.Remaining;
      run.Total = answer.Total;

      if (!string.IsNullOrEmpty(answer.Error))
        Status = answer.Error;

      return answer;
    }
  }

  /// <summary>
  /// A plate that stands in the wall in front of one group of titles while the shelf is grouped: the value's
  /// own word, and what the server counted under it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is deliberately not a title and never can be one: it has no id, no file, no details and nothing to
  /// play, so a caret that somehow stood on it could not do the one thing a wrong caret does worst. The wall
  /// lays it like any other item - which is the only way anything stands BETWEEN the covers of one horizontal
  /// row - and the view hands it no caret, no press and no picture; the grouping's contract is that every
  /// title stays and only the standing changes, and a divider the viewer could get stuck on would change more
  /// than the standing.
  /// </para>
  /// <para>
  /// <see cref="ShowsCount"/> is decided by whoever builds the plate, not by the plate: the count comes from
  /// the server counting the WHOLE shelf by the category, so the moment a search, a watched cut or a filter
  /// narrows what the shelf reads, the counted number and the titles on screen answer different questions -
  /// and a header naming a number the shelf cannot show is a header that lies. Then it says its word alone.
  /// </para>
  /// </remarks>
  public sealed class GroupHeaderRow
  {
    public GroupHeaderRow(string label, int count, bool showsCount)
    {
      Label = label;
      Count = count;
      ShowsCount = showsCount;
    }

    /// <summary>The value's own word - "Action", "2022", "Tom Hanks".</summary>
    public string Label { get; }

    /// <summary>What the server counted under this value, for the dim number beside the word.</summary>
    public int Count { get; }

    /// <summary>Whether that number may be said, which a narrowed shelf does not allow.</summary>
    public bool ShowsCount { get; }

    /// <summary>The count as the plate speaks it, or nothing when the plate may not speak it.</summary>
    public string CountWord => ShowsCount ? Count.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
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
    public MediaBrowserMenuEntry(string label, string icon, System.Windows.Input.ICommand command, bool isCurrent,
        string detail = "")
    {
      Label = label;
      Icon = icon;
      Command = command;
      IsCurrent = isCurrent;
      Detail = detail ?? string.Empty;
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

    /// <summary>
    /// What the row has to say beyond its name, for the line at the screen's foot: the end an order reads the
    /// shelf from, and nothing else.  Empty for the rows that have no second word to add, which is most of them -
    /// a line that fills itself with guesses is worse than one that stays quiet.
    /// </summary>
    public string Detail { get; }
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
