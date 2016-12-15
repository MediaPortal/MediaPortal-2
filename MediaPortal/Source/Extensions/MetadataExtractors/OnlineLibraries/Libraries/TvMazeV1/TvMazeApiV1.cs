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
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvMazeV1.Data;
using System.Linq;
using System;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvMazeV1
{
  internal class TvMazeApiV1
  {
    #region Constants

    public const string DefaultLanguage = "en";

    private const string URL_API_BASE = "http://api.tvmaze.com/";
    private const string URL_QUERYSERIES = URL_API_BASE + "search/shows?q={0}";
    private const string URL_QUERYPEOPLE = URL_API_BASE + "search/people?q={0}";
    private const string URL_GETTVDBSERIES = URL_API_BASE + "lookup/shows?thetvdb={0}";
    private const string URL_GETIMDBIDSERIES = URL_API_BASE + "lookup/shows?imdb={0}";
    private const string URL_GETSERIES = URL_API_BASE + "shows/{0}?embed[]=episodes&embed[]=cast";
    private const string URL_GETSEASONS = URL_API_BASE + "shows/{0}/seasons";
    private const string URL_GETCAST = URL_API_BASE + "shows/{0}/cast";
    private const string URL_GETEPISODE =  URL_API_BASE + "shows/{0}/episodebynumber?season={1}&number={2}";
    private const string URL_GETPERSON = URL_API_BASE + "people/{0}";
    private const string URL_GETCHARACTER =  URL_API_BASE + "characters/{0}";
    private const string URL_GETSERIESCHANGES = URL_API_BASE + "updates/shows";

    #endregion

    #region Fields

    private readonly string _cachePath;
    private readonly Downloader _downloader;

    #endregion

    #region Constructor

    public TvMazeApiV1(string cachePath)
    {
      _cachePath = cachePath;
      _downloader = new Downloader { EnableCompression = true };
      _downloader.Headers["Accept"] = "application/json";
    }

    #endregion

    #region Public members

    /// <summary>
    /// Return the last change date of all series.
    /// </summary>
    public Dictionary<int, DateTime> GetSeriesChangeDates()
    {
      string url = GetUrl(URL_GETSERIESCHANGES);
      Dictionary<string, long> results = _downloader.Download<Dictionary<string, long>>(url);
      if (results == null) return null;
      return results.ToDictionary(entry => Convert.ToInt32(entry.Key), entry => Util.UnixToDotNet(entry.Value.ToString()));
    }

    /// <summary>
    /// Deletes all cache files for the specified series.
    /// </summary>
    /// <param name="id">TvMaze id of series</param>
    /// <returns></returns>
    public void DeleteSeriesCache(int id)
    {
      string folder = Path.Combine(_cachePath, id.ToString());
      if (!Directory.Exists(folder))
        return;

      string cacheFileMask = Path.GetFileName(CreateAndGetCacheName(id, "Series*"));
      string[] cacheFiles = Directory.GetFiles(folder, cacheFileMask);
      foreach (string file in cacheFiles)
      {
        try
        {
          _downloader.DeleteCache(file);
        }
        catch
        { }
      }

      cacheFileMask = Path.GetFileName(CreateAndGetCacheName(id, "Season*"));
      cacheFiles = Directory.GetFiles(folder, cacheFileMask);
      foreach (string file in cacheFiles)
      {
        try
        {
          _downloader.DeleteCache(file);
        }
        catch
        { }
      }
    }

    /// <summary>
    /// Search for series by name given in <paramref name="title"/>.
    /// </summary>
    /// <param name="title">Full or partly name of series</param>
    /// <returns>List of possible matches</returns>
    public List<TvMazeSeries> SearchSeries(string title)
    {
      string url = GetUrl(URL_QUERYSERIES, HttpUtility.UrlEncode(title));
      List<TvMazeSeriesSearchResult> results = _downloader.Download<List<TvMazeSeriesSearchResult>>(url);
      if (results == null) return null;
      return results.Select(e => e.Series).ToList();
    }

    /// <summary>
    /// Search for people by name given in <paramref name="name"/>.
    /// </summary>
    /// <param name="name">Full or partly name of person</param>
    /// <returns>List of possible matches</returns>
    public List<TvMazePerson> SearchPerson(string name)
    {
      string url = GetUrl(URL_QUERYPEOPLE, HttpUtility.UrlEncode(name));
      List<TvMazePersonSearchResult> results = _downloader.Download<List<TvMazePersonSearchResult>>(url);
      if (results == null) return null;
      return results.Select(e => e.Person).ToList();
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="TvMazeSeries"/> with given <paramref name="id"/>. This method caches request
    /// to same series using the cache path given in <see cref="TvMazeApiV1"/> constructor.
    /// </summary>
    /// <param name="id">TvMaze id of Series</param>
    /// <returns>Series information</returns>
    public TvMazeSeries GetSeries(int id, bool cacheOnly)
    {
      string cache = CreateAndGetCacheName(id, "Series");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<TvMazeSeries>(cache);
      }
      if (cacheOnly) return null;
      string url = GetUrl(URL_GETSERIES, id);
      return _downloader.Download<TvMazeSeries>(url, cache);
    }

    /// <summary>
    /// Returns cache file for <see cref="TvMazeSeries"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of series</param>
    /// <returns>Cache file name</returns>
    public string GetSeriesCacheFile(int id)
    {
      return CreateAndGetCacheName(id, "Series");
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="TvMazeSeries"/> with given <paramref name="id"/>. This method caches request
    /// to same series using the cache path given in <see cref="TvMazeApiV1"/> constructor.
    /// </summary>
    /// <param name="id">IMDB id of Series</param>
    /// <returns>Series information</returns>
    public TvMazeSeries GetSeriesByImDb(string id, bool cacheOnly)
    {
      string cache = CreateAndGetCacheName(id, "Series");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<TvMazeSeries>(cache);
      }
      if (cacheOnly) return null;
      string url = GetUrl(URL_GETIMDBIDSERIES, id);
      return _downloader.Download<TvMazeSeries>(url, cache);
    }

    /// <summary>
    /// Returns cache file for <see cref="TvMazeSeries"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of series</param>
    /// <returns>Cache file name</returns>
    public string GetSeriesImdbCacheFile(string ImdbId)
    {
      return CreateAndGetCacheName(ImdbId, "Series");
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="TvMazeSeries"/> with given <paramref name="id"/>. This method caches request
    /// to same series using the cache path given in <see cref="TvMazeApiV1"/> constructor.
    /// </summary>
    /// <param name="id">TvDB id of Series</param>
    /// <returns>Series information</returns>
    public TvMazeSeries GetSeriesByTvDb(int id, bool cacheOnly)
    {
      string cache = CreateAndGetCacheName(id, "Series");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<TvMazeSeries>(cache);
      }
      if (cacheOnly) return null;
      string url = GetUrl(URL_GETTVDBSERIES, id);
      return _downloader.Download<TvMazeSeries>(url, cache);
    }

    /// <summary>
    /// Returns cache file for <see cref="TvMazeSeries"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of series</param>
    /// <returns>Cache file name</returns>
    public string GetSeriesTvdbCacheFile(int id)
    {
      return CreateAndGetCacheName(id, "Series");
    }

    /// <summary>
    /// Returns detailed season information for a single <see cref="TvMazeSeries"/> with given <paramref name="id"/>. This method caches request
    /// to same series using the cache path given in <see cref="TvMazeApiV1"/> constructor.
    /// </summary>
    /// <param name="id">TvMaze id of series</param>
    /// <returns>Season information</returns>
    public List<TvMazeSeason> GetSeriesSeasons(int id, bool cacheOnly)
    {
      string cache = CreateAndGetCacheName(id, "Seasons");
      TvMazeSeriesSeasonSearch returnValue = null;
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        returnValue = _downloader.ReadCache<TvMazeSeriesSeasonSearch>(cache);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_GETSEASONS, id);
        returnValue = _downloader.Download<TvMazeSeriesSeasonSearch>(url, cache);
      }
      if (returnValue.Results == null) return null;
      return returnValue.Results;
    }

    /// <summary>
    /// Returns season cache file for <see cref="TvMazeSeries"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of series</param>
    /// <returns>Cache file name</returns>
    public string GetSeriesSeasonsCacheFile(int id)
    {
      return CreateAndGetCacheName(id, "Seasons");
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="TvMazeEpisode"/> with given <paramref name="id"/>. This method caches request
    /// to same episodes using the cache path given in <see cref="TvMazeApiV1"/> constructor.
    /// </summary>
    /// <param name="id">TvMaze id of series</param>
    /// <param name="season">Season number</param>
    /// <param name="episode">Episode number</param>
    /// <returns>Episode information</returns>
    public TvMazeEpisode GetSeriesEpisode(int id, int season, int episode, bool cacheOnly)
    {
      if (season == 0) //Does not support special episode requests
        return null;

      string cache = CreateAndGetCacheName(id, string.Format("Season{0}_Episode{1}", season, episode));
      TvMazeEpisode returnValue = null;
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        returnValue = _downloader.ReadCache<TvMazeEpisode>(cache);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_GETEPISODE, id, season, episode);
        returnValue = _downloader.Download<TvMazeEpisode>(url, cache);
      }
      return returnValue;
    }

    /// <summary>
    /// Returns season cache file for <see cref="TvMazeEpisode"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of series</param>
    /// <returns>Cache file name</returns>
    public string GetSeriesEpisodeCacheFile(int id, int season, int episode)
    {
      return CreateAndGetCacheName(id, string.Format("Season{0}_Episode{1}", season, episode));
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="TvMazePerson"/> with given <paramref name="id"/>. This method caches request
    /// to same person using the cache path given in <see cref="TvMazeApiV1"/> constructor.
    /// </summary>
    /// <param name="id">TvMaze id of person</param>
    /// <returns>Person information</returns>
    public TvMazePerson GetPerson(int id, bool cacheOnly)
    {
      string cache = CreateAndGetCacheName(id, "Person");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<TvMazePerson>(cache);
      }
      if (cacheOnly) return null;
      string url = GetUrl(URL_GETPERSON, id);
      return _downloader.Download<TvMazePerson>(url, cache);
    }

    /// <summary>
    /// Returns season cache file for <see cref="TvMazePerson"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of person</param>
    /// <returns>Cache file name</returns>
    public string GetPersonCacheFile(int id)
    {
      return CreateAndGetCacheName(id, "Person");
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="TvMazePerson"/> with given <paramref name="id"/>. This method caches request
    /// to same character using the cache path given in <see cref="TvMazeApiV1"/> constructor.
    /// </summary>
    /// <param name="id">TvMaze id of character</param>
    /// <returns>Character information</returns>
    public TvMazePerson GetCharacter(int id, bool cacheOnly)
    {
      string cache = CreateAndGetCacheName(id, "Character");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<TvMazePerson>(cache);
      }
      if (cacheOnly) return null;
      string url = GetUrl(URL_GETCHARACTER, id);
      return _downloader.Download<TvMazePerson>(url, cache);
    }

    /// <summary>
    /// Returns season cache file for <see cref="TvMazePerson"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of person</param>
    /// <returns>Cache file name</returns>
    public string GetCharacterCacheFile(int id)
    {
      return CreateAndGetCacheName(id, "Character");
    }

    /// <summary>
    /// Downloads images in "original" size and saves them to cache.
    /// </summary>
    /// <param name="image">Image to download</param>
    /// <param name="folderPath">The folder to store the image</param>
    /// <returns><c>true</c> if successful</returns>
    public bool DownloadImage(int id, TvMazeImageCollection image, string folderPath)
    {
      string imageUrl = image.OriginalUrl ?? image.MediumUrl;
      string cacheFileName = CreateAndGetCacheName(id, imageUrl, folderPath);
      if (string.IsNullOrEmpty(cacheFileName))
        return false;

      string sourceUri = imageUrl;
      _downloader.DownloadFile(sourceUri, cacheFileName);
      return true;
    }

    public byte[] GetImage(int id, TvMazeImageCollection image, string folderPath)
    {
      string imageUrl = image.OriginalUrl ?? image.MediumUrl;
      string cacheFileName = CreateAndGetCacheName(id, imageUrl, folderPath);
      if (string.IsNullOrEmpty(cacheFileName))
        return null;

      return _downloader.ReadDownloadedFile(cacheFileName);
    }

    #endregion

    #region Protected members

    /// <summary>
    /// Builds and returns the full request url.
    /// </summary>
    /// <param name="urlBase">Query base</param>
    /// <param name="args">Optional arguments to format <paramref name="urlBase"/></param>
    /// <returns>Complete url</returns>
    protected string GetUrl(string urlBase, params object[] args)
    {
      return string.Format(urlBase, args);
    }

    /// <summary>
    /// Creates a local file name for loading and saving details for movie. It supports both TMDB id and IMDB id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="prefix"></param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName<T>(T id, string prefix)
    {
      try
      {
        string folder = Path.Combine(_cachePath, id.ToString());
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

    /// <summary>
    /// Creates a local file name for loading and saving images.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="imageUrl"></param>
    /// <param name="folderPath"></param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName(int id, string imageUrl, string folderPath)
    {
      try
      {
        string prefix = string.Format(@"TVM({0})_", id);
        if (!Directory.Exists(folderPath))
          Directory.CreateDirectory(folderPath);
        return Path.Combine(folderPath, prefix + imageUrl.Substring(imageUrl.LastIndexOf('/') + 1));
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
