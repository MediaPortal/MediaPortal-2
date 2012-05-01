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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net;
using System.IO;
using System.Threading;
using TvdbLib.Cache;

namespace TvdbLib.Data
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
  ///   <description>Banner for each season of a series, dvd-style (400 x 578) or banner style (758 x 140) (http://thetvdb.com/wiki/index.php/Wide_Season_Banners)</description>
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
    private String m_bannerPath;
    private Image m_banner;
    private bool m_isLoaded;
    private int m_id;
    private TvdbLanguage m_language;
    private bool m_bannerLoading = false;
    private System.Object m_bannerLoadingLock = new System.Object();
    private DateTime m_lastUpdated;
    private int m_seriesId;
    private ICacheProvider m_cacheProvider;
    #endregion

    /// <summary>
    /// Used to load/save images persistent if we're using a cache provider 
    /// (should keep memory usage much lower)
    /// 
    /// on the other hand we have a back-ref to tvdb (from a data class), which sucks
    /// 
    /// todo: think of a better way to handle this
    /// </summary>
    public ICacheProvider CacheProvider
    {
      get { return m_cacheProvider; }
      set { m_cacheProvider = value; }
    }

    /// <summary>
    /// Language of the banner
    /// </summary>
    public TvdbLanguage Language
    {
      get { return m_language; }
      set { m_language = value; }
    }

    /// <summary>
    /// Id of the banner
    /// </summary>
    public int Id
    {
      get { return m_id; }
      set { m_id = value; }
    }

    /// <summary>
    /// Image data of the banner
    /// </summary>
    public Image BannerImage
    {
      get { return m_banner; }
      set { m_banner = value; }
    }

    /// <summary>
    /// True if the image data has been already loaded, false otherwise
    /// </summary>
    public bool IsLoaded
    {
      get { return m_isLoaded; }
    }

    /// <summary>
    /// Is the banner currently beeing loaded
    /// </summary>
    public bool BannerLoading
    {
      get { return m_bannerLoading; }
      set { m_bannerLoading = value; }
    }

    /// <summary>
    /// Path to the location on the tvdb server where the image is located
    /// </summary>
    public String BannerPath
    {
      get { return m_bannerPath; }
      set { m_bannerPath = value; }
    }

    /// <summary>
    /// When was the banner updated the last time
    /// </summary>
    public DateTime LastUpdated
    {
      get { return m_lastUpdated; }
      set { m_lastUpdated = value; }
    }

    /// <summary>
    /// Id of the series this banner belongs to
    /// </summary>
    public int SeriesId
    {
      get { return m_seriesId; }
      set { m_seriesId = value; }
    }

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
    /// <param name="_replaceOld">If true will replace an old image (if one exists already)</param>
    /// <returns>true if the banner could be loaded successfully, false otherwise</returns>
    public bool LoadBanner(bool _replaceOld)
    {
      bool wasLoaded = m_isLoaded;//is the banner already loaded at this point
      lock (m_bannerLoadingLock)
      {//if another thread is already loading THIS banner, the lock will block this thread until the other thread
        //has finished loading
        if (!wasLoaded && !_replaceOld && m_isLoaded)
        {////the banner has already been loaded from a different thread and we don't want to replace it
          return false;
        }

        m_bannerLoading = true;
        if (m_bannerPath.Equals("")) return false;
        try
        {
          Image img = null;
          String cacheName = CreateCacheName(m_bannerPath, false);
          if (m_cacheProvider != null && m_cacheProvider.Initialised)
          {//try to load the image from cache first
            img = m_cacheProvider.LoadImageFromCache(m_seriesId, cacheName);
          }

          if (img == null)
          {//couldn't load image from cache -> load it from http://thetvdb.com
            img = LoadImage(TvdbLinkCreator.CreateBannerLink(m_bannerPath));

            if (img != null && m_cacheProvider != null && m_cacheProvider.Initialised)
            {//store the image to cache
              m_cacheProvider.SaveToCache(img, m_seriesId, cacheName);
            }
          }

          if (img != null)
          {//image was successfully loaded
            m_banner = img;
            m_isLoaded = true;
            m_bannerLoading = false;
            return true;
          }
        }
        catch (WebException ex)
        {
          Log.Error("Couldn't load banner " + m_bannerPath, ex);
        }
        m_isLoaded = false;
        m_bannerLoading = false;
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
    /// <param name="_saveToCache">should the image kept in cache</param>
    /// <returns>true if successful, false otherwise</returns>
    public bool UnloadBanner(bool _saveToCache)
    {
      if (m_bannerLoading)
      {//banner is currently loading
        Log.Warn("Can't remove banner while it's loading");
        return false;
      }
      else
      {
        try
        {
          if (m_isLoaded)
          {
            LoadBanner(null);
          }
          if (!_saveToCache)
          {//we don't want the image in cache -> if we already cached it it should be deleted
            String cacheName = CreateCacheName(m_bannerPath, false);
            if (m_cacheProvider != null && m_cacheProvider.Initialised)
            {//try to load the image from cache first
              m_cacheProvider.RemoveImageFromCache(m_seriesId, cacheName);
            }
          }
        }
        catch (Exception ex)
        {
          Log.Warn("Error while unloading banner", ex);
        }
        return true;
      }
    }

    /// <summary>
    /// Creates the name used to store images in cache
    /// </summary>
    /// <param name="_path">Path of the image</param>
    /// <param name="_thumb">Is the image a thumbnail</param>
    /// <returns>Name used for caching image</returns>
    protected String CreateCacheName(String _path, bool _thumb)
    {
      if (_path.Contains("_cache/"))
      {
        _path = _path.Replace("_cache/", "");
      }
      if (_path.Contains("fanart/original/"))
      {
        _path = _path.Replace("fanart/original/", "fan-");
      }
      else if (_path.Contains("fanart/vignette/"))
      {
        _path = _path.Replace("fanart/vignette/", "fan-vig-");
      }
      _path = _path.Replace('/', '_');
      return (_thumb ? "thumb_": "img_") + _path;
    }

    /// <summary>
    /// Loads the banner with the given image
    /// </summary>
    /// <param name="_img">Image object that should be used for this banner</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool LoadBanner(Image _img)
    {
      if (_img != null)
      {
        m_banner = _img;
        m_isLoaded = true;
        return true;
      }
      else
      {
        m_banner = null;
        m_isLoaded = false;
        return false;
      }
    }

    /// <summary>
    /// Loads the image from the given path
    /// </summary>
    /// <param name="_path">Path of image that should be used for this banner</param>
    /// <returns>True if successful, false otherwise</returns>
    protected Image LoadImage(String _path)
    {
      try
      {
        WebClient client = new WebClient();
        byte[] imgData = client.DownloadData(_path);
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
