#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaPortal.Core.PathManager
{
  public interface IPathManager : IServiceInfo
  {
    /// <summary>
    /// Checks if a directory with the specified label exists.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <returns>true/false the label exists</returns>
    bool Exists(string label);

    /// <summary>
    /// Sets a path.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <param name="pathPattern">The path pattern.</param>
    void SetPath(string label, string pathPattern);

    /// <summary>
    /// Gets a path.
    /// </summary>
    /// <param name="pathPattern">The path pattern.</param>
    /// <returns>the path as a string</returns>
    string GetPath(string pathPattern);

    /// <summary>
    /// Replaces an existing path.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <param name="pathPattern">The path pattern.</param>
    void ReplacePath(string label, string pathPattern);

    /// <summary>
    /// Removes a path.
    /// </summary>
    /// <param name="label">The path label.</param>
    void RemovePath(string label);

    /// <summary>
    /// Gets a path as DirectoryInfo
    /// </summary>
    /// <param name="pathPattern">The path pattern.</param>
    /// <returns>the path as DirectoryInfo</returns>
    DirectoryInfo GetDirectoryInfo(string pathPattern);
  }
}
