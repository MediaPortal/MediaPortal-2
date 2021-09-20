#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using Microsoft.Owin;
using System.Net;
using System.Net.Sockets;
using System.Web;

namespace MediaPortal.Plugins.MP2Extended.Utils
{
  internal static class GetBaseStreamUrl
  {
    public static string GetBaseStreamURL(IOwinContext context)
    {
      var rs = ServiceRegistration.Get<IResourceServer>();
      return rs.GetServiceUrl(GetLocalIp());
    }

    private static IPAddress GetLocalIp()
    {
      bool useIPv4 = true;
      bool useIPv6 = false;
      ServerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ServerSettings>();
      if (settings.UseIPv4) useIPv4 = true;
      if (settings.UseIPv6) useIPv6 = true;

      var host = Dns.GetHostEntry(Dns.GetHostName());
      IPAddress ip6 = null;
      foreach (var ip in host.AddressList)
      {
        if (IPAddress.IsLoopback(ip) == true)
        {
          continue;
        }
        if (useIPv4)
        {
          if (ip.AddressFamily == AddressFamily.InterNetwork)
          {
            return ip;
          }
        }
        if (useIPv6)
        {
          if (ip.AddressFamily == AddressFamily.InterNetworkV6)
          {
            ip6 = ip;
          }
        }
      }
      if (ip6 != null)
      {
        return ip6;
      }
      return null;
    }
  }
}
