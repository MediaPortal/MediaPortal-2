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

using System.Collections.Generic;
using System.IO;
using System.Web;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using Newtonsoft.Json;
using MediaPortal.Common.Logging;
using MediaPortal.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1.Data;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1
{
  internal class OmDbApiV1
  {
    //Images cannot currently be downloaded

    #region Constants

    public const string DefaultLanguage = "en";

    private const string URL_API_BASE = "www.omdbapi.com/";
    private const string URL_QUERYMOVIE = URL_API_BASE + "?s={0}&type=movie";
    private const string URL_QUERYSERIES = URL_API_BASE + "?s={0}&type=series";
    private const string URL_GETTITLEMOVIE = URL_API_BASE + "?t={0}&type=movie";
    private const string URL_GETIMDBIDMOVIE =   URL_API_BASE + "?i={0}&type=movie";
    private const string URL_GETTITLESERIES = URL_API_BASE + "?t={0}&type=series";
    private const string URL_GETIMDBIDSERIES = URL_API_BASE + "?i={0}&type=series";
    private const string URL_GETTITLESEASON = URL_API_BASE + "?t={0}&Season={1}&type=series";
    private const string URL_GETIMDBIDSEASON =  URL_API_BASE + "?i={0}&Season={1}&type=series";
    private const string URL_GETTITLEEPISODE = URL_API_BASE + "?t={0}&Season={1}&episode={2}&type=episode";
    private const string URL_GETIMDBIDEPISODE =  URL_API_BASE + "?i={0}&Season={1}&episode={2}&type=episode";

    #endregion

    #region Fields

    private readonly string _cachePath;
    private readonly Downloader _downloader;
    private object _movieSync = new object();
    private object _seriesSync = new object();
    private object _seasonSync = new object();
    private object _episodeSync = new object();
    private readonly bool _useHttps;

    #endregion

    #region Constructor

    public OmDbApiV1(string cachePath, bool useHttps)
    {
      _cachePath = cachePath;
      _useHttps = useHttps;
      _downloader = new Downloader { EnableCompression = true };
      _downloader.Headers["Accept"] = "application/json";
    }

    #endregion

    #region Public members

    /// <summary>
    /// Search for movies by name given in <paramref name="title"/>.
    /// </summary>
    /// <param name="title">Full or partly name of movie</param>
    /// <returns>List of possible matches</returns>
    public List<OmDbSearchItem> SearchMovie(string title, int year)
    {
      string url = GetUrl(URL_QUERYMOVIE, year, false, false, HttpUtility.UrlEncode(title));
      OmDbSearchResult results = _downloader.Download<OmDbSearchResult>(url);
      if (results.ResponseValid == false) return null;
      foreach (OmDbSearchItem item in results.SearchResults) item.AssignProperties();
      return results.SearchResults;
    }

    /// <summary>
    /// Search for series by name given in <paramref name="title"/>.
    /// </summary>
    /// <param name="title">Full or partly name of series</param>
    /// <returns>List of possible matches</returns>
    public List<OmDbSearchItem> SearchSeries(string title, int year)
    {
      string url = GetUrl(URL_QUERYSERIES, year, false, false, HttpUtility.UrlEncode(title));
      OmDbSearchResult results = _downloader.Download<OmDbSearchResult>(url);
      if (results.ResponseValid == false) return null;
      foreach (OmDbSearchItem item in results.SearchResults) item.AssignProperties();
      return results.SearchResults;
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="OmDbMovie"/> with given <paramref name="id"/>. This method caches request
    /// to same movies using the cache path given in <see cref="OmDbApiV1"/> constructor.
    /// </summary>
    /// <param name="id">IMDB id of movie</param>
    /// <returns>Movie information</returns>
    public OmDbMovie GetMovie(string id, bool cacheOnly)
    {
      lock (_movieSync)
      {
        string cache = CreateAndGetCacheName(id, "Movie");
        OmDbMovie returnValue = null;
        if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
        {
          returnValue = _downloader.ReadCache<OmDbMovie>(cache);
        }
        else
        {
          if (cacheOnly) return null;
          string url = GetUrl(URL_GETIMDBIDMOVIE, 0, true, true, id);
          returnValue = _downloader.Download<OmDbMovie>(url, cache);
        }
        if (returnValue == null) return null;
        if (returnValue.ResponseValid == false) return null;
        if (returnValue != null) returnValue.AssignProperties();
        return returnValue;
      }
    }

    /// <summary>
    /// Returns cache file for a single <see cref="OmDbMovie"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">IMDB id of movie</param>
    /// <returns>Cache file name</returns>
    public string GetMovieCacheFile(string id)
    {
      return CreateAndGetCacheName(id, "Movie");
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="OmDbSeries"/> with given <paramref name="id"/>. This method caches request
    /// to same series using the cache path given in <see cref="OmDbApiV1"/> constructor.
    /// </summary>
    /// <param name="id">IMDB id of series</param>
    /// <returns>Series information</returns>
    public OmDbSeries GetSeries(string id, bool cacheOnly)
    {
      lock (_seriesSync)
      {
        string cache = CreateAndGetCacheName(id, "Series");
        OmDbSeries returnValue = null;
        if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
        {
          returnValue = _downloader.ReadCache<OmDbSeries>(cache);
        }
        else
        {
          if (cacheOnly) return null;
          string url = GetUrl(URL_GETIMDBIDSERIES, 0, true, true, id);
          returnValue = _downloader.Download<OmDbSeries>(url, cache);
        }
        if (returnValue == null) return null;
        if (returnValue.ResponseValid == false) return null;
        if (returnValue != null) returnValue.AssignProperties();
        return returnValue;
      }
    }

    /// <summary>
    /// Returns cache file for a single <see cref="OmDbSeries"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">IMDB id of series</param>
    /// <returns>Cache file name</returns>
    public string GetSeriesCacheFile(string id)
    {
      return CreateAndGetCacheName(id, "Series");
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="OmDbSeason"/> with given <paramref name="id"/>. This method caches request
    /// to same seasons using the cache path given in <see cref="OmDbApiV1"/> constructor.
    /// </summary>
    /// <param name="id">IMDB id of series</param>
    /// <param name="season">Season number</param>
    /// <returns>Season information</returns>
    public OmDbSeason GetSeriesSeason(string id, int season, bool cacheOnly)
    {
      lock (_seasonSync)
      {
        string cache = CreateAndGetCacheName(id, string.Format("Season{0}", season));
        OmDbSeason returnValue = null;
        if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
        {
          returnValue = _downloader.ReadCache<OmDbSeason>(cache);
        }
        else
        {
          if (cacheOnly) return null;
          string url = GetUrl(URL_GETIMDBIDSEASON, 0, true, true, id, season);
          returnValue = _downloader.Download<OmDbSeason>(url, cache);
        }
        if (returnValue == null) return null;
        if (returnValue.ResponseValid == false) return null;
        if (returnValue != null) returnValue.InitEpisodes();
        return returnValue;
      }
    }

    /// <summary>
    /// Returns cache file for a single <see cref="OmDbSeason"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">IMDB id of season</param>
    /// <returns>Cache file name</returns>
    public string GetSeriesSeasonCacheFile(string id, int season)
    {
      return CreateAndGetCacheName(id, string.Format("Season{0}", season));
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="OmDbEpisode"/> with given <paramref name="id"/>. This method caches request
    /// to same episodes using the cache path given in <see cref="OmDbApiV1"/> constructor.
    /// </summary>
    /// <param name="id">IMDB id of series</param>
    /// <param name="season">Season number</param>
    /// <param name="episode">Episode number</param>
    /// <returns>Episode information</returns>
    public OmDbEpisode GetSeriesEpisode(string id, int season, int episode, bool cacheOnly)
    {
      lock (_episodeSync)
      {
        string cache = CreateAndGetCacheName(id, string.Format("Season{0}_Episode{1}", season, episode));
        OmDbEpisode returnValue = null;
        if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
        {
          returnValue = _downloader.ReadCache<OmDbEpisode>(cache);
        }
        else
        {
          if (cacheOnly) return null;
          string url = GetUrl(URL_GETIMDBIDEPISODE, 0, true, true, id, season, episode);
          returnValue = _downloader.Download<OmDbEpisode>(url, cache);
        }
        if (returnValue == null) return null;
        if (returnValue.ResponseValid == false) return null;
        if (returnValue != null) returnValue.AssignProperties();
        return returnValue;
      }
    }

    /// <summary>
    /// Returns cache file for a single <see cref="OmDbEpisode"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">IMDB id of episode</param>
    /// <returns>Cache file name</returns>
    public string GetSeriesEpisodeCacheFile(string id, int season, int episode)
    {
      return CreateAndGetCacheName(id, string.Format("Season{0}_Episode{1}", season, episode));
    }

    #endregion

    #region Protected members

    /// <summary>
    /// Builds and returns the full request url.
    /// </summary>
    /// <param name="urlBase">Query base</param>
    /// <param name="args">Optional arguments to format <paramref name="urlBase"/></param>
    /// <returns>Complete url</returns>
    protected string GetUrl(string urlBase, int year, bool fullPlot, bool includeTomatoRating, params object[] args)
    {
      string replacedUrl = string.Format(urlBase, args);
      if (year > 0) replacedUrl += "&y=" + year.ToString();
      if(fullPlot) replacedUrl += "&plot=full";
      else replacedUrl += "&plot=short";
      if(includeTomatoRating) replacedUrl += "&tomatoes=true";
      else replacedUrl += "&tomatoes=false";

      if(_useHttps)
        return string.Format("https://{0}&r=json", replacedUrl);
      else
        return string.Format("http://{0}&r=json", replacedUrl);
    }

    /// <summary>
    /// Creates a local file name for loading and saving details for movie. It supports both TMDB id and IMDB id.
    /// </summary>
    /// <param name="movieId"></param>
    /// <param name="prefix"></param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName<TE>(TE movieId, string prefix)
    {
      try
      {
        string folder = Path.Combine(_cachePath, movieId.ToString());
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        return Path.Combine(folder, string.Format("{0}.json", prefix));
      }
      catch
      {
        // TODO: logging
        return null;
      }
    }

    protected static ILogger Logger
    {
      get
      {
        return ServiceRegistration.Get<ILogger>();
      }
    }

    #endregion
  }
}
