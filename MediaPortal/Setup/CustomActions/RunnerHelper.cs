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
using System.Diagnostics;
using System.IO;
using System.Net;
using Microsoft.Win32;

namespace CustomActions
{
  public class RunnerHelper : WebClient, IRunnerHelper
  {
    /// <inheritdoc />
    public void DownloadFileAndReleaseResources(string address, string fileName)
    {
      DownloadFile(address, fileName);
      Dispose();
    }

    /// <inheritdoc />
    public string GetPathForRegistryKey(string registryKey)
    {
      RegistryKey key = Registry.ClassesRoot.OpenSubKey(registryKey, false);
      if (key != null)
      {
        return key.GetValue(null) as string;
      }
      return string.Empty;
    }

    /// <inheritdoc />
    public int GetFileMajorVersion(string pathToFile)
    {
      return FileVersionInfo.GetVersionInfo(pathToFile).FileMajorPart;
    }

    /// <inheritdoc />
    public int GetFileMinorVersion(string pathToFile)
    {
      return FileVersionInfo.GetVersionInfo(pathToFile).FileMinorPart;
    }

    /// <inheritdoc />
    public int GetFileBuildVersion(string pathToFile)
    {
      return FileVersionInfo.GetVersionInfo(pathToFile).FileBuildPart;
    }

    /// <inheritdoc />
    public int GetFilePrivateVersion(string pathToFile)
    {
      return FileVersionInfo.GetVersionInfo(pathToFile).FilePrivatePart;
    }

    /// <inheritdoc />
    public bool Exists(string path)
    {
      return !string.IsNullOrEmpty(path) && File.Exists(path);
    }

    /// <inheritdoc />
    public bool Start(string filenam, string arg, int time)
    {
      Process prc = Process.Start(filenam, arg);
      return prc.WaitForExit(time);
    }

    /// <summary>
    /// Enable compression and set a shorter request timeout.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    protected override WebRequest GetWebRequest(Uri address)
    {
      Headers["Accept-Encoding"] = "gzip,deflate";
      HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
      request.Timeout = 15000; // use 15 seconds - default is 100 seconds
      request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
      return request;
    }
  }
}
