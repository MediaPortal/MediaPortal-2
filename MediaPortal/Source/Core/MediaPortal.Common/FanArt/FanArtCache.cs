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

using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Utilities.Cache;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Utilities.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Common.FanArt
{
  public class FanArtCache : IFanArtCache
  {
    #region Inner classes

    public class FanArtCount
    {
      public FanArtCount(int count)
      {
        Count = count;
      }

      public int Count { get; set; }
    }

    #endregion

    //The maximum length of the cache file's name, longer names will be truncated to this length
    private const int MAX_CACHE_NAME_LENGTH = 50;
    private readonly string FANART_CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArt\");
    private readonly TimeSpan FANART_COUNT_TIMEOUT = new TimeSpan(0, 5, 0);
    
    protected KeyedAsyncReaderWriterLock<Guid> _fanArtSync;
    protected AsyncStaticTimeoutCache<string, FanArtCount> _fanArtCounts;
    protected Dictionary<string, int> _maxFanArtCounts;
    private SettingsChangeWatcher<FanArtSettings> _settingsChangeWatcher;

    public FanArtCache()
    {
      Init();
    }

    protected void Init()
    {
      _fanArtSync = new KeyedAsyncReaderWriterLock<Guid>();
      _fanArtCounts = new AsyncStaticTimeoutCache<string, FanArtCount>(FANART_COUNT_TIMEOUT);
      _maxFanArtCounts = new Dictionary<string, int>();
      _settingsChangeWatcher = new SettingsChangeWatcher<FanArtSettings>();
      _settingsChangeWatcher.SettingsChanged += SettingsChanged;
      LoadSettings();
    }

    private void LoadSettings()
    {
      FanArtSettings settings = _settingsChangeWatcher.Settings;
      Dictionary<string, int> maxFanArtCounts = new Dictionary<string, int>();
      maxFanArtCounts[FanArtTypes.Banner] = settings.MaxBannerFanArt;
      maxFanArtCounts[FanArtTypes.ClearArt] = settings.MaxClearArt;
      maxFanArtCounts[FanArtTypes.Cover] = settings.MaxPosterFanArt;
      maxFanArtCounts[FanArtTypes.DiscArt] = settings.MaxDiscArt;
      maxFanArtCounts[FanArtTypes.FanArt] = settings.MaxBackdropFanArt;
      maxFanArtCounts[FanArtTypes.Logo] = settings.MaxLogoFanArt;
      maxFanArtCounts[FanArtTypes.Poster] = settings.MaxPosterFanArt;
      maxFanArtCounts[FanArtTypes.Thumbnail] = settings.MaxThumbFanArt;
      maxFanArtCounts[FanArtTypes.Undefined] = 0;
      _maxFanArtCounts = maxFanArtCounts;
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      LoadSettings();
    }

    public async Task<bool> TrySaveFanArt(Guid mediaItemId, string title, string fanArtType, TrySaveFanArtAsyncDelegate saveDlgt)
    {
      string fanArtCacheDirectory = GetFanArtDirectory(mediaItemId);
      string fanArtTypeSubDirectory = GetFanArtTypeDirectory(fanArtCacheDirectory, fanArtType);

      using (var writer = await _fanArtSync.WriterLockAsync(mediaItemId).ConfigureAwait(false))
      {
        if (!await InitCache(fanArtCacheDirectory, fanArtTypeSubDirectory, title).ConfigureAwait(false))
          return false;
        FanArtCount currentCount = await _fanArtCounts.GetValue(CreateFanArtTypeId(mediaItemId, fanArtType), _ => CreateFanArtCount(mediaItemId, fanArtType)).ConfigureAwait(false);
        if (currentCount.Count < GetMaxFanArtCount(fanArtType) && await saveDlgt(fanArtTypeSubDirectory).ConfigureAwait(false))
        {
          currentCount.Count++;
          return true;
        }
      }
      return false;
    }

    public async Task<int> TrySaveFanArt<T>(Guid mediaItemId, string title, string fanArtType, ICollection<T> files, TrySaveMultipleFanArtAsyncDelegate<T> saveDlgt)
    {
      if (files == null || files.Count == 0)
        return 0;

      string fanArtCacheDirectory = GetFanArtDirectory(mediaItemId);
      string fanArtTypeSubDirectory = GetFanArtTypeDirectory(fanArtCacheDirectory, fanArtType);

      int savedCount = 0;

      using (var writer = await _fanArtSync.WriterLockAsync(mediaItemId).ConfigureAwait(false))
      {
        if (!await InitCache(fanArtCacheDirectory, fanArtTypeSubDirectory, title).ConfigureAwait(false))
          return savedCount;

        int maxCount = GetMaxFanArtCount(fanArtType);
        FanArtCount currentCount = await _fanArtCounts.GetValue(CreateFanArtTypeId(mediaItemId, fanArtType), _ => CreateFanArtCount(mediaItemId, fanArtType)).ConfigureAwait(false);
        if (currentCount.Count >= maxCount)
          return savedCount;

        foreach (T file in files)
        {
          if (await saveDlgt(fanArtTypeSubDirectory, file).ConfigureAwait(false))
          {
            savedCount++;
            currentCount.Count++;
            if (currentCount.Count >= maxCount)
              break;
          }
        }
      }
      return savedCount;
    }

    public IList<string> GetFanArtFiles(Guid mediaItemId, string fanArtType)
    {
      string fanArtId = CreateFanArtId(mediaItemId);
      List<string> fanartFiles = new List<string>();
      string path = Path.Combine(FANART_CACHE_PATH, fanArtId, fanArtType);
      if (Directory.Exists(path))
      {
        int maxCount = GetMaxFanArtCount(fanArtType);
        fanartFiles.AddRange(Directory.GetFiles(path, "*.jpg"));
        if (fanartFiles.Count < maxCount)
          fanartFiles.AddRange(Directory.GetFiles(path, "*.png"));
        if (fanartFiles.Count < maxCount)
          fanartFiles.AddRange(Directory.GetFiles(path, "*.tbn"));
      }
      return fanartFiles;
    }

    public ICollection<Guid> GetAllFanArtIds()
    {
      List<Guid> mediaItemIds = new List<Guid>();
      try
      {
        foreach (DirectoryInfo fanartDirectory in new DirectoryInfo(FANART_CACHE_PATH).EnumerateDirectories())
        {
          Guid mediaItemId;
          if (Guid.TryParse(fanartDirectory.Name, out mediaItemId))
            mediaItemIds.Add(mediaItemId);
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("FanArtCache: Error reading fanart directory '{0}'", ex, FANART_CACHE_PATH);
      }
      return mediaItemIds;
    }

    public void DeleteFanArtFiles(Guid mediaItemId)
    {
      string fanArtId = CreateFanArtId(mediaItemId);
      try
      {
        string folderPath = Path.Combine(FANART_CACHE_PATH, fanArtId);
        int maxTries = 3;
        if (Directory.Exists(folderPath))
        {
          for (int i = 0; i < maxTries; i++)
          {
            try
            {
              if (Directory.Exists(folderPath))
              {
                //Make sure file permissions are correct
                foreach(string cacheFile in Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories))
                {
                  try
                  {
                    File.SetAttributes(cacheFile, FileAttributes.Normal);
                  }
                  catch
                  { }
                }
                Directory.Delete(folderPath, true);
              }
              return;
            }
            catch
            {
              if (i == maxTries - 1)
                throw;
            }
            Thread.Sleep(200);
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("Unable to delete FanArt files for media item {0}", ex, fanArtId);
      }
    }

    protected async Task<bool> InitCache(string fanArtCacheDirectory, string fanArtTypeSubDirectory, string cacheName)
    {
      try
      {
        if (Directory.Exists(fanArtTypeSubDirectory))
          return true;
        Directory.CreateDirectory(fanArtTypeSubDirectory);
        string cacheFile = CreateCacheNameFilePath(fanArtCacheDirectory, cacheName);
        if (cacheFile != null)
        {
          FileInfo file = new FileInfo(cacheFile);
          if (!file.Exists)
          {
            using (StreamWriter sw = file.CreateText())
              await sw.WriteAsync(cacheName).ConfigureAwait(false);
            file.Attributes = FileAttributes.Normal;
          }
        }
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("FanArtCache: Error creating fanart cache directory '{0}'", ex, fanArtTypeSubDirectory);
      }
      return false;
    }

    protected string CreateFanArtId(Guid mediaItemId)
    {
      return mediaItemId.ToString().ToUpperInvariant();
    }

    protected string CreateFanArtTypeId(Guid mediaItemId, string fanArtType)
    {
      return string.Format("{0}|{1}", CreateFanArtId(mediaItemId), fanArtType);
    }

    protected string GetFanArtDirectory(Guid mediaItemId)
    {
      return Path.Combine(FANART_CACHE_PATH, CreateFanArtId(mediaItemId));
    }

    protected string GetFanArtTypeDirectory(string fanArtDirectory, string fanArtType)
    {
      return Path.Combine(fanArtDirectory, fanArtType);
    }

    protected string CreateCacheNameFilePath(string fanArtCacheDirectory, string cacheName)
    {
      if (string.IsNullOrEmpty(cacheName))
        return null;
      cacheName = cacheName.Trim();
      //Long names can cause a PathTooLongException
      if (cacheName.Length > MAX_CACHE_NAME_LENGTH)
        cacheName = cacheName.Substring(0, MAX_CACHE_NAME_LENGTH);
      return Path.Combine(fanArtCacheDirectory, FileUtils.GetSafeFilename(cacheName.ToUpperInvariant() + ".mpcache"));
    }

    protected int GetMaxFanArtCount(string fanArtType, int defaultCount = 3)
    {
      int maxCount;
      if (_maxFanArtCounts.TryGetValue(fanArtType, out maxCount))
        return maxCount;
      return defaultCount;
    }

    protected Task<FanArtCount> CreateFanArtCount(Guid mediaItemId, string fanArtType)
    {
      int count = GetFanArtFiles(mediaItemId, fanArtType).Count;
      return Task.FromResult(new FanArtCount(count));
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
