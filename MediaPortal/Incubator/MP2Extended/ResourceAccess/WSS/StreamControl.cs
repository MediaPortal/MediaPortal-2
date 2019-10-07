#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS
{
  static class StreamControl
  {
    private static readonly ConcurrentDictionary<string, StreamItem> StreamItems = new ConcurrentDictionary<string, StreamItem>();

    /// <summary>
    /// Adds a new stream Item to the list.
    /// </summary>
    /// <param name="identifier">The unique string which identifies the stream Item</param>
    /// <param name="item">The stream item which should be added</param>
    internal static async Task<bool> AddStreamItemAsync(string identifier, StreamItem item)
    {
      if (await DeleteStreamItemAsync(identifier))
        Logger.Debug("StreamControl: Identifier {0} is already in list -> deleting old stream item", identifier);

      return StreamItems.TryAdd(identifier, item);
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
        if(StreamItems.TryRemove(identifier, out var stream))
        {
          if (stream.TranscoderObject != null)
            stream.TranscoderObject.StopTranscoding();
        }
        return true;
      }
      return false;
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
      if (ValidateIdentifier(identifier))
      {
        if (StreamItems.TryGetValue(identifier, out var stream))
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
      return StreamItems;
    }

    internal static bool ValidateIdentifier(string identifier)
    {
      return StreamItems.ContainsKey(identifier);
    }

    /// <summary>
    /// Does the preparation to start a stream
    /// </summary>
    /// <param name="identifier">The unique string which identifies the stream Item</param>
    /// <param name="context">Transcoder context</param>
    internal static async Task StartStreamingAsync(string identifier, double startTime)
    {
      if (ValidateIdentifier(identifier))
      {
        if (StreamItems.TryGetValue(identifier, out var stream))
        {
          await stream.BusyLock.WaitAsync();
          try
          {
            if (stream.TranscoderObject == null)
              return;
            if (stream.TranscoderObject.StartTrancoding() == false)
            {
              Logger.Debug("StreamControl: Transcoding busy for mediaitem {0}", stream.RequestedMediaItem.MediaItemId);
              return;
            }
            stream.TranscoderObject.StartStreaming();
            if (stream.IsLive == true)
              stream.StreamContext = await MediaConverter.GetLiveStreamAsync(identifier, stream.TranscoderObject.TranscodingParameter, stream.LiveChannelId, true);
            else
              stream.StreamContext = await MediaConverter.GetMediaStreamAsync(identifier, stream.TranscoderObject.TranscodingParameter, startTime, 0, true);

            stream.TranscoderObject.SegmentDir = stream.StreamContext.SegmentDir;
            stream.IsActive = true;
          }
          finally
          {
            stream.BusyLock.Release();
          }
        }
      }
    }

    /// <summary>
    /// Stops the streaming
    /// </summary>
    /// <param name="identifier">The unique string which identifies the stream Item</param>
    internal static async Task<bool> StopStreamingAsync(string identifier)
    {
      if (ValidateIdentifier(identifier))
      {
        if (StreamItems.TryGetValue(identifier, out var stream))
        {
          await stream.BusyLock.WaitAsync();
          try
          {
            stream.IsActive = false;
            if (stream.TranscoderObject != null)
              stream.TranscoderObject.StopStreaming();

            if (stream.StreamContext != null)
              stream.StreamContext.UpdateStreamUse(false);

            return true;
          }
          finally
          {
            stream.BusyLock.Release();
          }
        }
      }
      return false;
    }

    internal static IMediaConverter MediaConverter
    {
      get { return ServiceRegistration.Get<IMediaConverter>(); }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
