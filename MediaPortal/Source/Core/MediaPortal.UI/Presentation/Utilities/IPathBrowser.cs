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
  }
}
