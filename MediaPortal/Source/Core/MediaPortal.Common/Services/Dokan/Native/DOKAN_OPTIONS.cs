using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Common.Services.Dokan.Native
{
  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
  internal struct DOKAN_OPTIONS
  {
    public ushort Version;
    public ushort ThreadCount; // number of threads to be used
    public uint Options;
    public ulong GlobalContext;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string MountPoint;
    public uint Timeout;
  }
}
