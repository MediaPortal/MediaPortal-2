using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Drawing;
using MovieDbLib.Cache;
using System.IO;
using MovieDb;

namespace MovieDbLib.Data.Banner
{
  public class BannerSize
  {
    #region private/protected fields
    private String m_bannerPath;
    private Image m_banner;
    private bool m_isLoaded;
    private System.Object m_bannerLoadingLock = new System.Object();
    private DateTime m_lastUpdated;
    private bool m_bannerLoading = false;
    private ICacheProvider m_cacheProvider;
    private int m_objectId;
    private string m_bannerId;
    private MovieDbBanner.BannerTypes m_bannerType;
    private MovieDbBanner.BannerSizes m_bannerSize;
    private String m_cacheNamePrefix;
    #endregion

    public BannerSize(ICacheProvider _cacheProvider, int _objectId, string _bannerId, MovieDbBanner.BannerTypes _type, MovieDbBanner.BannerSizes _size, String _bannerPath)
    {
      m_cacheProvider = _cacheProvider;
      m_objectId = _objectId;
      m_bannerId = _bannerId;
      m_bannerType = _type;
      m_bannerSize = _size;
      m_bannerPath = _bannerPath;
    }

    public MovieDbBanner.BannerSizes Size
    {
      get { return m_bannerSize; }
      set { m_bannerSize = value; }
    }

    public MovieDbBanner.BannerTypes Type
    {
      get { return m_bannerType; }
      set { m_bannerType = value; }
    }

    public string BannerId
    {
      get { return m_bannerId; }
      set { m_bannerId = value; }
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
            img = m_cacheProvider.LoadImageFromCache(m_objectId, m_bannerId, m_bannerType, m_bannerSize);
          }

          if (img == null)
          {//couldn't load image from cache -> load it from http://TheMovieDb.org
            img = LoadImage(m_bannerPath);

            if (img != null && m_cacheProvider != null && m_cacheProvider.Initialised)
            {//store the image to cache
              m_cacheProvider.SaveToCache(img, m_objectId, m_bannerId, m_bannerType, m_bannerSize);
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
              m_cacheProvider.RemoveImageFromCache(m_objectId, cacheName);
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
      return (_thumb ? "thumb_" : "img_") + _path;
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
      set 
      { 
        m_cacheProvider = value; 
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
