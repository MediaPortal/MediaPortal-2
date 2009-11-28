#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.Core.General;

namespace MediaPortal.UI.ServerCommunication
{
  /// <summary>
  /// Manages the UPnP connection to the backend home server.
  /// </summary>
  public interface IServerConnectionManager
  {
    /// <summary>
    /// Gets a collection of available MediaPortal servers, if this MediaPortal client is not attached to a home server.
    /// Else, this property will be <c>null</c>.
    /// </summary>
    ICollection<ServerDescriptor> AvailableServers { get; }

    /// <summary>
    /// Returns the information if this MediaPortal client is currently connected to its home server.
    /// </summary>
    bool IsHomeServerConnected { get; }

    /// <summary>
    /// If this MediaPortal client is attached to a home server, this property gets the System ID of that home server.
    /// Else, this property will be <c>null</c>.
    /// </summary>
    string HomeServerSystemId { get; }

    /// <summary>
    /// Gets the display name of the last connected home server, if this MediaPortal client is attached to a home server.
    /// Else, this property will be <c>null</c>.
    /// </summary>
    string LastHomeServerName { get; }

    /// <summary>
    /// Gets the computer name of the last connecdted home server, if this MediaPortal client is attached to a home server.
    /// Else, this property will be <c>null</c>.
    /// </summary>
    SystemName LastHomeServerSystem { get; }

    /// <summary>
    /// Gets a proxy object to communicate with the MediaPortal server's content directory service.
    /// If the home server is not connected, this value is <c>null</c>.
    /// </summary>
    IContentDirectory ContentDirectory { get; }

    /// <summary>
    /// Gets a proxy object to communicate with the MediaPortal server's server controller service.
    /// If the home server is not connected, this value is <c>null</c>.
    /// </summary>
    IServerController ServerController { get; }

    /// <summary>
    /// Starts the UPnP subsystem and tries to connect to the home server, if set.
    /// </summary>
    void Startup();

    /// <summary>
    /// Shuts the UPnP subsystem down.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Detaches this MediaPortal client from its current home server. All local shares will be removed from the former
    /// home server.
    /// </summary>
    void DetachFromHomeServer();

    /// <summary>
    /// Sets the server with the given <paramref name="backendServerSystemId"/> as new home server and tries to connect to it.
    /// </summary>
    /// <param name="backendServerSystemId">System Id of the MediaPortal server to set as home server.</param>
    void SetNewHomeServer(string backendServerSystemId);
  }
}
