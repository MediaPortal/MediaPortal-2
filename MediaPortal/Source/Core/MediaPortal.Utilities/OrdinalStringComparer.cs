#region Copyright (C) 2019 Team MediaPortal

/*
    Copyright (C) 2019 Team MediaPortal
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
using System.Text.RegularExpressions;

namespace MediaPortal.Utilities
{
  /// <summary> 
  /// Defines a method that a string implements to compare two values according to their ordinal value (text and number). 
  /// i.e. it sorts the same way a Windows file listing sorts, so that (e.g.) "File 10" is > "File 1"
  /// Note that the class emulates the Windows bug/feature that (e.g.) "list 2.txt" sorts BEFORE "list.txt"
  /// This class implements the <c>IComparer&lt;T&gt;</c> interface with type string. 
  /// Thanks to Stefano Tempesta https://social.msdn.microsoft.com/profile/stefano%20tempesta/ for the initial code.
  /// </summary> 
  public class OrdinalStringComparer : IComparer<string>
  {
    private static Regex _numericSplitter = new Regex("([0-9]+)");
    private bool _ignoreCase = true;

    /// <summary> 
    /// Creates an instance of <c>OrdinalStringComparer</c> for case-insensitive string comparison. 
    /// </summary> 
    public OrdinalStringComparer()
        : this(true)
    {
    }

    /// <summary> 
    /// Creates an instance of <c>OrdinalStringComparer</c> for case comparison according to the value specified in input. 
    /// </summary> 
    /// <param name="ignoreCase">true to ignore case during the comparison; otherwise, false.</param> 
    public OrdinalStringComparer(bool ignoreCase)
    {
      _ignoreCase = ignoreCase;
    }

    /// <summary> 
    /// Compares two strings and returns a value indicating whether one is less than, equal to, or greater than the other. 
    /// </summary> 
    /// <param name="x">The first string to compare.</param> 
    /// <param name="y">The second string to compare.</param> 
    /// <returns>A signed integer that indicates the relative values of x and y, as in the Compare method in the <c>IComparer&lt;T&gt;</c> interface.</returns> 
    public int Compare(string x, string y)
    {
      // check for null values first: a null reference is considered to be less than any reference that is not null 
      if (x == null)
      {
        return y == null ? 0 : -1;
      }
      if (y == null)
      {
        return 1;
      }

      StringComparison comparisonMode = _ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;

      string[] splitX = _numericSplitter.Split(x.Replace(" ", ""));
      string[] splitY = _numericSplitter.Split(y.Replace(" ", ""));

      int comparer = 0;

      for (int i = 0; comparer == 0 && i < splitX.Length; i++)
      {
        if (splitY.Length <= i)
          return 1; // x > y 

        int numericX = -1;
        int numericY = -1;
        if (int.TryParse(splitX[i], out numericX))
        {
          if (int.TryParse(splitY[i], out numericY))
          {
            comparer = numericX - numericY;
          } else
          {
            return 1; // x > y 
          }
        } else
        {
          comparer = String.Compare(splitX[i], splitY[i], comparisonMode);
        }
      }
      if (comparer == 0 && splitY.Length > splitX.Length)
        comparer = -1;
      return comparer;
    }
  }
}

