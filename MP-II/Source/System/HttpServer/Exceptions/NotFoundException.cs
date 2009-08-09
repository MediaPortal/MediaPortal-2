using System;
using System.Net;

namespace HttpServer.Exceptions
{
  /// <summary>
  /// The requested resource was not found in the web server.
  /// </summary>
  public class NotFoundException : HttpException
  {
    /// <summary>
    /// Create a new exception
    /// </summary>
    /// <param name="message">message describing the error</param>
    /// <param name="inner">inner exception</param>
    public NotFoundException(string message, Exception inner) : base(HttpStatusCode.NotFound, message, inner)
    {
    }

    /// <summary>
    /// Create a new exception
    /// </summary>
    /// <param name="message">message describing the error</param>
    public NotFoundException(string message)
        : base(HttpStatusCode.NotFound, message)
    {
    }
  }
}