#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

namespace MediaPortal.Plugins.WifiRemote.Messages
{
  internal class MessageWelcome : IMessage
  {
    private string type = "welcome";
    private int server_version = 17;
    private AuthMethod authMethod = AuthMethod.UserPassword;

    /// <summary>
    /// Type of this method
    /// </summary>
    public string Type
    {
      get { return type; }
    }

    /// <summary>
    /// API version of this WifiRemote instance. 
    /// Should be increased on breaking changes.
    /// </summary>
    public int Server_Version
    {
      get { return server_version; }
    }

    /// <summary>
    /// Authentication method required of the client.
    /// </summary>
    public AuthMethod AuthMethod
    {
      get { return authMethod; }
      set { authMethod = value; }
    }

    public Dictionary<string, bool> MPExtendedServicesInstalled
    {
      get
      {
        return new Dictionary<string, bool>()
        {
          { "MAS", false },
          { "TAS", false },
          { "WSS", false }
        };
      }
    }

    public Boolean TvPluginInstalled
    {
      get { return false; }
    }

    /// <summary>
    /// Contructor.
    /// </summary>
    public MessageWelcome()
    {
    }
  }
}
