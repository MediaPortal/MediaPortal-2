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
using System.Globalization;
using MediaPortal.Database.SQLite;
using NUnit.Framework;

namespace Tests.Server.Database
{
  /// <summary>
  /// Behaviour tests for <see cref="NaturalStringComparer"/>, the comparison SQLite's
  /// <c>NOCASE</c> collation uses on platforms without <c>CompareStringEx</c>, and a cross-check that
  /// it agrees with the Windows implementation on the comparisons MP2 actually performs.
  /// </summary>
  [TestFixture]
  public class TestNaturalStringComparer
  {
    #region Helpers

    /// <summary>
    /// The configuration <see cref="SQLiteCultureSensitiveCollation"/> registers: case-insensitive,
    /// linguistic casing, numbers compared by value.
    /// </summary>
    private static readonly NaturalStringComparer NoCase = new NaturalStringComparer(
        CultureInfo.InvariantCulture, true, true);

    private static int Sign(int value)
    {
      return Math.Sign(value);
    }

    private static void AssertBefore(string first, string second)
    {
      Assert.That(Sign(NoCase.Compare(first, second)), Is.EqualTo(-1),
          "\"{0}\" should sort before \"{1}\"", first, second);
      Assert.That(Sign(NoCase.Compare(second, first)), Is.EqualTo(1),
          "\"{1}\" should sort after \"{0}\"", first, second);
    }

    private static void AssertEqual(string left, string right)
    {
      Assert.That(NoCase.Compare(left, right), Is.EqualTo(0),
          "\"{0}\" and \"{1}\" should compare equal", left, right);
    }

    #endregion

    #region Natural number tests

    [Test]
    public void TestNumbersCompareByValueNotByText()
    {
      // The whole reason this comparer exists: in a plain text comparison "Episode 10" comes before
      // "Episode 2", which is how a season ends up ordered 1, 10, 11, 2, 3.
      AssertBefore("Episode 2", "Episode 10");
      AssertBefore("file1", "file10");
      AssertBefore("Season 9", "Season 100");
    }

    [Test]
    public void TestEqualNumbersWithLeadingZerosSortLongestFirst()
    {
      // Value decides first, so "x7" and "x007" are not ordered by their characters. Whether they are
      // equal is a different question, and it was not guessed: CompareStringEx with
      // SORT_DIGITS_AS_NUMBERS puts the longer run first, so the portable comparer does too.
      AssertBefore("x007", "x7");
      AssertBefore("x07", "x7");
      AssertBefore("000", "0");
      AssertBefore("x7", "x8");
      AssertEqual("0", "0");
    }

    [Test]
    public void TestNumbersLongerThanAnyIntegerType()
    {
      // Comparing by value without parsing means a runaway digit run cannot overflow.
      AssertBefore("id999999999999999999999999", "id1000000000000000000000000");
    }

    [Test]
    public void TestNumberBoundaryDoesNotSkipDigits()
    {
      // "a1b" against "a12": digit runs "1" and "12" compare by value first, so the shorter number
      // loses before the trailing text is ever looked at.
      AssertBefore("a1b", "a12");
      AssertBefore("a1b2", "a12");
    }

    #endregion

    #region Text and case tests

    [Test]
    public void TestIsCaseInsensitive()
    {
      AssertEqual("abc", "ABC");
      AssertEqual("Episode 2", "EPISODE 2");
    }

    [Test]
    public void TestTextOrder()
    {
      AssertBefore("aardvark", "zebra");
      AssertBefore("abc", "abcd");
      AssertBefore("", "a");
    }

    [Test]
    public void TestIgnoreCaseCanBeTurnedOff()
    {
      var caseSensitive = new NaturalStringComparer(CultureInfo.InvariantCulture, false, false);
      // Only "case matters" is asserted, not which case wins: the direction belongs to the culture's
      // collation, and ICU (managed) and CompareStringEx (Win32) do not agree on it. The
      // case-insensitive configuration used by NOCASE is covered by TestIsCaseInsensitive.
      Assert.That(caseSensitive.Compare("abc", "ABC"), Is.Not.EqualTo(0));
      Assert.That(caseSensitive.Compare("ABC", "abc"), Is.Not.EqualTo(0));
      Assert.That(Math.Sign(caseSensitive.Compare("abc", "ABC")),
          Is.EqualTo(-Math.Sign(caseSensitive.Compare("ABC", "abc"))));
    }

    [Test]
    public void TestDigitsAsNumbersCanBeTurnedOff()
    {
      var textOnly = new NaturalStringComparer(CultureInfo.InvariantCulture, true, false);
      // Without numeric handling this is the ordering the comparer replaced.
      Assert.That(Sign(textOnly.Compare("Episode 10", "Episode 2")), Is.EqualTo(-1));
    }

    #endregion

    #region Degenerate input

    [Test]
    public void TestNullIsTheEmptyString()
    {
      Assert.That(NoCase.Compare(null, null), Is.EqualTo(0));
      Assert.That(NoCase.Compare(null, ""), Is.EqualTo(0));
      Assert.That(Sign(NoCase.Compare(null, "a")), Is.EqualTo(-1));
    }

    [Test]
    public void TestCultureIsRequired()
    {
      Assert.Throws<ArgumentNullException>(() => new NaturalStringComparer(null, true, true));
    }

    #endregion

    #region Agreement with the Windows implementation

    /// <summary>
    /// The comparisons a media library actually makes, in the collation SQLite registers. On Windows
    /// <see cref="LexicographicComparer"/> answers with <c>CompareStringEx</c>; elsewhere it answers
    /// with the comparer under test, and then this test compares the class with itself. That is still
    /// worth running: it fails if the delegation is dropped and the flags stop reaching the portable
    /// path.
    /// </summary>
    [Test]
    public void TestAgreesWithWindowsCompareStringExOnLibraryNames()
    {
      var windows = new LexicographicComparer(
          CultureInfo.InvariantCulture,
          LexicographicComparer.CmpFlags.LinguisticIgnorecase |
          LexicographicComparer.CmpFlags.NormLinguisticCasing |
          LexicographicComparer.CmpFlags.SortDigitsasnumbers);

      // Two strings per line: the pair to compare, in the order the test expects them to sort.
      string[] pairs =
      {
        "Episode 2", "Episode 10",
        "abc", "ABC",
        "THE BEATLES", "The Beatles",
        "x007", "x7",
        "x7", "x007",
        "a12", "a1b2",
        "Movie", "Movie 2",
        "aardvark", "zebra",
        "", "a"
      };

      for (int i = 0; i < pairs.Length; i += 2)
      {
        string left = pairs[i];
        string right = pairs[i + 1];
        Assert.That(Sign(windows.Compare(left, right)),
            Is.EqualTo(Sign(NoCase.Compare(left, right))),
            "Windows and the portable collation disagree about \"{0}\" vs \"{1}\"", left, right);
      }
    }

    #endregion
  }
}
