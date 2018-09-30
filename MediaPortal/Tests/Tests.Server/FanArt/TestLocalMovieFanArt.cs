#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
  public class TestLocalMovieFanArt
  {
    MockResourceAccess _mockResourceAccess;
    MockFanArtCache _fanArtCache;

    [OneTimeSetUp]
    public void Setup()
    {
      _mockResourceAccess = new MockResourceAccess();
      _mockResourceAccess.Provider.AddDirectory("Movies/TestCollection", new[] { "thumb.png" });
      _mockResourceAccess.Provider.AddDirectory("Movies/TestCollection/ExtraFanArt", new[] { "fanart.png" });
      _mockResourceAccess.Provider.AddDirectory("Movies/TestCollection/TestMovie/", new[] { "poster.png", "movie.mkv" });
      _mockResourceAccess.Provider.AddDirectory("Movies/TestCollection/TestMovie/.actors", new[] { "movie_actor1-thumb.png", "movie_actor2-thumb.png" });
      _mockResourceAccess.Provider.AddDirectory("Movies/TestCollection/TestMovie/ExtraFanArt", new[] { "fanart.png" });

      _fanArtCache = new MockFanArtCache();
      ServiceRegistration.Set<IFanArtCache>(_fanArtCache);
    }

    [Test]
    public void TestFanArtExtractMovieFolderFanArt()
    {
      //Arrange
      _fanArtCache.Clear();
      Guid movieId = Guid.NewGuid();
      MovieFanArtHandlerForTests fh = new MovieFanArtHandlerForTests();

      //Act
      fh.TestExtractMovieFolderFanArt(movieId, ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, "Movies/TestCollection/TestMovie/movie.mkv")).Wait();

      //Assert
      List<string> fanart;
      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(movieId, out fanart));
      ICollection<string> fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Movies/TestCollection/TestMovie/poster.png");
      CollectionAssert.Contains(fanartBasePaths, "Movies/TestCollection/TestMovie/ExtraFanArt/fanart.png");
    }

    [Test]
    public void TestFanArtExtractMovieActorFolderFanArt()
    {
      //Arrange
      _fanArtCache.Clear();
      Guid movieId = Guid.NewGuid();

      Guid actor1Id = Guid.NewGuid();
      Guid actor2Id = Guid.NewGuid();
      IList<Tuple<Guid, string>> actors = new List<Tuple<Guid, string>>
      {
        new Tuple<Guid, string>(actor1Id, "Movie Actor1"),
        new Tuple<Guid, string>(actor2Id, "Movie Actor2")
      };

      MovieFanArtHandlerForTests fh = new MovieFanArtHandlerForTests();

      //Act
      fh.TestExtractMovieFolderFanArt(movieId, ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, "Movies/TestCollection/TestMovie/movie.mkv"), actors).Wait();

      //Assert
      List<string> fanart;
      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(actor1Id, out fanart));
      ICollection<string> fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Movies/TestCollection/TestMovie/.actors/movie_actor1-thumb.png");

      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(actor2Id, out fanart));
      fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Movies/TestCollection/TestMovie/.actors/movie_actor2-thumb.png");
    }

    [Test]
    public void TestFanArtExtractCollectionFolderFanArt()
    {
      //Arrange
      _fanArtCache.Clear();
      Guid collectionId = Guid.NewGuid();
      MovieFanArtHandlerForTests fh = new MovieFanArtHandlerForTests();

      //Act
      fh.TestExtractCollectionFolderFanArt(collectionId, ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, "Movies/TestCollection/TestMovie/movie.mkv")).Wait();

      //Assert
      List<string> fanart;
      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(collectionId, out fanart));
      ICollection<string> fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Movies/TestCollection/thumb.png");
      CollectionAssert.Contains(fanartBasePaths, "Movies/TestCollection/ExtraFanArt/fanart.png");
    }
  }
}
