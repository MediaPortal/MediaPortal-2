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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MediaPortal.Tools.StringManager.SolutionFiles
{
  class FileList
  {
    string _startSearch;
    string _itemSearch;
    char _delimChar;

    public FileList(string startSearch, string itemSearch, char delimChar)
    {
      _startSearch = startSearch;
      _itemSearch = itemSearch;
      _delimChar = delimChar;
    }

    public string[] GetList(string filename)
    {
      StreamReader fileStream = new StreamReader(filename);
      string file = fileStream.ReadToEnd();

      if (file == null)
        return null;
      
      int startPos = 0;
      int pos;
      int count = 0;
      ArrayList listArray = new ArrayList();

      while ((pos = file.IndexOf(_startSearch, startPos)) != -1)
      {
        int nameStart = file.IndexOf(_delimChar, pos);
        nameStart++;
        int nameEnd = file.IndexOf(_delimChar, nameStart);
        string listItem = file.Substring(nameStart, nameEnd - nameStart);

        if (listItem.IndexOf(_itemSearch) != -1)
        {
          listItem = listItem.Trim(_delimChar);
          listItem = listItem.Trim('"', ' ');
          listArray.Add(listItem);
          count++;
        }
        startPos = nameEnd;
      }

      string[] list = new string[count];

      for (int i = 0; i < count; i++)
      {
        list[i] = (string)listArray[i];
      }

      return list;
    }
  }
}
