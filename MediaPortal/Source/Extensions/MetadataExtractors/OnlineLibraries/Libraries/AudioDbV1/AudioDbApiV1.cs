#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.AudioDbV1.Data;
using Newtonsoft.Json;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.AudioDbV1
{
  internal class AudioDbApiV1
  {
    #region Constants

    public const string DefaultLanguage = "en";

    private const string URL_API_BASE = "http://www.theaudiodb.com/api/v1/json/{0}/";
    private const string URL_ARTIST_BY_NAME = URL_API_BASE + "search.php?s={1}";
    private const string URL_ARTIST_BY_MBID = URL_API_BASE + "artist-mb.php?i={1}";
    private const string URL_ARTIST_BY_TADB = URL_API_BASE + "artist.php?i={1}";
    private const string URL_ALBUM_BY_NAME_AND_ARTIST = URL_API_BASE + "searchalbum.php?s={1}&a={2}";
    private const string URL_ALBUM_BY_MBID = URL_API_BASE + "album-mb.php?i={1}";
    private const string URL_ALBUM_BY_TADB = URL_API_BASE + "album.php?m={1}";
    private const string URL_ALBUM_BY_ARTIST_TADB = URL_API_BASE + "album.php?i={1}";
    private const string URL_TRACK_BY_ARTIST_AND_NAME = URL_API_BASE + "searchtrack.php?s={1}&t={2}";
    private const string URL_TRACK_BY_ALBUM_TADB = URL_API_BASE + "track.php?m={1}";
    private const string URL_TRACK_BY_MBDB = URL_API_BASE + "track-mb.php?i={1}";
    private const string URL_TRACK_BY_TADB = URL_API_BASE + "track.php?h={1}";
    private const string URL_MVID_BY_ARTIST_TADB = URL_API_BASE + "mvid.php?i={1}";
    private const string URL_MVID_BY_ARTIST_MBID = URL_API_BASE + "mvid-mb.php?i={1}";

    #endregion

    #region Fields
    
    private readonly string _apiKey;
    private readonly string _cachePath;
    private readonly Downloader _downloader;

    #endregion

    #region Constructor

    public AudioDbApiV1(string apiKey, string cachePath)
    {
      _apiKey = apiKey;
      _cachePath = cachePath;
      _downloader = new Downloader { EnableCompression = true };
      _downloader.Headers["Accept"] = "application/json";
    }

    #endregion

    #region Public members

    public List<AudioDbArtist> SearchArtist(string artistName)
    {
      string url = GetUrl(URL_ARTIST_BY_NAME, Uri.EscapeDataString(artistName));
      AudioDbArtists audioDbArtists = _downloader.Download<AudioDbArtists>(url);
      if (audioDbArtists.Artists != null && audioDbArtists.Artists.Count > 0)
        return audioDbArtists.Artists;
      return null;
    }

    public List<AudioDbArtist> GetArtistByMbid(string mbid, bool cacheOnly)
    {
      AudioDbArtists audioDbArtists = null;
      string cache = CreateAndGetCacheName(mbid, "Artist_mbId");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbArtists = _downloader.ReadCache<AudioDbArtists>(cache);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_ARTIST_BY_MBID, mbid);
        audioDbArtists = _downloader.Download<AudioDbArtists>(url, cache);
      }
      if (audioDbArtists.Artists != null && audioDbArtists.Artists.Count > 0)
        return audioDbArtists.Artists;
      return null;
    }

    public AudioDbArtist GetArtist(long tadbArtistID, bool cacheOnly)
    {
      AudioDbArtists audioDbArtists = null;
      string cache = CreateAndGetCacheName(tadbArtistID, "Artist");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbArtists = _downloader.ReadCache<AudioDbArtists>(cache);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_ARTIST_BY_TADB, tadbArtistID);
        audioDbArtists = _downloader.Download<AudioDbArtists>(url, cache);
      }
      if (audioDbArtists.Artists != null && audioDbArtists.Artists.Count > 0)
        return audioDbArtists.Artists[0];
      return null;
    }

    public List<AudioDbAlbum> SearchAlbum(string artistName, string albumName)
    {
      string url = GetUrl(URL_ALBUM_BY_NAME_AND_ARTIST, Uri.EscapeDataString(artistName), Uri.EscapeDataString(albumName));
      AudioDbAlbums audioDbAlbums = _downloader.Download<AudioDbAlbums>(url);
      if (audioDbAlbums != null && audioDbAlbums.Albums != null && audioDbAlbums.Albums.Count > 0)
        return audioDbAlbums.Albums;
      return null;
    }

    public List<AudioDbAlbum> GetAlbumByMbid(string mbid, bool cacheOnly)
    {
      AudioDbAlbums audioDbAlbums = null;
      string cache = CreateAndGetCacheName(mbid, "Album_mbId");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbAlbums = _downloader.ReadCache<AudioDbAlbums>(cache);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_ALBUM_BY_MBID, mbid);
        audioDbAlbums = _downloader.Download<AudioDbAlbums>(url, cache);
      }
      if (audioDbAlbums.Albums != null && audioDbAlbums.Albums.Count > 0)
        return audioDbAlbums.Albums;
      return null;
    }

    public List<AudioDbAlbum> GetAlbumsByArtistId(long tadbArtistId, bool cacheOnly)
    {
      AudioDbAlbums audioDbAlbums = null;
      string cache = CreateAndGetCacheName(tadbArtistId, "ArtistAlbums");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbAlbums = _downloader.ReadCache<AudioDbAlbums>(cache);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_ALBUM_BY_ARTIST_TADB, tadbArtistId);
        audioDbAlbums = _downloader.Download<AudioDbAlbums>(url, cache);
      }
      if (audioDbAlbums.Albums != null && audioDbAlbums.Albums.Count > 0)
        return audioDbAlbums.Albums;
      return null;
    }

    public AudioDbAlbum GetAlbum(long tadbAlbumId, bool cacheOnly)
    {
      AudioDbAlbums audioDbAlbums = null;
      string cache = CreateAndGetCacheName(tadbAlbumId, "Album");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbAlbums = _downloader.ReadCache<AudioDbAlbums>(cache);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_ALBUM_BY_TADB, tadbAlbumId);
        audioDbAlbums = _downloader.Download<AudioDbAlbums>(url, cache);
      }
      if (audioDbAlbums.Albums != null && audioDbAlbums.Albums.Count > 0)
        return audioDbAlbums.Albums[0];
      return null;
    }

    public List<AudioDbTrack> GetTracksByAlbumId(long tadbAlbumId, bool cacheOnly)
    {
      AudioDbTracks audioDbTracks = null;
      string cache = CreateAndGetCacheName(tadbAlbumId, "AlbumTracks");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbTracks = _downloader.ReadCache<AudioDbTracks>(cache);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_TRACK_BY_ALBUM_TADB, tadbAlbumId);
        audioDbTracks = _downloader.Download<AudioDbTracks>(url, cache);
      }
      if (audioDbTracks.Tracks != null && audioDbTracks.Tracks.Count > 0)
        return audioDbTracks.Tracks;
      return null;
    }

    public List<AudioDbTrack> SearchTrack(string artistName, string trackName)
    {
      AudioDbTracks audioDbTracks = null;
      string url = GetUrl(URL_TRACK_BY_ARTIST_AND_NAME, Uri.EscapeDataString(artistName), Uri.EscapeDataString(trackName));
      audioDbTracks = _downloader.Download<AudioDbTracks>(url);
      if (audioDbTracks.Tracks != null && audioDbTracks.Tracks.Count > 0)
        return audioDbTracks.Tracks;
      return null;
    }

    public AudioDbTrack GetTrack(long tadbTrackId, bool cacheOnly)
    {
      AudioDbTracks audioDbTracks = null;
      string cache = CreateAndGetCacheName(tadbTrackId, "Track");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbTracks = _downloader.ReadCache<AudioDbTracks>(cache);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_TRACK_BY_TADB, tadbTrackId);
        audioDbTracks = _downloader.Download<AudioDbTracks>(url, cache);
      }
      if (audioDbTracks.Tracks != null && audioDbTracks.Tracks.Count > 0)
        return audioDbTracks.Tracks[0];
      return null;
    }

    public AudioDbTrack GetTrackByMbid(string mbid, bool cacheOnly)
    {
      AudioDbTracks audioDbTracks = null;
      string cache = CreateAndGetCacheName(mbid, "Track_mbId");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbTracks = _downloader.ReadCache<AudioDbTracks>(cache);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_TRACK_BY_MBDB, mbid);
        audioDbTracks = _downloader.Download<AudioDbTracks>(url, cache);
      }
      if (audioDbTracks.Tracks != null && audioDbTracks.Tracks.Count > 0)
        return audioDbTracks.Tracks[0];
      return null;
    }

    public List<AudioDbMvid> GetMusicVideosByArtistId(string tadbArtistId, bool cacheOnly)
    {
      AudioDbMvids audioDbMvids = null;
      string cache = CreateAndGetCacheName(tadbArtistId, "ArtistVideos");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbMvids = _downloader.ReadCache<AudioDbMvids>(cache);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_MVID_BY_ARTIST_TADB, tadbArtistId);
        audioDbMvids = _downloader.Download<AudioDbMvids>(url, cache);
      }
      if (audioDbMvids.MVids != null && audioDbMvids.MVids.Count > 0)
        return audioDbMvids.MVids;
      return null;
    }

    public List<AudioDbMvid> GetMusicVideosByArtistMbid(string mbid, bool cacheOnly)
    {
      AudioDbMvids audioDbMvids = null;
      string cache = CreateAndGetCacheName(mbid, "ArtistVideos_mbId");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbMvids = _downloader.ReadCache<AudioDbMvids>(cache);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_MVID_BY_ARTIST_MBID, mbid);
        audioDbMvids = _downloader.Download<AudioDbMvids>(url, cache);
      }
      if (audioDbMvids.MVids != null && audioDbMvids.MVids.Count > 0)
        return audioDbMvids.MVids;
      return null;
    }

    /// <summary>
    /// Downloads images in "original" size and saves them to cache.
    /// </summary>
    /// <param name="url">Image to download</param>
    /// <param name="category">Image category (Poster, Cover, Backdrop...)</param>
    /// <returns><c>true</c> if successful</returns>
    public bool DownloadImage(long id, string url, string category)
    {
      string cacheFileName = CreateAndGetCacheName(id, url, category);
      if (string.IsNullOrEmpty(cacheFileName))
        return false;

      _downloader.DownloadFile(url, cacheFileName);
      return true;
    }

    public byte[] GetImage(long id, string url, string category)
    {
      string cacheFileName = CreateAndGetCacheName(id, url, category);
      if (string.IsNullOrEmpty(cacheFileName))
        return null;

      return _downloader.ReadDownloadedFile(cacheFileName);
    }

    #endregion

    #region Protected members

    /// <summary>
    /// Builds and returns the full request url.
    /// </summary>
    /// <param name="urlBase">Query base</param>
    /// <param name="args">Optional arguments to format <paramref name="urlBase"/></param>
    /// <returns>Complete url</returns>
    protected string GetUrl(string urlBase, params object[] args)
    {
      List<object> arguments = new List<object>();
      arguments.Add(_apiKey);
      arguments.AddRange(args);
      return string.Format(urlBase, arguments.ToArray());
    }
    /// <summary>
    /// Creates a local file name for loading and saving <see cref="MovieImage"/>s.
    /// </summary>
    /// <param name="album"></param>
    /// <param name="category"></param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName(long id, string url, string category)
    {
      try
      {
        string folder = Path.Combine(_cachePath, string.Format(@"{0}\{1}", id, category));
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        return Path.Combine(folder, url.Substring(url.LastIndexOf('/') + 1));
      }
      catch
      {
        // TODO: logging
        return null;
      }
    }

    /// <summary>
    /// Creates a local file name for loading and saving details for movie. It supports both TMDB id and IMDB id.
    /// </summary>
    /// <param name="movieId"></param>
    /// <param name="prefix"></param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName<TE>(TE Id, string prefix)
    {
      try
      {
        string folder = Path.Combine(_cachePath, Id.ToString());
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        return Path.Combine(folder, string.Format("{0}.json", prefix));
      }
      catch
      {
        // TODO: logging
        return null;
      }
    }

    #endregion
  }
}
