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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.MediaServer.DLNA;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.MediaServer.Objects.MediaLibrary;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MediaServer.ResourceAccess
{
  static class StreamControl
  {
    private static readonly ConcurrentDictionary<Guid, StreamItem> STREAM_ITEMS = new ConcurrentDictionary<Guid, StreamItem>();

    /// <summary>
    /// Returns a DLNA media item based on the given client and request
    /// </summary>
    /// <returns>Returns the requested DLNA media item otherwise null</returns>
    internal static StreamItem GetNewStreamItem(EndPointSettings client, Uri uri)
    {
      if (STREAM_ITEMS.TryGetValue(client.ClientId, out var currentStreamItem))
      {
        return currentStreamItem;
      }

      currentStreamItem = new StreamItem(uri.Host);

      Guid mediaItemGuid = Guid.Empty;
      DlnaMediaItem dlnaItem = null;
      int channel = 0;
      if (DlnaResourceAccessUtils.ParseMediaItem(uri, out mediaItemGuid))
      {
        if (mediaItemGuid == Guid.Empty)
          throw new InvalidOperationException(string.Format("Illegal request syntax. Correct syntax is '{0}'", DlnaResourceAccessUtils.SYNTAX));

        if (!client.DlnaMediaItems.TryGetValue(mediaItemGuid, out dlnaItem))
        {
          // Attempt to grab the media item from the database.
          MediaItem item = MediaLibraryHelper.GetMediaItem(mediaItemGuid);
          if (item == null)
            throw new Exception(string.Format("Media item '{0}' not found.", mediaItemGuid));

          dlnaItem = client.GetDlnaItem(item);
        }

        if (dlnaItem == null)
          throw new Exception(string.Format("DLNA media item '{0}' not found.", mediaItemGuid));
      }
      else if (DlnaResourceAccessUtils.ParseTVChannel(uri, out channel))
      {
        dlnaItem = client.GetLiveDlnaItem(channel);

        if (dlnaItem == null)
          throw new Exception(string.Format("DLNA TV channel '{0}' was never tuned.", channel));
      }
      else if(DlnaResourceAccessUtils.ParseRadioChannel(uri, out channel))
      {
        dlnaItem = client.GetLiveDlnaItem(channel);

        if (dlnaItem == null)
          throw new Exception(string.Format("DLNA radio channel '{0}' was never tuned.", channel));
      }

      currentStreamItem.RequestedMediaItem = mediaItemGuid;
      currentStreamItem.TranscoderObject = dlnaItem;
      currentStreamItem.Title = dlnaItem?.MediaItemTitle;
      currentStreamItem.LiveChannelId = channel > 0 ? channel : 0;

      return currentStreamItem;
    }

    /// <summary>
    /// Returns a stream item based on the given client
    /// </summary>
    /// <returns>Returns the requested stream item otherwise null</returns>
    internal static StreamItem GetExistingStreamItem(EndPointSettings client)
    {
      if (STREAM_ITEMS.TryGetValue(client.ClientId, out var currentStreamItem))
        return currentStreamItem;

      return null;
    }
    
    /// <summary>
    /// Does the preparation to start a transcode stream
    /// </summary>
    internal static async Task<TranscodeContext> StartTranscodeStreamingAsync(EndPointSettings client, double startTime, double lengthTime, StreamItem newStreamItem)
    {
      if (STREAM_ITEMS.TryAdd(client.ClientId, newStreamItem))
      {
        await newStreamItem.BusyLock.WaitAsync();
        try
        {
          if (newStreamItem?.TranscoderObject?.TranscodingParameter == null)
          {
            STREAM_ITEMS.TryRemove(client.ClientId, out _);
            return null;
          }

          if (!newStreamItem.TranscoderObject.StartTrancoding())
          {
            Logger.Debug("StreamControl: Transcoding busy for mediaitem {0}", newStreamItem.RequestedMediaItem);
            return null;
          }

          if (newStreamItem.IsLive)
            newStreamItem.StreamContext = await MediaConverter.GetLiveStreamAsync(client.ClientId.ToString(), newStreamItem.TranscoderObject.TranscodingParameter, newStreamItem.LiveChannelId, true);
          else
            newStreamItem.StreamContext = await MediaConverter.GetMediaStreamAsync(client.ClientId.ToString(), newStreamItem.TranscoderObject.TranscodingParameter, startTime, lengthTime, true);

          if (newStreamItem.StreamContext is TranscodeContext context)
          {
            context.UpdateStreamUse(true);
            return context;
          }
          else if (newStreamItem.StreamContext != null)
          {
            //We want a transcoded stream
            newStreamItem.StreamContext.Dispose();
            newStreamItem.StreamContext = null;
          }

          return null;
        }
        finally
        {
          newStreamItem.BusyLock.Release();
        }
      }

      return null;
    }

    /// <summary>
    /// Does the preparation to start a stream
    /// </summary>
    internal static async Task<StreamContext> StartOriginalFileStreamingAsync(EndPointSettings client, StreamItem newStreamItem)
    {
      if (STREAM_ITEMS.TryAdd(client.ClientId, newStreamItem))
      {
        await newStreamItem.BusyLock.WaitAsync();
        try
        {
          IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
          List<IResourceAccessor> resources = new List<IResourceAccessor>();
          foreach (var res in newStreamItem.TranscoderObject.Metadata.FilePaths)
          {
            var path = ResourcePath.Deserialize(res.Value);
            if (mediaAccessor.LocalResourceProviders.TryGetValue(path.BasePathSegment.ProviderId, out var resourceProvider) &&
                     resourceProvider is IBaseResourceProvider baseProvider && baseProvider.TryCreateResourceAccessor(path.BasePathSegment.Path, out var accessor))
            {
              using (accessor)
              {
                if (accessor is IFileSystemResourceAccessor)
                {
                  newStreamItem.TranscoderObject.StartStreaming();
                  newStreamItem.StreamContext = await MediaConverter.GetFileStreamAsync(path);
                  return newStreamItem.StreamContext;
                }
              }
            }
          }
        }
        finally
        {
          newStreamItem.BusyLock.Release();
        }
      }

      return null;
    }

    /// <summary>
    /// Stops the streaming
    /// </summary>
    internal static async Task StopStreamingAsync(EndPointSettings client)
    {
      if (STREAM_ITEMS.TryRemove(client.ClientId, out var currentStreamItem))
      {
        await currentStreamItem.BusyLock.WaitAsync();
        try
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
        finally
        {
          currentStreamItem.BusyLock.Release();
        }
      }
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
