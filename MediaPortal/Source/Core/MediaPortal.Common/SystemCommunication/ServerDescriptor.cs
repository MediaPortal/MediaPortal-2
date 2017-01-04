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
using MediaPortal.Common.General;
using MediaPortal.Common.UPnP;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.Description;

namespace MediaPortal.Common.SystemCommunication
{
  public class ServerDescriptor
  {
    protected readonly DeviceDescriptor _serverDeviceDescriptor;

    internal ServerDescriptor(DeviceDescriptor serverDeviceDescriptor)
    {
      _serverDeviceDescriptor = serverDeviceDescriptor;
    }

    public static ServerDescriptor GetMPBackendServerDescriptor(RootDescriptor uPnPRootDescriptor)
    {
      DeviceDescriptor rootDescriptor = DeviceDescriptor.CreateRootDeviceDescriptor(uPnPRootDescriptor);
      if (rootDescriptor == null)
        return null;
      DeviceDescriptor serverDeviceDescriptor = rootDescriptor.FindFirstDevice(
          UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION);
      return serverDeviceDescriptor == null ? null : new ServerDescriptor(serverDeviceDescriptor);
    }

    public DeviceDescriptor ServerDeviceDescriptor
    {
      get { return _serverDeviceDescriptor; }
    }

    public string ServerName
    {
      get { return _serverDeviceDescriptor.FriendlyName; }
    }

    public string MPBackendServerUUID
    {
      get { return _serverDeviceDescriptor.DeviceUUID; }
    }

    public SystemName GetPreferredLink()
    {
      return new SystemName(new Uri(_serverDeviceDescriptor.RootDescriptor.SSDPRootEntry.PreferredLink.DescriptionLocation).Host);
    }

    public static bool operator == (ServerDescriptor a, ServerDescriptor b)
    {
      bool bnull = ReferenceEquals(b, null);
      if (ReferenceEquals(a, null))
        return bnull;
      if (bnull)
        return false;
      return a.MPBackendServerUUID == b.MPBackendServerUUID;
    }

    public static bool operator != (ServerDescriptor a, ServerDescriptor b)
    {
      return !(a == b);
    }

    public override bool Equals(object obj)
    {
      ServerDescriptor other = obj as ServerDescriptor;
      if (obj == null)
        return false;
      return other == this;
    }

    public override int GetHashCode()
    {
      return MPBackendServerUUID.GetHashCode();
    }

    public override string ToString()
    {
      SystemName preferredLink = GetPreferredLink();
      return string.Format("MP backend server '{0}' at host '{1}' (IP address: '{2}')", MPBackendServerUUID, preferredLink.HostName, preferredLink.Address);
    }
  }
}
