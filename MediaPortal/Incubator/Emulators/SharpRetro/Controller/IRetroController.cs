using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.Controller
{
  public interface IRetroController
  {
  }

  public interface IRetroPad : IRetroController
  {
    bool IsButtonPressed(uint port, RETRO_DEVICE_ID_JOYPAD button);
  }

  public interface IRetroAnalog : IRetroController
  {
    short GetAnalog(uint port, RETRO_DEVICE_INDEX_ANALOG index, RETRO_DEVICE_ID_ANALOG direction);
  }

  public interface IRetroKeyboard : IRetroController
  {
    bool IsKeyPressed(RETRO_KEY key);
  }

  public interface IRetroPointer : IRetroController
  {
    short GetPointerX();
    short GetPointerY();
    bool IsPointerPressed();
  }

  public interface IRetroRumble
  {
    bool SetRumbleState(uint port, retro_rumble_effect effect, ushort strength);
  }
}
