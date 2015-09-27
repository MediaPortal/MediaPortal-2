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
