#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
