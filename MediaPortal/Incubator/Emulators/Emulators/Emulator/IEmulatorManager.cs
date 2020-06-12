using Emulators.Common.Emulators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Emulator
{
  interface IEmulatorManager
  {
    bool TryGetConfiguration(string mimeType, string extension, out EmulatorConfiguration configuration);
    List<EmulatorConfiguration> Load();
    void Save(List<EmulatorConfiguration> configurations);
    void AddOrUpdate(EmulatorConfiguration configuration);
    void Remove(EmulatorConfiguration configuration);
  }
}
