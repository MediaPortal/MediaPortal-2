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
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;

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
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromMinutes(1);

    #endregion

    #region Init

    public MovieOmDbMatcher() : 
      base(CACHE_PATH, MAX_MEMCACHE_DURATION)
    {
    }

    public override bool InitWrapper(bool useHttps)
    {
      try
      {
        OmDbWrapper wrapper = new OmDbWrapper();
        if (wrapper.Init(CACHE_PATH, useHttps))
        {
          _wrapper = wrapper;
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("MovieOmDbMatcher: Error initializing wrapper", ex);
      }
      return false;
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

    #region FanArt

    public override List<string> GetFanArtFiles<T>(T infoObject, string scope, string type)
    {
      // No fanart
      return new List<string>();
    }

    protected override void DownloadFanArt(string downloadId)
    {
      // No fanart to download
      FinishDownloadFanArt(downloadId);
    }

    #endregion
  }
}
