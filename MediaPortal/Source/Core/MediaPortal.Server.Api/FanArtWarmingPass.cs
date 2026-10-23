#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// How the server got to know where the pictures of its library lie.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>What this is for.</b> The first look at a wall that was imported a minute ago is the slow one: every tile
  /// asks for its poster, its banner and its backdrop, and each of those questions is answered by looking the
  /// title up in the database and listing the folders next to its file - see <see cref="LocalFanArtSearch"/>.
  /// After an import there is nothing to watch and nothing to do, so this pass asks all those questions once,
  /// quietly, while nobody is waiting: the answers go into the same memory the requests read from, and the wall
  /// that used to stall for a second per tile is drawn from file opens.
  /// </para>
  /// <para>
  /// <b>Why it waits for the importer.</b> A pass that runs while files are being read competes with the very
  /// work that is making the titles worth looking at, and a folder that is half imported has no pictures in it
  /// yet - the answers gathered then would be wrong and would have to be forgotten again. So a pass scheduled by
  /// an import first waits for the importer's queue to empty, and a pass that is running gives the disc back the
  /// moment the importer has work again. Both are reported through <see cref="Read"/>, because a viewer who sees
  /// "collecting" wants to know whether the machine is busy or idle.
  /// </para>
  /// <para>
  /// <b>What it costs.</b> One database lookup and one folder listing per title, and a title the memory already
  /// knows all seven kinds of is not looked at again - so the pass after a handful of new films costs a handful
  /// of titles, not the library. A library of a few thousand titles is swept in a minute of background time.
  /// </para>
  /// </remarks>
  public sealed class FanArtWarmingPass
  {
    /// <summary>The one pass of this server's run. Two passes at a time would only compete with each other.</summary>
    public static readonly FanArtWarmingPass Worn = new FanArtWarmingPass();

    /// <summary>How many titles are read from the library at a time.</summary>
    private const int PAGE = 200;

    /// <summary>How long the pass rests between pages, so that a viewer who started watching is not raced.</summary>
    private static readonly TimeSpan BETWEEN_PAGES = TimeSpan.FromMilliseconds(20);

    /// <summary>
    /// How often a pass waiting for the importer looks at its queue.
    /// </summary>
    public static TimeSpan QueueLook { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long a pass is willing to wait for an import to finish. Past that it gives up; the import that is taking
    /// that long will schedule a pass of its own when it is done, and a pass from a stale schedule is not worth the
    /// folder listings it would make. Read again as every wait goes by, so that shortening it takes effect on a pass
    /// that is already waiting.
    /// </summary>
    public static TimeSpan PatienceWithTheImporter { get; set; } = TimeSpan.FromHours(3);

    /// <summary>How many titles are looked over before the pass asks whether the importer wants the disc back.</summary>
    private const int TITLES_BEFORE_LOOKING_UP = 25;

    private readonly object _gate = new object();

    private bool _running;
    private int _titles;
    private int _skipped;
    private int _kindsFound;
    private string _waiting;
    private DateTimeOffset? _started;
    private DateTimeOffset? _ended;
    private string _stopped;

    private FanArtWarmingPass()
    {
    }

    /// <summary>
    /// Starting a pass over the whole library. False when one is already running, which is not a failure - the
    /// work is queued either way, whoever asked last.
    /// </summary>
    internal bool Start()
    {
      return Start(null);
    }

    /// <summary>
    /// Starting a pass that first waits for the importer's queue to empty. Called by the endpoints that queue an
    /// import, so that an import of a new folder is followed by the pass that learns its pictures without anybody
    /// having to ask for it.
    /// </summary>
    internal bool StartAfterImport()
    {
      return Start(ServiceRegistration.Get<IImporterWorker>(false));
    }

    /// <summary>
    /// How the pass is doing, for the endpoint that reports it and the screen that shows it.
    /// </summary>
    internal FanArtWarmingResult Read()
    {
      lock (_gate)
      {
        return new FanArtWarmingResult
        {
          Running = _running,
          Titles = _titles,
          Skipped = _skipped,
          KindsFound = _kindsFound,
          Answers = LocalFanArtSearch.Worn.RememberedCount,
          Waiting = _waiting,
          Stopped = _stopped,
          Started = _started,
          Ended = _ended
        };
      }
    }

    private bool Start(IImporterWorker importer)
    {
      lock (_gate)
      {
        if (_running)
          return false;

        _running = true;
        _titles = 0;
        _skipped = 0;
        _kindsFound = 0;
        _stopped = null;
        _waiting = importer == null ? null : "waiting for the importer to finish";
        _started = DateTimeOffset.UtcNow;
        _ended = null;
      }

      Task.Run(delegate { Run(importer); });
      return true;
    }
    private void Run(IImporterWorker importer)
    {
      try
      {
        if (importer != null && !WaitForImport(importer))
          return;

        Sweep();
      }
      catch (Exception ex)
      {
        // A library that throws halfway through a pass is a library whose pictures are learned by the next
        // question about them instead. A background pass has no caller to report to, so what it can do is say
        // what stopped it and let the answers it did gather stand.
        Stopped("stopped: " + ex.Message);
      }
      finally
      {
        lock (_gate)
        {
          _running = false;
          _waiting = null;
          _ended = DateTimeOffset.UtcNow;
        }

        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
        {
          FanArtWarmingResult said = Read();
          logger.Info("FanArtWarmingPass: looked at {0} titles, learned {1} pictures, {2} already known, "
              + "{3} answers kept. {4}",
              said.Titles, said.KindsFound, said.Skipped, said.Answers,
              said.Stopped ?? "the whole library was walked");
        }
      }
    }

    /// <summary>
    /// Standing back until the importer's queue is empty. False when the queue did not empty inside
    /// <see cref="PATIENCE_WITH_THE_IMPORTER"/>, which the pass reports as giving up rather than pretending to
    /// have walked the library.
    /// </summary>
    private bool WaitForImport(IImporterWorker importer)
    {
      DateTimeOffset since = DateTimeOffset.UtcNow;

      while (DateTimeOffset.UtcNow - since < PatienceWithTheImporter)
      {
        int pending;
        try
        {
          ICollection<ImportJobInformation> jobs = importer.ImportJobs;
          pending = jobs == null ? 0 : jobs.Count;
        }
        catch (Exception)
        {
          // A worker that cannot be asked is not worth waiting for; the pass runs and the next question about a
          // picture still searches, which is what would have happened without this pass at all.
          return true;
        }

        if (pending == 0)
        {
          lock (_gate)
          {
            _waiting = null;
          }
          return true;
        }

        lock (_gate)
        {
          _waiting = "the importer still has " + pending + " job" + (pending == 1 ? "" : "s");
        }

        Thread.Sleep(QueueLook);
      }

      Stopped("gave up waiting for the importer");
      return false;
    }

    /// <summary>
    /// Asking every title the library knows where its pictures are, page by page, and giving up the moment the
    /// importer wants the disc back.
    /// </summary>
    private void Sweep()
    {
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>(false);
      if (library == null)
      {
        Stopped("this server has no media library to walk");
        return;
      }

      IImporterWorker importer = ServiceRegistration.Get<IImporterWorker>(false);
      IList<IMediaFanArtHandler> handlers = PictureHandlers();
      HashSet<Guid> seen = new HashSet<Guid>();
      uint offset = 0;

      while (true)
      {
        IList<MediaItem> page = AskPage(library, offset, handlers);
        List<MediaItem> fresh = page.Where(title => seen.Add(title.MediaItemId)).ToList();

        // A whole page of titles this pass already looked at means the order tied over itself, which is as good
        // an end as any: the memory holds the answers either way.
        if (fresh.Count == 0)
          return;

        foreach (MediaItem title in fresh)
        {
          if (importer != null && HasJobs(importer))
          {
            Stopped("the importer wanted the disc back");
            return;
          }

          // The handlers come first: a picture they can take out of a title's own files is a picture the
          // folders alone would never have named.
          CollectPictures(handlers, title);

          if (LocalFanArtSearch.Worn.Learned(title.MediaItemId))
          {
            Count(0, true);
            continue;
          }

          Count(LocalFanArtSearch.Worn.Warm(title.MediaItemId), false);
        }

        offset += (uint)page.Count;
        if (page.Count < PAGE)
          return;

        Thread.Sleep(BETWEEN_PAGES);
      }
    }

    /// <summary>
    /// Asking the plugins' picture handlers about one title, so that whatever they can get at is in the cache
    /// before anybody asks for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the reference's own step, read from the client's <c>FanArtActionBlock</c>: a title is handed to
    /// every handler that declared one of its aspects, and the handler decides what a picture of that title is
    /// and where it comes from. On the reference a window had to open a title's pictures for that to happen;
    /// here there is no window, so the pass that already walks the library does the asking - which is also the
    /// only moment a headless server has, being a server nobody is looking at.
    /// </para>
    /// <para>
    /// A handler is asked once per title per pass and answers nothing when it has already stored the picture,
    /// so the cost of asking again is one look at a folder. What a handler throws is its own affair: a title
    /// whose handler failed is a title whose pictures are answered by the folders instead, and the pass has
    /// another thousand titles to get through.
    /// </para>
    /// </remarks>
    private static void CollectPictures(IEnumerable<IMediaFanArtHandler> handlers, MediaItem title)
    {
      foreach (IMediaFanArtHandler handler in handlers.Where(handler =>
          handler.FanArtAspects.Any(aspect => title.Aspects.ContainsKey(aspect))))
      {
        try
        {
          handler.CollectFanArtAsync(title.MediaItemId, title.Aspects).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
          ILogger logger = ServiceRegistration.Get<ILogger>(false);
          if (logger != null)
            logger.Warn("FanArtWarmingPass: the picture handler '{0}' could not finish for the media item {1}",
                ex, handler.Metadata.Name, title.MediaItemId);
        }
      }
    }

    /// <summary>
    /// The picture handlers the plugins registered, asked for once per pass.
    /// </summary>
    /// <remarks>
    /// The handlers are the media accessor's, which is where MP2 keeps them and where the client's fanart
    /// service takes them from. A server that was started without a media accessor, or whose plugins failed to
    /// load, has no handlers to ask and warms the folders alone - which is what it did before there were any.
    /// </remarks>
    private static IList<IMediaFanArtHandler> PictureHandlers()
    {
      try
      {
        IMediaAccessor accessor = ServiceRegistration.Get<IMediaAccessor>(false);
        return accessor == null
            ? new List<IMediaFanArtHandler>()
            : new List<IMediaFanArtHandler>(accessor.LocalFanArtHandlers.Values);
      }
      catch (Exception ex)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Warn("FanArtWarmingPass: no picture handlers to ask, because the media accessor would not say: {0}",
              ex.Message);
        return new List<IMediaFanArtHandler>();
      }
    }

    /// <summary>
    /// One page of titles: every item the library holds that is a title with a file or folder of its own, in the
    /// database's own order of the name it sorts by, which is stable enough for walking a library in pages.
    /// </summary>
    /// <remarks>
    /// The aspects asked for beside the file are the ones the handlers declared, plus the relationship aspect:
    /// a handler is asked about a title in the aspects it serves, and the album a track's cover belongs to is
    /// named by a relationship rather than by an attribute of the track itself.
    /// </remarks>
    private static IList<MediaItem> AskPage(IMediaLibrary library, uint offset,
        IEnumerable<IMediaFanArtHandler> handlers)
    {
      List<Guid> supplementary = new List<Guid> { ProviderResourceAspect.ASPECT_ID, RelationshipAspect.ASPECT_ID };
      foreach (Guid served in handlers.SelectMany(handler => handler.FanArtAspects).Distinct())
        if (!supplementary.Contains(served))
          supplementary.Add(served);

      MediaItemQuery query = new MediaItemQuery(
          new List<Guid> { MediaAspect.ASPECT_ID },
          supplementary,
          null);
      query.SetSortInformation(new List<ISortInformation>
      {
          new AttributeSortInformation(MediaAspect.ATTR_SORT_TITLE, SortDirection.Ascending)
      });
      query.Offset = offset;
      query.Limit = PAGE;

      IList<MediaItem> items = library.Search(query, false, null, false);
      return items ?? new List<MediaItem>();
    }

    private static bool HasJobs(IImporterWorker importer)
    {
      try
      {
        ICollection<ImportJobInformation> jobs = importer.ImportJobs;
        return jobs != null && jobs.Count > 0;
      }
      catch (Exception)
      {
        return false;
      }
    }

    private void Count(int kindsFound, bool alreadyKnown)
    {
      lock (_gate)
      {
        _titles++;
        _kindsFound += kindsFound;
        if (alreadyKnown)
          _skipped++;
      }
    }

    private void Stopped(string why)
    {
      lock (_gate)
      {
        _stopped = why;
      }
    }
  }

  /// <summary>
  /// How the server is doing at learning where the pictures of its library lie, as its endpoint reports it.
  /// </summary>
  public class FanArtWarmingResult
  {
    /// <summary>Whether a pass is running or waiting right now.</summary>
    public bool Running { get; set; }

    /// <summary>How many titles the running or last pass looked at.</summary>
    public int Titles { get; set; }

    /// <summary>How many of those the server already knew all seven kinds of, and was not asked about again.</summary>
    public int Skipped { get; set; }

    /// <summary>How many (title, kind) pairs the pass actually found a picture for.</summary>
    public int KindsFound { get; set; }

    /// <summary>How many answers the server is holding, of the current pass and of every request before it.</summary>
    public int Answers { get; set; }

    /// <summary>What a running pass is doing between titles, or null while it is walking.</summary>
    public string Waiting { get; set; }

    /// <summary>Why a pass ended before walking the whole library, or null when it walked it.</summary>
    public string Stopped { get; set; }

    /// <summary>When the running or last pass started.</summary>
    public DateTimeOffset? Started { get; set; }

    /// <summary>When the last pass ended; null while one is running.</summary>
    public DateTimeOffset? Ended { get; set; }
  }
}