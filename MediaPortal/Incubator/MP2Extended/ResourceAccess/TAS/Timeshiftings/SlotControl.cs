using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Timeshiftings
{
  internal static class SlotControl
  {
    private static readonly Dictionary<string, int> _slotMap = new Dictionary<string, int>();
    private static int _slotCounter = 2;  // we start at 2 because 0 is the primary Player and 1 the PiP Player

    internal static int GetSlotIndex(string userName)
    {
      if (_slotMap.ContainsKey(userName))
        return _slotMap[userName];

      _slotMap.Add(userName, _slotCounter);
      _slotCounter++;
      return _slotMap[userName];
    }

    internal static void DeleteSlotIndex(string userName)
    {
      if (_slotMap.ContainsKey(userName))
        _slotMap.Remove(userName);
    }
  }
}
