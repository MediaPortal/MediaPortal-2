using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Test.Common
{
  [TestClass]
  public class TestMediaItemQuery
  {
    [TestMethod]
    public void TestFilter()
    {
      Guid itemId = new Guid("11111111-aaaa-aaaa-aaaa-111111111111");
      Guid itemType = new Guid("22222222-bbbb-bbbb-bbbb-222222222222");
      Guid relationshipType = new Guid("33333333-cccc-cccc-cccc-333333333333");

      MediaItemQuery query1 = new MediaItemQuery(new Guid[] { MediaAspect.ASPECT_ID }, new RelationshipFilter(itemId, itemType, relationshipType));

      TextWriter writer = new StringWriter();
      XmlWriter serialiser = new XmlTextWriter(writer);
      query1.Serialize(serialiser);

      Console.WriteLine("XML: {0}", writer.ToString());

      XmlReader reader = XmlReader.Create(new StringReader(writer.ToString()));
      MediaItemQuery query2 = MediaItemQuery.Deserialize(reader);

      Assert.IsTrue(query2.Filter is RelationshipFilter, "Query filter type");
      RelationshipFilter filter = (RelationshipFilter)query2.Filter;
      Assert.AreEqual(filter.ItemId, itemId, "Filter item ID");
      Assert.AreEqual(filter.Role, itemType, "Filter item type");
      Assert.AreEqual(filter.LinkedRole, relationshipType, "Filter item linked role");
    }
  }
}
