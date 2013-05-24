#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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

#endregion Copyright (C) 2007-2013 Team MediaPortal

#region Imports

using System;
using System.Net;

#endregion Imports

namespace MediaPortal.Extensions.GeoLocation.IPLookup.Data
{
  internal class TraceRouteResponse
  {
    #region Private variables

    private string _firstResponseHostname;
    private string _firstResponseIP;
    private int _firstResponseTtl;
    private IPAddress _remoteHost;

    #endregion Private variables

    #region Public properties

    public String FirstResponseHostname
    {
      get { return _firstResponseHostname; }
      set { _firstResponseHostname = value; }
    }

    public String FirstResponseIP
    {
      get { return _firstResponseIP; }
      set { _firstResponseIP = value; }
    }

    public int FirstResponseTtl
    {
      get { return _firstResponseTtl; }
      set { _firstResponseTtl = value; }
    }

    public IPAddress RemoteHost
    {
      get { return _remoteHost; }
      set { _remoteHost = value; }
    }

    #endregion Public properties
  }
}