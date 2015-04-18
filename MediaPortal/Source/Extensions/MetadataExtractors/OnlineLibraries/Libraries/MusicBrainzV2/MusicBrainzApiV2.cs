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
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
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

    public const string DefaultLanguage = "GB";

    private const string URL_API_BASE = "http://musicbrainz.org/ws/2/";
    private const string URL_GETRECORDING = URL_API_BASE + "recording/{0}?fmt=json";
    private const string URL_QUERYRECORDING = URL_API_BASE + "recording?query={0}&limit=5&fmt=json";

    #endregion

    #region Fields

    private static readonly FileVersionInfo FILE_VERSION_INFO;

    private readonly string _cachePath;
    private readonly Downloader _downloader;

    #endregion

    #region Constructor

    static MusicBrainzApiV2()
    {
      FILE_VERSION_INFO = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetCallingAssembly().Location);
    }

    public MusicBrainzApiV2(string cachePath)
    {
      _cachePath = cachePath;
      _downloader = new Downloader { EnableCompression = true };
      _downloader.Headers["Accept"] = "application/json";
      _downloader.Headers["User-Agent"] = "MediaPortal/" + FILE_VERSION_INFO.FileVersion + " (http://www.team-mediaportal.com/)";
    }

    #endregion

    #region Public members

    /// <summary>
    /// Search for tracks by name given in <paramref name="query"/> using the <paramref name="language"/>.
    /// </summary>
    /// <param name="language">Language</param>
    /// <returns>List of possible matches</returns>
    public IList<TrackSearchResult> SearchTrack(string title, string artist, string album, string genre, int? year, int? trackNum, string language)
    {
      string query = string.Format("\"{0}\"", title);
	    if(!string.IsNullOrEmpty(artist))
        query += string.Format(" and artistname:\"{0}\"", artist);
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

      Logger.Debug("Loading '{0}','{1}','{2}','{3}','{4}','{5}','{6} -> {7}", title, artist, album, genre, year, trackNum, language, url);
	
      return Parse(url);
    }

    [DataContract]
    private class RecordingResult
    {
      [DataMember(Name = "recordings")]
      public IList<TrackSearchResult> Results { get; set; }
    }

    public IList<TrackSearchResult> Parse(string url)
    {
      Logger.Debug("Loading {0}", url + " as " + _downloader.Headers["User-Agent"]);

      IList<TrackSearchResult> results = _downloader.Download<RecordingResult>(url).Results;
      foreach(TrackSearchResult result in results)
      {
        Logger.Debug("Result: Id={0} Title={1} ArtistId={2} ArtistName={3} AlbumId={4} AlbumName={5} AlbumArtistId={6} AlbumArtistName={7}",
          result.Id, result.Title,
          result.ArtistId, result.ArtistName,
          result.AlbumId, result.AlbumName,
          result.AlbumArtistId, result.AlbumArtistName);
      }

      return results;
    }

    /// <summary>
    /// Returns detailed information for a single <see cref="Track"/> with given <paramref name="id"/>. This method caches request
    /// to same tracks using the cache path given in <see cref="MusicBrainzApiV2"/> constructor.
    /// </summary>
    /// <param name="musicBrainzId">MusicBrainz id of track</param>
    /// <param name="language">Language</param>
    /// <returns>Track information</returns>
    public Track GetTrack(string musicBrainzId, string language)
    {
      string cache = CreateAndGetCacheName(musicBrainzId, language);
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        string json = File.ReadAllText(cache);
        return JsonConvert.DeserializeObject<Track>(json);
      }

      string url = GetUrl(URL_GETRECORDING, musicBrainzId);
      return _downloader.Download<Track>(url, cache);
    }

    /// <summary>
    /// Returns a <see cref="Data.ImageCollection"/> for the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">MusicBrainz id of track</param>
    /// <param name="language">Language</param>
    /// <returns>Image collection</returns>
    public ImageCollection GetImages(string id, string language)
    {
      // TODO: Fix
      throw new NotImplementedException();
    }

    /// <summary>
    /// Downloads images in "original" size and saves them to cache.
    /// </summary>
    /// <param name="image">Image to download</param>
    /// <param name="category">Image category (Poster, Cover, Backdrop...)</param>
    /// <returns><c>true</c> if successful</returns>
    public bool DownloadImage(TrackImage image, string category)
    {
      string cacheFileName = CreateAndGetCacheName(image, category);
      if (string.IsNullOrEmpty(cacheFileName))
        return false;

      // TODO: Fix
      throw new NotImplementedException();
    }

    public bool DownloadImages(TrackCollection trackCollection)
    {
      DownloadImages(trackCollection, true);
      DownloadImages(trackCollection, false);
      return true;
    }

    private bool DownloadImages(TrackCollection trackCollection, bool usePoster)
    {
      string cacheFileName = CreateAndGetCacheName(trackCollection, usePoster);
      if (string.IsNullOrEmpty(cacheFileName))
        return false;

      // TODO: Fix
      throw new NotImplementedException();
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
    protected string CreateAndGetCacheName(TrackImage image, string category)
    {
      try
      {
        string folder = Path.Combine(_cachePath, string.Format(@"{0}\{1}", image.TrackId, category));
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        return Path.Combine(folder, image.FilePath.TrimStart(new[] { '/' }));
      }
      catch
      {
        // TODO: logging
        return null;
      }
    }

    /// <summary>
    /// Creates a local file name for loading and saving images of a <see cref="TrackCollection"/>.
    /// </summary>
    /// <param name="collection">TrackCollection</param>
    /// <param name="usePoster"><c>true</c> for Poster, <c>false</c> for Backdrop</param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName(TrackCollection collection, bool usePoster)
    {
      try
      {
        string folder = Path.Combine(_cachePath, string.Format(@"COLL_{0}\{1}", collection.Id, usePoster ? "Posters" : "Backdrops"));
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        string fileName = usePoster ? collection.PosterPath : collection.BackdropPath;
        if (string.IsNullOrEmpty(fileName))
          return null;
        return Path.Combine(folder, fileName.TrimStart(new[] { '/' }));
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
    protected string CreateAndGetCacheName<TE>(TE trackId, string language)
    {
      try
      {
        string folder = Path.Combine(_cachePath, trackId.ToString());
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        return Path.Combine(folder, string.Format("track_{0}.json", language));
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
