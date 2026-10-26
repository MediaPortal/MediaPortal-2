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
using System.Globalization;
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
    private static readonly byte[] SERIES_POSTER = { 10, 10 };
    private static readonly byte[] SEASON_POSTER = { 11, 11 };
    private static readonly byte[] FOLDER_POSTER = { 12, 12 };

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
    /// An episode filed the way a household that keeps one folder per season files it:
    /// Series/&lt;series&gt;/Season NN/&lt;episode&gt;.mkv. The number goes on the episode's aspect because that is where the
    /// importer writes it, and it is the number the season's picture names carry.
    /// </summary>
    /// <remarks>
    /// The episode is added to the library before the season a test will ask about (
    /// <see cref="GiveASeasonWithoutAFile"/>), because the fixture answers a relationship query with the titles it
    /// holds in order and the picture search takes the first episode it is given.
    /// </remarks>
    private Title GiveAnEpisodeInASeasonFolder(string seriesName, int seasonNumber, string episodeName)
    {
      string folder = Path.Combine(_folder, "Series", seriesName,
          "Season " + seasonNumber.ToString("00", CultureInfo.InvariantCulture));
      return GiveAnEpisodeIn(folder, seriesName, seasonNumber, episodeName);
    }

    /// <summary>
    /// An episode filed the way a household that does not keep season folders files it: the episode's own folder
    /// directly inside the series' one, which makes that folder both the episode's folder and the series' folder.
    /// </summary>
    private Title GiveAnEpisodeInASeriesFolder(string seriesName, int seasonNumber, string episodeName)
    {
      return GiveAnEpisodeIn(Path.Combine(_folder, "Series", seriesName), seriesName, seasonNumber, episodeName);
    }

    private Title GiveAnEpisodeIn(string folder, string seriesName, int seasonNumber, string episodeName)
    {
      Directory.CreateDirectory(folder);
      string file = Path.Combine(folder, episodeName + ".mkv");
      File.WriteAllBytes(file, new byte[] { 0 });

      SingleMediaItemAspect episode = new SingleMediaItemAspect(EpisodeAspect.Metadata);
      episode.SetAttribute(EpisodeAspect.ATTR_SERIES_NAME, seriesName);
      episode.SetAttribute(EpisodeAspect.ATTR_SEASON, seasonNumber);
      episode.SetCollectionAttribute(EpisodeAspect.ATTR_EPISODE, new[] { 1 });

      Title title = new Title { Id = Guid.NewGuid(), Folder = folder };
      _items.Add(LocatedItem.Numbered(title.Id, episodeName, file, episode));
      return title;
    }

    /// <summary>
    /// The season above the episodes a test filed: a title with a number and no file of its own, which is exactly
    /// what MP2's importer leaves behind and exactly what makes the picture search take its detour through one of
    /// the episodes.
    /// </summary>
    /// <returns>The id the title is asked about by.</returns>
    private Guid GiveASeasonWithoutAFile(string name, int seasonNumber)
    {
      SingleMediaItemAspect media = new SingleMediaItemAspect(MediaAspect.Metadata);
      media.SetAttribute(MediaAspect.ATTR_TITLE, name);
      media.SetAttribute(MediaAspect.ATTR_SORT_TITLE, name);

      SingleMediaItemAspect season = new SingleMediaItemAspect(SeasonAspect.Metadata);
      season.SetAttribute(SeasonAspect.ATTR_SERIES_NAME, name);
      season.SetAttribute(SeasonAspect.ATTR_SEASON, seasonNumber);

      Guid id = Guid.NewGuid();
      _items.Add(new FilelessItem(id, new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } },
          { SeasonAspect.ASPECT_ID, new List<MediaItemAspect> { season } }
      }));
      return id;
    }

    /// <summary>
    /// The series above a shelf of episodes: a title with a name and no file of its own. This is what the picture
    /// search is pointed at when a season exists in the library as nothing but a number written on its episodes -
    /// there is no season title to ask, so the series is asked and the number comes along with the question.
    /// </summary>
    /// <returns>The id the series is asked about by.</returns>
    private Guid GiveASeriesWithoutAFile(string name)
    {
      SingleMediaItemAspect media = new SingleMediaItemAspect(MediaAspect.Metadata);
      media.SetAttribute(MediaAspect.ATTR_TITLE, name);
      media.SetAttribute(MediaAspect.ATTR_SORT_TITLE, name);

      Guid id = Guid.NewGuid();
      _items.Add(new FilelessItem(id, new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } },
          { SeriesAspect.ASPECT_ID, new List<MediaItemAspect> { new SingleMediaItemAspect(SeriesAspect.Metadata) } }
      }));
      return id;
    }

    /// <summary>
    /// A title the library holds without a file: a series, a season, an album. Their pictures lie with the things
    /// inside them, which is why the search climbs from an episode rather than from the title itself.
    /// </summary>
    private sealed class FilelessItem : MediaItem
    {
      internal FilelessItem(Guid id, IDictionary<Guid, IList<MediaItemAspect>> aspects)
          : base(id, aspects)
      {
      }

      public override IResourceLocator GetResourceLocator()
      {
        return null;
      }
    }

    /// <summary>
    /// A title whose file is whatever the test wrote, resolved by overriding the lookup rather than by running the
    /// system manager a live server would need - the same stand-in <see cref="TestMediaController"/> uses.
    /// </summary>
    private sealed class LocatedItem : MediaItem
    {
      private readonly IResourceLocator _locator;

      internal LocatedItem(Guid id, string name, string filePath, params SingleMediaItemAspectMetadata[] moreAspects)
          : this(id, name, filePath, (moreAspects ?? new SingleMediaItemAspectMetadata[0])
              .Where(metadata => metadata != null)
              .Select(metadata => new SingleMediaItemAspect(metadata)))
      {
      }

      /// <summary>
      /// A title whose aspects the test wrote out itself. The other constructor can only say <i>which</i> aspects
      /// a title carries, and some titles are their values: a season is the number standing on its season aspect,
      /// and an empty aspect cannot make one.
      /// </summary>
      internal static LocatedItem Numbered(Guid id, string name, string filePath,
          params SingleMediaItemAspect[] filled)
      {
        return new LocatedItem(id, name, filePath, (IEnumerable<SingleMediaItemAspect>) filled);
      }

      private LocatedItem(Guid id, string name, string filePath, IEnumerable<SingleMediaItemAspect> filled)
          : base(id, Aspects(name, filled))
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
          IEnumerable<SingleMediaItemAspect> moreAspects)
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
        foreach (SingleMediaItemAspect aspect in moreAspects ?? new SingleMediaItemAspect[0])
          if (aspect != null && !aspects.ContainsKey(aspect.Metadata.AspectId))
            aspects[aspect.Metadata.AspectId] = new List<MediaItemAspect> { aspect };

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

      // The one condition a picture search puts on top of "which titles lie under which" is a season number, and the
      // fake has to honour it. A test that asks for season two and is handed season one's episode to start from would
      // find its answer two folders up in the series' folder and pass without ever having looked inside the season it
      // was meant to be looking inside.
      int? numbered = SeasonAskedFor(query.Filter);
      if (numbered.HasValue)
        matching = matching.Where(item => NumberOf(item) == numbered.Value);

      int skip = (int) (query.Offset ?? 0);
      int take = query.Limit.HasValue && query.Limit.Value > 0 ? (int) query.Limit.Value : int.MaxValue;
      return matching.Skip(skip).Take(take).ToList();
    }

    /// <summary>
    /// The season number a query narrows its titles to, or <c>null</c> when it names none. The picture search adds
    /// exactly one such condition, beside the link that says whose episodes these are.
    /// </summary>
    private static int? SeasonAskedFor(IFilter filter)
    {
      RelationalFilter condition = ConditionOn(filter, EpisodeAspect.ATTR_SEASON);
      return condition != null && condition.Operator == RelationalOperator.EQ
          ? condition.FilterValue as int?
          : (int?) null;
    }

    private static RelationalFilter ConditionOn(IFilter filter,
        MediaItemAspectMetadata.AttributeSpecification attribute)
    {
      RelationalFilter one = filter as RelationalFilter;
      if (one != null)
        return ReferenceEquals(one.AttributeType, attribute) ? one : null;

      BooleanCombinationFilter combination = filter as BooleanCombinationFilter;
      if (combination == null)
        return null;

      foreach (IFilter operand in combination.Operands)
      {
        RelationalFilter found = ConditionOn(operand, attribute);
        if (found != null)
          return found;
      }
      return null;
    }

    /// <summary>The season number one of this library's episodes wears, or <c>-1</c> for a title that is none.</summary>
    private static int NumberOf(MediaItem item)
    {
      SingleMediaItemAspect episode;
      return MediaItemAspect.TryGetAspect(item.Aspects, EpisodeAspect.Metadata, out episode)
          ? episode.GetAttributeValue<int>(EpisodeAspect.ATTR_SEASON)
          : -1;
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

    #region The names a season's pictures carry

    [Test]
    public void ASeasonStandingInItsOwnFolderIsAskedAboutByItsOwnNumber()
    {
      Title episode = GiveAnEpisodeInASeasonFolder("Bones", 3, "The Salt in the Water");
      File.WriteAllBytes(Path.Combine(episode.Folder, "season03-poster.jpg"), SEASON_POSTER);
      // The series' own poster two folders up, which is the picture a season must never be handed out as its own:
      // it shows the whole show, and a shelf of seasons where every row wears the same poster says nothing about
      // which season is which.
      File.WriteAllBytes(Path.Combine(_folder, "Series", "Bones", "poster.jpg"), SERIES_POSTER);
      GiveTheServerItsLibrary();

      Guid season = GiveASeasonWithoutAFile("Bones", 3);

      // The season has no file, so the walk starts at one of its episodes: the first folder it reaches is the
      // season's own ("Season 03"), and that folder's numbered picture is the season's poster.
      Assert.That(LocalFanArtSearch.Worn.Picture(season, FanArtTypes.Poster), Is.EqualTo(SEASON_POSTER),
          "fanart.tv and the tagging tools that read it file a whole series in one place by naming a season's "
          + "pictures after its number - season03-poster.jpg (LocalSeriesFanartProvider.cs:119-136 through "
          + "LocalFanartHelper.GetAdditionalSeasonPrefixes) - and a season that was never asked about by its "
          + "number has one poster for every season in the house");

      // A tagging tool that writes the plain name inside the season's own folder means the season, and the
      // reference reads the plain names of that folder before the numbered ones.
      File.WriteAllBytes(Path.Combine(episode.Folder, "poster.jpg"), FOLDER_POSTER);
      LocalFanArtSearch.Worn.ForgetEverything();
      Assert.That(LocalFanArtSearch.Worn.Picture(season, FanArtTypes.Poster), Is.EqualTo(FOLDER_POSTER),
          "a plain poster standing in the season's own folder is the season's, and outranks the numbered one");
    }

    [Test]
    public void ASeasonOfAFileLibraryThatKeepsNoSeasonFoldersIsNotGivenTheSeriesPoster()
    {
      // The other layout the reference distinguishes: no "Season NN" folders at all, so the folder an episode
      // lies in is the series' folder and its plain "poster.jpg" belongs to the whole show. The reference
      // searches nothing but the numbered names there, and so does this.
      Title episode = GiveAnEpisodeInASeriesFolder("Bones", 2, "The Titan on the Ice");
      File.WriteAllBytes(Path.Combine(episode.Folder, "poster.jpg"), SERIES_POSTER);
      File.WriteAllBytes(Path.Combine(episode.Folder, "season02-poster.jpg"), SEASON_POSTER);
      GiveTheServerItsLibrary();

      Guid season = GiveASeasonWithoutAFile("Bones", 2);

      Assert.That(LocalFanArtSearch.Worn.Picture(season, FanArtTypes.Poster), Is.EqualTo(SEASON_POSTER),
          "the numbered picture is the season's, whatever else lies in the folder");

      LocalFanArtSearch.Worn.ForgetEverything();
      Assert.That(LocalFanArtSearch.Worn.Picture(episode.Id, FanArtTypes.Poster), Is.EqualTo(SERIES_POSTER),
          "and the episode still wears the show's poster, which is what an episode without a season folder has "
          + "always worn - distinguishing the two is not a trade that costs the episode its picture");
    }

    [Test]
    public void TheSpecialsAreAskedAboutAsTheirOwnSeason()
    {
      // MP2 spells the specials season nought (SeasonInfo), and the reference answers that number with
      // "season-specials" rather than "season-all" (LocalFanartHelper.GetAdditionalSeasonPrefixes:245), because
      // the picture that belongs to every season is not the picture of the extras.
      Title episode = GiveAnEpisodeInASeasonFolder("Bones", 0, "The Dark in the Light");
      File.WriteAllBytes(Path.Combine(episode.Folder, "season-specials-poster.jpg"), SEASON_POSTER);
      GiveTheServerItsLibrary();

      Guid specials = GiveASeasonWithoutAFile("Bones", 0);

      Assert.That(LocalFanArtSearch.Worn.Picture(specials, FanArtTypes.Poster), Is.EqualTo(SEASON_POSTER),
          "a season zero is the specials, and it is the one season whose pictures are not numbered with its own "
          + "number");
    }

    [Test]
    public void APictureForEverySeasonIsThePictureOfEverySeason()
    {
      // "season-all-*" is what a tagger writes when it has one picture for the whole show's seasons; the
      // reference takes it for whichever season has nothing numbered of its own
      // (LocalFanartHelper.GetAdditionalSeasonPrefixes:245).
      Title episode = GiveAnEpisodeInASeasonFolder("Bones", 4, "The Brain in the Jar");
      File.WriteAllBytes(Path.Combine(episode.Folder, "season-all-poster.jpg"), SEASON_POSTER);
      GiveTheServerItsLibrary();

      Guid fourth = GiveASeasonWithoutAFile("Bones", 4);
      Assert.That(LocalFanArtSearch.Worn.Picture(fourth, FanArtTypes.Poster), Is.EqualTo(SEASON_POSTER));

      LocalFanArtSearch.Worn.ForgetEverything();
      Guid fifth = GiveASeasonWithoutAFile("Bones", 5);
      Assert.That(LocalFanArtSearch.Worn.Picture(fifth, FanArtTypes.Poster), Is.EqualTo(SEASON_POSTER),
          "a season with nothing numbered of its own wears the show's shared season picture rather than nothing");
    }

    [Test]
    public void ASeasonTheLibraryOnlyCountsIsAskedOfItsSeriesByNumber()
    {
      // What a tagger that reads fanart.tv writes, and what the household's own library looks like: every season's
      // pictures in the series' folder, named after the season, lying next to the show's plain "poster.jpg".
      Title first = GiveAnEpisodeInASeasonFolder("Prison Break", 1, "Buried");
      GiveAnEpisodeInASeasonFolder("Prison Break", 2, "Reconvene");
      Guid series = GiveASeriesWithoutAFile("Prison Break");

      string seriesFolder = Path.Combine(_folder, "Series", "Prison Break");
      File.WriteAllBytes(Path.Combine(seriesFolder, "poster.jpg"), SERIES_POSTER);
      File.WriteAllBytes(Path.Combine(seriesFolder, "season01-poster.jpg"), POSTER);
      File.WriteAllBytes(Path.Combine(seriesFolder, "season02-poster.jpg"), SEASON_POSTER);
      GiveTheServerItsLibrary();

      Assert.That(LocalFanArtSearch.Worn.Picture(series, FanArtTypes.Poster, 2), Is.EqualTo(SEASON_POSTER),
          "a season the library holds as nothing but a number on its episodes is asked about by that number, of the "
          + "series' own id - and the plain poster lying in the same folder as the numbered one is not the answer, "
          + "because it shows the whole show and would paint every row of a shelf of seasons the same");

      Assert.That(LocalFanArtSearch.Worn.Picture(series, FanArtTypes.Poster), Is.EqualTo(SERIES_POSTER),
          "the series itself still wears its own poster; asking it without a number is the question it always was");

      Assert.That(LocalFanArtSearch.Worn.Picture(first.Id, FanArtTypes.Poster), Is.EqualTo(SERIES_POSTER),
          "and an episode of that series wears it too, which a season's own numbering must not reach back and take "
          + "away from it");
    }

    [Test]
    public void ASeasonAskedByNumberStartsItsWalkInThatSeasonsFolder()
    {
      // The two seasons are numbered the only way the fixture can tell them apart to the search: each season's own
      // folder carries that season's numbered picture, which is where a library with one folder per season keeps them.
      GiveAnEpisodeInASeasonFolder("Prison Break", 1, "Buried").Write("season01-poster.jpg", POSTER);
      GiveAnEpisodeInASeasonFolder("Prison Break", 2, "Reconvene").Write("season02-poster.jpg", SEASON_POSTER);
      Guid series = GiveASeriesWithoutAFile("Prison Break");
      GiveTheServerItsLibrary();

      Assert.That(LocalFanArtSearch.Worn.Picture(series, FanArtTypes.Poster, 2), Is.EqualTo(SEASON_POSTER),
          "the walk starts from an episode of the season that was asked for; taken from whichever episode the "
          + "library happens to hold first, every season of the series would be answered from season one's folder "
          + "and every row of the shelf would wear season one's poster");
    }

    [Test]
    public void TheSpecialsOfACountedSeriesAnswerToTheirOwnName()
    {
      GiveAnEpisodeInASeasonFolder("Prison Break", 0, "The Road to Paradise");
      GiveAnEpisodeInASeasonFolder("Prison Break", 1, "Buried");
      Guid series = GiveASeriesWithoutAFile("Prison Break");

      string seriesFolder = Path.Combine(_folder, "Series", "Prison Break");
      File.WriteAllBytes(Path.Combine(seriesFolder, "season-specials-poster.jpg"), SEASON_POSTER);
      File.WriteAllBytes(Path.Combine(seriesFolder, "season01-poster.jpg"), POSTER);
      GiveTheServerItsLibrary();

      Assert.That(LocalFanArtSearch.Worn.Picture(series, FanArtTypes.Poster, 0), Is.EqualTo(SEASON_POSTER),
          "nought is asked about as the specials, which is MP2's spelling of it and the name the taggers write - "
          + "a season nought looked up as season00-* finds nothing anywhere, because nothing is named that");
      Assert.That(LocalFanArtSearch.Worn.Picture(series, FanArtTypes.Poster, 1), Is.EqualTo(POSTER),
          "and the number does not blur: the season beside the specials is still asked about by its own number");
    }

    [Test]
    public void ACountedSeasonOfALibraryWithoutSeasonFoldersIsFoundBesideTheShow()
    {
      // The other layout, counted rather than held: the episode lies directly in the series' folder, so that folder
      // is the series' own and its plain names belong to the show.
      GiveAnEpisodeInASeriesFolder("Prison Break", 2, "Reconvene");
      Guid series = GiveASeriesWithoutAFile("Prison Break");

      string seriesFolder = Path.Combine(_folder, "Series", "Prison Break");
      File.WriteAllBytes(Path.Combine(seriesFolder, "poster.jpg"), SERIES_POSTER);
      File.WriteAllBytes(Path.Combine(seriesFolder, "season02-poster.jpg"), SEASON_POSTER);
      GiveTheServerItsLibrary();

      Assert.That(LocalFanArtSearch.Worn.Picture(series, FanArtTypes.Poster, 2), Is.EqualTo(SEASON_POSTER),
          "asked by number in a flat library, the season is answered by the numbered picture lying with the media, "
          + "which is the only picture in that folder that means one season");
      Assert.That(LocalFanArtSearch.Worn.Picture(series, FanArtTypes.Poster), Is.EqualTo(SERIES_POSTER),
          "the show's own poster stays the show's");
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
