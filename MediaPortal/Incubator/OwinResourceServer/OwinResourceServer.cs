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
using Microsoft.Owin.StaticFiles;
using Owin;

namespace MediaPortal.Plugins.WebServices.OwinResourceServer
{
  /// <summary>
  /// Implementation of <see cref="IResourceServer"/> using the <see cref="OwinServer"/>.
  /// </summary>
  public class OwinResourceServer : IResourceServer
  {
    #region Public methods

    /// <summary>
    /// Configures the ResourceServer WebApp
    /// </summary>
    /// <remarks>
    /// It is important to first start the <see cref="ResourceQueryToPathMiddleware"/>, which translates the query string
    /// received by the client into a path string and then the <see cref="StaticFileMiddleware"/>. The latter is configured with the
    /// <see cref="ResourceServerFileSystem"/>, which can only understand the path string because the StaticFilesMiddleware
    /// only passes the path string as parameter to the methods of <see cref="ResourceServerFileSystem"/>.
    /// </remarks>
    /// <param name="app"></param>
    public void Startup(IAppBuilder app)
    {
      app.Use<ResourceQueryToPathMiddleware>();
      var options = new StaticFileOptions { FileSystem = new ResourceServerFileSystem(), ServeUnknownFileTypes = true };
      app.UseStaticFiles(options);
    }

    #endregion

    #region IResourceServer Implementation

    public int PortIPv4 { get; private set; }

    public int PortIPv6 { get; private set; }

    public void Startup()
    {
      // ToDo: Make the port (a) configurable and (b) detect whether the port is already in use
      PortIPv4 = 50000;
      PortIPv6 = 50000;
      
      // Startup happens asynchronously
      ServiceRegistration.Get<IOwinServer>().TryStartWebAppAsync(Startup, PortIPv4, ResourceHttpAccessUrlUtils.RESOURCE_ACCESS_PATH);
    }

    public void Shutdown()
    {
      // Handled by OwinServer
    }

    public void RestartHttpServers()
    {
      // We need to wait until TryStopWebAppAsync has completed
      // ToDo: Let the OwinServer coordinate this
      ServiceRegistration.Get<IOwinServer>().TryStopWebAppAsync(PortIPv4, ResourceHttpAccessUrlUtils.RESOURCE_ACCESS_PATH).Wait();
      
      // Restart happens asynchronously
      ServiceRegistration.Get<IOwinServer>().TryStartWebAppAsync(Startup, PortIPv4, ResourceHttpAccessUrlUtils.RESOURCE_ACCESS_PATH);
    }

    public void RemoveHttpModule(HttpModule module)
    {
      // ToDo: Remove this medthod from the IResourceServer interface
    }

    public void AddHttpModule(HttpModule module)
    {
      // ToDo: Remove this medthod from the IResourceServer interface
    }

    #endregion
  }
}
