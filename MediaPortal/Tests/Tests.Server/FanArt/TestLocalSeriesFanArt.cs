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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.ResourceAccess;
using NUnit.Framework;
using Tests.Server.FanArt.FanArtHandlersForTests;
using Tests.Server.FanArt.MockFanArtAccess;

namespace Tests.Server.FanArt
{
  [TestFixture]
  public class TestLocalSeriesFanArt
  {
    #region Test data

    static string[] TEST_ADDITIONAL_SEASON_1_FANART_PATHS =
    {
      "Test/season01-thumb.jpg",
      "Test/season01-poster.jpg",
      "Test/season01-folder.jpg",
      "Test/season01-cover.jpg",
      "Test/season01-logo.jpg",
      "Test/season01-clearart.jpg",
      "Test/season01-cdart.jpg",
      "Test/season01-discart.jpg",
      "Test/season01-disc.jpg",
      "Test/season01-banner.jpg",
      "Test/season01-backdrop.jpg",
      "Test/season01-fanart.jpg"
    };

    static string[] TEST_ADDITIONAL_SEASON_0_FANART_PATHS =
    {
      "Test/season00-thumb.jpg",
      "Test/season00-poster.jpg",
      "Test/season00-folder.jpg",
      "Test/season00-cover.jpg",
      "Test/season00-logo.jpg",
      "Test/season00-clearart.jpg",
      "Test/season00-cdart.jpg",
      "Test/season00-discart.jpg",
      "Test/season00-disc.jpg",
      "Test/season00-banner.jpg",
      "Test/season00-backdrop.jpg",
      "Test/season00-fanart.jpg"
    };

    static string[] TEST_ADDITIONAL_SEASON_ALL_FANART_PATHS =
    {
      "Test/season-all-thumb.jpg",
      "Test/season-all-poster.jpg",
      "Test/season-all-folder.jpg",
      "Test/season-all-cover.jpg",
      "Test/season-all-logo.jpg",
      "Test/season-all-clearart.jpg",
      "Test/season-all-cdart.jpg",
      "Test/season-all-discart.jpg",
      "Test/season-all-disc.jpg",
      "Test/season-all-banner.jpg",
      "Test/season-all-backdrop.jpg",
      "Test/season-all-fanart.jpg",
    };

    static string[] TEST_ADDITIONAL_SEASON_SPECIALS_FANART_PATHS =
    {
      "Test/season-specials-thumb.jpg",
      "Test/season-specials-poster.jpg",
      "Test/season-specials-folder.jpg",
      "Test/season-specials-cover.jpg",
      "Test/season-specials-logo.jpg",
      "Test/season-specials-clearart.jpg",
      "Test/season-specials-cdart.jpg",
      "Test/season-specials-discart.jpg",
      "Test/season-specials-disc.jpg",
      "Test/season-specials-banner.jpg",
      "Test/season-specials-backdrop.jpg",
      "Test/season-specials-fanart.jpg",
    };

    #endregion

    MockResourceAccess _mockResourceAccess;
    MockFanArtCache _fanArtCache;

    [OneTimeSetUp]
    public void Setup()
    {
      _mockResourceAccess = new MockResourceAccess();
      _mockResourceAccess.Provider.AddDirectory("Series/TestSeries", new[] { "poster.png", "season01-thumb.png", "season-all-fanart.png", "season-specials-fanart.png" });
      _mockResourceAccess.Provider.AddDirectory("Series/TestSeries/.actors", new[] { "series_actor1-thumb.png", "series_actor2-thumb.png" });
      _mockResourceAccess.Provider.AddDirectory("Series/TestSeries/ExtraFanArt", new[] { "fanart.png" });
      _mockResourceAccess.Provider.AddDirectory("Series/TestSeries/Season 1", new[] { "poster.png", "episode1-thumb.png", "episode1.mkv" });
      _mockResourceAccess.Provider.AddDirectory("Series/TestSeries/Season 1/.actors", new[] { "season_actor1-thumb.png", "season_actor2-thumb.png" });

      _fanArtCache = new MockFanArtCache();
      ServiceRegistration.Set<IFanArtCache>(_fanArtCache);
    }

    [Test]
    public void TestFanArtExtractEpisodeFolderFanArt()
    {
      //Arrange
      _fanArtCache.Clear();
      Guid episodeId = Guid.NewGuid();
      SeriesFanArtHandlerForTests fh = new SeriesFanArtHandlerForTests();

      //Act
      fh.TestExtractEpisodeFolderFanArt(episodeId, ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, "Series/TestSeries/Season 1/episode1.mkv")).Wait();

      //Assert
      List<string> fanart;
      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(episodeId, out fanart));
      ICollection<string> fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Series/TestSeries/Season 1/episode1-thumb.png");
    }

    [Test]
    public void TestFanArtExtractSeriesFolderFanArt()
    {
      //Arrange
      _fanArtCache.Clear();
      Guid seriesId = Guid.NewGuid();
      SeriesFanArtHandlerForTests fh = new SeriesFanArtHandlerForTests();

      //Act
      fh.TestExtractSeriesFolderFanArt(seriesId, ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, "Series/TestSeries/Season 1/episode1.mkv")).Wait();

      //Assert
      List<string> fanart;
      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(seriesId, out fanart));
      ICollection<string> fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Series/TestSeries/poster.png");
      CollectionAssert.Contains(fanartBasePaths, "Series/TestSeries/ExtraFanArt/fanart.png");
    }

    [Test]
    public void TestFanArtExtractSeriesActorFolderFanArt()
    {
      //Arrange
      _fanArtCache.Clear();
      Guid seriesId = Guid.NewGuid();

      Guid actor1Id = Guid.NewGuid();
      Guid actor2Id = Guid.NewGuid();
      IList<Tuple<Guid, string>> actors = new List<Tuple<Guid, string>>
      {
        new Tuple<Guid, string>(actor1Id, "Series Actor1"),
        new Tuple<Guid, string>(actor2Id, "Series Actor2")
      };

      SeriesFanArtHandlerForTests fh = new SeriesFanArtHandlerForTests();

      //Act
      fh.TestExtractSeriesFolderFanArt(seriesId, ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, "Series/TestSeries/Season 1/episode1.mkv"), actors).Wait();

      //Assert
      List<string> fanart;
      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(actor1Id, out fanart));
      ICollection<string> fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Series/TestSeries/.actors/series_actor1-thumb.png");

      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(actor2Id, out fanart));
      fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Series/TestSeries/.actors/series_actor2-thumb.png");
    }

    [Test]
    public void TestFanArtExtractSeasonFolderFanArt()
    {
      //Arrange
      _fanArtCache.Clear();
      Guid seasonId = Guid.NewGuid();
      SeriesFanArtHandlerForTests fh = new SeriesFanArtHandlerForTests();

      //Act
      fh.TestExtractSeasonFolderFanArt(seasonId, ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, "Series/TestSeries/Season 1/episode1.mkv"), 1).Wait();

      //Assert
      List<string> fanart;
      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(seasonId, out fanart));
      ICollection<string> fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Series/TestSeries/Season 1/poster.png");
      CollectionAssert.Contains(fanartBasePaths, "Series/TestSeries/season01-thumb.png");
      CollectionAssert.Contains(fanartBasePaths, "Series/TestSeries/season-all-fanart.png");
    }

    [Test]
    public void TestFanArtExtractSeasonActorFolderFanArt()
    {
      //Arrange
      _fanArtCache.Clear();
      Guid seasonId = Guid.NewGuid();

      Guid actor1Id = Guid.NewGuid();
      Guid actor2Id = Guid.NewGuid();
      IList<Tuple<Guid, string>> actors = new List<Tuple<Guid, string>>
      {
        new Tuple<Guid, string>(actor1Id, "Season Actor1"),
        new Tuple<Guid, string>(actor2Id, "Season Actor2")
      };

      SeriesFanArtHandlerForTests fh = new SeriesFanArtHandlerForTests();

      //Act
      fh.TestExtractSeasonFolderFanArt(seasonId, ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, "Series/TestSeries/Season 1/episode1.mkv"), 1, actors).Wait();

      //Assert
      List<string> fanart;
      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(actor1Id, out fanart));
      ICollection<string> fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Series/TestSeries/Season 1/.actors/season_actor1-thumb.png");

      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(actor2Id, out fanart));
      fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Series/TestSeries/Season 1/.actors/season_actor2-thumb.png");
    }

    static object[] AdditionalSeasonFanArtTestCases = new[]
    {
      //Season number == 1, should return all season01 and season-all paths
      //and no season00 and season-specials paths
      new object[]{ TEST_ADDITIONAL_SEASON_1_FANART_PATHS, 1, 12 },
      new object[]{ TEST_ADDITIONAL_SEASON_0_FANART_PATHS, 1, 0 },
      new object[]{ TEST_ADDITIONAL_SEASON_ALL_FANART_PATHS, 1, 12 },
      new object[]{ TEST_ADDITIONAL_SEASON_SPECIALS_FANART_PATHS, 1, 0 },

      //Season number == 0, should return all season00 and season-specials paths
      //and no season01 and season-all paths
      new object[]{ TEST_ADDITIONAL_SEASON_1_FANART_PATHS, 0, 0 },
      new object[]{ TEST_ADDITIONAL_SEASON_0_FANART_PATHS, 0, 12 },
      new object[]{ TEST_ADDITIONAL_SEASON_ALL_FANART_PATHS, 0, 0 },
      new object[]{ TEST_ADDITIONAL_SEASON_SPECIALS_FANART_PATHS, 0, 12 }
    };

    [Test]
    [TestCaseSource("AdditionalSeasonFanArtTestCases")]
    public void TestFanArtGetAdditionalSeasonFolderFanArt(string[] paths, int seasonNumber, int expectedCount)
    {
      //Arrange
      List<ResourcePath> resourcePaths = paths.Select(p => ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, p)).ToList();
      SeriesFanArtHandlerForTests fh = new SeriesFanArtHandlerForTests();

      //Act
      FanArtPathCollection fanart = fh.TestGetAdditionalSeasonFolderFanArt(resourcePaths, seasonNumber);

      //Assert
      Assert.AreEqual(expectedCount, fanart.Sum(kvp => kvp.Value.Count));
    }
  }
}
