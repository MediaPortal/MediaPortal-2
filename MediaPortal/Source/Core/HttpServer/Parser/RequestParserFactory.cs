using HttpServer.Parser;

namespace HttpServer
{
  /// <summary>
  /// Creates request parsers when needed.
  /// </summary>
  public class RequestParserFactory : IRequestParserFactory
  {
    /// <summary>
    /// Create a new request parser.
    /// </summary>
    /// <param name="logWriter">Used when logging should be enabled.</param>
    /// <returns>A new request parser.</returns>
    public IHttpRequestParser CreateParser(ILogWriter logWriter)
    {
      return new HttpRequestParser(logWriter);
    }
  }

  /// <summary>
  /// Creates request parsers when needed.
  /// </summary>
  public interface IRequestParserFactory
  {
    /// <summary>
    /// Create a new request parser.
    /// </summary>
    /// <param name="logWriter">Used when logging should be enabled.</param>
    /// <returns>A new request parser.</returns>
    IHttpRequestParser CreateParser(ILogWriter logWriter);
  }
}