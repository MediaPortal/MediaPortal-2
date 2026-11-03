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

      // Two artists as a tagging tool writes them: one whose folder carries only its own picture under the
      // Kodi name, and one that also has a picture of its own.
      _mockResourceAccess.Provider.AddDirectory("Audio/FolderArtist", new[] { "folder.jpg", "fanart.jpg", "banner.jpg", "clearlogo.png", "clearart.png" });
      _mockResourceAccess.Provider.AddDirectory("Audio/FolderArtist/Album", new[] { "track1.mp3", "folder.jpg" });
      _mockResourceAccess.Provider.AddDirectory("Audio/FaceArtist", new[] { "thumb.jpg", "folder.jpg", "artist.jpg" });
      _mockResourceAccess.Provider.AddDirectory("Audio/FaceArtist/Album", new[] { "track1.mp3" });

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

    /// <summary>
    /// That the picture standing in an artist's own folder is filed as the artist's <b>Thumbnail</b>, which is
    /// the one and only kind anybody ever asks a person for: the skin's person tile reads
    /// <c>FanArtType="Thumbnail"</c> (themes/default/styles/CurrentlyPlayingStyles.xaml:220-222). Filing that
    /// picture under "Poster" - as the shared list of kinds did - left every artist tile showing the skin's
    /// standing figure although the cache held the picture.
    /// </summary>
    [Test]
    public void TestFanArtArtistFolderPictureIsStoredAsArtistsThumbnail()
    {
      //Arrange
      _fanArtCache.Clear();
      Guid artistId = Guid.NewGuid();
      IList<Tuple<Guid, string>> artists = new List<Tuple<Guid, string>>
      {
        new Tuple<Guid, string>(artistId, "FolderArtist")
      };
      AudioFanArtHandlerForTests fh = new AudioFanArtHandlerForTests();

      //Act
      fh.TestExtractArtistFolderFanArt(ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, "Audio/FolderArtist/Album/"), artists).Wait();

      //Assert
      List<string> stored;
      Assert.IsTrue(_fanArtCache.Kinds.TryGetValue(artistId, out stored));
      CollectionAssert.Contains(OfKind(stored, FanArtTypes.Thumbnail), "Audio/FolderArtist/folder.jpg",
          "the folder's own picture is the artist's portrait, and a person is only ever asked for its thumbnail");
      CollectionAssert.Contains(OfKind(stored, FanArtTypes.FanArt), "Audio/FolderArtist/fanart.jpg");
      CollectionAssert.Contains(OfKind(stored, FanArtTypes.Banner), "Audio/FolderArtist/banner.jpg");
      CollectionAssert.Contains(OfKind(stored, FanArtTypes.Logo), "Audio/FolderArtist/clearlogo.png");
      CollectionAssert.Contains(OfKind(stored, FanArtTypes.ClearArt), "Audio/FolderArtist/clearart.png");
    }

    /// <summary>
    /// That an artist's own picture is its portrait and the folder's picture only stands in for it. The cache
    /// real to this mock keeps a single thumbnail per artist, so the picture offered first is the one a front
    /// end sees; that is the order the reference's artist provider used
    /// (<c>Source/UI/FanArt/FanArtService.Local/LocalArtistFanartProvider.cs:163-166</c>).
    /// </summary>
    [Test]
    public void TestFanArtArtistOwnsItsFaceBeforeTheFoldersPicture()
    {
      //Arrange
      _fanArtCache.Clear();
      Guid artistId = Guid.NewGuid();
      IList<Tuple<Guid, string>> artists = new List<Tuple<Guid, string>>
      {
        new Tuple<Guid, string>(artistId, "FaceArtist")
      };
      AudioFanArtHandlerForTests fh = new AudioFanArtHandlerForTests();

      //Act
      fh.TestExtractArtistFolderFanArt(ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, "Audio/FaceArtist/Album/"), artists).Wait();

      //Assert
      List<string> stored;
      Assert.IsTrue(_fanArtCache.Kinds.TryGetValue(artistId, out stored));
      ICollection<string> offered = OfKind(stored, FanArtTypes.Thumbnail);
      CollectionAssert.Contains(offered, "Audio/FaceArtist/artist.jpg");
      CollectionAssert.Contains(offered, "Audio/FaceArtist/folder.jpg");
      Assert.AreEqual("Audio/FaceArtist/artist.jpg", offered.First(),
          "the artist's own face comes first; the folder's picture only fills in when there is none");
    }

    /// <summary>
    /// Returns the paths the mock cache was offered under one fanart kind, named as the other tests here name
    /// them ("Audio/TestArtist/thumb.png"), in the order they were offered.
    /// </summary>
    /// <param name="stored">Entries of the form "kind|path", as the mock cache records them</param>
    /// <param name="fanArtType">Kind to select</param>
    /// <returns>Paths stored under that kind</returns>
    private static ICollection<string> OfKind(IEnumerable<string> stored, string fanArtType)
    {
      string prefix = fanArtType + "|";
      return stored.Where(entry => entry.StartsWith(prefix))
          .Select(entry => ResourcePath.Deserialize(entry.Substring(prefix.Length)).BasePathSegment.Path)
          .ToList();
    }
  }
}
