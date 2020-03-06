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
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Threading;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.TranscodingService.AnalysisDataflow;
using MediaPortal.Extensions.TranscodingService.Interfaces.Settings;
using MediaPortal.Extensions.TranscodingService.Interfaces;

namespace MediaPortal.Extensions.TranscodingService
{
  /// <summary>
  /// Class for managing the analysis and deletion of media items.
  /// </summary>
  public class AnalysisLibraryManager
  {
    #region Protected fields

    protected readonly object _syncObj = new object();
    protected bool _isInit = true;
    protected bool _hasSkippedCleanupBeenLogged = false;
    protected AsynchronousMessageQueue _messageQueue;
    protected IIntervalWork _analysisCleanupIntervalWork;
    protected CancellationTokenSource _cleanupTokenSource = new CancellationTokenSource();

    protected AnalysisActionBlock _analysisActionBlock; //Handles individual analysis creation/deletion
    protected ActionBlock<bool> _analysisCleanupBlock; //Handles full analysis cleanup

    private bool _enableAnalysisOfImportedMedia = false;

    #endregion

    #region Constructor/Dispose

    public AnalysisLibraryManager()
    {
      InitBlocks();
      SubscribeToMessages();
    }

    /// <summary>
    /// Waits for all scheduled tasks to complete and disposes.
    /// </summary>
    public void Dispose()
    {
      UnsubscribeFromMessages();
      CompleteBlocks();
    }

    #endregion

    #region Init

    /// <summary>
    /// Initialize to TPL blocks
    /// </summary>
    protected void InitBlocks()
    {
      _analysisActionBlock = new AnalysisActionBlock();

      //Bounded capacity of 2 means there is at max 1 cleanup task running and 1 waiting
      _analysisCleanupBlock = new ActionBlock<bool>(_ => CleanupAnalysis(),
        new ExecutionDataflowBlockOptions { BoundedCapacity = 2 });
    }

    protected void CompleteBlocks()
    {
      lock (_syncObj)
      {
        _isInit = false;
        //Mark blocks for completion, no new tasks will be scheduled
        _analysisCleanupBlock.Complete();
        //Cancel the AnalysisActionBlock, this ensures we stop processing immediately, we persist
        //any pending actions below and will restore them on next startup
        _analysisActionBlock.Cancel();
        //Cancel the cleanup if running
        _cleanupTokenSource.Cancel();
        //Wait for all blocks to complete before returning
        Task.WhenAll(_analysisActionBlock.Completion, _analysisCleanupBlock.Completion).Wait();
      }
    }

    protected void RestorePendingActions()
    {
      lock (_syncObj)
        if (_isInit)
          _analysisActionBlock.Restore();
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
          IEnumerable<Guid> ids = message.MessageData[MediaLibraryMessaging.PARAM] as IEnumerable<Guid>;
          if (ids != null)
            foreach (Guid id in ids)
              ScheduleAnalysis(id);
        }
        else if (messageType == MediaLibraryMessaging.MessageType.MediaItemsDeleted)
        {
          ScheduleAnalysisCleanup();
        }
      }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Schedules the analysis of the media item with the specified <paramref name="mediaItemId"/>.
    /// </summary>
    /// <param name="mediaItemId">The media item id of the media item to analyze.</param>
    public void ScheduleAnalysis(Guid mediaItemId)
    {
      if (!_enableAnalysisOfImportedMedia)
        return;

      if (mediaItemId != Guid.Empty)
      {
        ServiceRegistration.Get<ILogger>().Debug("AnalysisLibraryManager: Scheduling analysis of {0}.", mediaItemId);
        if (!_analysisActionBlock.Post(new AnalysisManagerAction(ActionType.Analyze, mediaItemId)))
          ServiceRegistration.Get<ILogger>().Warn("AnalysisLibraryManager: Failed to scheduling analysis of {0}.", mediaItemId);
      }
    }

    /// <summary>
    /// Schedules the deletion of analysis for the media item with the specified <paramref name="mediaItemId"/>.
    /// </summary>
    /// <param name="mediaItemId">The media item id of the media item to delete the analysis for.</param>
    public void ScheduleAnalysisDeletion(Guid mediaItemId)
    {
      if (mediaItemId != Guid.Empty)
      {
        ServiceRegistration.Get<ILogger>().Debug("AnalysisLibraryManager: Scheduling analysis deletion for {0}.", mediaItemId);
        if (!_analysisActionBlock.Post(new AnalysisManagerAction(ActionType.Delete, mediaItemId)))
          ServiceRegistration.Get<ILogger>().Warn("AnalysisLibraryManager: Failed to schedule delete analysis for {0}.", mediaItemId);
      }
    }
    
    /// <summary>
    /// Schedules a cleanup of all analysis where the corresponding media item no longer exists.
    /// </summary>
    public void ScheduleAnalysisCleanup()
    {
      if (_analysisCleanupBlock.Post(true))
      {
        _hasSkippedCleanupBeenLogged = false;
        ServiceRegistration.Get<ILogger>().Debug("AnalysisLibraryManager: Scheduling analysis cleanup.");
      }
      else if (!_hasSkippedCleanupBeenLogged)
      {
        _hasSkippedCleanupBeenLogged = true;
        ServiceRegistration.Get<ILogger>().Debug("AnalysisLibraryManager: Skipping additional analysis cleanup. There is already a cleanup in the works and another one scheduled.");
      }
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Performs a complete cleanup of all orphaned analysis.
    /// </summary>
    protected void CleanupAnalysis()
    {
      try
      {
        var sw = Stopwatch.StartNew();
        IMediaAnalyzer analyzer = ServiceRegistration.Get<IMediaAnalyzer>();
        //Order is important here, get all analysis ids first to ensure we don't delete the analysis
        //of a media item that was added after the call to GetAllMediaItemIds
        ICollection<Guid> analysisIds = analyzer.GetAllAnalysisIds();
        ICollection<Guid> mediaItemIds = GetAllMediaItemIds();
        ICollection<Guid> analysisToDelete = analysisIds.Except(mediaItemIds).ToList();
        if (analysisToDelete.Count == 0)
        {
          sw.Stop();
          ServiceRegistration.Get<ILogger>().Debug("AnalysisLibraryManager: No orphaned analysis found.");
          return;
        }

        foreach (Guid mediaItemId in analysisToDelete)
        {
          analyzer.DeleteAnalysisAsync(mediaItemId).Wait();
          if (_cleanupTokenSource.IsCancellationRequested)
            break;
        }

        sw.Stop();
        if(_cleanupTokenSource.IsCancellationRequested)
          ServiceRegistration.Get<ILogger>().Debug("AnalysisLibraryManager: Cleaned up canceled after {0}ms.", sw.ElapsedMilliseconds);
        else
          ServiceRegistration.Get<ILogger>().Debug("AnalysisLibraryManager: Cleaned up orphaned analysis for {0} non existent media items in {1}ms.", analysisToDelete.Count, sw.ElapsedMilliseconds);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("AnalysisLibraryManager: Error cleaning up analysis.", ex);
      }
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
    /// Creates a scheduled task that will run a analysis cleanup periodically.
    /// </summary>
    public void UpdateAnalysisCleanupIntervalWork(TranscodingServiceSettings settings)
    {
      _enableAnalysisOfImportedMedia = settings.EnableAnalysisOfImportedMedia;
      if (_analysisCleanupIntervalWork != null)
      {
        if (settings.EnableCleanupOrphanedAnalysis && settings.CleanupOrphanedAnalysisIntervalHours == _analysisCleanupIntervalWork.WorkInterval.TotalHours)
          return; //Nothing has changed
        ServiceRegistration.Get<IThreadPool>().RemoveIntervalWork(_analysisCleanupIntervalWork);
      }

      if (settings.EnableCleanupOrphanedAnalysis && settings.CleanupOrphanedAnalysisIntervalHours > 0)
      {
        _analysisCleanupIntervalWork = new IntervalWork(ScheduleAnalysisCleanup, TimeSpan.FromHours(settings.CleanupOrphanedAnalysisIntervalHours));
        ServiceRegistration.Get<IThreadPool>().AddIntervalWork(_analysisCleanupIntervalWork, false);
      }
    }

    #endregion
  }
}
