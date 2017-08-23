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

namespace MediaPortal.Backend.ClientCommunication
{
  public class ClientDescriptor
  {
    protected readonly DeviceDescriptor _clientDeviceDescriptor;

    public ClientDescriptor(DeviceDescriptor clientDeviceDescriptor)
    {
      _clientDeviceDescriptor = clientDeviceDescriptor;
    }

    public static ClientDescriptor GetMPFrontendServerDescriptor(RootDescriptor uPnPRootDescriptor)
    {
      DeviceDescriptor rootDescriptor = DeviceDescriptor.CreateRootDeviceDescriptor(uPnPRootDescriptor);
      if (rootDescriptor == null)
        return null;
      DeviceDescriptor serverDeviceDescriptor = rootDescriptor.FindFirstDevice(
          UPnPTypesAndIds.FRONTEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.FRONTEND_SERVER_DEVICE_TYPE_VERSION);
      return serverDeviceDescriptor == null ? null : new ClientDescriptor(serverDeviceDescriptor);
    }

    public DeviceDescriptor ClientDeviceDescriptor
    {
      get { return _clientDeviceDescriptor; }
    }

    public string ClientName
    {
      get { return _clientDeviceDescriptor.FriendlyName; }
    }

    public SystemName System
    {
      get { return new SystemName(new Uri(_clientDeviceDescriptor.RootDescriptor.SSDPRootEntry.PreferredLink.DescriptionLocation).Host); }
    }

    /// <summary>
    /// Device ID of the client's UPnP device. This ID is referred to as "system ID".
    /// </summary>
    public string MPFrontendServerUUID
    {
      get { return _clientDeviceDescriptor.DeviceUUID; }
    }

    public static bool operator ==(ClientDescriptor a, ClientDescriptor b)
    {
      bool bnull = ReferenceEquals(b, null);
      if (ReferenceEquals(a, null))
        return bnull;
      if (bnull)
        return false;
      return a.MPFrontendServerUUID == b.MPFrontendServerUUID;
    }

    public static bool operator !=(ClientDescriptor a, ClientDescriptor b)
    {
      return !(a == b);
    }

    public override bool Equals(object obj)
    {
      ClientDescriptor other = obj as ClientDescriptor;
      if (obj == null)
        return false;
      return other == this;
    }

    public override int GetHashCode()
    {
      return MPFrontendServerUUID.GetHashCode();
    }

    public override string ToString()
    {
      SystemName systemName = System;
      return string.Format("MP frontend server '{0}' at system '{1}' (IP address: '{2}')", MPFrontendServerUUID, systemName.HostName, systemName.Address);
    }
  }
}
