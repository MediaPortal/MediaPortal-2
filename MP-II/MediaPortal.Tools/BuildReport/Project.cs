using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BuildReport
{
  class Project
  {
    public string name;
    public string build;
    public int errors = 0;
    public int warnings = 0;
  }
}
