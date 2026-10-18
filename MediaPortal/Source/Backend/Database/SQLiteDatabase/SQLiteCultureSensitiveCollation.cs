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

namespace MediaPortal.Database.SQLite
{
  /// <summary>
  /// This class is used to add case insensitive but culture sensitive sorting
  /// to the SQLiteDatabase. It overrides the SQLite inbuilt NOCASE collation
  /// sequence in order to avoid errors when the MP2 database is opened by a
  /// third party database tool. The only impact on third party database tools
  /// that do not register this collation sequence is that the sort order within
  /// these tools is different from the sort order in MP2.
  /// </summary>
  /// <remarks>
  /// Under System.Data.SQLite this was a <c>[SQLiteFunction(FuncType = Collation, Name = "NOCASE")]</c>
  /// class, and the driver found it by scanning assemblies each time it opened a connection.
  /// Microsoft.Data.Sqlite has an explicit call instead - <c>SqliteConnection.CreateCollation</c> -
  /// which the provider makes in <c>CreateOpenAndInitializeConnection</c> for every connection it
  /// opens, because registrations are per connection: measured, another connection to the same
  /// database does not see them.
  /// </remarks>
  public class SQLiteCultureSensitiveCollation
  {
    /// <summary>
    /// Name of the collation this class implements. It deliberately reuses SQLite's built-in name, so
    /// that a database file whose schema says <c>COLLATE NOCASE</c> - which is nearly every table of
    /// MP2's - resolves to this comparison instead of SQLite's ASCII-only one.
    /// </summary>
    public const string CollationName = "NOCASE";

    protected static readonly LexicographicComparer COMPARER = new LexicographicComparer
    (
      CultureInfo.InvariantCulture,
      LexicographicComparer.CmpFlags.LinguisticIgnorecase |
      LexicographicComparer.CmpFlags.NormLinguisticCasing |
      LexicographicComparer.CmpFlags.SortDigitsasnumbers
    );

    /// <summary>
    /// The comparison SQLite calls. SQLite itself never passes null here (a NULL sorts without a
    /// collation comparison), but the delegate is public, so it does not throw on them.
    /// </summary>
    public static int Compare(string x, string y)
    {
      if (x == null || y == null)
        return string.Equals(x, y, StringComparison.Ordinal) ? 0 : (x == null ? -1 : 1);
      return COMPARER.Compare(x, y);
    }
  }
}
