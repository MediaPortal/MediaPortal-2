using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.GoodMerge
{
  public static class ExtractorFactory
  {
    public static IExtractor Create(string path)
    {
      return new SharpCompressExtractor(path);
    }
  }
}