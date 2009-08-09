using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace HttpServer
{
  /// <summary>
  /// Contains a listener that doesn't do anything with the connections.
  /// </summary>
  public abstract class HttpListenerBase
  {
    private readonly IPAddress _address;
    private readonly X509Certificate _certificate;
    private readonly IHttpContextFactory _factory;
    private readonly int _port;
    private readonly ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
    private readonly SslProtocols _sslProtocol = SslProtocols.Tls;
    private TcpListener _listener;
    private ILogWriter _logWriter = NullLogWriter.Instance;
    private int _pendingAccepts;
    private bool _shutdown;

    /// <summary>
    /// Listen for regular HTTP connections
    /// </summary>
    /// <param name="address">IP Address to accept connections on</param>
    /// <param name="port">TCP Port to listen on, default HTTP port is 80.</param>
    /// <param name="factory">Factory used to create <see cref="IHttpClientContext"/>es.</param>
    /// <exception cref="ArgumentNullException"><c>address</c> is null.</exception>
    /// <exception cref="ArgumentException">Port must be equal or greater than 0.</exception>
    // Changed by Albert, Team-MediaPortal: Port number is allowed to be 0.
    protected HttpListenerBase(IPAddress address, int port, IHttpContextFactory factory)
    {
      Check.Require(address, "address");
      Check.Min(0, port, "port");
      Check.Require(factory, "factory");

      _address = address;
      _port = port;
      _factory = factory;
      _factory.RequestReceived += OnRequestReceived;
    }

    private void OnRequestReceived(object sender, RequestEventArgs e)
    {
      RequestReceived(sender, e);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpListenerBase"/> class.
    /// </summary>
    /// <param name="address">IP Address to accept connections on</param>
    /// <param name="port">TCP Port to listen on, default HTTPS port is 443</param>
    /// <param name="factory">Factory used to create <see cref="IHttpClientContext"/>es.</param>
    /// <param name="certificate">Certificate to use</param>
    protected HttpListenerBase(IPAddress address, int port, IHttpContextFactory factory, X509Certificate certificate)
        : this(address, port, factory)
    {
      Check.Require(certificate, "certificate");

      _certificate = certificate;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpListenerBase"/> class.
    /// </summary>
    /// <param name="address">IP Address to accept connections on</param>
    /// <param name="port">TCP Port to listen on, default HTTPS port is 443</param>
    /// <param name="factory">Factory used to create <see cref="IHttpClientContext"/>es.</param>
    /// <param name="certificate">Certificate to use</param>
    /// <param name="protocol">which HTTPS protocol to use, default is TLS.</param>
    protected HttpListenerBase(
        IPAddress address, int port, IHttpContextFactory factory, X509Certificate certificate,
        SslProtocols protocol)
        : this(address, port, factory, certificate)
    {
      _sslProtocol = protocol;
    }


    /// <summary>
    /// Added by Albert, Team-MediaPortal: Returns the endpoint this listener is bound to or <c>null</c> if this listener
    /// isn't started yet.
    /// </summary>
    public IPEndPoint LocalEndpoint
    {
      get { return _listener == null ? null : (IPEndPoint) _listener.LocalEndpoint; }
    }

    /// <summary>
    /// Gives you a change to receive log entries for all internals of the HTTP library.
    /// </summary>
    /// <remarks>
    /// You may not switch log writer after starting the listener.
    /// </remarks>
    public ILogWriter LogWriter
    {
      get { return _logWriter; }
      set
      {
        _logWriter = value ?? NullLogWriter.Instance;
        if (_certificate != null)
          _logWriter.Write(
              this, LogPrio.Info,
              "HTTPS(" + _sslProtocol + ") listening on " + _address + ":" + _port);
        else
          _logWriter.Write(this, LogPrio.Info, "HTTP listening on " + _address + ":" + _port);
      }
    }

    /// <summary>
    /// True if we should turn on trace logs.
    /// </summary>
    public bool UseTraceLogs { get; set; }


    /// <exception cref="Exception"><c>Exception</c>.</exception>
    private void OnAccept(IAsyncResult ar)
    {
      bool beginAcceptCalled = false;
      try
      {
        int count = Interlocked.Decrement(ref _pendingAccepts);
        if (_shutdown)
        {
          if (count == 0)
            _shutdownEvent.Set();
          return;
        }

        Interlocked.Increment(ref _pendingAccepts);
        _listener.BeginAcceptSocket(OnAccept, null);
        beginAcceptCalled = true;
        Socket socket = _listener.EndAcceptSocket(ar);

        if (!OnAcceptingSocket(socket))
        {
          socket.Disconnect(true);
          return;
        }

        _logWriter.Write(this, LogPrio.Debug, "Accepted connection from: " + socket.RemoteEndPoint);

        if (_certificate != null)
          _factory.CreateSecureContext(socket, _certificate, _sslProtocol);
        else
          _factory.CreateContext(socket);
      }
      catch (Exception err)
      {
        _logWriter.Write(this, LogPrio.Debug, err.Message);
        if (ExceptionThrown == null)
#if DEBUG
          throw;
#else
                   _logWriter.Write(this, LogPrio.Fatal, err.Message);
        // we can't really do anything but close the connection
#endif
        if (ExceptionThrown != null)
          ExceptionThrown(this, err);

        if (!beginAcceptCalled)
          RetryBeginAccept();
      }
    }

    /// <summary>
    /// Will try to accept connections one more time.
    /// </summary>
    /// <exception cref="Exception">If any exceptions is thrown.</exception>
    private void RetryBeginAccept()
    {
      try
      {
        _logWriter.Write(this, LogPrio.Error, "Trying to accept connections again.");
        _listener.BeginAcceptSocket(OnAccept, null);
      }
      catch (Exception err)
      {
        _logWriter.Write(this, LogPrio.Fatal, err.Message);
        if (ExceptionThrown == null)
#if DEBUG
          throw;
#else
                   _logWriter.Write(this, LogPrio.Fatal, err.Message);
        // we can't really do anything but close the connection
#endif
        if (ExceptionThrown != null)
          ExceptionThrown(this, err);
      }
    }

    /// <summary>
    /// Can be used to create filtering of new connections.
    /// </summary>
    /// <param name="socket">Accepted socket</param>
    /// <returns>true if connection can be accepted; otherwise false.</returns>
    protected abstract bool OnAcceptingSocket(Socket socket);

    /// <summary>
    /// Start listen for new connections
    /// </summary>
    /// <param name="backlog">Number of connections that can stand in a queue to be accepted.</param>
    /// <exception cref="InvalidOperationException">Listener have already been started.</exception>
    public void Start(int backlog)
    {
      if (_listener != null)
        throw new InvalidOperationException("Listener have already been started.");

      _listener = new TcpListener(_address, _port);
      _listener.Start(backlog);
      Interlocked.Increment(ref _pendingAccepts);
      _listener.BeginAcceptSocket(OnAccept, null);
    }


    /// <summary>
    /// Stop the listener
    /// </summary>
    /// <exception cref="SocketException"></exception>
    public void Stop()
    {
      _shutdown = true;
      _listener.Stop();
      if (!_shutdownEvent.WaitOne())
        _logWriter.Write(this, LogPrio.Error, "Failed to shutdown listener properly.");
      _listener = null;
    }

    /// <summary>
    /// Catch exceptions not handled by the listener.
    /// </summary>
    /// <remarks>
    /// Exceptions will be thrown during debug mode if this event is not used,
    /// exceptions will be printed to console and suppressed during release mode.
    /// </remarks>
    public event ExceptionHandler ExceptionThrown = delegate { };

    /// <summary>
    /// A request have been received from a <see cref="IHttpClientContext"/>.
    /// </summary>
    public event EventHandler<RequestEventArgs> RequestReceived = delegate { };
  }
}