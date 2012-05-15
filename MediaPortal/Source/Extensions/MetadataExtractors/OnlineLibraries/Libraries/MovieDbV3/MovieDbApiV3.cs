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
    public const string DefaultLanguage = "en";

    private const string URL_QUERY = "http://api.themoviedb.org/3/search/movie";
    private const string URL_GETMOVIE = "http://api.themoviedb.org/3/movie/{0}";
    private const string URL_GETIMAGES = "http://api.themoviedb.org/3/movie/{0}/images";
    private const string URL_GETCONFIG = "http://api.themoviedb.org/3/configuration";

    private readonly string _apiKey;
    private readonly string _cachePath;
    private Configuration _configuration;

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

    public MovieDbApiV3(string apiKey, string cachePath)
    {
      _apiKey = apiKey;
      _cachePath = cachePath;
    }

    public List<MovieSearchResult> SearchMovie(string query, string language)
    {
      string url = GetUrl(URL_QUERY, language) + "&query=" + query;
      string json = DownloadJSON(url);
      PagedMovieSearchResult results = JsonConvert.DeserializeObject<PagedMovieSearchResult>(json);
      return results.Results;
    }

    public Movie GetMovie(int id, string language)
    {
      string cache = CreateAndGetCacheName(id, language);
      string json;
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        json = File.ReadAllText(cache);
      }
      else
      {
        string url = GetUrl(URL_GETMOVIE, language, id);
        json = DownloadJSON(url);
        WriteCache(cache, json);
      }
      Movie result = JsonConvert.DeserializeObject<Movie>(json);
      return result;
    }

    public Configuration GetImageConfiguration()
    {
      string url = GetUrl(URL_GETCONFIG, null);
      string json = DownloadJSON(url);
      Configuration result = JsonConvert.DeserializeObject<Configuration>(json);
      return result;
    }

    public ImageCollection GetImages(int id, string language)
    {
      string url = GetUrl(URL_GETIMAGES, language, id);
      string json = DownloadJSON(url);
      ImageCollection result = JsonConvert.DeserializeObject<ImageCollection>(json);
      result.SetMovieIds();
      return result;
    }

    public bool DownloadImage(MovieImage image, string category)
    {
      string cacheFileName = CreateAndGetCacheName(image, category);
      if (string.IsNullOrEmpty(cacheFileName))
        return false;

      string sourceUri = Configuration.Images.BaseUrl + "original" + image.FilePath;
      DownloadFile(sourceUri, cacheFileName);
      return true;
    }

    protected string GetUrl(string urlBase, string language, params object[] args)
    {
      string replacedUrl = string.Format(urlBase, args);
      return string.Format("{0}?api_key={1}", replacedUrl, _apiKey) + (string.IsNullOrEmpty(language) ? "" : "&language=" + language);
    }

    protected string DownloadJSON(string url)
    {
      WebClient webClient = new WebClient { Encoding = Encoding.UTF8 };
      webClient.Headers["Accept"] = "application/json";
      return webClient.DownloadString(url);
    }

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
  }
}
