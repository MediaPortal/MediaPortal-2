using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Mock;
using Test.Common;

namespace Test.Backend
{
  public class TestBackendUtils
  {
    public static SingleTestMIA CreateSingleMIA(string table, Cardinality cardinality, bool createStringAttribute, bool createIntegerAttribute)
    {
      SingleTestMIA mia = TestCommonUtils.CreateSingleMIA(table, cardinality, createStringAttribute, createIntegerAttribute);

      MockCore.AddMediaItemAspectStorage(mia.Metadata);

      return mia;
    }

    public static MultipleTestMIA CreateMultipleMIA(string table, Cardinality cardinality, bool createStringAttribute, bool createIntegerAttribute)
    {
      MultipleTestMIA mia = TestCommonUtils.CreateMultipleMIA(table, cardinality, createStringAttribute, createIntegerAttribute);

      MockCore.AddMediaItemAspectStorage(mia.Metadata);

      return mia;
    }
  }
}
