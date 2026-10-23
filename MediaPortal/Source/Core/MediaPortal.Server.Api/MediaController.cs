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
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Certifications;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Settings;
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
    /// Whether the titles the library invented - a film of a collection nobody has, an episode of a series whose
    /// rest is still missing - belong in the answers this endpoint gives.
    /// </summary>
    /// <remarks>
    /// Read from the settings on every request rather than remembered, because the answer is a thing a household
    /// changes from a television and expects the next shelf to know about. A server without a settings manager at
    /// all - a test, a tool - is a server where nobody ever answered the question, which is what a fresh
    /// <see cref="VirtualMediaSettings"/> says by itself.
    /// </remarks>
    private static VirtualMediaSettings VirtualTitles
    {
      get
      {
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>(false);
        if (settingsManager == null)
          return new VirtualMediaSettings();

        // A manager that answers nothing for a class nobody registered is a server with no wish about invented
        // titles, which is the same answer as the switches in their default: none of them.
        return settingsManager.Load<VirtualMediaSettings>() ?? new VirtualMediaSettings();
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

      /// <summary>
      /// The category the answer was narrowed to - <c>genre</c>, <c>year</c>, <c>actors</c>, <c>directors</c> or
      /// <c>writers</c> - or null when it was not narrowed by one. Echoed because a genre list that came back empty
      /// has to be able to say which genre it was asked for.
      /// </summary>
      public string By { get; set; }

      /// <summary>
      /// The value of <see cref="By"/> this answer holds only, or null when nothing was picked out. Echoed so that
      /// a screen can show whose films it is looking at even before a single title arrives.
      /// </summary>
      public string Value { get; set; }

      public List<TitleResult> Titles { get; set; }

      /// <summary>
      /// Set when the answer is empty for a reason the caller can act on.
      /// </summary>
      public string Error { get; set; }
    }

    /// <summary>
    /// Lists the titles carrying the given aspect.
    /// </summary>
    /// <param name="aspect">Aspect name as <c>GET api/library/aspects</c> reports it, e.g. <c>MovieItem</c>.</param>
    /// <param name="sort">
    /// <c>title</c> (default), <c>year</c>, <c>added</c> for when the importer first saw the file,
    /// <c>lastplayed</c>, <c>episode</c>, <c>track</c>, or <c>none</c> for whatever order the database returns.
    /// </param>
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
    /// <param name="by">
    /// One of the categories of <c>GET api/media/groups</c> - <c>genre</c>, <c>year</c>, <c>actors</c>,
    /// <c>directors</c>, <c>writers</c> - to keep only the titles carrying <paramref name="value"/> under it.
    /// Together with the value it answers the second half of browsing: the groups say which genres there are,
    /// this says which films are in one of them.
    /// </param>
    /// <param name="value">
    /// The value to match: a genre, an actor's name as the importer wrote it, or - under <c>by=year</c> - a year,
    /// which holds every title whose recording time falls inside that year rather than a title recorded at the
    /// stroke of one.
    /// </param>
    [HttpGet("titles")]
    public ActionResult<TitlesResult> GetTitles([FromQuery] string aspect, [FromQuery] string sort,
        [FromQuery] string order, [FromQuery] int limit, [FromQuery] int offset, [FromQuery] string search,
        [FromQuery] string seen, [FromQuery] Guid inside = default(Guid),
        [FromQuery] string by = null, [FromQuery] string value = null)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      if (string.IsNullOrEmpty(aspect))
        return BadRequest(new TitlesResult { Error = "an aspect has to be named, e.g. ?aspect=MovieItem" });

      int take = limit <= 0 ? DEFAULT_LIMIT : Math.Min(limit, MAX_LIMIT);
      int skip = Math.Max(offset, 0);

      try
      {
        IDictionary<Guid, MediaItemAspectMetadata> metadata = mediaLibrary.GetManagedMediaItemAspectMetadata();
        KeyValuePair<Guid, MediaItemAspectMetadata> match = metadata.FirstOrDefault(entry =>
            entry.Value != null && string.Equals(entry.Value.Name, aspect, StringComparison.OrdinalIgnoreCase));
        if (match.Value == null)
          // The names are worth reading here: what a shelf asks for is an aspect ("MovieItem"), while what a share
          // is filed under is a media category ("Movie"), and the two lists are similar enough that mixing them up
          // is the mistake every front end makes once. Naming the ones this server has turns a search through the
          // source into one look at the answer.
          return NotFound(new TitlesResult
          {
            Error = "this server has no aspect called '" + aspect + "'; it has " + string.Join(", ",
                metadata.Where(entry => entry.Value != null)
                    .Select(entry => entry.Value.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)) +
                " - see GET api/library/aspects"
          });

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

        // The browse screen's half of the bargain: <c>GET api/media/groups</c> says which genres, years and names a
        // shelf holds, and a picked one of those comes back here as <c>by</c> plus <c>value</c> - "the films whose
        // genre is Action", "the films recorded in 1998". Unlike an unknown sort, an unknown category is refused
        // rather than ignored: silently dropping the filter would hand a viewer the whole shelf under a heading
        // that names one genre, which is an answer that looks right and is wrong.
        string category = string.IsNullOrWhiteSpace(by) ? null : by.Trim();
        string picked = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (category != null && picked == null)
          return BadRequest(new TitlesResult
          {
            Error = "'" + category + "' has to come with the value to pick out, e.g. ?by=genre&value=Action"
          });

        IFilter groupFilter = null;
        string askedBy = null;
        string askedValue = null;
        if (category != null)
        {
          string known;
          MediaItemAspectMetadata.AttributeSpecification spec;
          if (!TryCategory(category, out known, out spec))
            return BadRequest(new TitlesResult
            {
              By = category,
              Error = "this endpoint can pick titles out by genre, year, actors, directors and writers; '" +
                  category + "' is not one of them - see GET api/media/groups"
            });
          askedBy = known;
          askedValue = picked;

          if (!known.Equals("year", StringComparison.OrdinalIgnoreCase))
          {
            groupFilter = new RelationalFilter(spec, RelationalOperator.EQ, picked);
          }
          else
          {
            // A year is not a value that stands in the database - what stands there is a date and time - so the
            // year is asked for as the twelve months it covers. Equality against a made-up midnight of the first
            // of January would lose every film whose recording time carries an hour.
            int year;
            if (!int.TryParse(picked, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out year)
                || year < 1 || year > 9999)
              return BadRequest(new TitlesResult
              {
                By = known,
                Value = picked,
                Error = "'" + picked + "' is not a year; the year has to be a number from 1 to 9999"
              });
            IFilter since = new RelationalFilter(MediaAspect.ATTR_RECORDINGTIME, RelationalOperator.GE,
                new DateTime(year, 1, 1));
            // The 9999th year has no January the first of the next one to stop at - no date after it fits the
            // calendar this runs on - so its last year is bounded from that January onwards alone.
            groupFilter = year == 9999
                ? since
                : BooleanCombinationFilter.CombineFilters(BooleanOperator.And, since,
                    new RelationalFilter(MediaAspect.ATTR_RECORDINGTIME, RelationalOperator.LT,
                        new DateTime(year + 1, 1, 1)));
          }

          asking = asking == null
              ? groupFilter
              : BooleanCombinationFilter.CombineFilters(BooleanOperator.And, asking, groupFilter);
        }

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

        // Whether the titles the library invented take part is the household's answer, not this endpoint's
        // opinion, and it is asked of the aspects this browse is about - the same reading the reference used for
        // the same switch. Off, which is what every installation runs with, this is the same call it always was.
        bool invented = VirtualTitles.ShowsVirtualFor(new[] { aspectId });
        IList<MediaItem> items = mediaLibrary.Search(query, false, profile, invented);

        List<TitleResult> titles = items.Select(item => ToTitle(item, profile.HasValue)).ToList();

        // "how many are there" is asked through the same filter the list was read through, or a front end that
        // reached the bottom of a search would keep offering more of a list that has no more in it - and would say
        // "12 of 4 000" about a search that found twelve titles out of four thousand. The same answer about
        // invented titles goes in, because a count of a different shelf than the one being listed is worse than
        // no count at all.
        int total = mediaLibrary.CountMediaItems(new[] { aspectId }, query.Filter, false, invented);

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
          By = askedBy,
          Value = askedValue,
          Titles = titles
        };
      }
      catch (IllegalCallException ex)
      {
        return NotReady(ex);
      }
    }

    /// <summary>
    /// One value a shelf can be counted under, and how many titles of the shelf carry it.
    /// </summary>
    public class GroupResult
    {
      /// <summary>The value in words: a genre, a year, a name as the importer wrote it.</summary>
      public string Value { get; set; }

      /// <summary>
      /// How many titles of the asked-for aspect carry it - counted by the database, which is the only party
      /// that has ever seen all of them.
      /// </summary>
      public int Count { get; set; }
    }

    /// <summary>
    /// The answer of <c>GET api/media/groups</c>.
    /// </summary>
    public class GroupsResult
    {
      public string Aspect { get; set; }

      public Guid? AspectId { get; set; }

      /// <summary>
      /// Which question was answered, echoed the way <c>sort</c> is echoed on a title list.
      /// </summary>
      public string By { get; set; }

      public List<GroupResult> Groups { get; set; }

      /// <summary>
      /// Set when the answer is empty for a reason the caller can act on.
      /// </summary>
      public string Error { get; set; }
    }

    /// <summary>
    /// Counts the titles of one aspect under each value of one attribute - the question a browse-by-category
    /// screen asks.
    /// </summary>
    /// <param name="aspect">
    /// The shelf to count, named exactly as <c>GET api/media/titles</c> names it. Titles that do not carry it are
    /// not counted, so <c>aspect=MovieItem</c> counts genres as films have them and leaves the albums' out.
    /// </param>
    /// <param name="by">
    /// <c>genre</c>, <c>year</c>, <c>actors</c>, <c>directors</c> or <c>writers</c> - the categories MP2's own
    /// browse tree was built from. An unknown word is refused rather than guessed at: a browse screen that
    /// silently counted the wrong thing would sort the library into categories nobody asked for, and the list of
    /// what does work is in the refusal.
    /// </param>
    /// <remarks>
    /// <para>
    /// The counting happens in the database - <c>SELECT value, COUNT(*) ... GROUP BY value</c> - because a page
    /// is not a census: reading the whole shelf into a front end to count ten names by hand is the same mistake
    /// an unpaginated title list would be, and the database does not get tired. The answer is the groups, biggest
    /// first and then by name, because a browse screen wants the categories the library actually lives in at the
    /// top and needs the rest findable anyway.
    /// </para>
    /// <para>
    /// The year counts each title under the year of its recording time - the same figure the <c>year</c> on a
    /// title row is taken from - and a title without one belongs to no year and to no group; "unknown" would be a
    /// category nobody came to the screen to browse. Genres and the names are what the importer wrote on the
    /// title (<c>GenreAspect</c> rows and the credit lists on <c>VideoAspect</c>), which is the same source the
    /// details of a title read from; a library imported from bare files without NFOs will answer with few groups,
    /// truthfully.
    /// </para>
    /// </remarks>
    [HttpGet("groups")]
    public ActionResult<GroupsResult> GetGroups([FromQuery] string aspect, [FromQuery] string by)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      if (string.IsNullOrEmpty(aspect))
        return BadRequest(new GroupsResult { Error = "an aspect has to be named, e.g. ?aspect=MovieItem" });

      string known;
      MediaItemAspectMetadata.AttributeSpecification spec;
      ProjectionFunction projection;
      if (!TryCategory(by, out known, out spec, out projection))
        return BadRequest(new GroupsResult
        {
          By = by,
          Error = "the library can be counted by genre, year, actors, directors and writers; '" + by +
              "' is not one of them"
        });

      try
      {
        IDictionary<Guid, MediaItemAspectMetadata> metadata = mediaLibrary.GetManagedMediaItemAspectMetadata();
        KeyValuePair<Guid, MediaItemAspectMetadata> match = metadata.FirstOrDefault(entry =>
            entry.Value != null && string.Equals(entry.Value.Name, aspect, StringComparison.OrdinalIgnoreCase));
        if (match.Value == null)
          return NotFound(new GroupsResult
          {
            By = known,
            Error = "this server has no aspect called '" + aspect + "'; it has " + string.Join(", ",
                metadata.Where(entry => entry.Value != null)
                    .Select(entry => entry.Value.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)) +
                " - see GET api/library/aspects"
          });

        Guid aspectId = match.Key;

        // The aspect holding the values has to take part in the query or its column is not there to group on,
        // and the shelf aspect has to take part too or the count would run over every title that carries the
        // attribute - every film, episode and recording at once, under whichever heading was asked for.
        List<Guid> necessary = new List<Guid> { aspectId };
        Guid holding = spec.ParentMIAM != null ? spec.ParentMIAM.AspectId : Guid.Empty;
        if (holding != Guid.Empty && !necessary.Contains(holding))
          necessary.Add(holding);

        HomogenousMap counted = mediaLibrary.GetValueGroups(spec, null, projection, necessary, null, false,
            VirtualTitles.ShowsVirtualFor(necessary));
        if (counted == null)
          // Null is the library's way of saying it is in maintenance mode and counted nothing; an empty answer
          // here would read as "no title carries a genre" rather than as "ask again later".
          return StatusCode(StatusCodes.Status503ServiceUnavailable);

        List<GroupResult> groups = new List<GroupResult>();
        foreach (KeyValuePair<object, object> entry in counted)
        {
          // The database counts "nothing written here" as a group of its own; it is not a category a viewer
          // came to the screen to browse, and it arrives here under HomogenousMap's own marker for the absent.
          if (entry.Key == null || entry.Key.Equals(HomogenousMap.NULL) || entry.Value == null)
            continue;
          string named = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
          if (string.IsNullOrWhiteSpace(named))
            continue;
          groups.Add(new GroupResult
          {
            Value = named,
            Count = Convert.ToInt32(entry.Value, CultureInfo.InvariantCulture)
          });
        }

        groups.Sort((a, b) => b.Count != a.Count
            ? b.Count.CompareTo(a.Count)
            : string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));

        return new GroupsResult
        {
          Aspect = match.Value.Name,
          AspectId = aspectId,
          By = known,
          Groups = groups
        };
      }
      catch (IllegalCallException ex)
      {
        return NotReady(ex);
      }
    }

    /// <summary>
    /// One of the categories a shelf can be counted by, and one of the questions a picked value comes back to
    /// <see cref="GetTitles"/> as.
    /// </summary>
    private class Category
    {
      public Category(string word, MediaItemAspectMetadata.AttributeSpecification attribute,
          ProjectionFunction projection)
      {
        Word = word;
        Attribute = attribute;
        Projection = projection;
      }

      public string Word { get; private set; }

      public MediaItemAspectMetadata.AttributeSpecification Attribute { get; private set; }

      public ProjectionFunction Projection { get; private set; }
    }

    /// <summary>
    /// The five categories MP2's own browse tree was built from, in the order a browse screen lists them.
    /// </summary>
    /// <remarks>
    /// These are the attributes the importer writes <i>on the title itself</i> - genre rows on the
    /// <c>GenreAspect</c> and the credit name lists on the <c>VideoAspect</c>, the same source the details screen
    /// reads - so grouping them costs one query per question rather than the relationship walk the people and
    /// genre <i>items</i> would need. The year is not a stored column anywhere; it is taken from the recording
    /// time in the database itself, which is why it carries a projection and why the titles of one year are
    /// later asked for as a span of dates rather than an equality.
    /// </remarks>
    private static readonly Category[] CATEGORIES =
    {
        new Category("genre", GenreAspect.ATTR_GENRE, ProjectionFunction.None),
        new Category("year", MediaAspect.ATTR_RECORDINGTIME, ProjectionFunction.DateToYear),
        new Category("actors", VideoAspect.ATTR_ACTORS, ProjectionFunction.None),
        new Category("directors", VideoAspect.ATTR_DIRECTORS, ProjectionFunction.None),
        new Category("writers", VideoAspect.ATTR_WRITERS, ProjectionFunction.None)
    };

    /// <summary>
    /// Reads a category word into the attribute it names and how the database has to treat it. Null for a word
    /// this endpoint does not know, which both endpoints turn into the same refusal.
    /// </summary>
    private static bool TryCategory(string by, out string word,
        out MediaItemAspectMetadata.AttributeSpecification attribute, out ProjectionFunction projection)
    {
      Category found = CATEGORIES.FirstOrDefault(entry =>
          string.Equals(entry.Word, by, StringComparison.OrdinalIgnoreCase));
      word = found != null ? found.Word : null;
      attribute = found != null ? found.Attribute : null;
      projection = found != null ? found.Projection : ProjectionFunction.None;
      return found != null;
    }

    /// <summary>
    /// The two-answer form of <see cref="TryCategory(string, out string, out MediaItemAspectMetadata.AttributeSpecification, out ProjectionFunction)"/>,
    /// for <see cref="GetTitles"/>, which asks the database for a filter rather than a projection and so has no
    /// use for the third answer.
    /// </summary>
    private static bool TryCategory(string by, out string word,
        out MediaItemAspectMetadata.AttributeSpecification attribute)
    {
      ProjectionFunction unused;
      return TryCategory(by, out word, out attribute, out unused);
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

      /// <summary>
      /// When the title first ran on television or in cinemas, when the library or a metadata file next to the
      /// media names a date; null when nobody did. Distinct from <see cref="Year"/>, which is the import date's
      /// year and an approximation of this.
      /// </summary>
      public DateTime? FirstAired { get; set; }

      public int? RuntimeMinutes { get; set; }

      /// <summary>
      /// The genres the title carries, from the metadata file that lies next to the media - the television show's
      /// own file for a series and its episodes. Null when no such file names any.
      /// </summary>
      public List<string> Genres { get; set; }

      /// <summary>
      /// The rating a metadata file gives the title, on the hundred-point scale the sites rate with, divided by
      /// ten. <see cref="Rating"/> when the library itself rated the title wins; this is the tool's number for a
      /// library that never rated, and it keeps the decimal - "7.3" is what the tagging site said.
      /// </summary>
      public double? TotalRating { get; set; }

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
    /// <b>What is not in the answer, on purpose:</b> people. MP2 does not store them on the title; the importer
    /// files them as separate items linked by relationships (<c>PersonAspect</c>), so naming them here would mean
    /// a relationship query per title and an answer that looks complete while reporting the ids of other items.
    /// Genres are named when a metadata file next to the media holds them (see
    /// <see cref="FillMissingDetailsFromFiles"/>); what the title's own aspects carry always wins.
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
          EpisodeAspect.ASPECT_ID,
          // Asked for here because the metadata search below detours from a series or a season to one of its
          // episodes, and it can only know it is talking to one of those when the answer carries the aspect.
          SeriesAspect.ASPECT_ID,
          SeasonAspect.ASPECT_ID
        };

        MediaItemQuery query = new MediaItemQuery(necessary, optional, new MediaItemIdFilter(id));
        Guid? profile = CallerProfile;

        // Looked up by its id, so this is not a browse that could be about one kind of media or another: the
        // title is already chosen, and a household that lets the library's invented titles onto its shelves
        // expects to be able to stand in front of one. Refusing to read it here would show it and then deny it.
        IList<MediaItem> items = mediaLibrary.Search(query, false, profile, VirtualTitles.LetsVirtualTitlesIn);
        MediaItem item = items == null ? null : items.FirstOrDefault();

        if (item == null)
          return NotFound(new ItemResult { Id = id, Error = "this server has no title with the id " + id });

        ItemResult result = ToItem(item, profile.HasValue);
        FillMissingDetailsFromFiles(item, result);

        // Translated last, because the file beside a title can be the only place its certificate is written down,
        // and a household that asked for its own country's mark should get it whichever of the two sources spoke.
        result.Certification = ShownCertification(item, result.Certification);
        return result;
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
        {
          result.Year = recordingTime.Year;
          // The reference draws this date as the episode's first aired date, and an importer that knew the air
          // date stored it here; a file's import timestamp is not that, but it is what the library holds.
          result.FirstAired = recordingTime;
        }
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

      // A show keeps its certificate on the show and not on the episode, and an importer that knew the one
      // usually knew the other. Asked for after the film's, because a title is either a film or an episode and
      // never both, and a certificate that is already there needs no second opinion.
      SingleMediaItemAspect series;
      if (result.Certification == null
          && MediaItemAspect.TryGetAspect(item.Aspects, SeriesAspect.Metadata, out series))
        result.Certification = Text(series.GetAttributeValue<string>(SeriesAspect.ATTR_CERTIFICATION));

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

    #region The picture of a title

    /// <summary>
    /// How long a front end may keep a picture it fetched, in seconds.
    /// </summary>
    /// <remarks>
    /// A day. The address names the title rather than the picture, so a reimport that swaps a poster would go
    /// unseen for as long as a client believed the URL; a day is short enough that a viewer who replaces a poster
    /// sees it after restarting the television, and long enough that walking a shelf of fifty films does not fetch
    /// the same fifty pictures again on every visit.
    /// </remarks>
    public const int THUMBNAIL_CACHE_SECONDS = 86400;

    /// <summary>
    /// The picture a front end draws next to a title: the bytes MP2's own importers store in the database.
    /// </summary>
    /// <param name="id">The media item id, as <c>GET api/media/titles</c> reported it.</param>
    /// <remarks>
    /// <para>
    /// <b>Where the bytes come from.</b> MP2 has never kept a poster in a file next to the film and served the
    /// file; the importers and online metadata providers write the picture into the media item itself, as the
    /// <see cref="ThumbnailLargeAspect.ATTR_THUMBNAIL"/> blob, and the old skin engine read exactly that -
    /// <c>ImageSourceFactory.CreateMediaItemThumbnailAspectSource</c> (ImageSourceFactory.cs:123) pulls the byte
    /// array out of the item's aspects and hands it to a texture, cached by media item id. This endpoint serves
    /// the same bytes over HTTP, which is what a front end on another machine needs, and nothing else: no
    /// thumbnail is generated here, no picture is downloaded from the internet here, and a title the library has
    /// no picture for is not given one.
    /// </para>
    /// <para>
    /// <b>A 404 says two things and that is deliberate.</b> The query asks for the thumbnail aspect as a
    /// <i>necessary</i> aspect, so it finds nothing both when no title has that id and when the title exists but
    /// nobody ever gave it a picture. Telling those apart would cost a second query for information a screen
    /// cannot use: in both cases the picture is not there, the placeholder is drawn, and the title next to it is
    /// still listed and still playable. The body says so in words rather than pretending to know.
    /// </para>
    /// <para>
    /// <b>Why the caller's profile is not passed to the query.</b> The profile parameter of
    /// <see cref="IMediaLibrary.Search"/> exists so that the library can attach one person's user data - their
    /// position in a title, their rating of it. A picture is nobody's: it is the same poster for everybody in the
    /// household, so joining a user data table onto it would make two people's requests fetch different amounts of
    /// database to return the same bytes.
    /// </para>
    /// <para>
    /// An empty blob is treated as no picture. The database can hold one - a provider that found nothing and wrote
    /// an array of zero length rather than nothing - and a front end handed zero bytes gets a decode failure where
    /// it should have got a placeholder.
    /// </para>
    /// </remarks>
    [HttpGet("items/{id}/thumbnail")]
    public IActionResult GetThumbnail(Guid id)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      try
      {
        List<Guid> necessary = new List<Guid> { ThumbnailLargeAspect.ASPECT_ID };
        MediaItemQuery query = new MediaItemQuery(necessary, null, new MediaItemIdFilter(id));

        // The picture of a title somebody is looking at is asked for by that title's id, whatever the title
        // turned out to be - see the details endpoint for why an invented title is looked up like any other.
        IList<MediaItem> items = mediaLibrary.Search(query, false, null, VirtualTitles.LetsVirtualTitlesIn);
        MediaItem item = items == null ? null : items.FirstOrDefault();

        byte[] picture = null;
        SingleMediaItemAspect thumbnail;
        if (item != null &&
            MediaItemAspect.TryGetAspect(item.Aspects, ThumbnailLargeAspect.Metadata, out thumbnail))
          picture = thumbnail.GetAttributeValue<byte[]>(ThumbnailLargeAspect.ATTR_THUMBNAIL);

        if (picture == null || picture.Length == 0)
        {
          return NotFound(new ItemResult
          {
            Id = id,
            Error = "this server has no picture for the id " + id
          });
        }

        Response.Headers["Cache-Control"] = "private, max-age=" + THUMBNAIL_CACHE_SECONDS;
        return File(picture, PictureMediaType(picture));
      }
      catch (IllegalCallException ex)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Info("MediaController: the media library refused a picture request, answering 503: {0}",
              ex.Message);
        return StatusCode(StatusCodes.Status503ServiceUnavailable);
      }
    }

    /// <summary>
    /// The media item fanart the front end draws behind a browser shelf: the fanart cache's poster/fanart bundle
    /// for one title, or nothing when the item has no fanart and the skin asks for it. The file lives in the server's
    /// fanart cache, just like the thumbnail lives in the media item's own blob. A missing file is a normal state,
    /// not a failure: the front end already draws the placeholder when the bytes are absent.
    /// </summary>
    [HttpGet("items/{id}/fanart")]
    public IActionResult GetFanArt(Guid id)
    {
      IFanArtCache cache = ServiceRegistration.Get<IFanArtCache>(false);
      if (cache == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      IList<string> files = cache.GetFanArtFiles(id, FanArtTypes.Poster);
      if (files == null || files.Count == 0)
        files = cache.GetFanArtFiles(id, FanArtTypes.FanArt);
      if (files == null || files.Count == 0)
        files = AlbumFanArtFiles(id, FanArtTypes.Poster, FanArtTypes.Cover, FanArtTypes.FanArt);
      if (files == null || files.Count == 0)
        return NotFound(new ItemResult { Id = id, Error = "this server has no fanart for the id " + id });

      string path = files.First();
      byte[] picture = System.IO.File.ReadAllBytes(path);
      Response.Headers["Cache-Control"] = "private, max-age=" + THUMBNAIL_CACHE_SECONDS;
      return File(picture, PictureMediaType(picture));
    }

    /// <summary>
    /// One named kind of fanart for one title: the banner a banner tile wears, the backdrop a screen paints
    /// behind itself, the screenshot a thumbnail tile draws - as opposed to the poster of
    /// <see cref="GetThumbnail"/>, which fits none of those frames.
    /// </summary>
    /// <param name="id">The media item id, as <c>GET api/media/titles</c> reported it.</param>
    /// <param name="type">
    /// The kind asked for, in the reference's own words: <c>Poster</c>, <c>Banner</c>, <c>FanArt</c>,
    /// <c>Thumbnail</c>, <c>Logo</c>, <c>ClearArt</c>, <c>DiscArt</c> or <c>Cover</c> - the last being what
    /// music calls its front picture - whichever case the caller likes. Anything else is answered with 400
    /// rather than a guess, because a tile that silently drew a poster when somebody asked for a banner is the
    /// exact lie this endpoint exists to stop.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>Where the bytes come from - in the reference's order.</b> First the fanart cache, which is where MP2's
    /// metadata extractors store what they downloaded online. Then - and this is what the old client's
    /// <c>FanArtService.Local</c> providers did and what makes a library that was tagged by an outside tool like
    /// TinyMediaManager just work - the pictures lying <em>next to the media itself</em>: <c>banner.jpg</c>
    /// beside the file, <c>fanart.jpg</c> next to the episodes' season folder, and the names the reference's
    /// <see cref="LocalFanartHelper"/> spells for each kind. The search walks up from the title's own folder to
    /// its parent and grandparent, because an episode's banner lives with its series, one or two folders up.
    /// </para>
    /// <para>
    /// A kind the library has nothing for is a 404, and the front end draws the skin's placeholder - the same
    /// "no banner" layer the reference drew under <c>!HasImage</c>. It is never answered with another kind's
    /// picture: stretching a poster into a banner frame is what this endpoint was added to end.
    /// </para>
    /// </remarks>
    [HttpGet("items/{id}/fanart/{type}")]
    public IActionResult GetFanArtOfType(Guid id, string type)
    {
      string fanArtType = KnownFanArtType(type);
      if (fanArtType == null)
        return BadRequest(new ItemResult
        {
          Id = id,
          Error = "'" + type + "' is not a kind of fanart; ask for " + string.Join(", ", FANART_KINDS)
        });

      try
      {
        IFanArtCache cache = ServiceRegistration.Get<IFanArtCache>(false);
        IList<string> cached = cache?.GetFanArtFiles(id, fanArtType);
        byte[] picture = FirstReadablePicture(cached);

        // A track wears its album's cover, and the import stored that cover once, under the album's id. Only the
        // two names for a front picture make the detour: a track asked for a banner stays a 404, because
        // stretching an album's square into a banner frame is the lie this endpoint was added to prevent.
        //
        // It is asked before the folder is walked, and that order is the whole point: the track lives in the
        // album's folder, so walking it would answer a track with whatever <c>folder.jpg</c> happens to lie there
        // while the album itself answers with the picture its files carry. Two tiles of one album would then wear
        // two different pictures. The cache is where the chosen picture lies, for an album as for its tracks.
        if (picture == null &&
            (fanArtType == FanArtTypes.Cover || fanArtType == FanArtTypes.Poster))
          picture = FirstReadablePicture(AlbumFanArtFiles(id, fanArtType, FanArtTypes.Cover));

        // Music calls its front picture a cover, but the files a library holds it in are called
        // "folder.jpg" and "cover.jpg", which are the names the reference lists for a poster - which is also
        // why the reference renames a poster to a cover as soon as it knows the item is an album.
        string folderKind = fanArtType == FanArtTypes.Cover ? FanArtTypes.Poster : fanArtType;
        if (picture == null)
          picture = LocalFanArtSearch.Worn.Picture(id, folderKind);

        if (picture == null || picture.Length == 0)
        {
          return NotFound(new ItemResult
          {
            Id = id,
            Error = "this server has no " + fanArtType.ToLowerInvariant() + " for the id " + id
          });
        }

        Response.Headers["Cache-Control"] = "private, max-age=" + THUMBNAIL_CACHE_SECONDS;
        return File(picture, PictureMediaType(picture));
      }
      catch (IllegalCallException ex)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Info("MediaController: the media library refused a fanart request, answering 503: {0}", ex.Message);
        return StatusCode(StatusCodes.Status503ServiceUnavailable);
      }
    }

    /// <summary>
    /// Telling the server to learn where the pictures of its library lie, so that the first look at a wall costs
    /// file opens and no searching. See <see cref="FanArtWarmingPass"/> for what such a pass does and what it costs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The answer is the pass' state either way: <c>202</c> when this call started one, <c>200</c> when one was
    /// already running - which is not a failure, because the work is queued either way and whoever asked last has
    /// nothing to wait for. A pass that is asked for while the importer still has jobs waits for it, so a front
    /// end may press this the moment the queue drains without racing the import.
    /// </para>
    /// <para>
    /// Nothing here has to be pressed for the server to be quick: a picture asked for is answered and remembered
    /// as always. The pass only moves that work to a moment when nobody is waiting for it.
    /// </para>
    /// </remarks>
    [HttpPost("fanart/warming")]
    public ActionResult<FanArtWarmingResult> WarmFanArt()
    {
      bool started = FanArtWarmingPass.Worn.StartAfterImport();
      FanArtWarmingResult state = FanArtWarmingPass.Worn.Read();
      return started ? Accepted(state) : (ActionResult<FanArtWarmingResult>) Ok(state);
    }

    /// <summary>
    /// How the server is doing at learning where its pictures lie: whether a pass runs, how far it got, and how
    /// many answers it is holding.
    /// </summary>
    [HttpGet("fanart/warming")]
    public FanArtWarmingResult GetFanArtWarming()
    {
      return FanArtWarmingPass.Worn.Read();
    }

    /// <summary>
    /// The kinds of fanart this endpoint answers for, in the reference's words.
    /// </summary>
    /// <remarks>
    /// The seven the reference's folder search names are the ones a film shelf asks for. The eighth is
    /// <see cref="FanArtTypes.Cover"/>, which is what music calls the same front picture: the reference keeps
    /// an album's art under that name, so a caller that asks a shelf of albums for their covers is answered -
    /// from the cache, because a cover is something an album's files were read for rather than something a
    /// folder happens to be called.
    /// </remarks>
    private static readonly string[] FANART_KINDS = LocalFanArtSearch.KINDS
        .Concat(new[] { FanArtTypes.Cover }).ToArray();

    /// <summary>
    /// What the album behind a track keeps, which is where a track's own cover lies.
    /// </summary>
    /// <param name="id">The media item asked about, which may be a track of an album.</param>
    /// <param name="kinds">The kinds to try, in the order the caller would rather have them.</param>
    /// <returns>
    /// The files the cache holds for the track's album, or nothing when the id is not a track, its album is not
    /// known, or the album has nothing of those kinds.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A thousand tracks share forty-odd covers, so the import writes each one once, under the album - which is
    /// what the reference does too. A shelf of tracks asked tile by tile would then be answered with
    /// placeholders although every file in it carries its art, so the question is followed along the
    /// <c>Track-&gt;Album</c> relationship the import wrote and answered from where the picture actually lies.
    /// </para>
    /// <para>
    /// The relationship is asked for as an aspect rather than assumed, because a track that was merged into an
    /// album by a later import has it while a track that is its own release has nothing to follow.
    /// </para>
    /// </remarks>
    private static IList<string> AlbumFanArtFiles(Guid id, params string[] kinds)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      IFanArtCache cache = ServiceRegistration.Get<IFanArtCache>(false);
      if (mediaLibrary == null || cache == null)
        return new List<string>();

      try
      {
        // The audio aspect is asked for alongside the link because it is what says this id is a track at all;
        // without asking, the answer would never carry it and every title would look like one that has no album.
        MediaItemQuery query = new MediaItemQuery(
            new List<Guid> { MediaAspect.ASPECT_ID },
            new List<Guid> { AudioAspect.ASPECT_ID, RelationshipAspect.ASPECT_ID },
            new MediaItemIdFilter(id));
        IList<MediaItem> items = mediaLibrary.Search(query, false, null, VirtualTitles.LetsVirtualTitlesIn);
        MediaItem item = items == null ? null : items.FirstOrDefault();
        if (item == null || !item.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
          return new List<string>();

        if (!RelationshipExtractorUtils.TryGetLinkedId(AudioAlbumAspect.ROLE_ALBUM, item.Aspects,
                out Guid albumId))
          return new List<string>();

        foreach (string kind in kinds)
        {
          IList<string> files = cache.GetFanArtFiles(albumId, kind);
          if (files != null && files.Count > 0)
            return files;
        }
      }
      catch (IllegalCallException ex)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Info("MediaController: the media library refused to say what album a track belongs to: {0}",
              ex.Message);
      }
      return new List<string>();
    }

    /// <summary>
    /// The requested kind as the cache names it, or null when the caller asked for something that is not a kind.
    /// </summary>
    private static string KnownFanArtType(string requested)
    {
      return Array.Find(FANART_KINDS, kind =>
          string.Equals(kind, requested, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The bytes of the first of the given paths that is a file with something in it, or null when none is.
    /// </summary>
    private static byte[] FirstReadablePicture(IList<string> paths)
    {
      if (paths == null)
        return null;
      foreach (string path in paths)
      {
        try
        {
          byte[] bytes = System.IO.File.ReadAllBytes(path);
          if (bytes.Length > 0)
            return bytes;
        }
        catch (IOException)
        {
          // A cache entry whose file went away is as absent as one that was never written; the local search
          // below gets its turn either way.
        }
      }
      return null;
    }

    /// <summary>
    /// The media type of a picture from the bytes that begin it, or
    /// <c>application/octet-stream</c> when none of them match.
    /// <c>application/octet-stream</c> when none of them match.
    /// </summary>
    /// <remarks>
    /// Read from the data rather than stored, because the database does not record what format a thumbnail is in
    /// - it holds a byte array and trusts whoever wrote it - and the importers write whatever their source gave
    /// them (the video thumbnailer writes PNG, online providers write what their servers serve, which is usually
    /// JPEG). A wrong type would make a browser refuse to draw a picture it should draw, which is why the fallback
    /// is the honest "bytes of an unknown kind" rather than a guess at JPEG.
    /// </remarks>
    private static string PictureMediaType(byte[] picture)
    {
      if (picture.Length >= 8 && picture[0] == 0x89 && picture[1] == 0x50 && picture[2] == 0x4E &&
          picture[3] == 0x47)
        return "image/png";
      if (picture.Length >= 3 && picture[0] == 0xFF && picture[1] == 0xD8 && picture[2] == 0xFF)
        return "image/jpeg";
      if (picture.Length >= 3 && picture[0] == 'G' && picture[1] == 'I' && picture[2] == 'F')
        return "image/gif";
      if (picture.Length >= 2 && picture[0] == 'B' && picture[1] == 'M')
        return "image/bmp";
      return "application/octet-stream";
    }

    #region The words that lie next to the title

    /// <summary>
    /// The detail fields the library left empty, filled from the metadata files that lie next to the media.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why files, and why they never win.</b> A library imported from bare file names holds a title and a path
    /// and nothing else, while the tagging tools that described it - tinyMediaManager and its like - wrote what
    /// they found into <c>.nfo</c> files next to the media: one per episode or film, and one <c>tvshow.nfo</c>
    /// for the whole show. Those bytes are the household's own data, and a details screen that ignores them tells
    /// the viewer there is nothing to know about a show that was described to the last genre. The database stays
    /// the authority all the same: every field an aspect already filled keeps its value, and the files only fill
    /// the nulls - the same division of labor as for the pictures (see <see cref="LocalFanArtSearch"/>).
    /// </para>
    /// <para>
    /// <b>Which file describes which title.</b> An episode is described by the file of its own name; a film by
    /// its sibling of the same stem; a series or season by the show's file, which - having no file of its own -
    /// is found through the same detour to one of its episodes that the pictures take
    /// (<see cref="LocalFanArtSearch.FirstEpisodeOf"/>). The show's file is looked for from the media file's folder up to two
    /// levels above it, so a library with season folders and one without are both read.
    /// </para>
    /// </remarks>
    /// <summary>
    /// The age certificate this household is told about, out of the one the library holds.
    /// </summary>
    /// <param name="item">The title, whose aspects say whether it is a film or something of a series.</param>
    /// <param name="stored">The certificate as the importer or a file beside the title wrote it.</param>
    /// <remarks>
    /// A title is usually imported with the mark of the country that rated it, so a film bought in America says
    /// "PG-13" here and a household in Germany reads a sign from a system it does not use. Which country's mark
    /// to name is asked of <see cref="MediaCertificationSettings"/> - empty, which is what everything runs with,
    /// leaves the imported word standing, and so does a mark the table does not know: the alternative is a screen
    /// guessing how old somebody has to be, which is the one thing a certificate should not do.
    /// </remarks>
    private static string ShownCertification(MediaItem item, string stored)
    {
      if (string.IsNullOrEmpty(stored) || item == null || item.Aspects == null)
        return stored;

      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>(false);
      if (settingsManager == null)
        return stored;

      MediaCertificationSettings settings = settingsManager.Load<MediaCertificationSettings>();
      if (settings == null)
        return stored;

      return item.Aspects.ContainsKey(MovieAspect.ASPECT_ID)
          ? settings.WordsForMovie(stored)
          : settings.WordsForSeries(stored);
    }

    private void FillMissingDetailsFromFiles(MediaItem item, ItemResult result)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return;

      MediaItem anchor = item;
      if (anchor.GetResourceLocator() == null)
      {
        try
        {
          MediaItem episode = LocalFanArtSearch.FirstEpisodeOf(mediaLibrary, item);
          if (episode != null)
            anchor = episode;
        }
        catch (IllegalCallException)
        {
          return;
        }
      }

      IResourceLocator locator = anchor.GetResourceLocator();
      if (locator == null)
        return;

      ResourcePath mediaFile = locator.NativeResourcePath;
      TitleMetadata fromFile = ReadTitleMetadata(SiblingMetadataFile(mediaFile));
      TitleMetadata fromShow = ReadTitleMetadata(ShowMetadataFileAbove(mediaFile));
      if (fromFile == null && fromShow == null)
        return;

      // Whichever file describes the asked-about title itself is read first and its words win: an episode's plot
      // is its own and the show's file only fills what the episode's leaves out, while for the show it is the
      // other way round - its file speaks for it, and an episode's summary is not its plot.
      SingleMediaItemAspect episodeAspect;
      bool askedAboutEpisode = MediaItemAspect.TryGetAspect(item.Aspects, EpisodeAspect.Metadata,
          out episodeAspect);
      TitleMetadata first = askedAboutEpisode ? fromFile : fromShow;
      TitleMetadata second = askedAboutEpisode ? fromShow : fromFile;

      if (result.Overview == null && result.StoryPlot == null)
        result.Overview = first != null && first.Plot != null ? first.Plot
            : second == null ? null : second.Plot;
      if (result.Tagline == null)
        result.Tagline = first != null && first.Tagline != null ? first.Tagline
            : second == null ? null : second.Tagline;
      if (result.Certification == null)
        result.Certification = first != null && first.Certification != null ? first.Certification
            : second == null ? null : second.Certification;
      if (result.RuntimeMinutes == null)
        result.RuntimeMinutes = first != null && first.RuntimeMinutes.HasValue ? first.RuntimeMinutes
            : second == null ? null : second.RuntimeMinutes;
      if (result.Year == null)
        result.Year = first != null && first.Year.HasValue ? first.Year
            : second == null ? null : second.Year;
      if (result.FirstAired == null)
        result.FirstAired = first != null && first.FirstAired.HasValue ? first.FirstAired
            : second == null ? null : second.FirstAired;
      if (result.EpisodeName == null && askedAboutEpisode && fromFile != null)
        result.EpisodeName = fromFile.EpisodeTitle;

      // Genres come from the show's file when it has them, because an episode file names none and a film's own
      // file is its own "show"; the order between the two files cannot make the wrong one win here.
      if (result.Genres == null || result.Genres.Count == 0)
      {
        if (fromShow != null && fromShow.Genres.Count > 0)
          result.Genres = fromShow.Genres;
        else if (fromFile != null && fromFile.Genres.Count > 0)
          result.Genres = fromFile.Genres;
      }

      // The library's own rating wins outright - it is the household's number, and the tools' is a crowd's - so
      // the file's number is only asked for when the library has none, and it keeps its decimal.
      if ((result.Rating == null || result.Rating.Value == 0) && result.TotalRating == null)
        result.TotalRating = first != null && first.Rating.HasValue ? first.Rating
            : second == null ? null : second.Rating;
    }

    /// <summary>
    /// What the metadata files know about one title; every field null when the file did not name it.
    /// </summary>
    private sealed class TitleMetadata
    {
      public string Plot;
      public string EpisodeTitle;
      public string Tagline;
      public string Certification;
      public readonly List<string> Genres = new List<string>();
      public int? RuntimeMinutes;
      public double? Rating;
      public DateTime? FirstAired;
      public int? Year;
    }

    /// <summary>
    /// The file of the same stem as the media file, with the extension of the tagging tools' text metadata.
    /// </summary>
    private static ResourcePath SiblingMetadataFile(ResourcePath mediaFile)
    {
      if (mediaFile == null)
        return null;
      try
      {
        ResourcePath directory = ResourcePathHelper.Combine(mediaFile, "../");
        return ResourcePathHelper.Combine(directory,
            ResourcePath.GetFileNameWithoutExtension(mediaFile) + ".nfo");
      }
      catch (ArgumentException)
      {
        return null;
      }
    }

    /// <summary>
    /// The television show's own file above the given media file, or null when no folder within two levels holds
    /// one. Never throws: a folder that cannot be read holds no show file as far as this answer is concerned.
    /// </summary>
    private static ResourcePath ShowMetadataFileAbove(ResourcePath mediaFile)
    {
      if (mediaFile == null)
        return null;

      try
      {
        ResourcePath directory = ResourcePathHelper.Combine(mediaFile, "../");
        for (int hop = 0; hop < 3 && directory != null; hop++)
        {
          ResourcePath candidate = ResourcePathHelper.Combine(directory, "tvshow.nfo");
          if (MetadataFileReadable(candidate))
            return candidate;
          directory = ResourcePathHelper.Combine(directory, "../");
        }
      }
      catch (ArgumentException)
      {
        // The walk climbed past the root of the provider; there is no folder above this one.
      }
      return null;
    }

    /// <summary>
    /// Whether the given path is a readable local file. The accessor is opened and closed here; the answer is
    /// only used to skip files that are not there.
    /// </summary>
    private static bool MetadataFileReadable(ResourcePath path)
    {
      IResourceAccessor accessor = LocalFanArtSearch.TryOpenLocalFile(path);
      if (accessor == null)
        return false;
      try
      {
        IFileSystemResourceAccessor file = accessor as IFileSystemResourceAccessor;
        return file != null && file.IsFile && file.Exists;
      }
      finally
      {
        accessor.Dispose();
      }
    }

    /// <summary>
    /// The words of one metadata file, or null when it is not there, cannot be read, or is not a file a tagging
    /// tool wrote.
    /// </summary>
    private static TitleMetadata ReadTitleMetadata(ResourcePath path)
    {
      IResourceAccessor accessor = LocalFanArtSearch.TryOpenLocalFile(path);
      if (accessor == null)
        return null;
      try
      {
        IFileSystemResourceAccessor file = accessor as IFileSystemResourceAccessor;
        if (file == null || !file.IsFile || !file.Exists)
          return null;

        XDocument document;
        using (Stream body = file.OpenRead())
          document = XDocument.Load(body);
        return ParseTitleMetadata(document);
      }
      catch (IOException)
      {
        return null;
      }
      catch (UnauthorizedAccessException)
      {
        return null;
      }
      catch (System.Xml.XmlException)
      {
        return null;
      }
      finally
      {
        accessor.Dispose();
      }
    }

    /// <summary>
    /// The fields this API speaks out of one Kodi-shape metadata file. The root's name says what kind of file it
    /// is, and anything else than the three kinds the tagging tools write is not metadata for a title.
    /// </summary>
    private static TitleMetadata ParseTitleMetadata(XDocument document)
    {
      XElement root = document == null ? null : document.Root;
      if (root == null)
        return null;

      string kind = root.Name.LocalName;
      if (kind != "episodedetails" && kind != "tvshow" && kind != "movie")
        return null;

      TitleMetadata metadata = new TitleMetadata();
      metadata.Plot = FirstText(root.Elements("plot").Select(element => element.Value))
          ?? FirstText(root.Elements("outline").Select(element => element.Value));
      metadata.EpisodeTitle = Text((string)root.Element("title"));
      metadata.Tagline = Text((string)root.Element("tagline"));
      metadata.Certification = Text((string)root.Element("certification"))
          ?? Text((string)root.Element("mpaa"));
      foreach (string genre in root.Elements("genre").Select(element => Text(element.Value))
          .Where(words => words != null).Distinct())
        metadata.Genres.Add(genre);

      int number;
      if (int.TryParse((string)root.Element("runtime"), NumberStyles.Integer, CultureInfo.InvariantCulture,
          out number) && number > 0)
        metadata.RuntimeMinutes = number;
      if (int.TryParse((string)root.Element("year"), NumberStyles.Integer, CultureInfo.InvariantCulture,
          out number) && number > 1800)
        metadata.Year = number;

      metadata.Rating = ReadMetadataRating(root);
      metadata.FirstAired = ReadMetadataDate(kind == "episodedetails"
          ? (string)root.Element("aired") ?? (string)root.Element("premiered")
          : (string)root.Element("premiered") ?? (string)root.Element("aired"));
      return metadata;
    }

    /// <summary>
    /// The file's rating of the title, told in tenths like this API's own. A rating site's number is read from
    /// the tagged {@code <ratings>} list when the file holds one (the default entry first), and from the plain
    /// element when it does not; nothing, and zero, both mean the file did not rate the title.
    /// </summary>
    private static double? ReadMetadataRating(XElement root)
    {
      XElement source = null;
      XElement ratings = root.Element("ratings");
      if (ratings != null)
      {
        List<XElement> listed = ratings.Elements("rating").ToList();
        XElement chosen = listed.FirstOrDefault(entry =>
            string.Equals((string)entry.Attribute("default"), "true", StringComparison.OrdinalIgnoreCase))
            ?? listed.FirstOrDefault();
        if (chosen != null)
          source = chosen.Element("value") ?? chosen;
      }
      if (source == null)
        source = root.Element("rating");
      if (source == null)
        return null;

      double value;
      if (!double.TryParse(source.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
          || value <= 0)
        return null;
      return value > 10 ? value / 10.0 : value;
    }

    /// <summary>
    /// The file's date, in the one shape the tools write ("yyyy-MM-dd") and in anything a
    /// <see cref="DateTime.TryParse(string, IFormatProvider, DateTimeStyles, out DateTime)"/> still recognises;
    /// null when the field held no date at all.
    /// </summary>
    private static DateTime? ReadMetadataDate(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return null;
      DateTime parsed;
      if (DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
          DateTimeStyles.None, out parsed))
        return parsed;
      if (DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        return parsed;
      return null;
    }

    #endregion

    #region Streaming

    /// <summary>
    /// The type sent for a media file whose extension means nothing to the server.
    /// </summary>
    public const string DEFAULT_STREAM_MEDIA_TYPE = "application/octet-stream";

    /// <summary>
    /// Why the server could not send the bytes of a title it knows about.
    /// </summary>
    public class StreamResult
    {
      public Guid Id { get; set; }

      /// <summary>
      /// What was looked for and what came of it, in words a screen can show as it stands.
      /// </summary>
      public string Error { get; set; }
    }

    /// <summary>
    /// The title's own file, as a stream a player can watch.
    /// </summary>
    /// <param name="id">The media item id, as <c>GET api/media/titles</c> reported it.</param>
    /// <param name="cancellationToken">Cancelled when the viewer walks away mid-film.</param>
    /// <remarks>
    /// <para>
    /// <b>Why this endpoint exists at all.</b> <see cref="GetItem"/> hands a client the serialized
    /// <see cref="ResourcePath"/> of a title, and a front end on the same machine as the server - or one that can
    /// reach the same share - plays it directly. Everything else needs the bytes over HTTP: a television in the
    /// living room whose files live in the cellar, a Linux client that was never given the Windows share, an
    /// Android box that cannot open a path at all. Without this the library is a catalogue you can look at and
    /// nothing you can watch away from the server.
    /// </para>
    /// <para>
    /// <b>Why the id and not the path.</b> MP2 already has an HTTP file server
    /// (<c>ResourceAccessModule</c>), and it is addressed by a serialized resource path handed in the URL. Its own
    /// source says why that is not enough here: <c>IsAllowedToAccess</c> is a <c>TODO</c> that returns true, so
    /// anybody who can name a path can read whatever the server's process may read - including the paths that were
    /// never in anybody's library. This endpoint takes a media item id instead, so the largest set of files a
    /// caller can ask for is the set the importer registered, which is exactly the set
    /// <see cref="GetTitles"/> already lists. The path never leaves the server.
    /// </para>
    /// <para>
    /// <b>Ranges, because a player seeks.</b> <see cref="FileStreamResult.EnableRangeProcessing"/> is on, which
    /// makes ASP.NET answer a <c>Range</c> header with <c>206 Partial Content</c> and a <c>Content-Range</c> of
    /// its own, refuse an impossible one with <c>416</c>, and advertise <c>Accept-Ranges: bytes</c>. That is not
    /// a nicety: an MP4 keeps its index at the end of the file and a Matroska its cues after the clusters, so a
    /// player that cannot seek gets a black screen and a working audio track - and libVLC is the player here.
    /// A single range per request is all MP2's own module ever supported either, and it is all any player in
    /// this tree asks for. <c>HEAD</c> is answered by the same action, because asking how long the film is before
    /// committing to it is the first thing a player that can seek does.
    /// </para>
    /// <para>
    /// <b>What the failures say.</b> <c>404</c> means the id names no title, or a title with no file recorded
    /// (a stub, or an import that wrote the metadata and lost the resource); <c>410</c> means the title is there,
    /// its file is named, and the server could not open it - the disk is unmounted, the network share is asleep,
    /// the file was deleted since the last import. Those are different sentences to show a viewer: one is "your
    /// library has a hole in it", the other is "your library is right and your disk is not answering".
    /// <c>401</c> and <c>403</c> come from <see cref="MediaPortalAuthorizeAttribute"/> like everywhere else in
    /// this API - a stream is the least anonymous thing a media centre serves.
    /// </para>
    /// <para>
    /// The media type comes from the extension through MP2's own table
    /// (<see cref="MediaPortal.Utilities.SystemAPI.MimeTypeDetector"/>), not from magic bytes: the sniffing that
    /// table is paired with reads the whole file header off a local path, which the resource here may not be,
    /// and a player is going to look at the bytes itself whatever this header says. An unknown extension is
    /// <see cref="DEFAULT_STREAM_MEDIA_TYPE"/>, which is the truth rather than a guess at MP4.
    /// </para>
    /// </remarks>
    [HttpGet("items/{id}/stream")]
    [HttpHead("items/{id}/stream")]
    public async Task<IActionResult> GetStream(Guid id, CancellationToken cancellationToken)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      // The accessor is opened before any byte is sent and closed when the response stream is closed, which is
      // the only moment that is correct on every platform: a network provider's accessor holds the connection
      // the stream reads from, and disposing it when the handler returns would pull the ground out from under a
      // response that is still being written.
      IResourceAccessor accessor = null;
      bool streamClosesIt = false;

      try
      {
        // ProviderResourceAspect is asked for as an optional aspect because GetResourceLocator reads the file
        // out of it; an item found without it is an item whose file the server could not name even if it has one.
        MediaItemQuery query = new MediaItemQuery(
            new List<Guid> { MediaAspect.ASPECT_ID },
            new List<Guid> { ProviderResourceAspect.ASPECT_ID },
            new MediaItemIdFilter(id));

        // Nobody's read: the profile parameter attaches one person's position in a title, and a position has no
        // bearing on the bytes. It also means two people watching the same film cost the database the same
        // amount of work. The title is named by its id, so the same reading as the details endpoint applies: a
        // household that let this title onto a shelf is a household that means to play it, and whether it turns
        // out to have bytes to play is answered further down, not by refusing to look.
        IList<MediaItem> items = mediaLibrary.Search(query, false, null, VirtualTitles.LetsVirtualTitlesIn);
        MediaItem item = items == null ? null : items.FirstOrDefault();

        if (item == null)
          return NotFound(new StreamResult { Id = id, Error = "this server has no title with the id " + id });

        IResourceLocator locator = item.GetResourceLocator();
        if (locator == null)
        {
          return NotFound(new StreamResult
          {
            Id = id,
            Error = "this server has no file recorded for the title " + id
          });
        }

        accessor = locator.CreateAccessor();
        IFileSystemResourceAccessor file = accessor as IFileSystemResourceAccessor;
        if (file == null)
          return FileGone(id, "the server cannot read '" + locator.NativeSystemId + "' resources");

        if (!file.Exists || !file.IsFile)
          return FileGone(id, "'" + file.ResourceName + "' is not there any more");

        Stream body = await file.OpenReadAsync().ConfigureAwait(false);
        streamClosesIt = true;

        return new FileStreamResult(new ResourceReadStream(body, accessor), StreamMediaType(file.ResourceName))
        {
          EnableRangeProcessing = true
        };
      }
      catch (IllegalCallException ex)
      {
        LogStreamRefusal("the media library refused a stream request, answering 503: " + ex.Message);
        return StatusCode(StatusCodes.Status503ServiceUnavailable);
      }
      catch (IOException ex)
      {
        return FileGone(id, "the file could not be read: " + ex.Message);
      }
      catch (UnauthorizedAccessException ex)
      {
        return FileGone(id, "the server is not allowed to read the file: " + ex.Message);
      }
      catch (ArgumentException ex)
      {
        // What MP2's resource access throws when a stored path resolves to nothing any provider claims.
        return FileGone(id, "the file the library recorded cannot be reached: " + ex.Message);
      }
      catch (NotSupportedException ex)
      {
        return FileGone(id, "the server cannot open this kind of resource: " + ex.Message);
      }
      finally
      {
        if (!streamClosesIt && accessor != null)
          accessor.Dispose();
      }
    }

    /// <summary>
    /// The answer for a title the library knows but cannot hand over, with the reason in the body.
    /// </summary>
    private ObjectResult FileGone(Guid id, string reason)
    {
      LogStreamRefusal(reason);
      return new ObjectResult(new StreamResult { Id = id, Error = reason })
      {
        StatusCode = StatusCodes.Status410Gone
      };
    }

    /// <summary>
    /// One line in the server's log about a stream that did not happen.
    /// </summary>
    /// <remarks>
    /// Logged because the viewer's symptom is a film that refuses to start, which they will describe as the
    /// player being broken. The server is where the reason is, and without this line the reason is not there
    /// either.
    /// </remarks>
    private static void LogStreamRefusal(string reason)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>(false);
      if (logger != null)
        logger.Info("MediaController: a stream was asked for and refused: {0}", reason);
    }

    /// <summary>
    /// The media type of a media file, from its extension, or <see cref="DEFAULT_STREAM_MEDIA_TYPE"/>.
    /// </summary>
    private static string StreamMediaType(string resourceName)
    {
      string type = MediaPortal.Utilities.SystemAPI.MimeTypeDetector.GetMimeTypeFromExtension(resourceName);
      return string.IsNullOrEmpty(type) ? DEFAULT_STREAM_MEDIA_TYPE : type;
    }

    /// <summary>
    /// The stream a response is written from, which closes the resource accessor that opened it.
    /// </summary>
    /// <remarks>
    /// ASP.NET disposes the stream it was given and nothing else, so without this wrapper the accessor - and
    /// with it a connection to whoever serves the file - would outlive every film watched, one per request.
    /// Everything else is passed through untouched, including seeking, which is what makes a range request
    /// work: a wrapper that lost the seek would turn this endpoint into a download.
    /// </remarks>
    private sealed class ResourceReadStream : Stream
    {
      private readonly Stream _inner;
      private IResourceAccessor _accessor;

      internal ResourceReadStream(Stream inner, IResourceAccessor accessor)
      {
        _inner = inner;
        _accessor = accessor;
      }

      public override bool CanRead { get { return _inner.CanRead; } }
      public override bool CanSeek { get { return _inner.CanSeek; } }
      public override bool CanWrite { get { return _inner.CanWrite; } }
      public override long Length { get { return _inner.Length; } }

      public override long Position
      {
        get { return _inner.Position; }
        set { _inner.Position = value; }
      }

      public override void Flush()
      {
        _inner.Flush();
      }

      public override Task FlushAsync(CancellationToken cancellationToken)
      {
        return _inner.FlushAsync(cancellationToken);
      }

      public override int Read(byte[] buffer, int offset, int count)
      {
        return _inner.Read(buffer, offset, count);
      }

      public override int Read(Span<byte> buffer)
      {
        return _inner.Read(buffer);
      }

      public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
      {
        return _inner.ReadAsync(buffer, offset, count, cancellationToken);
      }

      public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
      {
        return _inner.ReadAsync(buffer, cancellationToken);
      }

      public override long Seek(long offset, SeekOrigin origin)
      {
        return _inner.Seek(offset, origin);
      }

      public override void SetLength(long value)
      {
        _inner.SetLength(value);
      }

      public override void Write(byte[] buffer, int offset, int count)
      {
        _inner.Write(buffer, offset, count);
      }

      protected override void Dispose(bool disposing)
      {
        try
        {
          _inner.Dispose();
        }
        finally
        {
          IResourceAccessor accessor = _accessor;
          _accessor = null;
          if (accessor != null)
            accessor.Dispose();
          base.Dispose(disposing);
        }
      }
    }

    #endregion

    #endregion

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

      // Every endpoint that opens a title comes through here, so this is where "the household asked for the
      // library's invented titles too" has to be obeyed; see the details endpoint for why an invented title is
      // then read like any other one.
      IList<MediaItem> items = mediaLibrary.Search(query, false, profile, VirtualTitles.LetsVirtualTitlesIn);
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
    /// The sorts a title list has: by name, by year, by when it arrived in the library, by when it was last
    /// watched, by where a title sits in the one that holds it, or by none at all.
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
      if (string.Equals(sort, "added", StringComparison.OrdinalIgnoreCase))
        return "added";
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
        case "added":
          // The date the library first saw the file. This is not the recording time - that one answers "what year
          // was this made", which is a fact about the film; DateAdded is a fact about this library, and it is the
          // only column that answers "what arrived lately". The media library stamps it itself on every title the
          // first time it stores it (MediaLibrary.AddOrUpdateMediaItem) and keeps it through every reimport, so
          // unlike an importer's field it is here for anything the library knows, however it got in.
          sorts.Add(new AttributeSortInformation(ImporterAspect.ATTR_DATEADDED, direction));
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
      // Looked up by its id again, so the same reading as every other lookup by id: a household that lets
      // invented titles in is not asked to name which one it means first.
      IList<MediaItem> found = mediaLibrary.Search(query, false, null, VirtualTitles.LetsVirtualTitlesIn);
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
