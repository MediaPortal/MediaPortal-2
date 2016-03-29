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
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.FanArtTV;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="FanArtTvMusicMatcher"/> is used to download music images from FanArt.tv.
  /// </summary>
  public class FanArtTvMusicMatcher : BaseMatcher<TrackMatch, string>
  {
    #region Static instance

    public static FanArtTvMusicMatcher Instance
    {
      get { return ServiceRegistration.Get<FanArtTvMusicMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArtTV\");
    protected static string _matchesSettingsFile = Path.Combine(CACHE_PATH, "TrackMatches.xml");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromHours(12);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected DateTime _memoryCacheInvalidated = DateTime.MinValue;
    protected ConcurrentDictionary<string, string> _memoryCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Contains the initialized FanArtTvWrapper.
    /// </summary>
    private FanArtTVWrapper _fanArt;

    #endregion

    /// <summary>
    /// Tries to lookup the music from FanArt.tv and downloads images.
    /// </summary>
    /// <param name="trackInfo">Track to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateTrack(TrackInfo trackInfo)
    {
      // Try online lookup
      if (!Init())
        return false;

      if (!string.IsNullOrEmpty(trackInfo.MusicBrainzId))
      {
        string mbId = "";
        CheckCacheAndRefresh();
        if (_memoryCache.TryGetValue(trackInfo.MusicBrainzId, out mbId))
        {
          return true;
        }

        ScheduleDownload(trackInfo.MusicBrainzId);
        return true;
      }
      return false;
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

      if (_fanArt != null)
        return true;

      try
      {
        _fanArt = new FanArtTVWrapper();
        bool res = _fanArt.Init();
        // Try to lookup online content in the configured language
        CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
        _fanArt.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
        return res;
      }
      catch (Exception)
      {
        return false;
      }
    }

    protected override void DownloadFanArt(string mbId)
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("FanArtTvMusicMatcher Download: Started for ID {0}", mbId);

        if (!Init())
          return;

        string albumId = "";
        string[] artistIds = null;

        // Save Album Covers
        ServiceRegistration.Get<ILogger>().Debug("FanArtTvMusicMatcher Download: Begin saving album covers for ID {0}", mbId);
        //_fanArt.DownloadAlbumCovers(albumId);

        // Save Artist Banners
        ServiceRegistration.Get<ILogger>().Debug("FanArtTvMusicMatcher Download: Begin saving artitst banners for ID {0}", mbId);
        //foreach(string artistId in artistIds) _fanArt.DownloadArtistBanners(artistId);

        // Save Artist FanArt
        ServiceRegistration.Get<ILogger>().Debug("FanArtTvMusicMatcher Download: Begin saving artist fanarts for ID {0}", mbId);
        //foreach (string artistId in artistIds) _fanArt.DownloadArtistFanArt(artistId);

        ServiceRegistration.Get<ILogger>().Debug("FanArtTvMusicMatcher Download: Finished ID {0}", mbId);

        TrackMatch onlineMatch = new TrackMatch
        {
          ItemName = mbId,
          Id = mbId
        };

        // Save cache
        _storage.TryAddMatch(onlineMatch);

        // Remember we are finished
        FinishDownloadFanArt(mbId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("FanArtTvMusicMatcher: Exception downloading FanArt for ID {0}", ex, mbId);
      }
    }
  }
}
