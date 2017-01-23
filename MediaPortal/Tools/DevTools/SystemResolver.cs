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
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.SystemResolver;
using MediaPortal.Common.SystemResolver;

namespace MediaPortal.DevTools
{
  public class SystemResolver : SystemResolverBase
  {
    public SystemResolver()
    {
      ServiceRegistration.Get<ILogger>().Info("SystemResolver: Local system id is '{0}'", _localSystemId);
    }

    #region ISystemResolver implementation

    public override SystemName GetSystemNameForSystemId(string systemId)
    {
      if (systemId == _localSystemId)
        // Shortcut for local system
        return SystemName.GetLocalSystemName();
      var serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
      if (systemId == serverConnectionManager.HomeServerSystemId)
      {
        // Shortcut for home server
        var result = serverConnectionManager.LastHomeServerSystem;
        if (result != null)
          return result;
      }
      var serverController = serverConnectionManager.ServerController;
      return serverController == null ? null : serverController.GetSystemNameForSystemId(systemId);
    }

    public override SystemType SystemType
    {
      get { return SystemType.Client; }
    }

    #endregion
  }
}