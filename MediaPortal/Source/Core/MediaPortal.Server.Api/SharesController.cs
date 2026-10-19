#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediaPortal.Backend.Exceptions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// The folders the media library is built from, and the import that fills it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Why this exists before anything else on the write side.</b> <see cref="MediaController"/> can list
  /// titles, count them and hand out their pictures, and on a fresh server all of those answers are empty - not
  /// because the library is broken but because nobody ever told it where the files are. A share is that
  /// instruction, and it is the one thing a front end cannot do by itself: the folders are on the machine the
  /// server runs on, so the decision has to be taken over the network that separates the two. Everything else
  /// the media centre shows is downstream of this request.
  /// </para>
  /// <para>
  /// <b>Registering is the trigger.</b> <see cref="IMediaLibrary.RegisterShare"/> already schedules the import of
  /// a local share (MediaLibrary.cs:856 <c>TryScheduleLocalShareImport</c>, which passes the share's categories
  /// and <c>includeSubDirectories: true</c>), so <c>POST api/media/shares</c> does not have to call the importer
  /// to get a library filled - and does not, because calling it a second time would queue the same tree twice.
  /// <see cref="ImportShare"/> is therefore for the case the automatic path does not cover: a folder that
  /// changed after it was first read.
  /// </para>
  /// <para>
  /// <b>The categories are not decoration.</b> An import job is bound to a set of metadata extractors, and that
  /// set is resolved from the share's media categories when the job is scheduled
  /// (ImporterWorkerNewGen.cs:464). A category the server does not know yields no extractors, and a job with no
  /// extractors walks the whole tree, asks nobody, finishes, and leaves an empty library without a word. So a
  /// name that is not registered is refused with the list of names that are, and a request that names none is
  /// read as "everything this server can recognise" rather than as nothing at all. The names come from
  /// <see cref="IMediaAccessor.MediaCategories"/>, which is where the plugins that provide extractors register
  /// theirs, so what a client may ask for is exactly what a server can answer - and
  /// <see cref="GetShares"/> reports them for the same reason: the screen that adds a folder should offer the
  /// choices rather than make up spellings.
  /// </para>
  /// <para>
  /// <b>Paths are the server's, not the caller's.</b> A share names a directory on a system, identified by
  /// <see cref="Share.SystemId"/>, and the importer only reads folders on the machine the server itself runs on.
  /// That is why a request carries a plain filesystem path (as the server's OS spells it) and gets a plain path
  /// back when the share is a local one, while <see cref="ShareResult.ResourcePath"/> always carries the
  /// serialized <see cref="ResourcePath"/> - the provider chain the library really stores, and the only form
  /// that survives a share on another system. A remote share is listed faithfully and refused by
  /// <see cref="ImportShare"/>, because "scheduled" would be a promise this server cannot keep.
  /// </para>
  /// <para>
  /// Like the rest of the content endpoints this one is behind <see cref="MediaPortalAuthorizeAttribute"/>: a
  /// request that can add a share can make the server read any directory its user can read and publish its
  /// contents to every screen, which is not something to hand to whoever can reach the port.
  /// </para>
  /// </remarks>
  [ApiController]
  [Route("api/media")]
  [MediaPortalAuthorize]
  public class SharesController : ControllerBase
  {
    /// <summary>
    /// What a caller asks for when it points the server at a folder.
    /// </summary>
    public class ShareRequest
    {
      /// <summary>
      /// Directory to import, spelled the way the server's filesystem spells it. Required.
      /// </summary>
      public string Path { get; set; }

      /// <summary>
      /// Name to show for the share. Optional; the folder's own name is used when it is left out.
      /// </summary>
      public string Name { get; set; }

      /// <summary>
      /// Media categories the folder holds, as <see cref="SharesResult.MediaCategories"/> reports them.
      /// Optional; every category the server knows is used when it is left out.
      /// </summary>
      public List<string> MediaCategories { get; set; }

      /// <summary>
      /// Whether the server should watch the folder for changes. Optional, and on by default; a filesystem that
      /// cannot be watched is reported by the library and does not fail the request.
      /// </summary>
      public bool? UseShareWatcher { get; set; }
    }

    /// <summary>
    /// One share, as a screen shows it.
    /// </summary>
    public class ShareResult
    {
      public Guid Id { get; set; }

      public string Name { get; set; }

      /// <summary>
      /// The directory in the server's own filesystem notation, or null when the share is not on the local
      /// filesystem - in which case <see cref="ResourcePath"/> is the only true description of it.
      /// </summary>
      public string Path { get; set; }

      /// <summary>
      /// The serialized resource path, i.e. the provider chain the library stores. Always set.
      /// </summary>
      public string ResourcePath { get; set; }

      /// <summary>
      /// The system the directory belongs to.
      /// </summary>
      public string SystemId { get; set; }

      /// <summary>
      /// Whether that system is the one the server runs on, which is what decides whether an import can run.
      /// </summary>
      public bool Local { get; set; }

      public List<string> MediaCategories { get; set; }

      public bool UseShareWatcher { get; set; }
    }

    /// <summary>
    /// The answer of <c>GET api/media/shares</c>.
    /// </summary>
    public class SharesResult
    {
      public int Count { get; set; }

      /// <summary>
      /// The shares, ordered by name so that two servers can be diffed.
      /// </summary>
      public List<ShareResult> Shares { get; set; }

      /// <summary>
      /// Every media category this server can import, which is the set a screen offers as choices.
      /// </summary>
      public List<string> MediaCategories { get; set; }
    }

    /// <summary>
    /// One import job the server has in hand.
    /// </summary>
    public class ImportJobResult
    {
      /// <summary><c>Import</c> or <c>Refresh</c>.</summary>
      public string Type { get; set; }

      public string Path { get; set; }

      public bool IncludeSubDirectories { get; set; }
    }

    /// <summary>
    /// The answer of <c>GET api/media/import</c>.
    /// </summary>
    public class ImportStatusResult
    {
      /// <summary>
      /// Number of jobs the worker still holds. A share was imported completely when this dropped to zero after
      /// having been above it, which is what a "still importing" indicator watches. It is a count of jobs, not of
      /// files: one job is one scheduled tree and can hold a hundred thousand files.
      /// </summary>
      public int Pending { get; set; }

      /// <summary>
      /// Whether the worker was suspended - by a shutdown, or because its connection to the library was dropped.
      /// A suspended worker holds its jobs and does not work on them.
      /// </summary>
      public bool Suspended { get; set; }

      public List<ImportJobResult> Jobs { get; set; }
    }

    /// <summary>
    /// The body of an answer that refused: the reason, in words a screen can show.
    /// </summary>
    public class ErrorResponse
    {
      public string Error { get; set; }
    }

    /// <summary>
    /// Lists the shares, and the media categories a new one may be given.
    /// </summary>
    [HttpGet("shares")]
    public ActionResult<SharesResult> GetShares()
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      try
      {
        string localSystemId = LocalSystemId();
        IDictionary<Guid, Share> shares = mediaLibrary.GetShares(null);
        List<ShareResult> results = (shares != null ? shares.Values : Enumerable.Empty<Share>())
            .OrderBy(share => share.Name, StringComparer.OrdinalIgnoreCase)
            .Select(share => ToResult(share, localSystemId))
            .ToList();

        return new SharesResult
        {
          Count = results.Count,
          Shares = results,
          MediaCategories = KnownCategories()
        };
      }
      catch (IllegalCallException ex)
      {
        return NotReady(ex);
      }
    }

    /// <summary>
    /// Points the server at a folder, registers it as a share and starts importing it.
    /// </summary>
    /// <param name="request">The folder, and what is expected to be in it.</param>
    /// <remarks>
    /// <para>
    /// The answer is <c>201</c> while the import is still running: registering the share queues the work
    /// (see the class remarks) and the queue is not waited on, because a first import of a film collection is
    /// measured in minutes and an HTTP request that waits for it is a request that times out.
    /// <c>GET api/media/titles</c> and <c>GET api/media/import</c> are how a caller finds out how far it got.
    /// </para>
    /// <para>
    /// The folder is required to exist. A share whose path does not exist is not a promise to import later - the
    /// library keeps it, the importer finds nothing, and the reason the shelf is empty is now a database row.
    /// The screen that typed the path can fix a typo; a person looking at the library months later cannot.
    /// </para>
    /// </remarks>
    [HttpPost("shares")]
    public ActionResult<ShareResult> AddShare([FromBody] ShareRequest request)
    {
      if (request == null || string.IsNullOrWhiteSpace(request.Path))
        return BadRequest(new ErrorResponse { Error = "say which folder to import" });

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>(false);
      // Asked for along with the library: without the accessor the server cannot say which categories it can
      // import, and a 400 blaming the caller's spelling for the server's own missing service would be a lie.
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>(false);
      if (mediaLibrary == null || systemResolver == null || mediaAccessor == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      string path;
      try
      {
        path = System.IO.Path.GetFullPath(request.Path);
      }
      catch (Exception ex) // ArgumentException, InvalidPathException, PathTooLongException: all one answer
      {
        return BadRequest(new ErrorResponse { Error = "'" + request.Path + "' is not a path this server can use: " + ex.Message });
      }

      if (!Directory.Exists(path))
        return BadRequest(new ErrorResponse
        {
          Error = "the server cannot see a folder at '" + path + "'. A share is a folder on the machine the " +
              "server runs on, not on the one asking."
        });

      List<string> categories;
      string categoryError;
      if (!TryResolveCategories(request.MediaCategories, out categories, out categoryError))
        return BadRequest(new ErrorResponse { Error = categoryError });

      string name = string.IsNullOrWhiteSpace(request.Name) ? FolderName(path) : request.Name.Trim();
      Share share = Share.CreateNewShare(systemResolver.LocalSystemId,
          LocalFsResourceProviderBase.ToResourcePath(path), name,
          request.UseShareWatcher ?? true, categories);

      try
      {
        mediaLibrary.RegisterShare(share);
      }
      catch (ShareExistsException ex)
      {
        // Not "the request was wrong" but "the thing you wanted is already there", so the answer names it - the
        // caller wanted this folder in its library and it is, under an id it can now use.
        Share existing = FindShareForPath(mediaLibrary, share);
        return new ObjectResult(new ErrorResponse
        {
          Error = existing == null
              ? ex.Message
              : ex.Message + " (as share '" + existing.ShareId + "' named '" + existing.Name + "')"
        })
        {
          StatusCode = StatusCodes.Status409Conflict
        };
      }
      catch (IllegalCallException ex)
      {
        return NotReady(ex);
      }

      return StatusCode(StatusCodes.Status201Created, ToResult(share, systemResolver.LocalSystemId));
    }

    /// <summary>
    /// Removes a share.
    /// </summary>
    /// <param name="id">The share id, as <c>GET api/media/shares</c> reported it.</param>
    /// <remarks>
    /// The library drops the share, the items reached through it and any import running for it
    /// (<see cref="IMediaLibrary.RemoveShare"/>), which is a bigger deletion than the one the caller clicked.
    /// It is still the honest meaning of the verb, so no confirmation flag is invented here - a screen that wants
    /// to warn warns about the items, not about the share.
    /// </remarks>
    [HttpDelete("shares/{id}")]
    public IActionResult RemoveShare(Guid id)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      try
      {
        // Asked for first: the library's own RemoveShare walks into the share without looking, and a 500 saying
        // "object reference not set" is a worse answer to "no such share" than 404 saying that.
        if (mediaLibrary.GetShare(id) == null)
          return NotFound(new ErrorResponse { Error = "this server has no share with the id " + id });

        mediaLibrary.RemoveShare(id);
        return NoContent();
      }
      catch (IllegalCallException ex)
      {
        return NotReady(ex);
      }
    }

    /// <summary>
    /// Queues another import of a share's folder.
    /// </summary>
    /// <param name="id">The share id, as <c>GET api/media/shares</c> reported it.</param>
    /// <remarks>
    /// <para>
    /// For a folder that gained files after it was registered; adding the share already imported it once. The
    /// categories of the share are used, so a share registered as video is re-read by the video extractors and
    /// not by every extractor the server has.
    /// </para>
    /// <para>
    /// The answer is <c>202</c>, not a result: the caller asked for work to be queued and queueing it is all this
    /// endpoint can promise before the work is done. <c>GET api/media/import</c> says how the queue is doing.
    /// </para>
    /// </remarks>
    [HttpPost("shares/{id}/import")]
    public ActionResult<ImportStatusResult> ImportShare(Guid id)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>(false);
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>(false);
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>(false);
      if (mediaLibrary == null || importerWorker == null || systemResolver == null || mediaAccessor == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      Share share;
      List<string> categories;
      string categoryError;
      try
      {
        share = mediaLibrary.GetShare(id);
        if (share == null)
          return NotFound(new ErrorResponse { Error = "this server has no share with the id " + id });

        if (share.SystemId != systemResolver.LocalSystemId)
          return new ObjectResult(new ErrorResponse
          {
            Error = "share '" + id + "' is on another system ('" + share.SystemId + "'), and this server's " +
                "importer only reads folders it can reach itself"
          })
          {
            StatusCode = StatusCodes.Status409Conflict
          };

        // A share registered elsewhere (an older server, a plugin) can hold categories this server has never
        // heard of, which would queue a job with no extractors. Same rule as when a share is added.
        if (!TryResolveCategories(share.MediaCategories, out categories, out categoryError))
          return new ObjectResult(new ErrorResponse { Error = categoryError })
          {
            StatusCode = StatusCodes.Status409Conflict
          };
      }
      catch (IllegalCallException ex)
      {
        return NotReady(ex);
      }

      importerWorker.ScheduleImport(share.BaseResourcePath, categories, true);
      return Accepted(ReadStatus(importerWorker));
    }

    /// <summary>
    /// How many import jobs the server still has, and whether it is working on them.
    /// </summary>
    [HttpGet("import")]
    public ActionResult<ImportStatusResult> GetImportStatus()
    {
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>(false);
      if (importerWorker == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      return ReadStatus(importerWorker);
    }

    /// <summary>
    /// The importer's queue as a result object.
    /// </summary>
    /// <remarks>
    /// A job is in the collection while the worker holds it and is taken out when it finishes, so the count going
    /// to zero is the end of the import - there is no separate "finished" state to watch, and none is invented
    /// here. The worker's own <c>ImportJobInformation.State</c> is deliberately not reported: the current
    /// worker's job state lives in the completion task of its controller (the <c>State</c> field is marked as a
    /// leftover of the old worker), so a state string from it would be a value nothing maintains.
    /// </remarks>
    private static ImportStatusResult ReadStatus(IImporterWorker importerWorker)
    {
      ICollection<ImportJobInformation> jobs = importerWorker.ImportJobs;
      List<ImportJobResult> results = (jobs != null ? jobs : Enumerable.Empty<ImportJobInformation>())
          .Select(job => new ImportJobResult
          {
            Type = job.JobType.ToString(),
            Path = job.BasePath != null ? (LocalFsResourceProviderBase.ToDosPath(job.BasePath) ?? job.BasePath.Serialize()) : null,
            IncludeSubDirectories = job.IncludeSubDirectories
          })
          .OrderBy(job => job.Path, StringComparer.OrdinalIgnoreCase)
          .ToList();

      return new ImportStatusResult
      {
        Pending = results.Count,
        Suspended = importerWorker.IsSuspended,
        Jobs = results
      };
    }

    /// <summary>
    /// The categories to import with, and why that is a question at all: see the class remarks.
    /// </summary>
    /// <param name="requested">What was asked for; null or empty means "everything this server knows".</param>
    /// <param name="categories">The categories to use, never null when the method returns true.</param>
    /// <param name="error">What to tell the caller when the method returns false.</param>
    /// <returns>False when a requested category is unknown to the server.</returns>
    private static bool TryResolveCategories(IEnumerable<string> requested, out List<string> categories, out string error)
    {
      categories = null;
      error = null;

      List<string> known = KnownCategories();
      List<string> wanted = requested == null
          ? new List<string>()
          : requested.Where(category => !string.IsNullOrWhiteSpace(category))
              .Select(category => category.Trim())
              .Distinct(StringComparer.OrdinalIgnoreCase)
              .ToList();

      if (wanted.Count == 0)
      {
        categories = known;
        return true;
      }

      List<string> unknown = wanted
          .Where(category => !known.Any(name => string.Equals(name, category, StringComparison.OrdinalIgnoreCase)))
          .ToList();
      if (unknown.Count > 0)
      {
        error = "this server has no media category " + Quote(unknown) +
            (known.Count > 0 ? "; it knows " + Quote(known) : ", and no plugin has registered one");
        return false;
      }

      // The server's own spelling wins, so that a share stored as "video" and one stored as "Video" do not become
      // two different things in a library.
      categories = wanted
          .Select(want => known.First(name => string.Equals(name, want, StringComparison.OrdinalIgnoreCase)))
          .ToList();
      return true;
    }

    /// <summary>
    /// The media categories the plugins of this process registered, ordered.
    /// </summary>
    private static List<string> KnownCategories()
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>(false);
      IDictionary<string, MediaCategory> registered = mediaAccessor != null ? mediaAccessor.MediaCategories : null;
      return registered == null
          ? new List<string>()
          : registered.Keys.OrderBy(name => name, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The share of the local system that points at the same directory as <paramref name="share"/>, or null.
    /// </summary>
    private static Share FindShareForPath(IMediaLibrary mediaLibrary, Share share)
    {
      IDictionary<Guid, Share> shares = mediaLibrary.GetShares(share.SystemId);
      return shares == null ? null : shares.Values.FirstOrDefault(other => other.BaseResourcePath == share.BaseResourcePath);
    }

    /// <summary>
    /// The id of the system this process runs on, or null when no system resolver is registered.
    /// </summary>
    private static string LocalSystemId()
    {
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>(false);
      return systemResolver != null ? systemResolver.LocalSystemId : null;
    }

    /// <summary>
    /// The last component of a directory path, for a share that was added without a name.
    /// </summary>
    private static string FolderName(string path)
    {
      string name = new DirectoryInfo(path).Name;
      return string.IsNullOrEmpty(name) ? path : name;
    }

    /// <summary>
    /// Quotes a list of names for an error message.
    /// </summary>
    private static string Quote(List<string> names)
    {
      return string.Join(", ", names.Select(name => "'" + name + "'"));
    }

    private static ShareResult ToResult(Share share, string localSystemId)
    {
      ResourcePath resourcePath = share.BaseResourcePath;
      return new ShareResult
      {
        Id = share.ShareId,
        Name = share.Name,
        Path = LocalFsResourceProviderBase.ToDosPath(resourcePath),
        ResourcePath = resourcePath != null ? resourcePath.Serialize() : null,
        SystemId = share.SystemId,
        Local = localSystemId != null && share.SystemId == localSystemId,
        MediaCategories = share.MediaCategories == null
            ? new List<string>()
            : share.MediaCategories.OrderBy(category => category, StringComparer.Ordinal).ToList(),
        UseShareWatcher = share.UseShareWatcher
      };
    }

    /// <summary>
    /// The answer for a library that cannot answer yet, in the same shape as <see cref="LibraryController"/>'s.
    /// </summary>
    private ObjectResult NotReady(IllegalCallException exception)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>(false);
      if (logger != null)
        logger.Info("SharesController: the media library refused a request, answering 503: {0}", exception.Message);
      return new ObjectResult(new ErrorResponse { Error = exception.Message })
      {
        StatusCode = StatusCodes.Status503ServiceUnavailable
      };
    }
  }
}
