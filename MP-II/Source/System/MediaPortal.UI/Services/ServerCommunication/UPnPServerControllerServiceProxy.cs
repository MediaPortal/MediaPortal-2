#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Core.General;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.UI.Services.ServerCommunication
{
  /// <summary>
  /// Encapsulates the MediaPortal-II UPnP client's proxy for the ServerController service.
  /// </summary>
  public class UPnPServerControllerServiceProxy : IServerController
  {
    protected CpService _serviceStub;

    public UPnPServerControllerServiceProxy(CpService serviceStub)
    {
      _serviceStub = serviceStub;
      _serviceStub.SubscribeStateVariables();
    }

    protected CpAction GetAction(string actionName)
    {
      CpAction result;
      if (!_serviceStub.Actions.TryGetValue(actionName, out result))
        throw new FatalException("Method '{0}' is not present in the connected MP-II ServerController", actionName);
      return result;
    }

    public bool IsClientAttached(string clientSystemId)
    {
      CpAction action = GetAction("IsClientAttached");
      IList<object> outParams = action.InvokeAction(new List<object> {clientSystemId});
      return (bool) outParams[0];
    }

    public void AttachClient(string clientSystemId)
    {
      CpAction action = GetAction("AttachClient");
      action.InvokeAction(new List<object> {clientSystemId});
    }

    public void DetachClient(string clientSystemId)
    {
      CpAction action = GetAction("DetachClient");
      action.InvokeAction(new List<object> {clientSystemId});
    }

    public SystemName GetSystemNameForSystemId(string systemId)
    {
      CpAction action = GetAction("GetSystemNameForSytemId");
      IList<object> outParams = action.InvokeAction(new List<object> {systemId});
      string hostName = (string) outParams[0];
      if (string.IsNullOrEmpty(hostName))
        return null;
      return new SystemName(hostName);
    }

    // TODO: State variables, if present
  }
}
