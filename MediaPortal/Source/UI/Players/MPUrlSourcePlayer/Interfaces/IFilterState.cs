using System;
using System.Runtime.InteropServices;

namespace MediaPortal.UI.Players.MPUrlSource.Interfaces
{
    /// <summary>
    /// Specifies interface for splitter state.
    /// </summary>
    [ComImport, Guid("420E98EF-0338-472F-B77B-C5BA8997ED10"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFilterState
    {
        /// <summary>
        /// Tests if filter is ready to connect output pins.
        /// </summary>
        /// <param name="ready">The reference to variable to get filter state.</param>
        /// <returns>0 if successful, error code otherwise</returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int IsFilterReadyToConnectPins([Out, MarshalAs(UnmanagedType.Bool)] out Boolean ready);

        /// <summary>
        /// Gets filter cache file name.
        /// </summary>
        /// <param name="cacheFileName">The reference to variable to get filter cache file name.</param>
        /// <returns>0 if successful, error code otherwise</returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int GetCacheFileName([Out, MarshalAs(UnmanagedType.LPWStr)] out String cacheFileName);
    }
}
