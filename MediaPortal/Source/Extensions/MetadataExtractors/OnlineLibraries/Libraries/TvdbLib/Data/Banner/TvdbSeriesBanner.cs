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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner
{
  /// <summary>
  /// Graphical representation of a series, tpyes are text, graphical or blank
  /// - Graphical Banners are defined as having a graphical/logo version of the series name 
  /// - Text Banners generally use Arial Bold font, 27pt as the text 
  /// - The main requirement for blank banners is they should be blank on the left side of the banner as 
  ///   that is where the auto-generated text will be placed
  ///   
  /// More information on http://thetvdb.com/wiki/index.php/Series_Banners
  /// </summary>
  [Serializable]
  public class TvdbSeriesBanner: TvdbBannerWithThumb
  {
    #region private fields

    #endregion

    /// <summary>
    /// Type of the series banner
    /// </summary>
    public enum Type { 
      /// <summary>
      /// Banners contains a text of the seriesname
      /// </summary>
      Text, 
      /// <summary>
      /// Banner containing a graphical representation of the seriesname
      /// </summary>
      Graphical, 
      /// <summary>
      /// Banner containing a free space on the left side to place your own series description
      /// </summary>
      Blank, 
      /// <summary>
      /// Nothing specified
      /// </summary>
      None };

    /// <summary>
    /// TvdbSeriesBanner constructor
    /// </summary>
    public TvdbSeriesBanner()
    {

    }

    /// <summary>
    /// TvdbSeriesBanner constructor
    /// </summary>
    /// <param name="id">Id of banner</param>
    /// <param name="path">Path of banner image</param>
    /// <param name="lang">Language of this banner</param>
    /// <param name="type">Banner type (text, graphical, blank, None)</param>
    public TvdbSeriesBanner(int id, String path, TvdbLanguage lang, Type type)
    {
      BannerPath = path;
      Language = lang;
      Id = id;
      BannerType = type;
    }

    /// <summary>
    /// Banner type of the series banner
    /// </summary>
    public Type BannerType { get; set; }
  }
}
