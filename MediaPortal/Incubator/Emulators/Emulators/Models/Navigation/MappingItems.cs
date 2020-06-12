using Emulators.LibRetro.Controllers.Mapping;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.SkinBase.General;
using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Models.Navigation
{
  public class PortMappingItem : ListItem
  {
    public const string KEY_MAPPED_DEVICE = "MappedDevice";
    protected PortMapping _portMapping;

    public PortMapping PortMapping
    {
      get { return _portMapping; }
    }

    public PortMappingItem(string name, PortMapping portMapping)
      : base(Consts.KEY_NAME, name)
    {
      _portMapping = portMapping;
      SetLabel(KEY_MAPPED_DEVICE, portMapping.DeviceName);
    }
  }

  public class MappableDeviceItem : ListItem
  {
    protected IMappableDevice _device;

    public IMappableDevice Device
    {
      get { return _device; }
    }

    public MappableDeviceItem(string name, IMappableDevice device)
      : base(Consts.KEY_NAME, name)
    {
      _device = device;
    }
  }

  public class MappedInputItem : ListItem
  {
    public const string KEY_MAPPED_INPUT = "MappedInput";
    protected MappedInput _mappedInput;

    public MappedInput MappedInput
    {
      get { return _mappedInput; }
    }

    public MappedInputItem(string name, MappedInput mappedInput)
      : base(Consts.KEY_NAME, name)
    {
      _mappedInput = mappedInput;
      Update();
    }

    public void Update()
    {
      string label = _mappedInput.Input != null ? _mappedInput.Input.Label : "";
      SetLabel(KEY_MAPPED_INPUT, label);
    }
  }
}
