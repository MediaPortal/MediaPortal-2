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
using System.Text;

namespace MediaPortal.Utilities.DB
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
            c >= 'A' && c <= 'Z' ||
            Char.IsDigit(c) || c == '_')
          result.Append(c);
        else
          result.Append('_');
      }
      return result.ToString().ToUpperInvariant();
    }

    /// <summary>
    /// Special characters in an SQL LIKE expression which need to be escaped.
    /// </summary>
    public static char[] LIKE_SPECIAL_CHARACTERS = new char[]
      {
          '%', '_'
      };

    /// <summary>
    /// Escapes all characters in the given string <paramref name="str"/> which are special characters in SQL LIKE expressions.
    /// </summary>
    /// <param name="str">String to escape.</param>
    /// <param name="escapeChar">Character to use as escape character. The character will be put in front of each special
    /// character in the string.</param>
    /// <returns>Escaped string.</returns>
    public static string LikeEscape(string str, char escapeChar)
    {
      return StringUtils.Escape(str, LIKE_SPECIAL_CHARACTERS, escapeChar);
    }

    /// <summary>
    /// Formats a SQL command for debug output.
    /// </summary>
    /// <param name="sqlCommand">SQL command.</param>
    /// <returns>Formatted command text.</returns>
    public static string FormatSQL(String sqlCommand)
    {
      String formattedSQL = sqlCommand;
      formattedSQL = formattedSQL.Replace("SELECT", "SELECT\r\n  ");
      formattedSQL = formattedSQL.Replace("CREATE", "CREATE\r\n");
      formattedSQL = formattedSQL.Replace(" FROM", "\r\nFROM");
      formattedSQL = formattedSQL.Replace(",", ",\r\n  ");
      formattedSQL = formattedSQL.Replace(" LEFT", "\r\n  LEFT");
      formattedSQL = formattedSQL.Replace(" INNER", "\r\n  INNER");
      formattedSQL = formattedSQL.Replace("WHERE", "\r\nWHERE\r\n  ");
      formattedSQL = formattedSQL.Replace("GROUP BY", "\r\nGROUP BY\r\n  ");
      formattedSQL = formattedSQL.Replace(" AND", "\r\n  AND");
      formattedSQL = formattedSQL.Replace(" OR", "\r\n  OR");
      formattedSQL = formattedSQL.Replace("(", "(\r\n   ");
      formattedSQL = formattedSQL.Replace(")", "\r\n)");
      formattedSQL = "\r\n" + formattedSQL; // always start logging inside new line, makes copy&paste easier.
      return formattedSQL;
    }

    /// <summary>
    /// Formats a ParameterCollection for debug output.
    /// </summary>
    /// <param name="parameterCollection">ParameterCollection</param>
    /// <returns>Formatted text</returns>
    public static string FormatSQLParameters(System.Data.IDataParameterCollection parameterCollection)
    {
      if (parameterCollection == null)
        return String.Empty;

      StringBuilder sb = new StringBuilder();
      foreach (System.Data.IDbDataParameter param in parameterCollection)
      {
        String quoting = "";
        String pv = "[NULL]";
        if (param.Value != null)
          pv = param.Value.ToString();

        if (param.DbType == System.Data.DbType.String)
          quoting = "'";

        sb.AppendFormat("\r\n\"{0}\" [{1}]: {3}{2}{3}", param.ParameterName, param.DbType, pv, quoting);
      }
      return sb.ToString();
    }
  }
}