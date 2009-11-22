#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MediaPortal.Core;
using MediaPortal.Backend.Database;
using InvalidDataException=MediaPortal.Utilities.Exceptions.InvalidDataException;

namespace MediaPortal.Backend.Services.Database
{
  /// <summary>
  /// Preprocessor for SQL scripts.
  /// </summary>
  /// <remarks>
  /// Here is a table of all supported datatype placeholders:
  /// <list type="table">
  /// <listheader><term>Datatype placeholder</term><description>.net type</description></listheader>  
  /// <item><term>%TIMESTAMP%</term><description>DateTime</description></item>
  /// <item><term>%CHAR%</term><description>Char</description></item>
  /// <item><term>%BOOLEAN%</term><description>Boolean</description></item>
  /// <item><term>%SINGLE%</term><description>Single</description></item>
  /// <item><term>%DOUBLE%</term><description>Double</description></item>
  /// <item><term>%SMALLINT%</term><description>SByte, Byte, Int16</description></item>
  /// <item><term>%INTEGER%</term><description>UInt16, Int32</description></item>
  /// <item><term>%BIGINT%</term><description>UInt32, Int64</description></item>
  /// <item><term>%STRING([N])%</term><description>string with maximum length of N unicode characters</description></item>
  /// <item><term>%STRING_FIXED([N])%</term><description>string with a fixed length of N characters</description></item>
  /// </list>
  /// There are also general placeholders:
  /// <list type="table">
  /// <listheader><term>Placeholder</term><description>Replacement</description></listheader>  
  /// <item><term>%CREATE_SEQUENCE([Name])%</term><description>Command creating a sequence.</description></item>
  /// <item><term>%SELECT_SEQUENCE_NEXTVAL([Name])%</term><description>Pseudo select attribute selecting the next value from the sequence.</description></item>
  /// <item><term>%SELECT_SEQUENCE_CURRVAL([Name])%</term><description>Pseudo select attribute selecting the current value from the sequence.</description></item>
  /// </list>
  /// </remarks>
  public class SqlScriptPreprocessor : StringReader
  {
    public SqlScriptPreprocessor(string scriptFilePath) : base(PreprocessScript(scriptFilePath)) { }
    public SqlScriptPreprocessor(TextReader reader) : base(PreprocessScript(reader)) { }

    protected static string PreprocessScript(string scriptFilePath)
    {
      StreamReader reader = new StreamReader(new FileStream(scriptFilePath, FileMode.Open));
      return PreprocessScript(reader);
    }

    protected static string PreprocessScript(TextReader reader)
    {
      string orig = reader.ReadToEnd();
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      return Preprocess(orig, database);
    }

    protected static string GetType(Type type, ISQLDatabase database)
    {
      string result = database.GetSQLType(type);
      if (result == null)
        throw new InvalidDataException(".net type '{0}' isn't supported by the installed database '{1}', version {2}", type.Name, database.DatabaseType, database.DatabaseVersion);
      return result;
    }

    protected static string GetStringPatternReplacement(string stringPattern, ISQLDatabase database)
    {
      int start = stringPattern.IndexOf('(');
      int end = stringPattern.IndexOf(')');
      if (start == -1 || end <= start)
        throw new InvalidDataException("Malformed string pattern '{0}'", stringPattern);
      string lstr = stringPattern.Substring(start + 1, end - start - 1).Trim();
      uint length;
      if (!uint.TryParse(lstr, out length))
        throw new InvalidDataException("Invalid length in string pattern '{0}'", stringPattern);
      return database.GetSQLVarLengthStringType(length);
    }

    protected static string GetFixedStringPatternReplacement(string stringPattern, ISQLDatabase database)
    {
      int start = stringPattern.IndexOf('(');
      int end = stringPattern.IndexOf(')');
      if (start == -1 || end <= start)
        throw new InvalidDataException("Malformed fixed string pattern '{0}'", stringPattern);
      string lStr = stringPattern.Substring(start + 1, end - start - 1).Trim();
      uint length;
      if (!uint.TryParse(lStr, out length))
        throw new InvalidDataException("Invalid length in fixed string pattern '{0}'", stringPattern);
      return database.GetSQLFixedLengthStringType(length);
    }

    protected static string GetCreateSequencePatternReplacement(string stringPattern, ISQLDatabase database)
    {
      int start = stringPattern.IndexOf('(');
      int end = stringPattern.IndexOf(')');
      if (start == -1 || end <= start)
        throw new InvalidDataException("Malformed fixed string pattern '{0}'", stringPattern);
      string name = stringPattern.Substring(start + 1, end - start - 1).Trim();
      return database.GetCreateSequenceCommand(name);
    }

    protected static string GetSelectSequenceNextValPatternReplacement(string stringPattern, ISQLDatabase database)
    {
      int start = stringPattern.IndexOf('(');
      int end = stringPattern.IndexOf(')');
      if (start == -1 || end <= start)
        throw new InvalidDataException("Malformed fixed string pattern '{0}'", stringPattern);
      string name = stringPattern.Substring(start + 1, end - start - 1).Trim();
      return database.GetSelectSequenceNextValStatement(name);
    }

    protected static string GetSelectSequenceCurrValPatternReplacement(string stringPattern, ISQLDatabase database)
    {
      int start = stringPattern.IndexOf('(');
      int end = stringPattern.IndexOf(')');
      if (start == -1 || end <= start)
        throw new InvalidDataException("Malformed fixed string pattern '{0}'", stringPattern);
      string name = stringPattern.Substring(start + 1, end - start - 1).Trim();
      return database.GetSelectSequenceCurrValStatement(name);
    }

    protected static string Preprocess(string origScript, ISQLDatabase database)
    {
      // Replace simple types
      StringBuilder result = new StringBuilder(origScript);
      result = result.Replace("%TIMESTAMP%", GetType(typeof(DateTime), database)).
          Replace("%CHAR%", GetType(typeof(Char), database)).
          Replace("%BOOLEAN%", GetType(typeof(Boolean), database)).
          Replace("%SINGLE%", GetType(typeof(Single), database)).
          Replace("%DOUBLE%", GetType(typeof(Double), database)).
          Replace("%SMALLINT%", GetType(typeof(Int16), database)).
          Replace("%INTEGER%", GetType(typeof(Int32), database)).
          Replace("%BIGINT%", GetType(typeof(Int64), database));

      // For extended replacements: First collect all patterns to be replaced...
      IDictionary<string, string> replacements = new Dictionary<string, string>();

      string interimStr = result.ToString();
      // %STRING([N])%
      Match match = new Regex(@"%STRING\(\d*\)%").Match(interimStr);
      while (match.Success)
      {
        string pattern = match.Value;
        if (!replacements.ContainsKey(pattern))
          replacements.Add(pattern, GetStringPatternReplacement(pattern, database));
        match = match.NextMatch();
      }

      // %STRING_FIXED([N])%
      match = new Regex(@"%STRING_FIXED\(\d*\)%").Match(interimStr);
      while (match.Success)
      {
        string pattern = match.Value;
        if (!replacements.ContainsKey(pattern))
          replacements.Add(pattern, GetFixedStringPatternReplacement(pattern, database));
        match = match.NextMatch();
      }

      // %CREATE_SEQUENCE([NAME])%
      match = new Regex(@"%CREATE_SEQUENCE\((\w|_)*\)%").Match(interimStr);
      while (match.Success)
      {
        string pattern = match.Value;
        if (!replacements.ContainsKey(pattern))
          replacements.Add(pattern, GetCreateSequencePatternReplacement(pattern, database));
        match = match.NextMatch();
      }

      // %SELECT_SEQUENCE_NEXTVAL([NAME])%
      match = new Regex(@"%SELECT_SEQUENCE_NEXTVAL\((\w|_)*\)%").Match(interimStr);
      while (match.Success)
      {
        string pattern = match.Value;
        if (!replacements.ContainsKey(pattern))
          replacements.Add(pattern, GetSelectSequenceNextValPatternReplacement(pattern, database));
        match = match.NextMatch();
      }

      // %SELECT_SEQUENCE_CURRVAL([NAME])%
      match = new Regex(@"%SELECT_SEQUENCE_CURRVAL\((\w|_)*\)%").Match(interimStr);
      while (match.Success)
      {
        string pattern = match.Value;
        if (!replacements.ContainsKey(pattern))
          replacements.Add(pattern, GetSelectSequenceCurrValPatternReplacement(pattern, database));
        match = match.NextMatch();
      }

      // ... then do the actual replacements
      foreach (KeyValuePair<string, string> replacement in replacements)
        result = result.Replace(replacement.Key, replacement.Value);
      return result.ToString();
    }
  }
}
