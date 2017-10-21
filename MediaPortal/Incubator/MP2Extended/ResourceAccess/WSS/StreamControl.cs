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

using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS
{
  static class StreamControl
  {
    private static readonly Dictionary<string, StreamItem> STREAM_ITEMS = new Dictionary<string, StreamItem>();

    /// <summary>
    /// Adds a new stream Item to the list.
    /// </summary>
    /// <param name="identifier">The unique string which identifies the stream Item</param>
    /// <param name="item">The stream item which should be added</param>
    internal static void AddStreamItem(string identifier, StreamItem item)
    {
      if (DeleteStreamItem(identifier))
      {
        Logger.Debug("StreamControl: identifier {0} is already in list -> deleting old stream item", identifier);
      }

      STREAM_ITEMS.Add(identifier, item);
    }

    /// <summary>
    /// Deletes a stream Item from the list
    /// </summary>
    /// <param name="identifier">The unique string which identifies the stream Item</param>
    /// <returns>Returns true if an item was deleted, false if no item was deleted</returns>
    internal static bool DeleteStreamItem(string identifier)
    {
      if (ValidateIdentifier(identifier))
      {
        StopStreaming(identifier);
        if (STREAM_ITEMS[identifier].TranscoderObject != null)
          STREAM_ITEMS[identifier].TranscoderObject.StopTranscoding();
        STREAM_ITEMS.Remove(identifier);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Updates an already existent stream item
    /// </summary>
    /// <param name="identifier">The unique string which identifies the stream Item</param>
    /// <param name="item">The updated stream item</param>
    internal static void UpdateStreamItem(string identifier, StreamItem item)
    {
      DeleteStreamItem(identifier);
      AddStreamItem(identifier, item);
    }

    /// <summary>
    /// Returns a stream item based on the given identifier
    /// </summary>
    /// <param name="identifier">The unique string which identifies the stream Item</param>
    /// <returns>Returns the requested stream item otherwise null</returns>
    internal static StreamItem GetStreamItem(string identifier)
    {
      if (ValidateIdentifier(identifier))
      {
        return STREAM_ITEMS[identifier];
      }
      return null;
    }

    /// <summary>
    /// Gets all available stream items
    /// </summary>
    /// <returns>Returns a Dictionary of stream Items</returns>
    internal static Dictionary<string, StreamItem> GetStreamItems()
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
    internal static void StartStreaming(string identifier, double startTime)
    {
      if (ValidateIdentifier(identifier))
      {
        lock (STREAM_ITEMS[identifier].BusyLock)
        {
          if (STREAM_ITEMS[identifier].TranscoderObject == null) return;
          if (STREAM_ITEMS[identifier].TranscoderObject.StartTrancoding() == false)
          {
            Logger.Debug("StreamControl: Transcoding busy for mediaitem {0}", STREAM_ITEMS[identifier].RequestedMediaItem.MediaItemId);
            return;
          }
          STREAM_ITEMS[identifier].TranscoderObject.StartStreaming();
          if (STREAM_ITEMS[identifier].IsLive == true)
          {
            STREAM_ITEMS[identifier].StreamContext = MediaConverter.GetLiveStream(identifier, STREAM_ITEMS[identifier].TranscoderObject.TranscodingParameter, STREAM_ITEMS[identifier].LiveChannelId, true);
          }
          else
          {
            STREAM_ITEMS[identifier].StreamContext = MediaConverter.GetMediaStream(identifier, STREAM_ITEMS[identifier].TranscoderObject.TranscodingParameter, startTime, 0, true);
          }
          STREAM_ITEMS[identifier].TranscoderObject.SegmentDir = STREAM_ITEMS[identifier].StreamContext.SegmentDir;
          STREAM_ITEMS[identifier].StreamContext.InUse = true;
          STREAM_ITEMS[identifier].IsActive = true;
        }
      }
    }

    /// <summary>
    /// Stops the streaming
    /// </summary>
    /// <param name="identifier">The unique string which identifies the stream Item</param>
    internal static void StopStreaming(string identifier)
    {
      if (ValidateIdentifier(identifier))
      {
        lock (STREAM_ITEMS[identifier].BusyLock)
        {
          STREAM_ITEMS[identifier].IsActive = false;
          if (STREAM_ITEMS[identifier].TranscoderObject != null)
            STREAM_ITEMS[identifier].TranscoderObject.StopStreaming();

          if (STREAM_ITEMS[identifier].StreamContext != null)
            STREAM_ITEMS[identifier].StreamContext.InUse = false;
        }
      }
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
