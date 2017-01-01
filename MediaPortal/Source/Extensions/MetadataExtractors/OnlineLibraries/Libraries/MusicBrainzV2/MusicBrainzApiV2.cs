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
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data;
using System.Diagnostics;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2
{
  internal class MusicBrainzApiV2
  {
    #region Constants

    public const string DefaultLanguage = "US";

    private const string URL_API_BASE = "musicbrainz.org/ws/2/";
    private const string URL_API_MIRROR = "musicbrainz-mirror.eu:5000/ws/2/";
    private const string URL_FANART_API_BASE = "coverartarchive.org/";

    private const string URL_GETRECORDING = "recording/{0}?inc=artist-credits+discids+artist-rels+releases+tags+ratings+isrcs&fmt=json";
    private const string URL_GETRELEASE = "release/{0}?inc=artist-credits+labels+discids+recordings+tags&fmt=json";
    private const string URL_GETRELEASEGROUP = "release-group/{0}?inc=artist-credits+discids+artist-rels+releases+tags+ratings&fmt=json";
    private const string URL_GETARTIST = "artist/{0}?fmt=json";
    private const string URL_GETLABEL = "label/{0}?fmt=json";
    private const string URL_QUERYISRCRECORDING = "isrc/{0}?limit=5&fmt=json";
    private const string URL_QUERYRECORDING = "recording?query={0}&limit=5&fmt=json";
    private const string URL_FANART_LIST = "release/{0}/";
    private const string URL_QUERYLABEL = "label?query={0}&limit=5&fmt=json";
    private const string URL_QUERYARTIST = "artist?query={0}&limit=5&fmt=json";
    private const string URL_QUERYRELEASE = "release?query={0}&limit=5&fmt=json";

    #endregion

    #region Fields

    private static readonly FileVersionInfo FILE_VERSION_INFO;
    private readonly string _cachePath;
    private readonly MusicBrainzDownloader _downloader;
    private readonly bool _useHttps;

    #endregion

    #region Constructor

    static MusicBrainzApiV2()
    {
      FILE_VERSION_INFO = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetCallingAssembly().Location);
    }

    public MusicBrainzApiV2(string cachePath, bool useHttps)
    {
      _cachePath = cachePath;
      _useHttps = useHttps;
      _downloader = new MusicBrainzDownloader { EnableCompression = true };
      _downloader.Headers["Accept"] = "application/json";
      _downloader.Headers["User-Agent"] = "MediaPortal/" + FILE_VERSION_INFO.FileVersion + " (http://www.team-mediaportal.com/)";

      if (_useHttps)
        _downloader.Mirrors.Add("https://" + URL_API_BASE);
      else
        _downloader.Mirrors.Add("http://" + URL_API_BASE);
      _downloader.Mirrors.Add("http://" + URL_API_MIRROR);
    }

    #endregion

    #region Public members

    /// <summary>
    /// Search for tracks by name given in <paramref name="query"/>.
    /// </summary>
    /// <returns>List of possible matches</returns>
    public List<TrackResult> SearchTrack(string title, List<string> artists, string album, int? year, int? trackNum)
    {
      string query = string.Format("\"{0}\"", title);
      if (artists != null && artists.Count > 0)
      {
        if (artists.Count > 1) query += " and (";
        else query += " and ";
        for (int artist = 0; artist < artists.Count; artist++)
        {
          if (artist > 0) query += " and ";
          query += string.Format("artistname:\"{0}\"", artists[artist]);
        }
        if (artists.Count > 1) query += ")";
      }
      if (!string.IsNullOrEmpty(album))
      {
        if (album.IndexOf(" ") > 0)
          query += string.Format(" and (release:\"{0}\" or release:\"{1}\")", album, album.Split(' ')[0]);
        else
          query += string.Format(" and release:\"{0}\"", album);
      }
      if (year.HasValue)
        query += string.Format(" and date:{0}", year.Value);
      if (trackNum.HasValue)
        query += string.Format(" and tid:{0}", trackNum.Value);

      string url = GetUrl(URL_QUERYRECORDING, Uri.EscapeDataString(query));

      Logger.Debug("Loading '{0}','{1}','{2}','{3}','{4} -> {5}", title, string.Join(",", artists), album, year, trackNum, url);

      return ParseTracks(url);
    }

    public List<TrackResult> SearchTrackFromIsrc(string isrc)
    {
      string url = GetUrl(URL_QUERYISRCRECORDING, Uri.EscapeDataString(isrc));
      return ParseTracks(url);
    }

    public List<TrackResult> ParseTracks(string url)
    {
      List<TrackResult> tracks = new List<TrackResult>();
      TrackRecordingResult searchResult = Download<TrackRecordingResult>(url);
      if (searchResult == null)
        return tracks;
      List<TrackSearchResult> results = new List<TrackSearchResult>(searchResult.Results);
      foreach (TrackSearchResult result in results)
        tracks.AddRange(result.GetTracks());
      return tracks;
    }

    /// <summary>
    /// Search for releases by name given in <paramref name="query"/>.
    /// </summary>
    /// <returns>List of possible matches</returns>
    public List<TrackRelease> SearchRelease(string title, List<string> artists, int? year, int? trackCount)
    {
      string query = string.Format("release:\"{0}\"", title);
      if (artists != null && artists.Count > 0)
      {
        if (artists.Count > 1) query += " and (";
        else query += " and ";
        for (int artist = 0; artist < artists.Count; artist++)
        {
          if (artist > 0) query += " and ";
          query += string.Format("artistname:\"{0}\"", artists[artist]);
        }
        if (artists.Count > 1) query += ")";
      }
      if (year.HasValue)
        query += string.Format(" and date:{0}", year.Value);
      if (trackCount.HasValue)
        query += string.Format(" and tracksmedium:{0}", trackCount.Value);
      query += " and primarytype:album";
      query += " and status:official";

      string url = GetUrl(URL_QUERYRELEASE, Uri.EscapeDataString(query));
      TrackReleaseSearchResult searchResult = Download<TrackReleaseSearchResult>(url);
      if (searchResult == null)
        return new List<TrackRelease>();
      return searchResult.Releases;
    }

    /// <summary>
    /// Search for artist by name given in <paramref name="query"/>.
    /// </summary>
    /// <returns>List of possible matches</returns>
    public List<TrackArtist> SearchArtist(string artistName)
    {
      string query = string.Format("\"{0}\"", artistName);
      string url = GetUrl(URL_QUERYARTIST, Uri.EscapeDataString(query));
      TrackArtistResult searchResult = Download<TrackArtistResult>(url);
      if (searchResult == null)
        return new List<TrackArtist>();
      return searchResult.Results;
    }

    /// <summary>
    /// Search for label by name given in <paramref name="query"/>.
    /// </summary>
    /// <returns>List of possible matches</returns>
    public List<TrackLabelSearchResult> SearchLabel(string labelName)
    {
      string query = string.Format("\"{0}\"", labelName);
      string url = GetUrl(URL_QUERYLABEL, Uri.EscapeDataString(query));
      TrackLabelResult searchResult = Download<TrackLabelResult>(url);
      if (searchResult == null)
        return new List<TrackLabelSearchResult>();
      return searchResult.Results;
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="Track"/> with given <paramref name="id"/>. This method caches request
    /// to same tracks using the cache path given in <see cref="MusicBrainzApiV2"/> constructor.
    /// </summary>
    /// <param name="id">MusicBrainz id of track</param>
    /// <returns>Track information</returns>
    public Track GetTrack(string id, bool cahceOnly)
    {
      string cache = CreateAndGetCacheName(id, "Track");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<Track>(cache);
      }
      if (cahceOnly) return null;
      string url = GetUrl(URL_GETRECORDING, id);
      return Download<Track>(url, cache);
    }

    /// <summary>
    /// Returns cache file for a single <see cref="Track"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">MusicBrainz id of track</param>
    /// <returns>Cache file name</returns>
    public string GetTrackCacheFile(string id)
    {
      return CreateAndGetCacheName(id, "Track");
    }

    /// <summary>
    /// Returns detailed information for an album <see cref="TrackRelease"/> with given <paramref name="id"/>. This method caches request
    /// to same albums using the cache path given in <see cref="MusicBrainzApiV2"/> constructor.
    /// </summary>
    /// <param name="id">MusicBrainz id of album</param>
    /// <returns>Track information</returns>
    public TrackRelease GetAlbum(string id, bool cahceOnly)
    {
      string cache = CreateAndGetCacheName(id, "Album");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<TrackRelease>(cache);
      }
      if (cahceOnly) return null;
      string url = GetUrl(URL_GETRELEASE, id);
      return Download<TrackRelease>(url, cache);
    }

    /// <summary>
    /// Returns cache file for an album <see cref="TrackRelease"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">MusicBrainz id of album</param>
    /// <returns>Cache file name</returns>
    public string GetAlbumCacheFile(string id)
    {
      return CreateAndGetCacheName(id, "Album");
    }

    /// <summary>
    /// Returns detailed information for an release group <see cref="TrackReleaseGroup"/> with given <paramref name="id"/>. This method caches request
    /// to same groups using the cache path given in <see cref="MusicBrainzApiV2"/> constructor.
    /// </summary>
    /// <param name="id">MusicBrainz id of release group</param>
    /// <returns>Track information</returns>
    public TrackReleaseGroup GetReleaseGroup(string id, bool cahceOnly)
    {
      string cache = CreateAndGetCacheName(id, "ReleaseGroup");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<TrackReleaseGroup>(cache);
      }
      if (cahceOnly) return null;
      string url = GetUrl(URL_GETRELEASEGROUP, id);
      return Download<TrackReleaseGroup>(url, cache);
    }

    /// <summary>
    /// Returns cache file for a release group <see cref="TrackReleaseGroup"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">MusicBrainz id of release group</param>
    /// <returns>Cache file name</returns>
    public string GetReleaseGroupCacheFile(string id)
    {
      return CreateAndGetCacheName(id, "ReleaseGroup");
    }

    /// <summary>
    /// Returns detailed information for an artist <see cref="TrackArtist"/> with given <paramref name="id"/>. This method caches request
    /// to same artist using the cache path given in <see cref="MusicBrainzApiV2"/> constructor.
    /// </summary>
    /// <param name="id">MusicBrainz id of artist</param>
    /// <returns>Artist information</returns>
    public TrackArtist GetArtist(string id, bool cahceOnly)
    {
      string cache = CreateAndGetCacheName(id, "Artist");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<TrackArtist>(cache);
      }
      if (cahceOnly) return null;
      string url = GetUrl(URL_GETARTIST, id);
      return Download<TrackArtist>(url, cache);
    }

    /// <summary>
    /// Returns cache file for an artist <see cref="TrackArtist"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">MusicBrainz id of artists</param>
    /// <returns>Cache file name</returns>
    public string GetArtistCacheFile(string id)
    {
      return CreateAndGetCacheName(id, "Artist");
    }

    /// <summary>
    /// Returns detailed information for a music label <see cref="TrackLabel"/> with given <paramref name="id"/>. This method caches request
    /// to same label using the cache path given in <see cref="MusicBrainzApiV2"/> constructor.
    /// </summary>
    /// <param name="id">MusicBrainz id of nusic label</param>
    /// <returns>Music label information</returns>
    public TrackLabel GetLabel(string id, bool cahceOnly)
    {
      string cache = CreateAndGetCacheName(id, "Label");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<TrackLabel>(cache);
      }
      if (cahceOnly) return null;
      string url = GetUrl(URL_GETLABEL, id);
      return Download<TrackLabel>(url, cache);
    }

    /// <summary>
    /// Returns cache file for a music label <see cref="TrackLabel"/> with given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">MusicBrainz id of nusic label</param>
    /// <returns>Cache file name</returns>
    public string GetLabelCacheFile(string id)
    {
      return CreateAndGetCacheName(id, "Label");
    }

    /// <summary>
    /// Returns a <see cref="Data.TrackImageCollection"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">MusicBrainz id of album</param>
    /// <returns>Image collection</returns>
    public TrackImageCollection GetImages(string albumId)
    {
      if (string.IsNullOrEmpty(albumId)) return null;
      string cache = CreateAndGetCacheName(albumId, "Image");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<TrackImageCollection>(cache);
      }
      string url = GetUrl(URL_FANART_LIST, albumId);
      return Download<TrackImageCollection>(url, cache);
    }

    public bool HasImages(string albumId, string category = "Front")
    {
      try
      {
        if (string.IsNullOrEmpty(albumId)) return false;
        string url = GetUrl(URL_FANART_LIST, albumId);
        TrackImageCollection imageCollection = Download<TrackImageCollection>(url);
        if (imageCollection != null)
        {
          foreach (TrackImage image in imageCollection.Images)
          {
            foreach (string imageType in image.Types)
            {
              if (imageType.Equals(category, StringComparison.InvariantCultureIgnoreCase))
                if (!string.IsNullOrEmpty(image.ImageUrl)) return true;
            }
          }
        }
      }
      catch { }

      return false;
    }

    /// <summary>
    /// Downloads images in "original" size and saves them to cache.
    /// </summary>
    /// <param name="image">Image to download</param>
    /// <param name="folderPath">The folder to store the image</param>
    /// <returns><c>true</c> if successful</returns>
    public bool DownloadImage(string albumId, TrackImage image, string folderPath)
    {
      if (string.IsNullOrEmpty(albumId)) return false;
      string cacheFileName = CreateAndGetCacheName(albumId, image, folderPath);
      if (string.IsNullOrEmpty(cacheFileName))
        return false;

      _downloader.DownloadFile(image.ImageUrl, cacheFileName);
      return true;
    }

    public byte[] GetImage(string albumId, TrackImage image, string folderPath)
    {
      if (string.IsNullOrEmpty(albumId)) return null;
      string cacheFileName = CreateAndGetCacheName(albumId, image, folderPath);
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
      return string.Format(urlBase, args);
    }

    protected TE Download<TE>(string url, string saveCacheFile = null)
    {
      return _downloader.Download<TE>(url, saveCacheFile);
    }

    /// <summary>
    /// Creates a local file name for loading and saving <see cref="TrackImage"/>s.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="folderPath"></param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName(string id, TrackImage image, string folderPath)
    {
      try
      {
        string prefix = string.Format(@"MB({0})_", id);
        if (!Directory.Exists(folderPath))
          Directory.CreateDirectory(folderPath);
        return Path.Combine(folderPath, prefix + image.ImageUrl.Substring(image.ImageUrl.LastIndexOf('/') + 1));
      }
      catch
      {
        // TODO: logging
        return null;
      }
    }

    /// <summary>
    /// Creates a local file name for loading and saving details for track.
    /// </summary>
    /// <param name="trackId"></param>
    /// <param name="language"></param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName<TE>(TE trackId, string prefix)
    {
      try
      {
        if (trackId == null) return null;
        string folder = Path.Combine(_cachePath, trackId.ToString());
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

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
