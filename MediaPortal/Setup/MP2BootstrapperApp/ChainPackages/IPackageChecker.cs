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

namespace MP2BootstrapperApp.ChainPackages
{
  public interface IPackageChecker
  {
    /// <summary>
    /// Gets the major part of the version number.
    /// </summary>
    /// <param name="pathToFile">Path to the file.</param>
    /// <returns>The version number as int.</returns>
    int GetFileMajorVersion(string pathToFile);

    /// <summary>
    /// Gets the minor part of the version number.
    /// </summary>
    /// <param name="pathToFile">Path to the file.</param>
    /// <returns>The version number as int.</returns>
    int GetFileMinorVersion(string pathToFile);

    /// <summary>
    /// Gets the build part of the version number.
    /// </summary>
    /// <param name="pathToFile">Path to the file.</param>
    /// <returns>The version number as int.</returns>
    int GetFileBuildVersion(string pathToFile);

    /// <summary>
    /// Gets the private part of the version number.
    /// </summary>
    /// <param name="pathToFile">Path to the file.</param>
    /// <returns>The version number as int.</returns>
    int GetFilePrivateVersion(string pathToFile);

    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The path to the file to check.</param>
    /// <returns>true if the file exists, otherwise false.</returns>
    bool Exists(string path);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pathToInstalledVersion"></param>
    /// <param name="onlineVersion"></param>
    /// <returns></returns>
    bool IsEqualOrHigherVersion(string pathToInstalledVersion, Version onlineVersion);

    /// <summary>
    /// Checks the presence of an MSI package by its product code.
    /// </summary>
    /// <param name="productCode">The MSI product code to check.</param>
    /// <returns>true if the MSI is installed, otherwise false.</returns>
    bool CheckInstallPresenceByMsiProductCode(string productCode);
  }
}
