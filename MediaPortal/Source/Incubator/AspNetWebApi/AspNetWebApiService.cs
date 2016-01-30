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
using MediaPortal.Plugins.AspNetWebApi.Json;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;

namespace MediaPortal.Plugins.AspNetWebApi
{
  public class AspNetWebApiService : IDisposable
  {
    #region Consts

    private const string WEB_APPLICATION_NAME = "MP2WebApi";
    private const int PORT = 5555;
    private const string BASE_PATH = "/api/";

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

          services.AddMvc(options =>
          {
            var jsonOutputFormatter = new JsonOutputFormatter { SerializerSettings = { ContractResolver = new MediaItemResolver(httpContextAccessor) } };
            options.OutputFormatters.RemoveType<JsonOutputFormatter>();
            options.OutputFormatters.Insert(0, jsonOutputFormatter);
          });
          services.AddCors(options => options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
        },
        configureApp: app =>
        {
          // Cors must be started before Mvc - otherwise there are no Cors headers
          app.UseCors("AllowAll");
          app.UseMvc();
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
