#region Copyright (C) 2007-2025 Team MediaPortal

/*
    Copyright (C) 2007-2025 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using Moq;
using NUnit.Framework;
using TagExtractor = global::MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor.AudioTagMetadataExtractor;
using AudioTagRelationshipExtractor = global::MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor.AudioTagRelationshipExtractor;
using TrackAlbumRelationshipExtractor = global::MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor.TrackAlbumRelationshipExtractor;
using TrackArtistRelationshipExtractor = global::MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor.TrackArtistRelationshipExtractor;
using TrackAlbumArtistRelationshipExtractor = global::MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor.TrackAlbumArtistRelationshipExtractor;
using AlbumArtistRelationshipExtractor = global::MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor.AlbumArtistRelationshipExtractor;

namespace Tests.Server.AudioTagMetadataExtractor
{
  /// <summary>
  /// Tests for the audio tag extractor and the albums and people that come out of the tags it reads.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The extractor is the only thing in a portable server layout that assigns the
  /// <see cref="AudioAspect"/>, and the importer drops every resource no extractor has claimed. So the
  /// first question these tests ask is the one the bug report asked: does a music file end up as a
  /// track at all? Everything after that is about the two ways this can go quietly wrong - by claiming
  /// things the tag does not say, or by saying nothing about a file that does have something to say.
  /// </para>
  /// <para>
  /// The two mp3 files the tests read are three seconds of silence written by ffmpeg, once with a
  /// complete ID3v2.3 tag and once with none. They are real files opened through the real local file
  /// system provider: a tag reader is precisely the code a hand-written byte array fails to exercise,
  /// and the accessor has to be able to <c>OpenRead()</c> for the extractor to work at all. The tests
  /// ask only that some length is reported, because a reader that estimates it from the first frame's
  /// bitrate does not report three seconds.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestAudioTagMetadataExtractor
  {
    private string _folder;

    [SetUp]
    public void SetUp()
    {
      _folder = Directory.CreateDirectory(
          Path.Combine(Path.GetTempPath(), "mp2-audio-tags-" + Guid.NewGuid().ToString("N"))).FullName;
      if (!ServiceRegistration.IsRegistered<ILogger>())
        ServiceRegistration.Set<ILogger>(new Mock<ILogger>().Object);
      if (!ServiceRegistration.IsRegistered<IGenreConverter>())
        ServiceRegistration.Set<IGenreConverter>(new Mock<IGenreConverter>().Object);
    }

    [TearDown]
    public void TearDown()
    {
      try { Directory.Delete(_folder, true); }
      catch (IOException) { }
      if (ServiceRegistration.IsRegistered<IGenreConverter>())
        ServiceRegistration.Remove<IGenreConverter>();
      if (ServiceRegistration.IsRegistered<ILogger>())
        ServiceRegistration.Remove<ILogger>();
    }

    #region Fixtures

    /// <summary>
    /// Writes one of the embedded mp3 files into the test folder under the given name and hands back an
    /// accessor for it.
    /// </summary>
    private ILocalFsResourceAccessor GivenAudioFile(string embeddedName, string fileName)
    {
      string path = Path.Combine(_folder, fileName);
      File.WriteAllBytes(path, ReadEmbeddedResource(embeddedName));
      return AccessorFor(path);
    }

    private static byte[] ReadEmbeddedResource(string name)
    {
      string resourceName = typeof(TestAudioTagMetadataExtractor).Assembly.GetManifestResourceNames()
          .Single(n => n.EndsWith("." + name, StringComparison.Ordinal));
      using (Stream stream = typeof(TestAudioTagMetadataExtractor).Assembly.GetManifestResourceStream(resourceName))
      using (MemoryStream memory = new MemoryStream())
      {
        stream.CopyTo(memory);
        return memory.ToArray();
      }
    }

    private static ILocalFsResourceAccessor AccessorFor(string path)
    {
      return new LocalFsResourceAccessor(new LocalFsResourceProvider(),
          LocalFsResourceProviderBase.ToProviderPath(path));
    }

    private static IDictionary<Guid, IList<MediaItemAspect>> Extract(IResourceAccessor accessor, out bool claimed)
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      claimed = new TagExtractor().TryExtractMetadataAsync(accessor, aspects, false).Result;
      return aspects;
    }

    /// <summary>
    /// Lets the extractor read the file and insists that it claimed the resource - a track no extractor
    /// claimed is dropped by the importer without a word, which is how this bug reached a user.
    /// </summary>
    private IDictionary<Guid, IList<MediaItemAspect>> Extract(IResourceAccessor accessor)
    {
      bool claimed;
      IDictionary<Guid, IList<MediaItemAspect>> aspects = Extract(accessor, out claimed);
      Assert.IsTrue(claimed, "the extractor did not claim the resource, so the importer would have dropped it");
      CollectionAssert.Contains(aspects.Keys, AudioAspect.ASPECT_ID);
      return aspects;
    }

    private static IList<IDictionary<Guid, IList<MediaItemAspect>>> BuildAlbum(IDictionary<Guid, IList<MediaItemAspect>> trackAspects)
    {
      IList<IDictionary<Guid, IList<MediaItemAspect>>> linked = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      Assert.IsTrue(new TrackAlbumRelationshipExtractor()
          .TryExtractRelationshipsAsync(null, trackAspects, linked).Result);
      return linked;
    }

    #endregion

    #region What the extractor promises

    [Test]
    public void RunsInTheRoundThatDecidesWhatAResourceIs()
    {
      MetadataExtractorMetadata metadata = new TagExtractor().Metadata;

      Assert.AreEqual(TagExtractor.METADATAEXTRACTOR_ID, metadata.MetadataExtractorId);
      // "Core" is the round that decides what a resource is; the NFO audio reader runs at "Extended" and
      // refuses a resource no earlier extractor has called audio.
      Assert.AreEqual(MetadataExtractorPriority.Core, metadata.Priority);
      // Only files carry tags, and claiming a folder would hide the tracks inside it from the importer.
      Assert.IsFalse(metadata.ProcessesNonFiles);
      CollectionAssert.Contains(metadata.MediaCategories, DefaultMediaCategories.Audio);
      CollectionAssert.Contains(metadata.ExtractedAspectTypes.Keys, AudioAspect.ASPECT_ID);
      CollectionAssert.Contains(metadata.ExtractedAspectTypes.Keys, ProviderResourceAspect.ASPECT_ID);
    }

    #endregion

    #region A file with tags

    [Test]
    public void ATaggedFileBecomesATrack()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = Extract(GivenAudioFile("tagged.mp3", "Test Track.mp3"));

      Assert.AreEqual("Test Track", Single<string>(aspects, AudioAspect.ATTR_TRACKNAME));
      Assert.AreEqual("Fixture Album", Single<string>(aspects, AudioAspect.ATTR_ALBUM));
      Assert.AreEqual(3, Single<int>(aspects, AudioAspect.ATTR_TRACK));
      CollectionAssert.Contains(Collection(aspects, AudioAspect.ATTR_ARTISTS), "Fixture Artist");
      CollectionAssert.Contains(Collection(aspects, AudioAspect.ATTR_ALBUMARTISTS), "Fixture Artist");
      // The title a track list shows is the album with the number in front of it.
      Assert.AreEqual("Fixture Album: 3 - Test Track", Single<string>(aspects, MediaAspect.ATTR_TITLE));
      Assert.AreEqual(false, Single<bool>(aspects, MediaAspect.ATTR_ISVIRTUAL));
    }

    [Test]
    public void TheShapeOfTheAudioComesFromTheFile()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = Extract(GivenAudioFile("tagged.mp3", "Test Track.mp3"));

      // Without these the library shows a track of no length, which is the first thing a listener
      // notices. They come from the container header, so no native stream analyser is needed for them.
      Assert.Greater(Single<long>(aspects, AudioAspect.ATTR_DURATION), 0);
      Assert.Greater(Single<int>(aspects, AudioAspect.ATTR_BITRATE), 0);
      Assert.AreEqual(44100, Single<long>(aspects, AudioAspect.ATTR_SAMPLERATE));
      Assert.AreEqual(1, Single<int>(aspects, AudioAspect.ATTR_CHANNELS));
      Assert.IsNotEmpty(Single<string>(aspects, AudioAspect.ATTR_ENCODING));
    }

    [Test]
    public void TheTrackPointsAtTheFileItCameFrom()
    {
      ILocalFsResourceAccessor accessor = GivenAudioFile("tagged.mp3", "Test Track.mp3");
      IDictionary<Guid, IList<MediaItemAspect>> aspects = Extract(accessor);

      Assert.IsTrue(aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID),
          "a track without a resource cannot be played");
      MediaItemAspect resource = aspects[ProviderResourceAspect.ASPECT_ID].First();
      Assert.AreEqual(ProviderResourceAspect.TYPE_PRIMARY,
          resource.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE));
      Assert.AreEqual(accessor.CanonicalLocalResourcePath.Serialize(),
          resource.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH));
      Assert.AreEqual(accessor.Size, resource.GetAttributeValue<long>(ProviderResourceAspect.ATTR_SIZE));
      StringAssert.StartsWith("audio/", resource.GetAttributeValue<string>(ProviderResourceAspect.ATTR_MIME_TYPE));
    }

    [Test]
    public void TheYearInTheTagBecomesTheReleaseDate()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = Extract(GivenAudioFile("tagged.mp3", "Test Track.mp3"));

      Assert.AreEqual(2008, Single<DateTime>(aspects, MediaAspect.ATTR_RECORDINGTIME).Year);
    }

    [Test]
    public void AGenreIsKeptAndGivenAnId()
    {
      int genreId = 17;
      Mock<IGenreConverter> converter = new Mock<IGenreConverter>();
      converter.Setup(c => c.GetGenreId(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), out genreId)).Returns(true);
      ServiceRegistration.Set<IGenreConverter>(converter.Object);

      IDictionary<Guid, IList<MediaItemAspect>> aspects = Extract(GivenAudioFile("tagged.mp3", "Test Track.mp3"));

      IList<MultipleMediaItemAspect> genres;
      Assert.IsTrue(MediaItemAspect.TryGetAspects(aspects, GenreAspect.Metadata, out genres),
          "the tag names a genre, so the track has to carry one");
      Assert.AreEqual("Rock", genres[0].GetAttributeValue<string>(GenreAspect.ATTR_GENRE));
      // The id is what lets the UI put "Rock" and "rock" into one genre.
      Assert.AreEqual(genreId, genres[0].GetAttributeValue<int?>(GenreAspect.ATTR_ID));
    }

    [Test]
    public void ReadingTagsLeavesTheUsersFileAlone()
    {
      byte[] before = ReadEmbeddedResource("tagged.mp3");
      string path = Path.Combine(_folder, "Test Track.mp3");
      File.WriteAllBytes(path, before);

      Extract(AccessorFor(path));

      CollectionAssert.AreEqual(before, File.ReadAllBytes(path),
          "an import must never rewrite a user's audio file");
    }

    #endregion

    #region A file without tags

    [Test]
    public void AFileWithoutTagsIsStillATrackAndIsNamedAfterItsFileName()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects =
          Extract(GivenAudioFile("untagged.mp3", "05 - Anonymous Track.mp3"));

      Assert.AreEqual("Anonymous Track", Single<string>(aspects, AudioAspect.ATTR_TRACKNAME));
      Assert.AreEqual(5, Single<int>(aspects, AudioAspect.ATTR_TRACK));
    }

    [Test]
    public void NothingIsInventedThatTheTagDidNotSay()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects =
          Extract(GivenAudioFile("untagged.mp3", "05 - Anonymous Track.mp3"));

      // A file named "05 - Anonymous Track.mp3" says nothing about its album or its genre, and making
      // them up would put the track on a shelf it does not belong to.
      Assert.IsFalse(HasSingle(aspects, AudioAspect.ATTR_ALBUM));
      Assert.IsFalse(HasCollection(aspects, AudioAspect.ATTR_ARTISTS));
      Assert.IsFalse(HasSingle(aspects, AudioAspect.ATTR_NUMTRACKS));
      Assert.IsFalse(aspects.ContainsKey(GenreAspect.ASPECT_ID));
      // The length however is in the file, so it is reported.
      Assert.Greater(Single<long>(aspects, AudioAspect.ATTR_DURATION), 0);
    }

    [Test]
    public void AnArtistIsGuessedFromTheFileNameOnlyWhenTheFileNameNamesOne()
    {
      IDictionary<Guid, IList<MediaItemAspect>> withArtist =
          Extract(GivenAudioFile("untagged.mp3", "02 - Fixture Artist - Second Track.mp3"));
      Assert.AreEqual("Second Track", Single<string>(withArtist, AudioAspect.ATTR_TRACKNAME));
      CollectionAssert.Contains(Collection(withArtist, AudioAspect.ATTR_ARTISTS), "Fixture Artist");

      IDictionary<Guid, IList<MediaItemAspect>> withoutArtist =
          Extract(GivenAudioFile("untagged.mp3", "03 - Third Track.mp3"));
      Assert.AreEqual("Third Track", Single<string>(withoutArtist, AudioAspect.ATTR_TRACKNAME));
      Assert.IsFalse(HasCollection(withoutArtist, AudioAspect.ATTR_ARTISTS));
    }

    #endregion

    #region Resources that are not tracks

    [Test]
    public void AFileThatIsNotAudioIsLeftToTheOtherExtractors()
    {
      foreach (string name in new[] { "album.nfo", "cover.jpg", "notes.txt" })
      {
        string path = Path.Combine(_folder, name);
        File.WriteAllText(path, "not audio");

        bool claimed;
        IDictionary<Guid, IList<MediaItemAspect>> aspects = Extract(AccessorFor(path), out claimed);

        Assert.IsFalse(claimed, $"'{name}' is not a track and must not be claimed as one");
        Assert.AreEqual(0, aspects.Count, $"a resource that was not claimed must not leave aspects behind");
      }
    }

    [Test]
    public void AFolderIsNotATrack()
    {
      string folder = Directory.CreateDirectory(Path.Combine(_folder, "Fixture Album (2008)")).FullName;

      bool claimed;
      Extract(AccessorFor(folder), out claimed);

      // The folder around an album is listed by the importer; if it were claimed as an item, the tracks
      // inside it would never be looked at.
      Assert.IsFalse(claimed);
    }

    #endregion

    #region Albums and people out of the tags

    [Test]
    public void TheAlbumComesOutOfTheTracksTag()
    {
      IDictionary<Guid, IList<MediaItemAspect>> trackAspects = Extract(GivenAudioFile("tagged.mp3", "Test Track.mp3"));

      IList<IDictionary<Guid, IList<MediaItemAspect>>> linked = BuildAlbum(trackAspects);
      Assert.AreEqual(1, linked.Count);

      Assert.AreEqual("Fixture Album", Single<string>(linked[0], AudioAlbumAspect.ATTR_ALBUM));
      Assert.AreEqual(2008, Single<DateTime>(linked[0], MediaAspect.ATTR_RECORDINGTIME).Year);
      // Without an id the library could not recognise the album again from the next track on it.
      CollectionAssert.Contains(linked[0].Keys, ExternalIdentifierAspect.ASPECT_ID);
    }

    [Test]
    public void TwoTracksDescribeTheSameAlbumOnlyWhenTheirTagsDo()
    {
      TrackAlbumRelationshipExtractor extractor = new TrackAlbumRelationshipExtractor();
      IList<IDictionary<Guid, IList<MediaItemAspect>>> first =
          BuildAlbum(Extract(GivenAudioFile("tagged.mp3", "Track One.mp3")));
      IList<IDictionary<Guid, IList<MediaItemAspect>>> second =
          BuildAlbum(Extract(GivenAudioFile("tagged.mp3", "Track Two.mp3")));

      // Two files whose tags name the same album are one album in the library.
      Assert.IsTrue(extractor.TryMatch(first[0], second[0]));

      MediaItemAspect.SetAttribute(second[0], AudioAlbumAspect.ATTR_ALBUM, "A Different Album");
      Assert.IsFalse(extractor.TryMatch(first[0], second[0]));
    }

    [Test]
    public void TracksOnAnAlbumAreOrderedByDiscThenTrackNumber()
    {
      IDictionary<Guid, IList<MediaItemAspect>> trackAspects = Extract(GivenAudioFile("tagged.mp3", "Test Track.mp3"));

      int index;
      Assert.IsTrue(new TrackAlbumRelationshipExtractor().TryGetRelationshipIndex(trackAspects, null, out index));
      // The fixture carries no disc frame, so it is disc 1; its tag says track 3.
      Assert.AreEqual(1003, index);
    }

    [Test]
    public void TheArtistOfTheTrackBecomesALinkedPerson()
    {
      IDictionary<Guid, IList<MediaItemAspect>> trackAspects = Extract(GivenAudioFile("tagged.mp3", "Test Track.mp3"));

      IList<IDictionary<Guid, IList<MediaItemAspect>>> linked = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      Assert.IsTrue(new TrackArtistRelationshipExtractor()
          .TryExtractRelationshipsAsync(null, trackAspects, linked).Result);

      Assert.AreEqual("Fixture Artist", Single<string>(linked[0], PersonAspect.ATTR_PERSON_NAME));
      Assert.AreEqual(PersonAspect.OCCUPATION_ARTIST, Single<string>(linked[0], PersonAspect.ATTR_OCCUPATION));
      Assert.AreEqual(PersonAspect.ROLE_ARTIST, new TrackArtistRelationshipExtractor().LinkedRole);
    }

    [Test]
    public void TheAlbumArtistOfTheTrackBecomesALinkedPerson()
    {
      IDictionary<Guid, IList<MediaItemAspect>> trackAspects = Extract(GivenAudioFile("tagged.mp3", "Test Track.mp3"));

      IList<IDictionary<Guid, IList<MediaItemAspect>>> linked = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      Assert.IsTrue(new TrackAlbumArtistRelationshipExtractor()
          .TryExtractRelationshipsAsync(null, trackAspects, linked).Result);

      Assert.AreEqual("Fixture Artist", Single<string>(linked[0], PersonAspect.ATTR_PERSON_NAME));
      Assert.AreEqual(PersonAspect.ROLE_ALBUMARTIST, new TrackAlbumArtistRelationshipExtractor().LinkedRole);
    }

    [Test]
    public void TheAlbumLinksItsArtists()
    {
      IDictionary<Guid, IList<MediaItemAspect>> trackAspects = Extract(GivenAudioFile("tagged.mp3", "Test Track.mp3"));
      IDictionary<Guid, IList<MediaItemAspect>> album = BuildAlbum(trackAspects)[0];

      IList<IDictionary<Guid, IList<MediaItemAspect>>> linked = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      Assert.IsTrue(new AlbumArtistRelationshipExtractor()
          .TryExtractRelationshipsAsync(null, album, linked).Result);
      Assert.AreEqual("Fixture Artist", Single<string>(linked[0], PersonAspect.ATTR_PERSON_NAME));
    }

    [Test]
    public void AVirtualTrackLinksNoPeople()
    {
      IDictionary<Guid, IList<MediaItemAspect>> trackAspects = Extract(GivenAudioFile("tagged.mp3", "Test Track.mp3"));
      // A virtual resource (a radio stream, a recording placeholder) stands for something that is not on
      // disk, so it is not proof that the people its names suggest exist.
      IList<MultipleMediaItemAspect> resources;
      Assert.IsTrue(MediaItemAspect.TryGetAspects(trackAspects, ProviderResourceAspect.Metadata, out resources));
      foreach (MultipleMediaItemAspect resource in resources)
        resource.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_VIRTUAL);

      IList<IDictionary<Guid, IList<MediaItemAspect>>> linked = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      Assert.IsFalse(new TrackArtistRelationshipExtractor()
          .TryExtractRelationshipsAsync(null, trackAspects, linked).Result);
      Assert.AreEqual(0, linked.Count);
    }

    [Test]
    public void ATrackNamingNoArtistLinksNoPeople()
    {
      IDictionary<Guid, IList<MediaItemAspect>> trackAspects =
          Extract(GivenAudioFile("untagged.mp3", "08 - Anonymous Track.mp3"));

      // The link extractors read the names the tag extractor left behind and do not open the file again,
      // so a track that names nobody has nobody to link.
      Assert.IsFalse(HasCollection(trackAspects, AudioAspect.ATTR_ARTISTS));
      IList<IDictionary<Guid, IList<MediaItemAspect>>> linked = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      Assert.IsFalse(new TrackArtistRelationshipExtractor()
          .TryExtractRelationshipsAsync(null, trackAspects, linked).Result);
      Assert.AreEqual(0, linked.Count);
    }

    [Test]
    public void TheRelationshipTypesAreRegisteredInAnOrderTheLibraryCanTraverse()
    {
      List<RelationshipType> registered = new List<RelationshipType>();
      Mock<IRelationshipTypeRegistration> registration = new Mock<IRelationshipTypeRegistration>();
      registration.Setup(r => r.RegisterLocallyKnownRelationshipType(It.IsAny<RelationshipType>(), It.IsAny<bool>()))
          .Callback((RelationshipType type, bool isChildPrimaryResource) => registered.Add(type));
      ServiceRegistration.Set<IRelationshipTypeRegistration>(registration.Object);
      try
      {
        AudioTagRelationshipExtractor extractor = new AudioTagRelationshipExtractor();

        // Every relationship that does not start at a primary resource may only start from a role an
        // earlier relationship has already reached, so "Album->Artist" has to come after "Track->Album";
        // if it does not, the server refuses it and the library has no album queries at all.
        int trackToAlbum = registered.FindIndex(r => r.ChildRole == AudioAspect.ROLE_TRACK && r.ParentRole == AudioAlbumAspect.ROLE_ALBUM);
        int albumToArtist = registered.FindIndex(r => r.ChildRole == AudioAlbumAspect.ROLE_ALBUM && r.ParentRole == PersonAspect.ROLE_ALBUMARTIST);
        Assert.GreaterOrEqual(trackToAlbum, 0, "tracks are not linked to albums at all");
        Assert.Greater(albumToArtist, trackToAlbum, "\"Album->Artist\" was registered before albums could be reached");

        foreach (IRelationshipRoleExtractor roleExtractor in extractor.RoleExtractors)
        {
          Assert.IsNotNull(FindRelationship(registered, roleExtractor.Role, roleExtractor.LinkedRole),
              "a role extractor has no registered relationship behind it");
        }
      }
      finally
      {
        ServiceRegistration.Remove<IRelationshipTypeRegistration>();
      }
    }

    private static RelationshipType FindRelationship(List<RelationshipType> types, Guid childRole, Guid parentRole)
    {
      return types.FirstOrDefault(t => t.ChildRole == childRole && t.ParentRole == parentRole);
    }

    #endregion

    #region Reading attribute values

    private static T Single<T>(IDictionary<Guid, IList<MediaItemAspect>> aspects,
        MediaItemAspectMetadata.SingleAttributeSpecification spec)
    {
      T value;
      Assert.IsTrue(MediaItemAspect.TryGetAttribute(aspects, spec, out value),
          $"the aspect data carries no '{spec.ParentMIAM.Name}.{spec.AttributeName}'");
      return value;
    }

    private static bool HasSingle(IDictionary<Guid, IList<MediaItemAspect>> aspects,
        MediaItemAspectMetadata.SingleAttributeSpecification spec)
    {
      object value;
      return MediaItemAspect.TryGetAttribute(aspects, spec, out value) && value != null;
    }

    private static List<string> Collection(IDictionary<Guid, IList<MediaItemAspect>> aspects,
        MediaItemAspectMetadata.SingleAttributeSpecification spec)
    {
      Assert.IsTrue(MediaItemAspect.TryGetAspect(aspects, spec.ParentMIAM, out SingleMediaItemAspect aspect),
          $"the aspect data carries no '{spec.ParentMIAM.Name}'");
      return new List<string>(aspect.GetCollectionAttribute<string>(spec) ?? Enumerable.Empty<string>());
    }

    private static bool HasCollection(IDictionary<Guid, IList<MediaItemAspect>> aspects,
        MediaItemAspectMetadata.SingleAttributeSpecification spec)
    {
      return Collection(aspects, spec).Count > 0;
    }

    #endregion
  }
}
