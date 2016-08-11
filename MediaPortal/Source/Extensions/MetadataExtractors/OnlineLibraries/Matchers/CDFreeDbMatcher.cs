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

using System;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public class CDFreeDbMatcher : MusicMatcher<string, string>
  {
    #region Static instance

    public static CDFreeDbMatcher Instance
    {
      get { return ServiceRegistration.Get<CDFreeDbMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FreeDB\");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromMinutes(1);

    #endregion

    #region Init

    public CDFreeDbMatcher() : 
      base(CACHE_PATH, MAX_MEMCACHE_DURATION)
    {
    }

    public override bool InitWrapper(bool useHttps)
    {
      try
      {
        FreeDbWrapper wrapper = new FreeDbWrapper();
        if (wrapper.Init(CACHE_PATH))
        {
          _wrapper = wrapper;
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("CDFreeDbMatcher: Error initializing wrapper", ex);
      }
      return false;
    }

    #endregion

    #region Translators

    protected override bool GetTrackAlbumId(AlbumInfo album, out string id)
    {
      id = null;
      if (!string.IsNullOrEmpty(album.CdDdId))
        id = album.CdDdId;
      return id != null;
    }

    protected override bool SetTrackAlbumId(AlbumInfo album, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        album.CdDdId = id;
        return true;
      }
      return false;
    }

    protected override bool GetTrackId(TrackInfo track, out string id)
    {
      id = null;
      if (!string.IsNullOrEmpty(track.AlbumCdDdId))
        id = track.AlbumCdDdId;
      return id != null;
    }

    protected override bool SetTrackId(TrackInfo track, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        track.AlbumCdDdId = id;
        return true;
      }
      return false;
    }

    #endregion

    #region FanArt

    public override List<string> GetFanArtFiles<T>(T infoObject, string scope, string type)
    {
      // No fanart
      return new List<string>();
    }

    protected override void DownloadFanArt(string downloadId)
    {
      // No fanart to download
      FinishDownloadFanArt(downloadId);
    }

    #endregion
  }
}
