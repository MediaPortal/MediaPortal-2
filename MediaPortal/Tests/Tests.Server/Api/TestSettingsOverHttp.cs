/*
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.MediaManagement;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.Threading;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Server.Api;
using Moq;
using UPnP.Infrastructure.Http;
using NUnit.Framework;

namespace Tests.Server.Api
{
  /// <summary>
  /// The server's own settings over HTTP: what a stranger may read, what a caller may change, what the answer
  /// says about restarts, and what a server that wants to know its callers does to somebody who does not.
  /// </summary>
  [TestFixture]
  public class TestSettingsOverHttp
  {
    private const string PREFIX = "/MediaPortal/Server";

    private readonly List<IDisposable> _listeners = new List<IDisposable>();

    private string _baseUrl;
    private ServerSettings _server;
    private ImporterWorkerSettings _importer;
    private UserLoginSettings _login;

    [TearDown]
    public void StopEverything()
    {
      foreach (IDisposable listener in _listeners)
        listener.Dispose();
      _listeners.Clear();

      ServiceRegistration.Remove<ISystemResolver>();
      ServiceRegistration.Remove<ISettingsManager>();
      ServiceRegistration.Remove<IThreadPool>();
      ServiceRegistration.Remove<IUserProfileDataManagement>();
      ServiceRegistration.Remove<ILogger>();
      ServiceRegistration.Remove<IMediaLibrary>();
    }

    private void StartServer(bool authorize = false)
    {
      ServiceRegistration.Set<ILogger>(new DefaultLogger(new StringWriter(), LogLevel.All, false, true));

      Mock<ISystemResolver> resolver = new Mock<ISystemResolver>();
      resolver.SetupGet(r => r.SystemType).Returns(SystemType.Server);
      ServiceRegistration.Set<ISystemResolver>(resolver.Object);

      ServiceRegistration.Set<IMediaLibrary>(new Mock<IMediaLibrary>().Object);

      // The two settings objects stand in for the files the real manager would read: Save replaces the live
      // instance, so a test can ask whether a PUT actually reached the settings or only the answer.
      _server = new ServerSettings { WebAutorizationEnabled = authorize, HttpServerPort = 55555 };
      _importer = new ImporterWorkerSettings { EnableAutoRefresh = true, ImporterStartTime = 2.0 };
      _login = new UserLoginSettings();

      Mock<ISettingsManager> settingsManager = new Mock<ISettingsManager>();
      settingsManager.Setup(s => s.Load<ServerSettings>()).Returns(() => _server);
      settingsManager.Setup(s => s.Load<ImporterWorkerSettings>()).Returns(() => _importer);
      settingsManager.Setup(s => s.Load<UserLoginSettings>()).Returns(() => _login);
      settingsManager.Setup(s => s.Save(It.IsAny<ServerSettings>())).Callback<object>(o => _server = (ServerSettings)o);
      settingsManager.Setup(s => s.Save(It.IsAny<ImporterWorkerSettings>())).Callback<object>(o => _importer = (ImporterWorkerSettings)o);
      settingsManager.Setup(s => s.Save(It.IsAny<UserLoginSettings>())).Callback<object>(o => _login = (UserLoginSettings)o);
      ServiceRegistration.Set<ISettingsManager>(settingsManager.Object);

      ServiceRegistration.Set<IThreadPool>(new Mock<IThreadPool>().Object);
      ServiceRegistration.Set<IUserProfileDataManagement>(new Mock<IUserProfileDataManagement>().Object);

      ResourceServer resourceServer = new ResourceServer();
      resourceServer.AddHttpControllerAssembly(typeof(MediaController));

      int port = FreePort();
      _listeners.Add(SharedHttpPortHost.Start(new[] { "http://127.0.0.1:" + port + PREFIX },
          resourceServer.ConfigureApiServices, resourceServer.ConfigureApiPipeline));
      _baseUrl = "http://127.0.0.1:" + port + PREFIX;
    }

    static int FreePort()
    {
      TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
      listener.Start();
      int port = ((IPEndPoint)listener.LocalEndpoint).Port;
      listener.Stop();
      return port;
    }

    static readonly JsonSerializerOptions Json = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true
    };

    static async Task<(HttpStatusCode Code, string Body)> Get(string url)
    {
      using (HttpClient client = new HttpClient())
      using (HttpResponseMessage response = await client.GetAsync(url))
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    static async Task<(HttpStatusCode Code, string Body)> Put(string url, string json)
    {
      using (HttpClient client = new HttpClient())
      using (HttpResponseMessage response = await client.PutAsync(url,
          new StringContent(json, Encoding.UTF8, "application/json")))
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task AGuestReadsWhatTheServerHolds()
    {
      StartServer();

      (HttpStatusCode code, string body) = await Get(_baseUrl + "/api/media/settings");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);

      SettingsController.ServerSettingsResult read =
          JsonSerializer.Deserialize<SettingsController.ServerSettingsResult>(body, Json);
      Assert.That(read.WebAuthorizationEnabled, Is.False);
      Assert.That(read.HttpServerPort, Is.EqualTo(55555));
      Assert.That(read.ImporterEnabled, Is.True);
      Assert.That(read.ImporterStartTime, Is.EqualTo(2.0));
      Assert.That(read.EnableUserLogin, Is.True,
          "a household that has given its people names and numbers is asked about them from the first start");
      Assert.That(read.EnableUserLoginScreen, Is.True);
      Assert.That(read.AutoLoginUser, Is.EqualTo(Guid.Empty.ToString()), "and nobody is walked in yet");
      Assert.That(read.AutoLogoutEnabled, Is.False);
      Assert.That(read.AutoLogoutIdleTimeoutInMin, Is.EqualTo(30));
      Assert.That(read.RestartNeededFor, Is.Empty,
          "reading changes nothing, so nothing awaits a restart");
    }

    [Test]
    public async Task OnlyAFieldThatWasMovedIsNamedForARestart()
    {
      StartServer();

      // A read that never touched the endpoints must not come back demanding a reboot,
      // and a save that moves one restart-only field names that one field - no more.
      (HttpStatusCode code, string body) = await Put(_baseUrl + "/api/media/settings",
          "{\"importerStartTime\":4}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);
      SettingsController.ServerSettingsResult untouched =
          JsonSerializer.Deserialize<SettingsController.ServerSettingsResult>(body, Json);
      Assert.That(untouched.RestartNeededFor, Is.Empty, "the importer's hour is read on its next pass");

      (code, body) = await Put(_baseUrl + "/api/media/settings", "{\"useIPv6\":true}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);
      SettingsController.ServerSettingsResult moved =
          JsonSerializer.Deserialize<SettingsController.ServerSettingsResult>(body, Json);
      Assert.That(moved.RestartNeededFor, Is.EquivalentTo(new[] { "use_ipv6" }),
          "IPv6 is picked up when the server listens again");
      Assert.That(_server.UseIPv6, Is.True);

      // Saving the same values again moves nothing, so the answer stops repeating itself.
      (code, body) = await Put(_baseUrl + "/api/media/settings", "{\"useIPv6\":true}");
      untouched = JsonSerializer.Deserialize<SettingsController.ServerSettingsResult>(body, Json);
      Assert.That(untouched.RestartNeededFor, Is.Empty);
    }

    [Test]
    public async Task AChangeLandsInTheSettingsNotOnlyInTheAnswer()
    {
      StartServer();

      (HttpStatusCode code, string body) = await Put(_baseUrl + "/api/media/settings",
          "{\"webAuthorizationEnabled\":true,\"importerStartTime\":3.5,\"importerEnabled\":false}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);

      SettingsController.ServerSettingsResult read =
          JsonSerializer.Deserialize<SettingsController.ServerSettingsResult>(body, Json);
      Assert.That(read.WebAuthorizationEnabled, Is.True, "the answer says the change happened");
      Assert.That(read.ImporterStartTime, Is.EqualTo(3.5));
      Assert.That(read.ImporterEnabled, Is.False);

      Assert.That(_server.WebAutorizationEnabled, Is.True, "and the settings themselves hold it");
      Assert.That(_importer.ImporterStartTime, Is.EqualTo(3.5));
      Assert.That(_importer.EnableAutoRefresh, Is.False);
    }

    [Test]
    public async Task TheHouseholdsRulesLandInTheSettingsNotOnlyInTheAnswer()
    {
      StartServer();

      Guid ralf = Guid.NewGuid();
      (HttpStatusCode code, string body) = await Put(_baseUrl + "/api/media/settings",
          "{\"enableUserLogin\":true,\"enableUserLoginScreen\":false,\"autoLoginUser\":\"" + ralf +
          "\",\"autoLogoutEnabled\":true,\"autoLogoutIdleTimeoutInMin\":15}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);

      SettingsController.ServerSettingsResult read =
          JsonSerializer.Deserialize<SettingsController.ServerSettingsResult>(body, Json);
      Assert.That(read.EnableUserLoginScreen, Is.False);
      Assert.That(read.AutoLoginUser, Is.EqualTo(ralf.ToString()));
      Assert.That(read.AutoLogoutEnabled, Is.True);
      Assert.That(read.AutoLogoutIdleTimeoutInMin, Is.EqualTo(15));

      Assert.That(_login.EnableUserLoginScreen, Is.False,
          "the rules belong to the household, so they are kept and not only echoed back");
      Assert.That(_login.AutoLoginUser, Is.EqualTo(ralf));
      Assert.That(_login.AutoLogoutIdleTimeoutInMin, Is.EqualTo(15));

      (code, body) = await Get(_baseUrl + "/api/media/settings");
      read = JsonSerializer.Deserialize<SettingsController.ServerSettingsResult>(body, Json);
      Assert.That(read.AutoLogoutIdleTimeoutInMin, Is.EqualTo(15),
          "and the next television in the house reads the same household");
      Assert.That(read.RestartNeededFor, Is.Empty, "no rule of the household waits for a reboot");
    }

    [Test]
    public async Task ARuleNobodyCouldMeanIsRefusedWholesale()
    {
      StartServer();

      (HttpStatusCode code, string body) = await Put(_baseUrl + "/api/media/settings",
          "{\"autoLogoutIdleTimeoutInMin\":0}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.BadRequest), body);
      Assert.That(_login.AutoLogoutIdleTimeoutInMin, Is.EqualTo(30), "and the half hour stayed");

      // A half-typed id would be saved as a person nobody can ever become, so it never reaches the store.
      (code, body) = await Put(_baseUrl + "/api/media/settings", "{\"autoLoginUser\":\"half-typed\"}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.BadRequest), body);
      Assert.That(_login.AutoLoginUser, Is.EqualTo(Guid.Empty), "and nobody was expected because of a typo");
    }

    [Test]
    public async Task AValueNobodyCanDialIsRefusedWholesale()
    {
      StartServer();

      (HttpStatusCode code, string body) = await Put(_baseUrl + "/api/media/settings",
          "{\"httpServerPort\":70000}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.BadRequest), body);
      Assert.That(_server.HttpServerPort, Is.EqualTo(55555), "and the port stayed dialable");

      (code, body) = await Put(_baseUrl + "/api/media/settings", "{\"importerStartTime\":25}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.BadRequest), body);
      Assert.That(_importer.ImporterStartTime, Is.EqualTo(2.0), "and the importer kept its hour");
    }

    [Test]
    public async Task AServerThatWantsToKnowItsCallersRefusesStrangersToItsSettingsToo()
    {
      StartServer(authorize: true);

      (HttpStatusCode code, _) = await Get(_baseUrl + "/api/media/settings");
      Assert.That((int)code, Is.EqualTo(401).Or.EqualTo(403),
          "changing who may use the server cannot be a thing strangers do");
    }
  }
}