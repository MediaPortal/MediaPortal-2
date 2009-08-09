using System;
using System.Net;

namespace HttpServer.Exceptions
{
  /// <summary>
  /// The request could not be understood by the server due to malformed syntax. 
  /// The client SHOULD NOT repeat the request without modifications.
  /// 
  /// Text taken from: http://www.submissionchamber.com/help-guides/error-codes.php
  /// </summary>
  public class BadRequestException : HttpException
  {
    /// <summary>
    /// Create a new bad request exception.
    /// </summary>
    /// <param name="errMsg">reason to why the request was bad.</param>
    public BadRequestException(string errMsg)
        : base(HttpStatusCode.BadRequest, errMsg)
    {
    }

    /// <summary>
    /// Create a new bad request exception.
    /// </summary>
    /// <param name="errMsg">reason to why the request was bad.</param>
    /// <param name="inner">inner exception</param>
    public BadRequestException(string errMsg, Exception inner)
        : base(HttpStatusCode.BadRequest, errMsg, inner)
    {
    }
  }
}