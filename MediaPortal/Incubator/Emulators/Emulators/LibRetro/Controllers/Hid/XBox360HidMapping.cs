using Emulators.LibRetro.Controllers.Mapping;
using SharpLib.Hid;
using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.Hid
{
  public static class XBox360HidMapping
  {
    const string DEVICE_NAME = "XBox 360 Hid";
    public static readonly RetroPadMapping DefaultMapping;

    static XBox360HidMapping()
    {
      RetroPadMapping mapping = new RetroPadMapping() { DeviceId = HidGameControl.DEVICE_ID, DeviceName = DEVICE_NAME };
      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.LEFT, new DeviceInput("Left", DirectionPadState.Left.ToString(), InputType.Button));
      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.RIGHT, new DeviceInput("Right", DirectionPadState.Right.ToString(), InputType.Button));
      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.UP, new DeviceInput("Up", DirectionPadState.Up.ToString(), InputType.Button));
      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.DOWN, new DeviceInput("Down", DirectionPadState.Down.ToString(), InputType.Button));

      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.A, new DeviceInput("2", "2", InputType.Button));
      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.B, new DeviceInput("1", "1", InputType.Button));
      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.X, new DeviceInput("4", "4", InputType.Button));
      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.Y, new DeviceInput("3", "3", InputType.Button));
      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.SELECT, new DeviceInput("7", "7", InputType.Button));
      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.START, new DeviceInput("8", "8", InputType.Button));
      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.L, new DeviceInput("5", "5", InputType.Button));
      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.R, new DeviceInput("6", "6", InputType.Button));
      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.L3, new DeviceInput("9", "9", InputType.Button));
      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.R3, new DeviceInput("10", "10", InputType.Button));

      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.L2, new DeviceInput("Z", "50", InputType.Axis, true));
      mapping.MapButton(RETRO_DEVICE_ID_JOYPAD.R2, new DeviceInput("Z", "50", InputType.Axis, false));

      mapping.MapAnalog(RetroAnalogDevice.LeftThumbLeft, new DeviceInput("X", "48", InputType.Axis, false));
      mapping.MapAnalog(RetroAnalogDevice.LeftThumbRight, new DeviceInput("X", "48", InputType.Axis, true));
      mapping.MapAnalog(RetroAnalogDevice.LeftThumbUp, new DeviceInput("Y", "49", InputType.Axis, false));
      mapping.MapAnalog(RetroAnalogDevice.LeftThumbDown, new DeviceInput("Y", "49", InputType.Axis, true));

      mapping.MapAnalog(RetroAnalogDevice.RightThumbLeft, new DeviceInput("Rx", "51", InputType.Axis, false));
      mapping.MapAnalog(RetroAnalogDevice.RightThumbRight, new DeviceInput("Rx", "51", InputType.Axis, true));
      mapping.MapAnalog(RetroAnalogDevice.RightThumbUp, new DeviceInput("Ry", "52", InputType.Axis, false));
      mapping.MapAnalog(RetroAnalogDevice.RightThumbDown, new DeviceInput("Ry", "52", InputType.Axis, true));

      DefaultMapping = mapping;
    }
  }
}