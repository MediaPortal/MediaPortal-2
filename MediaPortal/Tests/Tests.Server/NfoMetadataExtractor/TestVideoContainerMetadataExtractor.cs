#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors;
using Moq;
using NUnit.Framework;

namespace Tests.Server.NfoMetadataExtractor
{
  /// <summary>
  /// Tests for the <see cref="VideoContainerMetadataExtractor"/>, the extractor that tells the importer
  /// that a resource is a video.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The extractor looks small - it writes one aspect and no values - but what hangs on that aspect is
  /// the whole import on the platforms without MediaInfo: the NFO extractors refuse every resource that
  /// no earlier extractor called a video, so if this one does not recognise a film folder, the folder
  /// is walked, nothing is saved, and the library stays empty without a single error message.
  /// </para>
  /// <para>
  /// The second group of tests is about restraint: the extractor must stay silent for anything it
  /// cannot tell, and must not overwrite what a real stream analyser next to it wrote. A wrong "this is
  /// a video" is worse than no answer, because it puts the file on a shelf it does not belong to.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestVideoContainerMetadataExtractor
  {
    private string _folder;

    [SetUp]
    public void SetUp()
    {
      _folder = Directory.CreateDirectory(
          Path.Combine(Path.GetTempPath(), "mp2-video-container-" + Guid.NewGuid().ToString("N"))).FullName;
      if (!ServiceRegistration.IsRegistered<ILogger>())
        ServiceRegistration.Set<ILogger>(new Mock<ILogger>().Object);
    }

    [TearDown]
    public void TearDown()
    {
      try { Directory.Delete(_folder, true); }
      catch (IOException) { }
      if (ServiceRegistration.IsRegistered<ILogger>())
        ServiceRegistration.Remove<ILogger>();
    }

    #region Fakes

    /// <summary>
    /// A file that the server can see on the local filesystem. Only its name decides whether it is a
    /// video, so no content is needed - but the name is a real path in a real temporary folder, because
    /// a made-up path would let the test pass on extension parsing mistakes that a real one catches
    /// (a trailing space, a directory that is itself named like a file).
    /// </summary>
    private Mock<ILocalFsResourceAccessor> GivenFile(string name)
    {
      string path = Path.Combine(_folder, name);
      File.WriteAllBytes(path, new byte[] { 0, 1, 2, 3 });
      Mock<ILocalFsResourceAccessor> accessor = new Mock<ILocalFsResourceAccessor>();
      accessor.SetupGet(a => a.Exists).Returns(true);
      accessor.SetupGet(a => a.IsFile).Returns(true);
      accessor.SetupGet(a => a.Size).Returns(4);
      accessor.SetupGet(a => a.LocalFileSystemPath).Returns(path);
      accessor.SetupGet(a => a.CanonicalLocalResourcePath)
          .Returns(ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, path));
      return accessor;
    }

    /// <summary>
    /// A directory whose children are the given accessors, as <see cref="IFileSystemResourceAccessor.GetFiles"/>
    /// hands them out: newly created accessors that the caller has to dispose.
    /// </summary>
    private static Mock<IFileSystemResourceAccessor> GivenDirectory(params IFileSystemResourceAccessor[] children)
    {
      Mock<IFileSystemResourceAccessor> accessor = new Mock<IFileSystemResourceAccessor>();
      accessor.SetupGet(a => a.Exists).Returns(true);
      accessor.SetupGet(a => a.IsFile).Returns(false);
      accessor.Setup(a => a.GetFiles()).Returns(new List<IFileSystemResourceAccessor>(children));
      return accessor;
    }

    private static IDictionary<Guid, IList<MediaItemAspect>> AskExtractor(IResourceAccessor accessor)
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      Assert.AreEqual(true, new VideoContainerMetadataExtractor()
          .TryExtractMetadataAsync(accessor, aspects, false).Result);
      return aspects;
    }

    private static bool WasClaimedAsVideo(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      return aspects.ContainsKey(VideoAspect.ASPECT_ID);
    }

    #endregion

    #region What the extractor promises

    [Test]
    public void RunsBeforeTheExtractorsThatNeedItsAnswer()
    {
      MetadataExtractorMetadata metadata = new VideoContainerMetadataExtractor().Metadata;

      Assert.AreEqual(VideoContainerMetadataExtractor.METADATAEXTRACTOR_ID, metadata.MetadataExtractorId);
      // "Core" is what puts this extractor in the round that decides what a resource is. The nfo
      // extractors run at "Extended" and would otherwise never see a video.
      Assert.AreEqual(MetadataExtractorPriority.Core, metadata.Priority);
      // The folder around a film has to be recognisable as a video by itself, so non-files are processed.
      Assert.IsTrue(metadata.ProcessesNonFiles);
      CollectionAssert.Contains(metadata.MediaCategories, DefaultMediaCategories.Video);
      CollectionAssert.Contains(metadata.ExtractedAspectTypes.Keys, VideoAspect.ASPECT_ID);
    }

    #endregion

    #region Resources that are videos

    [Test]
    public void AVideoFileIsMarkedAsVideo()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = AskExtractor(GivenFile("film.mkv").Object);

      Assert.IsTrue(WasClaimedAsVideo(aspects));
      // The extractor does not know anything about the file beyond it being a video, and says so by
      // writing no values. An aspect without values is what the nfo extractors wait for.
      Assert.IsFalse(MediaItemAspect.TryGetAttribute(aspects, VideoAspect.ATTR_ISDVD, out bool isDvd));
    }

    [Test]
    public void ADirectoryHoldingAVideoIsMarkedAsVideo()
    {
      Mock<IFileSystemResourceAccessor> directory =
          GivenDirectory(GivenFile("sample.txt").Object, GivenFile("film.mkv").Object);

      Assert.IsTrue(WasClaimedAsVideo(AskExtractor(directory.Object)));
    }

    [Test]
    public void ADirectoryIsNoLongerOnlyAListingOfOtherItems()
    {
      Mock<IFileSystemResourceAccessor> directory = GivenDirectory(GivenFile("film.mkv").Object);
      IDictionary<Guid, IList<MediaItemAspect>> aspects = AskExtractor(directory.Object);

      // Without this the folder keeps the "virtual" flag it got when the importer unfolded it, and a
      // virtual item is a folder view, not the film itself: it does not show up on the shelf.
      Assert.IsTrue(MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_ISVIRTUAL, out bool isVirtual));
      Assert.IsFalse(isVirtual);
    }

    #region The resource the item points at

    [Test]
    public void AVideoFileBecomesTheItemsPrimaryResource()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = AskExtractor(GivenFile("film.mkv").Object);

      // Whoever asks the library for a title afterwards reads this resource back to find out where the
      // film is. If no extractor writes it, the title is in the database but has no path in the answer,
      // and nothing can be played from it.
      Assert.IsTrue(MediaItemAspect.TryGetAspects(aspects, ProviderResourceAspect.Metadata, out IList<MultipleMediaItemAspect> resources));
      Assert.AreEqual(1, resources.Count);
      Assert.AreEqual(ProviderResourceAspect.TYPE_PRIMARY, resources[0].GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE));
      Assert.AreEqual(0, resources[0].GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX));
      Assert.AreEqual(4L, resources[0].GetAttributeValue<long>(ProviderResourceAspect.ATTR_SIZE));
      StringAssert.StartsWith("video/", resources[0].GetAttributeValue<string>(ProviderResourceAspect.ATTR_MIME_TYPE));
    }

    [Test]
    public void TheResourceIsStoredAsASerializablePath()
    {
      Mock<ILocalFsResourceAccessor> file = GivenFile("film.mkv");
      IDictionary<Guid, IList<MediaItemAspect>> aspects = AskExtractor(file.Object);

      // The path travels to the database and to other machines, so it has to be the serialized canonical
      // path - a plain filesystem string would not survive that.
      MediaItemAspect.TryGetAspects(aspects, ProviderResourceAspect.Metadata, out IList<MultipleMediaItemAspect> resources);
      string serialized = resources[0].GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      ResourcePath path = ResourcePath.Deserialize(serialized);
      Assert.AreEqual(file.Object.CanonicalLocalResourcePath, path);
    }

    [Test]
    public void ADirectoryDoesNotGetAResourceFromThisExtractor()
    {
      Mock<IFileSystemResourceAccessor> directory = GivenDirectory(GivenFile("film.mkv").Object);
      IDictionary<Guid, IList<MediaItemAspect>> aspects = AskExtractor(directory.Object);

      // The importer notes a folder's own resource while it unfolds the share. Writing one here too would
      // give the folder a second resource that points at one file inside it, which the library reads as a
      // second version of the same film.
      Assert.IsTrue(WasClaimedAsVideo(aspects));
      Assert.IsFalse(aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID));
    }

    #endregion

    [TestCase("film.mkv")]
    [TestCase("film.MKV")]
    [TestCase("film.mp4")]
    [TestCase("film.m4v")]
    [TestCase("film.mov")]
    [TestCase("film.avi")]
    [TestCase("film.wmv")]
    [TestCase("film.webm")]
    [TestCase("film.flv")]
    [TestCase("film.vob")]
    [TestCase("film.iso.mkv")]
    public void KnownVideoContainersAreRecognised(string fileName)
    {
      Assert.IsTrue(WasClaimedAsVideo(AskExtractor(GivenFile(fileName).Object)), fileName);
    }

    #endregion

    #region Resources that are not

    [TestCase("notes.txt")]
    [TestCase("cover.jpg")]
    [TestCase("song.mp3")]
    [TestCase("soundtrack.flac")]
    [TestCase("movie.nfo")]
    [TestCase("subtitle.srt")]
    [TestCase("film")]
    [TestCase("film.qqq")]
    public void OtherFilesAreLeftAlone(string fileName)
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects =
          new Dictionary<Guid, IList<MediaItemAspect>>();

      Assert.IsFalse(new VideoContainerMetadataExtractor()
          .TryExtractMetadataAsync(GivenFile(fileName).Object, aspects, false).Result, fileName);
      Assert.IsFalse(WasClaimedAsVideo(aspects), fileName);
    }

    [Test]
    public void ADirectoryWithoutAVideoIsLeftAlone()
    {
      Mock<IFileSystemResourceAccessor> directory =
          GivenDirectory(GivenFile("cover.jpg").Object, GivenFile("movie.nfo").Object);
      IDictionary<Guid, IList<MediaItemAspect>> aspects =
          new Dictionary<Guid, IList<MediaItemAspect>>();

      Assert.IsFalse(new VideoContainerMetadataExtractor()
          .TryExtractMetadataAsync(directory.Object, aspects, false).Result);
      Assert.IsFalse(WasClaimedAsVideo(aspects));
    }

    [Test]
    public void ADirectoryThatCannotBeListedIsLeftAlone()
    {
      // A share that vanished mid-import, a directory we are not allowed to read. The importer asks
      // thousands of resources per run; an exception here would stop the whole job.
      Mock<IFileSystemResourceAccessor> directory = new Mock<IFileSystemResourceAccessor>();
      directory.SetupGet(a => a.Exists).Returns(true);
      directory.SetupGet(a => a.IsFile).Returns(false);
      directory.Setup(a => a.GetFiles()).Throws(new IOException("the share is gone"));
      IDictionary<Guid, IList<MediaItemAspect>> aspects =
          new Dictionary<Guid, IList<MediaItemAspect>>();

      Assert.IsFalse(new VideoContainerMetadataExtractor()
          .TryExtractMetadataAsync(directory.Object, aspects, false).Result);
      Assert.IsFalse(WasClaimedAsVideo(aspects));
    }

    [Test]
    public void AResourceThatIsNotOnAFileSystemIsLeftAlone()
    {
      // A recording of the TV server or a stream from another provider has no file name to look at.
      Mock<IResourceAccessor> resource = new Mock<IResourceAccessor>();
      IDictionary<Guid, IList<MediaItemAspect>> aspects =
          new Dictionary<Guid, IList<MediaItemAspect>>();

      Assert.IsFalse(new VideoContainerMetadataExtractor()
          .TryExtractMetadataAsync(resource.Object, aspects, false).Result);
      Assert.IsFalse(WasClaimedAsVideo(aspects));
    }

    [Test]
    public void AFileWithoutALocalPathIsLeftAlone()
    {
      Mock<IFileSystemResourceAccessor> file = new Mock<IFileSystemResourceAccessor>();
      file.SetupGet(a => a.Exists).Returns(true);
      file.SetupGet(a => a.IsFile).Returns(true);
      IDictionary<Guid, IList<MediaItemAspect>> aspects =
          new Dictionary<Guid, IList<MediaItemAspect>>();

      Assert.IsFalse(new VideoContainerMetadataExtractor()
          .TryExtractMetadataAsync(file.Object, aspects, false).Result);
      Assert.IsFalse(WasClaimedAsVideo(aspects));
    }

    [Test]
    public void AResourceThatDisappearedIsLeftAlone()
    {
      Mock<ILocalFsResourceAccessor> file = GivenFile("film.mkv");
      file.SetupGet(a => a.Exists).Returns(false);
      IDictionary<Guid, IList<MediaItemAspect>> aspects =
          new Dictionary<Guid, IList<MediaItemAspect>>();

      Assert.IsFalse(new VideoContainerMetadataExtractor()
          .TryExtractMetadataAsync(file.Object, aspects, false).Result);
      Assert.IsFalse(WasClaimedAsVideo(aspects));
    }

    #endregion

    #region Staying out of the way

    [Test]
    public void AVideoAspectFromAnotherExtractorIsKept()
    {
      // On Windows the MediaInfo based extractor runs in the same round. If this one arrived second it
      // must leave the values it did not read alone, otherwise it would erase the disc flag that
      // extractor set.
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MediaItemAspect.GetOrCreateAspect(aspects, VideoAspect.Metadata)
          .SetAttribute(VideoAspect.ATTR_ISDVD, true);

      Assert.IsTrue(new VideoContainerMetadataExtractor()
          .TryExtractMetadataAsync(GivenFile("film.mkv").Object, aspects, false).Result);
      Assert.IsTrue(MediaItemAspect.TryGetAttribute(aspects, VideoAspect.ATTR_ISDVD, out bool isDvd));
      Assert.IsTrue(isDvd);
    }

    [Test]
    public void EveryChildAccessorIsDisposed()
    {
      // GetFiles hands out new accessors that belong to the caller. Leaking them on the way to "this is
      // not a video" would leak one handle per file of the library on every import.
      Mock<ILocalFsResourceAccessor> first = GivenFile("cover.jpg");
      Mock<ILocalFsResourceAccessor> second = GivenFile("movie.nfo");
      Mock<ILocalFsResourceAccessor> third = GivenFile("film.mkv");

      AskExtractor(GivenDirectory(first.Object, second.Object, third.Object).Object);

      first.Verify(a => a.Dispose(), Times.Once);
      second.Verify(a => a.Dispose(), Times.Once);
      third.Verify(a => a.Dispose(), Times.Once);
    }

    #endregion
  }
}
