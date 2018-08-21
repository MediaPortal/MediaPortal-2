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

using System.Net;

namespace MediaPortal.Plugins.MP2Extended.Exceptions
{
  /// <summary>
  /// The server understood the request, but is refusing to fulfill it. 
  /// Authorization will not help and the request SHOULD NOT be repeated. 
  /// If the request method was not HEAD and the server wishes to make public why the request has not been fulfilled, 
  /// it SHOULD describe the reason for the refusal in the entity. If the server does not wish to make this information 
  /// available to the client, the status code 404 (Not Found) can be used instead.
  /// 
  /// Text taken from: http://www.submissionchamber.com/help-stringes/error-codes.php
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
