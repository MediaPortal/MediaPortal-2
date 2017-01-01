#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries;
using System.Globalization;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor.Settings;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor.Matchers
{
  /// <summary>
  /// <see cref="MovieNameMatcher"/> tries to match movie title, year and other information from filenames, and cleans up titles for online lookup 
  /// using regular expressions.
  /// </summary>
  public class MovieNameMatcher
  {
    public const string GROUP_TITLE = "title";
    public const string GROUP_YEAR = "year";
    public static readonly IList<Regex> REGEXP_TITLE_YEAR = new List<Regex>
      {
        new Regex(@"(?<title>[^\\|\/]+?)\s*[\[\(]?(?<year>(19|20)\d{2})[\]\)]?[\.|\\|\/]*", RegexOptions.IgnoreCase), // For LocalFileSystemPath & CanonicalLocalResourcePath
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
        // Common tags regex from MovingPictures
        new Regex(@"(([\(\{\[]|\b)((576|720|1080)[pi]|dvd([r59]|rip|scr)|(avc)?hd|wmv|ntsc|pal|mpeg|dsr|r[1-5]|bd[59]|dts|ac3|blu(-)?ray|[hp]dtv|stv|hddvd|xvid|divx|x264|dxva)([\]\)\}]|\b)(-[^\s]+$)?)", RegexOptions.IgnoreCase),
        // Can be extended
      };

    protected static Regex _cleanUpWhiteSpaces = new Regex(@"[\.|_](\S|$)");
    protected static Regex _trimWhiteSpaces = new Regex(@"\s{2,}");

    public static bool MatchTitleYear(string path, MovieInfo movieInfo)
    {
      try
      {
        if (string.IsNullOrEmpty(path))
          return false;
        if (!string.IsNullOrEmpty(movieInfo.ImdbId))
        {
          //Remove IMDB id from file name because it can lead to wrong detection of year
          if (path.Contains("[" + movieInfo.ImdbId + "]"))
            path = path.Replace("[" + movieInfo.ImdbId + "]", "");
          else
            path = path.Replace(movieInfo.ImdbId, "");
        }

        var settings = ServiceRegistration.Get<ISettingsManager>().Load<MovieMetadataExtractorSettings>();

        foreach (SerializableRegex regex in settings.MovieYearPatterns)
        {
          Match match = regex.Regex.Match(path);
          if (match.Groups[GROUP_TITLE].Length > 0 || match.Groups[GROUP_YEAR].Length > 0)
          {
            string title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(match.Groups[GROUP_TITLE].Value.Trim(new[] { ' ', '-' }));
            movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref movieInfo.MovieName, title, true);
            movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref movieInfo.ReleaseDate, new DateTime(int.Parse(match.Groups[GROUP_YEAR].Value), 1, 1));
            return true;
          }
        }
      }
      catch(Exception e)
      {
        ServiceRegistration.Get<ILogger>().Info("MovieNameMatcher: Exception matching title year for title '{0}' (Text: '{1}')", movieInfo.MovieName, e.Message);
      }
      return false;
    }

    public static bool CleanupTitle(MovieInfo movieInfo)
    {
      try
      {
        if (movieInfo.MovieName.IsEmpty)
          return false;

        string originalTitle = movieInfo.MovieName.Text;
        foreach (Regex regex in REGEXP_CLEANUPS)
          movieInfo.MovieName.Text = regex.Replace(movieInfo.MovieName.Text, "");
        movieInfo.MovieName.Text = CleanupWhiteSpaces(movieInfo.MovieName.Text);
        return originalTitle != movieInfo.MovieName.Text;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Info("MovieNameMatcher: Exception cleaning title '{0}' (Text: '{1}')", movieInfo.MovieName, e.Message);
      }
      return false;
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
