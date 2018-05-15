using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Mock;
using NUnit.Framework;

namespace Tests.Server.Backend
{
  [TestFixture]
  public class TestMediaItemAspectMetadata
  {
    private void AssertMultipleMIAMSpec(ICollection<MediaItemAspectMetadata.AttributeSpecification> src, ICollection<MediaItemAspectMetadata.AttributeSpecification> dest, MediaItemAspectMetadata.MultipleAttributeSpecification attr)
    {
      MediaItemAspectMetadata.MultipleAttributeSpecification srcSpec = src.FirstOrDefault(x => x.AttributeName == attr.AttributeName) as MediaItemAspectMetadata.MultipleAttributeSpecification;
      Assert.IsNotNull(srcSpec, "Source spec");

      MediaItemAspectMetadata.MultipleAttributeSpecification destSpec = dest.FirstOrDefault(x => x.AttributeName == attr.AttributeName) as MediaItemAspectMetadata.MultipleAttributeSpecification;
      Assert.IsNotNull(destSpec, "Dest spec");

      Assert.AreEqual(srcSpec.AttributeName, destSpec.AttributeName, "Attribute name");
      Assert.AreEqual(srcSpec.AttributeType, destSpec.AttributeType, "Attribute type");
      Assert.AreEqual(srcSpec.Cardinality, destSpec.Cardinality, "Attribute cardinality");
      Assert.AreEqual(srcSpec.IsCollectionAttribute, destSpec.IsCollectionAttribute, "Attribute collection attribute");
      Assert.AreEqual(srcSpec.IsIndexed, destSpec.IsIndexed, "Attribute indexed");
      Assert.AreEqual(srcSpec.MaxNumChars, destSpec.MaxNumChars, "Attribute max characters");
    }

    [Test]
    public void TestSingleMIAM()
    {
      SingleTestMIA src = TestCommonUtils.CreateSingleMIA("Single", Cardinality.Inline, true, false);

      TextWriter writer = new StringWriter();
      XmlWriter serialiser = new XmlTextWriter(writer);
      serialiser.WriteStartElement("Test"); // Wrapper around the albums
      src.Metadata.Serialize(serialiser);
      serialiser.WriteEndElement();

      Console.WriteLine("XML: {0}", writer);

      XmlReader reader = XmlReader.Create(new StringReader(writer.ToString()));
      reader.Read(); // Test tag

      reader.Read(); // MIAM tag
      SingleMediaItemAspectMetadata dest = MediaItemAspectMetadata.Deserialize(reader) as SingleMediaItemAspectMetadata;

      Assert.IsNotNull(dest);
      Assert.AreEqual(src.Metadata.AspectId, dest.AspectId);
      Assert.AreEqual(src.Metadata.AttributeSpecifications.Count, dest.AttributeSpecifications.Count);

      MediaItemAspectMetadata.SingleAttributeSpecification srcSpec = src.Metadata.AttributeSpecifications[src.ATTR_STRING.AttributeName] as MediaItemAspectMetadata.SingleAttributeSpecification;
      Assert.IsNotNull(srcSpec);

      MediaItemAspectMetadata.SingleAttributeSpecification destSpec = dest.AttributeSpecifications[src.ATTR_STRING.AttributeName] as MediaItemAspectMetadata.SingleAttributeSpecification;
      Assert.IsNotNull(destSpec);

      Assert.AreEqual(srcSpec.AttributeName, destSpec.AttributeName);
      Assert.AreEqual(srcSpec.AttributeType, destSpec.AttributeType);
      Assert.AreEqual(srcSpec.Cardinality, destSpec.Cardinality);
      Assert.AreEqual(srcSpec.IsCollectionAttribute, destSpec.IsCollectionAttribute);
      Assert.AreEqual(srcSpec.IsIndexed, destSpec.IsIndexed);
      Assert.AreEqual(srcSpec.MaxNumChars, destSpec.MaxNumChars);
    }

    [Test]
    public void TestMultipleMIAM()
    {
      MultipleTestMIA src = TestCommonUtils.CreateMultipleMIA("Multiple", Cardinality.Inline, true, false);

      TextWriter writer = new StringWriter();
      XmlWriter serialiser = new XmlTextWriter(writer);
      serialiser.WriteStartElement("Test");
      src.Metadata.Serialize(serialiser);
      serialiser.WriteEndElement();

      Console.WriteLine("XML: {0}", writer);

      XmlReader reader = XmlReader.Create(new StringReader(writer.ToString()));
      reader.Read(); // Test tag

      reader.Read(); // MIAM tag
      MultipleMediaItemAspectMetadata dest = MediaItemAspectMetadata.Deserialize(reader) as MultipleMediaItemAspectMetadata;

      Assert.IsNotNull(dest, "Dest");
      Assert.AreEqual(src.Metadata.AspectId, dest.AspectId, "Aspect ID");
      Assert.AreEqual(src.Metadata.AttributeSpecifications.Count, dest.AttributeSpecifications.Count, "Attribute spec count");

      AssertMultipleMIAMSpec(src.Metadata.AttributeSpecifications.Values, dest.AttributeSpecifications.Values, src.ATTR_STRING);

      Assert.IsNotNull(src.Metadata.UniqueAttributeSpecifications, "Source unique spec");
      Assert.IsNotNull(dest.UniqueAttributeSpecifications, "Source unique spec");

      Assert.AreEqual(src.Metadata.UniqueAttributeSpecifications.Count, dest.UniqueAttributeSpecifications.Count, "Attribute unique spec count");

      AssertMultipleMIAMSpec(src.Metadata.UniqueAttributeSpecifications.Values, dest.UniqueAttributeSpecifications.Values, src.ATTR_ID);
    }
  }
}
