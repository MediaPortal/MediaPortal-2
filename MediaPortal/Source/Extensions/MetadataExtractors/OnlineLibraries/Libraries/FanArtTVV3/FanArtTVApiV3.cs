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

using System.Collections.Generic;
using System.IO;
using System.Web;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3.Data;
using Newtonsoft.Json;
using MediaPortal.Common.Logging;
using MediaPortal.Common;
using System;
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
    private const string URL_GETSHOW =  URL_API_BASE + "tv/{0}";

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

    public TVThumbs GetShowThumbs(string ttvdbid)
    {
      string cache = CreateAndGetCacheName(ttvdbid, "Show");
      if (!string.IsNullOrEmpty(cache) && File.Exists(cache))
      {
        string json = File.ReadAllText(cache);
        return JsonConvert.DeserializeObject<TVThumbs>(json);
      }

      string url = GetUrl(URL_GETSHOW, ttvdbid);
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

    public bool DownloadArtistBanners(ArtistThumbs artist)
    {
      bool returnVal = false;
      string id = artist.MusicBrainzID;
      try
      {
        foreach (Thumb image in artist.ArtistBanners) if(DownloadImage(id, image, "Banners")) returnVal = true;       
      }
      catch(Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Artist Banner", ex);
      }
      return returnVal;
    }

    public bool DownloadArtistFanArt(ArtistThumbs artist)
    {
      bool returnVal = false;
      string id = artist.MusicBrainzID;
      try
      {
        foreach (Thumb image in artist.ArtistFanart) if (DownloadImage(id, image, "Backdrop")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Artist FanArt", ex);
      }
      return returnVal;
    }

    public bool DownloadArtistThumbs(ArtistThumbs artist)
    {
      bool returnVal = false;
      string id = artist.MusicBrainzID;
      try
      {
        foreach (Thumb image in artist.ArtistThumbnails) if (DownloadImage(id, image, "Thumbs")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Artist Thumb", ex);
      }
      return returnVal;
    }

    public bool DownloadArtistLogos(ArtistThumbs artist)
    {
      bool returnVal = false;
      string id = artist.MusicBrainzID;
      try
      {
        foreach (Thumb image in artist.ArtistLogos) if (DownloadImage(id, image, "Logos")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Artist Logo", ex);
      }
      return returnVal;
    }

    public bool DownloadAlbumCovers(AlbumDetails albums)
    {
      bool returnVal = false;
      foreach (KeyValuePair<string, AlbumThumbs> album in albums.Albums)
      {
        try
        {
          foreach (Thumb image in album.Value.AlbumCovers) if (DownloadImage(album.Key, image, "Covers")) returnVal = true;
        }
        catch (Exception ex)
        {
          Logger.Error("FanArtTVV3: Error downloading Album Cover", ex);
        }
      }
      return returnVal;
    }

    public bool DownloadAlbumCDArt(AlbumDetails albums)
    {
      bool returnVal = false;
      foreach (KeyValuePair<string, AlbumThumbs> album in albums.Albums)
      {
        try
        {
          foreach (Thumb image in album.Value.CDArts) if (DownloadImage(album.Key, image, "CDArt")) returnVal = true;
        }
        catch (Exception ex)
        {
          Logger.Error("FanArtTVV3: Error downloading Album CDArt", ex);
        }
      }
      return returnVal;
    }

    public bool DownloadLabelLogos(LabelThumbs label)
    {
      bool returnVal = false;
      string id = label.MusicBrainzID;
      try
      {
        foreach (Thumb image in label.LabelLogos) if (DownloadImage(id, image, "Logos")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Label Logo", ex);
      }
      return returnVal;
    }

    public bool DownloadMovieBanners(MovieThumbs movie, string language)
    {
      bool returnVal = false;
      string id = string.IsNullOrEmpty(movie.ImDbID) ? movie.TheMovieDbID : movie.ImDbID;
      try
      {
        foreach (MovieThumb image in movie.MovieBanners) if (DownloadMovieThumb(id, image, language, "Banners")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Movie Banner", ex);
      }
      return returnVal;
    }

    public bool DownloadMovieClearArt(MovieThumbs movie, string language)
    {
      bool returnVal = false;
      string id = string.IsNullOrEmpty(movie.ImDbID) ? movie.TheMovieDbID : movie.ImDbID;
      try
      {
        foreach (MovieThumb image in movie.MovieClearArt) if (DownloadMovieThumb(id, image, language, "ClearArt")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Movie ClearArt", ex);
      }
      return returnVal;
    }

    public bool DownloadMovieFanArt(MovieThumbs movie, string language)
    {
      bool returnVal = false;
      string id = string.IsNullOrEmpty(movie.ImDbID) ? movie.TheMovieDbID : movie.ImDbID;
      try
      {
        foreach (MovieThumb image in movie.MovieFanArt) if (DownloadMovieThumb(id, image, language, "Backdrops")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Movie FanArt", ex);
      }
      return returnVal;
    }

    public bool DownloadMovieCDArt(MovieThumbs movie, string language)
    {
      bool returnVal = false;
      string id = string.IsNullOrEmpty(movie.ImDbID) ? movie.TheMovieDbID : movie.ImDbID;
      try
      {
        foreach (MovieThumb image in movie.MovieCDArt) if (DownloadMovieThumb(id, image, language, "CDArt")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Movie CDArt", ex);
      }
      return returnVal;
    }

    public bool DownloadMoviePosters(MovieThumbs movie, string language)
    {
      bool returnVal = false;
      string id = string.IsNullOrEmpty(movie.ImDbID) ? movie.TheMovieDbID : movie.ImDbID;
      try
      {
        foreach (MovieThumb image in movie.MoviePosters) if (DownloadMovieThumb(id, image, language, "Posters")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Movie Poster", ex);
      }
      return returnVal;
    }

    public bool DownloadMovieLogos(MovieThumbs movie, string language)
    {
      bool returnVal = false;
      string id = string.IsNullOrEmpty(movie.ImDbID) ? movie.TheMovieDbID : movie.ImDbID;
      try
      {
        foreach (MovieThumb image in movie.MovieLogos) if (DownloadMovieThumb(id, image, language, "Logos")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Movie Logo", ex);
      }
      return returnVal;
    }

    public bool DownloadMovieThumbs(MovieThumbs movie, string language)
    {
      bool returnVal = false;
      string id = string.IsNullOrEmpty(movie.ImDbID) ? movie.TheMovieDbID : movie.ImDbID;
      try
      {
        foreach (MovieThumb image in movie.MovieThumbnails) if (DownloadMovieThumb(id, image, language, "Thumbs")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Movie Thumb", ex);
      }
      return returnVal;
    }

    public bool DownloadShowBanners(TVThumbs show, string language, bool includeSeasons)
    {
      bool returnVal = false;
      string id = show.TheTVDbID;
      try
      {
        foreach (SeasonThumb image in show.SeasonBanners) if (DownloadMovieThumb(id, image, language, string.Format(@"Banners\Season {0}", image.Season))) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Season Banner", ex);
      }
      try
      {
        if(includeSeasons)
          foreach (MovieThumb image in show.ShowBanners) if (DownloadMovieThumb(id, image, language, "Banners")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Show Banner", ex);
      }
      return returnVal;
    }

    public bool DownloadShowPosters(TVThumbs show, string language, bool includeSeasons)
    {
      bool returnVal = false;
      string id = show.TheTVDbID;
      try
      {
        if(includeSeasons)
          foreach (SeasonThumb image in show.SeasonPosters) if (DownloadMovieThumb(id, image, language, string.Format(@"Posters\Season {0}", image.Season))) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Season Poster", ex);
      }
      try
      {
        foreach (MovieThumb image in show.ShowPosters) if (DownloadMovieThumb(id, image, language, "Posters")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Show Poster", ex);
      }
      return returnVal;
    }

    public bool DownloadShowThumbs(TVThumbs show, string language, bool includeSeasons)
    {
      bool returnVal = false;
      string id = show.TheTVDbID;
      try
      {
        if(includeSeasons)
          foreach (SeasonThumb image in show.SeasonThumbnails) if (DownloadMovieThumb(id, image, language, string.Format(@"Thumbs\Season {0}", image.Season))) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Season Thumb", ex);
      }
      try
      {
        foreach (MovieThumb image in show.ShowThumbnails) if (DownloadMovieThumb(id, image, language, "Thumbs")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Show Thumb", ex);
      }
      return returnVal;
    }

    public bool DownloadShowClearArt(TVThumbs show, string language)
    {
      bool returnVal = false;
      string id = show.TheTVDbID;
      try
      {
        foreach (MovieThumb image in show.ShowClearArt) if (DownloadMovieThumb(id, image, language, "ClearArt")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Show ClearArt", ex);
      }
      return returnVal;
    }

    public bool DownloadShowFanArt(TVThumbs show, string language)
    {
      bool returnVal = false;
      string id = show.TheTVDbID;
      try
      {
        foreach (MovieThumb image in show.ShowFanArt) if (DownloadMovieThumb(id, image, language, "Backdrops")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Show FanArt", ex);
      }
      return returnVal;
    }

    public bool DownloadShowLogos(TVThumbs show, string language)
    {
      bool returnVal = false;
      string id = show.TheTVDbID;
      try
      {
        foreach (MovieThumb image in show.ShowLogos) if (DownloadMovieThumb(id, image, language, "Logos")) returnVal = true;
      }
      catch (Exception ex)
      {
        Logger.Error("FanArtTVV3: Error downloading Show Logo", ex);
      }
      return returnVal;
    }

    private bool DownloadMovieThumb(string id, MovieThumb image, string language, string category)
    {
      if (string.IsNullOrEmpty(language) || language.Equals(image.Language, StringComparison.InvariantCultureIgnoreCase))
        return DownloadImage(id, image, category);
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
