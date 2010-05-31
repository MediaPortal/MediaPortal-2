#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.SystemResolver;
using MediaPortal.Core.UPnP;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Backend.Services.ClientCommunication
{
  /// <summary>
  /// Provides the UPnP service implementation for the MediaPortal 2 server controller interface.
  /// </summary>
  public class UPnPServerControllerServiceImpl : DvService
  {
    public UPnPServerControllerServiceImpl() : base(
        UPnPTypesAndIds.SERVER_CONTROLLER_SERVICE_TYPE, UPnPTypesAndIds.SERVER_CONTROLLER_SERVICE_TYPE_VERSION,
        UPnPTypesAndIds.SERVER_CONTROLLER_SERVICE_ID)
    {
      // Used for a system ID string
      DvStateVariable A_ARG_TYPE_SystemId = new DvStateVariable("A_ARG_TYPE_SystemId", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_SystemId);

      // Used for a system ID string
      DvStateVariable A_ARG_TYPE_Bool = new DvStateVariable("A_ARG_TYPE_Bool", new DvStandardDataType(UPnPStandardDataType.Boolean))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_Bool);

      // Used to transport a system name - contains the hostname string
      DvStateVariable A_ARG_TYPE_SystemName = new DvStateVariable("A_ARG_TYPE_SystemName", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_SystemName);

      // More state variables go here

      DvAction isClientAttachedAction = new DvAction("IsClientAttached", OnIsClientAttached,
          new DvArgument[] {
            new DvArgument("ClientSystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("IsAttached", A_ARG_TYPE_Bool, ArgumentDirection.Out),
          });
      AddAction(isClientAttachedAction);

      DvAction attachClientAction = new DvAction("AttachClient", OnAttachClient,
          new DvArgument[] {
            new DvArgument("ClientSystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(attachClientAction);

      DvAction detachClientAction = new DvAction("DetachClient", OnDetachClient,
          new DvArgument[] {
            new DvArgument("ClientSystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(detachClientAction);

      DvAction getSystemNameForSytemIdAction = new DvAction("GetSystemNameForSystemId", OnGetSystemNameForSytemId,
          new DvArgument[] {
            new DvArgument("SystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("SystemName", A_ARG_TYPE_SystemName, ArgumentDirection.Out),
          });
      AddAction(getSystemNameForSytemIdAction);

      // More actions go here
    }

    static UPnPError OnIsClientAttached(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string clientSystemId = (string) inParams[0];
      bool isAttached = ServiceScope.Get<IClientManager>().AttachedClientsSystemIds.Contains(clientSystemId);
      outParams = new List<object> {isAttached};
      return null;
    }

    static UPnPError OnAttachClient(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string clientSystemId = (string) inParams[0];
      ServiceScope.Get<IClientManager>().AttachClient(clientSystemId);
      outParams = null;
      return null;
    }

    static UPnPError OnDetachClient(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string clientSystemId = (string) inParams[0];
      ServiceScope.Get<IClientManager>().DetachClientAndRemoveShares(clientSystemId);
      outParams = null;
      return null;
    }

    static UPnPError OnGetSystemNameForSytemId(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string systemId = (string) inParams[0];
      SystemName result = ServiceScope.Get<ISystemResolver>().GetSystemNameForSystemId(systemId);
      outParams = new List<object> {result == null ? null : result.HostName};
      return null;
    }
  }
}
