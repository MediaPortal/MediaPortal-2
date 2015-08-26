// Type: OnlineVideos.MPUrlSourceFilter.IFilterStateEx
// Assembly: OnlineVideos, Version=1.10.0.1, Culture=neutral, PublicKeyToken=null
// MVID: 8F27759C-CFBE-47CB-A39F-F16055EE5D07
// Assembly location: M:\Programmieren\C#\MediaPortal 2\MediaPortal\Incubator\UPnPRenderer\references\OnlineVideos.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OnlineVideos.MPUrlSourceFilter
{
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [Guid("505C28D8-01F4-41C7-BD51-013FA6DBBD39")]
  [ComImport]
  public interface IFilterStateEx : IFilterState
  {
    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int IsFilterReadyToConnectPins([MarshalAs(UnmanagedType.Bool)] out bool ready);

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetCacheFileName([MarshalAs(UnmanagedType.LPWStr)] out string cacheFileName);

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetVersion([MarshalAs(UnmanagedType.U4)] out uint version);

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int IsFilterError([MarshalAs(UnmanagedType.Bool)] out bool isFilterError, [MarshalAs(UnmanagedType.I4), In] int error);

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetErrorDescription([MarshalAs(UnmanagedType.I4), In] int error, [MarshalAs(UnmanagedType.LPWStr)] out string description);

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int LoadAsync([MarshalAs(UnmanagedType.LPWStr), In] string url);

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [return: MarshalAs(UnmanagedType.I4)]
    int IsStreamOpened([MarshalAs(UnmanagedType.Bool)] out bool opened);
  }
}
