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
using System.Diagnostics;
using System.DirectoryServices;
using System.Net;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.NeighborhoodBrowser
{
  /// <summary>
  /// <see cref="INeighborhoodBrowser"/> implementation that uses the <see cref="DirectoryEntry"/> class
  /// to enumerate the computers in the NetworkNeighborhood
  /// </summary>
  class DirectoryEntryNeighborhoodBrowser : INeighborhoodBrowser
  {
    #region Constants

    /// <summary>
    /// Name of the provider to be used for the <see cref="DirectoryEntry"/> class
    /// </summary>
    /// <remarks>
    /// The <see cref="DirectoryEntry"/> class provides access to various directory services via various providers.
    /// The provider that works without an ActiveDirectory installation is the WinNT-Provider.
    /// </remarks>
    private const String PROVIDER_STRING = "WinNT:";

    /// <summary>
    /// Name of the computer object
    /// </summary>
    private const String COMPUTER_OBJECT = "Computer";

    #endregion

    #region Private methods

    /// <summary>
    /// Asynchronously returns a collection of IPHostEntries of all computers found in the NetworkNeighborhood
    /// </summary>
    /// <returns></returns>
    private async Task<ICollection<IPHostEntry>> DoGetHostsAsync()
    {
      var stopWatch = Stopwatch.StartNew();

      // Enumerate all computers in the NetworkNeighborhood for all Domains / Workgroups
      var hosts = new Dictionary<String, Task<IPHostEntry>>();
      try
      {
        using (var root = new DirectoryEntry(PROVIDER_STRING))
        {
          foreach (DirectoryEntry domain in root.Children)
          {
            using (domain)
            {
              foreach (DirectoryEntry computer in domain.Children)
              {
                using (computer)
                {
                  if (computer.SchemaClassName == COMPUTER_OBJECT)
                  {
                    ServiceRegistration.Get<ILogger>().Info("DirectoryEntryNeighborhoodBrowser: Adding '{0}' to hosts list.", computer.Name);
                    hosts[computer.Name] = Dns.GetHostEntryAsync(computer.Name);
                  }
                }
              }
            }
          }
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("DirectoryEntryNeighborhoodBrowser: Error while enumerating computers in the NetworkNeighborhood", e);
      }

      // Wait until an IPHostEntry was found for all computers
      try
      {
        await Task.WhenAll(hosts.Values);
      }
      catch (System.Net.Sockets.SocketException) { }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("DirectoryEntryNeighborhoodBrowser: Error while getting IPHostEntries", e);
      }

      // Assemble the result
      var result = new HashSet<IPHostEntry>();
      foreach (var kvp in hosts)
      {
        // If the call to GetHostEntryAsync was successful, we return the retrieved IPHostEntry
        // If not, we return a new IPHostEntry object just with its HostName property set.
        var host = kvp.Value.IsFaulted ? new IPHostEntry { HostName = kvp.Key } : kvp.Value.Result;
        NeighborhoodBrowserHelper.ConvertDnsHostNameToNetBiosName(host);
        result.Add(host);
      }
      NeighborhoodBrowserHelper.LogResult(result, GetType().Name, stopWatch.ElapsedMilliseconds);
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
