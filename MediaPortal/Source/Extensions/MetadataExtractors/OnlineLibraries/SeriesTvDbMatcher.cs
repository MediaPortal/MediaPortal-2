#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Globalization;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.TheTvDB;
using TvdbLib.Data;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="SeriesTvDbMatcher"/> is used to look up online series information from TheTvDB.com.
  /// </summary>
  public class SeriesTvDbMatcher
  {
    public const int MAX_FANART_IMAGES = 10;
    //TODO: store cache in server folder, make contents acessible via client-server communication (WCF, UPnP?)
    public const string SETTINGS_MATCHES = @"C:\ProgramData\Team MediaPortal\MP2-Client\TvDB\Matches.xml";

    /// <summary>
    /// Tries to lookup the series from TheTvDB and return the found ID.
    /// </summary>
    /// <param name="seriesName">Series name to check</param>
    /// <param name="tvDbId">Return the TvDB ID of series</param>
    /// <returns><c>true</c> if successful</returns>
    public bool TryGetTvDbId(string seriesName, out int tvDbId)
    {
      SeriesMatch match;
      TvdbSeries seriesDetail;
      if (TryMatch(seriesName, true, out match, out seriesDetail))
      {
        tvDbId = match.TvDBID;
        return true;
      }
      tvDbId = 0;
      return false;
    }

    /// <summary>
    /// Tries to lookup the series from TheTvDB and updates the given <paramref name="seriesInfo"/> with the online information (Series and Episode names).
    /// </summary>
    /// <param name="seriesInfo">Series to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateSeries(SeriesInfo seriesInfo)
    {
      SeriesMatch match;
      TvdbSeries seriesDetail;
      if (TryMatch(seriesInfo.Series, false, out match, out seriesDetail))
      {
        int tvDbId = 0;
        if (seriesDetail != null)
        {
          tvDbId = seriesDetail.Id;
          seriesInfo.Series = seriesDetail.SeriesName;
          // Also try to fill episode title from series details (most file names don't contain episode name).
          TryMatchEpisode(seriesInfo, seriesDetail);
        }
        else
          if (match != null)
          {
            match.TvDBID = match.TvDBID;
            seriesInfo.Series = match.TvDBName;
          }

        // TODO: download fanart asynch
        if (tvDbId > 0)
          DownloadFanArt(tvDbId);
        return true;
      }
      return false;
    }

    protected bool TryMatchEpisode(SeriesInfo seriesInfo, TvdbSeries seriesDetail)
    {
      // We deal with two scenarios here:
      //  - Having a real episode title, but the Season/Episode numbers might be wrong (seldom case)
      //  - Having only Season/Episode numbers and we need to fill Episode title (more common)
      TvdbEpisode episode = seriesDetail.Episodes.Find(e => e.EpisodeName == seriesInfo.Episode);
      if (episode != null)
      {
        seriesInfo.SeasonNumber = episode.SeasonNumber;
        seriesInfo.EpisodeNumbers.Clear();
        seriesInfo.EpisodeNumbers.Add(episode.EpisodeNumber);
        return true;
      }
      
      episode = seriesDetail.Episodes.Find(e => e.EpisodeNumber == seriesInfo.EpisodeNumbers.FirstOrDefault() && e.SeasonNumber == seriesInfo.SeasonNumber);
      if (episode != null)
      {
        seriesInfo.Episode = episode.EpisodeName;
        return true;
      }
      return false;
    }

    protected bool TryMatch(string seriesName, bool cacheOnly, out SeriesMatch match, out TvdbSeries seriesDetail)
    {
      // Load cache or create new list
      List<SeriesMatch> matches = Settings.Load<List<SeriesMatch>>(SETTINGS_MATCHES) ?? new List<SeriesMatch>();

      // Init empty
      seriesDetail = null;

      // Use cached values before doing online query
      match = matches.Find(m => m.SeriesName == seriesName || m.TvDBName == seriesName);
      if (cacheOnly)
        return match != null;

      // Try online lookup
      var tv = GetTvDbWrapper();

      List<TvdbSearchResult> series;
      if (tv.SearchSeriesUnique(seriesName, out series))
      {
        TvdbSearchResult matchedSeries = series[0];
        if (tv.GetSeries(matchedSeries.Id, true, out seriesDetail))
        {
          // Add this match to cache
          SeriesMatch onlineMatch = new SeriesMatch
          {
            SeriesName = seriesName,
            TvDBID = seriesDetail.Id,
            TvDBName = seriesDetail.SeriesName
          };

          if (matches.All(m => m.SeriesName != seriesName))
            matches.Add(onlineMatch);

          // Save cache
          Settings.Save(SETTINGS_MATCHES, matches);
          return true;
        }
      }
      return false;
    }

    private static TvDbWrapper GetTvDbWrapper()
    {
      TvDbWrapper tv = new TvDbWrapper();
      // Try to lookup online content in the configured language
      CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      tv.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
      tv.Init();
      return tv;
    }

    public bool DownloadFanArt(int tvDbId)
    {
      var tv = GetTvDbWrapper();
      TvdbSeries seriesDetail;
      if (!tv.GetSeriesFanArt(tvDbId, out seriesDetail))
        return false;
      // Save Banners
      SaveBanners(seriesDetail.SeriesBanners, tv.PreferredLanguage);

      // Save Posters
      SaveBanners(seriesDetail.PosterBanners, tv.PreferredLanguage);

      // Save FanArt
      //SaveBanners(seriesDetail.FanartBanners, tv.PreferredLanguage);
      return true;
    }

    private static int SaveBanners<TE>(IEnumerable<TE> banners, TvdbLanguage language) where TE : TvdbBanner
    {
      int idx = 0;
      foreach (TE tvdbBanner in banners)
      {
        if (tvdbBanner.Language != language)
          continue;

        if (idx++ >= 10)
          break;

        if (!tvdbBanner.IsLoaded)
        {
          // We need the image only loaded once, later we will access the cache directly
          tvdbBanner.LoadBanner();
          tvdbBanner.UnloadBanner();
        }
      }
      if (idx > 0)
        return idx;

      // Try fallback languages if no images found for preferred
      if (language != TvdbLanguage.UniversalLanguage && language != TvdbLanguage.DefaultLanguage)
      {
        idx = SaveBanners(banners, TvdbLanguage.UniversalLanguage);
        if (idx > 0)
          return idx;

        idx = SaveBanners(banners, TvdbLanguage.DefaultLanguage);
      }
      return idx;
    }
  }
}