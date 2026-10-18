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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using NUnit.Framework;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Server.Api;
using MediaPortal.Utilities.Exceptions;

namespace Tests.Server.Api
{
  /// <summary>
  /// The titles endpoint: what a media menu shows, and what it needs to be able to play what it shows.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The library is mocked strictly, so an endpoint that reached for a member of <see cref="IMediaLibrary"/> it
  /// has no business reaching for fails here instead of running an unexpected query against the database of
  /// whoever's machine the tests run on. The aspects are the real <see cref="MediaAspect"/> and
  /// <see cref="MovieAspect"/> objects rather than invented ones, for two reasons: the mapping from a real
  /// attribute specification to a row is the thing under test, and a made-up aspect would let the endpoint pass
  /// with metadata that no importer ever produces.
  /// </para>
  /// <para>
  /// What the endpoint asks the library for is tested as carefully as what it answers, because the asking is where
  /// a media menu goes wrong silently: ask for virtual items and a viewer gets folders mixed in with films, ask
  /// without a limit and the first page reads a whole library, drop the sort and the order depends on the
  /// database's mood. Those are invisible in a screenshot of ten items and visible here.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestMediaController
  {
    #region Fixtures

    private static readonly Guid TEST_ASPECT_ID = new Guid("33333333-3333-3333-3333-333333333333");
    private static readonly Guid RESOURCE_SYSTEM_ID = new Guid("44444444-4444-4444-4444-444444444444");
    // The serialized form a client has to understand: the provider id in braces, then ://, then the path
    // the provider resolves it to. Written out rather than round-tripped through Serialize(), so that a
    // change of format breaks this test instead of quietly breaking every client that stored a path.
    private const string RESOURCE_PATH = "{44444444-4444-4444-4444-444444444444}://Films/Movie.mkv";

    [SetUp]
    public void SetUp()
    {
      RemoveMediaLibrary();
    }

    [TearDown]
    public void TearDown()
    {
      RemoveMediaLibrary();
    }

    private static void RemoveMediaLibrary()
    {
      if (ServiceRegistration.IsRegistered<IMediaLibrary>())
        ServiceRegistration.Remove<IMediaLibrary>();
    }

    /// <summary>
    /// A media item whose resource is whatever the test wants it to be, including unresolvable.
    /// </summary>
    /// <remarks>
    /// The locator is supplied rather than derived because resolving a stored path needs the resource provider
    /// system of a running server. The aspect data stays real - only the last step of the lookup, the one that
    /// needs a live system manager, stands in for.
    /// </remarks>
    private class TestItem : MediaItem
    {
      private readonly IResourceLocator _locator;
      private readonly bool _unresolvable;

      public TestItem(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects,
          IResourceLocator locator, bool unresolvable = false)
          : base(mediaItemId, aspects)
      {
        _locator = locator;
        _unresolvable = unresolvable;
      }

      public override IResourceLocator GetResourceLocator()
      {
        if (_unresolvable)
          throw new InvalidOperationException("Sequence contains no matching element");
        return _locator;
      }
    }

    private static SingleMediaItemAspect CreateMediaAspect(string title, string sortTitle, DateTime time)
    {
      SingleMediaItemAspect aspect = new SingleMediaItemAspect(MediaAspect.Metadata);
      aspect.SetAttribute(MediaAspect.ATTR_TITLE, title);
      aspect.SetAttribute(MediaAspect.ATTR_SORT_TITLE, sortTitle);
      aspect.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, time);
      return aspect;
    }

    private static SingleMediaItemAspect CreateMovieAspect(int runtimeMinutes)
    {
      SingleMediaItemAspect aspect = new SingleMediaItemAspect(MovieAspect.Metadata);
      if (runtimeMinutes > 0)
        aspect.SetAttribute(MovieAspect.ATTR_RUNTIME_M, runtimeMinutes);
      return aspect;
    }

    /// <summary>
    /// One title as the importer would have left it: the media aspect, and a movie aspect when a runtime is known.
    /// </summary>
    private static MediaItem CreateItem(Guid id, string title, string sortTitle, DateTime time,
        int runtimeMinutes = 0, IResourceLocator locator = null, bool unresolvable = false)
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { CreateMediaAspect(title, sortTitle, time) } }
      };
      if (runtimeMinutes > 0)
        aspects[MovieAspect.ASPECT_ID] = new List<MediaItemAspect> { CreateMovieAspect(runtimeMinutes) };
      return new TestItem(id, aspects, locator, unresolvable);
    }

    private static IMediaLibrary CreateLibrary(IList<MediaItem> items, int total,
        out List<MediaItemQuery> queries, out List<IEnumerable<Guid>> counted)
    {
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(new Dictionary<Guid, MediaItemAspectMetadata>
      {
          { MediaAspect.ASPECT_ID, MediaAspect.Metadata },
          { MovieAspect.ASPECT_ID, MovieAspect.Metadata },
          { TEST_ASPECT_ID, new SingleMediaItemAspectMetadata(TEST_ASPECT_ID, "Novel",
              new MediaItemAspectMetadata.SingleAttributeSpecification[0]) }
      });

      List<MediaItemQuery> asked = new List<MediaItemQuery>();
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false))
          .Callback<MediaItemQuery, bool, Guid?, bool>((query, online, profile, virtualItems) => asked.Add(query))
          .Returns(items);

      List<IEnumerable<Guid>> countCalls = new List<IEnumerable<Guid>>();
      library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), null, false, false))
          .Callback<IEnumerable<Guid>, IFilter, bool, bool>(
              (necessary, filter, online, virtualItems) => countCalls.Add(necessary))
          .Returns(total);

      queries = asked;
      counted = countCalls;
      return library.Object;
    }

    private static MediaController.TitlesResult GetTitles(IMediaLibrary library, string aspect, string sort,
        string order, int limit, int offset, out ActionResult<MediaController.TitlesResult> action)
    {
      ServiceRegistration.Set<IMediaLibrary>(library);
      action = new MediaController().GetTitles(aspect, sort, order, limit, offset);
      return Body(action);
    }

    /// <summary>
    /// Asks the details endpoint for one title, the way <see cref="GetTitles"/> asks the list endpoint.
    /// </summary>
    private static MediaController.ItemResult GetItem(IMediaLibrary library, Guid id,
        out ActionResult<MediaController.ItemResult> action)
    {
      ServiceRegistration.Set<IMediaLibrary>(library);
      action = new MediaController().GetItem(id);
      return Body(action);
    }

    /// <summary>
    /// An item with everything a details screen reads on it, each attribute written out separately because a test
    /// that filled them from a loop could not say which one the endpoint had mixed up.
    /// </summary>
    private static MediaItem CreateDetailedItem(Guid id, string title, string overview, string storyPlot,
        string seriesName, int season, int episode, string episodeName)
    {
      SingleMediaItemAspect media = CreateMediaAspect(title, title, new DateTime(2025, 4, 27, 20, 15, 0));
      media.SetAttribute(MediaAspect.ATTR_COMMENT, overview);
      media.SetAttribute(MediaAspect.ATTR_RATING, 8);
      media.SetAttribute(MediaAspect.ATTR_PLAYCOUNT, 3);
      media.SetAttribute(MediaAspect.ATTR_LASTPLAYED, new DateTime(2025, 5, 1, 21, 30, 0));

      SingleMediaItemAspect movie = CreateMovieAspect(52);
      movie.SetAttribute(MovieAspect.ATTR_CERTIFICATION, "FSK 16");
      movie.SetAttribute(MovieAspect.ATTR_TAGLINE, "In space, no one can hear you run");
      movie.SetAttribute(MovieAspect.ATTR_COLLECTION_NAME, "Alien Collection");

      SingleMediaItemAspect video = new SingleMediaItemAspect(VideoAspect.Metadata);
      video.SetAttribute(VideoAspect.ATTR_STORYPLOT, storyPlot);

      SingleMediaItemAspect episodeAspect = new SingleMediaItemAspect(EpisodeAspect.Metadata);
      episodeAspect.SetAttribute(EpisodeAspect.ATTR_SERIES_NAME, seriesName);
      episodeAspect.SetAttribute(EpisodeAspect.ATTR_SEASON, season);
      episodeAspect.SetCollectionAttribute(EpisodeAspect.ATTR_EPISODE, new[] { episode });
      episodeAspect.SetAttribute(EpisodeAspect.ATTR_EPISODE_NAME, episodeName);

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } },
          { MovieAspect.ASPECT_ID, new List<MediaItemAspect> { movie } },
          { VideoAspect.ASPECT_ID, new List<MediaItemAspect> { video } },
          { EpisodeAspect.ASPECT_ID, new List<MediaItemAspect> { episodeAspect } }
      };
      return new TestItem(id, aspects, CreateLocator(RESOURCE_PATH));
    }

    /// <summary>A resource locator that hands back the path a test names.</summary>
    private static IResourceLocator CreateLocator(string serializedPath)
    {
      var locator = new Mock<IResourceLocator>();
      locator.Setup(get => get.NativeResourcePath).Returns(ResourcePath.Deserialize(serializedPath));
      return locator.Object;
    }

    /// <summary>
    /// The status code a controller asked the framework to send, or null when it simply returned a body.
    /// </summary>
    /// <remarks>
    /// <see cref="ActionResult{T}"/> keeps the two halves apart: the framework's own result object goes into
    /// <c>Result</c> and the payload into <c>Value</c>, and only the former knows the status code. Reading it
    /// here is what keeps that fact out of every test.
    /// </remarks>
    private static int? Status<T>(ActionResult<T> action) where T : class
    {
      return action.Result == null ? (int?) null : ((IStatusCodeActionResult) action.Result).StatusCode;
    }

    /// <summary>
    /// The body of an answer, whether the controller returned it as the value or wrapped it in an error result.
    /// </summary>
    private static T Body<T>(ActionResult<T> action) where T : class
    {
      return (T) ((action.Result as ObjectResult)?.Value ?? action.Value);
    }

    #endregion

    #region When there is nothing to say

    [Test]
    public void NoMediaLibraryAnswersServiceUnavailable()
    {
      ActionResult<MediaController.TitlesResult> action =
          new MediaController().GetTitles("MovieItem", null, null, 0, 0);
      Assert.That(Status(action), Is.EqualTo(StatusCodes.Status503ServiceUnavailable),
          "the resource server starts before the media library does, so too early has to mean try again");
    }

    [Test]
    public void AnAnswerWithoutAnAspectIsRefused()
    {
      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(CreateLibrary(new List<MediaItem>(), 0, out _, out _),
          null, null, null, 0, 0, out action);
      Assert.That(Status(action), Is.EqualTo(StatusCodes.Status400BadRequest));
      Assert.That(result.Error, Does.Contain("aspect"), "and it says what is missing");
    }

    [Test]
    public void AnAspectTheServerDoesNotHaveIsNotFoundAndNamed()
    {
      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(CreateLibrary(new List<MediaItem>(), 0, out _, out _),
          "Karaoke", null, null, 0, 0, out action);
      Assert.That(Status(action), Is.EqualTo(StatusCodes.Status404NotFound));
      Assert.That(result.Error, Does.Contain("Karaoke"),
          "a front end can only offer the sections the server has, so it is told which one it asked for");
    }

    [Test]
    public void ARefusalFromTheLibraryIsAnsweredAsServiceUnavailable()
    {
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata())
          .Throws(new IllegalCallException("the media library is still initializing"));
      ServiceRegistration.Set<IMediaLibrary>(library.Object);

      ActionResult<MediaController.TitlesResult> action =
          new MediaController().GetTitles("MovieItem", null, null, 0, 0);
      Assert.That(Status(action), Is.EqualTo(StatusCodes.Status503ServiceUnavailable));
      Assert.That(Body(action).Error, Does.Contain("initializing"),
          "the server's reason is passed through rather than swallowed");
    }

    #endregion

    #region What a row holds

    [Test]
    public void ATitleCarriesWhatAViewerReads()
    {
      MediaItem item = CreateItem(Guid.NewGuid(), "Alien Earth", "Alien Earth",
          new DateTime(2025, 4, 27, 20, 15, 0), 52);
      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(CreateLibrary(new List<MediaItem> { item }, 1, out _, out _),
          "MovieItem", null, null, 0, 0, out action);

      Assert.That(result.Titles, Has.Count.EqualTo(1));
      MediaController.TitleResult title = result.Titles[0];
      Assert.That(title.Id, Is.EqualTo(item.MediaItemId));
      Assert.That(title.Title, Is.EqualTo("Alien Earth"));
      Assert.That(title.SortTitle, Is.EqualTo("Alien Earth"));
      Assert.That(title.Year, Is.EqualTo(2025), "a title list shows a year, not a timestamp");
      Assert.That(title.RuntimeMinutes, Is.EqualTo(52));
    }

    [Test]
    public void AnItemWithoutARuntimeHasNoneRatherThanZero()
    {
      MediaItem item = CreateItem(Guid.NewGuid(), "Silent Title", "Silent Title", DateTime.MinValue);
      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(CreateLibrary(new List<MediaItem> { item }, 1, out _, out _),
          "MovieItem", null, null, 0, 0, out action);

      Assert.That(result.Titles[0].RuntimeMinutes, Is.Null);
      Assert.That(result.Titles[0].Year, Is.Null,
          "no recording time is not year one; the attribute is simply empty");
    }

    [Test]
    public void TheDatabaseFallbackNameIsNotShownAsATitle()
    {
      MediaItem item = CreateItem(Guid.NewGuid(), "<Unknown>", "<Unknown>", DateTime.Now);
      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(CreateLibrary(new List<MediaItem> { item }, 1, out _, out _),
          "MovieItem", null, null, 0, 0, out action);

      Assert.That(result.Titles[0].Title, Is.Null,
          "an importer that never named a file leaves that name behind, and a media menu repeating it as the "
          + "title would be quoting the database at the viewer");
    }

    [Test]
    public void ThePathIsThePrimaryResourceAsTheLibrarySerializedIt()
    {
      var locator = new Mock<IResourceLocator>();
      locator.Setup(get => get.NativeResourcePath).Returns(ResourcePath.Deserialize(RESOURCE_PATH));
      MediaItem item = CreateItem(Guid.NewGuid(), "With File", "With File", DateTime.Now, 0, locator.Object);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(CreateLibrary(new List<MediaItem> { item }, 1, out _, out _),
          "MovieItem", null, null, 0, 0, out action);

      Assert.That(result.Titles[0].Path, Is.EqualTo(RESOURCE_PATH),
          "this is the whole point of the endpoint: the title arrives with the means to play it");
    }

    [Test]
    public void ATitleWhoseResourceCannotBeResolvedIsStillListed()
    {
      MediaItem broken = CreateItem(Guid.NewGuid(), "Broken Editions", "Broken Editions", DateTime.Now, 0,
          null, true);
      var locator = new Mock<IResourceLocator>();
      locator.Setup(get => get.NativeResourcePath).Returns(ResourcePath.Deserialize(RESOURCE_PATH));
      MediaItem fine = CreateItem(Guid.NewGuid(), "Good Title", "Good Title", DateTime.Now, 0, locator.Object);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(
          CreateLibrary(new List<MediaItem> { broken, fine }, 2, out _, out _),
          "MovieItem", null, null, 0, 0, out action);

      Assert.That(result.Titles, Has.Count.EqualTo(2),
          "one item whose recorded editions do not add up must not take the page down with it");
      Assert.That(result.Titles[0].Path, Is.Null);
      Assert.That(result.Titles[1].Path, Is.EqualTo(RESOURCE_PATH));
    }

    #endregion

    #region What the endpoint asks for

    [Test]
    public void OnlyRealItemsAreAskedFor()
    {
      List<MediaItemQuery> queries;
      List<IEnumerable<Guid>> counted;
      IMediaLibrary library = CreateLibrary(new List<MediaItem>(), 0, out queries, out counted);

      ActionResult<MediaController.TitlesResult> action;
      GetTitles(library, "MovieItem", null, null, 0, 0, out action);

      Assert.That(queries, Has.Count.EqualTo(1));
      Assert.That(queries[0].NecessaryRequestedMIATypeIDs, Is.EquivalentTo(new[] { MovieAspect.ASPECT_ID }),
          "the aspect the caller named is the one an item has to have to be listed");
      Assert.That(queries[0].OptionalRequestedMIATypeIDs, Does.Contain(MediaAspect.ASPECT_ID),
          "the title and the year live in the media aspect, which is asked for on every request");
      Assert.That(queries[0].OptionalRequestedMIATypeIDs, Does.Contain(ProviderResourceAspect.ASPECT_ID),
          "without the provider resource aspect no item has a path, and a pathless list cannot play anything");

      Assert.That(counted, Has.Count.EqualTo(1));
      Assert.That(counted[0], Is.EquivalentTo(new[] { MovieAspect.ASPECT_ID }),
          "the total is counted over the same aspect the titles came from");
    }

    [Test]
    public void AskingForTheMediaAspectDoesNotAskForItTwice()
    {
      List<MediaItemQuery> queries;
      IMediaLibrary library = CreateLibrary(new List<MediaItem>(), 0, out queries, out _);

      ActionResult<MediaController.TitlesResult> action;
      GetTitles(library, "MediaItem", null, null, 0, 0, out action);

      Assert.That(queries[0].NecessaryRequestedMIATypeIDs, Is.EquivalentTo(new[] { MediaAspect.ASPECT_ID }));
      Assert.That(queries[0].OptionalRequestedMIATypeIDs, Has.None.EqualTo(MediaAspect.ASPECT_ID),
          "an aspect named as both necessary and optional is a query the database has to reconcile for nothing");
    }

    [Test]
    public void AnAspectNobodyHasNamesIsFoundWithoutCareForCase()
    {
      List<MediaItemQuery> queries;
      IMediaLibrary library = CreateLibrary(new List<MediaItem>(), 0, out queries, out _);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(library, "movieitem", null, null, 0, 0, out action);

      Assert.That(Status(action), Is.Null, "a URL typed by hand should not fail on capitalization");
      Assert.That(result.AspectId, Is.EqualTo(MovieAspect.ASPECT_ID));
      Assert.That(result.Aspect, Is.EqualTo("MovieItem"), "and the answer uses the server's own spelling back");
    }

    [Test]
    public void TheDefaultOrderIsByName()
    {
      List<MediaItemQuery> queries;
      IMediaLibrary library = CreateLibrary(new List<MediaItem>(), 0, out queries, out _);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(library, "MovieItem", null, null, 0, 0, out action);

      Assert.That(queries[0].SortInformation, Has.Count.EqualTo(1));
      AttributeSortInformation sort = (AttributeSortInformation)queries[0].SortInformation[0];
      Assert.That(sort.AttributeType, Is.SameAs(MediaAspect.ATTR_SORT_TITLE),
          "the library sorts by the title with the article moved, which is why 'The Shawshank Redemption' "
          + "turns up under S");
      Assert.That(sort.Direction, Is.EqualTo(SortDirection.Ascending));
      Assert.That(result.Sort, Is.EqualTo("title"));
    }

    [Test]
    public void TheViewerCanSortByYearAndBackwards()
    {
      List<MediaItemQuery> queries;
      IMediaLibrary library = CreateLibrary(new List<MediaItem>(), 0, out queries, out _);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(library, "MovieItem", "year", "desc", 0, 0, out action);

      AttributeSortInformation sort = (AttributeSortInformation)queries[0].SortInformation[0];
      Assert.That(sort.AttributeType, Is.SameAs(MediaAspect.ATTR_RECORDINGTIME));
      Assert.That(sort.Direction, Is.EqualTo(SortDirection.Descending));
      Assert.That(result.Sort, Is.EqualTo("year descending"), "and the answer says what it was sorted by");
    }

    [Test]
    public void ASortThisServerDoesNotHaveFallsBackToTheNameOrder()
    {
      List<MediaItemQuery> queries;
      IMediaLibrary library = CreateLibrary(new List<MediaItem>(), 0, out queries, out _);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(library, "MovieItem", "user_rating", null, 0, 0, out action);

      Assert.That(Status(action), Is.Null,
          "a client from before an attribute existed still gets its list; refusing it would show the viewer "
          + "an empty screen over a word in a URL");
      Assert.That(queries[0].SortInformation, Has.Count.EqualTo(1));
      Assert.That(((AttributeSortInformation) queries[0].SortInformation[0]).AttributeType,
          Is.SameAs(MediaAspect.ATTR_SORT_TITLE));
      Assert.That(result.Sort, Is.EqualTo("title"),
          "and the echo is the correction: it says what the list was actually sorted by");
    }

    [Test]
    public void SortNoneAsksForNoSortAtAll()
    {
      List<MediaItemQuery> queries;
      IMediaLibrary library = CreateLibrary(new List<MediaItem>(), 0, out queries, out _);

      ActionResult<MediaController.TitlesResult> action;
      GetTitles(library, "MovieItem", "none", null, 0, 0, out action);

      Assert.That(queries[0].SortInformation, Is.Empty,
          "the order the database gives is a legitimate answer, and cheaper than sorting a library to prove it");
    }

    [Test]
    public void APageIsAlwaysLimited()
    {
      List<MediaItemQuery> queries;
      IMediaLibrary library = CreateLibrary(new List<MediaItem>(), 0, out queries, out _);

      ActionResult<MediaController.TitlesResult> action;
      GetTitles(library, "MovieItem", null, null, 0, 0, out action);
      Assert.That(queries[0].Limit, Is.EqualTo((uint)MediaController.DEFAULT_LIMIT),
          "a caller who says nothing gets a page, not the library");

      GetTitles(library, "MovieItem", null, null, 100000, 0, out action);
      Assert.That(queries[1].Limit, Is.EqualTo((uint)MediaController.MAX_LIMIT),
          "and asking for more than the ceiling gets the ceiling");

      GetTitles(library, "MovieItem", null, null, 25, -10, out action);
      Assert.That(queries[2].Limit, Is.EqualTo(25u));
      Assert.That(queries[2].Offset, Is.EqualTo(0u), "a negative offset is the beginning, not an error");
    }

    [Test]
    public void TheAnswerDescribesThePageItGave()
    {
      List<MediaItem> items = new List<MediaItem>
      {
          CreateItem(Guid.NewGuid(), "First", "First", DateTime.Now),
          CreateItem(Guid.NewGuid(), "Second", "Second", DateTime.Now)
      };
      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(CreateLibrary(items, 1234, out _, out _),
          "MovieItem", null, null, 2, 10, out action);

      Assert.That(result.Total, Is.EqualTo(1234), "a front end needs to know there is more");
      Assert.That(result.Offset, Is.EqualTo(10));
      Assert.That(result.Limit, Is.EqualTo(2));
      Assert.That(result.Returned, Is.EqualTo(2));
      Assert.That(result.Error, Is.Null);
    }

    #endregion

    #region What one title holds

    [Test]
    public void OneTitleAnswersWithWhatTheLibraryKnowsAboutIt()
    {
      Guid id = Guid.NewGuid();
      MediaItem item = CreateDetailedItem(id, "Alien Earth", "A family on a distant world.",
          "Stranded colonists learn the planet was inhabited.", "Alien Earth", 1, 1, "Nimmerland");

      ActionResult<MediaController.ItemResult> action;
      MediaController.ItemResult result = GetItem(
          CreateLibrary(new List<MediaItem> { item }, 1, out _, out _), id, out action);

      Assert.That(Status(action), Is.Null, "the item was found, so the answer is a plain body");
      Assert.That(result.Id, Is.EqualTo(id));
      Assert.That(result.Title, Is.EqualTo("Alien Earth"));
      Assert.That(result.Year, Is.EqualTo(2025));
      Assert.That(result.RuntimeMinutes, Is.EqualTo(52));
      Assert.That(result.Overview, Is.EqualTo("A family on a distant world."));
      Assert.That(result.StoryPlot, Is.EqualTo("Stranded colonists learn the planet was inhabited."));
      Assert.That(result.Certification, Is.EqualTo("FSK 16"));
      Assert.That(result.Tagline, Is.EqualTo("In space, no one can hear you run"));
      Assert.That(result.Collection, Is.EqualTo("Alien Collection"));
      Assert.That(result.Rating, Is.EqualTo(8));
      Assert.That(result.PlayCount, Is.EqualTo(3));
      Assert.That(result.LastPlayed, Is.EqualTo(new DateTime(2025, 5, 1, 21, 30, 0)));
      Assert.That(result.Path, Is.EqualTo(RESOURCE_PATH),
          "the same path the title list gave, so the row that offered the film can still play it");
    }

    [Test]
    public void AnEpisodeNamesItsSeriesAndNumbers()
    {
      Guid id = Guid.NewGuid();
      MediaItem item = CreateDetailedItem(id, "Nimmerland", "", "", "Alien Earth", 1, 1, "Nimmerland");

      ActionResult<MediaController.ItemResult> action;
      MediaController.ItemResult result = GetItem(
          CreateLibrary(new List<MediaItem> { item }, 1, out _, out _), id, out action);

      Assert.That(result.SeriesName, Is.EqualTo("Alien Earth"));
      Assert.That(result.Season, Is.EqualTo(1));
      Assert.That(result.Episode, Is.EqualTo(1));
      Assert.That(result.EpisodeName, Is.EqualTo("Nimmerland"));
      Assert.That(result.Overview, Is.Null,
          "an attribute the database holds empty is answered as nothing, not as an empty string that a screen "
          + "would draw as a blank line");
    }

    [Test]
    public void TheDetailsAreAskedForByIdAndNotGuessedAt()
    {
      Guid id = Guid.NewGuid();
      List<MediaItemQuery> queries;
      MediaItem item = CreateItem(id, "One Of One", "One Of One", DateTime.Now);

      ActionResult<MediaController.ItemResult> action;
      GetItem(CreateLibrary(new List<MediaItem> { item }, 1, out queries, out _), id, out action);

      Assert.That(queries, Has.Count.EqualTo(1), "one question to the library per details screen");
      Assert.That(queries[0].Filter, Is.InstanceOf<MediaItemIdFilter>());
      Assert.That(((MediaItemIdFilter) queries[0].Filter).MediaItemIds,
          Is.EquivalentTo(new[] { id }),
          "the id the caller gave is the id the library was asked for; nothing here lists the library and picks");
    }

    [Test]
    public void ATitleTheServerDoesNotHaveIsNotFoundAndNamed()
    {
      Guid id = Guid.NewGuid();
      ActionResult<MediaController.ItemResult> action;
      MediaController.ItemResult result = GetItem(
          CreateLibrary(new List<MediaItem>(), 0, out _, out _), id, out action);

      Assert.That(Status(action), Is.EqualTo(404));
      Assert.That(result.Error, Does.Contain(id.ToString()));
      Assert.That(result.Title, Is.Null, "a 404 with a title in it would be two answers to one question");
    }

    [Test]
    public void ATitleNobodyDescribedStillAnswersWithWhatItHas()
    {
      Guid id = Guid.NewGuid();
      MediaItem item = CreateItem(id, "Imported Without Metadata", "Imported Without Metadata",
          new DateTime(2024, 1, 1), 0);

      ActionResult<MediaController.ItemResult> action;
      MediaController.ItemResult result = GetItem(
          CreateLibrary(new List<MediaItem> { item }, 1, out _, out _), id, out action);

      Assert.That(result.Title, Is.EqualTo("Imported Without Metadata"));
      Assert.That(result.Overview, Is.Null);
      Assert.That(result.StoryPlot, Is.Null);
      Assert.That(result.RuntimeMinutes, Is.Null);
      Assert.That(result.Season, Is.Null, "a film is not season zero");
      Assert.That(result.Rating, Is.EqualTo(0),
          "the library's own default for an unrated title is zero, and the endpoint reports the number rather "
          + "than quietly deciding that nobody meant it");
    }

    [Test]
    public void NoMediaLibraryAnswersServiceUnavailableToADetailsRequest()
    {
      RemoveMediaLibrary();
      ActionResult<MediaController.ItemResult> action = new MediaController().GetItem(Guid.NewGuid());
      Assert.That(Status(action), Is.EqualTo(503));
    }

    [Test]
    public void ARefusedDetailsRequestIsAnsweredAsServiceUnavailable()
    {
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false))
          .Throws(new IllegalCallException("the media library is reloading"));

      ActionResult<MediaController.ItemResult> action;
      MediaController.ItemResult result = GetItem(library.Object, Guid.NewGuid(), out action);

      Assert.That(Status(action), Is.EqualTo(503));
      Assert.That(result.Error, Is.EqualTo("the media library is reloading"));
    }

    #endregion
  }
}
