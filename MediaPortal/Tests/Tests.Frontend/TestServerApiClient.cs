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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Frontend.Api;
using MediaPortal.Utilities.Exceptions;

namespace Tests.Frontend
{
  /// <summary>
  /// The front end's reader of the server's API, against the API that is really answering on localhost.
  /// </summary>
  [TestFixture]
  public class TestServerApiClient
  {
    private readonly List<IDisposable> _toDispose = new List<IDisposable>();

    [TearDown]
    public void TearDown()
    {
      InProcessApi.RemoveMediaLibrary();
      foreach (IDisposable disposable in _toDispose)
        disposable.Dispose();
      _toDispose.Clear();
    }

    private InProcessApi StartServer(IMediaLibrary library)
    {
      InProcessApi.RegisterMediaLibrary(library);
      InProcessApi api = InProcessApi.Start();
      _toDispose.Add(api);
      return api;
    }

    [Test]
    public async Task TheAspectsOfARunningServerArriveAsObjects()
    {
      List<IEnumerable<Guid>> ignored;
      InProcessApi api = StartServer(InProcessApi.CreateLibrary(InProcessApi.CreateAspects(), 4, out ignored).Object);

      using (var client = new ServerApiClient(api.BaseUrl))
      {
        LibraryAspects aspects = await client.GetLibraryAspectsAsync(CancellationToken.None);

        Assert.That(aspects.Count, Is.EqualTo(2));
        Assert.That(aspects.Aspects.Select(aspect => aspect.Name), Is.EqualTo(new[] { "Core", "VideoFile" }));

        MediaAspect core = aspects.Aspects[0];
        Assert.That(core.Id, Is.EqualTo(InProcessApi.CORE_ASPECT_ID));
        Assert.That(core.SystemAspect, Is.False);
        Assert.That(core.Attributes.Select(attribute => attribute.Name), Is.EqualTo(new[] { "Title", "Watched" }));

        // The two flags a front end builds its queries with, read back out of the server's JSON. Whether an
        // attribute holds many values is decided by its cardinality, so "Title" (Inline) is indexed but not a
        // collection and "Watched" (ManyToMany) is a collection but not indexed.
        AspectAttribute title = core.Attributes[0];
        Assert.That(title.Indexed, Is.True);
        Assert.That(title.Collection, Is.False);
        Assert.That(title.Type, Is.EqualTo(typeof(string).FullName));

        AspectAttribute watched = core.Attributes[1];
        Assert.That(watched.Indexed, Is.False);
        Assert.That(watched.Collection, Is.True);
      }
    }

    [Test]
    public async Task ACountIsAskedForByItsNameAndComesBackWithTheAspectItBelongsTo()
    {
      List<IEnumerable<Guid>> counted;
      InProcessApi api = StartServer(InProcessApi.CreateLibrary(InProcessApi.CreateAspects(), 17, out counted).Object);

      using (var client = new ServerApiClient(api.BaseUrl))
      {
        MediaCount result = await client.GetMediaCountAsync("VideoFile", CancellationToken.None);

        Assert.That(result.Aspect, Is.EqualTo("VideoFile"));
        Assert.That(result.AspectId, Is.EqualTo(InProcessApi.VIDEO_ASPECT_ID));
        Assert.That(result.Count, Is.EqualTo(17));

        // The wire carries a name, the database wants an id, and the mapping happens on the server. That it
        // happened is visible in what the library was asked to count.
        Assert.That(counted.Count, Is.EqualTo(1));
        Assert.That(counted[0], Is.EquivalentTo(new[] { InProcessApi.VIDEO_ASPECT_ID }));
      }
    }

    [Test]
    public async Task CountingWithoutANameCountsEveryMediaItem()
    {
      List<IEnumerable<Guid>> counted;
      InProcessApi api = StartServer(InProcessApi.CreateLibrary(InProcessApi.CreateAspects(), 99, out counted).Object);

      using (var client = new ServerApiClient(api.BaseUrl))
      {
        MediaCount result = await client.GetMediaCountAsync(null, CancellationToken.None);
        Assert.That(result.Count, Is.EqualTo(99));
        Assert.That(result.Aspect, Is.Null);
        Assert.That(counted[0], Is.Empty);
      }
    }

    [Test]
    public async Task TheServerAnswersQuestionsAboutItselfToo()
    {
      List<IEnumerable<Guid>> ignored;
      InProcessApi api = StartServer(InProcessApi.CreateLibrary(InProcessApi.CreateAspects(), 0, out ignored).Object);

      using (var client = new ServerApiClient(api.BaseUrl))
      {
        ServerInfo info = await client.GetServerInfoAsync(CancellationToken.None);

        Assert.That(info.Name, Is.EqualTo("MediaPortal 2 Server"));
        Assert.That(info.Version, Is.Not.Empty);
        Assert.That(info.ProcessId, Is.EqualTo(Environment.ProcessId));

        // The prefix is the client's business: every other URL is built below it, and this is the field that
        // proves the server saw it rather than assumed it.
        Assert.That(info.PathBase, Is.EqualTo("/MediaPortal/Server"));
      }
    }

    [Test]
    public async Task ABaseUrlWithoutATrailingSlashStillReachesTheApi()
    {
      // HttpClient resolves a relative URI against the *directory* of its base address, so a base of
      // ".../MediaPortal/Server" would turn "api/serverinfo" into ".../MediaPortal/api/serverinfo" and every
      // request would 404 while the URL printed on screen looks right.
      List<IEnumerable<Guid>> ignored;
      InProcessApi api = StartServer(InProcessApi.CreateLibrary(InProcessApi.CreateAspects(), 3, out ignored).Object);

      Uri withoutSlash = new Uri(api.BaseUrl.ToString().TrimEnd('/'));
      Assert.That(withoutSlash.AbsoluteUri, Does.Not.EndWith("/"));

      using (var client = new ServerApiClient(withoutSlash))
      {
        Assert.That(client.BaseUrl.AbsoluteUri, Does.EndWith("/"));
        ServerInfo info = await client.GetServerInfoAsync(CancellationToken.None);
        Assert.That(info.PathBase, Is.EqualTo("/MediaPortal/Server"));
      }
    }

    [Test]
    public void AClientBuiltWithoutADifferentPatienceDoesNotWaitForever()
    {
      // HttpClient's own default is 100 seconds. For a screen that is not patience but a hang, so the client
      // shipped with the front end has a bound of its own, and the bound is part of what a viewer experiences.
      using (var builtIn = new ServerApiClient(new Uri("http://127.0.0.1:55555/MediaPortal/Server")))
      {
        Assert.That(builtIn.Timeout, Is.EqualTo(ServerApiClient.DefaultTimeout));
        Assert.That(builtIn.Timeout, Is.LessThan(TimeSpan.FromSeconds(30)),
            "a viewer in front of a television should not wait half a minute to be told nothing answered");
      }

      using (var told = new ServerApiClient(new Uri("http://127.0.0.1:55555/MediaPortal/Server"),
          null, TimeSpan.FromSeconds(2)))
      {
        Assert.That(told.Timeout, Is.EqualTo(TimeSpan.FromSeconds(2)), "unless it was told otherwise");
      }
    }

    [Test]
    public async Task NobodyAnsweringIsReportedAsNoAnswerRatherThanAsAnEmptyServer()
    {
      // Nothing is listening on this port. The distinction matters: "0 aspects" and "the server is not there"
      // need different reactions, so the exception has to carry the difference.
      Uri dead = new Uri(string.Format("http://127.0.0.1:{0}/MediaPortal/Server/", InProcessApi.GetFreePort()));
      using (var client = new ServerApiClient(dead, null, TimeSpan.FromSeconds(2)))
      {
        ServerApiException error = Assert.ThrowsAsync<ServerApiException>(
            () => client.GetLibraryAspectsAsync(CancellationToken.None))!;
        Assert.That(error.StatusCode, Is.Null, "there was no HTTP answer at all");
        Assert.That(error.NotReady, Is.False);
        Assert.That(error.Message, Does.Contain(dead.ToString()));
      }
    }

    [Test]
    public async Task AServerThatIsNotReadyYetIsRecognisedAsSuch()
    {
      // The library refusing because it is still starting is the case the server answers 503 for, and the one
      // answer a client should retry instead of showing an error.
      //
      // This test is also why the 503 no longer reports itself through a demanded logger: it used to answer 500
      // here, because writing the log line asked for a logger that a host without one does not have, and the
      // exception escaped from inside the catch. A 503 that depends on something else working is not a contract.
      InProcessApi api = StartServer(InProcessApi.CreateStartingLibrary().Object);

      using (var client = new ServerApiClient(api.BaseUrl))
      {
        ServerApiException error = Assert.ThrowsAsync<ServerApiException>(
            () => client.GetLibraryAspectsAsync(CancellationToken.None))!;
        Assert.That(error.StatusCode, Is.EqualTo(503));
        Assert.That(error.NotReady, Is.True);
        Assert.That(error.Message, Does.Contain("not finished starting up"));
      }
    }

    [Test]
    public async Task ALibraryThatIsAbsentEntirelyIsNotTheSameThingAsOneThatIsStarting()
    {
      // "No library was registered" and "the library is still starting" are different states, and this records
      // what each of them actually answers rather than what it would be convenient for it to answer: both say
      // "ask again" - a server without a library cannot answer any question about media, so refusing is right -
      // but only the one that is starting can say why, and the reason is the part a viewer is told.
      // The server registers the library before its listener starts, so a running server is never in the first
      // state; it is exercised here because the difference between the two is otherwise invisible.
      InProcessApi api = InProcessApi.Start();
      _toDispose.Add(api);
      InProcessApi.RemoveMediaLibrary();

      using (var client = new ServerApiClient(api.BaseUrl))
      {
        ServerApiException error = Assert.ThrowsAsync<ServerApiException>(
            () => client.GetLibraryAspectsAsync(CancellationToken.None))!;
        Assert.That(error.StatusCode, Is.EqualTo(503));
        Assert.That(error.Message, Does.Not.Contain("not finished starting up"),
            "there is no library to have a reason, so nothing is claimed");
      }
    }

    [Test]
    public async Task AnUnknownAspectNameIsNotFoundAndStaysNotFound()
    {
      List<IEnumerable<Guid>> ignored;
      InProcessApi api = StartServer(InProcessApi.CreateLibrary(InProcessApi.CreateAspects(), 0, out ignored).Object);

      using (var client = new ServerApiClient(api.BaseUrl))
      {
        ServerApiException error = Assert.ThrowsAsync<ServerApiException>(
            () => client.GetMediaCountAsync("NoSuchAspect", CancellationToken.None))!;
        Assert.That(error.StatusCode, Is.EqualTo(404));
        Assert.That(error.NotReady, Is.False, "404 is not \"try again later\"");
      }
    }

    [Test]
    public void TheServerUrlComesFromTheArgumentThenTheEnvironmentThenTheDefault()
    {
      string url = "http://10.0.0.5:55555/MediaPortal/Server";
      Assert.That(ServerApiClient.ResolveBaseUrl(url).ToString(), Does.StartWith("http://10.0.0.5"));

      Environment.SetEnvironmentVariable(ServerApiClient.UrlEnvironmentVariable, url);
      try
      {
        Assert.That(ServerApiClient.ResolveBaseUrl(null).ToString(), Does.StartWith("http://10.0.0.5"));
        Assert.That(ServerApiClient.ResolveBaseUrl("http://127.0.0.1:1/MediaPortal/Server").ToString(),
            Does.StartWith("http://127.0.0.1"), "an explicit argument beats the environment");
      }
      finally
      {
        Environment.SetEnvironmentVariable(ServerApiClient.UrlEnvironmentVariable, null);
      }

      Assert.That(ServerApiClient.ResolveBaseUrl(null).ToString(), Is.EqualTo(ServerApiClient.DefaultUrl),
          "with nothing said, the client looks on this machine");

      Assert.That(() => ServerApiClient.ResolveBaseUrl("not a url"), Throws.ArgumentException);
    }
  }
}
