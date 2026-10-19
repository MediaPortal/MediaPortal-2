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
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Backend.Services.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.Threading;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Server.Api;
using Moq;
using NUnit.Framework;
using UPnP.Infrastructure.Http;

namespace Tests.Server.Api
{
  /// <summary>
  /// The file server on a real socket, with files that really exist on disk: what it answers when somebody asks it
  /// for a path, now that the answer is looked up rather than assumed.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>What this fixture exists for.</b> Until the previous commit the answer to "may this file be sent?" was
  /// written as <c>return true;</c> next to a note saying the code had no way to know. Measured on the deployed
  /// layout, with the shipped settings, the API said <c>403</c> to a stranger while the same socket served
  /// <c>C:\Windows\win.ini</c> to them two lines further down, and the listener was bound to every address of the
  /// machine. The tests below are that measurement written back the other way round: the file outside the shares
  /// gets refused, the file inside them gets served, and a path that pretends to be inside gets refused.
  /// </para>
  /// <para>
  /// <b>Why over a socket, again.</b> The hole was in the pipeline - the module runs after <c>UseEndpoints</c>, is
  /// not covered by the authorization attribute, and is reached at a URL no controller owns. Deciding by calling
  /// <see cref="ResourceAccessModule"/> by hand would test a method and not the arrangement that leaked. So this
  /// builds the same listener the portable host builds - <see cref="ResourceServer.ConfigureApiServices"/> and
  /// <see cref="ResourceServer.ConfigureApiPipeline"/> - and asks it over TCP the way a stranger on the LAN does.
  /// </para>
  /// <para>
  /// <b>What is real here.</b> The files are real files in a real directory under the temporary path, and the
  /// accessor is MP2's own <see cref="LocalFsResourceProvider"/> rather than a mock, because the point at issue is
  /// whether <i>this path on this disk</i> may be sent. Pretending with a mock accessor would have let the
  /// traversal case pass for the wrong reason: it is the provider's translation of a provider path into a DOS path
  /// that makes <c>..\</c> interesting, and a mock has no translation.
  /// </para>
  /// <para>
  /// <b>Why authorization is switched off in all of them.</b> Not because it does not matter, but because the
  /// leak did not depend on it: with <c>WebAutorizationEnabled</c> at its default of <c>true</c> the API was
  /// protected and this endpoint was not, so the only thing between a stranger and the file was the question this
  /// module answers. These tests ask the question with the noise of the ticket-handling removed.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestResourceAccessModuleOverHttp
  {
    #region Files on disk

    private const string PREFIX = "/MediaPortal/Server";

    private string _root;
    private string _filmsDir;
    private string _filmPath;
    private string _outsidePath;
    private byte[] _filmBytes;
    private readonly List<IDisposable> _listeners = new List<IDisposable>();
    private string _baseUrl;
    private StringWriter _serverLog;

    [SetUp]
    public void MakeFilesOnDisk()
    {
      _root = Path.Combine(Path.GetTempPath(), "mp2-resource-access-" + Guid.NewGuid().ToString("N"));
      _filmsDir = Path.Combine(_root, "films");
      Directory.CreateDirectory(_filmsDir);

      _filmBytes = Enumerable.Range(0, 4096).Select(i => (byte)(i % 251)).ToArray();
      _filmPath = Path.Combine(_filmsDir, "A Film.mkv");
      File.WriteAllBytes(_filmPath, _filmBytes);

      // A second file in the same tree that nobody registered. Being on the same disk as a share, and below the
      // same folder, is exactly what the previous code did not distinguish.
      _outsidePath = Path.Combine(_root, "notes.txt");
      File.WriteAllText(_outsidePath, "the server's own business");
    }

    [TearDown]
    public void StopTheServerAndDeleteTheFiles()
    {
      foreach (IDisposable listener in _listeners)
        listener.Dispose();
      _listeners.Clear();

      ServiceRegistration.Remove<IResourceAccessGuard>();
      ServiceRegistration.Remove<IMediaAccessor>();
      ServiceRegistration.Remove<IMediaLibrary>();
      ServiceRegistration.Remove<ISystemResolver>();
      ServiceRegistration.Remove<ISettingsManager>();
      ServiceRegistration.Remove<IThreadPool>();
      ServiceRegistration.Remove<IUserProfileDataManagement>();
      ServiceRegistration.Remove<ILogger>();

      if (_root == null || !Directory.Exists(_root))
        return;
      try
      {
        Directory.Delete(_root, true);
      }
      catch (IOException)
      {
        // The module keeps a file handle on what it served for a while, so the last file of a run can still be
        // open. The directory is under the temporary path; leaving one behind is not worth failing a test over.
      }
    }

    #endregion

    #region Starting the server

    /// <summary>
    /// Starts the resource server's pipeline with the local filesystem provider registered, which is the state a
    /// running server is in, and with the shares somebody registered.
    /// </summary>
    /// <param name="shares">
    /// The shares the library answers with, or <c>null</c> to leave no guard registered at all - the state a host
    /// that forgot the registration is in, which must serve nothing rather than everything.
    /// </param>
    private void StartServer(IDictionary<Guid, Share> shares)
    {
      // A provider registered under the local filesystem provider's own id: paths are resolved for real, through
      // MP2's provider, because the point at issue is whether this path on this disk may be sent.
      // The module derives the URL it answers from the system type - a server serves "/MediaPortal/Server" and a
      // client "/MediaPortal/Client" - so the resolver has to say what this process is. Without it the module
      // cannot even decide whether the request is for it, which is what the first failing run showed.
      Mock<ISystemResolver> resolver = new Mock<ISystemResolver>();
      resolver.SetupGet(r => r.SystemType).Returns(SystemType.Server);
      ServiceRegistration.Set<ISystemResolver>(resolver.Object);

      // The server's own log goes into a buffer the tests can print, because when a request comes back wrong the
      // only explanation of a refusal or a crash is what the module said about it on the server side.
      _serverLog = new StringWriter();
      ServiceRegistration.Set<ILogger>(new DefaultLogger(_serverLog, LogLevel.All, false, true));
      ServiceRegistration.Set<IMediaAccessor>(AccessorWithLocalFileSystem());

      Mock<IMediaLibrary> library = new Mock<IMediaLibrary>();
      library.Setup(get => get.GetShares(It.IsAny<string>())).Returns(shares);
      ServiceRegistration.Set<IMediaLibrary>(library.Object);

      if (shares != null)
        ServiceRegistration.Set<IResourceAccessGuard>(new SharesResourceAccessGuard(library.Object));

      Mock<ISettingsManager> settingsManager = new Mock<ISettingsManager>();
      settingsManager.Setup(s => s.Load<ServerSettings>()).Returns(() => new ServerSettings
      {
        WebAutorizationEnabled = false
      });
      ServiceRegistration.Set<ISettingsManager>(settingsManager.Object);

      // The pipeline's authorization middleware asks for these two; the bearer handler only reaches the profile
      // store once somebody names themselves, which nobody below does.
      ServiceRegistration.Set<IThreadPool>(new Mock<IThreadPool>().Object);
      ServiceRegistration.Set<IUserProfileDataManagement>(new Mock<IUserProfileDataManagement>().Object);

      ResourceServer resourceServer = new ResourceServer();
      resourceServer.AddHttpControllerAssembly(typeof(MediaController));

      int port = FreePort();
      _listeners.Add(SharedHttpPortHost.Start(new[] { "http://127.0.0.1:" + port + PREFIX },
          resourceServer.ConfigureApiServices, resourceServer.ConfigureApiPipeline));
      _baseUrl = "http://127.0.0.1:" + port + PREFIX;
    }

    /// <summary>
    /// An accessor that knows only MP2's own local filesystem provider, under the id the provider's paths carry.
    /// </summary>
    private static IMediaAccessor AccessorWithLocalFileSystem()
    {
      var providers = new Dictionary<Guid, IResourceProvider>
      {
        { LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, new LocalFsResourceProvider() }
      };

      Mock<IMediaAccessor> accessor = new Mock<IMediaAccessor>(MockBehavior.Strict);
      accessor.SetupGet(a => a.LocalResourceProviders).Returns(providers);
      return accessor.Object;
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
    /// The status the server gave, and - when it did not give the one expected - everything the server said about
    /// the request. From outside, a refusal and a crash are both a number; only the log says which one it was.
    /// </summary>
    private void AssertStatus(Answer answer, int expected, string why)
    {
      Assert.That(answer.Status, Is.EqualTo(expected), "{0}\nanswered: {1}\nthe server said:\n{2}",
          why, answer, _serverLog);
    }

    #endregion

    #region Asking for a file

    /// <summary>
    /// The URL the module answers: the one the server hands to UPnP renderers and the one the old clients used.
    /// </summary>
    private string UrlFor(ResourcePath path)
    {
      return _baseUrl + "/GetResource?ResourcePath=" + Uri.EscapeDataString(path.Serialize());
    }

    /// <summary>Asking the running server for one file on disk.</summary>
    private async Task<Answer> AskForAsync(string dosPath)
    {
      ResourcePath path = LocalPath(dosPath);
      using (HttpClient client = new HttpClient())
      {
        using (HttpResponseMessage response = await client.GetAsync(UrlFor(path)).ConfigureAwait(false))
        {
          return new Answer((int)response.StatusCode, response.ReasonPhrase,
              await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false));
        }
      }
    }

    /// <summary>The path the library and the shares use for a file on disk.</summary>
    private static ResourcePath LocalPath(string dosPath)
    {
      return ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID,
          LocalFsResourceProviderBase.ToProviderPath(dosPath));
    }

    /// <summary>The share that contains <see cref="_filmsDir"/>.</summary>
    private IDictionary<Guid, Share> FilmsShare()
    {
      var shares = new Dictionary<Guid, Share>();
      shares[Guid.NewGuid()] = new Share(Guid.NewGuid(), "Filesystem", LocalPath(_filmsDir), "Films", false, null);
      return shares;
    }

    #endregion

    #region Tests

    [Test]
    public async Task AFileOutsideEveryShareIsNotSent()
    {
      StartServer(FilmsShare());

      Answer answer = await AskForAsync(_outsidePath);

      // This is the file whose contents used to be the answer to any request the network could make.
      AssertStatus(answer, 403,
          "a file nobody registered as media was not refused, which is the whole of the finding");
      Assert.That(answer.Body, Is.Empty,
          "a refusal that carried the file would still be sending the file");
    }

    [Test]
    public async Task TheOperatingSystemIsNotInTheLibrary()
    {
      StartServer(FilmsShare());

      // The measurement from the deployment, on this machine: C:\Windows\win.ini, which is small, always present
      // and nobody's media. Asserting the refusal and not the path, because a test that required Windows would be
      // a test that does not run on the platforms this work is for.
      Answer answer = await AskForAsync(
          Path.Combine(Path.GetPathRoot(Path.GetTempPath()), "Windows", "win.ini"));

      AssertStatus(answer, 403,
          "the server used to answer this with the file; whatever the operating system, it is not media");
    }

    [Test]
    public async Task APathThatWalksOutOfTheShareIsNotSent()
    {
      StartServer(FilmsShare());

      // The shape that a prefix test lets through and a boundary does not: it starts inside the share and ends
      // outside it, and the local provider turns it into a real DOS path by swapping separators and nothing else.
      string traversal = Path.Combine(_filmsDir, "..", "notes.txt");
      Answer answer = await AskForAsync(traversal);

      Assert.That(File.Exists(traversal), Is.True, "the file the guard refused is the one that was asked for");
      AssertStatus(answer, 403,
          "'" + traversal + "' begins inside the share and is the file that is not; the guard compares paths, not prefixes");
      Assert.That(answer.Body, Has.Length.EqualTo(0));
    }

    [Test]
    public async Task AFolderThatOnlySharesTheNameOfAShareIsNotSent()
    {
      // ".../films-other/x.txt" starts with ".../films" as a string. A guard that compared strings without the
      // separator would serve a directory that was never registered.
      string otherDir = Path.Combine(_root, "films-other");
      Directory.CreateDirectory(otherDir);
      string other = Path.Combine(otherDir, "x.txt");
      File.WriteAllText(other, "a different folder");

      StartServer(FilmsShare());

      Answer answer = await AskForAsync(other);

      AssertStatus(answer, 403, "a share called 'films' is not a share called 'films-other'");
    }

    [Test]
    public async Task AFileInsideAShareIsSentAsItself()
    {
      StartServer(FilmsShare());

      Answer answer = await AskForAsync(_filmPath);

      AssertStatus(answer, 200,
          "a registered folder is the whole point of the endpoint: refusing the library would only move the hole");
      Assert.That(answer.Body, Has.Length.EqualTo(_filmBytes.Length));
      Assert.That(answer.Body, Is.EqualTo(_filmBytes),
          "the bytes that arrive are the bytes of the file, which is what a player checks by asking for ranges");
    }

    [Test]
    public async Task AServerWithNoRuleRegisteredSendsNothing()
    {
      // No shares and no guard: a host that does not install the rule gets the safe half of the mistake. This is
      // the state the code was in before the registration line, so it is a state worth naming out loud.
      StartServer(null);

      Answer answer = await AskForAsync(_filmPath);

      AssertStatus(answer, 403,
          "nobody said what may be served, so nothing may be; the previous behaviour was the opposite");
    }

    [Test]
    public async Task TheShareRootItselfIsNotSent()
    {
      // The folder is not media either, it is where media is. Being the share is not being inside it.
      StartServer(FilmsShare());

      Answer answer = await AskForAsync(_filmsDir);

      AssertStatus(answer, 403,
          "asking for the folder a share was registered on is not asking for a file inside it");
    }

    [Test]
    public void APathOnSomeOtherProviderIsNotSentJustBecauseTheTextMatches()
    {
      // The provider id is part of the decision. A guard that read only the text would let a caller claim that a
      // path belongs to a share registered on an unrelated provider.
      SharesResourceAccessGuard guard = GuardWith(FilmsShare());
      string providerPath = LocalFsResourceProviderBase.ToProviderPath(_filmPath);

      Assert.That(guard.IsAllowed(ResourcePath.BuildBaseProviderPath(
          LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, providerPath)), Is.True,
          "the share's own provider does serve it, so what follows is about the id and not about the file");
      Assert.That(guard.IsAllowed(ResourcePath.BuildBaseProviderPath(Guid.NewGuid(), providerPath)), Is.False,
          "the same text under a provider nobody registered is not inside the share");
    }

    [Test]
    public void AShareReadsOnceWhileAFilmPlays()
    {
      // One film is hundreds of range requests. Reading the share table for each of them would be the guard's own
      // bug, and the cache is invisible unless somebody counts.
      SharesResourceAccessGuard guard = GuardWith(FilmsShare());

      for (int i = 0; i < 200; i++)
        Assert.That(guard.IsAllowed(LocalPath(_filmPath)), Is.True, "request " + i);

      Assert.That(guard.SharesReadCount, Is.EqualTo(1),
          "two hundred requests for a film read the share table {0} times; they should read it once",
          guard.SharesReadCount);
    }

    [Test]
    public void AFileIsBelowAShareAndAShareIsNotBelowItself()
    {
      SharesResourceAccessGuard guard = GuardWith(FilmsShare());

      Assert.That(guard.IsAllowed(LocalPath(_filmPath)), Is.True);
      Assert.That(guard.IsAllowed(LocalPath(Path.Combine(_filmsDir, "deeper", "sub", "x.mkv"))), Is.True,
          "a subfolder of a share is in it, however deep, and the library records files that deep");
      Assert.That(guard.IsAllowed(LocalPath(_filmsDir)), Is.False,
          "the share root is not a file inside it; asking for the folder is asking for the share");
      Assert.That(guard.IsAllowed(LocalPath(_outsidePath)), Is.False);
      Assert.That(guard.IsAllowed(null), Is.False);
    }

    [Test]
    public void BrowsingStartsAtAShareEvenThoughTheFileServerRefusesIt()
    {
      // The media server's UPnP service asks the looser question, because a renderer asked to show somebody's
      // films asks for the folder they registered. The file server asks the stricter one, because a folder is not
      // something it can send.
      SharesResourceAccessGuard guard = GuardWith(FilmsShare());

      Assert.That(guard.IsInsideAShare(LocalPath(_filmsDir)), Is.True,
          "the share itself is where browsing begins; refusing it would refuse the feature");
      Assert.That(guard.IsInsideAShare(LocalPath(_filmPath)), Is.True);
      Assert.That(guard.IsInsideAShare(LocalPath(Path.Combine(_filmsDir, "deeper", "x.mkv"))), Is.True);
      Assert.That(guard.IsInsideAShare(LocalPath(_outsidePath)), Is.False);
      Assert.That(guard.IsInsideAShare(LocalPath(_root)), Is.False,
          "the folder holding the share is not the share; that is the whole disk above it");
      Assert.That(guard.IsInsideAShare(null), Is.False);

      Assert.That(guard.IsAllowed(LocalPath(_filmsDir)), Is.False,
          "the file server still refuses the folder, which is the one difference between the two questions");
    }

    [Test]
    public void AGuardThatCannotReadTheLibrarySendsNothingTheFirstTime()
    {
      var library = new Mock<IMediaLibrary>();
      library.Setup(get => get.GetShares(It.IsAny<string>()))
          .Throws(new InvalidOperationException("the database is not up yet"));
      SharesResourceAccessGuard guard = new SharesResourceAccessGuard(library.Object);

      Assert.That(guard.IsAllowed(LocalPath(_filmPath)), Is.False,
          "not knowing the shares is not a licence to serve; the server comes up before the database answers");
    }

    private static SharesResourceAccessGuard GuardWith(IDictionary<Guid, Share> shares)
    {
      var library = new Mock<IMediaLibrary>();
      library.Setup(get => get.GetShares(It.IsAny<string>())).Returns(shares);
      return new SharesResourceAccessGuard(library.Object);
    }

    #endregion

    #region What came back

    /// <summary>
    /// A what-was-answered. The body is kept as bytes because the answer is a file, and the reason phrase is kept
    /// because this endpoint puts its explanation there: the module reports an ill-formed request or a failure to
    /// read with the phrase rather than in the body, which is empty by design.
    /// </summary>
    private sealed class Answer
    {
      internal Answer(int status, string reason, byte[] body)
      {
        Status = status;
        Reason = reason;
        Body = body;
      }

      internal int Status { get; }
      internal string Reason { get; }
      internal byte[] Body { get; }

      public override string ToString()
      {
        return Status + (Reason == null ? "" : " '" + Reason + "'");
      }
    }

    #endregion
  }
}
