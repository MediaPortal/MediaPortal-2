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
using System.Text.RegularExpressions;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="SeriesInfo"/> contains information about a series. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class SeriesInfo : BaseInfo
  {
    /// <summary>
    /// Returns the index for "Series" used in <see cref="FormatString"/>.
    /// </summary>
    public static int SERIES_INDEX = 0;
    /// <summary>
    /// Returns the index for "Year" used in <see cref="FormatString"/>.
    /// </summary>
    public static int SERIES_YEAR_INDEX = 1;
    /// <summary>
    /// Format string that holds series name including premiere year.
    /// </summary>
    public static string SERIES_FORMAT_STR = "{0} ({1})";
    /// <summary>
    /// Short format string that holds series name.
    /// </summary>
    public static string SHORT_FORMAT_STR = "{0}";
    /// <summary>
    /// Format string that holds series name, season and episode numbers of next episode.
    /// </summary>
    public static string NEXT_EPISODE_FORMAT_STR = "{0} S{1:00}E{2:00}";

    protected static Regex _fromName = new Regex(@"(?<series>.*) \((?<year>\d+)\)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Gets or sets the series TheTvDB id.
    /// </summary>
    public int TvdbId = 0;
    public int MovieDbId = 0;
    public string ImdbId = null;
    public int TvMazeId = 0;
    public int TvRageId = 0;

    public LanguageText SeriesName = null;
    public string OriginalName = null;
    /// <summary>
    /// Gets or sets the first aired date of series.
    /// </summary>
    public DateTime? FirstAired = null;
    public string Certification = null;
    public LanguageText Description = null;
    public bool IsEnded = false;

    public float Popularity = 0;
    public double Score = 0;
    public double TotalRating = 0;
    public int RatingCount = 0;

    public LanguageText NextEpisodeName = null;
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
      if (SeriesName.IsEmpty) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_SERIES_NAME, SeriesName.Text);
      if (!string.IsNullOrEmpty(OriginalName)) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_ORIG_SERIES_NAME, OriginalName);
      if (FirstAired.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, FirstAired.Value);
      if (!Description.IsEmpty) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_DESCRIPTION, CleanString(Description.Text));
      if (!string.IsNullOrEmpty(Certification)) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_CERTIFICATION, Certification);
      MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_ENDED, IsEnded);

      if(NextEpisodeAirDate.HasValue)
      {
        if(!NextEpisodeName.IsEmpty) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE_NAME, NextEpisodeName.Text);
        else if(NextEpisodeNumber.HasValue && NextEpisodeSeasonNumber.HasValue)
          MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE, string.Format(NEXT_EPISODE_FORMAT_STR, SeriesName, NextEpisodeSeasonNumber.Value, NextEpisodeNumber.Value));
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

      if (Actors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_ACTORS, Actors.Select(p => p.Name).ToList<object>());
      if (Characters.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_CHARACTERS, Characters.Select(p => p.Name).ToList<object>());

      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_GENRES, Genres.ToList<object>());
      if (Awards.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_AWARDS, Awards.ToList<object>());

      if (Networks.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_NETWORKS, Networks.Select(p => p.Name).ToList<object>());
      if (ProductionCompanies.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_COMPANIES, ProductionCompanies.Select(p => p.Name).ToList<object>());

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (aspectData.ContainsKey(SeriesAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_ORIG_SERIES_NAME, out OriginalName);
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, out FirstAired);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_CERTIFICATION, out Certification);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_ENDED, out IsEnded);

        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_POPULARITY, out Popularity);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_TOTAL_RATING, out TotalRating);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_RATING_COUNT, out RatingCount);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_SCORE, out Score);

        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_NEXT_SEASON, out NextEpisodeSeasonNumber);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_NEXT_AIR_DATE, out NextEpisodeAirDate);

        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_SERIES_NAME, out tempString);
        SeriesName = new LanguageText(tempString, false);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_DESCRIPTION, out tempString);
        Description = new LanguageText(tempString, false);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE_NAME, out tempString);
        NextEpisodeName = new LanguageText(tempString, false);

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

        if (aspectData.ContainsKey(VideoAudioAspect.ASPECT_ID))
        {
          Languages.Clear();
          IList<MultipleMediaItemAspect> audioAspects;
          if (MediaItemAspect.TryGetAspects(aspectData, VideoAudioAspect.Metadata, out audioAspects))
          {
            foreach (MultipleMediaItemAspect audioAspect in audioAspects)
            {
              string language = audioAspect.GetAttributeValue<string>(VideoAudioAspect.ATTR_AUDIOLANGUAGE);
              if (!string.IsNullOrEmpty(language))
              {
                if (Languages.Contains(language))
                  Languages.Add(language);
              }
            }
          }
        }

        return true;
      }
      else if (aspectData.ContainsKey(SeasonAspect.ASPECT_ID))
      {
        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, SeasonAspect.ATTR_SERIES_NAME, out tempString);
        SeriesName = new LanguageText(tempString, false);

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
        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_SERIES_NAME, out tempString);
        SeriesName = new LanguageText(tempString, false);

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
        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_TITLE, out tempString);
        SeriesName = new LanguageText(tempString, false);

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

        if (aspectData.ContainsKey(VideoAudioAspect.ASPECT_ID))
        {
          Languages.Clear();
          IList<MultipleMediaItemAspect> audioAspects;
          if (MediaItemAspect.TryGetAspects(aspectData, VideoAudioAspect.Metadata, out audioAspects))
          {
            foreach (MultipleMediaItemAspect audioAspect in audioAspects)
            {
              string language = audioAspect.GetAttributeValue<string>(VideoAudioAspect.ATTR_AUDIOLANGUAGE);
              if (!string.IsNullOrEmpty(language))
              {
                if (Languages.Contains(language))
                  Languages.Add(language);
              }
            }
          }
        }

        return true;
      }
      return false;
    }

    public string ToShortString()
    {
      return string.Format(SHORT_FORMAT_STR, SeriesName);
    }

    public bool FromString(string name)
    {
      if (name.Contains("("))
      {
        Match match = _fromName.Match(name);
        if (match.Success)
        {
          SeriesName = match.Groups["series"].Value;
          int year = Convert.ToInt32(match.Groups["year"].Value);
          if (year > 0)
            FirstAired = new DateTime(year, 1, 1);
          return true;
        }
        return false;
      }
      SeriesName = name;
      return true;
    }

    public bool CopyIdsFrom(SeriesInfo otherSeries)
    {
      MovieDbId = otherSeries.MovieDbId;
      ImdbId = otherSeries.ImdbId;
      TvdbId = otherSeries.TvdbId;
      TvMazeId = otherSeries.TvMazeId;
      TvRageId = otherSeries.TvRageId;
      return true;
    }

    public bool CopyIdsFrom(SeasonInfo seriesSeason)
    {
      MovieDbId = seriesSeason.SeriesMovieDbId;
      ImdbId = seriesSeason.SeriesImdbId;
      TvdbId = seriesSeason.SeriesTvdbId;
      TvMazeId = seriesSeason.SeriesTvMazeId;
      TvRageId = seriesSeason.SeriesTvRageId;
      return true;
    }

    public bool CopyIdsFrom(EpisodeInfo seriesEpisode)
    {
      MovieDbId = seriesEpisode.SeriesMovieDbId;
      ImdbId = seriesEpisode.SeriesImdbId;
      TvdbId = seriesEpisode.SeriesTvdbId;
      TvMazeId = seriesEpisode.SeriesTvMazeId;
      TvRageId = seriesEpisode.SeriesTvRageId;
      return true;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      //if(FirstAired.HasValue)
      //  return string.Format(SERIES_FORMAT_STR, Series, FirstAired.Value.Year);
      return SeriesName.Text;
    }

    public override bool Equals(object obj)
    {
      SeriesInfo other = obj as SeriesInfo;
      if (obj == null) return false;
      if (TvdbId > 0 && TvdbId == other.TvdbId) return true;
      if (MovieDbId > 0 && MovieDbId == other.MovieDbId) return true;
      if (TvMazeId > 0 && TvMazeId == other.TvMazeId) return true;
      if (TvRageId > 0 && TvRageId == other.TvRageId) return true;
      if (!string.IsNullOrEmpty(ImdbId) && !string.IsNullOrEmpty(other.ImdbId) &&
        string.Equals(ImdbId, other.ImdbId, StringComparison.InvariantCultureIgnoreCase))
        return true;
      if (!SeriesName.IsEmpty && !other.SeriesName.IsEmpty &&
        MatchNames(SeriesName.Text, other.SeriesName.Text) && FirstAired.HasValue && other.FirstAired.HasValue &&
        FirstAired.Value == other.FirstAired.Value)
        return true;
      if (!SeriesName.IsEmpty && !other.SeriesName.IsEmpty &&
        MatchNames(SeriesName.Text, other.SeriesName.Text))
        return true;

      return false;
    }

    #endregion
  }
}
