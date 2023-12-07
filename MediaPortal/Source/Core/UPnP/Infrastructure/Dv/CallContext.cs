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

using UPnP.Infrastructure.Http;
#if NET5_0_OR_GREATER
using Microsoft.AspNetCore.Http;
#else
using Microsoft.Owin;
#endif

namespace UPnP.Infrastructure.Dv
{
  public class CallContext
  {
    protected EndpointConfiguration _endpoint;
#if NET5_0_OR_GREATER
    protected HttpContext _httpContext;

    public CallContext(HttpRequest request, HttpContext httpContext, EndpointConfiguration endpoint)
    {
      _httpContext = httpContext;
      _endpoint = endpoint;
    }

    public HttpRequest Request
    {
      get { return _httpContext?.Request; }
    }

    public HttpContext HttpContext
    {
      get { return _httpContext; }
    }
#else
    protected IOwinContext _httpContext;

    public CallContext(IOwinRequest request, IOwinContext httpContext, EndpointConfiguration endpoint)
    {
      _httpContext = httpContext;
      _endpoint = endpoint;
    }

    public IOwinRequest Request
    {
      get { return _httpContext?.Request; }
    }

    public IOwinContext HttpContext
    {
      get { return _httpContext; }
    }
#endif

    public EndpointConfiguration Endpoint
    {
      get { return _endpoint; }
    }

    public string RemoteAddress
    {
      get { return Request?.GetRemoteAddress(); }
    }
  }
}
