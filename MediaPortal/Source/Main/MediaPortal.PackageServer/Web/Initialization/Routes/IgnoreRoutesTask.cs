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

namespace MediaPortal.PackageServer.Initialization.Routes
{
  public class IgnoreRoutesTask : IPrioritizedConfigurationTask
  {
    public int Priority
    {
      get { return (int)TaskPriority.RouteIgnores; }
    }

    public void Configure()
    {
      RouteCollection routes = RouteTable.Routes;

      // match routes even when matching file exists
      routes.RouteExistingFiles = false;

      // ignore static content files (text, html)
      routes.IgnoreRoute("{file}.txt");
      routes.IgnoreRoute("{file}.htm");
      routes.IgnoreRoute("{file}.html");

      // ignore axd files such as asset, image, sitemap, etc.
      routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

      // ignore the static content directories (images, js, css)
      routes.IgnoreRoute("{*content}", new { scripts = @"(./)?content(/.*)?" });
      routes.IgnoreRoute("{*scripts}", new { scripts = @"(./)?scripts(/.*)?" });
      routes.IgnoreRoute("{*styles}", new { styles = @"(./)?styles(/.*)?" });
      routes.IgnoreRoute("{*images}", new { images = @"(./)?images(/.*)?" });
      routes.IgnoreRoute("{*uploads}", new { images = @"(./)?uploads(/.*)?" });
      // routes.IgnoreRoute( "{*content}", new { content = @"(.*)?/content(/.*)?" } );
      // routes.IgnoreRoute( "assets/{*pathInfo}" );

      // ignore the error directory which contains error pages
      routes.IgnoreRoute("ErrorPages/{*pathInfo}");

      // exclude favicon (google toolbar requests gif file as fav icon, so match both)
      routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.\w{3}(/.*)?" });
    }
  }
}