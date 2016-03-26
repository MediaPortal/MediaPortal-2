#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Freedb.Data;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Freedb;

namespace MediaPortal.Extensions.OnlineLibraries
{
  public class FreeDbMatcher : BaseMatcher<DiscIdMatch, string>
  {
    #region Static instance

    public static FreeDbMatcher Instance
    {
      get { return ServiceRegistration.Get<FreeDbMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FreeDB\");
    protected static string _matchesSettingsFile = Path.Combine(CACHE_PATH, "Matches.xml");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromHours(12);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected DateTime _memoryCacheInvalidated = DateTime.MinValue;
    protected ConcurrentDictionary<string, CDInfoDetail> _memoryCache = new ConcurrentDictionary<string, CDInfoDetail>();
    private FreeDbWrapper _freeDb;

    #endregion

    public bool FindAndUpdateTrack(TrackInfo trackInfo)
    {
      CDInfoDetail trackDetails;
      if (
        /* Best way is to get details by an unique CDDB id */
        TryMatch(trackInfo.CdDdId, trackInfo.Title, false, out trackDetails)
        )
      {
        if (trackDetails != null)
        {
          trackInfo.Album = trackDetails.Title;
          trackInfo.AlbumArtists.Add(trackDetails.Artist);
          trackInfo.Genres.Add(trackDetails.Genre);
          trackInfo.Year = trackDetails.Year;

          List<CDTrackDetail> tracks = new List<CDTrackDetail>(trackDetails.Tracks);
          if (_freeDb.FindTrack(trackInfo.Title, ref tracks))
          {
            trackInfo.Title = tracks[0].Title;
            if (string.IsNullOrEmpty(tracks[0].Artist))
            {
              trackInfo.Artists.Add(trackDetails.Artist);
            }
            else
            {
              trackInfo.Artists.Add(tracks[0].Artist);
            }
            trackInfo.TrackNum = tracks[0].TrackNumber;
          }
          else
          {
            return false;
          }

          // Add this match to cache
          DiscIdMatch onlineMatch = new DiscIdMatch
          {
            CdDbId = trackDetails.DiscID,
            ItemName = trackInfo.Title
          };
          // Save cache
          _storage.TryAddMatch(onlineMatch);
          return true;
        }
      }
      return false;
    }

    private bool MatchByCdDbId(string cdDbId, string trackName, out CDInfoDetail trackDetails)
    {
      trackDetails = null;
      if (!string.IsNullOrEmpty(cdDbId))
      {
        List<CDInfoDetail> discs = new List<CDInfoDetail>();
        if (_freeDb.SearchDisc(cdDbId, trackName, out discs))
        {
          trackDetails = discs[0];
          ServiceRegistration.Get<ILogger>().Debug("FreeDBMatcher: Found online match for \"{0}\": \"{1}\"", cdDbId, trackDetails.Title);
          return true;
        }
      }
      return false;
    }

    protected bool TryMatch(string cdDbId, string trackName, bool cacheOnly, out CDInfoDetail cdDetail)
    {
      cdDetail = null;
      try
      {
        // Prefer memory cache
        CheckCacheAndRefresh();
        if (_memoryCache.TryGetValue(cdDbId + trackName, out cdDetail))
          return true;

        // Load cache or create new list
        List<DiscIdMatch> matches = _storage.GetMatches();

        // Init empty
        cdDetail = null;

        DiscIdMatch match = null;

        // Use cached values before doing online query
        match = matches.Find(m =>
          string.Equals(m.CdDbId, cdDbId, StringComparison.OrdinalIgnoreCase) && string.Equals(m.ItemName, trackName, StringComparison.OrdinalIgnoreCase));
        ServiceRegistration.Get<ILogger>().Debug("FreeDbMatcher: Try to lookup CD \"{0}\" from cache: {1}", cdDbId, match != null && string.IsNullOrEmpty(match.CdDbId) == false);

        // Try online lookup
        if (!Init())
          return false;

        //If this is a known CD return
        if (match != null)
          return string.IsNullOrEmpty(match.CdDbId) == false && _freeDb.GetDisc(match.CdDbId, match.ItemName, out cdDetail);

        if (cacheOnly)
          return false;

        if (MatchByCdDbId(cdDbId, trackName, out cdDetail))
        {
          return true;
        }

        ServiceRegistration.Get<ILogger>().Debug("FreeDBMatcher: No unique CD found for \"{0}\"", cdDbId);
        // Also save "non matches" to avoid retrying
        _storage.TryAddMatch(new DiscIdMatch { CdDbId = cdDbId, ItemName = trackName });
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("FreeDBMatcher: Exception while processing CD {0}", ex, cdDbId);
        return false;
      }
      finally
      {
        if (cdDetail != null)
          _memoryCache.TryAdd(cdDbId + trackName, cdDetail);
      }
    }

    /// <summary>
    /// Check if the memory cache should be cleared and starts an online update of (file-) cached series information.
    /// </summary>
    private void CheckCacheAndRefresh()
    {
      if (DateTime.Now - _memoryCacheInvalidated <= MAX_MEMCACHE_DURATION)
        return;
      _memoryCache.Clear();
      _memoryCacheInvalidated = DateTime.Now;

      // TODO: when updating track information is implemented, start here a job to do it
    }

    public override bool Init()
    {
      if (!base.Init())
        return false;

      if (_freeDb != null)
        return true;

      _freeDb = new FreeDbWrapper();
      return _freeDb.Init();
    }

    protected override void DownloadFanArt(string itemId)
    {
      throw new NotImplementedException();
    }
  }
}
