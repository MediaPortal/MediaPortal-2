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

namespace MediaPortal.UI.ServerCommunication
{
  public class ServerDescriptor
  {
    protected RootDescriptor _rootDescriptor;
    protected string _serverName;
    protected SystemName _system;
    protected string _mpBackendServerUUID;

    public ServerDescriptor(RootDescriptor rootDescriptor, string mpBackendServerUUID, string serverName, SystemName system)
    {
      _rootDescriptor = rootDescriptor;
      _serverName = serverName;
      _system = system;
      _mpBackendServerUUID = mpBackendServerUUID;
    }

    public RootDescriptor UPnPRootDescriptor
    {
      get { return _rootDescriptor; }
    }

    public string ServerName
    {
      get { return _serverName; }
    }

    public SystemName System
    {
      get { return _system; }
    }

    public string MPBackendServerUUID
    {
      get { return _mpBackendServerUUID; }
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
      return _mpBackendServerUUID.GetHashCode();
    }

    public override string ToString()
    {
      return string.Format("MP backend server '{0}' at system '{1}'", _mpBackendServerUUID, _system.HostName);
    }
  }
}
