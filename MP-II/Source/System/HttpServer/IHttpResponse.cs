using System;
using System.IO;
using System.Net;
using System.Text;

namespace HttpServer
{
  /// <summary>
  /// Response that is sent back to the web browser / client.
  /// 
  /// A response can be sent if different ways. The easiest one is
  /// to just fill the Body stream with content, everything else
  /// will then be taken care of by the framework. The default content-type
  /// is text/html, you should change it if you send anything else.
  /// 
  /// The second and slighty more complex way is to send the response
  /// as parts. Start with sending the header using the SendHeaders method and 
  /// then you can send the body using SendBody method, but do not forget
  /// to set ContentType and ContentLength before doing so.
  /// </summary>
  /// <example>
  /// public void MyHandler(IHttpRequest request, IHttpResponse response)
  /// {
  ///   
  /// }
  /// </example>
  public interface IHttpResponse
  {
    /// <summary>
    /// The body stream is used to cache the body contents
    /// before sending everything to the client. It's the simplest
    /// way to serve documents.
    /// </summary>
    Stream Body { get; set; }

    /// <summary>
    /// The chunked encoding modifies the body of a message in order to
    /// transfer it as a series of chunks, each with its own size indicator,
    /// followed by an OPTIONAL trailer containing entity-header fields. This
    /// allows dynamically produced content to be transferred along with the
    /// information necessary for the recipient to verify that it has
    /// received the full message.
    /// </summary>
    bool Chunked { get; set; }

    /// <summary>
    /// Kind of connection
    /// </summary>
    ConnectionType Connection { get; set; }

    /// <summary>
    /// Encoding to use when sending stuff to the client.
    /// </summary>
    /// <remarks>Default is UTF8</remarks>
    Encoding Encoding { get; set; }

    /// <summary>
    /// Number of seconds to keep connection alive
    /// </summary>
    /// <remarks>Only used if Connection property is set to ConnectionType.KeepAlive</remarks>
    int KeepAlive { get; set; }

    /// <summary>
    /// Status code that is sent to the client.
    /// </summary>
    /// <remarks>Default is HttpStatusCode.Ok</remarks>
    HttpStatusCode Status { get; set; }

    /// <summary>
    /// Information about why a specific status code was used.
    /// </summary>
    string Reason { get; set; }

    /// <summary>
    /// Size of the body. MUST be specified before sending the header,
    /// unless property Chunked is set to true.
    /// </summary>
    long ContentLength { get; set; }

    /// <summary>
    /// Kind of content in the body
    /// </summary>
    /// <remarks>Default is text/html</remarks>
    string ContentType { get; set; }

    /// <summary>
    /// Headers have been sent to the client-
    /// </summary>
    /// <remarks>You can not send any additional headers if they have already been sent.</remarks>
    bool HeadersSent { get; }

    /// <summary>
    /// The whole response have been sent.
    /// </summary>
    bool Sent { get; }

    /// <summary>
    /// Cookies that should be created/changed.
    /// </summary>
    ResponseCookies Cookies { get; }

    /// <summary>
    /// Add another header to the document.
    /// </summary>
    /// <param name="name">Name of the header, case sensitive, use lower cases.</param>
    /// <param name="value">Header values can span over multiple lines as long as each line starts with a white space. New line chars should be \r\n</param>
    /// <exception cref="InvalidOperationException">If headers already been sent.</exception>
    /// <exception cref="ArgumentException">If value conditions have not been met.</exception>
    /// <remarks>Adding any header will override the default ones and those specified by properties.</remarks>
    void AddHeader(string name, string value);

    /// <summary>
    /// Send headers and body to the browser.
    /// </summary>
    /// <exception cref="InvalidOperationException">If content have already been sent.</exception>
    void Send();

    /// <summary>
    /// Make sure that you have specified ContentLength and sent the headers first.
    /// </summary>
    /// <param name="buffer"></param>
    /// <exception cref="InvalidOperationException">If headers have not been sent.</exception>
    /// <see cref="IHttpResponse.SendHeaders"/>
    /// <param name="offset">offest of first byte to send</param>
    /// <param name="count">number of bytes to send.</param>
    /// <seealso cref="IHttpResponse.Send"/>
    /// <seealso cref="IHttpResponse.SendHeaders"/>
    /// <remarks>This method can be used if you want to send body contents without caching them first. This
    /// is recommended for larger files to keep the memory usage low.</remarks>
    void SendBody(byte[] buffer, int offset, int count);

    /// <summary>
    /// Make sure that you have specified ContentLength and sent the headers first.
    /// </summary>
    /// <param name="buffer"></param>
    /// <exception cref="InvalidOperationException">If headers have not been sent.</exception>
    /// <see cref="IHttpResponse.SendHeaders"/>
    /// <seealso cref="IHttpResponse.Send"/>
    /// <seealso cref="IHttpResponse.SendHeaders"/>
    /// <remarks>This method can be used if you want to send body contents without caching them first. This
    /// is recommended for larger files to keep the memory usage low.</remarks>
    void SendBody(byte[] buffer);

    /// <summary>
    /// Send headers to the client.
    /// </summary>
    /// <exception cref="InvalidOperationException">If headers already been sent.</exception>
    /// <seealso cref="IHttpResponse.AddHeader"/>
    /// <seealso cref="IHttpResponse.Send"/>
    /// <seealso cref="IHttpResponse.SendBody(byte[])"/>
    void SendHeaders();

    /// <summary>
    /// Redirect client to somewhere else using the 302 status code.
    /// </summary>
    /// <param name="uri">Destination of the redirect</param>
    /// <exception cref="InvalidOperationException">If headers already been sent.</exception>
    /// <remarks>You can not do anything more with the request when a redirect have been done. This should be your last
    /// action.</remarks>
    void Redirect(Uri uri);

    /// <summary>
    /// redirect to somewhere
    /// </summary>
    /// <param name="url">where the redirect should go</param>
    /// <remarks>
    /// No body are allowed when doing redirects.
    /// </remarks>
    void Redirect(string url);
  }

  /// <summary>
  /// Type of HTTP connection
  /// </summary>
  public enum ConnectionType
  {
    /// <summary>
    /// Connection is closed after each request-response
    /// </summary>
    Close,

    /// <summary>
    /// Connection is kept alive for X seconds (unless another request have been made)
    /// </summary>
    KeepAlive
  }
}