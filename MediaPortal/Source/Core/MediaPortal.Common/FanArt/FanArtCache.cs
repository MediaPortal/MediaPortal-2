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
        string cacheTitle = Path.Combine(cacheFolder, FileUtils.GetSafeFilename(title + ".mpcache"));
        if (!Directory.Exists(cacheFolder))
        {
          Directory.CreateDirectory(cacheFolder);
          File.AppendAllText(cacheTitle, "");
        }
        else if (!File.Exists(cacheTitle))
        {
          File.AppendAllText(cacheTitle, "");
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

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
