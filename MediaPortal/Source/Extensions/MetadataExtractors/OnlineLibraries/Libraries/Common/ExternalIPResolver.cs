using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Common
{
  /// <summary>
  /// Helper class to detect the public IP address. Lookup is based on different online sites.
  /// </summary>
  public class ExternalIPResolver
  {
    private static List<string> LOOKUP_SITES = new List<string>
    {
      "http://checkip.amazonaws.com/",
      "https://ipinfo.io/ip",
      "https://api.ipify.org"
    };

    /// <summary>
    /// Tries to lookup the external IP address.
    /// </summary>
    /// <param name="ip">IP</param>
    /// <returns><c>true</c> if successful.</returns>
    public static bool GetExternalIPAddress(out IPAddress ip)
    {
      using (var client = new WebClient())
      {
        client.Headers["User-Agent"] = "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";

        foreach (var site in LOOKUP_SITES)
        {
          try
          {
            string response = client.DownloadString(site);
            var result = response.Trim(' ', '\r', '\n');
            if (IPAddress.TryParse(result, out ip))
              return true;
          }
          catch (Exception ex)
          {
            ServiceRegistration.Get<ILogger>().Warn("Error resolving external IPAddress. Site: {0}.", site, ex);
          }
        }
      }
      ip = null;
      return false;
    }
  }
}
