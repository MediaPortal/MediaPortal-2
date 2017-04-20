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
using System.Globalization;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public class MovieTheMovieDbMatcher : MovieMatcher<ImageItem, string>
  {
    #region Static instance

    public static MovieTheMovieDbMatcher Instance
    {
      get { return ServiceRegistration.Get<MovieTheMovieDbMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TheMovieDB\");
    private static readonly TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromMinutes(10);

    #endregion

    #region Init

    public MovieTheMovieDbMatcher() :
      base(CACHE_PATH, MAX_MEMCACHE_DURATION, true)
    {
      Primary = true;
    }

    public override bool InitWrapper(bool useHttps)
    {
      try
      {
        TheMovieDbWrapper wrapper = new TheMovieDbWrapper();
        // Try to lookup online content in the configured language
        CultureInfo currentCulture = new CultureInfo(PreferredLanguageCulture);
        wrapper.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
        if (wrapper.Init(CACHE_PATH, useHttps, true))
        {
          _wrapper = wrapper;
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(Id + ": Error initializing wrapper", ex);
      }
      return false;
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

    protected override bool GetMovieCollectionId(MovieCollectionInfo movieCollection, out string id)
    {
      id = null;
      if (movieCollection.MovieDbId > 0)
        id = movieCollection.MovieDbId.ToString();
      return id != null;
    }

    protected override bool SetMovieCollectionId(MovieCollectionInfo movieCollection, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        movieCollection.MovieDbId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    protected override bool GetCompanyId(CompanyInfo company, out string id)
    {
      id = null;
      if (company.MovieDbId > 0)
        id = company.MovieDbId.ToString();
      return id != null;
    }

    protected override bool SetCompanyId(CompanyInfo company, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        company.MovieDbId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    protected override bool GetCharacterId(CharacterInfo character, out string id)
    {
      id = null;
      if (character.MovieDbId > 0)
        id = character.MovieDbId.ToString();
      return id != null;
    }

    protected override bool SetCharacterId(CharacterInfo character, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        character.MovieDbId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    protected override bool GetPersonId(PersonInfo person, out string id)
    {
      id = null;
      if (person.MovieDbId > 0)
        id = person.MovieDbId.ToString();
      return id != null;
    }

    protected override bool SetPersonId(PersonInfo person, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        person.MovieDbId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    #endregion

    #region FanArt

    protected override bool VerifyFanArtImage(ImageItem image, string language)
    {
      if (image.Language == null || image.Language == language)
        return true;
      return false;
    }

    #endregion
  }
}
