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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.NeighborhoodBrowser
{
  internal static class NeighborhoodBrowserHelper
  {
    #region Logging

    private static readonly object LOGGER_LOCK = new object();

    /// <summary>
    /// Logs a collection of IPHostEntries to our Logger
    /// </summary>
    /// <param name="hosts">Collection of IPHostEntries to be logged</param>
    /// <param name="neighborhoodBrowserName">Name of the <see cref="INeighborhoodBrowser"/></param>
    /// <param name="milliSeconds">Time in milliseconds it took to get the collection of IPHostEntries</param>
    internal static void LogResult(ICollection<IPHostEntry> hosts, String neighborhoodBrowserName, long milliSeconds)
    {
      if (hosts == null || hosts.Count == 0)
      {
        ServiceRegistration.Get<ILogger>().Warn("{0}: No computers in the NetworkNeighborhood found.", neighborhoodBrowserName);
        return;
      }
      lock (LOGGER_LOCK)
      {
        ServiceRegistration.Get<ILogger>().Debug("{0}: Found {1} computers in {2:N0} milliseconds.", neighborhoodBrowserName, hosts.Count, milliSeconds);
        foreach (var host in hosts)
          ServiceRegistration.Get<ILogger>().Debug("{0}:    HostName: '{1}', IP-Addresses: {2}", neighborhoodBrowserName, host.HostName, (host.AddressList == null) ? "<not found>" : String.Join(" / ", host.AddressList.Select(adress => adress.ToString())));
      }
    }

    #endregion

    # region HostName string helper

    /// <summary>
    /// Converts the HostName property of a <see cref="IPHostEntry"/> from a DNS name to a NetBios name if necessary
    /// </summary>
    /// <remarks>
    /// The NetBios name of a computer is always a subset of its DNS name.
    /// For details see here: http://msdn.microsoft.com/en-us/library/windows/desktop/ms724220%28v=vs.85%29.aspx
    /// </remarks>
    /// <param name="host"><see cref="IPHostEntry"/> the HostName property of which is converted</param>
    internal static void ConvertDnsHostNameToNetBiosName(IPHostEntry host)
    {
      // NetBios names are uppercase
      host.HostName = host.HostName.ToUpperInvariant();

      // No dot => it is already a NetBios name
      if (!host.HostName.Contains('.'))
        return;

      // remove everything from the first dot until the end
      host.HostName = host.HostName.Split('.')[0];

      // Truncate to max 15 characters
      if (host.HostName.Length > 15)
        host.HostName = host.HostName.Substring(0, 15);
    }

    #endregion
  }
}
