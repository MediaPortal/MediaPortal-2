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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.OnlineLibraries
{
  public class CDFreeDbMatcher : BaseMatcher<TrackMatch, string>
  {
    #region Static instance

    public static CDFreeDbMatcher Instance
    {
      get { return ServiceRegistration.Get<CDFreeDbMatcher>(); }
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
    protected ConcurrentDictionary<string, FreeDBCDInfoDetail> _memoryCache = new ConcurrentDictionary<string, FreeDBCDInfoDetail>(StringComparer.OrdinalIgnoreCase);
    private FreeDbWrapper _freeDb;

    #endregion

    public bool FindAndUpdateTrack(TrackInfo trackInfo)
    {
      FreeDBCDInfoDetail trackDetails;
      if (
        /* Best way is to get details by an unique CDDB id */
        TryMatch(trackInfo.AlbumCdDdId, trackInfo.TrackNum, false, out trackDetails)
        )
      {
        if (trackDetails != null)
        {
          MetadataUpdater.SetOrUpdateId(ref trackInfo.AlbumCdDdId, trackDetails.DiscID);

          MetadataUpdater.SetOrUpdateString(ref trackInfo.Album, trackDetails.Title, true);
          MetadataUpdater.SetOrUpdateList(trackInfo.AlbumArtists, ConvertToPersons(trackDetails.Artist, PersonAspect.OCCUPATION_ARTIST), false, true);
          MetadataUpdater.SetOrUpdateList(trackInfo.Genres, new List<string>(new string[] { trackDetails.Genre }), false, true);
          MetadataUpdater.SetOrUpdateValue(ref trackInfo.ReleaseDate, new DateTime(trackDetails.Year, 1, 1));

          List<FreeDBCDTrackDetail> tracks = new List<FreeDBCDTrackDetail>(trackDetails.Tracks);
          if (_freeDb.FindTrack(trackInfo.TrackNum, ref tracks))
          {
            MetadataUpdater.SetOrUpdateString(ref trackInfo.TrackName, tracks[0].Title, true);
            MetadataUpdater.SetOrUpdateList(trackInfo.AlbumArtists, ConvertToPersons(tracks[0].Artist, PersonAspect.OCCUPATION_ARTIST), false, true);
            MetadataUpdater.SetOrUpdateValue(ref trackInfo.TrackNum, tracks[0].TrackNumber);
          }
          else
          {
            return false;
          }

          // Add this match to cache
          TrackMatch onlineMatch = new TrackMatch
          {
            Id = trackDetails.DiscID,
            TrackNum = trackInfo.TrackNum,
            TrackName = trackInfo.TrackName,
            AlbumName = trackInfo.Album,
            ItemName = trackDetails.DiscID
          };
          // Save cache
          _storage.TryAddMatch(onlineMatch);
          return true;
        }
      }
      return false;
    }

    private List<PersonInfo> ConvertToPersons(string name, string occupation)
    {
      if (string.IsNullOrEmpty(name))
        return new List<PersonInfo>();

      return new List<PersonInfo> {
        new PersonInfo()
        {
          Name = name,
          Occupation = occupation
        }
      };
    }

    private bool MatchByCdDbId(string cdDbId, out FreeDBCDInfoDetail trackDetails)
    {
      trackDetails = null;
      if (!string.IsNullOrEmpty(cdDbId))
      {
        List<FreeDBCDInfoDetail> discs = new List<FreeDBCDInfoDetail>();
        if (_freeDb.SearchDisc(cdDbId, out discs))
        {
          trackDetails = discs[0];
          ServiceRegistration.Get<ILogger>().Debug("FreeDBMatcher: Found online match for \"{0}\": \"{1}\"", cdDbId, trackDetails.Title);
          return true;
        }
      }
      return false;
    }

    protected bool TryMatch(string cdDbId, int trackNumber, bool cacheOnly, out FreeDBCDInfoDetail cdDetail)
    {
      cdDetail = null;
      try
      {
        // Prefer memory cache
        CheckCacheAndRefresh();
        if (_memoryCache.TryGetValue(cdDbId, out cdDetail))
          return true;

        // Load cache or create new list
        List<TrackMatch> matches = _storage.GetMatches();

        // Init empty
        cdDetail = null;

        TrackMatch match = null;

        // Use cached values before doing online query
        match = matches.Find(m =>
          string.Equals(m.Id, cdDbId, StringComparison.OrdinalIgnoreCase) && int.Equals(m.TrackNum, trackNumber));
        ServiceRegistration.Get<ILogger>().Debug("FreeDbMatcher: Try to lookup CD \"{0}\" from cache: {1}", cdDbId, match != null && string.IsNullOrEmpty(match.Id) == false);

        // Try online lookup
        if (!Init())
          return false;

        //If this is a known CD return
        if (match != null)
          return string.IsNullOrEmpty(match.Id) == false && _freeDb.GetDisc(match.Id, out cdDetail);

        if (cacheOnly)
          return false;

        if (MatchByCdDbId(cdDbId, out cdDetail))
        {
          return true;
        }

        ServiceRegistration.Get<ILogger>().Debug("FreeDBMatcher: No unique CD found for \"{0}\"", cdDbId);
        // Also save "non matches" to avoid retrying
        _storage.TryAddMatch(new TrackMatch { Id = cdDbId, ItemName = cdDbId });
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
          _memoryCache.TryAdd(cdDbId, cdDetail);
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
      return _freeDb.Init(CACHE_PATH);
    }

    protected override void DownloadFanArt(string itemId)
    {
    }
  }
}
