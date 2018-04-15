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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;

namespace MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.NeighborhoodBrowser
{
  public class NeighborhoodBrowserService : INeighborhoodBrowserSerivce
  {
    #region Private fields

    private readonly ConcurrentBag<INeighborhoodBrowser> _browsers;

    #endregion

    #region Constructor

    public NeighborhoodBrowserService()
    {
      _browsers = new ConcurrentBag<INeighborhoodBrowser>();

      var browserSettings = ServiceRegistration.Get<ISettingsManager>().Load<NeighborhoodBrowserServiceSettings>();

      if (browserSettings.UseWNetEnumNeighborhoodBrowser)
        RegisterBrowser(new WNetEnumNeighborhoodBrowser());
      if (browserSettings.UseDirectoryEntryNeighborhoodBrowser)
        RegisterBrowser(new DirectoryEntryNeighborhoodBrowser());
      if (browserSettings.UseNetbiosNameServiceNeighborhoodBrowser)
        RegisterBrowser(new NetbiosNameServiceNeighborhoodBrowser());
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Returns a deduplicated list of all computers in the NetworkNeighborhood
    /// </summary>
    /// <returns>List of computers in the NetworkNeighborhood</returns>
    private async Task<ICollection<IPHostEntry>> GetHostsAsync()
    {
      if (!_browsers.Any())
      {
        ServiceRegistration.Get<ILogger>().Error("NeighborhoodBrowserService: No Browsers enabled in NeighborhoodBrowserServiceSettings.xml");
        return new List<IPHostEntry>();
      }
      
      var stopWatch = System.Diagnostics.Stopwatch.StartNew();
      ICollection<IPHostEntry> result = null;
      var tasks = _browsers.Select(browser => browser.GetHostsAsync()).ToList();
      try
      {
        await Task.WhenAll(tasks);
        result = Combine(tasks.Select(task => task.Result).ToList());
        NeighborhoodBrowserHelper.LogResult(result, GetType().Name, stopWatch.ElapsedMilliseconds);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("NeighborhoodBrowserService: Error collecting neighborhood computers", e);
      }
      return result;
    }

    /// <summary>
    /// Combines several collections of <see cref="IPHostEntry"/> objects into one deduplicated list
    /// </summary>
    /// <param name="collections">Collections to combine</param>
    /// <returns>Deduplicated list of <see cref="IPHostEntry"/> objects</returns>
    private static ICollection<IPHostEntry> Combine(IEnumerable<ICollection<IPHostEntry>> collections)
    {
      var result = new HashSet<IPHostEntry>();
      if (collections == null)
      {
        return result;
      }
      foreach (var collection in collections.Where(collection => collection != null))
      {
        foreach (var host in collection.Where(host => host?.AddressList != null && host.AddressList.Any()))
        {
          if (String.IsNullOrEmpty(host.HostName))
          { 
            // We only have one or more IP addresses - no HostName. We consider this host the same as a host we
            // already have in our result, when at least one IP address in this host equals one IP address of the result host.
            host.HostName = String.Empty;
            host.AddressList = host.AddressList.Where(address => address != null).ToArray();
            var alreadyPresentHost = result.FirstOrDefault(presentHost => presentHost.AddressList.Intersect(host.AddressList).Any());
            if (alreadyPresentHost == null)
              result.Add(host);
            else
              alreadyPresentHost.AddressList = alreadyPresentHost.AddressList.Union(host.AddressList).ToArray();
          }
          else
          {
            // We have both, HostName and one or more IP addresses.
            // If there is already a host with the same HostName, we combine the IP addresses.
            // If there is a host with a different HostName, but at least one identical IP address,
            //   if the already present HostName is String.Empty, we replace the empty string with the new HostName
            //     and combine the IP addresses;
            //   if the already present HostName is not String.Empty, we log a warning,
            //     combine the IP addresses of the already present host and discard the HostName of the new host.
            // If there is no host with the same HostName and no host with one or more identical IP addresses, we
            //   add the new host to the result.
            host.AddressList = host.AddressList.Where(address => address != null).ToArray();
            var alreadyPresentHost = result.FirstOrDefault(presentHost => presentHost.HostName == host.HostName);
            if (alreadyPresentHost != null)
            {
              alreadyPresentHost.AddressList = alreadyPresentHost.AddressList.Union(host.AddressList).ToArray();
            }
            else
            {
              alreadyPresentHost = result.FirstOrDefault(presentHost => presentHost.AddressList.Intersect(host.AddressList).Any());
              if (alreadyPresentHost != null)
              {
                if (alreadyPresentHost.HostName == String.Empty)
                {
                  alreadyPresentHost.HostName = host.HostName;
                  alreadyPresentHost.AddressList = alreadyPresentHost.AddressList.Union(host.AddressList).ToArray();
                }
                else
                {
                  ServiceRegistration.Get<ILogger>().Warn("NeighborhoodBrowserService: Found two computers with different HostNames but at least one identical IP-Address:");
                  ServiceRegistration.Get<ILogger>().Warn("NeighborhoodBrowserService:   HostName: '{0}', IP-Addresses {1}", alreadyPresentHost.HostName, String.Join(" / ", alreadyPresentHost.AddressList.Select(adress => adress.ToString())));
                  ServiceRegistration.Get<ILogger>().Warn("NeighborhoodBrowserService:   HostName: '{0}', IP-Addresses {1}", host.HostName, String.Join(" / ", host.AddressList.Select(adress => adress.ToString())));
                  ServiceRegistration.Get<ILogger>().Warn("NeighborhoodBrowserService:   Discarding the second HostName and adding its IP-Addresses to the first host.");
                  alreadyPresentHost.AddressList = alreadyPresentHost.AddressList.Union(host.AddressList).ToArray();
                }
              }
              else
              {
                result.Add(host);
              }
            }
          }
        }
      }
      return result;
    }

    #endregion

    #region INeighborhoodBrowserSerivce implementation

    public ICollection<IPHostEntry> Hosts
    {
      get { return GetHostsAsync().Result; }
    }

    public void RegisterBrowser(INeighborhoodBrowser browser)
    {
      _browsers.Add(browser);
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      foreach (var d in _browsers.OfType<IDisposable>())
        d.Dispose();
    }

    #endregion
  }
}
