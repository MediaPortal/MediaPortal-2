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

using HttpServer.HttpModules;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Plugins.WebServices.OwinServer;
using Owin;

namespace MediaPortal.Plugins.WebServices.OwinResourceServer
{
  /// <summary>
  /// Implementation of <see cref="IResourceServer"/> using the <see cref="OwinServer"/>.
  /// </summary>
  public class OwinResourceServer : IResourceServer
  {
    public void Startup(IAppBuilder app)
    {
    }    
    
    public int PortIPv4 { get; private set; }

    public int PortIPv6 { get; private set; }

    public void Startup()
    {
      PortIPv4 = 50000;
      PortIPv6 = 50000;
      ServiceRegistration.Get<IOwinServer>().TryStartWebAppAsync(Startup, PortIPv4, ResourceHttpAccessUrlUtils.RESOURCE_ACCESS_PATH);
    }

    public void Shutdown()
    {
    }

    public void RestartHttpServers()
    {
    }

    public void RemoveHttpModule(HttpModule module)
    {
    }

    public void AddHttpModule(HttpModule module)
    {
    }
  }
}
