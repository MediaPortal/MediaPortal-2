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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Utilities.Network;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
#if !NET5_0
using Microsoft.Owin.Security.OAuth;
#endif
using Owin;
using UPnP.Infrastructure.Dv;

[assembly: OwinStartup(typeof(MediaPortal.Common.Services.ResourceAccess.ResourceServer))]
namespace MediaPortal.Common.Services.ResourceAccess
{
  public class ResourceServer : IResourceServer, IDisposable
  {
    public const string MEDIAPORTAL_AUTHENTICATION_TYPE = "MediaPortal";

    protected readonly List<Type> _middleWares = new List<Type>();
    protected IDisposable _httpServer;
    protected int _serverPort = UPnPServer.DEFAULT_UPNP_AND_SERVICE_PORT_NUMBER;
    protected readonly object _syncObj = new object();
    protected string _servicePrefix;

    public ResourceServer()
    {
      AddHttpModule(typeof(ResourceAccessModule));
      //CreateAndStartServer();
    }

    private void CreateAndStartServer()
    {
      ServerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ServerSettings>();
      List<string> filters = settings.IPAddressBindingsList;
      _serverPort = UPnPServer.DEFAULT_UPNP_AND_SERVICE_PORT_NUMBER;
      _servicePrefix = ResourceHttpAccessUrlUtils.RESOURCE_SERVER_BASE_PATH;
      var startOptions = UPnPServer.BuildStartOptions(_servicePrefix, filters, _serverPort);

      lock (_syncObj)
      {
        if (_httpServer != null) //Already started
          return;

        _httpServer = WebApp.Start(startOptions, builder =>
        {
#if !NET5_0
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
#endif
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
      }
    }

#if !NET5_0
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
#endif

    private string GetPassword(string encoded)
    {
      byte[] converted = Convert.FromBase64String(encoded);
      return Encoding.UTF8.GetString(converted);
    }

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

#endregion
  }
}
