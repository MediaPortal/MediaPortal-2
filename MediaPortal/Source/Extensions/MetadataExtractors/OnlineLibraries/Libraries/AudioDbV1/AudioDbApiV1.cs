#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Extensions.OnlineLibraries.Libraries.AudioDbV1.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.AudioDbV1
{
  internal class AudioDbApiV1
  {
    #region Constants

    public const string DefaultLanguage = "en";
    public static readonly Dictionary<string, string> AvailableLanguageMap = new Dictionary<string, string>()
    {
      {"en", "en" },
      {"de", "de" },
      {"fr", "fr" },
      {"it", "it" },
      {"zh", "cn" },
      {"ja", "jp" },
      {"ru", "ru" },
      {"es", "es" },
      {"pt", "pt" },
      {"sv", "se" },
      {"nl", "nl" },
      {"hu", "hu" },
      {"no", "no" },
      {"he", "il" },
      {"pl", "pl" },
    };

    private const string URL_API_BASE = "http://www.theaudiodb.com/api/v1/json/{0}/";
    private const string URL_ARTIST_BY_NAME = URL_API_BASE + "search.php?s={1}";
    private const string URL_ARTIST_BY_MBID = URL_API_BASE + "artist-mb.php?i={1}";
    private const string URL_ARTIST_BY_TADB = URL_API_BASE + "artist.php?i={1}";
    private const string URL_ALBUM_BY_NAME_AND_ARTIST = URL_API_BASE + "searchalbum.php?s={1}&a={2}";
    private const string URL_ALBUM_BY_NAME = URL_API_BASE + "searchalbum.php?s={1}";
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

    public async Task<List<AudioDbArtist>> SearchArtistAsync(string artistName, string language)
    {
      string url = GetUrl(URL_ARTIST_BY_NAME, Uri.EscapeDataString(artistName));
      AudioDbArtists audioDbArtists = await _downloader.DownloadAsync<AudioDbArtists>(url).ConfigureAwait(false);
      if (audioDbArtists.Artists != null && audioDbArtists.Artists.Count > 0)
      {
        List<AudioDbArtist> list = audioDbArtists.Artists.Where(a => a.ArtistId > 0).ToList();
        foreach (AudioDbArtist artist in list)
          artist.SetLanguage(language);
        if (list.Count > 0)
          return list;
      }
      return null;
    }

    public async Task<List<AudioDbArtist>> GetArtistByMbidAsync(string mbid, string language, bool cacheOnly)
    {
      AudioDbArtists audioDbArtists = null;
      string cache = CreateAndGetCacheName(mbid, "Artist_mbId");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbArtists = await _downloader.ReadCacheAsync<AudioDbArtists>(cache).ConfigureAwait(false);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_ARTIST_BY_MBID, mbid);
        audioDbArtists = await _downloader.DownloadAsync<AudioDbArtists>(url, cache).ConfigureAwait(false);
      }
      if (audioDbArtists.Artists != null && audioDbArtists.Artists.Count > 0)
      {
        List<AudioDbArtist> list = audioDbArtists.Artists.Where(a => a.ArtistId > 0).ToList();
        foreach (AudioDbArtist artist in list)
          artist.SetLanguage(language);
        if (list.Count > 0)
          return list;
      }
      return null;
    }

    public string GetArtistMbCacheFile(string mbid)
    {
      return CreateAndGetCacheName(mbid, "Artist_mbId");
    }

    public async Task<AudioDbArtist> GetArtistAsync(long tadbArtistID, string language, bool cacheOnly)
    {
      AudioDbArtists audioDbArtists = null;
      string cache = CreateAndGetCacheName(tadbArtistID, "Artist");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbArtists = await _downloader.ReadCacheAsync<AudioDbArtists>(cache).ConfigureAwait(false);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_ARTIST_BY_TADB, tadbArtistID);
        audioDbArtists = await _downloader.DownloadAsync<AudioDbArtists>(url, cache).ConfigureAwait(false);
      }
      if (audioDbArtists.Artists != null && audioDbArtists.Artists.Count > 0)
      {
        AudioDbArtist artist = audioDbArtists.Artists.Where(a => a.ArtistId > 0).FirstOrDefault();
        if(artist != null)
          artist.SetLanguage(language);
        return artist;
      }
      return null;
    }

    public string GetArtistCacheFile(long tadbArtistID)
    {
      return CreateAndGetCacheName(tadbArtistID, "Artist");
    }

    public async Task<List<AudioDbAlbum>> SearchAlbumAsync(string artistName, string albumName, string language)
    {
      string url = GetUrl(URL_ALBUM_BY_NAME_AND_ARTIST, Uri.EscapeDataString(artistName), Uri.EscapeDataString(albumName));
      AudioDbAlbums audioDbAlbums = await _downloader.DownloadAsync<AudioDbAlbums>(url).ConfigureAwait(false);
      if(audioDbAlbums == null || audioDbAlbums.Albums == null || audioDbAlbums.Albums.Count == 0)
      {
        url = GetUrl(URL_ALBUM_BY_NAME, Uri.EscapeDataString(albumName));
        audioDbAlbums = await _downloader.DownloadAsync<AudioDbAlbums>(url).ConfigureAwait(false);
      }
      if (audioDbAlbums != null && audioDbAlbums.Albums != null && audioDbAlbums.Albums.Count > 0)
      {
        List<AudioDbAlbum> list = audioDbAlbums.Albums.Where(a => a.AlbumId > 0).ToList();
        foreach (AudioDbAlbum album in list)
          album.SetLanguage(language);
        if (list.Count > 0)
          return list;
      }
      return null;
    }

    public async Task<List<AudioDbAlbum>> GetAlbumByMbidAsync(string mbid, string language, bool cacheOnly)
    {
      AudioDbAlbums audioDbAlbums = null;
      string cache = CreateAndGetCacheName(mbid, "Album_mbId");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbAlbums = await _downloader.ReadCacheAsync<AudioDbAlbums>(cache).ConfigureAwait(false);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_ALBUM_BY_MBID, mbid);
        audioDbAlbums = await _downloader.DownloadAsync<AudioDbAlbums>(url, cache).ConfigureAwait(false);
      }
      if (audioDbAlbums.Albums != null && audioDbAlbums.Albums.Count > 0)
      {
        List<AudioDbAlbum> list = audioDbAlbums.Albums.Where(a => a.AlbumId > 0).ToList();
        foreach (AudioDbAlbum album in list)
          album.SetLanguage(language);
        if (list.Count > 0)
          return list;
      }
      return null;
    }

    public string GetAlbumMbCacheFile(string mbid)
    {
      return CreateAndGetCacheName(mbid, "Album_mbId");
    }

    public async Task<List<AudioDbAlbum>> GetAlbumsByArtistIdAsync(long tadbArtistId, string language, bool cacheOnly)
    {
      AudioDbAlbums audioDbAlbums = null;
      string cache = CreateAndGetCacheName(tadbArtistId, "ArtistAlbums");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbAlbums = await _downloader.ReadCacheAsync<AudioDbAlbums>(cache).ConfigureAwait(false);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_ALBUM_BY_ARTIST_TADB, tadbArtistId);
        audioDbAlbums = await _downloader.DownloadAsync<AudioDbAlbums>(url, cache).ConfigureAwait(false);
      }
      if (audioDbAlbums.Albums != null && audioDbAlbums.Albums.Count > 0)
      {
        List<AudioDbAlbum> list = audioDbAlbums.Albums.Where(a => a.AlbumId > 0).ToList();
        foreach (AudioDbAlbum album in list)
          album.SetLanguage(language);
        if (list.Count > 0)
          return list;
      }
      return null;
    }

    public string GetArtistAlbumCacheFile(long tadbArtistId)
    {
      return CreateAndGetCacheName(tadbArtistId, "ArtistAlbums");
    }

    public async Task<AudioDbAlbum> GetAlbumAsync(long tadbAlbumId, string language, bool cacheOnly)
    {
      AudioDbAlbums audioDbAlbums = null;
      string cache = CreateAndGetCacheName(tadbAlbumId, "Album");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbAlbums = await _downloader.ReadCacheAsync<AudioDbAlbums>(cache).ConfigureAwait(false);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_ALBUM_BY_TADB, tadbAlbumId);
        audioDbAlbums = await _downloader.DownloadAsync<AudioDbAlbums>(url, cache).ConfigureAwait(false);
      }
      if (audioDbAlbums.Albums != null && audioDbAlbums.Albums.Count > 0)
      {
        AudioDbAlbum album = audioDbAlbums.Albums.Where(a => a.AlbumId > 0).FirstOrDefault();
        if (album != null)
          album.SetLanguage(language);
        return album;
      }
      return null;
    }

    public string GetAlbumCacheFile(long tadbAlbumId)
    {
      return CreateAndGetCacheName(tadbAlbumId, "Album");
    }

    public async Task<List<AudioDbTrack>> GetTracksByAlbumIdAsync(long tadbAlbumId, string language, bool cacheOnly)
    {
      AudioDbTracks audioDbTracks = null;
      string cache = CreateAndGetCacheName(tadbAlbumId, "AlbumTracks");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbTracks = await _downloader.ReadCacheAsync<AudioDbTracks>(cache).ConfigureAwait(false);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_TRACK_BY_ALBUM_TADB, tadbAlbumId);
        audioDbTracks = await _downloader.DownloadAsync<AudioDbTracks>(url, cache).ConfigureAwait(false);
      }
      if (audioDbTracks.Tracks != null && audioDbTracks.Tracks.Count > 0)
      {
        List<AudioDbTrack> list = audioDbTracks.Tracks.Where(t => t.TrackID > 0).ToList();
        foreach (AudioDbTrack track in list)
          track.SetLanguage(language);
        if (list.Count > 0)
          return list;
      }
      return null;
    }

    public string GetAlbumTracksCacheFile(long tadbAlbumId)
    {
      return CreateAndGetCacheName(tadbAlbumId, "AlbumTracks");
    }

    public async Task<List<AudioDbTrack>> SearchTrackAsync(string artistName, string trackName, string language)
    {
      string url = GetUrl(URL_TRACK_BY_ARTIST_AND_NAME, Uri.EscapeDataString(artistName), Uri.EscapeDataString(trackName));
      AudioDbTracks audioDbTracks = await _downloader.DownloadAsync<AudioDbTracks>(url).ConfigureAwait(false);
      if (audioDbTracks.Tracks != null && audioDbTracks.Tracks.Count > 0)
      {
        List<AudioDbTrack> list = audioDbTracks.Tracks.Where(t => t.TrackID > 0).ToList();
        foreach (AudioDbTrack track in list)
          track.SetLanguage(language);
        if (list.Count > 0)
          return list;
      }
      return null;
    }

    public async Task<AudioDbTrack> GetTrackAsync(long tadbTrackId, string language, bool cacheOnly)
    {
      AudioDbTracks audioDbTracks = null;
      string cache = CreateAndGetCacheName(tadbTrackId, "Track");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbTracks = await _downloader.ReadCacheAsync<AudioDbTracks>(cache).ConfigureAwait(false);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_TRACK_BY_TADB, tadbTrackId);
        audioDbTracks = await _downloader.DownloadAsync<AudioDbTracks>(url, cache).ConfigureAwait(false);
      }
      if (audioDbTracks.Tracks != null && audioDbTracks.Tracks.Count > 0)
      {
        AudioDbTrack track = audioDbTracks.Tracks.Where(t => t.TrackID > 0).FirstOrDefault();
        if (track != null)
          track.SetLanguage(language);
        return track;
      }
      return null;
    }

    public string GetTrackCacheFile(long tadbTrackId)
    {
      return CreateAndGetCacheName(tadbTrackId, "Track");
    }

    public async Task<AudioDbTrack> GetTrackByMbidAsync(string mbid, string language, bool cacheOnly)
    {
      AudioDbTracks audioDbTracks = null;
      string cache = CreateAndGetCacheName(mbid, "Track_mbId");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbTracks = await _downloader.ReadCacheAsync<AudioDbTracks>(cache).ConfigureAwait(false);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_TRACK_BY_MBDB, mbid);
        audioDbTracks = await _downloader.DownloadAsync<AudioDbTracks>(url, cache).ConfigureAwait(false);
      }
      if (audioDbTracks.Tracks != null && audioDbTracks.Tracks.Count > 0)
      {
        AudioDbTrack track = audioDbTracks.Tracks.Where(t => t.TrackID > 0).FirstOrDefault();
        if (track != null)
          track.SetLanguage(language);
        return track;
      }
      return null;
    }

    public string GetTrackMbCacheFile(string mbid)
    {
      return CreateAndGetCacheName(mbid, "Track_mbId");
    }

    public async Task<List<AudioDbMvid>> GetMusicVideosByArtistIdAsync(string tadbArtistId, string language, bool cacheOnly)
    {
      AudioDbMvids audioDbMvids = null;
      string cache = CreateAndGetCacheName(tadbArtistId, "ArtistVideos");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbMvids = await _downloader.ReadCacheAsync<AudioDbMvids>(cache).ConfigureAwait(false);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_MVID_BY_ARTIST_TADB, tadbArtistId);
        audioDbMvids = await _downloader.DownloadAsync<AudioDbMvids>(url, cache).ConfigureAwait(false);
      }
      if (audioDbMvids.MVids != null && audioDbMvids.MVids.Count > 0)
      {
        List<AudioDbMvid> list = audioDbMvids.MVids.Where(v => !string.IsNullOrEmpty(v.MusicVid)).ToList();
        foreach (AudioDbMvid vid in list)
          vid.SetLanguage(language);
        if (list.Count > 0)
          return list;
      }
      return null;
    }

    public string GetArtistMusicVideoCacheFile(string tadbArtistId)
    {
      return CreateAndGetCacheName(tadbArtistId, "ArtistVideos");
    }

    public async Task<List<AudioDbMvid>> GetMusicVideosByArtistMbidAsync(string mbid, string language, bool cacheOnly)
    {
      AudioDbMvids audioDbMvids = null;
      string cache = CreateAndGetCacheName(mbid, "ArtistVideos_mbId");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        audioDbMvids = await _downloader.ReadCacheAsync<AudioDbMvids>(cache).ConfigureAwait(false);
      }
      else
      {
        if (cacheOnly) return null;
        string url = GetUrl(URL_MVID_BY_ARTIST_MBID, mbid);
        audioDbMvids = await _downloader.DownloadAsync<AudioDbMvids>(url, cache).ConfigureAwait(false);
      }
      if (audioDbMvids.MVids != null && audioDbMvids.MVids.Count > 0)
      {
        List<AudioDbMvid> list = audioDbMvids.MVids.Where(v => !string.IsNullOrEmpty(v.MusicVid)).ToList();
        foreach (AudioDbMvid vid in list)
          vid.SetLanguage(language);
        if (list.Count > 0)
          return list;
      }
      return null;
    }

    public string GetArtistMusicVideoMbCacheFile(string mbid)
    {
      return CreateAndGetCacheName(mbid, "ArtistVideos_mbId");
    }

    /// <summary>
    /// Downloads images in "original" size and saves them to cache.
    /// </summary>
    /// <param name="url">Image to download</param>
    /// <param name="folderPath">The folder to store the image</param>
    /// <returns><c>true</c> if successful</returns>
    public Task<bool> DownloadImageAsync(long id, string url, string folderPath)
    {
      string cacheFileName = CreateAndGetCacheName(id, url, folderPath);
      if (string.IsNullOrEmpty(cacheFileName))
        return Task.FromResult(false);

      return _downloader.DownloadFileAsync(url, cacheFileName);
    }

    public Task<byte[]> GetImageAsync(long id, string url, string folderPath)
    {
      string cacheFileName = CreateAndGetCacheName(id, url, folderPath);
      if (string.IsNullOrEmpty(cacheFileName))
        return Task.FromResult<byte[]>(null);

      return _downloader.ReadDownloadedFileAsync(cacheFileName);
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
    /// <param name="folderPath"></param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName(long id, string url, string folderPath)
    {
      try
      {
        string prefix = string.Format(@"TADB({0})_", id);
        if (!Directory.Exists(folderPath))
          Directory.CreateDirectory(folderPath);
        return Path.Combine(folderPath, prefix + url.Substring(url.LastIndexOf('/') + 1));
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
