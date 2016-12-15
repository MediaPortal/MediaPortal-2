#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MediaPortal.Utilities.Network
{
  /// <summary>
  /// Enumerator for shares of a given server.
  /// </summary>
  public class SharesEnumerator
  {
    #region Platform

    /// <summary>
    /// Is this an NT platform?
    /// </summary>
    protected static bool IsNT
    {
      get { return (PlatformID.Win32NT == Environment.OSVersion.Platform); }
    }

    /// <summary>
    /// Returns true if this is Windows 2000 or higher
    /// </summary>
    protected static bool IsW2KUp
    {
      get
      {
        OperatingSystem os = Environment.OSVersion;
        return PlatformID.Win32NT == os.Platform && os.Version.Major >= 5;
      }
    }

    #endregion

    #region Constants

    /// <summary>Maximum path length</summary>
    protected const int MAX_PATH = 260;
    /// <summary>No error</summary>
    protected const int NO_ERROR = 0;
    /// <summary>Access denied</summary>
    protected const int ERROR_ACCESS_DENIED = 5;
    /// <summary>Access denied</summary>
    protected const int ERROR_WRONG_LEVEL = 124;
    /// <summary>More data available</summary>
    protected const int ERROR_MORE_DATA = 234;
    /// <summary>Not connected</summary>
    protected const int ERROR_NOT_CONNECTED = 2250;
    /// <summary>Level 1</summary>
    protected const int UNIVERSAL_NAME_INFO_LEVEL = 1;
    /// <summary>Max extries (9x)</summary>
    protected const int MAX_SI50_ENTRIES = 20;

    #endregion

    #region Structures

    /// <summary>Unc name</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    protected struct UniversalNameInfo
    {
      [MarshalAs(UnmanagedType.LPTStr)]
      public string lpUniversalName;
    }

    /// <summary>Share information, NT, level 2</summary>
    /// <remarks>
    /// Requires admin rights to work. 
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    protected struct ShareInfo2
    {
      [MarshalAs(UnmanagedType.LPWStr)]
      public string NetName;
      public ShareType ShareType;
      [MarshalAs(UnmanagedType.LPWStr)]
      public string Remark;
      public int Permissions;
      public int MaxUsers;
      public int CurrentUsers;
      [MarshalAs(UnmanagedType.LPWStr)]
      public string Path;
      [MarshalAs(UnmanagedType.LPWStr)]
      public string Password;
    }

    /// <summary>Share information, NT, level 1</summary>
    /// <remarks>
    /// Fallback when no admin rights.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    protected struct ShareInfo1
    {
      [MarshalAs(UnmanagedType.LPWStr)]
      public string NetName;
      public ShareType ShareType;
      [MarshalAs(UnmanagedType.LPWStr)]
      public string Remark;
    }

    /// <summary>Share information, Win9x</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    protected struct ShareInfo50
    {
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 13)]
      public string NetName;

      public byte bShareType;
      public ushort Flags;

      [MarshalAs(UnmanagedType.LPTStr)]
      public string Remark;
      [MarshalAs(UnmanagedType.LPTStr)]
      public string Path;

      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 9)]
      public string PasswordRW;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 9)]
      public string PasswordRO;

      public ShareType ShareType
      {
        get { return (ShareType) (bShareType & 0x7F); }
      }
    }

    /// <summary>Share information level 1, Win9x</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    protected struct ShareInfo1_9X
    {
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 13)]
      public string NetName;
      public byte Padding;

      public ushort bShareType;

      [MarshalAs(UnmanagedType.LPTStr)]
      public string Remark;

      public ShareType ShareType
      {
        get { return (ShareType) (bShareType & 0x7FFF); }
      }
    }

    #endregion

    #region Windows API functions

    /// <summary>Get a UNC name</summary>
    [DllImport("mpr.dll", CharSet = CharSet.Auto)]
    protected static extern int WNetGetUniversalName(string lpLocalPath, int dwInfoLevel, ref UniversalNameInfo lpBuffer, ref int lpBufferSize);

    /// <summary>Get a UNC name</summary>
    [DllImport("mpr.dll", CharSet = CharSet.Auto)]
    protected static extern int WNetGetUniversalName(string lpLocalPath, int dwInfoLevel, IntPtr lpBuffer, ref int lpBufferSize);

    /// <summary>Enumerate shares (NT)</summary>
    [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
    protected static extern int NetShareEnum(string lpServerName, int dwLevel, out IntPtr lpBuffer, int dwPrefMaxLen, out int entriesRead,
        out int totalEntries, ref int hResume);

    /// <summary>Enumerate shares (9x)</summary>
    [DllImport("svrapi.dll", CharSet = CharSet.Ansi)]
    protected static extern int NetShareEnum([MarshalAs(UnmanagedType.LPTStr)] string lpServerName, int dwLevel,
        IntPtr lpBuffer, ushort cbBuffer, out ushort entriesRead, out ushort totalEntries);

    /// <summary>Free the buffer (NT)</summary>
    [DllImport("netapi32.dll")]
    protected static extern int NetApiBufferFree(IntPtr lpBuffer);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    protected static extern int WNetGetConnection(
        [MarshalAs(UnmanagedType.LPTStr)] string localName,
        [MarshalAs(UnmanagedType.LPTStr)] StringBuilder remoteName,
        ref int length);

    #endregion

    #region Local share name translation

    /// <summary>
    /// Tries to convert a local mapped network path into an UNC path. 
    /// For example, "P:\2008-02-29" might return: "\\networkserver\Shares\Photos\2008-02-09".
    /// </summary>
    /// <param name="localMountPath">Local path mounting a UNC or network path.</param>
    /// <param name="uncPath">If <paramref name="localMountPath"/> refers to a local path which mounts a UNC or network path, the local drive letter
    /// is converted to its underlaying UNC or network path. The rest of the <paramref name="localMountPath"/> will be preserved. The combination
    /// of those two path segments are returned in this parameter.</param>
    /// <returns><c>true</c>, if the <paramref name="localMountPath"/> is an UNC path and could be converted. In that case, <paramref name="uncPath"/>
    /// will return the corresponding UNC path pointing to the same resource as <paramref name="localMountPath"/>. Else, <c>false</c> will be
    /// returned.</returns>
    public static bool GetUNCPath(string localMountPath, out string uncPath)
    {
      uncPath = null;
      StringBuilder sb = new StringBuilder(512);
      int size = sb.Capacity;

      // Look for the {LETTER}: combination ...
      if (localMountPath.Length > 2 && localMountPath[1] == ':')
      {
        char c = localMountPath.ToLowerInvariant()[0];
        if (c >= 'a' && c <= 'z')
        {
          int error = WNetGetConnection(localMountPath.Substring(0, 2), sb, ref size);
          if (error == 0)
          {
            string path = Path.GetFullPath(localMountPath).Substring(Path.GetPathRoot(localMountPath).Length);
            uncPath = Path.Combine(sb.ToString().TrimEnd(), path);
            return true;
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Returns a formatted name of a network mapped drive like the Windows explorer does: SHARE (\\SERVER)
    /// </summary>
    /// <param name="localMountPath">Local path which mounts a network or UNC path.</param>
    /// <param name="formattedUNCPath">Returns the formatted UNC path, if <paramref name="localMountPath"/> is a network mount.
    /// Else, this parameter will be set to <c>null</c>.</param>
    /// <returns><c>true</c>, if the given <paramref name="localMountPath"/> is a local network mount and could be converted
    /// successfully. Else, <c>false</c> will be returned.</returns>
    public static bool GetFormattedUNCPath(string localMountPath, out string formattedUNCPath)
    {
      string uncPath;
      if (!GetUNCPath(localMountPath, out uncPath))
      {
        formattedUNCPath = null;
        return false;
      }
      int lastIndex = uncPath.LastIndexOf('\\');
      string sharePart = uncPath.Substring(lastIndex + 1);
      string serverPart = uncPath.Substring(0, lastIndex);
      formattedUNCPath = string.Format("{0} ({1})", sharePart, serverPart);
      return true;
    }

    #endregion

    #region Enumerate shares

    /// <summary>
    /// Enumerates the shares on Windows NT.
    /// </summary>
    /// <param name="server">The server name.</param>
    /// <returns>Collection of shares of the given <paramref name="server"/>.</returns>
    protected static ICollection<Share> EnumerateSharesNT(string server)
    {
      ICollection<Share> result = new List<Share>();
      int level = 2;
      int hResume = 0;
      IntPtr pBuffer = IntPtr.Zero;

      try
      {
        int entriesRead;
        int totalEntries;
        int nRet = NetShareEnum(server, level, out pBuffer, -1, out entriesRead, out totalEntries, ref hResume);

        if (ERROR_ACCESS_DENIED == nRet)
        {
          //Need admin for level 2, drop to level 1
          level = 1;
          nRet = NetShareEnum(server, level, out pBuffer, -1, out entriesRead, out totalEntries, ref hResume);
        }

        if (NO_ERROR == nRet && entriesRead > 0)
        {
          Type t = (2 == level) ? typeof(ShareInfo2) : typeof(ShareInfo1);
          int offset = Marshal.SizeOf(t);

          for (int i = 0, lpItem = pBuffer.ToInt32(); i < entriesRead; i++, lpItem += offset)
          {
            IntPtr pItem = new IntPtr(lpItem);
            if (1 == level)
            {
              ShareInfo1 si = (ShareInfo1) Marshal.PtrToStructure(pItem, t);
              result.Add(new Share(server, si.NetName, string.Empty, si.ShareType, si.Remark));
            }
            else
            {
              ShareInfo2 si = (ShareInfo2) Marshal.PtrToStructure(pItem, t);
              result.Add(new Share(server, si.NetName, si.Path, si.ShareType, si.Remark));
            }
          }
        }

      }
      finally
      {
        // Clean up buffer allocated by system
        if (IntPtr.Zero != pBuffer)
          NetApiBufferFree(pBuffer);
      }
      return result;
    }

    /// <summary>
    /// Returns the shares of the given <paramref name="server"/>.
    /// </summary>
    /// <param name="server">Name of the server whose shares should be returned. Use <see cref="string.Empty"/> to obtain
    /// local shares.</param>
    /// <returns>Collection of shares of the given <paramref name="server"/>.</returns>
    public static ICollection<Share> EnumerateShares(string server)
    {
      // Works for Win2k and up
      server = server.ToUpper();
      if (!server.StartsWith(@"\\"))
        server = @"\\" + server;

      return EnumerateSharesNT(server);
    }

    #endregion
  }
}
