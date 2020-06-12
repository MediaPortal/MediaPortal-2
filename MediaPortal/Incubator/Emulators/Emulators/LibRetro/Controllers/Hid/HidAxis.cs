using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.Hid
{
  class HidAxis
  {
    public HidAxis(ushort axis, bool positiveValues)
    {
      Axis = axis;
      PositiveValues = positiveValues;
    }

    public ushort Axis { get; set; }
    public bool PositiveValues { get; set; }
  }
}
