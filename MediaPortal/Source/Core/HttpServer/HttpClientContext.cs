using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using HttpServer.Exceptions;
using HttpServer.Parser;

namespace HttpServer
{
  /// <summary>
  /// Contains a connection to a browser/client.
  /// </summary>
  /// <remarks>
  /// Remember to <see cref="Start"/> after you have hooked the <see cref="RequestReceived"/> event.
  /// </remarks>
  /// TODO: Maybe this class should be broken up into HttpClientChannel and HttpClientContext?
  public class HttpClientContext : IHttpClientContext, IDisposable
  {
    private readonly byte[] _buffer;
    private int _bytesLeft;
    private ILogWriter _log;
    private readonly IHttpRequestParser _parser;
    private readonly int _bufferSize;
    private IHttpRequest _currentRequest;

    public bool Available = true;
    public bool EndWhenDone = false;

    /// <summary>
    /// This context have been cleaned, which means that it can be reused.
    /// </summary>
    public event EventHandler Cleaned = delegate { };

    /// <summary>
    /// Context have been started (a new client have connected)
    /// </summary>
    public event EventHandler Started = delegate { };

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpClientContext"/> class.
    /// </summary>
    /// <param name="secured">true if the connection is secured (SSL/TLS)</param>
    /// <param name="remoteEndPoint">client that connected.</param>
    /// <param name="stream">Stream used for communication</param>
    /// <param name="parserFactory">Used to create a <see cref="IHttpRequestParser"/>.</param>
    /// <param name="bufferSize">Size of buffer to use when reading data. Must be at least 1024 bytes.</param>
    /// <param name="sock">Socket to use.</param>
    /// <exception cref="SocketException">If <see cref="Socket.BeginReceive(byte[],int,int,SocketFlags,AsyncCallback,object)"/> fails</exception>
    /// <exception cref="ArgumentException">Stream must be writable and readable.</exception>
    public HttpClientContext(
        bool secured, IPEndPoint remoteEndPoint,
        Stream stream, IRequestParserFactory parserFactory, int bufferSize, Socket sock)
    {
      Check.Require(remoteEndPoint, "remoteEndPoint");
      Check.NotEmpty(remoteEndPoint.Address.ToString(), "remoteEndPoint.Address");
      Check.Require(stream, "stream");
      Check.Require(parserFactory, "parser");
      Check.Min(1024, bufferSize, "bufferSize");
      Check.Require(sock, "socket");

      if (!stream.CanWrite || !stream.CanRead)
        throw new ArgumentException("Stream must be writable and readable.");

      _bufferSize = bufferSize;
      RemoteAddress = remoteEndPoint.Address.ToString();
      RemotePort = remoteEndPoint.Port.ToString();
      _log = NullLogWriter.Instance;
      _parser = parserFactory.CreateParser(_log);
      _parser.RequestCompleted += OnRequestCompleted;
      _parser.RequestLineReceived += OnRequestLine;
      _parser.HeaderReceived += OnHeaderReceived;
      _parser.BodyBytesReceived += OnBodyBytesReceived;
      _currentRequest = new HttpRequest();

      IsSecured = secured;
      _stream = stream;
      _buffer = new byte[bufferSize];
    }

    /// <summary>
    /// Process incoming body bytes.
    /// </summary>
    /// <param name="sender"><see cref="IHttpRequestParser"/></param>
    /// <param name="e">Bytes</param>
    protected virtual void OnBodyBytesReceived(object sender, BodyEventArgs e)
    {
      _currentRequest.AddToBody(e.Buffer, e.Offset, e.Count);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void OnHeaderReceived(object sender, HeaderEventArgs e)
    {
      if (string.Compare(e.Name, "expect", true) == 0 && e.Value.Contains("100-continue"))
      {
        Respond("HTTP/1.0", HttpStatusCode.Continue, "Please continue mate.");
      }

      _currentRequest.AddHeader(e.Name, e.Value);
    }

    private void OnRequestLine(object sender, RequestLineEventArgs e)
    {
      _currentRequest.Method = e.HttpMethod;
      _currentRequest.HttpVersion = e.HttpVersion;
      _currentRequest.UriPath = e.UriPath;
    }

    /// <summary>
    /// Overload to specify own type.
    /// </summary>
    /// <remarks>
    /// Must be specified before the context is being used.
    /// </remarks>
    protected IHttpRequest CurrentRequest
    {
      get { return _currentRequest; }
      set { _currentRequest = value; }
    }

    /// <summary>
    /// Start reading content.
    /// </summary>
    /// <remarks>
    /// Make sure to call base.Start() if you override this method.
    /// </remarks>
    public virtual void Start()
    {
      try
      {
        _stream.BeginRead(_buffer, 0, _bufferSize, OnReceive, null);
      }
      catch (IOException err)
      {
        LogWriter.Write(this, LogPrio.Debug, err.ToString());
      }

      Started(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clean up context.
    /// </summary>
    /// <remarks>
    /// Make sure to call base.Cleanup() if you override the method.
    /// </remarks>
    public virtual void Cleanup()
    {
      if (Stream == null)
        return;

      Stream.Dispose();
      Stream = null;
      _currentRequest.Clear();
      _bytesLeft = 0;
      Cleaned(this, EventArgs.Empty);
      _parser.Clear();
    }

    public void Close()
    {
      Cleanup();
      Available = true;
    }

    /// <summary>
    /// Using SSL or other encryption method.
    /// </summary>
    [Obsolete("Use IsSecured instead.")]
    public bool Secured
    {
      get { return IsSecured; }
    }

    /// <summary>
    /// Using SSL or other encryption method.
    /// </summary>
    public bool IsSecured { get; internal set; }

    /// <summary>
    /// Specify which logger to use.
    /// </summary>
    public ILogWriter LogWriter
    {
      get { return _log; }
      set
      {
        _log = value ?? NullLogWriter.Instance;
        _parser.LogWriter = _log;
      }
    }

    private Stream _stream;

    /// <summary>
    /// Gets or sets the network stream.
    /// </summary>
    internal Stream Stream
    {
      get { return _stream; }
      set { _stream = value; }
    }

    /// <summary>
    /// Gets or sets IP address that the client connected from.
    /// </summary>
    internal string RemoteAddress { get; set; }

    /// <summary>
    /// Gets or sets port that the client connected from.
    /// </summary>
    internal string RemotePort { get; set; }

    /// <summary>
    /// Disconnect from client
    /// </summary>
    /// <param name="error">error to report in the <see cref="Disconnected"/> event.</param>
    public void Disconnect(SocketError error)
    {
      // Disconnect may not throw any exceptions
      try
      {
        if (error == SocketError.Success)
        {
          ReusableSocketNetworkStream reusableSocketNetworkStream = Stream as ReusableSocketNetworkStream;
          if (reusableSocketNetworkStream != null)
            reusableSocketNetworkStream.Flush();
        }

        Disconnected(this, new DisconnectedEventArgs(error));
      }
      catch (Exception err)
      {
        LogWriter.Write(this, LogPrio.Error, "Disconnect threw an exception: " + err);
      }
    }

    /// <exception cref="BadRequestException"><c>BadRequestException</c>.</exception>
    private void OnReceive(IAsyncResult ar)
    {
      try
      {
        int bytesRead = Stream.EndRead(ar);
        if (bytesRead == 0)
        {
          Disconnect(SocketError.ConnectionReset);
          return;
        }
        // Albert, Team MediaPortal: Replaced usage of _bytesLeft by a local variable bytesLeft in this method
        // to minimize multithreading issues - to fix it finally, the multithreading capability of this method has to be reworked.
        int bytesLeft = _bytesLeft + bytesRead;
        if (bytesLeft > _buffer.Length)
        {
#if DEBUG
          throw new BadRequestException("Too large HTTP header: " + Encoding.UTF8.GetString(_buffer, 0, bytesRead));
#else
          throw new BadRequestException("Too large HTTP header: " + _bytesLeft);
#endif
        }

#if DEBUG
#pragma warning disable 219
        string temp = Encoding.ASCII.GetString(_buffer, 0, bytesLeft);
        LogWriter.Write(this, LogPrio.Trace, "Received: " + temp);
#pragma warning restore 219
#endif
        int offset = _parser.Parse(_buffer, 0, bytesLeft);
        if (Stream == null)
          return; // "Connection: Close" in effect.

        // try again to see if we can parse another message (check parser to see if it is looking for a new message)
        int oldOffset = offset;
        while (_parser.CurrentState == RequestParserState.FirstLine && offset != 0 && bytesLeft - offset > 0)
        {
#if DEBUG
          temp = Encoding.ASCII.GetString(_buffer, offset, bytesLeft - offset);
          LogWriter.Write(this, LogPrio.Trace, "Processing: " + temp);
#endif
          offset = _parser.Parse(_buffer, offset, bytesLeft - offset);
          if (Stream == null)
            return; // "Connection: Close" in effect.
        }

        // need to be able to move prev bytes, so restore offset.
        if (offset == 0)
          offset = oldOffset;

        // copy unused bytes to the beginning of the array
        if (offset > 0 && bytesLeft != offset)
          Buffer.BlockCopy(_buffer, offset, _buffer, 0, bytesLeft - offset);

        bytesLeft -= offset;
        if (Stream != null && Stream.CanRead)
          Stream.BeginRead(_buffer, bytesLeft, _buffer.Length - bytesLeft, OnReceive, null);
        else
        {
          _log.Write(this, LogPrio.Warning, "Could not read any more from the socket.");
          Disconnect(SocketError.Success);
        }
        // Albert, Team MediaPortal: Write the value back to the object's field
        _bytesLeft = bytesLeft;
      }
      catch (BadRequestException err)
      {
        LogWriter.Write(this, LogPrio.Warning, "Bad request, responding with it. Error: " + err);
        try
        {
          Respond("HTTP/1.0", HttpStatusCode.BadRequest, err.Message);
        }
        catch (Exception err2)
        {
          LogWriter.Write(this, LogPrio.Fatal, "Failed to reply to a bad request. " + err2);
        }
        Disconnect(SocketError.NoRecovery);
      }
      catch (IOException err)
      {
        LogWriter.Write(this, LogPrio.Debug, "Failed to end receive: " + err.Message);
        if (err.InnerException is SocketException)
          Disconnect((SocketError) ((SocketException) err.InnerException).ErrorCode);
        else
          Disconnect(SocketError.ConnectionReset);
      }
      catch (ObjectDisposedException err)
      {
        LogWriter.Write(this, LogPrio.Debug, "Failed to end receive : " + err.Message);
        Disconnect(SocketError.NotSocket);
      }
      catch (NullReferenceException err)
      {
        LogWriter.Write(this, LogPrio.Debug, "Failed to end receive : NullRef: " + err.Message);
        Disconnect(SocketError.NoRecovery);
      }
    }

    private void OnRequestCompleted(object source, EventArgs args)
    {
      _currentRequest.AddHeader("remote_addr", RemoteAddress);
      _currentRequest.AddHeader("remote_port", RemotePort);
      _currentRequest.Body.Seek(0, SeekOrigin.Begin);
      RequestReceived(this, new RequestEventArgs(_currentRequest));
      _currentRequest.Clear();
    }

    /// <summary>
    /// Send a response.
    /// </summary>
    /// <remarks>
    /// The <paramref name="body"/> will be encoded in ASCII.
    /// </remarks>
    /// <param name="httpVersion">Either <see cref="HttpHelper.HTTP10"/> or <see cref="HttpHelper.HTTP11"/></param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="reason">Reason for the status code.</param>
    /// <param name="body">HTML body contents, can be null or empty.</param>
    /// <param name="contentType">A content type to return the body as, i.e. 'text/html' or 'text/plain', defaults to 'text/html'
    /// if null or empty</param>
    /// <exception cref="ArgumentException">If <paramref name="httpVersion"/> is invalid.</exception>
    public void Respond(string httpVersion, HttpStatusCode statusCode, string reason, string body, string contentType)
    {
      if (string.IsNullOrEmpty(contentType))
        contentType = "text/html";

      if (string.IsNullOrEmpty(httpVersion) || !httpVersion.StartsWith("HTTP/1"))
        throw new ArgumentException("Invalid HTTP version");

      if (string.IsNullOrEmpty(reason))
        reason = statusCode.ToString();

      string response = string.IsNullOrEmpty(body)
          ? httpVersion + " " + (int) statusCode + " " + reason + "\r\nContent-Length: 0\r\n\r\n"
          : string.Format(
              "{0} {1} {2}\r\nContent-Type: {5}\r\nContent-Length: {3}\r\n\r\n{4}",
              httpVersion, (int) statusCode, reason ?? statusCode.ToString(),
              body.Length, body, contentType);
      byte[] buffer = Encoding.ASCII.GetBytes(response);

      Send(buffer);
    }

    /// <summary>
    /// Send a response.
    /// </summary>
    /// <param name="httpVersion">Either <see cref="HttpHelper.HTTP10"/> or <see cref="HttpHelper.HTTP11"/></param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="reason">reason for the status code.</param>
    public void Respond(string httpVersion, HttpStatusCode statusCode, string reason)
    {
      Respond(httpVersion, statusCode, reason, null, null);
    }

    /// <summary>
    /// Send a response.
    /// </summary>
    /// <remarks>
    /// The <paramref name="body"/> will be encoded in ASCII.
    /// </remarks>
    /// <exception cref="ArgumentNullException"></exception>
    public void Respond(string body)
    {
      if (body == null)
        throw new ArgumentNullException("body");
      Respond("HTTP/1.1", HttpStatusCode.OK, null, body, null);
    }

    /// <summary>
    /// Send a whole buffer
    /// </summary>
    /// <param name="buffer">buffer to send</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Send(byte[] buffer)
    {
      if (buffer == null)
        throw new ArgumentNullException("buffer");
      Send(buffer, 0, buffer.Length);
    }

    /// <summary>
    /// Send data using the stream
    /// </summary>
    /// <param name="buffer">Contains data to send</param>
    /// <param name="offset">Start position in buffer</param>
    /// <param name="size">number of bytes to send</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Send(byte[] buffer, int offset, int size)
    {
      if (offset + size > buffer.Length)
        throw new ArgumentOutOfRangeException("offset", offset, "offset + size is beyond end of buffer.");

      if (Stream != null && Stream.CanWrite)
      {
        try
        {
          Stream.Write(buffer, offset, size);
        }
        catch (IOException)
        {

        }
      }

    }

    /// <summary>
    /// The context have been disconnected.
    /// </summary>
    /// <remarks>
    /// Event can be used to clean up a context, or to reuse it.
    /// </remarks>
    public event EventHandler<DisconnectedEventArgs> Disconnected = delegate { };

    /// <summary>
    /// A request have been received in the context.
    /// </summary>
    public event EventHandler<RequestEventArgs> RequestReceived = delegate { };
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    private void Dispose(bool unmanaged)
    {
      if (unmanaged)
        if (Stream != null)
        {
          try
          {
            if (Stream.CanWrite)
              Stream.Flush();
            Cleanup();
          }
          catch (IOException)
          { }
        }
    }

    ~HttpClientContext()
    {
      Dispose();
    }
  }
}