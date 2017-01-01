/*
 *   TvdbLib: A library to retrieve information and media from http://thetvdb.com
 * 
 *   Copyright (C) 2008  Benjamin Gmeiner
 * 
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

using System;
using System.Drawing;
using System.Net;
using System.IO;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Cache;
using System.Threading;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner
{
  /// <summary>
  /// Tvdb Banners are the graphical element of tvdb. There are different types of banners which are
  /// representet by sub-classes in this library. These subclasses are:
  /// <list type="bullet">
  /// <item>
  ///   <term>TvdbEpisodeBanner</term>
  ///   <description>Each episode may contain a small image that should be an non-spoiler action shot from the episode (http://thetvdb.com/wiki/index.php/Episode_Images)</description>
  /// </item>                 
  /// <item>
  ///   <term>TvdbFanartBanner</term>
  ///   <description>Fan Art is high quality artwork that is displayed in the background of HTPC menus (http://thetvdb.com/wiki/index.php/Fan_Art)</description>
  /// </item>                    
  /// <item>
  ///   <term>TvdbSeasonBanner</term>
  ///   <description>Banner for each Season of a series, dvd-style (400 x 578) or banner style (758 x 140) (http://thetvdb.com/wiki/index.php/Wide_Season_Banners)</description>
  /// </item>                    
  /// <item>
  ///   <term>TvdbSeriesBanner</term>
  ///   <description>Wide banner for each series (758 x 140), comes in graphical, text or blank style. For further information see http://thetvdb.com/wiki/index.php/Series_Banners</description>
  /// </item>                    
  /// <item>
  ///   <term>TvdbPosterBanner</term>
  ///   <description>Newest addition to the tvdb graphical section (680px x 1000px) and not smaller than 500k (http://thetvdb.com/wiki/index.php/Posters)</description>
  /// </item>                    
  /// </list>
  /// </summary>
  [Serializable]
  public class TvdbBanner
  {
    #region private/protected fields

    private readonly object _bannerLoadingLock = new object();
    private const int _bannerLoadTimeout = 2000;

    public TvdbBanner ()
    {
      BannerLoading = false;
    }

    #endregion

    /// <summary>
    /// Used to load/save images persistent if we're using a cache provider 
    /// (should keep memory usage much lower)
    /// 
    /// on the other hand we have a back-ref to tvdb (from a data class), which sucks
    /// 
    /// todo: think of a better way to handle this
    /// </summary>
    public ICacheProvider CacheProvider { get; set; }

    /// <summary>
    /// Language of the banner
    /// </summary>
    public TvdbLanguage Language { get; set; }

    /// <summary>
    /// Id of the banner
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Image data of the banner
    /// </summary>
    public Image BannerImage { get; set; }

    /// <summary>
    /// True if the image data has been already loaded, false otherwise
    /// </summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// Is the banner currently beeing loaded
    /// </summary>
    public bool BannerLoading { get; set; }

    /// <summary>
    /// Path to the location on the tvdb server where the image is located
    /// </summary>
    public string BannerPath { get; set; }

    /// <summary>
    /// Path to the cache folder
    /// </summary>
    public string CachePath { get; set; }

    /// <summary>
    /// When was the banner updated the last time
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Id of the series this banner belongs to
    /// </summary>
    public int SeriesId { get; set; }

    /// <summary>
    /// Loads the actual image data of the banner
    /// </summary>
    /// <returns>true if the banner could be loaded successfully, false otherwise</returns>
    public bool LoadBanner()
    {
      return LoadBanner(false);
    }

    /// <summary>
    /// Loads the actual image data of the banner
    /// </summary>
    /// <param name="replaceOld">If true will replace an old image (if one exists already)</param>
    /// <returns>true if the banner could be loaded successfully, false otherwise</returns>
    public bool LoadBanner(bool replaceOld)
    {
      bool wasLoaded = IsLoaded;//is the banner already loaded at this point
      lock (_bannerLoadingLock)
      {//if another thread is already loading THIS banner, the lock will block this thread until the other thread
        //has finished loading
        if (!wasLoaded && !replaceOld && IsLoaded)
        {////the banner has already been loaded from a different thread and we don't want to replace it
          return false;
        }

        BannerLoading = true;
        if (BannerPath.Equals("")) return false;
        try
        {
          Image img = null;
          String cacheName = CreateCacheName(Id, BannerPath);
          if (CacheProvider != null && CacheProvider.Initialised)
          {//try to load the image from cache first
            img = CacheProvider.LoadImageFromCache(SeriesId, CachePath, cacheName);
          }

          if (img == null)
          {//couldn't load image from cache -> load it from http://thetvdb.com
            img = LoadImage(TvdbLinkCreator.CreateBannerLink(BannerPath));

            if (img != null && CacheProvider != null && CacheProvider.Initialised)
            {//store the image to cache
              CacheProvider.SaveToCache(img, SeriesId, CachePath, cacheName);
            }
          }

          if (img != null)
          {//image was successfully loaded
            BannerImage = img;
            IsLoaded = true;
            BannerLoading = false;
            return true;
          }
        }
        catch (WebException ex)
        {
          Log.Error("Couldn't load banner " + BannerPath, ex);
        }
        IsLoaded = false;
        BannerLoading = false;
        return false;
      }
    }

    /// <summary>
    /// Unloads the image and saves it to cache
    /// </summary>
    /// <returns>true if successful, false otherwise</returns>
    public bool UnloadBanner()
    {
      return UnloadBanner(true);
    }

    /// <summary>
    /// Unloads the image
    /// </summary>
    /// <param name="saveToCache">should the image kept in cache</param>
    /// <returns>true if successful, false otherwise</returns>
    public bool UnloadBanner(bool saveToCache)
    {
      if (BannerLoading)
      {
        if (!SpinWait.SpinUntil(BannerIsLoading, _bannerLoadTimeout))
        {
          //banner is currently loading
          Log.Warn("Can't remove banner while it's loading");
          return false;
        }
      }
      try
      {
        if (IsLoaded)
        {
          LoadBanner(null);
        }
        if (!saveToCache)
        {//we don't want the image in cache -> if we already cached it it should be deleted
          String cacheName = CreateCacheName(Id, BannerPath);
          if (CacheProvider != null && CacheProvider.Initialised)
          {//try to load the image from cache first
            CacheProvider.RemoveImageFromCache(SeriesId, CachePath, cacheName);
          }
        }
      }
      catch (Exception ex)
      {
        Log.Warn("Error while unloading banner", ex);
      }
      return true;
    }

    private bool BannerIsLoading()
    {
      return BannerLoading;
    }

    /// <summary>
    /// Creates the name used to store images in cache
    /// </summary>
    /// <param name="path">Path of the image</param>
    /// <param name="thumb">Is the image a thumbnail</param>
    /// <returns>Name used for caching image</returns>
    protected String CreateCacheName(int id, string bannerPath)
    {
      if (bannerPath.Contains("_cache/"))
        bannerPath = bannerPath.Replace("_cache/", "");
      if (bannerPath.Contains("fanart/original/"))
        bannerPath = bannerPath.Replace("fanart/original/", "");
      else if (bannerPath.Contains("fanart/vignette/"))
        bannerPath = bannerPath.Replace("fanart/vignette/", "");
      bannerPath = bannerPath.Replace('/', '_');
      return "TVDB(" + id + ")_" + bannerPath;
    }

    /// <summary>
    /// Loads the banner with the given image
    /// </summary>
    /// <param name="img">Image object that should be used for this banner</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool LoadBanner(Image img)
    {
      if (img != null)
      {
        BannerImage = img;
        IsLoaded = true;
        return true;
      }
      if (BannerImage != null)
        BannerImage.Dispose();
      BannerImage = null;
      IsLoaded = false;
      return false;
    }

    /// <summary>
    /// Loads the image from the given path
    /// </summary>
    /// <param name="path">Path of image that should be used for this banner</param>
    /// <returns>True if successful, false otherwise</returns>
    protected Image LoadImage(String path)
    {
      try
      {
        WebClient client = new CompressionWebClient();
        byte[] imgData = client.DownloadData(path);

        MemoryStream ms = new MemoryStream(imgData);
        Image img = Image.FromStream(ms, true, true);
        return img;
      }
      catch (Exception ex)
      {
        Log.Error("Error while loading image ", ex);
        return null;
      }
    }
  }
}
