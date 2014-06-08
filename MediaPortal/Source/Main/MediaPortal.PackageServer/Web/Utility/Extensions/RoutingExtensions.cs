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

using System.Web.Mvc;
using System.Web.Routing;
using MediaPortal.PackageServer.Utility.Hooks;

namespace MediaPortal.PackageServer.Utility.Extensions
{
  public static class RoutingExtensions
  {
    public static Route MapRouteLowerCase(this RouteCollection routes, string name, string url, object defaults = null, object constraints = null, object dataTokens = null)
    {
      Route route = new LowerCaseRoute(url, new MvcRouteHandler())
      {
        DataTokens = new RouteValueDictionary(dataTokens),
        Defaults = new RouteValueDictionary(defaults),
        Constraints = new RouteValueDictionary(constraints)
      };
      routes.Add(name, route);
      return route;
    }

    public static Route MapRouteLowerCase(this AreaRegistrationContext context, string name, string url, object defaults = null, object constraints = null, object dataTokens = null)
    {
      return context.Routes.MapRouteLowerCase(name, url, defaults, constraints, dataTokens);
    }
  }
}