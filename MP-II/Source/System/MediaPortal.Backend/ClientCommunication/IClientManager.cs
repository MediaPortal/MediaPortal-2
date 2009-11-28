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

namespace MediaPortal.Backend.ClientCommunication
{
  /// <summary>
  /// Manages the UPnP connection to all attached clients.
  /// </summary>
  public interface IClientManager
  {
    /// <summary>
    /// Starts the UPnP subsystem and searches all attached clients.
    /// </summary>
    void Startup();

    /// <summary>
    /// Shuts the UPnP subsystem down.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Gets a collection of descriptor objects for all connected MediaPortal clients. This is a subset of
    /// <see cref="AttachedClientsSystemIds"/>, i.e. only attached clients are contained in this collection.
    /// </summary>
    ICollection<ClientConnection> ConnectedClients { get; }

    /// <summary>
    /// Gets a collection of UUIDs of all MediaPortal clients which are attached to this server.
    /// </summary>
    ICollection<string> AttachedClientsSystemIds { get; }

    void AttachClient(string clientSystemId);
    void DetachClientAndRemoveShares(string clientSystemId);

    SystemName GetSystemNameForSystemId(string systemId);
  }
}
