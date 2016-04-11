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
using System.Linq;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="SeriesInfo"/> contains information about a series. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class SeriesInfo
  {
    /// <summary>
    /// Format string that holds series name, season and episode numbers and episode name.
    /// </summary>
    public static string NEXT_EPISODE_FORMAT_STR = "{0} S{1:00}E{2:00}";

    /// <summary>
    /// Gets or sets the series TheTvDB id.
    /// </summary>
    public int TvdbId = 0;
    public int MovieDbId = 0;
    public string ImdbId = null;
    public int TvMazeId = 0;

    public string Series = null;
    public string OriginalName = null;
    /// <summary>
    /// Gets or sets the first aired date of series.
    /// </summary>
    public DateTime? FirstAired = null;
    public string Certification = null;
    public string Description = null;
    public bool IsEnded = false;

    public float Popularity = 0;
    public double Score = 0;
    public double TotalRating = 0;
    public int RatingCount = 0;

    public string NextEpisodeName = null;
    public int? NextEpisodeSeasonNumber = null;
    public int? NextEpisodeNumber = null;
    public DateTime? NextEpisodeAirDate = null;

    /// <summary>
    /// Contains a list of <see cref="CultureInfo.TwoLetterISOLanguageName"/> of the medium. This can be used
    /// to do an online lookup in the best matching language.
    /// </summary>
    public List<string> Languages = new List<string>();
    public List<PersonInfo> Actors = new List<PersonInfo>();
    public List<PersonInfo> Directors = new List<PersonInfo>();
    public List<PersonInfo> Writers = new List<PersonInfo>();
    public List<CharacterInfo> Characters = new List<CharacterInfo>();
    public List<CompanyInfo> Networks = new List<CompanyInfo>();
    public List<CompanyInfo> ProductionCompanys = new List<CompanyInfo>();
    public List<string> Genres = new List<string>();
    public List<string> Awards = new List<string>();

    #region Members

    /// <summary>
    /// Copies the contained series information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(Series)) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, Series);
      MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_SERIES_NAME, Series);
      if (!string.IsNullOrEmpty(OriginalName)) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_ORIG_SERIES_NAME, OriginalName);
      if (FirstAired.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, FirstAired.Value);
      if (!string.IsNullOrEmpty(Description)) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_DESCRIPTION, Description);
      if (!string.IsNullOrEmpty(Certification)) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_CERTIFICATION, Certification);
      MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_ENDED, IsEnded);

      if(NextEpisodeAirDate.HasValue)
      {
        if(!string.IsNullOrEmpty(NextEpisodeName)) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE, NextEpisodeName);
        else if(NextEpisodeNumber.HasValue && NextEpisodeSeasonNumber.HasValue)
          MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE, string.Format(NEXT_EPISODE_FORMAT_STR, Series, NextEpisodeSeasonNumber.Value, NextEpisodeNumber.Value));
        if (NextEpisodeSeasonNumber.HasValue) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_SEASON, NextEpisodeSeasonNumber.Value);
        if (NextEpisodeNumber.HasValue) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE, NextEpisodeNumber.Value);
        MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_AIR_DATE, NextEpisodeAirDate.Value);
      }

      if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, ImdbId);
      if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, TvdbId.ToString());
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, MovieDbId.ToString());
      if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, TvMazeId.ToString());

      if (Popularity > 0f) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_POPULARITY, Popularity);
      if (TotalRating > 0d) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_TOTAL_RATING, TotalRating);
      if (RatingCount > 0) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_RATING_COUNT, RatingCount);
      if (Score > 0d) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_SCORE, Score);

      if (Actors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_ACTORS, Actors.Select(p => p.Name).ToList());
      if (Directors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_DIRECTORS, Directors.Select(p => p.Name).ToList());
      if (Writers.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_WRITERS, Writers.Select(p => p.Name).ToList());
      if (Characters.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_CHARACTERS, Characters.Select(p => p.Name).ToList());

      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_GENRES, Genres);
      if (Awards.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_AWARDS, Awards);

      if (Networks.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_NETWORKS, Networks.Select(p => p.Name).ToList());
      if (ProductionCompanys.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_COMPANYS, ProductionCompanys.Select(p => p.Name).ToList());
      return true;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return Series;
    }

    #endregion
  }
}
