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

using MediaPortal.Common;
using MediaPortal.Common.Services.ServerCommunication;
using MediaPortal.Plugins.ServerStateService.Interfaces.UPnP;
using MediaPortal.UI.ServerCommunication;
using System;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.DeviceTree;
using System.Collections.Generic;

namespace MediaPortal.Plugins.ServerStateService.Client.UPnP
{
  public class ServerStateProxyRegistration : IServerStateManager
  {
    ServerStateProxy _proxy = null;

    public ServerStateProxyRegistration()
    {
      ServiceRegistration.Set<IServerStateManager>(this);
      RegisterService();
    }

    public IDictionary<Guid, object> GetAllStates()
    {
      var proxy = _proxy;
      if (proxy != null && proxy.ServiceStub.IsConnected)
        return proxy.GetAllStates();
      return new Dictionary<Guid, object>();
    }

    public bool TryGetState<T>(Guid stateId, out T state)
    {
      var proxy = _proxy;
      if (proxy != null && proxy.ServiceStub.IsConnected)
        proxy.TryGetState(stateId, out state);
      state = default(T);
      return false;
    }

    public void RegisterService()
    {
      UPnPClientControlPoint controlPoint = ServiceRegistration.Get<IServerConnectionManager>().ControlPoint;
      if (controlPoint == null)
        return;

      controlPoint.RegisterAdditionalService(RegisterServerStateProxy);
    }

    public void UnregisterService()
    {
      _proxy = null;
    }

    protected ServerStateProxy RegisterServerStateProxy(DeviceConnection connection)
    {
      CpService stub = connection.Device.FindServiceByServiceId(Consts.SERVICE_ID);

      if (stub == null)
        throw new NotSupportedException("ServerStateProxy not supported by this UPnP device.");

      _proxy = new ServerStateProxy(stub);
      return _proxy;
    }
  }
}
