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
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using NUnit.Framework;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Certifications;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Common.Settings;
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
      if (ServiceRegistration.IsRegistered<IFanArtCache>())
        ServiceRegistration.Remove<IFanArtCache>();
      if (ServiceRegistration.IsRegistered<IMediaAccessor>())
        ServiceRegistration.Remove<IMediaAccessor>();
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
    /// Asks the list endpoint for one season of a series by number. This is how the shelf inside a season the
    /// library only counts is opened: the series' id and the number, because there is no id of that season's own to
    /// look inside of.
    /// </summary>
    private static MediaController.TitlesResult GetTitlesOfSeason(IMediaLibrary library, string aspect,
        Guid series, int season, out ActionResult<MediaController.TitlesResult> action)
    {
      ServiceRegistration.Set<IMediaLibrary>(library);
      action = new MediaController().GetTitles(aspect, null, null, 0, 0, null, null, series, null, null, season);
      return Body(action);
    }

    /// <summary>The details endpoint asked the same way: one season of the title the id names.</summary>
    private static MediaController.ItemResult GetItemOfSeason(IMediaLibrary library, Guid series, int season,
        out ActionResult<MediaController.ItemResult> action)
    {
      ServiceRegistration.Set<IMediaLibrary>(library);
      action = new MediaController().GetItem(series, season);
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
    public void TheViewerCanSortByWhenTheLibraryFirstSawTheTitle()
    {
      List<MediaItemQuery> queries;
      IMediaLibrary library = CreateLibrary(new List<MediaItem>(), 0, out queries, out _);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(library, "MovieItem", "added", "desc", 0, 0, out action);

      AttributeSortInformation sort = (AttributeSortInformation)queries[0].SortInformation[0];
      Assert.That(sort.AttributeType, Is.SameAs(ImporterAspect.ATTR_DATEADDED),
          "the date the library first saw the file - a fact about this library, not about the film, and the only "
          + "column that answers 'what arrived lately'; the media library stamps it itself when it stores a title");
      Assert.That(sort.Direction, Is.EqualTo(SortDirection.Descending),
          "newest arrivals first, which is the only order a 'latest media' list wants");
      Assert.That(result.Sort, Is.EqualTo("added descending"),
          "and the echo distinguishes it from the year, which is a different question");
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

    /// <summary>
    /// Stands one answer for the household's own switches in front of the server, for the length of a test.
    /// </summary>
    /// <remarks>
    /// The media endpoints read these switches through the settings manager, which a media test has no reason
    /// to have registered - and which is exactly why the ordinary tests of this fixture prove the ordinary
    /// answer: no manager, so no wish, so the library is asked the way it has always been asked.
    /// </remarks>
    private static void TheHouseholdWishes<Answer>(Answer answer) where Answer : class
    {
      Mock<ISettingsManager> manager = new Mock<ISettingsManager>();
      manager.Setup(load => load.Load<Answer>()).Returns(answer);
      // Asked by its type rather than by its generic name is the same answer; anything else the server asks
      // for comes back as a class of its own, which is what "nobody ever answered that" looks like.
      manager.Setup(load => load.Load(It.IsAny<Type>()))
          .Returns<Type>(type => type == typeof(Answer) ? answer : Activator.CreateInstance(type));
      ServiceRegistration.Set<ISettingsManager>(manager.Object);
    }

    /// <summary>Takes the household's wish back away, so the next test starts from "nobody answered".</summary>
    private static void NobodyWishesAnything()
    {
      if (ServiceRegistration.IsRegistered<ISettingsManager>())
        ServiceRegistration.Remove<ISettingsManager>();
    }

    [Test]
    public void TheFilmsWithoutFilesComeIntoALibraryWhenTheHouseholdAskedForThem()
    {
      TheHouseholdWishes(new VirtualMediaSettings { ShowVirtualMovieMedia = true });
      try
      {
        List<bool> searched = new List<bool>();
        List<bool> counted = new List<bool>();
        var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
        library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(new Dictionary<Guid, MediaItemAspectMetadata>
        {
            { MediaAspect.ASPECT_ID, MediaAspect.Metadata },
            { MovieAspect.ASPECT_ID, MovieAspect.Metadata }
        });

        // Only the question the switch is supposed to change is answered with an "any": everything else about
        // the call is pinned, so an endpoint that turned the switch into some other argument fails here.
        library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, true))
            .Callback<MediaItemQuery, bool, Guid?, bool>((query, online, profile, virtualItems) =>
                searched.Add(virtualItems))
            .Returns(new List<MediaItem>());
        library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), null, false, true))
            .Callback<IEnumerable<Guid>, IFilter, bool, bool>((necessary, filter, online, virtualItems) =>
                counted.Add(virtualItems))
            .Returns(0);

        ServiceRegistration.Set<IMediaLibrary>(library.Object);
        ActionResult<MediaController.TitlesResult> action =
            new MediaController().GetTitles("MovieItem", null, null, 0, 0, null, null);

        Assert.That(Status(action), Is.Null, "the shelf was answered");
        Assert.That(searched, Is.Not.Empty.And.All.EqualTo(true),
            "a browse about films carries the films switch to the library");
        Assert.That(counted, Is.Not.Empty.And.All.EqualTo(true),
            "and the count of that shelf cannot disagree with the shelf itself");
      }
      finally
      {
        NobodyWishesAnything();
      }
    }

    [Test]
    public void AQuestionAboutOneKindOfMediaCarriesThatKindsAnswerAndNoOther()
    {
      // The series switch is the one that is on. A browse about films has to be answered without the invented
      // films, because a household that wants its missing episodes has not asked for missing films - and the
      // strict library here has no answer at all for a search carrying the films' answer.
      TheHouseholdWishes(new VirtualMediaSettings { ShowVirtualSeriesMedia = true });
      try
      {
        List<bool> searched = new List<bool>();
        var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
        library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(new Dictionary<Guid, MediaItemAspectMetadata>
        {
            { MediaAspect.ASPECT_ID, MediaAspect.Metadata },
            { MovieAspect.ASPECT_ID, MovieAspect.Metadata },
            { EpisodeAspect.ASPECT_ID, EpisodeAspect.Metadata }
        });
        library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false))
            .Callback<MediaItemQuery, bool, Guid?, bool>((query, online, profile, virtualItems) =>
                searched.Add(virtualItems))
            .Returns(new List<MediaItem>());
        library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, true))
            .Callback<MediaItemQuery, bool, Guid?, bool>((query, online, profile, virtualItems) =>
                searched.Add(virtualItems))
            .Returns(new List<MediaItem>());
        library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), null, false, false)).Returns(0);
        library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), null, false, true)).Returns(0);

        ServiceRegistration.Set<IMediaLibrary>(library.Object);
        ActionResult<MediaController.TitlesResult> films =
            new MediaController().GetTitles("MovieItem", null, null, 0, 0, null, null);
        Assert.That(Status(films), Is.Null, "the films were answered");

        ActionResult<MediaController.TitlesResult> episodes =
            new MediaController().GetTitles("EpisodeItem", null, null, 0, 0, null, null);
        Assert.That(Status(episodes), Is.Null, "the episodes were answered");

        Assert.That(searched, Has.Count.EqualTo(2));
        Assert.That(searched[0], Is.False,
            "the films were asked about, and the films switch - which nobody turned on - decides about them");
        Assert.That(searched[1], Is.True,
            "the episodes were asked about, and that is the switch the household did turn on");
      }
      finally
      {
        NobodyWishesAnything();
      }
    }

    [Test]
    public void AnInventedTitleIsOpenFromTheShelfItWasShownOn()
    {
      // A browse that let an invented film onto the shelf has to let the same title be read by its id, or the
      // viewer gets a cover that refuses to open. The lookup by id cannot know the kind before it has the
      // title, so it asks whether invented titles are welcome anywhere here.
      TheHouseholdWishes(new VirtualMediaSettings { ShowVirtualSeriesMedia = true });
      try
      {
        Guid id = Guid.NewGuid();
        MediaItem item = CreateDetailedItem(id, "The Missing One", "", "", "Alien Earth", 2, 7, "The Missing One");

        var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
        // Answered only for a lookup that lets invented titles in. Nothing in this fixture sets that switch
        // except this test, so the setup itself is the assertion.
        library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, true))
            .Returns(new List<MediaItem> { item });

        ActionResult<MediaController.ItemResult> action;
        MediaController.ItemResult result = GetItem(library.Object, id, out action);

        Assert.That(Status(action), Is.Null, "the title the shelf showed is the title the details answer");
        Assert.That(result.Title, Is.EqualTo("The Missing One"));
      }
      finally
      {
        NobodyWishesAnything();
      }
    }

    [Test]
    public void TheCertificateOfAFilmIsSpelledInTheCountryTheHouseholdNamed()
    {
      MediaCertificationSettings countries = new MediaCertificationSettings
      {
        DisplayMovieCertificationCountry = "DE"
      };
      TheHouseholdWishes(countries);
      try
      {
        Guid id = Guid.NewGuid();
        MediaItem item = CreateCertifiedItem(id, "Alien Earth", "R");

        ActionResult<MediaController.ItemResult> action;
        MediaController.ItemResult result = GetItem(
            CreateLibrary(new List<MediaItem> { item }, 1, out _, out _), id, out action);

        Assert.That(result.Certification, Is.EqualTo("FSK 18"),
            "\"R\" asks for seventeen years, and the German mark that asks for at least as many is FSK 18");
      }
      finally
      {
        NobodyWishesAnything();
      }
    }

    [Test]
    public void TheCertificateOfASeriesIsSpelledInTheCountryTheHouseholdNamed()
    {
      MediaCertificationSettings countries = new MediaCertificationSettings
      {
        DisplaySeriesCertificationCountry = "DE"
      };
      TheHouseholdWishes(countries);
      try
      {
        Guid id = Guid.NewGuid();
        MediaItem item = CreateCertifiedItem(id, "Alien Earth", "TV-14");

        ActionResult<MediaController.ItemResult> action;
        MediaController.ItemResult result = GetItem(
            CreateLibrary(new List<MediaItem> { item }, 1, out _, out _), id, out action);

        Assert.That(result.Certification, Is.EqualTo("FSK 16"),
            "a show's mark is read off the show and turned into the country's own words the same way");
      }
      finally
      {
        NobodyWishesAnything();
      }
    }

    [Test]
    public void ACertificateNobodyCanAnswerForIsLeftAsTheImporterWroteIt()
    {
      // The household named a country the table holds no marks for. There is nothing to compare the stored one
      // with, and inventing an age for a title the importer rated elsewhere is the one thing a certificate
      // must not do - so the words the library holds are shown rather than no words, or a wrong one.
      MediaCertificationSettings countries = new MediaCertificationSettings
      {
        DisplayMovieCertificationCountry = "ZZ"
      };
      TheHouseholdWishes(countries);
      try
      {
        Guid id = Guid.NewGuid();
        MediaItem item = CreateCertifiedItem(id, "Alien Earth", "PG-13");

        ActionResult<MediaController.ItemResult> action;
        MediaController.ItemResult result = GetItem(
            CreateLibrary(new List<MediaItem> { item }, 1, out _, out _), id, out action);

        Assert.That(result.Certification, Is.EqualTo("PG-13"),
            "the mark the title came in with, because no mark anywhere else says the same");
      }
      finally
      {
        NobodyWishesAnything();
      }
    }

    [Test]
    public void ACertificateOfTheCountryTheHouseholdNamedStandsUntouched()
    {
      MediaCertificationSettings countries = new MediaCertificationSettings
      {
        DisplayMovieCertificationCountry = "DE"
      };
      TheHouseholdWishes(countries);
      try
      {
        Guid id = Guid.NewGuid();
        MediaItem item = CreateCertifiedItem(id, "Alien Earth", "FSK 12");

        ActionResult<MediaController.ItemResult> action;
        MediaController.ItemResult result = GetItem(
            CreateLibrary(new List<MediaItem> { item }, 1, out _, out _), id, out action);

        Assert.That(result.Certification, Is.EqualTo("FSK 12"),
            "a mark already in the asked-for country is not walked through the table and back");
      }
      finally
      {
        NobodyWishesAnything();
      }
    }

    /// <summary>
    /// A title carrying one certificate and nothing else of the kind, so that a test can say which of the two
    /// the endpoint read: a film carries it on the <see cref="MovieAspect"/>, a show on
    /// <see cref="SeriesAspect"/>.
    /// </summary>
    private static MediaItem CreateCertifiedItem(Guid id, string title, string certification)
    {
      SingleMediaItemAspect media = CreateMediaAspect(title, title, new DateTime(2025, 4, 27, 20, 15, 0));

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } }
      };

      if (certification.StartsWith("TV", StringComparison.Ordinal))
      {
        SingleMediaItemAspect series = new SingleMediaItemAspect(SeriesAspect.Metadata);
        series.SetAttribute(SeriesAspect.ATTR_SERIES_NAME, title);
        series.SetAttribute(SeriesAspect.ATTR_CERTIFICATION, certification);
        aspects[SeriesAspect.ASPECT_ID] = new List<MediaItemAspect> { series };
      }
      else
      {
        SingleMediaItemAspect movie = CreateMovieAspect(52);
        movie.SetAttribute(MovieAspect.ATTR_CERTIFICATION, certification);
        aspects[MovieAspect.ASPECT_ID] = new List<MediaItemAspect> { movie };
      }

      return new TestItem(id, aspects, CreateLocator(RESOURCE_PATH));
    }

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

    /// <summary>
    /// A show answers with the name it keeps on its own aspect, which is the yearless one the importers write, and
    /// not with the library's title column, which carries the year welded on.  The reference's cover-details
    /// templates bound exactly that field (<c>SeriesAspectWrapper.SeriesName</c>), and a details screen that is
    /// handed the title column's spelling says the year twice - once above its facts and once in them.
    /// </summary>
    [Test]
    public void AShowAnswersWithTheNameItsOwnAspectKeeps()
    {
      Guid id = Guid.NewGuid();
      SingleMediaItemAspect media = CreateMediaAspect("Alien Earth (2025)", "Alien Earth (2025)",
          new DateTime(2025, 1, 1));
      SingleMediaItemAspect show = new SingleMediaItemAspect(SeriesAspect.Metadata);
      show.SetAttribute(SeriesAspect.ATTR_SERIES_NAME, "Alien: Earth");
      MediaItem item = new TestItem(id, new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } },
          { SeriesAspect.ASPECT_ID, new List<MediaItemAspect> { show } }
      }, null, false);

      ActionResult<MediaController.ItemResult> action;
      MediaController.ItemResult result = GetItem(
          CreateLibrary(new List<MediaItem> { item }, 1, out _, out _), id, out action);

      Assert.That(Status(action), Is.Null, "the item was found, so the answer is a plain body");
      Assert.That(result.Title, Is.EqualTo("Alien Earth (2025)"),
          "the title column is answered as the library keeps it - the name is added to the answer, not rewritten "
          + "out of the title");
      Assert.That(result.SeriesName, Is.EqualTo("Alien: Earth"),
          "the show's own aspect says its name, punctuation and all, and that is the name a details screen has to "
          + "be given because the year already stands in the answer's own fields");
    }

    /// <summary>
    /// A series as an importer that wrote summaries leaves it: the show aspect and its sentence on the media
    /// aspect, which is where this endpoint reads every title's prose from.
    /// </summary>
    private static MediaItem CreateDescribedSeries(Guid id, string prose)
    {
      SingleMediaItemAspect media = CreateMediaAspect("Alien Earth", "Alien Earth", new DateTime(2025, 1, 1));
      if (prose != null)
        media.SetAttribute(MediaAspect.ATTR_COMMENT, prose);
      SingleMediaItemAspect seriesAspect = new SingleMediaItemAspect(SeriesAspect.Metadata);
      seriesAspect.SetAttribute(SeriesAspect.ATTR_SERIES_NAME, "Alien Earth");
      return new TestItem(id, new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } },
          { SeriesAspect.ASPECT_ID, new List<MediaItemAspect> { seriesAspect } }
      }, null, false);
    }

    /// <summary>
    /// An episode file tagged with a number but no summary, under a series whose importer did write one: the
    /// shape the details endpoint has to answer when the viewer stands in front of an episode of a described show.
    /// </summary>
    /// <remarks>
    /// The two questions are answered apart because the endpoint asks them apart - the title by its id, and the
    /// series above it by walking the importer's relationship backwards - and a single answer for both would let
    /// a borrow that never asked the relationship anything pass as one that did.
    /// </remarks>
    private static IMediaLibrary EpisodeUnderDescribedSeries(Guid episodeId, string seriesProse,
        out List<MediaItemQuery> asked)
    {
      MediaItem bare = CreateEpisodeItem(episodeId, "Nimmerland", 1, 1);
      MediaItem show = CreateDescribedSeries(Guid.NewGuid(), seriesProse);

      List<MediaItemQuery> questions = new List<MediaItemQuery>();
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(
          new Dictionary<Guid, MediaItemAspectMetadata>
          {
              { MediaAspect.ASPECT_ID, MediaAspect.Metadata },
              { EpisodeAspect.ASPECT_ID, EpisodeAspect.Metadata },
              { SeriesAspect.ASPECT_ID, SeriesAspect.Metadata },
              { SeasonAspect.ASPECT_ID, SeasonAspect.Metadata }
          });
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false))
          .Callback<MediaItemQuery, bool, Guid?, bool>(
              (query, online, profile, virtualItems) => questions.Add(query))
          .Returns<MediaItemQuery, bool, Guid?, bool>((query, online, profile, virtualItems) =>
              query.Filter is MediaItemIdFilter
                  ? new List<MediaItem> { bare }
                  : new List<MediaItem> { show });
      library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), false, false))
          .Returns(1);

      asked = questions;
      return library.Object;
    }

    [Test]
    public void ADescribedSeriesLendsItsSentenceToAnEpisodeThatCarriesNone()
    {
      Guid episodeId = Guid.NewGuid();
      List<MediaItemQuery> asked;
      ActionResult<MediaController.ItemResult> action;
      MediaController.ItemResult result = GetItem(
          EpisodeUnderDescribedSeries(episodeId, "A family on a distant world.", out asked), episodeId, out action);

      Assert.That(Status(action), Is.Null);
      Assert.That(result.Overview, Is.Null,
          "the lent sentence is not answered as the episode's own plot; the episode stays as bare as the importer "
          + "left it, and the series' words arrive under the series' own field");
      Assert.That(result.SeriesOverview, Is.EqualTo("A family on a distant world."),
          "a details screen with nothing to say about an episode is the screen the viewer came to the series for, "
          + "and the series' sentence beats admitting that nothing was written");

      Assert.That(asked, Has.Count.EqualTo(2));
      RelationshipFilter link = (RelationshipFilter) asked[1].Filter;
      Assert.That(link.Role, Is.EqualTo(SeriesAspect.ROLE_SERIES));
      Assert.That(link.LinkedRole, Is.EqualTo(EpisodeAspect.ROLE_EPISODE));
      Assert.That(link.LinkedMediaItemId, Is.EqualTo(episodeId),
          "the series is reached by walking the importer's own relationship backwards from this very episode, "
          + "so the lent words are the words of this episode's series and no other");
    }

    [Test]
    public void AnEpisodeWithItsOwnSummaryAsksNothingBesideIt()
    {
      Guid id = Guid.NewGuid();
      MediaItem described = CreateDetailedItem(id, "Nimmerland", "Troops wake up a century too late.", "",
          "Alien Earth", 1, 1, "Nimmerland");

      List<MediaItemQuery> queries;
      ActionResult<MediaController.ItemResult> action;
      MediaController.ItemResult result = GetItem(
          CreateLibrary(new List<MediaItem> { described }, 1, out queries, out _), id, out action);

      Assert.That(result.SeriesOverview, Is.Null, "a title that speaks for itself borrows nothing");
      Assert.That(queries, Has.Count.EqualTo(1),
          "and the borrowing means one library question for every title that has prose, so that asking it there "
          + "would double the cost of describing the described half of the library");
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

    /// <summary>
    /// One season as MP2's series importer leaves it (<c>SeasonInfo.SetMetadata:239-243</c>): the media aspect
    /// carrying the name the importer built - <c>"&lt;series&gt; (&lt;year&gt;) S&lt;nn&gt;"</c>, which is a name
    /// written for a list of every season in the house - and the season aspect carrying the number, the series,
    /// how many episodes were counted and the prose. None of those four are on the media aspect.
    /// </summary>
    private static MediaItem CreateSeasonItem(Guid id, string title, string seriesName, int season, int episodes,
        string description)
    {
      SingleMediaItemAspect seasonAspect = new SingleMediaItemAspect(SeasonAspect.Metadata);
      if (seriesName != null)
        seasonAspect.SetAttribute(SeasonAspect.ATTR_SERIES_NAME, seriesName);
      if (season > 0)
        seasonAspect.SetAttribute(SeasonAspect.ATTR_SEASON, season);
      if (episodes > 0)
        seasonAspect.SetAttribute(SeasonAspect.ATTR_NUM_EPISODES, episodes);
      if (description != null)
        seasonAspect.SetAttribute(SeasonAspect.ATTR_DESCRIPTION, description);

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { CreateMediaAspect(title, title, new DateTime(2007, 9, 13)) } },
          { SeasonAspect.ASPECT_ID, new List<MediaItemAspect> { seasonAspect } }
      };
      return new TestItem(id, aspects, null, false);
    }

    [Test]
    public void TheSeasonsOfASeriesAreFoundThroughTheSeasonRole()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = ContainingLibrary(
          CreateParentItem(SERIES_ID, "Alien Earth", SeriesAspect.Metadata),
          new List<MediaItem>(), 0, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result =
          GetTitlesInside(library, "SeasonItem", null, null, SERIES_ID, out action);

      Assert.That(Status(action), Is.Null,
          "a season is a title MP2 files in its own right - it has its own aspect, its own role and its own "
          + "relationship extractor - so a viewer standing on a series and pressing into it is asking a question "
          + "this server has an answer to, not one no library can hold");
      Assert.That(asked[1].Filter, Is.InstanceOf<RelationshipFilter>());
      RelationshipFilter link = (RelationshipFilter) asked[1].Filter;
      Assert.That(link.Role, Is.EqualTo(SeasonAspect.ROLE_SEASON));
      Assert.That(link.LinkedRole, Is.EqualTo(SeriesAspect.ROLE_SERIES),
          "the pair of roles MP2's own extractor writes the season-to-series link with "
          + "(SeasonSeriesRelationshipExtractor.cs:58,68), which is a different pair of roles from the one that "
          + "same series' episodes hang on the season by - asking for one when the other was meant would list "
          + "episodes under a heading that says seasons");
      Assert.That(link.LinkedMediaItemId, Is.EqualTo(SERIES_ID));
      Assert.That(result.Inside, Is.EqualTo(SERIES_ID));
    }

    [Test]
    public void AShelfOfSeasonsIsSortedByTheNumberTheImporterWrote()
    {
      List<MediaItemQuery> asked;
      List<IFilter> counted;
      IMediaLibrary library = ContainingLibrary(null, new List<MediaItem>(), 0, out asked, out counted);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(library, "SeasonItem", "season", null, 0, 0, out action);

      AttributeSortInformation sort = (AttributeSortInformation) asked[0].SortInformation[0];
      Assert.That(sort.AttributeType, Is.SameAs(SeasonAspect.ATTR_SEASON),
          "a season is watched in the order it was made, and the number the importer wrote is the only column "
          + "that says so: the name MP2 builds for a season begins with the series, so sorting seasons by name "
          + "would leave the viewer reading one series' whole shelf to find season three and would put season ten "
          + "between one and two");
      Assert.That(sort.Direction, Is.EqualTo(SortDirection.Ascending));
      Assert.That(result.Sort, Is.EqualTo("season"));
    }

    [Test]
    public void ASeasonRowCarriesItsNumberAndHowManyEpisodesItHolds()
    {
      MediaItem season = CreateSeasonItem(Guid.NewGuid(), "Bones (2005) S03", "Bones", 3, 15,
          "The team adjusts to running the lab without its lead.");
      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitles(
          ContainingLibrary(null, new List<MediaItem> { season }, 1, out _, out _),
          "SeasonItem", null, null, 0, 0, out action);

      Assert.That(result.Titles, Has.Count.EqualTo(1));
      MediaController.TitleResult row = result.Titles[0];
      Assert.That(row.Season, Is.EqualTo(3),
          "the number is on the season aspect rather than in the name, and a screen that read it out of the name "
          + "would be parsing a string the importer is free to rewrite");
      Assert.That(row.Episodes, Is.EqualTo(15),
          "how much is inside is the one thing a season has to say about itself, which is what an album's row "
          + "answers with its tracks (SeasonAspect.NUM_EPISODES, the same figure the reference's season panel drew)");

      MediaItem unnumbered = CreateSeasonItem(Guid.NewGuid(), "Alien Earth", "Alien Earth", 0, 0, null);
      MediaController.TitleResult blank = GetTitles(
          ContainingLibrary(null, new List<MediaItem> { unnumbered }, 1, out _, out _),
          "SeasonItem", null, null, 0, 0, out action).Titles[0];
      Assert.That(blank.Season, Is.Null,
          "a season the importer never numbered is answered as nothing rather than as season nought, which a "
          + "screen would draw as a season that does not exist");
      Assert.That(blank.Episodes, Is.Null);
    }

    [Test]
    public void ASeasonAnswersWithItsSeriesItsNumberAndItsEpisodes()
    {
      Guid id = Guid.NewGuid();
      MediaItem season = CreateSeasonItem(id, "Bones (2005) S03", "Bones", 3, 15,
          "The team adjusts to running the lab without its lead.");

      ActionResult<MediaController.ItemResult> action;
      MediaController.ItemResult result = GetItem(
          CreateLibrary(new List<MediaItem> { season }, 1, out _, out _), id, out action);

      Assert.That(result.SeriesName, Is.EqualTo("Bones"),
          "a season belongs to a series, and the series' name is on the season aspect - the episode aspect it "
          + "would normally come from is not on this item at all");
      Assert.That(result.Season, Is.EqualTo(3));
      Assert.That(result.Episodes, Is.EqualTo(15));
      Assert.That(result.Episode, Is.Null, "a season is not an episode and has no number inside itself");
      Assert.That(result.Overview, Is.EqualTo("The team adjusts to running the lab without its lead."),
          "what the reference's season panel showed as its description is on the season aspect "
          + "(SeasonAspect.ATTR_DESCRIPTION), and a details screen with nothing to say about a season is the "
          + "screen the viewer came to the season for");
    }

    /// <summary>
    /// A series and its episodes and no season titles at all, which is what a library looks like when it was
    /// pointed straight at the episode files: whatever tagged them wrote a season number onto every episode and
    /// never made a title of it, so the link that a shelf of seasons is read through exists and answers nothing.
    /// </summary>
    /// <remarks>
    /// The two questions are answered apart, and that separation is the point of the fixture rather than an
    /// accident of it: a shelf asked through the season aspect comes back empty - which is the situation these
    /// tests are about - while the episodes the counting falls back on come back through their own aspect. One
    /// list for both would make "no seasons" and "no episodes" the same state, and a test built on it could not
    /// tell a server that counted the episodes from one that simply had nothing to say.
    /// </remarks>
    private static IMediaLibrary EpisodesOnlyLibrary(string seriesName, IList<MediaItem> episodes,
        out Guid series, out List<MediaItemQuery> asked)
    {
      Guid id = Guid.NewGuid();
      List<MediaItemQuery> questions = new List<MediaItemQuery>();
      MediaItem parent = CreateParentItem(id, seriesName, SeriesAspect.Metadata);

      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(
          new Dictionary<Guid, MediaItemAspectMetadata>
          {
              { MediaAspect.ASPECT_ID, MediaAspect.Metadata },
              { EpisodeAspect.ASPECT_ID, EpisodeAspect.Metadata },
              { SeriesAspect.ASPECT_ID, SeriesAspect.Metadata },
              { SeasonAspect.ASPECT_ID, SeasonAspect.Metadata }
          });
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false))
          .Callback<MediaItemQuery, bool, Guid?, bool>(
              (query, online, profile, virtualItems) => questions.Add(query))
          .Returns<MediaItemQuery, bool, Guid?, bool>((query, online, profile, virtualItems) =>
              query.Filter is MediaItemIdFilter
                  ? new List<MediaItem> { parent }
                  : query.NecessaryRequestedMIATypeIDs.Contains(SeasonAspect.ASPECT_ID)
                      ? new List<MediaItem>()
                      : new List<MediaItem>(episodes));
      library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), false, false))
          .Returns<IEnumerable<Guid>, IFilter, bool, bool>((necessary, filter, online, virtualItems) =>
              necessary.Contains(SeasonAspect.ASPECT_ID) ? 0 : episodes.Count);

      series = id;
      asked = questions;
      return library.Object;
    }

    /// <summary>Four episodes of one series as an outside tool leaves them: seasons one, two and five.</summary>
    private static IList<MediaItem> CountedEpisodes()
    {
      return new List<MediaItem>
      {
          CreateEpisodeItem(Guid.NewGuid(), "Buried", 1, 1),
          CreateEpisodeItem(Guid.NewGuid(), "Confrontation", 1, 2),
          CreateEpisodeItem(Guid.NewGuid(), "Reconvene", 2, 1),
          CreateEpisodeItem(Guid.NewGuid(), "Finale", 5, 1)
      };
    }

    [Test]
    public void TheSeasonsOfASeriesAreCountedOffItsEpisodesWhenNoSeasonTitleExists()
    {
      Guid series;
      List<MediaItemQuery> asked;
      IMediaLibrary library = EpisodesOnlyLibrary("Prison Break (2005)", CountedEpisodes(), out series, out asked);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result =
          GetTitlesInside(library, "SeasonItem", "season", null, series, out action);

      Assert.That(Status(action), Is.Null,
          "a viewer standing on a series and pressing into it is asking a question with an answer, even in the "
          + "commonest library where that answer is not a title of its own anywhere");
      Assert.That(result.Titles, Has.Count.EqualTo(3),
          "three different season numbers were worn by the four episodes, so three rows are what the shelf holds");
      Assert.That(result.Total, Is.EqualTo(3),
          "the count is of the shelf that was made, not of the episodes the counting read");

      Assert.That(result.Titles[0].Season, Is.EqualTo(1));
      Assert.That(result.Titles[0].Episodes, Is.EqualTo(2),
          "a counted season says how much is inside it the way a season the library holds does, because that is "
          + "the line the reference's season row showed under the number");
      Assert.That(result.Titles[0].Title, Is.EqualTo("Prison Break (2005) S01"),
          "named as the reference names a season (SeasonInfo.cs:54 SEASON_FORMAT_STR = \"{0} S{1:00}\"), since a "
          + "counted row is shown to a caller that has no other name to read");

      Assert.That(result.Titles[1].Season, Is.EqualTo(2));
      Assert.That(result.Titles[1].Episodes, Is.EqualTo(1));

      Assert.That(result.Titles[2].Season, Is.EqualTo(5),
          "a series whose library holds seasons one, two and five shows three rows: seasons are counted, not "
          + "numbered up to the highest, and a season nobody tagged is not invented");
      Assert.That(result.Titles[2].Title, Is.EqualTo("Prison Break (2005) S05"));

      RelationshipFilter link = (RelationshipFilter) asked[asked.Count - 1].Filter;
      Assert.That(link.Role, Is.EqualTo(EpisodeAspect.ROLE_EPISODE));
      Assert.That(link.LinkedRole, Is.EqualTo(SeriesAspect.ROLE_SERIES),
          "the counting reads the series' episodes through the pair of roles the importer filed them with "
          + "(EpisodeSeasonRelationshipExtractor.cs:41 writes the episode-to-series link), which is the only road "
          + "from a series to its episode numbers in a library like this");
    }

    [Test]
    public void ACountedSeasonSaysWhichSeriesToOpenItInAndStaysTheSameRowTheNextTime()
    {
      Guid series;
      List<MediaItemQuery> asked;
      IMediaLibrary library = EpisodesOnlyLibrary("Prison Break (2005)", CountedEpisodes(), out series, out asked);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult first =
          GetTitlesInside(library, "SeasonItem", "season", null, series, out action);
      MediaController.TitlesResult again =
          GetTitlesInside(library, "SeasonItem", "season", null, series, out action);

      Assert.That(first.Titles[0].ParentId, Is.EqualTo(series),
          "a counted season has no identity of its own, so its row carries the two parts that do exist - the "
          + "series and the number - and says which of them is where the shelf inside it has to be asked from");
      Assert.That(first.Titles[0].Season, Is.EqualTo(1));
      Assert.That(again.Titles[0].ParentId, Is.EqualTo(series));

      Assert.That(again.Titles[0].Id, Is.EqualTo(first.Titles[0].Id),
          "the key of a counted season is made from its series and number rather than invented afresh per request, "
          + "because the screen holds on to it between reads: it is what its rows are told apart by, what a picture "
          + "is cached under and what the row the viewer left the screen on is found again by");
      Assert.That(again.Titles[1].Id, Is.EqualTo(first.Titles[1].Id));

      Assert.That(first.Titles[0].Id, Is.Not.EqualTo(first.Titles[1].Id),
          "two seasons of one series cannot be handed the same key, or a screen would paint one row's picture on "
          + "the other and select the wrong one");
      Assert.That(first.Titles[0].Id, Is.Not.EqualTo(series),
          "and a counted season is not passed off as its own series, which would have the shelf inside a series "
          + "open the series itself");
    }

    [Test]
    public void TheSeasonNoughtOfASeriesIsTheSpecialsAndKeepsItsOwnRow()
    {
      Guid series;
      List<MediaItemQuery> asked;
      IMediaLibrary library = EpisodesOnlyLibrary("Prison Break (2005)", new List<MediaItem>
      {
          CreateEpisodeItem(Guid.NewGuid(), "The Road to Paradise", 0, 1),
          CreateEpisodeItem(Guid.NewGuid(), "J-Two", 0, 2),
          CreateEpisodeItem(Guid.NewGuid(), "Buried", 1, 1)
      }, out series, out asked);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result =
          GetTitlesInside(library, "SeasonItem", "season", null, series, out action);

      Assert.That(result.Titles, Has.Count.EqualTo(2));
      Assert.That(result.Titles[0].Season, Is.EqualTo(0),
          "nought is MP2's spelling of the specials - it is the number an importer that named nothing writes and "
          + "the number whose pictures are tagged season-specials-* (LocalFanArtHelper.GetAdditionalSeasonPrefixes), "
          + "so it stays a row of its own and sorts in front of season one");
      Assert.That(result.Titles[0].Episodes, Is.EqualTo(2));
      Assert.That(result.Titles[0].Title, Is.EqualTo("Prison Break (2005) S00"));
      Assert.That(result.Titles[1].Season, Is.EqualTo(1));
    }

    [Test]
    public void OpeningACountedSeasonNarrowsTheSeriesEpisodesToThatNumber()
    {
      Guid series;
      List<MediaItemQuery> asked;
      IMediaLibrary library = EpisodesOnlyLibrary("Prison Break (2005)", CountedEpisodes(), out series, out asked);

      ActionResult<MediaController.TitlesResult> action;
      GetTitlesOfSeason(library, "EpisodeItem", series, 2, out action);

      Assert.That(Status(action), Is.Null,
          "the row of a counted season is opened by the screen exactly as any other row is, with the two parts it "
          + "was given: the series' id and the number");
      BooleanCombinationFilter both = (BooleanCombinationFilter) asked[1].Filter;
      RelationalFilter numbered = both.Operands.OfType<RelationalFilter>().Single();
      Assert.That(numbered.AttributeType, Is.SameAs(EpisodeAspect.ATTR_SEASON),
          "the number the row was built from is asked back as the attribute the episodes carry it in, which is the "
          + "only place it is written in a library like this");
      Assert.That(numbered.Operator, Is.EqualTo(RelationalOperator.EQ));
      Assert.That(numbered.FilterValue, Is.EqualTo(2));

      RelationshipFilter link = both.Operands.OfType<RelationshipFilter>().Single();
      Assert.That(link.Role, Is.EqualTo(EpisodeAspect.ROLE_EPISODE));
      Assert.That(link.LinkedRole, Is.EqualTo(SeriesAspect.ROLE_SERIES));
      Assert.That(link.LinkedMediaItemId, Is.EqualTo(series),
          "the narrowing is added to the question of which episodes belong to the series, not asked beside it - "
          + "without the link a season of one series could be answered with another series' season two");
    }

    [Test]
    public void OnlyEpisodesCanBeNarrowedToOneSeason()
    {
      Guid series;
      List<MediaItemQuery> asked;
      IMediaLibrary library = EpisodesOnlyLibrary("Prison Break (2005)", CountedEpisodes(), out series, out asked);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitlesOfSeason(library, "SeasonItem", series, 2, out action);

      Assert.That(Status(action), Is.EqualTo(StatusCodes.Status400BadRequest),
          "a season number means nothing for a shelf that is itself a shelf of seasons - the caller mixed the two "
          + "up, and an empty list here would read as a series with nothing in it");
      Assert.That(result.Error, Does.Contain("season"));
    }

    [Test]
    public void ACountedSeasonAnswersItsDetailsFromTheEpisodesItHolds()
    {
      Guid series;
      List<MediaItemQuery> asked;
      IMediaLibrary library = EpisodesOnlyLibrary("Prison Break (2005)", CountedEpisodes(), out series, out asked);

      ActionResult<MediaController.ItemResult> action;
      MediaController.ItemResult result = GetItemOfSeason(library, series, 2, out action);

      Assert.That(Status(action), Is.Null);
      Assert.That(result.Season, Is.EqualTo(2));
      Assert.That(result.Episodes, Is.EqualTo(1),
          "the panel about a counted season says what the counting knows and nothing else: the series, the number, "
          + "and how many episodes wear it");
      Assert.That(result.SeriesName, Is.EqualTo("Prison Break (2005)"));
      Assert.That(result.Title, Is.EqualTo("Prison Break (2005) S02"));
      Assert.That(result.Id, Is.Not.EqualTo(series),
          "the details of a season are not the details of its series, and answering them as such would have a "
          + "season panel open the whole library under one series' name");
    }

    [Test]
    public void ACountedSeasonLendsItsSeriesSentenceWhenAskedAbout()
    {
      Guid series = Guid.NewGuid();

      // The counted shelf's own question shape: the id question answers the series, and the episode question
      // answers what to count - here one season's worth, so that the counting has something to count.
      MediaItem show = CreateDescribedSeries(series, "Five seasons in a prison and one way out.");
      IList<MediaItem> episodes = new List<MediaItem> { CreateEpisodeItem(Guid.NewGuid(), "Reconvene", 2, 1) };

      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(
          new Dictionary<Guid, MediaItemAspectMetadata>
          {
              { MediaAspect.ASPECT_ID, MediaAspect.Metadata },
              { EpisodeAspect.ASPECT_ID, EpisodeAspect.Metadata },
              { SeriesAspect.ASPECT_ID, SeriesAspect.Metadata },
              { SeasonAspect.ASPECT_ID, SeasonAspect.Metadata }
          });
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, It.IsAny<Guid?>(), It.IsAny<bool>()))
          .Returns<MediaItemQuery, bool, Guid?, bool>((query, online, profile, virtualItems) =>
              query.NecessaryRequestedMIATypeIDs.Contains(EpisodeAspect.ASPECT_ID)
                  ? new List<MediaItem>(episodes)
                  : new List<MediaItem> { show });
      library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), false, false))
          .Returns(1);

      ActionResult<MediaController.ItemResult> action;
      MediaController.ItemResult result = GetItemOfSeason(library.Object, series, 2, out action);

      Assert.That(Status(action), Is.Null);
      Assert.That(result.Season, Is.EqualTo(2));
      Assert.That(result.SeriesOverview, Is.EqualTo("Five seasons in a prison and one way out."),
          "a season the library holds as nothing but a number was never described of its own, and the sentence of "
          + "the series it was counted from is already in hand when its answer is made - the panel shows that "
          + "rather than nothing");
      Assert.That(result.Overview, Is.Null,
          "and still as the series' sentence rather than as a plot the season never had");
    }

    [Test]
    public void ASeasonTheSeriesDoesNotHoldIsAnsweredAsMissing()
    {
      Guid series;
      List<MediaItemQuery> asked;
      IMediaLibrary library = EpisodesOnlyLibrary("Prison Break (2005)", CountedEpisodes(), out series, out asked);

      ActionResult<MediaController.ItemResult> action;
      MediaController.ItemResult result = GetItemOfSeason(library, series, 7, out action);

      Assert.That(Status(action), Is.EqualTo(StatusCodes.Status404NotFound),
          "a number no episode of the series wears is not a title this server can describe");
      Assert.That(result.Error, Does.Contain("7"));
    }

    [Test]
    public void ASeasonTheLibraryHoldsAnswersToItsOwnIdEvenWhenANumberComesAlong()
    {
      Guid id = Guid.NewGuid();
      MediaItem season = CreateSeasonItem(id, "Bones (2005) S03", "Bones", 3, 15, null);

      ActionResult<MediaController.ItemResult> action;
      MediaController.ItemResult result = GetItemOfSeason(
          CreateLibrary(new List<MediaItem> { season }, 1, out _, out _), id, 3, out action);

      Assert.That(Status(action), Is.Null);
      Assert.That(result.Episodes, Is.EqualTo(15),
          "a season the library holds as a title is described from what was tagged on it, and the number a caller "
          + "carried along is not counted over that - the counting is only there for libraries that have nothing else");
    }

    [Test]
    public void OnlyASeriesHasSeasonsToCount()
    {
      Guid film = Guid.NewGuid();
      MediaItem movie = CreateParentItem(film, "Titanic", MovieAspect.Metadata);

      ActionResult<MediaController.ItemResult> action;
      MediaController.ItemResult result = GetItemOfSeason(
          CreateLibrary(new List<MediaItem> { movie }, 1, out _, out _), film, 1, out action);

      Assert.That(Status(action), Is.EqualTo(StatusCodes.Status400BadRequest),
          "asking which is film one of a film is a question about a title that cannot hold seasons, and saying so "
          + "is kinder than answering with a season row that leads nowhere");
      Assert.That(result.Error, Does.Contain("series"));
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

    #region The picture of a title

    private static readonly Guid PICTURE_ID = new Guid("77777777-7777-7777-7777-777777777777");

    /// <summary>
    /// A library that answers picture questions with the given titles and remembers what it was asked.
    /// </summary>
    /// <remarks>
    /// The profile is recorded rather than constrained to null by the matcher, so that a test can say what went
    /// wrong when an endpoint starts passing the caller's profile into a picture query: the mock would otherwise
    /// answer the unexpected call with its own exception, which is a true complaint but a confusing one.
    /// </remarks>
    private static IMediaLibrary PictureLibrary(out List<MediaItemQuery> asked, out List<Guid?> profiles,
        params MediaItem[] items)
    {
      List<MediaItemQuery> questions = new List<MediaItemQuery>();
      List<Guid?> named = new List<Guid?>();

      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(
          new Dictionary<Guid, MediaItemAspectMetadata>
          {
              { ThumbnailLargeAspect.ASPECT_ID, ThumbnailLargeAspect.Metadata }
          });
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, It.IsAny<Guid?>(), false))
          .Callback<MediaItemQuery, bool, Guid?, bool>((query, online, profile, virtualItems) =>
          {
            questions.Add(query);
            named.Add(profile);
          })
          .Returns(items.ToList());

      asked = questions;
      profiles = named;
      return library.Object;
    }

    /// <summary>
    /// A title whose picture is the given bytes - or no picture at all, when the library holds an aspect with
    /// nothing in it, which is what an online provider that found nothing leaves behind.
    /// </summary>
    private static MediaItem PicturedItem(Guid id, byte[] picture)
    {
      SingleMediaItemAspect thumbnail = new SingleMediaItemAspect(ThumbnailLargeAspect.Metadata);
      thumbnail.SetAttribute(ThumbnailLargeAspect.ATTR_THUMBNAIL, picture);

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { ThumbnailLargeAspect.ASPECT_ID, new List<MediaItemAspect> { thumbnail } }
      };
      return new TestItem(id, aspects, null);
    }

    /// <summary>
    /// Bytes that begin like a PNG, which is all an endpoint that does not decode pictures can tell.
    /// </summary>
    private static byte[] Png(string body)
    {
      List<byte> bytes = new List<byte> { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
      bytes.AddRange(Encoding.ASCII.GetBytes(body));
      return bytes.ToArray();
    }

    /// <summary>
    /// A controller answering a picture request, with the HTTP response the endpoint writes its cache header to.
    /// </summary>
    private static MediaController AskedForAPicture()
    {
      return new MediaController
      {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
      };
    }

    private static byte[] Bytes(IActionResult result)
    {
      return (result as FileContentResult)?.FileContents;
    }

    private static int PictureStatus(IActionResult result)
    {
      // A FileContentResult carries no status code of its own - MVC sends 200 for it - so the absence of one is
      // the answer, not a gap in the test.
      return (result as IStatusCodeActionResult)?.StatusCode ?? 200;
    }

    [Test]
    public void ATitlesPictureIsTheBytesTheLibraryHolds()
    {
      byte[] picture = Png("poster");
      List<MediaItemQuery> asked;
      List<Guid?> profiles;
      ServiceRegistration.Set<IMediaLibrary>(PictureLibrary(out asked, out profiles, PicturedItem(PICTURE_ID, picture)));

      IActionResult result = AskedForAPicture().GetThumbnail(PICTURE_ID);

      Assert.That(PictureStatus(result), Is.EqualTo(200));
      Assert.That(Bytes(result), Is.EqualTo(picture),
          "the picture a front end draws is the one the importer stored, byte for byte");
      Assert.That(((FileContentResult) result).ContentType, Is.EqualTo("image/png"));
    }

    [Test]
    public void AServerWithNoMediaLibraryHasNoPicturesEither()
    {
      RemoveMediaLibrary();
      Assert.That(PictureStatus(AskedForAPicture().GetThumbnail(PICTURE_ID)), Is.EqualTo(503));
    }

    [Test]
    public void ALibraryThatIsNotYetStartedSaysSoAboutPicturesToo()
    {
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, It.IsAny<Guid?>(), false))
          .Throws(new IllegalCallException("the media library is still starting"));
      ServiceRegistration.Set<IMediaLibrary>(library.Object);

      Assert.That(PictureStatus(AskedForAPicture().GetThumbnail(PICTURE_ID)), Is.EqualTo(503),
          "a picture that cannot be read yet is not a title without a picture");
    }

    [Test]
    public void ATitleWithoutAPictureIsAnsweredAsMissingRatherThanBroken()
    {
      List<MediaItemQuery> asked;
      List<Guid?> profiles;
      ServiceRegistration.Set<IMediaLibrary>(
          PictureLibrary(out asked, out profiles, PicturedItem(PICTURE_ID, null)));

      IActionResult result = AskedForAPicture().GetThumbnail(PICTURE_ID);

      Assert.That(PictureStatus(result), Is.EqualTo(404));
      Assert.That(((ObjectResult) result).Value, Is.InstanceOf<MediaController.ItemResult>(),
          "the body still says who was answered, so a screen that logs the failure knows which title has no art");
      Assert.That(((MediaController.ItemResult) ((ObjectResult) result).Value).Error,
          Does.Contain(PICTURE_ID.ToString()));
    }

    [Test]
    public void APictureOfZeroBytesIsNoPicture()
    {
      List<MediaItemQuery> asked;
      List<Guid?> profiles;
      ServiceRegistration.Set<IMediaLibrary>(
          PictureLibrary(out asked, out profiles, PicturedItem(PICTURE_ID, new byte[0])));

      Assert.That(PictureStatus(AskedForAPicture().GetThumbnail(PICTURE_ID)), Is.EqualTo(404),
          "an empty array is not a picture, and handing it to a decoder would turn a missing poster into an error");
    }

    [Test]
    public void ATitleTheServerDoesNotKnowHasNoPictureEither()
    {
      List<MediaItemQuery> asked;
      List<Guid?> profiles;
      ServiceRegistration.Set<IMediaLibrary>(PictureLibrary(out asked, out profiles));

      Assert.That(PictureStatus(AskedForAPicture().GetThumbnail(Guid.NewGuid())), Is.EqualTo(404),
          "a title that is gone and a title that never had art look the same to the screen that has to draw one");
    }

    [Test]
    public void APictureQuestionAsksTheLibraryForNothingButThePicture()
    {
      List<MediaItemQuery> asked;
      List<Guid?> profiles;
      ServiceRegistration.Set<IMediaLibrary>(PictureLibrary(out asked, out profiles, PicturedItem(PICTURE_ID, Png("x"))));

      AskedForAPicture().GetThumbnail(PICTURE_ID);

      Assert.That(asked, Has.Count.EqualTo(1), "one question per picture");
      Assert.That(asked[0].NecessaryRequestedMIATypeIDs, Is.EquivalentTo(new[] { ThumbnailLargeAspect.ASPECT_ID }));
      Assert.That(asked[0].OptionalRequestedMIATypeIDs, Is.Empty,
          "a picture does not need the title next to it, and reading the plot out of the database to draw a poster "
          + "is work nobody asked for");
      Assert.That(asked[0].Filter, Is.InstanceOf<MediaItemIdFilter>());
      Assert.That(((MediaItemIdFilter) asked[0].Filter).MediaItemIds, Is.EquivalentTo(new[] { PICTURE_ID }));
    }

    [Test]
    public void APictureIsTheSameForEverybodyInTheHousehold()
    {
      List<MediaItemQuery> asked;
      List<Guid?> profiles;
      ServiceRegistration.Set<IMediaLibrary>(PictureLibrary(out asked, out profiles, PicturedItem(PICTURE_ID, Png("x"))));

      MediaController controller = AskedForAPicture();
      ClaimsPrincipal caller = new ClaimsPrincipal(new ClaimsIdentity(new[]
      {
        new Claim(ClaimTypes.Name, "georg"),
        new Claim(ClaimTypes.Sid, VIEWER.ToString())
      }, "MediaPortalTest"));
      controller.ControllerContext.HttpContext.User = caller;

      controller.GetThumbnail(PICTURE_ID);

      Assert.That(profiles, Has.Count.EqualTo(1));
      Assert.That(profiles[0], Is.Null,
          "a poster is not somebody's data, so asking as a named person must not make the library join that "
          + "person's user data onto it");
    }

    [Test]
    public void APictureSaysHowLongItMayBeKept()
    {
      List<MediaItemQuery> asked;
      List<Guid?> profiles;
      ServiceRegistration.Set<IMediaLibrary>(PictureLibrary(out asked, out profiles, PicturedItem(PICTURE_ID, Png("x"))));

      MediaController controller = AskedForAPicture();
      controller.GetThumbnail(PICTURE_ID);

      Assert.That(controller.HttpContext.Response.Headers["Cache-Control"].ToString(),
          Does.Contain("max-age=" + MediaController.THUMBNAIL_CACHE_SECONDS),
          "walking a shelf of fifty films must not fetch the same fifty pictures on every visit");
    }

    [TestCase("png", "image/png")]
    [TestCase("jpeg", "image/jpeg")]
    [TestCase("gif", "image/gif")]
    [TestCase("bmp", "image/bmp")]
    [TestCase("other", "application/octet-stream")]
    public void APictureIsNamedFromTheBytesThatBeginIt(string kind, string mediaType)
    {
      byte[] picture;
      switch (kind)
      {
        case "png":
          picture = Png("x");
          break;
        case "jpeg":
          picture = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 };
          break;
        case "gif":
          picture = Encoding.ASCII.GetBytes("GIF89a");
          break;
        case "bmp":
          picture = Encoding.ASCII.GetBytes("BM......");
          break;
        default:
          picture = new byte[] { 0x00, 0x01, 0x02, 0x03 };
          break;
      }

      List<MediaItemQuery> asked;
      List<Guid?> profiles;
      ServiceRegistration.Set<IMediaLibrary>(
          PictureLibrary(out asked, out profiles, PicturedItem(PICTURE_ID, picture)));

      IActionResult result = AskedForAPicture().GetThumbnail(PICTURE_ID);

      Assert.That(((FileContentResult) result).ContentType, Is.EqualTo(mediaType),
          kind + " is what the database holds and the type a viewer's decoder is told");
    }

    #endregion

    #region Counting a shelf by a category

    /// <summary>
    /// A library that answers the counting question with a standing set of groups and records the attribute, the
    /// projection and the necessary aspects it was asked to count by.
    /// </summary>
    /// <remarks>
    /// The groups are supplied as the database hands them over - a <see cref="HomogenousMap"/> of value to count,
    /// keyed by whatever type the grouping produced - because the shape of that map and the trimming this endpoint
    /// does to it is exactly where an invented fixture would hide a mistake.
    /// </remarks>
    private static IMediaLibrary CreateCountingLibrary(HomogenousMap counted,
        out List<MediaItemAspectMetadata.AttributeSpecification> groupedBy,
        out List<ProjectionFunction> projections, out List<IEnumerable<Guid>> necessary)
    {
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(new Dictionary<Guid, MediaItemAspectMetadata>
      {
          { MediaAspect.ASPECT_ID, MediaAspect.Metadata },
          { MovieAspect.ASPECT_ID, MovieAspect.Metadata },
          { GenreAspect.ASPECT_ID, GenreAspect.Metadata },
          { VideoAspect.ASPECT_ID, VideoAspect.Metadata }
      });

      List<MediaItemAspectMetadata.AttributeSpecification> specs = new List<MediaItemAspectMetadata.AttributeSpecification>();
      List<ProjectionFunction> proj = new List<ProjectionFunction>();
      List<IEnumerable<Guid>> nec = new List<IEnumerable<Guid>>();
      library.Setup(get => get.GetValueGroups(It.IsAny<MediaItemAspectMetadata.AttributeSpecification>(), null,
              It.IsAny<ProjectionFunction>(), It.IsAny<IEnumerable<Guid>>(), null, false, false))
          .Callback<MediaItemAspectMetadata.AttributeSpecification, IFilter, ProjectionFunction, IEnumerable<Guid>,
              IFilter, bool, bool>((spec, sel, projection, necessaryIds, filter, online, virtualItems) =>
          {
            specs.Add(spec);
            proj.Add(projection);
            nec.Add(necessaryIds);
          })
          .Returns(counted);

      groupedBy = specs;
      projections = proj;
      necessary = nec;
      return library.Object;
    }

    private static MediaController.GroupsResult GetGroups(IMediaLibrary library, string aspect, string by,
        out ActionResult<MediaController.GroupsResult> action)
    {
      ServiceRegistration.Set<IMediaLibrary>(library);
      action = new MediaController().GetGroups(aspect, by);
      return Body(action);
    }

    /// <summary>
    /// Asks the list endpoint for the titles of one shelf that carry one picked value, the way a browse screen
    /// does after it has counted the shelf and the viewer chose one group.
    /// </summary>
    private static MediaController.TitlesResult GetTitlesInGroup(IMediaLibrary library, string aspect, string by,
        string value, out ActionResult<MediaController.TitlesResult> action)
    {
      ServiceRegistration.Set<IMediaLibrary>(library);
      action = new MediaController().GetTitles(aspect, null, null, 0, 0, null, null, Guid.Empty, by, value);
      return Body(action);
    }

    /// <summary>
    /// A shelf library for the picked-value tests. It differs from <see cref="CreateLibrary"/> in one thing only:
    /// the count is allowed to run through a filter, because the point of these tests is that it does - "how many
    /// Action films" is counted through the Action condition, not over the whole shelf.
    /// </summary>
    private static IMediaLibrary CreateFilteredShelfLibrary(out List<MediaItemQuery> queries,
        out List<IFilter> countedThrough)
    {
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(new Dictionary<Guid, MediaItemAspectMetadata>
      {
          { MediaAspect.ASPECT_ID, MediaAspect.Metadata },
          { MovieAspect.ASPECT_ID, MovieAspect.Metadata },
          { GenreAspect.ASPECT_ID, GenreAspect.Metadata },
          { VideoAspect.ASPECT_ID, VideoAspect.Metadata }
      });

      List<MediaItemQuery> asked = new List<MediaItemQuery>();
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false))
          .Callback<MediaItemQuery, bool, Guid?, bool>((query, online, profile, virtualItems) => asked.Add(query))
          .Returns(new List<MediaItem>());

      List<IFilter> counted = new List<IFilter>();
      library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), false, false))
          .Callback<IEnumerable<Guid>, IFilter, bool, bool>(
              (necessary, filter, online, virtualItems) => counted.Add(filter))
          .Returns(0);

      queries = asked;
      countedThrough = counted;
      return library.Object;
    }

    [Test]
    public void TheCountRunsInTheDatabaseOverTheShelfAskedFor()
    {
      HomogenousMap counted = new HomogenousMap(typeof(string), typeof(int));
      counted.Add("Drama", 5);
      IMediaLibrary library = CreateCountingLibrary(counted, out List<MediaItemAspectMetadata.AttributeSpecification> groupedBy,
          out List<ProjectionFunction> projections, out List<IEnumerable<Guid>> necessary);

      ActionResult<MediaController.GroupsResult> action;
      MediaController.GroupsResult result = GetGroups(library, "MovieItem", "genre", out action);

      Assert.That(Status(action), Is.Null);
      Assert.That(groupedBy, Has.Count.EqualTo(1), "counting a shelf is one question to the database");
      Assert.That(groupedBy[0], Is.SameAs(GenreAspect.ATTR_GENRE),
          "genres are grouped on the rows the importer wrote on the title itself, the same source the details "
          + "screen reads, not on a made-up field");
      Assert.That(necessary[0], Is.EquivalentTo(new[] { MovieAspect.ASPECT_ID, GenreAspect.ASPECT_ID }),
          "both the shelf and the aspect holding the values have to take part: counting every title that carries a "
          + "genre under a heading that says films would answer a question nobody asked");
      Assert.That(result.Groups, Has.Count.EqualTo(1));
      Assert.That(result.Groups[0].Value, Is.EqualTo("Drama"));
      Assert.That(result.Groups[0].Count, Is.EqualTo(5),
          "and the number is the one the database counted, because a page of titles is not a census");
    }

    [Test]
    public void TheGroupsComeBiggestFirstAndThenByName()
    {
      HomogenousMap counted = new HomogenousMap(typeof(string), typeof(int));
      counted.Add("Comedy", 3);
      counted.Add("Documentary", 1);
      counted.Add("Action", 9);
      counted.Add("Drama", 3);
      IMediaLibrary library = CreateCountingLibrary(counted, out _, out _, out _);

      ActionResult<MediaController.GroupsResult> action;
      MediaController.GroupsResult result = GetGroups(library, "MovieItem", "genre", out action);

      Assert.That(result.Groups.Select(group => group.Value),
          Is.EqualTo(new[] { "Action", "Comedy", "Drama", "Documentary" }),
          "the categories a library actually lives in come to the top, and the rest stay in a name order a remote "
          + "can walk down to find");
    }

    [Test]
    public void TheEmptyGroupIsNotACategoryAndDoesNotGetARow()
    {
      // Some titles carry no genre at all; the database hands that over as a group keyed by nothing, which the
      // map stores under its own marker for the absent. A browse screen with a blank entry among the genres is
      // a category nobody came to the screen to look at.
      HomogenousMap counted = new HomogenousMap(typeof(string), typeof(int));
      counted.Add(null, 7);
      counted.Add("Action", 2);
      IMediaLibrary library = CreateCountingLibrary(counted, out _, out _, out _);

      ActionResult<MediaController.GroupsResult> action;
      MediaController.GroupsResult result = GetGroups(library, "MovieItem", "genre", out action);

      Assert.That(result.Groups, Has.Count.EqualTo(1));
      Assert.That(result.Groups[0].Value, Is.EqualTo("Action"),
          "the untitled group is dropped: 'no genre written here' is not a genre to browse into");
    }

    [Test]
    public void TheYearIsCountedByTakingTheYearFromTheDateInTheDatabase()
    {
      HomogenousMap counted = new HomogenousMap(typeof(int), typeof(int));
      counted.Add(1998, 3);
      IMediaLibrary library = CreateCountingLibrary(counted, out List<MediaItemAspectMetadata.AttributeSpecification> groupedBy,
          out List<ProjectionFunction> projections, out _);

      ActionResult<MediaController.GroupsResult> action;
      MediaController.GroupsResult result = GetGroups(library, "MovieItem", "year", out action);

      Assert.That(groupedBy[0], Is.SameAs(MediaAspect.ATTR_RECORDINGTIME),
          "there is no year column anywhere in the library; the year is taken from the recording time, the same "
          + "figure the year on a title row is taken from");
      Assert.That(projections[0], Is.EqualTo(ProjectionFunction.DateToYear),
          "and the year comes off inside the database, so the count is over every title and not over a page");
      Assert.That(result.Groups[0].Value, Is.EqualTo("1998"), "the year arrives as words, like every other group");
    }

    [Test]
    public void ACategoryNobodyHasIsRefusedRatherThanGuessedAt()
    {
      IMediaLibrary library = CreateCountingLibrary(new HomogenousMap(typeof(string), typeof(int)),
          out _, out _, out _);

      ActionResult<MediaController.GroupsResult> action;
      MediaController.GroupsResult result = GetGroups(library, "MovieItem", "colour", out action);

      Assert.That(Status(action), Is.EqualTo(400));
      Assert.That(result.Error, Does.Contain("genre").And.Contain("year").And.Contain("actors"),
          "the refusal names the categories there are, which is the difference between a mistake that teaches and "
          + "one that just fails");
    }

    [Test]
    public void TheTitlesOfOneGenreAreAskedForByTheGenreAndNothingElse()
    {
      List<MediaItemQuery> queries;
      List<IFilter> countedThrough;
      IMediaLibrary library = CreateFilteredShelfLibrary(out queries, out countedThrough);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitlesInGroup(library, "MovieItem", "genre", "Action", out action);

      Assert.That(Status(action), Is.Null);
      RelationalFilter condition = (RelationalFilter) queries[0].Filter;
      Assert.That(condition.AttributeType, Is.SameAs(GenreAspect.ATTR_GENRE),
          "the same genre rows the count grouped on, so the titles that arrive match the heading they arrived under");
      Assert.That(condition.Operator, Is.EqualTo(RelationalOperator.EQ));
      Assert.That(condition.FilterValue, Is.EqualTo("Action"));
      Assert.That(result.By, Is.EqualTo("genre"));
      Assert.That(result.Value, Is.EqualTo("Action"),
          "and the answer says whose films it is looking at, even before a single one arrives");
      Assert.That(countedThrough, Has.Count.EqualTo(1));
      Assert.That(countedThrough[0], Is.SameAs(queries[0].Filter),
          "and the total is counted through the very same filter, so a screen reads '12 of 12 Action films' and "
          + "not '12 of the whole shelf'");
    }

    [Test]
    public void TheYearOfATitleIsAWholeYearAndNotAMoment()
    {
      List<MediaItemQuery> queries;
      IMediaLibrary library = CreateFilteredShelfLibrary(out queries, out _);

      ActionResult<MediaController.TitlesResult> action;
      GetTitlesInGroup(library, "MovieItem", "year", "1998", out action);

      BooleanCombinationFilter combined = (BooleanCombinationFilter) queries[0].Filter;
      Assert.That(combined.Operator, Is.EqualTo(BooleanOperator.And));

      RelationalFilter from = combined.Operands.OfType<RelationalFilter>()
          .Single(entry => entry.Operator == RelationalOperator.GE);
      RelationalFilter to = combined.Operands.OfType<RelationalFilter>()
          .Single(entry => entry.Operator == RelationalOperator.LT);
      Assert.That(from.AttributeType, Is.SameAs(MediaAspect.ATTR_RECORDINGTIME));
      Assert.That(from.FilterValue, Is.EqualTo(new DateTime(1998, 1, 1)));
      Assert.That(to.FilterValue, Is.EqualTo(new DateTime(1999, 1, 1)),
          "the year is asked for as the twelve months it covers; equality against a made-up midnight would lose "
          + "every film whose recording time carries an hour");
    }

    [Test]
    public void ACategoryWithoutItsValueIsRefusedRatherThanAnsweredWithTheWholeShelf()
    {
      List<MediaItemQuery> queries;
      IMediaLibrary library = CreateLibrary(new List<MediaItem>(), 0, out queries, out _);

      ActionResult<MediaController.TitlesResult> action;
      MediaController.TitlesResult result = GetTitlesInGroup(library, "MovieItem", "genre", null, out action);

      Assert.That(Status(action), Is.EqualTo(400));
      Assert.That(queries, Is.Empty, "nothing is read for a question that was not finished");
      Assert.That(result.Error, Does.Contain("value"),
          "handing back the whole shelf under a heading that names one genre is the wrong answer that looks right");
    }

    #endregion

    #region One named kind of picture

    /// <summary>
    /// Asks the typed fanart endpoint the way the framework would, with a response to write its caching hint on.
    /// </summary>
    private static IActionResult AskFanArt(Guid id, string type)
    {
      MediaController controller = new MediaController();
      controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
      return controller.GetFanArtOfType(id, type);
    }

    private static int StatusOf(IActionResult result)
    {
      int? code = (result as IStatusCodeActionResult)?.StatusCode;
      return code ?? 200;
    }

    /// <summary>
    /// The picture's bytes when the answer was one, or null when it was an error.
    /// </summary>
    private static byte[] PictureOf(IActionResult result)
    {
      return (result as FileContentResult)?.FileContents;
    }

    private static string ErrorOf(IActionResult result)
    {
      return ((result as ObjectResult)?.Value as MediaController.ItemResult)?.Error;
    }

    /// <summary>A folder that belongs to one test and is deleted again by it.</summary>
    private static string OwnFolder()
    {
      string dir = Path.Combine(Path.GetTempPath(), "mp2-fanart-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(dir);
      return dir;
    }

    private static void WritePicture(string path, byte[] bytes)
    {
      Directory.CreateDirectory(Path.GetDirectoryName(path));
      File.WriteAllBytes(path, bytes);
    }

    /// <summary>
    /// Registers MP2's own local filesystem provider, so the endpoint's search next to the media reads real
    /// folders the way it does on a running server.
    /// </summary>
    private static void GiveTheServerItsFileSystem()
    {
      var providers = new Dictionary<Guid, IResourceProvider>
      {
          { LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, new LocalFsResourceProvider() }
      };
      var accessor = new Mock<IMediaAccessor>(MockBehavior.Strict);
      accessor.SetupGet(get => get.LocalResourceProviders).Returns(providers);
      ServiceRegistration.Set<IMediaAccessor>(accessor.Object);
    }

    /// <summary>
    /// A library that answers the id lookup with the given titles and every relationship query with the given
    /// episodes, so the series detour can be watched as it happens.
    /// </summary>
    private static IMediaLibrary CreateFanArtLibrary(IList<MediaItem> byId, IList<MediaItem> episodes,
        out List<MediaItemQuery> queries)
    {
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      List<MediaItemQuery> asked = new List<MediaItemQuery>();
      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), false, null, false))
          .Callback<MediaItemQuery, bool, Guid?, bool>((query, online, profile, virtualItems) => asked.Add(query))
          .Returns<MediaItemQuery, bool, Guid?, bool>((query, online, profile, virtualItems) =>
              query.Filter is RelationshipFilter ? episodes : byId);
      queries = asked;
      return library.Object;
    }

    /// <summary>One episode as the importer left it, with the media file a test has really written.</summary>
    private static MediaItem CreateEpisode(Guid id, string filePath)
    {
      SingleMediaItemAspect media = CreateMediaAspect("Crimson Sky", "Crimson Sky", DateTime.Now);
      SingleMediaItemAspect episode = new SingleMediaItemAspect(EpisodeAspect.Metadata);
      episode.SetAttribute(EpisodeAspect.ATTR_SERIES_NAME, "Shōgun");
      episode.SetAttribute(EpisodeAspect.ATTR_SEASON, 1);
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } },
          { EpisodeAspect.ASPECT_ID, new List<MediaItemAspect> { episode } }
      };
      // The local filesystem provider spells its paths "/C:/...", which is what Serialize() and the "../"
      // walking in the endpoint both read.
      return new TestItem(id, aspects, CreateLocator(ResourcePath.BuildBaseProviderPath(
          LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID,
          "/" + filePath.Replace('\\', '/')).Serialize()));
    }

    /// <summary>
    /// One track as the music importer left it: the file its tags were read from, and - when the release is part
    /// of an album - the link saying which one.
    /// </summary>
    private static MediaItem CreateTrack(Guid id, Guid? albumId, string filePath)
    {
      SingleMediaItemAspect media = CreateMediaAspect("01 Song", "01 Song", DateTime.Now);
      SingleMediaItemAspect audio = new SingleMediaItemAspect(AudioAspect.Metadata);
      audio.SetAttribute(AudioAspect.ATTR_TRACK, 1);
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } },
          { AudioAspect.ASPECT_ID, new List<MediaItemAspect> { audio } }
      };
      if (albumId.HasValue)
      {
        MultipleMediaItemAspect link = new MultipleMediaItemAspect(RelationshipAspect.Metadata);
        link.SetAttribute(RelationshipAspect.ATTR_ROLE, AudioAspect.ROLE_TRACK);
        link.SetAttribute(RelationshipAspect.ATTR_LINKED_ROLE, AudioAlbumAspect.ROLE_ALBUM);
        link.SetAttribute(RelationshipAspect.ATTR_LINKED_ID, albumId.Value);
        aspects[RelationshipAspect.ASPECT_ID] = new List<MediaItemAspect> { link };
      }
      return new TestItem(id, aspects, CreateLocator(ResourcePath.BuildBaseProviderPath(
          LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID,
          "/" + filePath.Replace('\\', '/')).Serialize()));
    }

    /// <summary>One film as a tagging tool left it: a file of its own, with the pictures named after it.</summary>
    private static MediaItem CreateFilm(Guid id, string filePath)
    {
      SingleMediaItemAspect media = CreateMediaAspect("Minions & Monster", "Minions & Monster", DateTime.Now);
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } }
      };
      return new TestItem(id, aspects, CreateLocator(ResourcePath.BuildBaseProviderPath(
          LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID,
          "/" + filePath.Replace('\\', '/')).Serialize()));
    }

    /// <summary>One series as the importer left it: a name and no file of its own.</summary>
    private static MediaItem CreateSeries(Guid id)
    {
      SingleMediaItemAspect media = CreateMediaAspect("Shōgun", "Shōgun", DateTime.Now);
      SingleMediaItemAspect series = new SingleMediaItemAspect(SeriesAspect.Metadata);
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } },
          { SeriesAspect.ASPECT_ID, new List<MediaItemAspect> { series } }
      };
      return new TestItem(id, aspects, null);
    }

    [Test]
    public void AWordThatIsNoKindOfPictureIsRefused()
    {
      IActionResult result = AskFanArt(Guid.NewGuid(), "wallpaper");

      Assert.That(StatusOf(result), Is.EqualTo(400));
      Assert.That(ErrorOf(result), Does.Contain("not a kind of fanart"),
          "the answer names the mistake rather than guessing a picture");
    }

    [Test]
    public void TheCacheIsAskedForTheKindThatWasAskedFor()
    {
      string dir = OwnFolder();
      try
      {
        string banner = Path.Combine(dir, "banner.jpg");
        WritePicture(banner, new byte[] { 4, 5, 6 });

        var cache = new Mock<IFanArtCache>();
        Guid id = Guid.NewGuid();
        cache.Setup(get => get.GetFanArtFiles(id, FanArtTypes.Banner)).Returns(new List<string> { banner });
        ServiceRegistration.Set(cache.Object);

        byte[] picture = PictureOf(AskFanArt(id, "banner"));
        Assert.That(picture, Is.EqualTo(new byte[] { 4, 5, 6 }),
            "the extracted banner the cache holds answers a banner request");
        cache.Verify(get => get.GetFanArtFiles(id, FanArtTypes.Banner), Times.Once);
        cache.Verify(get => get.GetFanArtFiles(id, FanArtTypes.Poster), Times.Never,
            "a banner request never goes fishing in the posters");
      }
      finally
      {
        Directory.Delete(dir, true);
      }
    }

    [Test]
    public void APosterIsNeverAnAnswerToABanner()
    {
      string dir = OwnFolder();
      try
      {
        string poster = Path.Combine(dir, "poster.jpg");
        WritePicture(poster, new byte[] { 1, 2, 3 });

        var cache = new Mock<IFanArtCache>();
        Guid id = Guid.NewGuid();
        cache.Setup(get => get.GetFanArtFiles(id, FanArtTypes.Poster)).Returns(new List<string> { poster });
        ServiceRegistration.Set(cache.Object);

        IActionResult result = AskFanArt(id, "Banner");
        Assert.That(StatusOf(result), Is.EqualTo(404),
            "a shelf with no banner draws the skin's placeholder, exactly like the reference");
        Assert.That(PictureOf(result), Is.Null, "the poster lying about is not handed out as a banner");
      }
      finally
      {
        Directory.Delete(dir, true);
      }
    }

    [Test]
    public void ThePicturesNextToTheMediaAnswerWhatTheCacheLacks()
    {
      string dir = OwnFolder();
      try
      {
        // The reference's own layout: the series' banner and backdrop two folders up from the episode file,
        // and the episode's screenshot beside the episode itself.
        string episode = Path.Combine(dir, "Shogun (2024)", "Season 01", "ep01.mkv");
        Directory.CreateDirectory(Path.GetDirectoryName(episode));
        File.WriteAllBytes(episode, new byte[] { 0 });
        WritePicture(Path.Combine(dir, "Shogun (2024)", "banner.jpg"), new byte[] { 7, 7 });
        WritePicture(Path.Combine(dir, "Shogun (2024)", "fanart.jpg"), new byte[] { 8, 8 });
        WritePicture(Path.Combine(dir, "Shogun (2024)", "Season 01", "ep01-thumb.jpg"), new byte[] { 9, 9 });

        Guid id = Guid.NewGuid();
        GiveTheServerItsFileSystem();
        ServiceRegistration.Set(CreateFanArtLibrary(new List<MediaItem> { CreateEpisode(id, episode) },
            new List<MediaItem>(), out _));

        Assert.That(PictureOf(AskFanArt(id, "Banner")), Is.EqualTo(new byte[] { 7, 7 }),
            "the series' banner two folders above the episode file is the episode's banner, as the reference read it");
        Assert.That(PictureOf(AskFanArt(id, "FanArt")), Is.EqualTo(new byte[] { 8, 8 }));
        Assert.That(PictureOf(AskFanArt(id, "Thumbnail")), Is.EqualTo(new byte[] { 9, 9 }),
            "the screenshot named after the episode is its thumbnail");
      }
      finally
      {
        Directory.Delete(dir, true);
      }
    }

    [Test]
    public void ThePicturesNamedAfterTheFilmAnswerWhatAPlainNameLeavesOut()
    {
      string dir = OwnFolder();
      try
      {
        // A tagging tool that files one folder per film often puts the film's own file name in front of every
        // picture, so nothing in the folder is called just "poster" or "fanart". The reference reads those as
        // the film's pictures - "<file>-poster.jpg" is its poster - and a shelf that missed them stood there
        // empty over a library that had been tagged.
        string film = Path.Combine(dir, "Minions & Monster (2026)", "Minions & Monster (2026) 1080p.mkv");
        Directory.CreateDirectory(Path.GetDirectoryName(film));
        File.WriteAllBytes(film, new byte[] { 0 });
        WritePicture(Path.Combine(dir, "Minions & Monster (2026)",
            "Minions & Monster (2026) 1080p-poster.jpg"), new byte[] { 21, 21 });
        WritePicture(Path.Combine(dir, "Minions & Monster (2026)",
            "Minions & Monster (2026) 1080p-fanart.jpg"), new byte[] { 22, 22 });
        WritePicture(Path.Combine(dir, "Minions & Monster (2026)",
            "Minions & Monster (2026) 1080p-banner.jpg"), new byte[] { 23, 23 });

        Guid id = Guid.NewGuid();
        GiveTheServerItsFileSystem();
        ServiceRegistration.Set(CreateFanArtLibrary(new List<MediaItem> { CreateFilm(id, film) },
            new List<MediaItem>(), out _));

        Assert.That(PictureOf(AskFanArt(id, "Poster")), Is.EqualTo(new byte[] { 21, 21 }),
            "the poster named after the film is the film's poster, as the reference read it");
        Assert.That(PictureOf(AskFanArt(id, "FanArt")), Is.EqualTo(new byte[] { 22, 22 }),
            "the backdrop named after the film is the film's backdrop");
        Assert.That(PictureOf(AskFanArt(id, "Banner")), Is.EqualTo(new byte[] { 23, 23 }),
            "and the same rule holds for every other kind, not only for the screenshot it already knew");
      }
      finally
      {
        Directory.Delete(dir, true);
      }
    }

    [Test]
    public void ThePlainNameOfAPictureStillOutranksTheFilmNamedOne()
    {
      string dir = OwnFolder();
      try
      {
        string film = Path.Combine(dir, "Lilo & Stitch (2025)", "Lilo & Stitch (2025) 1080p.mkv");
        Directory.CreateDirectory(Path.GetDirectoryName(film));
        File.WriteAllBytes(film, new byte[] { 0 });
        WritePicture(Path.Combine(dir, "Lilo & Stitch (2025)", "poster.jpg"), new byte[] { 24, 24 });
        WritePicture(Path.Combine(dir, "Lilo & Stitch (2025)", "Lilo & Stitch (2025) 1080p-poster.jpg"),
            new byte[] { 25, 25 });
        WritePicture(Path.Combine(dir, "Lilo & Stitch (2025)", "fanart.jpg"), new byte[] { 26, 26 });
        WritePicture(Path.Combine(dir, "Lilo & Stitch (2025)", "Lilo & Stitch (2025) 1080p-fanart.jpg"),
            new byte[] { 27, 27 });

        Guid id = Guid.NewGuid();
        GiveTheServerItsFileSystem();
        ServiceRegistration.Set(CreateFanArtLibrary(new List<MediaItem> { CreateFilm(id, film) },
            new List<MediaItem>(), out _));

        Assert.That(PictureOf(AskFanArt(id, "Poster")), Is.EqualTo(new byte[] { 24, 24 }),
            "a picture called poster is the poster; the film-named one is the fallback the reference puts second");
        Assert.That(PictureOf(AskFanArt(id, "FanArt")), Is.EqualTo(new byte[] { 26, 26 }));
      }
      finally
      {
        Directory.Delete(dir, true);
      }
    }

    [Test]
    public void TwoBackdropsOfOneNameAreAnsweredAndNotThrown()
    {
      string dir = OwnFolder();
      try
      {
        // Three backdrops per film is what the server is configured to keep, and a tagging tool numbers them.
        // The reference's filter sorts candidates by a priority number it hands to Dictionary.Add, so two
        // pictures answering one prefix are two files for one number - an exception, and a shelf left without
        // a backdrop over a folder that has three.
        string film = Path.Combine(dir, "Jurassic World (2015)", "Jurassic World (2015) 720p.mkv");
        Directory.CreateDirectory(Path.GetDirectoryName(film));
        File.WriteAllBytes(film, new byte[] { 0 });
        WritePicture(Path.Combine(dir, "Jurassic World (2015)", "fanart.jpg"), new byte[] { 28, 28 });
        WritePicture(Path.Combine(dir, "Jurassic World (2015)", "fanart-2.jpg"), new byte[] { 29, 29 });

        Guid id = Guid.NewGuid();
        GiveTheServerItsFileSystem();
        ServiceRegistration.Set(CreateFanArtLibrary(new List<MediaItem> { CreateFilm(id, film) },
            new List<MediaItem>(), out _));

        Assert.That(PictureOf(AskFanArt(id, "FanArt")), Is.EqualTo(new byte[] { 28, 28 }),
            "the numbered second backdrop does not stop the first one from being handed out");
      }
      finally
      {
        Directory.Delete(dir, true);
      }
    }

    [Test]
    public void ATrackWearsTheCoverItsAlbumHolds()
    {
      string dir = OwnFolder();
      try
      {
        // A thousand tracks share forty-odd covers, so the import stores each one once, under the album. Asked
        // track by tile, the shelf would otherwise be placeholders although every file carries its art.
        string albumCover = Path.Combine(dir, "chosen.jpg");
        WritePicture(albumCover, new byte[] { 11, 12, 13 });

        Guid trackId = Guid.NewGuid();
        Guid albumId = Guid.NewGuid();
        var cache = new Mock<IFanArtCache>();
        cache.Setup(get => get.GetFanArtFiles(albumId, FanArtTypes.Cover))
            .Returns(new List<string> { albumCover });
        ServiceRegistration.Set(cache.Object);

        ServiceRegistration.Set(CreateFanArtLibrary(
            new List<MediaItem> { CreateTrack(trackId, albumId, Path.Combine(dir, "01 Song.mp3")) },
            new List<MediaItem>(), out List<MediaItemQuery> queries));

        Assert.That(PictureOf(AskFanArt(trackId, "Cover")), Is.EqualTo(new byte[] { 11, 12, 13 }),
            "a track asked for a cover is answered with the album's, which is where the picture lies");

        MediaItemQuery detour = queries.Single();
        Assert.That(detour.OptionalRequestedMIATypeIDs, Does.Contain(AudioAspect.ASPECT_ID),
            "the question asks for the aspect that says this id is a track at all; ask without it and no title "
            + "ever looks like one that has an album");
      }
      finally
      {
        Directory.Delete(dir, true);
      }
    }

    [Test]
    public void TheAlbumsChosenCoverOutranksThePictureInTheFolder()
    {
      string dir = OwnFolder();
      try
      {
        // The track lies in the album's folder, so walking that folder answers a track with whatever
        // folder.jpg happens to lie there - while the album itself answers with the picture the import chose.
        // Two tiles of one album must not wear two pictures, so the chosen one is asked for first.
        string track = Path.Combine(dir, "Fixture Album", "01 Song.mp3");
        Directory.CreateDirectory(Path.GetDirectoryName(track));
        File.WriteAllBytes(track, new byte[] { 0 });
        WritePicture(Path.Combine(dir, "Fixture Album", "cover.jpg"), new byte[] { 21, 22 });

        string chosen = Path.Combine(dir, "chosen.jpg");
        WritePicture(chosen, new byte[] { 11, 12, 13 });

        Guid trackId = Guid.NewGuid();
        Guid albumId = Guid.NewGuid();
        var cache = new Mock<IFanArtCache>();
        cache.Setup(get => get.GetFanArtFiles(albumId, FanArtTypes.Cover))
            .Returns(new List<string> { chosen });
        ServiceRegistration.Set(cache.Object);

        GiveTheServerItsFileSystem();
        ServiceRegistration.Set(CreateFanArtLibrary(
            new List<MediaItem> { CreateTrack(trackId, albumId, track) }, new List<MediaItem>(), out _));

        Assert.That(PictureOf(AskFanArt(trackId, "Cover")), Is.EqualTo(new byte[] { 11, 12, 13 }),
            "the picture the album wears is the picture its tracks wear");
      }
      finally
      {
        Directory.Delete(dir, true);
      }
    }

    [Test]
    public void ATrackOfAnAlbumNobodyKnowsIsStillAnsweredFromItsOwnFolder()
    {
      string dir = OwnFolder();
      try
      {
        // A track that is its own release has no link to follow; that is not a reason to withhold the cover
        // lying next to it.
        string track = Path.Combine(dir, "Own Release", "01 Song.mp3");
        Directory.CreateDirectory(Path.GetDirectoryName(track));
        File.WriteAllBytes(track, new byte[] { 0 });
        WritePicture(Path.Combine(dir, "Own Release", "cover.jpg"), new byte[] { 31, 32 });

        Guid trackId = Guid.NewGuid();
        ServiceRegistration.Set(new Mock<IFanArtCache>().Object);
        GiveTheServerItsFileSystem();
        ServiceRegistration.Set(CreateFanArtLibrary(
            new List<MediaItem> { CreateTrack(trackId, null, track) }, new List<MediaItem>(), out _));

        Assert.That(PictureOf(AskFanArt(trackId, "Cover")), Is.EqualTo(new byte[] { 31, 32 }),
            "with no album to ask, the folder the track lies in answers");
      }
      finally
      {
        Directory.Delete(dir, true);
      }
    }

    [Test]
    public void ASeriesBorrowsItsPictureFromOneOfItsEpisodes()
    {
      string dir = OwnFolder();
      try
      {
        string episode = Path.Combine(dir, "Shogun (2024)", "Season 01", "ep01.mkv");
        Directory.CreateDirectory(Path.GetDirectoryName(episode));
        File.WriteAllBytes(episode, new byte[] { 0 });
        WritePicture(Path.Combine(dir, "Shogun (2024)", "banner.jpg"), new byte[] { 7, 7 });

        Guid seriesId = Guid.NewGuid();
        Guid episodeId = Guid.NewGuid();
        GiveTheServerItsFileSystem();
        ServiceRegistration.Set(CreateFanArtLibrary(new List<MediaItem> { CreateSeries(seriesId) },
            new List<MediaItem> { CreateEpisode(episodeId, episode) }, out List<MediaItemQuery> queries));

        byte[] picture = PictureOf(AskFanArt(seriesId, "Banner"));
        Assert.That(picture, Is.EqualTo(new byte[] { 7, 7 }),
            "a series has no file of its own; the reference derived its folder from one episode, and so does this");

        RelationshipFilter detour = queries.Select(query => query.Filter).OfType<RelationshipFilter>()
            .Single();
        Assert.That(detour.Role, Is.EqualTo(EpisodeAspect.ROLE_EPISODE));
        Assert.That(detour.LinkedRole, Is.EqualTo(SeriesAspect.ROLE_SERIES));
        Assert.That(detour.LinkedMediaItemId, Is.EqualTo(seriesId),
            "the detour asks for one episode of this very series");
      }
      finally
      {
        Directory.Delete(dir, true);
      }
    }

    [Test]
    public void ATitleTheLibraryDoesNotKnowIsAnsweredHonestly()
    {
      ServiceRegistration.Set(CreateFanArtLibrary(new List<MediaItem>(), new List<MediaItem>(), out _));

      IActionResult result = AskFanArt(Guid.NewGuid(), "FanArt");
      Assert.That(StatusOf(result), Is.EqualTo(404));
      Assert.That(ErrorOf(result), Does.Contain("no fanart"),
          "the answer says there is no picture rather than inventing one");
    }

    #endregion

    #region The words next to the title

    /// <summary>
    /// An episode as the importer left it, with the file a test has really written and the air date - or its
    /// absence - the test decides on.
    /// </summary>
    private static MediaItem EpisodeOnDisk(Guid id, string filePath, DateTime recordingTime)
    {
      SingleMediaItemAspect media = CreateMediaAspect("Crimson Sky", "Crimson Sky", recordingTime);
      SingleMediaItemAspect episode = new SingleMediaItemAspect(EpisodeAspect.Metadata);
      episode.SetAttribute(EpisodeAspect.ATTR_SERIES_NAME, "Shōgun");
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } },
          { EpisodeAspect.ASPECT_ID, new List<MediaItemAspect> { episode } }
      };
      return new TestItem(id, aspects, CreateLocator(ResourcePath.BuildBaseProviderPath(
          LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID,
          "/" + filePath.Replace('\\', '/')).Serialize()));
    }

    /// <summary>
    /// A series without a file of its own, asked for by the caller's profile of nothing.
    /// </summary>
    private static MediaItem ShowWithoutFile(Guid id, string title, DateTime recordingTime)
    {
      SingleMediaItemAspect media = CreateMediaAspect(title, title, recordingTime);
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } },
          { SeriesAspect.ASPECT_ID, new List<MediaItemAspect> { new SingleMediaItemAspect(SeriesAspect.Metadata) } }
      };
      return new TestItem(id, aspects, null);
    }

    private static void WriteText(string path, string text)
    {
      Directory.CreateDirectory(Path.GetDirectoryName(path));
      File.WriteAllText(path, text, new UTF8Encoding(false));
    }

    /// <summary>One episode's own metadata file, in the shape tinyMediaManager writes it.</summary>
    private static void WriteEpisodeNfo(string path, string title, string plot, string rating)
    {
      WriteText(path,
          "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n"
          + "<episodedetails>\n"
          + "  <title>" + title + "</title>\n"
          + "  <showtitle>Prison Break</showtitle>\n"
          + "  <season>4</season>\n"
          + "  <episode>3</episode>\n"
          + "  <rating>" + rating + "</rating>\n"
          + "  <plot>" + plot + "</plot>\n"
          + "  <runtime>45</runtime>\n"
          + "  <mpaa/>\n"
          + "  <premiered>2008-09-08</premiered>\n"
          + "  <aired>2008-09-08</aired>\n"
          + "  <studio>FOX</studio>\n"
          + "</episodedetails>");
    }

    /// <summary>The show's own metadata file, with the genres and the crowd's rating the site gave it.</summary>
    private static void WriteShowNfo(string path)
    {
      WriteText(path,
          "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n"
          + "<tvshow>\n"
          + "  <title>Prison Break</title>\n"
          + "  <year>2005</year>\n"
          + "  <ratings>\n"
          + "    <rating default=\"true\" max=\"10\" name=\"themoviedb\">\n"
          + "      <value>7.3</value>\n"
          + "      <votes>989</votes>\n"
          + "    </rating>\n"
          + "  </ratings>\n"
          + "  <plot>Due to a political conspiracy, an innocent man is sent to death row - die Grüne Stunde.</plot>\n"
          + "  <certification>US:TV-14</certification>\n"
          + "  <premiered>2005-08-29</premiered>\n"
          + "  <genre>Action</genre>\n"
          + "  <genre>Drama</genre>\n"
          + "  <studio>FOX</studio>\n"
          + "</tvshow>");
    }

    [Test]
    public void TheEpisodeFileFillsTheWordsTheLibraryDoesNotHave()
    {
      string dir = OwnFolder();
      try
      {
        string media = Path.Combine(dir, "Season 4", "Show - S04E03.mkv");
        WriteText(media, "mkv");
        WriteEpisodeNfo(Path.ChangeExtension(media, ".nfo"), "Shut Down",
            "Michael attempts to track the cardholder.", "7.5");
        GiveTheServerItsFileSystem();

        Guid id = Guid.NewGuid();
        ActionResult<MediaController.ItemResult> action;
        MediaController.ItemResult result = GetItem(
            CreateFanArtLibrary(new List<MediaItem> { EpisodeOnDisk(id, media, DateTime.MinValue) },
                new List<MediaItem>(), out _), id, out action);

        Assert.That(result.EpisodeName, Is.EqualTo("Shut Down"),
            "the file knows the episode's name, which the bare import did not");
        Assert.That(result.Overview, Is.EqualTo("Michael attempts to track the cardholder."));
        Assert.That(result.RuntimeMinutes, Is.EqualTo(45));
        Assert.That(result.FirstAired, Is.EqualTo(new DateTime(2008, 9, 8)),
            "no library date exists, so the file's air date is the honest first aired date");
        Assert.That(result.TotalRating, Is.EqualTo(7.5),
            "the library never rated the episode, so the tagging tool's decimal is worth showing");
      }
      finally
      {
        Directory.Delete(dir, true);
      }
    }

    [Test]
    public void TheShowsOwnFileSpeaksForTheSeriesAndNamesItsGenres()
    {
      string dir = OwnFolder();
      try
      {
        string media = Path.Combine(dir, "Season 4", "Show - S04E03.mkv");
        WriteText(media, "mkv");
        WriteEpisodeNfo(Path.ChangeExtension(media, ".nfo"), "Shut Down", "An episode of it.", "0.0");
        WriteShowNfo(Path.Combine(dir, "tvshow.nfo"));
        GiveTheServerItsFileSystem();

        Guid seriesId = Guid.NewGuid();
        MediaItem episode = EpisodeOnDisk(Guid.NewGuid(), media, DateTime.MinValue);
        ActionResult<MediaController.ItemResult> action;
        MediaController.ItemResult result = GetItem(
            CreateFanArtLibrary(new List<MediaItem> { ShowWithoutFile(seriesId, "Prison Break", DateTime.MinValue) },
                new List<MediaItem> { episode }, out List<MediaItemQuery> queries), seriesId, out action);

        Assert.That(result.Overview, Does.StartWith("Due to a political conspiracy"),
            "the show's own file, two folders above the episode, describes the show - not the episode's summary");
        Assert.That(result.Overview, Does.Contain("Grüne Stunde"),
            "the file's own encoding survives into the answer");
        Assert.That(result.Genres, Is.EqualTo(new[] { "Action", "Drama" }),
            "the genres the tagging tool filed are the genres the details screen will show");
        Assert.That(result.TotalRating, Is.EqualTo(7.3),
            "the ratings list's default entry is the one read, with its decimal kept");
        Assert.That(result.FirstAired, Is.EqualTo(new DateTime(2005, 8, 29)),
            "for a series the show's premiere is the first aired date, not one episode's air date");

        // On the live server the mock is not there to hand out aspects nobody asked for: if the query that fetches
        // the series does not name the aspect that makes it one, the detour cannot see it and the whole search
        // quietly never starts. This is the assertion that keeps that from coming back.
        MediaItemQuery asked = queries.Find(what => what.Filter is MediaItemIdFilter);
        Assert.That(asked.OptionalRequestedMIATypeIDs, Does.Contain(SeriesAspect.ASPECT_ID),
            "the question for the title also asks for the aspect that makes it a series, or no file can be found");
      }
      finally
      {
        Directory.Delete(dir, true);
      }
    }

    [Test]
    public void TheEpisodeReadsItsShowsGenresThroughItsOwnFile()
    {
      string dir = OwnFolder();
      try
      {
        string media = Path.Combine(dir, "Season 4", "Show - S04E03.mkv");
        WriteText(media, "mkv");
        WriteEpisodeNfo(Path.ChangeExtension(media, ".nfo"), "Shut Down", "An episode of it.", "6.2");
        WriteShowNfo(Path.Combine(dir, "tvshow.nfo"));
        GiveTheServerItsFileSystem();

        Guid id = Guid.NewGuid();
        ActionResult<MediaController.ItemResult> action;
        MediaController.ItemResult result = GetItem(
            CreateFanArtLibrary(new List<MediaItem> { EpisodeOnDisk(id, media, DateTime.MinValue) },
                new List<MediaItem>(), out _), id, out action);

        Assert.That(result.Overview, Is.EqualTo("An episode of it."),
            "the episode's own plot wins over the show's");
        Assert.That(result.FirstAired, Is.EqualTo(new DateTime(2008, 9, 8)),
            "the episode's own air date wins over the show's premiere");
        Assert.That(result.TotalRating, Is.EqualTo(6.2),
            "the episode's own rating wins over the show's");
        Assert.That(result.Genres, Is.EqualTo(new[] { "Action", "Drama" }),
            "the episode file names no genres; the show's file does, and they are the same show's");
        Assert.That(result.Certification, Is.EqualTo("US:TV-14"),
            "the episode file holds an empty mpaa field; the show's fills the gap");
      }
      finally
      {
        Directory.Delete(dir, true);
      }
    }

    [Test]
    public void WhatTheLibraryKnowsIsNeverOverwrittenByAFile()
    {
      string dir = OwnFolder();
      try
      {
        string media = Path.Combine(dir, "Season 4", "Show - S04E03.mkv");
        WriteText(media, "mkv");
        WriteEpisodeNfo(Path.ChangeExtension(media, ".nfo"), "Shut Down", "What the file claims.", "7.5");

        SingleMediaItemAspect mediaAspect = CreateMediaAspect("Crimson Sky", "Crimson Sky",
            new DateTime(2010, 6, 1));
        mediaAspect.SetAttribute(MediaAspect.ATTR_COMMENT, "What the database holds.");
        mediaAspect.SetAttribute(MediaAspect.ATTR_RATING, 8);
        SingleMediaItemAspect episode = new SingleMediaItemAspect(EpisodeAspect.Metadata);
        episode.SetAttribute(EpisodeAspect.ATTR_SEASON, 4);
        episode.SetCollectionAttribute(EpisodeAspect.ATTR_EPISODE, new List<int> { 3 });
        SingleMediaItemAspect movie = CreateMovieAspect(52);
        IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>
        {
            { MediaAspect.ASPECT_ID, new List<MediaItemAspect> { mediaAspect } },
            { EpisodeAspect.ASPECT_ID, new List<MediaItemAspect> { episode } },
            { MovieAspect.ASPECT_ID, new List<MediaItemAspect> { movie } }
        };
        GiveTheServerItsFileSystem();

        Guid id = Guid.NewGuid();
        MediaItem item = new TestItem(id, aspects, CreateLocator(ResourcePath.BuildBaseProviderPath(
            LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID,
            "/" + media.Replace('\\', '/')).Serialize()));
        ActionResult<MediaController.ItemResult> action;
        MediaController.ItemResult result = GetItem(
            CreateFanArtLibrary(new List<MediaItem> { item }, new List<MediaItem>(), out _), id, out action);

        Assert.That(result.Overview, Is.EqualTo("What the database holds."),
            "the household's own note outranks the tool's words");
        Assert.That(result.RuntimeMinutes, Is.EqualTo(52), "the library's runtime is not replaced by the file's");
        Assert.That(result.Rating, Is.EqualTo(8));
        Assert.That(result.TotalRating, Is.Null,
            "the library rated this title itself, and the crowd's number does not speak over that");
        Assert.That(result.FirstAired, Is.EqualTo(new DateTime(2010, 6, 1)),
            "the date the library holds is the first aired date, even when the file knows another");
        Assert.That(result.EpisodeName, Is.EqualTo("Shut Down"),
            "the name the library does not hold is still filled - filling is what this is for");
      }
      finally
      {
        Directory.Delete(dir, true);
      }
    }

    [Test]
    public void AFileThatIsNotThereIsAnsweredWithNullsAndNoComplaint()
    {
      string dir = OwnFolder();
      try
      {
        string media = Path.Combine(dir, "Season 4", "Show - S04E03.mkv");
        WriteText(media, "mkv");
        GiveTheServerItsFileSystem();

        Guid id = Guid.NewGuid();
        ActionResult<MediaController.ItemResult> action;
        MediaController.ItemResult result = GetItem(
            CreateFanArtLibrary(new List<MediaItem> { EpisodeOnDisk(id, media, DateTime.MinValue) },
                new List<MediaItem>(), out _), id, out action);

        Assert.That(Status(action), Is.Null, "a title without metadata files is an ordinary title, not a failure");
        Assert.That(result.Overview, Is.Null);
        Assert.That(result.Genres, Is.Null);
        Assert.That(result.FirstAired, Is.Null);
        Assert.That(result.TotalRating, Is.Null);
      }
      finally
      {
        Directory.Delete(dir, true);
      }
    }

    [Test]
    public void APathNoProviderOnThisServerReachesCannotBringDownTheAnswer()
    {
      // The provider of RESOURCE_PATH is not registered here at all, and asking the filesystem machinery for it
      // throws rather than saying no - the details answer must not learn that by failing on a real server whose
      // titles come through a provider whose files cannot be read.
      Guid id = Guid.NewGuid();
      MediaItem item = new TestItem(id, new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { MediaAspect.ASPECT_ID, new List<MediaItemAspect>
              { CreateMediaAspect("Over The Stream", "Over The Stream", DateTime.MinValue) } }
      }, CreateLocator(RESOURCE_PATH));

      ActionResult<MediaController.ItemResult> action;
      MediaController.ItemResult result = GetItem(
          CreateFanArtLibrary(new List<MediaItem> { item }, new List<MediaItem>(), out _), id, out action);

      Assert.That(result.Title, Is.EqualTo("Over The Stream"),
          "the words the database holds still arrive; only the file's half of the answer is missing");
      Assert.That(result.Genres, Is.Null);
    }

    #endregion
  }
}
