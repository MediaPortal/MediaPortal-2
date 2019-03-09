#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Async;
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
