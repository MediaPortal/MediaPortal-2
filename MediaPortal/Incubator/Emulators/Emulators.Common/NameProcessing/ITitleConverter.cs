using Emulators.Common.Games;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.NameProcessing
{
  public interface ITitleConverter
  {
    bool ConvertTitle(GameInfo gameInfo);
  }
}
