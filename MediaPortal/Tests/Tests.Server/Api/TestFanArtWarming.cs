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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using NUnit.Framework;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Server.Api;

namespace Tests.Server.Api
{
  /// <summary>
  /// The server's memory of where the pictures of its library lie, and the pass that fills that memory after an
  /// import so that the first wall of titles costs file opens instead of searching.
  /// </summary>
  /// <remarks>
  /// <para>
  /// What is under test here is the asking rather than the answering: a picture that is remembered has to be the
  /// picture that is handed out, and - which is the whole point - getting it must not cost the library a single
  /// query. So the library is a mock that writes down every query it was sent, and most assertions below are about
  /// how long that list grew between two calls. An answer that is right but slow would pass a test that only looks
  /// at the bytes.
  /// </para>
  /// <para>
  /// The folders are real ones under the temporary directory, three levels below a folder of the test's own: the
  /// search walks up from a title's file to its grandparent, and a test would rather its walk not read whatever
  /// some other test left in the temporary directory. Files are written rather than stubbed because a remembered
  /// picture is checked by its write time and size, which is exactly the thing that has to notice a replaced file.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestFanArtWarming
  {
    private string _folder;
    private List<MediaItemQuery> _queries;

    [SetUp]
    public void SetUp()
    {
      // A pass left waiting by another fixture would hold the one pass slot for its whole patience, so the patience
      // is shortened for this fixture before anything is started - and any pass still running is waited out.
      FanArtWarmingPass.QueueLook = TimeSpan.FromMilliseconds(10);
      FanArtWarmingPass.PatienceWithTheImporter = TimeSpan.FromSeconds(2);
      LocalFanArtSearch.Worn.ForgetEverything();
      LocalFanArtSearch.Worn.Room = 40000;
      LocalFanArtSearch.Worn.GivingUpWhenFull = 4000;
      RemoveServices();

      _folder = Path.Combine(Path.GetTempPath(), "mp2-warm-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_folder);
      _queries = new List<MediaItemQuery>();
      _items = new List<MediaItem>();

      // The one pass slot has to be free before a test of the pass starts.
      Stopwatch untilIdle = Stopwatch.StartNew();
      while (untilIdle.Elapsed < TimeSpan.FromSeconds(10) && new MediaController().GetFanArtWarming().Running)
        Thread.Sleep(10);
      Assert.That(new MediaController().GetFanArtWarming().Running, Is.False,
          "a warming pass from an earlier test never ended");
    }

    [TearDown]
    public void TearDown()
    {
      RemoveServices();
      LocalFanArtSearch.Worn.ForgetEverything();
      FanArtWarmingPass.QueueLook = TimeSpan.FromSeconds(5);
      FanArtWarmingPass.PatienceWithTheImporter = TimeSpan.FromHours(3);
      if (Directory.Exists(_folder))
      {
        try
        {
          Directory.Delete(_folder, true);
        }
        catch (IOException)
        {
          // A pass that was still listing the folders when the test ended. The temporary directory is cleaned
          // with the machine, so a folder left behind costs nothing a later test.
        }
      }
    }

    private static void RemoveServices()
    {
      if (ServiceRegistration.IsRegistered<IMediaLibrary>())
        ServiceRegistration.Remove<IMediaLibrary>();
      if (ServiceRegistration.IsRegistered<IMediaAccessor>())
        ServiceRegistration.Remove<IMediaAccessor>();
      if (ServiceRegistration.IsRegistered<IImporterWorker>())
        ServiceRegistration.Remove<IImporterWorker>();
    }

    #region Fixtures

    private static readonly byte[] BACKDROP = { 1, 2, 3 };
    private static readonly byte[] OTHER_BACKDROP = { 4, 5, 6, 7, 8 };
    private static readonly byte[] POSTER = { 9, 9 };

    private List<MediaItem> _items;

    /// <summary>
    /// One title of this test: its id, the folder it lives in, and the file the library says it is. The pictures
    /// themselves are written by the test that cares which ones a title has.
    /// </summary>
    private sealed class Title
    {
      internal Guid Id;
      internal string Folder;

      internal void Write(string name, byte[] bytes)
      {
        File.WriteAllBytes(Path.Combine(Folder, name), bytes);
      }
    }

    /// <summary>
    /// A title as the importer left it, three levels below this test's folder: Films/&lt;title&gt;/&lt;title&gt;.mkv.
    /// </summary>
    private Title GiveATitle(string name, params SingleMediaItemAspectMetadata[] moreAspects)
    {
      string folder = Path.Combine(_folder, "Films", name);
      Directory.CreateDirectory(folder);
      string file = Path.Combine(folder, name + ".mkv");
      File.WriteAllBytes(file, new byte[] { 0 });

      Title title = new Title { Id = Guid.NewGuid(), Folder = folder };
      _items.Add(new LocatedItem(title.Id, name, file, moreAspects));
      return title;
    }

    /// <summary>
    /// A title whose file is whatever the test wrote, resolved by overriding the lookup rather than by running the
    /// system manager a live server would need - the same stand-in <see cref="TestMediaController"/> uses.
    /// </summary>
    private sealed class LocatedItem : MediaItem
    {
      private readonly IResourceLocator _locator;

      internal LocatedItem(Guid id, string name, string filePath, params SingleMediaItemAspectMetadata[] moreAspects)
          : base(id, Aspects(name, moreAspects))
      {
        // The local filesystem provider spells its paths "/C:/...", which is what the "../" walking reads.
        var stub = new Mock<IResourceLocator>();
        stub.Setup(get => get.NativeResourcePath)
            .Returns(ResourcePath.BuildBaseProviderPath(
                LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID,
                "/" + filePath.Replace('\\', '/')));
        _locator = stub.Object;
      }

      public override IResourceLocator GetResourceLocator()
      {
        return _locator;
      }

      private static IDictionary<Guid, IList<MediaItemAspect>> Aspects(string name,
          IEnumerable<SingleMediaItemAspectMetadata> moreAspects)
      {
        SingleMediaItemAspect media = new SingleMediaItemAspect(MediaAspect.Metadata);
        media.SetAttribute(MediaAspect.ATTR_TITLE, name);
        media.SetAttribute(MediaAspect.ATTR_SORT_TITLE, name);
        media.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, DateTime.Now);

        var aspects = new Dictionary<Guid, IList<MediaItemAspect>>
        {
            { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } }
        };

        // The aspects a picture handler serves have to be on the title for the handler to be asked about it,
        // which is the question some of the tests below are about.
        foreach (SingleMediaItemAspectMetadata metadata in moreAspects ?? new SingleMediaItemAspectMetadata[0])
          if (metadata != null && !aspects.ContainsKey(metadata.AspectId))
            aspects[metadata.AspectId] = new List<MediaItemAspect> { new SingleMediaItemAspect(metadata) };

        // The path the title is at is what the walk needs, and this fixture hands it over through the locator,
        // the way a resolved item carries it - the aspect the importer stores it in is not read here.
        return aspects;
      }
    }

    /// <summary>
    /// The library and the filesystem the server would search, with the library writing down every query it is
    /// sent. That list is what most of the tests below measure.
    /// </summary>
    private void GiveTheServerItsLibrary()
    {
      GiveTheServerItsLibrary(new Dictionary<Guid, IMediaFanArtHandler>());
    }

    /// <summary>
    /// A handler list for the one handler a test is watching.
    /// </summary>
    private static IDictionary<Guid, IMediaFanArtHandler> Handlers(IMediaFanArtHandler handler)
    {
      return new Dictionary<Guid, IMediaFanArtHandler> { { handler.Metadata.FanArtHandlerId, handler } };
    }

    /// <summary>
    /// A picture handler that writes down the titles it was asked about. It stores nothing, because what these
    /// tests are about is the asking; what a handler makes of a title is the handler's own tests.
    /// </summary>
    private sealed class RecordingFanArtHandler : BaseFanArtHandler
    {
      internal readonly List<Guid> Asked = new List<Guid>();

      internal RecordingFanArtHandler(MediaItemAspectMetadata served)
          : base(new FanArtHandlerMetadata(Guid.NewGuid(), "Recording FanArt handler"),
              new[] { served.AspectId })
      {
      }

      public override Task CollectFanArtAsync(Guid mediaItemId,
          IDictionary<Guid, IList<MediaItemAspect>> aspects)
      {
        Asked.Add(mediaItemId);
        return Task.FromResult(true);
      }
    }

    /// <summary>
    /// The library and the filesystem the server would search, with the handlers the plugins registered for
    /// getting pictures. That list is what most of the tests below measure, together with the queries.
    /// </summary>
    private void GiveTheServerItsLibrary(IDictionary<Guid, IMediaFanArtHandler> handlers)
    {
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, It.IsAny<Guid?>(), It.IsAny<bool>()))
          .Callback<MediaItemQuery, bool, Guid?, bool>((query, online, profile, virtualItems) => _queries.Add(query))
          .Returns<MediaItemQuery, bool, Guid?, bool>((query, online, profile, virtualItems) => Answer(query));
      ServiceRegistration.Set<IMediaLibrary>(library.Object);

      var providers = new Dictionary<Guid, IResourceProvider>
      {
          { LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, new LocalFsResourceProvider() }
      };
      var accessor = new Mock<IMediaAccessor>(MockBehavior.Strict);
      accessor.SetupGet(get => get.LocalResourceProviders).Returns(providers);
      accessor.SetupGet(get => get.LocalFanArtHandlers).Returns(handlers);
      ServiceRegistration.Set<IMediaAccessor>(accessor.Object);
    }

    /// <summary>
    /// What a query of the library is answered with: the titles its filter names, cut to the page it asked for, so
    /// that a walk of the library and a lookup of one title both get an answer worth acting on.
    /// </summary>
    private IList<MediaItem> Answer(MediaItemQuery query)
    {
      MediaItemIdFilter byId = query.Filter as MediaItemIdFilter;
      IEnumerable<MediaItem> matching = byId == null
          ? _items
          : _items.Where(item => byId.MediaItemIds.Contains(item.MediaItemId));

      int skip = (int) (query.Offset ?? 0);
      int take = query.Limit.HasValue && query.Limit.Value > 0 ? (int) query.Limit.Value : int.MaxValue;
      return matching.Skip(skip).Take(take).ToList();
    }

    /// <summary>
    /// Starting a pass the way a client does, and waiting for it to be through. The pass runs on a background
    /// thread by design, so a test has to wait for it rather than assume it is done.
    /// </summary>
    private static FanArtWarmingResult StartAPassAndReadItsEnd()
    {
      ActionResult<FanArtWarmingResult> action = new MediaController().WarmFanArt();
      Assert.That(StatusOf(action), Is.EqualTo(202),
          "a pass that this call started is reported as accepted work, not as something already done");

      Stopwatch watch = Stopwatch.StartNew();
      while (watch.Elapsed < TimeSpan.FromSeconds(10))
      {
        FanArtWarmingResult said = new MediaController().GetFanArtWarming();
        if (!said.Running)
          return said;
        Thread.Sleep(10);
      }

      Assert.Fail("the warming pass was still running after ten seconds");
      return null;
    }

    private static int StatusOf(ActionResult<FanArtWarmingResult> action)
    {
      return (action.Result as IStatusCodeActionResult)?.StatusCode ?? 200;
    }

    #endregion

    #region The memory

    [Test]
    public void TheSecondQuestionAboutAPictureAsksTheLibraryNothing()
    {
      Title film = GiveATitle("Shogun");
      film.Write("fanart.jpg", BACKDROP);
      GiveTheServerItsLibrary();

      Assert.That(LocalFanArtSearch.Worn.Picture(film.Id, FanArtTypes.FanArt), Is.EqualTo(BACKDROP),
          "the backdrop lying next to the file answers the question");
      int asked = _queries.Count;
      Assert.That(asked, Is.GreaterThan(0), "the first question has to look into the folders");

      Assert.That(LocalFanArtSearch.Worn.Picture(film.Id, FanArtTypes.FanArt), Is.EqualTo(BACKDROP));
      Assert.That(_queries.Count, Is.EqualTo(asked),
          "the second question about the same picture is answered out of memory, without asking the library");
    }

    [Test]
    public void TheKindsFoundWhileLookingForAnotherOneAreKept()
    {
      Title film = GiveATitle("Dune");
      film.Write("poster.jpg", POSTER);
      GiveTheServerItsLibrary();

      // A wall asks for backdrops; the poster in the same folder is found on the way and is worth keeping.
      Assert.That(LocalFanArtSearch.Worn.Picture(film.Id, FanArtTypes.FanArt), Is.Null);
      int asked = _queries.Count;

      Assert.That(LocalFanArtSearch.Worn.Picture(film.Id, FanArtTypes.Poster), Is.EqualTo(POSTER),
          "the poster that lay in the folder the backdrop was looked for in is not looked for again");
      Assert.That(_queries.Count, Is.EqualTo(asked), "and answering it from memory costs the library nothing");
    }

    [Test]
    public void AReplacedPictureIsNoticeAtTheNextQuestion()
    {
      Title film = GiveATitle("Fallout");
      film.Write("fanart.jpg", BACKDROP);
      GiveTheServerItsLibrary();

      Assert.That(LocalFanArtSearch.Worn.Picture(film.Id, FanArtTypes.FanArt), Is.EqualTo(BACKDROP));

      // What TMM rewrites in place has to be seen without restarting the server, or a viewer keeps looking at the
      // old artwork until something else goes wrong.
      film.Write("fanart.jpg", OTHER_BACKDROP);

      Assert.That(LocalFanArtSearch.Worn.Picture(film.Id, FanArtTypes.FanArt), Is.EqualTo(OTHER_BACKDROP),
          "a picture that changed under the memory is handed out as it is now");
    }

    [Test]
    public void APictureThatWasRenamedIsFoundAgain()
    {
      Title film = GiveATitle("Silo");
      film.Write("fanart.jpg", BACKDROP);
      GiveTheServerItsLibrary();

      Assert.That(LocalFanArtSearch.Worn.Picture(film.Id, FanArtTypes.FanArt), Is.EqualTo(BACKDROP));

      File.Delete(Path.Combine(film.Folder, "fanart.jpg"));
      film.Write("fanart-2.jpg", OTHER_BACKDROP);

      Assert.That(LocalFanArtSearch.Worn.Picture(film.Id, FanArtTypes.FanArt), Is.EqualTo(OTHER_BACKDROP),
          "a remembered file that went away sends the next question back to the folders instead of to nothing");
    }

    [Test]
    public void TheMemoryGivesUpItsGuessesBeforeItGivesUpAPicture()
    {
      LocalFanArtSearch.Worn.Room = 3;
      LocalFanArtSearch.Worn.GivingUpWhenFull = 1;

      Title one = GiveATitle("Alien");
      one.Write("fanart.jpg", BACKDROP);
      Title two = GiveATitle("Blade Runner");
      two.Write("fanart.jpg", BACKDROP);
      GiveTheServerItsLibrary();

      LocalFanArtSearch.Worn.Picture(one.Id, FanArtTypes.FanArt);
      LocalFanArtSearch.Worn.Picture(two.Id, FanArtTypes.FanArt);

      Assert.That(LocalFanArtSearch.Worn.RememberedCount, Is.EqualTo(3),
          "fourteen answers over a memory that holds three are trimmed down to three");

      int asked = _queries.Count;
      Assert.That(LocalFanArtSearch.Worn.Picture(one.Id, FanArtTypes.FanArt), Is.EqualTo(BACKDROP));
      Assert.That(LocalFanArtSearch.Worn.Picture(two.Id, FanArtTypes.FanArt), Is.EqualTo(BACKDROP));
      Assert.That(_queries.Count, Is.EqualTo(asked),
          "the pictures a viewer would ask for are the last thing this memory gives up, not the first");

      // The guesses about kinds that are not there are what left, and losing one costs a folder listing.
      Assert.That(LocalFanArtSearch.Worn.Picture(one.Id, FanArtTypes.Poster), Is.Null);
      Assert.That(_queries.Count, Is.GreaterThan(asked),
          "what was given up is looked for again rather than answered with nothing");
    }

    #endregion

    #region The pass

    [Test]
    public void APassLearsWhereThePicturesOfTheWholeLibraryLie()
    {
      Title one = GiveATitle("House of the Dragon");
      one.Write("fanart.jpg", BACKDROP);
      Title two = GiveATitle("The Last of Us");
      two.Write("poster.jpg", POSTER);
      GiveTheServerItsLibrary();

      FanArtWarmingResult said = StartAPassAndReadItsEnd();

      Assert.That(said.Stopped, Is.Null, "nothing interrupted the pass: " + said.Stopped);
      Assert.That(said.Titles, Is.EqualTo(2), "every title of the library was looked over");
      Assert.That(said.KindsFound, Is.EqualTo(2), "the backdrop of one title and the poster of the other were learned");
      Assert.That(said.Answers, Is.EqualTo(14), "all seven kinds were answered for both titles, found or not");
      Assert.That(said.Ended, Is.Not.Null, "an ended pass says when it ended, so a screen can tell a stale answer");

      // The point of the pass: the wall that comes after it costs the library nothing at all.
      int asked = _queries.Count;
      Assert.That(LocalFanArtSearch.Worn.Picture(one.Id, FanArtTypes.FanArt), Is.EqualTo(BACKDROP));
      Assert.That(LocalFanArtSearch.Worn.Picture(two.Id, FanArtTypes.Poster), Is.EqualTo(POSTER));
      Assert.That(_queries.Count, Is.EqualTo(asked), "the warmed wall asks the library nothing");
    }

    [Test]
    public void ASecondPassOverAnUnchangedLibraryCostsOneDatabaseLookup()
    {
      Title one = GiveATitle("Shogun");
      one.Write("fanart.jpg", BACKDROP);
      Title two = GiveATitle("Fallout");
      two.Write("fanart.jpg", BACKDROP);
      GiveTheServerItsLibrary();

      StartAPassAndReadItsEnd();

      int asked = _queries.Count;
      FanArtWarmingResult said = StartAPassAndReadItsEnd();

      Assert.That(said.Skipped, Is.EqualTo(2),
          "a title whose seven kinds the memory already holds is not walked again");
      Assert.That(said.KindsFound, Is.EqualTo(0), "so a pass over an unchanged library learns nothing new");
      Assert.That(_queries.Count - asked, Is.EqualTo(1),
          "what it does cost is the one lookup that walks the titles, and no folder listings");
    }

    [Test]
    public void AHandlerThatServesATitleIsAskedAboutItDuringAPass()
    {
      Title track = GiveATitle("Test Track", AudioAspect.Metadata);
      RecordingFanArtHandler handler = new RecordingFanArtHandler(AudioAspect.Metadata);
      GiveTheServerItsLibrary(Handlers(handler));

      StartAPassAndReadItsEnd();

      Assert.That(handler.Asked, Is.EquivalentTo(new[] { track.Id }),
          "a title is handed to the handler that declared one of its aspects, because only the handler knows "
          + "that a track carries its cover inside itself");
    }

    [Test]
    public void AHandlerIsNotAskedAboutTitlesItHasNothingToSayAbout()
    {
      GiveATitle("House of the Dragon");
      RecordingFanArtHandler handler = new RecordingFanArtHandler(AudioAspect.Metadata);
      GiveTheServerItsLibrary(Handlers(handler));

      StartAPassAndReadItsEnd();

      Assert.That(handler.Asked, Is.Empty,
          "a film is not asked of the handler that serves audio, and the pass does not pretend otherwise");
    }

    [Test]
    public void TheWalkOfTheLibraryCarriesWhatThePictureHandlersNeed()
    {
      GiveATitle("Test Track", AudioAspect.Metadata);
      RecordingFanArtHandler handler = new RecordingFanArtHandler(AudioAspect.Metadata);
      GiveTheServerItsLibrary(Handlers(handler));

      StartAPassAndReadItsEnd();

      MediaItemQuery walked = _queries.Single(query => query.Filter == null);
      Assert.That(walked.OptionalRequestedMIATypeIDs, Contains.Item(AudioAspect.ASPECT_ID),
          "a handler is chosen by the aspects a title carries, so the walk has to fetch the aspects handlers serve");
      Assert.That(walked.OptionalRequestedMIATypeIDs, Contains.Item(RelationshipAspect.ASPECT_ID),
          "and it has to fetch the relationships, because the album a track's cover lies with is named by one");
    }

    [Test]
    public void APassWaitsForTheImporterAndThenWalksTheLibrary()
    {
      FanArtWarmingPass.PatienceWithTheImporter = TimeSpan.FromSeconds(20);

      var jobs = new List<ImportJobInformation>
      {
          new ImportJobInformation(ImportJobType.Import, LocalFsResourceProviderBase.ToResourcePath(_folder),
              new List<Guid>(), true)
      };
      var worker = new Mock<IImporterWorker>(MockBehavior.Strict);
      worker.SetupGet(get => get.ImportJobs).Returns(jobs);
      ServiceRegistration.Set<IImporterWorker>(worker.Object);

      Title film = GiveATitle("Silo");
      film.Write("fanart.jpg", BACKDROP);
      GiveTheServerItsLibrary();

      new MediaController().WarmFanArt();

      FanArtWarmingResult waiting = WaitUntil(said => said.Waiting != null && said.Waiting.Contains("1 job"));
      Assert.That(waiting.Titles, Is.EqualTo(0));
      Assert.That(_queries.Count, Is.EqualTo(0),
          "a pass that is waiting for the importer lists no folders and asks no questions");

      jobs.Clear();
      FanArtWarmingResult done = WaitUntil(said => !said.Running);

      Assert.That(done.Stopped, Is.Null, "waiting for the importer is not giving up on it: " + done.Stopped);
      Assert.That(done.Titles, Is.EqualTo(1), "once the importer is through, the library is walked");
      Assert.That(LocalFanArtSearch.Worn.Picture(film.Id, FanArtTypes.FanArt), Is.EqualTo(BACKDROP));
    }

    [Test]
    public void APassThatWaitedTooLongSaysSoInsteadOfPretending()
    {
      FanArtWarmingPass.PatienceWithTheImporter = TimeSpan.FromMilliseconds(50);

      var worker = new Mock<IImporterWorker>(MockBehavior.Strict);
      worker.SetupGet(get => get.ImportJobs).Returns(new List<ImportJobInformation>
      {
          new ImportJobInformation(ImportJobType.Import, LocalFsResourceProviderBase.ToResourcePath(_folder),
              new List<Guid>(), true)
      });
      ServiceRegistration.Set<IImporterWorker>(worker.Object);

      Title film = GiveATitle("Shogun");
      film.Write("fanart.jpg", BACKDROP);
      GiveTheServerItsLibrary();

      FanArtWarmingResult said = WaitUntil(state => !state.Running, StartAPass);

      Assert.That(said.Stopped, Is.EqualTo("gave up waiting for the importer"),
          "an import that never ends is reported, not silently walked around");
      Assert.That(said.Titles, Is.EqualTo(0), "and no folder was listed while waiting");
      Assert.That(LocalFanArtSearch.Worn.Picture(film.Id, FanArtTypes.FanArt), Is.EqualTo(BACKDROP),
          "the picture is still found when somebody asks for it; the pass only saves that asking");
    }

    [Test]
    public void ThePassThatIsAlreadyRunningIsTheOneACallerWaitsFor()
    {
      var slow = new Mock<IMediaLibrary>(MockBehavior.Strict);
      slow.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, It.IsAny<Guid?>(), It.IsAny<bool>()))
          .Callback(() => Thread.Sleep(300))
          .Returns(new List<MediaItem>());
      ServiceRegistration.Set<IMediaLibrary>(slow.Object);
      GiveTheServerItsFileSystem();

      ActionResult<FanArtWarmingResult> first = new MediaController().WarmFanArt();
      ActionResult<FanArtWarmingResult> second = new MediaController().WarmFanArt();

      Assert.That(StatusOf(first), Is.EqualTo(202), "the call that started a pass is told so");
      Assert.That(StatusOf(second), Is.EqualTo(200),
          "a second call while it runs is not a failure, and is not a second pass either");
      Assert.That(new MediaController().GetFanArtWarming().Running, Is.True);

      WaitUntil(said => !said.Running);
    }

    #endregion

    #region Waiting

    private static void StartAPass()
    {
      new MediaController().WarmFanArt();
    }

    /// <summary>
    /// Reading the pass' state until it says what the test is waiting for. A pass runs on a background thread on
    /// purpose, so this is the only way to be sure a test is looking at a settled answer.
    /// </summary>
    private static FanArtWarmingResult WaitUntil(Func<FanArtWarmingResult, bool> settled,
        Action start = null)
    {
      start?.Invoke();

      Stopwatch watch = Stopwatch.StartNew();
      FanArtWarmingResult said = new MediaController().GetFanArtWarming();
      while (watch.Elapsed < TimeSpan.FromSeconds(10))
      {
        said = new MediaController().GetFanArtWarming();
        if (settled(said))
          return said;
        Thread.Sleep(10);
      }

      Assert.Fail("the warming pass did not get to the state the test waited for; it says: " + said.Stopped
          + " / " + said.Waiting);
      return null;
    }

    private void GiveTheServerItsFileSystem()
    {
      var providers = new Dictionary<Guid, IResourceProvider>
      {
          { LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, new LocalFsResourceProvider() }
      };
      var accessor = new Mock<IMediaAccessor>(MockBehavior.Strict);
      accessor.SetupGet(get => get.LocalResourceProviders).Returns(providers);
      ServiceRegistration.Set<IMediaAccessor>(accessor.Object);
    }

    #endregion
  }
}
