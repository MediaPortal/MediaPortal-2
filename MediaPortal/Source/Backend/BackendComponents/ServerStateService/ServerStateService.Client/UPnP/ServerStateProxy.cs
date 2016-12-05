using MediaPortal.Common;
using MediaPortal.Common.UPnP;
using MediaPortal.Plugins.ServerStateService.Interfaces;
using MediaPortal.Plugins.ServerStateService.Interfaces.UPnP;
using MediaPortal.UI.ServerCommunication;
using System;
using System.Collections.Generic;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Plugins.ServerStateService.Client.UPnP
{
  public class ServerStateProxy : UPnPServiceProxyBase
  {
    protected readonly object _syncObj = new object();
    protected uint _currentCacheKey = 0;
    protected Dictionary<Guid, object> _currentStates = new Dictionary<Guid, object>();

    public ServerStateProxy(CpService serviceStub) : base(serviceStub, Consts.SERVICE_NAME)
    {
      serviceStub.StateVariableChanged += OnStateVariableChanged;
      serviceStub.SubscribeStateVariables();
    }

    public IDictionary<Guid, object> GetAllStates()
    {
      lock (_syncObj)
        return new Dictionary<Guid, object>(_currentStates);
    }

    public bool TryGetState<T>(Guid stateId, out T state)
    {
      object stateObject;
      lock (_syncObj)
        if (_currentStates.TryGetValue(stateId, out stateObject) && stateObject is T)
        {
          state = (T)stateObject;
          return true;
        }
      state = default(T);
      return false;
    }

    private void OnStateVariableChanged(CpStateVariable stateVariable, object newValue)
    {
      if (stateVariable.Name == Consts.STATE_PENDING_SERVER_STATES)
        GetStates();
    }

    protected void GetStates()
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_STATES);
        IList<object> inParameters = new List<object> { _currentCacheKey };
        IList<object> outParameters = action.InvokeAction(inParameters);
        var states = ServerStateSerializer.Deserialize<List<ServerState>>((string)outParameters[0]);
        if (states == null || states.Count == 0)
          return;

        var updatedStates = new Dictionary<Guid, object>();
        lock (_syncObj)
        {
          _currentCacheKey = (uint)outParameters[1];
          foreach (ServerState state in states)
          {
            object stateObject = state.DeserializeState();
            _currentStates[state.Id] = stateObject;
            updatedStates[state.Id] = stateObject;
          }
        }
        ServerStateMessaging.SendStatesChangedMessage(updatedStates);
      }
      catch (Exception ex)
      {
        throw;
      }
    }
  }
}