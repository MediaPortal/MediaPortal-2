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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS
{
  static class StreamControl
  {
    private static readonly ConcurrentDictionary<string, StreamItem> STREAM_ITEMS = new ConcurrentDictionary<string, StreamItem>();

    private const int STREAM_CLEANUP_TICK_INTERVAL = 30000; // Millisecond interval between checks for inactive streams
    private const int STREAM_BUSY_WAIT_TIMEOUT = 2000; // Milliseconds to wait to obtain a lock on a stream
    private static readonly object _streamCleanupSyncObj = new object();
    static Timer _streamCleanupTimer;

    /// <summary>
    /// Starts a task that intermittently checks for inactive
    /// streams and stops and removes them if necessary.
    /// </summary>
    internal static void StartStreamCleanupTask()
    {
      lock (_streamCleanupSyncObj)
      {
        if (_streamCleanupTimer != null)
        {
          Logger.Warn("StreamControl: Unable to start stream cleanup timer, it's already started.");
          return;
        }

        _streamCleanupTimer = new Timer(CleanupTimerTick);
        _streamCleanupTimer.Change(STREAM_CLEANUP_TICK_INTERVAL, Timeout.Infinite);
      }
    }

    /// <summary>
    /// Stops checking for inactive streams and optionally
    /// stops and removes all streams.
    /// </summary>
    /// <param name="deleteAllStreams">Whether to stop and delete all streams.</param>
    internal static void StopStreamCleanupTask(bool deleteAllStreams)
    {
      lock (_streamCleanupSyncObj)
      {
        if (_streamCleanupTimer != null)
        {
          _streamCleanupTimer.Dispose();
          _streamCleanupTimer = null;
        }
      }

      if (deleteAllStreams)
        DeleteAllStreamItemsAsync().Wait();
    }

    private static void CleanupTimerTick(object state)
    {
      try
      {
        List<KeyValuePair<string, StreamItem>> streamItems = new List<KeyValuePair<string, StreamItem>>(STREAM_ITEMS);
        foreach (var streamItem in streamItems)
        {
          bool shouldRemove = false;
          // If stream is currently busy it's active, so just skip it
          if (!streamItem.Value.BusyLock.Wait(STREAM_BUSY_WAIT_TIMEOUT))
            continue;
          try
          {
            shouldRemove = (DateTime.Now - streamItem.Value.LastActivityTime).TotalSeconds > streamItem.Value.IdleTimeout;
          }
          finally
          {
            streamItem.Value.BusyLock.Release();
          }

          if (shouldRemove)
          {
            DeleteStreamItemAsync(streamItem.Key).Wait();
            Logger.Info("StreamControl: Removed stream with identifier {0} due to inactivity.", streamItem.Key);
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Error("StreamControl: Error when trying to cleanup inactive streams.", ex);
      }

      lock (_streamCleanupSyncObj)
      {
        if (_streamCleanupTimer != null)
          _streamCleanupTimer.Change(STREAM_CLEANUP_TICK_INTERVAL, Timeout.Infinite);
      }
    }

    /// <summary>
    /// Adds a new stream Item to the list.
    /// </summary>
    /// <param name="identifier">The unique string which identifies the stream Item</param>
    /// <param name="item">The stream item which should be added</param>
    internal static async Task<bool> AddStreamItemAsync(string identifier, StreamItem item)
    {
      if (await DeleteStreamItemAsync(identifier))
        Logger.Debug("StreamControl: Identifier {0} is already in list -> deleting old stream item", identifier);

      item.LastActivityTime = DateTime.Now;
      return STREAM_ITEMS.TryAdd(identifier, item);
    }

    /// <summary>
    /// Deletes a stream Item from the list
    /// </summary>
    /// <param name="identifier">The unique string which identifies the stream Item</param>
    /// <returns>Returns true if an item was deleted, false if no item was deleted</returns>
    internal static async Task<bool> DeleteStreamItemAsync(string identifier)
    {
      if (ValidateIdentifier(identifier))
      {
        await StopStreamingAsync(identifier);
        STREAM_ITEMS.TryRemove(identifier, out _);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Deletes all stream items from the list.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when all streams items have been deleted.</returns>
    internal static async Task DeleteAllStreamItemsAsync()
    {
      List<string> streamIdentifiers = new List<string>(STREAM_ITEMS.Keys);
      foreach (string identifier in streamIdentifiers)
        await DeleteStreamItemAsync(identifier);
    }

    /// <summary>
    /// Updates an already existent stream item
    /// </summary>
    /// <param name="identifier">The unique string which identifies the stream Item</param>
    /// <param name="item">The updated stream item</param>
    internal static async Task UpdateStreamItemAsync(string identifier, StreamItem item)
    {
      await DeleteStreamItemAsync(identifier);
      await AddStreamItemAsync(identifier, item);
    }

    /// <summary>
    /// Returns a stream item based on the given identifier
    /// </summary>
    /// <param name="identifier">The unique string which identifies the stream Item</param>
    /// <returns>Returns the requested stream item otherwise null</returns>
    internal static Task<StreamItem> GetStreamItemAsync(string identifier)
    {
      if (STREAM_ITEMS.TryGetValue(identifier, out var stream))
      {
        stream.LastActivityTime = DateTime.Now;
        return Task.FromResult(stream);
      }
      return Task.FromResult<StreamItem>(null);
    }

    /// <summary>
    /// Gets all available stream items
    /// </summary>
    /// <returns>Returns a Dictionary of stream Items</returns>
    internal static IReadOnlyDictionary<string, StreamItem> GetStreamItems()
    {
      return STREAM_ITEMS;
    }

    internal static bool ValidateIdentifier(string identifier)
    {
      return STREAM_ITEMS.ContainsKey(identifier);
    }

    /// <summary>
    /// Does the preparation to start a stream
    /// </summary>
    /// <param name="identifier">The unique string which identifies the stream Item</param>
    /// <param name="context">Transcoder context</param>
    internal static async Task<TranscodeContext> StartStreamingAsync(string identifier, double startTime)
    {
      if (!STREAM_ITEMS.TryGetValue(identifier, out StreamItem currentStreamItem))
        return null;

      using (await currentStreamItem.RequestBusyLockAsync())
      {
        if (currentStreamItem.TranscoderObject == null)
        {
          STREAM_ITEMS.TryRemove(identifier, out _);
          return null;
        }

        if (currentStreamItem.IsActive)
        {
          currentStreamItem.TranscoderObject.StopStreaming();
          if (currentStreamItem.StreamContext != null)
          {
            if (currentStreamItem.StreamContext is TranscodeContext transcodeContext)
              transcodeContext.UpdateStreamUse(false);
            currentStreamItem.StreamContext.Dispose();
            currentStreamItem.StreamContext = null;
          }
        }

        if (!currentStreamItem.TranscoderObject.StartTrancoding())
        {
          Logger.Debug("StreamControl: Transcoding busy for mediaitem {0}", currentStreamItem.RequestedMediaItem.MediaItemId);
          return null;
        }

        currentStreamItem.TranscoderObject.StartStreaming();
        if (currentStreamItem.IsLive)
          currentStreamItem.StreamContext = await MediaConverter.GetLiveStreamAsync(identifier, currentStreamItem.TranscoderObject.TranscodingParameter, currentStreamItem.LiveChannelId, true);
        else
          currentStreamItem.StreamContext = await MediaConverter.GetMediaStreamAsync(identifier, currentStreamItem.TranscoderObject.TranscodingParameter, startTime, 0, true);

        if (currentStreamItem.StreamContext is TranscodeContext context)
        {
          context.UpdateStreamUse(true);
          currentStreamItem.TranscoderObject.SegmentDir = context.SegmentDir;
          return context;
        }
        else if (currentStreamItem.StreamContext != null)
        {
          //We want a transcoded stream
          currentStreamItem.StreamContext.Dispose();
          currentStreamItem.StreamContext = null;
        }

        return null;
      }
    }

    /// <summary>
    /// Does the preparation to start a stream
    /// </summary>
    internal static async Task<StreamContext> StartOriginalFileStreamingAsync(string identifier)
    {
      if (STREAM_ITEMS.TryGetValue(identifier, out var currentStreamItem))
      {
        using (await currentStreamItem.RequestBusyLockAsync())
        {
          if (currentStreamItem.IsActive)
          {
            currentStreamItem.TranscoderObject?.StopStreaming();
            if (currentStreamItem.StreamContext is TranscodeContext context)
            {
              context.UpdateStreamUse(false);
            }
            else if (currentStreamItem.StreamContext != null)
            {
              currentStreamItem.StreamContext.Dispose();
              currentStreamItem.StreamContext = null;
            }
          }

          IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
          List<IResourceAccessor> resources = new List<IResourceAccessor>();
          foreach (var res in currentStreamItem.TranscoderObject.Metadata.FilePaths)
          {
            var path = ResourcePath.Deserialize(res.Value);
            if (mediaAccessor.LocalResourceProviders.TryGetValue(path.BasePathSegment.ProviderId, out var resourceProvider) &&
                resourceProvider is IBaseResourceProvider baseProvider && baseProvider.TryCreateResourceAccessor(res.Value, out var accessor))
            {
              using (accessor)
              {
                if (accessor is IFileSystemResourceAccessor)
                {
                  currentStreamItem.TranscoderObject.StartStreaming();
                  currentStreamItem.StreamContext = await MediaConverter.GetFileStreamAsync(path);
                  return currentStreamItem.StreamContext;
                }
              }
            }
          }
        }
      }

      return null;
    }

    /// <summary>
    /// Stops the streaming
    /// </summary>
    /// <param name="identifier">The unique string which identifies the stream Item</param>
    internal static async Task<bool> StopStreamingAsync(string identifier)
    {
      if (STREAM_ITEMS.TryGetValue(identifier, out var stream))
      {
        using (await stream.RequestBusyLockAsync())
        {
          stream.TranscoderObject?.StopStreaming();
          if (stream.StreamContext is TranscodeContext context)
          {
            context.UpdateStreamUse(false);
          }
          else if (stream.StreamContext != null)
          {
            stream.StreamContext.Dispose();
            stream.StreamContext = null;
          }
          stream.TranscoderObject?.StopTranscoding();
          return true;
        }
      }
      return false;
    }

    private static IMediaConverter MediaConverter
    {
      get { return ServiceRegistration.Get<IMediaConverter>(); }
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
