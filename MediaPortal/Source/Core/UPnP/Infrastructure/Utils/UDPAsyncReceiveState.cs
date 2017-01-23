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

using System.Net.Sockets;

namespace UPnP.Infrastructure.Utils
{
  /// <summary>
  /// <see cref="UDPAsyncReceiveState{T}"/> holds information about a Endpoint of type <typeparamref name="T"/>, the associated Socket
  /// and the receive buffer. The Socket's ReceiveBufferSize will be set to the <see cref="UPnPConsts.UDP_RECEIVE_BUFFER_FACTOR"/> times of the
  /// given C# read buffer size.
  /// </summary>
  public class UDPAsyncReceiveState<T>
  {
    protected byte[] _buffer;
    protected T _config;
    protected Socket _socket;

    public UDPAsyncReceiveState(T config, int bufferSize, Socket socket)
    {
      _config = config;
      _buffer = new byte[bufferSize];
      _socket = socket;
      _socket.ReceiveBufferSize = bufferSize * UPnPConsts.UDP_RECEIVE_BUFFER_FACTOR;
    }

    public T Endpoint
    {
      get { return _config; }
    }

    public Socket Socket
    {
      get { return _socket; }
    }

    public byte[] Buffer
    {
      get { return _buffer; }
    }
  }
}