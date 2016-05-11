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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data;
using Newtonsoft.Json;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2
{
  internal class MusicBrainzApiV2
  {
    #region Constants

    public const string DefaultLanguage = "US";

    private const string URL_API_BASE = "http://musicbrainz.org/ws/2/";
    private const string URL_FANART_API_BASE = "http://coverartarchive.org/";

    private const string URL_GETRECORDING = URL_API_BASE + "recording/{0}?inc=artist-credits+discids+artist-rels+releases+tags+ratings&fmt=json";
    private const string URL_GETRELEASE = URL_API_BASE + "release/{0}?inc=artist-credits+labels+discids+recordings+tags&fmt=json";
    private const string URL_GETRELEASEGROUP = URL_API_BASE + "release-group/{0}?inc=artist-credits+discids+artist-rels+releases+tags+ratings&fmt=json";
    private const string URL_GETARTIST = URL_API_BASE + "artist/{0}?fmt=json";
    private const string URL_QUERYRECORDING = URL_API_BASE + "recording?query={0}&limit=5&fmt=json";
    private const string URL_FANART_LIST = URL_FANART_API_BASE + "release/{0}/";
    private const string URL_QUERYLABEL = URL_API_BASE + "label?query={0}&limit=5&fmt=json";
    private const string URL_QUERYARTIST = URL_API_BASE + "artist?query={0}&limit=5&fmt=json";

    #endregion

    #region Fields

    private readonly string _cachePath;
    private readonly Downloader _downloader;

    #endregion

    #region Constructor

    public MusicBrainzApiV2(string cachePath)
    {
      _cachePath = cachePath;
      _downloader = new Downloader { EnableCompression = true };
      _downloader.Headers["Accept"] = "application/json";
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
        for (int artist = 0; artist <artists.Count; artist++)
        {
          if(artist > 0) query += " and ";
          query += string.Format("artistname:\"{0}\"", artists[artist]);
        }
        if (artists.Count > 1) query += ")";
      }
      if (!string.IsNullOrEmpty(album))
      {
        if(album.IndexOf(" ") > 0)
          query += string.Format(" and (release:\"{0}\" or release:\"{1}\")", album, album.Split(' ')[0]);
        else
          query += string.Format(" and release:\"{0}\"", album);
      }
	    if(year.HasValue)
        query += string.Format(" and date:{0}", year.Value);
	    if(trackNum.HasValue)
        query += string.Format(" and tid:{0}", trackNum.Value);

      string url = GetUrl(URL_QUERYRECORDING, Uri.EscapeDataString(query));

      Logger.Debug("Loading '{0}','{1}','{2}','{3}','{4} -> {5}", title, string.Join(",", artists), album, year, trackNum, url);
	
      return ParseTracks(url);
    }

    public List<TrackResult> ParseTracks(string url)
    {
      List<TrackResult> tracks = new List<TrackResult>();
      List<TrackSearchResult> results = new List<TrackSearchResult>(_downloader.Download<TrackRecordingResult>(url).Results);
      foreach (TrackSearchResult result in results) tracks.AddRange(result.GetTracks());
      return tracks;
    }

    /// <summary>
    /// Search for artist by name given in <paramref name="query"/>.
    /// </summary>
    /// <returns>List of possible matches</returns>
    public List<TrackArtist> SearchArtist(string artistName)
    {
      string query = string.Format("\"{0}\"", artistName);
      string url = GetUrl(URL_QUERYARTIST, Uri.EscapeDataString(query));

      return _downloader.Download<TrackArtistResult>(url).Results;
    }

    /// <summary>
    /// Search for label by name given in <paramref name="query"/>.
    /// </summary>
    /// <returns>List of possible matches</returns>
    public List<TrackLabelSearchResult> SearchLabel(string labelName)
    {
      string query = string.Format("\"{0}\"", labelName);
      string url = GetUrl(URL_QUERYLABEL, Uri.EscapeDataString(query));

      return _downloader.Download<TrackLabelResult>(url).Results;
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="Track"/> with given <paramref name="id"/>. This method caches request
    /// to same tracks using the cache path given in <see cref="MusicBrainzApiV2"/> constructor.
    /// </summary>
    /// <param name="id">MusicBrainz id of track</param>
    /// <returns>Track information</returns>
    public Track GetTrack(string id)
    {
      string cache = CreateAndGetCacheName(id, "Track");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        string json = File.ReadAllText(cache);
        return JsonConvert.DeserializeObject<Track>(json);
      }

      string url = GetUrl(URL_GETRECORDING, id);
      return _downloader.Download<Track>(url, cache);
    }

    /// <summary>
    /// Returns detailed information for an album <see cref="TrackRelease"/> with given <paramref name="id"/>. This method caches request
    /// to same albums using the cache path given in <see cref="MusicBrainzApiV2"/> constructor.
    /// </summary>
    /// <param name="id">MusicBrainz id of album</param>
    /// <returns>Track information</returns>
    public TrackRelease GetAlbum(string id)
    {
      string cache = CreateAndGetCacheName(id, "Album");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        string json = File.ReadAllText(cache);
        return JsonConvert.DeserializeObject<TrackRelease>(json);
      }

      string url = GetUrl(URL_GETRELEASE, id);
      return _downloader.Download<TrackRelease>(url, cache);
    }

    /// <summary>
    /// Returns detailed information for an release group <see cref="TrackReleaseGroup"/> with given <paramref name="id"/>. This method caches request
    /// to same groups using the cache path given in <see cref="MusicBrainzApiV2"/> constructor.
    /// </summary>
    /// <param name="id">MusicBrainz id of release group</param>
    /// <returns>Track information</returns>
    public TrackReleaseGroup GetReleaseGroup(string id)
    {
      string cache = CreateAndGetCacheName(id, "ReleaseGroup");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        string json = File.ReadAllText(cache);
        return JsonConvert.DeserializeObject<TrackReleaseGroup>(json);
      }

      string url = GetUrl(URL_GETRELEASEGROUP, id);
      return _downloader.Download<TrackReleaseGroup>(url, cache);
    }

    /// <summary>
    /// Returns detailed information for an artist <see cref="TrackArtist"/> with given <paramref name="id"/>. This method caches request
    /// to same artist using the cache path given in <see cref="MusicBrainzApiV2"/> constructor.
    /// </summary>
    /// <param name="id">MusicBrainz id of artist</param>
    /// <returns>Artist information</returns>
    public TrackArtist GetArtist(string id)
    {
      string cache = CreateAndGetCacheName(id, "Artist");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        string json = File.ReadAllText(cache);
        return JsonConvert.DeserializeObject<TrackArtist>(json);
      }

      string url = GetUrl(URL_GETARTIST, id);
      return _downloader.Download<TrackArtist>(url, cache);
    }

    /// <summary>
    /// Returns a <see cref="Data.TrackImageCollection"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">MusicBrainz id of album</param>
    /// <returns>Image collection</returns>
    public TrackImageCollection GetImages(string albumId)
    {
      if(string.IsNullOrEmpty(albumId)) return null;
      string cache = CreateAndGetCacheName(albumId, "Image");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        string json = File.ReadAllText(cache);
        return JsonConvert.DeserializeObject<TrackImageCollection>(json);
      }
      string url = GetUrl(URL_FANART_LIST, albumId);
      return _downloader.Download<TrackImageCollection>(url, cache);
    }

    public bool HasImages(string albumId, string category = "Front")
    {
      try
      {
        if (string.IsNullOrEmpty(albumId)) return false;
        string url = GetUrl(URL_FANART_LIST, albumId);
        TrackImageCollection imageCollection = _downloader.Download<TrackImageCollection>(url);
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
    /// <param name="category">Image category (Front, Back, ...)</param>
    /// <returns><c>true</c> if successful</returns>
    public bool DownloadImage(string albumId, TrackImage image, string category)
    {
      if (string.IsNullOrEmpty(albumId)) return false;
      string cacheFileName = CreateAndGetCacheName(albumId, image, category);
      if (string.IsNullOrEmpty(cacheFileName))
        return false;

      _downloader.DownloadFile(image.ImageUrl, cacheFileName);
      return true;
    }

    public byte[] GetImage(string albumId, TrackImage image, string category)
    {
      if (string.IsNullOrEmpty(albumId)) return null;
      string cacheFileName = CreateAndGetCacheName(albumId, image, category);
      if (string.IsNullOrEmpty(cacheFileName))
        return null;

      if (File.Exists(cacheFileName))
        return File.ReadAllBytes(cacheFileName);

      return null;
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

    /// <summary>
    /// Creates a local file name for loading and saving <see cref="TrackImage"/>s.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="category"></param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName(string Id, TrackImage image, string category)
    {
      try
      {
        string folder = Path.Combine(_cachePath, string.Format(@"{0}\{1}", Id, category));
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        return Path.Combine(folder, image.ImageUrl.Substring(image.ImageUrl.LastIndexOf('/') + 1));
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
