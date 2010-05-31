using System;
using System.Net;

namespace HttpServer.Exceptions
{
  /// <summary>
  /// All HTTP based exceptions will derive this class.
  /// </summary>
  public class HttpException : Exception
  {
    private readonly HttpStatusCode _code;

    /// <summary>
    /// Create a new HttpException
    /// </summary>
    /// <param name="code">http status code (sent in the response)</param>
    /// <param name="message">error description</param>
    public HttpException(HttpStatusCode code, string message) : base(code + ": " + message)
    {
      _code = code;
    }

    /// <summary>
    /// Create a new HttpException
    /// </summary>
    /// <param name="code">http status code (sent in the response)</param>
    /// <param name="message">error description</param>
    /// <param name="inner">inner exception</param>
    public HttpException(HttpStatusCode code, string message, Exception inner)
        : base(code + ": " + message, inner)
    {
      _code = code;
    }

    /// <summary>
    /// status code to use in the response.
    /// </summary>
    public HttpStatusCode HttpStatusCode
    {
      get { return _code; }
    }
  }
}