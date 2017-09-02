#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using System.Linq;
using MediaPortal.Backend.BackendServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Common.UPnP;
using UPnP.Infrastructure.Dv.DeviceTree;
using MediaPortal.Plugins.SlimTv.Service.UPnP;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public class SlimTvProxyPlugin : IPluginStateTracker
  {
    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      DvDevice device = ServiceRegistration.Get<IBackendServer>().UPnPBackendServer.FindDevicesByDeviceTypeAndVersion(UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION, true).FirstOrDefault();
      if (device != null)
      {
        var slimTvService = new SlimTvService();
        slimTvService.Init();
        ServiceRegistration.Set<ITvProvider>(slimTvService);
        Logger.Debug("SlimTvProxy: Registered SlimTvService.");
        device.AddService(new SlimTvServiceImpl());
        Logger.Debug("SlimTvProxy: Adding SlimTvService to MP2 backend root device");
      }
      else
      {
        Logger.Error("SlimTvProxy: MP2 backend root device not found!");
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
      ITvProvider tvProvider = ServiceRegistration.Get<ITvProvider>(false);
      if (tvProvider != null)
        tvProvider.DeInit();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
