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
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using UPnP.Infrastructure.Http;

namespace Tests.Server.Upnp
{
  /// <summary>
  /// Behaviour tests for <see cref="SharedHttpPortHost"/>, the listener that replaces the http.sys feature of
  /// sharing one TCP port between several hosts on the platforms without http.sys.
  /// </summary>
  /// <remarks>
  /// The rules asserted here are the ones http.sys applies and that MP2 relies on: several endpoints on one
  /// port, the longest matching URL prefix wins, matching is case-insensitive, the request is handed over with
  /// the prefix as PathBase and the remainder as Path (so route matching works the same on both hosts), and
  /// synchronous access to the request stream is allowed (the GENA handling reads it with the XmlSerializer).
  /// </remarks>
  [TestFixture]
  public class TestSharedHttpPortHost
  {
    #region Helpers

    private readonly List<IDisposable> _registrations = new List<IDisposable>();

    [TearDown]
    public void TearDown()
    {
      foreach (IDisposable registration in _registrations)
        registration.Dispose();
      _registrations.Clear();
    }

    /// <summary>
    /// Reserves a port that is currently free. Every test opens its own listener, and none of them uses MP2's
    /// port 55555, so the tests stay independent of an MP2 server that happens to run on the machine.
    /// </summary>
    private static int GetFreePort()
    {
      var listener = new TcpListener(IPAddress.Loopback, 0);
      listener.Start();
      int port = ((IPEndPoint) listener.LocalEndpoint).Port;
      listener.Stop();
      return port;
    }

    private static string Url(int port, string prefix)
    {
      return "http://127.0.0.1:" + port + prefix;
    }

    /// <summary>
    /// Registers a handler that answers with <paramref name="response"/> for all given URL prefixes.
    /// </summary>
    private void RegisterHandler(int port, string response, params string[] prefixes)
    {
      var urls = new string[prefixes.Length];
      for (int i = 0; i < prefixes.Length; i++)
        urls[i] = Url(port, prefixes[i]);
      _registrations.Add(SharedHttpPortHost.Start(urls,
          context => context.Response.WriteAsync(response)));
    }

    private static async Task<string> GetAsync(int port, string path)
    {
      using (var client = new HttpClient())
      {
        HttpResponseMessage response = await client.GetAsync(Url(port, path));
        return (int) response.StatusCode + " " + await response.Content.ReadAsStringAsync();
      }
    }

    #endregion

    #region URL parsing

    [Test]
    public void StartRejectsSecuredUrls()
    {
      Assert.That(
          () => SharedHttpPortHost.Start(new[] { "https://127.0.0.1:443/MediaPortal" },
              context => context.Response.WriteAsync("x")),
          Throws.TypeOf<NotSupportedException>().And.Message.Contains("https://127.0.0.1:443/MediaPortal"));
    }

    [Test]
    public void StartRejectsHostNamesBecauseTheyCannotBeBound()
    {
      Assert.That(
          () => SharedHttpPortHost.Start(new[] { "http://mediaportal-server:55555/MediaPortal" },
              context => context.Response.WriteAsync("x")),
          Throws.TypeOf<NotSupportedException>().And.Message.Contains("mediaportal-server"));
    }

    [Test]
    public void StartRejectsUrlsWithAnUnparsablePort()
    {
      Assert.That(
          () => SharedHttpPortHost.Start(new[] { "http://127.0.0.1:notaport/MediaPortal" },
              context => context.Response.WriteAsync("x")),
          Throws.TypeOf<ArgumentException>().And.Message.Contains("port"));
    }

    [Test]
    public void StartRejectsUrlsOfDifferentPorts()
    {
      Assert.That(
          () => SharedHttpPortHost.Start(
              new[] { "http://127.0.0.1:55001/MediaPortal", "http://127.0.0.1:55002/MediaPortal" },
              context => context.Response.WriteAsync("x")),
          Throws.TypeOf<ArgumentException>().And.Message.Contains("same port"));
    }

    #endregion

    #region Dispatching

    [Test]
    public async Task TwoEndpointsShareOnePortAndSeeTheirOwnRequests()
    {
      int port = GetFreePort();
      RegisterHandler(port, "resource", "/MediaPortal/Server");
      RegisterHandler(port, "upnp", "/MediaPortal/UPnPServer_1a2b3c");

      Assert.That(await GetAsync(port, "/MediaPortal/Server/api/Resources"), Is.EqualTo("200 resource"));
      Assert.That(await GetAsync(port, "/MediaPortal/UPnPServer_1a2b3c/description.xml"), Is.EqualTo("200 upnp"));
    }

    [Test]
    public async Task TheEndpointWithTheLongestMatchingPrefixWins()
    {
      int port = GetFreePort();
      RegisterHandler(port, "outer", "/MediaPortal");
      RegisterHandler(port, "inner", "/MediaPortal/Server");

      Assert.That(await GetAsync(port, "/MediaPortal/Server/api/Resources"), Is.EqualTo("200 inner"));
      Assert.That(await GetAsync(port, "/MediaPortal/Client"), Is.EqualTo("200 outer"));
    }

    [Test]
    public async Task MatchingIgnoresTheCaseOfThePathAndRespectsPathBoundaries()
    {
      int port = GetFreePort();
      RegisterHandler(port, "server", "/MediaPortal/Server");

      Assert.That(await GetAsync(port, "/MEDIAPORTAL/server/api/Resources"), Is.EqualTo("200 server"));
      // A prefix only matches whole path segments, otherwise an endpoint for "/Server" would also get the
      // requests of a neighbour called "/ServerRoom".
      Assert.That(await GetAsync(port, "/MediaPortal/ServerRoom/api"), Does.StartWith("404"));
    }

    [Test]
    public async Task RequestsWithoutAMatchingEndpointAreNotAnswered()
    {
      int port = GetFreePort();
      RegisterHandler(port, "server", "/MediaPortal/Server");

      Assert.That(await GetAsync(port, "/unrelated"), Does.StartWith("404"));
    }

    [Test]
    public async Task EndpointsSeeTheRequestInTheShapeHttpSysHandsItOver()
    {
      int port = GetFreePort();
      string url = Url(port, "/MediaPortal/UPnPServer_1a2b3c");
      _registrations.Add(SharedHttpPortHost.Start(new[] { url }, context =>
      {
        // Measured on Windows: http.sys reports the prefix it registered as PathBase and lets Path continue
        // behind it. The UPnP endpoints build their matching key from the full URI - PathBase + Path - so
        // they behave the same in both shapes, but MVC route matching sees Path only, which is exactly why
        // the two hosts must agree on this.
        string text = "Path=" + context.Request.Path + " PathBase=" + context.Request.PathBase +
                      " FullPath=" + context.Request.PathBase.Add(context.Request.Path);
        return context.Response.WriteAsync(text);
      }));

      Assert.That(await GetAsync(port, "/MediaPortal/UPnPServer_1a2b3c/description.xml"),
          Is.EqualTo("200 Path=/description.xml PathBase=/MediaPortal/UPnPServer_1a2b3c" +
                     " FullPath=/MediaPortal/UPnPServer_1a2b3c/description.xml"));
    }

    [Test]
    public async Task TheLongestPrefixBecomesThePathBaseOfTheEndpoint()
    {
      int port = GetFreePort();
      RegisterHandler(port, "short", "/MediaPortal");
      _registrations.Add(SharedHttpPortHost.Start(new[] { Url(port, "/MediaPortal/Server/deep") }, context =>
          context.Response.WriteAsync("PathBase=" + context.Request.PathBase + " Path=" + context.Request.Path)));

      // Two endpoints on one port, one prefix inside the other: each has to see itself as the base, and the
      // shorter one still serves everything the longer one does not claim.
      Assert.That(await GetAsync(port, "/MediaPortal/x"), Is.EqualTo("200 short"));
      Assert.That(await GetAsync(port, "/MediaPortal/Server/other"), Is.EqualTo("200 short"));
      Assert.That(await GetAsync(port, "/MediaPortal/Server/deep/x"),
          Is.EqualTo("200 PathBase=/MediaPortal/Server/deep Path=/x"));
      Assert.That(await GetAsync(port, "/MediaPortal/Server/deep"),
          Is.EqualTo("200 PathBase=/MediaPortal/Server/deep Path="));
    }

    [Test]
    public async Task SynchronousAccessToTheRequestStreamIsAllowed()
    {
      int port = GetFreePort();
      string url = Url(port, "/MediaPortal/UPnPServer_1a2b3c");
      _registrations.Add(SharedHttpPortHost.Start(new[] { url }, async context =>
      {
        // GENA event notifications are deserialized by the XmlSerializer, which reads the request stream
        // synchronously. http.sys is configured with AllowSynchronousIO for exactly that reason.
        using (var reader = new System.IO.StreamReader(context.Request.Body, Encoding.UTF8))
        {
          string body = reader.ReadToEnd();
          await context.Response.WriteAsync("body:" + body);
        }
      }));

      using (var client = new HttpClient())
      {
        HttpResponseMessage response = await client.PostAsync(url, new StringContent("<Body/>"));
        Assert.That((int) response.StatusCode, Is.EqualTo(200));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("body:<Body/>"));
      }
    }

    #endregion

    #region Pipelines

    [Test]
    public async Task EndpointWithPipelineServesItsOwnPrefixOnlyAndToleratesEndpointsBelowIt()
    {
      int port = GetFreePort();
      int pipelineRequests = 0;
      Action<IServiceCollection> configureServices = services => { };
      Action<IApplicationBuilder> configurePipeline = app => app.Run(context =>
      {
        pipelineRequests++;
        return context.Response.WriteAsync("pipeline");
      });

      _registrations.Add(SharedHttpPortHost.Start(new[] { Url(port, "/MediaPortal/Server") },
          configureServices, configurePipeline));
      // The resource server registers the endpoints of its resource access modules after starting its own
      // host. Those have to win over the pipeline below them, like the separate http.sys hosts they replace.
      RegisterHandler(port, "thumbnail", "/MediaPortal/Server/Thumbnails");

      Assert.That(await GetAsync(port, "/MediaPortal/Server/api/Resources"), Is.EqualTo("200 pipeline"));
      Assert.That(await GetAsync(port, "/MediaPortal/Server/Thumbnails/1"), Is.EqualTo("200 thumbnail"));
      Assert.That(await GetAsync(port, "/MediaPortal/UPnPServer_1a2b3c/x"), Does.StartWith("404"));
      Assert.That(pipelineRequests, Is.EqualTo(1),
          "the pipeline must not be asked about requests of other endpoints");
    }

    /// <summary>
    /// The property the API of the new frontend depends on: routes are written relative to the URL prefix of
    /// the endpoint, so the same code answers under "/MediaPortal/Server" on Windows (http.sys) and on every
    /// other platform (the shared listener). Everything below the prefix that no route claims falls through to
    /// the rest of the pipeline, which is where the resource access modules of the resource server live.
    /// </summary>
    [Test]
    public async Task PipelineRoutesAreRelativeToThePrefixOfTheEndpoint()
    {
      int port = GetFreePort();
      _registrations.Add(SharedHttpPortHost.Start(new[] { Url(port, "/MediaPortal/Server") },
          services => services.AddRouting(),
          app =>
          {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
              endpoints.MapGet("health", context => context.Response.WriteAsync("healthy"));
              endpoints.MapGet("api/ping", context => context.Response.WriteAsync("pong"));
            });
            app.Run(context => context.Response.WriteAsync("rest of the pipeline"));
          }));

      Assert.That(await GetAsync(port, "/MediaPortal/Server/health"), Is.EqualTo("200 healthy"));
      Assert.That(await GetAsync(port, "/MediaPortal/Server/api/ping"), Is.EqualTo("200 pong"));
      Assert.That(await GetAsync(port, "/MediaPortal/Server/anythingElse"), Is.EqualTo("200 rest of the pipeline"));
    }

    [Test]
    public void EndpointWithPipelineHasToBeRegisteredFirst()    {
      int port = GetFreePort();
      RegisterHandler(port, "upnp", "/MediaPortal/UPnPServer_1a2b3c");

      // The service provider of a running listener cannot be extended, so an endpoint that needs MVC
      // controllers has to be registered before the listener is built.
      Assert.That(
          () => SharedHttpPortHost.Start(new[] { Url(port, "/MediaPortal/Server") },
              services => { }, app => app.Run(context => context.Response.WriteAsync("x"))),
          Throws.TypeOf<InvalidOperationException>().And.Message.Contains("before"));
    }

    #endregion

    #region Lifetimes

    [Test]
    public void RegisteringTheSamePrefixOnOnePortTwiceIsRejected()
    {
      int port = GetFreePort();
      RegisterHandler(port, "first", "/MediaPortal/Server");

      Assert.That(
          () => RegisterHandler(port, "second", "/MediaPortal/Server"),
          Throws.TypeOf<InvalidOperationException>().And.Message.Contains("/MediaPortal/Server"));
    }

    [Test]
    public async Task DisposingAnEndpointKeepsItsNeighboursAndFreesThePort()
    {
      int port = GetFreePort();
      IDisposable resource = SharedHttpPortHost.Start(new[] { Url(port, "/MediaPortal/Server") },
          context => context.Response.WriteAsync("resource"));
      IDisposable upnp = SharedHttpPortHost.Start(new[] { Url(port, "/MediaPortal/UPnPServer_1a2b3c") },
          context => context.Response.WriteAsync("upnp"));

      resource.Dispose();

      // The other endpoint of the port survives, and a new endpoint can be opened for the freed prefix.
      Assert.That(await GetAsync(port, "/MediaPortal/UPnPServer_1a2b3c/description.xml"), Is.EqualTo("200 upnp"));
      _registrations.Add(SharedHttpPortHost.Start(new[] { Url(port, "/MediaPortal/Server") },
          context => context.Response.WriteAsync("resource-again")));
      Assert.That(await GetAsync(port, "/MediaPortal/Server/api/Resources"), Is.EqualTo("200 resource-again"));

      // After the last endpoint of the port is gone the port has to be free again, otherwise restarting the
      // resource server inside one process would fail to bind.
      upnp.Dispose();
      _registrations[0].Dispose();
      _registrations.Clear();
      var listener = new TcpListener(IPAddress.Loopback, port);
      Assert.That(() => listener.Start(), Throws.Nothing);
      listener.Stop();
    }

    #endregion
  }
}