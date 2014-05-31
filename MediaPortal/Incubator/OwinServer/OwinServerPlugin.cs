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

using MediaPortal.Common;
using MediaPortal.Common.PluginManager;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;

namespace MediaPortal.Plugins.WebServices.OwinServer
{
  /// <summary>
  /// PluginStateStracker for the <see cref="OwinServer"/>.
  /// </summary>
  /// <remarks>
  /// Creates an instance of <see cref="OwinServer"/> and registers it in the <see cref="ServiceRegistration"/>
  /// when the plugin is activated. Calls <see cref="OwinServer.ShutdownAsync"/>, when the plugin is shut down.
  /// </remarks>
  public class OwinServerPlugin : IPluginStateTracker
  {

    #region Test Code To Be Removed

    public void StartupWelcomePage(IAppBuilder appBuilder)
    {
      appBuilder.UseWelcomePage();
    }

    public void StartupFileServer(IAppBuilder appBuilder)
    {
      var options = new FileServerOptions
      {
        EnableDirectoryBrowsing = true,
        FileSystem = new PhysicalFileSystem("\\")
      };      
      appBuilder.UseFileServer(options);
    }

    #endregion

    #region IPluginStateStracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      ServiceRegistration.Set<IOwinServer>(new OwinServer());
      
      // ToDo: Remove TestCode
      ServiceRegistration.Get<IOwinServer>().TryStartWebAppAsync(StartupWelcomePage, 80, "");
      ServiceRegistration.Get<IOwinServer>().TryStartWebAppAsync(StartupFileServer, 81, "/fileserver");
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
    }

    public void Continue()
    {
    }

    public void Shutdown()
    {
      ServiceRegistration.Get<IOwinServer>().ShutdownAsync();
    }

    #endregion

  }
}
