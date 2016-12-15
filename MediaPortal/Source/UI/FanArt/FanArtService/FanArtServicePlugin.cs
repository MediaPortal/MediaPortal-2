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

using HttpServer.HttpModules;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;

namespace MediaPortal.Extensions.UserServices.FanArtService
{
  public class FanArtServicePlugin: IPluginStateTracker
  {
    private HttpModule _fanartModule;
    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      IResourceServer server = ServiceRegistration.Get<IResourceServer>();
      if (server != null)
      {
        ServiceRegistration.Set<IFanArtService>(new FanArtService());
        _fanartModule = new FanartAccessModule();
        server.AddHttpModule(_fanartModule);
      }
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
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
