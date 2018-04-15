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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MediaPortal.Common;
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
  /// <item><term>%GUID%</term><description>Guid</description></item>
  /// <item><term>%STRING([N])%</term><description>string with maximum length of N unicode characters</description></item>
  /// <item><term>%STRING_FIXED([N])%</term><description>String with a fixed length of N characters</description></item>
  /// <item><term>%BINARY%</term><description>Binary array (<c>byte[]</c>)</description></item>
  /// </list>
  /// There are also general placeholders:
  /// <list type="table">
  /// <listheader><term>Placeholder</term><description>Replacement</description></listheader>  
  /// <item><term>%CREATE_NEW_GUID%</term><description>Pseudo constant attribute which will be replaced by a new GUID in the form <c>"920F3A24-292E-48ae-AAC3-C66E35AFA22A"</c>.</description></item>
  /// <item><term>%GET_LAST_GUID%</term><description>Pseudo constant attribute which will be replaced by the last generated GUID constant.</description></item>
  /// </list>
  /// </remarks>
  public class SqlScriptPreprocessor : StringReader
  {
    public SqlScriptPreprocessor(string scriptFilePath) : base(PreprocessScript(scriptFilePath)) { }
    public SqlScriptPreprocessor(TextReader reader) : base(PreprocessScript(reader)) { }

    protected static string PreprocessScript(string scriptFilePath)
    {
      using (StreamReader reader = new StreamReader(new FileStream(scriptFilePath, FileMode.Open, FileAccess.Read)))
        return PreprocessScript(reader);
    }

    protected static string PreprocessScript(TextReader reader)
    {
      string orig = reader.ReadToEnd();
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      return Preprocess(orig, database);
    }

    protected static string GetType(Type type, ISQLDatabase database)
    {
      string result = database.GetSQLType(type);
      if (result == null)
        throw new InvalidDataException(".net type '{0}' isn't supported by the installed database '{1}', version {2}", type.Name, database.DatabaseType, database.DatabaseVersion);
      return result;
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
          Replace("%BIGINT%", GetType(typeof(Int64), database)).
          Replace("%GUID%", GetType(typeof(Guid), database)).
          Replace("%BINARY%", GetType(typeof(byte[]), database));

      // For extended replacements: First collect all patterns to be replaced...
      IDictionary<string, string> replacements = new Dictionary<string, string>();

      string interimStr = result.ToString();

      // %STRING([N])%
      Match match = new Regex(@"%STRING\((\d*)\)%").Match(interimStr);
      while (match.Success)
      {
        string pattern = match.Value;
        if (!replacements.ContainsKey(pattern))
        {
          uint length = uint.Parse(match.Groups[1].Value);
          replacements.Add(pattern, database.GetSQLVarLengthStringType(length));
        }
        match = match.NextMatch();
      }

      // %STRING_FIXED([N])%
      match = new Regex(@"%STRING_FIXED\((\d*)\)%").Match(interimStr);
      while (match.Success)
      {
        string pattern = match.Value;
        if (!replacements.ContainsKey(pattern))
        {
          uint length = uint.Parse(match.Groups[1].Value);
          replacements.Add(pattern, database.GetSQLFixedLengthStringType(length));
        }
        match = match.NextMatch();
      }

      // %CREATE_NEW_GUID% / %GET_LAST_GUID%
      string lastGuid = null;
      match = new Regex(@"(%CREATE_NEW_GUID%)|(%GET_LAST_GUID%)").Match(interimStr);
      while (match.Success)
      {
        Group g;
        if ((g = match.Groups[1]).Success) // %CREATE_NEW_GUID% matched
          result.Replace("%CREATE_NEW_GUID%", lastGuid = Guid.NewGuid().ToString("B"), g.Index, g.Length);
        else if ((g = match.Groups[2]).Success) // %GET_LAST_GUID% matched
          result.Replace("%GET_LAST_GUID%", lastGuid, g.Index, g.Length);
        match = match.NextMatch();
      }

      // ... then do the actual replacements
      result = replacements.Aggregate(result, (current, replacement) => current.Replace(replacement.Key, replacement.Value));
      return result.ToString();
    }
  }
}
