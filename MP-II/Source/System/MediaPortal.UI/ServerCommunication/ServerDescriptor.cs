#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

using MediaPortal.Core.General;
using UPnP.Infrastructure.CP;

namespace MediaPortal.ServerCommunication
{
  public class ServerDescriptor
  {
    protected RootDescriptor _rootDescriptor;
    protected string _serverName;
    protected SystemName _system;
    protected string _mpMediaServerUUID;

    public ServerDescriptor(RootDescriptor rootDescriptor, string mpMediaServerUUID, string serverName, SystemName system)
    {
      _rootDescriptor = rootDescriptor;
      _serverName = serverName;
      _system = system;
      _mpMediaServerUUID = mpMediaServerUUID;
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

    public string MPMediaServerUUID
    {
      get { return _mpMediaServerUUID; }
    }

    public static bool operator == (ServerDescriptor a, ServerDescriptor b)
    {
      if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
        return true;
      if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
        return false;
      return a.MPMediaServerUUID == b.MPMediaServerUUID;
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
      return _mpMediaServerUUID.GetHashCode();
    }

    public override string ToString()
    {
      return string.Format("MP media server '{0}' at system '{1}'", _mpMediaServerUUID, _system.HostName);
    }
  }
}
