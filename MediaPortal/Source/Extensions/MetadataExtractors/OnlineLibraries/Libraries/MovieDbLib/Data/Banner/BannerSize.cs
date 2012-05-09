using System;
using System.Net;
using System.Drawing;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Cache;
using System.IO;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Banner
{
  public class BannerSize
  {
    #region private/protected fields

    private readonly Object _bannerLoadingLock = new Object();
    private ICacheProvider _cacheProvider;
    private readonly int _objectId;

    #endregion

    public BannerSize(ICacheProvider cacheProvider, int objectId, string bannerId, MovieDbBanner.BannerTypes type, MovieDbBanner.BannerSizes size, String bannerPath)
    {
      BannerLoading = false;
      _cacheProvider = cacheProvider;
      _objectId = objectId;
      BannerId = bannerId;
      Type = type;
      Size = size;
      BannerPath = bannerPath;
    }

    public MovieDbBanner.BannerSizes Size { get; set; }

    public MovieDbBanner.BannerTypes Type { get; set; }

    public string BannerId { get; set; }

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
    /// When was the banner updated the last time
    /// </summary>
    public DateTime LastUpdated { get; set; }

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
          if (_cacheProvider != null && _cacheProvider.Initialised)
          {//try to load the image from cache first
            img = _cacheProvider.LoadImageFromCache(_objectId, BannerId, Type, Size);
          }

          if (img == null)
          {//couldn't load image from cache -> load it from http://TheMovieDb.org
            img = LoadImage(BannerPath);

            if (img != null && _cacheProvider != null && _cacheProvider.Initialised)
            {//store the image to cache
              _cacheProvider.SaveToCache(img, _objectId, BannerId, Type, Size);
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
        //banner is currently loading
        Log.Warn("Can't remove banner while it's loading");
        return false;
      }
      try
      {
        if (IsLoaded)
        {
          LoadBanner(null);
        }
        if (!saveToCache)
        {//we don't want the image in cache -> if we already cached it it should be deleted
          String cacheName = CreateCacheName(BannerPath, false);
          if (_cacheProvider != null && _cacheProvider.Initialised)
          {//try to load the image from cache first
            _cacheProvider.RemoveImageFromCache(_objectId, cacheName);
          }
        }
      }
      catch (Exception ex)
      {
        Log.Warn("Error while unloading banner", ex);
      }
      return true;
    }

    /// <summary>
    /// Creates the name used to store images in cache
    /// </summary>
    /// <param name="path">Path of the image</param>
    /// <param name="thumb">Is the image a thumbnail</param>
    /// <returns>Name used for caching image</returns>
    protected String CreateCacheName(String path, bool thumb)
    {
      if (path.Contains("_cache/"))
      {
        path = path.Replace("_cache/", "");
      }
      if (path.Contains("fanart/Original/"))
      {
        path = path.Replace("fanart/Original/", "fan-");
      }
      else if (path.Contains("fanart/vignette/"))
      {
        path = path.Replace("fanart/vignette/", "fan-vig-");
      }
      path = path.Replace('/', '_');
      return (thumb ? "thumb_" : "img_") + path;
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
      BannerImage = null;
      IsLoaded = false;
      return false;
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
      get { return _cacheProvider; }
      set 
      { 
        _cacheProvider = value; 
      }
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
        WebClient client = new WebClient();
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
