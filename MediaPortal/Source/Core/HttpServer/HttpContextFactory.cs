using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace HttpServer
{
  /// <summary>
  /// Used to create and reuse contexts.
  /// </summary>
  public class HttpContextFactory : IHttpContextFactory
  {
    private readonly int _bufferSize;
    private readonly Queue<HttpClientContext> _contextQueue = new Queue<HttpClientContext>();
    private readonly IRequestParserFactory _factory;
    private readonly ILogWriter _logWriter;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpContextFactory"/> class.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="bufferSize">Amount of bytes to read from the incoming socket stream.</param>
    /// <param name="factory">Used to create a request parser.</param>
    public HttpContextFactory(ILogWriter writer, int bufferSize, IRequestParserFactory factory)
    {
      _logWriter = writer;
      _bufferSize = bufferSize;
      _factory = factory;
    }

    ///<summary>
    /// True if detailed trace logs should be written.
    ///</summary>
    public bool UseTraceLogs { get; set; }

    /// <summary>
    /// Create a new context.
    /// </summary>
    /// <param name="isSecured">true if socket is running HTTPS.</param>
    /// <param name="endPoint">Client that connected</param>
    /// <param name="stream">Network/SSL stream.</param>
    /// <param name="sock">Socket to use.</param>
    /// <returns>A context.</returns>
    protected HttpClientContext CreateContext(bool isSecured, IPEndPoint endPoint, Stream stream, Socket sock)
    {
      HttpClientContext context;
      lock (_contextQueue)
      {
        if (_contextQueue.Count > 0)
        {
          context = _contextQueue.Dequeue();
          if (!context.Available)
          {
            context = CreateNewContext(isSecured, endPoint, stream, sock);
            context.Disconnected += OnFreeContext;
            context.RequestReceived += OnRequestReceived;
            context.EndWhenDone = true;
          }
        }
        else
        {
          context = CreateNewContext(isSecured, endPoint, stream, sock);
          context.Disconnected += OnFreeContext;
          context.RequestReceived += OnRequestReceived;
        }
      }

      context.Stream = stream;
      context.IsSecured = isSecured;
      context.RemotePort = endPoint.Port.ToString();
      context.RemoteAddress = endPoint.Address.ToString();
      context.Start();
      return context;
    }

    /// <summary>
    /// Create a new context.
    /// </summary>
    /// <param name="isSecured">true if HTTPS is used.</param>
    /// <param name="endPoint">Remote client</param>
    /// <param name="stream">Network stream, <see cref="HttpClientContext"/> uses <see cref="ReusableSocketNetworkStream"/>.</param>
    /// <param name="sock">Socket to use.</param>
    /// <returns>A new context (always).</returns>
    protected virtual HttpClientContext CreateNewContext(bool isSecured, IPEndPoint endPoint, Stream stream, Socket sock)
    {
      return new HttpClientContext(isSecured, endPoint, stream, _factory, _bufferSize, sock);
    }

    private void OnRequestReceived(object sender, RequestEventArgs e)
    {
      RequestReceived(sender, e);
    }

    private void OnFreeContext(object sender, DisconnectedEventArgs e)
    {
      var imp = (HttpClientContext) sender;
      imp.Cleanup();

      if (!imp.EndWhenDone)
      {
        lock (_contextQueue)
          _contextQueue.Enqueue(imp);
      }
      else
      {
        imp.Close();
      }
    }


    #region IHttpContextFactory Members

    /// <summary>
    /// Create a secure <see cref="IHttpClientContext"/>.
    /// </summary>
    /// <param name="socket">Client socket (accepted by the <see cref="HttpListener"/>).</param>
    /// <param name="certificate">HTTPS certificate to use.</param>
    /// <param name="protocol">Kind of HTTPS protocol. Usually TLS or SSL.</param>
    /// <returns>
    /// A created <see cref="IHttpClientContext"/>.
    /// </returns>
    public IHttpClientContext CreateSecureContext(Socket socket, X509Certificate certificate, SslProtocols protocol)
    {
      var networkStream = new ReusableSocketNetworkStream(socket, true);
      var remoteEndPoint = (IPEndPoint) socket.RemoteEndPoint;

      var sslStream = new SslStream(networkStream, false);
      try
      {
        //TODO: this may fail
        sslStream.AuthenticateAsServer(certificate, false, protocol, false);
        return CreateContext(true, remoteEndPoint, sslStream, socket);
      }
      catch (IOException err)
      {
        if (UseTraceLogs)
          _logWriter.Write(this, LogPrio.Trace, err.Message);
      }
      catch (ObjectDisposedException err)
      {
        if (UseTraceLogs)
          _logWriter.Write(this, LogPrio.Trace, err.Message);
      }

      return null;
    }


    /// <summary>
    /// A request have been received from one of the contexts.
    /// </summary>
    public event EventHandler<RequestEventArgs> RequestReceived = delegate { };

    /// <summary>
    /// Creates a <see cref="IHttpClientContext"/> that handles a connected client.
    /// </summary>
    /// <param name="socket">Client socket (accepted by the <see cref="HttpListener"/>).</param>
    /// <returns>
    /// A creates <see cref="IHttpClientContext"/>.
    /// </returns>
    public IHttpClientContext CreateContext(Socket socket)
    {
      var networkStream = new ReusableSocketNetworkStream(socket, true);
      var remoteEndPoint = (IPEndPoint) socket.RemoteEndPoint;
      return CreateContext(false, remoteEndPoint, networkStream, socket);
    }

    #endregion
  }

  /// <summary>
  /// Custom network stream to mark sockets as reusable when disposing the stream.
  /// </summary>
  internal class ReusableSocketNetworkStream : NetworkStream
  {
    private bool disposed = false;
    /// <summary>
    ///                     Creates a new instance of the <see cref="T:System.Net.Sockets.NetworkStream" /> class for the specified <see cref="T:System.Net.Sockets.Socket" />.
    /// </summary>
    /// <param name="socket">
    ///                     The <see cref="T:System.Net.Sockets.Socket" /> that the <see cref="T:System.Net.Sockets.NetworkStream" /> will use to send and receive data. 
    ///                 </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///                     The <paramref name="socket" /> parameter is null. 
    ///                 </exception>
    /// <exception cref="T:System.IO.IOException">
    ///                     The <paramref name="socket" /> parameter is not connected.
    ///                     -or- 
    ///                     The <see cref="P:System.Net.Sockets.Socket.SocketType" /> property of the <paramref name="socket" /> parameter is not <see cref="F:System.Net.Sockets.SocketType.Stream" />.
    ///                     -or- 
    ///                     The <paramref name="socket" /> parameter is in a nonblocking state. 
    ///                 </exception>
    public ReusableSocketNetworkStream(Socket socket)
      : base(socket)
    {
    }

    /// <summary>
    ///                     Initializes a new instance of the <see cref="T:System.Net.Sockets.NetworkStream" /> class for the specified <see cref="T:System.Net.Sockets.Socket" /> with the specified <see cref="T:System.Net.Sockets.Socket" /> ownership.
    /// </summary>
    /// <param name="socket">
    ///                     The <see cref="T:System.Net.Sockets.Socket" /> that the <see cref="T:System.Net.Sockets.NetworkStream" /> will use to send and receive data. 
    ///                 </param>
    /// <param name="ownsSocket">
    ///                     Set to true to indicate that the <see cref="T:System.Net.Sockets.NetworkStream" /> will take ownership of the <see cref="T:System.Net.Sockets.Socket" />; otherwise, false. 
    ///                 </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///                     The <paramref name="socket" /> parameter is null. 
    ///                 </exception>
    /// <exception cref="T:System.IO.IOException">
    ///                     The <paramref name="socket" /> parameter is not connected.
    ///                     -or- 
    ///                     the value of the <see cref="P:System.Net.Sockets.Socket.SocketType" /> property of the <paramref name="socket" /> parameter is not <see cref="F:System.Net.Sockets.SocketType.Stream" />.
    ///                     -or- 
    ///                     the <paramref name="socket" /> parameter is in a nonblocking state. 
    ///                 </exception>
    public ReusableSocketNetworkStream(Socket socket, bool ownsSocket)
      : base(socket, ownsSocket)
    {
    }

    /// <summary>
    ///                     Creates a new instance of the <see cref="T:System.Net.Sockets.NetworkStream" /> class for the specified <see cref="T:System.Net.Sockets.Socket" /> with the specified access rights.
    /// </summary>
    /// <param name="socket">
    ///                     The <see cref="T:System.Net.Sockets.Socket" /> that the <see cref="T:System.Net.Sockets.NetworkStream" /> will use to send and receive data. 
    ///                 </param>
    /// <param name="access">
    ///                     A bitwise combination of the <see cref="T:System.IO.FileAccess" /> values that specify the type of access given to the <see cref="T:System.Net.Sockets.NetworkStream" /> over the provided <see cref="T:System.Net.Sockets.Socket" />. 
    ///                 </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///                     The <paramref name="socket" /> parameter is null. 
    ///                 </exception>
    /// <exception cref="T:System.IO.IOException">
    ///                     The <paramref name="socket" /> parameter is not connected.
    ///                     -or- 
    ///                     the <see cref="P:System.Net.Sockets.Socket.SocketType" /> property of the <paramref name="socket" /> parameter is not <see cref="F:System.Net.Sockets.SocketType.Stream" />.
    ///                     -or- 
    ///                     the <paramref name="socket" /> parameter is in a nonblocking state. 
    ///                 </exception>
    public ReusableSocketNetworkStream(Socket socket, FileAccess access)
      : base(socket, access)
    {
    }

    /// <summary>
    ///                     Creates a new instance of the <see cref="T:System.Net.Sockets.NetworkStream" /> class for the specified <see cref="T:System.Net.Sockets.Socket" /> with the specified access rights and the specified <see cref="T:System.Net.Sockets.Socket" /> ownership.
    /// </summary>
    /// <param name="socket">
    ///                     The <see cref="T:System.Net.Sockets.Socket" /> that the <see cref="T:System.Net.Sockets.NetworkStream" /> will use to send and receive data. 
    ///                 </param>
    /// <param name="access">
    ///                     A bitwise combination of the <see cref="T:System.IO.FileAccess" /> values that specifies the type of access given to the <see cref="T:System.Net.Sockets.NetworkStream" /> over the provided <see cref="T:System.Net.Sockets.Socket" />. 
    ///                 </param>
    /// <param name="ownsSocket">
    ///                     Set to true to indicate that the <see cref="T:System.Net.Sockets.NetworkStream" /> will take ownership of the <see cref="T:System.Net.Sockets.Socket" />; otherwise, false. 
    ///                 </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///                     The <paramref name="socket" /> parameter is null. 
    ///                 </exception>
    /// <exception cref="T:System.IO.IOException">
    ///                     The <paramref name="socket" /> parameter is not connected.
    ///                     -or- 
    ///                     The <see cref="P:System.Net.Sockets.Socket.SocketType" /> property of the <paramref name="socket" /> parameter is not <see cref="F:System.Net.Sockets.SocketType.Stream" />.
    ///                     -or- 
    ///                     The <paramref name="socket" /> parameter is in a nonblocking state. 
    ///                 </exception>
    public ReusableSocketNetworkStream(Socket socket, FileAccess access, bool ownsSocket)
      : base(socket, access, ownsSocket)
    {
    }

    /// <summary>
    /// Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.
    /// </summary>
    public override void Close()
    {
      if (Socket != null && Socket.Connected)
        Socket.Close(); //TODO: Maybe use Disconnect with reuseSocket=true? I tried but it took forever.
      base.Close();
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="T:System.Net.Sockets.NetworkStream"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {

      if (!disposed)
      {
        disposed = true;
        if (Socket != null && Socket.Connected)
          // Exception handler added by Albert, Team-Mediaportal:
          // The Disconnect call raises an ObjectDisposedException if the system isn't shut down correctly.
          // Besides that, sometimes a SocketException is raised.
          try
          {
            Socket.Disconnect(true);
          }
          catch (SystemException) { }

        base.Dispose(disposing);
      }
    }
  }

  /// <summary>
  /// Used to create <see cref="IHttpClientContext"/>es.
  /// </summary>
  public interface IHttpContextFactory
  {
    /// <summary>
    /// Creates a <see cref="IHttpClientContext"/> that handles a connected client.
    /// </summary>
    /// <param name="socket">Client socket (accepted by the <see cref="HttpListener"/>).</param>
    /// <returns>A creates <see cref="IHttpClientContext"/>.</returns>
    IHttpClientContext CreateContext(Socket socket);

    /// <summary>
    /// Create a secure <see cref="IHttpClientContext"/>.
    /// </summary>
    /// <param name="socket">Client socket (accepted by the <see cref="HttpListener"/>).</param>
    /// <param name="certificate">HTTPS certificate to use.</param>
    /// <param name="protocol">Kind of HTTPS protocol. Usually TLS or SSL.</param>
    /// <returns>A created <see cref="IHttpClientContext"/>.</returns>
    IHttpClientContext CreateSecureContext(Socket socket, X509Certificate certificate, SslProtocols protocol);

    /// <summary>
    /// A request have been received from one of the contexts.
    /// </summary>
    event EventHandler<RequestEventArgs> RequestReceived;
  }
}