/*
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

using System;
using System.Net;

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// A question to the server that did not produce an answer to read.
  /// </summary>
  /// <remarks>
  /// The distinction that matters to whoever sees this is between "nobody was there" and "somebody answered
  /// and said no", so <see cref="StatusCode"/> is a nullable: a refused connection and an HTTP <c>500</c> are
  /// different facts and a front end should not blur them. <see cref="NotReady"/> is broken out because the
  /// server answers <c>503</c> while its media library is still starting, and the right reaction to that is to
  /// ask again rather than to show an error.
  /// </remarks>
  public class ServerApiException : Exception
  {
    public ServerApiException(Uri url, string message, Exception? inner = null)
        : base(message, inner)
    {
      Url = url;
    }

    public ServerApiException(Uri url, HttpStatusCode statusCode, string body)
        : base(string.IsNullOrEmpty(body)
            ? string.Format("The server answered {0} ({1}) at {2}", (int) statusCode, statusCode, url)
            : string.Format("The server answered {0} ({1}) at {2}: {3}",
                (int) statusCode, statusCode, url, body))
    {
      Url = url;
      StatusCode = (int) statusCode;
    }

    /// <summary>
    /// The URL that was asked, so an error message can say where the front end was trying to talk to.
    /// </summary>
    public Uri Url { get; }

    /// <summary>
    /// The HTTP status, or <c>null</c> when the request never got an answer.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Whether the server said "not yet" instead of answering, which is a reason to retry and not to worry.
    /// </summary>
    public bool NotReady
    {
      get { return StatusCode == (int) HttpStatusCode.ServiceUnavailable; }
    }
  }
}
