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

using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;

namespace Test.Common
{
  [TestFixture]
  public class TestMediaItemQuery
  {
    [Test]
    public void TestRelationshipFilter()
    {
      Guid itemId = new Guid("11111111-aaaa-aaaa-aaaa-111111111111");
      Guid itemType = new Guid("22222222-bbbb-bbbb-bbbb-222222222222");
      Guid relationshipType = new Guid("33333333-cccc-cccc-cccc-333333333333");

      MediaItemQuery query1 = new MediaItemQuery(new Guid[] { MediaAspect.ASPECT_ID }, new RelationshipFilter(itemType, relationshipType, itemId));

      TextWriter writer = new StringWriter();
      XmlWriter serialiser = new XmlTextWriter(writer);
      query1.Serialize(serialiser);

      //Console.WriteLine("XML: {0}", writer.ToString());

      XmlReader reader = XmlReader.Create(new StringReader(writer.ToString()));
      MediaItemQuery query2 = MediaItemQuery.Deserialize(reader);

      Assert.IsTrue(query2.Filter is RelationshipFilter, "Query filter type");
      RelationshipFilter filter = (RelationshipFilter)query2.Filter;
      Assert.AreEqual(filter.LinkedMediaItemId, itemId, "Filter item linked ID");
      Assert.AreEqual(filter.Role, itemType, "Filter item type");
      Assert.AreEqual(filter.LinkedRole, relationshipType, "Filter item linked role");
    }
  }
}
