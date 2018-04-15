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

using UPnP.Infrastructure.Dv.DeviceTree;

namespace UPnPLightServer
{
  public class LightServerDevice : DvDevice
  {
    public const string LIGHT_SERVER_DEVICE_TYPE = "schemas-team-mediaportal-com:device:MP2-Client";
    public const int LIGHT_SERVER_DEVICE_TYPE_VERSION = 1;

    public LightServerDevice(string serverId) : base(LIGHT_SERVER_DEVICE_TYPE, LIGHT_SERVER_DEVICE_TYPE_VERSION, serverId, new LightServerDeviceInformation())
    {
      // TODO: Add light server service
    }
  }
}