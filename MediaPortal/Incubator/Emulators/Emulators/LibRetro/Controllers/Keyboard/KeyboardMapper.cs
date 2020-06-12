using Emulators.LibRetro.Controllers.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Emulators.LibRetro.Controllers.Keyboard
{
  class KeyboardMapper : IDeviceMapper, IDisposable
  {
    protected KeyboardListener _listener;

    public bool SupportsDeadZone
    {
      get { return false; }
    }

    public KeyboardMapper()
    {
      _listener = new KeyboardListener();
    }

    public DeviceInput GetPressedInput()
    {
      Keys key = _listener.GetPressedKey();
      if (key != Keys.None)
        return new DeviceInput(key.ToString(), key.ToString(), InputType.Button);
      return null;
    }

    public void Dispose()
    {
      if (_listener != null)
      {
        _listener.Dispose();
        _listener = null;
      }
    }
  }
}
