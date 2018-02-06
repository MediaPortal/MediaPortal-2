#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Test.NfoMetadataExtractor
{
  [TestFixture]
  public class TestNfoMovieReader
  {
    [Test]
    public void TestNfoMovieReaderWriteMetadata()
    {
      MovieStub movieStub = CreateTestMovieStub();

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();

      //Video only
      NfoMovieReader reader = new NfoMovieReader(new ConsoleLogger(LogLevel.All, true), 1, true, false, false, null, new NfoMovieMetadataExtractorSettings());
      reader.GetMovieStubs().Add(movieStub);
      reader.TryWriteMetadata(aspects);

      MediaItemAspect mediaAspect = MediaItemAspect.GetAspect(aspects, MediaAspect.Metadata);
      Assert.NotNull(mediaAspect);
      Assert.AreEqual(movieStub.Title, mediaAspect.GetAttributeValue<string>(MediaAspect.ATTR_TITLE));
      Assert.AreEqual(movieStub.SortTitle, mediaAspect.GetAttributeValue<string>(MediaAspect.ATTR_SORT_TITLE));
      Assert.AreEqual(movieStub.Premiered, mediaAspect.GetAttributeValue<DateTime?>(MediaAspect.ATTR_RECORDINGTIME));
      Assert.AreEqual(movieStub.PlayCount, mediaAspect.GetAttributeValue<int?>(MediaAspect.ATTR_PLAYCOUNT));
      Assert.AreEqual(movieStub.LastPlayed, mediaAspect.GetAttributeValue<DateTime?>(MediaAspect.ATTR_LASTPLAYED));

      MediaItemAspect videoAspect = MediaItemAspect.GetAspect(aspects, VideoAspect.Metadata);
      Assert.NotNull(videoAspect);
      Assert.AreEqual(movieStub.Plot, videoAspect.GetAttributeValue<string>(VideoAspect.ATTR_STORYPLOT));
      CollectionAssert.AreEqual(movieStub.Actors.OrderBy(p => p.Order).Select(p => p.Name), videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_ACTORS));
      Assert.AreEqual(movieStub.Director, videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_DIRECTORS).First());
      CollectionAssert.AreEqual(movieStub.Credits, videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_WRITERS));

      //ToDo: Rework Genre Mapper to make it testable, currently depends on IPathManager/language files!
      //IList<MediaItemAspect> genreAspects;
      //Assert.IsTrue(aspects.TryGetValue(GenreAspect.ASPECT_ID, out genreAspects));
      //CollectionAssert.AreEqual(movieStub.Genres, genreAspects.Select(g => g.GetAttributeValue<string>(GenreAspect.ATTR_GENRE)));

      MediaItemAspect thumbnailAspect = MediaItemAspect.GetAspect(aspects, ThumbnailLargeAspect.Metadata);
      Assert.NotNull(thumbnailAspect);
      CollectionAssert.AreEqual(movieStub.Thumb, thumbnailAspect.GetAttributeValue<byte[]>(ThumbnailLargeAspect.ATTR_THUMBNAIL));

      //Movie only
      aspects.Clear();
      reader = new NfoMovieReader(new ConsoleLogger(LogLevel.All, true), 1, false, false, false, null, new NfoMovieMetadataExtractorSettings());
      reader.GetMovieStubs().Add(movieStub);
      reader.TryWriteMetadata(aspects);

      MediaItemAspect movieAspect = MediaItemAspect.GetAspect(aspects, MovieAspect.Metadata);
      Assert.NotNull(movieAspect);
      CollectionAssert.AreEqual(movieStub.Companies, movieAspect.GetCollectionAttribute<string>(MovieAspect.ATTR_COMPANIES));
      Assert.AreEqual(movieStub.Title, movieAspect.GetAttributeValue<string>(MovieAspect.ATTR_MOVIE_NAME));
      Assert.AreEqual(movieStub.OriginalTitle, movieAspect.GetAttributeValue<string>(MovieAspect.ATTR_ORIG_MOVIE_NAME));
      Assert.AreEqual(movieStub.Sets.OrderBy(set => set.Order).First().Name, movieAspect.GetAttributeValue<string>(MovieAspect.ATTR_COLLECTION_NAME));
      Assert.AreEqual((int)movieStub.Runtime.Value.TotalMinutes, movieAspect.GetAttributeValue<int?>(MovieAspect.ATTR_RUNTIME_M));
      Assert.AreEqual("US_PG", movieAspect.GetAttributeValue<string>(MovieAspect.ATTR_CERTIFICATION));
      Assert.AreEqual(movieStub.Tagline, movieAspect.GetAttributeValue<string>(MovieAspect.ATTR_TAGLINE));
      Assert.AreEqual((double)movieStub.Rating, movieAspect.GetAttributeValue<double?>(MovieAspect.ATTR_TOTAL_RATING));
      Assert.AreEqual(movieStub.Votes, movieAspect.GetAttributeValue<int?>(MovieAspect.ATTR_RATING_COUNT));

      IList<MediaItemAspect> externalIdentifiers;
      Assert.IsTrue(aspects.TryGetValue(ExternalIdentifierAspect.ASPECT_ID, out externalIdentifiers));
      Assert.IsTrue(TestUtils.HasExternalId(externalIdentifiers, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, movieStub.TmdbId.ToString()));
      Assert.IsTrue(TestUtils.HasExternalId(externalIdentifiers, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COLLECTION, movieStub.TmdbCollectionId.ToString()));
      Assert.IsTrue(TestUtils.HasExternalId(externalIdentifiers, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, movieStub.Id));
      Assert.IsTrue(TestUtils.HasExternalId(externalIdentifiers, ExternalIdentifierAspect.SOURCE_ALLOCINE, ExternalIdentifierAspect.TYPE_MOVIE, movieStub.Allocine.ToString()));
      Assert.IsTrue(TestUtils.HasExternalId(externalIdentifiers, ExternalIdentifierAspect.SOURCE_CINEPASSION, ExternalIdentifierAspect.TYPE_MOVIE, movieStub.Cinepassion.ToString()));
    }

    [Test]
    public void TestNfoMovieReaderReadMetadata()
    {
      //Arrange
      var resourceProviders = new Dictionary<Guid, IResourceProvider>();
      resourceProviders.Add(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, new LocalFsResourceProvider());
      var mockMediaAccessor = new Mock<IMediaAccessor>();
      ServiceRegistration.Set(mockMediaAccessor.Object);
      mockMediaAccessor.Setup(x => x.LocalResourceProviders).Returns(resourceProviders);

      Mock <IFileSystemResourceAccessor> mockRA = new Mock<IFileSystemResourceAccessor>();
      mockRA.Setup(r => r.OpenReadAsync()).Returns(Task.FromResult(
        Assembly.GetExecutingAssembly().GetManifestResourceStream("Test.NfoMetadataExtractor.TestData.MovieNfo.movie.nfo")));
      mockRA.SetupGet(t => t.CanonicalLocalResourcePath).Returns(ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, @"TestData\MovieNfo\movie.nfo"));
      NfoMovieReader reader = new NfoMovieReader(new ConsoleLogger(LogLevel.All, true), 1, false, false, false, null, new NfoMovieMetadataExtractorSettings());

      //Act
      reader.TryReadMetadataAsync(mockRA.Object).Wait();
      var stubs = reader.GetMovieStubs();

      //Assert
      Assert.NotNull(stubs);
      Assert.AreEqual(1, stubs.Count);
      var stub = stubs[0];
      Assert.NotNull(stub);
      Assert.AreEqual("The Lego Batman Movie", stub.Title);
      Assert.AreEqual("The Lego Batman Movie", stub.OriginalTitle);
      Assert.AreEqual(7.2m, stub.Rating);
      Assert.AreEqual(new DateTime(2017, 1, 1), stub.Year);
      Assert.AreEqual(1853, stub.Votes);
      Assert.AreEqual("In the irreverent spirit of fun that made “The Lego Movie” a worldwide phenomenon, the self-described leading man of that ensemble—Lego Batman—stars in his own big-screen adventure. But there are big changes...", stub.Outline);
      Assert.AreEqual("In the irreverent spirit of fun that made “The Lego Movie” a worldwide phenomenon, the self-described leading man of that ensemble—Lego Batman—stars in his own big-screen adventure. But there are big changes brewing in Gotham, and if he wants to save the city from The Joker’s hostile takeover, Batman may have to drop the lone vigilante thing, try to work with others and maybe, just maybe, learn to lighten up.", stub.Plot);
      Assert.AreEqual("Always be yourself. Unless you can be Batman.", stub.Tagline);
      Assert.AreEqual(TimeSpan.FromMinutes(104), stub.Runtime);
      CollectionAssert.AreEqual(new[] { "UK:U" }, stub.Mpaa);
      CollectionAssert.AreEqual(new[] { "UK:U" }, stub.Certification);
      Assert.AreEqual("tt4116284", stub.Id);
      Assert.AreEqual(324849, stub.TmdbId);
      CollectionAssert.AreEqual(new[] { "Denmark", "United States" }, stub.Countries);
      Assert.AreEqual(false, stub.Watched);
      Assert.AreEqual(0, stub.PlayCount);
      CollectionAssert.AreEqual(new[] { "Action", "Animation", "Comedy", "Family", "Fantasy" }, stub.Genres);
      CollectionAssert.AreEqual(new[] { "Lin Pictures", "Warner Bros. Animation", "Warner Bros.", "Animal Logic", "DC Entertainment", "Lord Miller", "LEGO System A", "S" }, stub.Studios);
      CollectionAssert.AreEqual(new[] { "Chris McKenna", "Erik Sommers", "Seth Grahame-Smith", "Jared Stern", "John Whittington" }, stub.Credits);
      Assert.AreEqual("Chris McKay", stub.Director);
      CollectionAssert.AreEqual(new[] { "English" }, stub.Languages);
    }

    protected MovieStub CreateTestMovieStub()
    {
      HashSet<PersonStub> actors = new HashSet<PersonStub>(new[]
      {
        new PersonStub
        {
          Name = "Actor1",
          Role = "Role1"
        },
        new PersonStub
        {
          Name = "Actor2",
          Role = "Role2"
      }});

      HashSet<SetStub> sets = new HashSet<SetStub>(new[]
      {
        new SetStub
        {
          Name = "Set2",
          Order = 2,
          TmdbId = 5
        },
        new SetStub
        {
          Name = "Set1",
          Order = 1,
          TmdbId = 6
      }});

      return new MovieStub
      {
        Title = "Test Movie",
        SortTitle = "Test Movie Sort",
        Premiered = new DateTime(2000, 12, 31),
        Year = new DateTime(2000, 1, 1),
        PlayCount = 10,
        Watched = true,
        LastPlayed = new DateTime(2000, 1, 31),
        Genres = new HashSet<string>(new[] { "Action", "Comedy" }),
        Actors = actors,
        Director = "TestDirector",
        DirectorImdb = "DirectorId",
        Credits = new HashSet<string>(new[] { "Writer1", "Writer2" }),
        Plot = "TestPlot",
        Outline = "TestOutline",
        Companies = new HashSet<string>(new[] { "Company1", "Company2" }),
        Studios = new HashSet<string>(new[] { "Studio1", "Studio2" }),
        OriginalTitle = "Test Original Title",
        TmdbId = 1,
        Thmdb = 2,
        IdsTmdbId = 3,
        TmdbCollectionId = 4,
        Sets = sets,
        Id = "Imdb1",
        Imdb = "Imdb2",
        IdsImdbId = "Imdb3",
        Allocine = 7,
        Cinepassion = 8,
        Runtime = TimeSpan.FromHours(1),
        Certification = new HashSet<string>(new[] { "PG", "18" }),
        Mpaa = new HashSet<string>(new[] { "12A", "15" }),
        Tagline = "TestTagline",
        Rating = 5,
        Ratings = new Dictionary<string, decimal> { { "tmdb", 2 }, { "imdb", 3 } },
        Votes = 10,
        Thumb = new byte[] { 0x01, 0x02, 0x03 }
      };
    }
  }
}
