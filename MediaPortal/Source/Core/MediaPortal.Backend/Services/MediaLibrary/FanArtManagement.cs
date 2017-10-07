#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using MediaPortal.Backend.Database;
using MediaPortal.Backend.MediaLibrary.Settings;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MediaPortal.Backend.Services.MediaLibrary
{
  /// <summary>
  /// Class for managing the collection and deletion of fanart.
  /// </summary>
  public class FanArtManagement : IDisposable
  {
    #region Internal classes

    protected enum ActionType
    {
      Collect,
      Delete
    }

    protected class FanArtManagerAction
    {
      public FanArtManagerAction(ActionType actionType, Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
      {
        Type = actionType;
        MediaItemId = mediaItemId;
        Aspects = aspects;
      }

      public ActionType Type { get; private set; }
      public Guid MediaItemId { get; private set; }
      public IDictionary<Guid, IList<MediaItemAspect>> Aspects { get; private set; }
    }

    #endregion

    protected const int FANART_CHECK_INTERVAL_HOURS = 24;

    protected ActionBlock<FanArtManagerAction> _fanartActionBlock;
    protected ActionBlock<bool> _fanartCleanupBlock;

    public FanArtManagement()
    {
      InitBlocks();
      CreateFanArtCleanupSchedule();
    }

    /// <summary>
    /// Waits for all scheduled tasks to complete and disposes.
    /// </summary>
    public void Dispose()
    {
      //Mark blocks for completion, no new tasks will be scheduled
      _fanartActionBlock.Complete();
      _fanartCleanupBlock.Complete();
      //Wait for all blocks to complete before returning
      Task.WhenAll(_fanartActionBlock.Completion, _fanartCleanupBlock.Completion).Wait();
    }

    /// <summary>
    /// Initialize to TPL blocks
    /// </summary>
    protected void InitBlocks()
    {
      _fanartActionBlock = new ActionBlock<FanArtManagerAction>(a => OnFanArtAction(a),
        new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 5 });
      
      //Bounded capacity of 2 means there is at max 1 cleanup task running and 1 waiting
      _fanartCleanupBlock = new ActionBlock<bool>(_ => CleanupFanArt(),
        new ExecutionDataflowBlockOptions { BoundedCapacity = 2 });
    }

    /// <summary>
    /// Creates a scheduled task that will run a fanart cleanup every <see cref="FANART_CHECK_INTERVAL_HOURS"/> hours.
    /// </summary>
    protected void CreateFanArtCleanupSchedule()
    {
      IntervalWork scheduledFanartCleanupWork = new IntervalWork(() =>
      {
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
        MediaLibrarySettings settings = settingsManager.Load<MediaLibrarySettings>();
        if (settings.DeleteOrphanedFanart)
          ScheduleFanArtCleanup();
      }, TimeSpan.FromHours(FANART_CHECK_INTERVAL_HOURS));

      ServiceRegistration.Get<IThreadPool>().AddIntervalWork(scheduledFanartCleanupWork, false);
    }

    /// <summary>
    /// Schedules fanart collection/download for the given media item.
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="aspects">The aspects of the media item.</param>
    public void ScheduleFanArtCollection(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      if (aspects == null)
        throw new ArgumentNullException("aspects", "cannot be null");

      ServiceRegistration.Get<ILogger>().Debug("FanArtManagement: Scheduling fanart collection for {0}.", mediaItemId);
      _fanartActionBlock.Post(new FanArtManagerAction(ActionType.Collect, mediaItemId, aspects));
    }

    /// <summary>
    /// Schedules fanart deletion for the given media item.
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    public void ScheduleFanArtDeletion(Guid mediaItemId)
    {
      ServiceRegistration.Get<ILogger>().Debug("FanArtManagement: Scheduling fanart deletion for {0}.", mediaItemId);
      _fanartActionBlock.Post(new FanArtManagerAction(ActionType.Delete, mediaItemId, null));
    }

    /// <summary>
    /// Schedules a cleanup of all orphaned fanart where the media item no longer exists.
    /// </summary>
    public void ScheduleFanArtCleanup()
    {
      ServiceRegistration.Get<ILogger>().Debug("FanArtManagement: Scheduling fanart cleanup.");
      if (!_fanartCleanupBlock.Post(true))
        ServiceRegistration.Get<ILogger>().Info("FanArtManagement: Skipping additional fanart cleanup. There is already a cleanup in the works and another one scheduled.");
    }

    protected void OnFanArtAction(FanArtManagerAction action)
    {
      if (action.Type == ActionType.Collect)
        CollectFanArt(action.MediaItemId, action.Aspects);
      else if (action.Type == ActionType.Delete)
        DeleteFanArt(action.MediaItemId);
    }

    /// <summary>
    /// Collects all fanart for the given media item using the registered <see cref="IMediaFanArtHandler"/>s
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="aspects">The aspects of the media item.</param>
    protected void CollectFanArt(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      try
      {
        IEnumerable<IMediaFanArtHandler> handlers = GetFanArtHandlers().Where(h => h.FanArtAspects.Any(a => aspects.ContainsKey(a)));
        foreach (IMediaFanArtHandler handler in handlers)
          handler.CollectFanArt(mediaItemId, aspects);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("FanArtManagement: Error collecting fanart for media item {0}.", ex, mediaItemId);
      }
    }

    /// <summary>
    /// Deletes all fanart for the given media item.
    /// </summary>
    /// <param name="mediaItemId"></param>
    protected void DeleteFanArt(Guid mediaItemId)
    {
      try
      {
        DoDeleteFanArt(mediaItemId, GetFanArtHandlers());
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("FanArtManagement: Error deleting fanart for media item {0}.", ex, mediaItemId);
      }
    }

    /// <summary>
    /// Performs a complete cleanup of all orphaned fanart.
    /// </summary>
    protected void CleanupFanArt()
    {
      try
      {
        var sw = Stopwatch.StartNew();
        //Order is important here, get all fanart ids first to ensure we don't delete the fanart
        //of a media item that was added after the call to GetAllMediaItemIds
        ICollection<Guid> fanArtIds = FanArtCache.GetAllFanArtIds();
        ICollection<Guid> mediaItemIds = GetAllMediaItemIds();
        ICollection<Guid> fanArtToDelete = fanArtIds.Except(mediaItemIds).ToList();
        if (fanArtToDelete.Count == 0)
        {
          sw.Stop();
          ServiceRegistration.Get<ILogger>().Debug("FanArtManagement: No orphaned fanart found.");
          return;
        }

        ICollection<IMediaFanArtHandler> handlers = GetFanArtHandlers();
        foreach (Guid mediaItemId in fanArtToDelete)
          DoDeleteFanArt(mediaItemId, handlers);

        sw.Stop();
        ServiceRegistration.Get<ILogger>().Debug("FanArtManagement: Cleaned up orphaned fanart for {0} non existant media items in {1}ms.", fanArtToDelete.Count, sw.ElapsedMilliseconds);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("FanArtManagement: Error cleaning up fanart.", ex);
      }
    }

    /// <summary>
    /// Gets all registered <see cref="IMediaFanArtHandler"/>s.
    /// </summary>
    /// <returns></returns>
    protected ICollection<IMediaFanArtHandler> GetFanArtHandlers()
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      return mediaAccessor.LocalFanArtHandlers.Values;
    }

    protected ICollection<Guid> GetAllMediaItemIds()
    {
      HashSet<Guid> mediaItemIds = new HashSet<Guid>();

      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      using (ITransaction transaction = database.BeginTransaction())
      using (IDbCommand command = MediaLibrary_SubSchema.SelectAllMediaItemIdsCommand(transaction))
      using (var reader = command.ExecuteReader())
        while (reader.Read())
          mediaItemIds.Add(database.ReadDBValue<Guid>(reader, 0));

      return mediaItemIds;
    }

    /// <summary>
    /// Deletes fanart for the given media item using the provided <see cref="IMediaFanArtHandler"/>s
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="handlers">Collection of <see cref="IMediaFanArtHandler"/>s to use to delete.</param>
    protected void DoDeleteFanArt(Guid mediaItemId, ICollection<IMediaFanArtHandler> handlers)
    {
      foreach (IMediaFanArtHandler handler in handlers)
        handler.DeleteFanArt(mediaItemId);
    }
  }
}