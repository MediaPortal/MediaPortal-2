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

#if NET5_0_OR_GREATER
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
#else
using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
#endif

namespace MediaPortal.Utilities.Network
{
  public class LocalEndPointHttpClientOptions
  {
    public DecompressionMethods AutomaticDecompression { get; set; }
  }

  /// <summary>
  /// Implementation of <see cref="HttpClient"/> that can optionally send messages on a specific local endpoint.
  /// Messages that should be sent on a specific local endpoint should set the local <see cref="IPAddress"/>
  /// to use with a call to <see cref="SetLocalEndpoint(HttpRequestMessage, IPAddress)"/>.
  /// </summary>
  public class LocalEndPointHttpClient : HttpClient
  {
    /// <summary>
    /// Creates a new instance of <see cref="LocalEndPointHttpClient"/> that uses a <see cref="SocketsHttpHandler"/> configured to bind to
    /// the local endpoint specified for a request with a call to <see cref="SetLocalEndpoint(HttpRequestMessage, IPAddress)"/>. 
    /// </summary>
    /// <param name="configureHandlerAction">Optional delegate that will be called with the <see cref="SocketsHttpHandler"/> that will be used to send requests to allow additional configuration.</param>
    /// <returns>A new instance of <see cref="LocalEndPointHttpClient"/>.</returns>
    public static LocalEndPointHttpClient Create(LocalEndPointHttpClientOptions options = null)
    {
      HttpMessageHandler socketsHttpHandler = CreateHandler(options);
      return new LocalEndPointHttpClient(socketsHttpHandler);
    }

#if NET5_0_OR_GREATER
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

    public static readonly HttpRequestOptionsKey<IPAddress> LOCAL_ENDPOINT_KEY = new HttpRequestOptionsKey<IPAddress>("LocalEndpoint");

    protected static HttpMessageHandler CreateHandler(LocalEndPointHttpClientOptions options)
    {
      SocketsHttpHandler handler = new SocketsHttpHandler();
      handler.ConnectCallback = ConnectCallback;
      if (options != null)
        handler.AutomaticDecompression = options.AutomaticDecompression;
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
#else
    const string LOCAL_ENDPOINT_KEY = "MP2_LocalEndPoint";

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
      request.Properties[LOCAL_ENDPOINT_KEY] = preferredLocalIpAddress;
    }

    protected static HttpMessageHandler CreateHandler(LocalEndPointHttpClientOptions options)
    {
      HttpClientHandler handler = new HttpClientHandler();

      if (options != null)
        handler.AutomaticDecompression = options.AutomaticDecompression;
      SetServicePointOptions(handler);
      return handler;
    }

    private static FieldInfo _startRequestField;
    private static FieldInfo _webRequestField;
    private static FieldInfo _requestMessageField;

    /// <summary>
    /// This is a horrible hack using reflection, but it's the only way to bind a message sent using a HttpClient to a local endpoint
    /// on .net framework. In order to do so it's necessary to get the underlying HttpWebRequest so that a local endpoint can be
    /// bound to the request's ServicePoint. The HttpWebRequest is passed as a property of a private RequestState class, which is
    /// itself passed as an argument to a delegate stored in a private _startRequest field of the HttpClientHandler.
    /// The below uses reflection to set our own _startRequest delegate, then uses reflection on the object passed to that to
    /// get the HttpWebRequest and the original HttpRequestMessage.
    /// This method is based on the source code for the HttpClientHandler here -
    /// https://github.com/microsoft/referencesource/blob/e7b9db69533dca155a33f96523875e9c50445f44/System/net/System/Net/Http/HttpClientHandler.cs
    /// </summary>
    /// <param name="handler"></param>
    static void SetServicePointOptions(HttpClientHandler handler)
    {
      // Cache the _startRequest FieldInfo if not already
      if (_startRequestField == null)
        _startRequestField = handler.GetType().GetField("_startRequest", BindingFlags.NonPublic | BindingFlags.Instance);
      
      // Get the existing _startRequest delegate
      var startRequest = (Action<object>)_startRequestField.GetValue(handler);

      // Create a new _startRequest delegate that gets the HttpWebRequest and the original HttpRequestMessage and binds
      // the HttpWebRequest to a local enpoint if required, then calls the original delegate
      Action<object> newStartRequest = obj =>
      {
        // If LimitIPEndpoints is false then requests aren't bound to a local enpoint so avoid the
        // reflection overhead and simply pass through to the original _startRequest delegate
        if (NetworkUtils.LimitIPEndpoints)
        {
          if (_webRequestField == null)
            _webRequestField = obj.GetType().GetField("webRequest", BindingFlags.NonPublic | BindingFlags.Instance);
          if (_requestMessageField == null)
            _requestMessageField = obj.GetType().GetField("request", BindingFlags.NonPublic | BindingFlags.Instance);

          var requestMessage = _requestMessageField.GetValue(obj) as HttpRequestMessage;
          if (requestMessage.Properties.TryGetValue(LOCAL_ENDPOINT_KEY, out object ipAddressObj))
          {
            var webRequest = _webRequestField.GetValue(obj) as HttpWebRequest;
            NetworkUtils.SetLocalEndpoint(webRequest, (IPAddress)ipAddressObj);
          }
        }
        //call the original delegate
        startRequest(obj);
      };

      //replace original _startRequest delegate with the one above
      _startRequestField.SetValue(handler, newStartRequest);
    }

#endif

    protected LocalEndPointHttpClient(HttpMessageHandler httpHandler)
      : base(httpHandler)
    {
    }
  }
}
