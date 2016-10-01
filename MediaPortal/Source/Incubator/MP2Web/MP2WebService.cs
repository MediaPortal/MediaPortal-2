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
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.AspNetServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json.Serialization;

namespace MediaPortal.Plugins.MP2Web
{
  public class MP2WebService : IDisposable
  {
    #region Consts

    private const string WEB_APPLICATION_NAME = "MP2WebApp";
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
          services.AddCors(options =>
          {
            options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
          });

          services.AddMvc()
            // https://weblog.west-wind.com/posts/2016/Jun/27/Upgrading-to-ASPNET-Core-RTM-from-RC2
            // In the RTM release Microsoft has changed the default serialization behavior so that all properties are automatically
            // camel cased - or really changed to have a lower case first letter (ie. "BirthDate" becomes "birthDate" and
            // "Birthdate" becomes "birthdate")
            .AddJsonOptions(options =>
            {
              var resolver = options.SerializerSettings.ContractResolver;
              var res = resolver as DefaultContractResolver;
              if (res != null) res.NamingStrategy = null;  // <<!-- this removes the camelcasing
            })
            // We need to add a physical file provider to the plugin directory of AspNetServerSample to make sure that the Razor-engine
            // finds the view files (which need to be copied to the plugin directory via build.targets and need to be in the default
            // folders as defined by Razor: [PluginDirectory]\Views\[ControllerName]
            // ToDo: In later versions of MvcRazorOptions, FileProvider was made a List so we can .Clear() and .Add our FileProvider
            .AddRazorOptions(options => options.FileProviders.Add(new PhysicalFileProvider(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))))
            // This line is important to register the controllers from this webApp. If this is missing, no controller can be reached / no route gets generated
            .AddApplicationPart(this.GetType().Assembly);
        },
        configureApp: app =>
        {
          app.UseCors("AllowAll");
          // Add static files to the request pipeline.
          string resourcePathWww = Path.Combine(ASSEMBLY_PATH, "wwwroot").TrimEnd(Path.DirectorySeparatorChar);
          app.UseFileServer(new FileServerOptions
          {
            FileProvider = new PhysicalFileProvider(resourcePathWww),
            RequestPath = new PathString(""),
            EnableDirectoryBrowsing = false,
            EnableDefaultFiles = true         // Serve default files (default.{htm|html} and index.{htm|html}
          });
          app.UseMvc();
        },
        port: GetPort(),
        basePath: BASE_PATH);
    }

    #endregion

    #region Private methods

    private int GetPort()
    {
      var port = ServiceRegistration.Get<ISettingsManager>().Load<MP2WebSettings>().TcpPort;
      if (port < 1 || port > 65535)
      {
        ServiceRegistration.Get<ILogger>().Warn("MP2WebService: Tcp-Port {0} from settings is invalid; using default port {1}.", port, MP2WebSettings.DEFAULT_PORT);
        port = MP2WebSettings.DEFAULT_PORT;
      }
      return port;
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
