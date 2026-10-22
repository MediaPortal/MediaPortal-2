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
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.Threading;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor.Settings;
using MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor.Settings;
using MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.Settings;
using MediaPortal.Server.Api;
using Moq;
using NUnit.Framework;
using UPnP.Infrastructure.Http;

namespace Tests.Server.Api
{
  /// <summary>
  /// What the extractors are told to fetch over HTTP: whose details of the people behind a title a wish may
  /// change, how many of them it may ask for, and what a number outside the range the endpoint states does -
  /// which is nothing at all, to nothing at all. The settings classes here are the reference's own, loaded for
  /// real, because the endpoint reaches them by name through the settings manager and a stand-in of our own
  /// would prove nothing about that lookup.
  /// </summary>
  [TestFixture]
  public class TestMetadataSettingsOverHttp
  {
    private const string ENDPOINT = "/api/media/settings/metadata";
    private const string PREFIX = "/MediaPortal/Server";

    private readonly List<IDisposable> _listeners = new List<IDisposable>();
    private readonly Dictionary<Type, object> _store = new Dictionary<Type, object>();

    private string _baseUrl;
    private MovieMetadataExtractorSettings _movie;
    private SeriesMetadataExtractorSettings _series;
    private AudioMetadataExtractorSettings _audio;

    [TearDown]
    public void StopEverything()
    {
      foreach (IDisposable listener in _listeners)
        listener.Dispose();
      _listeners.Clear();
      _store.Clear();

      ServiceRegistration.Remove<ISystemResolver>();
      ServiceRegistration.Remove<ISettingsManager>();
      ServiceRegistration.Remove<IThreadPool>();
      ServiceRegistration.Remove<IUserProfileDataManagement>();
      ServiceRegistration.Remove<ILogger>();
      ServiceRegistration.Remove<IMediaLibrary>();
    }

    /// <summary>
    /// Brings the API up over a port of its own with the three extractors' settings classes held by a stand-in
    /// settings manager. Holding their instances is also how their assemblies reach the process: the endpoint
    /// looks each settings class up among the assemblies the running server has loaded, so an extractor this
    /// test never touched would read as absent - which is the honest answer it gives on a real server too.
    /// </summary>
    private void StartServer(bool authorize = false)
    {
      ServiceRegistration.Set<ILogger>(new DefaultLogger(new StringWriter(), LogLevel.All, false, true));

      Mock<ISystemResolver> resolver = new Mock<ISystemResolver>();
      resolver.SetupGet(r => r.SystemType).Returns(SystemType.Server);
      ServiceRegistration.Set<ISystemResolver>(resolver.Object);

      ServiceRegistration.Set<IMediaLibrary>(new Mock<IMediaLibrary>().Object);

      // One flag off, one flag on, and one count moved per class, so an answer that copied the endpoint's own
      // guesses rather than the stored values shows up as a wrong assertion instead of a coincidence. The
      // reference keeps its defaults in [Setting] attributes, which the real settings manager applies and a bare
      // constructor does not, so what the classes hold is written out here.
      _movie = new MovieMetadataExtractorSettings
      {
        IncludeActorDetails = false,
        IncludeWriterDetails = true,
        MaximumActorCount = 7,
        MaximumCharacterCount = 10
      };
      _series = new SeriesMetadataExtractorSettings
      {
        IncludeTVNetworkDetails = false,
        MaximumCharacterCount = 13
      };
      _audio = new AudioMetadataExtractorSettings { IncludeConductorDetails = false };
      _store[typeof(MovieMetadataExtractorSettings)] = _movie;
      _store[typeof(SeriesMetadataExtractorSettings)] = _series;
      _store[typeof(AudioMetadataExtractorSettings)] = _audio;

      Mock<ISettingsManager> settingsManager = new Mock<ISettingsManager>();
      settingsManager.Setup(s => s.Load(It.IsAny<Type>()))
          .Returns<Type>(type => _store.TryGetValue(type, out object held)
              ? held : Activator.CreateInstance(type));
      settingsManager.Setup(s => s.Save(It.IsAny<object>()))
          .Callback<object>(held => _store[held.GetType()] = held);
      // The guard that decides whether a stranger may change any of this asks for the server's own settings
      // object by its generic overload, so the stand-in has to answer that one too.
      settingsManager.Setup(s => s.Load<ServerSettings>())
          .Returns(new ServerSettings { WebAutorizationEnabled = authorize });
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

    static JsonElement Read(string body)
    {
      return JsonDocument.Parse(body).RootElement.Clone();
    }

    /// <summary>Whether the answer says nothing about a field - absent or null, which mean the same thing here.</summary>
    static bool Absent(JsonElement block, string field)
    {
      return !block.TryGetProperty(field, out JsonElement value) || value.ValueKind == JsonValueKind.Null;
    }

    [Test]
    public async Task TheDetailFlagsAndTheCountsAnswerWithWhatTheSettingsClassesHold()
    {
      StartServer();

      (HttpStatusCode code, string body) = await Get(_baseUrl + ENDPOINT);
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);

      JsonElement movie = Read(body).GetProperty("movie");
      Assert.That(movie.GetProperty("includeActorDetails").GetBoolean(), Is.False,
          "the switch a screen is offered is the switch the extractor reads, not the other way round");
      Assert.That(movie.GetProperty("includeWriterDetails").GetBoolean(), Is.True,
          "a flag nobody touched answers with what the settings class holds, not with a value the endpoint made up");
      Assert.That(movie.GetProperty("maximumActorCount").GetInt32(), Is.EqualTo(7),
          "the number on the screen is the number the settings class holds");
      Assert.That(movie.GetProperty("maximumCharacterCount").GetInt32(), Is.EqualTo(10),
          "and the count beside it is the number the class holds, not one the endpoint invented");

      JsonElement series = Read(body).GetProperty("series");
      Assert.That(series.GetProperty("includeTVNetworkDetails").GetBoolean(), Is.False);
      Assert.That(series.GetProperty("maximumCharacterCount").GetInt32(), Is.EqualTo(13));

      JsonElement audio = Read(body).GetProperty("audio");
      Assert.That(audio.GetProperty("includeConductorDetails").GetBoolean(), Is.False);
      Assert.That(Absent(audio, "maximumActorCount"), Is.True,
          "the music extractor keeps no count of its own, so the answer invents none");
      Assert.That(Absent(audio, "includeTVNetworkDetails"), Is.True,
          "and no television network either - a switch nothing reads has no row to offer");
    }

    [Test]
    public async Task AChangedDetailFlagMovesItselfAndNothingElse()
    {
      StartServer();

      (HttpStatusCode code, string body) =
          await Put(_baseUrl + ENDPOINT, "{\"movie\":{\"includeActorDetails\":true}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);

      Assert.That(_movie.IncludeActorDetails, Is.True,
          "the answer is worth nothing when the settings class the extractor reads still holds the old switch");
      Assert.That(_movie.IncludeWriterDetails, Is.True, "a flag the wish never named stands where it stood");
      Assert.That(_movie.MaximumActorCount, Is.EqualTo(7), "a count the wish never named is not written back");
      Assert.That(_series.IncludeTVNetworkDetails, Is.False, "and another extractor is not touched at all");
      Assert.That(Read(body).GetProperty("movie").GetProperty("includeActorDetails").GetBoolean(), Is.True,
          "the answer of a write is the whole state after it, not only the field that moved");
    }

    [Test]
    public async Task ACountPutBackWhereItWasAsksForNothing()
    {
      StartServer();

      // Screens send the whole form, not only what was touched, so a number that never moved must leave the
      // settings class exactly as it was - the extractor should not even notice the pass.
      (HttpStatusCode code, string body) =
          await Put(_baseUrl + ENDPOINT, "{\"movie\":{\"maximumActorCount\":7}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);
      Assert.That(_movie.MaximumActorCount, Is.EqualTo(7));
      Assert.That(Read(body).GetProperty("nextImportPass").GetBoolean(), Is.True,
          "what these settings speak to is the next import pass, whatever moved");
    }

    [Test]
    public async Task ACountAboveTheRangeIsRefusedAndNothingAtAllIsStored()
    {
      StartServer();

      // The wish carries a switch that would have been stored on its own, so the refusal has to prove it applied
      // nothing: half a wish stored would leave a screen reading back a state nobody asked for.
      (HttpStatusCode code, string body) = await Put(_baseUrl + ENDPOINT,
          "{\"movie\":{\"skipOnlineSearches\":true,\"maximumActorCount\":101}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.BadRequest), body);
      Assert.That(body, Does.Contain("between 0 and 100"), "the refusal says what would have been stored");
      Assert.That(_movie.MaximumActorCount, Is.EqualTo(7), "the refused number stayed out of the settings class");
      Assert.That(_movie.SkipOnlineSearches, Is.False, "and so did the legal switch travelling with it");
    }

    [Test]
    public async Task ACountBelowTheRangeIsRefusedByTheSameSentence()
    {
      StartServer();

      // The reference hands the stored count straight to LINQ's Take, which throws on a negative number, so a
      // negative count is not a value a running extractor can be handed; the endpoint says so when it is asked
      // for instead of leaving the next import pass to find out.
      (HttpStatusCode code, string body) =
          await Put(_baseUrl + ENDPOINT, "{\"series\":{\"maximumCharacterCount\":-1}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.BadRequest), body);
      Assert.That(body, Does.Contain("characters"), "the refusal names which of the two counts was meant");
      Assert.That(_series.MaximumCharacterCount, Is.EqualTo(13));
    }

    [Test]
    public async Task TheEndsOfTheRangeAreBothStoredAndAnsweredBack()
    {
      StartServer();

      // Zero is inside the range on purpose: keeping nobody is a thing a household may want, and the reference
      // reads it as "none" rather than as an error.
      (HttpStatusCode none, string noneBody) =
          await Put(_baseUrl + ENDPOINT, "{\"movie\":{\"maximumActorCount\":0}}");
      Assert.That(none, Is.EqualTo(HttpStatusCode.OK), noneBody);
      Assert.That(_movie.MaximumActorCount, Is.EqualTo(0));

      (HttpStatusCode all, string allBody) =
          await Put(_baseUrl + ENDPOINT, "{\"movie\":{\"maximumActorCount\":100}}");
      Assert.That(all, Is.EqualTo(HttpStatusCode.OK), allBody);
      Assert.That(_movie.MaximumActorCount, Is.EqualTo(100));
      Assert.That(Read(allBody).GetProperty("movie").GetProperty("maximumActorCount").GetInt32(), Is.EqualTo(100),
          "the answer comes back with the count in it, so a screen redraws what was stored");
    }

    [Test]
    public async Task AWordWhereACountWasAskedForIsRefused()
    {
      StartServer();

      (HttpStatusCode code, string body) =
          await Put(_baseUrl + ENDPOINT, "{\"movie\":{\"maximumActorCount\":\"many\"}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.BadRequest), body);
      Assert.That(_movie.MaximumActorCount, Is.EqualTo(7), "and the settings class never saw the word");
    }
  }
}
