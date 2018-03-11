using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.ServerCommunication;

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
    /// <returns>AsyncResult.Success = <c>true</c> if successful.</returns>
    public static async Task<AsyncResult<IPAddress>> GetExternalIPAddressAsync()
    {
      IPAddress ip;
      using (var client = new HttpClient())
      {
        foreach (var site in LOOKUP_SITES)
        {
          try
          {
            string response = await client.GetStringAsync(site).ConfigureAwait(false);
            var result = response.Trim(' ', '\r', '\n');
            if (IPAddress.TryParse(result, out ip))
              return new AsyncResult<IPAddress>(true, ip);
          }
          catch (Exception ex)
          {
            ServiceRegistration.Get<ILogger>().Warn("Error resolving external IPAddress. Site: {0}.", site, ex);
          }
        }
      }
      return new AsyncResult<IPAddress>(false, null);
    }
  }
}
