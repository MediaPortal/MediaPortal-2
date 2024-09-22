using System;
using System.Runtime.InteropServices;

namespace MediaPortal.UI.Players.MPUrlSource.Interfaces
{
    /// <summary>
    /// Specifies interface for distinguishing MediaPortal IPTV filter and url source splitter from previous MediaPortal Url Source Splitter.
    /// </summary>
    [ComImport, Guid("505C28D8-01F4-41C7-BD51-013FA6DBBD39"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFilterStateEx : IFilterState
    {
        #region IFilterState interface

        /// <summary>
        /// Tests if filter is ready to connect output pins.
        /// </summary>
        /// <param name="ready">The reference to variable to get filter state.</param>
        /// <returns>0 if successful, error code otherwise</returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        new int IsFilterReadyToConnectPins([Out, MarshalAs(UnmanagedType.Bool)] out Boolean ready);


        /// <summary>
        /// Gets filter cache file name.
        /// </summary>
        /// <param name="cacheFileName">The reference to variable to get filter cache file name.</param>
        /// <returns>0 if successful, error code otherwise</returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        new int GetCacheFileName([Out, MarshalAs(UnmanagedType.LPWStr)] out String cacheFileName);

        #endregion

        /// <summary>
        /// Gets filter version.
        /// </summary>
        /// <param name="version">The reference to variable to get filter version.</param>
        /// <returns>0 if successful, error code otherwise</returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int GetVersion([Out, MarshalAs(UnmanagedType.U4)] out uint version);

        /// <summary>
        /// Tests if error code if filter error code.
        /// </summary>
        /// <param name="isFilterError">The reference to variable to test.</param>
        /// <param name="error">The error code to test.</param>
        /// <returns>0 if successful, error code otherwise</returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int IsFilterError([Out, MarshalAs(UnmanagedType.Bool)] out Boolean isFilterError, [In, MarshalAs(UnmanagedType.I4)] int error);

        /// <summary>
        /// Gets description of filter error.
        /// </summary>
        /// <param name="error">The filter error code to get error description.</param>
        /// <param name="description">The reference to variable to get filter error description.</param>
        /// <returns>0 if successful, error code otherwise</returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int GetErrorDescription([In, MarshalAs(UnmanagedType.I4)] int error, [Out, MarshalAs(UnmanagedType.LPWStr)] out String description);

        /// <summary>
        /// Loads stream into file.
        /// </summary>
        /// <param name="url">The formatted URL to load stream.</param>
        /// <returns>0 if successful, 1 if pending, error code otherwise</returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int LoadAsync([In, MarshalAs(UnmanagedType.LPWStr)] String url);

        /// <summary>
        /// Tests if stream is opened.
        /// </summary>
        /// <param name="opened">The reference to variable to get filter stream state.</param>
        /// <returns>0 if successful, error code otherwise</returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int IsStreamOpened([Out, MarshalAs(UnmanagedType.Bool)] out Boolean opened);
    }
}
