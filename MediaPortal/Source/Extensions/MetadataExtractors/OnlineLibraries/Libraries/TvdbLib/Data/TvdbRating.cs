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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data
{
  /// <summary>
  /// Represents a rating entry from thetvdb
  /// </summary>
  public class TvdbRating
  {
    #region private properties

    #endregion

    /// <summary>
    /// Enum with all items on thetvdb that can be rated
    /// </summary>
    public enum ItemType { 
      /// <summary>
      /// Item is a series
      /// </summary>
      Series, 
      /// <summary>
      /// Item is an episode
      /// </summary>
      Episode }

    /// <summary>
    /// Which item type is this rating for
    /// </summary>
    public ItemType RatingItemType { get; set; }

    /// <summary>
    /// Community Rating is a double value from 0 to 10 and is the mean value of all user ratings for this item
    /// </summary>
    public double CommunityRating { get; set; }

    /// <summary>
    /// The rating from this user
    /// </summary>
    public int UserRating { get; set; }
  }
}
