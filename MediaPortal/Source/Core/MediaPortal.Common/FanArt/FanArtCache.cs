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
using MediaPortal.Common.Settings;
using MediaPortal.Utilities.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MediaPortal.Common.FanArt
{
  public class FanArtCache
  {
    public static readonly Dictionary<string, int> MAX_FANART_IMAGES = new Dictionary<string, int>();
    public static readonly string FANART_CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArt\");
    public const int FANART_CLEAN_DELAY = 300000;

    private static Dictionary<string, Dictionary<string, FanArtCount>> _fanArtCounts = new Dictionary<string, Dictionary<string, FanArtCount>>();
    private static Timer _clearCountTimer = new Timer(ClearFanArtCount, null, Timeout.Infinite, Timeout.Infinite);
    private static object _fanArtCountSync = new object();
    private static object _initSync = new object();

    static FanArtCache()
    {
      FanArtSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<FanArtSettings>();
      MAX_FANART_IMAGES.Add(FanArtTypes.Banner, settings.MaxBannerFanArt);
      MAX_FANART_IMAGES.Add(FanArtTypes.ClearArt, settings.MaxClearArt);
      MAX_FANART_IMAGES.Add(FanArtTypes.Cover, settings.MaxPosterFanArt);
      MAX_FANART_IMAGES.Add(FanArtTypes.DiscArt, settings.MaxDiscArt);
      MAX_FANART_IMAGES.Add(FanArtTypes.FanArt, settings.MaxBackdropFanArt);
      MAX_FANART_IMAGES.Add(FanArtTypes.Logo, settings.MaxLogoFanArt);
      MAX_FANART_IMAGES.Add(FanArtTypes.Poster, settings.MaxPosterFanArt);
      MAX_FANART_IMAGES.Add(FanArtTypes.Thumbnail, settings.MaxThumbFanArt);
      MAX_FANART_IMAGES.Add(FanArtTypes.Undefined, 0);
    }

    public static void InitFanArtCache(string mediaItemId)
    {
      lock (_initSync)
      {
        mediaItemId = mediaItemId.ToUpperInvariant();
        string cacheFolder = Path.Combine(FANART_CACHE_PATH, mediaItemId);
        if (!Directory.Exists(cacheFolder))
        {
          Directory.CreateDirectory(cacheFolder);
        }
      }
    }

    public static void InitFanArtCache(string mediaItemId, string title)
    {
      lock (_initSync)
      {
        mediaItemId = mediaItemId.ToUpperInvariant();
        string cacheFolder = Path.Combine(FANART_CACHE_PATH, mediaItemId);
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

    public static IList<string> GetFanArtFiles(string mediaItemId, string fanartType)
    {
      mediaItemId = mediaItemId.ToUpperInvariant();
      List<string> fanartFiles = new List<string>();
      string path = Path.Combine(FANART_CACHE_PATH, mediaItemId, fanartType);
      if (Directory.Exists(path))
      {
        fanartFiles.AddRange(Directory.GetFiles(path, "*.jpg"));
        if (fanartFiles.Count < MAX_FANART_IMAGES[fanartType])
          fanartFiles.AddRange(Directory.GetFiles(path, "*.png"));
        if (fanartFiles.Count < MAX_FANART_IMAGES[fanartType])
          fanartFiles.AddRange(Directory.GetFiles(path, "*.tbn"));
      }
      return fanartFiles;
    }

    public static void DeleteFanArtFiles(string mediaItemId)
    {
      try
      {
        string folderPath = Path.Combine(FANART_CACHE_PATH, mediaItemId);
        mediaItemId = mediaItemId.ToUpperInvariant();
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
        Logger.Warn("Unable to delete FanArt files for media item {0}", ex, mediaItemId);
      }
    }

    #region FanArt Count

    public class FanArtCount
    {
      public object SyncObj { get; private set; }
      public int Count { get; set; }

      public FanArtCount(int count)
      {
        SyncObj = new object();
        Count = count;
      }
    }

    public class FanArtCountLock : IDisposable
    {
      protected FanArtCount _count;
      public int Count { get; set; }

      public FanArtCountLock(FanArtCount count)
      {
        _count = count;
        Monitor.Enter(_count.SyncObj);
        Count = _count.Count;
      }

      public void Dispose()
      {
        _count.Count = Count;
        Monitor.Exit(_count.SyncObj);
      }
    }

    private static void ClearFanArtCount(object state)
    {
      lock (_fanArtCountSync)
      {
        _clearCountTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _fanArtCounts.Clear();
      }
    }

    protected static FanArtCount InitFanArtCount(string mediaItemId, string fanArtType)
    {
      lock (_fanArtCountSync)
      {
        _clearCountTimer.Change(FANART_CLEAN_DELAY, Timeout.Infinite);
        Dictionary<string, FanArtCount> mediaItemCounts;
        if (!_fanArtCounts.TryGetValue(mediaItemId, out mediaItemCounts))
          _fanArtCounts[mediaItemId] = mediaItemCounts = new Dictionary<string, FanArtCount>();
        FanArtCount count;
        if (!mediaItemCounts.TryGetValue(fanArtType, out count))
          mediaItemCounts[fanArtType] = count = new FanArtCount(GetFanArtFiles(mediaItemId, fanArtType).Count);
        return count;
      }
    }

    public static FanArtCountLock GetFanArtCountLock(string mediaItemId, string fanArtType)
    {
      if (string.IsNullOrEmpty(mediaItemId))
        return null;
      return new FanArtCountLock(InitFanArtCount(mediaItemId, fanArtType));
    }

    #endregion

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
