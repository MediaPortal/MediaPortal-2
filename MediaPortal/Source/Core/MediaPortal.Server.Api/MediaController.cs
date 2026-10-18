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
using System.Linq;
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
    [HttpGet("titles")]
    public ActionResult<TitlesResult> GetTitles([FromQuery] string aspect, [FromQuery] string sort,
        [FromQuery] string order, [FromQuery] int limit, [FromQuery] int offset)
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

        MediaItemQuery query = new MediaItemQuery(necessary, optional, null);
        string sortBy = ReadSort(sort);
        ISortInformation sortInformation = BuildSort(sortBy, order);
        if (sortInformation != null)
          query.SetSortInformation(new List<ISortInformation> { sortInformation });
        query.Offset = (uint)skip;
        query.Limit = (uint)take;

        // The last argument is includeVirtual, and false is the answer to "give me things to watch". The one
        // before it is a user profile, which would add per-user data (watched state) the day this front end has
        // one; nothing here asks for it, so nobody is asked to name themselves for it.
        IList<MediaItem> items = mediaLibrary.Search(query, false, null, false);

        List<TitleResult> titles = items.Select(ToTitle).ToList();

        return new TitlesResult
        {
          Aspect = match.Value.Name,
          AspectId = aspectId,
          Total = mediaLibrary.CountMediaItems(new[] { aspectId }, null, false, false),
          Offset = skip,
          Limit = take,
          Returned = titles.Count,
          Sort = sortBy + (IsDescending(order) ? " descending" : ""),
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
        IList<MediaItem> items = mediaLibrary.Search(query, false, null, false);
        MediaItem item = items == null ? null : items.FirstOrDefault();

        if (item == null)
          return NotFound(new ItemResult { Id = id, Error = "this server has no title with the id " + id });

        return ToItem(item);
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
    /// Turns a media item into the answer of <see cref="GetItem"/>.
    /// </summary>
    /// <remarks>
    /// Every attribute read is one the item may not carry, so each is read inside its aspect's presence check -
    /// and a missing one stays null rather than becoming zero, which for a runtime or a season number would be a
    /// claim about the media that the database never made.
    /// </remarks>
    private static ItemResult ToItem(MediaItem item)
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
    /// <b>The title's own counters, not one viewer's.</b> MP2 also keeps per-profile counts and resume positions
    /// through <see cref="IMediaLibrary.NotifyUserPlayback(Guid, Guid, int, bool)"/>; this does not write those,
    /// because they belong to a user profile and this front end has no profile to name - a bearer token says which
    /// machine may ask, not which person is sitting on the sofa. Several viewers sharing one "watched" flag is a
    /// simplification, and it is written down here rather than discovered by whoever adds the profile picker.
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
        // Asked for before the write, because "there is no such title" and "the title is there and now says it was
        // played" are different answers; a library that never heard of the id must not be told to mark it, and a
        // client must not be told it recorded something that went nowhere.
        if (FindTitle(mediaLibrary, id) == null)
          return NotFound(new PlaybackResult { Id = id, Error = "this server has no title with the id " + id });

        bool watched = playback.Percentage >= 100;
        mediaLibrary.NotifyPlayback(id, watched);
        return ToPlayback(id, FindTitle(mediaLibrary, id), watched);
      }
      catch (IllegalCallException ex)
      {
        return NotReadyPlayback(ex, id);
      }
    }

    /// <summary>
    /// The one item with this id, or null when the library has none.
    /// </summary>
    /// <remarks>
    /// Only the Media aspect is necessary and nothing optional is asked for: this is the lookup that decides
    /// "is there such a title, and what does it say about being played", and a details answer would be a second
    /// read of the same row for no reason.
    /// </remarks>
    private static MediaItem FindTitle(IMediaLibrary mediaLibrary, Guid id)
    {
      MediaItemQuery query = new MediaItemQuery(
          new List<Guid> { MediaAspect.ASPECT_ID }, null, new MediaItemIdFilter(id));
      IList<MediaItem> items = mediaLibrary.Search(query, false, null, false);
      return items == null ? null : items.FirstOrDefault();
    }

    /// <summary>
    /// The playback counters of a title, as the library holds them now.
    /// </summary>
    private static PlaybackResult ToPlayback(Guid id, MediaItem item, bool watched)
    {
      PlaybackResult result = new PlaybackResult { Id = id, Watched = watched };

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
    /// The three sorts a title list has: by name, by year, or by none at all.
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
      if (string.Equals(sort, "none", StringComparison.OrdinalIgnoreCase))
        return "none";
      return "title";
    }

    private static bool IsDescending(string order)
    {
      return string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase)
          || string.Equals(order, "descending", StringComparison.OrdinalIgnoreCase);
    }

    private static ISortInformation BuildSort(string sortBy, string order)
    {
      SortDirection direction = IsDescending(order) ? SortDirection.Descending : SortDirection.Ascending;
      switch (sortBy)
      {
        case "title":
          return new AttributeSortInformation(MediaAspect.ATTR_SORT_TITLE, direction);
        case "year":
          return new AttributeSortInformation(MediaAspect.ATTR_RECORDINGTIME, direction);
        default:
          return null;
      }
    }

    /// <summary>
    /// Turns a media item into the row a title list shows.
    /// </summary>
    private static TitleResult ToTitle(MediaItem item)
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

      result.Stub = item.IsStub;

      result.Path = ReadPath(item);
      return result;
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