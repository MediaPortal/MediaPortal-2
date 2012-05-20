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