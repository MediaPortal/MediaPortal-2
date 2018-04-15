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

namespace MediaPortal.UiComponents.Diagnostics.Tools
{
  public class FileSorter
  {
    #region Public Methods

    public static List<FileInfo> SortByLastWriteTime(string path, string filter)
    {
      FileInfo[] tFiles = new DirectoryInfo(path).GetFiles(filter);
      List<FileInfo> tReturn = tFiles.ToList();
      tReturn.Sort(new LastWriteTimeComparer());
      return tReturn;
    }

    #endregion Public Methods

    #region Private Classes

    private class LastWriteTimeComparer : IComparer<FileInfo>
    {
      #region Public Methods

      public int Compare(FileInfo f1, FileInfo f2)
      {
        return DateTime.Compare(f2.LastWriteTime, f1.LastWriteTime);
      }

      #endregion Public Methods
    }

    #endregion Private Classes
  }
}
