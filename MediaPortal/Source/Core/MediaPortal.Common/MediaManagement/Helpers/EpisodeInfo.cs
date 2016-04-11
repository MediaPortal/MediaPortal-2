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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Utilities;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="EpisodeInfo"/> contains metadata information about a series episode item.
  /// </summary>
  /// <remarks>
  /// If all required fields are filled, the <see cref="AreReqiredFieldsFilled"/> 
  /// returns <c>true</c>. The <see cref="ToString"/> method returns a well formatted series title if <see cref="AreReqiredFieldsFilled"/> is <c>true</c>.
  /// </remarks>
  public class EpisodeInfo
  {
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
    /// Format string for constructing a "Series Season" name pattern.
    /// </summary>
    public static string SERIES_SEASON_FORMAT_STR = "{0} S{1}";

    /// <summary>
    /// Used to replace all "." and "_" that are not followed by a word character.
    /// <example>Replaces <c>"Once.Upon.A.Time.S01E13"</c> to <c>"Once Upon A Time S01E13"</c>, but keeps the <c>"."</c> inside
    /// <c>"Dr. House"</c>.</example>
    /// </summary>
    protected static Regex _cleanUpWhiteSpaces = new Regex(@"[\.|_](\S|$)");

    /// <summary>
    /// Indicates that all required fields are filled.
    /// </summary>
    public bool AreReqiredFieldsFilled
    {
      get
      {
        return !(string.IsNullOrEmpty(Series) || !SeasonNumber.HasValue || EpisodeNumbers.Count == 0);
      }
    }

    /// <summary>
    /// Gets or sets the series IMDB id.
    /// </summary>
    public string ImdbId = null;
    /// <summary>
    /// Gets or sets the series TheTvDB id.
    /// </summary>
    public int TvdbId = 0;
    public int MovieDbId = 0;
    public int TvMazeId = 0;

    /// <summary>
    /// Gets or sets the series title.
    /// </summary>
    public string Series = null;
    /// <summary>
    /// Gets or sets the episode title.
    /// </summary>
    public string Episode = null;
    /// <summary>
    /// Gets or sets the season number. A "0" value will be treated as valid season number.
    /// </summary>
    public int? SeasonNumber = null;
    /// <summary>
    /// Gets a list of episode numbers.
    /// </summary>
    public List<int> EpisodeNumbers = new List<int>();
    /// <summary>
    /// Gets a list of episode numbers as they are released on DVD.
    /// </summary>
    public List<double> DvdEpisodeNumbers = new List<double>();
    /// <summary>
    /// Gets or sets the first aired date of episode.
    /// </summary>
    public DateTime? FirstAired = null;
    /// <summary>
    /// Gets or sets the episode summary.
    /// </summary>
    public string Summary = null;
    public string Certification = null;
    public double TotalRating = 0;
    public int RatingCount = 0;

    /// <summary>
    /// Gets a list of actors.
    /// </summary>
    public List<PersonInfo> Actors = new List<PersonInfo>();
    /// <summary>
    /// Gets a list of directors.
    /// </summary>
    public List<PersonInfo> Directors = new List<PersonInfo>();
    /// <summary>
    /// Gets a list of directors.
    /// </summary>
    public List<PersonInfo> Writers = new List<PersonInfo>();
    public List<CharacterInfo> Characters = new List<CharacterInfo>();
    public List<CompanyInfo> Networks = new List<CompanyInfo>();
    public List<CompanyInfo> ProductionCompanys = new List<CompanyInfo>();
    /// <summary>
    /// Gets a list of genres.
    /// </summary>
    public List<string> Genres = new List<string>();
    public List<string> Languages = new List<string>();

    #region Members

    /// <summary>
    /// Cleans up strings by replacing unwanted characters (<c>'.'</c>, <c>'_'</c>) by spaces.
    /// </summary>
    public static string CleanupWhiteSpaces(string str)
    {
      return str == null ? null : _cleanUpWhiteSpaces.Replace(str, " $1").Trim(' ', '-');
    }

    /// <summary>
    /// Copies the contained series information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (!AreReqiredFieldsFilled)
        return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_SERIES_NAME, Series);
      MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_EPISODE_NAME, Episode);
      if (SeasonNumber.HasValue) MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_SEASON, SeasonNumber.Value);
      if (FirstAired.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, FirstAired.Value);
      MediaItemAspect.SetCollectionAttribute(aspectData, EpisodeAspect.ATTR_EPISODE, EpisodeNumbers);
      MediaItemAspect.SetCollectionAttribute(aspectData, EpisodeAspect.ATTR_DVDEPISODE, DvdEpisodeNumbers);
      if (!string.IsNullOrEmpty(Certification)) MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_CERTIFICATION, Certification);

      if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, ImdbId);
      if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, TvdbId.ToString());
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, MovieDbId.ToString());
      if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, TvMazeId.ToString());

      if (TotalRating > 0d) MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_TOTAL_RATING, TotalRating);
      if (RatingCount > 0) MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_RATING_COUNT, RatingCount);

      // Construct a "Series Season" string, which will be used for filtering and season banner retrieval.
      int season = SeasonNumber ?? 0;
      string seriesSeason = string.Format(SERIES_SEASON_FORMAT_STR, Series, season.ToString().PadLeft(2, '0'));
      MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_SERIES_SEASON, seriesSeason);

      if (!string.IsNullOrEmpty(Summary)) MediaItemAspect.SetAttribute(aspectData, VideoAspect.ATTR_STORYPLOT, Summary);
      if (Actors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_ACTORS, Actors.Select(p => p.Name).ToList());
      if (Directors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_DIRECTORS, Directors.Select(p => p.Name).ToList());
      if (Writers.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_WRITERS, Writers.Select(p => p.Name).ToList());
      if (Characters.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_CHARACTERS, Characters.Select(p => p.Name).ToList());

      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_GENRES, Genres);
      return true;
    }

    public string FormatString(string format)
    {
      if (AreReqiredFieldsFilled)
      {
        return string.Format(format,
          Series,
          SeasonNumber.ToString().PadLeft(2, '0'),
          StringUtils.Join(", ", EpisodeNumbers.OrderBy(e => e).Select(episodeNumber => episodeNumber.ToString().PadLeft(2, '0'))),
          Episode);
      }
      return "EpisodeInfo: No complete match";
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
