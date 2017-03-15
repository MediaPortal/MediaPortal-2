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
using System.Xml.Linq;

namespace CustomActions
{
  /// <summary>
  /// An interface to make the a custom action unit-teastable.
  /// </summary>
  public interface IRunnerHelper
  {
    /// <summary>
    /// Returns the absolute path for to file by given registry key.
    /// </summary>
    /// <param name="registryKey">The registry key for which to obtain absolute path.</param>
    /// <returns>The fully qualified location of path, such as "C:\MyFile.txt".</returns>
    string GetPathForRegistryKey(string registryKey);

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
    /// Downloads the file and releases used resources.
    /// </summary>
    /// <param name="address">The addres from where downalod the file</param>
    /// <param name="fileName">The file location</param>
    void DownloadFileAndReleaseResources(string address, string fileName);

    /// <summary>
    /// Creates a new System.Xml.Linq.XDocument from a file.
    /// </summary>
    /// <param name="uri">A URI string that references the file to load into a new System.Xml.Linq.XDocument</param>
    /// <returns>An System.Xml.Linq.XDocument that contains the contents of the specified file.</returns>
    XDocument LoadXmlDocument(string uri);

    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The path to the file to check.</param>
    /// <returns>true if the file exists, otherwise false.</returns>
    bool Exists(string path);

    /// <summary>
    /// Starts a process resource by specifying the name of an application and a set
    /// of command-line arguments, and associates the resource with a new System.Diagnostics.Process
    /// component.
    /// </summary>
    /// <param name="fileName">The name of an application file to run in the process.</param>
    /// <param name="arg">Command-line arguments to pass when starting the process.</param>
    /// <param name="time">The amount of time, in milliseconds, to wait for the associated process to exit.</param>
    /// <returns>true if the associated process has exited; otherwise, false.</returns>
    bool Start(string fileName, string arg, int time);
  }
}
