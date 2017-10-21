#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.AspNetServer;
using MediaPortal.Plugins.AspNetWebApi.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;

namespace MediaPortal.Plugins.AspNetWebApi
{
  public class AspNetWebApiService : IAspNetWebApiService, IDisposable
  {
    #region Consts

    private const string WEB_APPLICATION_NAME = "MP2WebApi";
    private const string BASE_PATH = "/api/";

    #endregion

    #region Private fields

    private int? _port;

    #endregion

    #region Constructor

    public AspNetWebApiService()
    {
      ServiceRegistration.Get<IAspNetServerService>().TryStartWebApplicationAsync(
        webApplicationName: WEB_APPLICATION_NAME,
        configureServices: services =>
        {
          // HttpContextAccessor is not created automatically because it is not needed necessarily.
          // We need it to inject it into the MediaItemResolver so that it can inject it into the MediaItemJsonConverter.
          // It needs to be registered as a service so that it is automatically injected into the HttpContextFactory.
          var httpContextAccessor = new HttpContextAccessor();
          services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

          services.AddMemoryCache();

          services.AddMvc()
            .AddJsonOptions(options =>
            {
              options.SerializerSettings.ContractResolver = new MediaItemResolver(httpContextAccessor);
              // https://weblog.west-wind.com/posts/2016/Jun/27/Upgrading-to-ASPNET-Core-RTM-from-RC2
              // In the RTM release Microsoft has changed the default serialization behavior so that all properties are automatically
              // camel cased - or really changed to have a lower case first letter (ie. "BirthDate" becomes "birthDate" and
              // "Birthdate" becomes "birthdate")
              var resolver = options.SerializerSettings.ContractResolver;
              var res = (DefaultContractResolver)resolver;
              res.NamingStrategy = null;  // <<!-- this removes the camelcasing
            })
            // This line is important to register the controllers from this webApp. If this is missing, no controller can be reached / no route gets generated
            .AddApplicationPart(this.GetType().Assembly);
          services.AddCors(options => options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
        },
        configureApp: app =>
        {
          // Cors must be started before Mvc - otherwise there are no Cors headers
          app.UseCors("AllowAll");
          app.UseMvc();
        },
        port: Port,
        basePath: BASE_PATH);
    }

    #endregion

    #region IAspNetWebApiService implmentation

    public int Port
    {
      get
      {
        if (_port.HasValue)
          return _port.Value;
        var port = ServiceRegistration.Get<ISettingsManager>().Load<AspNetWebApiSettings>().TcpPort;
        if (port < 1 || port > 65535)
        {
          ServiceRegistration.Get<ILogger>().Warn("AspNetWebApiService: Tcp-Port {0} from settings is invalid; using default port {1}.", port, AspNetWebApiSettings.DEFAULT_PORT);
          port = AspNetWebApiSettings.DEFAULT_PORT;
        }
        _port = port;
        return port;
      }
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
