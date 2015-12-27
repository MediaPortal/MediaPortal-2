#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using MediaPortal.Common;
using System;
using MediaPortal.Plugins.AspNetServer;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.StaticFiles;
using Microsoft.Extensions.DependencyInjection;

namespace MediaPortal.Plugins.AspNetServerSample
{
  public class AspNetServerSampleService : IDisposable
  {
    #region Consts

    private const string WEB_APPLICATION_NAME = "TestWebApp";
    private const int PORT = 5001;
    private const string BASE_PATH = "/";

    #endregion

    #region Constructor

    public AspNetServerSampleService()
    {
      ServiceRegistration.Get<IAspNetServerService>().TryStartWebApplicationAsync(
        webApplicationName: WEB_APPLICATION_NAME,
        configureServices: services =>
        {
          services.AddMvc();
        },
        configureApp: app =>
        {
          app.UseFileServer(new FileServerOptions
          {
            FileProvider = new PhysicalFileProvider(@"C:\"),
            RequestPath = new PathString("/StaticFiles"),
            EnableDirectoryBrowsing = true,
          });
          app.UseMvc();
          app.Run(context => context.Response.WriteAsync("Hello World"));
        },
        port: PORT,
        basePath: BASE_PATH);
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      ServiceRegistration.Get<IAspNetServerService>().TryStopWebApplicationAsync(WEB_APPLICATION_NAME).Wait();
    }

    #endregion
  }
}
