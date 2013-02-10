#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
    private const string URL_GETCASTCREW = URL_API_BASE + "movie/{0}/casts";
    private const string URL_GETIMAGES =  URL_API_BASE + "movie/{0}/images";
    private const string URL_GETCONFIG =  URL_API_BASE + "configuration";

    #endregion

    #region Fields

    private readonly string _apiKey;
    private readonly string _cachePath;
    private Configuration _configuration;
    private readonly Downloader _downloader;

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
      _downloader = new Downloader { EnableCompression = true };
      _downloader.Headers["Accept"] = "application/json";
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
      string url = GetUrl(URL_QUERY, language) + "&query=" + HttpUtility.UrlEncode(query);
      PagedMovieSearchResult results = _downloader.Download<PagedMovieSearchResult>(url);
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
      return _downloader.Download<Movie>(url, cache);
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="Movie"/> with given <paramref name="imdbId"/>. This method caches request
    /// to same movies using the cache path given in <see cref="MovieDbApiV3"/> constructor.
    /// </summary>
    /// <param name="imdbId">IMDB id of movie</param>
    /// <param name="language">Language</param>
    /// <returns>Movie information</returns>
    public Movie GetMovie(string imdbId, string language)
    {
      string cache = CreateAndGetCacheName(imdbId, language);
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        string json = File.ReadAllText(cache);
        return JsonConvert.DeserializeObject<Movie>(json);
      }
      string url = GetUrl(URL_GETMOVIE, language, imdbId);
      return _downloader.Download<Movie>(url, cache);
    }

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
    /// Returns a <see cref="MovieCasts"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">TMDB id of movie</param>
    /// <returns>Cast and Crew</returns>
    public MovieCasts GetCastCrew(int id)
    {
      string url = GetUrl(URL_GETCASTCREW, null, id);
      MovieCasts result = _downloader.Download<MovieCasts>(url);
      return result;
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
      ImageCollection result = _downloader.Download<ImageCollection>(url);
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
      _downloader.DownloadFile(sourceUri, cacheFileName);
      return true;
    }

    public bool DownloadImages(MovieCollection movieCollection)
    {
      DownloadImages(movieCollection, true);
      DownloadImages(movieCollection, false);
      return true;
    }

    private bool DownloadImages(MovieCollection movieCollection, bool usePoster)
    {
      string cacheFileName = CreateAndGetCacheName(movieCollection, usePoster);
      if (string.IsNullOrEmpty(cacheFileName))
        return false;

      string sourceUri = Configuration.Images.BaseUrl + "original" + (usePoster ? movieCollection.PosterPath : movieCollection.BackdropPath);
      _downloader.DownloadFile(sourceUri, cacheFileName);
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
    /// Creates a local file name for loading and saving images of a <see cref="MovieCollection"/>.
    /// </summary>
    /// <param name="collection">MovieCollection</param>
    /// <param name="usePoster"><c>true</c> for Poster, <c>false</c> for Backdrop</param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName(MovieCollection collection, bool usePoster)
    {
      try
      {
        string folder = Path.Combine(_cachePath, string.Format(@"COLL_{0}\{1}", collection.Id, usePoster ? "Posters" : "Backdrops"));
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        string fileName = usePoster ? collection.PosterPath : collection.BackdropPath;
        if (string.IsNullOrEmpty(fileName))
          return null;
        return Path.Combine(folder, fileName.TrimStart(new[] { '/' }));
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
    protected string CreateAndGetCacheName<TE>(TE movieId, string language)
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

    #endregion
  }
}
