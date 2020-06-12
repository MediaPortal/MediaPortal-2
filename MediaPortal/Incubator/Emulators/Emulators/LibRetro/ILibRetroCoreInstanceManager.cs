using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro
{
  public interface ILibRetroCoreInstanceManager
  {
    bool TrySetCoreLoading(string corePath);
    void SetCoreUnloaded(string corePath);
  }
}
