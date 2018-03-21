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
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace MediaPortal.Database.SQLite
{
  /// <summary>
  /// Comparer using the native CompareStringEx method. Makes it possible to support
  /// culture sensitive and natural number sorting.
  /// For details see here: http://msdn.microsoft.com/en-us/library/dd317761%28v=vs.85%29.aspx
  /// </summary>
  public class LexicographicComparer : IComparer<String>
  {
    #region Constants

    // Generic locale names that can be used for the localeName parameter of CompareStringEx
    public const String LOCALE_NAME_INVARIANT = "";
    public const String LOCALE_NAME_USER_DEFAULT = null;
    public const String LOCALE_NAME_SYSTEM_DEFAULT = "!sys-default-locale";

    #endregion

    #region Enums

    /// <summary>
    /// Compare options used by the native CompareStringEx method. For details on the
    /// behavior see here: http://msdn.microsoft.com/en-us/library/dd317761%28v=vs.85%29.aspx
    /// </summary>
    [Flags]
    public enum CmpFlags
    {
      Default = 0x00000000,
      NormIgnorecase = 0x00000001,
      NormIgnorenonspace = 0x00000002,
      NormIgnoresymbols = 0x00000004,
      LinguisticIgnorecase = 0x00000010,
      LinguisticIgnorediacritic = 0x00000020,
      NormIgnorekanatype = 0x00010000,
      NormIgnorewidth = 0x00020000,
      NormLinguisticCasing = 0x08000000,
      SortStringsort = 0x00001000,
      SortDigitsasnumbers = 0x00000008,
    }

    #endregion

    #region Imports

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern Int32 CompareStringEx
    (
      String localeName,
      CmpFlags flags,
      String str1,
      Int32 count1,
      String str2,
      Int32 count2,
      IntPtr versionInformation,
      IntPtr reserved,
      Int32 param
    );

    #endregion

    #region Variables

    private readonly String _locale;
    private readonly CmpFlags _flags;

    #endregion

    #region Constructors/Destructors

    public LexicographicComparer(CultureInfo cultureInfo, CmpFlags flags = CmpFlags.Default)
    {
      // It is not recommended to use a neutral culture for comparing / sorting.
      // (http://msdn.microsoft.com/de-de/library/system.globalization.cultureinfo%28v=vs.110%29.aspx)
      // If a neutral culture is passed, we use InvariantCulture for sorting.
      if (cultureInfo.Equals(CultureInfo.InvariantCulture) || cultureInfo.IsNeutralCulture)
        _locale = LOCALE_NAME_INVARIANT;
      else
        _locale = cultureInfo.Name;
      
      _flags = flags;
    }

    #endregion

    #region IComparer implementation

    public int Compare(string x, string y)
    {
      int result = CompareStringEx(_locale, _flags, x, -1, y, -1, IntPtr.Zero, IntPtr.Zero, 0);
      
      // CompareStringEx returns the values 1, 2 or 3 for less than, equal or greater than,
      // if it the call was successfull. We have to subtract the value 2 to maintain the
      // C runtime convention of comparing strings.
      // If CompareStringEx returns 0, the call was not successful. This can only have two
      // reasons: ERROR_INVALID_FLAGS or ERROR_INVALID_PARAMETER. In both cases the calling
      // code must be wrong and it's justified to throw an exception.
      if (result != 0)
        return result - 2;
      
      throw new ArgumentException("CompareStringEx has been called with invalid flags or invalid parameters.");
    }

    #endregion
  }
}
