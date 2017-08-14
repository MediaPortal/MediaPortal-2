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

namespace MP2BootstrapperApp.ChainPackages
{
  public class PackageChecker : IPackageChecker
  {
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

    public bool IsEqualOrHigherVersion(string pathToFile, Version onlineVersion)
    {
      int majorPart = GetFileMajorVersion(pathToFile);
      int minorPart = GetFileMinorVersion(pathToFile);
      int buildPart = GetFileBuildVersion(pathToFile);
      int privatePart = GetFilePrivateVersion(pathToFile);
      Version localVersion = new Version(majorPart, minorPart, buildPart, privatePart);

      return localVersion >= onlineVersion;
    }
  }
}
