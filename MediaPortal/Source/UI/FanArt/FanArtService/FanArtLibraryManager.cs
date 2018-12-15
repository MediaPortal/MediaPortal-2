#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Backend.Services.Database;
using MediaPortal.Backend.Services.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.UserServices.FanArtService.FanArtDataflow;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Extensions.UserServices.FanArtService.Settings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MediaPortal.Extensions.UserServices.FanArtService
{
  /// <summary>
  /// Class for managing the collection and deletion of fanart.
  /// </summary>
  public class FanArtLibraryManager : IFanArtLibraryManager
  {
    #region Protected fields

    protected readonly object _syncObj = new object();
    protected bool _isInit = true;
    protected bool _hasSkippedCleanupBeenLogged = false;
    protected AsynchronousMessageQueue _messageQueue;
    protected SettingsChangeWatcher<FanArtServiceSettings> _settings;
    protected IIntervalWork _fanArtCleanupIntervalWork;
    protected CancellationTokenSource _cleanupTokenSource = new CancellationTokenSource();

    protected FanArtActionBlock _fanartActionBlock; //Handles individual fanart collection/deletion
    protected ActionBlock<bool> _fanartCleanupBlock; //Handles full fanart cleanup

    #endregion

    #region Constructor/Dispose

    public FanArtLibraryManager()
    {
      InitBlocks();
      InitSettings();
      UpdateFanArtCleanupIntervalWork();
      SubscribeToMessages();
    }

    /// <summary>
    /// Waits for all scheduled tasks to complete and disposes.
    /// </summary>
    public void Dispose()
    {
      UnsubscribeFromMessages();
      if (_settings != null)
      {
        _settings.Dispose();
        _settings = null;
      }
      CompleteBlocks();
    }

    #endregion

    #region Init

    /// <summary>
    /// Initialize to TPL blocks
    /// </summary>
    protected void InitBlocks()
    {
      _fanartActionBlock = new FanArtActionBlock();
      
      //Bounded capacity of 2 means there is at max 1 cleanup task running and 1 waiting
      _fanartCleanupBlock = new ActionBlock<bool>(_ => CleanupFanArt(),
        new ExecutionDataflowBlockOptions { BoundedCapacity = 2 });
    }

    protected void CompleteBlocks()
    {
      lock (_syncObj)
      {
        _isInit = false;
        //Mark blocks for completion, no new tasks will be scheduled
        _fanartCleanupBlock.Complete();
        //Cancel the FanartActionBlock, this ensures we stop processing immediately, we persist
        //any pending actions below and will restore them on next startup
        _fanartActionBlock.Cancel();
        //Cancel the cleanup if running
        _cleanupTokenSource.Cancel();
        //Wait for all blocks to complete before returning
        Task.WhenAll(_fanartActionBlock.Completion, _fanartCleanupBlock.Completion).Wait();
      }
    }

    protected void RestorePendingActions()
    {
      lock (_syncObj)
        if (_isInit)
          _fanartActionBlock.Restore();
    }

    /// <summary>
    /// Initialize the settings watcher
    /// </summary>
    protected void InitSettings()
    {
      _settings = new SettingsChangeWatcher<FanArtServiceSettings>();
      _settings.SettingsChanged = OnSettingsChanged;
    }

    private void OnSettingsChanged(object sender, EventArgs e)
    {
      UpdateFanArtCleanupIntervalWork();
    }

    #endregion

    #region Messaging

    protected void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new[] { SystemMessaging.CHANNEL, MediaLibraryMessaging.CHANNEL });
      _messageQueue.PreviewMessage += OnPreviewMessage;
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    protected void UnsubscribeFromMessages()
    {
      if (_messageQueue != null)
      {
        _messageQueue.Shutdown();
        _messageQueue = null;
      }
    }

    //This handler is called synchronously, we complete the blocks in here when the system state changes
    //to ShuttingDown to ensure that all pending tasks are completed whilst all services are still available
    private void OnPreviewMessage(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType)message.MessageType;
        if (messageType == SystemMessaging.MessageType.SystemStateChanged)
        {
          SystemState newState = (SystemState)message.MessageData[SystemMessaging.NEW_STATE];
          if (newState == SystemState.ShuttingDown)
            CompleteBlocks();
        }
      }
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType)message.MessageType;
        if (messageType == SystemMessaging.MessageType.SystemStateChanged)
        {
          SystemState newState = (SystemState)message.MessageData[SystemMessaging.NEW_STATE];
          if (newState == SystemState.Running)
            RestorePendingActions();
        }
      }
      else if (message.ChannelName == MediaLibraryMessaging.CHANNEL)
      {
        MediaLibraryMessaging.MessageType messageType = (MediaLibraryMessaging.MessageType)message.MessageType;
        if (messageType == MediaLibraryMessaging.MessageType.MediaItemsAddedOrUpdated)
        {
          IEnumerable<MediaItem> items = message.MessageData[MediaLibraryMessaging.PARAM] as IEnumerable<MediaItem>;
          if (items != null)
            foreach (MediaItem item in items)
              ScheduleFanArtCollection(item.MediaItemId, item.Aspects);
        }
        else if (messageType == MediaLibraryMessaging.MessageType.MediaItemsDeleted)
        {
          ScheduleFanArtCleanup();
        }
      }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Schedules the collection of fanart for the media item with the specified <paramref name="mediaItemId"/> and <paramref name="aspects"/>.
    /// </summary>
    /// <param name="mediaItemId">The media item id of the media item to collect fanart for.</param>
    /// <param name="aspects">The media item aspects of the media item to collect fanart for.</param>
    public void ScheduleFanArtCollection(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      if (aspects == null)
        throw new ArgumentNullException("aspects", "cannot be null");

      ServiceRegistration.Get<ILogger>().Debug("FanArtManagement: Scheduling fanart collection for {0}.", mediaItemId);
      _fanartActionBlock.Post(new FanArtManagerAction(ActionType.Collect, mediaItemId, aspects));
    }

    /// <summary>
    /// Schedules the deletion of fanart for the media item with the specified <paramref name="mediaItemId"/>.
    /// </summary>
    /// <param name="mediaItemId">The media item id of the media item to delete fanart for.</param>
    public void ScheduleFanArtDeletion(Guid mediaItemId)
    {
      ServiceRegistration.Get<ILogger>().Debug("FanArtManagement: Scheduling fanart deletion for {0}.", mediaItemId);
      _fanartActionBlock.Post(new FanArtManagerAction(ActionType.Delete, mediaItemId, null));
    }
    
    /// <summary>
    /// Schedules a cleanup of all fanart where the corresponding media item no longer exists.
    /// </summary>
    public void ScheduleFanArtCleanup()
    {
      if (_fanartCleanupBlock.Post(true))
      {
        _hasSkippedCleanupBeenLogged = false;
        ServiceRegistration.Get<ILogger>().Debug("FanArtManagement: Scheduling fanart cleanup.");
      }
      else if (!_hasSkippedCleanupBeenLogged)
      {
        _hasSkippedCleanupBeenLogged = true;
        ServiceRegistration.Get<ILogger>().Debug("FanArtManagement: Skipping additional fanart cleanup. There is already a cleanup in the works and another one scheduled.");
      }
    }

    #endregion

    #region Protected methods

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
        ICollection<Guid> fanArtIds = ServiceRegistration.Get<IFanArtCache>().GetAllFanArtIds();
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
        {
          DoDeleteFanArt(mediaItemId, handlers);
          if (_cleanupTokenSource.IsCancellationRequested)
            break;
        }

        sw.Stop();
        if(_cleanupTokenSource.IsCancellationRequested)
          ServiceRegistration.Get<ILogger>().Debug("FanArtManagement: Cleaned up cancelled after {0}ms.", sw.ElapsedMilliseconds);
        else
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

    /// <summary>
    /// Gets all media item ids from the database.
    /// </summary>
    /// <returns></returns>
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
    /// Deletes fanart for the given media item using the provided <see cref="IMediaFanArtHandler"/>s.
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="handlers">Collection of <see cref="IMediaFanArtHandler"/>s to use to delete.</param>
    protected void DoDeleteFanArt(Guid mediaItemId, ICollection<IMediaFanArtHandler> handlers)
    {
      foreach (IMediaFanArtHandler handler in handlers)
        handler.DeleteFanArt(mediaItemId);
    }

    /// <summary>
    /// Creates a scheduled task that will run a fanart cleanup periodically.
    /// </summary>
    protected void UpdateFanArtCleanupIntervalWork()
    {
      FanArtServiceSettings settings = _settings.Settings;
      if (_fanArtCleanupIntervalWork != null)
      {
        if (settings.EnableCleanupOrphanedFanArt && settings.CleanupOrphanedFanArtIntervalHours == _fanArtCleanupIntervalWork.WorkInterval.TotalHours)
          return; //Nothing has changed
        ServiceRegistration.Get<IThreadPool>().RemoveIntervalWork(_fanArtCleanupIntervalWork);
      }

      if (settings.EnableCleanupOrphanedFanArt && settings.CleanupOrphanedFanArtIntervalHours > 0)
      {
        _fanArtCleanupIntervalWork = new IntervalWork(ScheduleFanArtCleanup, TimeSpan.FromHours(_settings.Settings.CleanupOrphanedFanArtIntervalHours));
        ServiceRegistration.Get<IThreadPool>().AddIntervalWork(_fanArtCleanupIntervalWork, false);
      }
    }

    #endregion
  }
}
