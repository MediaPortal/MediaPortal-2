#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

namespace MP2BootstrapperApp.Utils
{
  public static class VersionUtils
  {
    private static readonly string EMPTY_VERSION_STRING = new Version().ToString();

    /// <summary>
    /// Determines whether a string is null, empty, or equal to 0.0.0.0 
    /// </summary>
    /// <param name="versionString">A string that represents a version.</param>
    /// <returns><c>true</c> if <paramref name="versionString"/> was equal to <c>null</c>, <see cref="string.Empty"/> or '0.0.0.0'; else <c>false</c>.</returns>
    public static bool IsNullOrEmptyVersionString(string versionString)
    {
      return string.IsNullOrEmpty(versionString) || string.Compare(versionString, EMPTY_VERSION_STRING, StringComparison.InvariantCultureIgnoreCase) == 0;
    }
  }
}
