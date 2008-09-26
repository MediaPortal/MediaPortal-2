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

using System.Collections.Generic;
using System.IO;

namespace MediaPortal.Presentation.Screen
{
  /// <summary>
  /// Interface to access a pool of resources available in a special context.
  /// </summary>
  public interface IResourceAccessor
  {
    /// <summary>
    /// Returns the resource file for the specified resource name.
    /// </summary>
    /// <param name="resourceName">Name of the resource. This is the
    /// path of the resource relative to the root directory level of this resource
    /// collection directory.</param>
    /// <returns>Absolute file path of the specified resource or <c>null</c> if
    /// the resource is not defined.</returns>
    string GetResourceFilePath(string resourceName);

    /// <summary>
    /// Returns all resource files in this resource collection, where their relative directory
    /// name match the specified regular expression pattern <paramref name="regExPattern"/>.
    /// </summary>
    /// <param name="regExPattern">Regular expression pattern which will be applied on the
    /// unified resource name.</param>
    /// <returns>Dictionary with a mapping of unified resource names to full qualified file paths of those
    /// resource files which match the search criterion.</returns>
    IDictionary<string, string> GetResourceFilePaths(string regExPattern);
  }
}
