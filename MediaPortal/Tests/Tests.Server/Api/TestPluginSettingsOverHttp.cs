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
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.Threading;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;
using MediaPortal.Server.Api;
using Moq;
using NUnit.Framework;
using UPnP.Infrastructure.Http;

namespace Tests.Server.Api
{
  /// <summary>
  /// The plugins' settings sections over HTTP: what a screen is told exists, what it may read, what a value has
  /// to look like before the server stores it, and what a server that wants to know its callers does to a
  /// stranger who comes to change them.
  /// </summary>
  [TestFixture]
  public class TestPluginSettingsOverHttp
  {
    private const string PREFIX = "/MediaPortal/Server";

    private readonly List<IDisposable> _listeners = new List<IDisposable>();
    private readonly Dictionary<Type, object> _store = new Dictionary<Type, object>();

    private string _baseUrl;
    private FanArtSettings _fanArt;
    private LogSettings _logging;
    private NfoMovieMetadataExtractorSettings _nfoMovie;

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
    /// Brings the API up over a port of its own with a settings manager that stands in for the files the real
    /// one reads: the object a <c>Load</c> answers with is the object the test inspects afterwards, and a
    /// <c>Save</c> puts the very instance back, so "refused" can be proved to mean "nothing was stored".
    /// </summary>
    private void StartServer(bool authorize = false)
    {
      ServiceRegistration.Set<ILogger>(new DefaultLogger(new StringWriter(), LogLevel.All, false, true));

      // The reader of the files a media manager leaves beside a title asks the localiser which language it speaks
      // the moment its settings are built. On the running server that service is always there; in a test somebody
      // has to say so, or the first settings object of that reader throws.
      if (!ServiceRegistration.IsRegistered<ILocalization>())
        ServiceRegistration.Set<ILocalization>(new NoLocalization());

      // Two of the catalogue's sections name the .nfo reader's classes rather than referencing them, because the
      // API assembly may not know the plugins, and a name is only found among assemblies this process has actually
      // loaded. Saying it here is what the server's plugin system does there.
      TheSidecarReaderIsLoaded();

      Mock<ISystemResolver> resolver = new Mock<ISystemResolver>();
      resolver.SetupGet(r => r.SystemType).Returns(SystemType.Server);
      ServiceRegistration.Set<ISystemResolver>(resolver.Object);

      ServiceRegistration.Set<IMediaLibrary>(new Mock<IMediaLibrary>().Object);

      _fanArt = new FanArtSettings { MaxPosterFanArt = 1, MaxBackdropFanArt = 3 };
      _logging = new LogSettings { LogLevel = LogLevel.Information };
      _nfoMovie = new NfoMovieMetadataExtractorSettings
      {
        IncludeActorDetails = true,
        IncludeCharacterDetails = false,
        MaximumActorCount = 12
      };
      _store[typeof(FanArtSettings)] = _fanArt;
      _store[typeof(LogSettings)] = _logging;
      _store[typeof(NfoMovieMetadataExtractorSettings)] = _nfoMovie;

      Mock<ISettingsManager> settingsManager = new Mock<ISettingsManager>();
      settingsManager.Setup(s => s.Load(It.IsAny<Type>()))
          .Returns<Type>(type => _store.TryGetValue(type, out object held)
              ? held : Activator.CreateInstance(type));
      settingsManager.Setup(s => s.Save(It.IsAny<object>()))
          .Callback<object>(held => _store[held.GetType()] = held);
      // The guard that decides whether a stranger may read any of this asks for the server's own settings
      // object by its generic overload, so the stand-in has to answer that one too - a null there is a crash
      // behind a 500, not a refusal.
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

    /// <summary>
    /// Hands the .nfo reader's assembly to the runtime, so that looking its classes up by name succeeds.
    /// </summary>
    /// <remarks>
    /// The catalogue names those classes instead of referencing them - the API assembly stays ignorant of the
    /// plugins it serves - and a name is only found among assemblies the process has actually taken. On the
    /// server the plugin system does that taking; a test has to say it itself. Naming the assembly loads it
    /// without building anything of it, which is all the lookup needs.
    /// </remarks>
    static void TheSidecarReaderIsLoaded()
    {
      Assembly.Load("NfoMetadataExtractors");
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

    static JsonElement FieldOf(JsonElement section, string key)
    {
      foreach (JsonElement field in section.GetProperty("fields").EnumerateArray())
      {
        if (field.GetProperty("key").GetString() == key)
          return field;
      }
      Assert.Fail("the answer named no field \"" + key + "\": " + section);
      return default;
    }

    static List<string> NamesOf(JsonElement array)
    {
      List<string> names = new List<string>();
      foreach (JsonElement entry in array.EnumerateArray())
        names.Add(entry.GetString());
      return names;
    }

    [Test]
    public async Task TheOpenSectionsAreListed()
    {
      StartServer();

      (HttpStatusCode code, string body) = await Get(_baseUrl + "/api/media/settings/sections");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);

      List<string> keys = new List<string>();
      foreach (JsonElement section in Read(body).GetProperty("sections").EnumerateArray())
      {
        keys.Add(section.GetProperty("key").GetString());
        // A list row without words of its own is a row a viewer cannot decide about, so every section owes a
        // title, a sentence, and the part of the server the answer belongs to.
        Assert.That(section.GetProperty("title").GetString(), Is.Not.Empty);
        Assert.That(section.GetProperty("description").GetString(), Is.Not.Empty);
        Assert.That(section.GetProperty("owner").GetString(), Is.Not.Empty);
        Assert.That(section.GetProperty("fieldCount").GetInt32(), Is.GreaterThan(0));
      }

      Assert.That(keys, Does.Contain("fanart"), "how many pictures a title keeps is a household's question");
      Assert.That(keys, Does.Contain("logging"), "the log level is what a support question needs first");
    }

    [Test]
    public async Task EveryFieldOfEveryOpenSectionAnswersWithAValue()
    {
      StartServer();

      // The table of sections and the classes behind it live in different files, and a row that outlives the
      // property it names shows up on a television as a field without a value - which the person standing in
      // front of it can neither explain nor fix. So the whole table is walked here, section by section, and
      // every field has to answer.
      (HttpStatusCode listCode, string listBody) = await Get(_baseUrl + "/api/media/settings/sections");
      Assert.That(listCode, Is.EqualTo(HttpStatusCode.OK), listBody);

      foreach (JsonElement summary in Read(listBody).GetProperty("sections").EnumerateArray())
      {
        string key = summary.GetProperty("key").GetString();
        (HttpStatusCode code, string body) = await Get(_baseUrl + "/api/media/settings/sections/" + key);
        Assert.That(code, Is.EqualTo(HttpStatusCode.OK), key + ": " + body);

        foreach (JsonElement field in Read(body).GetProperty("fields").EnumerateArray())
        {
          string name = field.GetProperty("key").GetString();
          Assert.That(field.GetProperty("value").ValueKind, Is.Not.EqualTo(JsonValueKind.Null),
              "the section \"" + key + "\" opened the field \"" + name + "\" but has no value for it");
          Assert.That(new string[] { "toggle", "number", "choice" },
              Does.Contain(field.GetProperty("kind").GetString()),
              "the field \"" + name + "\" wears a kind no screen knows how to draw");
          if (field.GetProperty("kind").GetString() == "choice")
            Assert.That(field.GetProperty("options").GetArrayLength(), Is.GreaterThan(0),
                "a choice without names to offer is a field nobody can fill");
        }
      }
    }

    [Test]
    public void EverySectionNamesASettingsClassThatCanActuallyBeFound()
    {
      // Most of the catalogue's rows name their class outright and the compiler checks them. The sections that
      // belong to a plugin cannot be named that way, so nothing but this test stands between a renamed class
      // and a section that quietly stops being offered - which is the sort of silent disappearance nobody
      // notices from a sofa.
      TheSidecarReaderIsLoaded();

      foreach (PluginSettingsCatalog.Section section in PluginSettingsCatalog.Sections)
      {
        Assert.That(section.ResolveSettings(), Is.Not.Null,
            "the section \"" + section.Key + "\" offers settings of a class that cannot be found ("
            + section.SettingsTypeName + ")");
      }
    }

    [Test]
    public async Task TheSidecarReaderStandsInTheListWhereItRuns()
    {
      StartServer();

      (HttpStatusCode code, string body) = await Get(_baseUrl + "/api/media/settings/sections");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);

      List<string> keys = new List<string>();
      foreach (JsonElement section in Read(body).GetProperty("sections").EnumerateArray())
        keys.Add(section.GetProperty("key").GetString());

      // The reader of the files a media manager leaves beside a title is the one extractor this server runs
      // everywhere, so its settings are asked here and not only in XML by somebody with a shell.
      Assert.That(keys, Does.Contain("nfo_movie"), "a film's sidecar file says who played in it");
      Assert.That(keys, Does.Contain("nfo_series"), "a series' says the same");
      Assert.That(keys, Does.Contain("nfo_audio"), "and an album's says who made it");
    }

    [Test]
    public async Task ASidecarSectionAnswersWithTheDetailsItTakesAndHowManyItKeeps()
    {
      StartServer();

      (HttpStatusCode code, string body) = await Get(_baseUrl + "/api/media/settings/sections/nfo_movie");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);

      JsonElement section = Read(body);
      Assert.That(FieldOf(section, "include_actors").GetProperty("kind").GetString(), Is.EqualTo("toggle"));
      Assert.That(FieldOf(section, "include_actors").GetProperty("value").GetBoolean(), Is.True);
      Assert.That(FieldOf(section, "include_characters").GetProperty("value").GetBoolean(), Is.False);

      JsonElement actors = FieldOf(section, "max_actors");
      Assert.That(actors.GetProperty("kind").GetString(), Is.EqualTo("number"));
      Assert.That(actors.GetProperty("value").GetInt32(), Is.EqualTo(12),
          "a plugin's own number reaches the screen the same way the library's does");
      Assert.That(actors.GetProperty("max").GetInt32(), Is.EqualTo(100),
          "the same ground the metadata endpoint settled on for the same question");
    }

    [Test]
    public async Task ATurnedOffDetailOfTheSidecarReaderStaysOffInTheReader()
    {
      StartServer();

      (HttpStatusCode code, string body) = await Put(_baseUrl + "/api/media/settings/sections/nfo_movie",
          "{\"values\":{\"include_actors\":false}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);
      Assert.That(_nfoMovie.IncludeActorDetails, Is.False,
          "the reader is what asks for these settings, so the reader is where the answer has to be");
      Assert.That(NamesOf(Read(body).GetProperty("restartNeededFor")), Is.Empty,
          "the next title read comes out differently without the server coming back");
    }

    [Test]
    public async Task TooManyActorsForTheSidecarReaderIsRefusedAndItsNumberStands()
    {
      StartServer();

      (HttpStatusCode code, string body) = await Put(_baseUrl + "/api/media/settings/sections/nfo_movie",
          "{\"values\":{\"max_actors\":5000}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.BadRequest), body);
      Assert.That(body, Does.Contain("between 0 and 100"));
      Assert.That(_nfoMovie.MaximumActorCount, Is.EqualTo(12), "the reader keeps the number it had");
    }

    [Test]
    public async Task ASectionAnswersWithItsFieldsAndTheValuesTheServerHolds()
    {
      StartServer();

      (HttpStatusCode code, string body) = await Get(_baseUrl + "/api/media/settings/sections/fanart");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);

      JsonElement section = Read(body);
      JsonElement posters = FieldOf(section, "max_posters");
      Assert.That(posters.GetProperty("kind").GetString(), Is.EqualTo("number"));
      Assert.That(posters.GetProperty("value").GetInt32(), Is.EqualTo(1),
          "the number on the screen is the number the settings manager holds, not one the endpoint invented");
      Assert.That(posters.GetProperty("min").GetInt32(), Is.EqualTo(1));
      Assert.That(posters.GetProperty("max").GetInt32(), Is.EqualTo(20));
      Assert.That(posters.GetProperty("options").GetArrayLength(), Is.EqualTo(0),
          "a number is asked for a number, not for a name");
      Assert.That(FieldOf(section, "max_backdrops").GetProperty("value").GetInt32(), Is.EqualTo(3));
    }

    [Test]
    public async Task AStoredNumberReachesTheSettingsTheLibraryReads()
    {
      StartServer();

      (HttpStatusCode code, string body) =
          await Put(_baseUrl + "/api/media/settings/sections/fanart", "{\"values\":{\"max_backdrops\":5}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);

      Assert.That(_fanArt.MaxBackdropFanArt, Is.EqualTo(5),
          "the answer is worth nothing when the settings class the plugin loads still holds the old number");
      Assert.That(NamesOf(Read(body).GetProperty("restartNeededFor")), Is.Empty,
          "the next fetch already counts with the new number, so nothing here asks for a restart");
    }

    [Test]
    public async Task AFieldTheRunningServerOnlyReadsOnStartupSaysSo()
    {
      StartServer();

      (HttpStatusCode code, string body) =
          await Put(_baseUrl + "/api/media/settings/sections/logging", "{\"values\":{\"log_level\":\"Debug\"}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);
      Assert.That(_logging.LogLevel, Is.EqualTo(LogLevel.Debug));

      JsonElement answer = Read(body);
      Assert.That(FieldOf(answer, "log_level").GetProperty("value").GetString(), Is.EqualTo("Debug"),
          "a choice travels as its name, which is the only spelling the screen was given");
      Assert.That(NamesOf(answer.GetProperty("restartNeededFor")), Does.Contain("log_level"),
          "the level is read when the log manager comes up, so promising a live change would be a lie");
    }

    [Test]
    public async Task ASavingTheSameValueAgainAsksForNoRestart()
    {
      StartServer();

      // The screen sends the whole form, not only what was touched. Naming a field that never moved would send
      // a viewer rebooting the server over a value that was already right.
      (HttpStatusCode code, string body) =
          await Put(_baseUrl + "/api/media/settings/sections/logging",
              "{\"values\":{\"log_level\":\"Information\"}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.OK), body);
      Assert.That(NamesOf(Read(body).GetProperty("restartNeededFor")), Is.Empty);
    }

    [Test]
    public async Task AChoiceThatWasNotOfferedIsRefusedWithItsNames()
    {
      StartServer();

      (HttpStatusCode code, string body) =
          await Put(_baseUrl + "/api/media/settings/sections/logging", "{\"values\":{\"log_level\":\"Chatty\"}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.BadRequest), body);
      Assert.That(_logging.LogLevel, Is.EqualTo(LogLevel.Information), "nothing moved");
      Assert.That(body, Does.Contain("Debug"), "the refusal names what could be said instead");
    }

    [Test]
    public async Task ANumberOutsideItsRangeIsRefusedAndNothingIsStored()
    {
      StartServer();

      (HttpStatusCode code, string body) =
          await Put(_baseUrl + "/api/media/settings/sections/fanart", "{\"values\":{\"max_posters\":99}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.BadRequest), body);
      Assert.That(body, Does.Contain("between 1 and 20"));
      Assert.That(_fanArt.MaxPosterFanArt, Is.EqualTo(1));
    }

    [Test]
    public async Task AWordWhereANumberWasAskedForIsRefused()
    {
      StartServer();

      (HttpStatusCode code, string body) =
          await Put(_baseUrl + "/api/media/settings/sections/fanart", "{\"values\":{\"max_posters\":\"many\"}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.BadRequest), body);
      Assert.That(body, Does.Contain("whole number"));
      Assert.That(_fanArt.MaxPosterFanArt, Is.EqualTo(1));
    }

    [Test]
    public async Task AFieldNobodyOpenedIsRefusedByItsKey()
    {
      StartServer();

      // The class behind the fanart section holds nothing secret, but the answer has to be the same for a key a
      // caller made up as for a property a caller hoped to reach through this endpoint.
      (HttpStatusCode code, string body) = await Put(_baseUrl + "/api/media/settings/sections/fanart",
          "{\"values\":{\"pending_import_jobs\":1}}");
      Assert.That(code, Is.EqualTo(HttpStatusCode.BadRequest), body);
      Assert.That(body, Does.Contain("pending_import_jobs"));
    }

    [Test]
    public async Task ASectionThatIsNotOpenIsNotFound()
    {
      StartServer();

      (HttpStatusCode code, string body) = await Get(_baseUrl + "/api/media/settings/sections/database");
      Assert.That(code, Is.EqualTo(HttpStatusCode.NotFound), body);

      (HttpStatusCode putCode, string putBody) =
          await Put(_baseUrl + "/api/media/settings/sections/database", "{\"values\":{\"path\":\"x\"}}");
      Assert.That(putCode, Is.EqualTo(HttpStatusCode.NotFound), putBody);
    }

    [Test]
    public async Task AServerThatWantsToKnowItsCallersRefusesAStranger()
    {
      StartServer(true);

      (HttpStatusCode code, string body) = await Get(_baseUrl + "/api/media/settings/sections");
      Assert.That(code, Is.EqualTo(HttpStatusCode.Forbidden), body);

      (HttpStatusCode putCode, string putBody) =
          await Put(_baseUrl + "/api/media/settings/sections/fanart", "{\"values\":{\"max_posters\":2}}");
      Assert.That(putCode, Is.EqualTo(HttpStatusCode.Forbidden), putBody);
      Assert.That(_fanArt.MaxPosterFanArt, Is.EqualTo(1), "and a stranger changed nothing");
    }
  }
}
