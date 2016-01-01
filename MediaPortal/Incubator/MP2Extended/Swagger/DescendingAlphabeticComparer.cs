using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MP2Extended.Swagger
{
  public class DescendingAlphabeticComparer : IComparer<string>
  {
    public int Compare(string x, string y)
    {
      return y.CompareTo(x);
    }
  }
}
