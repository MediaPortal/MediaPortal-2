using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using NUnit.Framework;

namespace Tests.Common
{
  [TestFixture]
  public class TestRelationshipExtraction
  {
    [OneTimeSetUp]
    public void SetUp()
    {
      IMediaItemAspectTypeRegistration miatr = new TestMediaItemAspectTypeRegistration();
      ServiceRegistration.Set(miatr);

      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(MediaAspect.Metadata).Wait();
      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(AudioAspect.Metadata).Wait();
      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(ProviderResourceAspect.Metadata).Wait();
      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(RelationshipAspect.Metadata).Wait();
    }

    [Test]
    public void TestRelationshipSerialization()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects1 = new Dictionary<Guid, IList<MediaItemAspect>>();
      Guid trackId = new Guid("11111111-aaaa-aaaa-aaaa-100000000000");
      Guid albumId = new Guid("11111111-aaaa-aaaa-aaaa-100000000001");
      Guid artistId = new Guid("11111111-aaaa-aaaa-aaaa-100000000002");

      Guid trackRelationship = new Guid("22222222-bbbb-bbbb-bbbb-200000000001");
      Guid albumRelationship = new Guid("33333333-cccc-cccc-cccc-300000000001");
      Guid artistRelationship = new Guid("44444444-dddd-dddd-dddd-400000000001");

      MediaItemAspect.AddOrUpdateRelationship(aspects1, trackRelationship, albumRelationship, albumId, true, 1);
      MediaItemAspect.AddOrUpdateRelationship(aspects1, trackRelationship, artistRelationship, artistId, false, 0);

      MultipleMediaItemAspect resourceAspect1 = new MultipleMediaItemAspect(ProviderResourceAspect.Metadata);
      resourceAspect1.Deleted = true;
      resourceAspect1.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, "c:\\file.mp3");
      MediaItemAspect.AddOrUpdateAspect(aspects1, resourceAspect1);

      Guid role1 = new Guid("55555555-eeee-eeee-eeee-500000000001");
      Guid linkedRole1 = new Guid("66666666-ffff-ffff-ffff-600000000001");

      RelationshipItem relationship1 = new RelationshipItem(role1, linkedRole1, aspects1);

      TextWriter writer = new StringWriter();
      XmlWriter serialiser = new XmlTextWriter(writer);
      serialiser.WriteStartElement("Test"); // Wrapper around the relationships
      // Write the relationship twice
      relationship1.Serialize(serialiser);
      relationship1.Serialize(serialiser);
      serialiser.WriteEndElement();

      XmlReader reader = XmlReader.Create(new StringReader(writer.ToString()));
      reader.Read(); // Test

      // Read the relationship once
      reader.Read(); // RI
      RelationshipItem relationship2 = RelationshipItem.Deserialize(reader);

      Assert.AreEqual(relationship2.Role, role1, "Role");
      Assert.AreEqual(relationship2.LinkedRole, linkedRole1, "Linked role");
      IList<MultipleMediaItemAspect> resourceAspect2;
      Assert.IsTrue(MediaItemAspect.TryGetAspects(relationship2.Aspects, ProviderResourceAspect.Metadata, out resourceAspect2), "Resource aspects");
      Assert.AreEqual(true, resourceAspect2[0].Deleted, "Track deleted status");
      Assert.AreEqual("c:\\file.mp3", resourceAspect2[0].GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH), "Track location");
      IList<MediaItemAspect> relationships2 = relationship2.Aspects[RelationshipAspect.ASPECT_ID];
      Assert.IsTrue(relationship2.Aspects[RelationshipAspect.ASPECT_ID] != null, "Relationship aspects");
      Assert.AreEqual(relationships2.Count, 2, "Track relationship count");
      Assert.AreEqual(trackRelationship, relationships2[0].GetAttributeValue(RelationshipAspect.ATTR_ROLE), "Track -> album item type");
      Assert.AreEqual(albumRelationship, relationships2[0].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ROLE), "Track -> album relationship type");
      Assert.AreEqual(albumId, relationships2[0].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ID), "Track -> album relationship ID");
      Assert.AreEqual(trackRelationship, relationships2[1].GetAttributeValue(RelationshipAspect.ATTR_ROLE), "Track -> album item type");
      Assert.AreEqual(artistRelationship, relationships2[1].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ROLE), "Track -> album relationship type");
      Assert.AreEqual(artistId, relationships2[1].GetAttributeValue(RelationshipAspect.ATTR_LINKED_ID), "Track -> album relationship ID");

      // Read the relationship a second time
      RelationshipItem relationship3 = RelationshipItem.Deserialize(reader);

      Assert.AreEqual(relationship3.Role, relationship2.Role, "Role");
      Assert.AreEqual(relationship3.LinkedRole, relationship2.LinkedRole, "Linked role");
      IList<MultipleMediaItemAspect> resourceAspect3;
      Assert.IsTrue(MediaItemAspect.TryGetAspects(relationship3.Aspects, ProviderResourceAspect.Metadata, out resourceAspect3), "Resource aspects");
      Assert.AreEqual("c:\\file.mp3", resourceAspect3[0].GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH), "Track location");
      IList<MediaItemAspect> relationships3 = relationship3.Aspects[RelationshipAspect.ASPECT_ID];
      Assert.IsTrue(relationship3.Aspects[RelationshipAspect.ASPECT_ID] != null, "Relationship aspects");
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
    public void TestCreateExternalItemFilter()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, "tvdb_01");
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, "tvmaze_01");
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_EPISODE, "tvdb_02");
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_EPISODE, "tvmaze_02");

      IFilter seriesFilter = RelationshipExtractorUtils.CreateExternalItemFilter(aspects, ExternalIdentifierAspect.TYPE_SERIES);
      Assert.AreEqual(seriesFilter.ToString(),
        "ExternalIdentifier.Source EQ TVDB And ExternalIdentifier.Type EQ SERIES And ExternalIdentifier.Id EQ tvdb_01 Or ExternalIdentifier.Source EQ TVMAZE And ExternalIdentifier.Type EQ SERIES And ExternalIdentifier.Id EQ tvmaze_01");

      IFilter episodeFilter = RelationshipExtractorUtils.CreateExternalItemFilter(aspects, ExternalIdentifierAspect.TYPE_EPISODE);
      Assert.AreEqual(episodeFilter.ToString(),
        "ExternalIdentifier.Source EQ TVDB And ExternalIdentifier.Type EQ EPISODE And ExternalIdentifier.Id EQ tvdb_02 Or ExternalIdentifier.Source EQ TVMAZE And ExternalIdentifier.Type EQ EPISODE And ExternalIdentifier.Id EQ tvmaze_02");
    }

    [Test]
    public void TestCreateExternalItemIdentifiers()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, "tvdb_01");
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, "tvmaze_01");
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_EPISODE, "tvdb_02");
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_EPISODE, "tvmaze_02");
      
      var seriesIdentifiers = RelationshipExtractorUtils.CreateExternalItemIdentifiers(aspects, ExternalIdentifierAspect.TYPE_SERIES);
      List<string> expectedSeriesIdentifiers = new List<string>
      {
        string.Format("{0} | {1} | {2}", ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, "tvdb_01"),
        string.Format("{0} | {1} | {2}", ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, "tvmaze_01")
      };
      
      CollectionAssert.AreEqual(seriesIdentifiers, expectedSeriesIdentifiers);

      var episodeIdentifiers = RelationshipExtractorUtils.CreateExternalItemIdentifiers(aspects, ExternalIdentifierAspect.TYPE_EPISODE);
      List<string> expectedEpisodeIdentifiers = new List<string>
      {
        string.Format("{0} | {1} | {2}", ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_EPISODE, "tvdb_02"),
        string.Format("{0} | {1} | {2}", ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_EPISODE, "tvmaze_02")
      };

      CollectionAssert.AreEqual(episodeIdentifiers, expectedEpisodeIdentifiers);
    }
  }
}
