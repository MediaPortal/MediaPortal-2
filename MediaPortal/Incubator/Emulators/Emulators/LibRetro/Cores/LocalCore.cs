using SharpRetro.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Cores
{
  public class LocalCore
  {
    public LocalCore()
    {
      Supported = true;
    }

    public string Url { get; set; }
    public string ArchiveName { get; set; }
    public string CoreName { get; set; }
    public bool Supported { get; set; }
    public CoreInfo Info { get; set; }
  }
}
