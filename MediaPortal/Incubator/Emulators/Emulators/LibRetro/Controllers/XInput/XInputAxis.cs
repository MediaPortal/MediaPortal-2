using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.XInput
{
  enum XInputAxisType
  {
    LeftThumbX,
    LeftThumbY,
    RightThumbX,
    RightThumbY,
    LeftTrigger,
    RightTrigger
  }

  class XInputAxis
  {
    public XInputAxis(XInputAxisType axisType, bool positiveValues)
    {
      AxisType = axisType;
      PositiveValues = positiveValues;
    }

    public XInputAxisType AxisType { get; set; }
    public bool PositiveValues { get; set; }
  }
}
