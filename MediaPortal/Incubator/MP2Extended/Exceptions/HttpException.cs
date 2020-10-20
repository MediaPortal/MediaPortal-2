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
