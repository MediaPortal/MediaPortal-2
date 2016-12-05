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
