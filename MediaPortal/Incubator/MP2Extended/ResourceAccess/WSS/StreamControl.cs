using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.Base;
using MediaPortal.Plugins.Transcoding.Service;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS
{
  static class StreamControl
  {
    private static readonly Dictionary<string, StreamItem> STREAM_ITEMS = new Dictionary<string, StreamItem>();

    /// <summary>
    /// Adds a new stream Item to the list.
    /// </summary>
    /// <param name="identifier">The unique string which idetifies the stream Item</param>
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
    /// <param name="identifier">The unique string which idetifies the stream Item</param>
    /// <returns>Returns true if an item was deleted, false if no item was deleted</returns>
    internal static bool DeleteStreamItem(string identifier)
    {
      if (ValidateIdentifie(identifier))
      {
        StopStreaming(identifier);
        STREAM_ITEMS.Remove(identifier);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Updates an already existent stream item
    /// </summary>
    /// <param name="identifier">The unique string which idetifies the stream Item</param>
    /// <param name="item">The updated stream item</param>
    internal static void UpdateStreamItem(string identifier, StreamItem item)
    {
      DeleteStreamItem(identifier);
      AddStreamItem(identifier, item);
    }

    /// <summary>
    /// Returns a stream item based on the given identifier
    /// </summary>
    /// <param name="identifier">The unique string which idetifies the stream Item</param>
    /// <returns>Returns the requested stream item otherwise null</returns>
    internal static StreamItem GetStreamItem(string identifier)
    {
      if (ValidateIdentifie(identifier))
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

    internal static bool ValidateIdentifie(string identifier)
    {
      return STREAM_ITEMS.ContainsKey(identifier);
    }

    /// <summary>
    /// Does the preparation to start a stream
    /// </summary>
    /// <param name="identifier">The unique string which idetifies the stream Item</param>
    /// <param name="context">Transcoder context</param>
    internal static void StartStreaming(string identifier, double startTime)
    {
      if (ValidateIdentifie(identifier))
      {
        lock (STREAM_ITEMS[identifier].BusyLock)
        {
          STREAM_ITEMS[identifier].StreamContext = MediaConverter.GetMediaStream(identifier, STREAM_ITEMS[identifier].TranscoderObject.TranscodingParameter, startTime, 0, true);
          STREAM_ITEMS[identifier].TranscoderObject.SegmentDir = STREAM_ITEMS[identifier].StreamContext.SegmentDir;
          STREAM_ITEMS[identifier].StreamContext.InUse = true;
          STREAM_ITEMS[identifier].IsActive = true;
        }
      }
    }

    /// <summary>
    /// Stops the streaming
    /// </summary>
    /// <param name="identifier">The unique string which idetifies the stream Item</param>
    internal static void StopStreaming(string identifier)
    {
      if (ValidateIdentifie(identifier))
      {
        STREAM_ITEMS[identifier].IsActive = false;
        if (STREAM_ITEMS[identifier].TranscoderObject != null) 
          STREAM_ITEMS[identifier].TranscoderObject.StopStreaming();

        lock (STREAM_ITEMS[identifier].BusyLock)
        {
          if (STREAM_ITEMS[identifier].StreamContext != null)
            STREAM_ITEMS[identifier].StreamContext.InUse = false;
        }
      }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
