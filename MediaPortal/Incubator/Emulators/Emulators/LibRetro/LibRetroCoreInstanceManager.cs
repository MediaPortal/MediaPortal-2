using MediaPortal.Common;
using MediaPortal.Common.Logging;
using SharpRetro.LibRetro;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro
{
  public class LibRetroCoreInstanceManager : ILibRetroCoreInstanceManager
  {
    SynchronizedCollection<string> _loadedCores = new SynchronizedCollection<string>();

    public bool TrySetCoreLoading(string corePath)
    {
      if (corePath == null)
        return false;

      bool loaded = false;
      lock (_loadedCores.SyncRoot)
      {
        if (!_loadedCores.Contains(corePath))
        {
          _loadedCores.Add(corePath);
          loaded = true;
        }
      }
      if (!loaded)
        ServiceRegistration.Get<ILogger>().Warn("LibRetroCoreInstanceManager: Attempt to load a core that was already loaded '{0}'", corePath);
      return loaded;
    }

    public void SetCoreUnloaded(string corePath)
    {
      _loadedCores.Remove(corePath);
    }
  }
}
