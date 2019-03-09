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
  /// <see cref="MovieFanArtTvMatcher"/> is used to download movie images from FanArt.tv.
  /// </summary>
  public class MovieFanArtTvMatcher : MovieMatcher<FanArtMovieThumb, string>
  {
    #region Static instance

    public static MovieFanArtTvMatcher Instance
    {
      get { return ServiceRegistration.Get<MovieFanArtTvMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArtTV\");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromMinutes(10);

    #endregion
    
    #region Init

    public MovieFanArtTvMatcher() : 
      base(CACHE_PATH, MAX_MEMCACHE_DURATION, false)
    {
    }

    public override Task<bool> InitWrapperAsync(bool useHttps)
    {
      try
      {
        FanArtTVWrapper wrapper = new FanArtTVWrapper();
        // Try to lookup online content in the configured language
        CultureInfo currentCulture = new CultureInfo(PreferredLanguageCulture);
        wrapper.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
        if (wrapper.Init(CACHE_PATH, useHttps))
        {
          _wrapper = wrapper;
          return Task.FromResult(true);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(Id + ": Error initializing wrapper", ex);
      }
      return Task.FromResult(false);
    }

    #endregion

    #region Metadata updaters

    public override Task<bool> UpdatePersonsAsync(MovieInfo movieInfo, string occupation)
    {
      return Task.FromResult(false);
    }

    public override Task<bool> UpdateCharactersAsync(MovieInfo movieInfo)
    {
      return Task.FromResult(false);
    }

    public override Task<bool> UpdateCompaniesAsync(MovieInfo movieInfo, string companyType)
    {
      return Task.FromResult(false);
    }

    public override Task<bool> UpdateCollectionAsync(MovieCollectionInfo movieCollectionInfo, bool updateMovieList)
    {
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

    #endregion

    #region Translators

    protected override bool SetMovieId(MovieInfo movie, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        movie.MovieDbId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    protected override bool GetMovieId(MovieInfo movie, out string id)
    {
      id = null;
      if (movie.MovieDbId > 0)
        id = movie.MovieDbId.ToString();
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
