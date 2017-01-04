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

using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Common.UPnP
{
  public abstract class UPnPServiceProxyBase
  {
    protected CpService _serviceStub;
    protected string _serviceName;

    protected UPnPServiceProxyBase(CpService serviceStub, string serviceName)
    {
      _serviceName = serviceName;
      _serviceStub = serviceStub;
      _serviceStub.SubscribeStateVariables();
    }

    public CpService ServiceStub
    {
      get { return _serviceStub; }
    }

    protected CpAction GetAction(string actionName)
    {
      CpAction result;
      if (!_serviceStub.Actions.TryGetValue(actionName, out result))
        throw new FatalException("Method '{0}' is not present in the connected {1} service", actionName, _serviceName);
      return result;
    }

    protected CpStateVariable GetStateVariable(string stateVariableName)
    {
      CpStateVariable result;
      if (!_serviceStub.StateVariables.TryGetValue(stateVariableName, out result))
        throw new FatalException("State variable '{0}' is not present in the connected {1} service", stateVariableName, _serviceName);
      return result;
    }

    public override string ToString()
    {
      return string.Format("UPnP service proxy of type '{0}', version '{1}'", _serviceStub.ServiceType, _serviceStub.ServiceTypeVersion);
    }
  }
}