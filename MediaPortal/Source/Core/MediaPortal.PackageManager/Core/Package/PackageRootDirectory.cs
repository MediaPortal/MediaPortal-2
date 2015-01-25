#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MediaPortal.PackageManager.Core.Package
{
  /// <summary>
  /// Class representing an directory that is directly located in the package root
  /// </summary>
  [DebuggerDisplay("RootDirectory: {RealName} Used={IsUsed}")]
  public class PackageRootDirectory
  {
    #region constants

    public const string AUTO_COPY_DIRECTORY_PREFIX = "%";
    public const string AUTO_COPY_DIRECTORY_SUFFIX = "%";

    #endregion

    #region ctor

    /// <summary>
    /// Creates an instance from a given path
    /// </summary>
    /// <param name="fullPath">Full path of the directory.</param>
    internal PackageRootDirectory(string fullPath)
    {
      FullPath = fullPath;
      Name = System.IO.Path.GetFileName(fullPath) ?? String.Empty;
      IsAutoCopyDirectory = Name.StartsWith(AUTO_COPY_DIRECTORY_PREFIX) && Name.EndsWith(AUTO_COPY_DIRECTORY_SUFFIX);
      if (IsAutoCopyDirectory)
      {
        Name = Name.Substring(
          AUTO_COPY_DIRECTORY_PREFIX.Length,
          Name.Length - (AUTO_COPY_DIRECTORY_PREFIX.Length + AUTO_COPY_DIRECTORY_SUFFIX.Length));
      }
      IsUsed = false;
    }

    #endregion

    #region public properties

    /// <summary>
    /// Gets the full path of the directory
    /// </summary>
    public string FullPath { get; private set; }

    /// <summary>
    /// Gets the name of the directory.
    /// </summary>
    /// <remarks>
    /// For auto copy directories the prefix and suffix are not included in the name.
    /// Use <see cref="RealName"/> instead.
    /// </remarks>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the real name of the directory.
    /// </summary>
    /// <remarks>The real name includes prefix and suffix of auto copy directories.</remarks>
    public string RealName
    {
      get
      {
        if (IsAutoCopyDirectory)
        {
          return String.Concat(AUTO_COPY_DIRECTORY_PREFIX, Name, AUTO_COPY_DIRECTORY_SUFFIX);
        }
        return Name;
      }
    }

    /// <summary>
    /// Gets if this is an auto copy directory.
    /// </summary>
    public bool IsAutoCopyDirectory { get; private set; }

    /// <summary>
    /// Gets if this directory is used by any element of the package.
    /// </summary>
    public bool IsUsed { get; private set; }

    #endregion

    #region internal methods

    /// <summary>
    /// Sets the <see cref="IsUsed"/> flag to <c>true</c>.
    /// </summary>
    internal void SetUsed()
    {
      IsUsed = true;
    }

    #endregion
  }

  /// <summary>
  /// Collection of <see cref="PackageRootDirectory"/>s.
  /// </summary>
  public class PackageRootDirectoryCollection : Collection<PackageRootDirectory>
  { }
}