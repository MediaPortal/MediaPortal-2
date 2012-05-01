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
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data
{
  /// <summary>
  /// Class representing the result of a tvdb name query -> for further information
  /// visit http://thetvdb.com/wiki/index.php/API:GetSeries
  /// </summary>
  public class TvdbSearchResult
  {
    /// <summary>
    /// TvdbSearchResult constructor
    /// </summary>
    public TvdbSearchResult()
    {

    }

    /// <summary>
    /// TvdbSearchResult constructor
    /// </summary>
    /// <param name="id">Id of series</param>
    public TvdbSearchResult(int id)
    {
      Id = id;
    }

    #region private properties

    #endregion

    #region tvdb properties

    /// <summary>
    /// Id of the returned series
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the returned series
    /// </summary>
    public string SeriesName { get; set; }

    /// <summary>
    /// When was the returned series aired first
    /// </summary>
    public DateTime FirstAired { get; set; }

    /// <summary>
    /// Language of the returned series
    /// </summary>
    public TvdbLanguage Language { get; set; }

    /// <summary>
    /// Overview of the returned series
    /// </summary>
    public string Overview { get; set; }

    /// <summary>
    /// Banner of the returned series
    /// </summary>
    public TvdbSeriesBanner Banner { get; set; }

    /// <summary>
    /// Imdb id of the returned series
    /// </summary>
    public string ImdbId { get; set; }

    #endregion
  }
}
