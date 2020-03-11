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

using System;
using System.Net;

namespace MediaPortal.Plugins.MP2Extended.Exceptions
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
