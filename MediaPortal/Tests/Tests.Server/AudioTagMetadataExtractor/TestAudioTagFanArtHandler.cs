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
using System.Net;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor;
using Moq;
using NUnit.Framework;

namespace Tests.Server.AudioTagMetadataExtractor
{
  /// <summary>
  /// Tests for the fanart handler of the audio tag plugin: the cover a user pasted into their files has to end up
  /// in the fanart cache under the album, which is where the picture endpoints of the API look for it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Everything here runs against real files on disc and the real resource access of the local filesystem provider,
  /// because the question this handler answers is a question about files: whether a picture can be taken out of an
  /// ID3 <c>APIC</c> frame, and whether a folder that holds <c>folder.jpg</c> is recognised as holding one. The
  /// cache itself is a stand-in that writes into a temporary folder the way <c>FanArtCache</c> does - the cache's
  /// own business of how many pictures of a kind to keep is <see cref="FanArtCache"/>'s to test, and is not what
  /// these tests are about.
  /// </para>
  /// <para>
  /// The tracks are the same fixtures the metadata tests use, written into a folder that plays the music share.
  /// Their aspects are built the way the importer leaves them, because the handler reads the album's id out of the
  /// relationship the importer wrote and nothing else.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestAudioTagFanArtHandler
  {
    #region Fixtures

    private string _share;
    private string _cacheRoot;
    private FanArtCacheOnDisk _cache;
    private AudioTagFanArtHandler _handler;
    private Guid _albumId;

    [SetUp]
    public void GiveTheHandlerAShareAndACache()
    {
      _share = Path.Combine(Path.GetTempPath(), "mp2-tagcovers-" + Guid.NewGuid());
      _cacheRoot = Path.Combine(_share, "FanArt");
      Directory.CreateDirectory(_cacheRoot);

      _cache = new FanArtCacheOnDisk(_cacheRoot);
      ServiceRegistration.Set<IFanArtCache>(_cache);
      ServiceRegistration.Set<ILogger>(new NoLogger());

      // The local files are opened through the real resource access, which asks these two who owns a path. Both
      // answers say "this machine", which is what a server reading its own music share is.
      var accessor = new Mock<IMediaAccessor>();
      accessor.Setup(get => get.LocalResourceProviders)
          .Returns(new Dictionary<Guid, IResourceProvider>
          {
              { LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, new LocalFsResourceProvider() }
          });
      ServiceRegistration.Set<IMediaAccessor>(accessor.Object);

      var resolver = new Mock<ISystemResolver>();
      resolver.Setup(get => get.GetSystemNameForSystemId(It.IsAny<string>()))
          .Returns(new MediaPortal.Common.General.SystemName(IPAddress.Loopback.ToString()));
      ServiceRegistration.Set<ISystemResolver>(resolver.Object);

      _handler = new AudioTagFanArtHandler();
      _albumId = Guid.NewGuid();
    }

    [TearDown]
    public void TakeTheShareAndTheCacheAwayAgain()
    {
      ServiceRegistration.Remove<IFanArtCache>();
      ServiceRegistration.Remove<IMediaAccessor>();
      ServiceRegistration.Remove<ISystemResolver>();
      try
      {
        if (Directory.Exists(_share))
          Directory.Delete(_share, true);
      }
      catch (IOException)
      {
        // A leftover folder of a finished test is of no consequence; the next test builds its own.
      }
    }

    /// <summary>
    /// Writing one track of the fixture album into the share, and building the aspects the import would have left
    /// behind for it: its own audio aspect, the path its file is at, and the link to the album.
    /// </summary>
    /// <param name="fixture">Name of the bundled audio file to write.</param>
    /// <param name="fileName">Name to give it in the share.</param>
    /// <param name="albumFolder">Folder of the share the file lies in.</param>
    /// <param name="albumId">Album the track is linked to; pass <c>null</c> for a track that belongs to none.</param>
    /// <param name="virtualResource">Whether the library only knows the title without having its file.</param>
    private IDictionary<Guid, IList<MediaItemAspect>> GivenATrack(string fixture, string fileName,
        string albumFolder = "", Guid? albumId = null, bool virtualResource = false)
    {
      string folder = Path.Combine(_share, "Music", albumFolder);
      Directory.CreateDirectory(folder);
      string file = Path.Combine(folder, fileName);
      File.WriteAllBytes(file, Read(fixture));

      SingleMediaItemAspect audio = new SingleMediaItemAspect(AudioAspect.Metadata);
      audio.SetAttribute(AudioAspect.ATTR_ALBUM, "Fixture Album");

      MultipleMediaItemAspect provider = new MultipleMediaItemAspect(ProviderResourceAspect.Metadata);
      provider.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID,
          LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID.ToString());
      provider.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH,
          LocalFsResourceProviderBase.ToResourcePath(file).Serialize());
      provider.SetAttribute(ProviderResourceAspect.ATTR_TYPE,
          virtualResource ? ProviderResourceAspect.TYPE_VIRTUAL : ProviderResourceAspect.TYPE_PRIMARY);

      var aspects = new Dictionary<Guid, IList<MediaItemAspect>>
      {
          { AudioAspect.ASPECT_ID, new List<MediaItemAspect> { audio } },
          { ProviderResourceAspect.ASPECT_ID, new List<MediaItemAspect> { provider } }
      };

      if (albumId.HasValue)
      {
        MultipleMediaItemAspect relationship = new MultipleMediaItemAspect(RelationshipAspect.Metadata);
        relationship.SetAttribute(RelationshipAspect.ATTR_ROLE, AudioAspect.ROLE_TRACK);
        relationship.SetAttribute(RelationshipAspect.ATTR_LINKED_ROLE, AudioAlbumAspect.ROLE_ALBUM);
        relationship.SetAttribute(RelationshipAspect.ATTR_LINKED_ID, albumId.Value);
        aspects[RelationshipAspect.ASPECT_ID] = new List<MediaItemAspect> { relationship };
      }

      return aspects;
    }

    /// <summary>
    /// Asking the handler about a track the way the server does, and handing back what the album got.
    /// </summary>
    private async Task Collect(Guid trackId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      await _handler.CollectFanArtAsync(trackId, aspects).ConfigureAwait(false);
    }

    private static byte[] Read(string name)
    {
      string resourceName = typeof(TestAudioTagFanArtHandler).Assembly.GetManifestResourceNames()
          .Single(n => n.EndsWith("." + name, StringComparison.Ordinal));
      using (Stream stream = typeof(TestAudioTagFanArtHandler).Assembly.GetManifestResourceStream(resourceName))
      using (MemoryStream memory = new MemoryStream())
      {
        stream.CopyTo(memory);
        return memory.ToArray();
      }
    }

    /// <summary>
    /// The bytes of the picture a fixture carries, so that a test can say the stored cover is that picture and not
    /// merely some picture.
    /// </summary>
    private static TagLib.IPicture PictureOf(string fixture)
    {
      string file = Path.Combine(Path.GetTempPath(), "mp2-tagcovers-read-" + Guid.NewGuid() + ".mp3");
      File.WriteAllBytes(file, Read(fixture));
      try
      {
        using (TagLib.File tag = TagLib.File.Create(file))
          return tag.Tag.Pictures.FirstOrDefault();
      }
      finally
      {
        // TagLib keeps a file open until its reference is released, which can outlive this test; a leftover of a
        // few kilobytes in the temporary folder is the lesser trouble.
        try
        {
          File.Delete(file);
        }
        catch (IOException) { }
      }
    }

    #endregion

    #region The picture inside the file

    [Test]
    public async Task ATrackCarryingAPictureLeavesThatPictureOnItsAlbum()
    {
      await Collect(Guid.NewGuid(), GivenATrack("cover.mp3", "track01.mp3", "Fixture Album", _albumId));

      IList<string> covers = _cache.GetFanArtFiles(_albumId, FanArtTypes.Cover);
      Assert.That(covers, Has.Count.EqualTo(1), "the front cover in the tag is the album's cover");
      Assert.That(Path.GetExtension(covers[0]), Is.EqualTo(".jpg"),
          "a picture keeps the format its bytes are already in, and the cache only finds .jpg/.png/.tbn");

      byte[] stored = File.ReadAllBytes(covers[0]);
      byte[] embedded = PictureOf("cover.mp3").Data.Data;
      Assert.That(stored, Is.EqualTo(embedded),
          "the shelf wears the user's own cover, not a re-encoded one");

      Assert.That(_cache.GetFanArtFiles(_albumId, FanArtTypes.Poster), Has.Count.EqualTo(1),
          "the same picture answers the name that film shelves already ask by");
      Assert.That(_cache.Writes, Has.Some.Contains(FanArtCacheOnDisk.IdOf(_albumId)).And.Contains(FanArtTypes.Cover));
      Assert.That(_cache.Writes, Has.None.Contains(FanArtCacheOnDisk.IdOf(Guid.Empty)),
          "a track is never given a cover of its own - the album holds it");
    }

    [Test]
    public async Task APictureUnderTheOtherTypeIsTakenForTheCover()
    {
      // Taggers that do not know the front-cover type write their picture as "other", which is common enough that
      // the reference counts it as a cover too.
      await Collect(Guid.NewGuid(), GivenATrack("othercover.mp3", "track01.mp3", "Fixture Album", _albumId));

      IList<string> covers = _cache.GetFanArtFiles(_albumId, FanArtTypes.Cover);
      Assert.That(covers, Has.Count.EqualTo(1));
      Assert.That(Path.GetExtension(covers[0]), Is.EqualTo(".png"), "a PNG stays a PNG");
      Assert.That(File.ReadAllBytes(covers[0]), Is.EqualTo(PictureOf("othercover.mp3").Data.Data));
    }

    [Test]
    public async Task AFileWithoutAPictureIsAskedAboutTheFolderItLiesIn()
    {
      // What a library tagged by an outside tool holds: no picture in the files, one JPEG next to them.
      IDictionary<Guid, IList<MediaItemAspect>> aspects =
          GivenATrack("untagged.mp3", "track01.mp3", "Fixture Album", _albumId);
      byte[] poster = PictureOf("cover.mp3").Data.Data;
      File.WriteAllBytes(Path.Combine(_share, "Music", "Fixture Album", "folder.jpg"), poster);

      await Collect(Guid.NewGuid(), aspects);

      IList<string> covers = _cache.GetFanArtFiles(_albumId, FanArtTypes.Cover);
      Assert.That(covers, Has.Count.EqualTo(1),
          "folder.jpg is a poster, and a poster of an album is called a cover");
      Assert.That(File.ReadAllBytes(covers[0]), Is.EqualTo(poster));
    }

    [Test]
    public async Task AFolderOfADiscIsWalkedOneLevelUpForTheAlbumsArt()
    {
      // MusicBrainz Picard writes the art of a set one level above the folders of its discs.
      IDictionary<Guid, IList<MediaItemAspect>> aspects =
          GivenATrack("untagged.mp3", "track01.mp3", Path.Combine("Fixture Album", "CD1"), _albumId);
      byte[] poster = PictureOf("cover.mp3").Data.Data;
      File.WriteAllBytes(Path.Combine(_share, "Music", "Fixture Album", "cover.jpg"), poster);

      await Collect(Guid.NewGuid(), aspects);

      Assert.That(_cache.GetFanArtFiles(_albumId, FanArtTypes.Cover), Has.Count.EqualTo(1),
          "a folder called CD1 holds a disc, so the album's art is looked for above it");
    }

    [Test]
    public async Task AFileWithNeitherAPictureNorAFolderArtGivesTheShelfNothing()
    {
      await Collect(Guid.NewGuid(), GivenATrack("untagged.mp3", "track01.mp3", "Fixture Album", _albumId));

      Assert.That(_cache.GetFanArtFiles(_albumId, FanArtTypes.Cover), Has.Count.EqualTo(0));
      Assert.That(_cache.Writes, Has.Count.EqualTo(0),
          "an album without art is drawn as the skin's placeholder rather than with someone else's picture");
    }

    #endregion

    #region Not doing the same work twice

    [Test]
    public async Task AnAlbumWhoseCoverIsStoredIsNotReadAgain()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects =
          GivenATrack("cover.mp3", "track01.mp3", "Fixture Album", _albumId);
      await Collect(Guid.NewGuid(), aspects);
      int afterFirst = _cache.Writes.Count;

      // A fresh handler is the next warming pass; the cache is what tells it that this album is done.
      _handler = new AudioTagFanArtHandler();
      await Collect(Guid.NewGuid(), GivenATrack("cover.mp3", "track01.mp3", "Fixture Album", _albumId));

      Assert.That(_cache.Writes, Has.Count.EqualTo(afterFirst),
          "a pass over a library whose covers are known must not open a thousand files for forty pictures");
      Assert.That(_cache.GetFanArtFiles(_albumId, FanArtTypes.Cover), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task TheSecondTrackOfAnAlbumDoesNotOpenTheFilesAgain()
    {
      await Collect(Guid.NewGuid(), GivenATrack("cover.mp3", "track01.mp3", "Fixture Album", _albumId));
      int afterFirst = _cache.Writes.Count;

      // The second track is the one that would have had nothing in it; the album is already known, so it is not
      // even opened.
      await Collect(Guid.NewGuid(), GivenATrack("untagged.mp3", "track02.mp3", "Fixture Album", _albumId));

      Assert.That(_cache.Writes, Has.Count.EqualTo(afterFirst),
          "one album is looked at once, whatever number of tracks it has");
    }

    [Test]
    public async Task AnAlbumWithoutArtIsNotLookedForAgainEither()
    {
      await Collect(Guid.NewGuid(), GivenATrack("untagged.mp3", "track01.mp3", "Fixture Album", _albumId));
      await Collect(Guid.NewGuid(), GivenATrack("untagged.mp3", "track02.mp3", "Fixture Album", _albumId));

      Assert.That(_cache.Writes, Has.Count.EqualTo(0));
      // Remembering the failure is the point: the second track above was answered without a folder being walked,
      // which only shows up as the library staying as fast as it is small here.
    }

    #endregion

    #region Titles the handler has nothing to say about

    [Test]
    public async Task ATrackOfNoAlbumKeepsItsPictureToItself()
    {
      await Collect(Guid.NewGuid(), GivenATrack("cover.mp3", "track01.mp3", "Fixture Album", null));

      Assert.That(_cache.Writes, Has.Count.EqualTo(0),
          "a picture of a track that belongs to no album has no album to be stored under");
    }

    [Test]
    public async Task AVirtualTrackIsNotRead()
    {
      // A webradio entry is a title the library only knows about; there is no file here that a picture could be
      // taken out of, and asking for one would reach out over the network.
      await Collect(Guid.NewGuid(),
          GivenATrack("cover.mp3", "track01.mp3", "Fixture Album", _albumId, virtualResource: true));

      Assert.That(_cache.Writes, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task ATitlesThatIsNoAudioIsLeftToTheHandlersThatKnowIt()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects =
          GivenATrack("cover.mp3", "track01.mp3", "Fixture Album", _albumId);
      aspects.Remove(AudioAspect.ASPECT_ID);

      await Collect(Guid.NewGuid(), aspects);

      Assert.That(_cache.Writes, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task ForgettingAnAlbumTakesItsPicturesWithIt()
    {
      await Collect(Guid.NewGuid(), GivenATrack("cover.mp3", "track01.mp3", "Fixture Album", _albumId));
      Assert.That(_cache.GetFanArtFiles(_albumId, FanArtTypes.Cover), Has.Count.EqualTo(1));

      _handler.DeleteFanArt(_albumId);

      Assert.That(_cache.GetFanArtFiles(_albumId, FanArtTypes.Cover), Has.Count.EqualTo(0),
          "a retagged or deleted album must not keep showing the picture it had");

      // The id is forgotten as well, so the album is looked at again on the next pass.
      await Collect(Guid.NewGuid(), GivenATrack("cover.mp3", "track01.mp3", "Fixture Album", _albumId));
      Assert.That(_cache.GetFanArtFiles(_albumId, FanArtTypes.Cover), Has.Count.EqualTo(1));
    }

    #endregion

    #region The rules of the walk

    [Test]
    public void AFolderNamedLikeADiscIsADiscUnlessTheAlbumIsToo()
    {
      Assert.That(AudioTagFanArtHandler.IsDiscFolder("Fixture Album", "CD1"), Is.True);
      Assert.That(AudioTagFanArtHandler.IsDiscFolder("Fixture Album", "disc 2"), Is.True);
      Assert.That(AudioTagFanArtHandler.IsDiscFolder("Fixture Album", "Deluxe Edition"), Is.False);
      Assert.That(AudioTagFanArtHandler.IsDiscFolder("CD", "CD"), Is.False,
          "CD is an album of its own, and its folder should not be walked past on the way up");
      Assert.That(AudioTagFanArtHandler.IsDiscFolder("DVD", "DVD"), Is.False);
    }

    [Test]
    public void ThePictureThatIsNotAFrontCoverIsStillAPicture()
    {
      // ReadTagPicture is the reference's order: front cover, then "other", then whatever the tag holds first.
      string file = Path.Combine(_share, "probe.mp3");
      File.WriteAllBytes(file, Read("othercover.mp3"));
      using (IFileSystemResourceAccessor fsra = new LocalFsResourceAccessor(new LocalFsResourceProvider(),
                 LocalFsResourceProviderBase.ToProviderPath(file)))
      {
        TagLib.IPicture picture = AudioTagFanArtHandler.ReadTagPicture(fsra);
        Assert.That(picture, Is.Not.Null);
        Assert.That(picture.Data.Data, Is.EqualTo(PictureOf("othercover.mp3").Data.Data));
      }

      File.WriteAllBytes(file, Read("untagged.mp3"));
      using (IFileSystemResourceAccessor fsra = new LocalFsResourceAccessor(new LocalFsResourceProvider(),
                 LocalFsResourceProviderBase.ToProviderPath(file)))
      {
        Assert.That(AudioTagFanArtHandler.ReadTagPicture(fsra), Is.Null,
            "a file without a picture is a file without a picture, not an error");
      }

      Assert.That(AudioTagFanArtHandler.ReadTagPicture(null), Is.Null);
    }

    #endregion

    #region Stand-ins

    /// <summary>
    /// A fanart cache that keeps its pictures in a folder of this test, in the layout the real cache uses. It
    /// counts what it was asked to write, which is how the tests above notice work done twice.
    /// </summary>
    /// <remarks>
    /// The real <see cref="FanArtCache"/> additionally caps how many pictures of a kind an item keeps and writes a
    /// title file beside them; that is its own business and its own tests, and a cap here would only hide a handler
    /// that stored a picture more often than it should.
    /// </remarks>
    private sealed class FanArtCacheOnDisk : IFanArtCache
    {
      private readonly string _root;

      /// <summary>
      /// One entry per picture written, as "<media item id>/<fanart type>/<file name>".
      /// </summary>
      internal readonly List<string> Writes = new List<string>();

      internal FanArtCacheOnDisk(string root)
      {
        _root = root;
      }

      private string FolderFor(Guid mediaItemId, string fanArtType)
      {
        return Path.Combine(_root, IdOf(mediaItemId), fanArtType);
      }

      /// <summary>
      /// The folder name the real cache gives an id, dashes and capitals included, so that a folder written here is
      /// laid out the way a server's data folder looks.
      /// </summary>
      internal static string IdOf(Guid mediaItemId)
      {
        return mediaItemId.ToString().ToUpperInvariant();
      }

      public async Task<bool> TrySaveFanArt(Guid mediaItemId, string title, string fanArtType,
          TrySaveFanArtAsyncDelegate saveDlgt)
      {
        string folder = FolderFor(mediaItemId, fanArtType);
        Directory.CreateDirectory(folder);
        if (!await saveDlgt(folder).ConfigureAwait(false))
          return false;
        Record(mediaItemId, fanArtType, folder);
        return true;
      }

      public async Task<int> TrySaveFanArt<T>(Guid mediaItemId, string title, string fanArtType,
          ICollection<T> files, TrySaveMultipleFanArtAsyncDelegate<T> saveDlgt)
      {
        string folder = FolderFor(mediaItemId, fanArtType);
        Directory.CreateDirectory(folder);
        int saved = 0;
        foreach (T file in files)
        {
          if (await saveDlgt(folder, file).ConfigureAwait(false))
            saved++;
        }
        if (saved > 0)
          Record(mediaItemId, fanArtType, folder);
        return saved;
      }

      private void Record(Guid mediaItemId, string fanArtType, string folder)
      {
        foreach (string file in Directory.GetFiles(folder))
          Writes.Add(Path.Combine(IdOf(mediaItemId), fanArtType, Path.GetFileName(file)));
      }

      public IList<string> GetFanArtFiles(Guid mediaItemId, string fanArtType)
      {
        // The real cache looks for one extension after the other and stops at the first kind it finds; a handler
        // writing a .png into a cache that only lists .jpg would never be seen again.
        string folder = FolderFor(mediaItemId, fanArtType);
        if (!Directory.Exists(folder))
          return new List<string>();
        foreach (string extension in new[] { "*.jpg", "*.png", "*.tbn" })
        {
          List<string> found = Directory.GetFiles(folder, extension).ToList();
          if (found.Count > 0)
            return found;
        }
        return new List<string>();
      }

      public ICollection<Guid> GetAllFanArtIds()
      {
        return Directory.GetDirectories(_root)
            .Select(Path.GetFileName)
            .Where(name => Guid.TryParse(name, out Guid id))
            .Select(name => Guid.Parse(name))
            .ToList();
      }

      public void DeleteFanArtFiles(Guid mediaItemId)
      {
        string folder = Path.Combine(_root, IdOf(mediaItemId));
        if (Directory.Exists(folder))
          Directory.Delete(folder, true);
      }
    }

    #endregion
  }
}
