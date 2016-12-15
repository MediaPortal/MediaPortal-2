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

    private const string URL_API_BASE = "webservice.fanart.tv/v3/";
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
    private readonly bool _useHttps;

    #endregion

    #region Constructor

    public FanArtTVApiV3(string apiKey, string cachePath, bool useHttps)
    {
      _apiKey = apiKey;
      _cachePath = cachePath;
      _useHttps = useHttps;
      _downloader = new Downloader { EnableCompression = true };
      _downloader.Headers["Accept"] = "application/json";
    }

    #endregion

    #region Public members

    public FanArtArtistThumbs GetArtistThumbs(string artistMbid)
    {
      string cache = CreateAndGetCacheName(artistMbid, "Artist");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<FanArtArtistThumbs>(cache);
      }

      string url = GetUrl(URL_GETMUSICARTIST, artistMbid);
      return _downloader.Download<FanArtArtistThumbs>(url, cache);
    }

    public FanArtAlbumDetails GetAlbumThumbs(string albumMbid)
    {
      string cache = CreateAndGetCacheName(albumMbid, "Album");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<FanArtAlbumDetails>(cache);
      }

      string url = GetUrl(URL_GETMUSICALBUM, albumMbid);
      return _downloader.Download<FanArtAlbumDetails>(url, cache);
    }

    public FanArtLabelThumbs GetLabelThumbs(string labelMbid)
    {
      string cache = CreateAndGetCacheName(labelMbid, "Label");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<FanArtLabelThumbs>(cache);
      }

      string url = GetUrl(URL_GETMUSICLABEL, labelMbid);
      return _downloader.Download<FanArtLabelThumbs>(url, cache);
    }

    public FanArtMovieThumbs GetMovieThumbs(string imDbIdOrtmDbId)
    {
      string cache = CreateAndGetCacheName(imDbIdOrtmDbId, "Movie");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<FanArtMovieThumbs>(cache);
      }

      string url = GetUrl(URL_GETMOVIE, imDbIdOrtmDbId);
      return _downloader.Download<FanArtMovieThumbs>(url, cache);
    }

    public FanArtTVThumbs GetSeriesThumbs(string tvdbid)
    {
      string cache = CreateAndGetCacheName(tvdbid, "Series");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        return _downloader.ReadCache<FanArtTVThumbs>(cache);
      }

      string url = GetUrl(URL_GETSERIES, tvdbid);
      return _downloader.Download<FanArtTVThumbs>(url, cache);
    }

    /// <summary>
    /// Downloads images in "original" size and saves them to cache.
    /// </summary>
    /// <param name="image">Image to download</param>
    /// <param name="folderPath">The folder to store the image</param>
    /// <returns><c>true</c> if successful</returns>
    public bool DownloadImage(string id, FanArtThumb image, string folderPath)
    {
      string cacheFileName = CreateAndGetCacheName(id, image, folderPath);
      if (string.IsNullOrEmpty(cacheFileName))
        return false;

      string sourceUri = image.Url;
      return _downloader.DownloadFile(sourceUri, cacheFileName);
    }

    public byte[] GetImage(string id, FanArtThumb image, string folderPath)
    {
      string cacheFileName = CreateAndGetCacheName(id, image, folderPath);
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
      string replacedUrl = string.Format(urlBase, args);
      if(_useHttps)
        return string.Format("https://{0}?api_key={1}", replacedUrl, _apiKey);
      else
        return string.Format("http://{0}?api_key={1}", replacedUrl, _apiKey);
    }

    /// <summary>
    /// Creates a local file name for loading and saving <see cref="MovieImage"/>s.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="folderPath"></param>
    /// <returns>Cache file name or <c>null</c> if directory could not be created</returns>
    protected string CreateAndGetCacheName(string id, FanArtThumb image, string folderPath)
    {
      try
      {
        string prefix = string.Format(@"FATV({0})_", id);
        if (!Directory.Exists(folderPath))
          Directory.CreateDirectory(folderPath);
        return Path.Combine(folderPath, prefix + image.Url.Substring(image.Url.LastIndexOf('/') + 1));
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
