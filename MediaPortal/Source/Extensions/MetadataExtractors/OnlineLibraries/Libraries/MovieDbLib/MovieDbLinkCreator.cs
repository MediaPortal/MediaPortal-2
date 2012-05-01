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
using MovieDbLib.Data;

namespace MovieDb
{

  /// <summary>
  /// Information on server structure and mirrors of tvdb
  /// 
  /// <![CDATA[
  /// <mirrorpath>/api/<apikey>/
  /// |---- mirrors.xml
  /// |---- languages.xml
  /// |
  /// |---- series/
  /// |     |---- <seriesid>/
  /// |           |---- <language>.xml  (Base Series Record)
  /// |           |---- banners.xml  (All banners related to this series)
  /// |           |
  /// |           |---- all/
  /// |           |     |---- <language>.xml  (Full Series Record)
  /// |           |     |---- <language>.zip  (Zipped version of Full Series Record and banners.xml)
  /// |           |
  /// |           |---- default/  (sorts using the default ordering method)
  /// |           |     |---- <Season#>/<episode#>/
  /// |           |           |---- <language>.xml  (Base Episode Record)
  /// |           |
  /// |           |---- dvd/  (sorts using the dvd ordering method)
  /// |           |     |---- <Season#>/<episode#>/
  /// |           |           |---- <language>.xml  (Base Episode Record)
  /// |           |
  /// |           |---- absolute/  (sorts using the absolute ordering method)
  /// |                 |---- <absolute#>/
  /// |                   |---- <language>.xml  (Base Episode Record)
  /// |
  /// |---- episodes
  /// |     |---- <episodeid>/  (will return en.xml by default)
  /// |           |---- <language>.xml  (Base Episode Record)
  /// |
  /// |---- (updates)
  ///       |---- s<timeframe>.xml
  ///       |---- updates_<timeframe>.zip
  /// ]]>
  /// </summary>
  internal class MovieDbLinkCreator
  {
    /// <summary>
    /// Base server where all operations start
    /// </summary>
    internal const String BASE_SERVER = "http://api.themoviedb.org/2.1";

    /// <summary>
    /// Path of file where we get the available languages
    /// </summary>
    internal const String LANG_PATH = "/languages.xml";


    /// <summary>
    /// Creates a Link for a movie
    /// </summary>
    /// <param name="_apiKey"></param>
    /// <param name="_seriesId"></param>
    /// <param name="_lang"></param>
    /// <param name="_full"></param>
    /// <param name="_zipped"></param>
    /// <returns></returns>
    internal static String CreateMovieLink(String _apiKey, int _movieId, MovieDbLanguage _lang, bool _zipped)
    {
      return String.Format("{0}/Movie.getInfo/{1}/xml/{2}/{3}", BASE_SERVER, (_lang != null ? _lang.Abbriviation : "en"), _apiKey,
                           _movieId);
    }

    internal static String CreateImdbLookupLink(string _apiKey, String _imdbKey, MovieDbLanguage _lang, bool _zipped)
    {
      return String.Format("{0}/Movie.imdbLookup/{1}/xml/{2}/{3}", BASE_SERVER, (_lang != null ? _lang.Abbriviation : "en"), _apiKey,
                     _imdbKey);
    }

    internal static string CreateBannerLink(string _bannerPath)
    {
      return "";
    }

    internal static String CreateMovieSearchLink(string _apiKey, String _searchString, MovieDbLanguage _lang)
    {
      return String.Format("{0}/Movie.search/{1}/xml/{2}/{3}", BASE_SERVER, (_lang != null ? _lang.Abbriviation : "en"), _apiKey,
                           _searchString);
    }



    internal static String CreatePersonSearchLink(string _apiKey, String _personName, MovieDbLanguage _lang)
    {
      return String.Format("{0}/Person.search/{1}/xml/{2}/{3}", BASE_SERVER, (_lang != null ? _lang.Abbriviation : "en"), _apiKey,
                     _personName);
    }

    internal static String CreatePersonLink(string _apiKey, int _personId, MovieDbLanguage _lang)
    {
      return String.Format("{0}/Person.getInfo/{1}/xml/{2}/{3}", BASE_SERVER, (_lang != null ? _lang.Abbriviation : "en"), _apiKey,
                     _personId);
    }

    internal static String CreateHashLink(string _apiKey, String _hash)
    {
      return String.Format("{0}/Hash.getInfo/{1}/xml/{2}/{3}", BASE_SERVER, "en", _apiKey, _hash);
    }

  }
}
