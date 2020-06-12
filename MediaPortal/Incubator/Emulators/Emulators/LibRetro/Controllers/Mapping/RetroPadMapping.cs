using Emulators.Settings;
using MediaPortal.Utilities.Xml;
using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Emulators.LibRetro.Controllers.Mapping
{
  public enum RetroAnalogDevice
  {
    LeftThumbLeft,
    LeftThumbRight,
    LeftThumbUp,
    LeftThumbDown,
    RightThumbLeft,
    RightThumbRight,
    RightThumbUp,
    RightThumbDown,
  }

  public class MappedInput
  {
    public string Name { get; set; }
    public RETRO_DEVICE_ID_JOYPAD? Button { get; set; }
    public RetroAnalogDevice? Analog { get; set; }
    public DeviceInput Input { get; set; }
  }

  public class RetroPadMapping
  {
    protected List<MappedInput> _availableInputs;
    protected SerializableDictionary<RETRO_DEVICE_ID_JOYPAD, DeviceInput> _buttonMappings;
    protected SerializableDictionary<RetroAnalogDevice, DeviceInput> _analogMappings;
    protected int _deadZone = -1;

    public RetroPadMapping()
    {
      _availableInputs = GetAvailableInputs();
      _buttonMappings = new SerializableDictionary<RETRO_DEVICE_ID_JOYPAD, DeviceInput>();
      _analogMappings = new SerializableDictionary<RetroAnalogDevice, DeviceInput>();
    }
    
    public Guid DeviceId { get; set; }
    public string SubDeviceId { get; set; }
    public string DeviceName { get; set; }
    
    [XmlIgnore]
    public List<MappedInput> AvailableInputs
    {
      get { return _availableInputs; }
    }
    
    public SerializableDictionary<RETRO_DEVICE_ID_JOYPAD, DeviceInput> ButtonMappings
    {
      get { return _buttonMappings; }
      set { _buttonMappings = value; }
    }
        
    public SerializableDictionary<RetroAnalogDevice, DeviceInput> AnalogMappings
    {
      get { return _analogMappings; }
      set { _analogMappings = value; }
    }

    public int DeadZone
    {
      get { return _deadZone; }
      set { _deadZone = value; }
    }

    public bool TryGetMapping(MappedInput mappedInput, out DeviceInput deviceInput)
    {
      if ((mappedInput.Button.HasValue && _buttonMappings.TryGetValue(mappedInput.Button.Value, out deviceInput))
            || (mappedInput.Analog.HasValue && _analogMappings.TryGetValue(mappedInput.Analog.Value, out deviceInput)))
        return true;

      deviceInput = null;
      return false;
    }

    public void Map(MappedInput mappedInput)
    {
      if (mappedInput.Button.HasValue)
        MapButton(mappedInput.Button.Value, mappedInput.Input);
      else if (mappedInput.Analog.HasValue)
        MapAnalog(mappedInput.Analog.Value, mappedInput.Input);
    }

    public void MapButton(RETRO_DEVICE_ID_JOYPAD retroButton, DeviceInput deviceInput)
    {
      _buttonMappings[retroButton] = deviceInput;
    }

    public void MapAnalog(RetroAnalogDevice retroAnalog, DeviceInput deviceInput)
    {
      _analogMappings[retroAnalog] = deviceInput;
    }

    public List<MappedInput> GetAvailableInputs()
    {
      List<MappedInput> inputList = new List<MappedInput>();
      inputList.Add(new MappedInput() { Name = "Up", Button = RETRO_DEVICE_ID_JOYPAD.UP });
      inputList.Add(new MappedInput() { Name = "Down", Button = RETRO_DEVICE_ID_JOYPAD.DOWN });
      inputList.Add(new MappedInput() { Name = "Left", Button = RETRO_DEVICE_ID_JOYPAD.LEFT });
      inputList.Add(new MappedInput() { Name = "Right", Button = RETRO_DEVICE_ID_JOYPAD.RIGHT });

      inputList.Add(new MappedInput() { Name = "Left Analog X- (Left)", Analog = RetroAnalogDevice.LeftThumbLeft });
      inputList.Add(new MappedInput() { Name = "Left Analog X+ (Right)", Analog = RetroAnalogDevice.LeftThumbRight });
      inputList.Add(new MappedInput() { Name = "Left Analog Y- (Up)", Analog = RetroAnalogDevice.LeftThumbUp });
      inputList.Add(new MappedInput() { Name = "Left Analog Y+ (Down)", Analog = RetroAnalogDevice.LeftThumbDown });
      inputList.Add(new MappedInput() { Name = "Right Analog X- (Left)", Analog = RetroAnalogDevice.RightThumbLeft });
      inputList.Add(new MappedInput() { Name = "Right Analog X+ (Right)", Analog = RetroAnalogDevice.RightThumbRight });
      inputList.Add(new MappedInput() { Name = "Right Analog Y- (Up)", Analog = RetroAnalogDevice.RightThumbUp });
      inputList.Add(new MappedInput() { Name = "Right Analog Y+ (Down)", Analog = RetroAnalogDevice.RightThumbDown });

      inputList.Add(new MappedInput() { Name = "A", Button = RETRO_DEVICE_ID_JOYPAD.A });
      inputList.Add(new MappedInput() { Name = "B", Button = RETRO_DEVICE_ID_JOYPAD.B });
      inputList.Add(new MappedInput() { Name = "X", Button = RETRO_DEVICE_ID_JOYPAD.X });
      inputList.Add(new MappedInput() { Name = "Y", Button = RETRO_DEVICE_ID_JOYPAD.Y });
      inputList.Add(new MappedInput() { Name = "Start", Button = RETRO_DEVICE_ID_JOYPAD.START });
      inputList.Add(new MappedInput() { Name = "Select", Button = RETRO_DEVICE_ID_JOYPAD.SELECT });
      inputList.Add(new MappedInput() { Name = "L1", Button = RETRO_DEVICE_ID_JOYPAD.L });
      inputList.Add(new MappedInput() { Name = "R1", Button = RETRO_DEVICE_ID_JOYPAD.R });
      inputList.Add(new MappedInput() { Name = "L2", Button = RETRO_DEVICE_ID_JOYPAD.L2 });
      inputList.Add(new MappedInput() { Name = "R2", Button = RETRO_DEVICE_ID_JOYPAD.R2 });
      inputList.Add(new MappedInput() { Name = "L3", Button = RETRO_DEVICE_ID_JOYPAD.L3 });
      inputList.Add(new MappedInput() { Name = "R3", Button = RETRO_DEVICE_ID_JOYPAD.R3 });
      return inputList;
    }

    public static void GetAnalogEnums(RETRO_DEVICE_INDEX_ANALOG index, RETRO_DEVICE_ID_ANALOG direction, out RetroAnalogDevice positive, out RetroAnalogDevice negative)
    {
      if (index == RETRO_DEVICE_INDEX_ANALOG.LEFT)
      {
        if (direction == RETRO_DEVICE_ID_ANALOG.X)
        {
          positive = RetroAnalogDevice.LeftThumbRight;
          negative = RetroAnalogDevice.LeftThumbLeft;
        }
        else
        {
          //Libretro defines positive Y values as down
          positive = RetroAnalogDevice.LeftThumbDown;
          negative = RetroAnalogDevice.LeftThumbUp;
        }
      }
      else
      {
        if (direction == RETRO_DEVICE_ID_ANALOG.X)
        {
          positive = RetroAnalogDevice.RightThumbRight;
          negative = RetroAnalogDevice.RightThumbLeft;
        }
        else
        {
          //Libretro defines positive Y values as down
          positive = RetroAnalogDevice.RightThumbDown;
          negative = RetroAnalogDevice.RightThumbUp;
        }
      }
    }
  }
}
