#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities.Network;
using MediaPortal.Utilities.Process;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.NeighborhoodBrowser
{
  /// <summary>
  /// <see cref="INeighborhoodBrowser"/> implementation that uses the WNetEnum API
  /// to enumerate the computers in the NetworkNeighborhood
  /// </summary>
  class WNetEnumNeighborhoodBrowser : INeighborhoodBrowser
  {
    #region Constants

    private const string NETWORK_SERVICE_USERNAME = "NETWORK SERVICE";
    private const string NETWORK_SERVICE_DOMAIN = "NT AUTHORITY";
    private const string NETWORK_SERVICE_PASSWORD = "";

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
      ICollection<String> hostNames;
      
      // The WNetEnum API requires at least to be impersonated as NetworkService
      using (ImpersonateNetworkService())
        hostNames = NetworkResourcesEnumerator.EnumerateResources(ResourceScope.GlobalNet, ResourceType.Disk, ResourceUsage.All, ResourceDisplayType.Server);
      
      // The hostNames returned by the NetworkResourceEnumerator are in the form \\COMPUTERNAME
      // We have to remove the leading \\ to be able to pass the hostName to GetHostEntryAsync
      foreach (var hostName in hostNames)
        hosts[hostName.Substring(2)] = Dns.GetHostEntryAsync(hostName.Substring(2));

      // Wait until an IPHostEntry was found for all computers
      try
      {
        await Task.WhenAll(hosts.Values);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("WNetEnumNeighborhoodBrowser: Error while getting IPHostEntries", e.Message);
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

    /// <summary>
    /// Impersonates the NetworkService
    /// </summary>
    /// <remarks>
    /// The NetworkService can only be logged on with LogonType.NewCredentials or LogonType.Service.
    /// Access to the WNetEnum API is only possible when using LogonType.NewCredentials. Additionally,
    /// LogonType.NewCredentials also works on the client side when we are not running as LocalSystem.
    /// </remarks>
    /// <returns>ImpersonationConntext of the NetworkService</returns>
    private ImpersonationHelper.ImpersonationContext ImpersonateNetworkService()
    {
      var tokenHandle = IntPtr.Zero;
      try
      {
        if (NativeMethods.LogonUser(NETWORK_SERVICE_USERNAME, NETWORK_SERVICE_DOMAIN, NETWORK_SERVICE_PASSWORD, NativeMethods.LogonType.NewCredentials, NativeMethods.LogonProvider.Default, out tokenHandle))
          return new ImpersonationHelper.ImpersonationContext { Identity = new WindowsIdentity(tokenHandle) };

        var error = Marshal.GetLastWin32Error();
        ServiceRegistration.Get<ILogger>().Error("WNetEnumNeighborhoodBrowser:  Error when calling LogonUser: {0}", new Win32Exception(error).Message);
        return null;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("WNetEnumNeighborhoodBrowser:  Error when impersonating: {0}", e);
        return null;
      }
      finally
      {
        if (tokenHandle != IntPtr.Zero)
          NativeMethods.CloseHandle(tokenHandle);
      }
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
