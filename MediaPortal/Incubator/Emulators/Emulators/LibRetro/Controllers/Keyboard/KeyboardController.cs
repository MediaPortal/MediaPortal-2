using SharpRetro.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpRetro.LibRetro;
using Emulators.LibRetro.Controllers.Mapping;

namespace Emulators.LibRetro.Controllers.Keyboard
{
  class KeyboardController : IRetroPad, IRetroAnalog, IMappableDevice
  {
    public static readonly Guid DEVICE_ID = new Guid("7910C3E3-F3D2-405F-B09B-8C73EEFB6C70");
    
    protected Dictionary<RETRO_DEVICE_ID_JOYPAD, Keys> _buttonMappings;
    protected Dictionary<RetroAnalogDevice, Keys> _analogMappings;

    public Guid DeviceId
    {
      get { return DEVICE_ID; }
    }

    public string SubDeviceId
    {
      get { return ""; }
    }

    public string DeviceName
    {
      get { return "Keyboard"; }
    }

    public RetroPadMapping DefaultMapping
    {
      get { return null; }
    }

    public KeyboardController()
    {
      _buttonMappings = new Dictionary<RETRO_DEVICE_ID_JOYPAD, Keys>();
      _analogMappings = new Dictionary<RetroAnalogDevice, Keys>();
    }

    public bool IsButtonPressed(uint port, RETRO_DEVICE_ID_JOYPAD button)
    {
      Keys key;
      return _buttonMappings.TryGetValue(button, out key) && Keyboard.IsKeyDown(key);
    }

    public short GetAnalog(uint port, RETRO_DEVICE_INDEX_ANALOG index, RETRO_DEVICE_ID_ANALOG direction)
    {
      RetroAnalogDevice positive;
      RetroAnalogDevice negative;
      RetroPadMapping.GetAnalogEnums(index, direction, out positive, out negative);

      short positivePosition = 0;
      short negativePosition = 0;

      Keys key;
      if (_analogMappings.TryGetValue(positive, out key) && Keyboard.IsKeyDown(key))
        positivePosition = short.MaxValue;
      if (_analogMappings.TryGetValue(negative, out key) && Keyboard.IsKeyDown(key))
        negativePosition = short.MinValue;

      if (positivePosition != 0 && negativePosition == 0)
        return positivePosition;
      if (positivePosition == 0 && negativePosition != 0)
        return negativePosition;
      return 0;
    }

    public IDeviceMapper CreateMapper()
    {
      return new KeyboardMapper();
    }

    public void Map(RetroPadMapping mapping)
    {
      foreach (var kvp in mapping.ButtonMappings)
      {
        Keys key;
        if (Enum.TryParse(kvp.Value.Id, out key))
          _buttonMappings[kvp.Key] = key;
      }

      foreach (var kvp in mapping.AnalogMappings)
      {
        Keys key;
        if (Enum.TryParse(kvp.Value.Id, out key))
          _analogMappings[kvp.Key] = key;
      }
    }
  }
}
