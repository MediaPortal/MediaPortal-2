#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using Deusty.Net;
using System.Collections.Concurrent;

namespace MediaPortal.Plugins.WifiRemote
{
  /// <summary>
  /// Extends the AsyncSocket class so additional properties can
  /// be added to the class AsyncSocket without changing the code of
  /// this class (so we can update the class later on)
  /// </summary>
  public static class AsyncSocketExtension
  {
    /// <summary>
    /// Remote clients associated with sockets
    /// </summary>
    private static ConcurrentDictionary<AsyncSocket, RemoteClient> RemoteClients = new ConcurrentDictionary<AsyncSocket, RemoteClient>();

    /// <summary>
    /// Get the remote client associated with the socket
    /// </summary>
    /// <param name="socket">socket</param>
    /// <returns>remote client</returns>
    public static RemoteClient GetRemoteClient(this AsyncSocket socket)
    {
      if (RemoteClients.TryGetValue(socket, out var client))
        return client;
      return null;
    }

    /// <summary>
    /// Sets the remote client associated with the socket
    /// </summary>
    /// <param name="socket">socket</param>
    /// <param name="client">remote clien</param>
    public static void SetRemoteClient(this AsyncSocket socket, RemoteClient client)
    {
      RemoteClients.TryAdd(socket, client);
    }
  }
}
