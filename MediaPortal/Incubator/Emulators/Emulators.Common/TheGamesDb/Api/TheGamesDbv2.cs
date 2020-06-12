using Emulators.Common.TheGamesDb.Data;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Emulators.Common.TheGamesDb.Api
{
  public class TheGamesDbV2
  {
    #region Image types

    public const string IMAGE_TYPE_BOXART = "boxart";
    public const string IMAGE_TYPE_FANART = "fanart";
    public const string IMAGE_TYPE_BANNER = "banner";
    public const string IMAGE_TYPE_CLEARLOGO = "clearlogo";
    public const string IMAGE_TYPE_SCREENSHOT = "screenshot";

    public const string IMAGE_SIDE_FRONT = "front";
    public const string IMAGE_SIDE_BACK = "back";

    #endregion

    protected const string GAME_FIELDS = "players,publishers,genres,overview,last_updated,rating,platform,coop,youtube,os,processor,ram,hdd,video,sound,alternates";
    protected const string GAME_INCLUDE = "boxart,platform";

    protected const string BASE_URL = "https://api.thegamesdb.net/";

    protected const string SEARCH_GAME_PATH = "Games/ByGameName";
    protected const string GET_GAME_PATH = "Games/ByGameID";
    protected const string GET_GAME_IMAGES_PATH = "/Games/Images";
    protected const string GET_GENRES_PATH = "/Genres";
    protected const string GET_DEVELOPERS_PATH = "/Developers";
    protected const string GET_PUBLISHERS_PATH = "/Publishers";

    protected const string GAME_CACHE_PATH = "Games";

    protected string _apiKey;

    protected string _cachePath;
    protected string _gameFieldsAndIncludeQuery;

    protected Downloader _downloader;

    public TheGamesDbV2(string apiKey, string cachePath)
    {
      _apiKey = apiKey;
      _cachePath = cachePath;
      _gameFieldsAndIncludeQuery = $"fields={Uri.EscapeDataString(GAME_FIELDS)}&include={Uri.EscapeDataString(GAME_INCLUDE)}";

      _downloader = new Downloader();
    }
    
    public Task<GameResult> SearchGameByNameAsync(string searchName, string platform)
    {
      string query = "name=" + Uri.EscapeDataString(searchName) + "&" + _gameFieldsAndIncludeQuery;
      if (!string.IsNullOrEmpty(platform))
      {
        string filter = Uri.EscapeDataString("platform=" + platform);
        query += "&filter=" + filter;
      }
      string url = BuildRequestUrl(SEARCH_GAME_PATH, query);
      return _downloader.DownloadAsync<GameResult>(url);
    }

    public Task<GameResult> GetGameAsync(int id)
    {
      string query = "id=" + id + "&" + _gameFieldsAndIncludeQuery;
      string cacheName = CreateAndGetGameCacheName(id);
      string url = BuildRequestUrl(GET_GAME_PATH, query);
      return _downloader.DownloadAsync<GameResult>(url, cacheName);
    }

    public void CacheGame(int gameId, GameResult gameResult)
    {
      string json = JsonConvert.SerializeObject(gameResult);
      string cachePath = CreateAndGetGameCacheName(gameId);
      WriteToCache(json, cachePath);
    }

    public Task<GenreResult> GetGenresAsync()
    {
      string cacheName = CreateAndGetCacheName("genres.json");
      string url = BuildRequestUrl(GET_GENRES_PATH, null);
      return _downloader.DownloadAsync<GenreResult>(url, cacheName);
    }

    public Task<DeveloperResult> GetDevelopersAsync()
    {
      string cacheName = CreateAndGetCacheName("developers.json");
      string url = BuildRequestUrl(GET_DEVELOPERS_PATH, null);
      return _downloader.DownloadAsync<DeveloperResult>(url, cacheName);
    }

    public Task<PublisherResult> GetPublishersAsync()
    {
      string cacheName = CreateAndGetCacheName("publishers.json");
      string url = BuildRequestUrl(GET_PUBLISHERS_PATH, null);
      return _downloader.DownloadAsync<PublisherResult>(url, cacheName);
    }

    public Task<ImageResult> GetGameImagesAsync(int gameId)
    {
      string query = "games_id=" + gameId;
      string cacheName = CreateAndGetGameImagesCacheName(gameId);
      string url = BuildRequestUrl(GET_GAME_IMAGES_PATH, query);
      return _downloader.DownloadAsync<ImageResult>(url, cacheName);
    }

    public Task<bool> DownloadImageAsync(int gameId, string baseUrl, Image image)
    {
      string url = baseUrl + image.Filename;
      string fileName = Path.GetFileName(new Uri(url).LocalPath);
      string downloadFile = CreateAndGetImageCacheName(fileName, gameId, image.Type, image.Side);
      return _downloader.DownloadFileAsync(url, downloadFile);
    }

    public string GetGameImageCachePath(string gameId, string type, string side)
    {
      return GetCachePath(GAME_CACHE_PATH, gameId, type, side);
    }

    protected string BuildRequestUrl(string path, string query)
    {
      string url = $"{BASE_URL}{path}?apikey={_apiKey}";
      if (!string.IsNullOrEmpty(query))
        url += $"&{query}";
      return url;
    }

    protected string CreateAndGetGameCacheName(int gameId)
    {
      return CreateAndGetCacheName($"game_{gameId}.json", GAME_CACHE_PATH, gameId.ToString());
    }

    protected string CreateAndGetGameImagesCacheName(int gameId)
    {
      return CreateAndGetCacheName($"game_{gameId}_images.json", GAME_CACHE_PATH, gameId.ToString());
    }

    protected string CreateAndGetImageCacheName(string fileName, int gameId, string type, string side)
    {
      return CreateAndGetCacheName(fileName, GAME_CACHE_PATH, gameId.ToString(), type, side);
    }

    protected string GetCachePath(params string[] paths)
    {
      string cachePath = _cachePath;
      if (paths != null)
        foreach (string path in paths)
          if (!string.IsNullOrEmpty(path))
            cachePath = Path.Combine(cachePath, path);
      return cachePath;
    }

    protected string CreateAndGetCacheName(string fileName, params string[] paths)
    {
      string path = GetCachePath(paths);
      try
      {
        if (!Directory.Exists(path))
          Directory.CreateDirectory(path);
        return Path.Combine(path, fileName);
      }
      catch (Exception ex)
      {
        Logger.Warn("TheGamesDbV2: Error creating cache directory '{0}'", ex, path);
        return null;
      }
    }

    public void WriteToCache(string json, string cachePath)
    {
      try
      {
        if (File.Exists(cachePath))
          return;
        using (FileStream fs = new FileStream(cachePath, FileMode.Create, FileAccess.Write))
        {
          using (StreamWriter sw = new StreamWriter(fs))
          {
            sw.Write(json);
            sw.Close();
          }
          fs.Close();
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("TheGamesDbV2: Error writing cache file '{0}'", ex, cachePath);
      }
    }

    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
