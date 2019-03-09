#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.UI.Presentation.Screens;

namespace MediaPortal.UI.Presentation.SkinResources
{
  /// <summary>
  /// Interface to access a pool of resources available in a special context.
  /// </summary>
  public interface IResourceAccessor
  {
    /// <summary>
    /// Returns the resource file path for the specified resource name.
    /// </summary>
    /// <param name="resourceName">Name of the resource. This is the path of the resource relative to the root directory level of this resource
    /// collection directory.</param>
    /// <returns>Absolute file path of the specified resource or <c>null</c> if the resource is not defined.</returns>
    string GetResourceFilePath(string resourceName);

    /// <summary>
    /// Returns the resource file path and the resource bundle where the file was found for the specified resource name.
    /// </summary>
    /// <param name="resourceName">Name of the resource. This is the path of the resource relative to the root directory level of this resource
    /// collection directory.</param>
    /// <param name="searchInheritedResources">If set to <c>true</c>, inherited skin resources will be searched if the specified resource
    /// is not present in the current resource accessor, else inherited resources won't be checked.</param>
    /// <param name="resourceBundle">Resource bundle which contains the requested resource.</param>
    /// <returns>Absolute file path of the specified resource or <c>null</c> if the resource is not defined.</returns>
    string GetResourceFilePath(string resourceName, bool searchInheritedResources,
        out ISkinResourceBundle resourceBundle);

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