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
using MediaPortal.Common.Async;
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
using MediaPortal.Utilities.DB;
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
    /// The character the search's LIKE pattern hides wildcards behind - the same one the library's own text
    /// search uses, so the escaping and the filter agree.
    /// </summary>
    private const char SEARCH_ESCAPE = '\\';

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
    /// The condition the signed-in caller's own restrictions put on the titles they may be shown, or null when
    /// nothing restricts them - nobody signed in, a grown-up, or a store that has no opinion.
    /// </summary>
    /// <remarks>
    /// This is the reference's own design, which is worth stating because it surprises people: restricted
    /// titles are not greyed out and not locked behind a password prompt - they are <i>not fetched</i>. The
    /// reference builds the very same <see cref="UserProfile.GetUserFilter"/> condition into every media
    /// query its screens run (<c>UserHelper.cs:75</c>, <c>BaseMediaListProvider.cs:102</c>), so a child's
    /// television never learns the forbidden title exists, and there is nothing on screen to unlock. The age
    /// ceiling, the parental-guided and unrated exceptions, and the share allowlist all speak through that one
    /// condition, straight from the user store.
    /// <para>
    /// It reads the profile from the store per request rather than caching it: the answer changes the moment a
    /// parent edits the child's page in Settings, and one lookup by primary key per list request is what a
    /// local network is for.
    /// </para>
    /// </remarks>
    /// <param name="necessaryMias">The aspects the query cannot do without, which decide whether the age
    /// condition is asked of films, of series, or through the episodes' link to their series - exactly the
    /// three branches the reference's own filter distinguishes.</param>
    private IFilter CallerRestrictionFilter(ICollection<Guid> necessaryMias)
    {
      Guid? profileId = CallerProfile;
      if (profileId == null)
        return null;

      IUserProfileDataManagement store = ServiceRegistration.Get<IUserProfileDataManagement>(false);
      if (store == null)
        return null;

      UserProfile profile;
      try
      {
        AsyncResult<UserProfile> found = store.GetProfileAsync(profileId.Value).Result;
        if (found == null || !found.Success)
          return null;
        profile = found.Result;
      }
      catch (Exception)
      {
        // A store that cannot answer is not a reason to show the whole library to a restricted user, but it is
        // also not a reason to blame the media library for - the restriction silently not applying is the same
        // state an unrestricted household is in, and the store will complain about itself.
        return null;
      }

      if (profile == null)
        return null;
      bool restrictsAges = profile.RestrictAges && profile.AllowedAge.HasValue;
      if (!restrictsAges && !profile.RestrictShares)
        return null;

      return profile.GetUserFilter(necessaryMias);
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
      /// The title's genres as the importer wrote them, or null when the title carries none - which is ordinary
      /// for files nobody described. Read off the genre aspect the request already asked along, so a screen can
      /// say what kind of thing a title is without opening one request per row.
      /// </summary>
      public List<string> Genres { get; set; }

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
      /// How far the caller's own viewer got in this title, nought to a hundred, or null when the request named
      /// nobody - an anonymous request has no viewer and so no figure of anybody's own.
      /// </summary>
      /// <remarks>
      /// <para>
      /// This is the number the reference decides with and the number screens have to have: a title counts as
      /// watched at a hundred, which is what puts the "set unplayed" entry in its menu and keeps "set played" out
      /// of it (<c>AbstractWatchedAction.cs:47-49</c>), a container carries the average of its children's numbers
      /// (<c>MediaLibrary.cs:2494-2513</c>), and the bar under a home tile is drawn from it.
      /// </para>
      /// <para>
      /// Unlike <see cref="Resume"/> this says the whole number, nought and a hundred included, because those two
      /// are the answers a screen cannot do without: "opened and left at the very start" and "watched through" are
      /// both things a resume position has nothing to say about.
      /// </para>
      /// </remarks>
      public int? Percentage { get; set; }

      /// <summary>
      /// For an episode: the season number the library holds. For a season: the season it is. Null for anything
      /// else, and for a title whose season was never written.
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
      /// For a season: how many episodes the library counts inside it, or null when it counted none. The shelf's
      /// answer to "what is this season", the way an album's row answers with its track count.
      /// </summary>
      public int? Episodes { get; set; }

      /// <summary>
      /// For a season that the library does not hold as a title of its own - one counted off the season numbers its
      /// episodes carry - the series that shelf of episodes has to be asked in, so that the two parts a counted
      /// season really has (this id's owner is nothing but a key to the list) can be sent back together:
      /// <c>?inside=&lt;ParentId&gt;&amp;aspect=EpisodeItem&amp;season=&lt;Season&gt;</c>. Null for every title the
      /// library holds, which is almost all of them.
      /// </summary>
      public Guid? ParentId { get; set; }

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
      /// means is the importer's business, not this answer's.  The details answer of one album counts the tracks it
      /// really holds instead (<see cref="ItemResult.TrackCount"/>), which is the number a screen standing in
      /// front of that one album should say.
      /// </remarks>
      public int? TrackCount { get; set; }

      /// <summary>
      /// The short films this title advertises itself with, or null when the library knows none.
      /// </summary>
      /// <remarks>
      /// On the row and not only on the details answer because the context menu of a shelf asks the question
      /// while the title is standing there: a "Trailer" entry that needed a second request per title to know
      /// whether to appear at all would be one request per step of the walk.
      /// </remarks>
      public List<TrailerResult> Trailers { get; set; }
    }

    /// <summary>
    /// One of the short films a title advertises itself with, as <c>GET api/media/titles</c> and
    /// <c>GET api/media/items/{id}</c> name them.
    /// </summary>
    public class TrailerResult
    {
      /// <summary>
      /// What sort of trailer this is: a file the library holds (<c>1</c>), a web address the player opens
      /// itself (<c>2</c>), a film at a video site (<c>3</c>), or something this server has no word for
      /// (<c>0</c>). The numbers are the <c>TrailerAspect.KIND_</c> values.
      /// </summary>
      public int Kind { get; set; }

      /// <summary>
      /// What the trailer is called, when anything does. Null for the trailer nobody named, which a screen
      /// reads as plain "Trailer".
      /// </summary>
      public string Label { get; set; }

      /// <summary>
      /// Where the trailer is, in the form a player is handed: the server's own address for a file the library
      /// holds, the page of the video site for a film standing there, the address itself for a web file.
      /// </summary>
      /// <remarks>
      /// Null when nothing can be made of the address the tagging tool wrote. Such a trailer is still named -
      /// it exists, and a screen that says so and shows <see cref="Source"/> is a better answer than one that
      /// quietly drops the film from the list.
      /// </remarks>
      public string Address { get; set; }

      /// <summary>
      /// The address as the metadata file wrote it, which for a trailer tagged by the reference tool is its own
      /// plugin format. Kept so that a screen can say what it was holding when it could not play it.
      /// </summary>
      public string Source { get; set; }
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
      /// The category the answer was narrowed to - <c>genre</c>, <c>year</c>, <c>actors</c>, <c>directors</c>,
      /// <c>writers</c> or <c>collection</c> - or null when it was not narrowed by one. Echoed because a genre list
      /// that came back empty has to be able to say which genre it was asked for.
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
    /// <c>lastplayed</c>, <c>season</c>, <c>episode</c>, <c>track</c>, or <c>none</c> for whatever order the
    /// database returns.
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
    /// <c>started</c> for the ones somebody began and did not finish, <c>favorite</c> for the ones somebody has
    /// played at least once - which the library orders by how often and how lately, so a <c>sort</c> is only
    /// needed if the caller wants a different order - and anything else, including nothing, for both. The
    /// <c>seen</c> field of the answer says which of the five readings was applied.
    /// </param>
    /// <param name="inside">
    /// The id of a title that holds other titles - an album, a series, a season, a film collection - to list the
    /// titles inside it: the tracks of an album with <c>aspect=AudioItem</c>, the seasons of a series with
    /// <c>aspect=SeasonItem</c>, the episodes of a series or of one season with <c>aspect=EpisodeItem</c>. What is
    /// asked for has to be something that title can hold, or the answer is a refusal rather than an empty list.
    /// Null or empty means the whole shelf, which is what it has always meant.
    /// </param>
    /// <param name="by">
    /// One of the categories of <c>GET api/media/groups</c> - <c>genre</c>, <c>year</c>, <c>actors</c>,
    /// <c>directors</c>, <c>writers</c>, <c>collection</c> - to keep only the titles carrying
    /// <paramref name="value"/> under it.  Together with the value it answers the second half of browsing: the
    /// groups say which genres there are, this says which films are in one of them.  <c>by=collection</c> is how a
    /// screen asks which films a collection holds.
    /// </param>
    /// <param name="value">
    /// The value to match: a genre, an actor's name as the importer wrote it, or - under <c>by=year</c> - a year,
    /// which holds every title whose recording time falls inside that year rather than a title recorded at the
    /// stroke of one.
    /// </param>
    /// <param name="season">
    /// A season number to narrow a shelf of episodes to: <c>?inside=&lt;series&gt;&amp;aspect=EpisodeItem&amp;season=3</c>
    /// answers that series' third season. A library built folder by folder holds a season as its own title, and then a
    /// season's id opens its shelf by itself; a library that only holds the episodes does not, so the shelf of its
    /// seasons is counted off those episodes instead and this parameter is how one of those counted seasons is
    /// opened. It is only taken on a shelf of episodes asked for inside a series.
    /// </param>
    [HttpGet("titles")]
    public ActionResult<TitlesResult> GetTitles([FromQuery] string aspect, [FromQuery] string sort,
        [FromQuery] string order, [FromQuery] int limit, [FromQuery] int offset, [FromQuery] string search,
        [FromQuery] string seen, [FromQuery] Guid inside = default(Guid),
        [FromQuery] string by = null, [FromQuery] string value = null, [FromQuery] int? season = null)
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

        // The genre aspect travels along for the same reason and pays the same price as the other optional
        // aspects - one grouped read over the page the library hands back, not a join per row: the by=genre shelf
        // has always filtered through this aspect in milliseconds, and the home band says the chosen title's
        // genres without opening a second request per step of the walk.
        bool hasGenres = metadata.Values.Any(entry => entry != null && entry.AspectId == GenreAspect.ASPECT_ID);
        if (hasGenres && aspectId != GenreAspect.ASPECT_ID)
          optional.Add(GenreAspect.ASPECT_ID);

        // The trailers travel along for the same reason and cost the same grouped read. Asked for by name rather
        // than read from the file beside the title on the spot, because a wall of a thousand films would then
        // open a thousand folders to answer a question the import already walked through once.
        bool hasTrailers = metadata.Values.Any(entry => entry != null && entry.AspectId == TrailerAspect.ASPECT_ID);
        if (hasTrailers && aspectId != TrailerAspect.ASPECT_ID)
          optional.Add(TrailerAspect.ASPECT_ID);

        // What the viewer asked the library to contain. Whitespace counts as nothing asked, because a search box
        // that the viewer cleared should show the whole shelf again rather than an empty one.
        string finding = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        // Whether the viewer has finished a title is a question MP2 answers twice over, and which of the two
        // answers applies is who is asking. With a name, its shelves read that person's own play date and
        // percentage (BaseLastWatchedMediaListProvider.cs:41-44, BaseUnwatchedMediaListProvider.cs:41-45,
        // BaseContinueWatchMediaListProvider.cs:41-46). Without one, they fall back to the household's counters on
        // the Media aspect: "PlayCount greater than nought" for watched and "PlayCount = 0" for unwatched
        // (BaseLastWatchedMediaListProvider.cs:44, BaseUnwatchedMediaListProvider.cs:45). Both readings are here,
        // and <see cref="AskedThroughChildren"/> adds the reference's second half - the children's own data - for
        // the two containers.
        //
        // The household's unwatched one needs a second half that MP2's provider does not, because PlayCount can be
        // null rather than zero: an imported film nobody has opened has never had anything written to that column,
        // and a condition of "PlayCount = 0" is not true about a column that holds nothing - in SQL it is unknown,
        // and an unknown in a WHERE clause throws the title out of the list. That would mean a brand new library
        // reporting "no unwatched titles", which is the one answer a viewer can tell is wrong. EmptyFilter compiles
        // to "IS NULL" (CompiledFilter.cs:255), so the two together mean "nothing was ever written here, or zero
        // was".
        // Who is asking decides which of MP2's two readings of these three questions applies, so the answer is
        // read from the same half of the library the marks were written to.
        Guid? viewer = CallerProfile;
        string seeing = ReadSeen(seen);
        IFilter seenFilter = null;
        IFilter halfWayChildren = null;
        bool askedChildrenHalfWay = false;
        Guid halfWayParentRole = Guid.Empty, halfWayChildRole = Guid.Empty, halfWayChildAspect = Guid.Empty;
        if (seeing == "watched")
        {
          if (viewer.HasValue)
            // "Something of it was seen", asked of this person. MP2's watched shelves are one condition on the
            // viewer's own play date: "this title has a date written under your name" and - because a series is
            // watched through its episodes - "one of your episodes of it has a date written" too
            // (BaseLastWatchedMediaListProvider.cs:41-44 plain, :92-99 for the two containers). The title's own
            // date is the average pass of the library keeping up: a series gets its date when one of its
            // episodes reports a viewing (MediaLibrary.cs:2535-2545, "if (updatePlayDate && watchPercentage > 0)").
            seenFilter = AskedThroughChildren(aspectId,
                new NotFilter(new EmptyUserDataFilter(viewer.Value, UserDataKeysKnown.KEY_PLAY_DATE)),
                new NotFilter(new EmptyUserDataFilter(viewer.Value, UserDataKeysKnown.KEY_PLAY_DATE)));
          else
          {
            // Nobody is asking, so there is no name to read dates under and the household's counters are all
            // there is: "PlayCount greater than nought" is MP2's own spelling of this case
            // (BaseLastWatchedMediaListProvider.cs:44). It is asked of the episodes and tracks, because that is
            // where the library keeps the number - a container's own column is never written, so asked of the
            // series it answered "not one of them" for every series and the browser's "seen" filter stayed empty
            // over a library full of watched ones.
            IFilter playedFilter = new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.GT, 0);
            if (aspectId == SeriesAspect.ASPECT_ID)
              seenFilter = new FilteredRelationshipFilter(SeriesAspect.ROLE_SERIES, EpisodeAspect.ROLE_EPISODE, playedFilter);
            else if (aspectId == AudioAlbumAspect.ASPECT_ID)
              seenFilter = new FilteredRelationshipFilter(AudioAlbumAspect.ROLE_ALBUM, AudioAspect.ROLE_TRACK, playedFilter);
            else
              seenFilter = playedFilter;
            // Without a viewer the library's averages do not exist either, so two shelves can hold one series
            // here: three episodes of nine seen put it under "seen" (an episode was) and under "unplayed"
            // (episodes remain). That is the shape of MP2's own shelves in this half, so it is not smoothed away.
          }
        }
        else if (seeing == "unwatched")
        {
          if (viewer.HasValue)
          {
            // "Nothing of it seen yet", about this person: an episode of theirs whose percentage is nought or was
            // never written, and - the half that settles which shelf a series ends up in - the series' own
            // average percentage also nought or never written
            // (BaseUnwatchedRelationshipMediaListProvider.cs:91-98; the plain form is
            // BaseUnwatchedMediaListProvider.cs:41-45). A series three episodes into nine has an average of
            // thirty-odd and is therefore <i>not</i> unplayed any more, which is exactly why it is not in this
            // shelf and in "continue watching" at the same time.
            seenFilter = AskedThroughChildren(aspectId,
                BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
                    new EmptyUserDataFilter(viewer.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE),
                    new RelationalUserDataFilter(viewer.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE,
                        RelationalOperator.EQ, PlayPercentage(0))),
                new RelationalUserDataFilter(viewer.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE,
                    RelationalOperator.EQ, PlayPercentage(0), true));
          }
          else
          {
            // "Nothing of it seen yet", in the household's own two halves: zero was written here, or nothing was
            // ever written - a column holding nothing is neither equal to nought nor unequal to it (see the note
            // above).
            IFilter neverPlayedFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
                new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.EQ, 0),
                new EmptyFilter(MediaAspect.ATTR_PLAYCOUNT));

            // A series is not the thing a viewer watches and an album not the thing they hear - the episodes and
            // the tracks are - so asking a container's own column whether anything was seen answers "never, about
            // every one of them": a container's PlayCount is never written, and with the "nothing was written"
            // half above that keeps every series unplayed for good, the completely watched ones included. MP2
            // asks the question of the children through the relationship instead
            // (UnwatchedSeriesMediaListProvider, whose base builds "a linked episode whose own position is empty
            // or nought", BaseUnwatchedRelationshipMediaListProvider.cs:92-98). The container's own half of that
            // condition is left out here on purpose: it is a viewer's percentage, and a request that names nobody
            // has no viewer's percentage to ask about.
            if (aspectId == SeriesAspect.ASPECT_ID)
              seenFilter = new FilteredRelationshipFilter(SeriesAspect.ROLE_SERIES, EpisodeAspect.ROLE_EPISODE, neverPlayedFilter);
            else if (aspectId == AudioAlbumAspect.ASPECT_ID)
              seenFilter = new FilteredRelationshipFilter(AudioAlbumAspect.ROLE_ALBUM, AudioAspect.ROLE_TRACK, neverPlayedFilter);
            else
              seenFilter = neverPlayedFilter;
          }
        }
        else if (seeing == "started")
        {
          if (viewer.HasValue)
            // "What did I leave half way?", about this person: a title with a date under their name whose own
            // percentage lies strictly between nought and a hundred, and a linked episode or track of theirs that
            // is half way or has no date at all (BaseContinueWatchRelationshipMediaListProvider.cs:97-105; the
            // plain form is BaseContinueWatchMediaListProvider.cs:41-46). The two ends are excluded on purpose for
            // a thing that is itself watched: a finished film is not something left to finish, and one opened and
            // abandoned at the first second has nothing a viewer would call half way.
          {
            IFilter childHalfWay = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
                new EmptyUserDataFilter(viewer.Value, UserDataKeysKnown.KEY_PLAY_DATE),
                new RelationalUserDataFilter(viewer.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE,
                    RelationalOperator.LT, PlayPercentage(100)));
            var ownHalves = new List<IFilter>
            {
                new NotFilter(new EmptyUserDataFilter(viewer.Value, UserDataKeysKnown.KEY_PLAY_DATE)),
                new RelationalUserDataFilter(viewer.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE,
                    RelationalOperator.LT, PlayPercentage(100))
            };
            // The nought end is only barred for the thing that is itself paused. A series' and an album's own
            // figure is no report but the mean of its children's whole per cent, so the one episode of eight that
            // stands at two per cent leaves the series standing at nought - and the shelf would throw out the
            // series that its own episode put on it. For the two containers the date and the "not finished" half
            // are therefore the whole of the question, and the child half below is what decides.
            if (aspectId != SeriesAspect.ASPECT_ID && aspectId != AudioAlbumAspect.ASPECT_ID)
              ownHalves.Add(new RelationalUserDataFilter(viewer.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE,
                  RelationalOperator.GT, PlayPercentage(0)));
            seenFilter = AskedThroughChildren(aspectId, childHalfWay,
                BooleanCombinationFilter.CombineFilters(BooleanOperator.And, ownHalves));
            halfWayChildren = childHalfWay;
          }
          else
          {
            // The household's half of the same question. MP2's own BaseContinueWatchMediaListProvider answers it
            // per profile; without one it states "PlayCount = 0", which is the answer of a shelf nobody has ever
            // reported to. What this server has instead - and what every library it has served so far was built
            // with - is the title's last-played date and play count, so the two halves of MP2's profile condition
            // are read from those: a date exists, and nothing was counted. Without the second half the filter
            // would also hand back films that were watched to the end, because finishing a film leaves its
            // last-played date behind, and a "still to finish" list holding finished films is worse than no list.
            halfWayChildren = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
                new NotFilter(new EmptyFilter(MediaAspect.ATTR_LASTPLAYED)),
                BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
                    new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.EQ, 0),
                    new EmptyFilter(MediaAspect.ATTR_PLAYCOUNT)));
          }

          if (aspectId == SeriesAspect.ASPECT_ID || aspectId == AudioAlbumAspect.ASPECT_ID)
          {
            // A series is never paused and an album never stopped - the episode and the track are - so the
            // half-way mark stands on the child, and every row of this shelf is asked one question back to say
            // where its viewer stopped (see the second pass below the search). MP2's continue shelves for the two
            // containers are relationship providers for the same reason
            // (ContinueWatchSeriesMediaListProvider.cs:37-42, ContinueWatchAlbumMediaListProvider.cs:41-46).
            if (seenFilter == null)
              // Nobody's report of "half way" is a date and a count on the child alone; a container has neither
              // of those of its own, so its half of the question is not asked.
              seenFilter = AskedThroughChildren(aspectId, halfWayChildren, null);
            askedChildrenHalfWay = true;
            if (aspectId == SeriesAspect.ASPECT_ID)
            {
              halfWayParentRole = SeriesAspect.ROLE_SERIES;
              halfWayChildRole = EpisodeAspect.ROLE_EPISODE;
              halfWayChildAspect = EpisodeAspect.ASPECT_ID;
            }
            else
            {
              halfWayParentRole = AudioAlbumAspect.ROLE_ALBUM;
              halfWayChildRole = AudioAspect.ROLE_TRACK;
              halfWayChildAspect = AudioAspect.ASPECT_ID;
            }
          }
          else
            seenFilter = seenFilter ?? halfWayChildren;
        }
        else if (seeing == "favorite")
        {
          // "What does this viewer keep coming back to?" The reference has no favourite marker to answer that
          // with: none of the nineteen user data keys it knows is a favourite (UserDataKeysKnown.cs:32-50), and
          // the whole shelf is one condition on the play count - "a count is written under your name"
          // (BaseFavoriteMediaListProvider.cs:41-43). A favourite is therefore not something a viewer switches
          // on; it is what the viewer has played, and the shelf's order does the rest.
          if (viewer.HasValue)
          {
            // Asked of a series or an album the reference reaches through the children, because the counting was
            // done there: a child of theirs whose count is written AND above nought, and the container carrying
            // the count of its own children this viewer played - which is the second half of
            // BaseFavoriteRelationshipMediaListProvider (in the favourite provider's own file, :96-117), and the
            // figure the library keeps up as the parents are reported (MediaLibrary.UpdateParentPlayUserData).
            IFilter childPlayed = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
                new NotFilter(new EmptyUserDataFilter(viewer.Value, UserDataKeysKnown.KEY_PLAY_COUNT)),
                new RelationalUserDataFilter(viewer.Value, UserDataKeysKnown.KEY_PLAY_COUNT,
                    RelationalOperator.GT, PlayCount(0)));
            seenFilter = AskedThroughChildren(aspectId, childPlayed,
                new NotFilter(new EmptyUserDataFilter(viewer.Value, UserDataKeysKnown.KEY_PLAY_MAX_CHILD_COUNT)));
          }
          else
          {
            // Nobody's name, so the household's own column answers "was this ever played"
            // (BaseFavoriteMediaListProvider.cs:44). As under "watched", a container is asked through its
            // children rather than through its own column, which the library never writes for a series or an
            // album - asking the column would answer "never" about a library full of played series.
            IFilter played = new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.GT, 0);
            seenFilter = AskedThroughChildren(aspectId, played, null);
          }
        }

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
        string insideName = null;
        bool insideOneAlbum = false;
        if (inside != Guid.Empty)
        {
          MediaItem parent = FindParentTitle(mediaLibrary, metadata, inside);
          if (parent == null)
            return NotFound(new TitlesResult
            {
              Aspect = match.Value.Name,
              Error = "this server has no title with the id '" + inside.ToString("D") + "' to list titles inside"
            });

          // Held on to because the disc is only a place within one record: a shelf of tunes standing inside an
          // album is walked disc by disc, while the same order asked of the whole library would lead every
          // album's first tune with the same number.
          insideOneAlbum = parent.Aspects != null
              && parent.Aspects.ContainsKey(AudioAlbumAspect.ASPECT_ID);
          // Held on to because a season counted off episode numbers has no title of its own to show: its row wears
          // the series' name with the number on the end, which is the spelling the reference uses for a season.
          SingleMediaItemAspect parentMedia;
          if (MediaItemAspect.TryGetAspect(parent.Aspects, MediaAspect.Metadata, out parentMedia))
            insideName = parentMedia.GetAttributeValue<string>(MediaAspect.ATTR_TITLE);

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

          // A season that the library holds as its own title opens its shelf with its own id. A season it does not
          // hold has only two parts to be asked with - the series it belongs to and the number its episodes carry -
          // so a caller that was handed a counted season comes back with both, and this narrows the series'
          // episodes down to the ones wearing that number.
          if (season.HasValue)
          {
            if (aspectId != EpisodeAspect.ASPECT_ID)
              return BadRequest(new TitlesResult
              {
                Aspect = match.Value.Name,
                Inside = inside,
                Error = "only episodes can be narrowed to one season; 'season=" +
                    season.Value.ToString(CultureInfo.InvariantCulture) + "' was asked for '" + match.Value.Name + "' titles"
              });

            withinFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, withinFilter,
                new RelationalFilter(EpisodeAspect.ATTR_SEASON, RelationalOperator.EQ, season.Value));
          }
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
              Error = "this endpoint can pick titles out by genre, year, actors, directors, writers and "
                  + "collections; '" +
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

        // What the signed-in person is allowed to be shown at all. A grown-up or an anonymous request changes
        // nothing here; a child's session adds the store's own condition - the certification ceiling with its
        // parental-guided and unrated exceptions, and the share allowlist - to every other question asked, so
        // the forbidden titles are not filtered out of an answer but never looked up. The count below travels
        // through the same query, so "12 of 4 000" says how many of the twelve a child may actually see.
        IFilter allowed = CallerRestrictionFilter(necessary);
        if (allowed != null)
          asking = asking == null
              ? allowed
              : BooleanCombinationFilter.CombineFilters(BooleanOperator.And, asking, allowed);

        // What the words are put to. MP2's own BuildSimpleTextSearchQuery offers to ask every short text attribute
        // of every aspect the request touches, and measured against a real library that is the one question that
        // takes the server down: the OR over those attributes drags the multi-value tables - every actor, director,
        // writer, character and award row of every title - and three subqueries over the stored file paths into one
        // statement, and the database thought about it for 214 seconds on a copy of that library while every other
        // request queued behind it (36 seconds without the path branches; 0.06 for the title columns alone). A
        // search box asks for titles; the questions "who acts in it" and "which genre" have their own shelves
        // (by=actor, by=genre), and those stay fast. So the words go to the two title columns of the Media aspect,
        // which every shelf of this endpoint carries anyway - escaped for LIKE (a viewer typing an underscore
        // should get titles with underscores, not an empty shelf) and case-insensitive, because a remote control
        // has no shift key.
        MediaItemQuery query;
        if (finding == null)
        {
          query = new MediaItemQuery(necessary, optional, asking);
        }
        else
        {
          string pattern = "%" + SqlUtils.LikeEscape(finding, SEARCH_ESCAPE) + "%";
          IFilter words = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
              new LikeFilter(MediaAspect.ATTR_TITLE, pattern, SEARCH_ESCAPE, false),
              new LikeFilter(MediaAspect.ATTR_SORT_TITLE, pattern, SEARCH_ESCAPE, false));
          query = new MediaItemQuery(necessary, optional, asking == null
              ? words
              : BooleanCombinationFilter.CombineFilters(BooleanOperator.And, asking, words));
        }

        string sortBy = ReadSort(sort);
        // A favourites shelf keeps its own order, which is not the order of a plain list: the reference's
        // provider states it itself rather than leaving it to whoever asked, most plays first and among equals
        // the most recently played first (BaseFavoriteMediaListProvider.cs:46-57; a container first by how many
        // of its children the viewer played, :112-117). A caller naming a sort still gets the sort they named.
        List<ISortInformation> sorts = seeing == "favorite" && string.IsNullOrEmpty(sort)
            ? FavoriteSorts(viewer, aspectId)
            : BuildSorts(sortBy, order, insideOneAlbum);
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

        // A series is never paused and an album never stopped - the episode and the track are - so when the
        // caller asks which containers they left half way and does it as themselves, every row is asked one
        // question back: where did you last stop? The answer is the stored position of the most recently
        // started child, which is exactly what the reference's continue screens show - they hand out the last
        // watched episode itself (BaseContinueWatchRelationshipMediaListProvider.AllowDisplayTitleAsLastWatchedEpisode).
        // The MP2 library does keep a container's own percentage and date from reports that carry a viewer
        // (MediaLibrary.UpdateParentPlayUserData, reached from NotifyUserPlayback at :2862-2863, over every
        // relationship the child marks playable - which is how a series gets its own figure, not only its season),
        // but it keeps nothing for a title whose viewing was reported without a name, and a library marked
        // before that path worked holds nothing on any parent either; the child's own row is the one answer both
        // cases share, so the position is still read from there.
        if (askedChildrenHalfWay && profile.HasValue)
        {
          for (int row = 0; row < items.Count && row < titles.Count; row++)
          {
            if (titles[row].Resume.HasValue)
              continue;
            // The pair of roles goes child-first, exactly the way TryRelationship pairs them when the same
            // episodes are listed through "inside": the relationship row stands on the child and names the
            // parent, so the question "which episodes hang off this series" is ROLE_EPISODE against ROLE_SERIES
            // with the series' id - turned the other way round the row exists in no library and the answer is
            // silently nothing.
            IFilter within = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
                new RelationshipFilter(halfWayChildRole, halfWayParentRole, items[row].MediaItemId), halfWayChildren);
            // The Media aspect travels with the child aspect on purpose: the library reads a profile's playback
            // data onto every found item but strips the aspect - and with it the data - back off any item that
            // did not ask for it (MediaLibrary.Search, the two "has to be removed" passes), and the stored
            // position IS that data. Asked for, the child arrives wearing its resume; unasked, ReadResume would
            // find nothing to read and the row would stay empty forever.
            MediaItemQuery childQuery = new MediaItemQuery(new[] { halfWayChildAspect, MediaAspect.ASPECT_ID }, null, within);
            childQuery.SetSortInformation(BuildSorts("lastplayed", "desc"));
            childQuery.Limit = 1;
            IList<MediaItem> children = mediaLibrary.Search(childQuery, false, profile.Value, false);
            if (children != null && children.Count > 0)
              titles[row].Resume = ReadResume(children[0]);
          }
        }

        // The figure a container shows over its cover is the average of its children's figures, which is what the
        // reference displays (PlayableContainerMediaItem.cs:47-58): the library keeps that average on the parents
        // its play reports reach through playable relationship cells, but the episode→season cell an importer
        // writes is not one of those, so a season carries nobody's figure and its shelf would say nothing about
        // a half-watched season. Asked live, the children answer: every child of this row is read with the
        // caller's own data, untouched children count as nought, and the rounded mean becomes the row's figure.
        // Only a row whose own stored figure says nothing is asked - and a stored nought says nothing, because it
        // is what the rounding of a fraction left behind before the mean learned to keep a one. Only a container
        // with at least one child that holds a figure gets an average - a shelf nobody watched keeps its nothing,
        // which is the household answer's (PlayCount's) ground, not this one's.
        if (profile.HasValue)
        {
          Guid childAspect = Guid.Empty, childRole = Guid.Empty, parentRole = Guid.Empty;
          if (aspectId == SeasonAspect.ASPECT_ID)
          {
            childAspect = EpisodeAspect.ASPECT_ID;
            childRole = EpisodeAspect.ROLE_EPISODE;
            parentRole = SeasonAspect.ROLE_SEASON;
          }
          else if (aspectId == SeriesAspect.ASPECT_ID)
          {
            childAspect = EpisodeAspect.ASPECT_ID;
            childRole = EpisodeAspect.ROLE_EPISODE;
            parentRole = SeriesAspect.ROLE_SERIES;
          }
          else if (aspectId == AudioAlbumAspect.ASPECT_ID)
          {
            childAspect = AudioAspect.ASPECT_ID;
            childRole = AudioAspect.ROLE_TRACK;
            parentRole = AudioAlbumAspect.ROLE_ALBUM;
          }

          if (childAspect != Guid.Empty)
          {
            for (int row = 0; row < items.Count && row < titles.Count; row++)
            {
              if (titles[row].Percentage > 0)
                continue;
              // Child-first roles, as every relationship walk in this file spells them: the row stands on the
              // child and names this container.
              IFilter within = new RelationshipFilter(childRole, parentRole, items[row].MediaItemId);
              MediaItemQuery childQuery = new MediaItemQuery(new[] { childAspect, MediaAspect.ASPECT_ID }, null, within);
              IList<MediaItem> children = mediaLibrary.Search(childQuery, false, profile.Value, false);
              if (children == null || children.Count == 0)
                continue;
              int sum = 0;
              bool anyChildSaid = false;
              foreach (MediaItem child in children)
              {
                int? own = ReadPercentage(child);
                anyChildSaid |= own.HasValue;
                sum += own ?? 0;
              }
              if (anyChildSaid)
                titles[row].Percentage = MeanOfChildren(sum, children.Count);
            }
          }
        }

        // A series whose row says nothing about episodes is asked one question of its own: a tagger that never
        // wrote a total onto the show still left the library holding the episodes, and they hang off the series
        // by relationship - the same walk CountSeasonsOfSeries makes to count one series' seasons, stopped one
        // step short at a plain number. A row that already carries the tagged figure is not asked twice.
        if (aspectId == SeriesAspect.ASPECT_ID)
        {
          for (int row = 0; row < items.Count && row < titles.Count; row++)
          {
            if (titles[row].Episodes.HasValue)
              continue;
            int onShelf = CountEpisodesOfSeries(mediaLibrary, items[row].MediaItemId);
            if (onShelf > 0)
              titles[row].Episodes = onShelf;
          }
        }

        // An album whose own tag stayed silent about its track count is asked the same question the series'
        // block above asks: measured over the household's library the tag spoke on only 18 of 43 albums (see
        // ItemResult.TrackCount), and a shelf whose third column is blank on most of its rows is read as
        // broken, not as honest. The count is the library's own - the files that hang off the album - which is
        // the number the viewer decided a standing shelf answers with (see FillTheTracksInsideItsAlbum). A row
        // whose tag did speak is not asked twice, so a tagged shelf pays nothing, and the asking is bounded by
        // the page, the same bound the resume and percentage questions above stand under.
        if (aspectId == AudioAlbumAspect.ASPECT_ID)
        {
          for (int row = 0; row < items.Count && row < titles.Count; row++)
          {
            if (titles[row].TrackCount.HasValue)
              continue;
            int onShelf = CountTracksOfAlbum(mediaLibrary, items[row].MediaItemId);
            if (onShelf > 0)
              titles[row].TrackCount = onShelf;
          }
        }

        // A season shelf that came back with nothing in it is asked one more question before it is called empty,
        // because the commonest reason a series has no season titles is that its library was pointed straight at the
        // episode files: every one of those episodes knows its season number, and no title wears it (see
        // CountSeasonsOfSeries). Counting the numbers gives the same shelf in the same order, and each row says which
        // series and which number its own shelf has to be asked with, because a counted season has no id that means
        // anything outside this list.
        List<KeyValuePair<int, int>> counted = null;
        if (titles.Count == 0 && !season.HasValue && aspectId == SeasonAspect.ASPECT_ID && inside != Guid.Empty)
        {
          counted = CountSeasonsOfSeries(mediaLibrary, inside);
          if (counted.Count == 0)
            counted = null;
          else
          {
            titles = counted.Skip(skip).Take(take)
                .Select(group => CountedSeasonTitle(inside, insideName, group.Key, group.Value)).ToList();

            // The figure a counted season row shows is worked out of the very episodes that were just counted
            // into it, and asked once for the whole shelf instead of per row: these seasons have no id of their
            // own to hang a relationship on, but every episode names its series and its number, which is the
            // same two facts the counting read. Asked under a name, because a figure belongs to a person; the
            // law is the containers' (see the averaging above) - empty episodes count as nought, and a season
            // nobody of whose episodes holds a figure keeps the row's figure unnamed.
            if (profile.HasValue)
            {
              Dictionary<int, int> figures = AverageEpisodePercentagePerSeason(mediaLibrary, inside, profile.Value);
              foreach (TitleResult row in titles)
              {
                int figure;
                if (row.Season.HasValue && figures.TryGetValue(row.Season.Value, out figure))
                  row.Percentage = figure;
              }
            }
          }
        }

        // "how many are there" is asked through the same filter the list was read through, or a front end that
        // reached the bottom of a search would keep offering more of a list that has no more in it - and would say
        // "12 of 4 000" about a search that found twelve titles out of four thousand. The same answer about
        // invented titles goes in, because a count of a different shelf than the one being listed is worse than
        // no count at all.
        // A counted shelf is as long as the counting made it; the library's own count of the asked aspect would
        // answer "how many episodes", which is not the question the shelf asked.
        int total = counted != null
            ? counted.Count
            : mediaLibrary.CountMediaItems(new[] { aspectId }, query.Filter, false, invented);

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
    /// <c>genre</c>, <c>year</c>, <c>actors</c>, <c>directors</c>, <c>writers</c> or <c>collection</c> - the
    /// categories a shelf is grouped by. An unknown word is refused rather than guessed at: a browse screen that
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
          Error = "the library can be counted by genre, year, actors, directors, writers and collections; '" + by +
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

        // The same ceiling the title list obeys: a shelf counted for a child does not list a genre whose only
        // films start at eighteen, because the list under that heading would be empty and an empty shelf reads
        // as a hole in the library rather than as a rule of the house.
        IFilter allowed = CallerRestrictionFilter(necessary);
        HomogenousMap counted = mediaLibrary.GetValueGroups(spec, allowed, projection, necessary, null, false,
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
    /// The six categories a screen can group a shelf by: the five MP2's own browse tree was built from, in the
    /// order a browse screen lists them, and the collection a film names.
    /// </summary>
    /// <remarks>
    /// These are the attributes the importer writes <i>on the title itself</i> - genre rows on the
    /// <c>GenreAspect</c> and the credit name lists on the <c>VideoAspect</c>, the same source the details screen
    /// reads - so grouping them costs one query per question rather than the relationship walk the people and
    /// genre <i>items</i> would need. The year is not a stored column anywhere; it is taken from the recording
    /// time in the database itself, which is why it carries a projection and why the titles of one year are
    /// later asked for as a span of dates rather than an equality.
    /// <para>
    /// A collection is the sixth word and the one nobody browsed before: it is asked by the reading screen, which
    /// shows the films of the collection the title it reads belongs to. Like the four above it is a name written on
    /// the title - the <c>MovieAspect</c>'s collection name, the very text <see cref="ItemResult.Collection"/> is
    /// answered from - so asking which films carry a collection's name is one query and no relationship walk. That
    /// is also the honest way round in a library whose films were imported with their collection named but never
    /// linked: counted over a library of 1136 films, all 228 films naming a collection name one the library also
    /// holds as a title, while the film-to-collection relationship rows those two would be joined by number zero.
    /// A shelf that is not films simply answers nothing, exactly as <c>by=actors</c> does for a shelf of music.
    /// </para>
    /// </remarks>
    private static readonly Category[] CATEGORIES =
    {
        new Category("genre", GenreAspect.ATTR_GENRE, ProjectionFunction.None),
        new Category("year", MediaAspect.ATTR_RECORDINGTIME, ProjectionFunction.DateToYear),
        new Category("actors", VideoAspect.ATTR_ACTORS, ProjectionFunction.None),
        new Category("directors", VideoAspect.ATTR_DIRECTORS, ProjectionFunction.None),
        new Category("writers", VideoAspect.ATTR_WRITERS, ProjectionFunction.None),
        new Category("collection", MovieAspect.ATTR_COLLECTION_NAME, ProjectionFunction.None)
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
      /// The short films this title advertises itself with, or null when the library knows none. The same list
      /// the title row carries (<see cref="TitleResult.Trailers"/>), read the same way.
      /// </summary>
      public List<TrailerResult> Trailers { get; set; }

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

      /// <summary>
      /// How old the viewer has to be for this title, in years, or null when the mark names no number.
      /// What the library keeps is a mark - "US_TV14", "DE_FSK16" - and a screen printing that mark
      /// shows the viewer a code instead of the one thing the mark exists for. So the server reads the
      /// years off its own table (CertificationMapper, the same table the country translation is read
      /// from) and answers them here, which lets a screen say "16" and leave the mark at home. A mark
      /// asking for nothing answers "0" rather than nothing at all: "FSK 0" and "G" are verdicts the
      /// certificate handed down like any other, and a screen that fell silent on them would leave the
      /// viewer wondering whether the title was ever rated. Only a mark the table does not know, and a
      /// title carrying no mark at all, answer null.
      /// </summary>
      public int? Age { get; set; }

      /// <summary>The collection a film belongs to, e.g. "Alien Collection". Null for the rest.</summary>
      public string Collection { get; set; }

      /// <summary>
      /// The title the library holds the collection under, as an id. A screen that shows the collection asks for
      /// its pictures by this id, because MP2 files a collection as a title of its own and every picture of the
      /// library is reached over the title it belongs to. Null when the film belongs to no collection, and null
      /// when a collection is named on the film but the library has no title wearing that name - which is the
      /// importer's doing, not this endpoint's.
      /// </summary>
      public string CollectionId { get; set; }

      /// <summary>The library's rating for the title, on MP2's 0-10 scale. Null when nothing rated it.</summary>
      public int? Rating { get; set; }

      /// <summary>For an episode: the series it belongs to. Null for anything else.</summary>
      public string SeriesName { get; set; }

      /// <summary>
      /// What the series an episode or season stands under is about, lent to the answer only while the asked
      /// title carries no prose of its own (<see cref="Overview"/> and <see cref="StoryPlot"/> both null). A
      /// details screen may show it where the episode's own summary would otherwise say nothing - the series
      /// sentence is the honest next-best answer - and a screen that will not mix the two can leave it unread.
      /// Null when the title spoke for itself, when no series is linked, or when the series was never described.
      /// </summary>
      public string SeriesOverview { get; set; }

      public int? Season { get; set; }

      public int? Episode { get; set; }

      /// <summary>For a season: how many episodes the library counts inside it. Null for the rest.</summary>
      public int? Episodes { get; set; }

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

      /// <summary>
      /// How long the file runs, in whole seconds, when the title is an audio file. Said in seconds rather than in
      /// the minutes <see cref="RuntimeMinutes"/> carries because a song is not a rounding error away from nothing:
      /// three and a half minutes read "3 min" here and "03:26" the way the worn skin writes an audio length
      /// (<c>MediaInfoAudioDetails.inc:308-315</c>, <c>Duration</c> through the skin's own duration converter).
      /// <para>
      /// For an album this is the sum of the lengths of the tracks the library holds inside it
      /// (<see cref="FillTheTracksInsideItsAlbum"/>), because an album runs exactly as long as its record and the
      /// album aspect carries no length of its own.  How a screen spells either figure - a song as "03:26", an
      /// album as "1 h 45 min" - is the screen's business, which is why both travel as seconds.
      /// </para>
      /// </summary>
      public int? DurationSeconds { get; set; }

      /// <summary>
      /// What the audio file is packed in, in the importer's own words ("MPEG Version 1 Audio, Layer 3", "Flac
      /// Audio"), or null for a title that is not an audio file. The worn skin asked for exactly this string: its
      /// codec plaque was <c>Medialogos\audio\{0}.png</c> bound to <c>Encoding</c>
      /// (<c>MediaInfoAudio.xaml:118-126</c>), so the words a front end needs are the words the library wrote.
      /// </summary>
      public string AudioEncoding { get; set; }

      /// <summary>How many channels the file carries, or null when the importer counted none.</summary>
      public int? AudioChannels { get; set; }

      /// <summary>Its bitrate in kilobits per second, or null when the importer wrote none.</summary>
      public int? AudioBitRate { get; set; }

      /// <summary>Its sample rate in hertz, or null when the importer wrote none.</summary>
      public long? AudioSampleRate { get; set; }

      /// <summary>
      /// Which disc of a set this file is, or null for a single disc the library never counted. Pairs with
      /// <see cref="DiscCount"/> the way the worn skin paired them ("<c>{DiscId} | {NumDiscs}</c>" beside a picture
      /// of a disc, <c>MediaInfoAudio.xaml:139-148</c>).
      /// </summary>
      public int? DiscId { get; set; }

      /// <summary>How many discs the set has, or null when the library did not count them.</summary>
      public int? DiscCount { get; set; }

      /// <summary>
      /// How many tracks the library holds <b>inside</b> this album, or null for a title that holds no tracks.
      /// </summary>
      /// <remarks>
      /// <para>
      /// Counted from the titles the library actually has, which is not the same number the title list answers
      /// under the same name: <see cref="TitleResult.TrackCount"/> repeats what the file's own tag wrote
      /// (<c>AudioAlbumAspect.ATTR_NUMTRACKS</c>), and a tag that was written for the ordinary pressing keeps
      /// silent about the bonus disc of the special edition.  Measured over the household's library, the tag spoke
      /// on 18 of 43 albums, and where it spoke it could still be short - the shelved copy of "Born This Way
      /// (Special Edition)" holds 23 files while its tag counts 17.
      /// </para>
      /// <para>
      /// The title list answers with the tag's number where the tag spoke, and with the counted children where
      /// it stayed silent (the album block in the listing, next to the series' episode question): a count per
      /// row costs the library one walk of a relationship the same request already walks for the resume and the
      /// figure questions, and it is bounded by the page rather than by the wall.
      /// </para>
      /// </remarks>
      public int? TrackCount { get; set; }

      /// <summary>
      /// Who sings this track, in the file's own tag, or null for a title the library holds no name for.  Only an
      /// audio file is ever asked: the name is read off the aspect the audio figures come from
      /// (<see cref="AudioAspect.ATTR_ARTISTS"/>), which a film's title does not carry.
      /// </summary>
      /// <remarks>
      /// One name and not the list, for the same reason as <see cref="TitleResult.Artist"/>: a song credited to a
      /// band, a producer and a featured singer is one line under a cover, and MP2 keeps the names on the title
      /// itself, so saying them costs no relationship query.
      /// </remarks>
      public string Artist { get; set; }

      /// <summary>
      /// The record this track is on (<see cref="AudioAspect.ATTR_ALBUM"/>), or null when the tag stayed silent.
      /// </summary>
      public string Album { get; set; }

      /// <summary>
      /// The name the record as a whole is credited to (<see cref="AudioAspect.ATTR_ALBUMARTISTS"/>), which is the
      /// singer of a various-artists compilation rather than the one of this track.  Null when the tag wrote no
      /// such name, which is most files: the importer fills it for albums whose tag carried it.
      /// </summary>
      public string AlbumArtist { get; set; }

      /// <summary>Set when the answer is empty for a reason the caller can act on.</summary>
      public string Error { get; set; }
    }

    /// <summary>
    /// Everything this server knows about one title, by its id.
    /// </summary>
    /// <param name="id">The media item id, as <c>GET api/media/titles</c> reported it.</param>
    /// <param name="season">
    /// A season number to ask about one season of that title rather than the title itself, for the seasons a library
    /// holds as nothing but a number written on its episodes: the shelf of them hands out rows that say which series
    /// to ask and which number to ask it with (<see cref="TitleResult.ParentId"/>). A title that is a season of its
    /// own is answered as itself either way, because its id already says which season is meant.
    /// </param>
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
    public ActionResult<ItemResult> GetItem(Guid id, [FromQuery] int? season = null)
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
          SeasonAspect.ASPECT_ID,
          // Asked for here because this is the one answer that is read for ONE title rather than for a shelf: an
          // audio file's own details - its codec, its channels, its two rates, its disc - live on this aspect and
          // nowhere else, and the title list cannot carry them for every row of a thousand-track wall to buy a
          // reading that is looked at once.  The worn skin read the same aspect for the same screen
          // (<c>MediaInfoAudio.xaml:115</c>, an <c>AudioAspectWrapper</c> over the item).
          AudioAspect.ASPECT_ID,
          // Asked for because it is what tells an album from everything else, and only an album is sent asking its
          // tracks for the packing of the record (<see cref="FillAudioOfTheFirstTrackOfItsAlbum"/>).  Optional like
          // the rest: a film is not an album and must still get its answer.
          AudioAlbumAspect.ASPECT_ID,
          // Asked for because this is the one answer read for ONE title, and the kinds of story a title is told in
          // are a fact a viewer reads there: the title list already names them (it adds the aspect to its own
          // reading whenever the shelf has any, <see cref="GetTitles"/>), while a details answer that left the
          // aspect out could only ever say "genres: none" about a title whose shelf had been naming its genre a
          // moment before.  Optional like the rest, because a title no importer ever put a genre on is a title and
          // not a failure.
          GenreAspect.ASPECT_ID,
          // Asked for because the "Trailer" entry of a title's menu is decided by whether this answer names any,
          // and a screen should not have to ask a second question to find out what the title it is standing in
          // front of advertises itself with.
          TrailerAspect.ASPECT_ID
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

        // One season of a series asked by number, which is how the seasons a library holds as nothing but a number
        // written on its episodes are asked about at all. Answered in the same shape as a season the library holds -
        // which series, which number, how much inside - from the episodes it does hold.
        if (season.HasValue)
        {
          ActionResult<ItemResult> counted = CountedSeasonItem(mediaLibrary, item, season.Value);
          if (counted != null)
            return counted;
        }

        ItemResult result = ToItem(item, profile.HasValue);
        FillMissingDetailsFromFiles(item, result);
        FillMissingOverviewFromSeries(mediaLibrary, item, result, profile);
        FillCollectionTitle(mediaLibrary, result, profile);
        // An album keeps its packing on the tracks inside it, so its answer borrows the figures of the track that
        // leads the record.  Asked last: every question above is about the title itself, this one is about its
        // children, and a title that can answer for itself has already been answered by the time it is reached.
        FillAudioOfTheFirstTrackOfItsAlbum(mediaLibrary, item, result, profile);

        // How many tracks the album holds and how long they play together, which is the second question about its
        // children.  Asked after the packing above because that one reads one track and can answer from the first,
        // while this one walks all of them - and an album whose packing is settled should not wait for the walk to
        // say what it is packed in.
        FillTheTracksInsideItsAlbum(mediaLibrary, item, result, profile);

        // Translated last, because the file beside a title can be the only place its certificate is written down,
        // and a household that asked for its own country's mark should get it whichever of the two sources spoke.
        result.Certification = ShownCertification(item, result.Certification);
        // Asked of the word that now stands in the answer rather than of the one the library keeps: when the
        // household had its marks translated, the translation picks its own country's nearest mark, and the
        // screen must show the years that go with the mark it is showing.
        result.Age = ShownAge(item, result.Certification);
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

      // A show keeps its own name on its own aspect, and the importers keep that name free of the year, while the
      // Media aspect's title carries the same name with the year appended ("Alien: Earth (2025)"). The reference's
      // cover-details templates bound that yearless name (SeriesAspectWrapper.SeriesName), so it is the one this
      // answer has to hand over; nothing else on the item says the name the way the reference spelled it.
      SingleMediaItemAspect show;
      if (result.SeriesName == null
          && MediaItemAspect.TryGetAspect(item.Aspects, SeriesAspect.Metadata, out show))
        result.SeriesName = Text(show.GetAttributeValue<string>(SeriesAspect.ATTR_SERIES_NAME));

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

      // A season keeps its own prose, its own number and the number of episodes it holds, and none of them are on
      // the media aspect: MP2 writes them onto the season aspect (SeasonInfo.SetMetadata:239-243), which is also
      // what the reference's season panels bound (MediaItemCoverSeasonDetails.inc through SeasonAspectWrapper).
      // The description goes into the answer's "overview", which is the field a screen reads as the title's prose.
      SingleMediaItemAspect seasonAspect;
      if (MediaItemAspect.TryGetAspect(item.Aspects, SeasonAspect.Metadata, out seasonAspect))
      {
        result.SeriesName = result.SeriesName
            ?? Text(seasonAspect.GetAttributeValue<string>(SeasonAspect.ATTR_SERIES_NAME));

        int season = seasonAspect.GetAttributeValue<int>(SeasonAspect.ATTR_SEASON);
        if (season > 0)
          result.Season = season;

        int episodes = seasonAspect.GetAttributeValue<int>(SeasonAspect.ATTR_NUM_EPISODES);
        if (episodes > 0)
          result.Episodes = episodes;

        if (result.Overview == null)
          result.Overview = Text(seasonAspect.GetAttributeValue<string>(SeasonAspect.ATTR_DESCRIPTION));
      }

      // The kinds of story this title is told in, read the same way the title list reads them - see
      // <see cref="ReadGenres"/>.  Measured over the household's library: every one of its 43 albums names its
      // genre on the shelf, and the album's own details answer named nothing, because the aspect was not asked for.
      result.Genres = ReadGenres(item);

      // The short films the title advertises itself with, read the same way the title rows read them.
      result.Trailers = ReadTrailers(item);

      // An audio file's own facts, which the audio reading is built from: what it is packed in, how many channels
      // it carries, its bitrate and sample rate, and which disc of how many it is.  Every one of them is the
      // library's own number rather than something this answer works out, and every one of them is a plaque on the
      // worn skin's audio screen (<c>MediaInfoAudio.xaml:115-170</c>, an <c>AudioAspectWrapper</c> whose six cells
      // are the codec picture, the channels picture, the disc pair, and the two rates written as "kbps" and "Hz").
      // Zero means the importer never wrote the fact, which is why each of them is asked for before it is set: a
      // screen is left to tell "no bitrate" from "a bitrate of nought" by itself otherwise.
      SingleMediaItemAspect audio;
      if (MediaItemAspect.TryGetAspect(item.Aspects, AudioAspect.Metadata, out audio))
      {
        WriteAudioFigures(audio, result, takesLength: true);

        // Who sings it and which record it came from, read off the very same aspect.  They are named here rather
        // than inside <see cref="WriteAudioFigures"/> because that method is also called for an album, from the
        // first track filed under it: an album that borrowed that track's singer would name a guest musician as
        // its own, which is exactly what the album's shelf gets right by reading its own aspect
        // (<see cref="AudioAlbumAspect.ATTR_ARTISTS"/>).
        result.Artist = Text(FirstText(audio.GetCollectionAttribute<string>(AudioAspect.ATTR_ARTISTS)));
        result.Album = Text(audio.GetAttributeValue<string>(AudioAspect.ATTR_ALBUM));
        result.AlbumArtist = Text(FirstText(audio.GetCollectionAttribute<string>(AudioAspect.ATTR_ALBUMARTISTS)));
      }

      // An album's own prose, which a tag reader writes on the album aspect and nowhere else.  The reference drew
      // it on the album's screens (MediaItemListAudioAlbumDetails.inc:307, MediaItemCoverAudioAlbumDetails.inc:7)
      // and drew NOTHING when the column was empty - which is the half of the rule a placeholder would break: an
      // album without a description is an album nobody described, not an album this server has nothing to say
      // about.  Only asked where the title's own comment stayed silent, the same order the season's description
      // takes above.
      SingleMediaItemAspect albumAspect;
      if (result.Overview == null
          && MediaItemAspect.TryGetAspect(item.Aspects, AudioAlbumAspect.Metadata, out albumAspect))
        result.Overview = Text(albumAspect.GetAttributeValue<string>(AudioAlbumAspect.ATTR_DESCRIPTION));

      result.Stub = item.IsStub;
      result.Path = ReadPath(item);
      return result;
    }

    /// <summary>
    /// The kinds of story a title is told in, or <c>null</c> when the library never wrote one.
    /// </summary>
    /// <remarks>
    /// The genre aspect is one record per genre - a ManyToMany aspect, which is also why the by=genre shelf can
    /// point at a single value. The names come in the importer's order and keep it; a repeated word is said once.
    /// <para>
    /// One reading for both endpoints on purpose. The shelf of albums names its genre and the album's own details
    /// answer did not, which is two screens of the same library disagreeing about one title over nothing but which
    /// aspect each of them happened to ask for.
    /// </para>
    /// </remarks>
    private static List<string> ReadGenres(MediaItem item)
    {
      IList<MultipleMediaItemAspect> genreAspects;
      if (!MediaItemAspect.TryGetAspects(item.Aspects, GenreAspect.Metadata, out genreAspects))
        return null;

      List<string> genres = genreAspects
          .Select(genre => Text(genre.GetAttributeValue<string>(GenreAspect.ATTR_GENRE)))
          .Where(genre => genre != null)
          .Distinct()
          .ToList();
      return genres.Count > 0 ? genres : null;
    }

    /// <summary>
    /// The trailers the library knows for one title, in the order a screen should list them, or <c>null</c> when
    /// it knows none.
    /// </summary>
    /// <param name="item">The title. Its trailer aspect must have been asked for, like every other aspect here.</param>
    /// <remarks>
    /// <para>
    /// The address a tagging tool wrote is translated here and not when it was imported, so that a title
    /// standing in a library since last year answers with whatever this server can make of its trailer today.
    /// The translation is at <see cref="TrailerAddress"/>; what is handed over along with it is the kind, which
    /// the importer already worked out and which is what a choice on a screen is labelled by.
    /// </para>
    /// <para>
    /// A trailer the library holds as a file is <b>not</b> handed over as a path, because a path on the machine
    /// holding the library means nothing to the machine asking, and one answer that works everywhere is worth
    /// more than a fast path for the one front end standing next to the server. Such a trailer is reached
    /// through <see cref="GetTrailerStream"/>, exactly as the film itself is reached through
    /// <see cref="GetStream"/>, and the index in that address is the position in this list - which is why the
    /// list is sorted rather than left in whatever order the database happened to return it in.
    /// </para>
    /// </remarks>
    private static List<TrailerResult> ReadTrailers(MediaItem item)
    {
      IList<MultipleMediaItemAspect> trailerAspects;
      if (!MediaItemAspect.TryGetAspects(item.Aspects, TrailerAspect.Metadata, out trailerAspects))
        return null;

      List<TrailerResult> trailers = trailerAspects
          .Select(trailer => new TrailerResult
          {
            Kind = trailer.GetAttributeValue<int>(TrailerAspect.ATTR_KIND),
            Label = Text(trailer.GetAttributeValue<string>(TrailerAspect.ATTR_LABEL)),
            Source = Text(trailer.GetAttributeValue<string>(TrailerAspect.ATTR_URL))
          })
          .Where(trailer => trailer.Source != null)
          .OrderBy(trailer => trailer.Source, StringComparer.OrdinalIgnoreCase)
          .ToList();
      if (trailers.Count == 0)
        return null;

      for (int index = 0; index < trailers.Count; index++)
      {
        TrailerResult trailer = trailers[index];
        trailer.Address = trailer.Kind == TrailerAspect.KIND_FILE
            // The server's own address for the file, spelled the way this server spells the address of a film
            // (<c>api/media/items/{id}/stream</c>), so that a front end opens the two the same way. The position
            // in this list is the number in it, which is why the list above is sorted.
            ? "api/media/items/" + item.MediaItemId.ToString("D") + "/trailer/" + index + "/stream"
            : TrailerAddress.ToPlayable(trailer.Source, trailer.Kind);
      }
      return trailers;
    }

    /// <summary>
    /// The six figures an audio aspect carries, written onto a details answer.
    /// </summary>
    /// <param name="takesLength">
    /// Whether the answer may take the file's length too.  <c>true</c> for the title that <b>is</b> the file;
    /// <c>false</c> when an album borrows the figures of one of its tracks, because an album is not as long as its
    /// first song - its length is the sum of everything inside it, which nothing here has read.
    /// </param>
    /// <remarks>
    /// Zero means the importer never wrote the fact, which is why each of them is asked for before it is set: a
    /// screen is left to tell "no bitrate" from "a bitrate of nought" by itself otherwise.
    /// </remarks>
    private static void WriteAudioFigures(SingleMediaItemAspect audio, ItemResult result, bool takesLength)
    {
      if (takesLength)
      {
        long seconds = audio.GetAttributeValue<long>(AudioAspect.ATTR_DURATION);
        if (seconds > 0)
          result.DurationSeconds = (int) seconds;
      }

      result.AudioEncoding = Text(audio.GetAttributeValue<string>(AudioAspect.ATTR_ENCODING));

      int channels = audio.GetAttributeValue<int>(AudioAspect.ATTR_CHANNELS);
      if (channels > 0)
        result.AudioChannels = channels;

      int bitRate = audio.GetAttributeValue<int>(AudioAspect.ATTR_BITRATE);
      if (bitRate > 0)
        result.AudioBitRate = bitRate;

      long sampleRate = audio.GetAttributeValue<long>(AudioAspect.ATTR_SAMPLERATE);
      if (sampleRate > 0)
        result.AudioSampleRate = sampleRate;

      int discId = audio.GetAttributeValue<int>(AudioAspect.ATTR_DISCID);
      if (discId > 0)
        result.DiscId = discId;

      int discCount = audio.GetAttributeValue<int>(AudioAspect.ATTR_NUMDISCS);
      if (discCount > 0)
        result.DiscCount = discCount;
    }

    /// <summary>
    /// The packing of an album's first track, lent to the album's own answer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why an album has to borrow at all.</b> The codec, the loudspeakers and the two rates are written on the
    /// <i>track's</i> audio aspect, and an album does not carry that aspect - it carries the album aspect, whose
    /// columns are the album's name, its artists and its counts.  Measured over the household's library (43 albums,
    /// 1018 tracks): every album answered with nothing at all, so the audio screen of an album had no plaque to
    /// show and stood there silent while the same file, opened as a track, named itself at length.
    /// </para>
    /// <para>
    /// <b>Why the first track answers for the whole album.</b> The viewer asked for it, and the library supports the
    /// reading: an album's tracks are one session pressed to one disc each, so the packing of the first is the
    /// packing of the record.  The reference sorted an album's tracks by disc and number before drawing them
    /// (<c>AudioSortByAlbumTrack</c>), which is the same track this question asks for, and its audio screen read
    /// the six figures out of one audio aspect and no more (<c>MediaInfoAudio.xaml:115-170</c>).
    /// </para>
    /// <para>
    /// Asked as one question for one track and not as a list, because the answer is a single file's packing; the
    /// album's own track list is a different question, asked by the screen that shows it.
    /// </para>
    /// </remarks>
    private static void FillAudioOfTheFirstTrackOfItsAlbum(IMediaLibrary mediaLibrary, MediaItem item,
        ItemResult result, Guid? profile)
    {
      // Only an album asks this question, and only while it has nothing of its own to say - the guard that keeps a
      // library which did file a track's aspect on its album from being talked over.
      if (!MediaItemAspect.TryGetAspect(item.Aspects, AudioAlbumAspect.Metadata, out _))
        return;
      if (result.AudioEncoding != null || result.AudioChannels != null || result.AudioBitRate != null
          || result.AudioSampleRate != null || result.DiscCount != null)
        return;

      try
      {
        // The tracks inside this album: the library files the relationship row on the <i>child</i>, naming the
        // album it hangs on, so the question is "which titles play ROLE_TRACK against this album's id" - the same
        // walk the titles endpoint makes for "titles inside an album" (its <c>inside</c> parameter,
        // <see cref="TryRelationship"/>).  The aspect named first is the one the found item has to carry, which is
        // what makes the answer a sound file rather than a row.
        Guid role;
        Guid linkedRole;
        if (!TryRelationship(AudioAspect.ASPECT_ID, item, out role, out linkedRole))
          return;

        MediaItemQuery first = new MediaItemQuery(
            new List<Guid> { AudioAspect.ASPECT_ID }, new List<Guid> { MediaAspect.ASPECT_ID },
            new RelationshipFilter(role, linkedRole, item.MediaItemId));
        first.SetSortInformation(new List<ISortInformation>
        {
          new AttributeSortInformation(AudioAspect.ATTR_DISCID, SortDirection.Ascending),
          new AttributeSortInformation(AudioAspect.ATTR_TRACK, SortDirection.Ascending)
        });
        first.Limit = 1;

        IList<MediaItem> held = mediaLibrary.Search(first, false, profile, VirtualTitles.LetsVirtualTitlesIn);
        MediaItem track = held == null ? null : held.FirstOrDefault();
        SingleMediaItemAspect audio;
        if (track == null || !MediaItemAspect.TryGetAspect(track.Aspects, AudioAspect.Metadata, out audio))
          return;

        // Not the length: the album's first song is four minutes long, the album is not.
        WriteAudioFigures(audio, result, takesLength: false);
      }
      catch (IllegalCallException)
      {
        // A library that will not say which track lies inside the album leaves the album as it found it - without
        // figures, which is what every album's answer used to be.
      }
    }

    /// <summary>
    /// How many tracks an album holds inside itself and how long they play together.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why the album has to be asked of its children.</b> An album carries the album aspect, whose columns are
    /// its name, its artists and the numbers its tag wrote; the length of a minute of music lives on a track's
    /// audio aspect and nowhere else.  Measured over the household's library (43 albums, 1018 tracks): every one of
    /// the 1018 tracks answered a duration, so the length of every one of the 43 albums is known - it is only never
    /// added up, because no screen had asked an album how long it is.
    /// </para>
    /// <para>
    /// <b>Why the count counts the files and not the tag.</b> The viewer decided it: a shelf standing in front of
    /// 23 files answers "23", and the 17 the tag of a special edition's ordinary pressing remembers is said by the
    /// title list, which reads the tag because it cannot count a thousand albums' children to fill one wall
    /// (<see cref="TitleResult.TrackCount"/>).
    /// </para>
    /// <para>
    /// Asked as one question for the whole album rather than as one per track. The largest album on this library's
    /// shelves holds 26 tracks and the question is asked about one title at a time, so the answer costs one walk of
    /// the relationship the shelf of tracks already walks.
    /// </para>
    /// </remarks>
    private static void FillTheTracksInsideItsAlbum(IMediaLibrary mediaLibrary, MediaItem item, ItemResult result,
        Guid? profile)
    {
      // Only an album asks this question. A film, a season, an episode: none of them holds tracks, and none of
      // them pays for a relationship query that answers nothing.
      if (!MediaItemAspect.TryGetAspect(item.Aspects, AudioAlbumAspect.Metadata, out _))
        return;

      try
      {
        Guid role;
        Guid linkedRole;
        if (!TryRelationship(AudioAspect.ASPECT_ID, item, out role, out linkedRole))
          return;

        // The audio aspect is the necessary one, which is what makes the answer a sound file rather than a row:
        // the same walk the "titles inside an album" list makes (its <c>inside</c> parameter).  Unsorted and
        // unlimited - a count and a sum do not care about the order, and an album missing from the count would be
        // an album lying about its length.
        MediaItemQuery tracks = new MediaItemQuery(
            new List<Guid> { AudioAspect.ASPECT_ID }, new List<Guid> { MediaAspect.ASPECT_ID },
            new RelationshipFilter(role, linkedRole, item.MediaItemId));

        IList<MediaItem> held = mediaLibrary.Search(tracks, false, profile, VirtualTitles.LetsVirtualTitlesIn);
        if (held == null || held.Count == 0)
          return;

        // Counted from what the library actually handed over as a track rather than from the number of rows it
        // answered with: an album whose count and length come from different lists of titles would be an album
        // saying two different things about itself in one answer.
        int count = 0;
        long seconds = 0;
        foreach (MediaItem track in held)
        {
          SingleMediaItemAspect audio;
          if (!MediaItemAspect.TryGetAspect(track.Aspects, AudioAspect.Metadata, out audio))
            continue;
          count++;
          seconds += audio.GetAttributeValue<long>(AudioAspect.ATTR_DURATION);
        }

        if (count == 0)
          return;

        result.TrackCount = count;
        if (seconds > 0)
          result.DurationSeconds = (int) seconds;
      }
      catch (IllegalCallException)
      {
        // Said nothing, as everywhere else in this method: an album whose library will not name its tracks keeps
        // its length to itself, which is a quieter answer than a wrong one.
      }
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
    /// <param name="width">
    /// How wide the caller paints the picture, in its own pixels. A caller that says nothing gets the file as the
    /// library holds it, which is how every front end that existed before this question behaves. A caller that does
    /// say gets the server's resized copy of it - made once for the whole house out of
    /// <see cref="FanArtVariants"/> - because a fanart that is two thousand pixels wide is a bad thing to drag
    /// across the house and stand a decoder in front of when somebody is going to paint it three hundred pixels
    /// wide. See <see cref="GetFanArtOfType"/> for the same question on every other kind.
    /// </param>
    [HttpGet("items/{id}/fanart")]
    public IActionResult GetFanArt(Guid id, [FromQuery] int? width = null)
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
      byte[] picture = FanArtVariants.Worn.Sized(System.IO.File.ReadAllBytes(path), width ?? 0);
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
    /// <param name="season">
    /// A season number to ask for the picture one season of this title wears, which is how the pictures of the seasons
    /// a library holds as nothing but a number on its episodes are reached: the files beside the media are named after
    /// that number (<c>season01-poster.jpg</c>, <c>season-specials-poster.jpg</c>,
    /// <c>season-all-banner.jpg</c>), and the number also picks the episode the folders are walked from, so that a
    /// library filed one folder per season starts inside it. The library's own stored pictures are not consulted for
    /// such a question - they belong to the title the id names, not to one season of it.
    /// </param>
    /// <param name="width">
    /// How wide the caller paints the picture, in its own pixels, and nought or nothing at all for "as the library
    /// holds it". A caller that says a number gets the server's resized copy of the picture out of
    /// <see cref="FanArtVariants"/>, which is the reference's own arrangement: its client asked
    /// <c>&amp;width=&amp;height=</c> of <c>FanartService</c> and the server kept a folder of resized copies so a
    /// size was made once rather than once per viewer per screen. A backdrop asked at twelve hundred and a tile
    /// asked at two hundred and fifty six are two files on the shelf and one read of the original.
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
    public IActionResult GetFanArtOfType(Guid id, string type, [FromQuery] int? season = null,
        [FromQuery] int? width = null)
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
        // A season asked by number names no title that anything could have filed pictures under, so the two lookups
        // that read the library's own pictures are skipped for it: whatever they hold belongs to the series the id
        // names, and answering season three with the series' poster is the mistake the number was named to avoid.
        // The walk through the folders is the one that knows season-numbered file names.
        IFanArtCache cache = ServiceRegistration.Get<IFanArtCache>(false);
        IList<string> cached = season.HasValue ? null : cache?.GetFanArtFiles(id, fanArtType);
        byte[] picture = FirstReadablePicture(cached);

        // A track wears its album's cover, and the import stored that cover once, under the album's id. Only the
        // two names for a front picture make the detour: a track asked for a banner stays a 404, because
        // stretching an album's square into a banner frame is the lie this endpoint was added to prevent.
        //
        // It is asked before the folder is walked, and that order is the whole point: the track lives in the
        // album's folder, so walking it would answer a track with whatever <c>folder.jpg</c> happens to lie there
        // while the album itself answers with the picture its files carry. Two tiles of one album would then wear
        // two different pictures. The cache is where the chosen picture lies, for an album as for its tracks.
        if (picture == null && !season.HasValue &&
            (fanArtType == FanArtTypes.Cover || fanArtType == FanArtTypes.Poster))
          picture = FirstReadablePicture(AlbumFanArtFiles(id, fanArtType, FanArtTypes.Cover));

        // Music calls its front picture a cover, but the files a library holds it in are called
        // "folder.jpg" and "cover.jpg", which are the names the reference lists for a poster - which is also
        // why the reference renames a poster to a cover as soon as it knows the item is an album.
        string folderKind = fanArtType == FanArtTypes.Cover ? FanArtTypes.Poster : fanArtType;
        if (picture == null)
          picture = LocalFanArtSearch.Worn.Picture(id, folderKind, season);

        if (picture == null || picture.Length == 0)
        {
          return NotFound(new ItemResult
          {
            Id = id,
            Error = "this server has no " + fanArtType.ToLowerInvariant() + " for the id " + id
          });
        }

        picture = FanArtVariants.Worn.Sized(picture, width ?? 0);
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
    /// <para>
    /// <b>Asking for one kind of title.</b> <c>?aspect=AudioItem</c> limits the pass to the titles carrying that
    /// aspect. A pass over everything is right after an import and wrong after a retag of the music: a library of
    /// a few thousand titles takes a minute of background time here, and on the library this was measured on -
    /// twenty-odd thousand media items, forty-odd albums among them - walking all of them to refresh a few dozen
    /// album covers is an hour for a job of seconds.
    /// </para>
    /// </remarks>
    [HttpPost("fanart/warming")]
    public ActionResult<FanArtWarmingResult> WarmFanArt([FromQuery] string aspect = null)
    {
      if (string.IsNullOrEmpty(aspect))
      {
        bool startedWhole = FanArtWarmingPass.Worn.StartAfterImport();
        FanArtWarmingResult whole = FanArtWarmingPass.Worn.Read();
        return startedWhole ? Accepted(whole) : (ActionResult<FanArtWarmingResult>) Ok(whole);
      }

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      IDictionary<Guid, MediaItemAspectMetadata> metadata = mediaLibrary.GetManagedMediaItemAspectMetadata();
      KeyValuePair<Guid, MediaItemAspectMetadata> match = metadata.FirstOrDefault(entry =>
          entry.Value != null && string.Equals(entry.Value.Name, aspect, StringComparison.OrdinalIgnoreCase));
      if (match.Value == null)
        return BadRequest(new FanArtWarmingResult
        {
          Error = "this server has no aspect called '" + aspect + "'; name one it has, e.g. AudioItem, MovieItem"
              + " - see GET api/library/aspects"
        });

      bool startedScoped = FanArtWarmingPass.Worn.Start(match.Key, match.Value.Name);
      FanArtWarmingResult scoped = FanArtWarmingPass.Worn.Read();
      return startedScoped ? Accepted(scoped) : (ActionResult<FanArtWarmingResult>) Ok(scoped);
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
    /// How old the viewer has to be for the mark this household is told about, in years, or null when the mark
    /// names no number.
    /// </summary>
    /// <param name="item">The title, whose aspects say whether it is a film or something of a series.</param>
    /// <param name="shown">The certificate as it is being answered, which <see cref="ShownCertification"/> picked.</param>
    /// <remarks>
    /// What the library keeps is a mark - "US_TV14" - and a screen printing that mark shows the viewer a code
    /// instead of the fourteen years the code stands for, so the years are read off the table the country
    /// translation is read from too. Asked of the SHOWN mark rather than of the stored one: a household that had
    /// its marks translated gets the nearest mark of its own country, and the number has to go with the mark it
    /// is actually shown. A mark the table does not know answers null, for the same reason the translation
    /// refuses to guess - a screen inventing an age would forbid titles the certificate never forbade. A mark
    /// that asks for nothing ("DE_FSK0", "US_G") answers "0" and is shown as such: it is a verdict the
    /// certificate handed down like any other, and a screen falling silent on it would leave the viewer
    /// wondering whether the title was ever rated at all.
    /// </remarks>
    private static int? ShownAge(MediaItem item, string shown)
    {
      if (string.IsNullOrEmpty(shown) || item == null || item.Aspects == null)
        return null;

      CertificationMapping mark;
      bool known = item.Aspects.ContainsKey(MovieAspect.ASPECT_ID)
          ? CertificationMapper.TryFindMovieCertification(shown, out mark)
          : CertificationMapper.TryFindSeriesCertification(shown, out mark);
      return known ? mark.AllowedAge : (int?)null;
    }

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
    /// Lends an episode or season the prose of the series it stands under, for as long as the title carries
    /// none of its own - as <see cref="ItemResult.SeriesOverview"/>, so the answer never claims the series'
    /// sentence is the episode's plot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An importer that numbers episodes but never writes their summaries is ordinary (the tools that name a
    /// season's pictures leave the episode files bare), and the details panel then says "nothing was written"
    /// under a show the library describes at length one shelf up. The viewer's law is that the series' sentence
    /// is the better answer than silence - and only the server can give it: the episode aspect holds the
    /// series' <em>name</em> and no id, while the link itself lives in the relationship the importer laid
    /// (<c>EpisodeAspect.ROLE_EPISODE</c> to <c>SeriesAspect.ROLE_SERIES</c>), which this walks backwards.
    /// </para>
    /// <para>
    /// A season asked by number of its series (<see cref="CountedSeasonItem"/>) never comes here - that answer
    /// is built with the series' item already in hand and lends its prose where it is made.
    /// </para>
    /// </remarks>
    private static void FillMissingOverviewFromSeries(IMediaLibrary mediaLibrary, MediaItem item,
        ItemResult result, Guid? profile)
    {
      // A title with prose of its own speaks for itself; the series is never asked a second opinion.
      if (result.Overview != null || result.StoryPlot != null)
        return;

      Guid linkedRole;
      if (MediaItemAspect.TryGetAspect(item.Aspects, EpisodeAspect.Metadata, out _))
        linkedRole = EpisodeAspect.ROLE_EPISODE;
      else if (MediaItemAspect.TryGetAspect(item.Aspects, SeasonAspect.Metadata, out _))
        linkedRole = SeasonAspect.ROLE_SEASON;
      else
        return;

      try
      {
        // The same walk the fanart search makes in reverse: instead of the episodes under a series, the one
        // series above this episode (or season). The aspect named first is the one the found items must carry.
        MediaItemQuery series = new MediaItemQuery(
            new List<Guid> { SeriesAspect.ASPECT_ID },
            new List<Guid> { MediaAspect.ASPECT_ID },
            new RelationshipFilter(SeriesAspect.ROLE_SERIES, linkedRole, item.MediaItemId));
        IList<MediaItem> held = mediaLibrary.Search(series, false, profile, VirtualTitles.LetsVirtualTitlesIn);
        MediaItem show = held == null ? null : held.FirstOrDefault();
        if (show == null)
          return;

        // The series' own overview is the comment on its Media aspect - the same field this endpoint reads as
        // every title's prose; the show aspect's description is what an importer leaves when it wrote no comment.
        SingleMediaItemAspect media;
        SingleMediaItemAspect aspect;
        result.SeriesOverview =
            (MediaItemAspect.TryGetAspect(show.Aspects, MediaAspect.Metadata, out media)
                ? Text(media.GetAttributeValue<string>(MediaAspect.ATTR_COMMENT))
                : null)
            ?? (MediaItemAspect.TryGetAspect(show.Aspects, SeriesAspect.Metadata, out aspect)
                ? Text(aspect.GetAttributeValue<string>(SeriesAspect.ATTR_DESCRIPTION))
                : null);
      }
      catch (IllegalCallException)
      {
        // A library that cannot answer the relationship question leaves the borrowing undone; the title's own
        // answer, thin as it is, stands.
      }
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
    /// One of a title's trailers, as a stream a player can watch.
    /// </summary>
    /// <param name="id">The media item the trailer belongs to.</param>
    /// <param name="number">Which of the title's trailers, counting as <see cref="ReadTrailers"/> lists them -
    /// which is the order the answer of <c>GET api/media/titles</c> shows them in, so a front end never has to
    /// count anything itself.</param>
    /// <param name="cancellationToken">Cancelled when the viewer walks away mid-trailer.</param>
    /// <remarks>
    /// <para>
    /// This is <see cref="GetStream"/> for the short film rather than for the film, and it exists for the same
    /// reason: the trailer of a library is a file on the machine holding the library, and a television in the
    /// living room cannot read that. It exists rather than reusing <see cref="GetStream"/> with the trailer's own
    /// id because a trailer has no id - it is not a title, and it should not turn up on a shelf as one.
    /// </para>
    /// <para>
    /// Only a trailer the library holds as a file is answered here. A trailer standing at a video site has
    /// nothing on this machine to open, and the answer of <c>GET api/media/titles</c> already names its address
    /// for the player; being asked to stream one is a front end that mixed the two up, and the answer says so.
    /// </para>
    /// <para>
    /// The address of the file is read out of the library and is never taken from the request, which is what
    /// makes this route safe to hand a number to: the largest set of files a caller can name is the set the
    /// importer wrote down, exactly as for the film itself.
    /// </para>
    /// </remarks>
    [HttpGet("items/{id}/trailer/{number}/stream")]
    [HttpHead("items/{id}/trailer/{number}/stream")]
    public async Task<IActionResult> GetTrailerStream(Guid id, int number, CancellationToken cancellationToken)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      IResourceAccessor accessor = null;
      bool streamClosesIt = false;

      try
      {
        MediaItemQuery query = new MediaItemQuery(
            new List<Guid> { MediaAspect.ASPECT_ID },
            new List<Guid> { TrailerAspect.ASPECT_ID, ProviderResourceAspect.ASPECT_ID },
            new MediaItemIdFilter(id));
        IList<MediaItem> items = mediaLibrary.Search(query, false, null, VirtualTitles.LetsVirtualTitlesIn);
        MediaItem item = items == null ? null : items.FirstOrDefault();

        if (item == null)
          return NotFound(new StreamResult { Id = id, Error = "this server has no title with the id " + id });

        List<TrailerResult> trailers = ReadTrailers(item);
        if (trailers == null)
          return NotFound(new StreamResult { Id = id, Error = "the title " + id + " has no trailer" });
        if (number < 0 || number >= trailers.Count)
          return NotFound(new StreamResult
          {
            Id = id,
            Error = "the title " + id + " has " + trailers.Count + " trailer" + (trailers.Count == 1 ? "" : "s") +
                ", so " + number + " is not one of them"
          });

        TrailerResult trailer = trailers[number];
        if (trailer.Kind != TrailerAspect.KIND_FILE)
        {
          // Nothing to open here: the trailer is somewhere on the internet, and the front end was handed the
          // address to take the player to.
          return NotFound(new StreamResult
          {
            Id = id,
            Error = "this trailer is not a file this server holds; its address is " +
                (trailer.Address ?? "'" + trailer.Source + "', which this server cannot play")
          });
        }

        accessor = OpenTrailerFile(item, trailer.Source);
        if (accessor == null)
          return FileGone(id, "'" + trailer.Source + "' is not a file the server can reach");

        IFileSystemResourceAccessor file = accessor as IFileSystemResourceAccessor;
        if (file == null)
          return FileGone(id, "the server cannot read the resource the trailer '" + trailer.Source + "' names");
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
        LogStreamRefusal("the media library refused a trailer request, answering 503: " + ex.Message);
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
        return FileGone(id, "the trailer the library recorded cannot be reached: " + ex.Message);
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
    /// Opens the file one of a title's trailers was written down as, or <c>null</c> when nothing any provider
    /// claims comes of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two spellings are worth understanding, because both are in the libraries this server is asked about. The
    /// importer writes a trailer it found in a film's folder as a resource address, which is what a file on a
    /// share is called here. A metadata file, on the other hand, sometimes names a trailer as a plain path -
    /// <c>extras/trailer.mp4</c> - which is neither an address nor rooted anywhere, and can only be understood
    /// against the folder of the title that named it. Both are tried, in that order.
    /// </para>
    /// <para>
    /// The second reading is the one that needs care: that path came out of a file somebody dropped into a
    /// folder, and a path written by a file is not a promise from the owner of the library. It is therefore
    /// asked through <see cref="IResourceAccessGuard.IsAllowed"/>, the same question the file server asks before
    /// it hands over bytes, and a trailer climbing out of every share is refused rather than opened.
    /// </para>
    /// </remarks>
    private static IResourceAccessor OpenTrailerFile(MediaItem item, string source)
    {
      if (string.IsNullOrWhiteSpace(source))
        return null;

      ResourcePath path = null;
      try
      {
        // How this server writes an address is recognisable by the marker between the provider and the path.
        if (source.IndexOf("://", StringComparison.Ordinal) >= 0)
          path = ResourcePath.Deserialize(source);
      }
      catch (ArgumentException)
      {
        // Not how this server writes an address. The reading against the title's folder below is for this case.
        path = null;
      }

      if (path == null)
      {
        path = TheTitleFolder(item);
        if (path == null)
          return null;
        // One step up from the title's own file is its folder, and inside that folder the metadata file's path
        // is read. A path climbing out of it with a ".." is not read at all: nothing a metadata file names has
        // any business leaving the folder of the title that named it.
        if (source.Split('/', '\\').Any(part => part == ".."))
          return null;
        foreach (string part in source.TrimStart('/', '\\').Split('/', '\\'))
        {
          if (part.Length == 0)
            continue;
          path = ResourcePathHelper.Combine(path, part);
        }
      }

      IResourceAccessGuard guard = ServiceRegistration.Get<IResourceAccessGuard>(false);
      if (guard != null && !guard.IsAllowed(path))
        return null;

      IResourceAccessor accessor;
      try
      {
        if (path.TryCreateLocalResourceAccessor(out accessor))
          return accessor;
      }
      catch (ArgumentException)
      {
        return null;
      }
      return null;
    }

    /// <summary>
    /// The folder a title's own file stands in, or <c>null</c> when the server cannot name the file at all.
    /// </summary>
    private static ResourcePath TheTitleFolder(MediaItem item)
    {
      IResourceLocator locator = item.GetResourceLocator();
      if (locator == null)
        return null;
      try
      {
        return ResourcePathHelper.Combine(locator.NativeResourcePath, "../");
      }
      catch (ArgumentException)
      {
        return null;
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

      /// <summary>
      /// How far the caller's own viewer got in this title, nought to a hundred, or null when the request named
      /// nobody. Where <see cref="Resume"/> hides the two ends of that range - a finished film is not a position
      /// to resume from - this one says them, because they are the two answers a menu needs: only somebody who
      /// stands at a hundred is offered the way back to "unplayed"
      /// (<c>AbstractWatchedAction.cs:47-49</c>), and only somebody below it is offered "played".
      /// </summary>
      public int? Percentage { get; set; }

      /// <summary>Set when the report was refused, in words a screen can show.</summary>
      public string Error { get; set; }
    }

    /// <summary>
    /// Whether the library took the ask to look at one title's file again.
    /// </summary>
    /// <remarks>
    /// What a missing <see cref="Error"/> does not say is worth spelling out: the library schedules the second
    /// look and carries it out later, so this answers "the ask was taken", not "the new figures are in". A
    /// screen that re-reads the title straight after would be reading the old ones.
    /// </remarks>
    public class RefreshResult
    {
      public Guid Id { get; set; }

      /// <summary>Set when the ask was refused, in words a screen can show.</summary>
      public string Error { get; set; }
    }

    /// <summary>
    /// One picture a title's file carries, as the importer wrote it into the library.
    /// </summary>
    /// <remarks>
    /// The figures are the library's own, stored in <c>VideoStreamAspect</c> when the file was first looked at;
    /// nothing here is measured now. A title whose importer never got inside the file answers with an empty list
    /// rather than with invented numbers.
    /// </remarks>
    public class VideoStreamInfo
    {
      /// <summary>Where in the file this picture sits.</summary>
      public int StreamIndex { get; set; }

      /// <summary>
      /// What kind of picture it is, in the library's own words: "SD", "HD", "UHD", and for a stereoscopic file
      /// one of "HSBS", "SBS", "HTAB", "TAB" (<c>VideoStreamAspect.cs:15-29</c>).
      /// </summary>
      public string VideoType { get; set; }

      /// <summary>The codec the file named for its picture, for example "AVC" or "HEVC".</summary>
      public string VideoEncoding { get; set; }

      public int Width { get; set; }
      public int Height { get; set; }

      /// <summary>The picture's own proportion, as the importer measured it.</summary>
      public float AspectRatio { get; set; }

      /// <summary>Bits per second, as the importer read them.</summary>
      public long BitRate { get; set; }

      /// <summary>Pictures per second.</summary>
      public float FPS { get; set; }

      /// <summary>How long this picture runs, in seconds; zero when the importer did not write it.</summary>
      public long DurationSeconds { get; set; }

      /// <summary>
      /// The high dynamic range format the file named for its picture, "HDR10" and the like; empty when the
      /// picture is an ordinary one, or when the library was built before the importer learned to ask.
      /// </summary>
      public string HdrFormat { get; set; }
    }

    /// <summary>One of the audio tracks a title's file carries.</summary>
    public class AudioStreamInfo
    {
      public int StreamIndex { get; set; }

      /// <summary>The two-letter code the file gave this track, or empty when it named none.</summary>
      public string Language { get; set; }

      /// <summary>The codec the file named for this track, for example "AC3" or "DTS".</summary>
      public string Encoding { get; set; }

      /// <summary>How many channels the track carries.</summary>
      public int Channels { get; set; }

      /// <summary>Bits per second, as the importer read them.</summary>
      public long BitRate { get; set; }

      /// <summary>Samples per second.</summary>
      public long SampleRate { get; set; }
    }

    /// <summary>One of the subtitle tracks a title's file carries.</summary>
    public class SubtitleStreamInfo
    {
      public int StreamIndex { get; set; }

      /// <summary>The two-letter code the file gave this track, or empty when it named none.</summary>
      public string Language { get; set; }

      /// <summary>The codec of the text, when the file named one.</summary>
      public string Encoding { get; set; }

      /// <summary>How the text is carried, for example "SRT" or "ASS".</summary>
      public string Format { get; set; }

      /// <summary>Whether the text lies inside the file or came to it from outside.</summary>
      public bool IsInternal { get; set; }

      /// <summary>Whether the file marks this track as the one to show by default.</summary>
      public bool IsDefault { get; set; }

      /// <summary>Whether the file insists on showing this track whatever else the viewer picks.</summary>
      public bool IsForced { get; set; }
    }

    /// <summary>
    /// The tracks one title's file carries, in the three kinds a viewer chooses between: picture, sound and text.
    /// </summary>
    /// <remarks>
    /// Every list is empty rather than null when the library holds nothing of that kind, so a screen asks one
    /// question - "is there anything to show" - and does not have to tell apart "the importer never looked inside
    /// this file" from "the file has no second audio track", which it cannot answer anyway.
    /// </remarks>
    public class MediaStreamsResult
    {
      public Guid Id { get; set; }

      public List<VideoStreamInfo> Videos { get; set; } = new List<VideoStreamInfo>();

      public List<AudioStreamInfo> Audios { get; set; } = new List<AudioStreamInfo>();

      public List<SubtitleStreamInfo> Subtitles { get; set; } = new List<SubtitleStreamInfo>();
    }

    /// <summary>
    /// One of the people a title names.
    /// </summary>
    /// <remarks>
    /// The name is handed over as the library holds it, without this server deciding what a good name looks like.
    /// </remarks>
    public class PersonInfo
    {
      /// <summary>
      /// The person's own media item id, which is what the library files that person's picture under - the
      /// reference asked for the portrait of a person by exactly this id (themes/default/styles/
      /// CurrentlyPlayingStyles.xaml:220-222).
      /// </summary>
      public Guid Id { get; set; }

      /// <summary>The name the person's own aspect carries.</summary>
      public string Name { get; set; }
    }

    /// <summary>
    /// The people who played one part of a title.
    /// </summary>
    public class PersonGroupInfo
    {
      /// <summary>
      /// The part, in the word the caller asked it with - so the answer can be sorted back into the band it belongs
      /// to without knowing anything about the library's own identifiers.
      /// </summary>
      public string Role { get; set; }

      /// <summary>Empty rather than null when nobody played this part.</summary>
      public List<PersonInfo> Persons { get; set; } = new List<PersonInfo>();
    }

    /// <summary>
    /// The people a title names, one group per part that was asked for.
    /// </summary>
    /// <remarks>
    /// One group per asked word, in the order the words were asked - including a group with nobody in it. A screen
    /// then reads the answer by walking its own bands and never has to tell "nobody played that part" from "that
    /// part was never asked about", which is the same difference the tracks answer settles by answering empty lists
    /// instead of leaving them out.
    /// </remarks>
    public class MediaPersonsResult
    {
      public Guid Id { get; set; }

      /// <summary>Set when the question itself was refused, and the only way such a refusal is explained.</summary>
      public string Error { get; set; }

      public List<PersonGroupInfo> Groups { get; set; } = new List<PersonGroupInfo>();
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
    /// <c>POST api/media/items/{id}/refresh</c> - ask the library to look at one title's file again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the reference's "Refresh" entry (registered at plugin.xml:275, carried out by
    /// <c>RefreshMediaItem.cs:60-69</c>) and it is the same call with the same argument: one title, metadata
    /// kept. <c>clearMetadata</c> is false because the reference's refresh is the entry that does NOT throw the
    /// found data away - the entry that does is re-import, which asks the viewer to pick a match first
    /// (<c>RefreshMediaItem.cs:29</c>, <c>ReimportMediaItem.cs:60-73</c>).
    /// </para>
    /// <para>
    /// What the library does with the ask is schedule the title's folder for its importer worker
    /// (<c>MediaLibrary.cs:1008-1034</c>, which marks the item dirty and calls
    /// <c>importerWorker.ScheduleRefresh(path, categories, includeSubDirectories: true)</c>), and it answers
    /// later through its own change message. So this answer says that the ask was taken and not what the second
    /// look found - a screen that re-read the title straight away would be reading the old figures and calling
    /// them new.
    /// </para>
    /// <para>
    /// A title with nothing to look at again is refused rather than agreed with. The reference's own condition is
    /// that the title names resources or is a stub, and that a locator can be made from it
    /// (<c>RefreshMediaItem.cs:44-49</c>); promising a refresh for a title whose file the library does not know
    /// would report work that was never planned.
    /// </para>
    /// </remarks>
    [HttpPost("items/{id}/refresh")]
    public ActionResult<RefreshResult> RefreshTitle(Guid id)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      try
      {
        Guid? profile = CallerProfile;
        MediaItem title = FindTitleWithResources(mediaLibrary, id, profile);
        if (title == null)
          return NotFound(new RefreshResult { Id = id, Error = "this server has no title with the id " + id });

        if (title.PrimaryResources.Count == 0 && !title.IsStub)
          return BadRequest(new RefreshResult
          {
            Id = id,
            Error = "the library holds no file for this title, so there is nothing it could look at again"
          });

        mediaLibrary.RefreshMediaItemMetadata(id, false);
        return new RefreshResult { Id = id };
      }
      catch (IllegalCallException ex)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Info("MediaController: the media library refused a refresh, answering 503: {0}", ex.Message);
        return new ObjectResult(new RefreshResult { Id = id, Error = ex.Message })
        {
          StatusCode = StatusCodes.Status503ServiceUnavailable
        };
      }
    }

    /// <summary>
    /// The tracks a title's file carries: its pictures, its audio tracks and its subtitle tracks.
    /// </summary>
    /// <param name="id">The media item id, as <c>GET api/media/titles</c> reported it.</param>
    /// <remarks>
    /// <para>
    /// <b>Where the figures come from.</b> Nothing is read from the file for this answer. When the importer first
    /// looked at a file it wrote what it found into three aspects of the title - <c>VideoStreamAspect</c>,
    /// <c>VideoAudioStreamAspect</c> and <c>SubtitleAspect</c> - and each of them holds one record per track, which
    /// is precisely what the reference's information screen iterated over: six logos above, one card per audio
    /// track and one per subtitle track below (<c>MediaInfoVideo.xaml:152-296</c> for the logos,
    /// <c>:309-380</c> for the two lists, and the cards themselves at
    /// <c>themes/default/styles/CurrentlyPlayingStyles.xaml:45-192</c>). Reading the same three aspects is
    /// therefore not an invention of a shape; it is the same data the old skin drew.
    /// </para>
    /// <para>
    /// <b>Asked for as optional aspects.</b> The Media aspect is the necessary one and the three track aspects are
    /// optional, so a title the library knows but never got inside answers with three empty lists instead of
    /// "no such title". That difference is the whole answer for a screen: an empty list is the ordinary state of a
    /// title whose importer was never run over the file, and it must not be reported as a missing title.
    /// </para>
    /// <para>
    /// The order is the library's own, which is the order the tracks lie in the file - the same order the
    /// reference listed them in, since it walked the aspect records as they came.
    /// </para>
    /// </remarks>
    [HttpGet("items/{id}/streams")]
    public ActionResult<MediaStreamsResult> GetStreams(Guid id)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      MediaItem title = FindTitleWithStreams(mediaLibrary, id, CallerProfile);
      MediaStreamsResult answer = new MediaStreamsResult { Id = id };
      if (title == null)
        return NotFound(answer);

      IList<MultipleMediaItemAspect> streams;
      if (MediaItemAspect.TryGetAspects(title.Aspects, VideoStreamAspect.Metadata, out streams))
      {
        foreach (MultipleMediaItemAspect stream in streams)
          answer.Videos.Add(new VideoStreamInfo
          {
            StreamIndex = ReadInt(stream, VideoStreamAspect.ATTR_STREAM_INDEX),
            VideoType = ReadText(stream, VideoStreamAspect.ATTR_VIDEO_TYPE),
            VideoEncoding = ReadText(stream, VideoStreamAspect.ATTR_VIDEOENCODING),
            Width = ReadInt(stream, VideoStreamAspect.ATTR_WIDTH),
            Height = ReadInt(stream, VideoStreamAspect.ATTR_HEIGHT),
            AspectRatio = ReadFloat(stream, VideoStreamAspect.ATTR_ASPECTRATIO),
            BitRate = ReadLong(stream, VideoStreamAspect.ATTR_VIDEOBITRATE),
            FPS = ReadFloat(stream, VideoStreamAspect.ATTR_FPS),
            DurationSeconds = ReadLong(stream, VideoStreamAspect.ATTR_DURATION),
            HdrFormat = ReadKnownText(stream, VideoStreamAspect.ATTR_HDRFORMAT)
          });
      }

      if (MediaItemAspect.TryGetAspects(title.Aspects, VideoAudioStreamAspect.Metadata, out streams))
      {
        foreach (MultipleMediaItemAspect stream in streams)
          answer.Audios.Add(new AudioStreamInfo
          {
            StreamIndex = ReadInt(stream, VideoAudioStreamAspect.ATTR_STREAM_INDEX),
            Language = ReadText(stream, VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE),
            Encoding = ReadText(stream, VideoAudioStreamAspect.ATTR_AUDIOENCODING),
            Channels = ReadInt(stream, VideoAudioStreamAspect.ATTR_AUDIOCHANNELS),
            BitRate = ReadLong(stream, VideoAudioStreamAspect.ATTR_AUDIOBITRATE),
            SampleRate = ReadLong(stream, VideoAudioStreamAspect.ATTR_AUDIOSAMPLERATE)
          });
      }

      if (MediaItemAspect.TryGetAspects(title.Aspects, SubtitleAspect.Metadata, out streams))
      {
        foreach (MultipleMediaItemAspect stream in streams)
          answer.Subtitles.Add(new SubtitleStreamInfo
          {
            StreamIndex = ReadInt(stream, SubtitleAspect.ATTR_STREAM_INDEX),
            Language = ReadText(stream, SubtitleAspect.ATTR_SUBTITLE_LANGUAGE),
            Encoding = ReadText(stream, SubtitleAspect.ATTR_SUBTITLE_ENCODING),
            Format = ReadText(stream, SubtitleAspect.ATTR_SUBTITLE_FORMAT),
            IsInternal = ReadBool(stream, SubtitleAspect.ATTR_INTERNAL),
            IsDefault = ReadBool(stream, SubtitleAspect.ATTR_DEFAULT),
            IsForced = ReadBool(stream, SubtitleAspect.ATTR_FORCED)
          });
      }

      return answer;
    }

    /// <summary>
    /// The one item with this id, read together with the three aspects that hold its tracks.
    /// </summary>
    /// <remarks>
    /// A separate read from <see cref="FindTitle"/> for the same reason <see cref="FindTitleWithResources"/> is:
    /// the aspects a caller needs decide what "not found" means, and for the tracks the honest answer about a
    /// title nobody looked inside is an empty list.
    /// </remarks>
    private static MediaItem FindTitleWithStreams(IMediaLibrary mediaLibrary, Guid id, Guid? profile)
    {
      MediaItemQuery query = new MediaItemQuery(new List<Guid> { MediaAspect.ASPECT_ID },
          new List<Guid> { VideoStreamAspect.ASPECT_ID, VideoAudioStreamAspect.ASPECT_ID, SubtitleAspect.ASPECT_ID },
          new MediaItemIdFilter(id));
      IList<MediaItem> items = mediaLibrary.Search(query, false, profile, VirtualTitles.LetsVirtualTitlesIn);
      return items == null ? null : items.FirstOrDefault();
    }

    /// <summary>
    /// One number of a track's record, or nought where the importer left it out.
    /// </summary>
    /// <remarks>
    /// Every one of these attributes is nullable in the library - a container that did not name its bitrate left
    /// the field empty - and a screen cannot show "the file did not say", so the absence is answered by nought and
    /// the screen leaves the figure out. A record whose value is missing and a record that is not there at all
    /// then read the same way, which is what the reference did too: it hid the logo rather than drawing a nought
    /// (<c>MediaInfoVideo.xaml:239-255</c>, where both labels are only visible when their figure is set).
    /// </remarks>
    private static int ReadInt(MultipleMediaItemAspect stream, MediaItemAspectMetadata.MultipleAttributeSpecification attribute)
    {
      return (int?)stream[attribute] ?? 0;
    }

    private static long ReadLong(MultipleMediaItemAspect stream, MediaItemAspectMetadata.MultipleAttributeSpecification attribute)
    {
      return (long?)stream[attribute] ?? 0L;
    }

    private static float ReadFloat(MultipleMediaItemAspect stream, MediaItemAspectMetadata.MultipleAttributeSpecification attribute)
    {
      return (float?)stream[attribute] ?? 0f;
    }

    private static bool ReadBool(MultipleMediaItemAspect stream, MediaItemAspectMetadata.MultipleAttributeSpecification attribute)
    {
      return (bool?)stream[attribute] ?? false;
    }

    private static string ReadText(MultipleMediaItemAspect stream, MediaItemAspectMetadata.MultipleAttributeSpecification attribute)
    {
      return (string)stream[attribute];
    }

    /// <summary>
    /// Reads a text the aspect might not carry at all, and says so with <c>null</c> rather than by throwing.
    /// </summary>
    /// <remarks>
    /// A database keeps the attribute list an aspect had when the library first saw it - adding an attribute to
    /// the aspect in code does not add it to a database that is already built. Asking such an aspect for the
    /// attribute throws, which would take the whole answer down for the sake of one figure an older library has
    /// never measured. So the aspect's own list is consulted first.
    /// </remarks>
    private static string ReadKnownText(MultipleMediaItemAspect stream, MediaItemAspectMetadata.MultipleAttributeSpecification attribute)
    {
      return stream.Metadata.AttributeSpecifications.ContainsKey(attribute.AttributeName)
          ? ReadText(stream, attribute)
          : null;
    }

    /// <summary>
    /// The people a title names, one group per part that was asked for.
    /// </summary>
    /// <param name="id">The media item id of the title, as <see cref="GetTitles"/> reported it.</param>
    /// <param name="roles">
    /// Which parts to answer for, spelled as words: <c>actor</c>, <c>director</c>, <c>writer</c>, <c>artist</c>,
    /// <c>albumartist</c>, <c>composer</c>, <c>conductor</c>. The parameter may be repeated or its words separated
    /// by commas. A part this server does not know is refused by name rather than skipped, because a screen that
    /// believes it asked for directors and gets nothing back shows an empty band instead of its own mistake.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>How a title knows its people.</b> A person is an item of its own in the library, and what ties a title to
    /// that person is a record naming the part the person played. The reference asked for one part at a time out of
    /// exactly that record: its two lists on the information screen were built by
    /// <c>RelationshipQueryExtension</c>, which turns a part and a title into the library's own
    /// <see cref="RelationshipFilter"/> (SkinEngine/MarkupExtensions/RelationshipQueryExtension.cs:78-86, used from
    /// screens/MediaInfoVideo.xaml:393-398 for the actors and :430-435 for the director). This endpoint asks that
    /// same question per part, so a group holds what the library says stands in that part - nothing this server
    /// sorted, merged or judged.
    /// </para>
    /// <para>
    /// <b>Asked for as aspects.</b> The reference asked a person for its provider resource, its media data and its
    /// person data (UiComponents/Media/General/Consts.cs:754-759). Two of those three are what this answer reads, so
    /// two are asked for; the provider resource is left out because nothing in the answer comes from it, and asking
    /// for an aspect has a cost in the database that a caller should not pay for a figure nobody reads.
    /// </para>
    /// <para>
    /// <b>The order.</b> No sort is asked for, exactly as the reference asked for none
    /// (SkinEngine/MarkupExtensions/MediaItemQueryExtension.cs:184 builds the query out of the aspects and the
    /// filter and nothing else), so a group lists its people in the order the library's rows come back. A screen
    /// that wants them by name sorts them itself.
    /// </para>
    /// </remarks>
    [HttpGet("items/{id}/persons")]
    public ActionResult<MediaPersonsResult> GetPersons(Guid id, [FromQuery] IList<string> roles)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      List<string> asked;
      string refusal;
      if (!TryReadRoles(roles, out asked, out refusal))
        return BadRequest(new MediaPersonsResult { Id = id, Error = refusal });

      if (FindTitle(mediaLibrary, id, CallerProfile) == null)
        return NotFound(new MediaPersonsResult { Id = id });

      MediaPersonsResult answer = new MediaPersonsResult { Id = id };
      foreach (string role in asked)
      {
        PersonGroupInfo group = new PersonGroupInfo { Role = role };
        foreach (MediaItem person in FindPeople(mediaLibrary, id, PersonRoles[role], CallerProfile))
        {
          SingleMediaItemAspect personAspect;
          if (!MediaItemAspect.TryGetAspect(person.Aspects, PersonAspect.Metadata, out personAspect))
            continue;

          group.Persons.Add(new PersonInfo
          {
            Id = person.MediaItemId,
            Name = personAspect.GetAttributeValue<string>(PersonAspect.ATTR_PERSON_NAME)
          });
        }
        answer.Groups.Add(group);
      }
      return answer;
    }

    /// <summary>
    /// The parts people are asked for by name, in the words a caller uses rather than the library's identifiers.
    /// </summary>
    /// <remarks>
    /// A screen should not have to know that the library's own word for an actor is a GUID
    /// (DefaultItemAspects/PersonAspect.cs:101-107), and an address that carries a GUID is one a person cannot
    /// type into a browser to see what a title says about its director.
    /// </remarks>
    private static readonly Dictionary<string, Guid> PersonRoles = new Dictionary<string, Guid>(
        StringComparer.OrdinalIgnoreCase)
    {
        { "actor", PersonAspect.ROLE_ACTOR },
        { "director", PersonAspect.ROLE_DIRECTOR },
        { "writer", PersonAspect.ROLE_WRITER },
        { "artist", PersonAspect.ROLE_ARTIST },
        { "albumartist", PersonAspect.ROLE_ALBUMARTIST },
        { "composer", PersonAspect.ROLE_COMPOSER },
        { "conductor", PersonAspect.ROLE_CONDUCTOR }
    };

    /// <summary>
    /// The parts a question asks for, in the order it asked for them and without a word asked twice.
    /// </summary>
    /// <param name="roles">The words as they arrived, which may be one list or several.</param>
    /// <param name="asked">The words to answer for, when the question is answerable.</param>
    /// <param name="refusal">Why the question is not answerable, when it is not.</param>
    /// <returns>False when a word is unknown or when no part at all was named.</returns>
    /// <remarks>
    /// Asking for nothing is refused instead of answered with an empty answer: a screen that forgot to say which
    /// part it wants is a bug, and "nobody played any part" is the wrong way to say so.
    /// </remarks>
    private static bool TryReadRoles(IList<string> roles, out List<string> asked, out string refusal)
    {
      asked = new List<string>();
      refusal = null;

      if (roles != null)
      {
        foreach (string rough in roles)
        {
          if (string.IsNullOrEmpty(rough))
            continue;

          foreach (string word in rough.Split(','))
          {
            string role = word.Trim();
            if (role.Length == 0)
              continue;

            if (!PersonRoles.ContainsKey(role))
            {
              refusal = string.Format("\"{0}\" is no part this server knows; it knows {1}", role,
                  string.Join(", ", PersonRoles.Keys.OrderBy(k => k, StringComparer.Ordinal)));
              return false;
            }

            if (!asked.Contains(role, StringComparer.OrdinalIgnoreCase))
              asked.Add(role);
          }
        }
      }

      if (asked.Count == 0)
      {
        refusal = "at least one part has to be named, e.g. ?roles=actor,director";
        return false;
      }

      return true;
    }

    /// <summary>
    /// The people standing in one part of one title.
    /// </summary>
    /// <remarks>
    /// The linked role is left empty, which is how the reference left it too
    /// (RelationshipQueryExtension.cs:82-83: the part the title plays in the relationship is answered by the
    /// library, not asked for) - asking for a part of a title is the whole question.
    /// </remarks>
    private static IList<MediaItem> FindPeople(IMediaLibrary mediaLibrary, Guid titleId, Guid role, Guid? profile)
    {
      MediaItemQuery query = new MediaItemQuery(
          new List<Guid> { MediaAspect.ASPECT_ID, PersonAspect.ASPECT_ID }, null,
          new RelationshipFilter(role, Guid.Empty, titleId));

      IList<MediaItem> items = mediaLibrary.Search(query, false, profile, VirtualTitles.LetsVirtualTitlesIn);
      return items ?? new List<MediaItem>();
    }

    /// <summary>
    /// The title the library holds a film's collection under, written onto the answer as
    /// <see cref="ItemResult.CollectionId"/>. Left null when the film names no collection or when no title wears
    /// that name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MP2 keeps a collection twice: the film carries its name as plain text on the Movie aspect, and the library
    /// holds a title of its own wearing the collection aspect. A screen that merely names the collection gets
    /// along with the text, but a screen that shows it - which is what a collection button does - has to ask for
    /// pictures, and every picture in this library is reached over the id of the title it belongs to.
    /// </para>
    /// <para>
    /// Looked up by name instead of over the relationship between film and collection, because the library files
    /// that relationship from the film's side only (<c>ROLE_MOVIE</c> linked to <c>ROLE_MOVIE_COLLECTION</c>)
    /// while <see cref="RelationshipFilter"/> walks the other way - it asks a collection which films it holds,
    /// which is the question a collection's picture provider asks. Counted over a library of 1136 films: all 228
    /// films naming a collection name one the library also holds as a title, so the name is a key that holds.
    /// </para>
    /// </remarks>
    private static void FillCollectionTitle(IMediaLibrary mediaLibrary, ItemResult result, Guid? profile)
    {
      if (result == null || string.IsNullOrEmpty(result.Collection))
        return;

      try
      {
        MediaItemQuery query = new MediaItemQuery(
            new List<Guid> { MediaAspect.ASPECT_ID, MovieCollectionAspect.ASPECT_ID }, null,
            new RelationalFilter(MovieCollectionAspect.ATTR_COLLECTION_NAME, RelationalOperator.EQ,
                result.Collection));

        IList<MediaItem> titles = mediaLibrary.Search(query, false, profile, VirtualTitles.LetsVirtualTitlesIn);
        MediaItem title = titles == null ? null : titles.FirstOrDefault();
        if (title != null)
          result.CollectionId = title.MediaItemId.ToString();
      }
      catch (IllegalCallException)
      {
        // A library that will not answer a question about collections still answers the question about the film;
        // the collection stays named without a link, which is all the answer used to say about it.
      }
    }

    /// <summary>
    /// The one item with this id, read together with the files it is made of.
    /// </summary>
    /// <remarks>
    /// The Media aspect alone is what <see cref="FindTitle"/> asks for; this adds the provider resources as an
    /// <i>optional</i> aspect, because a title without them is exactly the case the caller has to be able to tell
    /// apart from a title that does not exist - and a necessary aspect would answer that second question with
    /// "not found".
    /// </remarks>
    private static MediaItem FindTitleWithResources(IMediaLibrary mediaLibrary, Guid id, Guid? profile)
    {
      MediaItemQuery query = new MediaItemQuery(new List<Guid> { MediaAspect.ASPECT_ID },
          new List<Guid> { ProviderResourceAspect.ASPECT_ID }, new MediaItemIdFilter(id));
      IList<MediaItem> items = mediaLibrary.Search(query, false, profile, VirtualTitles.LetsVirtualTitlesIn);
      return items == null ? null : items.FirstOrDefault();
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
        Resume = askedAsSomebody ? ReadResume(item) : null,
        Percentage = askedAsSomebody ? ReadPercentage(item) : null
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
      if (string.Equals(sort, "season", StringComparison.OrdinalIgnoreCase))
        return "season";
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
    /// <summary>
    /// A play percentage spelled the way the library stores one.
    /// </summary>
    /// <remarks>
    /// The reference writes a viewer's percentage as three digits
    /// (<c>UserDataKeysKnown.GetSortablePlayPercentageString</c>, which is <c>value.ToString("000")</c>) so that
    /// the text comparison its shelves compile to still orders the values as numbers - "004" is below "049" is
    /// below "100". Its filters compare against that spelling, so this compares against it too.
    /// </remarks>
    private static string PlayPercentage(int value)
    {
      return UserDataKeysKnown.GetSortablePlayPercentageString(value);
    }

    /// <summary>
    /// A play count spelled the way the library stores one.
    /// </summary>
    /// <remarks>
    /// Like the percentage, a count is written as a fixed-width string - ten digits
    /// (<c>UserDataKeysKnown.GetSortablePlayCountString:52-55</c>) - so that the text comparison the shelves
    /// compile to still orders the values as numbers. The reference compares its favourite shelves against that
    /// spelling, so this does too.
    /// </remarks>
    private static string PlayCount(long value)
    {
      return UserDataKeysKnown.GetSortablePlayCountString(value);
    }

    /// <summary>
    /// The order a favourites shelf is played in: the titles this viewer played most often first, and among them
    /// the one they played most lately.
    /// </summary>
    /// <param name="viewer">The caller's profile, or null for the household's own counters.</param>
    /// <param name="aspectId">The aspect being listed.</param>
    /// <remarks>
    /// <para>
    /// The reference states this order inside each of its two favourite providers instead of leaving it to a
    /// caller: <c>BaseFavoriteMediaListProvider.cs:46-57</c> sorts a playable by its count and then by its date,
    /// and <c>BaseFavoriteRelationshipMediaListProvider</c> (in the same file, :112-117) puts one column in
    /// front for a container - how many of its children the viewer played - because a series with twelve played
    /// episodes is a likelier favourite than one with one.
    /// </para>
    /// <para>
    /// Without a profile there are no figures of anybody's to order by, and the household's columns take over,
    /// which is the reference's own second half: count, then last-played date.
    /// </para>
    /// </remarks>
    private static List<ISortInformation> FavoriteSorts(Guid? viewer, Guid aspectId)
    {
      if (!viewer.HasValue)
        return new List<ISortInformation>
        {
          new AttributeSortInformation(MediaAspect.ATTR_PLAYCOUNT, SortDirection.Descending),
          new AttributeSortInformation(MediaAspect.ATTR_LASTPLAYED, SortDirection.Descending)
        };

      List<ISortInformation> sorts = new List<ISortInformation>();
      if (aspectId == SeriesAspect.ASPECT_ID || aspectId == AudioAlbumAspect.ASPECT_ID)
        sorts.Add(new DataSortInformation(UserDataKeysKnown.KEY_PLAY_MAX_CHILD_COUNT, SortDirection.Descending));
      sorts.Add(new DataSortInformation(UserDataKeysKnown.KEY_PLAY_COUNT, SortDirection.Descending));
      sorts.Add(new DataSortInformation(UserDataKeysKnown.KEY_PLAY_DATE, SortDirection.Descending));
      return sorts;
    }

    /// <summary>
    /// Asks a question of a container through the children that carry the answer, which is how the reference's
    /// shelves for series and albums are built.
    /// </summary>
    /// <param name="aspectId">The aspect being listed.</param>
    /// <param name="aboutChildren">What has to be true about a linked episode or track.</param>
    /// <param name="aboutItem">
    /// What has to be true about the listed title itself, or <c>null</c> to ask the children alone. The reference
    /// always has both halves because both are read from a viewer's own data; a request that names nobody has
    /// nothing to ask the container with, and passes <c>null</c> here.
    /// </param>
    /// <remarks>
    /// <para>
    /// A series is never watched and an album never heard - the episodes and the tracks are - so the reference
    /// asks both halves together: "a linked episode whose own data says so" <i>and</i> "this title's own data says
    /// so" (<c>BaseUnwatchedRelationshipMediaListProvider.cs:91-98</c>,
    /// <c>BaseLastWatchedMediaListProvider.cs:88-99</c>,
    /// <c>BaseContinueWatchRelationshipMediaListProvider.cs:97-105</c>). The second half is not decoration: the
    /// library keeps the average of a container's children's percentages on the container itself
    /// (<c>MediaLibrary.UpdateParentPlayUserData</c>, reached from <c>NotifyUserPlayback</c> at :2862-2863), and it
    /// is that average which keeps a series with three of nine episodes seen out of the "unplayed" shelf while it
    /// sits in the continue one. Without it, one title shows in both shelves at once - which is what this endpoint
    /// used to answer.
    /// </para>
    /// <para>
    /// The two pairs of roles are the ones the reference's providers name: <c>UnwatchedSeriesMediaListProvider</c>
    /// pairs ROLE_SERIES with ROLE_EPISODE, the album shelves ROLE_ALBUM with ROLE_TRACK. Anything playable by
    /// itself - a film, an episode, a track - is only asked its own half.
    /// </para>
    /// </remarks>
    private static IFilter AskedThroughChildren(Guid aspectId, IFilter aboutChildren, IFilter aboutItem)
    {
      if (aspectId != SeriesAspect.ASPECT_ID && aspectId != AudioAlbumAspect.ASPECT_ID)
        return aboutItem;

      IFilter relationship = aspectId == SeriesAspect.ASPECT_ID
          ? new FilteredRelationshipFilter(SeriesAspect.ROLE_SERIES, EpisodeAspect.ROLE_EPISODE, aboutChildren)
          : new FilteredRelationshipFilter(AudioAlbumAspect.ROLE_ALBUM, AudioAspect.ROLE_TRACK, aboutChildren);
      return aboutItem == null
          ? relationship
          : BooleanCombinationFilter.CombineFilters(BooleanOperator.And, relationship, aboutItem);
    }

    private static string ReadSeen(string seen)
    {
      if (string.Equals(seen, "unwatched", StringComparison.OrdinalIgnoreCase))
        return "unwatched";
      if (string.Equals(seen, "watched", StringComparison.OrdinalIgnoreCase))
        return "watched";
      if (string.Equals(seen, "started", StringComparison.OrdinalIgnoreCase))
        return "started";
      // Spelled the way the reference spells its shelf keys ("FavoriteMovies", plugin.xml:299-307). The other
      // spelling is taken for the same thing, because a caller writing English in a second language will send it
      // and there is nothing to lose by understanding it.
      if (string.Equals(seen, "favorite", StringComparison.OrdinalIgnoreCase)
          || string.Equals(seen, "favourite", StringComparison.OrdinalIgnoreCase))
        return "favorite";
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
    /// (WifiRemote/Helper.cs:315). Only that those screens read the whole shelf into memory and sort it there: the
    /// episode number is a collection, and a collection cannot be a column in this query at all, so the order is
    /// reached through the importer's written number instead - see the <c>episode</c> case below.
    /// </para>
    /// <para>
    /// The empty list means "in whatever order the database fancies", which is what <c>sort=none</c> promises.
    /// </para>
    /// <para>
    /// A shelf of seasons sorts by <c>SeasonAspect.ATTR_SEASON</c>, one column: the number is the whole of the
    /// order, and the title an importer wrote above it is a name, not a place in a series.
    /// </para>
    /// <para>
    /// A shelf of tunes asks the disc first and the place on that disc second, but only while standing inside the
    /// album it came from - see <paramref name="insideOneAlbum"/> and the <c>track</c> case below.
    /// </para>
    /// </remarks>
    /// <param name="insideOneAlbum">
    /// True when the shelf being sorted stands inside an album, which is the only place where a disc number means
    /// anything. See the "track" case.
    /// </param>
    private static List<ISortInformation> BuildSorts(string sortBy, string order, bool insideOneAlbum = false)
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
        case "season":
          // A shelf of seasons is watched in the order they were made, and MP2's own seasons screen sorts them
          // the same way it sorts everything else - by the number the aspect carries, not by the title the
          // importer wrote ("Bones (2005) S02" sorts under B and would put season ten between one and two).
          sorts.Add(new AttributeSortInformation(SeasonAspect.ATTR_SEASON, direction));
          break;
        case "episode":
          // The season is a number the database can reach - one column, Inline (EpisodeAspect.cs:48-49). The
          // episode number is not: it is a collection, because a file holding two episodes is one item carrying
          // two numbers (EpisodeAspect.cs:62-63, Cardinality.ManyToMany), and the query engine refuses collection
          // attributes as sort keys outright - it drops the key and says nothing (MainQueryBuilder.cs:393-395,
          // "Sorting can only be done for Inline and MTO attributes"). Asking for the number by name therefore
          // sorted the shelf by season alone, and inside a season handed back whatever order the database felt
          // like: measured on this household's Prison Break season three that was 2, 12, 9, 13, 5, 8, 1, 3 ...
          sorts.Add(new AttributeSortInformation(EpisodeAspect.ATTR_SEASON, direction));
          // The number is therefore reached through the name the importer wrote over the episode, which carries
          // that number with two digits: "<series> S<nn>E<nn> - <name>" (EpisodeInfo.EPISODE_FORMAT_STR:67, the
          // digits padded by the readers themselves - NfoSeriesEpisodeReader.cs:939-940, PadLeft(2,'0'), and
          // SeasonInfo.SEASON_FORMAT_STR:54 for the season the same way). Inside one series - which is all a
          // shelf of episodes ever is - that name sorts in exactly the order its numbers sort, and it is a column
          // the database can reach. The same shelf after this key: 1 through 13.
          sorts.Add(new AttributeSortInformation(MediaAspect.ATTR_TITLE, direction));
          break;
        case "track":
          // An album is listened to disc by disc, so inside one record the disc is asked first and the place on
          // that disc second, which is what MP2's own reader asks for the same shelf
          // (WifiRemote/Helper.cs:397-401). The place on a disc alone interleaves two discs' first tunes beside
          // one another: measured on this household's "Born This Way (Special Edition)" it came back
          // 1, 1, 2, 2, 3, 3. Both columns are Inline and reach the database fine (AudioAspect.cs:84-85 for the
          // tune, :144-145 for the disc). Asked of a whole library shelf instead, the number of a disc inside its
          // own record means nothing - every album's second tune would be gathered behind every album's first -
          // so there the place on the disc stays the one question.
          if (insideOneAlbum)
            sorts.Add(new AttributeSortInformation(AudioAspect.ATTR_DISCID, direction));
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
    /// The seasons a series' episodes carry, for the library that holds the episodes and no season titles: the
    /// season numbers in order, each with how many episodes wear it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MP2 writes a season title when the importer hands one over - <c>EpisodeSeasonRelationshipExtractor</c> looks
    /// for a <see cref="SeasonAspect"/> among the aspects extracted for an episode and links the two when it finds
    /// one - so a library that was pointed straight at its episode files, tagged with the tools that name a season's
    /// pictures after it (<c>season01-poster.jpg</c>), numbers every episode and holds no season at all. The link
    /// between series and seasons then answers nothing, although the answer is written on every episode.
    /// </para>
    /// <para>
    /// So the numbers are counted, which gives the same shelf in the same order. Nought stays a shelf of its own,
    /// because that is MP2's spelling of the specials and the pictures answer to <c>season-specials-*</c>
    /// (<see cref="LocalFanartHelper.GetAdditionalSeasonPrefixes"/>); an importer that numbered nothing at all
    /// therefore leaves one season nought holding everything, which is a shelf, rather than no shelf at all.
    /// </para>
    /// </remarks>
    private static List<KeyValuePair<int, int>> CountSeasonsOfSeries(IMediaLibrary mediaLibrary, Guid seriesId)
    {
      MediaItemQuery episodes = new MediaItemQuery(
          new List<Guid> { EpisodeAspect.ASPECT_ID },
          new List<Guid> { ProviderResourceAspect.ASPECT_ID },
          new RelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES, seriesId));

      Dictionary<int, int> counted = new Dictionary<int, int>();
      IList<MediaItem> held = mediaLibrary.Search(episodes, false, null, false);
      if (held == null)
        return new List<KeyValuePair<int, int>>();

      foreach (MediaItem episode in held)
      {
        SingleMediaItemAspect numbered;
        int number = MediaItemAspect.TryGetAspect(episode.Aspects, EpisodeAspect.Metadata, out numbered)
            ? numbered.GetAttributeValue<int>(EpisodeAspect.ATTR_SEASON)
            : 0;
        counted[number] = counted.ContainsKey(number) ? counted[number] + 1 : 1;
      }

      return counted.OrderBy(pair => pair.Key).ToList();
    }

    /// <summary>
    /// The figure of each season of a series that holds its seasons as nothing but a number written on the
    /// episodes: the episodes asked once under the caller's name, grouped by the number they wear, each group
    /// averaged the way a container's children are (<see cref="ReadPercentage"/>; an episode nobody watched
    /// counts as nought). A season whose episodes all answer nothing is left out of the answer entirely -
    /// inventing a nought there would rob the client of the difference between untouched and unasked.
    /// </summary>
    private static Dictionary<int, int> AverageEpisodePercentagePerSeason(IMediaLibrary mediaLibrary,
        Guid seriesId, Guid profile)
    {
      Dictionary<int, int> figures = new Dictionary<int, int>();
      MediaItemQuery episodes = new MediaItemQuery(
          new List<Guid> { EpisodeAspect.ASPECT_ID, MediaAspect.ASPECT_ID },
          null,
          new RelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES, seriesId));

      IList<MediaItem> held = mediaLibrary.Search(episodes, false, profile, false);
      if (held == null)
        return figures;

      Dictionary<int, int> sums = new Dictionary<int, int>();
      Dictionary<int, int> amounts = new Dictionary<int, int>();
      HashSet<int> somebodySaid = new HashSet<int>();
      foreach (MediaItem episode in held)
      {
        SingleMediaItemAspect numbered;
        int number = MediaItemAspect.TryGetAspect(episode.Aspects, EpisodeAspect.Metadata, out numbered)
            ? numbered.GetAttributeValue<int>(EpisodeAspect.ATTR_SEASON)
            : 0;
        int? own = ReadPercentage(episode);
        if (own.HasValue)
          somebodySaid.Add(number);
        sums[number] = sums.ContainsKey(number) ? sums[number] + (own ?? 0) : own ?? 0;
        amounts[number] = amounts.ContainsKey(number) ? amounts[number] + 1 : 1;
      }

      foreach (KeyValuePair<int, int> season in sums)
      {
        if (somebodySaid.Contains(season.Key))
          figures[season.Key] = MeanOfChildren(season.Value, amounts[season.Key]);
      }
      return figures;
    }

    /// <summary>
    /// The episodes hanging off one series, counted - the answer for a series shelf no tagger ever told a
    /// figure. The same walk <see cref="CountSeasonsOfSeries"/> makes (episodes with a file behind them,
    /// tied to this one series), stopped at the number rather than grouped by season.
    /// </summary>
    private static int CountEpisodesOfSeries(IMediaLibrary mediaLibrary, Guid seriesId)
    {
      MediaItemQuery episodes = new MediaItemQuery(
          new List<Guid> { EpisodeAspect.ASPECT_ID },
          new List<Guid> { ProviderResourceAspect.ASPECT_ID },
          new RelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES, seriesId));

      IList<MediaItem> held = mediaLibrary.Search(episodes, false, null, false);
      return held == null ? 0 : held.Count;
    }

    /// <summary>
    /// The tracks hanging off one album, counted - the answer for an album shelf whose tag stayed silent
    /// (see the album block in the listing). The same walk <see cref="FillTheTracksInsideItsAlbum"/> makes
    /// for one standing title, stopped at the number; the files are the count, not the tag.
    /// </summary>
    private static int CountTracksOfAlbum(IMediaLibrary mediaLibrary, Guid albumId)
    {
      MediaItemQuery tracks = new MediaItemQuery(
          new List<Guid> { AudioAspect.ASPECT_ID },
          new List<Guid> { ProviderResourceAspect.ASPECT_ID },
          new RelationshipFilter(AudioAspect.ROLE_TRACK, AudioAlbumAspect.ROLE_ALBUM, albumId));

      IList<MediaItem> held = mediaLibrary.Search(tracks, false, null, false);
      return held == null ? 0 : held.Count;
    }

    /// <summary>
    /// The row for one of those counted seasons, in the shape a season row has anyway - number, how much is inside,
    /// and the series it was counted from.
    /// </summary>
    private static TitleResult CountedSeasonTitle(Guid seriesId, string seriesName, int number, int episodes)
    {
      return new TitleResult
      {
        Id = CountedSeasonId(seriesId, number),
        // The reference spells a season's title as the series' name with the number on the end
        // (SeasonInfo.cs:54 <c>SEASON_FORMAT_STR = "{0} S{1:00}"</c>, applied by ToString():506), so a caller that
        // reads the answer raw reads what MP2's own screens say. The screen in this front end says "Season 1" in the
        // viewer's words instead.
        Title = SeasonTitle(seriesName, number),
        Season = number,
        Episodes = episodes,
        ParentId = seriesId
      };
    }

    /// <summary>
    /// What a season of a series is called where the library did not name it: <c>"Bones (2005) S01"</c>.
    /// </summary>
    private static string SeasonTitle(string seriesName, int number)
    {
      return (seriesName ?? string.Empty) + " S" + number.ToString("D2", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Answering "what is season N of this title" out of the episodes, for the seasons a library holds as nothing but
    /// a number written on them; <c>null</c> when the title asked about is a season of its own, in which case its id
    /// already said which season was meant and the number adds nothing to ask.
    /// </summary>
    /// <remarks>
    /// The answer is deliberately as plain as the question: a counted season has no plot, no rating and no date of
    /// its own, because the library was never told any - what it has is the series the season belongs to, the number,
    /// and how many episodes wear it, which is also all the reference's season screen has to show when nothing was
    /// tagged beside the media.
    /// </remarks>
    private ActionResult<ItemResult> CountedSeasonItem(IMediaLibrary mediaLibrary, MediaItem item, int number)
    {
      SingleMediaItemAspect aspect;
      if (MediaItemAspect.TryGetAspect(item.Aspects, SeasonAspect.Metadata, out aspect))
        return null;

      if (!MediaItemAspect.TryGetAspect(item.Aspects, SeriesAspect.Metadata, out aspect))
        return BadRequest(new ItemResult
        {
          Id = item.MediaItemId,
          Season = number,
          Error = "the title " + item.MediaItemId.ToString("D") + " is not a series, so it has no seasons to count"
        });

      Guid id = item.MediaItemId;
      List<KeyValuePair<int, int>> counted = CountSeasonsOfSeries(mediaLibrary, id);
      KeyValuePair<int, int> group = counted.FirstOrDefault(pair => pair.Key == number);
      if (group.Value == 0)
        return NotFound(new ItemResult
        {
          Id = id,
          Season = number,
          Error = "the series " + id.ToString("D") + " holds no season " + number.ToString(CultureInfo.InvariantCulture)
        });

      SingleMediaItemAspect media;
      string seriesName = MediaItemAspect.TryGetAspect(item.Aspects, MediaAspect.Metadata, out media)
          ? media.GetAttributeValue<string>(MediaAspect.ATTR_TITLE)
          : null;

      // A season held as nothing but a number was never described of its own, so it borrows the sentence of the
      // series it was counted from - the same lending FillMissingOverviewFromSeries does for episodes, with the
      // series' item already in hand. `aspect` is the show aspect here: the season-aspect check above returned.
      string seriesProse = MediaItemAspect.TryGetAspect(item.Aspects, MediaAspect.Metadata, out media)
          ? Text(media.GetAttributeValue<string>(MediaAspect.ATTR_COMMENT))
          : null;
      if (seriesProse == null)
        seriesProse = Text(aspect.GetAttributeValue<string>(SeriesAspect.ATTR_DESCRIPTION));

      // Spelled the way the library would spell a season it holds: the series' name with the number on the end.
      string title = SeasonTitle(seriesName, number);
      return new ItemResult
      {
        Id = CountedSeasonId(id, number),
        Title = title,
        SortTitle = title,
        SeriesName = seriesName,
        Season = number,
        Episodes = group.Value,
        SeriesOverview = seriesProse
      };
    }

    /// <summary>
    /// A key for one counted season: the same for the same series and number in every request and every run, so that
    /// a list can tell its rows apart and the second page agrees with the first about which row is which.
    /// </summary>
    /// <remarks>
    /// It is not a title the library holds and is never answered as one; nothing outside the list it was made for is
    /// asked with it, because the shelf inside a counted season is opened with the series' id and the season number -
    /// the two parts of it that really exist (<see cref="TitleResult.ParentId"/>). Mixed the FNV-1a way, which is
    /// four lines of arithmetic and needs no library: what matters here is only that the same two inputs make the
    /// same key and that two different seasons of one series do not make the same one.
    /// </remarks>
    private static Guid CountedSeasonId(Guid seriesId, int number)
    {
      const long OFFSET = 1469598103934665603L;
      const long PRIME = 1099511628211L;

      unchecked
      {
        byte[] key = new byte[16];

        long forwards = OFFSET;
        foreach (byte part in seriesId.ToByteArray())
          forwards = (forwards ^ part) * PRIME;
        for (int shift = 0; shift < 32; shift += 8)
          forwards = (forwards ^ ((number >> shift) & 0xFF)) * PRIME;

        // The other half is the same mixing with the two inputs taken the other way round, so that the seasons of
        // one series cannot end up with keys that differ only in their last byte.
        long backwards = OFFSET;
        for (int shift = 24; shift >= 0; shift -= 8)
          backwards = (backwards ^ ((number >> shift) & 0xFF)) * PRIME;
        foreach (byte part in seriesId.ToByteArray())
          backwards = (backwards ^ part) * PRIME;

        for (int i = 0; i < 8; i++)
        {
          key[i] = (byte)(forwards >> (i * 8));
          key[8 + i] = (byte)(backwards >> (i * 8));
        }

        return new Guid(key);
      }
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
    /// (LocalSeriesFanartProvider.cs:77,81), a season on its series by <c>ROLE_SEASON</c> to
    /// <c>ROLE_SERIES</c> (SeasonSeriesRelationshipExtractor.cs:58,68), and a collection's films by
    /// <c>ROLE_MOVIE</c> to <c>ROLE_MOVIE_COLLECTION</c> (LocalMovieCollectionFanartProvider.cs:75).
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

      if (childAspectId == SeasonAspect.ASPECT_ID && has.Contains(SeriesAspect.ASPECT_ID))
      {
        // The season-to-series link is MP2's own (SeasonSeriesRelationshipExtractor.cs:58,68): a season hangs on
        // its series by ROLE_SEASON against ROLE_SERIES, which is a different pair of roles from the one its
        // episodes hang on it by. A viewer standing on a series and pressing into it is standing above that link.
        role = SeasonAspect.ROLE_SEASON;
        linkedRole = SeriesAspect.ROLE_SERIES;
        return true;
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

      // The kinds of story this title is told in - the same reading the details answer gives, so a title cannot
      // name one genre on its shelf and another on the screen standing in front of the viewer.
      result.Genres = ReadGenres(item);

      // The short films the title advertises itself with, which the context menu of a shelf asks about while the
      // viewer is still standing at the shelf. Read here for the same reason the genres are: an answer that left
      // them out could only offer "Trailer" for every title or for none.
      result.Trailers = ReadTrailers(item);

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

      // A season names the season it is and how many episodes the library counts inside it, which are the two
      // facts the reference wrote above a season's poster (MediaItemCoverSeasonDetails.inc:53,63). MP2's own title
      // for a season is "<series> S<nn>" (SeasonInfo.SEASON_FORMAT_STR) - a name that repeats the series the
      // viewer is already standing in - so the number is what a row of seasons is really about, and it travels
      // on its own rather than being parsed back out of a sentence.
      SingleMediaItemAspect seasonAspect;
      if (MediaItemAspect.TryGetAspect(item.Aspects, SeasonAspect.Metadata, out seasonAspect))
      {
        int season = seasonAspect.GetAttributeValue<int>(SeasonAspect.ATTR_SEASON);
        if (season > 0)
          result.Season = season;

        int episodes = seasonAspect.GetAttributeValue<int>(SeasonAspect.ATTR_NUM_EPISODES);
        if (episodes > 0)
          result.Episodes = episodes;
      }

      // A series says how many episodes the library counts under it, which is what the reference's series
      // filter item read of the show aspect (SeriesFilterItem.cs:80-88): the tagged total when the tagger
      // wrote one, and the count of episodes that are really there when it did not. The shelf says it on
      // the right of the row, where an album says its tracks.
      SingleMediaItemAspect seriesAspect;
      if (MediaItemAspect.TryGetAspect(item.Aspects, SeriesAspect.Metadata, out seriesAspect))
      {
        int total = seriesAspect.GetAttributeValue<int>(SeriesAspect.ATTR_NUM_EPISODES);
        if (total > 0)
          result.Episodes = total;
        else
        {
          int available = seriesAspect.GetAttributeValue<int>(SeriesAspect.ATTR_AVAILABLE_EPISODES);
          if (available > 0)
            result.Episodes = available;
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

      // And the whole number behind it, which the row's tick, the watched corner and the title's menu are all
      // read from. Said beside Resume rather than instead of it because the two answer different questions: this
      // title may be at nought, at a hundred, or at nothing at all, and each of those is an answer.
      result.Percentage = askedAsSomebody ? ReadPercentage(item) : null;

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
      int? stored = ReadPercentage(item);
      if (!stored.HasValue)
        return null;

      // A hundred is not a position to resume from and nought is nothing to resume to, which is MP2's own reading
      // of the same number (BaseContinueWatchMediaListProvider.cs:43-46).
      int percentage = stored.Value;
      if (percentage <= 0 || percentage >= 100)
        return null;

      return percentage;
    }

    /// <summary>
    /// The caller's own play percentage for this title, nought to a hundred, or null when their user data holds
    /// nothing - which is what a title nobody has opened looks like, and what every title looks like to a request
    /// that named nobody, since such a request carries no user data at all.
    /// </summary>
    /// <remarks>
    /// The whole range, where <see cref="ReadResume"/> reports only the stretch between the ends: the two ends are
    /// the answers a screen needs for themselves - "watched through" is what puts <i>Set unplayed</i> in a menu
    /// (<c>AbstractWatchedAction.cs:47-49</c>) and what a home tile's full bar is drawn from.
    /// </remarks>
    private static int? ReadPercentage(MediaItem item)
    {
      if (item == null || item.UserData == null)
        return null;

      string stored;
      if (!item.UserData.TryGetValue(UserDataKeysKnown.KEY_PLAY_PERCENTAGE, out stored))
        return null;

      int percentage;
      if (!int.TryParse(stored, NumberStyles.Integer, CultureInfo.InvariantCulture, out percentage))
        return null;

      return percentage;
    }

    /// <summary>
    /// The mean of a container's children's figures, in whole per cent - with one law the rounding must not
    /// break: nought is the answer only where nought was watched.
    /// </summary>
    /// <remarks>
    /// The key holds whole per cent, so a mean of eight children of which one stands at two per cent is a
    /// quarter, and a quarter rounds to nought. Nought is the figure of the untouched, and a shelf that asks
    /// "what did this viewer leave half way" reads it as "nothing of this was opened" - which throws the series
    /// out of the shelf its own episode put it on. A fraction therefore always leaves at least a one behind.
    /// </remarks>
    private static int MeanOfChildren(int sum, int count)
    {
      int figure = (int)Math.Round(sum / (double)count, MidpointRounding.AwayFromZero);
      return figure == 0 && sum > 0 ? 1 : figure;
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
