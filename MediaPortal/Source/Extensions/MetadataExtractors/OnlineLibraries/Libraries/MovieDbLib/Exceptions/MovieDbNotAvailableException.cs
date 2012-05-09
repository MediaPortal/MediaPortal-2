/*
 *   MovieDbLib: A library to retrieve information and media from http://TheMovieDb.org
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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Exceptions
{
  /// <summary>
  /// Exception that is thrown if http://TheMovieDb.org seems to be unavailable
  /// </summary>
  public class MovieDbNotAvailableException : MovieDbException
  {
    /// <summary>
    /// TvdbNotAvailableException constructor
    /// </summary>
    /// <param name="text">Message</param>
    public MovieDbNotAvailableException(String text)
      : base(text)
    {
    }

    /// <summary>
    /// TvdbNotAvailableException constructor
    /// </summary>
    public MovieDbNotAvailableException()
    {
    }
  }
}
