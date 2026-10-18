#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Net;
using System.Net.Sockets;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Auth;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using UPnP.Infrastructure.Dv;
#if NET5_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using UPnP.Infrastructure.Http;
#else
using MediaPortal.Common.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Security.OAuth;
using Owin;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using HttpServer = UPnP.Infrastructure.Http.HttpServer;
#endif

namespace MediaPortal.Common.Services.ResourceAccess
{
  public class ResourceServer : IResourceServer, IDisposable
  {
    public const string MEDIAPORTAL_AUTHENTICATION_TYPE = "MediaPortal";

    /// <summary>
    /// URL path of the health endpoint, below the resource server's base path
    /// (<c>/MediaPortal/Server</c> on a server, <c>/MediaPortal/Client</c> on a client - the base path is
    /// resolved from the running system, so the endpoint is built from it at startup).
    /// </summary>
    /// <remarks>
    /// This is the one address of the server that answers without plugins, users, tokens or the database
    /// being involved. A service manager (systemd, Android) needs such an address to tell "starting" from
    /// "ready", and the CI gates use it as the proof that the platform's HTTP listener really serves the
    /// endpoint it registered - "the listener started" is not the same statement as "the server answers".
    /// </remarks>
    public const string HEALTH_URL_SUFFIX = "/health";

#if NET5_0_OR_GREATER
    /// <summary>
    /// Writes the answer of the <see cref="HEALTH_URL_SUFFIX"/> endpoint.
    /// </summary>
    public static Task WriteHealthResponse(HttpContext context)
    {
      context.Response.StatusCode = (int)HttpStatusCode.OK;
      context.Response.ContentType = "application/json; charset=utf-8";
      return context.Response.WriteAsync(HealthJson(context.Request));
    }

    /// <summary>
    /// The body of the health answer as JSON. Kept separate from the response so that it can be asserted
    /// without a request pipeline.
    /// </summary>
    /// <remarks>
    /// It reports what the request looked like from the server's side, because the two HTTP hosts do not
    /// agree about the endpoint: http.sys puts the prefix it registered into <c>PathBase</c> and leaves
    /// <c>Path</c> relative to it, while the shared Kestrel listener keeps the prefix inside <c>Path</c>.
    /// Printing the two values is how each platform's gate measures that instead of assuming it.
    /// </remarks>
    public static string HealthJson(HttpRequest request)
    {
      return "{\"name\":\"MediaPortal 2 Server\",\"version\":\"" +
             typeof(ResourceServer).Assembly.GetName().Version +
             "\",\"utc\":\"" + DateTime.UtcNow.ToString("O") +
             "\",\"pathBase\":\"" + WebUtility.HtmlEncode(request.PathBase.Value) +
             "\",\"path\":\"" + WebUtility.HtmlEncode(request.Path.Value) + "\"}";
    }
#endif

    protected readonly List<Type> _middleWares = new List<Type>();

    /// <summary>
    /// Types whose assemblies MVC searches for API controllers, in addition to the assemblies of the
    /// resource access modules. See <see cref="AddHttpControllerAssembly"/>.
    /// </summary>
    protected readonly List<Type> _controllerTypes = new List<Type>();
    protected IDisposable _httpServer;
    protected int _serverPort = UPnPServer.DEFAULT_UPNP_AND_SERVICE_PORT_NUMBER;
    protected readonly object _syncObj = new object();
    protected string _servicePrefix;

    public ResourceServer()
    {
      AddHttpModule(typeof(ResourceAccessModule));
      //CreateAndStartServer();
    }

#if NET5_0_OR_GREATER
    /// <summary>
    /// Configures the services behind the resource server's HTTP endpoints.
    /// </summary>
    /// <remarks>
    /// Split out of <see cref="CreateAndStartServer"/> and public because the order of these services is part of
    /// what has to be right: a test builds the same pipeline in front of an in-memory listener and drives it with
    /// HTTP requests, which is the only way to find out that authentication runs before authorization without
    /// starting a real port. Both HTTP hosts - http.sys and <see cref="SharedHttpPortHost"/> - hand the same two
    /// delegates their services, so the test measures the configuration that is really used.
    /// </remarks>
    public void ConfigureApiServices(IServiceCollection services)
    {
      // The bearer scheme that turns "Authorization: ******" into HttpContext.User. Without it
      // MediaPortalAuthorizeAttribute answers 403 to every request on this target, because the Owin OAuth
      // middleware that used to do this job does not exist here.
      services.AddMediaPortalTokenAuthentication();

      var mvc = services.AddControllers();
      AddApplicationParts(mvc, _middleWares, _controllerTypes);
    }

    /// <summary>
    /// Builds the request pipeline of the resource server's HTTP endpoints. See
    /// <see cref="ConfigureApiServices"/> for why this is a method of its own.
    /// </summary>
    public void ConfigureApiPipeline(IApplicationBuilder app)
    {
      app.UseRouting();

      // Order matters: UseAuthentication populates HttpContext.User from the bearer token, UseAuthorization
      // reads it. Leaving the first out is what made every [MediaPortalAuthorize] endpoint answer 403.
      app.UseAuthentication();
      app.UseAuthorization();
      // Both HTTP hosts hand the request over below the prefix: http.sys reports it as PathBase, and
      // SharedHttpPortHost does the same on purpose (measured with the health endpoint, which prints the
      // two values). The route template is therefore relative to the prefix on every platform - it used
      // to contain "/MediaPortal/Server", which could only ever match when the prefix was left in Path.
      app.UseEndpoints(endpoints =>
      {
        endpoints.MapGet(HEALTH_URL_SUFFIX, WriteHealthResponse);
        endpoints.MapControllerRoute("DefaultApi", "api/{controller}/{id?}");
        endpoints.MapControllers();
      });

      // Configure MiddleWare
      foreach (Type middleWareType in _middleWares)
      {
        app.UseMiddleware(middleWareType);
      }
    }
#endif

    private void CreateAndStartServer()
    {
      ServerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ServerSettings>();
      List<string> filters = settings.IPAddressBindingsList;
      _serverPort = UPnPServer.DEFAULT_UPNP_AND_SERVICE_PORT_NUMBER;
      _servicePrefix = ResourceHttpAccessUrlUtils.RESOURCE_SERVER_BASE_PATH;

      lock (_syncObj)
      {
        if (_httpServer != null) //Already started
          return;

#if NET5_0_OR_GREATER
        var urls = HttpServer.BuildUrls(_servicePrefix, filters, _serverPort);

        // Both listeners take these two as delegates: the services behind the endpoints and the
        // pipeline in front of them are the same whether http.sys or the portable host serves the port, so
        // one configuration is measured and used. They live in ConfigureApiServices and
        // ConfigureApiPipeline, which a test also drives directly against an in-memory listener.
        Action<IServiceCollection> configureServices = ConfigureApiServices;
        Action<IApplicationBuilder> configurePipeline = ConfigureApiPipeline;

        if (OperatingSystem.IsWindows())
        {
          IWebHost host = HttpServer.CreateWebHostBuilder(urls)
            .ConfigureServices(configureServices)
            .Configure(configurePipeline)
            .Build();
          host.Start();
          _httpServer = host;
        }
        else
        {
          // http.sys is not available, so this endpoint joins the shared listener of its port. It is
          // registered before the UPnP endpoints, which is required for endpoints that need services.
          _httpServer = SharedHttpPortHost.Start(urls, configureServices, configurePipeline);
        }
#else
        var startOptions = HttpServer.BuildStartOptions(_servicePrefix, filters, _serverPort);
        _httpServer = WebApp.Start(startOptions, builder =>
        {
          // Configure OAuth Authorization Server
          builder.UseOAuthAuthorizationServer(new OAuthAuthorizationServerOptions
          {
            AuthenticationType = MEDIAPORTAL_AUTHENTICATION_TYPE,
            TokenEndpointPath = new PathString("/Token"),
            ApplicationCanDisplayErrors = true,
            AuthorizationCodeExpireTimeSpan = TimeSpan.FromDays(7),
#if DEBUG
            AllowInsecureHttp = true,
#endif
            // Authorization server provider which controls the lifecycle of Authorization Server
            Provider = new OAuthAuthorizationServerProvider
            {
              OnValidateClientAuthentication = ValidateClientAuthentication,
              OnGrantResourceOwnerCredentials = GrantResourceOwnerCredentials,
            }
          });
          builder.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());
        // Configure Web API
        HttpConfiguration config = new HttpConfiguration();

          // Support conventional routing
          var routeTemplate = (_servicePrefix + "/api/{controller}/{id}").TrimStart('/'); // No leading slash allowed
          config.Routes.MapHttpRoute(
              "DefaultApi",
              routeTemplate,
              new { id = RouteParameter.Optional }
          );

          // Support attribute based routing
          config.MapHttpAttributeRoutes();

          // Set json as default instead of xml
          config.Formatters.JsonFormatter.MediaTypeMappings
            .Add(new System.Net.Http.Formatting.RequestHeaderMapping(
              "Accept", "text/html", StringComparison.InvariantCultureIgnoreCase, true, "application/json"));

          builder.UseWebApi(config);

          // Configure MiddleWare
          foreach (Type middleWareType in _middleWares)
          {
            builder.Use(middleWareType);
          }
        });
        
#endif
      }
    }

#if NET5_0_OR_GREATER
#else
    private Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
    {
      context.Validated();
      return Task.CompletedTask;
    }

    public async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
    {
      var userManagement = ServiceRegistration.Get<IUserProfileDataManagement>(false);
      if (userManagement == null)
      {
        //Try client service
        IUserManagement clientUserManagement = ServiceRegistration.Get<IUserManagement>();
        userManagement = clientUserManagement.UserProfileDataManagement;
      }

      if (userManagement != null)
      {
        var user = await userManagement.GetProfileByNameAsync(context.UserName);
        if (user.Success)
        {
          var pass = GetPassword(context.Password);
          if (UserProfile.VerifyPassword(pass, user.Result.Password))
          {
            bool isAdmin = !user.Result.RestrictShares && !user.Result.RestrictAges && user.Result.ProfileType == UserProfileType.UserProfile &&
              !user.Result.EnableRestrictionGroups;

            var identity = new ClaimsIdentity(context.Options.AuthenticationType);
            identity.AddClaim(new Claim(ClaimTypes.Name, context.UserName));
            identity.AddClaim(new Claim(ClaimTypes.Sid, user.Result.ProfileId.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "User"));
            await userManagement.LoginProfileAsync(user.Result.ProfileId);

            context.Validated(identity);
            return;
          }
          else
          {
            context.Rejected();
            context.SetError("invalid_grant", "The user name or password is incorrect.");
          }
        }
      }
      context.Rejected();
      context.SetError("invalid_grant", "User management not available.");
    }

    private string GetPassword(string encoded)
    {
      byte[] converted = Convert.FromBase64String(encoded);
      return Encoding.UTF8.GetString(converted);
    }
#endif

    public void Dispose()
    {
      StopServer();
    }

    private void StopServer()
    {
      try
      {
        lock (_syncObj)
        {
          _httpServer?.Dispose();
          _httpServer = null;
        }
      }
      catch (SocketException e)
      {
        ServiceRegistration.Get<ILogger>().Warn("ResourceServer: Error stopping HTTP server", e);
      }
    }

#region IResourceServer implementation

    public string GetServiceUrl(IPAddress ipAddress)
    {
      if (ipAddress == null)
        return string.Format("http://{0}:{1}{2}", "127.0.0.1", _serverPort, _servicePrefix);

      return ipAddress.AddressFamily == AddressFamily.InterNetworkV6 ?
        string.Format("http://[{0}]:{1}{2}", RemoveScope(ipAddress.ToString()), _serverPort, _servicePrefix) :
        string.Format("http://{0}:{1}{2}", ipAddress, _serverPort, _servicePrefix);
    }

    private string RemoveScope(string ipAddress)
    {
      // %x is appended, but if we like to connect this address, we need to remove this scope identifier
      var SCOPE_DELIMITER = "%";
      if (!ipAddress.Contains(SCOPE_DELIMITER))
        return ipAddress;
      return ipAddress.Substring(0, ipAddress.IndexOf(SCOPE_DELIMITER));
    }

    public int GetPortForIP(IPAddress ipAddress)
    {
      // We use only one server that binds to multiple addresses
      return _serverPort;
    }

    public void Startup()
    {
      CreateAndStartServer();
      ServiceRegistration.Get<ILogger>().Info("ResourceServer: HTTP servers running on {0}", GetServiceUrl(IPAddress.Any));
    }

    public void Shutdown()
    {
      ServiceRegistration.Get<ILogger>().Info("ResourceServer: Shutting down HTTP servers");
      StopServer();
    }

    public void RestartHttpServers()
    {
      ServiceRegistration.Get<ILogger>().Info("ResourceServer: Restarting HTTP servers");
      StopServer();
      CreateAndStartServer();
    }

    public void AddHttpModule(Type moduleType)
    {
      _middleWares.Add(moduleType);
      if (_httpServer != null)
      {
        // Note: the Owin pipeline is not designed to allow dynamic changes, so we have to rebuild it completely.
        StopServer();
        CreateAndStartServer();
      }
    }

    public void RemoveHttpModule(Type moduleType)
    {
      _middleWares.Remove(moduleType);
    }

    public void AddHttpControllerAssembly(Type controllerType)
    {
      if (controllerType == null)
        throw new ArgumentNullException("controllerType");

      if (!_controllerTypes.Contains(controllerType))
        _controllerTypes.Add(controllerType);

      if (_httpServer != null)
      {
        // The set of controller assemblies is part of the service collection, so it is fixed once the host is
        // built - the same constraint the resource access modules have, and it is rebuilt the same way. On the
        // platforms without http.sys the shared listener refuses a rebuilt endpoint after other endpoints have
        // joined its port, which is why a server registers the assemblies of its own API while it registers
        // its services, not while it starts them.
        StopServer();
        CreateAndStartServer();
      }
    }

#if NET5_0_OR_GREATER
    /// <summary>
    /// Tells MVC which assemblies to search for API controllers.
    /// </summary>
    /// <remarks>
    /// The assemblies of the resource access modules are searched because that is how every assembly with
    /// controllers reached this host before <see cref="AddHttpControllerAssembly"/> existed - it worked by
    /// accident, and an assembly with controllers but without a middleware was invisible. Both lists end up
    /// here so that nothing which relies on the shortcut stops working while the explicit registration is
    /// introduced.
    /// </remarks>
    public static void AddApplicationParts(IMvcBuilder mvcBuilder, IEnumerable<Type> moduleTypes,
        IEnumerable<Type> controllerTypes)
    {
      if (moduleTypes != null)
      {
        foreach (Type moduleType in moduleTypes)
          mvcBuilder.AddApplicationPart(moduleType.Assembly);
      }
      if (controllerTypes != null)
      {
        foreach (Type controllerType in controllerTypes)
          mvcBuilder.AddApplicationPart(controllerType.Assembly);
      }
    }
#endif

    #endregion
  }
}
