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

namespace MediaPortal.Utilities
{
  public class Versions
  {
    /// <summary>
    /// Tries to parse the specified version string in the format <c>#.#</c> or <c>#</c>, where # stands
    /// for an int number.
    /// </summary>
    /// <param name="versionStr">The string to parse. This string should be in the format
    /// <c>#.#</c> or <c>#</c>.</param>
    /// <param name="verMajor">Returns the major version number.</param>
    /// <param name="verMinor">Returns the minor version number, if the string contains both. Else, this
    /// parameter will return <c>0</c>.</param>
    /// <returns><c>true</c>, if the version string could correctly be parsed, else <c>false</c>.</returns>
    public static bool TryParseVersionString(string versionStr, out int verMajor, out int verMinor)
    {
      string[] numbers = versionStr.Split(new char[] { '.' });
      verMinor = 0;
      verMajor = 0;
      if (numbers.Length < 1 || numbers.Length > 2)
        return false;
      if (!Int32.TryParse(numbers[0], out verMajor))
        return false;
      if (numbers.Length > 1)
        if (!Int32.TryParse(numbers[0], out verMinor))
          return false;
      return true;
    }

    /// <summary>
    /// Helper method to check the given version string to be compatible to the specified version number.
    /// A compatible version has the same major version number (<paramref name="expectedMajor"/>) and an
    /// equal or greater minor version number (<paramref name="minExpectedMinor"/>).
    /// </summary>
    /// <param name="versionStr">Version string to check against the expected values.</param>
    /// <param name="expectedMajor">Expected major version. The major version number of the given
    /// <paramref name="versionStr"/> must match exactly with the <paramref name="expectedMajor"/>
    /// version.</param>
    /// <param name="minExpectedMinor">Expected minor version. The minor version number of the given
    /// <paramref name="versionStr"/> must match this parameter or be greater than it.</param>
    public static void CheckVersionCompatible(string versionStr, int expectedMajor, int minExpectedMinor)
    {
      int verHigh;
      int verLow;
      if (!TryParseVersionString(versionStr, out verHigh, out verLow))
        throw new ArgumentException("Illegal version number '" + versionStr + "', expected format: '#.#'");
      if (verHigh == expectedMajor && verLow >= minExpectedMinor)
        return;
      throw new ArgumentException("Version number '" + versionStr +
          "' is not compatible with expected version number '" + expectedMajor + "." + minExpectedMinor + "'");
    }
  }
}