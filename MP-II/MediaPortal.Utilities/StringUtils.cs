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
  /// <summary>
  /// Contains String related utility methods.
  /// </summary>
  public class StringUtils
  {
    /// <summary>
    /// Tries to parse the specified version string in the format <c>#.#</c> or <c>#</c>, where # stands
    /// for an int number.
    /// </summary>
    /// <param name="versionStr">The string to parse. This string should be in the format
    /// <c>#.#</c> or <c>#</c>.</param>
    /// <param name="verHigh">Returns the version high number.</param>
    /// <param name="verHigh">Returns the version low number, if the string contains both. Else, this
    /// parameter will return <c>0</c>.</param>
    /// <returns><c>true</c>, if the version string could correctly be parsed, else <c>false</c>.</returns>
    public static bool TryParseVersionString(string versionStr, out int verHigh, out int verLow)
    {
      string[] numbers = versionStr.Split(new char[] { '.' });
      verLow = 0;
      verHigh = 0;
      if (numbers.Length < 1 || numbers.Length > 2)
        return false;
      if (!Int32.TryParse(numbers[0], out verHigh))
        return false;
      if (numbers.Length > 1)
        if (!Int32.TryParse(numbers[0], out verLow))
          return false;
      return true;
    }

    /// <summary>
    /// Helper method to check the given version string to be equal or greater than the
    /// specified version number.
    /// </summary>
    public static void CheckVersionEG(string versionStr, int expectedHigh, int expectedLow)
    {
      int verHigh;
      int verLow;
      if (!TryParseVersionString(versionStr, out verHigh, out verLow))
        throw new ArgumentException("Illegal version number '" + versionStr + "', expected format: '#.#'");
      if (verHigh >= expectedHigh)
        return;
      if (verLow >= expectedLow)
        return;
      throw new ArgumentException("Version number '" + versionStr +
                                  "' is too low, at least '" + expectedHigh + "." + expectedLow + "' is needed");
    }
  }
}