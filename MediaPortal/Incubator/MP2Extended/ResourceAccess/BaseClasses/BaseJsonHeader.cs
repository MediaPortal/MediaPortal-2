﻿#region Copyright (C) 2007-2017 Team MediaPortal

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
using System.Text;
using System.Web;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.BaseClasses
{
  class BaseJsonHeader
  {
    internal void SendHeader(HttpResponse response, int contentLength)
    {
      //response.Status = HttpStatusCode.OK;
      //response.Encoding = Encoding.UTF8;
      //response.ContentType = "application/json; charset=UTF-8";
      //response.ContentLength = contentLength;
      //response.SendHeaders();
    }
  }
}
