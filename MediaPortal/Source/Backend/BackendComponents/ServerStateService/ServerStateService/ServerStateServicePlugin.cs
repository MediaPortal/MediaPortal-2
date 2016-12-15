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

using MediaPortal.Backend.BackendServer;
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.UPnP;
using MediaPortal.Plugins.ServerStateService.UPnP;
using System.Linq;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Plugins.ServerStateService
{
  public class ServerStateServicePlugin : IPluginStateTracker
  {
    public void Activated(PluginRuntime pluginRuntime)
    {
      var stateService = new ServerStateServiceImpl();
      ServiceRegistration.Set<IServerStateService>(stateService);

      DvDevice device = ServiceRegistration.Get<IBackendServer>().UPnPBackendServer
        .FindDevicesByDeviceTypeAndVersion(UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION, true).FirstOrDefault();
      if (device != null)
      {
        Logger.Debug("ServerStateService: Registering ServerStateService service.");
        device.AddService(stateService);
        Logger.Debug("ServerStateService: Adding ServerStateService service to MP2 backend root device");
      }
      else
      {
        Logger.Error("ServerStateService: MP2 backend root device not found!");
      }
    }    

    public void Continue()
    {

    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Shutdown()
    {

    }

    public void Stop()
    {

    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}