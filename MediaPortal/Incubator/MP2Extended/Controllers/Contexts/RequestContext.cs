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
using System.Net.Http;
using System.Security.Claims;
#if NET5_0_OR_GREATER
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
#else
using Microsoft.Owin;
#endif

namespace MediaPortal.Plugins.MP2Extended.Controllers.Contexts
{
#if NET5_0_OR_GREATER
  public class RequestContext
  {
    protected HttpContext _context;

    public RequestContext(HttpContext context)
    {
      _context = context;
    }

    public HttpContext Context => _context;

    public HttpRequest Request => _context.Request;

    public HttpResponse Response => _context.Response;

    public ClaimsPrincipal User => _context.User;
  }

  public static class Extensions
  {
    public static RequestContext ToRequestContext(this HttpContext context)
    {
      return new RequestContext(context);
    }

    public static Uri GetUri(this HttpRequest request) 
    {
      return new Uri(request.GetEncodedUrl());
    }

    public static string GetRemoteIpAddress(this RequestContext context) 
    {
      return context.Context.Connection?.RemoteIpAddress?.ToString();
    }

    public static void SetReasonPhrase(this HttpResponse response, string reasonPhrase)
    {
      response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = reasonPhrase;
    }
  }
#else
  public class RequestContext
  {
    protected IOwinContext _context;

    public RequestContext(IOwinContext context)
    {
      _context = context;
    }

    public IOwinContext Context => _context;

    public IOwinRequest Request => _context.Request;

    public IOwinResponse Response => _context.Response;

    public ClaimsPrincipal User => _context.Authentication.User;
  }

  public static class Extensions
  {
    public static RequestContext ToRequestContext(this IOwinContext context)
    {
      return new RequestContext(context);
    }

    public static Uri GetUri(this IOwinRequest request)
    {
      return request.Uri;
    }

    public static string GetRemoteIpAddress(this RequestContext context)
    {
      return context.Request.RemoteIpAddress;
    }

    public static void SetReasonPhrase(this IOwinResponse response, string reasonPhrase)
    {
      response.ReasonPhrase = reasonPhrase;
    }

    public static Uri GetDisplayUrl(this HttpRequestMessage requestMessage)
    {
      return requestMessage.GetOwinContext().Request.Uri;
    }
  }
#endif
}
