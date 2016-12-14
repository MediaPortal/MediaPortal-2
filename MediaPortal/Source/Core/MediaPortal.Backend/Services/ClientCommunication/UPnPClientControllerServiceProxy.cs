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

using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Common.UPnP;
using MediaPortal.Utilities;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Backend.Services.ClientCommunication
{
  /// <summary>
  /// Encapsulates the MediaPortal 2 UPnP server's proxy for the ClientController service.
  /// </summary>
  public class UPnPClientControllerServiceProxy : UPnPServiceProxyBase, IClientController
  {
    public UPnPClientControllerServiceProxy(CpService serviceStub) : base(serviceStub, "ClientController") { }

    public string GetHomeServerSystemId()
    {
      CpAction action = GetAction("GetHomeServerSystemId");
      IList<object> outParameters = action.InvokeAction(null);
      return (string) outParameters[0];
    }

    public void ImportLocation(ResourcePath path, IEnumerable<string> mediaCategories, ImportJobType importJobType)
    {
      CpAction action = GetAction("ImportLocation");
      string importJobTypeStr;
      switch (importJobType)
      {
        case ImportJobType.Import:
          importJobTypeStr = "Import";
          break;
        case ImportJobType.Refresh:
          importJobTypeStr = "Refresh";
          break;
        default:
          throw new NotImplementedException(string.Format("Import job type '{0}' is not implemented", importJobType));
      }
      IList<object> inParameters = new List<object>
          {
            path.Serialize(),
            StringUtils.Join(", ", mediaCategories),
            importJobTypeStr
          };
      action.InvokeAction(inParameters);
    }

    // TODO: State variables, if present
  }
}
