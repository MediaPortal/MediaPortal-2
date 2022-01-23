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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using Microsoft.Owin;
using System.Threading.Tasks;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.General;
using System.IO;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  public class WebResourceAccessModule : OwinMiddleware, IDisposable
  {
    public const string DEFAULT_PAGE = "Default.html";
    public const string LOGIN_PAGE = "Login.html";
    public const string RESOURCE_ACCESS_PATH = "/MPExtended";

    public WebResourceAccessModule(OwinMiddleware next) : base(next)
    {
    }

    /// <summary>
    /// Method that process the url
    /// </summary>
    public override async Task Invoke(IOwinContext context)
    {
      string path = null;
      var uri = context.Request.Uri.ToString();
      if (uri.EndsWith($"{RESOURCE_ACCESS_PATH}/", StringComparison.InvariantCultureIgnoreCase))
      {
        if (context.Authentication.User?.Identity?.IsAuthenticated ?? false)
          path = DEFAULT_PAGE;
        else
          path = LOGIN_PAGE;
      }

      if (path == null && 
        (!uri.ToLowerInvariant().Contains(($"{RESOURCE_ACCESS_PATH}/").ToLowerInvariant()) || string.IsNullOrEmpty(Path.GetExtension(uri))))
      {
        if (uri.ToLowerInvariant().Contains(RESOURCE_ACCESS_PATH.ToLowerInvariant()))
          Logger.Debug("MP2Extended: Ignored request for {0}", context.Request.Path);
        await Next.Invoke(context);
        return;
      }

      Logger.Debug("MP2Extended: Received request for {0}", context.Request.Path);
      await GetHtmlResource.ProcessAsync(context, path);
    }

    public void Dispose()
    {
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
