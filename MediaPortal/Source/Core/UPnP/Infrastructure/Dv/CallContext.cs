#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using Microsoft.Owin;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Dv
{
  public class CallContext
  {
    protected IOwinContext _httpContext;
    protected EndpointConfiguration _endpoint;

    public CallContext(IOwinRequest request, IOwinContext httpContext, EndpointConfiguration endpoint)
    {
      _httpContext = httpContext;
      _endpoint = endpoint;
    }

    public IOwinRequest Request
    {
      get { return _httpContext.Request; }
    }

    public IOwinContext HttpContext
    {
      get { return _httpContext; }
    }

    public EndpointConfiguration Endpoint
    {
      get { return _endpoint; }
    }

    public string RemoteAddress
    {
      get { return HttpServerHelper.GetRemoteAddress(Request); }
    }
  }
}
