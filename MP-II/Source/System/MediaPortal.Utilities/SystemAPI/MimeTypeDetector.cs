#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32;

namespace MediaPortal.Utilities.SystemAPI
{
  /// <summary>
  /// Detect MimeType from binary file by reading the first 256 bytes.
  /// </summary>
  public static class MimeTypeDetector
  {
    [DllImport(@"urlmon.dll", CharSet = CharSet.Auto)]
    private extern static UInt32 FindMimeFromData(
        UInt32 pBC,
        [MarshalAs(UnmanagedType.LPStr)] String pwzUrl,
        [MarshalAs(UnmanagedType.LPArray)] byte[] pBuffer,
        UInt32 cbSize,
        [MarshalAs(UnmanagedType.LPStr)] String pwzMimeProposed,
        UInt32 dwMimeFlags,
        out UInt32 ppwzMimeOut,
        UInt32 dwReserverd
        );

    public static string GetMimeType(string filename)
    {
      String mimeType = GetMimeFromFile(filename);
      // If no specific type was found by binary data, try lookup via registry
      if (mimeType == "unknown/unknown" || mimeType == "application/octet-stream")
      {
        mimeType = GetMimeTypeFromRegistry(filename);
      }
      return mimeType;
    }

    public static string GetMimeFromFile(string filename)
    {
      if (!File.Exists(filename))
        throw new FileNotFoundException(filename + " not found");

      byte[] buffer = new byte[256];
      using (FileStream fs = new FileStream(filename, FileMode.Open))
      {
        if (fs.Length >= 256)
          fs.Read(buffer, 0, 256);
        else
          fs.Read(buffer, 0, (int)fs.Length);
      }
      try
      {
        UInt32 mimetype;
        FindMimeFromData(0, null, buffer, 256, null, 0, out mimetype, 0);
        IntPtr mimeTypePtr = new IntPtr(mimetype);
        string mime = Marshal.PtrToStringUni(mimeTypePtr);
        Marshal.FreeCoTaskMem(mimeTypePtr);
        return mime;
      }
      catch (Exception)
      {
        return "unknown/unknown";
      }
    }

    public static string GetMimeTypeFromRegistry(string filename)
    {
      try
      {
        string ext = Path.GetExtension(filename);

        if (string.IsNullOrEmpty(ext))
          return null;

        // Convert to lowercase as registry has lowercase keys
        ext = ext.ToLower();
        RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(ext);

        if (registryKey == null)
          return null;

        return registryKey.GetValue("Content Type") as string;
      }
      catch (Exception)
      {
        return "unknown/unknown";
      }
    }
  }
}