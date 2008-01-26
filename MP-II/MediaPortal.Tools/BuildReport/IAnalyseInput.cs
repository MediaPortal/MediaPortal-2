using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaPortal.Tools.BuildReport
{
  interface IAnalyseInput
  {
    Solution Solution
    {
      get;
    }

    void Parse(string input);
  }
}
