using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Plugins.ServerStateService.Interfaces;
using MediaPortal.Plugins.ServerStateService.Interfaces.UPnP;
using System;
using System.Collections.Generic;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Plugins.ServerStateService.UPnP
{
  public class ServerStateServiceImpl : DvService, IServerStateService
  {
    protected readonly object _syncObj = new object();
    protected DvStateVariable PendingServerStates;
    protected uint _pendingStatesChangeCt = 0;
    protected ServerStateCache _stateCache;

    public ServerStateServiceImpl()
      : base(Consts.SERVICE_TYPE, Consts.SERVICE_TYPE_VERSION, Consts.SERVICE_ID)
    {
      _stateCache = new ServerStateCache();

      // Used to transport a cache key
      // ReSharper disable once InconsistentNaming - Following UPnP 1.0 standards variable naming convention.
      DvStateVariable A_ARG_TYPE_CacheKey = new DvStateVariable("A_ARG_TYPE_CacheKey", new DvStandardDataType(UPnPStandardDataType.Ui4))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_CacheKey);

      // Used to transport an enumeration of server states
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_ServerStates = new DvStateVariable("A_ARG_TYPE_ServerStates", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_ServerStates);

      //Evented state to signal when states have been changed
      PendingServerStates = new DvStateVariable(Consts.STATE_PENDING_SERVER_STATES, new DvStandardDataType(UPnPStandardDataType.Ui4))
      {
        SendEvents = true,
        Value = (uint)0
      };
      AddStateVariable(PendingServerStates);

      //Gets a list of all states that have changed since the cache key was issued
      DvAction getStatesAction = new DvAction(Consts.ACTION_GET_STATES, OnGetStates,
                             new DvArgument[]
                             {
                               new DvArgument("CacheKey", A_ARG_TYPE_CacheKey, ArgumentDirection.In)
                             },
                             new DvArgument[]
                             {
                               new DvArgument("Result", A_ARG_TYPE_ServerStates, ArgumentDirection.Out, true),
                               new DvArgument("CacheKey", A_ARG_TYPE_CacheKey, ArgumentDirection.Out)
                             });
      AddAction(getStatesAction);
    }

    private UPnPError OnGetStates(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      uint cacheKey = (uint)inParams[0];
      List<ServerState> states;
      lock (_syncObj)
        states = _stateCache.GetStates(ref cacheKey);
      outParams = new List<object> { ServerStateSerializer.Serialize(states), cacheKey };
      return null;
    }

    public void UpdateState(Guid stateId, object state)
    {
      ServerState serverState = new ServerState(stateId, state);
      lock (_syncObj)
        _stateCache.UpdateState(serverState);
      PendingServerStates.Value = ++_pendingStatesChangeCt;
    }
  }
}