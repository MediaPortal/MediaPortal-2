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

using System;
using MediaPortal.Core;
using MediaPortal.Core.Settings;
using MediaPortal.Backend.ClientCommunication.Settings;
using UPnP.Infrastructure.Dv;

namespace MediaPortal.Backend.ClientCommunication
{
  /// <summary>
  /// Encapsulates the MediaPortal-II UPnP server device.
  /// </summary>
  public class UPnPMediaServer : UPnPServer
  {
    public const int SSDP_ADVERTISMENT_INTERVAL = 1800;

    public UPnPMediaServer()
    {
      ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
      MediaServerSettings settings = settingsManager.Load<MediaServerSettings>();
      Guid deviceId;
      if (settings.MediaServerDeviceId.HasValue)
        deviceId = settings.MediaServerDeviceId.Value;
      else
      {
        // Create a new id for our new mediacenter device
        deviceId = Guid.NewGuid();
        settings.MediaServerDeviceId = deviceId;
        settingsManager.Save(settings);
      }
      AddRootDevice(new MP2ServerDevice(deviceId.ToString("D")));
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
