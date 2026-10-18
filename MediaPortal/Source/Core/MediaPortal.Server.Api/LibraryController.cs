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
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// What the media database consists of: the media item aspects the server knows, and how many items each of
  /// them holds.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The endpoint describes the schema rather than the content, and that split is deliberate. The aspect
  /// metadata is what a front end needs before it can ask for anything - the names, the attributes with their
  /// types, and which of them are indexed - and it is the only part of the media library that is not anybody's
  /// data. Everything below that line (titles, watch state, playlists, users) waits for the token endpoint,
  /// because an HTTP API that hands out a household's viewing history without credentials is not an acceptable
  /// default; see <see cref="ServerInfoController"/> for the same reasoning and for why
  /// <c>MediaPortalAuthorizeAttribute</c> cannot be applied yet.
  /// </para>
  /// <para>
  /// It answers from <see cref="IMediaLibrary"/>, resolved on the request thread like every other MP2 service
  /// here. Three of its states are worth distinguishing: no media library in the process at all (a host that did
  /// not register the backend), a library that has not finished starting up, and a library in maintenance mode.
  /// The first two are answered with <c>503</c> - the endpoint would rather say "not yet" than describe an empty
  /// library it knows nothing about - the last one the library itself reports as a count of zero. The resource
  /// server starts before the media library does, so a client that connects as early as it can does hit the
  /// second state, and the <c>503</c> tells it to ask again.
  /// </para>
  /// <para>
  /// The server's own types are never serialized. <see cref="MediaItemAspectMetadata"/> carries index ids, XML
  /// serialization helpers and back-references into the database layer; a client that reads the JSON would be
  /// coupled to all of it. Each answer is therefore mapped to a result object of its own.
  /// </para>
  /// </remarks>
  [ApiController]
  [Route("api/[controller]")]
  public class LibraryController : ControllerBase
  {
    /// <summary>
    /// One attribute of a media item aspect, as a client sees it.
    /// </summary>
    public class AttributeResult
    {
      public string Name { get; set; }

      /// <summary>
      /// Full name of the CLR type, e.g. <c>System.String</c>. The names of these types are stable enough to
      /// build against; the attributes that carry them are listed so a client can notice a change.
      /// </summary>
      public string Type { get; set; }

      /// <summary>
      /// Whether the server keeps a database index over this attribute, i.e. whether filtering or sorting by it
      /// is cheap.
      /// </summary>
      public bool Indexed { get; set; }

      /// <summary>
      /// Whether the attribute holds a list of values rather than a single one.
      /// </summary>
      public bool Collection { get; set; }
    }

    /// <summary>
    /// One media item aspect: the unit the media database stores and sorts by.
    /// </summary>
    public class AspectResult
    {
      public Guid Id { get; set; }

      /// <summary>
      /// The aspect's name, e.g. <c>Core</c> or <c>VideoFile</c>. Clients address aspects by name in
      /// <c>GET api/library/count</c>; the ids are in the answer because that is what the query APIs take.
      /// </summary>
      public string Name { get; set; }

      /// <summary>
      /// A system aspect cannot be deleted by a plugin; every aspect of a fresh install is one.
      /// </summary>
      public bool SystemAspect { get; set; }

      /// <summary>
      /// A transient aspect is not stored in the database, so it never appears in a count.
      /// </summary>
      public bool Transient { get; set; }

      /// <summary>
      /// Set on an aspect that holds sub-data of another aspect (the timestamps of a recording, for example),
      /// and names the aspect it belongs to.
      /// </summary>
      public Guid? ReferencingAspectId { get; set; }

      public IReadOnlyList<AttributeResult> Attributes { get; set; }
    }

    /// <summary>
    /// The answer of <c>GET api/library/aspects</c>.
    /// </summary>
    public class AspectsResult
    {
      /// <summary>
      /// Number of aspects, so that a client can tell a truncated answer from a complete one without counting.
      /// </summary>
      public int Count { get; set; }

      /// <summary>
      /// The aspects, ordered by name. The order is not the database's - a client should be able to diff two
      /// answers from two servers.
      /// </summary>
      public IReadOnlyList<AspectResult> Aspects { get; set; }
    }

    /// <summary>
    /// The answer of an endpoint that could not answer: the reason, in the server's words.
    /// </summary>
    public class ErrorResponse
    {
      public string Error { get; set; }
    }

    /// <summary>
    /// The answer of <c>GET api/library/count</c>.
    /// </summary>
    public class CountResult
    {
      /// <summary>
      /// The aspect the count is about, or <c>null</c> when every media item was counted.
      /// </summary>
      public string Aspect { get; set; }

      public Guid? AspectId { get; set; }

      /// <summary>
      /// Number of media items. Virtual items - directories, and everything the importer only registered to
      /// browse by - are included, which is what <c>IMediaLibrary</c> reports by default.
      /// </summary>
      public int Count { get; set; }
    }

    /// <summary>
    /// Lists the media item aspects the server manages, with their attributes.
    /// </summary>
    [HttpGet("aspects")]
    public ActionResult<AspectsResult> GetAspects()
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      try
      {
        // The dictionary can hold a null value: while the storage of an aspect is being created the library keeps
        // a placeholder under its id and fills the metadata in once the tables exist
        // (MIA_Management.AddMediaItemAspectStorage). An aspect whose storage does not exist yet has no
        // describable shape, so it is not in the answer - unlike the placeholder, an answer claiming it would be
        // a lie.
        List<AspectResult> aspects = mediaLibrary.GetManagedMediaItemAspectMetadata().Values
            .Where(metadata => metadata != null)
            .Select(ToAspectResult)
            .OrderBy(aspect => aspect.Name, StringComparer.Ordinal)
            .ToList();

        return new AspectsResult { Count = aspects.Count, Aspects = aspects };
      }
      catch (IllegalCallException ex)
      {
        return NotReady(ex);
      }
    }

    /// <summary>
    /// Counts the media items that carry the given aspect, or all of them when no aspect is named.
    /// </summary>
    /// <param name="aspect">An aspect name as <c>GET api/library/aspects</c> reports it, e.g. <c>Core</c>.</param>
    [HttpGet("count")]
    public ActionResult<CountResult> GetCount([FromQuery] string aspect)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      try
      {
        if (string.IsNullOrEmpty(aspect))
          return new CountResult
          {
            Count = mediaLibrary.CountMediaItems(new List<Guid>(), null, false, true)
          };

        IDictionary<Guid, MediaItemAspectMetadata> metadata = mediaLibrary.GetManagedMediaItemAspectMetadata();
        // Names on the wire, ids in the database. Compared without case because that is how a person types an
        // aspect name, and the aspect names of a server are unique either way. The null check is for the
        // placeholder described in GetAspects.
        KeyValuePair<Guid, MediaItemAspectMetadata> match = metadata.FirstOrDefault(entry =>
            entry.Value != null && string.Equals(entry.Value.Name, aspect, StringComparison.OrdinalIgnoreCase));
        if (match.Value == null)
          return NotFound();

        return new CountResult
        {
          Aspect = match.Value.Name,
          AspectId = match.Key,
          Count = mediaLibrary.CountMediaItems(new[] { match.Key }, null, false, true)
        };
      }
      catch (IllegalCallException ex)
      {
        return NotReady(ex);
      }
    }

    /// <summary>
    /// The answer for a media library that cannot answer yet. The reason goes into the body because "the server
    /// is not ready" is only useful to whoever reads it if it says what is not ready.
    /// </summary>
    private static ObjectResult NotReady(IllegalCallException exception)
    {
      // Asked for without being demanded: this method is the answer to a request that is already going wrong, and
      // a report that throws because nothing registered a logger would turn the 503 the caller can act on back
      // into the 500 it cannot. The server always has a logger; a host that builds these controllers without one
      // - a test, for instance - still gets the 503.
      ILogger logger = ServiceRegistration.Get<ILogger>(false);
      if (logger != null)
        logger.Info("LibraryController: the media library refused a request, answering 503: {0}", exception.Message);
      return new ObjectResult(new ErrorResponse { Error = exception.Message }) { StatusCode = StatusCodes.Status503ServiceUnavailable };
    }

    private static AspectResult ToAspectResult(MediaItemAspectMetadata metadata)
    {
      return new AspectResult
      {
        Id = metadata.AspectId,
        Name = metadata.Name,
        SystemAspect = metadata.IsSystemAspect,
        Transient = metadata.IsTransientAspect,
        ReferencingAspectId = metadata.ReferencingAspectId,
        Attributes = metadata.AttributeSpecifications.Values
            .Select(attribute => new AttributeResult
            {
              Name = attribute.AttributeName,
              Type = attribute.AttributeType.FullName,
              Indexed = attribute.IsIndexed,
              Collection = attribute.IsCollectionAttribute
            })
            .OrderBy(attribute => attribute.Name, StringComparer.Ordinal)
            .ToList()
      };
    }
  }
}
