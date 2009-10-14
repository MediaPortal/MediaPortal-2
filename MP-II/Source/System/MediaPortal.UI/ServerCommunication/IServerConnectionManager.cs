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

namespace MediaPortal.ServerCommunication
{
  /// <summary>
  /// Manages the UPnP connection to the backend home server.
  /// </summary>
  public interface IServerConnectionManager
  {
    /// <summary>
    /// Gets the UUID of the home server, this MediaPortal client is attached to.
    /// </summary>
    string HomeServerUUID { get; }

    /// <summary>
    /// Gets the display name of the last connecdted home server.
    /// </summary>
    string LastHomeServerName { get; }

    /// <summary>
    /// Gets the computer name of the last connecdted home server.
    /// </summary>
    SystemName LastHomeServerSystem { get; }

    /// <summary>
    /// Gets the content directory service stub connected to the MediaPortal server's content directory service.
    /// If the home server is not connected, this value is <c>null</c>.
    /// </summary>
    /// <remarks>
    /// We publish the actual UPnP implementation of the content directory service here, because it would be too much work
    /// to encapsulate the whole UPnP system.
    /// </remarks>
    UPnPContentDirectoryService ContentDirectoryService { get; }

    /// <summary>
    /// Starts the UPnP subsystem and tries to connect to the home server, if set.
    /// </summary>
    void Startup();

    /// <summary>
    /// Shuts the UPnP subsystem down.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Sets the server given by the <paramref name="serverDescriptor"/> as new home server and tries to connect to it.
    /// </summary>
    /// <param name="serverDescriptor">Descriptor of the server to set as home server.</param>
    void SetNewHomeServer(ServerDescriptor serverDescriptor);
  }
}
