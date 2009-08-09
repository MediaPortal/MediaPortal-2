using System.Net;

namespace HttpServer.Exceptions
{
  /// <summary>
  /// The server understood the request, but is refusing to fulfill it. 
  /// Authorization will not help and the request SHOULD NOT be repeated. 
  /// If the request method was not HEAD and the server wishes to make public why the request has not been fulfilled, 
  /// it SHOULD describe the reason for the refusal in the entity. If the server does not wish to make this information 
  /// available to the client, the status code 404 (Not Found) can be used instead.
  /// 
  /// Text taken from: http://www.submissionchamber.com/help-guides/error-codes.php
  /// </summary>
  public class ForbiddenException : HttpException
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenException"/> class.
    /// </summary>
    /// <param name="errorMsg">error message</param>
    public ForbiddenException(string errorMsg)
        : base(HttpStatusCode.Forbidden, errorMsg)
    {
    }
  }
}