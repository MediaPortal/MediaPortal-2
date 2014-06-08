#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using System.Web.Routing;

namespace MediaPortal.PackageServer.Utility.Hooks
{
  public class LowerCaseRoute : Route
  {
    public LowerCaseRoute(string url, IRouteHandler routeHandler)
      : base(url, routeHandler)
    {
    }

    public LowerCaseRoute(string url, RouteValueDictionary defaults, IRouteHandler routeHandler)
      : base(url, defaults, routeHandler)
    {
    }

    public LowerCaseRoute(string url, RouteValueDictionary defaults, RouteValueDictionary constraints, IRouteHandler routeHandler)
      : base(url, defaults, constraints, routeHandler)
    {
    }

    public LowerCaseRoute(string url, RouteValueDictionary defaults, RouteValueDictionary constraints, RouteValueDictionary dataTokens, IRouteHandler routeHandler)
      : base(url, defaults, constraints, dataTokens, routeHandler)
    {
    }

    public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
    {
      VirtualPathData path = base.GetVirtualPath(requestContext, values);
      if (path != null)
      {
        path.VirtualPath = path.VirtualPath.ToLowerInvariant();
      }
      return path;
    }
  }
}