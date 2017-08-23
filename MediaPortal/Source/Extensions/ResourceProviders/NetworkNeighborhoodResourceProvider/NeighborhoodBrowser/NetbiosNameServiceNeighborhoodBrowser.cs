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
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities.Network;
using MediaPortal.Utilities.Network.Netbios;

namespace MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.NeighborhoodBrowser
{
  /// <summary>
  /// <see cref="INeighborhoodBrowser"/> implementation that queries all IPv4 addresses in all local
  /// subnets with a CIDR-netmask >= 16 via Netbios Name Service
  /// </summary>
  class NetbiosNameServiceNeighborhoodBrowser : INeighborhoodBrowser
  {
    #region Constants

    private static readonly IPAddress LARGEST_SUBNET_MASK_TO_PROCESS = IPAddress.Parse("255.255.0.0");

    #endregion

    #region Private methods

    /// <summary>
    /// Asynchronously returns a collection of IPHostEntries of all computers found in the NetworkNeighborhood
    /// </summary>
    /// <returns></returns>
    private async Task<ICollection<IPHostEntry>> DoGetHostsAsync()
    {
      var stopWatch = System.Diagnostics.Stopwatch.StartNew();
      var networks = NetworkUtils.GetAllLocalIPv4Networks();
      ServiceRegistration.Get<ILogger>().Debug("NetbiosNameServiceNeighborhoodBrowser: Found {0} local IPv4 network(s) with IP address(es) {1}", networks.Count, String.Join(" / ", networks.Select(i => i.Address + " (" + i.IPv4Mask + ")")));

      networks = RemoveLargeSubnets(networks);
      ServiceRegistration.Get<ILogger>().Debug("NetbiosNameServiceNeighborhoodBrowser: {0} local IPv4 network(s) are used: {1}", networks.Count, String.Join(" / ", networks.Select(i => i.Address + " (" + i.IPv4Mask + ")")));

      var tasks = new Dictionary<IPAddress, Task<NbNsNodeStatusResponse>>();
      using (var client = new NbNsClient())
      {
        foreach (var target in networks.SelectMany(NetworkUtils.GetAllAddressesInSubnet))
          tasks.Add(target, client.SendUnicastNodeStatusRequestAsync(NbNsNodeStatusRequest.WildCardNodeStatusRequest, target));
        await Task.WhenAll(tasks.Values);

        // Uncomment the following two lines for extensive logging
        // foreach (var kvp in tasks.Where(kvp => kvp.Value.Result != null))
        //   ServiceRegistration.Get<ILogger>().Debug("NetbiosNameServiceNeighborhoodBrowser: Found {0} ({1})\r\n{2}", kvp.Value.Result.WorkstationName, kvp.Key, kvp.Value.Result);
      }
      
      // If the computer has multiple network interfaces, it is found multiple times - once for each network interface.
      // Every occurence of the computer then has a different IP address (the one of the respective network interface).
      // As result we want this computer only as one IPHostEntry, but IPHostEntry.AddressList should show all IP addresses.
      var result = new HashSet<IPHostEntry>();
      var responses = tasks.Where(kvp => kvp.Value.Result != null);
      foreach (var kvp in responses)
      {
        var alreadyPresentHost = result.FirstOrDefault(host => host.HostName == kvp.Value.Result.WorkstationName);
        if (alreadyPresentHost == null)
          result.Add(new IPHostEntry { AddressList = new[] { kvp.Key }, HostName = kvp.Value.Result.WorkstationName });
        else
        {
          if (!alreadyPresentHost.AddressList.Any(address => address.Equals(kvp.Key)))
            alreadyPresentHost.AddressList = alreadyPresentHost.AddressList.Union(new[] { kvp.Key }).ToArray();
        }
      }
      NeighborhoodBrowserHelper.LogResult(result, GetType().Name, stopWatch.ElapsedMilliseconds);
      return result;
    }

    /// <summary>
    /// Takes a collection of <see cref="UnicastIPAddressInformation"/>s and removes those with a network mask
    /// resulting in more IP addresses than specified in <see cref="LARGEST_SUBNET_MASK_TO_PROCESS"/>
    /// </summary>
    /// <param name="networks">Collection of <see cref="UnicastIPAddressInformation"/>s to process</param>
    /// <returns>Collection of <see cref="UnicastIPAddressInformation"/>s without large subnets</returns>
    public static ICollection<UnicastIPAddressInformation> RemoveLargeSubnets(IEnumerable<UnicastIPAddressInformation> networks)
    {
      var result = new HashSet<UnicastIPAddressInformation>();
      foreach (var network in networks)
      {
        if (NetworkUtils.ToUInt32(network.IPv4Mask) < NetworkUtils.ToUInt32(LARGEST_SUBNET_MASK_TO_PROCESS))
          ServiceRegistration.Get<ILogger>().Debug("NetbiosNameServiceNeighborhoodBrowser: Network on IP endpoint {0} has subnet mask {1}. Will not process this subnet because it is too large.", network.Address, network.IPv4Mask);
        else
          result.Add(network);
      }
      return result;
    }

    #endregion

    #region INeighborhoodBrowser implementation

    public async Task<ICollection<IPHostEntry>> GetHostsAsync()
    {
      return await Task.Run(new Func<Task<ICollection<IPHostEntry>>>(DoGetHostsAsync));
    }

    #endregion
  }
}
