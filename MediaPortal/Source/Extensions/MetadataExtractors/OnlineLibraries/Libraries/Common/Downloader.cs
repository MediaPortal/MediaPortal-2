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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using Newtonsoft.Json;
using System.Threading;

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

    private ReaderWriterLockSlim _jsonLock = new ReaderWriterLockSlim();
    private ReaderWriterLockSlim _fileLock = new ReaderWriterLockSlim();

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
    public TE Download<TE>(string url, string saveCacheFile = null)
    {
      string json = DownloadJSON(url);
      if (string.IsNullOrEmpty(json))
        return default(TE);
      //Console.WriteLine("JSON: {0}", json);
      if (!string.IsNullOrEmpty(saveCacheFile))
        WriteCache(saveCacheFile, json);
      return JsonConvert.DeserializeObject<TE>(json);
    }

    /// <summary>
    /// Downloads the JSON string from API.
    /// </summary>
    /// <param name="url">Url to download</param>
    /// <returns>JSON result</returns>
    protected virtual string DownloadJSON(string url)
    {
      return DownloadString(url);
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

    /// <summary>
    /// Donwload a file from given <paramref name="url"/> and save it to <paramref name="downloadFile"/>.
    /// </summary>
    /// <param name="url">Url to download</param>
    /// <param name="downloadFile">Target file name</param>
    /// <returns><c>true</c> if successful</returns>
    public bool DownloadFile(string url, string downloadFile)
    {
      if (File.Exists(downloadFile))
        return true;
      try
      {
        try
        {
          _fileLock.EnterWriteLock();
          if (File.Exists(downloadFile))
            return true;
          using (WebClient webClient = new CompressionWebClient())
            webClient.DownloadFile(url, downloadFile);
          return true;
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("OnlineLibraries.Downloader: Exception when downloading file {0} from {1} ({2})", downloadFile, url, ex.Message);
          return false;
        }
      }
      finally
      {
        _fileLock.ExitWriteLock();
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

      try
      {
        _jsonLock.EnterWriteLock();
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
      finally
      {
        _jsonLock.ExitWriteLock();
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

      try
      {
        try
        {
          _jsonLock.EnterReadLock();
          if (string.IsNullOrEmpty(cacheFile))
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
      finally
      {
        _jsonLock.ExitReadLock();
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

      try
      {
        try
        {
          _jsonLock.EnterReadLock();
          if (string.IsNullOrEmpty(cacheFile))
            return false;
          FileInfo info = new FileInfo(cacheFile);
          if ((DateTime.Now - info.CreationTime).TotalDays > maxAgeInDays)
            return true;

          return false;
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("OnlineLibraries.Downloader: Exception when determining cache {0} age ({1})", cacheFile, ex.Message);
          return false;
        }
      }
      finally
      {
        _jsonLock.ExitReadLock();
      }
    }

    /// <summary>
    /// Deletes the cached response.
    /// </summary>
    /// <param name="cacheFile">Name for the cached response</param>
    /// <returns>Cached object</returns>
    public bool DeleteCache(string cacheFile)
    {
      if (string.IsNullOrEmpty(cacheFile))
        return true;

      try
      {
        try
        {
          _jsonLock.EnterWriteLock();
          if (string.IsNullOrEmpty(cacheFile))
            return true;

          File.Delete(cacheFile);
          return true;
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("OnlineLibraries.Downloader: Exception when determining cache {0} age ({1})", cacheFile, ex.Message);
          return false;
        }
      }
      finally
      {
        _jsonLock.ExitWriteLock();
      }
    }

    /// <summary>
    /// Returns contents of a file <paramref name="downloadFile"/> downloaded earlier.
    /// </summary>
    /// <param name="downloadedFile">Target file name</param>
    /// <returns>File contents</returns>
    public byte[] ReadDownloadedFile(string downloadedFile)
    {
      if (File.Exists(downloadedFile))
        return null;
      try
      {
        try
        {
          _fileLock.EnterReadLock();
          if (File.Exists(downloadedFile))
            return null;
          return File.ReadAllBytes(downloadedFile);
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("OnlineLibraries.Downloader: Exception when reading file {0} ({1})", downloadedFile, ex.Message);
          return null;
        }
      }
      finally
      {
        _fileLock.ExitReadLock();
      }
    }
  }
}
