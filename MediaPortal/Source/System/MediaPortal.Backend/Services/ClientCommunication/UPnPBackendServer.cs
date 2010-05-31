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

using UPnP.Infrastructure.Dv;

namespace MediaPortal.Backend.Services.ClientCommunication
{
  /// <summary>
  /// Encapsulates the MediaPortal 2 UPnP backend server device.
  /// </summary>
  public class UPnPBackendServer : UPnPServer
  {
    public const int SSDP_ADVERTISMENT_INTERVAL = 1800;

    public UPnPBackendServer(string backendServerSystemId)
    {
      AddRootDevice(new MP2BackendServerDevice(backendServerSystemId));
      // TODO: add UPnP standard MediaServer device: it's not implemented yet
      //AddRootDevice(new UPnPMediaServerDevice(...));
    }

    public void Start()
    {
      Bind(SSDP_ADVERTISMENT_INTERVAL);
    }

    public void Stop()
    {
      Close();
    }
  }
}
