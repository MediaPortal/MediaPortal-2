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
  [TestClass]
  public class TestMediaItemAspectMetadata
  {
    [TestMethod]
    public void TestMetadata()
    {
      TextWriter writer = new StringWriter();
      XmlWriter serialiser = new XmlTextWriter(writer);
      serialiser.WriteStartElement("Test"); // Wrapper around the albums
      MediaAspect.Metadata.Serialize(serialiser);
      RelationshipAspect.Metadata.Serialize(serialiser);
      serialiser.WriteEndElement();

      Console.WriteLine("XML: {0}", writer.ToString());
      //Assert.AreEqual("<MI Id=\"" + trackId + "\"><Relationship ItemType=\"" + AudioAspect.RELATIONSHIP_TRACK + "\" RelationshipType=\"" + AlbumAspect.RELATIONSHIP_ALBUM + "\" RelationshipId=\"" + albumId + "\" /></MI>", trackText.ToString(), "Track XML");

      XmlReader reader = XmlReader.Create(new StringReader(writer.ToString()));
      reader.Read(); // Test
      Console.WriteLine("Reader state Test, {0} {1}", reader.NodeType, reader.Name);

      // Media metadata
      reader.Read(); // MI
      Console.WriteLine("Reader state metadata1, {0} {1}", reader.NodeType, reader.Name);
      MediaItemAspectMetadata metadata1 = MediaItemAspectMetadata.Deserialize(reader);

      // Relationship metadata
      Console.WriteLine("Reader state metadata2, {0} {1}", reader.NodeType, reader.Name);
      MediaItemAspectMetadata metadata2 = MediaItemAspectMetadata.Deserialize(reader);

      reader.Read(); // Test
    }
  }
}
