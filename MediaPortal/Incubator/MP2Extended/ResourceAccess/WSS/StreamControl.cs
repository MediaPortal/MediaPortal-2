using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS
{
  static class StreamControl
  {
    private static readonly Dictionary<string, StreamItem> STREAM_ITEMS = new Dictionary<string, StreamItem>();
    internal static readonly Dictionary<string, EndPointProfile> PROFILES = new Dictionary<string, EndPointProfile>();

    internal static void AddStreamItem(string identifier, StreamItem item)
    {
      if (STREAM_ITEMS.ContainsKey(identifier))
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

    internal static bool ValidateIdentifie(string identifier)
    {
      return STREAM_ITEMS.ContainsKey(identifier);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
