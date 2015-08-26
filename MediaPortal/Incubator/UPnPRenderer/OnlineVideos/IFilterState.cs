// Type: OnlineVideos.MPUrlSourceFilter.IFilterState
// Assembly: OnlineVideos, Version=1.10.0.1, Culture=neutral, PublicKeyToken=null
// MVID: 8F27759C-CFBE-47CB-A39F-F16055EE5D07
// Assembly location: M:\Programmieren\C#\MediaPortal 2\MediaPortal\Incubator\UPnPRenderer\references\OnlineVideos.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OnlineVideos.MPUrlSourceFilter
{
  [Guid("420E98EF-0338-472F-B77B-C5BA8997ED10")]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [ComImport]
  public interface IFilterState
  {
    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int IsFilterReadyToConnectPins([MarshalAs(UnmanagedType.Bool)] out bool ready);

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetCacheFileName([MarshalAs(UnmanagedType.LPWStr)] out string cacheFileName);
  }
}
