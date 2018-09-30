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
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using System;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public class MovieOmDbMatcher : MovieMatcher<object, string>
  {
    #region Static instance

    public static MovieOmDbMatcher Instance
    {
      get { return ServiceRegistration.Get<MovieOmDbMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\OmDB\");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromMinutes(10);

    #endregion

    #region Init

    public MovieOmDbMatcher() : 
      base(CACHE_PATH, MAX_MEMCACHE_DURATION, false)
    {
      //TODO: Disabled for now. Has gone private for the time being.
      Enabled = false;
    }

    public override Task<bool> InitWrapperAsync(bool useHttps)
    {
      try
      {
        OmDbWrapper wrapper = new OmDbWrapper();
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
        movie.ImdbId = id;
        return true;
      }
      return false;
    }

    protected override bool GetMovieId(MovieInfo movie, out string id)
    {
      id = null;
      if (!string.IsNullOrEmpty(movie.ImdbId))
        id = movie.ImdbId;
      return id != null;
    }

    #endregion

    #region Caching

    protected override void RefreshCache()
    {
      // TODO: when updating movie information is implemented, start here a job to do it
    }

    #endregion
  }
}
