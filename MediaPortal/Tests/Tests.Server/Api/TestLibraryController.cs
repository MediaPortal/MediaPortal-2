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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using MediaPortal.Common;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Server.Api;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.Http;

namespace Tests.Server.Api
{
  /// <summary>
  /// The media library endpoint of the HTTP API: what the database is made of, and how much of it there is.
  /// </summary>
  /// <remarks>
  /// The library is mocked, and mocked strictly - an endpoint that decided to reach for another member of
  /// <see cref="IMediaLibrary"/> than the two it is supposed to read would fail here rather than issue an
  /// unexpected query against the running database. The metadata objects themselves are the real
  /// <see cref="MediaItemAspectMetadata"/> types, because the mapping from those to the answer is what is being
  /// tested; replacing them with fakes would test the fakes.
  /// </remarks>
  [TestFixture]
  public class TestLibraryController
  {
    #region Fixtures

    private static readonly Guid CORE_ASPECT_ID = new Guid("11111111-1111-1111-1111-111111111111");
    private static readonly Guid VIDEO_ASPECT_ID = new Guid("22222222-2222-2222-2222-222222222222");

    private readonly List<IDisposable> _listeners = new List<IDisposable>();

    [SetUp]
    public void SetUp()
    {
      RemoveMediaLibrary();
    }

    [TearDown]
    public void TearDown()
    {
      RemoveMediaLibrary();
      foreach (IDisposable listener in _listeners)
        listener.Dispose();
      _listeners.Clear();
    }

    private static void RemoveMediaLibrary()
    {
      if (ServiceRegistration.IsRegistered<IMediaLibrary>())
        ServiceRegistration.Remove<IMediaLibrary>();
    }

    private static void RegisterMediaLibrary(IMediaLibrary mediaLibrary)
    {
      ServiceRegistration.Set<IMediaLibrary>(mediaLibrary);
    }

    /// <summary>
    /// A media item aspect with one indexed single-value attribute and one collection attribute, which is the
    /// smallest shape that exercises every field of the answer.
    /// </summary>
    private static MediaItemAspectMetadata CreateAspect(Guid aspectId, string name)
    {
      return new MultipleMediaItemAspectMetadata(aspectId, name, new[]
      {
          new MediaItemAspectMetadata.MultipleAttributeSpecification("Title", typeof(string),
              Cardinality.ManyToMany, true),
          new MediaItemAspectMetadata.MultipleAttributeSpecification("Watched", typeof(bool),
              Cardinality.ManyToMany, false)
      });
    }

    private static IDictionary<Guid, MediaItemAspectMetadata> CreateAspects()
    {
      // Deliberately not in name order: the answer is ordered, and an answer that happened to be ordered
      // because the dictionary was would not prove it.
      return new Dictionary<Guid, MediaItemAspectMetadata>
      {
          { VIDEO_ASPECT_ID, CreateAspect(VIDEO_ASPECT_ID, "VideoFile") },
          { CORE_ASPECT_ID, CreateAspect(CORE_ASPECT_ID, "Core") }
      };
    }

    private static Mock<IMediaLibrary> CreateLibrary(IDictionary<Guid, MediaItemAspectMetadata> aspects,
        int count, out List<IEnumerable<Guid>> countedAspects)
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

    private static LibraryController.CountResult GetCount(IMediaLibrary library, string aspect)
    {
      RegisterMediaLibrary(library);
      Microsoft.AspNetCore.Mvc.ActionResult<LibraryController.CountResult> result =
          new LibraryController().GetCount(aspect);
      Assert.That(result.Result, Is.Null, "the answer is the object, not an error code");
      return result.Value;
    }

    #endregion

    #region The aspects endpoint

    [Test]
    public void AspectsAreAnsweredInNameOrderWithTheirAttributes()
    {
      List<IEnumerable<Guid>> ignored;
      RegisterMediaLibrary(CreateLibrary(CreateAspects(), 0, out ignored).Object);

      Microsoft.AspNetCore.Mvc.ActionResult<LibraryController.AspectsResult> action =
          new LibraryController().GetAspects();
      LibraryController.AspectsResult result = action.Value;

      Assert.That(result.Count, Is.EqualTo(2));
      Assert.That(result.Aspects.Select(aspect => aspect.Name), Is.EqualTo(new[] { "Core", "VideoFile" }));

      LibraryController.AspectResult core = result.Aspects[0];
      Assert.That(core.Id, Is.EqualTo(CORE_ASPECT_ID));
      Assert.That(core.Transient, Is.False);
      Assert.That(core.ReferencingAspectId, Is.Null);
      Assert.That(core.Attributes.Select(attribute => attribute.Name), Is.EqualTo(new[] { "Title", "Watched" }));
    }

    [Test]
    public void AnAttributeCarriesItsClrTypeAndItsIndexAndCollectionFlags()
    {
      // The front end builds its queries out of this: a filter on an unindexed attribute is slow, and a
      // multi-value attribute has to be edited with a list control. Getting these two flags wrong is a user
      // visible bug, not a cosmetic one.
      List<IEnumerable<Guid>> ignored;
      RegisterMediaLibrary(CreateLibrary(CreateAspects(), 0, out ignored).Object);

      LibraryController.AspectResult core = new LibraryController().GetAspects().Value.Aspects[0];

      LibraryController.AttributeResult title = core.Attributes[0];
      Assert.That(title.Type, Is.EqualTo(typeof(string).FullName));
      Assert.That(title.Indexed, Is.True);
      Assert.That(title.Collection, Is.True);

      LibraryController.AttributeResult watched = core.Attributes[1];
      Assert.That(watched.Type, Is.EqualTo(typeof(bool).FullName));
      Assert.That(watched.Indexed, Is.False);
    }

    [Test]
    public void WithoutAMediaLibraryTheEndpointSaysItIsUnavailable()
    {
      // A host without the backend must not answer "no aspects", which a client would read as an empty
      // database. 503 and 0 are different facts.
      Microsoft.AspNetCore.Mvc.ActionResult<LibraryController.AspectsResult> action =
          new LibraryController().GetAspects();

      Assert.That(action.Value, Is.Null);
      Assert.That(action.Result, Is.InstanceOf<Microsoft.AspNetCore.Mvc.StatusCodeResult>());
      Assert.That(((Microsoft.AspNetCore.Mvc.StatusCodeResult) action.Result).StatusCode, Is.EqualTo(503));
    }

    [Test]
    public void AnAspectWhoseStorageIsStillBeingCreatedIsLeftOut()
    {
      // Measured, not theoretical: the first time a live server was asked for its aspects it answered HTTP 500.
      // While the media library creates the tables of an aspect it keeps a placeholder under that aspect's id
      // and fills the metadata in afterwards, and a server that is asked about its schema while it is starting
      // is asked about a dictionary with holes in it. Those aspects are not in the answer; the endpoint does not
      // walk into them.
      Dictionary<Guid, MediaItemAspectMetadata> aspects = (Dictionary<Guid, MediaItemAspectMetadata>) CreateAspects();
      aspects.Add(new Guid("33333333-3333-3333-3333-333333333333"), null);

      List<IEnumerable<Guid>> ignored;
      Mock<IMediaLibrary> library = CreateLibrary(aspects, 5, out ignored);
      RegisterMediaLibrary(library.Object);

      LibraryController.AspectsResult result = new LibraryController().GetAspects().Value;
      Assert.That(result.Count, Is.EqualTo(2));
      Assert.That(result.Aspects.Select(aspect => aspect.Name), Is.EqualTo(new[] { "Core", "VideoFile" }));

      // The count endpoint looks names up in the same dictionary, so it has to tolerate the same hole.
      Assert.That(GetCount(library.Object, "Core").AspectId, Is.EqualTo(CORE_ASPECT_ID));
    }

    #endregion

    #region The count endpoint

    [Test]
    public void TheCountForAnAspectAsksForThatAspectById()
    {
      // The wire uses names, the database uses ids, and the mapping has to happen somewhere. That it happened
      // is visible in what the library was asked for.
      List<IEnumerable<Guid>> counted;
      IMediaLibrary library = CreateLibrary(CreateAspects(), 17, out counted).Object;

      LibraryController.CountResult result = GetCount(library, "Core");

      Assert.That(result.Aspect, Is.EqualTo("Core"));
      Assert.That(result.AspectId, Is.EqualTo(CORE_ASPECT_ID));
      Assert.That(result.Count, Is.EqualTo(17));
      Assert.That(counted.Count, Is.EqualTo(1));
      Assert.That(counted[0], Is.EquivalentTo(new[] { CORE_ASPECT_ID }));
    }

    [Test]
    public void TheAspectNameIsNotCaseSensitive()
    {
      List<IEnumerable<Guid>> ignored;
      IMediaLibrary library = CreateLibrary(CreateAspects(), 3, out ignored).Object;

      Assert.That(GetCount(library, "videofile").AspectId, Is.EqualTo(VIDEO_ASPECT_ID));
    }

    [Test]
    public void CountingWithoutAnAspectCountsEveryMediaItem()
    {
      List<IEnumerable<Guid>> counted;
      IMediaLibrary library = CreateLibrary(CreateAspects(), 1234, out counted).Object;

      LibraryController.CountResult result = GetCount(library, null);

      Assert.That(result.Aspect, Is.Null);
      Assert.That(result.AspectId, Is.Null);
      Assert.That(result.Count, Is.EqualTo(1234));
      Assert.That(counted[0], Is.Empty, "no aspect is required of a counted item");
    }

    [Test]
    public void AnUnknownAspectNameIsNotFound()
    {
      // Not "zero items", which would read as an empty library. The aspect does not exist on this server.
      List<IEnumerable<Guid>> ignored;
      RegisterMediaLibrary(CreateLibrary(CreateAspects(), 0, out ignored).Object);

      Microsoft.AspNetCore.Mvc.ActionResult<LibraryController.CountResult> action =
          new LibraryController().GetCount("NoSuchAspect");

      Assert.That(action.Result, Is.InstanceOf<Microsoft.AspNetCore.Mvc.NotFoundResult>());
    }

    #endregion

    #region While the library is starting

    /// <summary>
    /// A library mock that refuses every question the way a library which has not finished starting does: with
    /// the exception <see cref="IMediaLibrary"/> implementations throw for calls made in the wrong state.
    /// </summary>
    private static Mock<IMediaLibrary> CreateStartingLibrary()
    {
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata())
          .Throws(new IllegalCallException("The media library has not finished starting up"));
      library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), false, true))
          .Throws(new IllegalCallException("The media library has not finished starting up"));
      return library;
    }

    private static Microsoft.AspNetCore.Mvc.ObjectResult AsStatus<TResult>(
        Microsoft.AspNetCore.Mvc.ActionResult<TResult> action)
    {
      Assert.That(action.Value, Is.Null, "the answer is an error code, not an object");
      Assert.That(action.Result, Is.InstanceOf<Microsoft.AspNetCore.Mvc.ObjectResult>(),
          "a not-ready answer carries its reason");
      return (Microsoft.AspNetCore.Mvc.ObjectResult) action.Result;
    }

    [Test]
    public void ALibraryThatHasNotStartedAnswersUnavailableRatherThanError()
    {
      // Measured, not theoretical: a request that arrived while the media library was still starting was
      // answered with HTTP 500. The library's managers do not exist that early, so every query walked into a
      // NullReferenceException somewhere inside, and the endpoint had nothing to report but an internal error.
      // That is the wrong fact: a client that is simply too early should be told to ask again.
      ServiceRegistration.Set<ILogger>(new NoLogger());
      RegisterMediaLibrary(CreateStartingLibrary().Object);

      Microsoft.AspNetCore.Mvc.ObjectResult status =
          AsStatus(new LibraryController().GetAspects());

      Assert.That(status.StatusCode, Is.EqualTo(503));
      Assert.That(((LibraryController.ErrorResponse) status.Value).Error, Does.Contain("starting"));
    }

    [Test]
    public void CountingAnAspectWhileTheLibraryIsStartingIsAlsoUnavailable()
    {
      ServiceRegistration.Set<ILogger>(new NoLogger());
      IMediaLibrary library = CreateStartingLibrary().Object;
      RegisterMediaLibrary(library);

      Assert.That(AsStatus(new LibraryController().GetCount("Core")).StatusCode, Is.EqualTo(503));
    }

    [Test]
    public void CountingEverythingWhileTheLibraryIsStartingIsAlsoUnavailable()
    {
      // The branch without an aspect name never reads the aspect metadata, so it needs its own guard.
      ServiceRegistration.Set<ILogger>(new NoLogger());
      RegisterMediaLibrary(CreateStartingLibrary().Object);

      Assert.That(AsStatus(new LibraryController().GetCount(null)).StatusCode, Is.EqualTo(503));
    }

    #endregion

    #region Over a socket

    /// <summary>
    /// The endpoint the gates will ask, through the shared listener, with the API assembly as the application
    /// part - the whole chain for an endpoint that reads the media library rather than the process.
    /// </summary>
    [Test]
    public async Task TheLibraryEndpointsAnswerBelowTheServerPrefix()
    {
      List<IEnumerable<Guid>> ignored;
      RegisterMediaLibrary(CreateLibrary(CreateAspects(), 7, out ignored).Object);

      int port = GetFreePort();
      _listeners.Add(SharedHttpPortHost.Start(
          new[] { "http://127.0.0.1:" + port + "/MediaPortal/Server" },
          services => services.AddControllers().AddApplicationPart(typeof(LibraryController).Assembly),
          app =>
          {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
          }));

      using (var client = new HttpClient())
      {
        string aspects = await client.GetStringAsync(
            "http://127.0.0.1:" + port + "/MediaPortal/Server/api/library/aspects");
        Assert.That(aspects, Does.Contain("\"count\":2"));
        Assert.That(aspects, Does.Contain("\"name\":\"Core\""));
        Assert.That(aspects, Does.Contain("\"indexed\":true"));

        string count = await client.GetStringAsync(
            "http://127.0.0.1:" + port + "/MediaPortal/Server/api/library/count?aspect=Core");
        Assert.That(count, Does.Contain("\"aspect\":\"Core\""));
        Assert.That(count, Does.Contain("\"count\":7"));
      }
    }

    private static int GetFreePort()
    {
      var listener = new TcpListener(IPAddress.Loopback, 0);
      listener.Start();
      int port = ((IPEndPoint) listener.LocalEndpoint).Port;
      listener.Stop();
      return port;
    }

    #endregion
  }
}
