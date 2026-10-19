#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
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
  /// The stream endpoint driven over a real socket: a film asked for by its id, coming back as the bytes of the
  /// file the library recorded, in whole and in parts.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Why over a socket and not by calling the controller.</b> Everything that makes a stream watchable is done
  /// by the pipeline rather than by the controller: the <c>206 Partial Content</c>, the <c>Content-Range</c>, the
  /// <c>416</c> for an impossible range and the <c>Accept-Ranges</c> that tells a player to ask at all are what
  /// ASP.NET does with <see cref="FileStreamResult.EnableRangeProcessing"/>. Calling
  /// <see cref="MediaController.GetStream"/> by hand would show a correct object graph and prove nothing about the
  /// one thing a player depends on. So this fixture builds the listener from the resource server's own
  /// <see cref="ResourceServer.ConfigureApiServices"/> and <see cref="ResourceServer.ConfigureApiPipeline"/> the
  /// way <see cref="TestTokenEndpointOverHttp"/> does - which is also the portable host Linux and Android use.
  /// </para>
  /// <para>
  /// <b>What is faked and why.</b> The media library is the mock MP2's tests have always used, because a title's
  /// file has to exist wherever the tests happen to run and a checked-in 4 GB film is not an option. The file
  /// itself is a real seekable stream of three hundred thousand bytes with a counting pattern in it, so a response
  /// that sent the right <i>number</i> of bytes from the wrong <i>offset</i> - the exact shape of "the film starts
  /// at the wrong point" - fails here. The accessor behind it is a mock of MP2's own
  /// <see cref="IFileSystemResourceAccessor"/>, which is also how the endpoint reaches a file on a network share
  /// or in whatever provider an import found: nothing below that interface is this endpoint's business.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestMediaStreamOverHttp
  {
    #region Fixtures

    private const string PREFIX = "/MediaPortal/Server";

    /// <summary>
    /// Bigger than a socket buffer and bigger than the copy buffer of any layer involved, so that the whole-file
    /// test actually exercises a response written in several pieces.
    /// </summary>
    private const int SIZE = 300000;

    private static readonly Guid TITLE_ID = new Guid("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// The bytes of the file the library recorded: a counting pattern rather than zeros, because a response that
    /// sent the right length from the wrong offset has to fail rather than look plausible.
    /// </summary>
    private static readonly byte[] FILE_BYTES = Pattern(SIZE);

    private readonly List<IDisposable> _listeners = new List<IDisposable>();

    private string _baseUrl;
    private List<MediaItemQuery> _asked;
    private List<Guid?> _namedProfiles;
    private int _closedAccessors;

    private static byte[] Pattern(int length)
    {
      byte[] bytes = new byte[length];
      for (int i = 0; i < length; i++)
        bytes[i] = (byte)(i % 251);
      return bytes;
    }

    [TearDown]
    public void StopTheListener()
    {
      foreach (IDisposable listener in _listeners)
        listener.Dispose();
      _listeners.Clear();

      ServiceRegistration.Remove<IMediaLibrary>();
      ServiceRegistration.Remove<ISettingsManager>();
      ServiceRegistration.Remove<IThreadPool>();
      ServiceRegistration.Remove<IUserProfileDataManagement>();
    }

    /// <summary>
    /// Starts the resource server's own pipeline with at most one title in the library.
    /// </summary>
    /// <param name="item">The title the library answers with, or null for a library that knows nothing.</param>
    /// <param name="authorize">
    /// Whether the server was configured to want to know its callers, which is its default and the case that
    /// decides whether a stream is a hole in the authorization.
    /// </param>
    private void StartServer(MediaItem item, bool authorize = false)
    {
      List<MediaItemQuery> questions = new List<MediaItemQuery>();
      List<Guid?> named = new List<Guid?>();

      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(
          new Dictionary<Guid, MediaItemAspectMetadata>
          {
              { MediaAspect.ASPECT_ID, MediaAspect.Metadata },
              { ProviderResourceAspect.ASPECT_ID, ProviderResourceAspect.Metadata }
          });
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, It.IsAny<Guid?>(), false))
          .Callback<MediaItemQuery, bool, Guid?, bool>((query, online, profile, virtualItems) =>
          {
            questions.Add(query);
            named.Add(profile);
          })
          .Returns(() => item == null ? new List<MediaItem>() : new List<MediaItem> { item });
      ServiceRegistration.Set<IMediaLibrary>(library.Object);

      Mock<ISettingsManager> settingsManager = new Mock<ISettingsManager>();
      settingsManager.Setup(s => s.Load<ServerSettings>()).Returns(() => new ServerSettings
      {
        WebAutorizationEnabled = authorize
      });
      ServiceRegistration.Set<ISettingsManager>(settingsManager.Object);

      // The pipeline's resource access middleware wants the thread pool and nothing else here; the profile store
      // is what the bearer handler ends up asking when a request does name somebody.
      ServiceRegistration.Set<IThreadPool>(new Mock<IThreadPool>().Object);
      ServiceRegistration.Set<IUserProfileDataManagement>(new Mock<IUserProfileDataManagement>().Object);

      ResourceServer resourceServer = new ResourceServer();
      resourceServer.AddHttpControllerAssembly(typeof(MediaController));

      int port = FreePort();
      _listeners.Add(SharedHttpPortHost.Start(new[] { "http://127.0.0.1:" + port + PREFIX },
          resourceServer.ConfigureApiServices, resourceServer.ConfigureApiPipeline));
      _baseUrl = "http://127.0.0.1:" + port + PREFIX;

      _asked = questions;
      _namedProfiles = named;
      _closedAccessors = 0;
    }

    /// <summary>
    /// A file the server can read: <see cref="FILE_BYTES"/>, opened afresh for every request the way a file is.
    /// </summary>
    private Mock<IFileSystemResourceAccessor> ReadableFile(string fileName)
    {
      var accessor = new Mock<IFileSystemResourceAccessor>(MockBehavior.Strict);
      accessor.SetupGet(a => a.Exists).Returns(true);
      accessor.SetupGet(a => a.IsFile).Returns(true);
      accessor.SetupGet(a => a.ResourceName).Returns(fileName);
      accessor.Setup(a => a.OpenReadAsync()).ReturnsAsync(() => new MemoryStream(FILE_BYTES));
      accessor.Setup(a => a.Dispose()).Callback(() => Interlocked.Increment(ref _closedAccessors));
      return accessor;
    }

    /// <summary>
    /// A title with the given primary resource. Null stands for a title with no file recorded at all, which is
    /// what a stub leaves in the library.
    /// </summary>
    private static MediaItem TitleWith(IResourceLocator locator)
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      SingleMediaItemAspect media = new SingleMediaItemAspect(MediaAspect.Metadata);
      media.SetAttribute(MediaAspect.ATTR_TITLE, "Alien Earth");
      aspects[MediaAspect.ASPECT_ID] = new List<MediaItemAspect> { media };

      return new StreamableItem(TITLE_ID, aspects, locator);
    }

    /// <summary>
    /// The title whose file is the given accessor, with the locator standing in for the resource provider that
    /// would otherwise have to be registered and started to resolve a stored path.
    /// </summary>
    private static MediaItem TitleReadFrom(IResourceAccessor accessor)
    {
      var locator = new Mock<IResourceLocator>(MockBehavior.Strict);
      locator.SetupGet(l => l.NativeSystemId).Returns("Filesystem");
      locator.Setup(l => l.CreateAccessor()).Returns(accessor);
      return TitleWith(locator.Object);
    }

    /// <summary>The title, read from a file that is there and named <paramref name="fileName"/>.</summary>
    private MediaItem TitleFromFile(string fileName)
    {
      return TitleReadFrom(ReadableFile(fileName).Object);
    }

    #endregion

    #region Helpers

    static int FreePort()
    {
      TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
      listener.Start();
      int port = ((IPEndPoint)listener.LocalEndpoint).Port;
      listener.Stop();
      return port;
    }

    private string StreamUrl
    {
      get { return _baseUrl + "/api/media/items/" + TITLE_ID + "/stream"; }
    }

    /// <summary>
    /// One request against the running listener, with its body read into memory and its headers collected from
    /// both places <see cref="HttpResponseMessage"/> keeps them.
    /// </summary>
    private static async Task<Answer> SendAsync(string url, string range = null, HttpMethod method = null)
    {
      using (HttpClient client = new HttpClient())
      {
        using (HttpRequestMessage request = new HttpRequestMessage(method ?? HttpMethod.Get, url))
        {
          if (range != null)
            request.Headers.TryAddWithoutValidation("Range", range);

          using (HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false))
          {
            byte[] body = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            // Some of the interesting headers (Content-Range, Accept-Ranges, Content-Type) are content headers,
            // so HttpClient puts them on the content and not on the message. One dictionary for both spares every
            // test the job of knowing which half a header lives in.
            Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
              headers[header.Key] = string.Join(",", header.Value);
            foreach (KeyValuePair<string, IEnumerable<string>> header in response.Content.Headers)
              headers[header.Key] = string.Join(",", header.Value);

            return new Answer((int)response.StatusCode, body, headers);
          }
        }
      }
    }

    /// <summary>Asking for the whole film.</summary>
    private Task<Answer> AskForTheWholeFilmAsync()
    {
      return SendAsync(StreamUrl);
    }

    /// <summary>
    /// A what-was-answered, with the body kept as bytes because the body is a film.
    /// </summary>
    private sealed class Answer
    {
      internal Answer(int status, byte[] body, Dictionary<string, string> headers)
      {
        Status = status;
        Body = body;
        _headers = headers;
      }

      private readonly Dictionary<string, string> _headers;

      internal int Status { get; }
      internal byte[] Body { get; }

      internal string Header(string name)
      {
        string value;
        return _headers.TryGetValue(name, out value) ? value : null;
      }

      /// <summary>The body read as text, which is what the failure answers are.</summary>
      internal string Text
      {
        get { return System.Text.Encoding.UTF8.GetString(Body ?? new byte[0]); }
      }
    }

    /// <summary>
    /// The bytes of the file between two offsets, inclusive - what a range asks for and what a player expects to
    /// find there.
    /// </summary>
    private static byte[] Slice(long from, long to)
    {
      byte[] slice = new byte[to - from + 1];
      Array.Copy(FILE_BYTES, from, slice, 0, slice.Length);
      return slice;
    }

    /// <summary>
    /// A title whose locator yields whatever accessor the test wants, including none.
    /// </summary>
    private sealed class StreamableItem : MediaItem
    {
      private readonly IResourceLocator _locator;

      internal StreamableItem(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects,
          IResourceLocator locator)
          : base(mediaItemId, aspects)
      {
        _locator = locator;
      }

      public override IResourceLocator GetResourceLocator()
      {
        return _locator;
      }
    }

    #endregion

    #region The bytes

    [Test]
    public async Task TheWholeFileArrivesWhenNobodyAsksForAPart()
    {
      StartServer(TitleFromFile("Alien Earth - S01E01 - Nimmerland.mkv"));

      Answer answer = await AskForTheWholeFilmAsync();

      Assert.That(answer.Status, Is.EqualTo(200));
      Assert.That(answer.Body.Length, Is.EqualTo(SIZE), "the film a player gets has to be the film that is there");
      Assert.That(answer.Body, Is.EqualTo(FILE_BYTES),
          "a response of the right length built from the wrong bytes is a film that shows nothing");
      Assert.That(answer.Header("Content-Type"), Is.EqualTo("video/x-matroska"));
      Assert.That(answer.Header("Content-Length"), Is.EqualTo(SIZE.ToString()));
      Assert.That(answer.Header("Accept-Ranges"), Is.EqualTo("bytes"),
          "without this a player is told not to ask for parts, which means it cannot seek");
    }

    [Test]
    public async Task AByteRangeArrivesAsAPartialAnswerWithItsPlaceInTheFile()
    {
      StartServer(TitleFromFile("film.mkv"));

      Answer answer = await SendAsync(StreamUrl, "bytes=100000-100099");

      Assert.That(answer.Status, Is.EqualTo(206));
      Assert.That(answer.Body, Is.EqualTo(Slice(100000, 100099)));
      Assert.That(answer.Header("Content-Length"), Is.EqualTo("100"));
      Assert.That(answer.Header("Content-Range"), Is.EqualTo("bytes 100000-100099/" + SIZE),
          "a player that is not told where these bytes came from cannot place them in the film");
    }

    [Test]
    public async Task ARangeThatStopsAtNothingRunsToTheEndOfTheFile()
    {
      StartServer(TitleFromFile("film.mkv"));

      Answer answer = await SendAsync(StreamUrl, "bytes=" + (SIZE - 96) + "-");

      Assert.That(answer.Status, Is.EqualTo(206));
      Assert.That(answer.Body, Is.EqualTo(Slice(SIZE - 96, SIZE - 1)));
      Assert.That(answer.Header("Content-Range"), Is.EqualTo("bytes " + (SIZE - 96) + "-" + (SIZE - 1) + "/" + SIZE));
    }

    [Test]
    public async Task ARangePastTheEndOfTheFileIsRefused()
    {
      StartServer(TitleFromFile("film.mkv"));

      Answer answer = await SendAsync(StreamUrl, "bytes=" + (SIZE + 1000) + "-" + (SIZE + 2000));

      Assert.That(answer.Status, Is.EqualTo(416));
      Assert.That(answer.Header("Content-Range"), Is.EqualTo("bytes */" + SIZE),
          "the refusal is the place a player learns how long the file really is. The 'bytes ' prefix is what the " +
          "pipeline answers with; RFC 7233 writes the same fact without it, and no player in this tree reads the " +
          "prefix - the number is the answer.");
    }

    [Test]
    public async Task APlayerSeekingBackAndForthGetsTheBytesItAskedForEveryTime()
    {
      // The order a player works in: the index at the end of the file, then the first cluster, then back to the
      // beginning when the viewer presses play. Each of those is a separate request, and a server that answered
      // the second one from where the first left off would give a film that jumps.
      StartServer(TitleFromFile("film.mkv"));

      Answer index = await SendAsync(StreamUrl, "bytes=" + (SIZE - 4096) + "-" + (SIZE - 1));
      Answer start = await SendAsync(StreamUrl, "bytes=0-4095");
      Answer middle = await SendAsync(StreamUrl, "bytes=150000-150999");

      Assert.That(index.Body, Is.EqualTo(Slice(SIZE - 4096, SIZE - 1)));
      Assert.That(start.Body, Is.EqualTo(Slice(0, 4095)));
      Assert.That(middle.Body, Is.EqualTo(Slice(150000, 150999)));
    }

    [Test]
    public async Task AHeadRequestSaysHowLongTheFilmIsWithoutSendingIt()
    {
      StartServer(TitleFromFile("film.mkv"));

      Answer answer = await SendAsync(StreamUrl, null, HttpMethod.Head);

      Assert.That(answer.Status, Is.EqualTo(200));
      Assert.That(answer.Body.Length, Is.EqualTo(0), "nobody asked for the bytes twice");
      Assert.That(answer.Header("Content-Length"), Is.EqualTo(SIZE.ToString()));
    }

    [Test]
    public async Task AFilmIsSentAsVideoAndAMusicFileAsAudio()
    {
      StartServer(TitleFromFile("Song.flac"));
      Assert.That((await AskForTheWholeFilmAsync()).Header("Content-Type"), Is.EqualTo("audio/x-flac"));

      StartServer(TitleFromFile("film.mp4"));
      Assert.That((await AskForTheWholeFilmAsync()).Header("Content-Type"), Is.EqualTo("video/mp4"));
    }

    [Test]
    public async Task AFileOfAnUnknownKindIsSentAsPlainBytesRatherThanAGuess()
    {
      StartServer(TitleFromFile("recorded.mpx7"));

      Answer answer = await AskForTheWholeFilmAsync();

      Assert.That(answer.Status, Is.EqualTo(200));
      Assert.That(answer.Header("Content-Type"),
          Is.EqualTo(MediaController.DEFAULT_STREAM_MEDIA_TYPE));
    }

    #endregion

    #region How the file is looked for

    [Test]
    public async Task TheFileIsLookedForTheWayTheTitleListLooksItUp()
    {
      StartServer(TitleFromFile("film.mkv"));

      await AskForTheWholeFilmAsync();

      Assert.That(_asked.Count, Is.EqualTo(1), "one title is one query, not a query per attribute");
      MediaItemQuery query = _asked[0];
      Assert.That(query.NecessaryRequestedMIATypeIDs,
          Is.EquivalentTo(new[] { MediaAspect.ASPECT_ID }));
      Assert.That(query.OptionalRequestedMIATypeIDs,
          Is.EquivalentTo(new[] { ProviderResourceAspect.ASPECT_ID }),
          "the file a title plays from lives in the provider resource aspect, so a lookup without it finds nothing");
      Assert.That(query.Filter, Is.InstanceOf<MediaItemIdFilter>());
      Assert.That(((MediaItemIdFilter)query.Filter).MediaItemIds, Is.EquivalentTo(new[] { TITLE_ID }));

      // The bytes are the same for everybody in the household; asking as somebody would make two people watching
      // one film cost the database two different amounts of work.
      Assert.That(_namedProfiles, Is.All.Null);
    }

    [Test]
    public async Task TheResourceIsClosedWhenTheResponseHasBeenWritten()
    {
      StartServer(TitleFromFile("film.mkv"));

      await AskForTheWholeFilmAsync();

      // One watched film is one open connection to whoever serves the file unless the accessor goes with the
      // response, so this is the difference between a media centre and a server with a leak.
      for (int attempt = 0; attempt < 20 && _closedAccessors == 0; attempt++)
        await Task.Delay(50);

      Assert.That(_closedAccessors, Is.EqualTo(1),
          "the stream closed but the accessor that opened it is still open");
    }

    #endregion

    #region When there are no bytes to send

    [Test]
    public async Task ATitleTheServerDoesNotKnowIsAnsweredAsMissing()
    {
      StartServer(null);

      Answer answer = await AskForTheWholeFilmAsync();

      Assert.That(answer.Status, Is.EqualTo(404));
      Assert.That(answer.Text, Does.Contain("no title with the id"));
    }

    [Test]
    public async Task ATitleWithNoFileRecordedIsAnsweredAsMissing()
    {
      // What a stub leaves behind: the library holds the title and nothing to play.
      StartServer(TitleWith((IResourceLocator) null));

      Answer answer = await AskForTheWholeFilmAsync();

      Assert.That(answer.Status, Is.EqualTo(404));
      Assert.That(answer.Text, Does.Contain("no file recorded"));
    }

    [Test]
    public async Task AFileThatIsNotThereAnymoreIsAnsweredAsGone()
    {
      var file = new Mock<IFileSystemResourceAccessor>(MockBehavior.Strict);
      file.SetupGet(a => a.Exists).Returns(false);
      file.SetupGet(a => a.IsFile).Returns(true);
      file.SetupGet(a => a.ResourceName).Returns("film.mkv");
      file.Setup(a => a.Dispose());

      StartServer(TitleReadFrom(file.Object));
      Answer answer = await AskForTheWholeFilmAsync();

      Assert.That(answer.Status, Is.EqualTo(410));
      Assert.That(answer.Text, Does.Contain("not there any more"),
          "the title is in the library and its file is not: that is a different sentence to show than 404");
    }

    [Test]
    public async Task AFileTheServerMayNotReadIsAnsweredAsGoneWithWhatTheSystemSaid()
    {
      var file = ReadableFile("film.mkv");
      file.Setup(a => a.OpenReadAsync())
          .ThrowsAsync(new UnauthorizedAccessException("Access to the path 'film.mkv' is denied."));

      StartServer(TitleReadFrom(file.Object));
      Answer answer = await AskForTheWholeFilmAsync();

      Assert.That(answer.Status, Is.EqualTo(410));
      Assert.That(answer.Text, Does.Contain("not allowed to read"));
    }

    [Test]
    public async Task AFileThatVanishesWhileItIsBeingOpenedIsAnsweredAsGone()
    {
      var file = ReadableFile("film.mkv");
      file.Setup(a => a.OpenReadAsync())
          .ThrowsAsync(new IOException("The process cannot access the file because it is being used by another process."));

      StartServer(TitleReadFrom(file.Object));
      Answer answer = await AskForTheWholeFilmAsync();

      Assert.That(answer.Status, Is.EqualTo(410));
      Assert.That(answer.Text, Does.Contain("could not be read"));
    }

    [Test]
    public async Task AResourceThatIsNotAFileIsAnsweredAsGone()
    {
      // A provider whose accessor is not a file system one - which is what a virtual item from a provider the
      // server cannot read as a file looks like.
      var accessor = new Mock<IResourceAccessor>(MockBehavior.Strict);
      accessor.Setup(a => a.Dispose());

      StartServer(TitleReadFrom(accessor.Object));
      Answer answer = await AskForTheWholeFilmAsync();

      Assert.That(answer.Status, Is.EqualTo(410));
      Assert.That(answer.Text, Does.Contain("cannot read"));
    }

    [Test]
    public async Task AServerWithoutALibrarySaysItIsNotReady()
    {
      StartServer(TitleFromFile("film.mkv"));
      ServiceRegistration.Remove<IMediaLibrary>();

      Assert.That((await AskForTheWholeFilmAsync()).Status, Is.EqualTo(503));
    }

    #endregion

    #region Who may ask

    [Test]
    public async Task AServerThatWantsToKnowItsCallerRefusesAnAnonymousStream()
    {
      // The reason the endpoint takes an id instead of a path cannot be said loudly enough: a stream is the least
      // anonymous thing a media centre serves, so it is behind the same rule as the titles that name it.
      //
      // The refusal is a 403, which is what MP2's own authorize attribute answers rather than a 401 challenge,
      // and the reason is written where the attribute is: a client that has no token asks the token endpoint
      // again either way, so the difference decides nothing for MP2's clients. What matters here is that no byte
      // of the film went out.
      StartServer(TitleFromFile("film.mkv"), authorize: true);

      Answer answer = await AskForTheWholeFilmAsync();

      Assert.That(answer.Status, Is.EqualTo(403));
      Assert.That(answer.Header("Content-Type"), Does.Contain("application/problem+json"),
          "the refusal comes back in the same shape every other endpoint of this API refuses in");
      Assert.That(answer.Body.Length, Is.LessThan(1024), "a refusal must not have sent any of the film");
    }

    #endregion
  }
}
