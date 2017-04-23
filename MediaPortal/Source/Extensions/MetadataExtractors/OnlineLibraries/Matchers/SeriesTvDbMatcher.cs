#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Globalization;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Common.FanArt;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  /// <summary>
  /// <see cref="SeriesTvDbMatcher"/> is used to look up online series information from TheTvDB.com.
  /// </summary>
  public class SeriesTvDbMatcher : SeriesMatcher<TvdbBanner, TvdbLanguage>
  {
    #region Static instance

    public static SeriesTvDbMatcher Instance
    {
      get { return ServiceRegistration.Get<SeriesTvDbMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TvDB\");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromMinutes(10);

    #endregion

    #region Fields

    protected DateTime _lastRefresh = DateTime.MinValue;
    protected bool _useUniversalLanguage = false; // Universal language often leads to unwanted cover languages (i.e. russian)

    #endregion

    #region Init

    public SeriesTvDbMatcher() :
      base(CACHE_PATH, MAX_MEMCACHE_DURATION, true)
    {
      Primary = true;
    }

    public override bool InitWrapper(bool useHttps)
    {
      try
      {
        TvDbWrapper wrapper = new TvDbWrapper();
        // Try to lookup online content in the configured language
        CultureInfo mpLocal = new CultureInfo(PreferredLanguageCulture);
        if (wrapper.Init(CACHE_PATH, useHttps))
        {
          _wrapper = wrapper;
          wrapper.SetPreferredLanguage(mpLocal.TwoLetterISOLanguageName);
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SeriesTvDbMatcher: Error initializing wrapper", ex);
      }
      return false;
    }

    #endregion

    #region Translators

    protected override bool SetSeriesId(SeriesInfo series, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        series.TvdbId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    protected override bool SetSeriesId(EpisodeInfo episode, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        episode.SeriesTvdbId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    protected override bool GetSeriesId(SeriesInfo series, out string id)
    {
      id = null;
      if (series.TvdbId > 0)
        id = series.TvdbId.ToString();
      return id != null;
    }

    protected override bool GetSeriesEpisodeId(EpisodeInfo episode, out string id)
    {
      id = null;
      if (episode.TvdbId > 0)
        id = episode.TvdbId.ToString();
      return id != null;
    }

    protected override bool SetSeriesEpisodeId(EpisodeInfo episode, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        episode.TvdbId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    protected override bool GetCompanyId(CompanyInfo company, out string id)
    {
      id = null;
      if (company.TvdbId > 0)
        id = company.TvdbId.ToString();
      return id != null;
    }

    protected override bool SetCompanyId(CompanyInfo company, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        company.TvdbId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    protected override bool GetPersonId(PersonInfo person, out string id)
    {
      id = null;
      if (person.TvdbId > 0)
        id = person.TvdbId.ToString();
      return id != null;
    }

    protected override bool SetPersonId(PersonInfo person, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        person.TvdbId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    #endregion

    #region Metadata update helpers

    protected override TvdbLanguage FindBestMatchingLanguage(List<string> mediaLanguages)
    {
      TvdbLanguage returnVal;
      CultureInfo mpLocal = new CultureInfo(PreferredLanguageCulture);

      // If we don't have movie languages available, or the MP2 setting language is available, prefer it.
      if (mediaLanguages.Count == 0 || mediaLanguages.Contains(mpLocal.TwoLetterISOLanguageName))
      {
        returnVal = TvDbUtils.ParseLanguage(mpLocal.TwoLetterISOLanguageName);
        if (returnVal.Id != Util.NO_VALUE)
          return returnVal;
      }

      // If there is only one language available, use this one.
      if (mediaLanguages.Count == 1)
      {
        returnVal = TvDbUtils.ParseLanguage(mediaLanguages[0]);
        if (returnVal.Id != Util.NO_VALUE)
          return returnVal;
      }

      // If there are multiple languages, that are different to MP2 setting, we cannot guess which one is the "best".
      // Use preferred language if available.
      returnVal = TvDbUtils.ParseLanguage(mpLocal.TwoLetterISOLanguageName);
      if (returnVal.Id != Util.NO_VALUE)
        return returnVal;

      // By returning null we allow fallback to the default language of the online source (en).
      return TvdbLanguage.DefaultLanguage;
    }

    protected override TvdbLanguage FindMatchingLanguage(string shortLanguageString)
    {
      TvdbLanguage returnVal = TvDbUtils.ParseLanguage(shortLanguageString);
      if (returnVal.Id != Util.NO_VALUE)
        return returnVal;

      return TvdbLanguage.DefaultLanguage;
    }

    #endregion

    #region FanArt

    protected override int SaveFanArtImages(string id, IEnumerable<TvdbBanner> images, TvdbLanguage language, string mediaItemId, string name, string fanartType)
    {
      if (images == null)
        return 0;

      return SaveBanners(images, language, mediaItemId, name, fanartType);
    }

    private int SaveBanners(IEnumerable<TvdbBanner> banners, TvdbLanguage language, string mediaItemId, string name, string fanartType)
    {
      int idx = 0;
      foreach (TvdbBanner tvdbBanner in banners)
      {
        if (tvdbBanner.Language != TvdbLanguage.UniversalLanguage && tvdbBanner.Language != language && language != null && tvdbBanner.Language != null)
          continue;

        using (FanArtCache.FanArtCountLock countLock = FanArtCache.GetFanArtCountLock(mediaItemId, fanartType))
        {
          if (countLock.Count >= FanArtCache.MAX_FANART_IMAGES[fanartType])
            break;

          if (idx >= FanArtCache.MAX_FANART_IMAGES[fanartType])
            break;

          if (fanartType == FanArtTypes.Banner)
          {
            if (!tvdbBanner.BannerPath.Contains("wide") && !tvdbBanner.BannerPath.Contains("graphical"))
              continue;
          }

          if (!tvdbBanner.IsLoaded)
          {
            // We need the image only loaded once, later we will access the cache directly
            try
            {
              FanArtCache.InitFanArtCache(mediaItemId, name);
              tvdbBanner.CachePath = Path.Combine(FANART_CACHE_PATH, mediaItemId, fanartType);
              tvdbBanner.LoadBanner();
              tvdbBanner.UnloadBanner(true);
              idx++;
              countLock.Count++;
            }
            catch (Exception ex)
            {
              Logger.Debug(GetType().Name + " Download: Exception saving images for ID {0} [{1} ({2})]", ex, tvdbBanner.Id, mediaItemId, name);
            }
          }
        }
      }
      if (idx > 0)
      {
        Logger.Debug(GetType().Name + @" Download: Saved {0} for media item {1} ({2}) of type {3}", idx, mediaItemId, name, fanartType);
        return idx;
      }

      // Try fallback languages if no images found for preferred
      if (language != TvdbLanguage.UniversalLanguage && language != TvdbLanguage.DefaultLanguage)
      {
        if (_useUniversalLanguage)
        {
          idx = SaveBanners(banners, TvdbLanguage.UniversalLanguage, mediaItemId, name, fanartType);
          if (idx > 0)
            return idx;
        }

        idx = SaveBanners(banners, TvdbLanguage.DefaultLanguage, mediaItemId, name, fanartType);
      }
      Logger.Debug(GetType().Name + @" Download: Saved {0} for media item {1} ({2}) of type {3}", idx, mediaItemId, name, fanartType);
      return idx;
    }

    #endregion
  }
}
