using System.Net.Sockets;

namespace HttpServer
{
  /// <summary>
  /// Class that receives Requests from a <see cref="IHttpClientContext"/>.
  /// </summary>
  public interface IHttpContextHandler
  {
    /// <summary>
    /// Client have been disconnected.
    /// </summary>
    /// <param name="client">Client that was disconnected.</param>
    /// <param name="error">Reason</param>
    /// <see cref="IHttpClientContext"/>
    void ClientDisconnected(IHttpClientContext client, SocketError error);

    /// <summary>
    /// Invoked when a client context have received a new HTTP request
    /// </summary>
    /// <param name="client">Client that received the request.</param>
    /// <param name="request">Request that was received.</param>
    /// <see cref="IHttpClientContext"/>
    void RequestReceived(IHttpClientContext client, IHttpRequest request);
  }
}