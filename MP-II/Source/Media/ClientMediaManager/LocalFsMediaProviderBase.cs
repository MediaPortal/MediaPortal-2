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

namespace MediaPortal.Media.ClientMediaManager
{
  /// <summary>
  /// Base methods of the local filesystem media provider which are needed in the media manager.
  /// </summary>
  public class LocalFsMediaProviderBase
  {
    #region Public methods

    public static string ToDosPath(string providerPath)
    {
      if (string.IsNullOrEmpty(providerPath) || providerPath == "/" || !providerPath.StartsWith("/"))
        return string.Empty;
      providerPath = providerPath.Substring(1);
      return providerPath.Replace('/', Path.DirectorySeparatorChar);
    }

    public static string ToProviderPath(string dosPath)
    {
      return "/" + dosPath.Replace(Path.DirectorySeparatorChar, '/');
    }

    protected static ICollection<string> ConcatPaths(string rootPath,
        IEnumerable<string> namesWithPathPrefix, bool isDirectory)
    {
      if (!rootPath.EndsWith("/"))
        rootPath = rootPath + "/";
      ICollection<string> result = new List<string>();
      foreach (string file in namesWithPathPrefix)
        result.Add(rootPath + Path.GetFileName(file) + (isDirectory ? "/" : string.Empty));
      return result;
    }

    #endregion
  }
}
