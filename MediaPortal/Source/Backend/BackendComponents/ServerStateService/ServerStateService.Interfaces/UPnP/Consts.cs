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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.ServerStateService.Interfaces.UPnP
{
  public class Consts
  {
    public const string SERVICE_TYPE = "schemas-team-mediaportal-com:service:ServerStateService";
    public const int SERVICE_TYPE_VERSION = 1;
    public const string SERVICE_NAME = "ServerStateService";
    public const string SERVICE_ID = "urn:team-mediaportal-com:serviceId:ServerStateService";
    public const string STATE_PENDING_SERVER_STATES = "PendingServerStates";
    public const string ACTION_GET_STATES = "GetStates";
  }
}
