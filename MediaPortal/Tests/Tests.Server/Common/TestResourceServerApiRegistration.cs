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
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Server.Api;
using UPnP.Infrastructure.Http;

namespace Tests.Server.Common
{
  /// <summary>
  /// A controller in an assembly that registers no resource access module. This is the shape an API assembly
  /// has, and until IResourceServer.AddHttpControllerAssembly existed it was invisible to the resource server,
  /// which only looked into the assemblies of the middleware it was given.
  /// </summary>
  public class ApiSeamProbeController : ControllerBase
  {
    [HttpGet("api/probe")]
    public IActionResult Get()
    {
      return Content("probed");
    }
  }

  /// <summary>
  /// A middleware in the same assembly as <see cref="ApiSeamProbeController"/>, standing in for the resource
  /// access module a plugin registers. It exists so that the rule "the assembly of a registered middleware is
  /// searched for controllers too" can be tested with both halves in one assembly - which is exactly the shape
  /// the rule was written for.
  /// </summary>
  public class ApiSeamProbeMiddleware
  {
    private readonly RequestDelegate _next;

    public ApiSeamProbeMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
      await _next(context).ConfigureAwait(false);
    }
  }

  /// <summary>
  /// Records the assemblies a composition root declares. It is the only way to see what a call to
  /// <see cref="MediaPortalServerApi.Register"/> did without building a whole server around it.
  /// </summary>
  internal class RecordingResourceServer : IResourceServer
  {
    public List<Assembly> ControllerAssemblyTypes { get; } = new List<Assembly>();

    public string AddressIPv4 { get { return "127.0.0.1"; } }

    public int PortIPv4 { get { return 0; } }

    public string AddressIPv6 { get { return null; } }

    public int PortIPv6 { get { return -1; } }

    public void Startup() { }

    public void Shutdown() { }

    public void RestartHttpServers() { }

    public string GetServiceUrl(IPAddress ipAddress)
    {
      return "http://127.0.0.1:0";
    }

    public void AddHttpModule(Type moduleType) { }

    public void RemoveHttpModule(Type moduleType) { }

    public void AddHttpControllerAssembly(Type controllerType)
    {
      ControllerAssemblyTypes.Add(controllerType.Assembly);
    }
  }

  /// <summary>
  /// Tests for the seam that decides which assemblies MVC searches for controllers. They ask a listener over a
  /// socket rather than inspecting MVC's internals, because the question a regression here has to answer is
  /// "does the client get an answer", and because an assertion about a feature provider would pass while the
  /// routing itself is broken.
  /// </summary>
  [TestFixture]
  public class TestResourceServerApiRegistration
  {
    #region Helpers

    private readonly List<IDisposable> _registrations = new List<IDisposable>();

    [TearDown]
    public void TearDown()
    {
      foreach (IDisposable registration in _registrations)
        registration.Dispose();
      _registrations.Clear();
      if (ServiceRegistration.IsRegistered<IResourceServer>())
        ServiceRegistration.Remove<IResourceServer>();
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
    /// Starts a listener with the resource server's own registration: its assembly choices applied through
    /// <see cref="ResourceServer.AddApplicationParts"/>, controllers mapped, below the resource server's prefix.
    /// </summary>
    private int StartResourceServerLikeEndpoint(IEnumerable<Type> moduleTypes, IEnumerable<Type> controllerTypes)
    {
      int port = GetFreePort();
      _registrations.Add(SharedHttpPortHost.Start(
          new[] { "http://127.0.0.1:" + port + "/MediaPortal/Server" },
          services => ResourceServer.AddApplicationParts(services.AddControllers(), moduleTypes, controllerTypes),
          app =>
          {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
          }));
      return port;
    }

    private static async Task<string> GetAsync(int port, string path)
    {
      using (var client = new HttpClient())
      {
        HttpResponseMessage response = await client.GetAsync("http://127.0.0.1:" + port + path);
        return (int) response.StatusCode + " " + await response.Content.ReadAsStringAsync();
      }
    }

    #endregion

    #region Which assemblies are searched

    /// <summary>
    /// The point of the seam, end to end: a controller from an assembly that registers no middleware is
    /// reached below the URL prefix, over a socket.
    /// </summary>
    [Test]
    public async Task ControllerFromARegisteredControllerAssemblyIsReached()
    {
      int port = StartResourceServerLikeEndpoint(null, new[] { typeof(ApiSeamProbeController) });

      Assert.That(await GetAsync(port, "/MediaPortal/Server/api/probe"), Is.EqualTo("200 probed"));
    }

    [Test]
    public async Task ControllerIsNotReachedWhenItsAssemblyWasNotRegistered()
    {
      // The negative control: without the registration the same controller is invisible, which is exactly what
      // an API assembly used to be. Were this to answer, the test above would only prove that MVC finds the
      // test assembly on its own.
      int port = StartResourceServerLikeEndpoint(null, null);

      Assert.That(await GetAsync(port, "/MediaPortal/Server/api/probe"), Does.StartWith("404"));
    }

    [Test]
    public async Task ControllerFromTheAssemblyOfAResourceAccessModuleIsStillReached()
    {
      // The shortcut the resource server has always used: a plugin reached the API by registering a middleware,
      // and its controllers came along with the assembly. The explicit registration must not drop that, or
      // every plugin controller which relies on the shortcut disappears.
      int port = StartResourceServerLikeEndpoint(new[] { typeof(ApiSeamProbeMiddleware) }, null);

      Assert.That(await GetAsync(port, "/MediaPortal/Server/api/probe"), Is.EqualTo("200 probed"));
    }

    #endregion

    #region The registration itself

    [Test]
    public void ControllerAssembliesAreRegisteredWhileTheServerIsComposed()
    {
      // The resource server is constructible without services, and registering a controller assembly must not
      // start or need anything else.
      ResourceServer server = new ResourceServer();

      Assert.That(() => server.AddHttpControllerAssembly(typeof(ApiSeamProbeController)), Throws.Nothing);
      Assert.That(() => server.AddHttpControllerAssembly(typeof(ApiSeamProbeController)), Throws.Nothing,
          "registering the same assembly twice is harmless");
      Assert.That(() => server.AddHttpControllerAssembly(null), Throws.TypeOf<ArgumentNullException>());
    }

    /// <summary>
    /// The one line each server's composition root calls, checked against a resource server that records what it
    /// was handed. The two hosts are covered end to end by the gates, which ask the running server for an
    /// endpoint that only this assembly serves.
    /// </summary>
    [Test]
    public void TheApiNamesItsOwnAssemblyToTheRegisteredResourceServer()
    {
      RecordingResourceServer server = new RecordingResourceServer();
      ServiceRegistration.Set<IResourceServer>(server);

      MediaPortalServerApi.Register();

      Assert.That(server.ControllerAssemblyTypes, Is.EquivalentTo(new[] { typeof(MediaPortalServerApi).Assembly }));
    }

    [Test]
    public void RegisteringTheApiWithoutAResourceServerSaysWhatIsWrong()
    {
      // Called in the wrong place in a start sequence, this is the failure a future maintainer sees - a sentence
      // about the resource server, not a NullReferenceException in a log file.
      Assert.That(() => MediaPortalServerApi.Register(), Throws.TypeOf<InvalidOperationException>()
          .With.Message.Contains("resource server"));
    }

    #endregion
  }
}