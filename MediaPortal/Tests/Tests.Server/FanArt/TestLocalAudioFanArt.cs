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
  public class TestLocalAudioFanArt
  {
    MockResourceAccess _mockResourceAccess;
    MockFanArtCache _fanArtCache;

    [OneTimeSetUp]
    public void Setup()
    {
      _mockResourceAccess = new MockResourceAccess();
      _mockResourceAccess.Provider.AddDirectory("Audio/TestArtist", new[] { "thumb.png" });
      _mockResourceAccess.Provider.AddDirectory("Audio/TestArtist/ExtraFanArt", new[] { "fanart.png" });
      _mockResourceAccess.Provider.AddDirectory("Audio/TestArtist/TestAlbum/", new[] { "cover.png", "track1.mp3" });
      _mockResourceAccess.Provider.AddDirectory("Audio/TestArtist/TestAlbum/.artists", new[] { "album_artist1-thumb.png", "album_artist2-thumb.png" });
      _mockResourceAccess.Provider.AddDirectory("Audio/TestArtist/TestAlbum/ExtraFanArt", new[] { "fanart.png" });

      _fanArtCache = new MockFanArtCache();
      ServiceRegistration.Set<IFanArtCache>(_fanArtCache);
    }

    [Test]
    public void TestFanArtExtractAlbumFolderFanArt()
    {
      //Arrange
      _fanArtCache.Clear();
      Guid albumId = Guid.NewGuid();
      AudioFanArtHandlerForTests fh = new AudioFanArtHandlerForTests();

      //Act
      fh.TestExtractAlbumFolderFanArt(albumId, ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, "Audio/TestArtist/TestAlbum/")).Wait();

      //Assert
      List<string> fanart;
      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(albumId, out fanart));
      ICollection<string> fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Audio/TestArtist/TestAlbum/cover.png");
      CollectionAssert.Contains(fanartBasePaths, "Audio/TestArtist/TestAlbum/ExtraFanArt/fanart.png");
    }

    [Test]
    public void TestFanArtExtractAlbumArtistFolderFanArt()
    {
      //Arrange
      _fanArtCache.Clear();
      Guid albumId = Guid.NewGuid();

      Guid artist1Id = Guid.NewGuid();
      Guid artist2Id = Guid.NewGuid();
      IList<Tuple<Guid, string>> artists = new List<Tuple<Guid, string>>
      {
        new Tuple<Guid, string>(artist1Id, "Album Artist1"),
        new Tuple<Guid, string>(artist2Id, "Album Artist2")
      };

      AudioFanArtHandlerForTests fh = new AudioFanArtHandlerForTests();

      //Act
      fh.TestExtractAlbumFolderFanArt(albumId, ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, "Audio/TestArtist/TestAlbum/"), artists).Wait();

      //Assert
      List<string> fanart;
      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(artist1Id, out fanart));
      ICollection<string> fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Audio/TestArtist/TestAlbum/.artists/album_artist1-thumb.png");

      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(artist2Id, out fanart));
      fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Audio/TestArtist/TestAlbum/.artists/album_artist2-thumb.png");
    }

    [Test]
    public void TestFanArtExtractArtistFolderFanArt()
    {
      //Arrange
      _fanArtCache.Clear();

      Guid artist1Id = Guid.NewGuid();
      Guid artist2Id = Guid.NewGuid();
      IList<Tuple<Guid, string>> artists = new List<Tuple<Guid, string>>
      {
        new Tuple<Guid, string>(artist1Id, "TestArtist"),
        new Tuple<Guid, string>(artist2Id, "Album Artist2")
      };

      AudioFanArtHandlerForTests fh = new AudioFanArtHandlerForTests();

      //Act
      fh.TestExtractArtistFolderFanArt(ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, "Audio/TestArtist/TestAlbum/"), artists).Wait();

      //Assert
      List<string> fanart;
      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(artist1Id, out fanart));
      ICollection<string> fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Audio/TestArtist/thumb.png");
      CollectionAssert.Contains(fanartBasePaths, "Audio/TestArtist/ExtraFanArt/fanart.png");
    }
  }
}
