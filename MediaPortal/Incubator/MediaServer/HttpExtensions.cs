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
#if NET5_0_OR_GREATER
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
#else
using Microsoft.Owin;
#endif

namespace MediaPortal.Extensions.MediaServer
{
  public static class HttpExtensions
  {
#if NET5_0_OR_GREATER
    public static string GetRemoteAddress(this HttpRequest request)
    {
      return request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    public static Uri GetUri(this HttpRequest request)
    {
      return new Uri(request.GetEncodedUrl());
    }

    public static void SetReasonPhrase(this HttpResponse response, string reasonPhrase)
    {
      response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = reasonPhrase;
    }
#else
    public static string GetRemoteAddress(this IOwinRequest request)
    {
      return request.RemoteIpAddress;
    }

    public static Uri GetUri(this IOwinRequest request) 
    {
      return request.Uri;
    }

    public static void SetReasonPhrase(this IOwinResponse response, string reasonPhrase)
    {
      response.ReasonPhrase = reasonPhrase;
    }
#endif
  }
}
