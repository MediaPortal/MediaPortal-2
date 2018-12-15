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
using MediaPortal.Utilities.Threading;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Common
{
  public class Downloader
  {
    /// <summary>
    /// Dictionary of additional http headers to be added for each request.
    /// </summary>
    public Dictionary<string, string> Headers { get; internal set; }

    /// <summary>
    /// Enables gzip/deflate compression for web requests.
    /// </summary>
    public bool EnableCompression { get; set; }

    private KeyedAsyncReaderWriterLock<string> _jsonLock = new KeyedAsyncReaderWriterLock<string>();
    private KeyedAsyncReaderWriterLock<string> _fileLock = new KeyedAsyncReaderWriterLock<string>();

    public Downloader()
    {
      Headers = new Dictionary<string, string>();
    }

    /// <summary>
    /// Downloads the requested information from the JSON api and deserializes the response to the requested <typeparam name="TE">Type</typeparam>.
    /// This method can save the response to local cache, if a valid path is passed in <paramref name="saveCacheFile"/>.
    /// </summary>
    /// <typeparam name="TE">Target type</typeparam>
    /// <param name="url">Url to download</param>
    /// <param name="saveCacheFile">Optional name for saving response to cache</param>
    /// <returns>Downloaded object</returns>
    public TE Download<TE>(string url, string saveCacheFile = null, bool allowCached = true)
    {
      var writeLock = !string.IsNullOrEmpty(saveCacheFile) ? _jsonLock.WriterLock(saveCacheFile) : null;
      using (writeLock)
      {
        if (allowCached)
        {
          TE cached = ReadCacheInternal<TE>(saveCacheFile);
          if (cached != null)
            return cached;
        }

        string json = DownloadJSON(url).Result;
        if (string.IsNullOrEmpty(json))
          return default(TE);
        //Console.WriteLine("JSON: {0}", json);
        if (!string.IsNullOrEmpty(saveCacheFile))
          WriteCache(saveCacheFile, json);
        return JsonConvert.DeserializeObject<TE>(json);
      }
    }

    /// <summary>
    /// Asynchronously downloads the requested information from the JSON api and deserializes the response to the requested <typeparam name="TE">Type</typeparam>.
    /// This method can save the response to local cache, if a valid path is passed in <paramref name="saveCacheFile"/>.
    /// </summary>
    /// <typeparam name="TE">Target type</typeparam>
    /// <param name="url">Url to download</param>
    /// <param name="saveCacheFile">Optional name for saving response to cache</param>
    /// <returns>Downloaded object</returns>
    public async Task<TE> DownloadAsync<TE>(string url, string saveCacheFile = null, bool allowCached = true)
    {
      var writeLock = !string.IsNullOrEmpty(saveCacheFile) ? await _jsonLock.WriterLockAsync(saveCacheFile).ConfigureAwait(false) : null;
      using (writeLock)
      {
        if (allowCached)
        {
          TE cached = ReadCacheInternal<TE>(saveCacheFile);
          if (cached != null)
            return cached;
        }

        string json = await DownloadJSON(url).ConfigureAwait(false);
        if (string.IsNullOrEmpty(json))
          return default(TE);
        //Console.WriteLine("JSON: {0}", json);
        if (!string.IsNullOrEmpty(saveCacheFile))
          WriteCache(saveCacheFile, json);
        return JsonConvert.DeserializeObject<TE>(json);
      }
    }

    /// <summary>
    /// Downloads the JSON string from API.
    /// </summary>
    /// <param name="url">Url to download</param>
    /// <returns>JSON result</returns>
    protected virtual async Task<string> DownloadJSON(string url)
    {
      return await DownloadStringAsync(url).ConfigureAwait(false);
    }

    public string DownloadString(string url)
    {
      using (CompressionWebClient webClient = new CompressionWebClient(EnableCompression) { Encoding = Encoding.UTF8 })
      {
        foreach (KeyValuePair<string, string> headerEntry in Headers)
          webClient.Headers[headerEntry.Key] = headerEntry.Value;

        return webClient.DownloadString(url);
      }
    }

    public async Task<string> DownloadStringAsync(string url)
    {
      using (CompressionWebClient webClient = new CompressionWebClient(EnableCompression) { Encoding = Encoding.UTF8 })
      {
        foreach (KeyValuePair<string, string> headerEntry in Headers)
          webClient.Headers[headerEntry.Key] = headerEntry.Value;

        return await webClient.DownloadStringTaskAsync(url).ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Donwload a file from given <paramref name="url"/> and save it to <paramref name="downloadFile"/>.
    /// </summary>
    /// <param name="url">Url to download</param>
    /// <param name="downloadFile">Target file name</param>
    /// <returns><c>true</c> if successful</returns>
    public bool DownloadFile(string url, string downloadFile)
    {
      return DownloadFileAsync(url, downloadFile).Result;
    }

    /// <summary>
    /// Donwload a file from given <paramref name="url"/> and save it to <paramref name="downloadFile"/>.
    /// </summary>
    /// <param name="url">Url to download</param>
    /// <param name="downloadFile">Target file name</param>
    /// <returns><c>true</c> if successful</returns>
    public async Task<bool> DownloadFileAsync(string url, string downloadFile)
    {
      if (string.IsNullOrEmpty(downloadFile))
        return false;
      if (File.Exists(downloadFile))
        return true;

      using (await _fileLock.WriterLockAsync(downloadFile).ConfigureAwait(false))
      {
        try
        {
          if (File.Exists(downloadFile))
            return true;
          byte[] data = null;
          using (WebClient webClient = new CompressionWebClient())
            data = await webClient.DownloadDataTaskAsync(url).ConfigureAwait(false);
          if (data?.LongLength > 0)
          {
            using (FileStream sourceStream = new FileStream(downloadFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
              await sourceStream.WriteAsync(data, 0, data.Length);
            return true;
          }
          ServiceRegistration.Get<ILogger>().Warn("OnlineLibraries.Downloader: No data received when downloading file {0} from {1}", downloadFile, url);
          return false;
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("OnlineLibraries.Downloader: Exception when downloading file {0} from {1} ({2})", downloadFile, url, ex.Message);
          return false;
        }
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

    /// <summary>
    /// Reads the requested information from the cached JSON file and deserializes the response to the requested <typeparam name="TE">Type</typeparam>.
    /// </summary>
    /// <typeparam name="TE">Target type</typeparam>
    /// <param name="cacheFile">Name for the cached response</param>
    /// <returns>Cached object</returns>
    public TE ReadCache<TE>(string cacheFile)
    {
      if (string.IsNullOrEmpty(cacheFile))
        return default(TE);

      using (_jsonLock.ReaderLock(cacheFile))
        return ReadCacheInternal<TE>(cacheFile);
    }

    /// <summary>
    /// Asynchronously reads the requested information from the cached JSON file and deserializes the response to the requested <typeparam name="TE">Type</typeparam>.
    /// </summary>
    /// <typeparam name="TE">Target type</typeparam>
    /// <param name="cacheFile">Name for the cached response</param>
    /// <returns>Cached object</returns>
    public async Task<TE> ReadCacheAsync<TE>(string cacheFile)
    {
      if (string.IsNullOrEmpty(cacheFile))
        return default(TE);

      using (await _jsonLock.ReaderLockAsync(cacheFile).ConfigureAwait(false))
        return ReadCacheInternal<TE>(cacheFile);
    }

    protected TE ReadCacheInternal<TE>(string cacheFile)
    {
      try
      {
        if (string.IsNullOrEmpty(cacheFile) || !File.Exists(cacheFile))
          return default(TE);
        string json = File.ReadAllText(cacheFile, Encoding.UTF8);
        return JsonConvert.DeserializeObject<TE>(json);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("OnlineLibraries.Downloader: Exception when reading cache {0} ({1})", cacheFile, ex.Message);
        return default(TE);
      }
    }

    /// <summary>
    /// Checks if a cached file is above a specified age.
    /// </summary>
    /// <param name="cacheFile">Name for the cached response</param>
    /// <param name="maxAgeInDays">Maximum age of the cached response in days</param>
    /// <returns>Cached object</returns>
    public bool IsCacheExpired(string cacheFile, double maxAgeInDays)
    {
      if (string.IsNullOrEmpty(cacheFile))
        return false;

      using (_jsonLock.ReaderLock(cacheFile))
      {
        try
        {
          FileInfo info = new FileInfo(cacheFile);
          return info.Exists && (DateTime.Now - info.CreationTime).TotalDays > maxAgeInDays;
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("OnlineLibraries.Downloader: Exception when determining cache {0} age ({1})", cacheFile, ex.Message);
          return false;
        }
      }
    }

    /// <summary>
    /// Deletes the cached response.
    /// </summary>
    /// <param name="cacheFile">Name for the cached response</param>
    /// <returns>Cached object</returns>
    public async Task<bool> DeleteCacheAsync(string cacheFile)
    {
      if (string.IsNullOrEmpty(cacheFile))
        return true;

      using (await _jsonLock.WriterLockAsync(cacheFile).ConfigureAwait(false))
      {
        try
        {
          File.Delete(cacheFile);
          return true;
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("OnlineLibraries.Downloader: Exception when determining cache {0} age ({1})", cacheFile, ex.Message);
          return false;
        }
      }
    }

    /// <summary>
    /// Returns contents of a file <paramref name="downloadFile"/> downloaded earlier.
    /// </summary>
    /// <param name="downloadedFile">Target file name</param>
    /// <returns>File contents</returns>
    public byte[] ReadDownloadedFile(string downloadedFile)
    {
      if (!File.Exists(downloadedFile))
        return null;

      using (_fileLock.ReaderLock(downloadedFile))
      {
        try
        {
          if (!File.Exists(downloadedFile))
            return null;
          return File.ReadAllBytes(downloadedFile);
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("OnlineLibraries.Downloader: Exception when reading file {0} ({1})", downloadedFile, ex.Message);
          return null;
        }
      }
    }

    /// <summary>
    /// Returns contents of a file <paramref name="downloadFile"/> downloaded earlier.
    /// </summary>
    /// <param name="downloadedFile">Target file name</param>
    /// <returns>File contents</returns>
    public async Task<byte[]> ReadDownloadedFileAsync(string downloadedFile)
    {
      if (!File.Exists(downloadedFile))
        return null;

      using (await _fileLock.ReaderLockAsync(downloadedFile))
      {
        try
        {
          if (!File.Exists(downloadedFile))
            return null;
          return File.ReadAllBytes(downloadedFile);
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("OnlineLibraries.Downloader: Exception when reading file {0} ({1})", downloadedFile, ex.Message);
          return null;
        }
      }
    }
  }
}
