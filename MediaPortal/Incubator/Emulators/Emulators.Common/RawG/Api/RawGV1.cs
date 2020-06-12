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
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Emulators.Common.RawG.Data;

namespace Emulators.Common.RawG.Api
{
  public class RawGV1
  {
    protected static readonly FileVersionInfo FILE_VERSION_INFO;
    protected const string BASE_URL = "https://api.rawg.io/api/";

    protected const string SEARCH_GAME_PATH = "games";
    protected const string GET_GENRES_PATH = "genres";
    protected const string GET_DEVELOPERS_PATH = "developers";
    protected const string GET_PUBLISHERS_PATH = "publishers";

    protected const string GAME_CACHE_PATH = "Games";

    protected string _cachePath;

    protected Downloader _downloader;

    static RawGV1()
    {
      FILE_VERSION_INFO = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
    }

    public RawGV1(string cachePath)
    {
      _cachePath = cachePath;

      _downloader = new Downloader();
      _downloader.Headers["Accept"] = "application/json";
      _downloader.Headers["User-Agent"] = "MediaPortal/" + FILE_VERSION_INFO.FileVersion + " (http://www.team-mediaportal.com/)";
    }
    
    public Task<GameResult> SearchGameByNameAsync(string searchName, int platformId)
    {
      string query = "?search=" + Uri.EscapeDataString(searchName);
      if (platformId > 0)
        query += "&platforms=" + platformId;
      query += "&page_size=10";
      string url = BuildRequestUrl(SEARCH_GAME_PATH, query);
      return _downloader.DownloadAsync<GameResult>(url);
    }

    public Task<GameDetails> GetGameAsync(long id)
    {
      string query = "/" + id;
      string cacheName = CreateAndGetGameCacheName(id);
      string url = BuildRequestUrl(SEARCH_GAME_PATH, query);
      return _downloader.DownloadAsync<GameDetails>(url, cacheName);
    }

    public void CacheGame(long gameId, GameDetails gameResult)
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

    public Task<PlatformResult> GetPlatformsAsync()
    {
      string cacheName = CreateAndGetCacheName("platforms.json");
      string url = BuildRequestUrl(GET_GENRES_PATH, null);
      return _downloader.DownloadAsync<PlatformResult>(url, cacheName);
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

    public async Task<IEnumerable<Screenshot>> GetGameImagesAsync(long gameId)
    {
      string query = "/" + gameId + "/screenshots";
      string cacheName = CreateAndGetGameImagesCacheName(gameId);
      string url = BuildRequestUrl(SEARCH_GAME_PATH, query);
      var result = await _downloader.DownloadAsync<ScreenshotResult>(url, cacheName);
      return result?.Results;
    }

    public Task<bool> DownloadImageAsync(long gameId, string url, string downloadFolder)
    {
      string downloadFile = CreateAndGetImageCacheName(gameId, url, downloadFolder);
      return _downloader.DownloadFileAsync(url, downloadFile);
    }

    protected string CreateAndGetImageCacheName(long id, string url, string folderPath)
    {
      try
      {
        string prefix = string.Format(@"RWG({0})_", id);
        if (!Directory.Exists(folderPath))
          Directory.CreateDirectory(folderPath);

        string filename = Path.GetFileName(new Uri(url).LocalPath);
        return Path.Combine(folderPath, prefix + filename);
      }
      catch (Exception ex)
      {
        Logger.Warn("RawGV1: Error creating image cache directory '{0}'", ex, folderPath);
        return null;
      }
    }

    protected string BuildRequestUrl(string path, string query)
    {
      string url = $"{BASE_URL}{path}";
      if (!string.IsNullOrEmpty(query))
        url += $"{query}";
      return url;
    }

    protected string CreateAndGetGameCacheName(long gameId)
    {
      return CreateAndGetCacheName($"game_{gameId}.json", GAME_CACHE_PATH, gameId.ToString());
    }

    protected string CreateAndGetGameImagesCacheName(long gameId)
    {
      return CreateAndGetCacheName($"game_{gameId}_images.json", GAME_CACHE_PATH, gameId.ToString());
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
        Logger.Warn("RawGV1: Error creating cache directory '{0}'", ex, path);
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
        Logger.Warn("RawGV1: Error writing cache file '{0}'", ex, cachePath);
      }
    }

    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
