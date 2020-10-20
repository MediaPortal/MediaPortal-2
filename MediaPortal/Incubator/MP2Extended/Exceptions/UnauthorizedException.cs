#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Net;

namespace MediaPortal.Plugins.MP2Extended.Exceptions
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
  /// http://www.submissionchamber.com/help-stringes/error-codes.php#sec10.4.2)
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
