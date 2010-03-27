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

using MediaPortal.Core.General;
using UPnP.Infrastructure.CP;

namespace MediaPortal.Backend.ClientCommunication
{
  public class ClientDescriptor
  {
    protected RootDescriptor _rootDescriptor;
    protected string _clientName;
    protected SystemName _system;
    protected string _mpFrontendServerUUID;

    public ClientDescriptor(RootDescriptor rootDescriptor, string mpFrontendServerUUID, string clientName, SystemName system)
    {
      _rootDescriptor = rootDescriptor;
      _clientName = clientName;
      _system = system;
      _mpFrontendServerUUID = mpFrontendServerUUID;
    }

    public RootDescriptor UPnPRootDescriptor
    {
      get { return _rootDescriptor; }
    }

    public string ClientName
    {
      get { return _clientName; }
    }

    public SystemName System
    {
      get { return _system; }
    }

    /// <summary>
    /// Device ID of the client's UPnP device. This ID is referred to as "system ID".
    /// </summary>
    public string MPFrontendServerUUID
    {
      get { return _mpFrontendServerUUID; }
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
      return _mpFrontendServerUUID.GetHashCode();
    }

    public override string ToString()
    {
      return string.Format("MP frontend server '{0}' at system '{1}'", _mpFrontendServerUUID, _system.HostName);
    }
  }
}
