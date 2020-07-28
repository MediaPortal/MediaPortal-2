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

    public static readonly string[] THUMB_FILENAMES = new[] { "thumb", "landscape" };
    public static readonly string[] POSTER_FILENAMES = new[] { "poster", "folder", "cover", "keyart" };
    public static readonly string[] LOGO_FILENAMES = new[] { "logo", "clearlogo" };
    public static readonly string[] CLEARART_FILENAMES = new[] { "clearart" };
    public static readonly string[] DISCART_FILENAMES = new[] { "cdart", "discart", "disc" };
    public static readonly string[] BANNER_FILENAMES = new[] { "banner" };
    public static readonly string[] BACKDROP_FILENAMES = new[] { "backdrop", "fanart" };
    public static readonly string[] ARTIST_FILENAMES = new[] { "artist", "artistthumb" };
    public static readonly string[] ACTOR_FILENAMES = new[] { "actor", "actorthumb" };
    public static readonly string[] CHARACTER_FILENAMES = new[] { "character", "characterart" };

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

    /// <summary>
    /// Returns a list of ResourcePaths filtered FanArt files for a given list of potential FanArt files
    /// </summary>
    /// <param name="potentialFanArtFiles">List of potential FanArt files</param>
    /// <param name="filenames">File names to filter by</param>
    /// <returns>List of ResourcePaths to filtered FanArt files</returns>
    public static List<ResourcePath> FilterPotentialFanArtFilesByName(IEnumerable<ResourcePath> potentialFanArtFiles, params string[] filenames)
    {
      return FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles, filenames, null);
    }

    /// <summary>
    /// Returns a list of ResourcePaths filtered FanArt files for a given list of potential FanArt files
    /// </summary>
    /// <param name="potentialFanArtFiles">List of potential FanArt files</param>
    /// <param name="filenames">File names to filter by</param>
    /// <returns>List of ResourcePaths to filtered FanArt files</returns>
    public static List<ResourcePath> FilterPotentialFanArtFilesByName(IEnumerable<ResourcePath> potentialFanArtFiles, IEnumerable<string> filenames)
    {
      return FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles, filenames, null);
    }

    /// <summary>
    /// Returns a list of ResourcePaths filtered FanArt files for a given list of potential FanArt files
    /// </summary>
    /// <param name="potentialFanArtFiles">List of potential FanArt files</param>
    /// <param name="prefixes">File name prefixes to filter by</param>
    /// <returns>List of ResourcePaths to filtered FanArt files</returns>
    public static List<ResourcePath> FilterPotentialFanArtFilesByPrefix(IEnumerable<ResourcePath> potentialFanArtFiles, params string[] prefixes)
    {
      return FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles, null, prefixes);
    }

    /// <summary>
    /// Returns a list of ResourcePaths filtered FanArt files for a given list of potential FanArt files
    /// </summary>
    /// <param name="potentialFanArtFiles">List of potential FanArt files</param>
    /// <param name="prefixes">File name prefixes to filter by</param>
    /// <returns>List of ResourcePaths to filtered FanArt files</returns>
    public static List<ResourcePath> FilterPotentialFanArtFilesByPrefix(IEnumerable<ResourcePath> potentialFanArtFiles, IEnumerable<string> prefixes)
    {
      return FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles, null, prefixes);
    }

    /// <summary>
    /// Returns a list of ResourcePaths filtered FanArt files for a given list of potential FanArt files
    /// </summary>
    /// <param name="potentialFanArtFiles">List of potential FanArt files</param>
    /// <param name="filename">File name to filter by</param>
    /// <param name="prefix">File name prefix to filter by</param>
    /// <returns>List of ResourcePaths to filtered FanArt files</returns>
    public static List<ResourcePath> FilterPotentialFanArtFilesByNameOrPrefix(IEnumerable<ResourcePath> potentialFanArtFiles, string filename, string prefix)
    {
      return FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles, new[] { filename }, new[] { prefix });
    }

    /// <summary>
    /// Returns a list of ResourcePaths filtered FanArt files for a given list of potential FanArt files
    /// </summary>
    /// <param name="potentialFanArtFiles">List of potential FanArt files</param>
    /// <param name="filenames">File names to filter by</param>
    /// <param name="prefixes">File name prefixes to filter by </param>
    /// <returns>List of ResourcePaths to filtered FanArt files</returns>
    public static List<ResourcePath> FilterPotentialFanArtFilesByNameOrPrefix(IEnumerable<ResourcePath> potentialFanArtFiles,
      IEnumerable<string> filenames, IEnumerable<string> prefixes = null)
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
    /// <returns>True if folder is a disc folder</returns>
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

    /// <summary>
    /// Checks if a potential series folder has season folders
    /// </summary>
    /// <param name="seriesFolder">Potential series folder accessor</param>
    /// <param name="knownSeasonNo">Number of a known season if available</param>
    /// <returns>True if folder has season folders</returns>
    public static bool IsSeriesFolder(IFileSystemResourceAccessor seriesFolder, int? knownSeasonNo = null)
    {
      if (seriesFolder == null)
        return false;

      int maxInvalidFolders = 3;
      var seasonFolders = seriesFolder.GetChildDirectories();
      var seasonNos = seasonFolders.Select(GetSeasonFromFolder).ToList();
      var invalidSeasonCount = seasonNos.Count(s => s < 0);
      var validSeasonCount = seasonNos.Count(s => s >= 0);
      if (invalidSeasonCount <= maxInvalidFolders && validSeasonCount > 0)
        return true;
      if (invalidSeasonCount > maxInvalidFolders)
        return false;
      if (validSeasonCount > 0 && knownSeasonNo >= 0 && !seasonNos.Contains(knownSeasonNo.Value))
        return false;

      return true;
    }

    /// <summary>
    /// Returns the season number from a season folder
    /// </summary>
    /// <param name="seasonFolder">Season folder accessor</param>
    /// <returns>Season number found in folder name</returns>
    public static int GetSeasonFromFolder(IFileSystemResourceAccessor seasonFolder)
    {
      int beforeSeasonNoIndex = seasonFolder.ResourceName.LastIndexOf(" ");
      if (beforeSeasonNoIndex >= 0 && int.TryParse(seasonFolder.ResourceName.Substring(beforeSeasonNoIndex + 1), out int seasonNo))
        return seasonNo;

      return -1;
    }

    /// <summary>
    /// Returns prefixes to use for finding special FanArt files like used for example by TvDB
    /// </summary>
    /// <param name="seasonNumber">Season number to find FanArt for</param>
    /// <returns>Array of prefixes that can be used to search for season FanArt files</returns>
    public static string[] GetAdditionalSeasonPrefixes(int? seasonNumber)
    {
      List<string> prefixes = new List<string>();
      if (seasonNumber.HasValue)
        prefixes.Add($"season{seasonNumber:00}");
      prefixes.Add(seasonNumber == 0 ? "season-specials" : "season-all");
      return prefixes.ToArray();
    }
  }
}
