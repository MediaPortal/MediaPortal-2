#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Auth;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Server.Api;
using Moq;
using NUnit.Framework;
using UPnP.Infrastructure.Http;

namespace Tests.Server.Api
{
  /// <summary>
  /// Drives the resource server's own pipeline over a socket: password in at the token endpoint, token in the
  /// header, protected endpoint open.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The other tests in this folder call the parts. These cannot answer the question that matters for a token,
  /// which is whether the middleware is in the right order in front of the controllers - <c>UseAuthentication()</c>
  /// before <c>UseAuthorization()</c> is two lines in <see cref="ResourceServer.ConfigureApiPipeline"/> and a typo
  /// there is invisible to every unit test, because each half on its own is correct.
  /// </para>
  /// <para>
  /// So the listener is built from those same two methods, not from a copy of them: the test configures
  /// <see cref="SharedHttpPortHost"/> the way the resource server configures both of its HTTP hosts, and talks to
  /// it over a real socket on a free loopback port. That also makes it the portable host - the one Linux and
  /// Android use - rather than http.sys, so what is measured here is the code path those platforms take.
  /// </para>
  /// <para>
  /// The profile store, the settings store and the thread pool are the mocks MP2's tests have always used for
  /// services, registered in <see cref="ServiceRegistration"/> for the duration of the test. A fresh server has no
  /// profiles at all (its <c>USER_PROFILES</c> table is empty until a client creates one), so there is no real
  /// account to authenticate against in a test process; the store is where a credential check ends, and mocking it
  /// says so explicitly.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTokenEndpointOverHttp
  {
    private const string PROFILE_NAME = "Georg";
    private const string PASSWORD = "***";
    private const string PREFIX = "/MediaPortal/Server";

    private readonly List<IDisposable> _listeners = new List<IDisposable>();
    private string _baseUrl;
    private Mock<IUserProfileDataManagement> _profiles;
    private UserProfile _profile;

    [SetUp]
    public void StartTheResourceServersOwnPipeline()
    {
      _profile = new UserProfile(Guid.NewGuid(), PROFILE_NAME, UserProfileType.UserProfile,
          UserProfile.HashPassword(PASSWORD));

      _profiles = new Mock<IUserProfileDataManagement>();
      // The general rule first and the specific one second: Moq answers with the last setup that matches, so the
      // other way round the known profile would be reported as unknown.
      _profiles.Setup(m => m.GetProfileByNameAsync(It.IsAny<string>()))
          .ReturnsAsync(new MediaPortal.Common.Async.AsyncResult<UserProfile>(false, null));
      _profiles.Setup(m => m.GetProfileByNameAsync(PROFILE_NAME))
          .ReturnsAsync(new MediaPortal.Common.Async.AsyncResult<UserProfile>(true, _profile));
      _profiles.Setup(m => m.LoginProfileAsync(It.IsAny<Guid>())).ReturnsAsync(true);

      // One signing key for the whole test, stored the way the server stores it, so that the endpoint and the
      // handler both go through the settings store rather than through a key handed to them.
      ServerSettings settings = new ServerSettings();
      settings.WebAccessTokenSigningKey = Convert.ToBase64String(MediaPortalSigningKey.Create());
      Mock<ISettingsManager> settingsManager = new Mock<ISettingsManager>();
      settingsManager.Setup(s => s.Load<ServerSettings>()).Returns(() => new ServerSettings
      {
        WebAutorizationEnabled = settings.WebAutorizationEnabled,
        WebAccessTokenSigningKey = settings.WebAccessTokenSigningKey
      });
      settingsManager.Setup(s => s.Save(It.IsAny<object>()))
          .Callback<object>(o => settings.WebAccessTokenSigningKey = ((ServerSettings)o).WebAccessTokenSigningKey);

      // The resource access middleware the pipeline builds needs the thread pool; it is not otherwise involved.
      ServiceRegistration.Set<IThreadPool>(new Mock<IThreadPool>().Object);
      ServiceRegistration.Set<ISettingsManager>(settingsManager.Object);
      ServiceRegistration.Set<IUserProfileDataManagement>(_profiles.Object);

      ResourceServer resourceServer = new ResourceServer();
      resourceServer.AddHttpControllerAssembly(typeof(TokenController));

      int port = FreePort();
      _listeners.Add(SharedHttpPortHost.Start(new[] { "http://127.0.0.1:" + port + PREFIX },
          resourceServer.ConfigureApiServices, resourceServer.ConfigureApiPipeline));
      _baseUrl = "http://127.0.0.1:" + port + PREFIX;
    }

    [TearDown]
    public void StopTheListener()
    {
      foreach (IDisposable listener in _listeners)
        listener.Dispose();
      _listeners.Clear();
      ServiceRegistration.Remove<IUserProfileDataManagement>();
      ServiceRegistration.Remove<ISettingsManager>();
      ServiceRegistration.Remove<IThreadPool>();
    }

    #region Helpers

    static int FreePort()
    {
      TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
      listener.Start();
      int port = ((IPEndPoint)listener.LocalEndpoint).Port;
      listener.Stop();
      return port;
    }

    static string Base64(string value)
    {
      return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    static async Task<Answer> SendAsync(HttpRequestMessage request, string body = null, string contentType = null)
    {
      if (body != null)
        request.Content = new StringContent(body, Encoding.UTF8, contentType);

      using (HttpClient client = new HttpClient())
      {
        HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
        string text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        string wwwAuthenticate = null;
        IEnumerable<string> schemes;
        if (response.Headers.TryGetValues("WWW-Authenticate", out schemes))
          wwwAuthenticate = string.Join(",", schemes);
        return new Answer((int)response.StatusCode, text, wwwAuthenticate);
      }
    }

    static Task<Answer> PostFormAsync(string path, string form)
    {
      return SendAsync(new HttpRequestMessage(HttpMethod.Post, path), form, "application/x-www-form-urlencoded");
    }

    static Task<Answer> GetWithTokenAsync(string path, string token)
    {
      HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, path);
      if (token != null)
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
      return SendAsync(request);
    }

    /// <summary>
    /// The password grant, as a client sends it. Returns the answer so a test can assert on a failure too.
    /// </summary>
    async Task<string> RequestTokenAsync(string userName = PROFILE_NAME, string password = PASSWORD)
    {
      Answer answer = await PostFormAsync(_baseUrl + "/Token",
          "grant_type=password&username=" + userName + "&password=" + Base64(password)).ConfigureAwait(false);
      Assert.That(answer.StatusCode, Is.EqualTo(200), "the token endpoint said: " + answer.Body);
      return Json(answer.Body).GetProperty("access_token").GetString();
    }

    static JsonElement Json(string body)
    {
      return JsonSerializer.Deserialize<JsonElement>(body);
    }

    /// <summary>
    /// Status and body together. Asserting on both is deliberate: a 403 with an empty body and a 403 from the
    /// authorization filter are different events, and a test that only looks at the number cannot tell them apart.
    /// <paramref name="wwwAuthenticate"/> is kept because it is the fingerprint of <em>who</em> refused: a
    /// framework challenge announces a scheme, MP2's own filter says nothing.
    /// </summary>
    class Answer
    {
      public Answer(int statusCode, string body, string wwwAuthenticate = null)
      {
        StatusCode = statusCode;
        Body = body;
        WwwAuthenticate = wwwAuthenticate;
      }

      public int StatusCode { get; }
      public string Body { get; }
      public string WwwAuthenticate { get; }

      public override string ToString()
      {
        return StatusCode + " " + (Body != null && Body.Length > 200 ? Body.Substring(0, 200) : Body);
      }
    }

    #endregion

    [Test]
    public async Task TheProtectedEndpointIsShutWithoutAToken()
    {
      Answer answer = await GetWithTokenAsync(_baseUrl + "/api/session", null).ConfigureAwait(false);

      Assert.That(answer.StatusCode, Is.EqualTo(403), answer.ToString());
      // 403 from MP2's filter, not 401 from the framework's authorization middleware. The distinction is not
      // cosmetic: while the attribute also derived from AuthorizeAttribute the framework refused first, answered
      // 401, and could not be talked out of it by WebAutorizationEnabled=false.
      Assert.That(answer.WwwAuthenticate, Is.Null,
          "a challenge header would mean the framework decided this, not MediaPortal");
    }

    [Test]
    public async Task ATokenFromTheTokenEndpointOpensTheProtectedEndpoint()
    {
      string token = await RequestTokenAsync().ConfigureAwait(false);

      Answer answer = await GetWithTokenAsync(_baseUrl + "/api/session", token).ConfigureAwait(false);

      Assert.That(answer.StatusCode, Is.EqualTo(200), answer.ToString());
      JsonElement body = Json(answer.Body);
      Assert.That(body.GetProperty("name").GetString(), Is.EqualTo(PROFILE_NAME));
      Assert.That(body.GetProperty("profileId").GetString(), Is.EqualTo(_profile.ProfileId.ToString()));
      Assert.That(body.GetProperty("role").GetString(), Is.EqualTo("Admin"));
      Assert.That(body.GetProperty("authenticationType").GetString(),
          Is.EqualTo(ResourceServer.MEDIAPORTAL_AUTHENTICATION_TYPE));
    }

    [Test]
    public async Task TheTokenAnswerIsTheOneOAuthClientsRead()
    {
      Answer answer = await PostFormAsync(_baseUrl + "/Token",
          "grant_type=password&username=" + PROFILE_NAME + "&password=" + Base64(PASSWORD)).ConfigureAwait(false);

      Assert.That(answer.StatusCode, Is.EqualTo(200), answer.ToString());
      JsonElement body = Json(answer.Body);
      Assert.That(body.GetProperty("token_type").GetString(), Is.EqualTo("bearer"));
      Assert.That(body.GetProperty("expires_in").GetInt64(), Is.GreaterThan(0L));
      Assert.That(body.GetProperty("access_token").GetString().Split('.').Length, Is.EqualTo(2));
      _profiles.Verify(m => m.LoginProfileAsync(_profile.ProfileId), Times.Once,
          "the grant is also what makes the profile's shares and restrictions apply to the connection");
    }

    [Test]
    public async Task AJsonBodyWorksAsWellAsAForm()
    {
      // The old endpoint only spoke form encoding because that is what Owin's OAuth middleware accepted. A client
      // built today sends JSON, and the endpoint reads both.
      Answer answer = await SendAsync(new HttpRequestMessage(HttpMethod.Post, _baseUrl + "/Token"),
          "{\"grant_type\":\"password\",\"username\":\"" + PROFILE_NAME + "\",\"password\":\"" + Base64(PASSWORD) + "\"}",
          "application/json").ConfigureAwait(false);

      Assert.That(answer.StatusCode, Is.EqualTo(200), answer.ToString());
      Assert.That(Json(answer.Body).GetProperty("access_token").GetString(), Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task ATokenThatWasChangedOnTheWayIsWorthNothing()
    {
      string token = await RequestTokenAsync().ConfigureAwait(false);
      int position = token.IndexOf('.') / 2;
      string tampered = token.Substring(0, position)
          + (token[position] == 'A' ? 'B' : 'A')
          + token.Substring(position + 1);

      Answer answer = await GetWithTokenAsync(_baseUrl + "/api/session", tampered).ConfigureAwait(false);
      Assert.That(answer.StatusCode, Is.EqualTo(403), answer.ToString());

      Answer garbage = await GetWithTokenAsync(_baseUrl + "/api/session", "not.a.real.token").ConfigureAwait(false);
      Assert.That(garbage.StatusCode, Is.EqualTo(403));
    }

    [Test]
    public async Task AWrongPasswordIsAnsweredTheWayRfc6749Says()
    {
      Answer answer = await PostFormAsync(_baseUrl + "/Token",
          "grant_type=password&username=" + PROFILE_NAME + "&password=" + Base64("wrong")).ConfigureAwait(false);

      Assert.That(answer.StatusCode, Is.EqualTo(400), answer.ToString());
      Assert.That(Json(answer.Body).GetProperty("error").GetString(), Is.EqualTo("invalid_grant"));
      Assert.That(answer.Body, Does.Not.Contain("access_token"));
    }

    [Test]
    public async Task AGuessAboutSomebodyElsesAccountGetsTheSameAnswerAsAWrongPassword()
    {
      Answer unknown = await PostFormAsync(_baseUrl + "/Token",
          "grant_type=password&username=nobody-here&password=" + Base64(PASSWORD)).ConfigureAwait(false);
      Answer wrongPassword = await PostFormAsync(_baseUrl + "/Token",
          "grant_type=password&username=" + PROFILE_NAME + "&password=" + Base64("wrong")).ConfigureAwait(false);

      Assert.That(unknown.StatusCode, Is.EqualTo(wrongPassword.StatusCode));
      Assert.That(Json(unknown.Body).GetProperty("error_description").GetString(),
          Is.EqualTo(Json(wrongPassword.Body).GetProperty("error_description").GetString()),
          "otherwise the endpoint tells an attacker which profile names exist");
    }

    [Test]
    public async Task AGrantTypeTheServerDoesNotImplementIsRefused()
    {
      Answer answer = await PostFormAsync(_baseUrl + "/Token",
          "grant_type=authorization_code&username=" + PROFILE_NAME + "&password=" + Base64(PASSWORD))
              .ConfigureAwait(false);

      Assert.That(answer.StatusCode, Is.EqualTo(400), answer.ToString());
      Assert.That(Json(answer.Body).GetProperty("error").GetString(), Is.EqualTo("unsupported_grant_type"));
    }

    [Test]
    public async Task TurningWebAuthorizationOffOpensTheEndpointForAnonymousCallers()
    {
      // WebAutorizationEnabled=false is the setting MP2 has always had for "this LAN is trusted"; the filter reads
      // it, so it has to be measured rather than assumed to still work after the handler was introduced.
      Mock<ISettingsManager> off = new Mock<ISettingsManager>();
      off.Setup(s => s.Load<ServerSettings>())
          .Returns(new ServerSettings { WebAutorizationEnabled = false });
      ServiceRegistration.Set(off.Object);

      Answer answer = await GetWithTokenAsync(_baseUrl + "/api/session", null).ConfigureAwait(false);

      Assert.That(answer.StatusCode, Is.EqualTo(200), answer.ToString());
      Assert.That(Json(answer.Body).GetProperty("name").GetString(), Is.Null.Or.Empty,
          "the endpoint is open, but nobody walked in - an open endpoint must not invent a caller");
    }

    [Test]
    public async Task TheHealthEndpointStaysAnonymousBesidetheProtectedOne()
    {
      // The gate that starts the real server depends on health answering without credentials, and a pipeline
      // change that put authentication in front of it would break that. Measured here because the two endpoints
      // share the pipeline.
      using (HttpClient client = new HttpClient())
      {
        HttpResponseMessage response = await client.GetAsync(_baseUrl + "/health").ConfigureAwait(false);
        Assert.That((int)response.StatusCode, Is.EqualTo(200));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("MediaPortal 2 Server"));
      }
    }

    [Test]
    public async Task TheEndpointsAreBelowThePrefixAndNowhereElse()
    {
      // The prefix is part of every MP2 URL; a controller mapped outside it would be reachable on a bare path.
      Answer outside = await GetWithTokenAsync("http://127.0.0.1:" + new Uri(_baseUrl).Port + "/api/session", null)
          .ConfigureAwait(false);
      Assert.That(outside.StatusCode, Is.EqualTo(404), outside.ToString());
    }
  }
}
