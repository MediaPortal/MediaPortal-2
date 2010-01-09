#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Services.SystemResolver;

namespace MediaPortal.Backend.Services.SystemResolver
{
  public class SystemResolver : SystemResolverBase
  {
    #region ISystemResolver implementation

    public override SystemName GetSystemNameForSystemId(string systemId)
    {
      if (systemId == _localSystemId)
        return SystemName.GetLocalSystemName();
      IClientManager clientManager = ServiceScope.Get<IClientManager>();
      return clientManager.GetSystemNameForSystemId(systemId);
    }

    #endregion
  }
}