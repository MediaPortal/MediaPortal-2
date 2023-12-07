#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
#else
using Microsoft.Owin;
using System.IO;
using System.Net.Http;
#endif

namespace UPnP.Infrastructure.Http
{
  /// <summary>
  /// This class acts as a shim between Owin and AspNetCore implementations of http requests to help
  /// provide a unified interface to some of the common methods/properties needed when handling requests
  /// to minimise the code changes required between .net framework (using Owin) and .net 6 (using AspNetCore).
  /// </summary>
  public static class ExtensionMethods
  {
#if NET5_0_OR_GREATER
    public static Uri GetUri(this HttpRequest request)
    {
      return new Uri(request.GetEncodedUrl());
    }

    public static void SetReasonPhrase(this HttpContext context, string reasonPhrase) 
    { 
      context.Features.Get<IHttpResponseFeature>().ReasonPhrase = reasonPhrase;
    }

    /// <summary>
    /// Given an HTTP request, this method returns the client's IP address.
    /// </summary>
    /// <param name="request">Http client request.</param>
    /// <returns><see cref="string"/> instance containing the client's IP address. The returned IP address can be
    /// parsed by calling <see cref="IPAddress.Parse"/>.</returns>
    public static string GetRemoteAddress(this HttpRequest request)
    {
      return request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }
#else
    public static Uri GetUri(this IOwinRequest request)
    {
      return request.Uri;
    }

    public static string GetEncodedPathAndQuery(this IOwinRequest request)
    {
      return request.Uri.PathAndQuery;
    }

    public static void SetReasonPhrase(this IOwinContext context, string reasonPhrase) 
    { 
      context.Response.ReasonPhrase = reasonPhrase;
    }

    public static Stream ReadAsStream(this HttpContent content)
    {
      return content.ReadAsStreamAsync().Result;
    }

    /// <summary>
    /// Given an HTTP request, this method returns the client's IP address.
    /// </summary>
    /// <param name="request">Http client request.</param>
    /// <returns><see cref="string"/> instance containing the client's IP address. The returned IP address can be
    /// parsed by calling <see cref="IPAddress.Parse"/>.</returns>
    public static string GetRemoteAddress(this IOwinRequest request)
    {
      return request.RemoteIpAddress;
    }
#endif
  }
}
