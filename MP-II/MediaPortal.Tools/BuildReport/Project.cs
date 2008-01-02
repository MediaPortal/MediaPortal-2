using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaPortal.Tools.BuildReport
{
  class Project
  {
    public string name;
    public Solution.Compile build;
    public int errors = 0;
    public int warnings = 0;
  }
}
