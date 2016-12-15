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
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.ServerSettings.UPnP;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Plugins.ServerSettings
{
  public class ServerSettingsImpl : DvService, IServerSettings
  {
    public ServerSettingsImpl()
      : base(Consts.SERVERSETTINGS_SERVICE_TYPE, Consts.SERVERSETTINGS_SERVICE_TYPE_VERSION, Consts.SERVERSETTINGS_SERVICE_ID)
    {
      DvStateVariable A_ARG_TYPE_SettingsType = new DvStateVariable("A_ARG_TYPE_SettingsType", new DvStandardDataType(UPnPStandardDataType.String)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_SettingsType);
      DvStateVariable A_ARG_TYPE_SettingsValue = new DvStateVariable("A_ARG_TYPE_SettingsValue", new DvStandardDataType(UPnPStandardDataType.String)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_SettingsValue);

      DvAction loadAction = new DvAction(Consts.ACTION_LOAD, OnLoad,
                             new DvArgument[] { new DvArgument("SettingsTypeName", A_ARG_TYPE_SettingsType, ArgumentDirection.In) },
                             new DvArgument[] { new DvArgument("Result", A_ARG_TYPE_SettingsValue, ArgumentDirection.Out, true) });

      AddAction(loadAction);
      DvAction saveAction = new DvAction(Consts.ACTION_SAVE, OnSave,
                             new DvArgument[]
                               {
                                 new DvArgument("SettingsTypeName", A_ARG_TYPE_SettingsType, ArgumentDirection.In),
                                 new DvArgument("Settings", A_ARG_TYPE_SettingsValue, ArgumentDirection.In)
                               },
                             new DvArgument[] { });

      AddAction(saveAction);
    }

    private UPnPError OnLoad(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      string settingsTypeName = (string) inParams[0];
      object result = Load(settingsTypeName);
      string serialized = SettingsSerializer.Serialize(result);
      outParams = new List<object> { serialized };
      return null;
    }

    private UPnPError OnSave(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      string settingsTypeName = (string) inParams[0];
      string settings = (string) inParams[1];
      Save(settingsTypeName, settings);
      outParams = new List<object> { };
      return null;
    }

    public object Load(string settingsTypeName)
    {
      Type settingsType = SettingsSerializer.GetSettingsType(settingsTypeName);
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      return settingsManager.Load(settingsType);
    }

    public void Save(string settingsTypeName, string settings)
    {
      object settingsObject = SettingsSerializer.Deserialize(settingsTypeName, settings);
      if (settingsObject == null)
        return;
      
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      settingsManager.Save(settingsObject);
    }
  }
}
