using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Test.Common
{
  internal class MediaItemAspectTypeRegistration : IMediaItemAspectTypeRegistration
  {
    protected IDictionary<Guid, MediaItemAspectMetadata> _locallyKnownMediaItemAspectTypes =
        new Dictionary<Guid, MediaItemAspectMetadata>();

    public MediaItemAspectTypeRegistration()
    {
      _locallyKnownMediaItemAspectTypes[MediaAspect.ASPECT_ID] = MediaAspect.Metadata;
      _locallyKnownMediaItemAspectTypes[AudioAspect.ASPECT_ID] = AudioAspect.Metadata;
      _locallyKnownMediaItemAspectTypes[ProviderResourceAspect.ASPECT_ID] = ProviderResourceAspect.Metadata;
      _locallyKnownMediaItemAspectTypes[RelationshipAspect.ASPECT_ID] = RelationshipAspect.Metadata;
    }

    public IDictionary<Guid, MediaItemAspectMetadata> LocallyKnownMediaItemAspectTypes
    {
      get { return _locallyKnownMediaItemAspectTypes; }
    }

    public void RegisterLocallyKnownMediaItemAspectType(MediaItemAspectMetadata miaType)
    {
      throw new NotImplementedException();
    }
  }

  [TestClass]
  public class TestMediaItem
  {
    public TestMediaItem()
    {
      ServiceRegistration.Set<IMediaItemAspectTypeRegistration>(new MediaItemAspectTypeRegistration());
    }

    private void AddRelationship(IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid index, Guid localRole, Guid remoteRole, Guid remoteId)
    {
      MultipleMediaItemAspect relationship = new MultipleMediaItemAspect(index, RelationshipAspect.Metadata);
      relationship.SetAttribute(RelationshipAspect.ATTR_ROLE, localRole);
      relationship.SetAttribute(RelationshipAspect.ATTR_LINKED_ROLE, remoteRole);
      relationship.SetAttribute(RelationshipAspect.ATTR_LINKED_ID, remoteId);
      MediaItemAspect.AddAspect(aspects, relationship);
    }

    [TestMethod]
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
      Assert.AreEqual(mediaAspect1.GetAttributeValue<string>(MediaAspect.ATTR_TITLE), "The Album", "Album title");

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
      Assert.IsTrue(MediaItemAspect.TryGetAspect(track2.Aspects, MediaAspect.Metadata, out mediaAspect2), "Media aspect");
      Assert.AreEqual(mediaAspect2.GetAttributeValue<string>(MediaAspect.ATTR_TITLE), "The Album", "Album title");

      // Read the track again
      //Console.WriteLine("Reader state track3, {0} {1}", reader.NodeType, reader.Name);
      MediaItem track3 = MediaItem.Deserialize(reader);

      SingleMediaItemAspect mediaAspect3;
      Assert.IsTrue(MediaItemAspect.TryGetAspect(track3.Aspects, MediaAspect.Metadata, out mediaAspect3), "Media aspect");
      Assert.AreEqual(mediaAspect3.GetAttributeValue<string>(MediaAspect.ATTR_TITLE), "The Album", "Album title");

      reader.Read(); // Test
    }

    [TestMethod]
    public void TestComplexItem()
    {
      Guid trackId = new Guid("11111111-aaaa-aaaa-aaaa-100000000000");
      Guid albumId = new Guid("11111111-aaaa-aaaa-aaaa-100000000001");
      Guid artistId = new Guid("11111111-aaaa-aaaa-aaaa-100000000002");

      Guid trackAspect = new Guid("22222222-bbbb-bbbb-bbbb-200000000000");
      Guid trackRelationship = new Guid("22222222-bbbb-bbbb-bbbb-200000000001");

      Guid albumAspect = new Guid("33333333-cccc-cccc-cccc-300000000000");
      Guid albumRelationship = new Guid("33333333-cccc-cccc-cccc-300000000001");

      Guid artistAspect = new Guid("44444444-dddd-dddd-dddd-400000000000");
      Guid artistRelationship = new Guid("44444444-dddd-dddd-dddd-400000000001");

      IDictionary<Guid, IList<MediaItemAspect>> aspects1 = new Dictionary<Guid, IList<MediaItemAspect>>();

      SingleMediaItemAspect track1RA = new SingleMediaItemAspect(ProviderResourceAspect.Metadata);
      track1RA.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, "c:\\file.mp3");
      MediaItemAspect.SetAspect(aspects1, track1RA);

      AddRelationship(aspects1, albumAspect, trackRelationship, albumRelationship, albumId);
      AddRelationship(aspects1, artistAspect, trackRelationship, artistRelationship, artistId);

      MediaItem track1 = new MediaItem(trackId, aspects1);

      SingleMediaItemAspect resourceAspect1;
      Assert.IsTrue(MediaItemAspect.TryGetAspect(track1.Aspects, ProviderResourceAspect.Metadata, out resourceAspect1), "Resource aspects");
      Assert.AreEqual(resourceAspect1.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH), "c:\\file.mp3", "Track location");
      IList<MediaItemAspect> relationships1 = track1[RelationshipAspect.ASPECT_ID];
      Assert.IsTrue(track1[RelationshipAspect.ASPECT_ID] != null, "Relationship aspects");
      Assert.AreEqual(relationships1.Count, 2, "Track relationship count");
      Assert.AreEqual(relationships1[0].GetAttributeValue(RelationshipAspect.ATTR_ROLE), trackRelationship, "Track -> album item type");
      Assert.AreEqual(relationships1[0].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ROLE), albumRelationship, "Track -> album relationship type");
      Assert.AreEqual(relationships1[0].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ID), albumId, "Track -> album relationship ID");
      Assert.AreEqual(relationships1[1].GetAttributeValue(RelationshipAspect.ATTR_ROLE), trackRelationship, "Track -> album item type");
      Assert.AreEqual(relationships1[1].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ROLE), artistRelationship, "Track -> album relationship type");
      Assert.AreEqual(relationships1[1].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ID), artistId, "Track -> album relationship ID");

      TextWriter writer = new StringWriter();
      XmlWriter serialiser = new XmlTextWriter(writer);
      serialiser.WriteStartElement("Test"); // Wrapper around the tracks
      // Write the track twice
      track1.Serialize(serialiser);
      track1.Serialize(serialiser);
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

      SingleMediaItemAspect resourceAspect2;
      Assert.IsTrue(MediaItemAspect.TryGetAspect(track2.Aspects, ProviderResourceAspect.Metadata, out resourceAspect2), "Resource aspects");
      Assert.AreEqual(resourceAspect2.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH), "c:\\file.mp3", "Track location");
      IList<MediaItemAspect> relationships2 = track2[RelationshipAspect.ASPECT_ID];
      Assert.IsTrue(track2[RelationshipAspect.ASPECT_ID] != null, "Relationship aspects");
      Assert.AreEqual(relationships2.Count, 2, "Track relationship count");
      Assert.AreEqual(relationships2[0].GetAttributeValue(RelationshipAspect.ATTR_ROLE), trackRelationship, "Track -> album item type");
      Assert.AreEqual(relationships2[0].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ROLE), albumRelationship, "Track -> album relationship type");
      Assert.AreEqual(relationships2[0].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ID), albumId, "Track -> album relationship ID");
      Assert.AreEqual(relationships2[1].GetAttributeValue(RelationshipAspect.ATTR_ROLE), trackRelationship, "Track -> album item type");
      Assert.AreEqual(relationships2[1].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ROLE), artistRelationship, "Track -> album relationship type");
      Assert.AreEqual(relationships2[1].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ID), artistId, "Track -> album relationship ID");

      // Read the track a second time (
      //Console.WriteLine("Reader state track3, {0} {1}", reader.NodeType, reader.Name);
      MediaItem track3 = MediaItem.Deserialize(reader);

      SingleMediaItemAspect resourceAspect3;
      Assert.IsTrue(MediaItemAspect.TryGetAspect(track3.Aspects, ProviderResourceAspect.Metadata, out resourceAspect3), "Resource aspects");
      Assert.AreEqual(resourceAspect3.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH), "c:\\file.mp3", "Track location");
      IList<MediaItemAspect> relationships3 = track3[RelationshipAspect.ASPECT_ID];
      Assert.IsTrue(track3[RelationshipAspect.ASPECT_ID] != null, "Relationship aspects");
      Assert.AreEqual(relationships3.Count, 2, "Track relationship count");
      Assert.AreEqual(relationships3[0].GetAttributeValue(RelationshipAspect.ATTR_ROLE), trackRelationship, "Track -> album item type");
      Assert.AreEqual(relationships3[0].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ROLE), albumRelationship, "Track -> album relationship type");
      Assert.AreEqual(relationships3[0].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ID), albumId, "Track -> album relationship ID");
      Assert.AreEqual(relationships3[1].GetAttributeValue(RelationshipAspect.ATTR_ROLE), trackRelationship, "Track -> album item type");
      Assert.AreEqual(relationships3[1].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ROLE), artistRelationship, "Track -> album relationship type");
      Assert.AreEqual(relationships3[1].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ID), artistId, "Track -> album relationship ID");

      reader.Read(); // Test
    }
  }
}
