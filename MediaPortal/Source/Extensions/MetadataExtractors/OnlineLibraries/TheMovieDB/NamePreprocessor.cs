#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Text.RegularExpressions;
using MediaPortal.Common.MediaManagement.Helpers;

namespace MediaPortal.Extensions.OnlineLibraries.TheMovieDB
{
  /// <summary>
  /// <see cref="NamePreprocessor"/> tries to match movie title, year and other information from filenames, and cleans up titles for online lookup 
  /// using regular expressions.
  /// </summary>
  public class NamePreprocessor
  {
    public const string GROUP_TITLE = "title";
    public const string GROUP_YEAR = "year";
    public static readonly IList<Regex> REGEXP_TITLE_YEAR = new List<Regex>
      {
        new Regex(@"(?<title>[^\\|\/]*?)\s*[\[\(]?(?<year>(19|20)\d{2})[\]\)]?[\.|\\|\/]*", RegexOptions.IgnoreCase), // For LocalFileSystemPath & CanonicalLocalResourcePath
        // Can be extended
      };

    public static readonly IList<Regex> REGEXP_CLEANUPS = new List<Regex>
      {
        // Removing "disc n" from name, this can be used in future to detect multipart titles!
        new Regex(@"(\s|-|_)*(Disc|CD|DVD)\s*\d{1,2}", RegexOptions.IgnoreCase), 
        new Regex(@"\s*(Blu-ray|BD|3D|®|™)", RegexOptions.IgnoreCase), 
        // If source is an ISO or ZIP medium, remove the extensions for lookup
        new Regex(@".(iso|zip)$", RegexOptions.IgnoreCase), 
        new Regex(@"(\s|-)*$", RegexOptions.IgnoreCase), 
        // Can be extended
      };

    protected static Regex _cleanUpWhiteSpaces = new Regex(@"[\.|_](\S|$)");
    protected static Regex _trimWhiteSpaces = new Regex(@"\s{2,}");

    public static bool MatchTitleYear(MovieInfo movieInfo)
    {
      foreach (Regex regex in REGEXP_TITLE_YEAR)
      {
        Match match = regex.Match(movieInfo.MovieName);
        if (match.Groups[GROUP_TITLE].Length > 0 || match.Groups[GROUP_YEAR].Length > 0)
        {
          movieInfo.MovieName = match.Groups[GROUP_TITLE].Value.Trim(new[] { ' ', '-' });
          movieInfo.Year = int.Parse(match.Groups[GROUP_YEAR].Value);
          return true;
        }
      }
      return false;
    }

    public static bool CleanupTitle(MovieInfo movieInfo)
    {
      string originalTitle = movieInfo.MovieName;
      foreach (Regex regex in REGEXP_CLEANUPS)
        movieInfo.MovieName = regex.Replace(movieInfo.MovieName, "");
      movieInfo.MovieName = CleanupWhiteSpaces(movieInfo.MovieName);
      return originalTitle != movieInfo.MovieName;
    }

    /// <summary>
    /// Cleans up strings by replacing unwanted characters (<c>'.'</c>, <c>'_'</c>) by spaces.
    /// </summary>
    public static string CleanupWhiteSpaces(string str)
    {
      if (string.IsNullOrEmpty(str))
        return str;
      str = _cleanUpWhiteSpaces.Replace(str, " $1");
      //replace multiple spaces with single space
      return _trimWhiteSpaces.Replace(str, " ").Trim(' ', '-');
    }
  }
}
