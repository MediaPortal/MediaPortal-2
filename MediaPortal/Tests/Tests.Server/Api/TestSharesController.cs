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
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using NUnit.Framework;
using MediaPortal.Backend.Exceptions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Server.Api;

namespace Tests.Server.Api
{
  /// <summary>
  /// The shares endpoint: the folder a library is built from, and the import that fills it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The tests care more about what is <i>refused</i> than about what is listed, because that is where an import
  /// goes wrong without ever showing a symptom. A share registered for a folder that does not exist, or with a
  /// media category no extractor is registered for, is accepted by the library, walked by the importer, and
  /// leaves an empty library behind - a state a person looking at it months later cannot tell apart from a server
  /// that is broken. Each of those is answered with a number and a sentence here.
  /// </para>
  /// <para>
  /// The folder is a real temporary directory rather than a made-up path: the endpoint's claim is "the server can
  /// see this folder", and a test that asserts that claim against a path nobody created would be asserting a
  /// fiction. What is faked is the library, the importer and the media accessor, which need a running server.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestSharesController
  {
    private const string LOCAL_SYSTEM = "11111111-1111-1111-1111-111111111111";
    private const string OTHER_SYSTEM = "22222222-2222-2222-2222-222222222222";

    private static readonly Guid SHARE_ID = new Guid("33333333-3333-3333-3333-333333333333");
    private static readonly Guid REMOTE_SHARE_ID = new Guid("44444444-4444-4444-4444-444444444444");
    // A provider that is not the local filesystem, so that a share through it is a share this machine has no
    // path for - which is the case the answer has to distinguish from a local folder.
    private static readonly Guid OTHER_PROVIDER = new Guid("55555555-5555-5555-5555-555555555555");

    private string _folder;

    [SetUp]
    public void SetUp()
    {
      _folder = Directory.CreateDirectory(
          Path.Combine(Path.GetTempPath(), "mp2-shares-" + Guid.NewGuid().ToString("N"))).FullName;
      ClearServices();
    }

    [TearDown]
    public void TearDown()
    {
      ClearServices();
      try
      {
        if (_folder != null && Directory.Exists(_folder))
          Directory.Delete(_folder, true);
      }
      catch (IOException)
      {
        // A scratch directory left behind is not worth failing a test over
      }
    }

    private static void ClearServices()
    {
      if (ServiceRegistration.IsRegistered<IMediaLibrary>())
        ServiceRegistration.Remove<IMediaLibrary>();
      if (ServiceRegistration.IsRegistered<ISystemResolver>())
        ServiceRegistration.Remove<ISystemResolver>();
      if (ServiceRegistration.IsRegistered<IMediaAccessor>())
        ServiceRegistration.Remove<IMediaAccessor>();
      if (ServiceRegistration.IsRegistered<IImporterWorker>())
        ServiceRegistration.Remove<IImporterWorker>();
    }

    #region Fakes

    /// <summary>
    /// A server that can import the given categories, and no others.
    /// </summary>
    private static void GivenCategories(params string[] categories)
    {
      Dictionary<string, MediaCategory> known = categories
          .ToDictionary(category => category, category => new MediaCategory(category, null));
      Mock<IMediaAccessor> accessor = new Mock<IMediaAccessor>(MockBehavior.Strict);
      accessor.SetupGet(a => a.MediaCategories).Returns(known);
      ServiceRegistration.Set<IMediaAccessor>(accessor.Object);
    }

    private static void GivenLocalSystem()
    {
      Mock<ISystemResolver> resolver = new Mock<ISystemResolver>(MockBehavior.Strict);
      resolver.SetupGet(r => r.LocalSystemId).Returns(LOCAL_SYSTEM);
      ServiceRegistration.Set<ISystemResolver>(resolver.Object);
    }

    /// <summary>
    /// The share of a folder on a given system, with the path a local share of that folder would have.
    /// </summary>
    private static Share BuildShare(Guid id, string systemId, string folder, string name, params string[] categories)
    {
      return new Share(id, systemId, LocalFsResourceProviderBase.ToResourcePath(folder), name, true, categories);
    }

    private static Mock<IMediaLibrary> GivenLibrary(MockBehavior behavior)
    {
      Mock<IMediaLibrary> library = new Mock<IMediaLibrary>(behavior);
      ServiceRegistration.Set<IMediaLibrary>(library.Object);
      return library;
    }

    private static Mock<IImporterWorker> GivenImporter(MockBehavior behavior)
    {
      Mock<IImporterWorker> worker = new Mock<IImporterWorker>(behavior);
      ServiceRegistration.Set<IImporterWorker>(worker.Object);
      return worker;
    }

    private static SharesController Ask()
    {
      return new SharesController
      {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
      };
    }

    /// <summary>
    /// The status an endpoint answered with: the one it named, or 200 for the case where it answered with the
    /// object itself and let MVC decide.
    /// </summary>
    private static int Status<TResult>(ActionResult<TResult> action)
    {
      if (action.Result == null)
        return 200;
      IStatusCodeActionResult status = action.Result as IStatusCodeActionResult;
      Assert.That(status, Is.Not.Null, "the endpoint answered with something that has no status code");
      return status.StatusCode ?? 200;
    }

    private static int Status(IActionResult result)
    {
      IStatusCodeActionResult status = result as IStatusCodeActionResult;
      Assert.That(status, Is.Not.Null, "the endpoint answered with something that has no status code");
      return status.StatusCode ?? 200;
    }

    /// <summary>
    /// The object an endpoint put into its answer, whether that was a result or an error.
    /// </summary>
    private static object Body<TResult>(ActionResult<TResult> action)
    {
      // Two shapes: an error answer carries its object in the ObjectResult the endpoint returned, a successful
      // one is the object itself and lets MVC pick the status code.
      return (action.Result as ObjectResult)?.Value ?? (object) action.Value;
    }

    private static string ErrorOf<TResult>(ActionResult<TResult> action)
    {
      return (Body(action) as SharesController.ErrorResponse)?.Error;
    }

    #endregion

    #region Listing

    [Test]
    public void WithoutAMediaLibraryTheListOfSharesIsNotAnAnswer()
    {
      // 503 rather than an empty list: "nobody is at home" and "there is nothing here" are different facts, and a
      // folders screen that acts on the second one tells the user to add a folder when the server is still up.
      ActionResult<SharesController.SharesResult> result = Ask().GetShares();
      Assert.That(Status(result), Is.EqualTo(503));
    }

    [Test]
    public void SharesAreListedByNameWithThePathTheLibraryStores()
    {
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      GivenLocalSystem();
      GivenCategories("Video");
      library.Setup(l => l.GetShares(null)).Returns(new Dictionary<Guid, Share>
      {
        { SHARE_ID, BuildShare(SHARE_ID, LOCAL_SYSTEM, _folder, "Films", "Video") },
        {
          REMOTE_SHARE_ID,
          new Share(REMOTE_SHARE_ID, OTHER_SYSTEM,
              ResourcePath.BuildBaseProviderPath(OTHER_PROVIDER, "/Films/Filme"), "Other machine", true,
              new[] { "Video" })
        }
      });

      ActionResult<SharesController.SharesResult> result = Ask().GetShares();

      Assert.That(Status(result), Is.EqualTo(200));
      SharesController.SharesResult shares = result.Value;
      Assert.That(shares, Is.Not.Null, "a listing answer is the object, not a status code");
      Assert.That(shares.Count, Is.EqualTo(2));
      Assert.That(shares.Shares.Select(share => share.Name), Is.EqualTo(new[] { "Films", "Other machine" }));

      SharesController.ShareResult films = shares.Shares[0];
      Assert.That(films.Id, Is.EqualTo(SHARE_ID));
      Assert.That(films.Path, Is.EqualTo(_folder));
      Assert.That(films.ResourcePath, Does.Contain("://"));
      Assert.That(films.SystemId, Is.EqualTo(LOCAL_SYSTEM));
      Assert.That(films.Local, Is.True);
      Assert.That(films.MediaCategories, Is.EqualTo(new[] { "Video" }));
      Assert.That(films.UseShareWatcher, Is.True);

      // A folder on another machine has no path in this machine's filesystem, and the answer says so instead of
      // inventing one that would point at a different folder or none.
      SharesController.ShareResult other = shares.Shares[1];
      Assert.That(other.Local, Is.False);
      Assert.That(other.Path, Is.Null);
      Assert.That(other.ResourcePath, Is.Not.Null);
    }

    [Test]
    public void TheMediaCategoriesOnOfferAreTheOnesThePluginsRegistered()
    {
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      GivenLocalSystem();
      GivenCategories("Video", "Audio", "Picture");
      library.Setup(l => l.GetShares(null)).Returns(new Dictionary<Guid, Share>());

      SharesController.SharesResult result = Ask().GetShares().Value;

      Assert.That(result.MediaCategories, Is.EqualTo(new[] { "Audio", "Picture", "Video" }));
    }

    #endregion

    #region Adding

    [Test]
    public void AFolderIsRegisteredOnTheLocalSystemUnderItsOwnNameWithEveryKnownCategory()
    {
      Share registered = null;
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      GivenLocalSystem();
      GivenCategories("Video", "Audio");
      library.Setup(l => l.RegisterShare(It.IsAny<Share>()))
          .Callback<Share>(share => registered = share);

      ActionResult<SharesController.ShareResult> result = Ask().AddShare(new SharesController.ShareRequest
      {
        Path = _folder
      });

      Assert.That(Status(result), Is.EqualTo(201));
      Assert.That(registered, Is.Not.Null);
      Assert.That(registered.Name, Is.EqualTo(new DirectoryInfo(_folder).Name));
      Assert.That(registered.SystemId, Is.EqualTo(LOCAL_SYSTEM));
      Assert.That(registered.BaseResourcePath, Is.EqualTo(LocalFsResourceProviderBase.ToResourcePath(_folder)));
      Assert.That(registered.MediaCategories, Is.EquivalentTo(new[] { "Audio", "Video" }));
      Assert.That(registered.UseShareWatcher, Is.True);

      // What the caller gets back is what was stored, so that a screen can show the id of the share it just made.
      Assert.That((Body(result) as SharesController.ShareResult).Name, Is.EqualTo(registered.Name));
      Assert.That((Body(result) as SharesController.ShareResult).Id, Is.EqualTo(registered.ShareId));
    }

    [Test]
    public void ACallerMayNameAShareAndNarrowItToOneCategory()
    {
      Share registered = null;
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      GivenLocalSystem();
      GivenCategories("Video", "Audio");
      library.Setup(l => l.RegisterShare(It.IsAny<Share>())).Callback<Share>(share => registered = share);

      Ask().AddShare(new SharesController.ShareRequest
      {
        Path = _folder,
        Name = "Kino",
        MediaCategories = new List<string> { " video " },
        UseShareWatcher = false
      });

      Assert.That(registered.Name, Is.EqualTo("Kino"));
      // The server's own spelling is what gets stored, so that two callers meaning the same thing do not create
      // two different categories in the library.
      Assert.That(registered.MediaCategories, Is.EquivalentTo(new[] { "Video" }));
      Assert.That(registered.UseShareWatcher, Is.False);
    }

    [Test]
    public void ARequestThatDoesNotNameAFolderIsRefused()
    {
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      GivenLocalSystem();
      GivenCategories("Video");

      Assert.That(Status(Ask().AddShare(null)), Is.EqualTo(400));
      Assert.That(Status(Ask().AddShare(new SharesController.ShareRequest { Path = "  " })), Is.EqualTo(400));
      library.Verify(l => l.RegisterShare(It.IsAny<Share>()), Times.Never());
    }

    [Test]
    public void AFolderTheServerCannotSeeIsRefused()
    {
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      GivenLocalSystem();
      GivenCategories("Video");

      string missing = Path.Combine(_folder, "nope");
      ActionResult<SharesController.ShareResult> result =
          Ask().AddShare(new SharesController.ShareRequest { Path = missing });

      Assert.That(Status(result), Is.EqualTo(400));
      Assert.That(ErrorOf(result), Does.Contain("cannot see"));
      library.Verify(l => l.RegisterShare(It.IsAny<Share>()), Times.Never());
    }

    [Test]
    public void ACategoryTheServerHasNoExtractorForIsRefusedWithTheNamesThatWork()
    {
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      GivenLocalSystem();
      GivenCategories("Video", "Audio");

      ActionResult<SharesController.ShareResult> result = Ask().AddShare(new SharesController.ShareRequest
      {
        Path = _folder,
        MediaCategories = new List<string> { "Movies" }
      });

      // A job for an unknown category holds no extractors and imports nothing at all, silently. Saying
      // "Video, Audio" is the whole fix for a mistake a person cannot diagnose from an empty shelf.
      Assert.That(Status(result), Is.EqualTo(400));
      Assert.That(ErrorOf(result), Does.Contain("'Movies'"));
      Assert.That(ErrorOf(result), Does.Contain("'Video'"));
      library.Verify(l => l.RegisterShare(It.IsAny<Share>()), Times.Never());
    }

    [Test]
    public void AFolderIsStoredTheWayTheFilesystemSpellsIt()
    {
      Share registered = null;
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      GivenLocalSystem();
      GivenCategories("Video");
      library.Setup(l => l.RegisterShare(It.IsAny<Share>())).Callback<Share>(share => registered = share);

      string spelled = _folder + Path.DirectorySeparatorChar + ".";
      Assert.That(spelled, Is.Not.EqualTo(_folder));
      Ask().AddShare(new SharesController.ShareRequest { Path = spelled, Name = "x" });

      Assert.That(registered.BaseResourcePath, Is.EqualTo(LocalFsResourceProviderBase.ToResourcePath(_folder)));
    }

    [Test]
    public void ASecondShareForTheSameFolderNamesTheOneThatAlreadyExists()
    {
      Share existing = BuildShare(SHARE_ID, LOCAL_SYSTEM, _folder, "Films", "Video");
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      GivenLocalSystem();
      GivenCategories("Video");
      library.Setup(l => l.RegisterShare(It.IsAny<Share>()))
          .Throws(new ShareExistsException("A share with the given system '{0}' and path '{1}' already exists",
              LOCAL_SYSTEM, _folder));
      library.Setup(l => l.GetShares(LOCAL_SYSTEM)).Returns(new Dictionary<Guid, Share> { { SHARE_ID, existing } });

      ActionResult<SharesController.ShareResult> result =
          Ask().AddShare(new SharesController.ShareRequest { Path = _folder });

      // The caller wanted this folder in its library, and it is - so the answer is not "you did something wrong"
      // but "here is the share you wanted".
      Assert.That(Status(result), Is.EqualTo(409));
      Assert.That(ErrorOf(result), Does.Contain(SHARE_ID.ToString()));
      Assert.That(ErrorOf(result), Does.Contain("Films"));
    }

    [Test]
    public void AddingAShareDoesNotQueueASecondImport()
    {
      // Registering a local share already queues it (MediaLibrary.TryScheduleLocalShareImport). Calling the
      // importer as well would put the same tree in the queue twice, so the double call is what this pins out.
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      Mock<IImporterWorker> worker = GivenImporter(MockBehavior.Loose);
      GivenLocalSystem();
      GivenCategories("Video");
      library.Setup(l => l.RegisterShare(It.IsAny<Share>()));

      Ask().AddShare(new SharesController.ShareRequest { Path = _folder });

      worker.Verify(w => w.ScheduleImport(It.IsAny<ResourcePath>(), It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()),
          Times.Never());
    }

    [Test]
    public void WithoutTheMediaAccessorACategoryCannotBeChecked()
    {
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      GivenLocalSystem();

      ActionResult<SharesController.ShareResult> result = Ask().AddShare(new SharesController.ShareRequest
      {
        Path = _folder,
        MediaCategories = new List<string> { "Video" }
      });

      Assert.That(Status(result), Is.EqualTo(503));
    }

    #endregion

    #region Removing

    [Test]
    public void RemovingAShareTheServerDoesNotHaveIsNotFound()
    {
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      library.Setup(l => l.GetShare(SHARE_ID)).Returns((Share) null);

      Assert.That(Status(Ask().RemoveShare(SHARE_ID)), Is.EqualTo(404));
      library.Verify(l => l.RemoveShare(It.IsAny<Guid>()), Times.Never());
    }

    [Test]
    public void RemovingAShareTellsTheLibraryAndSaysNothingBack()
    {
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      library.Setup(l => l.GetShare(SHARE_ID)).Returns(BuildShare(SHARE_ID, LOCAL_SYSTEM, _folder, "Films", "Video"));
      library.Setup(l => l.RemoveShare(SHARE_ID));

      IActionResult result = Ask().RemoveShare(SHARE_ID);

      Assert.That(Status(result), Is.EqualTo(204));
      library.Verify(l => l.RemoveShare(SHARE_ID), Times.Once());
    }

    #endregion

    #region Importing

    [Test]
    public void ImportingAShareQueuesItsWholeFolderWithItsOwnCategories()
    {
      Share share = BuildShare(SHARE_ID, LOCAL_SYSTEM, _folder, "Films", "Video");
      ResourcePath queuedPath = null;
      List<string> queuedCategories = null;
      bool queuedSubs = false;

      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      Mock<IImporterWorker> worker = GivenImporter(MockBehavior.Strict);
      GivenLocalSystem();
      GivenCategories("Video", "Audio");
      library.Setup(l => l.GetShare(SHARE_ID)).Returns(share);
      worker.Setup(w => w.ScheduleImport(It.IsAny<ResourcePath>(), It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()))
          .Callback<ResourcePath, IEnumerable<string>, bool>((path, categories, subs) =>
          {
            queuedPath = path;
            queuedCategories = categories.ToList();
            queuedSubs = subs;
          });
      worker.SetupGet(w => w.ImportJobs).Returns(new List<ImportJobInformation>());
      worker.SetupGet(w => w.IsSuspended).Returns(false);

      ActionResult<SharesController.ImportStatusResult> result = Ask().ImportShare(SHARE_ID);

      Assert.That(Status(result), Is.EqualTo(202));
      Assert.That(queuedPath, Is.EqualTo(share.BaseResourcePath));
      Assert.That(queuedCategories, Is.EquivalentTo(new[] { "Video" }));
      Assert.That(queuedSubs, Is.True);
      Assert.That((Body(result) as SharesController.ImportStatusResult).Pending, Is.EqualTo(0));
    }

    [Test]
    public void AShareWithoutCategoriesIsImportedWithEveryCategoryTheServerKnows()
    {
      Share share = BuildShare(SHARE_ID, LOCAL_SYSTEM, _folder, "Everything");
      List<string> queuedCategories = null;

      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      Mock<IImporterWorker> worker = GivenImporter(MockBehavior.Strict);
      GivenLocalSystem();
      GivenCategories("Video", "Audio");
      library.Setup(l => l.GetShare(SHARE_ID)).Returns(share);
      worker.Setup(w => w.ScheduleImport(It.IsAny<ResourcePath>(), It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()))
          .Callback<ResourcePath, IEnumerable<string>, bool>(
              (path, categories, subs) => queuedCategories = categories.ToList());
      worker.SetupGet(w => w.ImportJobs).Returns(new List<ImportJobInformation>());
      worker.SetupGet(w => w.IsSuspended).Returns(false);

      Ask().ImportShare(SHARE_ID);

      Assert.That(queuedCategories, Is.EquivalentTo(new[] { "Audio", "Video" }));
    }

    [Test]
    public void AShareWhoseCategoriesThisServerDoesNotKnowIsNotQueuedQuietly()
    {
      Share share = BuildShare(SHARE_ID, LOCAL_SYSTEM, _folder, "Films", "Movies");
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      Mock<IImporterWorker> worker = GivenImporter(MockBehavior.Loose);
      GivenLocalSystem();
      GivenCategories("Video");
      library.Setup(l => l.GetShare(SHARE_ID)).Returns(share);

      ActionResult<SharesController.ImportStatusResult> result = Ask().ImportShare(SHARE_ID);

      // The share is not the caller's mistake - it was stored by something else - so this is not a 400, but it is
      // certainly not a 202 either.
      Assert.That(Status(result), Is.EqualTo(409));
      Assert.That(ErrorOf(result), Does.Contain("'Movies'"));
      worker.Verify(w => w.ScheduleImport(It.IsAny<ResourcePath>(), It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()),
          Times.Never());
    }

    [Test]
    public void AFolderOnAnotherMachineIsNotPromisedToThisServerSImporter()
    {
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      Mock<IImporterWorker> worker = GivenImporter(MockBehavior.Loose);
      GivenLocalSystem();
      GivenCategories("Video");
      library.Setup(l => l.GetShare(SHARE_ID)).Returns(BuildShare(SHARE_ID, OTHER_SYSTEM, _folder, "Other", "Video"));

      ActionResult<SharesController.ImportStatusResult> result = Ask().ImportShare(SHARE_ID);

      Assert.That(Status(result), Is.EqualTo(409));
      Assert.That(ErrorOf(result), Does.Contain(OTHER_SYSTEM));
      worker.Verify(w => w.ScheduleImport(It.IsAny<ResourcePath>(), It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()),
          Times.Never());
    }

    [Test]
    public void ImportingAShareNobodyRegisteredIsNotFound()
    {
      Mock<IMediaLibrary> library = GivenLibrary(MockBehavior.Strict);
      Mock<IImporterWorker> worker = GivenImporter(MockBehavior.Strict);
      GivenLocalSystem();
      GivenCategories("Video");
      library.Setup(l => l.GetShare(SHARE_ID)).Returns((Share) null);

      Assert.That(Status(Ask().ImportShare(SHARE_ID)), Is.EqualTo(404));
    }

    [Test]
    public void TheImportAnswerCountsTheJobsTheWorkerStillHolds()
    {
      Mock<IImporterWorker> worker = GivenImporter(MockBehavior.Strict);
      worker.SetupGet(w => w.ImportJobs).Returns(new List<ImportJobInformation>
      {
        new ImportJobInformation(ImportJobType.Import, LocalFsResourceProviderBase.ToResourcePath(_folder),
            new List<Guid>(), true)
      });
      worker.SetupGet(w => w.IsSuspended).Returns(true);

      ActionResult<SharesController.ImportStatusResult> result = Ask().GetImportStatus();

      Assert.That(Status(result), Is.EqualTo(200));
      SharesController.ImportStatusResult status = Body(result) as SharesController.ImportStatusResult;
      Assert.That(status.Pending, Is.EqualTo(1));
      Assert.That(status.Suspended, Is.True);
      Assert.That(status.Jobs[0].Type, Is.EqualTo("Import"));
      Assert.That(status.Jobs[0].Path, Is.EqualTo(_folder));
      Assert.That(status.Jobs[0].IncludeSubDirectories, Is.True);
    }

    [Test]
    public void AnImporterWithNothingLeftToDoSaysSo()
    {
      Mock<IImporterWorker> worker = GivenImporter(MockBehavior.Strict);
      worker.SetupGet(w => w.ImportJobs).Returns(new List<ImportJobInformation>());
      worker.SetupGet(w => w.IsSuspended).Returns(false);

      SharesController.ImportStatusResult status = Body(Ask().GetImportStatus()) as SharesController.ImportStatusResult;

      // A folders screen waits for this to go to zero; a null answer instead would leave it spinning.
      Assert.That(status.Pending, Is.EqualTo(0));
      Assert.That(status.Jobs, Is.Empty);
    }

    [Test]
    public void WithoutAnImporterWorkerTheImportStateIsNotAnAnswer()
    {
      Assert.That(Status(Ask().GetImportStatus()), Is.EqualTo(503));
    }

    #endregion
  }
}
