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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.MusicBrainz;
using MediaPortal.Extensions.OnlineLibraries.TheMovieDB;

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
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromHours(12);

    protected override string MatchesSettingsFile
    {
      get { return null; }
    }

    #endregion

    #region Fields

    protected DateTime _memoryCacheInvalidated = DateTime.MinValue;
    protected ConcurrentDictionary<string, Track> _memoryCache = new ConcurrentDictionary<string, Track>(StringComparer.OrdinalIgnoreCase);

    private MusicBrainzWrapper _musicBrainzDb;

    #endregion

    public bool FindAndUpdateTrack(TrackInfo trackInfo)
    {
      string preferredLookupLanguage = FindBestMatchingLanguage(trackInfo);
      Track trackDetails;
      if (
        /* Best way is to get details by an unique IMDB id */
        MatchByMusicBrainzId(trackInfo, out trackDetails) ||
        TryMatch(trackInfo.Title, trackInfo.ArtistName, trackInfo.AlbumName, trackInfo.Genre, trackInfo.Year, trackInfo.TrackNum, trackInfo.AlbumArtistName, preferredLookupLanguage, false, out trackDetails)
        )
      {
        string trackDbId = null;
        if (trackDetails != null)
        {
          trackDbId = trackDetails.Id;
          trackInfo.Title = trackDetails.Title;
          trackInfo.ArtistId = trackDetails.ArtistId;
          trackInfo.ArtistName = trackDetails.ArtistName;
          trackInfo.AlbumId = trackDetails.AlbumId;
          trackInfo.AlbumName = trackDetails.AlbumName;
          trackInfo.Genre = trackDetails.Genre;
          trackInfo.TrackNum = trackDetails.TrackNum;
          trackInfo.AlbumArtistId = trackDetails.AlbumArtistId;
          trackInfo.AlbumArtistName = trackDetails.AlbumArtistName;
          trackInfo.MusicBrainzId = trackDetails.MusicBrainzId;

          if (trackDetails.ReleaseDate.HasValue)
          {
            int year = trackDetails.ReleaseDate.Value.Year;
            if (year > 0)
              trackInfo.Year = year;
          }
        }

        if (!string.IsNullOrEmpty(trackDbId))
          //ScheduleDownload(trackDbId);
        return true;
      }
      return false;
    }

    private static string FindBestMatchingLanguage(TrackInfo trackInfo)
    {
      return null;
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

    protected bool TryMatch(string title, string artist, string album, string genre, int year, int trackNum, string albumArtist, string language, bool cacheOnly, out Track trackDetail)
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

        // Try online lookup
        if (!Init())
          return false;

        // If this is a known track, only return the track details.
        if (match != null)
          return !string.IsNullOrEmpty(match.Id) && _musicBrainzDb.GetTrack(match.Id, out trackDetail);

        if (cacheOnly)
          return false;

        IList<TrackSearchResult> tracks;
        if (_musicBrainzDb.SearchTrackUnique(title, artist, album, genre, year, trackNum, albumArtist, language, out tracks))
        {
          TrackSearchResult trackResult = tracks[0];
          ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: Found unique online match for \"{0}\": \"{1}\"", title, trackResult.Title);
          if (_musicBrainzDb.GetTrack(tracks[0].Id, out trackDetail))
          {
            // Add this match to cache
            TrackMatch onlineMatch = new TrackMatch
              {
                Id = trackDetail.Id,
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
      _musicBrainzDb.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
      return _musicBrainzDb.Init();
    }

    protected override void DownloadFanArt(string musicBrainzId)
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher Download: Started for ID {0}", musicBrainzId);

        if (!Init())
          return;

        // If track belongs to a collection, also download collection poster and fanart
        Track track;
        if (_musicBrainzDb.GetTrack(musicBrainzId, out track) && track.Collection != null)
          SaveBanners(track.Collection);

        ImageCollection imageCollection;
        if (!_musicBrainzDb.GetTrackFanArt(musicBrainzId, out imageCollection))
          return;

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher Download: Begin saving banners for ID {0}", musicBrainzId);
        SaveBanners(imageCollection.Backdrops, "Backdrops");
        SaveBanners(imageCollection.Covers, "Covers");
        SaveBanners(imageCollection.Posters, "Posters");
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher Download: Finished saving banners for ID {0}", musicBrainzId);

        // Remember we are finished
        FinishDownloadFanArt(musicBrainzId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: Exception downloading FanArt for ID {0}", ex, musicBrainzId);
      }
    }

    private void SaveBanners(TrackCollection trackCollection)
    {
      bool result = _musicBrainzDb.DownloadImages(trackCollection);
      ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher Download Collection: Saved {0} {1}", trackCollection.Name, result);
    }

    private int SaveBanners(IEnumerable<TrackImage> banners, string category)
    {
      if (banners == null)
        return 0;

      int idx = 0;
      foreach (TrackImage banner in banners.Where(b => b.Language == null || b.Language == _musicBrainzDb.PreferredLanguage))
      {
        if (idx >= MAX_FANART_IMAGES)
          break;
        if (_musicBrainzDb.DownloadImage(banner, category))
          idx++;
      }
      ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher Download: Saved {0} {1}", idx, category);
      return idx;
    }

    public bool FindAndUpdateSong(object movieInfo)
    {
      throw new NotImplementedException();
    }
  }
}
