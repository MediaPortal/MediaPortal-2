#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Frontend.Api;
using MediaPortal.Server.Api;
using FanArtWarming = MediaPortal.Frontend.Api.FanArtWarming;
using MediaAspect = MediaPortal.Common.MediaManagement.DefaultItemAspects.MediaAspect;

namespace Tests.Frontend
{
  /// <summary>
  /// The real warming endpoints of <see cref="MediaController"/>, hosted in this process over a library of titles
  /// that have no files of their own.
  /// </summary>
  /// <remarks>
  /// Titles without a file are on purpose: the server answers them with "nothing of that kind lies next to this
  /// title", which is a real answer taking real memory, and it comes without a filesystem fixture. What the tests
  /// here are about is what the front end makes of the server's answer, not whether the server can find a
  /// <c>fanart.jpg</c> - which is <c>Tests.Server.TestFanArtWarming</c>'s business.
  /// </remarks>
  internal sealed class TestWarmingServer : IDisposable
  {
    private readonly InProcessApi _api;

    private TestWarmingServer(InProcessApi api, ServerApiClient client)
    {
      _api = api;
      Client = client;
    }

    /// <summary>The client to ask the hosted server with, so a test can read what the screen reads.</summary>
    internal ServerApiClient Client { get; }

    internal static TestWarmingServer Start(int titles)
    {
      // The pass is the server's one slot, and the queue look is what a waiting pass costs a test.
      FanArtWarmingPass.QueueLook = TimeSpan.FromMilliseconds(10);
      FanArtWarmingPass.PatienceWithTheImporter = TimeSpan.FromSeconds(20);
      RemoveLeftovers();
      LocalFanArtSearch.Worn.ForgetEverything();

      InProcessApi.RegisterMediaLibrary(LibraryWith(titles));
      InProcessApi api = InProcessApi.Start();
      return new TestWarmingServer(api, new ServerApiClient(api.BaseUrl));
    }

    internal Task<FanArtWarming> WarmAsync()
    {
      return Client.WarmFanArtAsync();
    }

    internal Task<FanArtWarming> ReadAsync()
    {
      return Client.GetFanArtWarmingAsync();
    }

    /// <summary>
    /// Reading until the server says it is through. A pass runs on a background thread by design, so a test that
    /// looked once would be reading whatever the pass happened to be doing.
    /// </summary>
    internal async Task WaitUntilQuietAsync()
    {
      Stopwatch watch = Stopwatch.StartNew();
      while (watch.Elapsed < TimeSpan.FromSeconds(20))
      {
        FanArtWarming said = await ReadAsync().ConfigureAwait(false);
        if (!said.Running)
          return;
        await Task.Delay(20).ConfigureAwait(false);
      }

      Assert.Fail("the server's warming pass was still running after twenty seconds");
    }

    public void Dispose()
    {
      Client.Dispose();
      _api.Dispose();
      Clear();
    }

    /// <summary>Taking the library away again, and the answers the server gathered about it.</summary>
    internal static void Clear()
    {
      InProcessApi.RemoveMediaLibrary();
      LocalFanArtSearch.Worn.ForgetEverything();
    }

    /// <summary>
    /// Taking away what another fixture may have left registered: a library the pass would walk, and an importer
    /// the pass would wait for - which with the default patience would mean three hours of a test doing nothing.
    /// </summary>
    private static void RemoveLeftovers()
    {
      if (ServiceRegistration.IsRegistered<IImporterWorker>())
        ServiceRegistration.Remove<IImporterWorker>();
      if (ServiceRegistration.IsRegistered<IMediaLibrary>())
        ServiceRegistration.Remove<IMediaLibrary>();
    }

    private static IMediaLibrary LibraryWith(int titles)
    {
      List<MediaItem> items = new List<MediaItem>();
      for (int index = 0; index < titles; index++)
        items.Add(new TitleWithoutAFile("Title " + (index + 1)));

      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata())
          .Returns(new Dictionary<Guid, MediaItemAspectMetadata> { { MediaAspect.ASPECT_ID, MediaAspect.Metadata } });
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), It.IsAny<bool>(), It.IsAny<Guid?>(),
              It.IsAny<bool>()))
          .Returns(items);
      return library.Object;
    }

    /// <summary>A title with a name and no file, which is what a series and a season are.</summary>
    private sealed class TitleWithoutAFile : MediaItem
    {
      internal TitleWithoutAFile(string name)
          : base(Guid.NewGuid(), Aspects(name))
      {
      }

      public override IResourceLocator GetResourceLocator()
      {
        return null;
      }

      private static IDictionary<Guid, IList<MediaItemAspect>> Aspects(string name)
      {
        SingleMediaItemAspect media = new SingleMediaItemAspect(MediaAspect.Metadata);
        media.SetAttribute(MediaAspect.ATTR_TITLE, name);
        media.SetAttribute(MediaAspect.ATTR_SORT_TITLE, name);
        media.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, DateTime.Now);

        return new Dictionary<Guid, IList<MediaItemAspect>>
        {
            { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } }
        };
      }
    }
  }

  /// <summary>
  /// A transport that writes down what the front end asked for and answers the few routes the folders screen uses.
  /// </summary>
  /// <remarks>
  /// Scripted rather than hosted, because the thing to prove about the folders screen is that it <em>asked</em>:
  /// the server starts the same warming pass by itself when an import is queued, so on a real server the pass is
  /// there whether this screen knocked on the door or not, and the state of the door cannot answer who rang it.
  /// </remarks>
  internal sealed class RecordingTransport : HttpMessageHandler
  {
    private readonly List<string> _asked = new List<string>();
    private int _queueReads;

    /// <summary>Answering the warming request with a refusal, the way a server that wants a login does.</summary>
    internal bool RefusesWarming { get; set; }

    /// <summary>Whether the front end sent this kind of request to this route.</summary>
    internal bool AskedFor(string urlPart, HttpMethod method)
    {
      lock (_asked)
      {
        return _asked.Any(asked => asked.StartsWith(method.Method + " ") && asked.Contains(urlPart));
      }
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellation)
    {
      string path = request.RequestUri.AbsolutePath;
      lock (_asked)
      {
        _asked.Add(request.Method.Method + " " + path);
      }

      if (path.EndsWith("api/media/shares"))
        return Answer(HttpStatusCode.OK, SHARES);

      if (request.Method == HttpMethod.Post && path.EndsWith("/import"))
        return Answer(HttpStatusCode.Accepted, QUEUED);

      if (path.EndsWith("api/media/import"))
      {
        int read;
        lock (this)
        {
          read = ++_queueReads;
        }
        // Busy for the first two looks and empty afterwards, which is the sequence that has the screen say
        // "importing" once and "finished" after it.
        return Answer(HttpStatusCode.OK, read <= 2 ? QUEUED : DRAINED);
      }

      if (path.EndsWith("api/media/fanart/warming"))
      {
        return RefusesWarming
            ? Answer(HttpStatusCode.Forbidden, "{\"error\":\"this server wants you to sign in first\"}")
            : Answer(HttpStatusCode.Accepted, "{\"running\":true,\"titles\":0,\"skipped\":0,\"kindsFound\":0,"
                + "\"answers\":0,\"waiting\":null,\"stopped\":null}");
      }

      return Answer(HttpStatusCode.NotFound, "{}");
    }

    private static Task<HttpResponseMessage> Answer(HttpStatusCode code, string json)
    {
      return Task.FromResult(new HttpResponseMessage(code)
      {
          Content = new StringContent(json, Encoding.UTF8, "application/json")
      });
    }

    private const string SHARES = "{\"count\":1,\"mediaCategories\":[\"Movie\"],\"shares\":[{"
        + "\"id\":\"11111111-1111-1111-1111-111111111111\","
        + "\"name\":\"Films\",\"path\":\"D:/Films\",\"resourcePath\":\"Films\","
        + "\"systemId\":\"22222222-2222-2222-2222-222222222222\",\"local\":true,"
        + "\"mediaCategories\":[\"Movie\"],\"useShareWatcher\":true}]}";

    private const string QUEUED = "{\"pending\":1,\"suspended\":false,\"jobs\":"
        + "[{\"type\":\"Import\",\"path\":\"D:/Films\",\"includeSubDirectories\":true}]}";

    private const string DRAINED = "{\"pending\":0,\"suspended\":false,\"jobs\":[]}";
  }
}
