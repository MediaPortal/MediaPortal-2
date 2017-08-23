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
using System.Linq;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Freedb;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Freedb.Data;
using System.IO;
using System.Text;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Extensions.OnlineLibraries.Wrappers
{
  class FreeDbWrapper : ApiWrapper<string, string>
  {
    private string _fileFormat = "CD_{0}.xmcd";

    protected FreeDBQuery _freeDbHandler;

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns><c>true</c> if successful</returns>
    public bool Init(string cachePath)
    {
      _freeDbHandler = new FreeDBQuery();
      SetDefaultLanguage("en");
      SetCachePath(cachePath);
      return true;
    }

    #region Search

    public override bool SearchTrack(TrackInfo trackSearch, string language, out List<TrackInfo> tracks)
    {
      tracks = null;

      if (string.IsNullOrEmpty(trackSearch.AlbumCdDdId) || trackSearch.TrackNum == 0)
        return false;

      //TODO: Split CDDB ID into disc id and genre?
      string genre = null;
      string discId = null;

      try
      {
        if (_freeDbHandler.Connect())
        {
          string[] xmcds = _freeDbHandler.GetDiscDetailsXMCD(genre, discId);
          if (xmcds != null)
          {
            string fileName = GetCacheFilePath(discId, genre);
            if (File.Exists(fileName) == false)
            {
              File.WriteAllLines(fileName, xmcds, Encoding.UTF8);
            }

            FreeDBCDInfoDetail discInfo = _freeDbHandler.GetDiscDetailsFromXMCD(xmcds);
            if (discInfo != null)
            {
              FreeDBCDTrackDetail foundTrack = null;
              foreach (FreeDBCDTrackDetail trackDetail in discInfo.Tracks)
              {
                if (trackDetail.TrackNumber == trackSearch.TrackNum)
                {
                  foundTrack = trackDetail;
                  break;
                }
              }
              if (foundTrack == null) return false;

              if (tracks == null)
                tracks = new List<TrackInfo>();

              TrackInfo info = new TrackInfo()
              {
                AlbumCdDdId = trackSearch.AlbumCdDdId,
                Album = discInfo.Title,
                AlbumArtists = ConvertToPersons(discInfo.Artist, PersonAspect.OCCUPATION_ARTIST),
                TotalTracks = discInfo.Tracks.Count(),
                ReleaseDate = discInfo.Year > 0 ? new DateTime(discInfo.Year, 1, 1) : default(DateTime?),

                TrackNum = foundTrack.TrackNumber,
                TrackName = foundTrack.Title,
                Artists = ConvertToPersons(foundTrack.Artist, PersonAspect.OCCUPATION_ARTIST),
                Duration = foundTrack.Duration
              };
              tracks.Add(info);
            }
          }
        }
      }
      finally
      { 
        _freeDbHandler.Disconnect();
      }

      return tracks != null;
    }

    public override bool SearchTrackAlbum(AlbumInfo albumSearch, string language, out List<AlbumInfo> albums)
    {
      albums = null;

      if (string.IsNullOrEmpty(albumSearch.CdDdId))
        return false;

      //TODO: Split CDDB ID into disc id and genre?
      string genre = null;
      string discId = null;

      try
      {
        if (_freeDbHandler.Connect())
        {
          string[] xmcds = _freeDbHandler.GetDiscDetailsXMCD(genre, discId);
          if (xmcds != null)
          {
            string fileName = GetCacheFilePath(discId, genre);
            if (File.Exists(fileName) == false)
            {
              File.WriteAllLines(fileName, xmcds, Encoding.UTF8);
            }

            FreeDBCDInfoDetail discInfo = _freeDbHandler.GetDiscDetailsFromXMCD(xmcds);
            if (discInfo != null)
            {
              if (albums == null)
                albums = new List<AlbumInfo>();

              AlbumInfo info = new AlbumInfo()
              {
                CdDdId = albumSearch.CdDdId,
                Album = discInfo.Title,
                Artists = ConvertToPersons(discInfo.Artist, PersonAspect.OCCUPATION_ARTIST),
                TotalTracks = discInfo.Tracks.Count(),
                ReleaseDate = discInfo.Year > 0 ? new DateTime(discInfo.Year, 1, 1) : default(DateTime?),
              };
              albums.Add(info);
            }
          }
        }
      }
      finally
      {
        _freeDbHandler.Disconnect();
      }

      return albums != null;
    }

    #endregion

    #region Convert

    private List<PersonInfo> ConvertToPersons(string name, string occupation)
    {
      if (string.IsNullOrEmpty(name))
        return new List<PersonInfo>();

      return new List<PersonInfo> {
        new PersonInfo()
        {
          Name = name,
          Occupation = occupation
        }
      };
    }

    #endregion

    #region Update

    public override bool UpdateFromOnlineMusicTrack(TrackInfo track, string language, bool cacheOnly)
    {
      try
      {
        if (string.IsNullOrEmpty(track.AlbumCdDdId) || track.TrackNum == 0)
          return false;

        //TODO: Split CDDB ID into disc id and genre?
        string discId = null;

        FreeDBCDInfoDetail discInfo;
        if (GetCachedDisc(discId, out discInfo))
        {
          FreeDBCDTrackDetail foundTrack = null;
          foreach (FreeDBCDTrackDetail trackDetail in discInfo.Tracks)
          {
            if (trackDetail.TrackNumber == track.TrackNum)
            {
              foundTrack = trackDetail;
              break;
            }
          }
          if (foundTrack == null) return false;

          track.Album = discInfo.Title;
          track.AlbumArtists = ConvertToPersons(discInfo.Artist, PersonAspect.OCCUPATION_ARTIST);
          track.TotalTracks = discInfo.Tracks.Count();
          track.ReleaseDate = discInfo.Year > 0 ? new DateTime(discInfo.Year, 1, 1) : default(DateTime?);

          track.TrackNum = foundTrack.TrackNumber;
          track.TrackName = foundTrack.Title;
          track.Artists = ConvertToPersons(foundTrack.Artist, PersonAspect.OCCUPATION_ARTIST);
          track.Duration = foundTrack.Duration;

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("FreeDbWrapper: Exception while processing track {0}", ex, track.ToString());
        return false;
      }
    }

    public override bool UpdateFromOnlineMusicTrackAlbum(AlbumInfo album, string language, bool cacheOnly)
    {
      try
      {
        if (string.IsNullOrEmpty(album.CdDdId))
          return false;

        //TODO: Split CDDB ID into disc id and genre?
        string discId = null;

        FreeDBCDInfoDetail discInfo;
        if (GetCachedDisc(discId, out discInfo))
        {
          album.Album = discInfo.Title;
          album.Artists = ConvertToPersons(discInfo.Artist, PersonAspect.OCCUPATION_ARTIST);
          album.TotalTracks = discInfo.Tracks.Count();
          album.ReleaseDate = discInfo.Year > 0 ? new DateTime(discInfo.Year, 1, 1) : default(DateTime?);

          foreach (FreeDBCDTrackDetail trackDetail in discInfo.Tracks)
          {
            TrackInfo track = new TrackInfo()
            {
              AlbumCdDdId = album.CdDdId,
              Album = discInfo.Title,
              AlbumArtists = ConvertToPersons(discInfo.Artist, PersonAspect.OCCUPATION_ARTIST),
              TotalTracks = discInfo.Tracks.Count(),
              ReleaseDate = discInfo.Year > 0 ? new DateTime(discInfo.Year, 1, 1) : default(DateTime?),

              TrackNum = trackDetail.TrackNumber,
              TrackName = trackDetail.Title,
              Artists = ConvertToPersons(trackDetail.Artist, PersonAspect.OCCUPATION_ARTIST),
              Duration = trackDetail.Duration
            };
            album.Tracks.Add(track);
          }

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("FreeDbWrapper: Exception while processing album {0}", ex, album.ToString());
        return false;
      }
    }

    #endregion

    #region Cache

    /// <summary>
    /// Return a cache file name for a CDDB ID
    /// </summary>
    /// <returns>Cache file name</returns>
    private string GetCacheFilePath(string cdDbId, string genre)
    {
      return Path.Combine(CachePath, string.Format(_fileFormat, cdDbId.ToUpperInvariant() + "." + String.Concat(genre.Split(Path.GetInvalidFileNameChars()))));
    }

    /// <summary>
    /// Gets all cache files matching the CDDB ID. 
    /// </summary>
    /// <param name="cdDbId">The CDDB ID</param>
    /// <returns>List of file matching files.</returns>
    private string[] GetMatchingCacheFiles(string cdDbId)
    {
      return Directory.GetFiles(CachePath, string.Format(_fileFormat, cdDbId.ToUpperInvariant() + ".*"));
    }

    /// <summary>
    /// Clears cache. 
    /// </summary>
    /// <returns><c>true</c> if successful</returns>
    public bool ClearCache()
    {
      bool retValue = true;
      foreach (string file in Directory.GetFiles(CachePath))
      {
        try { File.Delete(file); }
        catch { retValue = false; }
      }
      return retValue;
    }

    /// <summary>
    /// Get cached disc info. 
    /// </summary>
    /// <param name="cdDbId">The CDDB ID</param>
    /// <returns><c>true</c> if disc match</returns>
    public bool GetCachedDisc(string cdDbId, out FreeDBCDInfoDetail disc)
    {
      disc = null;
      string[] files = GetMatchingCacheFiles(cdDbId);
      if (files == null || files.Length == 0 || files.Length > 1)
        return false;

      disc = _freeDbHandler.GetDiscDetailsFromXMCD(File.ReadAllLines(files[0], Encoding.UTF8));
      return disc != null;
    }

    #endregion
  }
}
