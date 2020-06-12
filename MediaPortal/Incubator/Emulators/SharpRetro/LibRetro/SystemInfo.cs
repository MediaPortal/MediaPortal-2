using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.LibRetro
{
  public class SystemInfo
  {
    public string LibraryName { get; set; }
    public string LibraryVersion { get; set; }
    public string ValidExtensions { get; set; }
    public bool NeedsFullPath { get; set; }
    public bool BlockExtract { get; set; }
  }
}
