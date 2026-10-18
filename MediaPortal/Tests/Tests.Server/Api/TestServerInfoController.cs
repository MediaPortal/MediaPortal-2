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
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Server.Api;
using UPnP.Infrastructure.Http;

namespace Tests.Server.Api
{
  /// <summary>
  /// Tests for the first endpoint of the HTTP API. The point is not the information it prints but the chain it
  /// goes through: a controller in <c>MediaPortal.Common</c> reached through MVC routing below the URL prefix
  /// of the resource server, answering from an MP2 service resolved on an ASP.NET Core request thread. That is
  /// the chain the new front end will be built on, on both HTTP hosts.
  /// </summary>
  [TestFixture]
  public class TestServerInfoController
  {
    #region Helpers

    private readonly List<IDisposable> _registrations = new List<IDisposable>();

    [SetUp]
    public void SetUp()
    {
      RemoveSharedServices();
    }

    [TearDown]
    public void TearDown()
    {
      foreach (IDisposable registration in _registrations)
        registration.Dispose();
      _registrations.Clear();
      RemoveSharedServices();
    }

    /// <summary>
    /// The service registration is process wide and shared with every other fixture of this assembly, so a
    /// leftover from another test must not decide what this one sees.
    /// </summary>
    private static void RemoveSharedServices()
    {
      if (ServiceRegistration.IsRegistered<ISettingsManager>())
        ServiceRegistration.Remove<ISettingsManager>();
      if (ServiceRegistration.IsRegistered<ISystemResolver>())
        ServiceRegistration.Remove<ISystemResolver>();
    }

    private static int GetFreePort()
    {
      var listener = new TcpListener(IPAddress.Loopback, 0);
      listener.Start();
      int port = ((IPEndPoint) listener.LocalEndpoint).Port;
      listener.Stop();
      return port;
    }

    /// <summary>
    /// Builds the controller the way MVC would: with a request to read the URL prefix from.
    /// </summary>
    private static ServerInfoController CreateController(string pathBase)
    {
      var request = new DefaultHttpContext();
      request.Request.PathBase = pathBase;
      return new ServerInfoController
      {
        ControllerContext = new ControllerContext { HttpContext = request }
      };
    }

    private static ServerInfoController.ServerInfoResult GetInfo(string pathBase)
    {
      ActionResult<ServerInfoController.ServerInfoResult> action = CreateController(pathBase).GetServerInfo();
      Assert.That(action.Result, Is.Null, "the endpoint is expected to answer with the object, not with an action result");
      return action.Value;
    }

    private static async Task<(HttpStatusCode StatusCode, string Body)> GetAsync(int port, string path)
    {
      using (var client = new HttpClient())
      {
        HttpResponseMessage response = await client.GetAsync("http://127.0.0.1:" + port + path);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
      }
    }

    /// <summary>
    /// Stands in for the settings service. <see cref="ServerSettings.IPAddressBindingsList"/> logs through
    /// <c>ILogger</c> as soon as bindings are configured, so the stub leaves them empty: the point here
    /// is that the endpoint read the settings object at all, not that the logging works.
    /// </summary>
    private sealed class StubSettingsManager : ISettingsManager
    {
      private readonly ServerSettings _settings;

      public StubSettingsManager(ServerSettings settings)
      {
        _settings = settings;
      }

      public SettingsType Load<SettingsType>() where SettingsType : class
      {
        return _settings as SettingsType;
      }

      public object Load(Type settingsType)
      {
        return _settings;
      }

      public void Save(object settingsObject) { }
      public void StartBatchUpdate() { }
      public void EndBatchUpdate() { }
      public void CancelBatchUpdate() { }
      public void ClearCache() { }
      public void ChangeUserContext(string userName) { }
      public void RemoveSettingsData(Type settingsType, bool user, bool global) { }
      public void RemoveAllSettingsData(bool user, bool global) { }
    }

    /// <summary>
    /// Says which kind of process this is. The resource server is part of the client as well, so the endpoint
    /// has to know that it belongs to a server only.
    /// </summary>
    private sealed class StubSystemResolver : ISystemResolver
    {
      public StubSystemResolver(SystemType systemType)
      {
        SystemType = systemType;
      }

      public string LocalSystemId
      {
        get { return "00000000-0000-0000-0000-000000000001"; }
      }

      public SystemName GetSystemNameForSystemId(string systemId)
      {
        return null;
      }

      public SystemType SystemType { get; }
    }

    #endregion

    #region The answer itself

    [Test]
    public void EndpointDescribesTheProcessItAnswersFrom()
    {
      ServerInfoController.ServerInfoResult info = GetInfo("/MediaPortal/Server");

      Assert.That(info.Name, Is.EqualTo("MediaPortal 2 Server"));
      Assert.That(info.Version, Is.EqualTo(typeof(ServerInfoController).Assembly.GetName().Version.ToString()));
      Assert.That(info.Framework, Does.StartWith(".NET"));
      Assert.That(info.OperatingSystem, Is.Not.Null.And.Not.Empty);
      Assert.That(info.OsArchitecture, Is.Not.Null.And.Not.Empty);
      Assert.That(info.ProcessArchitecture, Is.Not.Null.And.Not.Empty);
      Assert.That(info.ProcessId, Is.EqualTo(Environment.ProcessId));
      Assert.That(info.PathBase, Is.EqualTo("/MediaPortal/Server"));

      DateTime before = DateTime.UtcNow.AddMinutes(-5);
      DateTime utc = DateTime.Parse(info.ServerUtc, null, System.Globalization.DateTimeStyles.RoundtripKind);
      Assert.That(utc, Is.GreaterThanOrEqualTo(before).And.LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5)));
    }

    [Test]
    public void EndpointAnswersWithoutTheSettingsService()
    {
      // A host started without the core services has to answer too - a 500 over a missing service would make
      // the endpoint useless for finding out whether a server is alive but broken.
      ServerInfoController.ServerInfoResult info = GetInfo(string.Empty);

      Assert.That(info.Name, Is.EqualTo("MediaPortal 2 Server"));
      Assert.That(info.UseIPv4, Is.True, "the defaults of the settings class, not a guess");
      Assert.That(info.UseIPv6, Is.False);
      Assert.That(info.IpAddressBindings, Is.Null);
      Assert.That(info.PathBase, Is.Empty);
    }

    [Test]
    public void EndpointReadsItsSettingsThroughTheServiceRegistration()
    {
      ServerSettings settings = new ServerSettings { UseIPv4 = false, UseIPv6 = true };
      ServiceRegistration.Set<ISettingsManager>(new StubSettingsManager(settings));

      // This is the claim the API is built on: an MP2 service, resolved from the request thread of a listener,
      // is the same service the rest of the server uses.
      ServerInfoController.ServerInfoResult info = GetInfo("/MediaPortal/Server");

      Assert.That(info.UseIPv4, Is.False);
      Assert.That(info.UseIPv6, Is.True);
      Assert.That(info.IpAddressBindings, Is.Not.Null.And.Empty);
    }

    [Test]
    public void EndpointDoesNotAnswerInAClient()
    {
      // The resource server is started by the client too, below "/MediaPortal/Client". Without this guard the
      // client would describe itself as a server and would serve API endpoints that it does not use.
      ServiceRegistration.Set<ISystemResolver>(new StubSystemResolver(SystemType.Client));

      ActionResult<ServerInfoController.ServerInfoResult> action =
          CreateController("/MediaPortal/Client").GetServerInfo();

      Assert.That(action.Value, Is.Null);
      Assert.That(action.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public void EndpointAnswersForAServer()
    {
      ServiceRegistration.Set<ISystemResolver>(new StubSystemResolver(SystemType.Server));

      Assert.That(GetInfo("/MediaPortal/Server").Name, Is.EqualTo("MediaPortal 2 Server"));
    }

    #endregion

    #region Routing

    /// <summary>
    /// Reaches the controller over a real socket through the shared listener, with the registration the
    /// resource server uses: its assembly as an application part, controllers mapped, and the conventional
    /// <c>api/{controller}</c> route next to the attribute route. That the request arrives as
    /// <c>PathBase=/MediaPortal/Server</c> is what makes the same route work on http.sys.
    /// </summary>
    [Test]
    public async Task ControllerIsReachedBelowThePrefixOfTheResourceServer()
    {
      int port = GetFreePort();
      _registrations.Add(SharedHttpPortHost.Start(
          new[] { "http://127.0.0.1:" + port + "/MediaPortal/Server" },
          services => services.AddControllers().AddApplicationPart(typeof(ServerInfoController).Assembly),
          app =>
          {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
              endpoints.MapControllers();
              endpoints.MapControllerRoute("DefaultApi", "api/{controller}/{id?}");
            });
          }));

      (HttpStatusCode status, string body) = await GetAsync(port, "/MediaPortal/Server/api/serverinfo");
      Assert.That(status, Is.EqualTo(HttpStatusCode.OK), "the attribute route has to match below the prefix: " + body);

      JsonElement json = JsonSerializer.Deserialize<JsonElement>(body);
      Assert.That(json.GetProperty("name").GetString(), Is.EqualTo("MediaPortal 2 Server"));
      Assert.That(json.GetProperty("pathBase").GetString(), Is.EqualTo("/MediaPortal/Server"));
      Assert.That(json.GetProperty("version").GetString(),
          Is.EqualTo(typeof(ServerInfoController).Assembly.GetName().Version.ToString()));

      // The conventional route is the one the resource server declares for controllers without an attribute
      // route; it is matched case-insensitively and has to keep working next to the attribute route.
      (HttpStatusCode conventional, string conventionalBody) = await GetAsync(port, "/MediaPortal/Server/api/ServerInfo");
      Assert.That(conventional, Is.EqualTo(HttpStatusCode.OK), "the conventional route: " + conventionalBody);

      // Nothing else may be answered by the controller's routes: below the prefix, an unknown controller is a
      // 404 from MVC, not a fall-through.
      (HttpStatusCode unknown, string _) = await GetAsync(port, "/MediaPortal/Server/api/nosuchcontroller");
      Assert.That(unknown, Is.EqualTo(HttpStatusCode.NotFound));
    }

    #endregion
  }
}
