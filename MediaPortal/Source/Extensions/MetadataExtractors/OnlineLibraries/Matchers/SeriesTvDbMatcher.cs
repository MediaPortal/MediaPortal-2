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

using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
    }

    public override async Task<bool> InitWrapperAsync(bool useHttps)
    {
      try
      {
        TvDbWrapper wrapper = new TvDbWrapper();
        // Try to lookup online content in the configured language
        CultureInfo mpLocal = new CultureInfo(PreferredLanguageCulture);
        if (await wrapper.InitAsync(CACHE_PATH, useHttps).ConfigureAwait(false))
        {
          _wrapper = wrapper;
          wrapper.SetPreferredLanguageAsync(mpLocal.TwoLetterISOLanguageName).Wait();
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
        if (id.StartsWith("tt", StringComparison.InvariantCultureIgnoreCase))
          series.ImdbId = id;
        else
          series.TvdbId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    protected override bool SetSeriesId(EpisodeInfo episode, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        if (id.StartsWith("tt", StringComparison.InvariantCultureIgnoreCase))
          episode.SeriesImdbId = id;
        else
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
      else if (!string.IsNullOrEmpty(series.ImdbId))
        id = series.ImdbId;
      return id != null;
    }

    protected override bool GetSeriesEpisodeId(EpisodeInfo episode, out string id)
    {
      id = null;
      if (episode.TvdbId > 0)
        id = episode.TvdbId.ToString();
      else if (!string.IsNullOrEmpty(episode.ImdbId))
        id = episode.ImdbId;
      return id != null;
    }

    protected override bool SetSeriesEpisodeId(EpisodeInfo episode, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        if (id.StartsWith("tt", StringComparison.InvariantCultureIgnoreCase))
          episode.ImdbId = id;
        else
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

    protected override async Task<int> SaveFanArtImagesAsync(string id, IEnumerable<TvdbBanner> images, TvdbLanguage language, Guid mediaItemId, string name, string fanArtType)
    {
      if (images == null || !images.Any())
        return 0;

      int currentCount = await base.SaveFanArtImagesAsync(id, images, language, mediaItemId, name, fanArtType).ConfigureAwait(false);
      if (currentCount > 0 || language == TvdbLanguage.UniversalLanguage || language == TvdbLanguage.DefaultLanguage)
        return currentCount;

      // Try fallback languages if no images found for preferred
      language = _useUniversalLanguage ? TvdbLanguage.UniversalLanguage : TvdbLanguage.DefaultLanguage;
      return await base.SaveFanArtImagesAsync(id, images, language, mediaItemId, name, fanArtType).ConfigureAwait(false);
    }

    protected override bool VerifyFanArtImage(TvdbBanner image, TvdbLanguage language, string fanArtType)
    {
      if (image.IsLoaded)
        return false;
      if (image.Language != TvdbLanguage.UniversalLanguage && image.Language != language && language != null && image.Language != null)
        return false;
      if (fanArtType == FanArtTypes.Banner && !image.BannerPath.Contains("wide") && !image.BannerPath.Contains("graphical"))
        return false;
      return true;
    }

    #endregion
  }
}
