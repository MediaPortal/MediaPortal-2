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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MovieDbLib.Exceptions
{
  /// <summary>
  /// Exception thrown when a request is made which requires a valid
  /// api key but None is set
  /// </summary>
  public class MovieDbInvalidApiKeyException : MovieDbException
  {
    /// <summary>
    /// TvdbInvalidAPIKeyException constructor
    /// </summary>
    /// <param name="_text">Message</param>
    public MovieDbInvalidApiKeyException(String _text)
      : base(_text)
    {
    }

    /// <summary>
    /// TvdbInvalidAPIKeyException constructor
    /// </summary>
    public MovieDbInvalidApiKeyException()
      : base()
    {

    }
  }
}
