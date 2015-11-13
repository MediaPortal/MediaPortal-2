using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
using MediaPortal.Plugins.Transcoding.Service;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.Base;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS
{
  static class StreamControl
  {
    private static readonly Dictionary<string, StreamItem> STREAM_ITEMS = new Dictionary<string, StreamItem>();
    public static Dictionary<string, Dictionary<string, List<TranscodeContext>>> CurrentClientTranscodes = new Dictionary<string, Dictionary<string, List<TranscodeContext>>>();

    internal static void AddStreamItem(string identifier, StreamItem item)
    {
      if (ValidateIdentifie(identifier))
      {
        Logger.Debug("StreamControl: identifier {0} is already in list -> deleting old stream item", identifier);
        DeleteStreamItem(identifier);
      }

      STREAM_ITEMS.Add(identifier, item);
    }

    internal static void DeleteStreamItem(string identifier)
    {
      if (ValidateIdentifie(identifier))
      {
        StopStreaming(identifier);
        STREAM_ITEMS.Remove(identifier);
      }
    }

    internal static void UpdateStreamItem(string identifier, StreamItem item)
    {
      if (ValidateIdentifie(identifier))
      {
        DeleteStreamItem(identifier);
      }
      AddStreamItem(identifier, item);
    }

    internal static StreamItem GetStreamItem(string identifier)
    {
      if (ValidateIdentifie(identifier))
      {
        return STREAM_ITEMS[identifier];
      }
      return null;
    }

    internal static Dictionary<string, StreamItem> GetStreamItems()
    {
      return STREAM_ITEMS;
    }

    internal static bool ValidateIdentifie(string identifier)
    {
      return STREAM_ITEMS.ContainsKey(identifier);
    }

    internal static void StartStreaming(string identifier, TranscodeContext context)
    {
      if (ValidateIdentifie(identifier))
      {
        STREAM_ITEMS[identifier].IsActive = true;
        STREAM_ITEMS[identifier].StreamContext = context;
        STREAM_ITEMS[identifier].TranscoderObject.SegmentDir = context.SegmentDir;
      }
    }

    internal static void StopStreaming(string identifier)
    {
      if (ValidateIdentifie(identifier))
      {
        STREAM_ITEMS[identifier].IsActive = false;
        if (STREAM_ITEMS[identifier].StreamContext != null) 
          STREAM_ITEMS[identifier].StreamContext.InUse = false;
        if (STREAM_ITEMS[identifier].TranscoderObject != null) 
          STREAM_ITEMS[identifier].TranscoderObject.StopStreaming();
      }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
