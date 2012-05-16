using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data;
using Newtonsoft.Json;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3
{
  internal class MovieDbApiV3
  {
    #region Constants

    public const string DefaultLanguage = "en";

    private const string URL_API_BASE =   "http://api.themoviedb.org/3/";
    private const string URL_QUERY =      URL_API_BASE + "search/movie";
    private const string URL_GETMOVIE =   URL_API_BASE + "movie/{0}";
    private const string URL_GETIMAGES =  URL_API_BASE + "movie/{0}/images";
    private const string URL_GETCONFIG =  URL_API_BASE + "configuration";

    #endregion

    #region Fields

    private readonly string _apiKey;
    private readonly string _cachePath;
    private Configuration _configuration;

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

    public MovieDbApiV3(string apiKey, string cachePath)
    {
      _apiKey = apiKey;
      _cachePath = cachePath;
    }

    #endregion

    #region Public members

    /// <summary>
    /// Search for movies by name given in <paramref name="query"/> using the <paramref name="language"/>.
    /// </summary>
    /// <param name="query">Full or partly name of movie</param>
    /// <param name="language">Language</param>
    /// <returns>List of possible matches</returns>
    public List<MovieSearchResult> SearchMovie(string query, string language)
    {
      string url = GetUrl(URL_QUERY, language) + "&query=" + query;
      PagedMovieSearchResult results = Download<PagedMovieSearchResult>(url);
      return results.Results;
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="Movie"/> with given <paramref name="id"/>. This method caches request
    /// to same movies using the cache path given in <see cref="MovieDbApiV3"/> constructor.
    /// </summary>
    /// <param name="id">TMDB id of movie</param>
    /// <param name="language">Language</param>
    /// <returns>Movie information</returns>
    public Movie GetMovie(int id, string language)
    {
      string cache = CreateAndGetCacheName(id, language);
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        string json = File.ReadAllText(cache);
        return JsonConvert.DeserializeObject<Movie>(json);
      }
      string url = GetUrl(URL_GETMOVIE, language, id);
      return Download<Movie>(url, cache);
    }

    /// <summary>
    /// Returns configuration about image download servers and available sizes.
    /// </summary>
    /// <returns></returns>
    public Configuration GetImageConfiguration()
    {
      string url = GetUrl(URL_GETCONFIG, null);
      return Download<Configuration>(url);
    }

    /// <summary>
    /// Returns a <see cref="ImageCollection"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">TMDB id of movie</param>
    /// <param name="language">Language</param>
    /// <returns>Image collection</returns>
    public ImageCollection GetImages(int id, string language)
    {
      string url = GetUrl(URL_GETIMAGES, language, id);
      ImageCollection result = Download<ImageCollection>(url);
      result.SetMovieIds();
      return result;
    }

    /// <summary>
    /// Downloads images in "original" size and saves them to cache.
    /// </summary>
    /// <param name="image">Image to download</param>
    /// <param name="category">Image category (Poster, Cover, Backdrop...)</param>
    /// <returns><c>true</c> if successful</returns>
    public bool DownloadImage(MovieImage image, string category)
    {
      string cacheFileName = CreateAndGetCacheName(image, category);
      if (string.IsNullOrEmpty(cacheFileName))
        return false;

      string sourceUri = Configuration.Images.BaseUrl + "original" + image.FilePath;
      DownloadFile(sourceUri, cacheFileName);
      return true;
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
      return string.Format("{0}?api_key={1}", replacedUrl, _apiKey) + (string.IsNullOrEmpty(language) ? "" : "&language=" + language);
    }

    /// <summary>
    /// Downloads the requested information from the JSON api and deserializes the response to the requested <typeparam name="TE">Type</typeparam>.
    /// This method can save the response to local cache, if a valid path is passed in <paramref name="saveCacheFile"/>.
    /// </summary>
    /// <typeparam name="TE">Target type</typeparam>
    /// <param name="url">Url to download</param>
    /// <param name="saveCacheFile">Optional name for saving response to cache</param>
    /// <returns>Downloaded object</returns>
    protected TE Download<TE>(string url, string saveCacheFile = null)
    {
      string json = DownloadJSON(url);
      if (!string.IsNullOrEmpty(saveCacheFile))
        WriteCache(saveCacheFile, json);
      return JsonConvert.DeserializeObject<TE>(json);
    }

    /// <summary>
    /// Downloads the JSON string from API.
    /// </summary>
    /// <param name="url">Url to download</param>
    /// <returns>JSON result</returns>
    protected string DownloadJSON(string url)
    {
      WebClient webClient = new WebClient { Encoding = Encoding.UTF8 };
      webClient.Headers["Accept"] = "application/json";
      return webClient.DownloadString(url);
    }

    /// <summary>
    /// Donwload a file from given <paramref name="url"/> and save it to <paramref name="downloadFile"/>.
    /// </summary>
    /// <param name="url">Url to download</param>
    /// <param name="downloadFile">Target file name</param>
    /// <returns><c>true</c> if successful</returns>
    protected bool DownloadFile(string url, string downloadFile)
    {
      try
      {
        WebClient webClient = new WebClient();
        webClient.DownloadFile(url, downloadFile);
        return true;
      }
      catch (Exception ex)
      {
        // TODO: logging
        return false;
      }
    }

    /// <summary>
    /// Creates a local file name for loading and saving <see cref="MovieImage"/>s.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="category"></param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName(MovieImage image, string category)
    {
      try
      {
        string folder = Path.Combine(_cachePath, string.Format(@"{0}\{1}", image.MovieId, category));
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        return Path.Combine(folder, image.FilePath.TrimStart(new[] { '/' }));
      }
      catch
      {
        // TODO: logging
        return null;
      }
    }

    /// <summary>
    /// Creates a local file name for loading and saving details for movie.
    /// </summary>
    /// <param name="movieId"></param>
    /// <param name="language"></param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName(int movieId, string language)
    {
      try
      {
        string folder = Path.Combine(_cachePath, movieId.ToString());
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        return Path.Combine(folder, string.Format("movie_{0}.json", language));
      }
      catch
      {
        // TODO: logging
        return null;
      }
    }

    /// <summary>
    /// Writes JSON strings to cache file.
    /// </summary>
    /// <param name="cachePath"></param>
    /// <param name="json"></param>
    protected void WriteCache(string cachePath, string json)
    {
      if (string.IsNullOrEmpty(cachePath))
        return;

      using(FileStream fs = new FileStream(cachePath, FileMode.Create, FileAccess.Write))
      {
        using (StreamWriter sw = new StreamWriter(fs))
        {
          sw.Write(json);
          sw.Close();
        }
        fs.Close();
      }
    }

    #endregion
  }
}
