using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.Hid
{
  public interface IHidDevice
  {
    bool UpdateState(HidState state);
  }
}
