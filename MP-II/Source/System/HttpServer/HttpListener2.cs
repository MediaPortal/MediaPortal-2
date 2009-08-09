using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace HttpServer
{
    public class HttpListener2
    {
        /// <summary>
        /// HTTP Listener waits for HTTP connections and provide us with <see cref="HttpListenerContext"/>s using the
        /// <see cref="RequestHandler"/> delegate.
        /// </summary>
        public class HttpListener
        {
            private readonly IPAddress _address;
            private readonly X509Certificate _certificate;
            private readonly int _port;
            private readonly SslProtocols _sslProtocol = SslProtocols.Tls;
            private ClientDisconnectedHandler _disconnectHandler;
            private TcpListener _listener;
            private ILogWriter _logWriter = NullLogWriter.Instance;
            private RequestReceivedHandler _requestHandler;
            private readonly object _listenLock = new object();
            private bool _canListen;
            private bool _useTraceLogs;

            /// <summary>
            /// This event should be used to validate the incoming connection, i.e. to determine
            /// if it can be handled or not.
            /// </summary>
            public event EventHandler<ClientAcceptedEventArgs> SocketAccepted = delegate { };


            /// <summary>
            /// Listen for regular HTTP connections
            /// </summary>
            /// <param name="address">IP Address to accept connections on</param>
            /// <param name="port">TCP Port to listen on, default HTTP port is 80.</param>
            public HttpListener(IPAddress address, int port)
            {
                if (address == null)
                    throw new ArgumentNullException("address");
                if (port <= 0)
                    throw new ArgumentException("Port must be a positive number.");

                _address = address;
                _port = port;
            }

            /// <summary>
            /// Launch HttpListener in SSL mode
            /// </summary>
            /// <param name="address">IP Address to accept connections on</param>
            /// <param name="port">TCP Port to listen on, default HTTPS port is 443</param>
            /// <param name="certificate">Certificate to use</param>
            public HttpListener(IPAddress address, int port, X509Certificate certificate)
                : this(address, port)
            {
                _certificate = certificate;
            }

            /// <summary>
            /// Launch HttpListener in SSL mode
            /// </summary>
            /// <param name="address">IP Address to accept connections on</param>
            /// <param name="port">TCP Port to listen on, default HTTPS port is 443</param>
            /// <param name="certificate">Certificate to use</param>
            /// <param name="protocol">which HTTPS protocol to use, default is TLS.</param>
            public HttpListener(IPAddress address, int port, X509Certificate certificate, SslProtocols protocol)
                : this(address, port, certificate)
            {
                _sslProtocol = protocol;
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
                        _logWriter.Write(this, LogPrio.Info,
                                         "HTTPS(" + _sslProtocol + ") listening on " + _address + ":" + _port);
                    else
                        _logWriter.Write(this, LogPrio.Info, "HTTP listening on " + _address + ":" + _port);
                }
            }

            /// <summary>
            /// This handler will be invoked each time a new connection is accepted.
            /// </summary>
            public RequestReceivedHandler RequestHandler
            {
                get { return _requestHandler; }
                set
                {
                    if (value == null)
                        throw new ArgumentNullException("value");

                    _requestHandler = value;
                }
            }

            /// <summary>
            /// True if we should turn on trace logs.
            /// </summary>
            public bool UseTraceLogs
            {
                get { return _useTraceLogs; }
                set { _useTraceLogs = value; }
            }


            private void OnAccept(IAsyncResult ar)
            {
                try
                {
                    // i'm not trying to avoid ALL cases here. but just the most simple ones.
                    // doing a lock would defeat the purpose since only one socket could be accepted at once.

                    // the lock kills performance and that's why I temporarly disabled it.
                    // right now it's up to the exception block to handle Stop()
                    /*lock (_listenLock)
                        if (!_canListen)
                            return;
                    */
                    Socket socket = _listener.EndAcceptSocket(ar);
                    _listener.BeginAcceptSocket(OnAccept, null);

                    ClientAcceptedEventArgs args = new ClientAcceptedEventArgs(socket);
                    Accepted(this, args);
                    if (args.Revoked)
                    {
                        _logWriter.Write(this, LogPrio.Debug, "Socket was revoked by event handler.");
                        socket.Close();
                        return;
                    }

                    _logWriter.Write(this, LogPrio.Debug, "Accepted connection from: " + socket.RemoteEndPoint);

                    NetworkStream stream = new NetworkStream(socket, true);
                    IPEndPoint remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;

                    if (_certificate != null)
                        CreateSecureContext(stream, remoteEndPoint);
                    else
                        new HttpClientContextImp(false, remoteEndPoint, _requestHandler, _disconnectHandler, stream,
                                                 LogWriter);
                }
                catch (Exception err)
                {
                    if (err is ObjectDisposedException || err is NullReferenceException) // occurs when we shut down the listener.
                    {
                        if (UseTraceLogs)
                            _logWriter.Write(this, LogPrio.Trace, err.Message);
                        return;
                    }

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
                }
            }

            private void CreateSecureContext(Stream stream, IPEndPoint remoteEndPoint)
            {
                SslStream sslStream = new SslStream(stream, false);
                try
                {
                    sslStream.AuthenticateAsServer(_certificate, false, _sslProtocol, false); //todo: this may fail
                    new HttpClientContextImp(true, remoteEndPoint, _requestHandler, _disconnectHandler, sslStream,
                                                       LogWriter);
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
            }
            /// <summary>
            /// Start listen for new connections
            /// </summary>
            /// <param name="backlog">Number of connections that can stand in a queue to be accepted.</param>
            public void Start(int backlog)
            {
                if (_listener == null)
                {
                    _listener = new TcpListener(_address, _port);
                    _listener.Start(backlog);
                    _canListen = true;
                }
                _listener.BeginAcceptSocket(OnAccept, null);
            }


            /// <summary>
            /// Stop the listener
            /// </summary>
            /// <exception cref="SocketException"></exception>
            public void Stop()
            {
                lock (_listenLock)
                    _canListen = false;
                _listener.Stop();
                _listener = null;
            }

            /// <summary>
            /// Let's to receive unhandled exceptions from the threads.
            /// </summary>
            /// <remarks>
            /// Exceptions will be thrown during debug mode if this event is not used,
            /// exceptions will be printed to console and suppressed during release mode.
            /// </remarks>
            public event ExceptionHandler ExceptionThrown;
        }
    }
}