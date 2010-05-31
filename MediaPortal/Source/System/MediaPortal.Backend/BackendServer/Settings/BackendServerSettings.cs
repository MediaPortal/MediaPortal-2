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

using MediaPortal.Core.Settings;

namespace MediaPortal.Backend.BackendServer.Settings
{
  public class BackendServerSettings
  {
    protected string _friendlyName = null;
    protected int _httpServerPort = 80;
    protected bool _useIPv4 = true;
    protected bool _useIPv6 = true;

    [Setting(SettingScope.Global)]
    public string FriendlyName
    {
      get { return _friendlyName; }
      set { _friendlyName = value; }
    }

    [Setting(SettingScope.Global)]
    public int HttpServerPort
    {
      get { return _httpServerPort; }
      set { _httpServerPort = value; }
    }

    [Setting(SettingScope.Global)]
    public bool UseIPv4
    {
      get { return _useIPv4; }
      set { _useIPv4 = value; }
    }

    [Setting(SettingScope.Global)]
    public bool UseIPv6
    {
      get { return _useIPv6; }
      set { _useIPv6 = value; }
    }
  }
}