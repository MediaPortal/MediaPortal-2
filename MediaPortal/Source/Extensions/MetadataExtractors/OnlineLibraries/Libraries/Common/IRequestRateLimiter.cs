#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Common
{
  /// <summary>
  /// For downloads and web api calls we sometimes need to obey a request rate limit.
  /// Classes implementing this interface provide a mechanism to ensure
  /// that the request rate limit is obeyed.
  /// </summary>
  /// <example>
  /// try
  /// {
  ///   await RequestRateLimiter.RateLimit();
  ///   [send your request here]
  ///   [await your response(headers) here]
  /// }
  /// finally
  /// {
  ///   RequestRateLimiter.RequestDone();
  /// }
  /// </example>
  public interface IRequestRateLimiter
  {
    /// <summary>
    /// When a request rate limit needs to be obeyed, this method must be called before starting a download.
    /// </summary>
    /// <returns>A task that finishes when it is safe to download without infringing the request rate limit</returns>
    Task RateLimit();

    /// <summary>
    /// This method MUST be called once for every call to <see cref="RateLimit"/> when the respective request has
    /// reached the server; as we cannot determine this exactly, it should be called when a response (or ideally
    /// only the headers of the response) has been received.
    /// </summary>
    void RequestDone();
  }
}
