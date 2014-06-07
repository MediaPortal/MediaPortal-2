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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;

namespace MediaPortal.Plugins.WebServices.OwinResourceServer
{
  using AppFunc = Func<IDictionary<string, object>, Task>;

  /// <summary>
  /// Owin Middleware that translates a query string into a path string
  /// </summary>
  /// <remarks>
  /// The <see cref="StaticFileMiddleware"/> can be configured with a custom implementation of <see cref="IFileSystem"/>.
  /// We do this with <see cref="ResourceServerFileSystem"/> to let the <see cref="StaticFileMiddleware"/> serve resources
  /// via MP2's ResourceAccessors. However, <see cref="StaticFileMiddleware"/> only passes the path string of an Uri as
  /// parameter to the <see cref="IFileSystem"/> implementation so that it has to identify a resource based on this path
  /// string. The query string is not handed over to the <see cref="IFileSystem"/> implementation. Currently, the MP2 Clients
  /// put the information on the requested resource into the query string of the request in the form:
  ///   ?ResourcePath=[ResourcePath]
  /// If the request Uri processed by this middleware contains a query parameter in the above form, it translates it into a
  /// path string in the form:
  ///   /[ResourcePath]
  /// The original context.Request.Path property is replaced by this path string, which is then passed to the <see cref="IFileSystem"/>
  /// implementation by the <see cref="StaticFileMiddleware"/>
  /// This middleware must therefore be started before a <see cref="StaticFileMiddleware"/> and the latter must be configured
  /// to use the <see cref="ResourceServerFileSystem"/>.
  /// </remarks>
  class ResourceQueryToPathMiddleware
  {
    private readonly AppFunc _next;

    /// <summary>
    /// Creates a new instance of the ResourceQueryToPathMiddleware
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    public ResourceQueryToPathMiddleware(AppFunc next)
    {
      if (next == null)
        throw new ArgumentNullException("next");
      _next = next;
    }

    /// <summary>
    /// Processes a request to determine if the query string matches a known resource
    /// and if so, translates the query string into a path string.
    /// </summary>
    /// <param name="environment">Owin environment dictionary which stores state information about the request, response and relevant server state.</param>
    /// <returns></returns>
    public Task Invoke(IDictionary<string, object> environment)
    {
      IOwinContext context = new OwinContext(environment);

      ResourcePath resourcePath;
      if(!ResourceHttpAccessUrlUtils.ParseResourceURI(context.Request.Uri, out resourcePath))
        ServiceRegistration.Get<ILogger>().Warn("OwinResourceServer: Wrong URI: '{0}'", context.Request.Uri);
      else
      {
        // The Uri property of the Request is concatenated inter alia from the Path property
        // on each access. If we want to log the original Uri, we have to get it before changing the Path
        var uri = context.Request.Uri;
        
        // We need to put a slash in front of your Path to make sure that the PathString class accepts is as
        // a valid path. The slash is filtered out again by the ResourceServerFileSystem class.
        context.Request.Path = new PathString("/" + resourcePath);
        ServiceRegistration.Get<ILogger>().Debug("OwinResourceServer: URI: '{0}' translated to PathString '{1}'", uri, context.Request.Path.Value);
      }
      return _next(environment);
    }
  }
}
