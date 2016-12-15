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
using MediaPortal.Common.UPnP;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Plugins.ServerSettings.UPnP
{
  public class ServerSettingsProxy : UPnPServiceProxyBase, IServerSettingsClient
  {
    public ServerSettingsProxy(CpService serviceStub)
      : base(serviceStub, Consts.SERVERSETTINGS_SERVICE_NAME)
    {
      ServiceRegistration.Set<IServerSettingsClient>(this);
    }

    public object Load(string settingsTypeName)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_LOAD);
        IList<object> inParameters = new List<object> { settingsTypeName };
        IList<object> outParameters = action.InvokeAction(inParameters);

        return SettingsSerializer.Deserialize(settingsTypeName, (string) outParameters[0]);
      }
      catch (Exception ex)
      {
        throw;
      }
    }

    public void Save(string settingsTypeName, string settings)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_SAVE);
        IList<object> inParameters = new List<object> { settingsTypeName, settings };
        action.InvokeAction(inParameters);
      }
      catch (Exception ex)
      {
        throw;
      }
    }

    public SettingsType Load<SettingsType> () where SettingsType : class
    {
      return (SettingsType) Load(typeof (SettingsType).AssemblyQualifiedName);
    }

    public void Save (object settingsObject)
    {
      Save(settingsObject.GetType().AssemblyQualifiedName, SettingsSerializer.Serialize(settingsObject));
    }
  }
}
