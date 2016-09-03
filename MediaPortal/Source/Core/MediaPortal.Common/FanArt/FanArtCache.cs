#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

    private static Dictionary<string, Dictionary<string, int>> _fanArtCount = new Dictionary<string, Dictionary<string, int>>();
    private static Dictionary<string, Dictionary<string, object>> _fanArtLock = new Dictionary<string, Dictionary<string, object>>();
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
        string cacheFile = Path.Combine(cacheFolder, FileUtils.GetSafeFilename(title.Trim().ToUpperInvariant() + ".mpcache"));
        if (!Directory.Exists(cacheFolder))
        {
          Directory.CreateDirectory(cacheFolder);
          File.AppendAllText(cacheFile, title);
        }
        else if (!File.Exists(cacheFile))
        {
          File.AppendAllText(cacheFile, title);
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
        mediaItemId = mediaItemId.ToUpperInvariant();
        int maxTries = 3;
        if (Directory.Exists(Path.Combine(FANART_CACHE_PATH, mediaItemId)))
        {
          for (int i = 0; i < maxTries; i++)
          {
            try
            {
              if (Directory.Exists(Path.Combine(FANART_CACHE_PATH, mediaItemId)))
                Directory.Delete(Path.Combine(FANART_CACHE_PATH, mediaItemId), true);
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

    public class FanArtCountLock : IDisposable
    {
      public int Count { get; set; }
      public string Id { get; private set; }
      public string Type { get; private set; }

      public FanArtCountLock(string MediaItemId, string FanArtType)
      {
        Monitor.Enter(_fanArtLock[MediaItemId][FanArtType]);
        Id = MediaItemId;
        Type = FanArtType;
        Count = _fanArtCount[MediaItemId][FanArtType];
      }

      public void Dispose()
      {
        _fanArtCount[Id][Type] = Count;
        Monitor.Exit(_fanArtLock[Id][Type]);
      }
    }

    private static void ClearFanArtCount(object state)
    {
      lock (_fanArtCountSync)
      {
        _clearCountTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _fanArtCount.Clear();
        _fanArtLock.Clear();
      }
    }

    public static void InitFanArtCount(string MediaItemId, string FanArtType)
    {
      lock (_fanArtCountSync)
      {
        _clearCountTimer.Change(FANART_CLEAN_DELAY, Timeout.Infinite);
        if (!_fanArtCount.ContainsKey(MediaItemId) || !_fanArtCount[MediaItemId].ContainsKey(FanArtType))
        {
          if (!_fanArtCount.ContainsKey(MediaItemId))
            _fanArtCount.Add(MediaItemId, new Dictionary<string, int>());
          if (!_fanArtCount[MediaItemId].ContainsKey(FanArtType))
            _fanArtCount[MediaItemId].Add(FanArtType, GetFanArtFiles(MediaItemId, FanArtType).Count);

          if (!_fanArtLock.ContainsKey(MediaItemId))
            _fanArtLock.Add(MediaItemId, new Dictionary<string, object>());
          if (!_fanArtLock[MediaItemId].ContainsKey(FanArtType))
            _fanArtLock[MediaItemId].Add(FanArtType, new object());
        }
      }
    }

    public static FanArtCountLock GetFanArtCountLock(string MediaItemId, string FanArtType)
    {
      if (string.IsNullOrEmpty(MediaItemId))
        return null;

      _clearCountTimer.Change(FANART_CLEAN_DELAY, Timeout.Infinite);
      lock (_fanArtCountSync)
      {
        if (_fanArtCount.ContainsKey(MediaItemId) && _fanArtCount[MediaItemId].ContainsKey(FanArtType))
        {
          FanArtCountLock countLock = new FanArtCountLock(MediaItemId, FanArtType);
          return countLock;
        }
      }
      return null;
    }

    #endregion

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
