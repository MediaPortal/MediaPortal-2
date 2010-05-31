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
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.UPnP;
using MediaPortal.UI.ServerCommunication;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.UI.Services.ServerCommunication
{
  /// <summary>
  /// Provides the UPnP service implementation for the MediaPortal 2 client controller interface.
  /// </summary>
  public class UPnPClientControllerServiceImpl : DvService
  {
    public UPnPClientControllerServiceImpl() : base(
        UPnPTypesAndIds.CLIENT_CONTROLLER_SERVICE_TYPE, UPnPTypesAndIds.CLIENT_CONTROLLER_SERVICE_TYPE_VERSION,
        UPnPTypesAndIds.CLIENT_CONTROLLER_SERVICE_ID)
    {
      // Used for a system ID string
      DvStateVariable A_ARG_TYPE_SystemId = new DvStateVariable("A_ARG_TYPE_SystemId", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_SystemId);

      // Used to transport a resource path expression
      DvStateVariable A_ARG_TYPE_ResourcePath = new DvStateVariable("A_ARG_TYPE_ResourcePath", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_ResourcePath);

      // CSV of media category strings
      DvStateVariable A_ARG_TYPE_MediaCategoryEnumeration = new DvStateVariable("A_ARG_TYPE_MediaCategoryEnumeration", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_MediaCategoryEnumeration);

      // Used to transport the import modes "Import" and "Refresh" for the ImportLocation action
      DvStateVariable A_ARG_TYPE_ImportMode = new DvStateVariable("A_ARG_TYPE_ImportMode", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_ImportMode);

      // More state variables go here

      DvAction getHomeServerAction = new DvAction("GetHomeServer", OnGetHomeServer,
          new DvArgument[] {
          },
          new DvArgument[] {
            new DvArgument("HomeServerSystemId", A_ARG_TYPE_SystemId, ArgumentDirection.Out),
          });
      AddAction(getHomeServerAction);

      DvAction importLocationAction = new DvAction("ImportLocation", OnImportLocation,
          new DvArgument[] {
            new DvArgument("Path", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
            new DvArgument("MediaCategories", A_ARG_TYPE_MediaCategoryEnumeration, ArgumentDirection.In),
            new DvArgument("ImportMode", A_ARG_TYPE_ImportMode, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(importLocationAction);

      // More actions go here
    }

    static UPnPError OnGetHomeServer(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      outParams = new List<object> {ServiceScope.Get<IServerConnectionManager>().HomeServerSystemId};
      return null;
    }

    static UPnPError OnImportLocation(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      outParams = null;
      ResourcePath path = ResourcePath.Deserialize((string) inParams[0]);
      string[] mediaCategories = ((string) inParams[1]).Split(',');
      string importModeStr = (string) inParams[2];
      bool refresh;
      switch (importModeStr)
      {
        case "Import":
          refresh = false;
          break;
        case "Refresh":
          refresh = true;
          break;
        default:
          return new UPnPError(600, "Argument 'ImportMode' must be of value 'Import' or 'Refresh'");
      }
      if (refresh)
        ServiceScope.Get<IImporterWorker>().ScheduleRefresh(path, mediaCategories, true);
      else
        ServiceScope.Get<IImporterWorker>().ScheduleImport(path, mediaCategories, true);
      return null;
    }
  }
}
