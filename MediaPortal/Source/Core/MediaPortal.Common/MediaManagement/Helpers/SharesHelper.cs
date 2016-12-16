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

using System.Collections.Generic;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  public static class SharesHelper
  {
    /// <summary>
    /// Extension method which is added on <see cref="IEnumerable{Share}"/> to find that share in the given enumeration
    /// which best matches the given <paramref name="path"/>.
    /// </summary>
    /// <remarks>
    /// If there are shares where one share path contains the path of another share in the given <paramref name="shares"/> enumeration,
    /// the algorithm will always find the share whose path is as close to the given <paramref name="path"/>. If more than one share match best,
    /// the first best matching share is returned.
    /// </remarks>
    /// <param name="shares">Enumeration of shares to search through.</param>
    /// <param name="path">Path to find a share for.</param>
    /// <returns>Share which best matches the given <paramref name="path"/>, if one exists. Else, <c>null</c> will be returned.</returns>
    public static Share BestContainingPath(this IEnumerable<Share> shares, ResourcePath path)
    {
      if (path == null)
        return null;
      int bestMatchPathLength = int.MaxValue;
      Share bestMatchShare = null;
      foreach (Share share in shares)
      {
        ResourcePath currentSharePath = share.BaseResourcePath;
        if (!currentSharePath.IsSameOrParentOf(path))
          // The path is not located in the current share
          continue;
        if (bestMatchShare == null)
        {
          bestMatchShare = share;
          bestMatchPathLength = currentSharePath.Serialize().Length;
          continue;
        }
        // We want to find a share which is as close as possible to the given path
        int currentSharePathLength = currentSharePath.Serialize().Length;
        if (bestMatchPathLength >= currentSharePathLength)
          continue;
        bestMatchShare = share;
        bestMatchPathLength = currentSharePathLength;
      }
      return bestMatchShare;
    }
  }
}