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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System.Text.RegularExpressions;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="SeasonInfo"/> contains information about a series season. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class SeasonInfo : BaseInfo
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
    /// Format string that holds series name and season number.
    /// </summary>
    public static string SEASON_FORMAT_STR = "{0} S{1:00}";
    /// <summary>
    /// Short format string that holds season number. Used for browsing seasons by series name.
    /// </summary>
    public static string SHORT_FORMAT_STR = "S{1:00}";

    protected static Regex _fromName = new Regex(@"(?<series>[^\s]*) S(?<season>\d{1,2})", RegexOptions.IgnoreCase);

    /// <summary>
    /// Gets or sets the season IMDB id.
    /// </summary>
    public string ImdbId = null;
    /// <summary>
    /// Gets or sets the season TheTvDB id.
    /// </summary>
    public int TvdbId = 0;
    public int MovieDbId = 0;
    public int TvMazeId = 0;
    public int TvRageId = 0;

    /// <summary>
    /// Gets or sets the series IMDB id.
    /// </summary>
    public string SeriesImdbId = null;
    /// <summary>
    /// Gets or sets the series TheTvDB id.
    /// </summary>
    public int SeriesTvdbId = 0;
    public int SeriesMovieDbId = 0;
    public int SeriesTvMazeId = 0;
    public int SeriesTvRageId = 0;

    /// <summary>
    /// Gets or sets the series title.
    /// </summary>
    public SimpleTitle SeriesName = null;
    /// <summary>
    /// Gets or sets the season number. A "0" value will be treated as valid season number.
    /// </summary>
    public int? SeasonNumber = null;
    /// <summary>
    /// Gets or sets the first aired date of season.
    /// </summary>
    public DateTime? FirstAired = null;
    /// <summary>
    /// Gets or sets the season description.
    /// </summary>
    public SimpleTitle Description = null;
    public int TotalEpisodes = 0;

    public override bool IsBaseInfoPresent
    {
      get
      {
        if (SeriesName.IsEmpty)
          return false;
        if (!SeasonNumber.HasValue)
          return false;

        return true;
      }
    }

    public override bool HasExternalId
    {
      get
      {
        if (TvdbId > 0)
          return true;
        if (MovieDbId > 0)
          return true;
        if (TvMazeId > 0)
          return true;
        if (TvRageId > 0)
          return true;
        if (!string.IsNullOrEmpty(ImdbId))
          return true;

        if (SeriesTvdbId > 0)
          return true;
        if (SeriesMovieDbId > 0)
          return true;
        if (SeriesTvMazeId > 0)
          return true;
        if (SeriesTvRageId > 0)
          return true;
        if (!string.IsNullOrEmpty(SeriesImdbId))
          return true;

        return false;
      }
    }

    #region Members

    /// <summary>
    /// Copies the contained series information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public override bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (SeriesName.IsEmpty || !SeasonNumber.HasValue) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, GetSortTitle(ToString()));
      //MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_ISVIRTUAL, true); //Is maintained by medialibrary and metadata extractors
      MediaItemAspect.SetAttribute(aspectData, SeasonAspect.ATTR_SERIES_NAME, SeriesName.Text);
      if (!Description.IsEmpty) MediaItemAspect.SetAttribute(aspectData, SeasonAspect.ATTR_DESCRIPTION, CleanString(Description.Text));
      if (SeasonNumber.HasValue) MediaItemAspect.SetAttribute(aspectData, SeasonAspect.ATTR_SEASON, SeasonNumber.Value);
      if (FirstAired.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, FirstAired.Value);
      if (TotalEpisodes > 0) MediaItemAspect.SetAttribute(aspectData, SeasonAspect.ATTR_NUM_EPISODES, TotalEpisodes);

      if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SEASON, ImdbId);
      if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SEASON, TvdbId.ToString());
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SEASON, MovieDbId.ToString());
      if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SEASON, TvMazeId.ToString());
      if (TvRageId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SEASON, TvRageId.ToString());

      if (!string.IsNullOrEmpty(SeriesImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, SeriesImdbId);
      if (SeriesTvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, SeriesTvdbId.ToString());
      if (SeriesMovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, SeriesMovieDbId.ToString());
      if (SeriesTvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, SeriesTvMazeId.ToString());
      if (SeriesTvRageId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, SeriesTvRageId.ToString());

      // Construct a "Series Season" string, which will be used for filtering and season banner retrieval.
      MediaItemAspect.SetAttribute(aspectData, SeasonAspect.ATTR_SERIES_SEASON, ToString());

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public override bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (aspectData.ContainsKey(SeasonAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, SeasonAspect.ATTR_SEASON, out SeasonNumber);
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, out FirstAired);

        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, SeasonAspect.ATTR_SERIES_NAME, out tempString);
        SeriesName = new SimpleTitle(tempString, false);
        MediaItemAspect.TryGetAttribute(aspectData, SeasonAspect.ATTR_DESCRIPTION, out tempString);
        Description = new SimpleTitle(tempString, false);

        int? count;
        if (MediaItemAspect.TryGetAttribute(aspectData, SeasonAspect.ATTR_NUM_EPISODES, out count))
          TotalEpisodes = count.Value;

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SEASON, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SEASON, out id))
          TvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SEASON, out id))
          TvMazeId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SEASON, out id))
          TvRageId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SEASON, out ImdbId);

        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesMovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesTvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesTvMazeId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesTvRageId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out SeriesImdbId);

        byte[] data;
        if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
          Thumbnail = data;

        return true;
      }
      else if (aspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_SEASON, out SeasonNumber);

        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_SERIES_NAME, out tempString);
        SeriesName = new SimpleTitle(tempString, false);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesMovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesTvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesTvMazeId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesTvRageId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out SeriesImdbId);

        return true;
      }
      return false;
    }

    public string ToShortString()
    {
      return string.Format(SHORT_FORMAT_STR, SeriesName, SeasonNumber ?? 0);
    }

    public override bool FromString(string name)
    {
      Match match = _fromName.Match(name);
      if (match.Success)
      {
        SeriesName = match.Groups["series"].Value;
        SeasonNumber = Convert.ToInt32(match.Groups["season"].Value);
        return true;
      }
      return false;
    }

    public override bool CopyIdsFrom<T>(T otherInstance)
    {
      if (otherInstance == null)
        return false;

      if (otherInstance is SeriesInfo)
      {
        SeriesInfo seasonSeries = otherInstance as SeriesInfo;
        SeriesImdbId = seasonSeries.ImdbId;
        SeriesMovieDbId = seasonSeries.MovieDbId;
        SeriesTvdbId = seasonSeries.TvdbId;
        SeriesTvMazeId = seasonSeries.TvMazeId;
        SeriesTvRageId = seasonSeries.TvRageId;
        return true;
      }
      else if (otherInstance is SeasonInfo)
      {
        SeasonInfo otherSeason = otherInstance as SeasonInfo;
        MovieDbId = otherSeason.MovieDbId;
        ImdbId = otherSeason.ImdbId;
        TvdbId = otherSeason.TvdbId;
        TvMazeId = otherSeason.TvMazeId;
        TvRageId = otherSeason.TvRageId;

        SeriesImdbId = otherSeason.SeriesImdbId;
        SeriesMovieDbId = otherSeason.SeriesMovieDbId;
        SeriesTvdbId = otherSeason.SeriesTvdbId;
        SeriesTvMazeId = otherSeason.SeriesTvMazeId;
        SeriesTvRageId = otherSeason.SeriesTvRageId;
        return true;
      }
      else if (otherInstance is EpisodeInfo)
      {
        EpisodeInfo seasonEpisode = otherInstance as EpisodeInfo;
        SeriesImdbId = seasonEpisode.SeriesImdbId;
        SeriesMovieDbId = seasonEpisode.SeriesMovieDbId;
        SeriesTvdbId = seasonEpisode.SeriesTvdbId;
        SeriesTvMazeId = seasonEpisode.SeriesTvMazeId;
        SeriesTvRageId = seasonEpisode.SeriesTvRageId;
        return true;
      }
      return false;
    }

    public override T CloneBasicInstance<T>()
    {
      if (typeof(T) == typeof(SeriesInfo))
      {
        SeriesInfo info = new SeriesInfo();
        info.ImdbId = SeriesImdbId;
        info.MovieDbId = SeriesMovieDbId;
        info.TvdbId = SeriesTvdbId;
        info.TvMazeId = SeriesTvMazeId;
        info.TvRageId = SeriesTvRageId;

        SeriesName = new SimpleTitle(SeriesName.Text, SeriesName.DefaultLanguage);
        return (T)(object)info;
      }
      return default(T);
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return string.Format(SEASON_FORMAT_STR,
        SeriesName, 
        SeasonNumber ?? 0);
    }

    public override bool Equals(object obj)
    {
      SeasonInfo other = obj as SeasonInfo;
      if (other == null) return false;

      if (TvdbId > 0 && other.TvdbId > 0)
        return TvdbId == other.TvdbId;
      if (MovieDbId > 0 && other.MovieDbId > 0)
        return MovieDbId == other.MovieDbId;
      if (TvMazeId > 0 && other.TvMazeId > 0)
        return TvMazeId == other.TvMazeId;
      if (TvRageId > 0 && other.TvRageId > 0)
        return TvRageId == other.TvRageId;
      if (!string.IsNullOrEmpty(ImdbId) && !string.IsNullOrEmpty(other.ImdbId))
        return string.Equals(ImdbId, other.ImdbId, StringComparison.InvariantCultureIgnoreCase);
      if (!SeriesName.IsEmpty && !other.SeriesName.IsEmpty && SeriesName.Text == other.SeriesName.Text &&
        SeasonNumber.HasValue && SeasonNumber == other.SeasonNumber)
        return true;

      return false;
    }

    public override int GetHashCode()
    {
      //TODO: Check if this is functional
      return (SeriesName.IsEmpty ? "Unnamed Season" : ToString()).GetHashCode();
    }

    #endregion
  }
}
