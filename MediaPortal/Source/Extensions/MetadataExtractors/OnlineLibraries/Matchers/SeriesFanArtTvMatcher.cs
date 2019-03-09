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
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3.Data;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  /// <summary>
  /// <see cref="SeriesFanArtTvMatcher"/> is used to download series images from FanArt.tv.
  /// </summary>
  public class SeriesFanArtTvMatcher : SeriesMatcher<FanArtMovieThumb, string>
  {
    #region Static instance

    public static SeriesFanArtTvMatcher Instance
    {
      get { return ServiceRegistration.Get<SeriesFanArtTvMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArtTV\");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromMinutes(10);

    #endregion

    #region Init

    public SeriesFanArtTvMatcher() : 
      base(CACHE_PATH, MAX_MEMCACHE_DURATION, false)
    {
    }

    public override Task<bool> InitWrapperAsync(bool useHttps)
    {
      try
      {
        FanArtTVWrapper wrapper = new FanArtTVWrapper();
        // Try to lookup online content in the configured language
        CultureInfo mpLocal = new CultureInfo(PreferredLanguageCulture);
        wrapper.SetPreferredLanguage(mpLocal.TwoLetterISOLanguageName);
        if (wrapper.Init(CACHE_PATH, useHttps))
        {
          _wrapper = wrapper;
          return Task.FromResult(true);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SeriesFanArtTvMatcher: Error initializing wrapper", ex);
      }
      return Task.FromResult(false);
    }

    #endregion

    #region External match storage

    public override void StoreActorMatch(PersonInfo person)
    {
    }

    public override void StoreDirectorMatch(PersonInfo person)
    {
    }

    public override void StoreWriterMatch(PersonInfo person)
    {
    }

    public override void StoreCharacterMatch(CharacterInfo character)
    {
    }

    public override void StoreCompanyMatch(CompanyInfo company)
    {
    }

    public override void StoreTvNetworkMatch(CompanyInfo company)
    {
    }

    #endregion

    #region Metadata updaters

    public override Task<bool> UpdateSeriesPersonsAsync(SeriesInfo seriesInfo, string occupation)
    {
      return Task.FromResult(false);
    }

    public override Task<bool> UpdateSeriesCharactersAsync(SeriesInfo seriesInfo)
    {
      return Task.FromResult(false);
    }

    public override Task<bool> UpdateSeriesCompaniesAsync(SeriesInfo seriesInfo, string companyType)
    {
      return Task.FromResult(false);
    }

    public override Task<bool> UpdateEpisodePersonsAsync(EpisodeInfo episodeInfo, string occupation)
    {
      return Task.FromResult(false);
    }

    public override Task<bool> UpdateEpisodeCharactersAsync(EpisodeInfo episodeInfo)
    {
      return Task.FromResult(false);
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

    #endregion

    #region FanArt

    protected override bool VerifyFanArtImage(FanArtMovieThumb image, string language, string fanArtType)
    {
      if (image.Language == null || image.Language == language)
        return true;
      return false;
    }

    #endregion
  }
}
