using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MediaPortal.Common.Services.ResourceAccess;
using NUnit.Framework;

namespace Tests.Server.Common
{
  /// <summary>
  /// Tests for the health endpoint of the resource server.
  /// </summary>
  /// <remarks>
  /// The endpoint exists because "the listener started" and "the server answers" are different claims, and
  /// because a service manager on Linux or Android needs one address that tells it when the server is ready.
  /// It has to answer without plugins, users, tokens or a database, so it is testable without any of them.
  /// </remarks>
  [TestFixture]
  public class TestResourceServerHealth
  {
    [Test]
    public void HealthBodyNamesTheServerAndItsVersion()
    {
      JsonElement body = JsonSerializer.Deserialize<JsonElement>(
          ResourceServer.HealthJson(Request("/MediaPortal/Server", "/health")));

      Assert.That(body.GetProperty("name").GetString(), Is.EqualTo("MediaPortal 2 Server"));
      Assert.That(body.GetProperty("version").GetString(), Is.Not.Null.And.Not.Empty);
    }

    /// <summary>
    /// The body reports the request as the server saw it. That is how the two gates measure the difference
    /// between http.sys, which puts the registered prefix into PathBase, and the shared listener, which
    /// keeps it in Path - instead of anyone having to assume it.
    /// </summary>
    [Test]
    public void HealthBodyReportsTheRequestPathAsTheServerSeesIt()
    {
      JsonElement stripped = JsonSerializer.Deserialize<JsonElement>(
          ResourceServer.HealthJson(Request("/MediaPortal/Server", "/health")));
      Assert.That(stripped.GetProperty("pathBase").GetString(), Is.EqualTo("/MediaPortal/Server"));
      Assert.That(stripped.GetProperty("path").GetString(), Is.EqualTo("/health"));

      JsonElement unstripped = JsonSerializer.Deserialize<JsonElement>(
          ResourceServer.HealthJson(Request(string.Empty, "/MediaPortal/Server/health")));
      Assert.That(unstripped.GetProperty("pathBase").GetString(), Is.Null.Or.Empty);
      Assert.That(unstripped.GetProperty("path").GetString(), Is.EqualTo("/MediaPortal/Server/health"));
    }

    [Test]
    public void HealthBodyCarriesARoundTrippableUtcTime()
    {
      JsonElement body = JsonSerializer.Deserialize<JsonElement>(
          ResourceServer.HealthJson(Request(string.Empty, "/health")));

      DateTime before = DateTime.UtcNow.AddSeconds(-5);
      DateTime utc = DateTime.Parse(body.GetProperty("utc").GetString(), null,
          System.Globalization.DateTimeStyles.RoundtripKind);

      Assert.That(utc.Kind, Is.EqualTo(DateTimeKind.Utc));
      Assert.That(utc, Is.GreaterThanOrEqualTo(before).And.LessThanOrEqualTo(DateTime.UtcNow.AddSeconds(5)));
    }

    [Test]
    public async Task HealthAnswerIsOkayAndJson()
    {
      DefaultHttpContext context = new DefaultHttpContext();
      MemoryStream body = new MemoryStream();
      context.Response.Body = body;

      await ResourceServer.WriteHealthResponse(context);

      Assert.That(context.Response.StatusCode, Is.EqualTo(200));
      Assert.That(context.Response.ContentType, Is.EqualTo("application/json; charset=utf-8"));
      Assert.That(Encoding.UTF8.GetString(body.ToArray()), Does.Contain("MediaPortal 2 Server"));
    }

    static HttpRequest Request(string pathBase, string path)
    {
      DefaultHttpContext context = new DefaultHttpContext();
      context.Request.PathBase = pathBase;
      context.Request.Path = path;
      return context.Request;
    }

    [Test]
    public void HealthUrlIsBelowTheResourceServerBasePath()
    {
      // The base path is resolved from the running system ("/Server" there, "/Client" in the client), so
      // the endpoint contributes only its suffix. The gates probe the full URL end to end.
      Assert.That(ResourceServer.HEALTH_URL_SUFFIX, Is.EqualTo("/health"));
      Assert.That(ResourceServer.HEALTH_URL_SUFFIX, Does.StartWith("/"));
      Assert.That(ResourceServer.HEALTH_URL_SUFFIX.Trim('/'), Does.Not.Contain('/'),
          "the suffix has to be one segment, so it cannot collide with the api/{controller} route");
    }
  }
}
