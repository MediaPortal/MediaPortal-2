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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common.Data;
using MediaPortal.Utilities.Network;
using System;
using System.Net;
using System.Net.NetworkInformation;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Common
{
  /// <summary>
  /// Network trace route helper library.
  /// </summary>
  public class TraceRoute
  {
    #region Public methods

    /// <summary>
    /// Lookup the first IP address external to the LAN.
    /// </summary>
    /// <param name="ip">External host to trace too.</param>
    /// <param name="maxTtl">Maximum number of hops to try.</param>
    /// <param name="response">Trace route result.</param>
    /// <returns>If the trace is successful.</returns>
    public bool TrySearchForFirstExternalAddress(IPAddress ip, int maxTtl, out TraceRouteResponse response)
    {
      for (int i = 0; i < maxTtl; i++)
      {
        if (TryLookupInternal(ip, i, out response)) return true;
      }

      response = null;
      return false;
    }

    #endregion Public methods

    #region Private methods

    private bool TryLookupInternal(IPAddress remoteHost, int ttl, out TraceRouteResponse response)
    {
      if (ttl < 1)
      {
        response = null;
        return false;
      }

      if (!NetworkConnectionTracker.IsNetworkConnected)
      {
        ServiceRegistration.Get<ILogger>().Debug("TraceRoute: Lookup - No Network connected");
        response = null;
        return false;
      }

      try
      {
        using (Ping sender = new Ping())
        {
          PingOptions options = new PingOptions(ttl, true);

          PingReply reply = sender.Send(remoteHost, 1000, new byte[32], options);

          if (reply != null)
          {
            response = new TraceRouteResponse()
            {
              RemoteHost = remoteHost,
              FirstResponseTtl = ttl,
              FirstResponseHostname = Dns.GetHostEntry(reply.Address).HostName,
              FirstResponseIP = reply.Address
            };
            return true;
          }
        }
      }
      catch (Exception)
      {
        response = null;
        return false;
      }

      response = null;
      return false;
    }

    #endregion Private methods
  }
}