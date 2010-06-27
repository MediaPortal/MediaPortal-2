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

using System.Collections.Generic;
using MediaPortal.Core.General;

namespace MediaPortal.Backend.ClientCommunication
{
  /// <summary>
  /// Contains data about an attached MediaPortal client.
  /// </summary>
  public class AttachedClientData
  {
    protected string _systemId;
    protected SystemName _lastSystem;
    protected string _lastClientName;

    internal AttachedClientData(string systemId, SystemName lastHostName, string lastClientName)
    {
      _systemId = systemId;
      _lastSystem = lastHostName;
      _lastClientName = lastClientName;
    }

    /// <summary>
    /// UUID of the attached client.
    /// </summary>
    public string SystemId
    {
      get { return _systemId; }
    }

    /// <summary>
    /// Last known host name of the client.
    /// </summary>
    public SystemName LastSystem
    {
      get { return _lastSystem; }
    }


    /// <summary>
    /// Last known client name.
    /// </summary>
    public string LastClientName
    {
      get { return _lastClientName; }
    }
  }

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
    /// <see cref="AttachedClients"/>, i.e. only attached clients are contained in this collection.
    /// </summary>
    ICollection<ClientConnection> ConnectedClients { get; }

    /// <summary>
    /// Gets a dictionary of client's UUIDs to attachment structures of all clients which are currently attached to the server.
    /// </summary>
    IDictionary<string, AttachedClientData> AttachedClients { get; }

    void AttachClient(string clientSystemId);
    void DetachClientAndRemoveShares(string clientSystemId);

    SystemName GetSystemNameForSystemId(string systemId);
  }
}
