#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Common.FanArt
{
  public static class LocalFanartHelper
  {
    public static readonly ICollection<String> EXTENSIONS = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".png", ".tbn" };

    public static readonly string[] THUMB_FILENAMES = new[] { "thumb" };
    public static readonly string[] POSTER_FILENAMES = new[] { "poster", "folder", "cover" };
    public static readonly string[] LOGO_FILENAMES = new[] { "logo" };
    public static readonly string[] CLEARART_FILENAMES = new[] { "clearart" };
    public static readonly string[] DISCART_FILENAMES = new[] { "cdart", "discart", "disc" };
    public static readonly string[] BANNER_FILENAMES = new[] { "banner" };
    public static readonly string[] BACKDROP_FILENAMES = new[] { "backdrop", "fanart" };

    /// <summary>
    /// Returns a list of ResourcePaths to all potential FanArt files in a given directory
    /// </summary>
    /// <param name="directoryAccessor">ResourceAccessor pointing to the directory where FanArt files should be searched</param>
    /// <returns>List of ResourcePaths to potential FanArt files</returns>
    public static List<ResourcePath> GetPotentialFanArtFiles(IFileSystemResourceAccessor directoryAccessor)
    {
      var result = new List<ResourcePath>();
      if (directoryAccessor.IsFile)
        return result;
      foreach (var file in directoryAccessor.GetFiles())
        using (file)
        {
          var path = file.CanonicalLocalResourcePath;
          if (EXTENSIONS.Contains(ResourcePathHelper.GetExtension(path.ToString())))
            result.Add(path);
        }
      return result;
    }

    public static List<ResourcePath> FilterPotentialFanArtFilesByName(IEnumerable<ResourcePath> potentialFanArtFiles, params string[] filenames)
    {
      return FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles, filenames, null);
    }

    public static List<ResourcePath> FilterPotentialFanArtFilesByName(IEnumerable<ResourcePath> potentialFanArtFiles, IEnumerable<string> filenames)
    {
      return FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles, filenames, null);
    }

    public static List<ResourcePath> FilterPotentialFanArtFilesByPrefix(IEnumerable<ResourcePath> potentialFanArtFiles, params string[] prefixes)
    {
      return FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles, null, prefixes);
    }

    public static List<ResourcePath> FilterPotentialFanArtFilesByPrefix(IEnumerable<ResourcePath> potentialFanArtFiles, IEnumerable<string> prefixes)
    {
      return FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles, null, prefixes);
    }

    /// <summary>
    /// Returns a list of ResourcePaths to all potential FanArt files in a given directory
    /// </summary>
    /// <param name="directoryAccessor">ResourceAccessor pointing to the directory where FanArt files should be searched</param>
    /// <returns>List of ResourcePaths to potential FanArt files</returns>
    public static List<ResourcePath> FilterPotentialFanArtFilesByNameOrPrefix(IEnumerable<ResourcePath> potentialFanArtFiles, string filename, string prefix)
    {
      return FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles, new[] { filename }, new[] { prefix });
    }

    /// <summary>
    /// Returns a list of ResourcePaths to all potential FanArt files in a given directory
    /// </summary>
    /// <param name="directoryAccessor">ResourceAccessor pointing to the directory where FanArt files should be searched</param>
    /// <returns>List of ResourcePaths to potential FanArt files</returns>
    public static List<ResourcePath> FilterPotentialFanArtFilesByNameOrPrefix(IEnumerable<ResourcePath> potentialFanArtFiles,
      IEnumerable<string> filenames, IEnumerable<string> prefixes)
    {
      var result = new List<ResourcePath>();
      foreach (var potentialFanArtFile in potentialFanArtFiles)
      {
        var potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant();
        if ((filenames != null && filenames.Contains(potentialFanArtFileNameWithoutExtension)) ||
          (prefixes != null && prefixes.Any(p => potentialFanArtFileNameWithoutExtension.StartsWith(p))))
          result.Add(potentialFanArtFile);
      }
      return result;
    }

    /// <summary>
    /// Determines if a folder is a disc folder for albums consisting of multiple discs
    /// </summary>
    /// <param name="album">The name of the album</param>
    /// <param name="albumFolder">The folder to check</param>
    /// <returns></returns>
    public static bool IsDiscFolder(string album, string albumFolder)
    {
      int discNo = 0;
      int albumNo = 0;
      if (album != null &&
        (albumFolder.StartsWith("CD", StringComparison.InvariantCultureIgnoreCase) && !album.StartsWith("CD", StringComparison.InvariantCultureIgnoreCase)) ||
        (albumFolder.StartsWith("Disc", StringComparison.InvariantCultureIgnoreCase) && !album.StartsWith("Disc", StringComparison.InvariantCultureIgnoreCase)) ||
        (int.TryParse(albumFolder, out discNo) && int.TryParse(album, out albumNo) && discNo != albumNo))
      {
        return true;
      }
      return false;
    }
  }
}
