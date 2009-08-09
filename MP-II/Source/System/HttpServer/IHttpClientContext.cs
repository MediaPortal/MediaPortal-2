using System;
using System.Net;
using System.Net.Sockets;

namespace HttpServer
{
  /// <summary>
  /// Contains a connection to a browser/client.
  /// </summary>
  public interface IHttpClientContext
  {
    /// <summary>
    /// Using SSL or other encryption method.
    /// </summary>
    bool IsSecured { get; }

    /// <summary>
    /// Disconnect from client
    /// </summary>
    /// <param name="error">error to report in the <see cref="Disconnected"/> event.</param>
    void Disconnect(SocketError error);

    /// <summary>
    /// Send a response.
    /// </summary>
    /// <param name="httpVersion">Either <see cref="HttpHelper.HTTP10"/> or <see cref="HttpHelper.HTTP11"/></param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="reason">reason for the status code.</param>
    /// <param name="body">HTML body contents, can be null or empty.</param>
    /// <param name="contentType">A content type to return the body as, i.e. 'text/html' or 'text/plain', defaults to 'text/html' if null or empty</param>
    /// <exception cref="ArgumentException">If <paramref name="httpVersion"/> is invalid.</exception>
    void Respond(string httpVersion, HttpStatusCode statusCode, string reason, string body, string contentType);

    /// <summary>
    /// Send a response.
    /// </summary>
    /// <param name="httpVersion">Either <see cref="HttpHelper.HTTP10"/> or <see cref="HttpHelper.HTTP11"/></param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="reason">reason for the status code.</param>
    void Respond(string httpVersion, HttpStatusCode statusCode, string reason);

    /// <summary>
    /// Send a response.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    void Respond(string body);

    /// <summary>
    /// send a whole buffer
    /// </summary>
    /// <param name="buffer">buffer to send</param>
    /// <exception cref="ArgumentNullException"></exception>
    void Send(byte[] buffer);

    /// <summary>
    /// Send data using the stream
    /// </summary>
    /// <param name="buffer">Contains data to send</param>
    /// <param name="offset">Start position in buffer</param>
    /// <param name="size">number of bytes to send</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    void Send(byte[] buffer, int offset, int size);

    /// <summary>
    /// The context have been disconnected.
    /// </summary>
    /// <remarks>
    /// Event can be used to clean up a context, or to reuse it.
    /// </remarks>
    event EventHandler<DisconnectedEventArgs> Disconnected;

    /// <summary>
    /// A request have been received in the context.
    /// </summary>
    event EventHandler<RequestEventArgs> RequestReceived;
  }

  /// <summary>
  /// A <see cref="IHttpClientContext"/> have been disconnected.
  /// </summary>
  public class DisconnectedEventArgs : EventArgs
  {
    /// <summary>
    /// Gets reason to why client disconnected.
    /// </summary>
    public SocketError Error { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisconnectedEventArgs"/> class.
    /// </summary>
    /// <param name="error">Reason to disconnection.</param>
    public DisconnectedEventArgs(SocketError error)
    {
      Check.Require(error, "error");

      Error = error;
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public class RequestEventArgs : EventArgs
  {
    /// <summary>
    /// Gets received request.
    /// </summary>
    public IHttpRequest Request { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestEventArgs"/> class.
    /// </summary>
    /// <param name="request">The request.</param>
    public RequestEventArgs(IHttpRequest request)
    {
      Check.Require(request, "request");

      Request = request;
    }
  }
}