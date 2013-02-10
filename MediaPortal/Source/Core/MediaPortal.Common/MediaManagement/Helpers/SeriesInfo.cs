#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Utilities;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="SeriesInfo"/> contains metadata information about a series episode item.
  /// </summary>
  /// <remarks>
  /// If all required fields are filled, the <see cref="IsCompleteMatch"/> 
  /// returns <c>true</c>. The <see cref="ToString"/> method returns a well formatted series title if <see cref="IsCompleteMatch"/> is <c>true</c>.
  /// </remarks>
  public class SeriesInfo
  {
    #region Fields

    protected string _series;
    protected string _episode;

    #endregion

    /// <summary>
    /// Returns the index for "Series" used in <see cref="FormatString"/>.
    /// </summary>
    public static int SERIES_INDEX = 0;
    /// <summary>
    /// Returns the index for "Season" used in <see cref="FormatString"/>.
    /// </summary>
    public static int SEASON_INDEX = 1;
    /// <summary>
    /// Returns the index for "Episode Number(s)" used in <see cref="FormatString"/>.
    /// </summary>
    public static int EPISODENUM_INDEX = 2;
    /// <summary>
    /// Returns the index for "Episode" used in <see cref="FormatString"/>.
    /// </summary>
    public static int EPISODE_INDEX = 3;
    /// <summary>
    /// Format string that holds series name, season and episode numbers and episode name.
    /// </summary>
    public static string EPISODE_FORMAT_STR = "{0} S{1}E{2} - {3}";
    /// <summary>
    /// Short format string that holds season and episode numbers and episode name. Used for browsing episodes by series name.
    /// </summary>
    public static string SHORT_FORMAT_STR = "S{1}E{2} - {3}";

    /// <summary>
    /// Used to replace all "." and "_" that are not followed by a word character.
    /// <example>Replaces <c>"Once.Upon.A.Time.S01E13"</c> to <c>"Once Upon A Time S01E13"</c>, but keeps the <c>"."</c> inside
    /// <c>"Dr. House"</c>.</example>
    /// </summary>
    protected static Regex _cleanUpWhiteSpaces = new Regex(@"[\.|_](\S|$)");

    /// <summary>
    /// Indicates that all required fields are filled.
    /// </summary>
    public bool IsCompleteMatch
    {
      get
      {
        return !(string.IsNullOrEmpty(Series) || SeasonNumber == 0 || EpisodeNumbers.Count == 0);
      }
    }
    /// <summary>
    /// Gets or sets the series title. When setting a value, whitespaces will be cleaned up (<see cref="CleanupWhiteSpaces"/>).
    /// </summary>
    public string Series
    {
      get { return _series; }
      set { _series = CleanupWhiteSpaces(value); }
    }

    /// <summary>
    /// Gets or sets the episode title. When setting a value, whitespaces will be cleaned up (<see cref="CleanupWhiteSpaces"/>).
    /// </summary>
    public string Episode
    {
      get { return _episode; }
      set { _episode = CleanupWhiteSpaces(value); }
    }

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    public int SeasonNumber;

    /// <summary>
    /// Gets a list of episode numbers.
    /// </summary>
    public IList<int> EpisodeNumbers { get; internal set; }

    /// <summary>
    /// Gets or sets the episode summary.
    /// </summary>
    public string Summary { get; set; }

    /// <summary>
    /// Gets a list of directors.
    /// </summary>
    public ICollection<string> Directors { get; internal set; }

    /// <summary>
    /// Gets a list of actors.
    /// </summary>
    public ICollection<string> Actors { get; internal set; }

    /// <summary>
    /// Gets a list of genres.
    /// </summary>
    public ICollection<string> Genres { get; internal set; }

    #region Constructor

    public SeriesInfo()
    {
      EpisodeNumbers = new List<int>();
      Directors = new HashSet<string>();
      Actors = new HashSet<string>();
      Genres = new HashSet<string>();
    }

    #endregion

    #region Members

    /// <summary>
    /// Cleans up strings by replacing unwanted characters (<c>'.'</c>, <c>'_'</c>) by spaces.
    /// </summary>
    private static string CleanupWhiteSpaces(string str)
    {
      return str == null ? null : _cleanUpWhiteSpaces.Replace(str, " $1").Trim(' ', '-');
    }

    /// <summary>
    /// Copies the contained series information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, MediaItemAspect> aspectData)
    {
      if (!IsCompleteMatch)
        return false;

      MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_SERIESNAME, Series);
      MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_EPISODENAME, Episode);
      MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_SEASON, SeasonNumber);
      MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_EPISODE, EpisodeNumbers);

      if (!string.IsNullOrEmpty(Summary)) MediaItemAspect.SetAttribute(aspectData, VideoAspect.ATTR_STORYPLOT, Summary);
      if (Directors.Count > 0) MediaItemAspect.SetAttribute(aspectData, VideoAspect.ATTR_DIRECTOR, Directors.First());
      if (Actors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_ACTORS, Actors);
      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_GENRES, Genres);
      return true;
    }

    public string FormatString(string format)
    {
      if (IsCompleteMatch)
      {
        return string.Format(format,
          Series,
          SeasonNumber.ToString().PadLeft(2, '0'),
          StringUtils.Join(", ", EpisodeNumbers.Select(episodeNumber => episodeNumber.ToString().PadLeft(2, '0'))),
          Episode);
      }
      return "SeriesInfo: No complete match";
    }

    public string ToShortString()
    {
      return FormatString(SHORT_FORMAT_STR);
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return FormatString(EPISODE_FORMAT_STR);
    }

    #endregion
  }
}