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
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib
{
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
    internal static String CreateMovieLink(String apiKey, int movieId, MovieDbLanguage lang, bool zipped)
    {
      return String.Format("{0}/Movie.getInfo/{1}/xml/{2}/{3}", BASE_SERVER, (lang != null ? lang.Abbriviation : "en"), apiKey, movieId);
    }

    internal static String CreateImdbLookupLink(string apiKey, String imdbKey, MovieDbLanguage lang, bool zipped)
    {
      return String.Format("{0}/Movie.imdbLookup/{1}/xml/{2}/{3}", BASE_SERVER, (lang != null ? lang.Abbriviation : "en"), apiKey, imdbKey);
    }

    internal static String CreateMovieSearchLink(string apiKey, String searchString, MovieDbLanguage lang)
    {
      return String.Format("{0}/Movie.search/{1}/xml/{2}/{3}", BASE_SERVER, (lang != null ? lang.Abbriviation : "en"), apiKey, searchString);
    }
    
    internal static String CreatePersonSearchLink(string apiKey, String personName, MovieDbLanguage lang)
    {
      return String.Format("{0}/Person.search/{1}/xml/{2}/{3}", BASE_SERVER, (lang != null ? lang.Abbriviation : "en"), apiKey, personName);
    }

    internal static String CreatePersonLink(string apiKey, int personId, MovieDbLanguage lang)
    {
      return String.Format("{0}/Person.getInfo/{1}/xml/{2}/{3}", BASE_SERVER, (lang != null ? lang.Abbriviation : "en"), apiKey, personId);
    }

    internal static String CreateHashLink(string apiKey, String hash)
    {
      return String.Format("{0}/Hash.getInfo/{1}/xml/{2}/{3}", BASE_SERVER, "en", apiKey, hash);
    }
  }
}
