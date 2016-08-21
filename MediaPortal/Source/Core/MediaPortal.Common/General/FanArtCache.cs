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
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MediaPortal.Common.General
{
  public class FanArtCache
  {
    public static readonly int MAX_FANART_IMAGES = 5;
    public static readonly string FANART_CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArt\");

    static FanArtCache()
    {
    }

    public static IList<string> GetFanArtFiles(string mediaItemId, string fanartType)
    {
      List<string> fanartFiles = new List<string>();
      string path = Path.Combine(FANART_CACHE_PATH, mediaItemId, fanartType);
      if (Directory.Exists(path))
      {
        fanartFiles.AddRange(Directory.GetFiles(path, "*.jpg"));
        if(fanartFiles.Count < MAX_FANART_IMAGES)
          fanartFiles.AddRange(Directory.GetFiles(path, "*.png"));
        if (fanartFiles.Count < MAX_FANART_IMAGES)
          fanartFiles.AddRange(Directory.GetFiles(path, "*.tbn"));
      }
      return fanartFiles;
    }

    public static void DeleteFanArtFiles(string mediaItemId)
    {
      try
      {
        int maxTries = 3;
        if (Directory.Exists(Path.Combine(FANART_CACHE_PATH, mediaItemId)))
        {
          for (int i = 0; i < maxTries; i++)
          {
            try
            {
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
