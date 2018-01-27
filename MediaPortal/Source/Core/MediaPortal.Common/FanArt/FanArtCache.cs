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
    private readonly string FANART_CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArt\");
    private readonly TimeSpan FANART_COUNT_TIMEOUT = new TimeSpan(0, 5, 0);
    
    protected object _initSync = new object();
    protected KeyedAsyncReaderWriterLock<string> _fanArtCountSync = new KeyedAsyncReaderWriterLock<string>();
    protected AsyncStaticTimeoutCache<string, FanArtCount> _fanArtCounts;
    protected Dictionary<string, int> _maxFanArtCounts = new Dictionary<string, int>();

    private SettingsChangeWatcher<FanArtSettings> _settingsChangeWatcher = null;

    public FanArtCache()
    {
      Init();
    }

    protected void Init()
    {
      _fanArtCounts = new AsyncStaticTimeoutCache<string, FanArtCount>(FANART_COUNT_TIMEOUT);
      _settingsChangeWatcher = new SettingsChangeWatcher<FanArtSettings>();
      _settingsChangeWatcher.SettingsChanged += SettingsChanged;
      LoadSettings();
    }

    private void LoadSettings()
    {
      FanArtSettings settings = _settingsChangeWatcher.Settings;
      _maxFanArtCounts[FanArtTypes.Banner] = settings.MaxBannerFanArt;
      _maxFanArtCounts[FanArtTypes.ClearArt] = settings.MaxClearArt;
      _maxFanArtCounts[FanArtTypes.Cover] = settings.MaxPosterFanArt;
      _maxFanArtCounts[FanArtTypes.DiscArt] = settings.MaxDiscArt;
      _maxFanArtCounts[FanArtTypes.FanArt] = settings.MaxBackdropFanArt;
      _maxFanArtCounts[FanArtTypes.Logo] = settings.MaxLogoFanArt;
      _maxFanArtCounts[FanArtTypes.Poster] = settings.MaxPosterFanArt;
      _maxFanArtCounts[FanArtTypes.Thumbnail] = settings.MaxThumbFanArt;
      _maxFanArtCounts[FanArtTypes.Undefined] = 0;
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      LoadSettings();
    }

    public void InitFanArtCache(Guid mediaItemId)
    {
      lock (_initSync)
      {
        string fanArtId = CreateFanArtId(mediaItemId);
        string cacheFolder = Path.Combine(FANART_CACHE_PATH, fanArtId);
        if (!Directory.Exists(cacheFolder))
          Directory.CreateDirectory(cacheFolder);
      }
    }

    public void InitFanArtCache(Guid mediaItemId, string title)
    {
      lock (_initSync)
      {
        string fanArtId = CreateFanArtId(mediaItemId);
        string cacheFolder = Path.Combine(FANART_CACHE_PATH, fanArtId);
        string cacheFile = null;
        if(!string.IsNullOrEmpty(title))
          cacheFile = Path.Combine(cacheFolder, FileUtils.GetSafeFilename(title.Trim().ToUpperInvariant() + ".mpcache"));
        if (!Directory.Exists(cacheFolder))
        {
          Directory.CreateDirectory(cacheFolder);
          if (cacheFile != null)
          {
            File.AppendAllText(cacheFile, title);
            File.SetAttributes(cacheFile, FileAttributes.Normal);
          }
        }
        else if (cacheFile != null && !File.Exists(cacheFile))
        {
          File.AppendAllText(cacheFile, title);
          File.SetAttributes(cacheFile, FileAttributes.Normal);
        }
      }
    }

    public string GetFanArtDirectory(Guid mediaItemId, string fanArtType)
    {
      return Path.Combine(FANART_CACHE_PATH, CreateFanArtId(mediaItemId), fanArtType);
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

    public int GetMaxFanArtCount(string fanArtType, int defaultCount = 3)
    {
      int maxCount;
      if (_maxFanArtCounts.TryGetValue(fanArtType, out maxCount))
        return maxCount;
      return defaultCount;
    }

    public async Task<FanArtCountLock> GetFanArtCountLock(Guid mediaItemId, string fanArtType)
    {
      string key = CreateFanArtTypeId(mediaItemId, fanArtType);
      var countSync = await _fanArtCountSync.WriterLockAsync(key).ConfigureAwait(false);
      try
      {
        FanArtCount count = await _fanArtCounts.GetValue(key, _ => CreateFanArtCount(mediaItemId, fanArtType));
        return new FanArtCountLock(count, countSync);
      }
      catch
      {
        countSync.Dispose();
        throw;
      }
    }

    protected string CreateFanArtId(Guid mediaItemId)
    {
      return mediaItemId.ToString().ToUpperInvariant();
    }

    protected string CreateFanArtTypeId(Guid mediaItemId, string fanArtType)
    {
      return string.Format("{0}|{1}", CreateFanArtId(mediaItemId), fanArtType);
    }

    protected Task<FanArtCount> CreateFanArtCount(Guid mediaItemId, string fanArtType)
    {
      int count = GetFanArtFiles(mediaItemId, fanArtType).Count;
      return Task.FromResult(new FanArtCount(count));
    }

    #region FanArt Count

    public class FanArtCount
    {
      public FanArtCount(int count)
      {
        Count = count;
      }

      public int Count { get; set; }
    }

    public class FanArtCountLock : IDisposable
    {
      protected FanArtCount _count;
      protected IDisposable _countSync;

      public FanArtCountLock(FanArtCount count, IDisposable countSync)
      {
        _count = count;
        _countSync = countSync;
      }

      public int Count
      {
        get { return _count.Count; }
        set { _count.Count = value; }
      }

      public void Dispose()
      {
        if (_countSync != null)
        {
          _countSync.Dispose();
          _countSync = null;
        }
      }
    }

    #endregion

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
