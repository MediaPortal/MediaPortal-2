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
  public class SeriesInfo : BaseInfo
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
    public int TvRageId = 0;

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
    public List<CharacterInfo> Characters = new List<CharacterInfo>();
    public List<CompanyInfo> Networks = new List<CompanyInfo>();
    public List<CompanyInfo> ProductionCompanies = new List<CompanyInfo>();
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
      if (!string.IsNullOrEmpty(Description)) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_DESCRIPTION, CleanString(Description));
      if (!string.IsNullOrEmpty(Certification)) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_CERTIFICATION, Certification);
      MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_ENDED, IsEnded);

      if(NextEpisodeAirDate.HasValue)
      {
        if(!string.IsNullOrEmpty(NextEpisodeName)) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE_NAME, NextEpisodeName);
        else if(NextEpisodeNumber.HasValue && NextEpisodeSeasonNumber.HasValue)
          MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE, string.Format(NEXT_EPISODE_FORMAT_STR, Series, NextEpisodeSeasonNumber.Value, NextEpisodeNumber.Value));
        if (NextEpisodeSeasonNumber.HasValue) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_SEASON, NextEpisodeSeasonNumber.Value);
        if (NextEpisodeNumber.HasValue) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE, new List<int>() { NextEpisodeNumber.Value });
        MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_AIR_DATE, NextEpisodeAirDate.Value);
      }

      if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, ImdbId);
      if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, TvdbId.ToString());
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, MovieDbId.ToString());
      if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, TvMazeId.ToString());
      if (TvRageId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, TvRageId.ToString());

      if (Popularity > 0f) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_POPULARITY, Popularity);
      if (TotalRating > 0d) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_TOTAL_RATING, TotalRating);
      if (RatingCount > 0) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_RATING_COUNT, RatingCount);
      if (Score > 0d) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_SCORE, Score);

      if (Actors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_ACTORS, Actors.Select(p => p.Name).ToList());
      if (Characters.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_CHARACTERS, Characters.Select(p => p.Name).ToList());

      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_GENRES, Genres);
      if (Awards.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_AWARDS, Awards);

      if (Networks.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_NETWORKS, Networks.Select(p => p.Name).ToList());
      if (ProductionCompanies.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_COMPANIES, ProductionCompanies.Select(p => p.Name).ToList());

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (aspectData.ContainsKey(SeriesAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_SERIES_NAME, out Series);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_ORIG_SERIES_NAME, out OriginalName);
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, out FirstAired);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_DESCRIPTION, out Description);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_CERTIFICATION, out Certification);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_ENDED, out IsEnded);

        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_POPULARITY, out Popularity);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_TOTAL_RATING, out TotalRating);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_RATING_COUNT, out RatingCount);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_SCORE, out Score);

        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE_NAME, out NextEpisodeName);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_NEXT_SEASON, out NextEpisodeSeasonNumber);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_NEXT_AIR_DATE, out NextEpisodeAirDate);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvMazeId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvRageId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out ImdbId);

        ICollection<object> collection;
        if (MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE, out collection))
          NextEpisodeNumber = Convert.ToInt32(collection.First());

        Actors.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_ACTORS, out collection))
          Actors.AddRange(collection.Select(s => new PersonInfo() { Name = s.ToString(), Occupation = PersonAspect.OCCUPATION_ACTOR }));

        Characters.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_CHARACTERS, out collection))
          Characters.AddRange(collection.Select(s => new CharacterInfo() { Name = s.ToString() }));

        Genres.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_GENRES, out collection))
          Genres.AddRange(collection.Select(s => s.ToString()));

        Awards.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_AWARDS, out collection))
          Awards.AddRange(collection.Select(s => s.ToString()));

        Networks.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_COMPANIES, out collection))
          Networks.AddRange(collection.Select(s => new CompanyInfo() { Name = s.ToString(), Type = CompanyAspect.COMPANY_TV_NETWORK }));

        ProductionCompanies.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_COMPANIES, out collection))
          ProductionCompanies.AddRange(collection.Select(s => new CompanyInfo() { Name = s.ToString(), Type = CompanyAspect.COMPANY_PRODUCTION }));

        byte[] data;
        if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
          Thumbnail = data;

        return true;
      }
      else if (aspectData.ContainsKey(SeasonAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, SeasonAspect.ATTR_SERIES_NAME, out Series);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvMazeId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvRageId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out ImdbId);

        return true;
      }
      else if (aspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_SERIES_NAME, out Series);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvMazeId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvRageId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out ImdbId);

        return true;
      }
      else if (aspectData.ContainsKey(MediaAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_TITLE, out Series);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvMazeId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvRageId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out ImdbId);

        byte[] data;
        if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
          Thumbnail = data;

        return true;
      }
      return false;
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
