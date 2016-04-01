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

using System.IO;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3.Data;
using Newtonsoft.Json;
using MediaPortal.Common.Logging;
using MediaPortal.Common;
using System.Linq;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3
{
  internal class FanArtTVApiV3
  {
    #region Constants

    public const string DefaultLanguage = "en";

    private const string URL_API_BASE = "http://webservice.fanart.tv/v3/";
    private const string URL_GETMOVIE =   URL_API_BASE + "movies/{0}";
    private const string URL_GETMUSICARTIST = URL_API_BASE + "music/{0}";
    private const string URL_GETMUSICALBUM =  URL_API_BASE + "music/albums/{0}";
    private const string URL_GETMUSICLABEL = URL_API_BASE + "music/labels/{0}";
    private const string URL_GETSERIES =  URL_API_BASE + "tv/{0}";

    #endregion

    #region Fields

    private readonly string _apiKey;
    private readonly string _cachePath;
    private readonly Downloader _downloader;

    #endregion

    #region Constructor

    public FanArtTVApiV3(string apiKey, string cachePath)
    {
      _apiKey = apiKey;
      _cachePath = cachePath;
      _downloader = new Downloader { EnableCompression = true };
      _downloader.Headers["Accept"] = "application/json";
    }

    #endregion

    #region Public members

    public ArtistThumbs GetArtistThumbs(string artistMbid)
    {
      string cache = CreateAndGetCacheName(artistMbid, "Artist");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        string json = File.ReadAllText(cache);
        return JsonConvert.DeserializeObject<ArtistThumbs>(json);
      }

      string url = GetUrl(URL_GETMUSICARTIST, artistMbid);
      return _downloader.Download<ArtistThumbs>(url, cache);
    }

    public AlbumDetails GetAlbumThumbs(string albumMbid)
    {
      string cache = CreateAndGetCacheName(albumMbid, "Album");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        string json = File.ReadAllText(cache);
        return JsonConvert.DeserializeObject<AlbumDetails>(json);
      }

      string url = GetUrl(URL_GETMUSICALBUM, albumMbid);
      return _downloader.Download<AlbumDetails>(url, cache);
    }

    public LabelThumbs GetLabelThumbs(string labelMbid)
    {
      string cache = CreateAndGetCacheName(labelMbid, "Label");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        string json = File.ReadAllText(cache);
        return JsonConvert.DeserializeObject<LabelThumbs>(json);
      }

      string url = GetUrl(URL_GETMUSICLABEL, labelMbid);
      return _downloader.Download<LabelThumbs>(url, cache);
    }

    public MovieThumbs GetMovieThumbs(string imDbIdOrtmDbId)
    {
      string cache = CreateAndGetCacheName(imDbIdOrtmDbId, "Movie");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        string json = File.ReadAllText(cache);
        return JsonConvert.DeserializeObject<MovieThumbs>(json);
      }

      string url = GetUrl(URL_GETMOVIE, imDbIdOrtmDbId);
      return _downloader.Download<MovieThumbs>(url, cache);
    }

    public TVThumbs GetSeriesThumbs(string ttvdbid)
    {
      string cache = CreateAndGetCacheName(ttvdbid, "Series");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        string json = File.ReadAllText(cache);
        return JsonConvert.DeserializeObject<TVThumbs>(json);
      }

      string url = GetUrl(URL_GETSERIES, ttvdbid);
      return _downloader.Download<TVThumbs>(url, cache);
    }

    /// <summary>
    /// Downloads images in "original" size and saves them to cache.
    /// </summary>
    /// <param name="image">Image to download</param>
    /// <param name="category">Image category (Poster, Cover, Backdrop...)</param>
    /// <returns><c>true</c> if successful</returns>
    public bool DownloadImage(string id, Thumb image, string category)
    {
      string cacheFileName = CreateAndGetCacheName(id, image, category);
      if (string.IsNullOrEmpty(cacheFileName))
        return false;

      string sourceUri = image.Url;
      _downloader.DownloadFile(sourceUri, cacheFileName);
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
      string replacedUrl = string.Format(urlBase, args);
      return string.Format("{0}?api_key={1}", replacedUrl, _apiKey);
    }
    /// <summary>
    /// Creates a local file name for loading and saving <see cref="MovieImage"/>s.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="category"></param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName(string id, Thumb image, string category)
    {
      try
      {
        string folder = Path.Combine(_cachePath, string.Format(@"{0}\{1}", id, category));
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        return Path.Combine(folder, image.Url.Substring(image.Url.LastIndexOf('/') + 1));
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
    protected string CreateAndGetCacheName<TE>(TE movieId, string prefix)
    {
      try
      {
        string folder = Path.Combine(_cachePath, movieId.ToString());
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

    protected string ValidateFolderName(string folderName)
    {
      return Path.GetInvalidPathChars().Aggregate(folderName, (current, c) => current.Replace(c, '_'));
    }

    protected static ILogger Logger
    {
      get
      {
        return ServiceRegistration.Get<ILogger>();
      }
    }

    #endregion
  }
}
