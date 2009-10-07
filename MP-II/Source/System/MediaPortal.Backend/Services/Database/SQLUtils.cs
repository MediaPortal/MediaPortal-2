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
using System.Text;

namespace MediaPortal.Services.Database
{
  public class SqlUtils
  {
    public static string ToSQLIdentifier(string name)
    {
      StringBuilder result = new StringBuilder(name.Length);
      if (name.Length == 0)
        return null;
      if (Char.IsLetter(name[0]))
        result.Append(name[0]); // First character must be a letter
      for (int i = 1; i < name.Length; i++)
      {
        char c = name[i];
        if (c >= 'a' && c <= 'z' ||
            c >= 'A' && c <= 'A' ||
            Char.IsDigit(c) ||
            c == '_')
          result.Append(c);
        else
          result.Append('_');
      }
      return result.ToString().ToUpperInvariant();
    }

    public static string GetSQLType(Type dotNetType)
    {
      if (dotNetType == typeof(DateTime))
        return "TIMESTAMP";
      if (dotNetType == typeof(Char))
        return "CHAR(1)";
      if (dotNetType == typeof(Boolean))
        return "CHAR(1)";
      if (dotNetType == typeof(Single))
        return "FLOAT";
      if (dotNetType == typeof(Double))
        return "DOUBLE PRECISION";
      if (dotNetType == typeof(SByte) || dotNetType == typeof(Byte) || dotNetType == typeof(Int16))
        return "SMALLINT";
      if (dotNetType == typeof(UInt16) || dotNetType == typeof(Int32))
        return "INTEGER";
      if (dotNetType == typeof(UInt32) || dotNetType == typeof(Int64))
        return "BIGINT";
      return null;
    }

    public static string GetSQLStringType(int maxNumChars)
    {
      return "NVARCHAR(" + maxNumChars + ")";
    }

    public static string LikeEscape(string str, char escapeChar)
    {
      return str.Replace(escapeChar.ToString(), escapeChar.ToString() + escapeChar).
          Replace("%", escapeChar + "%").
          Replace("_", escapeChar + "_");
    }
  }
}