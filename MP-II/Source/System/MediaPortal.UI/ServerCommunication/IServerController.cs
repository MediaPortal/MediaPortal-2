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
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.UI.ServerCommunication
{
  /// <summary>
  /// Interface of the MediaPortal-II server's ServerController service.
  /// </summary>
  public interface IServerController
  {
    bool IsClientAttached(string clientSystemId);
    void AttachClient(string clientSystemId);
    void DetachClient(string clientSystemId);
    SystemName GetSystemNameForSystemId(string systemId);
  }
}
