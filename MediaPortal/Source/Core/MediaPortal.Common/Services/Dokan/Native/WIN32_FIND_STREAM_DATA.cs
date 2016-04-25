using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Common.Services.Dokan.Native
{
  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
  internal struct WIN32_FIND_STREAM_DATA
  {
    public long StreamSize;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string cStreamName;
  }
}
