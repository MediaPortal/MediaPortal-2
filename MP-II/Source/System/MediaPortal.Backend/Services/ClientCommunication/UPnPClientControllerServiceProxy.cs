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

using System;
using System.Collections.Generic;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Backend.Services.ClientCommunication
{
  /// <summary>
  /// Encapsulates the MediaPortal-II UPnP server's proxy for the ClientController service.
  /// </summary>
  public class UPnPClientControllerServiceProxy : IClientController
  {
    protected CpService _serviceStub;

    public UPnPClientControllerServiceProxy(CpService serviceStub)
    {
      _serviceStub = serviceStub;
      _serviceStub.SubscribeStateVariables();
    }

    protected CpAction GetAction(string actionName)
    {
      CpAction result;
      if (!_serviceStub.Actions.TryGetValue(actionName, out result))
        throw new FatalException("Method '{0}' is not present in the connected MP-II ClientController", actionName);
      return result;
    }

    public string GetHomeServer()
    {
      CpAction action = GetAction("GetHomeServer");
      IList<object> outParameters = action.InvokeAction(null);
      return (string) outParameters[0];
    }

    public void ImportLocation(ResourcePath path, IEnumerable<string> mediaCategories, ImportMode importMode)
    {
      CpAction action = GetAction("ImportLocation");
      string importModeStr;
      switch (importMode)
      {
        case ImportMode.Import:
          importModeStr = "Import";
          break;
        case ImportMode.Refresh:
          importModeStr = "Refresh";
          break;
        default:
          throw new NotImplementedException(string.Format("ImportMode '{0}' is not implemented", importMode));
      }
      IList<object> inParameters = new List<object>
          {
            path,
            StringUtils.Join(",", mediaCategories),
            importModeStr
          };
      action.InvokeAction(inParameters);
    }

    // TODO: State variables, if present
  }
}
