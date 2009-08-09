using System;
using System.Net;

namespace HttpServer.Exceptions
{
  /// <summary>
  /// The request requires user authentication. The response MUST include a 
  /// WWW-Authenticate header field (section 14.47) containing a challenge 
  /// applicable to the requested resource. 
  /// 
  /// The client MAY repeat the request with a suitable Authorization header 
  /// field (section 14.8). If the request already included Authorization 
  /// credentials, then the 401 response indicates that authorization has been 
  /// refused for those credentials. If the 401 response contains the same challenge 
  /// as the prior response, and the user agent has already attempted authentication 
  /// at least once, then the user SHOULD be presented the entity that was given in the response, 
  /// since that entity might include relevant diagnostic information. 
  /// 
  /// HTTP access authentication is explained in rfc2617:
  /// http://www.ietf.org/rfc/rfc2617.txt
  /// 
  /// (description is taken from 
  /// http://www.submissionchamber.com/help-guides/error-codes.php#sec10.4.2)
  /// </summary>
  public class UnauthorizedException : HttpException
  {
    /// <summary>
    /// Create a new unauhtorized exception.
    /// </summary>
    /// <seealso cref="UnauthorizedException"/>
    public UnauthorizedException()
        : base(HttpStatusCode.Unauthorized, "The request requires user authentication.")
    {
    }

    /// <summary>
    /// Create a new unauhtorized exception.
    /// </summary>
    /// <param name="message">reason to why the request was unauthorized.</param>
    /// <param name="inner">inner exception</param>
    public UnauthorizedException(string message, Exception inner)
        : base(HttpStatusCode.Unauthorized, message, inner)
    {
    }

    /// <summary>
    /// Create a new unauhtorized exception.
    /// </summary>
    /// <param name="message">reason to why the request was unauthorized.</param>
    public UnauthorizedException(string message)
        : base(HttpStatusCode.Unauthorized, message)
    {
    }
  }
}