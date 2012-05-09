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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner
{
  /// <summary>
  /// Newest addition to the graphical section. Like the name says it has Poster
  /// format (680px x 1000px) and is not smaller than 500 kb
  /// 
  /// More information at http://thetvdb.com/wiki/index.php/Posters
  /// </summary>
  [Serializable]
  public class TvdbPosterBanner : TvdbBannerWithThumb
  {
    #region private fields

    #endregion

    /// <summary>
    /// TvdbPosterBanner constructor
    /// </summary>
    /// <param name="id">Id of fanart banner</param>
    /// <param name="lang">Language for this banner</param>
    /// <param name="path">Path of image for this banner</param>
    public TvdbPosterBanner(int id, String path, TvdbLanguage lang)
      : this()
    {
      Id = id;
      BannerPath = path;
      Language = lang;
    }
    /// <summary>
    /// TvdbPosterBanner constructor
    /// </summary>
    public TvdbPosterBanner()
    {

    }

    /// <summary>
    /// Resolution of the Poster banner
    /// </summary>
    public Point Resolution { get; set; }
  }
}
