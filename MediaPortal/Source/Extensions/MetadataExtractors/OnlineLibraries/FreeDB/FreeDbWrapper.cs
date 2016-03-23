#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Freedb;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Freedb.Data;
using System.IO;
using System.Text;

namespace MediaPortal.Extensions.OnlineLibraries.Freedb
{
  class FreeDbWrapper
  {
    private string _cache = FreeDbMatcher.CACHE_PATH;
    private string _fileFormat = "CD_{0}.xmcd";

    protected FreeDBQuery _freeDbHandler;
    public const int MAX_LEVENSHTEIN_DIST = 4;

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns><c>true</c> if successful</returns>
    public bool Init()
    {
      _freeDbHandler = new FreeDBQuery();
      if (Directory.Exists(_cache) == false)
        Directory.CreateDirectory(_cache);
      return true;
    }

    /// <summary>
    /// Return a cache file name for a CDDB ID
    /// </summary>
    /// <returns>Cache file name</returns>
    private string GetCacheFilePath(string cdDbId, string genre)
    {
      return Path.Combine(_cache, string.Format(_fileFormat, cdDbId.ToUpperInvariant() + "."  + String.Concat(genre.Split(Path.GetInvalidFileNameChars()))));
    }

    /// <summary>
    /// Gets all cache files matching the CDDB ID. 
    /// </summary>
    /// <param name="cdDbId">The CDDB ID</param>
    /// <returns>List of file matching files.</returns>
    private string[] GetMatchingCacheFiles(string cdDbId)
    {
      return Directory.GetFiles(_cache, string.Format(_fileFormat, cdDbId.ToUpperInvariant() + ".*"));
    }

    /// <summary>
    /// Search for CD by CDDB ID.
    /// </summary>
    /// <param name="cdDbId">The CDDB ID</param>
    /// <param name="discs">Returns the list of matches.</param>
    /// <returns><c>true</c> if at least one CD was found.</returns>
    public bool SearchDisc(string cdDbId, string trackName, out List<CDInfoDetail> discs)
    {
      discs = new List<CDInfoDetail>();
      CDInfoDetail discInfo;
      if (GetDisc(cdDbId, trackName, out discInfo))
      {
        discs.Add(discInfo);
        return true;
      }

      if (_freeDbHandler.Connect())
      {
        Dictionary<string, string[]> xmcds = _freeDbHandler.GetDiscDetailsXMCDFromId(cdDbId);
        if (xmcds != null)
        {
          foreach (KeyValuePair<string, string[]> xmcd in xmcds)
          {
            string fileName = GetCacheFilePath(discInfo.DiscID, xmcd.Key);
            if (File.Exists(fileName) == false)
            {
              File.WriteAllLines(fileName, xmcd.Value, Encoding.UTF8);
            }

            discInfo = _freeDbHandler.GetDiscDetailsFromXMCD(xmcd.Value);
            if (TestTracks(trackName, discInfo))
            {
              discs.Add(discInfo);
            }
          }
        }
        _freeDbHandler.Disconnect();
      }
      return discs.Count > 0;
    }

    /// <summary>
    /// Checks if track exists on found CD
    /// </summary>
    /// <param name="trackName">The track name to find</param>
    /// <param name="discInfo">Found CD disc.</param>
    /// <returns><c>true</c> if the track was found.</returns>
    private bool TestTracks(string trackName, CDInfoDetail discInfo)
    {
      List<CDTrackDetail> tracks = new List<CDTrackDetail>(discInfo.Tracks);
      if (FindTrack(trackName, ref tracks))
        return true;
      return false;
    }

    /// <summary>
    /// Searches for unique track. 
    /// </summary>
    /// <param name="trackName">Track name</param>
    /// <param name="movies">Potential track matches. The collection will be modified inside this method.</param>
    /// <returns><c>true</c> if unique match</returns>
    public bool FindTrack(string trackName, ref List<CDTrackDetail> tracks)
    {
      if (tracks.Count == 1)
      {
        if (GetLevenshteinDistance(tracks[0].Title, trackName) <= MAX_LEVENSHTEIN_DIST)
        {
          ServiceRegistration.Get<ILogger>().Debug("FreeDbWrapper: Unique match found \"{0}\"!", tracks[0].Title);
          return true;
        }
        // No valid match, clear list to allow further detection ways
        tracks.Clear();
        return false;
      }

      // Multiple matches
      if (tracks.Count > 1)
      {
        var exactMatches = tracks.FindAll(s => s.Title == trackName || GetLevenshteinDistance(s.Title, trackName) == 0);
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug("FreeDbWrapper: Unique match found \"{0}\"!", exactMatches[0].Title);
          tracks = exactMatches;
          return true;
        }

        tracks = tracks.Where(s => GetLevenshteinDistance(s.Title, trackName) <= MAX_LEVENSHTEIN_DIST).ToList();
        if (tracks.Count > 1)
          ServiceRegistration.Get<ILogger>().Debug("FreeDbWrapper: Multiple tracks found for \"{0}\" (count: {1})", trackName, tracks.Count);

        return tracks.Count == 1;
      }
      return false;
    }

    /// <summary>
    /// Clears cache. 
    /// </summary>
    /// <returns><c>true</c> if successful</returns>
    public bool ClearCache()
    {
      bool retValue = true;
      foreach(string file in Directory.GetFiles(_cache))
      {
        try { File.Delete(file); }
        catch { retValue = false; }
      }
      return retValue;
    }

    /// <summary>
    /// Get cached disc info. 
    /// </summary>
    /// <param name="cdDbId">The CDDB ID</param>
    /// <param name="trackName">Track name used to find correct disc.</param>
    /// <returns><c>true</c> if disc match</returns>
    public bool GetDisc(string cdDbId, string trackName, out CDInfoDetail disc)
    {
      disc = null;
      foreach (string file in GetMatchingCacheFiles(cdDbId))
      {
        CDInfoDetail discInfo = _freeDbHandler.GetDiscDetailsFromXMCD(File.ReadAllLines(file, Encoding.UTF8));
        if (TestTracks(trackName, discInfo))
        {
          disc = discInfo;
          return true;
        }
      }
      return disc != null;
    }

    /// <summary>
    /// Returns the Levenshtein distance for a <paramref name="trackName"/> and a given <paramref name="searchName"/>.
    /// </summary>
    /// <param name="trackName">Track name to check</param>
    /// <param name="searchName">Track name to find</param>
    /// <returns>Levenshtein distance</returns>
    protected int GetLevenshteinDistance(string trackName, string searchName)
    {
      string cleanedName = RemoveCharacters(searchName);
      return StringUtils.GetLevenshteinDistance(RemoveCharacters(trackName), cleanedName);
    }

    /// <summary>
    /// Replaces characters that are not necessary for comparing (like whitespaces) and diacritics. The result is returned as <see cref="string.ToLowerInvariant"/>.
    /// </summary>
    /// <param name="name">Name to clean up</param>
    /// <returns>Cleaned string</returns>
    protected string RemoveCharacters(string name)
    {
      name = name.ToLowerInvariant();
      string result = new[] { "-", ",", "/", ":", " ", " ", ".", "'", "(", ")", "[", "]" }.Aggregate(name, (current, s) => current.Replace(s, ""));
      result = result.Replace("&", "and");
      return StringUtils.RemoveDiacritics(result);
    }
  }
}
