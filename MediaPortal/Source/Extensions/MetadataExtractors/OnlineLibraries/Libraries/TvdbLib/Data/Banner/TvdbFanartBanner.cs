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
using System.Drawing;
using System.Net;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner
{
  /// <summary>
  /// Fan Art is high quality artwork that is displayed in the background of 
  /// HTPC menus. Since fan art is displayed behind other content in most cases, 
  /// we place more restrictions on the formatting of the image. 
  /// 
  /// The resolution is either 1920x1080 or 1280x720...
  /// 
  /// More information: http://thetvdb.com/wiki/index.php/Fan_Art
  /// </summary>
  [Serializable]
  public class TvdbFanartBanner : TvdbBannerWithThumb
  {
    #region private fields

    private readonly object _vignetteLoadingLock = new object();

    #endregion

    /// <summary>
    /// TvdbFanartBanner constructor
    /// </summary>
    public TvdbFanartBanner()
    {

    }

    /// <summary>
    /// TvdbFanartBanner constructor
    /// </summary>
    /// <param name="id">Id of fanart banner</param>
    /// <param name="lang">Language for this banner</param>
    /// <param name="path">Path of image for this banner</param>
    public TvdbFanartBanner(int id, String path, TvdbLanguage lang)
    {
      Id = id;
      BannerPath = path;
      Language = lang;
    }

    /// <summary>
    /// Is the vignette image already loaded
    /// </summary>
    public bool IsVignetteLoaded { get; private set; }

    /// <summary>
    /// Is the vignette currently beeing loaded
    /// </summary>
    public bool VignetteLoading { get; set; }

    /// <summary>
    /// Vignette Image
    /// </summary>
    public Image VignetteImage { get; set; }

    /// <summary>
    /// These are the colors selected by the artist that match the image. The format is 3 colors separated by a pipe "|". This field has leading and trailing pipes. Each color is comma separated RGB, with each color portion being an integer from 1 to 255. So the format looks like |r,g,b|r,g,b|r,g,b|. The first color is the light accent color. The second color is the dark accent color. The third color is the neutral mid-tone color. 
    /// </summary>
    public List<Color> Colors { get; set; }

    /// <summary>
    /// Path to the fanart vignette
    /// </summary>
    public string VignettePath { get; set; }

    /// <summary>
    /// Does the image contain the series name
    /// </summary>
    public bool ContainsSeriesName { get; set; }

    /// <summary>
    /// Color 3 (see Colors property)
    /// </summary>
    public Color Color3 { get; set; }

    /// <summary>
    /// Color 2 (see Colors property)
    /// </summary>
    public Color Color2 { get; set; }

    /// <summary>
    /// Color 1 (see Colors property)
    /// </summary>
    public Color Color1 { get; set; }

    /// <summary>
    /// Resolution of the fanart
    /// </summary>
    public Point Resolution { get; set; }

    /// <summary>
    /// Load the vignette from tvdb
    /// </summary>
    /// <returns>True if successful, false otherwise</returns>
    public bool LoadVignette()
    {
      return LoadVignette(false);
    }

    /// <summary>
    /// Load the vignette from tvdb
    /// </summary>
    /// <returns>True if successful, false otherwise</returns>
    public bool LoadVignette(bool replaceOld)
    {
      bool wasLoaded = IsVignetteLoaded;//is the banner already loaded at this point
      lock (_vignetteLoadingLock)
      {//if another thread is already loading THIS banner, the lock will block this thread until the other thread
        //has finished loading
        if (!wasLoaded && !replaceOld && IsVignetteLoaded)
        {////the banner has already been loaded from a different thread and we don't want to replace it
          return false;
        }
        VignetteLoading = true;
        try
        {
          Image img = null;
          String cacheName = CreateCacheName(Id, VignettePath);
          if (CacheProvider != null && CacheProvider.Initialised)
          {//try to load the image from cache first
            img = CacheProvider.LoadImageFromCache(SeriesId, CachePath, cacheName);
          }

          if (img == null)
          {
            img = LoadImage(TvdbLinkCreator.CreateBannerLink(VignettePath));

            if (img != null && CacheProvider != null && CacheProvider.Initialised)
            {//store the image to cache
              CacheProvider.SaveToCache(img, SeriesId, CachePath, cacheName);
            }
          }

          if (img != null)
          {
            VignetteImage = img;
            IsVignetteLoaded = true;
            VignetteLoading = false;
            return true;
          }
        }
        catch (WebException ex)
        {
          Log.Error("Couldn't load banner thumb" + VignettePath, ex);
        }
        IsVignetteLoaded = false;
        VignetteLoading = false;
        return false;
      }
    }

    /// <summary>
    /// Load vignette with given image
    /// </summary>
    /// <param name="img">Image object that should be used for this banner</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool LoadVignette(Image img)
    {
      if (img != null)
      {
        VignetteImage = img;
        IsVignetteLoaded = true;
        return true;
      }
      IsVignetteLoaded = false;
      return false;
    }

    /// <summary>
    /// Unloads the image and saves it to cache
    /// </summary>
    /// <returns>true if successful, false otherwise</returns>
    public bool UnloadVignette()
    {
      return UnloadVignette(true);
    }

    /// <summary>
    /// Unloads the image
    /// </summary>
    /// <param name="saveToCache">should the image kept in cache</param>
    /// <returns>true if successful, false otherwise</returns>
    public bool UnloadVignette(bool saveToCache)
    {
      if (IsVignetteLoaded)
      {//banner is currently loading
        Log.Warn("Can't remove banner while it's loading");
        return false;
      }
      try
      {
        if (IsVignetteLoaded)
        {
          LoadVignette(null);
        }
        if (!saveToCache)
        {//we don't want the image in cache -> if we already cached it it should be deleted
          String cacheName = CreateCacheName(Id, VignettePath);
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
  }
}
