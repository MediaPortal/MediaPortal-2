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
using System.IO;
using System.Reflection;
using MediaPortal.Plugins.AspNetServer;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.StaticFiles;
using Microsoft.Extensions.DependencyInjection;

namespace MediaPortal.Plugins.MP2Web
{
  public class MP2WebService : IDisposable
  {
    #region Consts

    private const string WEB_APPLICATION_NAME = "MP2WebApp";
    private const int PORT = 8080;
    private const string BASE_PATH = "/";
    private static readonly Assembly ASS = Assembly.GetExecutingAssembly();
    internal static readonly string ASSEMBLY_PATH = Path.GetDirectoryName(ASS.Location);

    #endregion

    #region Constructor

    public MP2WebService()
    {
      ServiceRegistration.Get<IAspNetServerService>().TryStartWebApplicationAsync(
        webApplicationName: WEB_APPLICATION_NAME,
        configureServices: services =>
        {
          services.AddMvc()
            // We need to add a physical file provider to the plugin directory of AspNetServerSample to make sure that the Razor-engine
            // finds the view files (which need to be copied to the plugin directory via build.targets and need to be in the default
            // folders as defined by Razor: [PluginDirectory]\Views\[ControllerName]
            // ToDo: In later versions of MvcRazorOptions, FileProvider was made a List so we can .Clear() and .Add our FileProvider
            .AddRazorOptions(options => options.FileProvider = new PhysicalFileProvider(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)));
        },
        configureApp: app =>
        {
          // Add static files to the request pipeline.
          string resourcePathWww = Path.Combine(ASSEMBLY_PATH, "wwwroot").TrimEnd(Path.DirectorySeparatorChar);
          app.UseStaticFiles(new StaticFileOptions
          {
            FileProvider = new PhysicalFileProvider(resourcePathWww),
            RequestPath = new PathString("")
          });
          app.UseMvc();
          app.Run(context => context.Response.WriteAsync("Hello MP2Web"));
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
