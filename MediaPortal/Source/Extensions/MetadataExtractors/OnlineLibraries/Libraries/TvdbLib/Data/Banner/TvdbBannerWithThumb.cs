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
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner
{
  /// <summary>
  /// This class extends the regular banner class with the ability to retrieve thumbnails of the actual images.
  /// 
  /// These thumbnails are at the moment availabe for all banner types except actors
  /// </summary>
  [Serializable]
  public class TvdbBannerWithThumb : TvdbBanner
  {
    #region private fields

    private readonly object _thumbLoadingLock = new object();
    #endregion

    /// <summary>
    /// Is the thumbnail currently beeing loaded
    /// </summary>
    public bool ThumbLoading { get; set; }

    /// <summary>
    /// Path to the fanart thumbnail
    /// </summary>
    public string ThumbPath { get; set; }

    /// <summary>
    /// Image of the thumbnail
    /// </summary>
    public Image ThumbImage { get; set; }


    /// <summary>
    /// Load the thumb from tvdb, if there isn't already a thumb loaded,
    /// (an existing one will NOT be replaced)
    /// </summary>
    /// <see cref="LoadThumb(bool)"/>
    /// <returns>true if the loading completed sccessfully, false otherwise</returns>
    public bool LoadThumb()
    {
      return LoadThumb(false);
    }

    /// <summary>
    /// Load the thumb from tvdb
    /// </summary>
    /// <param name="replaceOld">if true, an existing banner will be replaced, 
    /// if false the banner will only be loaded if there is no existing banner</param>
    /// <returns>true if the loading completed sccessfully, false otherwise</returns>
    public bool LoadThumb(bool replaceOld)
    {
      bool wasLoaded = IsThumbLoaded;//is the banner already loaded at this point
      lock (_thumbLoadingLock)
      {//if another thread is already loading THIS banner, the lock will block this thread until the other thread
        //has finished loading
        if (!wasLoaded && !replaceOld && IsThumbLoaded)
        {////the banner has already been loaded from a different thread and we don't want to replace it
          return false;
        }
        ThumbLoading = true;

        /*
         * every banner (except actors) has a cached thumbnail on tvdb... The path to the thumbnail
         * is only given for fanart banners via the api, however every thumbnail path is "_cache/" + image_path
         * so if no path for the thumbnail is given, it is assumed that there is a thumbnail at that location
         */
        if (ThumbPath == null && (BannerPath != null || BannerPath.Equals("")))
        {
          ThumbPath = String.Concat("_cache/", BannerPath);
        }

        if (ThumbPath != null)
        {
          try
          {
            Image img = null;
            String cacheName = CreateCacheName(Id, ThumbPath);

            if (CacheProvider != null && CacheProvider.Initialised)
            {//try to load the image from cache first
              img = CacheProvider.LoadImageFromCache(SeriesId, CachePath, cacheName);
            }

            if (img == null)
            {
              img = LoadImage(TvdbLinkCreator.CreateBannerLink(ThumbPath));

              if (img != null && CacheProvider != null && CacheProvider.Initialised)
              {//store the image to cache
                CacheProvider.SaveToCache(img, SeriesId, CachePath, cacheName);
              }
            }

            if (img != null)
            {
              ThumbImage = img;
              IsThumbLoaded = true;
              ThumbLoading = false;
              return true;
            }
          }
          catch (WebException ex)
          {
            Log.Error("Couldn't load banner thumb" + ThumbPath, ex);
          }
        }
        IsThumbLoaded = false;
        ThumbLoading = false;
        return false;
      }
    }

    /// <summary>
    /// Load thumbnail with given image
    /// </summary>
    /// <param name="img">the image to be used forthe banner</param>
    /// <returns>true if the loading completed sccessfully, false otherwise</returns>
    public bool LoadThumb(Image img)
    {
      if (img != null)
      {
        ThumbImage = img;
        IsThumbLoaded = true;
        return true;
      }
      IsThumbLoaded = false;
      return false;
    }

    /// <summary>
    /// Unloads the image and saves it to cache
    /// </summary>
    /// <returns>true if successful, false otherwise</returns>
    public bool UnloadThumb()
    {
      return UnloadThumb(true);
    }

    /// <summary>
    /// Unloads the image
    /// </summary>
    /// <param name="saveToCache">should the image kept in cache</param>
    /// <returns>true if successful, false otherwise</returns>
    public bool UnloadThumb(bool saveToCache)
    {
      if (ThumbLoading)
      {//banner is currently loading
        Log.Warn("Can't remove banner while it's loading");
        return false;
      }
      try
      {
        if (IsThumbLoaded)
          LoadThumb(null);
        if (!saveToCache && ThumbPath != null && !ThumbPath.Equals(""))
        {//we don't want the image in cache -> if we already cached it it should be deleted
          String cacheName = CreateCacheName(Id, ThumbPath);
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

    /// <summary>
    /// Is the Image of the thumb already loaded
    /// </summary>
    public bool IsThumbLoaded { get; private set; }
  }
}
