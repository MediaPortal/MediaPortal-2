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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaPortal.Utilities.DB
{
  public class InstructionList : IEnumerable<string>
  {
    protected IList<string> _instructions = new List<string>();

    public InstructionList(string script)
    {
      Parse(script);
    }

    public InstructionList(TextReader reader)
    {
      Parse(reader.ReadToEnd());
    }

    public InstructionList() { }

    public void AddInstruction(string instruction)
    {
      instruction = instruction.Replace("\r\n", " ").Replace('\n', ' ');
      if (string.IsNullOrEmpty(instruction))
        return;
      if (instruction.StartsWith("--"))
        return;
      _instructions.Add(instruction);
    }

    protected void Parse(string script)
    {
      string[] instrList = RemoveComments(script).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
      foreach (string instr in instrList)
        AddInstruction(instr.Trim());
    }

    // This method was taken from the Firebird .net provider, class FbScript.
    /// <summary>
    /// Removes from the SQL code all comments of the type <c>/*...*/</c> or <c>--</c>
    /// </summary>
    /// <param name="source">The string containing the original SQL code.</param>
    /// <returns>A string containing the SQL code without comments.</returns>
    protected static string RemoveComments(string source)
    {
      int i = 0;
      int length = source.Length;
      StringBuilder result = new StringBuilder();
      bool insideComment = false;
      bool insideLiteral = false;

      while (i < length)
      {
        if (insideLiteral)
        {
          result.Append(source[i]);

          if (source[i] == '\'')
            insideLiteral = false;
        }
        else if (insideComment)
        {
          if (source[i] == '*')
            if ((i < length - 1) && (source[i + 1] == '/'))
            {
              i++;
              insideComment = false;
            }
        }
        else if ((source[i] == '\'') && (i < length - 1))
        {
          result.Append(source[i]);
          insideLiteral = true;
        }
        else if ((source[i] == '/') && (i < length - 1) && (source[i + 1] == '*'))
        {
          i++;
          insideComment = true;
        }
        else if ((source[i] == '-' && (i < length - 1) && source[i + 1] == '-'))
        {
          i++;
          while (source[i] != '\n')
            i++;
          i--;
        }
        else
          result.Append(source[i]);

        i++;
      }
      return result.ToString();
    }

    #region IEnumerable implementation

    public IEnumerator<string> GetEnumerator()
    {
      return _instructions.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    #endregion
  }
}