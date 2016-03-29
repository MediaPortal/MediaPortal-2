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
    private const string URL_QUERYRECORDING = URL_API_BASE + "recording?query={0}&limit=5&fmt=json";
    private const string URL_FANART_LIST = URL_FANART_API_BASE + "release/{0}/";

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
    /// <param name="language">Language</param>
    /// <returns>List of possible matches</returns>
    public List<TrackResult> SearchTrack(string title, string[] artists, string album, int? year, int? trackNum)
    {
      string query = string.Format("\"{0}\"", title);
      if (artists != null && artists.Length > 0)
      {
        if (artists.Length > 1) query += " and (";
        else query += " and ";
        for (int artist = 0; artist <artists.Length; artist++)
        {
          if(artist > 0) query += " and ";
          query += string.Format("artistname:\"{0}\"", artists[artist]);
        }
        if (artists.Length > 1) query += ")";
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
	
      return Parse(url);
    }

    public List<TrackResult> Parse(string url)
    {
      Logger.Debug("Loading {0}", url + " as " + _downloader.Headers["User-Agent"]);

      List<TrackResult> tracks = new List<TrackResult>();
      List<TrackSearchResult> results = new List<TrackSearchResult>(_downloader.Download<RecordingResult>(url).Results);
      foreach (TrackSearchResult result in results) tracks.AddRange(result.GetTracks());
      return tracks;
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="Track"/> with given <paramref name="id"/>. This method caches request
    /// to same tracks using the cache path given in <see cref="MusicBrainzApiV2"/> constructor.
    /// </summary>
    /// <param name="id">MusicBrainz id of track</param>
    /// <returns>Track information</returns>
    public Track GetTrack(string id)
    {
      string cache = CreateAndGetCacheName(id, "track");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        string json = File.ReadAllText(cache);
        return JsonConvert.DeserializeObject<Track>(json);
      }

      string url = GetUrl(URL_GETRECORDING, id);
      return _downloader.Download<Track>(url, cache);
    }

    /// <summary>
    /// Returns a <see cref="Data.TrackImageCollection"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">MusicBrainz id of album</param>
    /// <returns>Image collection</returns>
    public TrackImageCollection GetImages(string albumId)
    {
      string cache = CreateAndGetCacheName(albumId, "image");
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
      string cacheFileName = CreateAndGetCacheName(albumId, image, category);
      if (string.IsNullOrEmpty(cacheFileName))
        return false;

      _downloader.DownloadFile(image.ImageUrl, cacheFileName);
      return true;
    }

    public bool DownloadImages(string albumId, TrackImageCollection imageCollection, string category = "Front", string folderCategory = "Covers")
    {
      if (imageCollection == null) return false;
        foreach (TrackImage image in imageCollection.Images)
      {
        foreach (string imageType in image.Types)
        {
          if (imageType.Equals(category, StringComparison.InvariantCultureIgnoreCase))
            DownloadImage(albumId, image, folderCategory);
        }
      }
      return true;
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
