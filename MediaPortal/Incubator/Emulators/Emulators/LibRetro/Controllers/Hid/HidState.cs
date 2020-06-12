using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.Hid
{
  public class HidState
  {    
    public ushort VendorId { get; set; }
    public ushort ProductId { get; set; }
    public string Name { get; set; }
    public string FriendlyName { get; set; }
    public HashSet<ushort> Buttons { get; set; }
    public Dictionary<ushort, HidAxisState> AxisStates { get; set; }
    public SharpLib.Hid.DirectionPadState DirectionPadState { get; set; }
  }

  public class HidAxisState
  {
    public HidAxisState(string name, ushort index, uint value, ushort bitSize)
    {
      Name = name;
      Index = index;
      Value = value;
      BitSize = bitSize;
    }
    public string Name { get; private set; }
    public ushort Index { get; private set; }
    public uint Value { get; private set; }
    public ushort BitSize { get; private set; }
  }
}
