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

using System.Data.SQLite;
using System.Globalization;

namespace MediaPortal.Database.SQLite
{
  /// <summary>
  /// This class is used to add case insensitive but culture sensitive sorting
  /// to the SQLiteDatabase. It has to be registered as SQLiteFunction. It overrides
  /// the SQLite inbuilt NOCASE collation sequence in order to avoid errors when the
  /// MP2 database is opened by a third party database tool. The only impact on third
  /// party database tools that do not register this collation sequence is that the
  /// sort order within these tools is different from the sort order in MP2.
  /// </summary>
  [SQLiteFunction(FuncType = FunctionType.Collation, Name = "NOCASE")]
  public class SQLiteCultureSensitiveCollation : SQLiteFunction
  {

    protected static readonly LexicographicComparer COMPARER = new LexicographicComparer
    (
      CultureInfo.InvariantCulture,
      LexicographicComparer.CmpFlags.LinguisticIgnorecase |
      LexicographicComparer.CmpFlags.NormLinguisticCasing |
      LexicographicComparer.CmpFlags.SortDigitsasnumbers
    );
    
    public override int Compare(string x, string y)
    {
      return COMPARER.Compare(x, y);
    }
  }
}
