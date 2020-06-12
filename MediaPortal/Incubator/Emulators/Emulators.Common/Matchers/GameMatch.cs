using MediaPortal.Extensions.OnlineLibraries.Matches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.Matchers
{
  public class GameMatch<T> : BaseMatch<T>
  {
    public string GameName;
    public string Platform;

    public override string ToString()
    {
      return string.Format("{0}: {1} [{2}]", GameName, Platform, Id);
    }
  }
}
