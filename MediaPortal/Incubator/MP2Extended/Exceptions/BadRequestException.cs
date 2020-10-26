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
  /// The request could not be understood by the server due to malformed syntax. 
  /// The client SHOULD NOT repeat the request without modifications.
  /// 
  /// Text taken from: http://www.submissionchamber.com/help-stringes/error-codes.php
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
