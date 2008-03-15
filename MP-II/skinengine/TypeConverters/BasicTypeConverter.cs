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
using SlimDX;

namespace SkinEngine.TypeConverters
{
  /// <summary>
  /// This class holds static methods for converting string representations
  /// into objects of a specific type.
  /// </summary>
  public class BasicTypeConverter
  {
    #region Public methods
    /// <summary>
    /// Converts a string into a <see cref="float"/>.
    /// </summary>
    /// <param name="floatString">A string containing a float. It doesn't matter if
    /// the string contains commas or dots as decimal point.</param>
    /// <returns>Float number parsed from the specified <paramref name="floatString"/>.</returns>
    public static float Convert2Float(string floatString)
    {
      floatString = BringFloatStringToCurrentCultureFormat(floatString);
      float f;
      float.TryParse(floatString, out f);
      return f;
    }

    /// <summary>
    /// Converts a string into a <see cref="double"/>.
    /// </summary>
    /// <param name="doubleString">A string containing a double. It doesn't matter if
    /// the string contains commas or dots as decimal point.</param>
    /// <returns>Double number parsed from the specified <paramref name="doubleString"/>.</returns>
    public static double Convert2Double(string doubleString)
    {
      doubleString = BringFloatStringToCurrentCultureFormat(floatString);
      double f;
      double.TryParse(doubleString, out f);
      return f;
    }
    #endregion

    #region Private/protected methods
    protected static string BringFloatStringToCurrentCultureFormat(string floatString)
    {
      float test = 12.03f;
      string comma = test.ToString();
      // Bring the float string in a format which can be parsed with the current language settings
      bool langUsesComma = (comma.IndexOf(",") >= 0);
      if (langUsesComma)
      {
        floatString = floatString.Replace(".", ",");
      }
      else
      {
        floatString = floatString.Replace(",", ".");
      }
      return floatString;
    }
    #endregion
  }
}
