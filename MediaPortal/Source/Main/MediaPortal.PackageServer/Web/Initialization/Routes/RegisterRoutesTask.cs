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
using MediaPortal.PackageServer.Initialization.Core;
using MediaPortal.PackageServer.Utility.Extensions;

namespace MediaPortal.PackageServer.Initialization.Routes
{
  public class RegisterRoutesTask : IPrioritizedConfigurationTask
  {
    public int Priority
    {
      get { return (int)TaskPriority.Routes; }
    }

    public void Configure()
    {
      RouteCollection routes = RouteTable.Routes;

      // register attribute-based routes first 
      routes.MapMvcAttributeRoutes();

      // register areas (that do not use attribute-based config) second 
      AreaRegistration.RegisterAllAreas();

      // register "classic" routes last

      #region Author Controller

      //routes.MapRouteLowerCase(
      //  name: "package-publish",
      //  url: "package/publish",
      //  defaults: new { controller = "author", action = "publish" },
      //  constraints: null,
      //  dataTokens: new { area = "", namespaces = new[] { "MediaPortal.PackageServer.Controllers" } } );

      //routes.MapRouteLowerCase(
      //  name: "package-recall",
      //  url: "package/recall",
      //  defaults: new { controller = "author", action = "recall" },
      //  constraints: null,
      //  dataTokens: new { area = "", namespaces = new[] { "MediaPortal.PackageServer.Controllers" } } ); 

      #endregion

      #region Admin Controller

      //routes.MapRouteLowerCase( 
      //  name: "admin-create-user",
      //  url: "user/create",
      //  defaults: new { controller = "user", action = "create" },
      //  constraints: null,
      //  dataTokens: new { area = "", namespaces = new[] { "MediaPortal.PackageServer.Controllers" } } );

      //routes.MapRouteLowerCase( 
      //  name: "admin-revoke-user",
      //  url: "user/revoke",
      //  defaults: new { controller = "user", action = "revoke" },
      //  constraints: null,
      //  dataTokens: new { area = "", namespaces = new[] { "MediaPortal.PackageServer.Controllers" } } );

      #endregion

      #region Package Controller

      //routes.MapRouteLowerCase( 
      //  name: "package-list",
      //  url: "packages",
      //  defaults: new { controller = "package", action = "list" },
      //  constraints: null,
      //  dataTokens: new { area = "", namespaces = new[] { "MediaPortal.PackageServer.Controllers" } } );

      //routes.MapRouteLowerCase( 
      //  name: "package-releases",
      //  url: "packages/{id}/releases",
      //  defaults: new { controller = "package", action = "releases" },
      //  constraints: new { id = @"\d+" },
      //  dataTokens: new { area = "", namespaces = new[] { "MediaPortal.PackageServer.Controllers" } } );

      #endregion

      #region Default

      routes.MapRouteLowerCase(
        name: "default",
        url: "{controller}/{action}/{id}",
        defaults: new { controller = "home", action = "index", id = UrlParameter.Optional },
        dataTokens: new { area = "", namespaces = new[] { "MediaPortal.PackageServer.Controllers" } });

      #endregion
    }
  }
}