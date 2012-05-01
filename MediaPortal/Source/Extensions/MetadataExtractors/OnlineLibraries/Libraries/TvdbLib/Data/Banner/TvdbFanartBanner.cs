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
using System.Threading;
using System.Net;

namespace TvdbLib.Data.Banner
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
    private String m_vignettePath;
    private bool m_containsSeriesName;
    private Image m_vignette;
    private Point m_resolution;
    private List<Color> m_colors;
    private bool m_vignetteLoaded;
    private Color m_color1;
    private Color m_color2;
    private Color m_color3;
    private bool m_vignetteLoading;
    private System.Object m_vignetteLoadingLock = new System.Object();
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
    /// <param name="_id">Id of fanart banner</param>
    /// <param name="_lang">Language for this banner</param>
    /// <param name="_path">Path of image for this banner</param>
    public TvdbFanartBanner(int _id, String _path, TvdbLanguage _lang)
    {
      this.Id = _id;
      this.BannerPath = _path;
      this.Language = _lang;
    }

    /// <summary>
    /// Is the vignette image already loaded
    /// </summary>
    public bool IsVignetteLoaded
    {
      get { return m_vignetteLoaded; }
    }

    /// <summary>
    /// Is the vignette currently beeing loaded
    /// </summary>
    public bool VignetteLoading
    {
      get { return m_vignetteLoading; }
      set { m_vignetteLoading = value; }
    }

    /// <summary>
    /// Vignette Image
    /// </summary>
    public Image VignetteImage
    {
      get { return m_vignette; }
      set { m_vignette = value; }
    }

    /// <summary>
    /// These are the colors selected by the artist that match the image. The format is 3 colors separated by a pipe "|". This field has leading and trailing pipes. Each color is comma separated RGB, with each color portion being an integer from 1 to 255. So the format looks like |r,g,b|r,g,b|r,g,b|. The first color is the light accent color. The second color is the dark accent color. The third color is the neutral mid-tone color. 
    /// </summary>
    public List<Color> Colors
    {
      get { return m_colors; }
      set { m_colors = value; }
    }

    /// <summary>
    /// Path to the fanart vignette
    /// </summary>
    public String VignettePath
    {
      get { return m_vignettePath; }
      set { m_vignettePath = value; }
    }

    /// <summary>
    /// Does the image contain the series name
    /// </summary>
    public bool ContainsSeriesName
    {
      get { return m_containsSeriesName; }
      set { m_containsSeriesName = value; }
    }

    /// <summary>
    /// Color 3 (see Colors property)
    /// </summary>
    public Color Color3
    {
      get { return m_color3; }
      set { m_color3 = value; }
    }

    /// <summary>
    /// Color 2 (see Colors property)
    /// </summary>
    public Color Color2
    {
      get { return m_color2; }
      set { m_color2 = value; }
    }

    /// <summary>
    /// Color 1 (see Colors property)
    /// </summary>
    public Color Color1
    {
      get { return m_color1; }
      set { m_color1 = value; }
    }

    /// <summary>
    /// Resolution of the fanart
    /// </summary>
    public Point Resolution
    {
      get { return m_resolution; }
      set { m_resolution = value; }
    }

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
    public bool LoadVignette(bool _replaceOld)
    {
      bool wasLoaded = m_vignetteLoaded;//is the banner already loaded at this point
      lock (m_vignetteLoadingLock)
      {//if another thread is already loading THIS banner, the lock will block this thread until the other thread
        //has finished loading
        if (!wasLoaded && !_replaceOld && m_vignetteLoaded)
        {////the banner has already been loaded from a different thread and we don't want to replace it
          return false;
        }
        m_vignetteLoading = true;
        try
        {
          Image img = null;
          String cacheName = CreateCacheName(m_vignettePath, false);
          if (this.CacheProvider != null && this.CacheProvider.Initialised)
          {//try to load the image from cache first
            img = this.CacheProvider.LoadImageFromCache(this.SeriesId, cacheName);
          }

          if (img == null)
          {
            img = LoadImage(TvdbLinkCreator.CreateBannerLink(m_vignettePath));

            if (img != null && this.CacheProvider != null && this.CacheProvider.Initialised)
            {//store the image to cache
              this.CacheProvider.SaveToCache(img, this.SeriesId, cacheName);
            }
          }

          if (img != null)
          {
            m_vignette = img;
            m_vignetteLoaded = true;
            m_vignetteLoading = false;
            return true;
          }
        }
        catch (WebException ex)
        {
          Log.Error("Couldn't load banner thumb" + m_vignettePath, ex);
        }
        m_vignetteLoaded = false;
        m_vignetteLoading = false;
        return false;
      }
    }

    /// <summary>
    /// Load vignette with given image
    /// </summary>
    /// <param name="_img">Image object that should be used for this banner</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool LoadVignette(Image _img)
    {
      if (_img != null)
      {
        m_vignette = _img;
        m_vignetteLoaded = true;
        return true;
      }
      else
      {
        m_vignetteLoaded = false;
        return false;
      }
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
    /// <param name="_saveToCache">should the image kept in cache</param>
    /// <returns>true if successful, false otherwise</returns>
    public bool UnloadVignette(bool _saveToCache)
    {
      if (m_vignetteLoaded)
      {//banner is currently loading
        Log.Warn("Can't remove banner while it's loading");
        return false;
      }
      else
      {
        try
        {
          if (m_vignetteLoaded)
          {
            LoadVignette(null);
          }
          if (!_saveToCache)
          {//we don't want the image in cache -> if we already cached it it should be deleted
            String cacheName = CreateCacheName(m_vignettePath, true);
            if (this.CacheProvider != null && this.CacheProvider.Initialised)
            {//try to load the image from cache first
              this.CacheProvider.RemoveImageFromCache(this.SeriesId, cacheName);
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
}
