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