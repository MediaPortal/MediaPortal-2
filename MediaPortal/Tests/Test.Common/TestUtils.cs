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
using System.Linq;
using MediaPortal.Common.MediaManagement;

namespace Test.Common
{
  public class TestCommonUtils
  {
    public static SingleTestMIA CreateSingleMIA(string table, Cardinality cardinality, bool createStringAttribute, bool createIntegerAttribute)
    {
      SingleTestMIA mia = new SingleTestMIA();

      mia.ASPECT_ID = Guid.NewGuid();

      IList<MediaItemAspectMetadata.SingleAttributeSpecification> attributes = new List<MediaItemAspectMetadata.SingleAttributeSpecification>();
      if (createStringAttribute)
        attributes.Add(mia.ATTR_STRING = MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("ATTR_STRING", 10, cardinality, false));
      if (createIntegerAttribute)
        attributes.Add(mia.ATTR_INTEGER = MediaItemAspectMetadata.CreateSingleAttributeSpecification("ATTR_INTEGER", typeof(Int32), cardinality, true));

      mia.Metadata = new SingleMediaItemAspectMetadata(mia.ASPECT_ID, table, attributes.ToArray());

      return mia;
    }

    public static MultipleTestMIA CreateMultipleMIA(string table, Cardinality cardinality, bool createStringAttribute, bool createIntegerAttribute)
    {
      MultipleTestMIA mia = new MultipleTestMIA();

      mia.ASPECT_ID = Guid.NewGuid();

      IList<MediaItemAspectMetadata.MultipleAttributeSpecification> attributes = new List<MediaItemAspectMetadata.MultipleAttributeSpecification>();
      attributes.Add(mia.ATTR_ID = MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("ATTR_ID", 10, Cardinality.Inline, true));
      if (createStringAttribute)
        attributes.Add(mia.ATTR_STRING = MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("ATTR_STRING", 10, cardinality, false));
      if (createIntegerAttribute)
        attributes.Add(mia.ATTR_INTEGER = MediaItemAspectMetadata.CreateMultipleAttributeSpecification("ATTR_INTEGER", typeof(Int32), cardinality, true));

      mia.Metadata = new MultipleMediaItemAspectMetadata(mia.ASPECT_ID, table, attributes.ToArray(), new[] { mia.ATTR_ID } );

      return mia;
    }
  }
}
