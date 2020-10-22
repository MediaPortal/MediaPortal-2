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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Collections
{
  public class StringList : List<string>
  {
    public StringList()
    {
    }

    public StringList(string createStr)
    {
      LoadFromString(createStr);
    }

    public StringList(IEnumerable<string> createList)
    {
      AddRange(createList);
    }

    public void LoadFromString(string strList)
    {
      int startIndex = 0;
      while (startIndex < strList.Length)
      {
        // find the start index of this token
        while (startIndex < strList.Length && strList[startIndex] == '|')
          startIndex++;

        // figure it's length
        int len = 0;
        while (startIndex + len < strList.Length && strList[startIndex + len] != '|')
          len++;

        // store the token
        string token = strList.Substring(startIndex, len).Trim();
        if (startIndex < strList.Length && token.Length > 0)
          Add(token);

        startIndex += len;
      }
    }

    public override string ToString()
    {
      string rtn = "";

      if (this.Count > 0)
        rtn += "|";

      foreach (string currItem in this)
      {
        rtn += currItem;
        rtn += "|";
      }

      return rtn;
    }

    public string ToPrettyString()
    {
      return ToPrettyString(this.Count);
    }

    public string ToPrettyString(int max)
    {
      if (this.Count == 0 || max <= 0)
        return "";

      StringBuilder prettyStr = new StringBuilder("");

      int limit = max;
      if (limit > this.Count)
        limit = this.Count;

      for (int i = 0; i < limit; i++)
      {
        prettyStr.Append(this[i] + ", ");
      }
      prettyStr.Remove(prettyStr.Length - 2, 2);

      return prettyStr.ToString();
    }

    public override bool Equals(System.Object obj)
    {
      if (obj == null)
      {
        return false;
      }

      StringList p = obj as StringList;
      if ((System.Object)p == null)
      {
        return false;
      }

      return p.ToString() == ToString();
    }

    public bool Equals(StringList p)
    {
      if ((object)p == null)
      {
        return false;
      }
      else
      {
        return p.ToString() == ToString();
      }
    }

    public override int GetHashCode()
    {
      return ToString().GetHashCode();
    }
  }
}
