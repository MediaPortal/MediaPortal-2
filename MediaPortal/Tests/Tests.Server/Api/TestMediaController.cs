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
using System.Security.Claims;
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
using MediaPortal.Common.UserProfileDataManagement;
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
          : this(mediaItemId, aspects, locator, null, unresolvable)
      {
      }

      /// <param name="userData">
      /// What the library holds for the profile the request named, in the same dictionary a real search result
      /// carries - <c>MediaItem.UserData</c>. Null for the ordinary case of a title nobody's data on.
      /// </param>
      public TestItem(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects,
          IResourceLocator locator, IDictionary<string, string> userData, bool unresolvable = false)
          : base(mediaItemId, aspects)
      {
        _locator = locator;
        _unresolvable = unresolvable;
        if (userData != null)
        {
          foreach (KeyValuePair<string, string> entry in userData)
            _userData[entry.Key] = entry.Value;
        }
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
      return GetTitles(library, aspect, sort, order, limit, offset, null, out action);
    }

    private static MediaController.TitlesResult GetTitles(IMediaLibrary library, string aspect, string sort,
        string order, int limit, int offset, string search,
        out ActionResult<MediaController.TitlesResult> action)
    {
      return GetTitles(library, aspect, sort, order, limit, offset, search, null, out action);
    }

    private static MediaController.TitlesResult GetTitles(IMediaLibrary library, string aspect, string sort,
        string order, int limit, int offset, string search, string seen,
        out ActionResult<MediaController.TitlesResult> action)
    {
      ServiceRegistration.Set<IMediaLibrary>(library);
      action = new MediaController().GetTitles(aspect, sort, order, limit, offset, search, seen);
      return Body(action);
    }

    /// <summary>
    /// Asks the list endpoint for the titles inside one title - or for a whole shelf, when no parent is named.
    /// </summary>
    /// <param name="library">The server's library.</param>
    /// <param name="aspect">Which kind of title is wanted.</param>
    /// <param name="sort">The order, or null for the endpoint's own.</param>
    /// <param name="seen">The watched filter, or null for both halves.</param>
    /// <param name="inside">The title to look inside, or <see cref="Guid.Empty"/> for the whole shelf.</param>
    private static MediaController.TitlesResult GetTitlesInside(IMediaLibrary library, string aspect, string sort,
        string seen, Guid inside, out ActionResult<MediaController.TitlesResult> action)
    {
      ServiceRegistration.Set<IMediaLibrary>(library);
      action = new MediaController().GetTitles(aspect, sort, null, 0, 0, null, seen, inside);
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
          new MediaController().GetTitles("MovieItem", null, null, 0, 0, null, null);
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
          new MediaController().GetTitles("MovieItem", null, null, 0, 0, null, null);
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

    #region What a watched title says

    private static readonly Guid PLAYBACK_ID = new Guid("55555555-5555-5555-5555-555555555555");
    private static readonly DateTime LAST_PLAYED_BEFORE = new DateTime(2025, 5, 1, 21, 30, 0);
    private static readonly DateTime LAST_PLAYED_AFTER = new DateTime(2026, 2, 14, 22, 5, 0);

    /// <summary>
    /// A library that is told about a viewing and then answers with its own counters, the way
    /// <c>MediaLibrary.NotifyPlayback</c> behaves: the date moves for any viewing, the count only for a finished
    /// one. The title is rebuilt for every search, so what a test reads back is what the endpoint read after the
    /// write rather than what the fixture handed out before it.
    /// </summary>
    private class PlaybackLibrary
    {
      private readonly Guid _id;
      private int _playCount;
      private DateTime _lastPlayed;

      internal readonly List<Notice> Notices = new List<Notice>();

      internal PlaybackLibrary(Guid id, int playCount, DateTime lastPlayed)
      {
        _id = id;
        _playCount = playCount;
        _lastPlayed = lastPlayed;

        var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
        library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(
            new Dictionary<Guid, MediaItemAspectMetadata> { { MediaAspect.ASPECT_ID, MediaAspect.Metadata } });
        library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false))
            .Returns(delegate { return new List<MediaItem> { TitleNow() }; });
        library.Setup(get => get.NotifyPlayback(It.IsAny<Guid>(), It.IsAny<bool>()))
            .Callback<Guid, bool>(delegate (Guid mediaItemId, bool watched)
            {
              Notices.Add(new Notice(mediaItemId, watched));
              _lastPlayed = LAST_PLAYED_AFTER;
              if (watched)
                _playCount++;
            });
        Library = library.Object;
      }

      internal IMediaLibrary Library { get; }

      internal MediaItem TitleNow()
      {
        SingleMediaItemAspect media = CreateMediaAspect("Alien Earth", "Alien Earth", new DateTime(2025, 4, 27));
        media.SetAttribute(MediaAspect.ATTR_PLAYCOUNT, _playCount);
        if (_lastPlayed != DateTime.MinValue)
          media.SetAttribute(MediaAspect.ATTR_LASTPLAYED, _lastPlayed);

        return new TestItem(_id, new Dictionary<Guid, IList<MediaItemAspect>>
        {
            { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } }
        }, null);
      }

      internal class Notice
      {
        internal Notice(Guid mediaItemId, bool watched)
        {
          MediaItemId = mediaItemId;
          Watched = watched;
        }

        internal Guid MediaItemId { get; }
        internal bool Watched { get; }
      }
    }

    /// <summary>
    /// Posts a report to the endpoint. A null percentage is the body a caller forgot to fill in, which is a
    /// different thing from a percentage of zero and has to stay a different thing.
    /// </summary>
    private static ActionResult<MediaController.PlaybackResult> ReportPlayback(IMediaLibrary library, Guid id,
        int? percentage)
    {
      ServiceRegistration.Set<IMediaLibrary>(library);
      return new MediaController().ReportPlayback(id, percentage.HasValue
          ? new MediaController.PlaybackRequest { Percentage = percentage.Value }
          : null);
    }

    [Test]
    public void ATitleWatchedToTheEndIsCountedAsWatched()
    {
      PlaybackLibrary library = new PlaybackLibrary(PLAYBACK_ID, 2, LAST_PLAYED_BEFORE);

      ActionResult<MediaController.PlaybackResult> action = ReportPlayback(library.Library, PLAYBACK_ID, 100);

      Assert.That(Status(action), Is.Null, "a report the library took is a plain 200");
      MediaController.PlaybackResult result = Body(action);
      Assert.That(result.Id, Is.EqualTo(PLAYBACK_ID));
      Assert.That(result.Watched, Is.True);
      Assert.That(result.PlayCount, Is.EqualTo(3), "the count after the write, not the one from before it");
      Assert.That(result.LastPlayed, Is.EqualTo(LAST_PLAYED_AFTER));
      Assert.That(library.Notices, Has.Count.EqualTo(1));
      Assert.That(library.Notices[0].MediaItemId, Is.EqualTo(PLAYBACK_ID));
      Assert.That(library.Notices[0].Watched, Is.True, "the library is told in its own words: watched, or not");
    }

    [Test]
    public void ATitleStartedButNotFinishedDoesNotCountAsWatched()
    {
      PlaybackLibrary library = new PlaybackLibrary(PLAYBACK_ID, 2, LAST_PLAYED_BEFORE);

      MediaController.PlaybackResult result = Body(ReportPlayback(library.Library, PLAYBACK_ID, 40));

      Assert.That(result.Watched, Is.False);
      Assert.That(result.PlayCount, Is.EqualTo(2), "half a film is not a viewing");
      Assert.That(library.Notices[0].Watched, Is.False);
      Assert.That(result.LastPlayed, Is.EqualTo(LAST_PLAYED_AFTER),
          "but the date does move: a title somebody started is not a title nobody has ever seen");
    }

    [Test]
    public void NinetyNinePerCentIsNotAFinishedFilm()
    {
      PlaybackLibrary library = new PlaybackLibrary(PLAYBACK_ID, 0, DateTime.MinValue);

      MediaController.PlaybackResult result = Body(ReportPlayback(library.Library, PLAYBACK_ID, 99));

      Assert.That(result.Watched, Is.False);
      Assert.That(result.PlayCount, Is.EqualTo(0),
          "a tick on the wrong film is a lie a viewer has to notice in order to correct");
    }

    [Test]
    public void AReportForATitleTheServerDoesNotKnowIsRefused()
    {
      // The title was removed between the list showing it and the viewer finishing it.
      var empty = new Mock<IMediaLibrary>(MockBehavior.Strict);
      empty.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false))
          .Returns(new List<MediaItem>());

      ActionResult<MediaController.PlaybackResult> action = ReportPlayback(empty.Object, PLAYBACK_ID, 100);

      Assert.That(Status(action), Is.EqualTo(404));
      Assert.That(Body(action).Error, Does.Contain(PLAYBACK_ID.ToString()),
          "the id that was asked about is named in the complaint, or the complaint is useless");
      empty.Verify(get => get.NotifyPlayback(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never(),
          "a library that never heard of the id is not told to mark it");
    }

    [Test]
    public void AReportNobodyFilledInIsRefusedRatherThanTakenAsFinished()
    {
      PlaybackLibrary library = new PlaybackLibrary(PLAYBACK_ID, 2, LAST_PLAYED_BEFORE);

      ActionResult<MediaController.PlaybackResult> action = ReportPlayback(library.Library, PLAYBACK_ID, null);

      Assert.That(Status(action), Is.EqualTo(400));
      Assert.That(Body(action).Error, Is.Not.Null.And.Not.Empty);
      Assert.That(library.Notices, Is.Empty, "a request with no number in it is not a claim that something ended");
    }

    [Test]
    public void ANumberOutsideTheScaleIsRefused()
    {
      PlaybackLibrary library = new PlaybackLibrary(PLAYBACK_ID, 2, LAST_PLAYED_BEFORE);

      foreach (int nonsense in new[] { -1, 101, 4000 })
      {
        ActionResult<MediaController.PlaybackResult> action = ReportPlayback(library.Library, PLAYBACK_ID, nonsense);
        Assert.That(Status(action), Is.EqualTo(400), "how far somebody watched is not " + nonsense + " per cent");
      }
      Assert.That(library.Notices, Is.Empty);
    }

    [Test]
    public void AReportToALibraryThatIsNotReadySaysSo()
    {
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false))
          .Throws(new IllegalCallException("the media library is reloading"));

      ActionResult<MediaController.PlaybackResult> action = ReportPlayback(library.Object, PLAYBACK_ID, 100);

      Assert.That(Status(action), Is.EqualTo(503));
      Assert.That(Body(action).Error, Is.EqualTo("the media library is reloading"));
    }

    [Test]
    public void NoMediaLibraryAnswersServiceUnavailableToAReport()
    {
      RemoveMediaLibrary();

      ActionResult<MediaController.PlaybackResult> action = new MediaController().ReportPlayback(PLAYBACK_ID,
          new MediaController.PlaybackRequest { Percentage = 100 });

      Assert.That(Status(action), Is.EqualTo(503));
    }

    #endregion

    #region What a search asks the library

    /// <summary>
    /// One "find these words" question, as the library heard it.
    /// </summary>
    private class Question
    {
      internal string Words;
      internal List<Guid> Necessary;
      internal List<Guid> Optional;
      internal IFilter Extra;
      internal bool LargeText;
      internal bool CaseSensitive;
    }

    /// <summary>The filter the library says it built out of the words. Only its identity matters here.</summary>
    private static readonly IFilter SEARCH_FILTER = new FalseFilter();

    /// <summary>
    /// A library that answers the search question the way MP2's real one does - by handing back a query it built
    /// itself - and writes down every question it was asked and every total it was asked to count.
    /// </summary>
    private static IMediaLibrary SearchingLibrary(IList<MediaItem> items, int matches, out List<Question> questions,
        out List<IFilter> countedThrough)
    {
      List<Question> asked = new List<Question>();
      List<IFilter> counts = new List<IFilter>();

      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(
          new Dictionary<Guid, MediaItemAspectMetadata>
          {
              { MediaAspect.ASPECT_ID, MediaAspect.Metadata },
              { MovieAspect.ASPECT_ID, MovieAspect.Metadata }
          });

      library.Setup(get => get.BuildSimpleTextSearchQuery(It.IsAny<string>(), It.IsAny<IEnumerable<Guid>>(),
              It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), It.IsAny<bool>(), It.IsAny<bool>()))
          .Returns((string words, IEnumerable<Guid> necessary, IEnumerable<Guid> optional, IFilter extra,
              bool largeText, bool caseSensitive) =>
          {
            Question question = new Question();
            question.Words = words;
            question.Necessary = new List<Guid>(necessary);
            question.Optional = new List<Guid>(optional);
            question.Extra = extra;
            question.LargeText = largeText;
            question.CaseSensitive = caseSensitive;
            asked.Add(question);
            return new MediaItemQuery(necessary, optional, SEARCH_FILTER);
          });

      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false)).Returns(items);
      library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), false, false))
          .Callback<IEnumerable<Guid>, IFilter, bool, bool>(
              (necessary, filter, online, virtualItems) => counts.Add(filter))
          .Returns(matches);

      questions = asked;
      countedThrough = counts;
      return library.Object;
    }

    [Test]
    public void TheWordsArePutToTheLibrarysOwnSearchRatherThanToAFilterBuiltHere()
    {
      List<Question> questions;
      List<IFilter> counted;
      IMediaLibrary library = SearchingLibrary(new List<MediaItem>(), 1, out questions, out counted);

      ActionResult<MediaController.TitlesResult> action;
      GetTitles(library, "MovieItem", null, null, 0, 0, "  alien earth  ", out action);

      Assert.That(questions, Has.Count.EqualTo(1), "asked once, however the screen spells the question");
      Assert.That(questions[0].Words, Is.EqualTo("alien earth"), "and asked without the spaces around it");
      Assert.That(questions[0].Necessary, Is.EquivalentTo(new[] { MovieAspect.ASPECT_ID }),
          "a search for a film is still a search among films");
      Assert.That(questions[0].Optional, Does.Contain(MediaAspect.ASPECT_ID));

      Assert.That(questions[0].LargeText, Is.False,
          "plot text is not searched: every film mentioning a spaceship is not the answer to typing 'spaceship'");
      Assert.That(questions[0].CaseSensitive, Is.False, "a remote control has no shift key");
    }

    [Test]
    public void TheNumberOfMatchesIsCountedThroughTheSameQuestionTheTitlesCameThrough()
    {
      List<Question> questions;
      List<IFilter> counted;
      IMediaLibrary library = SearchingLibrary(new List<MediaItem>
      {
          CreateItem(Guid.NewGuid(), "Alien", "Alien", new DateTime(1979, 5, 25)),
          CreateItem(Guid.NewGuid(), "Alien Earth", "Alien Earth", new DateTime(2025, 4, 6))
      }, 2, out questions, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(library, "MovieItem", null, null, 0, 0, "alien", out action);

      Assert.That(counted, Has.Count.EqualTo(1));
      Assert.That(counted[0], Is.SameAs(SEARCH_FILTER),
          "a total counted over the whole shelf while the list shows matches is a list that never stops offering more");
      Assert.That(result.Total, Is.EqualTo(2));
      Assert.That(result.Returned, Is.EqualTo(2));
    }

    [Test]
    public void TheAnswerSaysWhichWordsItIsAbout()
    {
      List<Question> questions;
      List<IFilter> counted;
      IMediaLibrary library = SearchingLibrary(new List<MediaItem>(), 0, out questions, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(library, "MovieItem", null, null, 0, 0, "star trek", out action);

      Assert.That(result.Search, Is.EqualTo("star trek"),
          "an empty list is a different story when it was filtered, and only the answer can say which it was");
    }

    [Test]
    public void AskingForTheWholeShelfAsksNoQuestion()
    {
      List<Question> questions;
      List<IFilter> counted;
      IMediaLibrary library = SearchingLibrary(new List<MediaItem>(), 7, out questions, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(library, "MovieItem", null, null, 0, 0, out action);

      Assert.That(questions, Is.Empty, "no words, no search");
      Assert.That(counted[0], Is.Null, "and the whole shelf is what the total counts");
      Assert.That(result.Search, Is.Null);
      Assert.That(result.Total, Is.EqualTo(7));
    }

    [Test]
    public void SpacesAreNotAWord()
    {
      List<Question> questions;
      List<IFilter> counted;
      IMediaLibrary library = SearchingLibrary(new List<MediaItem>(), 7, out questions, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(library, "MovieItem", null, null, 0, 0, "   ", out action);

      Assert.That(questions, Is.Empty,
          "a search box somebody emptied means \"show everything again\", not \"find the titles that are spaces\"");
      Assert.That(result.Search, Is.Null);
    }

    [Test]
    public void SearchingAnAspectNobodyHasIsRefusedBeforeTheLibraryIsAsked()
    {
      List<Question> questions;
      List<IFilter> counted;
      IMediaLibrary library = SearchingLibrary(new List<MediaItem>(), 0, out questions, out counted);

      ActionResult<MediaController.TitlesResult> action;
      GetTitles(library, "Novel", null, null, 0, 0, "alien", out action);

      Assert.That(Status(action), Is.EqualTo(404));
      Assert.That(questions, Is.Empty, "the question is refused here rather than passed on to be answered with nothing");
    }

    #endregion

    #region What a watched filter asks the library

    /// <summary>
    /// A library that writes down the query it was asked to run and the condition its total was counted through.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="CreateLibrary"/> because that one is set up strictly for the no-filter call, and a
    /// watched filter is a condition - a stub that only answers "no condition" would let an endpoint that forgot to
    /// pass the filter through look as if it had passed it.
    /// </remarks>
    private static IMediaLibrary WatchedLibrary(IList<MediaItem> items, int matches,
        out List<MediaItemQuery> asked, out List<IFilter> countedThrough)
    {
      List<MediaItemQuery> questions = new List<MediaItemQuery>();
      List<IFilter> counts = new List<IFilter>();

      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(
          new Dictionary<Guid, MediaItemAspectMetadata>
          {
              { MediaAspect.ASPECT_ID, MediaAspect.Metadata },
              { MovieAspect.ASPECT_ID, MovieAspect.Metadata }
          });
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false))
          .Callback<MediaItemQuery, bool, Guid?, bool>(
              (query, online, profile, virtualItems) => questions.Add(query))
          .Returns(items);
      library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), false, false))
          .Callback<IEnumerable<Guid>, IFilter, bool, bool>(
              (necessary, filter, online, virtualItems) => counts.Add(filter))
          .Returns(matches);

      asked = questions;
      countedThrough = counts;
      return library.Object;
    }

    /// <summary>
    /// One title as a library that was told about a viewing looks: the media aspect carrying the play count and
    /// the date.
    /// </summary>
    private static MediaItem CreateSeenItem(Guid id, string title, int playCount, DateTime lastPlayed)
    {
      SingleMediaItemAspect media = CreateMediaAspect(title, title, new DateTime(2020, 1, 1));
      media.SetAttribute(MediaAspect.ATTR_PLAYCOUNT, playCount);
      if (lastPlayed != DateTime.MinValue)
        media.SetAttribute(MediaAspect.ATTR_LASTPLAYED, lastPlayed);

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } }
      };
      return new TestItem(id, aspects, null, false);
    }

    [Test]
    public void AWatchedListIsTheLibrarysOwnPlayCountCondition()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = WatchedLibrary(new List<MediaItem>(), 3, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result =
          GetTitles(library, "MovieItem", null, null, 0, 0, null, "watched", out action);

      RelationalFilter condition = (RelationalFilter) asked[0].Filter;
      Assert.That(condition.AttributeType, Is.SameAs(MediaAspect.ATTR_PLAYCOUNT),
          "the same column MP2's own last-watched list filters on, not one invented for this endpoint");
      Assert.That(condition.Operator, Is.EqualTo(RelationalOperator.GT));
      Assert.That(condition.FilterValue, Is.EqualTo(0));
      Assert.That(result.Seen, Is.EqualTo("watched"));
    }

    [Test]
    public void AnUnwatchedListAlsoAsksForATitleNobodyEverOpened()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = WatchedLibrary(new List<MediaItem>(), 400, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      GetTitles(library, "MovieItem", null, null, 0, 0, null, "unwatched", out action);

      BooleanCombinationFilter combined = (BooleanCombinationFilter) asked[0].Filter;
      Assert.That(combined.Operator, Is.EqualTo(BooleanOperator.Or));

      RelationalFilter zero = combined.Operands.OfType<RelationalFilter>().Single();
      Assert.That(zero.AttributeType, Is.SameAs(MediaAspect.ATTR_PLAYCOUNT));
      Assert.That(zero.Operator, Is.EqualTo(RelationalOperator.EQ),
          "MP2's own unwatched condition: a play count of zero");
      Assert.That(zero.FilterValue, Is.EqualTo(0));

      EmptyFilter nothing = combined.Operands.OfType<EmptyFilter>().Single();
      Assert.That(nothing.AttributeType, Is.SameAs(MediaAspect.ATTR_PLAYCOUNT),
          "and the half a freshly imported library needs: a title nobody ever opened has nothing written in that "
          + "column at all, and a condition of \"equals zero\" is not true about nothing - without this half, a "
          + "brand new library answers \"no unwatched titles\", which is the one answer a viewer knows is wrong");
    }

    [Test]
    public void AFilterNobodyNamedAsksTheLibraryForNothing()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = WatchedLibrary(new List<MediaItem>(), 10, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult unnamed =
          GetTitles(library, "MovieItem", null, null, 0, 0, null, null, out action);
      Assert.That(asked[0].Filter, Is.Null, "both halves of the shelf, which is what a screen shows by default");
      Assert.That(unnamed.Seen, Is.Null);

      MediaController.TitlesResult misspelled =
          GetTitles(library, "MovieItem", null, null, 0, 0, null, "half-seen", out action);
      Assert.That(asked[1].Filter, Is.Null,
          "a word this endpoint does not know is not a filter; asking for the whole shelf is the safe reading of "
          + "a request that cannot be honoured, and the echo says so");
      Assert.That(misspelled.Seen, Is.Null, "and the answer never claims a filter it did not apply");
    }

    [Test]
    public void TheSizeOfAWatchedListIsCountedThroughTheSameFilter()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = WatchedLibrary(new List<MediaItem>(), 7, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      GetTitles(library, "MovieItem", null, null, 0, 0, null, "unwatched", out action);

      Assert.That(counted, Has.Count.EqualTo(1));
      Assert.That(counted[0], Is.SameAs(asked[0].Filter),
          "the total a front end shows and the list it shows have to be answers to one question, or \"7 of 3443 "
          + "unwatched films\" would be counting the whole shelf behind a filtered list");
    }

    [Test]
    public void AWatchedTitleCarriesItsFiguresIntoItsRow()
    {
      Guid seenId = Guid.NewGuid();
      Guid freshId = Guid.NewGuid();
      List<MediaItemQuery> asked;
      IMediaLibrary library = WatchedLibrary(new List<MediaItem>
      {
          CreateSeenItem(seenId, "Alien Earth", 2, new DateTime(2025, 4, 27, 20, 15, 0)),
          CreateSeenItem(freshId, "Silent Title", 0, DateTime.MinValue)
      }, 2, out asked, out _);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result =
          GetTitles(library, "MovieItem", null, null, 0, 0, null, null, out action);

      MediaController.TitleResult seen = result.Titles.Single(title => title.Id == seenId);
      Assert.That(seen.PlayCount, Is.EqualTo(2));
      Assert.That(seen.LastPlayed, Is.EqualTo(new DateTime(2025, 4, 27, 20, 15, 0)));

      MediaController.TitleResult fresh = result.Titles.Single(title => title.Id == freshId);
      Assert.That(fresh.PlayCount, Is.EqualTo(0), "never finished, which a screen marks as unseen");
      Assert.That(fresh.LastPlayed, Is.Null,
          "and a title that was never opened says no date rather than showing the year 1 on screen");
    }

    [Test]
    public void AWordAndAWatchedFilterAreAskedAsOneQuestion()
    {
      List<Question> questions;
      List<IFilter> counted;
      IMediaLibrary library = SearchingLibrary(new List<MediaItem>(), 1, out questions, out counted);

      ActionResult<MediaController.TitlesResult> action;
      GetTitles(library, "MovieItem", null, null, 0, 0, "alien", "unwatched", out action);

      Assert.That(questions, Has.Count.EqualTo(1),
          "a filtered search is one question to the library, not two that have to be joined here");
      Assert.That(questions[0].Words, Is.EqualTo("alien"));

      BooleanCombinationFilter extra = (BooleanCombinationFilter) questions[0].Extra;
      Assert.That(extra, Is.Not.Null, "the watched condition travels into the library's own search builder");
      Assert.That(extra.Operands.OfType<EmptyFilter>().Any(), Is.True,
          "with the same second half, so that a search among unwatched titles finds the ones nobody ever opened");
    }

    [Test]
    public void TheViewerCanSortByWhenItWasLastPlayed()
    {
      List<MediaItemQuery> asked;
      IMediaLibrary library = WatchedLibrary(new List<MediaItem>(), 2, out asked, out _);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result =
          GetTitles(library, "MovieItem", "lastplayed", "desc", 0, 0, null, "watched", out action);

      AttributeSortInformation sort = (AttributeSortInformation) asked[0].SortInformation[0];
      Assert.That(sort.AttributeType, Is.SameAs(MediaAspect.ATTR_LASTPLAYED));
      Assert.That(sort.Direction, Is.EqualTo(SortDirection.Descending),
          "the most recent viewing first, which is the only order a \"what was I watching\" list wants");
      Assert.That(result.Sort, Is.EqualTo("lastplayed descending"));
    }

    [Test]
    public void AListLeftHalfWayAsksForATitleThatWasOpenedButNotFinished()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = WatchedLibrary(new List<MediaItem>(), 5, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result =
          GetTitles(library, "MovieItem", null, null, 0, 0, null, "started", out action);

      BooleanCombinationFilter combined = (BooleanCombinationFilter) asked[0].Filter;
      Assert.That(combined.Operator, Is.EqualTo(BooleanOperator.And),
          "two conditions have to hold at once, or the list is one of the two the viewer did not ask for");
      Assert.That(result.Seen, Is.EqualTo("started"));

      NotFilter opened = combined.Operands.OfType<NotFilter>().Single();
      EmptyFilter date = (EmptyFilter) opened.InnerFilter;
      Assert.That(date.AttributeType, Is.SameAs(MediaAspect.ATTR_LASTPLAYED),
          "somebody has to have opened it - without this half, every film nobody ever touched turns up as \"you "
          + "left it half way\", which is the whole shelf wearing a different name");

      BooleanCombinationFilter notFinished = combined.Operands.OfType<BooleanCombinationFilter>().Single();
      Assert.That(notFinished.Operator, Is.EqualTo(BooleanOperator.Or));

      RelationalFilter zero = notFinished.Operands.OfType<RelationalFilter>().Single();
      Assert.That(zero.AttributeType, Is.SameAs(MediaAspect.ATTR_PLAYCOUNT));
      Assert.That(zero.Operator, Is.EqualTo(RelationalOperator.EQ));
      Assert.That(zero.FilterValue, Is.EqualTo(0),
          "and nothing was ever counted as finished - a watched film keeps its last-played date behind it, so "
          + "without this half the strip would tell the viewer to sit down and watch again a film they finished");

      EmptyFilter nothing = notFinished.Operands.OfType<EmptyFilter>().Single();
      Assert.That(nothing.AttributeType, Is.SameAs(MediaAspect.ATTR_PLAYCOUNT),
          "again the NULL half: a title whose play count was never written was not finished either");
    }

    [Test]
    public void AListLeftHalfWayIsCountedThroughItsOwnQuestion()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = WatchedLibrary(new List<MediaItem>(), 3, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      GetTitles(library, "MovieItem", "lastplayed", "desc", 8, 0, null, "started", out action);

      Assert.That(counted, Has.Count.EqualTo(1));
      Assert.That(counted[0], Is.SameAs(asked[0].Filter),
          "the home screen says how many titles it found; counting the whole shelf behind that number would be a "
          + "\"3 titles left half way\" over a library of four thousand");
    }

    [Test]
    public void AWordAndAHalfWayFilterAreAskedAsOneQuestion()
    {
      List<Question> questions;
      List<IFilter> counted;
      IMediaLibrary library = SearchingLibrary(new List<MediaItem>(), 1, out questions, out counted);

      ActionResult<MediaController.TitlesResult> action;
      GetTitles(library, "MovieItem", null, null, 0, 0, "alien", "started", out action);

      Assert.That(questions, Has.Count.EqualTo(1));
      Assert.That(questions[0].Words, Is.EqualTo("alien"));

      BooleanCombinationFilter extra = (BooleanCombinationFilter) questions[0].Extra;
      Assert.That(extra, Is.Not.Null);
      Assert.That(extra.Operands.OfType<NotFilter>().Any(), Is.True,
          "searching among the titles left half way keeps both halves of the watched question");
    }

    #endregion

    #region What one person's own playback data adds

    /// <summary>
    /// Somebody. The profile id a bearer token names once a client has signed in with a username and password.
    /// </summary>
    private static readonly Guid VIEWER = new Guid("66666666-6666-6666-6666-666666666666");

    /// <summary>
    /// A controller answering a request from one named person.
    /// </summary>
    /// <param name="profile">
    /// The profile the token carries, or null for a request from nobody - which is what every other test in this
    /// fixture asks as, because a controller built by hand has no HTTP context at all.
    /// </param>
    private static MediaController AskedBy(Guid? profile)
    {
      MediaController controller = new MediaController();
      if (profile == null)
        return controller;

      ClaimsPrincipal caller = new ClaimsPrincipal(new ClaimsIdentity(new[]
      {
        new Claim(ClaimTypes.Name, "georg"),
        new Claim(ClaimTypes.Sid, profile.Value.ToString())
      }, "MediaPortalTest"));

      controller.ControllerContext = new ControllerContext
      {
        HttpContext = new DefaultHttpContext { User = caller }
      };
      return controller;
    }

    /// <summary>
    /// A library that answers under whatever name it is asked under and remembers both that name and every report
    /// it was given.
    /// </summary>
    /// <remarks>
    /// Both halves matter for a per-user feature, and they fail in opposite directions: asked under the wrong name
    /// the answer is somebody else's place in the film, written under the wrong name the position is filed where
    /// nobody will ever find it. Neither shows up in a screenshot of a shelf that has one viewer.
    /// </remarks>
    private class NameAwareLibrary
    {
      /// <summary>The profile each titles request was asked under, in order.</summary>
      internal readonly List<Guid?> AskedProfiles = new List<Guid?>();

      /// <summary>Reports of the form <c>profile|percentage|date updated</c>.</summary>
      internal readonly List<string> PerUserReports = new List<string>();

      /// <summary>Reports that went to the title's own counters instead of to a person.</summary>
      internal readonly List<string> SharedReports = new List<string>();

      private readonly IList<MediaItem> _items;

      internal NameAwareLibrary(IList<MediaItem> items)
      {
        _items = items;

        var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
        library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(
            new Dictionary<Guid, MediaItemAspectMetadata>
            {
                { MediaAspect.ASPECT_ID, MediaAspect.Metadata },
                { MovieAspect.ASPECT_ID, MovieAspect.Metadata }
            });
        library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, It.IsAny<Guid?>(), false))
            .Callback<MediaItemQuery, bool, Guid?, bool>(
                delegate (MediaItemQuery query, bool online, Guid? profile, bool virtualItems)
                { AskedProfiles.Add(profile); })
            .Returns(_items);
        library.Setup(count => count.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), false, false))
            .Returns(delegate { return _items.Count; });
        library.Setup(tell => tell.NotifyUserPlayback(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(),
                It.IsAny<bool>()))
            .Callback<Guid, Guid, int, bool>(delegate (Guid user, Guid mediaItemId, int percentage, bool updateDate)
            { PerUserReports.Add(user + "|" + percentage + "|" + updateDate); });
        library.Setup(tell => tell.NotifyPlayback(It.IsAny<Guid>(), It.IsAny<bool>()))
            .Callback<Guid, bool>(delegate (Guid mediaItemId, bool watched)
            { SharedReports.Add("watched=" + watched); });

        Library = library.Object;
      }

      internal IMediaLibrary Library { get; }
    }

    /// <summary>
    /// A title carrying one person's stored position, in the shape the database actually holds it: the percentage
    /// as the fixed-width text <c>UserDataKeysKnown.GetSortablePlayPercentageString</c> writes it
    /// (<c>"042"</c>, so that a text column sorts numerically), on <c>MediaItem.UserData</c>.
    /// </summary>
    private static MediaItem TitleWithStoredPosition(Guid id, string storedPercentage)
    {
      IDictionary<string, string> userData = storedPercentage == null
          ? null
          : new Dictionary<string, string>
          {
              { UserDataKeysKnown.KEY_PLAY_PERCENTAGE, storedPercentage }
          };

      return new TestItem(id,
          new Dictionary<Guid, IList<MediaItemAspect>>
          {
              { MediaAspect.ASPECT_ID, new List<MediaItemAspect>
                  { CreateMediaAspect("Salt and Static", "Salt and Static", new DateTime(2025, 4, 27)) } }
          },
          null, userData);
    }

    private static MediaController.TitlesResult TitlesAs(IMediaLibrary library, Guid? profile)
    {
      ServiceRegistration.Set<IMediaLibrary>(library);
      return Body(AskedBy(profile).GetTitles("MovieItem", null, null, 50, 0, null, null));
    }

    private static ActionResult<MediaController.PlaybackResult> ReportedBy(IMediaLibrary library, Guid? profile,
        Guid id, int percentage)
    {
      ServiceRegistration.Set<IMediaLibrary>(library);
      return AskedBy(profile).ReportPlayback(id, new MediaController.PlaybackRequest { Percentage = percentage });
    }

    [Test]
    public void ThePersonNamedByTheTokenIsThePersonTheLibraryIsAskedAbout()
    {
      NameAwareLibrary library = new NameAwareLibrary(
          new List<MediaItem> { TitleWithStoredPosition(PLAYBACK_ID, "042") });

      MediaController.TitlesResult body = TitlesAs(library.Library, VIEWER);

      Assert.That(library.AskedProfiles, Has.Count.EqualTo(1));
      Assert.That(library.AskedProfiles[0], Is.EqualTo(VIEWER),
          "a resume position belongs to a person, and the library only puts one on a row when the question named "
          + "somebody; asking under nobody therefore offers to resume films nobody started");
      Assert.That(body.Titles, Has.Count.EqualTo(1));
    }

    [Test]
    public void ARequestFromNobodyAsksTheLibraryAboutNobody()
    {
      NameAwareLibrary library = new NameAwareLibrary(
          new List<MediaItem> { TitleWithStoredPosition(PLAYBACK_ID, "042") });

      TitlesAs(library.Library, null);

      Assert.That(library.AskedProfiles[0], Is.Null,
          "an unauthenticated request must not reach the library carrying a profile; Guid.Empty would be worse "
          + "than null here, because the query engine joins the user data table on it and answers with nothing");
    }

    [Test]
    public void APositionStoredForAPersonArrivesOnTheirRow()
    {
      NameAwareLibrary library = new NameAwareLibrary(
          new List<MediaItem> { TitleWithStoredPosition(PLAYBACK_ID, "042") });

      MediaController.TitlesResult body = TitlesAs(library.Library, VIEWER);

      Assert.That(body.Titles[0].Resume, Is.EqualTo(42),
          "the library stores \"042\" as text so that it sorts; an answer carrying the text, or nothing at all, "
          + "is a screen that cannot say how far anybody got");
    }

    [Test]
    public void ARequestFromNobodyReportsNoPositionEvenWhenTheLibraryHandsOneOut()
    {
      // The library in this test is deliberately worse than the real one: it puts a stored position on the item
      // even though it was asked under nobody. Whatever the data layer does, an answer that names no caller may
      // not present one person's place in a film as the position to resume from.
      NameAwareLibrary library = new NameAwareLibrary(
          new List<MediaItem> { TitleWithStoredPosition(PLAYBACK_ID, "042") });

      MediaController.TitlesResult body = TitlesAs(library.Library, null);

      Assert.That(body.Titles[0].Resume, Is.Null);
    }

    [Test]
    public void AFinishedTitleAndAnAbandonedAtTheStartTitleOfferNothingToResume()
    {
      // A hundred is a film that is finished and nought is a film whose viewer never got anywhere: both have no
      // position worth seeking to, which is MP2's own continue-watch interval of one to ninety-nine.
      MediaController.TitlesResult finished = TitlesAs(new NameAwareLibrary(
          new List<MediaItem> { TitleWithStoredPosition(PLAYBACK_ID, "100") }).Library, VIEWER);
      MediaController.TitlesResult untouched = TitlesAs(new NameAwareLibrary(
          new List<MediaItem> { TitleWithStoredPosition(PLAYBACK_ID, "000") }).Library, VIEWER);

      Assert.That(finished.Titles[0].Resume, Is.Null, "a finished film has nothing left to resume");
      Assert.That(untouched.Titles[0].Resume, Is.Null, "offering to resume the first second of a film is a no-op "
          + "that looks like a feature");
    }

    [Test]
    public void AReportFromAPersonIsStoredUnderTheirName()
    {
      NameAwareLibrary library = new NameAwareLibrary(
          new List<MediaItem> { TitleWithStoredPosition(PLAYBACK_ID, "042") });

      ActionResult<MediaController.PlaybackResult> action = ReportedBy(library.Library, VIEWER, PLAYBACK_ID, 42);

      Assert.That(Status(action), Is.Null, "a report the library took is a plain 200");
      Assert.That(library.PerUserReports, Is.EqualTo(new List<string> { VIEWER + "|42|True" }),
          "the percentage is what makes a resume offer possible, and it only exists per profile");
      Assert.That(library.SharedReports, Is.Empty,
          "the endpoint must not also write the shared counters here: NotifyUserPlayback calls NotifyPlayback "
          + "itself (MediaLibrary.cs:2809), so calling both would count one viewing twice");
      Assert.That(Body(action).Resume, Is.EqualTo(42), "the answer says what the library now holds for this caller");
    }

    [Test]
    public void AReportFromNobodyStillMovesTheSharedCounters()
    {
      // Nobody has no place to resume from, but a film watched by an anonymous front end is still a film watched,
      // and the shelf's watched state is read from those shared counters (sections ac and ad).
      NameAwareLibrary library = new NameAwareLibrary(
          new List<MediaItem> { TitleWithStoredPosition(PLAYBACK_ID, "042") });

      ActionResult<MediaController.PlaybackResult> action = ReportedBy(library.Library, null, PLAYBACK_ID, 100);

      Assert.That(library.SharedReports, Is.EqualTo(new List<string> { "watched=True" }));
      Assert.That(library.PerUserReports, Is.Empty);
      Assert.That(Body(action).Resume, Is.Null, "a report filed under no profile has no position to hand back");
    }

    #endregion

    #region The titles inside a title

    private static readonly Guid ALBUM_ID = new Guid("aaaaaaaa-0000-4000-8000-000000000a01");
    private static readonly Guid TRACK_ID = new Guid("aaaaaaaa-0000-4000-8000-000000000a02");
    private static readonly Guid SERIES_ID = new Guid("aaaaaaaa-0000-4000-8000-000000000a03");
    private static readonly Guid SEASON_ID = new Guid("aaaaaaaa-0000-4000-8000-000000000a04");
    private static readonly Guid COLLECTION_ID = new Guid("aaaaaaaa-0000-4000-8000-000000000a05");
    private static readonly Guid EPISODE_ID = new Guid("aaaaaaaa-0000-4000-8000-000000000a06");
    private static readonly Guid NOTHING_ID = new Guid("aaaaaaaa-0000-4000-8000-000000000a07");

    /// <summary>
    /// A library that answers a question about one title with one title and every other question with a list.
    /// </summary>
    /// <remarks>
    /// That is the shape a request for the titles inside a title has: the server first reads the title it was
    /// pointed at, to find out whether it is an album or a series or nothing worth opening, and only then asks what
    /// is inside it. A fake that answered both questions the same way could not tell those two reads apart, which is
    /// the whole thing these tests are about.
    /// <para>
    /// Every aspect a shelf of films, a shelf of music or a shelf of episodes is read through is in the metadata,
    /// which is what a household's server has after its first import; the optional list this makes the parent lookup
    /// ask for is then the interesting one.
    /// </para>
    /// </remarks>
    private static IMediaLibrary ContainingLibrary(MediaItem parent, IList<MediaItem> contents, int matches,
        out List<MediaItemQuery> asked, out List<IFilter> countedThrough)
    {
      List<MediaItemQuery> questions = new List<MediaItemQuery>();
      List<IFilter> counts = new List<IFilter>();

      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(
          new Dictionary<Guid, MediaItemAspectMetadata>
          {
              { MediaAspect.ASPECT_ID, MediaAspect.Metadata },
              { MovieAspect.ASPECT_ID, MovieAspect.Metadata },
              { MovieCollectionAspect.ASPECT_ID, MovieCollectionAspect.Metadata },
              { EpisodeAspect.ASPECT_ID, EpisodeAspect.Metadata },
              { SeriesAspect.ASPECT_ID, SeriesAspect.Metadata },
              { SeasonAspect.ASPECT_ID, SeasonAspect.Metadata },
              { AudioAspect.ASPECT_ID, AudioAspect.Metadata },
              { AudioAlbumAspect.ASPECT_ID, AudioAlbumAspect.Metadata }
          });
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false))
          .Callback<MediaItemQuery, bool, Guid?, bool>(
              (query, online, profile, virtualItems) => questions.Add(query))
          .Returns<MediaItemQuery, bool, Guid?, bool>((query, online, profile, virtualItems) =>
              parent != null && query.Filter is MediaItemIdFilter
                  ? new List<MediaItem> { parent }
                  : contents);
      library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), false, false))
          .Callback<IEnumerable<Guid>, IFilter, bool, bool>(
              (necessary, filter, online, virtualItems) => counts.Add(filter))
          .Returns(matches);

      asked = questions;
      countedThrough = counts;
      return library.Object;
    }

    /// <summary>
    /// A title that holds others: the media aspect, and whichever aspect says what kind of container it is.
    /// </summary>
    private static MediaItem CreateParentItem(Guid id, string title, SingleMediaItemAspectMetadata container)
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { CreateMediaAspect(title, title, new DateTime(2024, 1, 1)) } },
          { container.AspectId, new List<MediaItemAspect> { new SingleMediaItemAspect(container) } }
      };
      return new TestItem(id, aspects, null, false);
    }

    /// <summary>One album as the music importer left it: who made it, and how much is on it.</summary>
    private static MediaItem CreateAlbumItem(Guid id, string title, string artist, int tracks)
    {
      SingleMediaItemAspect album = new SingleMediaItemAspect(AudioAlbumAspect.Metadata);
      album.SetAttribute(AudioAlbumAspect.ATTR_ALBUM, title);
      if (artist != null)
        album.SetCollectionAttribute(AudioAlbumAspect.ATTR_ARTISTS, new[] { artist });
      if (tracks > 0)
        album.SetAttribute(AudioAlbumAspect.ATTR_NUMTRACKS, tracks);

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { CreateMediaAspect(title, title, new DateTime(1979, 3, 1)) } },
          { AudioAlbumAspect.ASPECT_ID, new List<MediaItemAspect> { album } }
      };
      return new TestItem(id, aspects, null, false);
    }

    /// <summary>One track: its number, who is credited on it, and how long it plays.</summary>
    private static MediaItem CreateTrackItem(Guid id, string title, int number, string artist, int seconds,
        string path = null)
    {
      SingleMediaItemAspect audio = new SingleMediaItemAspect(AudioAspect.Metadata);
      audio.SetAttribute(AudioAspect.ATTR_TRACKNAME, title);
      if (number > 0)
        audio.SetAttribute(AudioAspect.ATTR_TRACK, number);
      if (artist != null)
        audio.SetCollectionAttribute(AudioAspect.ATTR_ARTISTS, new[] { artist });
      if (seconds > 0)
        audio.SetAttribute(AudioAspect.ATTR_DURATION, (long) seconds);

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { CreateMediaAspect(title, title, new DateTime(1979, 3, 1)) } },
          { AudioAspect.ASPECT_ID, new List<MediaItemAspect> { audio } }
      };
      return new TestItem(id, aspects, path == null ? null : CreateLocator(path), false);
    }

    /// <summary>One episode, numbered the way an importer that knows the season numbers it.</summary>
    private static MediaItem CreateEpisodeItem(Guid id, string title, int season, int number, string path = null)
    {
      SingleMediaItemAspect episode = new SingleMediaItemAspect(EpisodeAspect.Metadata);
      episode.SetAttribute(EpisodeAspect.ATTR_SERIES_NAME, "Alien Earth");
      episode.SetAttribute(EpisodeAspect.ATTR_SEASON, season);
      episode.SetCollectionAttribute(EpisodeAspect.ATTR_EPISODE, new[] { number });

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { CreateMediaAspect(title, title, new DateTime(2025, 8, 5)) } },
          { EpisodeAspect.ASPECT_ID, new List<MediaItemAspect> { episode } }
      };
      return new TestItem(id, aspects, path == null ? null : CreateLocator(path), false);
    }

    [Test]
    public void TheTracksOfAnAlbumAreFoundTheWayMP2FindsThem()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = ContainingLibrary(
          CreateParentItem(ALBUM_ID, "The Dark Side of the Moon", AudioAlbumAspect.Metadata),
          new List<MediaItem>(), 0, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result =
          GetTitlesInside(library, "AudioItem", null, null, ALBUM_ID, out action);

      Assert.That(Status(action), Is.Null, "an album whose tracks are being listed is a normal question");
      Assert.That(asked, Has.Count.EqualTo(2),
          "the server first reads the title it was pointed at, because only the title itself can say it is an album");
      Assert.That(asked[0].Filter, Is.InstanceOf<MediaItemIdFilter>());

      RelationshipFilter link = (RelationshipFilter) asked[1].Filter;
      Assert.That(link.Role, Is.EqualTo(AudioAspect.ROLE_TRACK));
      Assert.That(link.LinkedRole, Is.EqualTo(AudioAlbumAspect.ROLE_ALBUM));
      Assert.That(link.LinkedMediaItemId, Is.EqualTo(ALBUM_ID),
          "the same pair of roles MP2's own screens use to reach an album's music, so the answer is the same list "
          + "the old front end showed");
      Assert.That(result.Inside, Is.EqualTo(ALBUM_ID),
          "and the answer says which title it was read from, so that twelve tracks cannot be mistaken for a library "
          + "of twelve");
    }

    [Test]
    public void TheEpisodesOfASeriesAndOfASeasonAreBothFound()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary series = ContainingLibrary(
          CreateParentItem(SERIES_ID, "Alien Earth", SeriesAspect.Metadata),
          new List<MediaItem>(), 0, out asked, out counted);
      IMediaLibrary season = ContainingLibrary(
          CreateParentItem(SEASON_ID, "Alien Earth S01", SeasonAspect.Metadata),
          new List<MediaItem>(), 0, out List<MediaItemQuery> seasonAsked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      GetTitlesInside(series, "EpisodeItem", null, null, SERIES_ID, out action);
      GetTitlesInside(season, "EpisodeItem", null, null, SEASON_ID, out action);

      RelationshipFilter ofSeries = (RelationshipFilter) asked[1].Filter;
      Assert.That(ofSeries.Role, Is.EqualTo(EpisodeAspect.ROLE_EPISODE));
      Assert.That(ofSeries.LinkedRole, Is.EqualTo(SeriesAspect.ROLE_SERIES));

      RelationshipFilter ofSeason = (RelationshipFilter) seasonAsked[1].Filter;
      Assert.That(ofSeason.LinkedRole, Is.EqualTo(SeasonAspect.ROLE_SEASON),
          "an importer that filed the episodes under their season is not a library the front end cannot browse");
    }

    [Test]
    public void TheFilmsOfACollectionAreFoundThroughTheCollectionRole()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = ContainingLibrary(
          CreateParentItem(COLLECTION_ID, "Alien Collection", MovieCollectionAspect.Metadata),
          new List<MediaItem>(), 0, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      GetTitlesInside(library, "MovieItem", null, null, COLLECTION_ID, out action);

      RelationshipFilter link = (RelationshipFilter) asked[1].Filter;
      Assert.That(link.Role, Is.EqualTo(MovieAspect.ROLE_MOVIE));
      Assert.That(link.LinkedRole, Is.EqualTo(MovieCollectionAspect.ROLE_MOVIE_COLLECTION));
    }

    [Test]
    public void ATitleTheServerDoesNotHaveIsAnsweredAsMissing()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = ContainingLibrary(null, new List<MediaItem>(), 0, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result =
          GetTitlesInside(library, "AudioItem", null, null, NOTHING_ID, out action);

      Assert.That(Status(action), Is.EqualTo(StatusCodes.Status404NotFound),
          "an id that leads nowhere is not a shelf with nothing on it, and a 200 with an empty list is how a broken "
          + "link is forgiven until it is noticed");
      Assert.That(result.Error, Does.Contain(NOTHING_ID.ToString("D")));
      Assert.That(asked, Has.Count.EqualTo(1), "and the server does not go on to ask after the contents");
    }

    [Test]
    public void ATitleThatCannotHoldThisKindOfTitleIsRefusedRatherThanAnsweredWithAnEmptyList()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = ContainingLibrary(
          CreateParentItem(ALBUM_ID, "The Dark Side of the Moon", AudioAlbumAspect.Metadata),
          new List<MediaItem>(), 0, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result =
          GetTitlesInside(library, "MovieItem", null, null, ALBUM_ID, out action);

      Assert.That(Status(action), Is.EqualTo(StatusCodes.Status400BadRequest));
      Assert.That(result.Error, Does.Contain("MovieItem"),
          "the message names the kind that was asked for, because the mistake is in whoever asked");
      Assert.That(asked, Has.Count.EqualTo(1),
          "an empty list would read as \"this album holds no films\", which is a statement about somebody's music");
    }

    [Test]
    public void AQuestionAboutOneAlbumAndAboutUnwatchedTitlesAsksBothAtOnce()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = ContainingLibrary(
          CreateParentItem(ALBUM_ID, "The Dark Side of the Moon", AudioAlbumAspect.Metadata),
          new List<MediaItem>(), 0, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      GetTitlesInside(library, "AudioItem", null, "unwatched", ALBUM_ID, out action);

      BooleanCombinationFilter both = (BooleanCombinationFilter) asked[1].Filter;
      Assert.That(both.Operator, Is.EqualTo(BooleanOperator.And));
      Assert.That(both.Operands.OfType<RelationshipFilter>().Count(), Is.EqualTo(1));

      // The watched half is itself a combination - "counted zero times, or never written at all" - so the question
      // about one album and about unwatched titles is three conditions deep. Nesting is what the library speaks in,
      // and flattening it here would be inventing a filter MP2 cannot ask.
      BooleanCombinationFilter unwatched = both.Operands.OfType<BooleanCombinationFilter>().Single();
      Assert.That(unwatched.Operator, Is.EqualTo(BooleanOperator.Or));
      Assert.That(unwatched.Operands.OfType<RelationalFilter>().Single().AttributeType,
          Is.SameAs(MediaAspect.ATTR_PLAYCOUNT));
      Assert.That(counted[0], Is.SameAs(asked[1].Filter),
          "so the \"3 of 9 tracks\" a screen says is counting the tracks of that album, not the whole library");
    }

    [Test]
    public void AWholeShelfIsNotAskedAboutAnyParent()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = ContainingLibrary(null, new List<MediaItem>(), 0, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result =
          GetTitlesInside(library, "AlbumItem", null, null, Guid.Empty, out action);

      Assert.That(asked, Has.Count.EqualTo(1), "no parent means no read of a parent");
      Assert.That(result.Inside, Is.Null, "and the answer says so, which is how a screen knows it is on the shelf");
    }

    [Test]
    public void AnAlbumRowNamesWhoMadeItAndHowMuchIsOnIt()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = ContainingLibrary(null,
          new List<MediaItem> { CreateAlbumItem(ALBUM_ID, "The Dark Side of the Moon", "Pink Floyd", 10) },
          1, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result =
          GetTitlesInside(library, "AlbumItem", null, null, Guid.Empty, out action);

      MediaController.TitleResult album = result.Titles[0];
      Assert.That(album.Artist, Is.EqualTo("Pink Floyd"));
      Assert.That(album.TrackCount, Is.EqualTo(10));
      Assert.That(album.Path, Is.Null, "an album is not a file, and a row that offered one would be lying");
    }

    [Test]
    public void AnAlbumNobodyTaggedSaysNothingRatherThanNothingKnown()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = ContainingLibrary(null,
          new List<MediaItem> { CreateAlbumItem(ALBUM_ID, "Untitled", null, 0) }, 1, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result =
          GetTitlesInside(library, "AlbumItem", null, null, Guid.Empty, out action);

      Assert.That(result.Titles[0].Artist, Is.Null);
      Assert.That(result.Titles[0].TrackCount, Is.Null,
          "a zero here would be a claim that nothing is on the album, made by reading an empty column");
    }

    [Test]
    public void ATrackRowCarriesItsNumberItsCreditedArtistAndItsLength()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = ContainingLibrary(
          CreateParentItem(ALBUM_ID, "The Dark Side of the Moon", AudioAlbumAspect.Metadata),
          new List<MediaItem> { CreateTrackItem(TRACK_ID, "Time", 4, "Pink Floyd", 204, RESOURCE_PATH) },
          1, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result =
          GetTitlesInside(library, "AudioItem", null, null, ALBUM_ID, out action);

      MediaController.TitleResult track = result.Titles[0];
      Assert.That(track.Track, Is.EqualTo(4), "the number a listener calls a song by");
      Assert.That(track.Artist, Is.EqualTo("Pink Floyd"));
      Assert.That(track.RuntimeMinutes, Is.EqualTo(3), "three minutes and twenty-four seconds is three minutes");
      Assert.That(track.Path, Is.EqualTo(RESOURCE_PATH));
    }

    [Test]
    public void AnEpisodeRowCarriesItsSeasonAndItsNumber()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = ContainingLibrary(
          CreateParentItem(SERIES_ID, "Alien Earth", SeriesAspect.Metadata),
          new List<MediaItem>
          {
              CreateEpisodeItem(EPISODE_ID, "Nimmerland", 1, 1, RESOURCE_PATH),
              CreateEpisodeItem(TRACK_ID, "Specials", 0, 3, RESOURCE_PATH)
          },
          2, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result =
          GetTitlesInside(library, "EpisodeItem", null, null, SERIES_ID, out action);

      Assert.That(result.Titles[0].Season, Is.EqualTo(1));
      Assert.That(result.Titles[0].Episode, Is.EqualTo(1));
      Assert.That(result.Titles[1].Season, Is.Null,
          "a zero in that column is either a special or nothing written, and a row that announced specials about an "
          + "untagged file would be describing somebody's television series out of an empty column");
      Assert.That(result.Titles[1].Episode, Is.EqualTo(3));
    }

    [Test]
    public void TracksAreOrderedByNumberAndEpisodesBySeasonAndThenByNumber()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = ContainingLibrary(
          CreateParentItem(ALBUM_ID, "The Dark Side of the Moon", AudioAlbumAspect.Metadata),
          new List<MediaItem>(), 0, out asked, out counted);
      IMediaLibrary series = ContainingLibrary(
          CreateParentItem(SERIES_ID, "Alien Earth", SeriesAspect.Metadata),
          new List<MediaItem>(), 0, out List<MediaItemQuery> seasonAsked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult tracks = GetTitlesInside(library, "AudioItem", "track", null, ALBUM_ID, out action);
      MediaController.TitlesResult episodes =
          GetTitlesInside(series, "EpisodeItem", "episode", null, SERIES_ID, out action);

      Assert.That(asked[1].SortInformation, Has.Count.EqualTo(1));
      Assert.That(((AttributeSortInformation) asked[1].SortInformation[0]).AttributeType,
          Is.SameAs(AudioAspect.ATTR_TRACK),
          "an album whose third track is listed under B is not the thing a listener sat down for");
      Assert.That(tracks.Sort, Is.EqualTo("track"));

      Assert.That(seasonAsked[1].SortInformation, Has.Count.EqualTo(2),
          "one column would interleave a season two after a season one");
      Assert.That(((AttributeSortInformation) seasonAsked[1].SortInformation[0]).AttributeType,
          Is.SameAs(EpisodeAspect.ATTR_SEASON));
      Assert.That(((AttributeSortInformation) seasonAsked[1].SortInformation[1]).AttributeType,
          Is.SameAs(EpisodeAspect.ATTR_EPISODE));
      Assert.That(episodes.Sort, Is.EqualTo("episode"));
    }

    [Test]
    public void AWholeShelfIsStillSortedByName()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = ContainingLibrary(null, new List<MediaItem>(), 0, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      GetTitlesInside(library, "AudioItem", "track", null, Guid.Empty, out action);

      Assert.That(((AttributeSortInformation) asked[0].SortInformation[0]).AttributeType,
          Is.SameAs(AudioAspect.ATTR_TRACK),
          "a shelf of every track in the library by number is a strange order, but it is the one that was asked "
          + "for, and the answer says so");

      GetTitlesInside(library, "AudioItem", null, null, Guid.Empty, out action);
      Assert.That(((AttributeSortInformation) asked[1].SortInformation[0]).AttributeType,
          Is.SameAs(MediaAspect.ATTR_SORT_TITLE), "and nothing was asked, so the name order stands");
    }

    #endregion
  }
}
