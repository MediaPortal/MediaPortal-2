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
using MediaPortal.Common.Threading;
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
        //Calling GetStates on the callback thread seems to cause a timeout/possible deadlock
        //during startup in some cases so call on a different thread.
        ServiceRegistration.Get<IThreadPool>().Add(GetStates);
    }

    protected void GetStates()
    {
      try
      {
        uint cacheKey;
        lock (_syncObj)
          cacheKey = _currentCacheKey;

        CpAction action = GetAction(Consts.ACTION_GET_STATES);
        IList<object> inParameters = new List<object> { cacheKey };
        IList<object> outParameters = action.InvokeAction(inParameters);
        var states = ServerStateSerializer.Deserialize<List<ServerState>>((string)outParameters[0]);
        if (states == null || states.Count == 0)
          return;

        var updatedStates = new Dictionary<Guid, object>();
        lock (_syncObj)
        {
          uint newCacheKey = (uint)outParameters[1];
          //Due to threading the update might be being done out of order. e.g. a different thread has already updated with newer states.
          //Check if the current key has been modified and whether our key is newer.
          if (_currentCacheKey != cacheKey && newCacheKey <= _currentCacheKey)
            return;
          _currentCacheKey = newCacheKey;
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