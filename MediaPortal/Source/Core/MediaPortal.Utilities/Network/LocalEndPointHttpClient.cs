#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Utilities.Network
{
  /// <summary>
  /// Implementation of <see cref="HttpClient"/> that can optionally send messages on a specific local endpoint.
  /// Messages that should be sent on a specific local endpoint should set the local <see cref="IPAddress"/>
  /// to use with a call to <see cref="SetLocalEndpoint(HttpRequestMessage, IPAddress)"/>.
  /// </summary>
  public class LocalEndPointHttpClient : HttpClient
  {
    public static readonly HttpRequestOptionsKey<IPAddress> LOCAL_ENDPOINT_KEY = new HttpRequestOptionsKey<IPAddress>("LocalEndpoint");

    /// <summary>
    /// Creates a new instance of <see cref="LocalEndPointHttpClient"/> that uses a <see cref="SocketsHttpHandler"/> configured to bind to
    /// the local endpoint specified for a request with a call to <see cref="SetLocalEndpoint(HttpRequestMessage, IPAddress)"/>. 
    /// </summary>
    /// <param name="configureHandlerAction">Optional delegate that will be called with the <see cref="SocketsHttpHandler"/> that will be used to send requests to allow additional configuration.</param>
    /// <returns>A new instance of <see cref="LocalEndPointHttpClient"/>.</returns>
    public static LocalEndPointHttpClient Create(Action<SocketsHttpHandler> configureHandlerAction = null)
    {
      SocketsHttpHandler socketsHttpHandler = CreateHandler();
      configureHandlerAction?.Invoke(socketsHttpHandler);
      return new LocalEndPointHttpClient(socketsHttpHandler);
    }

    /// <summary>
    /// Sets the local endpoint/network interface which is used to send the given HTTP <paramref name="request"/> when using a <see cref="LocalEndPointHttpClient"/>.
    /// </summary>
    /// <remarks>
    /// Due to problems which can avoid HTTP requests being delivered correctly, it is sometimes necessary to give a HTTP request object a
    /// "hint" which local endpoint should be used. If HTTP requests time out (which is often the case when virtual VMWare network interfaces are
    /// installed), it might help to call this method.
    /// Callers should initially set the value <see cref="NetworkUtils.LimitIPEndpoints"/> to enable this behavior (default: disabled).
    /// </remarks>
    /// <param name="request">HTTP web request to patch.</param>
    /// <param name="preferredLocalIpAddress">Local IP address which is bound to the network interface which should be used to bind the
    /// outgoing HTTP request to.</param>
    public static void SetLocalEndpoint(HttpRequestMessage request, IPAddress preferredLocalIpAddress)
    {
      request.Options.Set(LOCAL_ENDPOINT_KEY, preferredLocalIpAddress);
    }

    protected static SocketsHttpHandler CreateHandler()
    {
      SocketsHttpHandler handler = new SocketsHttpHandler();
      handler.ConnectCallback = ConnectCallback;
      return handler;
    }

    /// <summary>
    /// Callback used by the <see cref="SocketsHttpHandler"/> to open new connections.
    /// </summary>
    /// <param name="context">The connection context.</param>
    /// <param name="cancellationToken">Cancellation token used to cancel the connection.</param>
    /// <returns>A task that completes when the connection has been opened.</returns>
    protected static async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
      Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

      // If the request soecified a specific local endpoint bind to it
      if (context.InitialRequestMessage.Options.TryGetValue(LOCAL_ENDPOINT_KEY, out IPAddress address))
        socket.Bind(new IPEndPoint(address, 0));

      try
      {
        await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);
        return new NetworkStream(socket, true);
      }
      catch
      {
        socket.Dispose();
        throw;
      }
    }

    protected LocalEndPointHttpClient(SocketsHttpHandler socketsHttpHandler)
      : base(socketsHttpHandler)
    {
    }
  }
}
