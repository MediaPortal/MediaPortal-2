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

using System.Collections.Generic;
using System.IO;

namespace MediaPortal.Utilities.FileSystem
{
  /// <summary>
  /// Unfortunately doen't classes <see cref="FileInfo"/> and <see cref="DirectoryInfo"/> implement
  /// the methods <see cref="object.Equals(object)"/> and <see cref="object.GetHashCode()"/> in a
  /// sensible way. This class is a comparer for <see cref="FileSystemInfo"/> descendants to be used
  /// in lists, dictionaries and for static comparisons of <see cref="FileSystemInfo"/> instances.
  /// It compares those instances by their path in lower case, so it is Windows specific!
  /// </summary>
  public class FileSystemInfoComparer<T> : IEqualityComparer<T> where T : FileSystemInfo
  {
    public bool Equals(T x, T y)
    {
      return FileUtils.PathEquals(x.FullName, y.FullName);
    }

    public int GetHashCode(T obj)
    {
      return obj.FullName.GetHashCode();
    }
  }

  public class DirectoryInfoComparer : FileSystemInfoComparer<DirectoryInfo> { }

  public class FileInfoComparer : FileSystemInfoComparer<FileInfo> { }
}
