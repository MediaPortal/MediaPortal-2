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

namespace TvdbLib.Data.Banner
{
  /// <summary>
  /// Season bannners for each season of a series come in poster format (400 x 578) and wide format(758 x 140)
  /// - Wide format: http://thetvdb.com/wiki/index.php/Wide_Season_Banners
  /// - Poster format: http://thetvdb.com/wiki/index.php/Season_Banners
  /// </summary>
  [Serializable]
  public class TvdbSeasonBanner: TvdbBannerWithThumb
  {
    /// <summary>
    /// Type of the season banner
    /// </summary>
    public enum Type  { 
      /// <summary>
      /// Season banner (poster format)
      /// </summary>
      season = 0, 
      /// <summary>
      /// Wide season banner (banner format)
      /// </summary>
      seasonwide = 1 , 
      /// <summary>
      /// no format specified
      /// </summary>
      none = 2};

    #region private fields
    private Type m_bannerType;
    private int m_season;
    #endregion

    /// <summary>
    /// Season of the banner
    /// </summary>
    public int Season
    {
      get { return m_season; }
      set { m_season = value; }
    }

    /// <summary>
    /// Type of the banner
    /// </summary>
    public Type BannerType
    {
      get { return m_bannerType; }
      set { m_bannerType = value; }
    }
  }
}
