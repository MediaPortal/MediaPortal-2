using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
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
      STREAM_ITEMS.Remove(identifier);
    }

    internal static void UpdateStreamItem(string identifier, StreamItem item)
    {
      DeleteStreamItem(identifier);
      AddStreamItem(identifier, item);
    }

    internal static StreamItem GetStreamItem(string identifier)
    {
      return STREAM_ITEMS[identifier];
    }

    internal static Dictionary<string, StreamItem> GetStreamItems()
    {
      return STREAM_ITEMS;
    }

    internal static bool ValidateIdentifie(string identifier)
    {
      return STREAM_ITEMS.ContainsKey(identifier);
    }

    internal static void StartStreaming(string identifier)
    {
      
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
