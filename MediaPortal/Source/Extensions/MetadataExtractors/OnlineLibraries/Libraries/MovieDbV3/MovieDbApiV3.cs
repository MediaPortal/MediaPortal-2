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

using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3
{
  internal class MovieDbApiV3
  {
    #region Constants

    public const string DefaultLanguage = "en";

    private const string URL_API_BASE = "api.themoviedb.org/3/";
    private const string URL_MOVIEQUERY = URL_API_BASE + "search/movie";
    private const string URL_GETMOVIE = URL_API_BASE + "movie/{0}";
    private const string URL_GETMOVIECASTCREW = URL_API_BASE + "movie/{0}/casts";
    private const string URL_GETMOVIEIMAGES = URL_API_BASE + "movie/{0}/images";
    private const string URL_GETPERSON = URL_API_BASE + "person/{0}";
    private const string URL_GETCOLLECTION = URL_API_BASE + "collection/{0}";
    private const string URL_GETCOLLECTIONIMAGES = URL_API_BASE + "collection/{0}/images";
    private const string URL_GETCOMPANY = URL_API_BASE + "company/{0}";
    private const string URL_GETNETWORK = URL_API_BASE + "network/{0}";
    private const string URL_GETPERSONIMAGES = URL_API_BASE + "person/{0}/images";
    private const string URL_SERIESQUERY = URL_API_BASE + "search/tv";
    private const string URL_GETSERIES = URL_API_BASE + "tv/{0}";
    private const string URL_GETSERIESIMAGES = URL_API_BASE + "tv/{0}/images";
    private const string URL_GETSERIESCASTCREW = URL_API_BASE + "tv/{0}/credits";
    private const string URL_GETSEASON = URL_API_BASE + "tv/{0}/season/{1}";
    private const string URL_GETSEASONIMAGES = URL_API_BASE + "tv/{0}/season/{1}/images";
    private const string URL_GETSEASONCASTCREW = URL_API_BASE + "tv/{0}/season/{1}/credits";
    private const string URL_GETEPISODE = URL_API_BASE + "tv/{0}/season/{1}/episode/{2}";
    private const string URL_GETEPISODEIMAGES = URL_API_BASE + "tv/{0}/season/{1}/episode/{2}/images";
    private const string URL_GETEPISODECASTCREW = URL_API_BASE + "tv/{0}/season/{1}/episode/{2}/credits";
    private const string URL_IDQUERY = URL_API_BASE + "find/{0}";
    private const string URL_COMPANYQUERY = URL_API_BASE + "search/company";
    private const string URL_PERSONQUERY = URL_API_BASE + "search/person";
    private const string URL_GETCONFIG = URL_API_BASE + "configuration";
    private const string URL_GETSERIESCHANGES = URL_API_BASE + "tv/changes";
    private const string URL_GETMOVIECHANGES = URL_API_BASE + "movie/changes";
    private const string URL_GETPERSONCHANGES = URL_API_BASE + "person/changes";

    private const string EXTERNAL_SOURCE_IMDB = "imdb_id";
    private const string EXTERNAL_SOURCE_TVRAGE = "tvrage_id";
    private const string EXTERNAL_SOURCE_TVDB = "tvdb_id";

    private const string PREFIX_MOVIE = "Movie";
    private const string PREFIX_CREW = "Crew";
    private const string PREFIX_COLLECTION = "Collection";
    private const string PREFIX_COMPANY = "Company";
    private const string PREFIX_NETWORK = "Network";
    private const string PREFIX_PERSON = "Person";
    private const string PREFIX_SERIES = "Series";
    private const string PREFIX_SERIES_CREW = "Series_Crew";
    private const string PREFIX_SEASON_FORMAT = "Season{0}";
    private const string PREFIX_SEASON_CREW_FORMAT = "Season{0}_Crew";
    private const string PREFIX_SEASON_EPISODE_FORMAT = "Season{0}_Episode{1}";
    private const string PREFIX_SEASON_EPISODE_CREW_FORMAT = "Season{0}_Episode{1}_Crew";

    #endregion

    #region Fields

    private readonly string _apiKey;
    private readonly string _cachePath;
    private Configuration _configuration;
    private readonly MovieDbDownloader _downloader;
    private Dictionary<int, List<int>> _collectionMovieList = new Dictionary<int, List<int>>();
    private readonly bool _useHttps;

    #endregion

    #region Properties

    public Configuration Configuration
    {
      get
      {
        if (_configuration != null)
          return _configuration;
        _configuration = GetImageConfiguration();
        return _configuration;
      }
    }

    #endregion

    #region Constructor

    public MovieDbApiV3(string apiKey, string cachePath, bool useHttps)
    {
      _apiKey = apiKey;
      _cachePath = cachePath;
      _useHttps = useHttps;
      _downloader = new MovieDbDownloader { EnableCompression = true };
      _downloader.Headers["Accept"] = "application/json";
    }

    #endregion

    #region Public members

    /// <summary>
    /// Returns configuration about image download servers and available sizes.
    /// </summary>
    /// <returns></returns>
    public Configuration GetImageConfiguration()
    {
      string url = GetUrl(URL_GETCONFIG, null);
      return _downloader.Download<Configuration>(url);
    }

    /// <summary>
    /// Returns a collection of changed movies.
    /// </summary>
    /// <returns></returns>
    public ChangeCollection GetMovieChanges(int page, DateTime startTime)
    {
      //Returns changes for the last 24 hours
      string url = GetUrl(URL_GETMOVIECHANGES, null);
      url += "&page=" + page;
      url += "&start_date=" + startTime.ToString(@"yyyy\-MM\-dd");
      return _downloader.Download<ChangeCollection>(url);
    }

    /// <summary>
    /// Returns a collection of changed persons.
    /// </summary>
    /// <returns></returns>
    public ChangeCollection GetPersonChanges(int page, DateTime startTime)
    {
      //Returns changes for the last 24 hours
      string url = GetUrl(URL_GETPERSONCHANGES, null);
      url += "&page=" + page;
      url += "&start_date=" + startTime.ToString(@"yyyy\-MM\-dd");
      return _downloader.Download<ChangeCollection>(url);
    }

    /// <summary>
    /// Returns a collection of changed series.
    /// </summary>
    /// <returns></returns>
    public ChangeCollection GetSeriesChanges(int page, DateTime startTime)
    {
      //Returns changes for the last 24 hours
      string url = GetUrl(URL_GETSERIESCHANGES, null);
      url += "&page=" + page;
      url += "&start_date=" + startTime.ToString(@"yyyy\-MM\-dd");
      return _downloader.Download<ChangeCollection>(url);
    }

    /// <summary>
    /// Deletes all cache files for the specified movie.
    /// </summary>
    /// <param name="id">TMDB id of movie</param>
    /// <returns></returns>
    public void DeleteMovieCache(int id)
    {
      string folder = Path.Combine(_cachePath, id.ToString());
      if (!Directory.Exists(folder))
        return;

      string cacheFileMask = Path.GetFileName(CreateAndGetCacheName(id, "*", "Movie"));
      string[] cacheFiles = Directory.GetFiles(folder, cacheFileMask);
      foreach(string file in cacheFiles)
      {
        try
        {
          _downloader.DeleteCacheAsync(file).Wait();
        }
        catch
        { }
      }

      cacheFileMask = Path.GetFileName(CreateAndGetCacheName(id, "*", "Crew"));
      cacheFiles = Directory.GetFiles(folder, cacheFileMask);
      foreach (string file in cacheFiles)
      {
        try
        {
          _downloader.DeleteCacheAsync(file).Wait();
        }
        catch
        { }
      }
    }

    /// <summary>
    /// Deletes all cache files for the specified movie collection.
    /// </summary>
    /// <param name="id">TMDB id of movie collection</param>
    /// <returns></returns>
    public void DeleteMovieCollectionCache(int id)
    {
      string folder = Path.Combine(_cachePath, id.ToString());
      if (!Directory.Exists(folder))
        return;

      string cacheFileMask = Path.GetFileName(CreateAndGetCacheName(id, "*", "Collection"));
      string[] cacheFiles = Directory.GetFiles(folder, cacheFileMask);
      foreach (string file in cacheFiles)
      {
        try
        {
          _downloader.DeleteCacheAsync(file).Wait();
        }
        catch
        { }
      }
    }

    /// <summary>
    /// Deletes all cache files for the specified person.
    /// </summary>
    /// <param name="id">TMDB id of person</param>
    /// <returns></returns>
    public void DeletePersonCache(int id)
    {
      string folder = Path.Combine(_cachePath, id.ToString());
      if (!Directory.Exists(folder))
        return;

      string cacheFileMask = Path.GetFileName(CreateAndGetCacheName(id, "*", "Person"));
      string[] cacheFiles = Directory.GetFiles(folder, cacheFileMask);
      foreach (string file in cacheFiles)
      {
        try
        {
          File.Delete(file);
        }
        catch
        { }
      }
    }

    /// <summary>
    /// Deletes all cache files for the specified series.
    /// </summary>
    /// <param name="id">TMDB id of series</param>
    /// <returns></returns>
    public void DeleteSeriesCache(int id)
    {
      string folder = Path.Combine(_cachePath, id.ToString());
      if (!Directory.Exists(folder))
        return;

      string cacheFileMask = Path.GetFileName(CreateAndGetCacheName(id, "*", "Series"));
      string[] cacheFiles = Directory.GetFiles(folder, cacheFileMask);
      foreach (string file in cacheFiles)
      {
        try
        {
          File.Delete(file);
        }
        catch
        { }
      }

      cacheFileMask = Path.GetFileName(CreateAndGetCacheName(id, "*", "Season*"));
      cacheFiles = Directory.GetFiles(folder, cacheFileMask);
      foreach (string file in cacheFiles)
      {
        try
        {
          File.Delete(file);
        }
        catch
        { }
      }
    }

    protected Task<T> SearchAsync<T>(string queryUrl, string query, string language)
    {
      string url = GetUrl(queryUrl, language) + "&query=" + HttpUtility.UrlEncode(query);
      return _downloader.DownloadAsync<T>(url);
    }

    /// <summary>
    /// Search for movies by name given in <paramref name="query"/> using the <paramref name="language"/>.
    /// </summary>
    /// <param name="query">Full or partly name of movie</param>
    /// <param name="language">Language</param>
    /// <returns>List of possible matches</returns>
    public async Task<List<MovieSearchResult>> SearchMovieAsync(string query, string language)
    {
      PagedMovieSearchResult result = await SearchAsync<PagedMovieSearchResult>(URL_MOVIEQUERY, query, language).ConfigureAwait(false);
      return result.Results;
    }

    /// <summary>
    /// Search for series by name given in <paramref name="query"/> using the <paramref name="language"/>.
    /// </summary>
    /// <param name="query">Full or partly name of series</param>
    /// <param name="language">Language</param>
    /// <returns>List of possible matches</returns>
    public async Task<List<SeriesSearchResult>> SearchSeriesAsync(string query, string language)
    {
      PagedSeriesSearchResult result = await SearchAsync<PagedSeriesSearchResult>(URL_SERIESQUERY, query, language).ConfigureAwait(false);
      return result.Results;
    }

    public async Task<List<PersonSearchResult>> SearchPersonAsync(string query, string language)
    {
      PagedPersonSearchResult result = await SearchAsync<PagedPersonSearchResult>(URL_PERSONQUERY, query, language).ConfigureAwait(false);
      return result.Results;
    }

    public async Task<List<CompanySearchResult>> SearchCompanyAsync(string query, string language)
    {
      PagedCompanySearchResult result = await SearchAsync<PagedCompanySearchResult>(URL_COMPANYQUERY, query, language).ConfigureAwait(false);
      return result.Results;
    }

    protected Task<IdSearchResult> FindByExternalIdAsync(object id, string language, string externalSource)
    {
      string url = GetUrl(URL_IDQUERY, language, id) + "&external_source=" + externalSource;
      return _downloader.DownloadAsync<IdSearchResult>(url);
    }

    public async Task<List<IdResult>> FindMovieByImdbId(string imDbId, string language)
    {
      IdSearchResult result = await FindByExternalIdAsync(imDbId, language, EXTERNAL_SOURCE_IMDB).ConfigureAwait(false);
      return result.MovieResults;
    }

    public async Task<List<IdResult>> FindPersonByImdbIdAsync(string imDbId, string language)
    {
      IdSearchResult result = await FindByExternalIdAsync(imDbId, language, EXTERNAL_SOURCE_IMDB).ConfigureAwait(false);
      return result.PersonResults;
    }

    public async Task<List<IdResult>> FindPersonByTvRageIdAsync(int tvRageId, string language)
    {
      IdSearchResult result = await FindByExternalIdAsync(tvRageId, language, EXTERNAL_SOURCE_TVRAGE).ConfigureAwait(false);
      return result.PersonResults;
    }

    public async Task<List<IdResult>> FindSeriesByImdbIdAsync(string imDbId, string language)
    {
      IdSearchResult result = await FindByExternalIdAsync(imDbId, language, EXTERNAL_SOURCE_IMDB).ConfigureAwait(false);
      return result.SeriesResults;
    }

    public async Task<List<IdResult>> FindSeriesByTvDbIdAsync(int tvDbId, string language)
    {
      IdSearchResult result = await FindByExternalIdAsync(tvDbId, language, EXTERNAL_SOURCE_TVDB).ConfigureAwait(false);
      return result.SeriesResults;
    }

    public async Task<List<IdResult>> FindSeriesByTvRageIdAsync(int tvRageId, string language)
    {
      IdSearchResult result = await FindByExternalIdAsync(tvRageId, language, EXTERNAL_SOURCE_TVRAGE).ConfigureAwait(false);
      return result.SeriesResults;
    }

    public async Task<List<IdResult>> FindSeriesSeasonByTvDbIdAsync(int tvDbId, string language)
    {
      IdSearchResult result = await FindByExternalIdAsync(tvDbId, language, EXTERNAL_SOURCE_TVDB).ConfigureAwait(false);
      return result.SeriesSeasonResults;
    }

    public async Task<List<IdResult>> FindSeriesSeasonByTvRageIdAsync(int tvRageId, string language)
    {
      IdSearchResult result = await FindByExternalIdAsync(tvRageId, language, EXTERNAL_SOURCE_TVRAGE).ConfigureAwait(false);
      return result.SeriesSeasonResults;
    }

    public async Task<List<IdResult>> FindSeriesEpisodeByImdbIdAsync(string imDbId, string language)
    {
      IdSearchResult result = await FindByExternalIdAsync(imDbId, language, EXTERNAL_SOURCE_IMDB).ConfigureAwait(false);
      return result.SeriesEpisodeResults;
    }

    public async Task<List<IdResult>> FindSeriesEpisodeByTvDbIdAsync(int tvDbId, string language)
    {
      IdSearchResult result = await FindByExternalIdAsync(tvDbId, language, EXTERNAL_SOURCE_TVDB).ConfigureAwait(false);
      return result.SeriesEpisodeResults;
    }

    public async Task<List<IdResult>> FindSeriesEpisodeByTvRageIdAsync(int tvRageId, string language)
    {
      IdSearchResult result = await FindByExternalIdAsync(tvRageId, language, EXTERNAL_SOURCE_TVRAGE).ConfigureAwait(false);
      return result.SeriesEpisodeResults;
    }

    protected Task<T> GetAsync<T>(string getUrl, string prefix, int id, string language, bool cacheOnly, string appendToQuery = null)
    {
      return GetAsync<T>(getUrl, prefix, id.ToString(), language, cacheOnly, appendToQuery);
    }

    protected Task<T> GetAsync<T>(string getUrl, string prefix, string id, string language, bool cacheOnly, string appendToQuery = null)
    {
      string cache = CreateAndGetCacheName(id, language, prefix);
      string url = GetUrl(getUrl, language, id);
      if (!string.IsNullOrEmpty(appendToQuery))
        url += "&append_to_response=" + appendToQuery;
      return GetAsync<T>(url, cache, cacheOnly);
    }

    protected Task<T> GetAsync<T>(string url, string cacheName, bool cacheOnly)
    {
      if (!string.IsNullOrEmpty(cacheName) && File.Exists(cacheName))
        return Task.FromResult(_downloader.ReadCache<T>(cacheName));
      if (cacheOnly) return Task.FromResult(default(T));
      return _downloader.DownloadAsync<T>(url, cacheName);
    }

    protected async Task<ImageCollection> GetImagesAsync(string getUrl, string language, params object[] args)
    {
      string url = GetUrl(getUrl, language, args);
      ImageCollection result = await _downloader.DownloadAsync<ImageCollection>(url).ConfigureAwait(false);
      result.SetMovieIds();
      return result;
    }

    protected async Task<Movie> GetMovieByIdAsync(string id, string language, bool cacheOnly)
    {
      Movie movie = await GetAsync<Movie>(URL_GETMOVIE, PREFIX_MOVIE, id, language, cacheOnly).ConfigureAwait(false);
      if (movie != null && movie.Id > 0 && movie.Collection != null && movie.Collection.Id > 0)
      {
        lock (_collectionMovieList)
        {
          if (!_collectionMovieList.ContainsKey(movie.Collection.Id))
            _collectionMovieList.Add(movie.Collection.Id, new List<int>());
          if (!_collectionMovieList[movie.Collection.Id].Contains(movie.Id))
            _collectionMovieList[movie.Collection.Id].Add(movie.Id);
        }
      }
      return movie;
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="Movie"/> with given <paramref name="id"/>. This method caches request
    /// to same movies using the cache path given in <see cref="MovieDbApiV3"/> constructor.
    /// </summary>
    /// <param name="id">TMDB id of movie</param>
    /// <param name="language">Language</param>
    /// <returns>Movie information</returns>
    public Task<Movie> GetMovieAsync(int id, string language, bool cacheOnly)
    {
      return GetMovieByIdAsync(id.ToString(), language, cacheOnly);
    }

    /// <summary>
    /// Returns cache file for a single <see cref="Movie"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of movie</param>
    /// <returns>Cache file name</returns>
    public string GetMovieCacheFile(int id, string language)
    {
      return CreateAndGetCacheName(id, language, PREFIX_MOVIE);
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="Movie"/> with given <paramref name="imdbId"/>. This method caches request
    /// to same movies using the cache path given in <see cref="MovieDbApiV3"/> constructor.
    /// </summary>
    /// <param name="imdbId">IMDB id of movie</param>
    /// <param name="language">Language</param>
    /// <returns>Movie information</returns>
    public Task<Movie> GetMovieAsync(string imdbId, string language, bool cacheOnly)
    {
      return GetMovieByIdAsync(imdbId, language, cacheOnly);
    }

    /// <summary>
    /// Returns cache file for a single <see cref="Movie"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of movie</param>
    /// <returns>Cache file name</returns>
    public string GetMovieCacheFile(string imdbId, string language)
    {
      return CreateAndGetCacheName(imdbId, language, PREFIX_MOVIE);
    }

    /// <summary>
    /// Returns a <see cref="MovieCasts"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">TMDB id of movie</param>
    /// <returns>Cast and Crew</returns>
    public Task<MovieCasts> GetMovieCastCrewAsync(int id, string language, bool cacheOnly)
    {
      string url = GetUrl(URL_GETMOVIECASTCREW, null, id);
      string cache = CreateAndGetCacheName(id, language, PREFIX_CREW);
      return GetAsync<MovieCasts>(url, cache, cacheOnly);
    }

    /// <summary>
    /// Returns cache file for <see cref="MovieCasts"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of movie</param>
    /// <returns>Cache file name</returns>
    public string GetMovieCastCrewCacheFile(int id, string language)
    {
      return CreateAndGetCacheName(id, language, PREFIX_CREW);
    }

    /// <summary>
    /// Returns a <see cref="ImageCollection"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">TMDB id of movie</param>
    /// <param name="language">Language</param>
    /// <returns>Image collection</returns>
    public Task<ImageCollection> GetMovieImagesAsync(int id, string language)
    {
      return GetImagesAsync(URL_GETMOVIEIMAGES, language, id);
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="MovieCollection"/> with given <paramref name="id"/>. This method caches request
    /// to same collection using the cache path given in <see cref="MovieDbApiV3"/> constructor.
    /// </summary>
    /// <param name="id">TMDB id of collection</param>
    /// <param name="language">Language</param>
    /// <returns>Collection information</returns>
    public async Task<MovieCollection> GetCollectionAsync(int id, string language, bool cacheOnly)
    {
      string cache = CreateAndGetCacheName(id, language, PREFIX_COLLECTION);
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        MovieCollection collection = await _downloader.ReadCacheAsync<MovieCollection>(cache).ConfigureAwait(false);
        if (collection != null)
        {
          bool expired = false;
          lock (_collectionMovieList)
          {
            if (_collectionMovieList.ContainsKey(id))
            {
              //Check if any movie has been found as part of the collection that is not found in the cache
              if (_collectionMovieList[id].Except(collection.Movies.Select(m => m.Id)).Any())
                expired = true;
            }
            else
            {
              _collectionMovieList.Add(id, new List<int>());
              _collectionMovieList[id].AddRange(collection.Movies.Select(m => m.Id));
            }
          }
          if (expired)
            await _downloader.DeleteCacheAsync(cache).ConfigureAwait(false);
          else
            return collection;
        }
      }
      if (cacheOnly) return null;
      string url = GetUrl(URL_GETCOLLECTION, language, id);
      return await _downloader.DownloadAsync<MovieCollection>(url, cache).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns cache file for <see cref="MovieCollection"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of collection</param>
    /// <returns>Cache file name</returns>
    public string GetCollectionCacheFile(int id, string language)
    {
      return CreateAndGetCacheName(id, language, PREFIX_COLLECTION);
    }

    /// <summary>
    /// Returns a <see cref="ImageCollection"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">TMDB id of movie collection</param>
    /// <param name="language">Language</param>
    /// <returns>Image collection</returns>
    public Task<ImageCollection> GetMovieCollectionImagesAsync(int id, string language)
    {
      return GetImagesAsync(URL_GETCOLLECTIONIMAGES, language, id);
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="Company"/> with given <paramref name="id"/>. This method caches request
    /// to same company using the cache path given in <see cref="MovieDbApiV3"/> constructor.
    /// </summary>
    /// <param name="id">TMDB id of company</param>
    /// <param name="language">Language</param>
    /// <returns>Company information</returns>
    public Task<Company> GetCompanyAsync(int id, string language, bool cacheOnly)
    {
      return GetAsync<Company>(URL_GETCOMPANY, PREFIX_COMPANY, id, language, cacheOnly);
    }

    /// <summary>
    /// Returns cache file for <see cref="Company"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of company</param>
    /// <returns>Cache file name</returns>
    public string GetCompanyCacheFile(int id, string language)
    {
      return CreateAndGetCacheName(id, language, PREFIX_COMPANY);
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="Network"/> with given <paramref name="id"/>. This method caches request
    /// to same network using the cache path given in <see cref="MovieDbApiV3"/> constructor.
    /// </summary>
    /// <param name="id">TMDB id of network</param>
    /// <param name="language">Language</param>
    /// <returns>Network information</returns>
    public Task<Network> GetNetworkAsync(int id, string language, bool cacheOnly)
    {
      return GetAsync<Network>(URL_GETNETWORK, PREFIX_NETWORK, id, language, cacheOnly);
    }

    /// <summary>
    /// Returns cache file for <see cref="Network"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of network</param>
    /// <returns>Cache file name</returns>
    public string GetNetworkCacheFile(int id, string language)
    {
      return CreateAndGetCacheName(id, language, PREFIX_NETWORK);
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="Person"/> with given <paramref name="id"/>. This method caches request
    /// to same person using the cache path given in <see cref="MovieDbApiV3"/> constructor.
    /// </summary>
    /// <param name="id">TMDB id of person</param>
    /// <param name="language">Language</param>
    /// <returns>Person information</returns>
    public Task<Person> GetPersonAsync(int id, string language, bool cacheOnly)
    {
      return GetAsync<Person>(URL_GETPERSON, PREFIX_PERSON, id, language, cacheOnly, "external_ids");
    }

    /// <summary>
    /// Returns cache file for <see cref="Person"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of person</param>
    /// <returns>Cache file name</returns>
    public string GetPersonCacheFile(int id, string language)
    {
      return CreateAndGetCacheName(id, language, PREFIX_PERSON);
    }

    /// <summary>
    /// Returns a <see cref="ImageCollection"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">TMDB id of person</param>
    /// <param name="language">Language</param>
    /// <returns>Image collection</returns>
    public Task<ImageCollection> GetPersonImagesAsync(int id, string language)
    {
      return GetImagesAsync(URL_GETPERSONIMAGES, language, id);
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="Series"/> with given <paramref name="id"/>. This method caches request
    /// to same series using the cache path given in <see cref="MovieDbApiV3"/> constructor.
    /// </summary>
    /// <param name="id">TMDB id of series</param>
    /// <param name="language">Language</param>
    /// <returns>Series information</returns>
    public Task<Series> GetSeriesAsync(int id, string language, bool cacheOnly)
    {
      return GetAsync<Series>(URL_GETSERIES, PREFIX_SERIES, id, language, cacheOnly, "external_ids,content_ratings");
    }

    /// <summary>
    /// Returns cache file for <see cref="Series"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of series</param>
    /// <returns>Cache file name</returns>
    public string GetSeriesCacheFile(int id, string language)
    {
      return CreateAndGetCacheName(id, language, PREFIX_SERIES);
    }

    /// <summary>
    /// Returns a <see cref="MovieCasts"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">TMDB id of series</param>
    /// <returns>Cast and Crew</returns>
    public Task<MovieCasts> GetSeriesCastCrewAsync(int id, string language, bool cacheOnly)
    {
      string url = GetUrl(URL_GETSERIESCASTCREW, null, id);
      string cache = CreateAndGetCacheName(id, language, PREFIX_SERIES_CREW);
      return GetAsync<MovieCasts>(url, cache, cacheOnly);
    }

    /// <summary>
    /// Returns cache file for <see cref="MovieCasts"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of series</param>
    /// <returns>Cache file name</returns>
    public string GetSeriesCastCrewCacheFile(int id, string language)
    {
      return CreateAndGetCacheName(id, language, PREFIX_SERIES_CREW);
    }

    /// <summary>
    /// Returns a <see cref="ImageCollection"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">TMDB id of series</param>
    /// <param name="language">Language</param>
    /// <returns>Image collection</returns>
    public Task<ImageCollection> GetSeriesImagesAsync(int id, string language)
    {
      return GetImagesAsync(URL_GETSERIESIMAGES, language, id);
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="Season"/> with given <paramref name="id"/>. This method caches request
    /// to same seasons using the cache path given in <see cref="MovieDbApiV3"/> constructor.
    /// </summary>
    /// <param name="id">TMDB id of series</param>
    /// <param name="season">Season number</param>
    /// <param name="language">Language</param>
    /// <returns>Season information</returns>
    public Task<Season> GetSeriesSeasonAsync(int id, int season, string language, bool cacheOnly)
    {
      string url = GetUrl(URL_GETSEASON, language, id, season) + "&append_to_response=external_ids";
      string cache = CreateAndGetCacheName(id, language, string.Format(PREFIX_SEASON_FORMAT, season));
      return GetAsync<Season>(url, cache, cacheOnly);
    }

    /// <summary>
    /// Returns cache file for <see cref="Season"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of series</param>
    /// <returns>Cache file name</returns>
    public string GetSeriesSeasonCacheFile(int id, int season, string language)
    {
      return CreateAndGetCacheName(id, language, string.Format(PREFIX_SEASON_FORMAT, season));
    }

    /// <summary>
    /// Returns a <see cref="MovieCasts"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">TMDB id of series</param>
    /// <param name="season">Season number</param>
    /// <returns>Cast and Crew</returns>
    public Task<MovieCasts> GetSeriesSeasonCastCrewAsync(int id, int season, string language, bool cacheOnly)
    {
      string url = GetUrl(URL_GETSEASONCASTCREW, null, id, season);
      string cache = CreateAndGetCacheName(id, language, string.Format(PREFIX_SEASON_CREW_FORMAT, season));
      return GetAsync<MovieCasts>(url, cache, cacheOnly);
    }

    /// <summary>
    /// Returns cache file for <see cref="MovieCasts"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of series</param>
    /// <returns>Cache file name</returns>
    public string GetSeriesSeasonCastCrewCacheFile(int id, int season, string language)
    {
      return CreateAndGetCacheName(id, language, string.Format(PREFIX_SEASON_CREW_FORMAT, season));
    }

    /// <summary>
    /// Returns a <see cref="ImageCollection"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">TMDB id of series</param>
    /// <param name="season">Season number</param>
    /// <param name="language">Language</param>
    /// <returns>Image collection</returns>
    public Task<ImageCollection> GetSeriesSeasonImagesAsync(int id, int season, string language)
    {
      return GetImagesAsync(URL_GETSEASONIMAGES, language, id, season);
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="Episode"/> with given <paramref name="id"/>. This method caches request
    /// to same episodes using the cache path given in <see cref="MovieDbApiV3"/> constructor.
    /// </summary>
    /// <param name="id">TMDB id of series</param>
    /// <param name="season">Season number</param>
    /// <param name="episode">Episode number</param>
    /// <param name="language">Language</param>
    /// <returns>Episode information</returns>
    public Task<Episode> GetSeriesEpisodeAsync(int id, int season, int episode, string language, bool cacheOnly)
    {
      string url = GetUrl(URL_GETEPISODE, language, id, season, episode) + "&append_to_response=external_ids";
      string cache = CreateAndGetCacheName(id, language, string.Format(PREFIX_SEASON_EPISODE_FORMAT, season, episode));
      return GetAsync<Episode>(url, cache, cacheOnly);
    }

    /// <summary>
    /// Returns cache file for <see cref="Episode"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of series</param>
    /// <returns>Cache file name</returns>
    public string GetSeriesEpisodeCacheFile(int id, int season, int episode, string language)
    {
      return CreateAndGetCacheName(id, language, string.Format(PREFIX_SEASON_EPISODE_FORMAT, season, episode));
    }

    /// <summary>
    /// Returns a <see cref="MovieCasts"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">TMDB id of series</param>
    /// <param name="season">Season number</param>
    /// <param name="episode">Episode number</param>
    /// <returns>Cast and Crew</returns>
    public Task<MovieCasts> GetSeriesEpisodeCastCrewAsync(int id, int season, int episode, string language, bool cacheOnly)
    {
      string url = GetUrl(URL_GETEPISODECASTCREW, null, id, season, episode);
      string cache = CreateAndGetCacheName(id, language, string.Format(PREFIX_SEASON_EPISODE_CREW_FORMAT, season, episode));
      return GetAsync<MovieCasts>(url, cache, cacheOnly);
    }

    /// <summary>
    /// Returns cache file for <see cref="MovieCasts"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Id of series</param>
    /// <returns>Cache file name</returns>
    public string GetSeriesEpisodeCastCrewCacheFile(int id, int season, int episode, string language)
    {
      return CreateAndGetCacheName(id, language, string.Format(PREFIX_SEASON_EPISODE_CREW_FORMAT, season, episode));
    }

    /// <summary>
    /// Returns a <see cref="ImageCollection"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">TMDB id of series</param>
    /// <param name="season">Season number</param>
    /// <param name="episode">Episode number</param>
    /// <param name="language">Language</param>
    /// <returns>Image collection</returns>
    public Task<ImageCollection> GetSeriesEpisodeImagesAsync(int id, int season, int episode, string language)
    {
      return GetImagesAsync(URL_GETEPISODEIMAGES, language, id, season, episode);
    }

    /// <summary>
    /// Downloads images in "original" size and saves them to cache.
    /// </summary>
    /// <param name="image">Image to download</param>
    /// <param name="folderPath">The folder to store the image</param>
    /// <returns><c>true</c> if successful</returns>
    public Task<bool> DownloadImageAsync(string Id, ImageItem image, string folderPath)
    {
      string cacheFileName = CreateAndGetCacheName(Id, image, folderPath);
      if (string.IsNullOrEmpty(cacheFileName))
        return Task.FromResult(false);

      string sourceUri = Configuration.Images.BaseUrl + "original" + image.FilePath;
      return _downloader.DownloadFileAsync(sourceUri, cacheFileName);
    }

    public byte[] GetImage(string Id, ImageItem image, string folderPath)
    {
      string cacheFileName = CreateAndGetCacheName(Id, image, folderPath);
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
    /// <param name="language">Language</param>
    /// <param name="args">Optional arguments to format <paramref name="urlBase"/></param>
    /// <returns>Complete url</returns>
    protected string GetUrl(string urlBase, string language, params object[] args)
    {
      string replacedUrl = string.Format(urlBase, args);
      if (_useHttps)
        return string.Format("https://{0}?api_key={1}", replacedUrl, _apiKey) + (string.IsNullOrEmpty(language) ? "" : "&language=" + language);
      else
        return string.Format("http://{0}?api_key={1}", replacedUrl, _apiKey) + (string.IsNullOrEmpty(language) ? "" : "&language=" + language);
    }
    /// <summary>
    /// Creates a local file name for loading and saving <see cref="ImageItem"/>s.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="folderPath"></param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName(string id, ImageItem image, string folderPath)
    {
      try
      {
        string prefix = string.Format(@"TMDB({0})_", id);
        if (!Directory.Exists(folderPath))
          Directory.CreateDirectory(folderPath);
        if (string.IsNullOrEmpty(image.FilePath)) return null;
        return Path.Combine(folderPath, prefix + image.FilePath.TrimStart(new[] { '/' }));
      }
      catch
      {
        // TODO: logging
        return null;
      }
    }

    /// <summary>
    /// Creates a local file name for loading and saving details for movie. It supports both TMDB id and IMDB id.
    /// </summary>
    /// <param name="movieId"></param>
    /// <param name="language"></param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName<TE>(TE movieId, string language, string prefix)
    {
      try
      {
        string folder = Path.Combine(_cachePath, movieId.ToString());
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        return Path.Combine(folder, string.Format("{0}_{1}.json", prefix, language));
      }
      catch
      {
        // TODO: logging
        return null;
      }
    }

    #endregion
  }
}
