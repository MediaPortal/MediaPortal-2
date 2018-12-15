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
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs;
using NUnit.Framework;

namespace Tests.Server.NfoMetadataExtractor
{
  [TestFixture]
  public class TestNfoArtistReader
  {
    [OneTimeSetUp]
    public void Init()
    {
      ServiceRegistration.Set<ILocalization>(new NoLocalization());
    }

    [Test]
    public void TestNfoArtistReaderWriteMetadata()
    {
      ArtistStub artistStub = CreateTestArtistStub();

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();

      NfoArtistReader reader = new NfoArtistReader(new ConsoleLogger(LogLevel.All, true), 1, false, null, new NfoAudioMetadataExtractorSettings());
      reader.GetArtistStubs().Add(artistStub);
      reader.TryWriteMetadata(aspects);

      MediaItemAspect mediaAspect = MediaItemAspect.GetAspect(aspects, MediaAspect.Metadata);
      Assert.NotNull(mediaAspect);
      Assert.AreEqual(artistStub.Name, mediaAspect.GetAttributeValue<string>(MediaAspect.ATTR_TITLE));

      MediaItemAspect personAspect = MediaItemAspect.GetAspect(aspects, PersonAspect.Metadata);
      Assert.NotNull(personAspect);
      Assert.AreEqual(artistStub.Name, personAspect.GetAttributeValue<string>(PersonAspect.ATTR_PERSON_NAME));
      Assert.AreEqual(PersonAspect.OCCUPATION_ARTIST, personAspect.GetAttributeValue<string>(PersonAspect.ATTR_OCCUPATION));
      Assert.AreEqual(artistStub.Biography, personAspect.GetAttributeValue<string>(PersonAspect.ATTR_BIOGRAPHY));
      Assert.AreEqual(artistStub.Birthdate, personAspect.GetAttributeValue<DateTime?>(PersonAspect.ATTR_DATEOFBIRTH));
      Assert.AreEqual(artistStub.Deathdate, personAspect.GetAttributeValue<DateTime?>(PersonAspect.ATTR_DATEOFDEATH));
      Assert.IsFalse(personAspect.GetAttributeValue<bool>(PersonAspect.ATTR_GROUP));

      IList<MediaItemAspect> externalIdentifiers;
      Assert.IsTrue(aspects.TryGetValue(ExternalIdentifierAspect.ASPECT_ID, out externalIdentifiers));
      Assert.IsTrue(TestUtils.HasExternalId(externalIdentifiers, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_PERSON, artistStub.AudioDbId.ToString()));
      Assert.IsTrue(TestUtils.HasExternalId(externalIdentifiers, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_PERSON, artistStub.MusicBrainzArtistId));

      MediaItemAspect thumbnailAspect = MediaItemAspect.GetAspect(aspects, ThumbnailLargeAspect.Metadata);
      Assert.NotNull(thumbnailAspect);
      CollectionAssert.AreEqual(artistStub.Thumb, thumbnailAspect.GetAttributeValue<byte[]>(ThumbnailLargeAspect.ATTR_THUMBNAIL));

      //Test Group
      aspects.Clear();
      artistStub.Birthdate = null;
      artistStub.Deathdate = null;

      reader = new NfoArtistReader(new ConsoleLogger(LogLevel.All, true), 1, false, null, new NfoAudioMetadataExtractorSettings());
      reader.GetArtistStubs().Add(artistStub);
      reader.TryWriteMetadata(aspects);

      personAspect = MediaItemAspect.GetAspect(aspects, PersonAspect.Metadata);
      Assert.NotNull(personAspect);
      Assert.AreEqual(artistStub.Formeddate, personAspect.GetAttributeValue<DateTime?>(PersonAspect.ATTR_DATEOFBIRTH));
      Assert.AreEqual(artistStub.Disbandeddate, personAspect.GetAttributeValue<DateTime?>(PersonAspect.ATTR_DATEOFDEATH));
      Assert.IsTrue(personAspect.GetAttributeValue<bool>(PersonAspect.ATTR_GROUP));
    }

    protected ArtistStub CreateTestArtistStub()
    {
      return new ArtistStub
      {
        Name = "Test Artist",
        Biography = "Test biography",
        Birthdate = new DateTime(2000,1,1),
        Deathdate = new DateTime(2000,1,31),
        Formeddate = new DateTime(2000, 2, 1),
        Disbandeddate = new DateTime(2000, 2, 28),
        AudioDbId = 1,
        MusicBrainzArtistId = "mbid1",
        Thumb = new byte[] { 0x01, 0x02, 0x03 }
      };
    }
  }
}
