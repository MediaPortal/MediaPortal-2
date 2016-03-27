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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.MusicBrainz;
using System.IO;

namespace MediaPortal.Extensions.OnlineLibraries
{
  public class MusicBrainzMatcher : BaseMatcher<TrackMatch, string>
  {
    #region Static instance

    public static MusicBrainzMatcher Instance
    {
      get { return ServiceRegistration.Get<MusicBrainzMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\MusicBrainz\");
    protected static string _matchesSettingsFile = Path.Combine(CACHE_PATH, "Matches.xml");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromHours(12);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected DateTime _memoryCacheInvalidated = DateTime.MinValue;
    protected ConcurrentDictionary<string, Track> _memoryCache = new ConcurrentDictionary<string, Track>(StringComparer.OrdinalIgnoreCase);
    private MusicBrainzWrapper _musicBrainzDb;

    #endregion

    public bool FindAndUpdateTrack(TrackInfo trackInfo)
    {
      Track trackDetails;
      if (
        /* Best way is to get details by an unique IMDB id */
        MatchByMusicBrainzId(trackInfo, out trackDetails) ||
        TryMatch(trackInfo.Title, trackInfo.Artists.ToArray(), trackInfo.Album, trackInfo.Year, trackInfo.TrackNum, false, out trackDetails)
        )
      {
        string albumId = null;
        if (trackDetails != null)
        {
          albumId = trackDetails.AlbumId;

          trackInfo.Title = trackDetails.Title;
          trackInfo.Artists.AddRange(trackDetails.TrackArtists);
          trackInfo.Composers.AddRange(trackDetails.Composers);
          trackInfo.Album = trackDetails.Album;
          trackInfo.AlbumArtists.AddRange(trackDetails.AlbumArtists);
          trackInfo.Genres.AddRange(trackDetails.Genre);
          trackInfo.TrackNum = trackDetails.TrackNum;
          trackInfo.TotalTracks = trackDetails.TotalTracks;
          trackInfo.DiscNum = trackDetails.DiscId;
          trackInfo.TotalRating = trackDetails.RatingValue * 2.0; //From 5 star to 10 star
          trackInfo.RatingCount = trackDetails.RatingVotes;
          trackInfo.MusicBrainzId = trackDetails.Id;

          if (trackDetails.ReleaseDate.HasValue)
          {
            int year = trackDetails.ReleaseDate.Value.Year;
            if (year > 0)
              trackInfo.Year = year;
          }
        }

        if (!string.IsNullOrEmpty(albumId))
          ScheduleDownload(albumId);
        return true;
      }
      return false;
    }

    private bool MatchByMusicBrainzId(TrackInfo trackInfo, out Track trackDetails)
    {
      if (!string.IsNullOrEmpty(trackInfo.MusicBrainzId) && _musicBrainzDb.GetTrack(trackInfo.MusicBrainzId, out trackDetails))
      {
        // Add this match to cache
        TrackMatch onlineMatch = new TrackMatch
        {
          Id = trackDetails.Id,
          ItemName = trackDetails.Title,
        };
        // Save cache
        _storage.TryAddMatch(onlineMatch);
        return true;
      }
      trackDetails = null;
      return false;
    }

    protected bool TryMatch(string title, string[] artists, string album, int year, int trackNum, bool cacheOnly, out Track trackDetail)
    {
      trackDetail = null;
      try
      {
        // Prefer memory cache
        CheckCacheAndRefresh();

        // Load cache or create new list
        List<TrackMatch> matches = _storage.GetMatches();

        // Init empty
        trackDetail = null;

        TrackMatch match = null;

        match = matches.Find(m =>
          string.Equals(m.TrackName, title, StringComparison.OrdinalIgnoreCase));
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: Try to lookup track \"{0}\" from cache: {1}", title, match != null && string.IsNullOrEmpty(match.Id) == false);

        // Try online lookup
        if (!Init())
          return false;

        // If this is a known track, only return the track details.
        if (match != null)
          return !string.IsNullOrEmpty(match.Id) && _musicBrainzDb.GetTrack(match.Id, out trackDetail);

        if (cacheOnly)
          return false;

        List<TrackResult> tracks;
        if (_musicBrainzDb.SearchTrackUnique(title, artists, album, year, trackNum, out tracks))
        {
          TrackResult trackResult = tracks[0];
          ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: Found unique online match for \"{0}\": \"{1}\"", title, trackResult.Title);
          if (_musicBrainzDb.GetTrack(trackResult.Id, out trackDetail))
          {
            trackDetail.InitProperties(trackResult.AlbumId);

            // Add this match to cache
            TrackMatch onlineMatch = new TrackMatch
              {
                Id = trackDetail.Id,
                ItemName = trackDetail.Title,
                TrackName = title
              };

            // Save cache
            _storage.TryAddMatch(onlineMatch);
          }
          return true;
        }
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: No unique match found for \"{0}\"", title);
        // Also save "non matches" to avoid retrying
        _storage.TryAddMatch(new TrackMatch { TrackName = title });
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: Exception while processing track {0}", ex, title);
        return false;
      }
      finally
      {
        if (trackDetail != null)
          _memoryCache.TryAdd(title, trackDetail);
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

      if (_musicBrainzDb != null)
        return true;

      _musicBrainzDb = new MusicBrainzWrapper();
      // Try to lookup online content in the configured language
      CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      string lang = currentCulture.Name;
      if (lang.Contains("-")) lang = lang.Split('-')[1];
      _musicBrainzDb.SetPreferredLanguage(lang);
      return _musicBrainzDb.Init();
    }

    protected override void DownloadFanArt(string musicBrainzId)
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher Download: Started for ID {0}", musicBrainzId);

        if (!Init())
          return;

        TrackImageCollection imageCollection;
        if (!_musicBrainzDb.GetTrackFanArt(musicBrainzId, out imageCollection))
          return;

        // Save Fronts
        bool result = _musicBrainzDb.DownloadImages(musicBrainzId, imageCollection);
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: Saved FanArt for release {0} {1}", imageCollection.ReleaseUrl, result);

        // Remember we are finished
        FinishDownloadFanArt(musicBrainzId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: Exception downloading FanArt for ID {0}", ex, musicBrainzId);
      }
    }
  }
}
