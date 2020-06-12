using Emulators.LibRetro.Controllers.Mapping;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.XInput
{
  class XInputMapper : IDeviceMapper
  {
    #region Available Inputs
    static readonly DeviceInput DPAD_LEFT = new DeviceInput("D-Pad Left", GamepadButtonFlags.DPadLeft.ToString(), InputType.Button);
    static readonly DeviceInput DPAD_RIGHT = new DeviceInput("D-Pad Right", GamepadButtonFlags.DPadRight.ToString(), InputType.Button);
    static readonly DeviceInput DPAD_UP = new DeviceInput("D-Pad Up", GamepadButtonFlags.DPadUp.ToString(), InputType.Button);
    static readonly DeviceInput DPAD_DOWN = new DeviceInput("D-Pad Down", GamepadButtonFlags.DPadDown.ToString(), InputType.Button);
    static readonly DeviceInput BACK = new DeviceInput("Back", GamepadButtonFlags.Back.ToString(), InputType.Button);
    static readonly DeviceInput START = new DeviceInput("Start", GamepadButtonFlags.Start.ToString(), InputType.Button);
    static readonly DeviceInput A = new DeviceInput("A", GamepadButtonFlags.A.ToString(), InputType.Button);
    static readonly DeviceInput B = new DeviceInput("B", GamepadButtonFlags.B.ToString(), InputType.Button);
    static readonly DeviceInput X = new DeviceInput("X", GamepadButtonFlags.X.ToString(), InputType.Button);
    static readonly DeviceInput Y = new DeviceInput("Y", GamepadButtonFlags.Y.ToString(), InputType.Button);
    static readonly DeviceInput LEFT_SHOULDER = new DeviceInput("Left Shoulder", GamepadButtonFlags.LeftShoulder.ToString(), InputType.Button);
    static readonly DeviceInput RIGHT_SHOULDER = new DeviceInput("Right Shoulder", GamepadButtonFlags.RightShoulder.ToString(), InputType.Button);
    static readonly DeviceInput LEFT_THUMB = new DeviceInput("Left Thumb", GamepadButtonFlags.LeftThumb.ToString(), InputType.Button);
    static readonly DeviceInput RIGHT_THUMB = new DeviceInput("Right Thumb", GamepadButtonFlags.RightThumb.ToString(), InputType.Button);

    static readonly DeviceInput LEFT_THUMB_LEFT = new DeviceInput("Left Thumb X-", XInputAxisType.LeftThumbX.ToString(), InputType.Axis, false);
    static readonly DeviceInput LEFT_THUMB_RIGHT = new DeviceInput("Left Thumb X+", XInputAxisType.LeftThumbX.ToString(), InputType.Axis, true);
    static readonly DeviceInput LEFT_THUMB_UP = new DeviceInput("Left Thumb Y+", XInputAxisType.LeftThumbY.ToString(), InputType.Axis, true);
    static readonly DeviceInput LEFT_THUMB_DOWN = new DeviceInput("Left Thumb Y-", XInputAxisType.LeftThumbY.ToString(), InputType.Axis, false);
    static readonly DeviceInput RIGHT_THUMB_LEFT = new DeviceInput("Right Thumb X-", XInputAxisType.RightThumbX.ToString(), InputType.Axis, false);
    static readonly DeviceInput RIGHT_THUMB_RIGHT = new DeviceInput("Right Thumb X+", XInputAxisType.RightThumbX.ToString(), InputType.Axis, true);
    static readonly DeviceInput RIGHT_THUMB_UP = new DeviceInput("Right Thumb Y+", XInputAxisType.RightThumbY.ToString(), InputType.Axis, true);
    static readonly DeviceInput RIGHT_THUMB_DOWN = new DeviceInput("Right Thumb Y-", XInputAxisType.RightThumbY.ToString(), InputType.Axis, false);
    static readonly DeviceInput LEFT_TRIGGER = new DeviceInput("Left Trigger", XInputAxisType.LeftTrigger.ToString(), InputType.Axis, true);
    static readonly DeviceInput RIGHT_TRIGGER = new DeviceInput("Right Trigger", XInputAxisType.RightTrigger.ToString(), InputType.Axis, true);

    protected static readonly DeviceInput[] AVAILABLE_BUTTONS =
    {
      DPAD_LEFT,
      DPAD_RIGHT,
      DPAD_UP,
      DPAD_DOWN,
      BACK,
      START,
      A,
      B,
      X,
      Y,
      LEFT_SHOULDER,
      RIGHT_SHOULDER,
      LEFT_THUMB,
      RIGHT_THUMB
    };

    protected static readonly DeviceInput[] AVAILABLE_AXIS =
    {
      LEFT_THUMB_LEFT,
      LEFT_THUMB_RIGHT,
      LEFT_THUMB_UP,
      LEFT_THUMB_DOWN,
      RIGHT_THUMB_LEFT,
      RIGHT_THUMB_RIGHT,
      RIGHT_THUMB_UP,
      RIGHT_THUMB_DOWN,
      LEFT_TRIGGER,
      RIGHT_TRIGGER
    };
    #endregion

    #region DefaultMapping
    
    public static RetroPadMapping GetDefaultMapping(string subDeviceId, bool mapAnalogToDPad)
    {
      RetroPadMapping mapping = new RetroPadMapping()
      {
        DeviceId = XInputController.DEVICE_ID,
        SubDeviceId = subDeviceId,
        DeviceName = XInputController.DEVICE_NAME
      };
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.LEFT, DPAD_LEFT);
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.RIGHT, DPAD_RIGHT);
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.UP, DPAD_UP);
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.DOWN, DPAD_DOWN);
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.SELECT, BACK);
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.START, START);
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.A, B);
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.B, A);
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.X, Y);
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.Y, X);
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.L, LEFT_SHOULDER);
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.R, RIGHT_SHOULDER);
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.L2, LEFT_TRIGGER);
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.R2, RIGHT_TRIGGER);
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.L3, LEFT_THUMB);
      mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.R3, RIGHT_THUMB);

      mapping.MapAnalog(RetroAnalogDevice.RightThumbLeft, RIGHT_THUMB_LEFT);
      mapping.MapAnalog(RetroAnalogDevice.RightThumbRight, RIGHT_THUMB_RIGHT);
      mapping.MapAnalog(RetroAnalogDevice.RightThumbUp, RIGHT_THUMB_UP);
      mapping.MapAnalog(RetroAnalogDevice.RightThumbDown, RIGHT_THUMB_DOWN);

      if (mapAnalogToDPad)
      {
        mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.LEFT, LEFT_THUMB_LEFT);
        mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.RIGHT, LEFT_THUMB_RIGHT);
        mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.UP, LEFT_THUMB_UP);
        mapping.MapButton(SharpRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.DOWN, LEFT_THUMB_DOWN);
      }
      else
      {
        mapping.MapAnalog(RetroAnalogDevice.LeftThumbLeft, LEFT_THUMB_LEFT);
        mapping.MapAnalog(RetroAnalogDevice.LeftThumbRight, LEFT_THUMB_RIGHT);
        mapping.MapAnalog(RetroAnalogDevice.LeftThumbUp, LEFT_THUMB_UP);
        mapping.MapAnalog(RetroAnalogDevice.LeftThumbDown, LEFT_THUMB_DOWN);
      }

      return mapping;
    }
    #endregion

    protected class AxisDeviceInput
    {
      public DeviceInput DeviceInput { get; set; }
      public XInputAxis Axis { get; set; }
    }
    
    protected Dictionary<GamepadButtonFlags, DeviceInput> _buttonInputs;
    protected List<AxisDeviceInput> _axisInputs;
    protected XInputControllerCache _controller;

    public XInputMapper(XInputControllerCache controller)
    {
      _controller = controller;
      InitializeInputs();
    }

    public bool SupportsDeadZone
    {
      get { return true; }
    }

    public DeviceInput GetPressedInput()
    {
      State state;
      if (!_controller.GetState(out state))
        return null;
      Gamepad gamepad = state.Gamepad;

      DeviceInput pressedInput;
      if (_buttonInputs.TryGetValue(gamepad.Buttons, out pressedInput))
        return pressedInput;

      foreach (AxisDeviceInput axisInput in _axisInputs)
        if (XInputController.IsAxisPressed(axisInput.Axis, gamepad))
          return axisInput.DeviceInput;

      return null;
    }

    protected void InitializeInputs()
    {
      _buttonInputs = new Dictionary<GamepadButtonFlags, DeviceInput>();
      foreach (DeviceInput input in AVAILABLE_BUTTONS)
      {
        GamepadButtonFlags buttonFlag;
        if (Enum.TryParse(input.Id, out buttonFlag))
          _buttonInputs.Add(buttonFlag, input);
      }

      _axisInputs = new List<AxisDeviceInput>();
      foreach (DeviceInput input in AVAILABLE_AXIS)
      {
        XInputAxisType axisType;
        if (Enum.TryParse(input.Id, out axisType))
          _axisInputs.Add(new AxisDeviceInput()
          {
            DeviceInput = input,
            Axis = new XInputAxis(axisType, input.PositiveValues)
          });
      }
    }
  }
}