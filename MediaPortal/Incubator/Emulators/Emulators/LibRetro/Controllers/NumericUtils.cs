using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers
{
  static class NumericUtils
  {
    public static short UShortToShort(ushort us)
    {
      return (short)(us ^ 0x8000);
    }

    public static short UIntToShort(uint ui)
    {
      return (short)(ui ^ 0x8000);
    }

    public static short ScaleByteToShort(byte b)
    {
      if (b == 0)
        return 0;
      return (short)(((b << 8) | b) >> 1);
    }
  }
}
