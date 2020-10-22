#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.UI.Presentation.Utilities
{
  /// <summary>
  /// Validates the given path.
  /// </summary>
  /// <param name="path">Path to check.</param>
  /// <returns><c>true</c>, if the path is considered valid, else <c>false</c>.</returns>
  public delegate bool ValidatePathDlgt(ResourcePath path);

  /// <summary>
  /// Validates a file name to see if it should be included (for implementing file filters).
  /// </summary>
  /// <param name="path">File to check.</param>
  /// <returns><c>true</c>, if the file is to be included in the dialog, else <c>false</c>.</returns>
  public delegate bool ValidateFileDlgt(ResourcePath path);

  /// <summary>
  /// Generic path browser API.
  /// </summary>
  /// <remarks>
  /// This service provides an API to show a browser for files and/or directories.
  /// To track the outcome of the path browser dialog, you can register at the message channel <see cref="PathBrowserMessaging.CHANNEL"/>.
  /// </remarks>
  public interface IPathBrowser
  {
    Guid ShowPathBrowser(string headerText, bool enumerateFiles, bool showSystemResources, ValidatePathDlgt validatePathDlgt);
    Guid ShowPathBrowser(string headerText, bool enumerateFiles, bool showSystemResources, ResourcePath initialPath, ValidatePathDlgt validatePathDlgt);
    Guid ShowPathBrowser(string headerText, bool enumerateFiles, bool showSystemResources, ResourcePath initialPath, ValidatePathDlgt validatePathDlgt, ValidateFileDlgt validateFileDlgt);
  }

  /// <summary>
  /// Class to provide a file name filter that only accepts certain extensions (case insensitive)
  /// </summary>
  public class FileExtensionFilter {
    IEnumerable<string> _permittedExtensions;

    /// <summary>
    /// Call with one parameter per permitted extension.
    /// NB The extension should contain the leading dot (.) - e.g. ".png"
    /// </summary>
    public FileExtensionFilter(params string[] extensions) {
      _permittedExtensions = extensions;
    }
    /// <summary>
    /// Call with enumerable (List/Array/etc.) of permitted extensions.
    /// NB The extension should contain the leading dot (.) - e.g. ".png"
    /// </summary>
    /// <param name="extensions"></param>
    public FileExtensionFilter(IEnumerable<string> permittedExtensions) {
      _permittedExtensions = permittedExtensions;
    }
    /// <summary>
    /// The ValidateFile method matches ValidateFileDlgt
    /// </summary>
    public bool ValidateFile(ResourcePath path) {
      return _permittedExtensions.Any(e => path.FileName.ToLower().EndsWith(e.ToLower()));
    }
  }
}
