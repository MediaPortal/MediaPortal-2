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
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Server.Api;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.Http;

namespace Tests.Frontend
{
  /// <summary>
  /// The server's API, hosted in this process on a port of its own, so that the front end's client reads real
  /// answers rather than answers a test wrote down.
  /// </summary>
  /// <remarks>
  /// The controllers are the product's (<see cref="LibraryController"/> and <see cref="ServerInfoController"/>),
  /// routed below <c>/MediaPortal/Server</c> through the same shared listener the server uses where http.sys is
  /// not available. Only the media library behind them is mocked - a real one needs a database, a plugin manager
  /// and a startup, and that combination is what the server's own gates run.
  /// </remarks>
  internal sealed class InProcessApi : IDisposable
  {
    private readonly IDisposable _host;

    private InProcessApi(IDisposable host, Uri baseUrl)
    {
      _host = host;
      BaseUrl = baseUrl;
    }

    public Uri BaseUrl { get; }

    public static InProcessApi Start()
    {
      int port = GetFreePort();
      IDisposable host = SharedHttpPortHost.Start(
          new[] { "http://127.0.0.1:" + port + "/MediaPortal/Server" },
          services => services.AddControllers().AddApplicationPart(typeof(LibraryController).Assembly),
          app =>
          {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
          });

      return new InProcessApi(host, new Uri(string.Format("http://127.0.0.1:{0}/MediaPortal/Server/", port)));
    }

    public void Dispose()
    {
      _host.Dispose();
    }

    /// <summary>
    /// The two aspects every test here uses: one with two attributes, one with none, deliberately not in name
    /// order so that an answer that happens to be ordered by accident is not mistaken for an ordered one.
    /// </summary>
    public static readonly Guid CORE_ASPECT_ID = new Guid("11111111-1111-1111-1111-111111111111");
    public static readonly Guid VIDEO_ASPECT_ID = new Guid("22222222-2222-2222-2222-222222222222");

    /// <remarks>
    /// The two attributes are deliberately the two corners a front end has to tell apart: <c>Title</c> is
    /// indexed and holds one value, <c>Watched</c> is not indexed and holds many. Whether an attribute is a
    /// collection is decided by its cardinality, not by a flag of its own, so a fixture that made both
    /// attributes <c>ManyToMany</c> could not tell the two apart at all.
    /// </remarks>
    public static MediaItemAspectMetadata CreateAspect(Guid aspectId, string name)
    {
      return new MultipleMediaItemAspectMetadata(aspectId, name, new[]
      {
          new MediaItemAspectMetadata.MultipleAttributeSpecification("Title", typeof(string),
              Cardinality.Inline, true),
          new MediaItemAspectMetadata.MultipleAttributeSpecification("Watched", typeof(bool),
              Cardinality.ManyToMany, false)
      });
    }

    public static IDictionary<Guid, MediaItemAspectMetadata> CreateAspects()
    {
      return new Dictionary<Guid, MediaItemAspectMetadata>
      {
          { VIDEO_ASPECT_ID, CreateAspect(VIDEO_ASPECT_ID, "VideoFile") },
          { CORE_ASPECT_ID, CreateAspect(CORE_ASPECT_ID, "Core") }
      };
    }

    /// <summary>
    /// A media library that knows the given aspects and counts, and remembers what it was asked to count.
    /// </summary>
    public static Mock<IMediaLibrary> CreateLibrary(IDictionary<Guid, MediaItemAspectMetadata> aspects, int count,
        out List<IEnumerable<Guid>> countedAspects)
    {
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(aspects);

      List<IEnumerable<Guid>> calls = new List<IEnumerable<Guid>>();
      library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), false, true))
          .Callback<IEnumerable<Guid>, IFilter, bool, bool>(
              (necessary, filter, online, virtualItems) => calls.Add(necessary))
          .Returns(count);

      countedAspects = calls;
      return library;
    }

    /// <summary>
    /// A media library that is registered but has not finished starting: every question about its aspects or its
    /// size is answered the way <c>MediaLibrary</c> answers it before its startup has run, with
    /// <see cref="IllegalCallException"/>. This is the state a request that arrives while the server boots lands
    /// in, and the API's 503 answer to it is what the front end's "ask again" behaviour is built on.
    /// </summary>
    public static Mock<IMediaLibrary> CreateStartingLibrary()
    {
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      IllegalCallException notStarted = new IllegalCallException("The media library has not finished starting up");
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Throws(notStarted);
      library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), It.IsAny<bool>(),
          It.IsAny<bool>())).Throws(notStarted);
      return library;
    }

    public static void RegisterMediaLibrary(IMediaLibrary mediaLibrary)
    {
      ServiceRegistration.Set<IMediaLibrary>(mediaLibrary);
    }

    /// <summary>
    /// Takes the library away, which leaves the API with no library to ask at all. The server never does this
    /// while it serves requests - the library is registered before the listener starts - but the difference
    /// between "not ready" and "absent" is worth seeing, so a test states what each one answers.
    /// </summary>
    public static void RemoveMediaLibrary()
    {
      if (ServiceRegistration.IsRegistered<IMediaLibrary>())
        ServiceRegistration.Remove<IMediaLibrary>();
    }

    public static int GetFreePort()
    {
      var listener = new TcpListener(IPAddress.Loopback, 0);
      listener.Start();
      int port = ((IPEndPoint) listener.LocalEndpoint).Port;
      listener.Stop();
      return port;
    }
  }
}
