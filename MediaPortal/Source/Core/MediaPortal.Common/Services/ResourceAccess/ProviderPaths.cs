#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Common.Services.ResourceAccess
{
  public class ProviderPaths
  {
    public static string ExpandPath(string rootPath, string path)
    {
      while (path.StartsWith("."))
      {
        if (path.StartsWith("./"))
          path = path.Substring(2);
        if (path.StartsWith("../"))
        {
          rootPath = GetDirectoryName(rootPath);
          if (rootPath == null)
            throw new InvalidDataException("Paths '{0}' and '{1}' cannot be concatenated", rootPath, path);
        }
      }
      return path.StartsWith("/") ? path : (StringUtils.CheckSuffix(rootPath, "/") + StringUtils.RemovePrefixIfPresent(path, "/"));
    }

    public static string GetDirectoryName(string path)
    {
      path = StringUtils.RemoveSuffixIfPresent(path, "/");
      int index = path.LastIndexOf('/');
      return path.Substring(0, index + 1);
    }

    public static string GetFileName(string path)
    {
      if (string.IsNullOrEmpty(path))
        return null;
      int sepIndex = path.LastIndexOf('/');
      return path.Substring(sepIndex + 1);
    }
  }
}