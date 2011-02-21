using System;
using System.Net;

namespace HttpServer.Exceptions
{
  /// <summary>
  /// The server encountered an unexpected condition which prevented it from fulfilling the request.
  /// </summary>
  public class InternalServerException : HttpException
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="InternalServerException"/> class.
    /// </summary>
    public InternalServerException()
        : base(
            HttpStatusCode.InternalServerError,
            "The server encountered an unexpected condition which prevented it from fulfilling the request.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InternalServerException"/> class.
    /// </summary>
    /// <param name="message">error message.</param>
    public InternalServerException(string message)
        : base(HttpStatusCode.InternalServerError, message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InternalServerException"/> class.
    /// </summary>
    /// <param name="message">error message.</param>
    /// <param name="inner">inner exception.</param>
    public InternalServerException(string message, Exception inner)
        : base(HttpStatusCode.InternalServerError, message, inner)
    {
    }
  }
}