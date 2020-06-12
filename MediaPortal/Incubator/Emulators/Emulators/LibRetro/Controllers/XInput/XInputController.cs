using SharpRetro.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.XInput;
using SharpRetro.LibRetro;
using Emulators.LibRetro.Controllers.Mapping;
using System.Globalization;

namespace Emulators.LibRetro.Controllers.XInput
{
  class XInputController : IRetroPad, IRetroAnalog, IRetroRumble, IMappableDevice
  {
    public static readonly Guid DEVICE_ID = new Guid("FD60533F-00E0-4301-8B97-F9681D6FC67D");
    public const string DEVICE_NAME = "Xbox 360 Controller (XInput)";
    const int CONTROLLER_CONNECTED_TIMEOUT = 2000;
    protected static short DEFAULT_THUMB_DEADZONE = Gamepad.LeftThumbDeadZone;
    protected static short DEFAULT_TRIGGER_DEADZONE = NumericUtils.ScaleByteToShort(Gamepad.TriggerThreshold);

    protected string _subDeviceId;
    protected string _deviceName;
    protected int _controllerIndex;
    protected XInputControllerCache _controller;
    protected Dictionary<RETRO_DEVICE_ID_JOYPAD, GamepadButtonFlags> _buttonToButtonMappings;
    protected Dictionary<RETRO_DEVICE_ID_JOYPAD, XInputAxis> _analogToButtonMappings;
    protected Dictionary<RetroAnalogDevice, XInputAxis> _analogToAnalogMappings;
    protected Dictionary<RetroAnalogDevice, GamepadButtonFlags> _buttonToAnalogMappings;
    protected ushort _leftMotorSpeed;
    protected ushort _rightMotorSpeed;
    protected short _thumbDeadZone;
    protected short _triggerDeadZone;

    public Guid DeviceId
    {
      get { return DEVICE_ID; }
    }

    public string DeviceName
    {
      get { return _deviceName; }
    }

    public string SubDeviceId
    {
      get { return _subDeviceId; }
    }

    public RetroPadMapping DefaultMapping
    {
      get { return XInputMapper.GetDefaultMapping(_subDeviceId, false); }
    }

    public XInputController(UserIndex userIndex)
    {
      _buttonToButtonMappings = new Dictionary<RETRO_DEVICE_ID_JOYPAD, GamepadButtonFlags>();
      _analogToButtonMappings = new Dictionary<RETRO_DEVICE_ID_JOYPAD, XInputAxis>();
      _analogToAnalogMappings = new Dictionary<RetroAnalogDevice, XInputAxis>();
      _buttonToAnalogMappings = new Dictionary<RetroAnalogDevice, GamepadButtonFlags>();
      InitController(userIndex);
    }

    protected void InitController(UserIndex userIndex)
    {
      _controllerIndex = (int)userIndex;
      _subDeviceId = _controllerIndex.ToString(CultureInfo.InvariantCulture);
      _deviceName = string.Format("{0} {1}", DEVICE_NAME, _subDeviceId);
      if (_controllerIndex > 3)
        _controllerIndex = 0;
      _controller = new XInputControllerCache(new Controller(userIndex), CONTROLLER_CONNECTED_TIMEOUT);
    }

    public IDeviceMapper CreateMapper()
    {
      return new XInputMapper(_controller);
    }

    public void Map(RetroPadMapping mapping)
    {
      if (mapping == null)
        mapping = DefaultMapping;
      ClearMappings();

      int percent = mapping.DeadZone;
      if (percent < 0)
      {
        _thumbDeadZone = DEFAULT_THUMB_DEADZONE;
        _triggerDeadZone = DEFAULT_TRIGGER_DEADZONE;
      }
      else
      {
        short deadZone = percent < 100 ? (short)(short.MaxValue * 100 / percent) : short.MaxValue;
        _thumbDeadZone = deadZone;
        _triggerDeadZone = deadZone;
      }

      foreach (var kvp in mapping.ButtonMappings)
      {
        DeviceInput deviceInput = kvp.Value;
        if (deviceInput.InputType == InputType.Button)
        {
          GamepadButtonFlags button;
          if (Enum.TryParse(deviceInput.Id, out button))
            _buttonToButtonMappings[kvp.Key] = button;
        }
        else if (deviceInput.InputType == InputType.Axis)
        {
          XInputAxisType analogInput;
          if (Enum.TryParse(deviceInput.Id, out analogInput))
            _analogToButtonMappings[kvp.Key] = new XInputAxis(analogInput, deviceInput.PositiveValues);
        }
      }

      foreach (var kvp in mapping.AnalogMappings)
      {
        DeviceInput deviceInput = kvp.Value;
        if (deviceInput.InputType == InputType.Button)
        {
          GamepadButtonFlags button;
          if (Enum.TryParse(deviceInput.Id, out button))
            _buttonToAnalogMappings[kvp.Key] = button;
        }
        else if (deviceInput.InputType == InputType.Axis)
        {
          XInputAxisType analogInput;
          if (Enum.TryParse(deviceInput.Id, out analogInput))
            _analogToAnalogMappings[kvp.Key] = new XInputAxis(analogInput, deviceInput.PositiveValues);
        }
      }
    }

    protected void ClearMappings()
    {
      _buttonToButtonMappings.Clear();
      _analogToButtonMappings.Clear();
      _analogToAnalogMappings.Clear();
      _buttonToAnalogMappings.Clear();
    }

    public bool IsConnected()
    {
      return _controller.IsConnected();
    }

    public bool IsButtonPressed(uint port, RETRO_DEVICE_ID_JOYPAD button)
    {
      if (port != _controllerIndex)
        return false;
      Gamepad gamepad;
      if (!TryGetGamepad(out gamepad))
        return false;

      GamepadButtonFlags buttonFlag;
      if (_buttonToButtonMappings.TryGetValue(button, out buttonFlag))
        return IsButtonPressed(buttonFlag, gamepad);
      XInputAxis axis;
      if (_analogToButtonMappings.TryGetValue(button, out axis))
        return IsAxisPressed(axis, gamepad, _thumbDeadZone, _triggerDeadZone);
      return false;
    }

    public short GetAnalog(uint port, RETRO_DEVICE_INDEX_ANALOG index, RETRO_DEVICE_ID_ANALOG direction)
    {
      if (port != _controllerIndex)
        return 0;
      Gamepad gamepad;
      if (!TryGetGamepad(out gamepad))
        return 0;

      RetroAnalogDevice positive;
      RetroAnalogDevice negative;
      RetroPadMapping.GetAnalogEnums(index, direction, out positive, out negative);
      short positivePosition = 0;
      short negativePosition = 0;

      XInputAxis axis;
      GamepadButtonFlags buttonFlag;
      if (_analogToAnalogMappings.TryGetValue(positive, out axis))
        positivePosition = GetAxisPositionMapped(axis, gamepad, true);
      else if (_buttonToAnalogMappings.TryGetValue(positive, out buttonFlag) && IsButtonPressed(buttonFlag, gamepad))
        positivePosition = short.MaxValue;

      if (_analogToAnalogMappings.TryGetValue(negative, out axis))
        negativePosition = GetAxisPositionMapped(axis, gamepad, false);
      else if (_buttonToAnalogMappings.TryGetValue(negative, out buttonFlag) && IsButtonPressed(buttonFlag, gamepad))
        negativePosition = short.MinValue;

      if (positivePosition != 0 && negativePosition == 0)
        return positivePosition;
      if (positivePosition == 0 && negativePosition != 0)
        return negativePosition;
      return 0;
    }

    public bool SetRumbleState(uint port, retro_rumble_effect effect, ushort strength)
    {
      if (port != _controllerIndex)
        return false;

      //Consider the low frequency (left) motor the "strong" one
      if (effect == retro_rumble_effect.RETRO_RUMBLE_STRONG)
        _leftMotorSpeed = strength;
      else if (effect == retro_rumble_effect.RETRO_RUMBLE_WEAK)
        _rightMotorSpeed = strength;

      if (!_controller.IsConnected())
        return false;

      _controller.Controller.SetVibration(new Vibration()
      {
        LeftMotorSpeed = _leftMotorSpeed,
        RightMotorSpeed = _rightMotorSpeed
      });
      return true;
    }

    protected bool TryGetGamepad(out Gamepad gamepad)
    {
      State state;
      if (_controller.GetState(out state))
      {
        gamepad = state.Gamepad;
        return true;
      }
      gamepad = default(Gamepad);
      return false;
    }

    public static bool IsButtonPressed(GamepadButtonFlags buttonFlag, Gamepad gamepad)
    {
      return (gamepad.Buttons & buttonFlag) == buttonFlag;
    }

    public static bool IsAxisPressed(XInputAxis axis, Gamepad gamepad)
    {
      return IsAxisPressed(axis, gamepad, DEFAULT_THUMB_DEADZONE, DEFAULT_TRIGGER_DEADZONE);
    }

    protected static bool IsAxisPressed(XInputAxis axis, Gamepad gamepad, short thumbDeadZone, short triggerDeadZone)
    {
      short position = GetAxisPosition(axis, gamepad, thumbDeadZone, triggerDeadZone);
      return axis.PositiveValues ? position > 0 : position < 0;
    }

    protected static short GetAxisPosition(XInputAxis axis, Gamepad gamepad, short thumbDeadZone, short triggerDeadZone)
    {
      switch (axis.AxisType)
      {
        case XInputAxisType.LeftThumbX:
          return CheckDeadZone(gamepad.LeftThumbX, thumbDeadZone);
        case XInputAxisType.LeftThumbY:
          return CheckDeadZone(gamepad.LeftThumbY, thumbDeadZone);
        case XInputAxisType.RightThumbX:
          return CheckDeadZone(gamepad.RightThumbX, thumbDeadZone);
        case XInputAxisType.RightThumbY:
          return CheckDeadZone(gamepad.RightThumbY, thumbDeadZone);
        case XInputAxisType.LeftTrigger:
          return CheckDeadZone(NumericUtils.ScaleByteToShort(gamepad.LeftTrigger), triggerDeadZone);
        case XInputAxisType.RightTrigger:
          return CheckDeadZone(NumericUtils.ScaleByteToShort(gamepad.RightTrigger), triggerDeadZone);
      }
      return 0;
    }

    protected short GetAxisPositionMapped(XInputAxis axis, Gamepad gamepad, bool isMappedToPositive)
    {
      short position = GetAxisPosition(axis, gamepad, _thumbDeadZone, _triggerDeadZone);
      if (position == 0 || (axis.PositiveValues && position <= 0) || (!axis.PositiveValues && position >= 0))
        return 0;

      bool shouldInvert = (axis.PositiveValues && !isMappedToPositive) || (!axis.PositiveValues && isMappedToPositive);
      if (shouldInvert)
        position = (short)(-position - 1);
      return position;
    }

    protected static short CheckDeadZone(short value, short deadZone)
    {
      if (value > deadZone || value < -deadZone)
        return value;
      return 0;
    }
  }
}