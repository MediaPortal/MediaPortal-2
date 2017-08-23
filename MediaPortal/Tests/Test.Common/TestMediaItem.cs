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
using System.IO;
using System.Xml;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using NUnit.Framework;

namespace Test.Common
{
  [TestFixture]
  public class TestMediaItem
  {
    [TestFixtureSetUp]
    public void SetUp()
    {
      IMediaItemAspectTypeRegistration miatr = new TestMediaItemAspectTypeRegistration();
      ServiceRegistration.Set(miatr);

      miatr.RegisterLocallyKnownMediaItemAspectType(MediaAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(AudioAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(ProviderResourceAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(RelationshipAspect.Metadata);
    }

      [Test]
      public void TestItemIds()
      {
          Guid id1 = new Guid("11111111-aaaa-aaaa-aaaa-111111111111");
          Guid id2 = new Guid("11111111-AAAA-AAAA-AAAA-111111111111");

          Console.WriteLine("{0} -vs- {1}", id1, id2);

          Assert.AreEqual(id1, id2, "GUID");
          Assert.AreEqual(id1.ToString(), id2.ToString(), "GUID string");
          Assert.AreEqual(id1.ToString().ToLower(), id2.ToString().ToLower(), "GUID string");
      }

    [Test]
    public void TestSimpleItem()
    {
      Guid albumId = new Guid("11111111-aaaa-aaaa-aaaa-100000000001");

      IDictionary<Guid, IList<MediaItemAspect>> aspects1 = new Dictionary<Guid, IList<MediaItemAspect>>();

      SingleMediaItemAspect album1MIA1 = new SingleMediaItemAspect(MediaAspect.Metadata);
      album1MIA1.SetAttribute(MediaAspect.ATTR_TITLE, "The Album");
      MediaItemAspect.SetAspect(aspects1, album1MIA1);

      MediaItem album1 = new MediaItem(albumId, aspects1);

      SingleMediaItemAspect mediaAspect1;
      Assert.IsTrue(MediaItemAspect.TryGetAspect(album1.Aspects, MediaAspect.Metadata, out mediaAspect1), "Media aspect");
      Assert.AreEqual(false, mediaAspect1.Deleted, "Album title");
      Assert.AreEqual("The Album", mediaAspect1.GetAttributeValue<string>(MediaAspect.ATTR_TITLE), "Album title");

      TextWriter writer = new StringWriter();
      XmlWriter serialiser = new XmlTextWriter(writer);
      serialiser.WriteStartElement("Test"); // Wrapper around the albums
      // Write the track twice
      album1.Serialize(serialiser);
      album1.Serialize(serialiser);
      serialiser.WriteEndElement();

      Console.WriteLine("XML: {0}", writer.ToString());
      //Assert.AreEqual("<MI Id=\"" + trackId + "\"><Relationship ItemType=\"" + AudioAspect.RELATIONSHIP_TRACK + "\" RelationshipType=\"" + AlbumAspect.RELATIONSHIP_ALBUM + "\" RelationshipId=\"" + albumId + "\" /></MI>", trackText.ToString(), "Track XML");

      XmlReader reader = XmlReader.Create(new StringReader(writer.ToString()));
      reader.Read(); // Test
      //Console.WriteLine("Reader state Test, {0} {1}", reader.NodeType, reader.Name);

      // Read the track once
      reader.Read(); // MI
      //Console.WriteLine("Reader state track2, {0} {1}", reader.NodeType, reader.Name);
      MediaItem track2 = MediaItem.Deserialize(reader);

      SingleMediaItemAspect mediaAspect2;
      Assert.IsTrue(MediaItemAspect.TryGetAspect(track2.Aspects, MediaAspect.Metadata, out mediaAspect2), "Media aspect 2");
      Assert.AreEqual("The Album", mediaAspect2.GetAttributeValue<string>(MediaAspect.ATTR_TITLE), "Album title 2");
      Assert.AreEqual(false, mediaAspect2.Deleted, "Album delete state 2");

      // Read the track again
      //Console.WriteLine("Reader state track3, {0} {1}", reader.NodeType, reader.Name);
      MediaItem track3 = MediaItem.Deserialize(reader);

      SingleMediaItemAspect mediaAspect3;
      Assert.IsTrue(MediaItemAspect.TryGetAspect(track3.Aspects, MediaAspect.Metadata, out mediaAspect3), "Media aspect 3");
      Assert.AreEqual("The Album", mediaAspect3.GetAttributeValue<string>(MediaAspect.ATTR_TITLE), "Album title 3");
      Assert.AreEqual(false, mediaAspect3.Deleted, "Album delete state 3");

      reader.Read(); // Test
    }

    [Test]
    public void TestComplexItem()
    {
      Guid trackId = new Guid("11111111-aaaa-aaaa-aaaa-100000000000");
      Guid albumId = new Guid("11111111-aaaa-aaaa-aaaa-100000000001");
      Guid artistId = new Guid("11111111-aaaa-aaaa-aaaa-100000000002");

      Guid trackRelationship = new Guid("22222222-bbbb-bbbb-bbbb-200000000001");
      Guid albumRelationship = new Guid("33333333-cccc-cccc-cccc-300000000001");
      Guid artistRelationship = new Guid("44444444-dddd-dddd-dddd-400000000001");

      IDictionary<Guid, IList<MediaItemAspect>> aspects1 = new Dictionary<Guid, IList<MediaItemAspect>>();

      MultipleMediaItemAspect resourceAspect1 = new MultipleMediaItemAspect(ProviderResourceAspect.Metadata);
      resourceAspect1.Deleted = true;
      resourceAspect1.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, "c:\\file.mp3");
      MediaItemAspect.AddOrUpdateAspect(aspects1, resourceAspect1);

      MediaItemAspect.AddOrUpdateRelationship(aspects1, trackRelationship, albumRelationship, albumId, 1);
      MediaItemAspect.AddOrUpdateRelationship(aspects1, trackRelationship, artistRelationship, artistId, 0);

      MediaItem track1 = new MediaItem(trackId, aspects1);

      TextWriter writer = new StringWriter();
      XmlWriter serialiser = new XmlTextWriter(writer);
      serialiser.WriteStartElement("Test"); // Wrapper around the tracks
      // Write the track twice
      track1.Serialize(serialiser);
      track1.Serialize(serialiser);
      serialiser.WriteEndElement();

      //Console.WriteLine("XML: {0}", writer.ToString());
      //Assert.AreEqual("<MI Id=\"" + trackId + "\"><Relationship ItemType=\"" + AudioAspect.RELATIONSHIP_TRACK + "\" RelationshipType=\"" + AlbumAspect.RELATIONSHIP_ALBUM + "\" RelationshipId=\"" + albumId + "\" /></MI>", trackText.ToString(), "Track XML");

      XmlReader reader = XmlReader.Create(new StringReader(writer.ToString()));
      reader.Read(); // Test
      //Console.WriteLine("Reader state Test, {0} {1}", reader.NodeType, reader.Name);

      // Read the track once
      reader.Read(); // MI
      //Console.WriteLine("Reader state track2, {0} {1}", reader.NodeType, reader.Name);
      MediaItem track2 = MediaItem.Deserialize(reader);

      IList<MultipleMediaItemAspect> resourceAspect2;
      Assert.IsTrue(MediaItemAspect.TryGetAspects(track2.Aspects, ProviderResourceAspect.Metadata, out resourceAspect2), "Resource aspects");
      Assert.AreEqual(true, resourceAspect2[0].Deleted, "Track deleted status");
      Assert.AreEqual("c:\\file.mp3", resourceAspect2[0].GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH), "Track location");
      IList<MediaItemAspect> relationships2 = track2[RelationshipAspect.ASPECT_ID];
      Assert.IsTrue(track2[RelationshipAspect.ASPECT_ID] != null, "Relationship aspects");
      Assert.AreEqual(relationships2.Count, 2, "Track relationship count");
      Assert.AreEqual(trackRelationship, relationships2[0].GetAttributeValue(RelationshipAspect.ATTR_ROLE), "Track -> album item type");
      Assert.AreEqual(albumRelationship, relationships2[0].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ROLE), "Track -> album relationship type");
      Assert.AreEqual(albumId, relationships2[0].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ID), "Track -> album relationship ID");
      Assert.AreEqual(trackRelationship, relationships2[1].GetAttributeValue(RelationshipAspect.ATTR_ROLE), "Track -> album item type");
      Assert.AreEqual(artistRelationship, relationships2[1].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ROLE), "Track -> album relationship type");
      Assert.AreEqual(artistId, relationships2[1].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ID), "Track -> album relationship ID");

      // Read the track a second time (
      //Console.WriteLine("Reader state track3, {0} {1}", reader.NodeType, reader.Name);
      MediaItem track3 = MediaItem.Deserialize(reader);

      IList<MultipleMediaItemAspect> resourceAspect3;
      Assert.IsTrue(MediaItemAspect.TryGetAspects(track3.Aspects, ProviderResourceAspect.Metadata, out resourceAspect3), "Resource aspects");
      Assert.AreEqual("c:\\file.mp3", resourceAspect3[0].GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH), "Track location");
      IList<MediaItemAspect> relationships3 = track3[RelationshipAspect.ASPECT_ID];
      Assert.IsTrue(track3[RelationshipAspect.ASPECT_ID] != null, "Relationship aspects");
      Assert.AreEqual(2, relationships3.Count, "Track relationship count");
      Assert.AreEqual(trackRelationship, relationships3[0].GetAttributeValue(RelationshipAspect.ATTR_ROLE), "Track -> album item type");
      Assert.AreEqual(albumRelationship, relationships3[0].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ROLE), "Track -> album relationship type");
      Assert.AreEqual(albumId, relationships3[0].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ID), "Track -> album relationship ID");
      Assert.AreEqual(trackRelationship, relationships3[1].GetAttributeValue(RelationshipAspect.ATTR_ROLE), "Track -> album item type");
      Assert.AreEqual(artistRelationship, relationships3[1].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ROLE), "Track -> album relationship type");
      Assert.AreEqual(artistId, relationships3[1].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ID), "Track -> album relationship ID");

      reader.Read(); // Test
    }

    [Test]
    public void TestRelationshipItem()
    {
      Guid albumId = new Guid("11111111-aaaa-aaaa-aaaa-100000000001");
      Guid artistId = new Guid("11111111-aaaa-aaaa-aaaa-100000000002");

      Guid trackRelationship = new Guid("22222222-bbbb-bbbb-bbbb-200000000001");
      Guid albumRelationship = new Guid("33333333-cccc-cccc-cccc-300000000001");
      Guid artistRelationship = new Guid("44444444-dddd-dddd-dddd-400000000001");

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();

      MediaItemAspect.AddOrUpdateRelationship(aspects, trackRelationship, artistRelationship, artistId, 0);

      Assert.AreEqual(aspects.Keys.Count, 1, "aspect key count");

      IList<MultipleMediaItemAspect> relationships;

      MediaItemAspect.AddOrUpdateRelationship(aspects, trackRelationship, albumRelationship, albumId, 1);
      Assert.IsTrue(MediaItemAspect.TryGetAspects(aspects, RelationshipAspect.Metadata, out relationships), "Relationships");
      Assert.AreEqual(relationships.Count, 2);

      MediaItemAspect.AddOrUpdateRelationship(aspects, trackRelationship, albumRelationship, albumId, 2); // This will update the existing track / album relationship
      Assert.IsTrue(MediaItemAspect.TryGetAspects(aspects, RelationshipAspect.Metadata, out relationships), "Relationships");
      Assert.AreEqual(relationships.Count, 2);
    }

    [Test]
    public void TestExternalItem()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();

      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, "test", ExternalIdentifierAspect.TYPE_EPISODE, "123");

      Assert.AreEqual(aspects.Keys.Count, 1, "aspect key count");

      IList<MultipleMediaItemAspect> externalIdentifiers;

      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, "test", ExternalIdentifierAspect.TYPE_SERIES, "456");
      Assert.IsTrue(MediaItemAspect.TryGetAspects(aspects, ExternalIdentifierAspect.Metadata, out externalIdentifiers), "Relationships");
      Assert.AreEqual(externalIdentifiers.Count, 2);

      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, "test", ExternalIdentifierAspect.TYPE_SERIES, "789");
      Assert.IsTrue(MediaItemAspect.TryGetAspects(aspects, ExternalIdentifierAspect.Metadata, out externalIdentifiers), "Relationships");
      Assert.AreEqual(externalIdentifiers.Count, 2);
    }

    [Test]
    public void TestConvenienceMethod()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();

      MultipleMediaItemAspect providerAspect = MediaItemAspect.CreateAspect(aspects, ProviderResourceAspect.Metadata);
      providerAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
      providerAspect.SetAttribute(ProviderResourceAspect.ATTR_PRIMARY, true);
      providerAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "audio/mp3");

      Assert.AreEqual(aspects.Keys.Count, 1, "aspect key count");

      IList<MultipleMediaItemAspect> providerAspects2;
      Assert.IsTrue(MediaItemAspect.TryGetAspects(aspects, ProviderResourceAspect.Metadata, out providerAspects2), "ProviderResource");
      Assert.AreEqual("audio/mp3", providerAspects2[0][ProviderResourceAspect.ATTR_MIME_TYPE], "MIME type");
    }
  }
}
