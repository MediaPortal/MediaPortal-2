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
using System.Globalization;

namespace MediaPortal.Database.SQLite
{
  /// <summary>
  /// Culture-sensitive string comparison that orders embedded numbers by value, so that
  /// "Episode 2" sorts before "Episode 10".
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the portable half of <see cref="LexicographicComparer"/>, split out because the
  /// alternative was an untestable class: while the algorithm lives behind an
  /// <c>OperatingSystem.IsWindows()</c> branch, no test on a Windows machine ever reaches it. Here it
  /// can be tested on every platform, and <see cref="LexicographicComparer"/> keeps a cross-check test
  /// that both halves agree on the cases MP2 actually sorts.
  /// </para>
  /// <para>
  /// The strings are walked in runs: a run of ASCII digits is compared numerically (ignoring leading
  /// zeros), anything else is compared with the culture's text comparer - which is where case
  /// sensitivity comes from. Only ASCII digits count as numbers on purpose; <c>char.IsDigit</c> also
  /// matches digits like Arabic-Indic ٢, and turning those into a numeric comparison would make the
  /// ordering depend on a conversion no one asked for.
  /// </para>
  /// <para>
  /// It is an approximation of <c>CompareStringEx</c> with <c>SORT_DIGITS_AS_NUMBERS</c>, not an
  /// identical one: Windows compares numbers with a digit-count limit and knows about collation
  /// elements inside numbers, and its ignore-symbol/ignore-non-space/ignore-width options have no
  /// counterpart here. No caller in this tree uses those three options; see
  /// <see cref="LexicographicComparer.CmpFlags"/> for what the Windows path supports.
  /// </para>
  /// </remarks>
  public class NaturalStringComparer : IComparer<string>
  {
    #region Variables

    private readonly StringComparer _text;
    private readonly bool _digitsAsNumbers;

    #endregion

    #region Constructors/Destructors

    /// <summary>
    /// Creates a comparer that orders text with <paramref name="cultureInfo"/> and, when
    /// <paramref name="digitsAsNumbers"/> is set, embedded ASCII digit runs by value.
    /// </summary>
    public NaturalStringComparer(CultureInfo cultureInfo, bool ignoreCase, bool digitsAsNumbers)
    {
      if (cultureInfo == null)
        throw new ArgumentNullException(nameof(cultureInfo));
      _text = StringComparer.Create(cultureInfo, ignoreCase);
      _digitsAsNumbers = digitsAsNumbers;
    }

    #endregion

    #region Public methods

    public int Compare(string x, string y)
    {
      x ??= string.Empty;
      y ??= string.Empty;

      int posX = 0;
      int posY = 0;
      while (posX < x.Length && posY < y.Length)
      {
        bool xDigit = IsAsciiDigit(x[posX]);
        bool yDigit = IsAsciiDigit(y[posY]);

        if (xDigit && yDigit)
        {
          if (!_digitsAsNumbers)
            return _text.Compare(x.Substring(posX), y.Substring(posY));

          int endX = EndOfDigitRun(x, posX);
          int endY = EndOfDigitRun(y, posY);
          int byValue = CompareNumbers(x, posX, endX, y, posY, endY);
          if (byValue != 0)
            return byValue;
          posX = endX;
          posY = endY;
          continue;
        }

        if (xDigit != yDigit)
        {
          // A number on one side and text on the other. There is no run structure left to compare, so
          // whatever remains is handed to the text comparer, which is what a single text comparison
          // would have done with it.
          return _text.Compare(x.Substring(posX), y.Substring(posY));
        }

        int textEndX = EndOfTextRun(x, posX);
        int textEndY = EndOfTextRun(y, posY);
        int byText = _text.Compare(x.Substring(posX, textEndX - posX), y.Substring(posY, textEndY - posY));
        if (byText != 0)
          return byText;
        posX = textEndX;
        posY = textEndY;
      }

      // Equal up to here. Whichever side still has characters is the longer one.
      if (posX < x.Length)
        return 1;
      return posY < y.Length ? -1 : 0;
    }

    #endregion

    #region Private methods

    private static bool IsAsciiDigit(char c)
    {
      return c >= '0' && c <= '9';
    }

    private static int EndOfDigitRun(string value, int start)
    {
      int pos = start;
      while (pos < value.Length && IsAsciiDigit(value[pos]))
        pos++;
      return pos;
    }

    private static int EndOfTextRun(string value, int start)
    {
      int pos = start;
      while (pos < value.Length && !IsAsciiDigit(value[pos]))
        pos++;
      return pos;
    }

    /// <summary>
    /// Compares two runs of digits by value, without parsing them - the runs can be longer than any
    /// integer type, and media titles have been that creative.
    /// </summary>
    private static int CompareNumbers(string x, int startX, int endX, string y, int startY, int endY)
    {
      // Kept before the zeros are skipped, for the tie-break at the end.
      int digitsX = endX - startX;
      int digitsY = endY - startY;

      // Leading zeros carry no value, but "0" and "000" must stay equal, so the last digit always
      // stays in the run.
      while (endX - startX > 1 && x[startX] == '0')
        startX++;
      while (endY - startY > 1 && y[startY] == '0')
        startY++;

      int lengthX = endX - startX;
      int lengthY = endY - startY;
      if (lengthX != lengthY)
        return lengthX < lengthY ? -1 : 1;

      for (int i = 0; i < lengthX; i++)
      {
        char a = x[startX + i];
        char b = y[startY + i];
        if (a != b)
          return a < b ? -1 : 1;
      }

      // Same value, but the runs may have differed in leading zeros, which the loop above skipped.
      // Measured against CompareStringEx with SORT_DIGITS_AS_NUMBERS on Windows, which does not call
      // "x007" and "x7" equal - the longer run sorts first - and an approximation of a collation that
      // flips that case is not worth the surprise when a library is reimported on another platform.
      if (digitsX == digitsY)
        return 0;
      return digitsX > digitsY ? -1 : 1;
    }

    #endregion
  }
}
