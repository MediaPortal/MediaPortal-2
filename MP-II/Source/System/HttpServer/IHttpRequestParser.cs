using System;
using HttpServer.Exceptions;
using HttpServer.Parser;

namespace HttpServer
{
  /// <summary>
  /// Event driven parser used to parse incoming HTTP requests.
  /// </summary>
  /// <remarks>
  /// The parser supports partial messages and keeps the states between
  /// each parsed buffer. It's therefore important that the parser gets
  /// <see cref="Clear"/>ed if a client disconnects.
  /// </remarks>
  public interface IHttpRequestParser
  {
    /// <summary>
    /// Current state in parser.
    /// </summary>
    RequestParserState CurrentState { get; }

    /// <summary>
    /// Parse partial or complete message.
    /// </summary>
    /// <param name="buffer">buffer containing incoming bytes</param>
    /// <param name="offset">where in buffer that parsing should start</param>
    /// <param name="count">number of bytes to parse</param>
    /// <returns>Unparsed bytes left in buffer.</returns>
    /// <exception cref="BadRequestException"><c>BadRequestException</c>.</exception>
    int Parse(byte[] buffer, int offset, int count);

    /// <summary>
    /// A request have been successfully parsed.
    /// </summary>
    event EventHandler RequestCompleted;

    /// <summary>
    /// More body bytes have been received.
    /// </summary>
    event EventHandler<BodyEventArgs> BodyBytesReceived;

    /// <summary>
    /// Request line have been received.
    /// </summary>
    event EventHandler<RequestLineEventArgs> RequestLineReceived;

    /// <summary>
    /// A header have been received.
    /// </summary>
    event EventHandler<HeaderEventArgs> HeaderReceived;

    /// <summary>
    /// Clear parser state.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets or sets the log writer.
    /// </summary>
    ILogWriter LogWriter { get; set; }
  }

  /// <summary>
  /// Current state in the parsing.
  /// </summary>
  public enum RequestParserState
  {
    /// <summary>
    /// Should parse the request line
    /// </summary>
    FirstLine,
    /// <summary>
    /// Searching for a complete header name
    /// </summary>
    HeaderName,
    /// <summary>
    /// Searching for colon after header name (ignoring white spaces)
    /// </summary>
    AfterName,
    /// <summary>
    /// Searching for start of header value (ignoring white spaces)
    /// </summary>
    Between,
    /// <summary>
    /// Searching for a complete header value (can span over multiple lines, as long as they are prefixed with one/more whitespaces)
    /// </summary>
    HeaderValue,

    /// <summary>
    /// Adding bytes to body
    /// </summary>
    Body
  }
}