using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.Mapping
{
  public enum InputType
  {
    Axis,
    Button
  }

  public class DeviceInput
  {
    public DeviceInput() { }

    public DeviceInput(string label, string id, InputType inputType, bool positiveValues = true)
    {
      Label = label;
      Id = id;
      InputType = inputType;
      PositiveValues = positiveValues;
    }

    public string Label { get; set; }
    public string Id { get; set; }
    public InputType InputType { get; set; }
    public bool PositiveValues { get; set; }
  }
}
