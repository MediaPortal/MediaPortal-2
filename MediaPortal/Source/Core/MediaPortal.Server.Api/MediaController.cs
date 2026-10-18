#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// The titles in the media library: the names a viewer picks from, and where each of them is.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <see cref="LibraryController"/> describes the schema; this describes the content, which is the line that
  /// file drew between "anybody may ask" and "somebody who is somebody first". Titles, years and file paths are
  /// a household's data, so the endpoint carries <see cref="MediaPortalAuthorizeAttribute"/> and inherits MP2's
  /// rule from there: identify the caller, unless the server was configured not to care. It is not a stricter
  /// rule than the rest of MP2's remote API (the Incubator's MPExtended controllers did the same) and not a
  /// looser one.
  /// </para>
  /// <para>
  /// <b>The path is the whole point.</b> A title a front end cannot reach is a screenshot. Every item therefore
  /// carries the serialized <see cref="ResourcePath"/> of its primary resource, taken the same way the local UI
  /// takes it - <see cref="MediaItem.GetResourceLocator"/> - because an importer stores the path through the
  /// provider that found the file, and rebuilding that path from the file name and a share would be guessing at
  /// work the database has already done. A client that can resolve that path (locally, over a share, over HTTP
  /// later) can play the item; one that cannot still lists it, which is what the field being nullable says.
  /// </para>
  /// <para>
  /// Virtual items - directories, and the entries the importer registers only to browse by - are never in the
  /// answer. They are what <see cref="Browse"/> would be for, they have no primary resource to play, and a media
  /// menu that lists folders next to films is a menu a viewer has to clean up first.
  /// </para>
  /// </remarks>
  [ApiController]
  [Route("api/[controller]")]
  [MediaPortalAuthorize]
  public class MediaController : ControllerBase
  {
    /// <summary>How many titles an answer holds when the caller does not say.</summary>
    public const int DEFAULT_LIMIT = 500;

    /// <summary>
    /// Ceiling on a page, so that one request cannot turn into the server reading a 200 000 item library into a
    /// JSON array while the front end that asked can show twenty of them.
    /// </summary>
    public const int MAX_LIMIT = 2000;

    /// <summary>
    /// The MediaPortal user profile the bearer token names, or null when the request carries no name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The token has always carried it: <c>MediaPortalCredentials</c> puts the profile id in
    /// <see cref="ClaimTypes.Sid"/> when it grants a password grant
    /// (MediaPortalCredentials.cs:165) and <c>MediaPortalAccessToken</c> signs it into the payload
    /// (MediaPortalAccessToken.cs:70). <see cref="SessionController"/> was the only endpoint reading it, which
    /// is why this controller could answer "who is watching?" but not "what has <i>this</i> person watched?".
    /// </para>
    /// <para>
    /// Null is not a failure, it is an answer with a different scope: without a name the library is read as the
    /// household's, so a title says whether <i>anybody</i> finished it and no resume position exists at all.
    /// <see cref="Guid.Empty"/> is deliberately folded into that null rather than passed on - the query engine
    /// treats a profile that merely <i>has</i> a value as a real one and joins the user data table on all zeros
    /// (CompiledMediaItemQuery.cs:422), which yields no rows and looks like an empty shelf rather than like an
    /// anonymous request.
    /// </para>
    /// </remarks>
    private Guid? CallerProfile
    {
      get
      {
        ClaimsPrincipal caller = HttpContext != null ? HttpContext.User : null;
        string sid = caller != null ? caller.FindFirstValue(ClaimTypes.Sid) : null;
        Guid profileId;
        if (sid == null || !Guid.TryParse(sid, out profileId) || profileId == Guid.Empty)
          return null;
        return profileId;
      }
    }

    /// <summary>
    /// One title, as a front end shows it in a list.
    /// </summary>
    public class TitleResult
    {
      public Guid Id { get; set; }

      public string Title { get; set; }

      /// <summary>
      /// The title with a leading article moved to the back, which is what the library sorts by and what a
      /// viewer expects when "The Shawshank Redemption" turns up under S.
      /// </summary>
      public string SortTitle { get; set; }

      /// <summary>
      /// Year of the recording time, or null when the item does not carry one. Kept as a year rather than a
      /// date because that is all a title list shows; the full time is on the item for whoever asks for it.
      /// </summary>
      public int? Year { get; set; }

      /// <summary>
      /// Length in minutes where the library knows it (movies store it), otherwise null.
      /// </summary>
      public int? RuntimeMinutes { get; set; }

      /// <summary>
      /// The item's primary resource, serialized as MP2 serializes a path. Null when the item has no primary
      /// resource at all, which a stub or an item whose provider went away looks like.
      /// </summary>
      public string Path { get; set; }

      /// <summary>
      /// A stub is a title whose file is not there but whose metadata is, offered through a stub provider.
      /// Said apart from "no path" because a viewer wants to know that the film is known, not lost.
      /// </summary>
      public bool Stub { get; set; }

      /// <summary>
      /// How often the library was told this title was played to the end, or null when it holds no Media aspect.
      /// Zero means "never finished", which is not the same as "never opened" - <see cref="LastPlayed"/> says that.
      /// </summary>
      public int? PlayCount { get; set; }

      /// <summary>
      /// When the title was last played, or null when the library was never told. Carried in the list rather than
      /// only in the item because a viewer scanning a shelf reads "seen" from the row, not from a panel opened per
      /// title.
      /// </summary>
      public DateTime? LastPlayed { get; set; }

      /// <summary>
      /// How far the caller had got in this title, 1 to 99, or null. Null means one of two things and the answer
      /// cannot tell them apart: the caller is nobody (an unauthenticated request has no resume positions), or
      /// that person has nothing part-way here - never opened, or watched to the end, which is not a position to
      /// resume from.
      /// </summary>
      /// <remarks>
      /// Read from the caller's own user data, which the library only attaches when the request named somebody
      /// (see <see cref="CallerProfile"/>). A hundred is deliberately not reported: MP2's continue-watch reading
      /// of the same number asks for a percentage strictly between nought and a hundred
      /// (BaseContinueWatchMediaListProvider.cs:43-46), because a film that is finished has nothing left to
      /// resume, and a row offering to resume one would offer to watch it again from the first minute.
      /// </remarks>
      public int? Resume { get; set; }

      /// <summary>
      /// For an episode: the season number the library holds. Null for anything that is not an episode, and for an
      /// episode whose season was never written.
      /// </summary>
      /// <remarks>
      /// A season of nought is MP2's own spelling for specials, but it is also what an episode with nothing
      /// written in that column reads as, and the two cannot be told apart from the attribute alone. Nought is
      /// therefore reported as nothing known: a row that announced "specials" about a file nobody tagged would be
      /// making a claim about somebody's television series out of an empty column.
      /// </remarks>
      public int? Season { get; set; }

      /// <summary>
      /// For an episode: its number within the season, or null. Where a file holds two episodes the lowest number
      /// is named here, because that is the one a row has room for; the item carries all of them.
      /// </summary>
      public int? Episode { get; set; }

      /// <summary>
      /// For a track: its number on the album, or null when the album it came from numbered nothing.
      /// </summary>
      public int? Track { get; set; }

      /// <summary>
      /// The name of whoever made this: for an album or a track, the first artist the library lists. Null for the
      /// media that has no artist - a film is not credited to one on the aspects this endpoint reads.
      /// </summary>
      /// <remarks>
      /// One name and not the list, because this is a row in a list: an album credited to four people and a
      /// compiler is one line on a television two metres away. MP2 stores these names on the title itself
      /// (<c>AudioAlbumAspect.ATTR_ARTISTS</c>, <c>AudioAspect.ATTR_ARTISTS</c>), so unlike genres and people they
      /// cost no relationship query to say.
      /// </remarks>
      public string Artist { get; set; }

      /// <summary>
      /// For an album: how many tracks the library says belong on it. Null when it does not say.
      /// </summary>
      /// <remarks>
      /// The number the album was tagged with, not a count of the track items that turned up - which is what a
      /// viewer reading "12 tracks" on a shelf of a half-ripped CD gets, and what the difference between the two
      /// means is the importer's business, not this answer's.
      /// </remarks>
      public int? TrackCount { get; set; }
    }

    /// <summary>
    /// The answer of <c>GET api/media/titles</c>.
    /// </summary>
    public class TitlesResult
    {
      public string Aspect { get; set; }

      public Guid? AspectId { get; set; }

      /// <summary>
      /// How many titles the aspect holds in total, not just this page - what a front end needs to know whether
      /// to offer more.
      /// </summary>
      public int Total { get; set; }

      public int Offset { get; set; }

      public int Limit { get; set; }

      public int Returned { get; set; }

      /// <summary>
      /// What the answer is sorted by, echoed back because "the list looks unsorted" is answered by this field
      /// rather than by an argument.
      /// </summary>
      public string Sort { get; set; }

      /// <summary>
      /// The words the answer was filtered by, or null when it was not filtered. Echoed because a front end that
      /// shows an empty list has to be able to tell "nobody owns anything called <i>alien</i>" from "the library is
      /// empty", and because a search box that silently lost its text is worse than one that never existed.
      /// </summary>
      public string Search { get; set; }

      /// <summary>
      /// How the answer was filtered by watched state: <c>unwatched</c>, <c>watched</c>, or null when every title
      /// was wanted. Echoed for the same reason as <see cref="Search"/>: a one-row list has to say whether it is a
      /// short shelf or a filter the viewer forgot about.
      /// </summary>
      public string Seen { get; set; }

      /// <summary>
      /// The title this answer was read from inside of - the album whose tracks these are, the series whose
      /// episodes these are - or null when the answer is the whole shelf. Echoed for the same reason as
      /// <see cref="Search"/>: a list of twelve tracks has to be able to say that it is the tracks of one album
      /// rather than a library of twelve.
      /// </summary>
      public Guid? Inside { get; set; }

      public List<TitleResult> Titles { get; set; }

      /// <summary>
      /// Set when the answer is empty for a reason the caller can act on.
      /// </summary>
      public string Error { get; set; }
    }

    /// <summary>
    /// Lists the titles carrying the given aspect.
    /// </summary>
    /// <param name="aspect">Aspect name as <c>GET api/library/aspects</c> reports it, e.g. <c>Movie</c>.</param>
    /// <param name="sort"><c>title</c> (default), <c>year</c>, or <c>none</c> for whatever order the database returns.</param>
    /// <param name="order"><c>asc</c> (default) or <c>desc</c>.</param>
    /// <param name="limit">Titles in the answer, capped at 2000.</param>
    /// <param name="offset">How many to skip, for a front end that fills its list as the viewer scrolls.</param>
    /// <param name="search">
    /// Words to look for. The answer then holds only the titles of this aspect that carry the words somewhere in
    /// their short text - the <c>search</c> field of the answer says what was asked for.
    /// </param>
    /// <param name="seen">
    /// <c>unwatched</c> for the titles nobody has finished, <c>watched</c> for the ones somebody has,
    /// <c>started</c> for the ones somebody began and did not finish, anything else - including nothing - for
    /// both. The <c>seen</c> field of the answer says which of the four readings was applied.
    /// </param>
    /// <param name="inside">
    /// The id of a title that holds other titles - an album, a series, a season, a film collection - to list the
    /// titles inside it: the tracks of an album with <c>aspect=AudioItem</c>, the episodes of a series with
    /// <c>aspect=EpisodeItem</c>. What is asked for has to be something that title can hold, or the answer is a
    /// refusal rather than an empty list. Null or empty means the whole shelf, which is what it has always meant.
    /// </param>
    [HttpGet("titles")]
    public ActionResult<TitlesResult> GetTitles([FromQuery] string aspect, [FromQuery] string sort,
        [FromQuery] string order, [FromQuery] int limit, [FromQuery] int offset, [FromQuery] string search,
        [FromQuery] string seen, [FromQuery] Guid inside = default(Guid))
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      if (string.IsNullOrEmpty(aspect))
        return BadRequest(new TitlesResult { Error = "an aspect has to be named, e.g. ?aspect=Movie" });

      int take = limit <= 0 ? DEFAULT_LIMIT : Math.Min(limit, MAX_LIMIT);
      int skip = Math.Max(offset, 0);

      try
      {
        IDictionary<Guid, MediaItemAspectMetadata> metadata = mediaLibrary.GetManagedMediaItemAspectMetadata();
        KeyValuePair<Guid, MediaItemAspectMetadata> match = metadata.FirstOrDefault(entry =>
            entry.Value != null && string.Equals(entry.Value.Name, aspect, StringComparison.OrdinalIgnoreCase));
        if (match.Value == null)
          return NotFound(new TitlesResult { Error = "this server has no aspect called '" + aspect + "'" });

        Guid aspectId = match.Key;

        // The Media aspect is asked for on every request, and not as a necessary one: it is what carries the
        // title, the year and the flag this endpoint sorts by, and it is on every item the importer knows - but
        // asking for it as necessary would silently drop any item the database has without it, and such an item
        // would then be missing from a list its owner can see on the television with no way to notice.
        List<Guid> necessary = new List<Guid> { aspectId };
        List<Guid> optional = new List<Guid> { MediaAspect.ASPECT_ID, ProviderResourceAspect.ASPECT_ID };
        if (aspectId == MediaAspect.ASPECT_ID)
          optional.RemoveAt(0);
        if (aspectId == ProviderResourceAspect.ASPECT_ID)
          optional.Remove(ProviderResourceAspect.ASPECT_ID);

        // The movie aspect is asked for whenever it exists, because that is where MP2 stores the runtime, and a
        // title list without a length is only worse for the aspects that have one.
        MediaItemAspectMetadata movie = metadata.Values.FirstOrDefault(entry =>
            entry != null && entry.AspectId == MovieAspect.ASPECT_ID);
        if (movie != null && aspectId != MovieAspect.ASPECT_ID)
          optional.Add(MovieAspect.ASPECT_ID);

        // What the viewer asked the library to contain. Whitespace counts as nothing asked, because a search box
        // that the viewer cleared should show the whole shelf again rather than an empty one.
        string finding = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        // Whether the viewer has finished a title is a question MP2 already answers with one condition on the
        // number this server itself wrote when a front end reported the end of a film: its own
        // BaseLastWatchedMediaListProvider filters PlayCount > 0 and BaseUnwatchedMediaListProvider PlayCount = 0
        // (BaseUnwatchedMediaListProvider.cs:45). The two conditions here are those two.
        //
        // The unwatched one needs a second half that MP2's provider does not, because PlayCount can be null rather
        // than zero: an imported film nobody has opened has never had anything written to that column, and a
        // condition of "PlayCount = 0" is not true about a column that holds nothing - in SQL it is unknown, and an
        // unknown in a WHERE clause throws the title out of the list. That would mean a brand new library reporting
        // "no unwatched titles", which is the one answer a viewer can tell is wrong. EmptyFilter compiles to
        // "IS NULL" (CompiledFilter.cs:255), so the two together mean "nothing was ever written here, or zero was".
        string seeing = ReadSeen(seen);
        IFilter seenFilter = null;
        if (seeing == "watched")
          seenFilter = new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.GT, 0);
        else if (seeing == "unwatched")
          seenFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
              new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.EQ, 0),
              new EmptyFilter(MediaAspect.ATTR_PLAYCOUNT));
        else if (seeing == "started")
          // The third question a viewer asks: "what did I leave half way?". MP2's own
          // BaseContinueWatchMediaListProvider answers it per profile, with "a play date exists, and the played
          // percentage is between one and ninety-nine" (BaseContinueWatchMediaListProvider.cs:43-46); the two
          // halves of that condition are exactly what LastPlayed-with-no-PlayCount means on the Media aspect,
          // which is what a server without profiles - this one - has. Without the second half the filter would
          // also hand back films that were watched to the end, because finishing a film leaves its last-played
          // date behind, and a "still to finish" list holding finished films is worse than no list.
          seenFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
              new NotFilter(new EmptyFilter(MediaAspect.ATTR_LASTPLAYED)),
              BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
                  new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.EQ, 0),
                  new EmptyFilter(MediaAspect.ATTR_PLAYCOUNT)));

        // The titles inside one title: the tracks on an album, the episodes of a season or a series, the films of
        // a collection. MP2 does not store a parent on its children - an episode carries the <i>name</i> of its
        // series, which is a string two series can share and that an importer rewrites - it links the two with a
        // relationship row, and that is how MP2's own screens find an album's music and a series' episodes
        // (LocalAlbumFanartProvider.cs:79 asks for the tracks of an album exactly this way). Which pair of roles
        // makes the link mean something is what <see cref="TryRelationship"/> knows.
        //
        // A parent that cannot hold the aspect being asked about is refused instead of answered with an empty
        // list. An empty list reads as "this album has no music in it", and the truth is "an album never holds
        // films, and you have asked me the wrong question" - the second is a bug in whoever asked, and saying it
        // loudly is kinder than agreeing.
        IFilter withinFilter = null;
        if (inside != Guid.Empty)
        {
          MediaItem parent = FindParentTitle(mediaLibrary, metadata, inside);
          if (parent == null)
            return NotFound(new TitlesResult
            {
              Aspect = match.Value.Name,
              Error = "this server has no title with the id '" + inside.ToString("D") + "' to list titles inside"
            });

          Guid role;
          Guid linkedRole;
          if (!TryRelationship(aspectId, parent, out role, out linkedRole))
            return BadRequest(new TitlesResult
            {
              Aspect = match.Value.Name,
              Inside = inside,
              Error = "'" + match.Value.Name + "' titles are not filed inside the title " + inside.ToString("D")
            });

          withinFilter = new RelationshipFilter(role, linkedRole, inside);
        }

        // Both questions at once if both were asked: the tracks of this album that nobody has finished, say.
        IFilter asking = seenFilter == null
            ? withinFilter
            : withinFilter == null
                ? seenFilter
                : BooleanCombinationFilter.CombineFilters(BooleanOperator.And, seenFilter, withinFilter);

        // MP2 already knows how to look for words in the library, and it is not "compare the title":
        // BuildSimpleTextSearchQuery asks the database whether any of the short text attributes of the aspects
        // being asked about carry the words, which is what typing "star trek" has to mean - a film is found under
        // its title, its sort title, its collection or its tagline, not under whichever one a hand-written filter
        // happened to pick. It also escapes the LIKE wildcards, which a filter built here would forget and a viewer
        // typing an underscore would notice as an empty list.
        // The last two arguments are the two a viewer would notice if they were the other way round: CLOBs are left
        // out, because every film whose plot mentions "spaceship" is not what a search for it asked for, and the
        // match is not case sensitive, because a remote control has no shift key.
        MediaItemQuery query = finding == null
            ? new MediaItemQuery(necessary, optional, asking)
            : mediaLibrary.BuildSimpleTextSearchQuery(finding, necessary, optional, asking, false, false);

        string sortBy = ReadSort(sort);
        List<ISortInformation> sorts = BuildSorts(sortBy, order);
        if (sorts.Count > 0)
          query.SetSortInformation(sorts);
        query.Offset = (uint)skip;
        query.Limit = (uint)take;

        // The last argument is includeVirtual, and false is the answer to "give me things to watch". The one
        // before it is the caller's user profile, which is what makes the library attach that person's own
        // playback data - how far they got in each title - to the rows. It is null when the request carries no
        // name, and then the answer is the household's: watched means "somebody finished it", and there is no
        // resume position to report because nobody's was asked for.
        Guid? profile = CallerProfile;
        IList<MediaItem> items = mediaLibrary.Search(query, false, profile, false);

        List<TitleResult> titles = items.Select(item => ToTitle(item, profile.HasValue)).ToList();

        // "how many are there" is asked through the same filter the list was read through, or a front end that
        // reached the bottom of a search would keep offering more of a list that has no more in it - and would say
        // "12 of 4 000" about a search that found twelve titles out of four thousand.
        int total = mediaLibrary.CountMediaItems(new[] { aspectId }, query.Filter, false, false);

        return new TitlesResult
        {
          Aspect = match.Value.Name,
          AspectId = aspectId,
          Total = total,
          Offset = skip,
          Limit = take,
          Returned = titles.Count,
          Sort = sortBy + (IsDescending(order) ? " descending" : ""),
          Search = finding,
          Seen = seeing,
          Inside = inside == Guid.Empty ? (Guid?)null : inside,
          Titles = titles
        };
      }
      catch (IllegalCallException ex)
      {
        return NotReady(ex);
      }
    }

    /// <summary>
    /// One title in full: what the library knows about it beyond the row in a list.
    /// </summary>
    public class ItemResult
    {
      public Guid Id { get; set; }

      public string Title { get; set; }

      public string SortTitle { get; set; }

      /// <summary>Year from the recording/import date, the same one the title list shows.</summary>
      public int? Year { get; set; }

      public int? RuntimeMinutes { get; set; }

      /// <summary>
      /// What the title is about, in the library's own words: the comment the importer or the viewer left on the
      /// Media aspect. Null when nothing was written there, which for an imported film without an NFO file is the
      /// normal answer, not a failure of this endpoint.
      /// </summary>
      public string Overview { get; set; }

      /// <summary>The plot the video importer found, which MP2 keeps apart from the comment.</summary>
      public string StoryPlot { get; set; }

      public string Tagline { get; set; }

      public string Certification { get; set; }

      /// <summary>The collection a film belongs to, e.g. "Alien Collection". Null for the rest.</summary>
      public string Collection { get; set; }

      /// <summary>The library's rating for the title, on MP2's 0-10 scale. Null when nothing rated it.</summary>
      public int? Rating { get; set; }

      /// <summary>For an episode: the series it belongs to. Null for anything else.</summary>
      public string SeriesName { get; set; }

      public int? Season { get; set; }

      public int? Episode { get; set; }

      /// <summary>The episode's own name, which for a series is usually not the title of the file.</summary>
      public string EpisodeName { get; set; }

      /// <summary>How often the library was told this title was played, across all titles.</summary>
      public int? PlayCount { get; set; }

      /// <summary>When it was last played, or null if the library was never told.</summary>
      public DateTime? LastPlayed { get; set; }

      /// <summary>
      /// How far the caller had got, 1 to 99, or null - the same figure the title list carries and the same two
      /// reasons for it to be absent: nobody asked, or this person has nothing part-way here.
      /// </summary>
      public int? Resume { get; set; }

      /// <summary>Serialized path of the primary resource, exactly as the title list gives it.</summary>
      public string Path { get; set; }

      public bool Stub { get; set; }

      /// <summary>Set when the answer is empty for a reason the caller can act on.</summary>
      public string Error { get; set; }
    }

    /// <summary>
    /// Everything this server knows about one title, by its id.
    /// </summary>
    /// <param name="id">The media item id, as <c>GET api/media/titles</c> reported it.</param>
    /// <remarks>
    /// <para>
    /// The id is enough: a media item id is a Guid the database gave to one item, so unlike
    /// <see cref="IMediaLibrary.LoadItem(string, Guid, IEnumerable{Guid}, IEnumerable{Guid}, Guid?)} this does not
    /// have to be told which media system the item came from - the search finds the item wherever it is filed.
    /// </para>
    /// <para>
    /// <b>What is not in the answer, on purpose:</b> genres and people. MP2 does not store them on the title; the
    /// importer files them as separate items linked by relationships (<c>GenreAspect</c>, <c>PersonAspect</c>), so
    /// naming them here would mean a relationship query per title and an answer that looks complete while
    /// reporting the ids of other items. The fields the title carries itself are what arrives, and a null means
    /// the library has nothing - which is the common case for a film imported without an NFO file.
    /// </para>
    /// </remarks>
    [HttpGet("items/{id}")]
    public ActionResult<ItemResult> GetItem(Guid id)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      try
      {
        // The Media aspect is the necessary one because it is the one this answer is meaningless without: an item
        // the database holds with no title and no date is a row, not a title, and "no such title" is the honest
        // answer to a request for it. Everything else is optional, so a film with only a Media aspect still gets
        // its answer rather than a 404 for lacking a plot.
        List<Guid> necessary = new List<Guid> { MediaAspect.ASPECT_ID };
        List<Guid> optional = new List<Guid>
        {
          ProviderResourceAspect.ASPECT_ID,
          MovieAspect.ASPECT_ID,
          VideoAspect.ASPECT_ID,
          EpisodeAspect.ASPECT_ID
        };

        MediaItemQuery query = new MediaItemQuery(necessary, optional, new MediaItemIdFilter(id));
        Guid? profile = CallerProfile;
        IList<MediaItem> items = mediaLibrary.Search(query, false, profile, false);
        MediaItem item = items == null ? null : items.FirstOrDefault();

        if (item == null)
          return NotFound(new ItemResult { Id = id, Error = "this server has no title with the id " + id });

        return ToItem(item, profile.HasValue);
      }
      catch (IllegalCallException ex)
      {
        return NotReadyItem(ex);
      }
    }

    /// <summary>
    /// The attribute's text, or null when it holds none.
    /// </summary>
    /// <remarks>
    /// The database stores an unset single-string attribute as the empty string, and a details screen handed one
    /// would draw a blank line - which reads as a broken screen rather than as a title nobody described. Trimmed
    /// for the same reason: a stored space is not a plot summary.
    /// </remarks>
    private static string Text(string value)
    {
      return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// The first name in a list of them, or <c>null</c> when the list is empty or holds nothing but blanks.
    /// </summary>
    /// <remarks>
    /// MP2 keeps the artists of an album and of a track as a list - an album can be credited to a band, a producer
    /// and a featured artist at once - and this is a row in a list, which has room for the one name a viewer reads
    /// first. The whole list is on the item for whoever wants all of it.
    /// </remarks>
    private static string FirstText(IEnumerable<string> values)
    {
      if (values == null)
        return null;

      foreach (string value in values)
        if (!string.IsNullOrWhiteSpace(value))
          return value;

      return null;
    }

    /// <summary>
    /// Turns a media item into the answer of <see cref="GetItem"/>.
    /// </summary>
    /// <param name="askedAsSomebody">
    /// Whether the request named a user profile, which decides whether the answer may carry that person's position
    /// in the title. The same rule as in <see cref="ToTitle"/>.
    /// </param>
    /// <remarks>
    /// Every attribute read is one the item may not carry, so each is read inside its aspect's presence check -
    /// and a missing one stays null rather than becoming zero, which for a runtime or a season number would be a
    /// claim about the media that the database never made.
    /// </remarks>
    private static ItemResult ToItem(MediaItem item, bool askedAsSomebody)
    {
      ItemResult result = new ItemResult { Id = item.MediaItemId };

      SingleMediaItemAspect media;
      if (MediaItemAspect.TryGetAspect(item.Aspects, MediaAspect.Metadata, out media))
      {
        result.Title = Text(media.GetAttributeValue<string>(MediaAspect.ATTR_TITLE));
        result.SortTitle = Text(media.GetAttributeValue<string>(MediaAspect.ATTR_SORT_TITLE));
        result.Overview = Text(media.GetAttributeValue<string>(MediaAspect.ATTR_COMMENT));
        result.Rating = media.GetAttributeValue<int>(MediaAspect.ATTR_RATING);
        result.PlayCount = media.GetAttributeValue<int>(MediaAspect.ATTR_PLAYCOUNT);
        DateTime lastPlayed = media.GetAttributeValue<DateTime>(MediaAspect.ATTR_LASTPLAYED);
        if (lastPlayed != DateTime.MinValue)
          result.LastPlayed = lastPlayed;
        DateTime recordingTime = media.GetAttributeValue<DateTime>(MediaAspect.ATTR_RECORDINGTIME);
        if (recordingTime != DateTime.MinValue)
          result.Year = recordingTime.Year;
      }

      // The caller's own position, from the user data the search attached to this item when the request named
      // somebody. Kept out of the aspect block above because it is not an attribute of the title - it is this
      // person's, which is why two people asking about the same id get two different answers here.
      result.Resume = askedAsSomebody ? ReadResume(item) : null;

      // The same rule as the title list: "<Unknown>" is MediaItem.Title's own fallback, and a details screen has
      // even less business showing an internal fallback as the name of a film than a list does.
      if (result.Title == "<Unknown>")
        result.Title = null;

      SingleMediaItemAspect movie;
      if (MediaItemAspect.TryGetAspect(item.Aspects, MovieAspect.Metadata, out movie))
      {
        int runtime = movie.GetAttributeValue<int>(MovieAspect.ATTR_RUNTIME_M);
        if (runtime > 0)
          result.RuntimeMinutes = runtime;
        result.Certification = Text(movie.GetAttributeValue<string>(MovieAspect.ATTR_CERTIFICATION));
        result.Tagline = Text(movie.GetAttributeValue<string>(MovieAspect.ATTR_TAGLINE));
        result.Collection = Text(movie.GetAttributeValue<string>(MovieAspect.ATTR_COLLECTION_NAME));
      }

      SingleMediaItemAspect video;
      if (MediaItemAspect.TryGetAspect(item.Aspects, VideoAspect.Metadata, out video))
        result.StoryPlot = Text(video.GetAttributeValue<string>(VideoAspect.ATTR_STORYPLOT));

      SingleMediaItemAspect episode;
      if (MediaItemAspect.TryGetAspect(item.Aspects, EpisodeAspect.Metadata, out episode))
      {
        result.SeriesName = Text(episode.GetAttributeValue<string>(EpisodeAspect.ATTR_SERIES_NAME));
        result.Season = episode.GetAttributeValue<int>(EpisodeAspect.ATTR_SEASON);
        result.EpisodeName = Text(episode.GetAttributeValue<string>(EpisodeAspect.ATTR_EPISODE_NAME));

        // An episode number is a collection in MP2 - a double episode is one item carrying two numbers - so it is
        // read as one, the way the series relationship extractors read it. The first number is what the answer
        // names, because "S01E01" is what a viewer recognises; the rest of a double episode is on the title
        // itself and a screen that has to show two numbers can ask for the item again.
        IEnumerable<int> numbers = episode.GetCollectionAttribute<int>(EpisodeAspect.ATTR_EPISODE);
        if (numbers != null)
        {
          List<int> ordered = numbers.Where(number => number > 0).Distinct().OrderBy(number => number).ToList();
          if (ordered.Count > 0)
            result.Episode = ordered[0];
        }
      }

      result.Stub = item.IsStub;
      result.Path = ReadPath(item);
      return result;
    }

    /// <summary>
    /// The answer for a media library that cannot answer yet, in the shape <see cref="GetItem"/> gives.
    /// </summary>
    /// <remarks>
    /// A second method rather than a shared one because the two endpoints answer with different types, and
    /// <see cref="NotReady"/> builds a <see cref="TitlesResult"/> body; a client that reads <c>error</c> out of
    /// either gets the same word from both, which is the only promise that has to hold.
    /// </remarks>
    private static ObjectResult NotReadyItem(IllegalCallException exception)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>(false);
      if (logger != null)
        logger.Info("MediaController: the media library refused a details request, answering 503: {0}",
            exception.Message);
      return new ObjectResult(new ItemResult { Error = exception.Message })
      {
        StatusCode = StatusCodes.Status503ServiceUnavailable
      };
    }

    /// <summary>
    /// What a viewer reports having watched.
    /// </summary>
    public class PlaybackRequest
    {
      /// <summary>
      /// How far into the title they got, 0 to 100. A hundred says it was watched to the end; anything less says
      /// it was started.
      /// </summary>
      public int Percentage { get; set; }
    }

    /// <summary>
    /// What the library believes about a title after it was told the title was watched.
    /// </summary>
    public class PlaybackResult
    {
      public Guid Id { get; set; }

      /// <summary>How often the title has been watched to the end, including this time when it was.</summary>
      public int PlayCount { get; set; }

      /// <summary>When it was last watched, as the library now has it.</summary>
      public DateTime? LastPlayed { get; set; }

      /// <summary>Whether this report counted as a completed viewing.</summary>
      public bool Watched { get; set; }

      /// <summary>
      /// What the library now holds as the caller's position in this title, 1 to 99, or null when it holds none.
      /// Null after a successful report means the viewing was finished (a hundred is not a position to resume
      /// from) or that the request came from nobody, whose playback is counted but not remembered.
      /// </summary>
      public int? Resume { get; set; }

      /// <summary>Set when the report was refused, in words a screen can show.</summary>
      public string Error { get; set; }
    }

    /// <summary>
    /// Tells the library that a title was watched, and answers with what it now believes.
    /// </summary>
    /// <param name="id">The media item id, as <c>GET api/media/titles</c> reported it.</param>
    /// <param name="playback">How far the viewer got.</param>
    /// <remarks>
    /// <para>
    /// This is the write that makes the numbers <see cref="GetItem"/> reads true. Without it a film can be watched
    /// a dozen times and the library will go on saying nobody ever played it, and a media centre whose "watched"
    /// column is decoration is one a viewer stops trusting. The library is told in the words it has always used,
    /// <see cref="IMediaLibrary.NotifyPlayback(Guid, bool)"/>, which counts a title as watched at the end and
    /// marks the date on anything shorter.
    /// </para>
    /// <para>
    /// <b>Whose viewing it is.</b> When the bearer token names a MediaPortal profile, the report goes to
    /// <see cref="IMediaLibrary.NotifyUserPlayback(Guid, Guid, int, bool)"/>, which is MP2's own entry point for
    /// one person's playback: it writes that person's play date, play count and percentage, and - because it
    /// calls <see cref="IMediaLibrary.NotifyPlayback(Guid, bool)"/> itself first
    /// (MediaLibrary.cs:2809) - the title's shared counters as well. So the watched figures sections
    /// <b>ac</b> and <b>ad</b> read keep moving whether or not anybody signed in, and the resume position is
    /// remembered only for the person who asked. A request from nobody keeps the older, narrower path: the
    /// household's counters move and no position is stored, because a position filed under no profile belongs to
    /// nobody and would show up as a resume offer on somebody else's television.
    /// </para>
    /// <para>
    /// The answer is what the library holds afterwards, read back rather than calculated here, so that a screen
    /// can put the real number on the row instead of the one it hoped for.
    /// </para>
    /// </remarks>
    [HttpPost("items/{id}/playback")]
    public ActionResult<PlaybackResult> ReportPlayback(Guid id, [FromBody] PlaybackRequest playback)
    {
      if (playback == null || playback.Percentage < 0 || playback.Percentage > 100)
      {
        return BadRequest(new PlaybackResult
        {
          Id = id,
          Error = "how far a viewer watched is a number from 0 to 100"
        });
      }

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      try
      {
        Guid? profile = CallerProfile;

        // Asked for before the write, because "there is no such title" and "the title is there and now says it was
        // played" are different answers; a library that never heard of the id must not be told to mark it, and a
        // client must not be told it recorded something that went nowhere.
        if (FindTitle(mediaLibrary, id, profile) == null)
          return NotFound(new PlaybackResult { Id = id, Error = "this server has no title with the id " + id });

        bool watched = playback.Percentage >= 100;
        if (profile.HasValue)
          // One person's viewing. This is the call that also stores the percentage to resume from, and it writes
          // the shared counters itself, so nothing is called twice here.
          mediaLibrary.NotifyUserPlayback(profile.Value, id, playback.Percentage, true);
        else
          // Nobody's viewing, so there is nobody to remember a position for: the title's own counters only.
          mediaLibrary.NotifyPlayback(id, watched);

        return ToPlayback(id, FindTitle(mediaLibrary, id, profile), watched, profile.HasValue);
      }
      catch (IllegalCallException ex)
      {
        return NotReadyPlayback(ex, id);
      }
    }

    /// <summary>
    /// The one item with this id, or null when the library has none.
    /// </summary>
    /// <param name="profile">
    /// The caller's profile, or null to read the title without anybody's playback data on it. Asked for with the
    /// same profile the write went through, so that the numbers this answer reports are the ones that write
    /// produced rather than a second opinion read from a different row of the database.
    /// </param>
    /// <remarks>
    /// Only the Media aspect is necessary and nothing optional is asked for: this is the lookup that decides
    /// "is there such a title, and what does it say about being played", and a details answer would be a second
    /// read of the same row for no reason.
    /// </remarks>
    private static MediaItem FindTitle(IMediaLibrary mediaLibrary, Guid id, Guid? profile)
    {
      MediaItemQuery query = new MediaItemQuery(
          new List<Guid> { MediaAspect.ASPECT_ID }, null, new MediaItemIdFilter(id));
      IList<MediaItem> items = mediaLibrary.Search(query, false, profile, false);
      return items == null ? null : items.FirstOrDefault();
    }

    /// <summary>
    /// The playback counters of a title, as the library holds them now.
    /// </summary>
    /// <param name="askedAsSomebody">
    /// Whether the report came from a named profile, which decides whether the answer may carry a position at all.
    /// </param>
    private static PlaybackResult ToPlayback(Guid id, MediaItem item, bool watched, bool askedAsSomebody)
    {
      PlaybackResult result = new PlaybackResult
      {
        Id = id,
        Watched = watched,
        Resume = askedAsSomebody ? ReadResume(item) : null
      };

      SingleMediaItemAspect media;
      if (item != null && MediaItemAspect.TryGetAspect(item.Aspects, MediaAspect.Metadata, out media))
      {
        result.PlayCount = media.GetAttributeValue<int>(MediaAspect.ATTR_PLAYCOUNT);
        DateTime lastPlayed = media.GetAttributeValue<DateTime>(MediaAspect.ATTR_LASTPLAYED);
        if (lastPlayed != DateTime.MinValue)
          result.LastPlayed = lastPlayed;
      }
      return result;
    }

    /// <summary>
    /// The answer for a media library that cannot write yet, in the shape <see cref="ReportPlayback"/> gives.
    /// </summary>
    private static ObjectResult NotReadyPlayback(IllegalCallException exception, Guid id)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>(false);
      if (logger != null)
        logger.Info("MediaController: the media library refused a playback report, answering 503: {0}",
            exception.Message);
      return new ObjectResult(new PlaybackResult { Id = id, Error = exception.Message })
      {
        StatusCode = StatusCodes.Status503ServiceUnavailable
      };
    }

    /// <summary>
    /// The sorts a title list has: by name, by year, by when it was last watched, by where a title sits in the one
    /// that holds it, or by none at all.
    /// </summary>
    /// <remarks>
    /// Anything else a caller sends is taken to mean "none" rather than refused. A front end that asks for a sort
    /// this server does not have still gets a list, and the <c>sort</c> field in the answer tells whoever looks
    /// what it was actually sorted by.
    /// </remarks>
    private static string ReadSort(string sort)
    {
      if (string.Equals(sort, "year", StringComparison.OrdinalIgnoreCase))
        return "year";
      if (string.Equals(sort, "lastplayed", StringComparison.OrdinalIgnoreCase))
        return "lastplayed";
      if (string.Equals(sort, "episode", StringComparison.OrdinalIgnoreCase))
        return "episode";
      if (string.Equals(sort, "track", StringComparison.OrdinalIgnoreCase))
        return "track";
      if (string.Equals(sort, "none", StringComparison.OrdinalIgnoreCase))
        return "none";
      return "title";
    }

    /// <summary>
    /// The watched filter a request asked for, as the words this endpoint answers with.
    /// </summary>
    /// <remarks>
    /// Anything that is not one of the three named filters means "both", which is also what a misspelling means. A
    /// front end that asked for <c>seen=unwatched</c> gets <c>unwatched</c> back in the answer, so the difference
    /// between the two readings of a one-row list - a short shelf, or a filter nobody remembers - is a field
    /// rather than a guess.
    /// </remarks>
    private static string ReadSeen(string seen)
    {
      if (string.Equals(seen, "unwatched", StringComparison.OrdinalIgnoreCase))
        return "unwatched";
      if (string.Equals(seen, "watched", StringComparison.OrdinalIgnoreCase))
        return "watched";
      if (string.Equals(seen, "started", StringComparison.OrdinalIgnoreCase))
        return "started";
      return null;
    }

    private static bool IsDescending(string order)
    {
      return string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase)
          || string.Equals(order, "descending", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// What the database is asked to put in order, which for the two orders that follow the shape of a shelf is
    /// more than one column.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An episode list is sorted by season and then by episode, because a series is watched in that order and one
    /// column would interleave a season two after a season one. MP2's own screens sort an episode by its number
    /// (WifiRemote/Helper.cs:315), and an episode number is a collection - a double episode is one item carrying
    /// two numbers - which the database flattens into the same ordering a single number would get.
    /// </para>
    /// <para>
    /// The empty list means "in whatever order the database fancies", which is what <c>sort=none</c> promises.
    /// </para>
    /// </remarks>
    private static List<ISortInformation> BuildSorts(string sortBy, string order)
    {
      SortDirection direction = IsDescending(order) ? SortDirection.Descending : SortDirection.Ascending;
      List<ISortInformation> sorts = new List<ISortInformation>();
      switch (sortBy)
      {
        case "title":
          sorts.Add(new AttributeSortInformation(MediaAspect.ATTR_SORT_TITLE, direction));
          break;
        case "year":
          sorts.Add(new AttributeSortInformation(MediaAspect.ATTR_RECORDINGTIME, direction));
          break;
        case "lastplayed":
          // Newest viewing first, which is the order a "what was I watching" list wants. A title nobody ever
          // opened has nothing in that column; where it lands among them is the database's idea of sorting
          // nothing, not this endpoint's, and that is why a continue-watching list asks for watched titles.
          sorts.Add(new AttributeSortInformation(MediaAspect.ATTR_LASTPLAYED, direction));
          break;
        case "episode":
          sorts.Add(new AttributeSortInformation(EpisodeAspect.ATTR_SEASON, direction));
          sorts.Add(new AttributeSortInformation(EpisodeAspect.ATTR_EPISODE, direction));
          break;
        case "track":
          sorts.Add(new AttributeSortInformation(AudioAspect.ATTR_TRACK, direction));
          break;
      }
      return sorts;
    }

    /// <summary>
    /// The aspects that hold other titles, and so are worth reading when a request names a title to look inside.
    /// </summary>
    /// <remarks>
    /// Only these are asked about, because the question "what is this title?" has exactly these answers as far as
    /// the titles-in-a-title business goes: a series holds seasons and episodes, a season holds episodes, an album
    /// holds tracks and a film collection holds films. Asking for every aspect of the parent to answer it would be
    /// a wider read of the database than the question needs.
    /// </remarks>
    private static readonly Guid[] PARENT_ASPECTS =
    {
        SeriesAspect.ASPECT_ID,
        SeasonAspect.ASPECT_ID,
        AudioAlbumAspect.ASPECT_ID,
        MovieCollectionAspect.ASPECT_ID
    };

    /// <summary>
    /// Reads the one title a request wants to list titles inside of, so that the server can tell what it has been
    /// asked to open.
    /// </summary>
    /// <param name="mediaLibrary">The library to ask.</param>
    /// <param name="metadata">
    /// The aspects this server manages, which is what makes it safe to ask for the parent's: an aspect the database
    /// was never told about is left out of the request rather than making the whole search fail.
    /// </param>
    /// <param name="id">The media item id the request named.</param>
    /// <returns>The parent as the library has it, or <c>null</c> when there is no such title.</returns>
    private static MediaItem FindParentTitle(IMediaLibrary mediaLibrary,
        IDictionary<Guid, MediaItemAspectMetadata> metadata, Guid id)
    {
      List<Guid> optional = PARENT_ASPECTS.Where(aspectId => metadata.ContainsKey(aspectId)).ToList();
      MediaItemQuery query = new MediaItemQuery(new List<Guid> { MediaAspect.ASPECT_ID }, optional,
          new MediaItemIdFilter(id));
      query.Limit = 1;
      IList<MediaItem> found = mediaLibrary.Search(query, false, null, false);
      return found != null && found.Count > 0 ? found[0] : null;
    }

    /// <summary>
    /// Which pair of roles links a title to the titles of the kind being asked about, if the parent holds them at
    /// all.
    /// </summary>
    /// <param name="childAspectId">The aspect of the titles being listed, as the request resolved it.</param>
    /// <param name="parent">The title they are supposed to be inside.</param>
    /// <param name="role">The role on the child's side of the link.</param>
    /// <param name="linkedRole">The role on the parent's side of the link.</param>
    /// <returns>
    /// <c>true</c> when this kind of title is filed inside that kind of parent, in which case the two roles say
    /// how; <c>false</c> when the question does not make sense and the caller should say so.
    /// </returns>
    /// <remarks>
    /// The pairs are MP2's own, taken from the screens that walk these relationships for a living: an album's
    /// tracks are reached by <c>ROLE_TRACK</c> to <c>ROLE_ALBUM</c> (LocalAlbumFanartProvider.cs:79), a series'
    /// and a season's episodes by <c>ROLE_EPISODE</c> to <c>ROLE_SERIES</c> and <c>ROLE_SEASON</c>
    /// (LocalSeriesFanartProvider.cs:77,81), a collection's films by <c>ROLE_MOVIE</c> to
    /// <c>ROLE_MOVIE_COLLECTION</c> (LocalMovieCollectionFanartProvider.cs:75).
    /// <para>
    /// A season is accepted where its series is not, and the other way round: MP2 files both, and which one a
    /// given importer filled in depends on where the metadata came from. Asking after a series and finding a
    /// season holding the same episodes is not a mistake worth refusing a viewer for.
    /// </para>
    /// </remarks>
    private static bool TryRelationship(Guid childAspectId, MediaItem parent, out Guid role, out Guid linkedRole)
    {
      role = Guid.Empty;
      linkedRole = Guid.Empty;
      if (parent == null)
        return false;

      // The aspects this title actually carries. An entry with nothing in it is the database saying "that one was
      // asked for and there is none", so it does not count as the title having it - which is the difference
      // between answering a question about an album's tracks and refusing one because the id was once mentioned.
      HashSet<Guid> has = new HashSet<Guid>(parent.Aspects
          .Where(entry => entry.Value != null && entry.Value.Count > 0)
          .Select(entry => entry.Key));

      if (childAspectId == EpisodeAspect.ASPECT_ID)
      {
        if (has.Contains(SeriesAspect.ASPECT_ID))
        {
          role = EpisodeAspect.ROLE_EPISODE;
          linkedRole = SeriesAspect.ROLE_SERIES;
          return true;
        }
        if (has.Contains(SeasonAspect.ASPECT_ID))
        {
          role = EpisodeAspect.ROLE_EPISODE;
          linkedRole = SeasonAspect.ROLE_SEASON;
          return true;
        }
        return false;
      }

      if (childAspectId == AudioAspect.ASPECT_ID && has.Contains(AudioAlbumAspect.ASPECT_ID))
      {
        role = AudioAspect.ROLE_TRACK;
        linkedRole = AudioAlbumAspect.ROLE_ALBUM;
        return true;
      }

      if (childAspectId == MovieAspect.ASPECT_ID && has.Contains(MovieCollectionAspect.ASPECT_ID))
      {
        role = MovieAspect.ROLE_MOVIE;
        linkedRole = MovieCollectionAspect.ROLE_MOVIE_COLLECTION;
        return true;
      }

      return false;
    }

    /// <summary>
    /// Turns a media item into the row a title list shows.
    /// </summary>
    /// <param name="askedAsSomebody">
    /// Whether the request that produced this item named a user profile. It decides whether the row may carry a
    /// resume position at all: the figure belongs to a person, and reporting somebody's position to a request that
    /// named nobody would be showing one viewer another's place in the film.
    /// </param>
    private static TitleResult ToTitle(MediaItem item, bool askedAsSomebody)
    {
      TitleResult result = new TitleResult { Id = item.MediaItemId };

      SingleMediaItemAspect media;
      if (MediaItemAspect.TryGetAspect(item.Aspects, MediaAspect.Metadata, out media))
      {
        result.Title = media.GetAttributeValue<string>(MediaAspect.ATTR_TITLE);
        result.SortTitle = media.GetAttributeValue<string>(MediaAspect.ATTR_SORT_TITLE);
        DateTime recordingTime = media.GetAttributeValue<DateTime>(MediaAspect.ATTR_RECORDINGTIME);
        if (recordingTime != DateTime.MinValue)
          result.Year = recordingTime.Year;

        // Whether the viewer has seen this one, which they read off the row rather than off a panel opened per
        // title. Both numbers live on the Media aspect, which a title list asks for on every item anyway, so
        // saying them here costs no second read of the database.
        result.PlayCount = media.GetAttributeValue<int>(MediaAspect.ATTR_PLAYCOUNT);
        DateTime lastPlayed = media.GetAttributeValue<DateTime>(MediaAspect.ATTR_LASTPLAYED);
        if (lastPlayed != DateTime.MinValue)
          result.LastPlayed = lastPlayed;
      }

      // "<Unknown>" is what MediaItem.Title falls back to. Said as null here, because a client that shows the
      // string would be showing an internal fallback of the database layer as if it were the name of a film.
      if (result.Title == "<Unknown>")
        result.Title = null;

      SingleMediaItemAspect movie;
      if (MediaItemAspect.TryGetAspect(item.Aspects, MovieAspect.Metadata, out movie))
      {
        int runtime = movie.GetAttributeValue<int>(MovieAspect.ATTR_RUNTIME_M);
        if (runtime > 0)
          result.RuntimeMinutes = runtime;
      }

      // Where a title sits in the one that holds it, and who made it. Everything here comes off the aspect the
      // request was already about - a caller listing episodes asked for the episode aspect, one listing tracks
      // asked for the audio aspect - so a shelf of films pays nothing for a shelf of music having these fields.
      SingleMediaItemAspect episodeAspect;
      if (MediaItemAspect.TryGetAspect(item.Aspects, EpisodeAspect.Metadata, out episodeAspect))
      {
        int season = episodeAspect.GetAttributeValue<int>(EpisodeAspect.ATTR_SEASON);
        if (season > 0)
          result.Season = season;

        // The lowest number, as the details endpoint says it: an episode number is a collection in MP2 because a
        // file holding two episodes is one item carrying two numbers, and a row has room for one.
        IEnumerable<int> numbers = episodeAspect.GetCollectionAttribute<int>(EpisodeAspect.ATTR_EPISODE);
        if (numbers != null)
        {
          List<int> ordered = numbers.Where(number => number > 0).Distinct().OrderBy(number => number).ToList();
          if (ordered.Count > 0)
            result.Episode = ordered[0];
        }
      }

      SingleMediaItemAspect album;
      if (MediaItemAspect.TryGetAspect(item.Aspects, AudioAlbumAspect.Metadata, out album))
      {
        result.Artist = Text(FirstText(album.GetCollectionAttribute<string>(AudioAlbumAspect.ATTR_ARTISTS)));
        int tracks = album.GetAttributeValue<int>(AudioAlbumAspect.ATTR_NUMTRACKS);
        if (tracks > 0)
          result.TrackCount = tracks;
      }

      SingleMediaItemAspect audio;
      if (MediaItemAspect.TryGetAspect(item.Aspects, AudioAspect.Metadata, out audio))
      {
        int track = audio.GetAttributeValue<int>(AudioAspect.ATTR_TRACK);
        if (track > 0)
          result.Track = track;

        // A track is credited to whoever made the album it came from unless the file itself says otherwise, which
        // is what a various-artists compilation looks like: the album has one name on it and the track has another.
        if (string.IsNullOrEmpty(result.Artist))
          result.Artist = Text(FirstText(audio.GetCollectionAttribute<string>(AudioAspect.ATTR_ARTISTS)));

        // A track's length is on its own aspect, in seconds, because a three-and-a-half-minute song is not a
        // rounding error away from nothing the way a film is: it is three minutes of the four the row will say.
        if (!result.RuntimeMinutes.HasValue)
        {
          long seconds = audio.GetAttributeValue<long>(AudioAspect.ATTR_DURATION);
          if (seconds >= 60)
            result.RuntimeMinutes = (int)(seconds / 60);
        }
      }

      result.Stub = item.IsStub;

      // How far the caller got, if the library was told who the caller is. Read off the item rather than asked
      // for separately: the search already carried this person's user data along with the row when the request
      // named them, and asking per title would be one database round trip per row of a list.
      result.Resume = askedAsSomebody ? ReadResume(item) : null;

      result.Path = ReadPath(item);
      return result;
    }

    /// <summary>
    /// The caller's stored position in this title as a percentage from 1 to 99, or null when there is none to
    /// offer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MP2 stores it as <see cref="UserDataKeysKnown.KEY_PLAY_PERCENTAGE"/> in the caller's user data, written by
    /// <see cref="IMediaLibrary.NotifyUserPlayback(Guid, Guid, int, bool)"/> as a fixed-width string
    /// (<c>GetSortablePlayPercentageString</c>, UserDataKeysKnown.cs:62: <c>"042"</c>) so that the database sorts
    /// those rows in numeric order. Reading it back is a parse of that text, not a number: the column is a string
    /// and an unparsable one means the library holds something this endpoint will not guess a meaning for.
    /// </para>
    /// <para>
    /// Nought and a hundred both come back as null, and for different reasons worth stating: nought is "opened
    /// and abandoned at the very start", where starting again is the same thing, and a hundred is finished. What
    /// is left is exactly MP2's own continue-watch interval, and it is the interval a screen can act on by
    /// seeking.
    /// </para>
    /// </remarks>
    private static int? ReadResume(MediaItem item)
    {
      if (item == null || item.UserData == null)
        return null;

      string stored;
      if (!item.UserData.TryGetValue(UserDataKeysKnown.KEY_PLAY_PERCENTAGE, out stored))
        return null;

      int percentage;
      if (!int.TryParse(stored, NumberStyles.Integer, CultureInfo.InvariantCulture, out percentage))
        return null;

      if (percentage <= 0 || percentage >= 100)
        return null;

      return percentage;
    }

    /// <summary>
    /// The item's primary resource as a serialized path, or null when there is none to name.
    /// </summary>
    /// <remarks>
    /// The same route the local UI takes, so the path means the same thing to both: the primary resource the
    /// importer recorded, through the provider that found it.
    /// <para>
    /// The call is guarded, which the UI has no reason to do and a list endpoint does: resolving goes through the
    /// item's editions, and an item whose recorded primary resource index matches no resource of its own makes
    /// that lookup throw. In the UI one such item breaks the one playback the viewer tried; here it would take
    /// the whole page of titles with it, including the ninety-nine that are fine. A title without a path is
    /// already a case this answer describes, so it is the answer for a broken one too.
    /// </para>
    /// </remarks>
    private static string ReadPath(MediaItem item)
    {
      try
      {
        IResourceLocator locator = item.GetResourceLocator();
        ResourcePath path = locator?.NativeResourcePath;
        return path?.Serialize();
      }
      catch (Exception ex)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Warn("MediaController: title {0} ({1}) has no readable primary resource: {2}",
              item.MediaItemId, ex.GetType().Name, ex.Message);
        return null;
      }
    }

    /// <summary>
    /// The answer for a media library that cannot answer yet.
    /// </summary>
    /// <remarks>
    /// Same shape as the one <see cref="LibraryController"/> gives, so a client has one rule for "ask again
    /// later" instead of one per endpoint. The resource server starts before the media library does, so this is
    /// the normal answer to a request that came in too early, not an error.
    /// </remarks>
    private static ObjectResult NotReady(IllegalCallException exception)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>(false);
      if (logger != null)
        logger.Info("MediaController: the media library refused a request, answering 503: {0}", exception.Message);
      return new ObjectResult(new TitlesResult { Error = exception.Message })
      {
        StatusCode = StatusCodes.Status503ServiceUnavailable
      };
    }
  }
}