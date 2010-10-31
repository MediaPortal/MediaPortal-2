#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
    #region Imports

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

    #endregion

    #region Static members

    /// <summary>
    /// Tries to detect the mimetype of a stream. 
    /// It uses both binary detection as well as registry extension lookup.
    /// </summary>
    /// <param name="mediaItemStream">Opened Stream</param>
    /// <returns>MimeType</returns>
    public static string GetMimeType(Stream mediaItemStream)
    {
      String mimeType = GetMimeFromStream(mediaItemStream);
      // If no specific type was found by binary data, try lookup via registry
      if ((mimeType == "unknown/unknown" || mimeType == "application/octet-stream") && (mediaItemStream is FileStream))
      {
        mimeType = GetMimeTypeFromRegistry((mediaItemStream as FileStream).Name);
      }
      return mimeType;
    }

    /// <summary>
    /// Tries to detect the mimetype of a local file.
    /// It uses both binary detection as well as registry extension lookup.
    /// </summary>
    /// <param name="filename">Filename</param>
    /// <returns>MimeType</returns>
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

    /// <summary>
    /// Tries to detect the mimetype of a local file using binary detection only.
    /// </summary>
    /// <param name="filename">Filename</param>
    /// <returns>MimeType</returns>
    public static string GetMimeFromFile(string filename)
    {
      if (!File.Exists(filename))
        throw new FileNotFoundException(filename + " not found");

      FileStream fs = new FileStream(filename, FileMode.Open);
      return GetMimeFromStream(fs);
    }

    /// <summary>
    /// Tries to detect the mimetype of a local file using registry extension lookup only.
    /// </summary>
    /// <param name="filename">Filename</param>
    /// <returns>MimeType</returns>
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

    #endregion

    #region Private members

    private static string GetMimeFromStream(Stream fs)
    {
      byte[] buffer = new byte[256];
      try
      {
        // read binary data from stream, maximum 256 bytes.
        if (fs.Length >= 256)
          fs.Read(buffer, 0, 256);
        else
          fs.Read(buffer, 0, (int)fs.Length);

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

    #endregion
  }
}